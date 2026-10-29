import test from "node:test";
import assert from "node:assert/strict";
import { createRound, stepRound } from "../src/round.js";
import { endingFor } from "../src/rules.js";
import { dodgeTarget, chaseTarget } from "./bots.js";

// Play one whole round with a test player steering every step; record what happened.
function play(bot, dt = 1 / 60, w) {
  const round = createRound(undefined, w);
  const events = [];
  let overlaps = 0, leftAt17 = null;
  while (!round.over) {
    events.push(...stepRound(round, dt, { target: bot(round), keys: { x: 0, y: 0 } }));
    const ps = round.pills;
    for (let i = 0; i < ps.length; i++) for (let j = i + 1; j < ps.length; j++) {
      const a = ps[i], b = ps[j];
      const ow = Math.min(a.x + a.w / 2, b.x + b.w / 2) - Math.max(a.x - a.w / 2, b.x - b.w / 2);
      const oh = Math.min(a.y + a.h / 2, b.y + b.h / 2) - Math.max(a.y - a.h / 2, b.y - b.h / 2);
      if (ow > 4 && oh > 4) overlaps++;
    }
    if (leftAt17 === null && round.t >= 17) leftAt17 = round.pills.filter(p => p.x + p.w / 2 > 0).length; // still visible
  }
  return { round, events, hits: round.hits, overlaps, leftAt17 };
}

// across the whole width, every 15 px, right up to the far edge, in the plain world and the wide ones phones get
// (a coarser sweep missed a spot just right of the middle where her last call could pin you against the bottom)
for (const w of [960, 1169, 1280])
  test(`in a ${w}-wide world, a careful dodger anywhere across it is never forced to answer`, () => {
    const caught = [];
    for (let x = 26; x <= w - 26; x += 15) if (play(r => dodgeTarget(r, x), undefined, w).hits) caught.push(x);
    assert.deepEqual(caught, []);
  });

// phones on their side get a wider world (up to 1280): the same promises hold right across it
for (const w of [1169, 1280]) {
  test(`in a ${w}-wide world, pills never overlap and are all gone by 17 s`, () => {
    assert.equal(play(r => dodgeTarget(r, 200), undefined, w).overlaps, 0);
    assert.equal(play(r => chaseTarget(r), undefined, w).overlaps, 0);
    assert.equal(play(r => dodgeTarget(r, 340), undefined, w).leftAt17, 0);
  });
  test(`in a ${w}-wide world, pills come in from its right edge and the moon can go all the way over`, () => {
    const round = createRound(undefined, w);
    const spawns = [];
    while (round.t < 3) for (const e of stepRound(round, 1 / 60, { target: { x: w, y: 270 }, keys: { x: 0, y: 0 } })) if (e.type === "spawn") spawns.push(e.pill.x - e.pill.w / 2);
    assert.ok(spawns.length && spawns.every(left => left > w - 6), "a pill appeared inside the screen: " + spawns.map(Math.round));
    assert.equal(Math.round(round.me.x), w - 26);
  });
}

test("pills never overlap", () => {
  assert.equal(play(r => dodgeTarget(r, 200)).overlaps, 0);
  assert.equal(play(r => chaseTarget(r)).overlaps, 0);
});

test("every pill is gone by 17 s", () => assert.equal(play(r => dodgeTarget(r, 340)).leftAt17, 0));

test("each pill that gets past counts once", () => {
  const res = play(r => dodgeTarget(r, 340));
  assert.equal(res.round.missed, 29);
  assert.equal(res.round.dodged, 29);
  assert.equal(res.events.filter(e => e.type === "pass").length, 29);
});

test("answering never cools the paper", () => {
  const res = play(r => chaseTarget(r));
  assert.equal(res.round.missed, 0);
  assert.ok(res.round.paperK < 0.001);
});

test("dodging everything cools it fully", () => assert.ok(play(r => dodgeTarget(r, 340)).round.paperK > 0.95));

test("her last call reaches a player who never moves", () =>
  assert.equal(play(() => ({ x: 340, y: 302 })).round.calls[13].answered, true));

test("dodge all, answer only her last call", () => {
  const res = play(r => (r.calls[13].spawned ? chaseTarget(r, p => p.ci === 13) : dodgeTarget(r, 340)));
  assert.equal(res.round.hits, 1);
  assert.equal(endingFor(res.round.calls), "last");
});

test("TIME fires once and t stops at 20", () => {
  const res = play(r => dodgeTarget(r, 340));
  assert.equal(res.events.filter(e => e.type === "time").length, 1);
  assert.equal(res.round.t, 20);
});

test("long frames play out the same as smooth ones (no tunnelling)", () => {
  const still = () => ({ x: 340, y: 302 });
  assert.equal(play(still, 0.2).round.hits, play(still, 1 / 60).round.hits);
  assert.ok(play(still, 0.2).round.hits > 0);
});

test("each call buzzes once, on its first pill, not every pill", () => {
  const spawns = play(r => dodgeTarget(r, 340)).events.filter(e => e.type === "spawn");
  assert.equal(spawns.length, 29);
  assert.equal(spawns.filter(e => e.pill.first).length, 14);
});

for (const w of [960, 1280]) test(`her last call drifts gently toward you until the middle of the screen, then goes dead straight (${w} wide)`, () => {
  const round = createRound(undefined, w), before = [], after = [];
  while (!round.over) {
    const y = 302 + 150 * Math.sin(round.t * 3); // a player who keeps moving up and down on the left
    stepRound(round, 1 / 60, { target: { x: 200, y }, keys: { x: 0, y: 0 } });
    const mom = round.pills.find(p => p.ci === 13);
    if (mom) (mom.x > w / 2 ? before : after).push(mom.vy);
  }
  assert.ok(before.length > 0 && after.length > 0, "her last call never crossed the middle");
  assert.ok(Math.max(...before.map(Math.abs)) <= 90, "steers harder than 90 px/s");
  assert.ok(new Set(before.slice(-30).map(v => v.toFixed(3))).size > 1, "stopped steering before the middle");
  // the commit happens on the step after it crosses the middle (one frame, ~5 px), so skip that crossing frame
  assert.ok(after.slice(1).every(v => v === 0), "doesn't go dead straight after the middle");
});

test("the moon eases toward a sudden new target instead of snapping, and still gets there quickly", () => {
  const round = createRound(), start = { ...round.me }, target = { x: start.x + 300, y: start.y };
  stepRound(round, 1 / 60, { target, keys: { x: 0, y: 0 } });
  assert.ok(round.me.x - start.x < 1.7, `jumped ${(round.me.x - start.x).toFixed(2)} px in the first frame`);
  let t = 1 / 60;
  while (Math.abs(target.x - round.me.x) > 8 && t < 2) { stepRound(round, 1 / 60, { target, keys: { x: 0, y: 0 } }); t += 1 / 60; }
  assert.ok(t <= 0.8, `took ${t.toFixed(2)} s to arrive`);
});
