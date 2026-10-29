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
  ]
};

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
