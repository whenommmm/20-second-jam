import test from "node:test";
import assert from "node:assert/strict";
import { shortDate, clarity, paperRGB, rankFor, endingFor, buildThread, turnBackLines, capsuleDist } from "../src/rules.js";
import { CALLS, PENDING, FOLLOW_UP, FINAL } from "../src/data.js";

test("shortDate", () => {
  assert.equal(shortDate("MAR 05"), "Mar 5");
  assert.equal(shortDate("APR 01"), "Apr 1");
  assert.equal(shortDate("MAR 28"), "Mar 28");
});

test("clarity", () => {
  assert.equal(clarity(0, 0), 0);
  assert.equal(clarity(16, 0), 1);
  assert.equal(clarity(20, 0), 1);
  assert.ok(Math.abs(clarity(8, 0) - 0.0884) < 0.001);
  assert.equal(clarity(8, 0.17 * 6), 1);
});

test("paperRGB", () => {
  assert.equal(paperRGB(0), "rgb(253,220,172)");
  assert.equal(paperRGB(1), "rgb(146,158,172)");
  assert.equal(paperRGB(0.5), "rgb(200,189,172)");
});

test("rankFor", () => {
  assert.deepEqual([0, 1, 2, 3, 9].map(rankFor), ["PERFECT!", "GREAT!", "GREAT!", "NICE TRY", "NICE TRY"]);
});

test("endingFor", () => {
  const calls = a => Array.from({ length: 14 }, (_, i) => ({ answered: a.includes(i) }));
  assert.equal(endingFor(calls([])), "late");
  assert.equal(endingFor(calls([3])), "late");
  assert.equal(endingFor(calls([13])), "last");
  assert.equal(endingFor(calls([0, 13])), "home");
});

test("buildThread", () => {
  const calls = Array.from({ length: 14 }, (_, i) => ({ ...CALLS[i], answered: i === 4 }));
  const th = buildThread(calls, "late");
  assert.equal(th.length, 14);
  assert.deepEqual(th[0], { who: "Mom", date: "MAR 01", text: PENDING[0] });
  assert.equal(th[4].text, FOLLOW_UP[4]);
  assert.deepEqual(th[13], FINAL.late);
});

test("turnBackLines", () => {
  assert.deepEqual(turnBackLines("late", 1), ["YOU SAID LATER ONCE.", "She would have taken any one of them."]);
  assert.equal(turnBackLines("late", 2)[0], "YOU SAID LATER TWICE.");
  assert.equal(turnBackLines("late", 24)[0], "YOU SAID LATER 24 TIMES.");
  assert.deepEqual(turnBackLines("home", 5), ["THERE'S NEVER ENOUGH TIME.", "But you were there."]);
});

test("capsuleDist", () => {
  const p = { x: 100, y: 100, w: 100, h: 42 };
  assert.equal(capsuleDist(p, 100, 100), -21);
  assert.equal(capsuleDist(p, 100, 150), 29);
  assert.equal(capsuleDist(p, 200, 100), 50);
});
