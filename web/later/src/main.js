// Boot, scaling, input, title screen and the main loop.
import { W, H, DEFAULT_PILL_W } from "./data.js";
import { createRound, stepRound } from "./round.js";
import { createPlayView, measurePillWidths } from "./view.js";
import { createSound } from "./sound.js";
import { playEnding } from "./ending.js";

const $ = id => document.getElementById(id);
const stage = $("stage"), world = $("world");
const sound = createSound();
const view = createPlayView(sound);

let state = "title", round = createRound(), pillW = DEFAULT_PILL_W, ending = null, lastTap = 0;
let scale = 1, last = 0, target = null, forcedTarget = null;
const keys = { x: 0, y: 0 }, held = new Set();

// one 960×540 world, scaled to the largest 16:9 box that fits the window
function fit() {
  scale = Math.min(innerWidth / W, innerHeight / H);
  stage.style.width = W * scale + "px"; stage.style.height = H * scale + "px";
  world.style.transform = `scale(${scale})`;
}
addEventListener("resize", fit);
fit();

document.fonts.ready.then(() => { pillW = measurePillWidths(); });

function start() {
  if (ending) { ending.dispose(); ending = null; }
  sound.unlock();
  round = createRound(pillW);
  view.reset();
  target = null; forcedTarget = null;
  state = "play";
  $("title").classList.remove("on");
  stage.classList.add("playing");
  last = performance.now();
}

function endRound() {
  state = "over";
  stage.classList.remove("playing");
  sound.silence();
  ending = playEnding(round, { onRestart: start });
}

function toWorld(e) {
  const r = stage.getBoundingClientRect();
  return { x: (e.clientX - r.left) / scale, y: (e.clientY - r.top) / scale };
}

stage.addEventListener("pointermove", e => { target = toWorld(e); });
stage.addEventListener("click", () => { if (state === "title") start(); });
// double-tap (or double-click) during the ending shows the rest of her words at once
stage.addEventListener("pointerdown", () => {
  const now = performance.now();
  if (state === "over" && ending && now - lastTap < 350) ending.tapTwice();
  lastTap = now;
});

const KEYMAP = { ArrowLeft: [-1, 0], a: [-1, 0], ArrowRight: [1, 0], d: [1, 0], ArrowUp: [0, -1], w: [0, -1], ArrowDown: [0, 1], s: [0, 1] };
function readKeys() {
  keys.x = 0; keys.y = 0;
  for (const k of held) { keys.x += KEYMAP[k][0]; keys.y += KEYMAP[k][1]; }
  keys.x = Math.sign(keys.x); keys.y = Math.sign(keys.y);
}
addEventListener("keydown", e => {
  const k = e.key.length === 1 ? e.key.toLowerCase() : e.key;
  if (!KEYMAP[k] || state !== "play") return;
  held.add(k); readKeys(); target = null; // after using keys, the moon stays where the keys left it
  if (e.key.startsWith("Arrow")) e.preventDefault();
});
addEventListener("keyup", e => { held.delete(e.key.length === 1 ? e.key.toLowerCase() : e.key); readKeys(); });

function loop(now) {
  const dt = Math.min(0.1, Math.max(0, (now - last) / 1000)); last = now;
  if (state === "play") {
    const events = stepRound(round, dt, { target: forcedTarget || target, keys });
    view.handle(events, round);
    if (events.some(e => e.type === "time")) endRound();
  }
  if (state !== "over") view.draw(round);
  requestAnimationFrame(loop);
}

view.reset();
requestAnimationFrame(loop);

// Automated checks only (see tests/browser/harness.js).
if (location.hash.startsWith("#test")) {
  window.__later = {
    state: () => state,
    round: () => round,
    setTarget: (x, y) => { forcedTarget = { x, y }; },
    ending: () => ending,
    start
  };
}
