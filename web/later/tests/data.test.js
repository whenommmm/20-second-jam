import test from "node:test";
import assert from "node:assert/strict";
import * as D from "../src/data.js";

test("schedule matches the spec", () => {
  assert.equal(D.CALLS.length, 14);
  assert.equal(D.CALLS.reduce((a, c) => a + c.n, 0), 29);
  assert.deepEqual(D.CALLS[13], { t: 12.4, who: "Mom", n: 1, v: 280, date: "MAR 28", home: true });
  assert.deepEqual(D.CALLS[4], { t: 4.4, who: "Jessie", n: 3, v: 250, date: "MAR 15" });
  assert.deepEqual(D.LANES, [54, 116, 178, 240, 302, 364, 426, 488]);
});

test("all the text is there, verbatim", () => {
  assert.equal(D.FOLLOW_UP.length, 13);
  assert.equal(D.PENDING.length, 13);
  assert.equal(D.FOLLOW_UP[0], "So good to hear your voice. Talk soon.");
  assert.equal(D.PENDING[8], "Call Dad. Now.");
  assert.equal(D.PENDING[12], "She's sleeping most of the day now. We're all here.");
  assert.match(D.VOICEMAIL, /^Hi sweetheart, it's just me\. No reason… .* I'm so proud of you\.$/);
  assert.match(D.LAST_CALL, /^Hi sweetheart… you picked up\./);
  assert.equal(D.FINAL.late.text, "Funeral's on Saturday.");
  assert.equal(D.TURN_BACK.last[0], "YOU GAVE HER FOUR MINUTES.");
});
