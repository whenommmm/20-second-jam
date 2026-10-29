// Draws a round into the page: pills, moon, badge, score, streak, the date pile, the paper, blurred names, shake.
// Ported from the mockup's drawing code. Sizes and positions are world units (960×540).
import { CALLS, START_DATE } from "./data.js";
import { clarity, paperRGB } from "./rules.js";

const $ = id => document.getElementById(id);

// Each name makes its pill a different width; measure the real ones once the fonts are in.
export function measurePillWidths() {
  const widths = {};
  for (const who of new Set(CALLS.map(c => c.who))) {
    const el = document.createElement("div");
    el.className = "pill"; el.style.visibility = "hidden"; el.innerHTML = `<i></i><span>${who}</span>`;
    $("pills").appendChild(el);
    widths[who] = el.offsetWidth;
    el.remove();
  }
  return widths;
}

export function createPlayView(sound) {
  const pillEls = new Map();
  let shakeUntil = 0, shownScore = null;

  // a bordered date card, put down by hand on top of the last one, a little crooked
  function slip(text) {
    const s = document.createElement("div"), p = document.createElement("div");
    s.className = "slip"; p.className = "paper"; p.textContent = text;
    s.style.setProperty("--x", (Math.random() * 22 - 12).toFixed(1) + "px");
    s.style.setProperty("--y", (Math.random() * 14 - 5).toFixed(1) + "px");
    s.style.setProperty("--r", (Math.random() * 13 - 6.5).toFixed(1) + "deg");
    s.appendChild(p); $("dates").appendChild(s);
    while ($("dates").children.length > 9) $("dates").firstChild.remove();
  }

  function pop(x, y, text) {
    const p = document.createElement("div");
    p.className = "pop"; p.textContent = text; p.style.left = x + "px"; p.style.top = y + "px";
    $("pops").appendChild(p); setTimeout(() => p.remove(), 800);
  }

  function setBadge(missed) {
    $("badge").textContent = missed;
    $("badge").classList.toggle("zero", missed === 0);
  }

  return {
    reset() {
      ["pills", "pops", "dates"].forEach(id => $(id).innerHTML = "");
      pillEls.clear(); shownScore = null; shakeUntil = 0;
      $("score").textContent = "0"; $("combo").textContent = "";
      setBadge(0);
      const w = $("mewrap"); w.style.transition = "none"; w.style.transform = ""; w.style.opacity = 1;
      const play = $("play"); play.style.transition = "none"; play.style.opacity = 1; play.style.translate = "0 0";
      document.documentElement.style.setProperty("--paper", paperRGB(0));
      slip(START_DATE);
    },

    handle(events, round) {
      for (const e of events) {
        if (e.type === "date") slip(e.date);
        else if (e.type === "spawn") {
          const el = document.createElement("div");
          el.className = "pill"; el.innerHTML = `<i></i><span>${e.pill.who}</span>`;
          $("pills").appendChild(el); pillEls.set(e.pill.id, el);
          sound.buzz();
        } else if (e.type === "pass") {
          if (e.close) { pop(round.me.x, round.me.y - 50, "CLOSE!"); sound.close(); } else sound.dodge(round.combo);
          setBadge(round.missed);
          $("combo").textContent = round.combo > 1 ? `PERFECT ×${round.combo}` : "";
        } else if (e.type === "unpass") setBadge(round.missed);
        else if (e.type === "hit") {
          $("combo").textContent = ""; shakeUntil = performance.now() + 300; sound.thud();
        } else if (e.type === "remove") {
          const el = pillEls.get(e.pill.id);
          if (el) { pillEls.delete(e.pill.id); el.classList.add("gone"); setTimeout(() => el.remove(), 250); }
        }
      }
    },

    draw(round) {
      for (const p of round.pills) {
        const el = pillEls.get(p.id);
        if (!el) continue;
        const buzz = Math.sin(round.t * 75 + p.seed) * 1.3, lean = Math.max(-14, Math.min(14, Math.atan(p.vy / p.vx) * 57.3 * 0.6));
        el.style.transform = `translate(${(p.x - p.w / 2 + buzz).toFixed(1)}px, ${(p.y - p.h / 2).toFixed(1)}px) rotate(${lean.toFixed(1)}deg)`;
      }
      $("me").style.transform = `translate(${round.me.x.toFixed(1)}px, ${round.me.y.toFixed(1)}px)`;
      $("mesvg").style.rotate = round.tilt.toFixed(1) + "deg";
      if (round.score !== shownScore) { shownScore = round.score; $("score").textContent = round.score.toLocaleString(); }
      // names stay unreadable through most of the round; 5–6 answered calls clear them
      $("world").style.setProperty("--nb", ((1 - clarity(round.t, round.bonus)) * 8).toFixed(2) + "px");
      // the paper is your month: vibrant at first, colder with every call you let ring out
      document.documentElement.style.setProperty("--paper", paperRGB(round.paperK));
      $("play").style.translate = performance.now() < shakeUntil
        ? `${(Math.random() * 14 - 7).toFixed(1)}px ${(Math.random() * 14 - 7).toFixed(1)}px` : "0 0";
    }
  };
}
