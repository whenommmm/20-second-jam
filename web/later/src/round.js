// The 20-second round as a simulation with no DOM and no sound: the view and the ending read it.
// Ported from docs/reference/later-mockup.html (update() and its helpers).
import { W, H, LANES, TOP, BOTTOM, CALLS, QUIET_DATES, TUNING as T, DEFAULT_PILL_W } from "./data.js";
import { capsuleDist } from "./rules.js";

const TOTAL_PILLS = CALLS.reduce((a, c) => a + c.n, 0);
const STEP = 1 / 60; // long frames are split into steps this small, so nothing tunnels
const clamp = (lo, hi, v) => Math.max(lo, Math.min(hi, v));
const nearestLane = y => LANES.reduce((b, l) => (Math.abs(l - y) < Math.abs(b - y) ? l : b));

export function createRound(pillW = DEFAULT_PILL_W) {
  return {
    pillW, t: 0, me: { x: 220, y: 270 }, vel: { x: 0, y: 0 }, tilt: 0,
    calls: CALLS.map(c => ({ ...c, spawned: 0, alive: 0, answered: false, missedN: 0, band: null, start: 0, gap: 0 })),
    pills: [], nextId: 1, nextCall: 0, quietIdx: 0,
    score: 0, combo: 0, best: 0, closeN: 0, missed: 0, dodged: 0, hits: 0, bonus: 0, paperK: 0, over: false
  };
}

// Advance the round by dt seconds. input.target is where the pointer wants the moon (world units, or null);
// input.keys is the arrow/WASD direction. Returns what happened, in order.
export function stepRound(round, dt, input) {
  const events = [];
  for (let left = dt; left > 1e-9 && !round.over; left -= STEP) step(round, Math.min(STEP, left), input, events);
  return events;
}

function step(r, dt, input, ev) {
  r.t = Math.min(T.ROUND_TIME, r.t + dt);
  move(r, dt, input);
  startCalls(r, ev);
  spawnPills(r, ev);
  while (r.quietIdx < QUIET_DATES.length && r.t >= QUIET_DATES[r.quietIdx][0]) ev.push({ type: "date", date: QUIET_DATES[r.quietIdx++][1] });
  movePills(r, dt, ev);
  // the paper is your month: a step colder with every call you let ring out; answering never cools it
  r.paperK += (r.missed / TOTAL_PILLS - r.paperK) * (1 - Math.exp(-dt * 3));
  if (r.t >= T.ROUND_TIME) { r.over = true; ev.push({ type: "time" }); }
}

// The moon steers its velocity toward the pointer (or keys) rather than snapping, and leans into its motion.
function move(r, dt, { target, keys }) {
  const kx = keys?.x || 0, ky = keys?.y || 0;
  let want = { x: 0, y: 0 };
  if (kx || ky) { const l = Math.hypot(kx, ky); want = { x: kx / l * T.KEY_SPEED, y: ky / l * T.KEY_SPEED }; }
  else if (target) {
    want = { x: (target.x - r.me.x) * 7, y: (target.y - r.me.y) * 7 };
    const wl = Math.hypot(want.x, want.y);
    if (wl > T.MAX_SPEED) { want.x *= T.MAX_SPEED / wl; want.y *= T.MAX_SPEED / wl; }
  }
  const a = 1 - Math.exp(-dt * 11);
  r.vel.x += (want.x - r.vel.x) * a; r.vel.y += (want.y - r.vel.y) * a;
  r.me.x = clamp(26, W - 26, r.me.x + r.vel.x * dt);
  r.me.y = clamp(TOP, BOTTOM, r.me.y + r.vel.y * dt);
  r.tilt += (clamp(-20, 20, r.vel.x * 0.03) - r.tilt) * (1 - Math.exp(-dt * 8));
}

// Each call slides in near you; Mom's last call is the only one that leaves the lanes.
function startCalls(r, ev) {
  while (r.nextCall < r.calls.length && r.t >= r.calls[r.nextCall].t) {
    const c = r.calls[r.nextCall];
    if (c.home) {
      // her last call comes straight for you, so it waits (briefly) until you'd have room to get out of its way
      if (r.t < c.t + T.HOME_WAIT && !fair(r, nearestLane(r.me.y), c)) break;
      c.band = [];
    } else {
      const lane = pickLane(r, c);
      if (lane === undefined) break; // every lane busy: try again next step
      c.band = [lane];
    }
    c.start = r.t; r.nextCall++;
    ev.push({ type: "date", date: c.date });
  }
}

function spawnPills(r, ev) {
  r.calls.forEach((c, ci) => {
    if (!c.band || c.answered) return;
    while (c.spawned < c.n && (c.spawned === 0 || r.t >= c.start + c.spawned * c.gap)) {
      const w = r.pillW[c.who], x = W + w / 2 + 10, id = r.nextId++;
      // pills of one call follow each other a fixed distance apart, whatever their speed
      if (!c.gap) c.gap = (w + T.PILL_SPACE) / c.v;
      const pill = { id, ci, who: c.who, first: c.spawned === 0, x, y: c.home ? clamp(TOP, BOTTOM, r.me.y) : c.band[0], w, h: T.PILL_H,
        vx: -c.v, vy: 0, minD: 1e9, passed: false, gain: 0, locked: false, dead: false, seed: (id * 2.399) % 10 };
      r.pills.push(pill); c.spawned++; c.alive++;
      ev.push({ type: "spawn", pill });
    }
  });
}

function movePills(r, dt, ev) {
  for (const p of r.pills) {
    if (p.dead) continue;
    const c = r.calls[p.ci];
    // Mom's last call drifts toward you until the middle of the screen, then commits to a gentler straight line you can dodge
    if (c.home && p.x > T.HOME_LOCK_X && p.x - r.me.x > T.HOME_LOCK_DIST) p.vy = clamp(-T.HOME_MAX_VY, T.HOME_MAX_VY, (r.me.y - p.y) * T.HOME_STEER);
    else if (c.home && !p.locked) { p.locked = true; p.vy *= 0.5; }
    p.x += p.vx * dt; p.y = clamp(TOP, BOTTOM, p.y + p.vy * dt);
    const d = capsuleDist(p, r.me.x, r.me.y);
    p.minD = Math.min(p.minD, d);
    if (d < T.HIT_R) {
      if (p.passed) unpass(r, p, ev); // you went back for it: answered, not missed
      c.answered = true; r.hits++; r.bonus += T.CLARITY_BONUS; r.combo = 0; r.score -= 100;
      ev.push({ type: "hit", pill: p });
      for (const o of r.pills) if (!o.dead && o.ci === p.ci) { o.dead = true; c.alive--; ev.push({ type: "remove", pill: o }); }
      continue;
    }
    if (!p.passed && p.x + p.w / 2 < r.me.x - 20) pass(r, p, ev);
    if (p.x < -p.w || p.y < -80 || p.y > H + 80) {
      if (!p.passed) pass(r, p, ev);
      p.dead = true; c.alive--;
      ev.push({ type: "remove", pill: p });
    }
  }
  r.pills = r.pills.filter(p => !p.dead);
}

// The moment a pill gets past you it's one more missed call.
function pass(r, p, ev) {
  const c = r.calls[p.ci];
  p.passed = true; p.gain = p.minD < T.CLOSE_R ? 25 : 10;
  r.score += p.gain;
  if (p.gain === 25) r.closeN++;
  r.dodged++; r.missed++; c.missedN++; r.combo++; r.best = Math.max(r.best, r.combo);
  ev.push({ type: "pass", pill: p, close: p.gain === 25 });
}

function unpass(r, p, ev) {
  const c = r.calls[p.ci];
  r.score -= p.gain; if (p.gain === 25) r.closeN--;
  r.dodged--; r.missed--; c.missedN--;
  ev.push({ type: "unpass", pill: p });
}

// ---- Lanes and fairness: calls come at you, but never without time to react or somewhere to go ----

// A lane takes a new call only once the last one has fully slid in, and only if the newcomer can't catch it up.
function laneFree(r, y, v) {
  if (r.calls.some(c => c.band && c.band.includes(y) && !c.answered && c.spawned < c.n)) return false;
  let tail = null;
  for (const p of r.pills) if (!p.dead && r.calls[p.ci].band.includes(y) && (!tail || p.x > tail.x)) tail = p;
  if (!tail) return true;
  const back = tail.x + tail.w / 2, u = -tail.vx;
  if (back > W - 40) return false;
  return v <= u || (W + 10 - back) / (v - u) > back / u;
}

// When, counting from now, a lane's pills cover the column you're in: [[from, to], ...] in seconds.
function laneWindows(r, y) {
  const xp = r.me.x, out = [];
  for (const p of r.pills) if (!p.dead && r.calls[p.ci].band.includes(y)) {
    const u = -p.vx;
    out.push([(p.x - p.w / 2 - xp - 40) / u, (p.x + p.w / 2 - xp + 40) / u]);
  }
  for (const c of r.calls) if (c.band && c.band.includes(y) && !c.answered && c.spawned < c.n) {
    const gap = c.gap || (100 + T.PILL_SPACE) / c.v;
    for (let k = c.spawned; k < c.n; k++) {
      const ts = c.start + k * gap - r.t;
      out.push([ts + (W + 10 - xp - 40) / c.v, ts + (W + 110 - xp + 40) / c.v]);
    }
  }
  // Mom's last call leaves the lanes, so it blocks whichever lane it's in right now
  for (const p of r.pills) if (!p.dead && r.calls[p.ci].home && Math.abs(p.y - y) < 40)
    out.push([(p.x - p.w / 2 - xp - 40) / -p.vx, (p.x + p.w / 2 - xp + 40) / -p.vx]);
  return out.filter(([, b]) => b > 0);
}

const clearDuring = (r, y, a, b) => y !== undefined && laneWindows(r, y).every(([p, q]) => q < a || p > b);

// A call may come at you, but only if you'll see it in time and some other lane around you stays open until it's gone past.
function fair(r, lane, c) {
  const mine = LANES.indexOf(nearestLane(r.me.y)), i = LANES.indexOf(lane);
  if (Math.abs(i - mine) > 1) return true;
  const reach = (W + 10 - r.me.x - 40) / c.v;
  if (reach < T.REACT_MIN) return false;
  const b = reach + (c.n * 100 + (c.n - 1) * T.PILL_SPACE + 80) / c.v;
  return [mine - 1, mine, mine + 1].some(j => j !== i && clearDuring(r, LANES[j], 0, b));
}

// The free, fair lane closest to you.
function pickLane(r, c) {
  const near = LANES.slice().sort((p, q) => Math.abs(p - r.me.y) - Math.abs(q - r.me.y));
  return near.find(y => laneFree(r, y, c.v) && fair(r, y, c));
}
