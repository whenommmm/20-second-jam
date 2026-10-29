import test from "node:test";
import assert from "node:assert/strict";
import { createRound, stepRound } from "../src/round.js";
import { endingFor } from "../src/rules.js";
import { dodgeTarget, chaseTarget } from "./bots.js";

// Play one whole round with a test player steering every step; record what happened.
function play(bot, dt = 1 / 60) {
  const round = createRound();
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
    if (leftAt17 === null && round.t >= 17) leftAt17 = round.pills.length;
  }
  return { round, events, hits: round.hits, overlaps, leftAt17 };
}

for (const x of [120, 200, 340, 480, 620, 760])
  test(`a careful dodger at x=${x} is never forced to answer`, () => assert.equal(play(r => dodgeTarget(r, x)).hits, 0));

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
