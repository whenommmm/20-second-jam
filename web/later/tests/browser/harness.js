// Automated checks that run inside the real page (loaded only when the URL hash starts with #test).
// The mode is the hash after "#test-". Each mode starts a round (or not), steers it with a test player,
// probes the page at set times, and reports "OK <mode>" or "FAIL <reasons>" through document.title.
import { dodgeTarget, chaseTarget } from "../bots.js";
import { DEFAULT_PILL_W, TOP } from "../../src/data.js";

const mode = location.hash.replace(/^#test-?/, "") || "dodge";
if (mode === "noaudio") { delete window.AudioContext; delete window.webkitAudioContext; }
// pretend to be a touch screen (coarse pointer) before the game reads it
if (mode === "taptext") { const real = window.matchMedia.bind(window); window.matchMedia = q => (/pointer:\s*coarse/.test(q) ? { matches: true, media: q, addEventListener() {}, removeEventListener() {} } : real(q)); }

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

  // ---- review fixes ----
  widths: [
    [1500, () => {
      const w = L().pillW();
      for (const who of Object.keys(DEFAULT_PILL_W)) expect(Math.abs(w[who] - DEFAULT_PILL_W[who]) <= 2, `${who} pill is ${w[who]} px wide, tests assume ${DEFAULT_PILL_W[who]}`);
    }]
  ],
  touchmargin: [
    [200, () => {
      expect(getComputedStyle(document.documentElement).touchAction === "none" && getComputedStyle(document.body).touchAction === "none", "page can still be pinch-zoomed from the margins");
      L().start();
    }],
    [1200, () => { window.__m0 = { ...L().round().me }; touchScreen("pointerdown", 1, 40, 300); touchScreen("pointermove", 1, 40, 340); }],
    [2400, () => {
      const me = L().round().me, s = $("stage").getBoundingClientRect().width / 960;
      expect($("stage").getBoundingClientRect().left > 60, "no margin beside the stage in this window");
      expect(Math.abs(me.y - (window.__m0.y + 40 / s)) < 4, `a drag started in the margin didn't move the moon (y ${me.y.toFixed(1)}, expected ${(window.__m0.y + 40 / s).toFixed(1)})`);
    }]
  ],
  touchclamp: [
    [200, () => L().start()],
    [1200, () => { touchAt("pointerdown", 0.5, 0.8, 0, 1); touchAt("pointermove", 0.5, 0.8, 0, 1, -600); }],
    [2200, () => { touchAt("pointermove", 0.5, 0.8, 0, 1, -560); }],
    [3200, () => {
      const me = L().round().me, s = $("stage").getBoundingClientRect().width / 960;
      expect(me.y > TOP + 40 / s - 4, `after overshooting the top and coming back 40 px the moon is at y ${me.y.toFixed(1)} (dead zone)`);
      window.__m1 = { ...me };
      touchAt("pointerdown", 0.2, 0.2, 0, 2); touchAt("pointerup", 0.2, 0.2, 0, 2); // a second finger taps and lifts
      touchAt("pointermove", 0.5, 0.8, 0, 1, -520);
    }],
    [4200, () => {
      const me = L().round().me, s = $("stage").getBoundingClientRect().width / 960;
      expect(Math.abs(me.y - (window.__m1.y + 40 / s)) < 4, `a second finger broke the drag (y ${me.y.toFixed(1)}, expected ${(window.__m1.y + 40 / s).toFixed(1)})`);
    }]
  ],
  pendingcard: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [29700, () => { const card = document.querySelector("#items .n.tap"); expect(card && card.classList.contains("pending"), "Messages card already revealed"); const r = card.getBoundingClientRect(); document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2)?.click(); }],
    [30000, () => expect(!document.querySelector("#sheet.on"), "a tap on the still-hidden Messages card opened the sheet")]
  ],
  taptargets: [
    ["start", () => { L().start(); steer(r => dodgeTarget(r, 340)); }],
    [40500, () => $("again").click()],
    [45800, () => {
      const hit = (el, name) => {
        const r = el.getBoundingClientRect(), cx = r.left + r.width / 2;
        for (const y of [r.top - 9, r.bottom + 9]) expect(el.contains(document.elementFromPoint(cx, y)), `${name}: a tap 9 px ${y < r.top ? "above" : "below"} it misses`);
      };
      hit($("restart"), "PLAY AGAIN");
      document.querySelector("#items .n.tap").click();
    }],
    [46300, () => hit2($("sheetClose"), "Show less")]
  ],
  fsplace: [
    [600, () => {
      const fs = $("fs").getBoundingClientRect(), go = document.querySelector("#title .go").getBoundingClientRect(), d = document.querySelector("#dates .slip")?.getBoundingClientRect();
      expect(visible($("fs")) && fs.top > go.bottom && Math.abs((fs.left + fs.right) / 2 - (go.left + go.right) / 2) < 6, "fullscreen isn't centred under the start text on the title screen");
      expect($("fs").textContent.includes("FULLSCREEN"), "fullscreen button isn't labelled on the title screen");
      const s = $("stage").getBoundingClientRect().width / 960;
      expect(fs.height >= 40 * s, "fullscreen button too small to tap: " + (fs.height / s).toFixed(0) + " world px tall");
      expect(!d || fs.right < d.left || fs.bottom < d.top || fs.top > d.bottom, "fullscreen button overlaps the date card");
    }],
    ["start", () => setTimeout(() => { L().start(); steer(r => dodgeTarget(r, 340)); }, 700)],
    [21600, () => {
      const fs = $("fs").getBoundingClientRect(), st = $("stage").getBoundingClientRect(), s = st.width / 960;
      expect(visible($("fs")) && fs.left - st.left < 60 * s && fs.top - st.top < 60 * s, "fullscreen button isn't in the top-left corner after TIME!");
    }]
  ],
  taptext: [
    [600, () => expect(document.querySelector("#title .go").textContent === "TAP TO START", "touch screens show: " + document.querySelector("#title .go").textContent)]
  ],
  clicktext: [
    [600, () => expect(document.querySelector("#title .go").textContent === "CLICK TO START", "mouse screens show: " + document.querySelector("#title .go").textContent)]
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
function touchAt(type, fx, fy, dx = 0, id = 1, dy = 0) {
  const r = $("stage").getBoundingClientRect();
  const x = r.left + r.width * fx + dx, y = r.top + r.height * fy + dy;
  (document.elementFromPoint(x, Math.max(0, Math.min(innerHeight - 1, y))) || $("stage")).dispatchEvent(new PointerEvent(type, { pointerType: "touch", pointerId: id, isPrimary: id === 1, clientX: x, clientY: y, bubbles: true }));
}
// a touch at window coordinates, wherever it lands (margins included)
function touchScreen(type, id, x, y) {
  (document.elementFromPoint(x, y) || document.body).dispatchEvent(new PointerEvent(type, { pointerType: "touch", pointerId: id, isPrimary: id === 1, clientX: x, clientY: y, bubbles: true }));
}
function hit2(el, name) {
  const r = el.getBoundingClientRect(), cx = r.left + r.width / 2;
  for (const y of [r.top - 9, r.bottom + 9]) expect(el.contains(document.elementFromPoint(cx, y)), `${name}: a tap 9 px ${y < r.top ? "above" : "below"} it misses`);
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
