// Everything from "TIME!" to PLAY AGAIN: the results screen, the shrink into the phone, the three flights,
// the lock screen and its Messages sheet, her words beside the phone, TURN BACK TIME.
// Ported from the mockup (endRound, morph, fly, endingMessages, playCaption, turnBack).
import { W, CALLS, VOICEMAIL, LAST_CALL, CAPTION_HEADERS } from "./data.js";
import { rankFor, endingFor, buildThread, turnBackLines, shortDate } from "./rules.js";

const $ = id => document.getElementById(id);
const TOTAL_PILLS = CALLS.reduce((a, c) => a + c.n, 0);

export function playEnding(round, { onRestart }) {
  const timers = [], anims = [];
  let caption = null, disposed = false;
  const ending = endingFor(round.calls);
  const later = (ms, fn) => timers.push(setTimeout(() => { if (!disposed) fn(); }, ms));
  const animate = (el, frames, opts) => { const a = el.animate(frames, opts); anims.push(a); return a; };
  const scale = () => $("world").getBoundingClientRect().width / W;

  // ---- TIME! and the results screen ----
  function countUp(el, to, ms) {
    const t0 = performance.now();
    const id = setInterval(() => {
      const k = Math.min(1, (performance.now() - t0) / ms), e = 1 - Math.pow(1 - k, 3);
      el.textContent = Math.round(to * e).toLocaleString();
      if (k >= 1) clearInterval(id);
    }, 16);
    timers.push(id);
  }

  $("timeov").classList.add("on");
  later(800, () => {
    $("timeov").classList.remove("on");
    $("card").className = "on";
    $("play").style.opacity = 0;
    // the moon glides over to sit beside your score
    const w = $("mewrap");
    w.style.transition = "transform .7s cubic-bezier(.3,0,.2,1)";
    w.style.transform = `translate(calc(-50% + ${720 - round.me.x}px), calc(-50% + ${170 - round.me.y}px)) scale(1.7)`;
    $("resnum").textContent = "0";
    later(150, () => countUp($("resnum"), round.score, 900));
    $("r1").textContent = `${round.dodged} / ${TOTAL_PILLS}`; $("r2").textContent = round.closeN; $("r3").textContent = `×${round.best}`;
    document.querySelectorAll("#res .row").forEach((row, i) => later(700 + i * 170, () => row.classList.add("on")));
    $("rank").textContent = rankFor(round.hits);
    later(1550, () => $("rank").classList.add("on"));
    if (round.hits === 0) later(2500, () => $("doubt").classList.add("on")); // only a perfect round earns the doubt
    later(4100, morph);
  });

  // ---- the shrink into the phone, and the flights home ----

  // world-space box of any element, whatever its transforms
  function box(el) {
    const r = el.getBoundingClientRect(), w = $("world").getBoundingClientRect(), s = scale();
    return { x: (r.left - w.left) / s, y: (r.top - w.top) / s, w: r.width / s, h: r.height / s };
  }

  // a stand-in that starts exactly where `src` is drawn
  function flyer(src, html, style) {
    const b = box(src), d = document.createElement("div");
    d.className = "flyer"; d.innerHTML = html;
    Object.assign(d.style, { left: b.x + "px", top: b.y + "px", width: b.w + "px", height: b.h + "px" }, style);
    $("fly").appendChild(d);
    return { el: d, b };
  }

  // glide along a gentle arc and shrink into `dest`, landing exactly on it
  function fly(f, dest, o) {
    const s = f.b, e = box(dest);
    const sx = s.x + s.w / 2, sy = s.y + s.h / 2, ex = e.x + e.w / 2, ey = e.y + e.h / 2;
    const c = o.ctrl(sx, sy, ex, ey), end = e.h / s.h;
    const frames = [];
    for (let i = 0; i <= 30; i++) {
      const u = i / 30, v = 1 - u;
      const x = v * v * sx + 2 * v * u * c.x + u * u * ex, y = v * v * sy + 2 * v * u * c.y + u * u * ey;
      const sc = 1 + (end - 1) * (1 - Math.pow(1 - u, 1.6));
      frames.push({ offset: u, transform: `translate(${(x - sx).toFixed(2)}px, ${(y - sy).toFixed(2)}px) rotate(${((o.rot || 0) * v).toFixed(2)}deg) scale(${sc.toFixed(4)})` });
    }
    (o.extra || []).forEach(k => frames.push(k));
    const a = animate(f.el, frames.sort((p, q) => p.offset - q.offset), { duration: o.dur, delay: o.delay || 0, easing: "cubic-bezier(.45,.05,.2,1)", fill: "forwards" });
    return a.finished.then(() => { if (disposed) throw new Error("disposed"); });
  }
  const quietly = p => p.catch(() => {}); // a cancelled flight (restart mid-ending) is not an error

  function morph() {
    $("res").style.opacity = 0;
    later(180, () => $("card").classList.add("phone"));
    later(1000, () => $("lock").classList.add("on"));
    later(1250, () => {
      // fill in the missed-calls group now so the badge has somewhere to land
      const missedCalls = round.calls.filter(c => c.missedN);
      $("gcount").textContent = round.missed;
      $("group").querySelector(".tt b").lastChild.textContent = round.missed === 1 ? " Missed Call" : " Missed Calls";
      $("gnames").textContent = [...new Set(missedCalls.slice().reverse().map(c => c.who))].join(", ");
      $("group").querySelector(".tt > span").textContent = missedCalls.length ? shortDate(missedCalls[missedCalls.length - 1].date) : "";

      // swap the real moon, tag and badge for stand-ins, then fly each one home. The moon and its name travel with
      // a pill that grows out around them, and LATER turns into "Later" on the way: together they become "☾ Later".
      const tag = document.querySelector("#me .tag");
      const textScale = box($("focusText")).h / box(tag).h;
      const moonF = flyer($("mesvg"), `<svg viewBox="0 0 24 24"><use href="#moon" fill="currentColor"/></svg>`, { color: "#141414" });
      const tagF = flyer(tag, `<span class="was">LATER</span><span class="now" style="font-size:${(11 / textScale).toFixed(2)}px">Later</span>`,
        { fontFamily: '"Archivo Black", sans-serif', fontSize: (14 * 1.7) + "px", letterSpacing: (1.5 * 1.7) + "px", color: "#141414", lineHeight: "1" });
      const pillBox = box($("focus")), pillBg = document.createElement("div");
      pillBg.className = "flyer pillbg";
      Object.assign(pillBg.style, { left: pillBox.x + "px", top: pillBox.y + "px", width: pillBox.w + "px", height: pillBox.h + "px" });
      $("fly").prepend(pillBg); // behind the moon and its name
      const hasBadge = round.missed > 0;
      $("group").style.display = hasBadge ? "" : "none";
      const badgeF = hasBadge ? flyer($("badge"), String(round.missed), { background: "#FF3B30", color: "#fff", borderRadius: "999px", fontFamily: '"Archivo Black", sans-serif', fontSize: (14 * 1.7) + "px" }) : null;
      $("me").style.visibility = "hidden";

      // swoop down and under the clock, then rise into the pill; turn white as they cross onto the screen
      const under = dip => (sx, sy, ex, ey) => ({ x: (sx + ex) / 2 + 30, y: Math.max(sy, ey) + dip });
      const toWhite = [{ offset: 0, color: "#141414" }, { offset: .36, color: "#141414" }, { offset: .58, color: "#ffffff" }, { offset: 1, color: "#ffffff" }];
      // one path for the moon; its name and the pill ride along with it, so the three move and change as one piece:
      // the pill grows out around the moon, and LATER slides from under it to beside it as it becomes "Later"
      const DUR = 1300, EASE = "cubic-bezier(.45,.05,.2,1)";
      const m = moonF.b, t0 = tagF.b, ic = box($("focusIcon")), tx = box($("focusText"));
      const c0 = { x: m.x + m.w / 2, y: m.y + m.h / 2 }, c1 = { x: ic.x + ic.w / 2, y: ic.y + ic.h / 2 }, cc = under(120)(c0.x, c0.y, c1.x, c1.y);
      const tagC = { x: t0.x + t0.w / 2, y: t0.y + t0.h / 2 }, pillC = { x: pillBox.x + pillBox.w / 2, y: pillBox.y + pillBox.h / 2 };
      const tagFrom = { x: tagC.x - c0.x, y: tagC.y - c0.y }, tagTo = { x: tx.x + tx.w / 2 - c1.x, y: tx.y + tx.h / 2 - c1.y };
      const pillOff = { x: pillC.x - c1.x, y: pillC.y - c1.y };
      const fm = [], ft = [], fp = [];
      for (let i = 0; i <= 40; i++) {
        const u = i / 40, v = 1 - u, k = 1 - Math.pow(v, 1.6), blend = u * u * (3 - 2 * u);
        const px = v * v * c0.x + 2 * v * u * cc.x + u * u * c1.x, py = v * v * c0.y + 2 * v * u * cc.y + u * u * c1.y;
        const sm = 1 + (ic.h / m.h - 1) * k, st = 1 + (tx.h / t0.h - 1) * k, sp = 0.2 + 0.8 * k;
        fm.push({ offset: u, transform: `translate(${(px - c0.x).toFixed(2)}px, ${(py - c0.y).toFixed(2)}px) rotate(${(round.tilt * v).toFixed(2)}deg) scale(${sm.toFixed(4)})` });
        const tcx = px + tagFrom.x + (tagTo.x - tagFrom.x) * blend, tcy = py + tagFrom.y + (tagTo.y - tagFrom.y) * blend;
        ft.push({ offset: u, transform: `translate(${(tcx - tagC.x).toFixed(2)}px, ${(tcy - tagC.y).toFixed(2)}px) scale(${st.toFixed(4)})` });
        fp.push({ offset: u, transform: `translate(${(px + pillOff.x * sp - pillC.x).toFixed(2)}px, ${(py + pillOff.y * sp - pillC.y).toFixed(2)}px) scale(${sp.toFixed(4)})` });
      }
      const timing = { duration: DUR, easing: EASE, fill: "forwards" }, byOffset = (p, q) => p.offset - q.offset;
      const moonFlight = animate(moonF.el, [...fm, ...toWhite].sort(byOffset), timing);
      animate(tagF.el, [...ft, ...toWhite.map(k => ({ ...k }))].sort(byOffset), timing);
      animate(pillBg, [...fp, { offset: 0, opacity: 0 }, { offset: .4, opacity: 0 }, { offset: .9, opacity: 1 }, { offset: 1, opacity: 1 }].sort(byOffset), timing);
      const crossfade = (el, from, to) => animate(el, [{ offset: 0, opacity: from }, { offset: .55, opacity: from }, { offset: .85, opacity: to }, { offset: 1, opacity: to }], timing);
      crossfade(tagF.el.querySelector(".was"), 1, 0); crossfade(tagF.el.querySelector(".now"), 0, 1);
      quietly(moonFlight.finished.then(() => {
        if (disposed) return;
        // everything has landed exactly on the real pill: hand over in the same frame, so nothing blinks
        $("focus").style.opacity = 1;
        moonF.el.remove(); tagF.el.remove(); pillBg.remove();
        $("sbMoon").classList.add("on");
        later(500, () => $("sil").classList.add("on"));
      }));
      if (hasBadge) {
        animate($("group"), [{ opacity: 0, transform: "translateY(8px)" }, { opacity: 1, transform: "none" }], { duration: 380, delay: 1050, fill: "forwards", easing: "ease-out" });
        animate($("gcount"), [{ opacity: 0 }, { opacity: 1 }], { duration: 200, delay: 1500, fill: "backwards" });
        quietly(fly(badgeF, $("gcount"), { dur: 1500, delay: 120, ctrl: (sx, sy, ex, ey) => ({ x: sx - 10, y: ey + 10 }), extra: [{ offset: 0, backgroundColor: "#FF3B30" }, { offset: .75, backgroundColor: "#FF3B30" }, { offset: 1, backgroundColor: "rgba(255,59,48,0)" }] })
          .then(() => animate(badgeF.el, [{ opacity: 1 }, { opacity: 0 }], { duration: 150, fill: "forwards" })));
      }
      later(2600, endingMessages);
    });
  }

  // ---- the lock screen ----

  // a notification card, laid out but hidden until it's revealed
  function note(icon, title, when, body, cls) {
    const n = document.createElement("div");
    n.className = "n pending" + (cls ? " " + cls : "");
    n.innerHTML = `<div class="ic"><svg viewBox="0 0 24 24"><use href="#${icon}" fill="#fff"/></svg></div><div><div class="tt"><b>${title}</b><span>${when}</span></div>${body}</div>`;
    $("items").appendChild(n);
    return n;
  }
  const reveal = n => { n.classList.remove("pending"); n.classList.add("in"); };

  function endingMessages() {
    // the lock screen shows one Messages notification, the newest, stacked on the rest; tap to open
    const thread = buildThread(round.calls, ending);
    const newest = thread[thread.length - 1], more = thread.length - 1;
    const msgs = note("bubble", newest.who, shortDate(newest.date),
      newest.text + (more ? `<div class="sub more">${more} more message${more === 1 ? "" : "s"}</div>` : ""), "tap" + (more ? " stacked" : ""));
    msgs.addEventListener("click", () => openThread(thread));
    const vm = ending === "late" ? note("handset", "Mom", "Mar 28", `<div class="sub">Voicemail · 0:18</div><div class="vm">${VOICEMAIL}</div>`) : null;
    if (vm && $("items").getBoundingClientRect().top < $("sil").getBoundingClientRect().bottom + 6 * scale()) vm.querySelector(".vm").style.webkitLineClamp = "2";

    if (vm) { reveal(vm); playCaption(CAPTION_HEADERS.voicemail, VOICEMAIL); }
    else playCaption(CAPTION_HEADERS.call, LAST_CALL);
    later(2600, () => reveal(msgs));
  }

  // tapping the Messages notification opens every message, newest first, in a sheet over the lock screen
  function openThread(thread) {
    if ($("sheet").classList.contains("on")) return;
    const list = $("sheetList");
    list.innerHTML = thread.slice().reverse().map(m =>
      `<div class="n"><div class="ic"><svg viewBox="0 0 24 24"><use href="#bubble" fill="#fff"/></svg></div><div><div class="tt"><b>${m.who}</b><span>${shortDate(m.date)}</span></div>${m.text}</div></div>`).join("");
    list.scrollTop = 0;
    $("sheet").classList.add("on");
  }
  $("sheetClose").onclick = e => { e.stopPropagation(); $("sheet").classList.remove("on"); };

  // ---- her words, read out line by line beside the phone ----
  function playCaption(header, text) {
    $("caption").innerHTML = `<div class="who">${header}</div>`;
    $("caption").classList.add("on");
    caption = { lines: text.match(/[^.?!…]+[.?!…]+/g), shown: 0, ids: [], done: false };
    caption.lines.forEach((_, i) => { const id = setTimeout(showLine, 500 + i * 1100); caption.ids.push(id); timers.push(id); });
  }
  function showLine() {
    if (disposed || !caption || caption.shown >= caption.lines.length) return;
    const p = document.createElement("p"); p.textContent = caption.lines[caption.shown++].trim(); $("caption").appendChild(p);
    if (caption.shown === caption.lines.length && !caption.done) {
      caption.done = true;
      later(1200, () => $("again").classList.add("on")); // only once she's finished
      later(16200, () => $("restart").classList.add("on")); // in case TURN BACK TIME is never pressed
    }
  }
  // tap twice and the rest of her words appear at once
  function showAllLines() {
    if (!caption || caption.done) return;
    caption.ids.forEach(clearTimeout);
    while (caption.shown < caption.lines.length) showLine();
  }

  // ---- TURN BACK TIME: there is no going back; it answers, in words that fit how the month ended ----
  $("again").onclick = () => {
    const a = $("again");
    if (a.classList.contains("tried")) return;
    const [loud, soft] = turnBackLines(ending, round.missed);
    a.classList.add("tried"); a.textContent = loud;
    later(1800, () => a.insertAdjacentHTML("beforeend", `<span class="soft">${soft}</span>`));
    later(4800, () => $("restart").classList.add("on")); // the way out, only after she's had the last word
  };
  $("restart").onclick = () => onRestart();

  function dispose() {
    disposed = true;
    timers.forEach(id => { clearTimeout(id); clearInterval(id); });
    anims.forEach(a => { try { a.cancel(); } catch (e) { /* already gone */ } });
    $("again").onclick = null; $("restart").onclick = null; $("sheetClose").onclick = null;
    ["fly", "caption", "items", "sheetList"].forEach(id => $(id).innerHTML = "");
    $("caption").classList.remove("on"); $("sheet").classList.remove("on"); $("timeov").classList.remove("on");
    $("card").className = ""; $("res").style.opacity = 1; $("lock").classList.remove("on");
    document.querySelectorAll("#res .row").forEach(r => r.classList.remove("on"));
    ["rank", "doubt", "sil", "restart"].forEach(id => $(id).classList.remove("on"));
    $("sbMoon").classList.remove("on");
    for (const id of ["focus", "focusText", "group", "gcount"]) { const s = $(id).style; s.opacity = ""; s.transform = ""; s.clipPath = ""; }
    $("group").style.display = ""; $("me").style.visibility = "";
    $("again").className = ""; $("again").textContent = "TURN BACK TIME";
  }

  return { tapTwice: showAllLines, dispose };
}
