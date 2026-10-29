// Automated checks that run inside the real page (loaded only when the URL hash starts with #test).
// The mode is the hash after "#test-". Each mode starts a round (or not), steers it with a test player,
// probes the page at set times, and reports "OK <mode>" or "FAIL <reasons>" through document.title.
import { dodgeTarget, chaseTarget } from "../bots.js";

const mode = location.hash.replace(/^#test-?/, "") || "dodge";
if (mode === "noaudio") { delete window.AudioContext; delete window.webkitAudioContext; }

const $ = id => document.getElementById(id);
const fails = [];
const expect = (ok, why) => { if (!ok) fails.push(why); };
const L = () => window.__later;
const visible = el => el && getComputedStyle(el).display !== "none" && getComputedStyle(el).visibility !== "hidden";
const paper = () => getComputedStyle(document.documentElement).getPropertyValue("--paper").trim();

// bots steer by setting the target in world units every 30 ms
function steer(pick) { return setInterval(() => { const r = L().round(); if (L().state() === "play") { const t = pick(r); L().setTarget(t.x, t.y); } }, 30); }

const MODES = {
  title: [
    [500, () => {
      expect(document.title === "LATER", `tab title is "${document.title}"`);
      expect($("title").textContent.includes("LATER") && $("title").textContent.includes("CLICK TO START"), "title screen text");
      expect(visible($("title")), "title screen hidden");
    }]
  ],
  dodge: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [10000, () => {
      expect(document.querySelectorAll(".pill:not(.gone)").length > 0, "no pills on screen at 10 s");
      expect(paper() !== "rgb(253,220,172)", "paper hasn't cooled at 10 s: " + paper());
    }],
    [20600, () => expect(visible($("timeov")), "TIME! not shown at 20.6 s")]
  ],
  noaudio: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [20600, () => expect(visible($("timeov")), "TIME! not shown at 20.6 s without audio")]
  ],

  // ---- the ending (timings: TIME! at 20 s, lock screen ~26 s, her words from ~29 s, TURN BACK TIME ~38–39 s) ----
  late: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [30000, () => {
      expect(topWho() === "John" && topText().startsWith("Funeral's on Saturday."), "top message: " + topWho() + " " + topText());
      expect(capHeader() === "VOICEMAIL · MOM · MAR 28", "caption header: " + capHeader());
    }],
    [40500, () => { expect(visible($("again")), "TURN BACK TIME not shown"); expect(!visible($("restart")), "PLAY AGAIN before TURN BACK TIME"); $("again").click(); }],
    [42500, () => {
      const t = $("again").textContent, n = L().round().missed;
      expect(t.startsWith(`YOU SAID LATER ${n} TIMES.`) && t.endsWith("She would have taken any one of them."), "response: " + t);
      expect(!visible($("restart")), "PLAY AGAIN too early");
    }],
    [45600, () => expect(visible($("restart")), "PLAY AGAIN not shown 3 s after the soft line")]
  ],
  home: [
    ["start", () => { L().start(); steer(r => chaseTarget(r)); }],
    [30000, () => {
      expect(topWho() === "Dad" && topText().startsWith("Thank you for coming home, kiddo."), "top message: " + topWho() + " " + topText());
      expect(capHeader() === "CALL · MOM · MAR 28 · 4:12", "caption header: " + capHeader());
    }],
    [39500, () => $("again").click()],
    [41500, () => expect($("again").textContent === "THERE'S NEVER ENOUGH TIME.But you were there.", "response: " + $("again").textContent)]
  ],
  lastonly: [
    ["start", () => { L().start(); steer(r => (r.calls[13].spawned ? chaseTarget(r, p => p.ci === 13) : dodgeTarget(r, 340))); }],
    [30000, () => expect(topWho() === "Dad" && topText().startsWith("She told everyone you called."), "top message: " + topWho() + " " + topText())],
    [39500, () => $("again").click()],
    [41500, () => expect($("again").textContent === "YOU GAVE HER FOUR MINUTES.She'd waited all month for them.", "response: " + $("again").textContent)]
  ],
  thread: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [31000, () => { const card = document.querySelector("#items .n.tap"); card.click(); card.click(); }],
    [31600, () => {
      expect(document.querySelectorAll("#sheet.on").length === 1, "sheet not open once");
      const cards = document.querySelectorAll("#sheetList .n");
      expect(cards.length === 14, "messages in the sheet: " + cards.length);
      expect(cards[0] && cards[0].querySelector("b").textContent === "John", "first message isn't John's");
      $("sheetClose").click();
    }],
    [32000, () => expect(!visible($("sheet")), "Show less didn't close the sheet")]
  ],
  threadend: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [31000, () => { document.querySelector("#items .n.tap").click(); }],
    [31400, () => { const l = $("sheetList"); l.scrollTop = l.scrollHeight; }],
    [31600, () => {
      const l = $("sheetList"), cards = l.querySelectorAll(".n"), lastCard = cards[cards.length - 1];
      const lr = l.getBoundingClientRect(), cr = lastCard.getBoundingClientRect(), sr = $("lock").getBoundingClientRect();
      expect(lastCard.querySelector("b").textContent === "Mom", "last message isn't Mom's first text");
      expect(cr.bottom <= lr.bottom + 0.5, `last message cut off: card bottom ${cr.bottom.toFixed(1)} > list bottom ${lr.bottom.toFixed(1)} (scrollTop ${l.scrollTop}, scrollHeight ${l.scrollHeight}, clientHeight ${l.clientHeight})`);
      const scale = $("world").getBoundingClientRect().width / 960;
      expect(sr.bottom - cr.bottom >= 20 * scale, `last message ends ${((sr.bottom - cr.bottom) / scale).toFixed(1)} px from the screen's rounded bottom (needs 20)`);
      expect(getComputedStyle(l).touchAction === "pan-y", "Messages can't be scrolled by touch: touch-action " + getComputedStyle(l).touchAction);
    }]
  ],
  doubletap: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [30000, () => { tap(); setTimeout(tap, 150); }],
    [30400, () => expect(document.querySelectorAll("#caption p").length === 9, "lines after a double tap: " + document.querySelectorAll("#caption p").length)],
    [31800, () => expect(visible($("again")), "TURN BACK TIME not shown after the double tap")]
  ],
  fallback: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [53000, () => expect(!visible($("restart")), "PLAY AGAIN showed before the 15 s fallback")],
    [55000, () => expect(visible($("restart")), "PLAY AGAIN fallback didn't show")]
  ],
  dblturnback: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [40500, () => { $("again").click(); $("again").click(); }],
    [43000, () => expect(document.querySelectorAll("#again .soft").length === 1, "soft lines: " + document.querySelectorAll("#again .soft").length)]
  ],
  restart: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [40500, () => $("again").click()],
    [45600, () => $("restart").click()],
    ...[47600, 55600, 63600].map(ms => [ms, () => {
      expect(L().state() === "play", "not playing " + (ms - 45600) + " ms into the new round");
      expect(!visible($("card")) && !$("caption").textContent && !visible($("again")) && !visible($("restart")), "old ending still showing at " + (ms - 45600) + " ms");
      expect(!document.querySelector(".flyer") && !document.querySelector("#sheet.on") && !visible($("timeov")), "leftover flyer, sheet or TIME! at " + (ms - 45600) + " ms");
    }])
  ],

  // ---- the ringing under play: rings while calls are coming, stops once her last call has gone ----
  ring: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [5000, () => expect(L().sound().ringing(), "not ringing at 5 s")],
    [17500, () => expect(!L().sound().ringing(), "still ringing after the last call has gone")]
  ],

  // ---- itch.io and mobile ----
  hidden: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [5000, () => { setHidden(true); window.__t0 = L().round().t; }],
    [7000, () => { expect(Math.abs(L().round().t - window.__t0) < 0.001, `round ran while hidden: ${window.__t0} → ${L().round().t}`); setHidden(false); }],
    [9000, () => expect(L().round().t > window.__t0 + 1, "round didn't resume after the tab came back")]
  ],
  touch: [
    [200, () => {
      expect(getComputedStyle($("stage")).touchAction === "none", "stage touch-action: " + getComputedStyle($("stage")).touchAction);
      expect(/user-scalable=no/.test(document.querySelector('meta[name="viewport"]').content), "viewport allows zoom");
      L().start();
    }],
    [1200, () => { window.__m0 = { ...L().round().me }; touchAt("pointerdown", 0.8, 0.8); touchAt("pointermove", 0.8, 0.8, 40); }],
    [2400, () => {
      const me = L().round().me, s = $("stage").getBoundingClientRect().width / 960, want = window.__m0.x + 40 / s;
      expect(Math.abs(me.x - want) < 4 && Math.abs(me.y - window.__m0.y) < 4, `moon at ${me.x.toFixed(1)},${me.y.toFixed(1)}; expected ${want.toFixed(1)},${window.__m0.y.toFixed(1)}`);
    }]
  ],
  fullscreen: [
    [200, () => { expect(visible($("fs")), "fullscreen button hidden on the title screen"); }],
    ["start", () => setTimeout(() => { L().start(); steer(r => dodgeTarget(r, 340)); }, 300)],
    [5300, () => expect(!visible($("fs")), "fullscreen button showing during play")],
    [21500, () => expect(visible($("fs")), "fullscreen button hidden after TIME!")]
  ],
  smallphone: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [32000, () => {
      const s = $("world").getBoundingClientRect().width / 960;
      const body = document.querySelector("#items .n");
      const px = body ? parseFloat(getComputedStyle(body).fontSize) * s : 0;
      expect(px >= 8, `notification text renders at ${px.toFixed(2)} CSS px (scale ${s.toFixed(3)})`);
      const items = $("items").getBoundingClientRect(), sil = $("sil").getBoundingClientRect();
      expect(items.top >= sil.bottom - 1, "notifications overlap 'Notifications silenced'");
    }]
  ]
};

function setHidden(h) {
  Object.defineProperty(document, "hidden", { configurable: true, get: () => h });
  Object.defineProperty(document, "visibilityState", { configurable: true, get: () => (h ? "hidden" : "visible") });
  document.dispatchEvent(new Event("visibilitychange"));
}
function touchAt(type, fx, fy, dx = 0) {
  const r = $("stage").getBoundingClientRect();
  $("stage").dispatchEvent(new PointerEvent(type, { pointerType: "touch", isPrimary: true, clientX: r.left + r.width * fx + dx, clientY: r.top + r.height * fy, bubbles: true }));
}

const topCard = () => document.querySelector("#items .n.tap");
const topWho = () => topCard()?.querySelector("b")?.textContent || "none";
const topText = () => topCard()?.lastElementChild?.childNodes[1]?.textContent || "";
const capHeader = () => document.querySelector("#caption .who")?.textContent || "none";
function tap() {
  const r = $("stage").getBoundingClientRect();
  $("stage").dispatchEvent(new PointerEvent("pointerdown", { clientX: r.left + r.width * 0.85, clientY: r.top + r.height * 0.3, bubbles: true }));
}

function report() {
  if (document.title.startsWith("ERR")) return;
  document.title = fails.length ? "FAIL " + mode + ": " + fails.join("; ") : "OK " + mode;
}

window.addEventListener("load", () => setTimeout(() => {
  const steps = MODES[mode];
  if (!steps) { document.title = "ERR unknown mode " + mode; return; }
  if (!L()) { document.title = "ERR no test hook (window.__later)"; return; }
  let last = 0;
  for (const [at, fn] of steps) {
    const ms = at === "start" ? 0 : at;
    last = Math.max(last, ms);
    setTimeout(() => { try { fn(); } catch (e) { fails.push("probe threw: " + e.message); } }, ms);
  }
  setTimeout(report, last + 50);
}, 300));
