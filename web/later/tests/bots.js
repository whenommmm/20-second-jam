// Test players, shared by the Node tests and the browser harness. They read the round directly (world units).
import { TOP, BOTTOM, W } from "../src/data.js";

// How long (in px of approach) until a pill reaches the moon at height y; -1 means off the play area.
function safety(round, y, ignore = () => false) {
  if (y < TOP || y > BOTTOM) return -1;
  const mx = round.me.x;
  let s = 2000;
  for (const p of round.pills) {
    if (ignore(p) || Math.abs(p.y - y) >= 34 || p.x + p.w / 2 <= mx - 34) continue;
    s = Math.min(s, Math.max(0, p.x - p.w / 2 - mx - 34));
  }
  return s;
}

// The best nearby height, judging each move by where it lands and the route to get there.
function bestHeight(round, score, ignore) {
  const my = round.me.y;
  let best = score(my, safety(round, my, ignore)) + 40, ty = my;
  for (const d of [-93, -62, -31, -15, 15, 31, 62, 93]) {
    let route = 2000;
    for (let k = 1; k <= Math.abs(d) / 10; k++) route = Math.min(route, safety(round, my + Math.sign(d) * k * 10, ignore));
    const v = score(my + d, Math.min(safety(round, my + d, ignore), route * 3)) - Math.abs(d) * 0.5;
    if (v > best) { best = v; ty = my + d; }
  }
  return ty;
}

// A careful dodger that holds x and moves up or down to stay clear.
export function dodgeTarget(round, x) {
  return { x, y: bestHeight(round, (y, safe) => safe) };
}

// Go for the leftmost wanted pill on screen. If some pills are unwanted, steer toward the wanted one's
// height only along routes that stay clear of the others.
export function chaseTarget(round, wants = () => true) {
  const goal = round.pills.filter(p => p.x - p.w / 2 < W && wants(p)).sort((a, b) => a.x - b.x)[0];
  if (!goal) return { x: 200, y: 300 };
  if (round.pills.every(wants)) return { x: goal.x, y: goal.y };
  const avoid = p => wants(p);
  return { x: round.me.x, y: bestHeight(round, (y, safe) => (safe < 60 ? -1e6 : -Math.abs(y - goal.y)), avoid) };
}
