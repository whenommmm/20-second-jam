// Test-only: headless Chrome here never advances animations, so step keyframes on timers instead.
window.requestAnimationFrame = cb => setTimeout(() => cb(performance.now()), 16);
window.onerror = (m, s, l) => { document.title = "ERR " + m + " @" + l; };
window.addEventListener("unhandledrejection", e => { document.title = "REJ " + e.reason; });
(function () {
  const anims = [];
  Element.prototype.animate = function (frames, opts) {
    const el = this, o = typeof opts === "number" ? { duration: opts } : (opts || {});
    const dur = o.duration || 1, delay = o.delay || 0, fill = o.fill || "none", start = performance.now();
    let resolve; const finished = new Promise(r => (resolve = r));
    const a = { el, done: false, finished, playState: "running", cancel() { a.done = true; a.playState = "idle"; } };
    const n = frames.length;
    const offs = frames.map((f, i) => (f.offset != null ? f.offset : n === 1 ? 1 : i / (n - 1)));
    const apply = p => {
      const props = {};
      frames.forEach((f, i) => { if (offs[i] <= p + 1e-9) for (const k in f) if (k !== "offset" && k !== "easing") props[k] = f[k]; });
      for (const k in props) el.style[k] = props[k];
      return props;
    };
    const tick = () => {
      if (a.done) return;
      const t = performance.now() - start - delay;
      if (t < 0) { if (fill === "backwards" || fill === "both") apply(0); return setTimeout(tick, 16); }
      const p = Math.min(1, t / dur), props = apply(p);
      if (p >= 1) {
        a.done = true; a.playState = "finished";
        if (fill !== "forwards" && fill !== "both") for (const k in props) el.style[k] = "";
        resolve(a); return;
      }
      setTimeout(tick, 16);
    };
    tick(); anims.push(a); return a;
  };
  Element.prototype.getAnimations = function () { return anims.filter(a => a.el === this && !a.done); };
  const st = document.createElement("style");
  st.textContent = "*{transition:none!important;animation-duration:0s!important;animation-delay:0s!important}";
  document.head.appendChild(st);
})();
