// Boot, scaling, input, title screen, the main loop, pausing, fullscreen.
import { W, H, DEFAULT_PILL_W, CALLS } from "./data.js";
import { createRound, stepRound } from "./round.js";
import { createPlayView, measurePillWidths } from "./view.js";
import { createSound } from "./sound.js";
import { playEnding } from "./ending.js";

const $ = id => document.getElementById(id);
const frame = $("frame"), stage = $("stage"), world = $("world");
const sound = createSound();
const view = createPlayView(sound);

let state = "title", round = createRound(), pillW = DEFAULT_PILL_W, ending = null, lastTap = 0;
let scale = 1, last = 0, target = null, forcedTarget = null, drag = null;
const keys = { x: 0, y: 0 }, held = new Set();
const upright = matchMedia("(orientation: portrait) and (max-width: 900px)"); // the rotate overlay is showing

// one 960×540 world, scaled to the largest 16:9 box that fits inside the safe area
function fit() {
  const cs = getComputedStyle(frame);
  const w = frame.clientWidth - parseFloat(cs.paddingLeft) - parseFloat(cs.paddingRight);
  const h = frame.clientHeight - parseFloat(cs.paddingTop) - parseFloat(cs.paddingBottom);
  scale = Math.min(w / W, h / H);
  stage.style.width = W * scale + "px"; stage.style.height = H * scale + "px";
  world.style.transform = `scale(${scale})`;
}
addEventListener("resize", fit);
fit();

document.fonts.ready.then(() => { pillW = measurePillWidths(); });

function start() {
  if (ending) { ending.dispose(); ending = null; }
  sound.unlock();
  sound.ringStart(); // the phone rings softly under play
  round = createRound(pillW);
  view.reset();
  target = null; forcedTarget = null; drag = null;
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

// Mouse: the moon follows the pointer. Touch: the moon moves with the finger from wherever it lands,
// so a thumb never covers it.
stage.addEventListener("pointerdown", e => {
  sound.wake();
  const now = performance.now();
  // double-tap (or double-click) during the ending shows the rest of her words at once
  if (state === "over" && ending && now - lastTap < 350) ending.tapTwice();
  lastTap = now;
  if (e.pointerType === "touch") { drag = { finger: toWorld(e), moon: { ...round.me } }; target = { ...round.me }; }
});
stage.addEventListener("pointermove", e => {
  if (e.pointerType !== "touch") { target = toWorld(e); return; }
  if (!drag) return;
  const f = toWorld(e);
  target = { x: drag.moon.x + f.x - drag.finger.x, y: drag.moon.y + f.y - drag.finger.y };
});
const endDrag = e => { if (e.pointerType === "touch") drag = null; };
stage.addEventListener("pointerup", endDrag);
stage.addEventListener("pointercancel", endDrag);
stage.addEventListener("click", () => { if (state === "title") start(); });

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

// Fullscreen, where the browser supports it (not on iPhone Safari).
const root = document.documentElement;
const canFullscreen = !!(root.requestFullscreen || root.webkitRequestFullscreen);
if (!canFullscreen) root.classList.add("nofs");
$("fs").addEventListener("pointerdown", e => e.stopPropagation());
$("fs").addEventListener("click", e => {
  e.stopPropagation();
  if (document.fullscreenElement || document.webkitFullscreenElement) (document.exitFullscreen || document.webkitExitFullscreen).call(document);
  else (root.requestFullscreen || root.webkitRequestFullscreen).call(root);
});

// A hidden tab or an upright phone pauses play; the first frame back starts from a fresh timestamp.
document.addEventListener("visibilitychange", () => {
  last = performance.now();
  if (document.hidden) sound.suspend(); else sound.wake();
});

function loop(now) {
  const dt = Math.min(0.1, Math.max(0, (now - last) / 1000)); last = now;
  if (state === "play" && !document.hidden && !upright.matches) {
    const events = stepRound(round, dt, { target: forcedTarget || target, keys });
    view.handle(events, round);
    // once every call has come and gone, the phone stops ringing: the last seconds are quiet
    if (sound.ringing() && round.nextCall === CALLS.length && round.pills.length === 0) sound.ringStop();
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
    sound: () => sound,
    start
  };
}
