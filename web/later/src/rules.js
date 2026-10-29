// Small pure rules shared by the round, the view and the ending.
import { PAPER_WARM, PAPER_COLD, FOLLOW_UP, PENDING, FINAL, TURN_BACK } from "./data.js";

// "MAR 05" → "Mar 5"
export const shortDate = d => d[0] + d.slice(1, 3).toLowerCase() + " " + +d.slice(4);

// How readable the names are: unreadable most of the round, sharp at 16 s; each answered call clears them more.
export const clarity = (t, bonus) => Math.min(1, Math.pow(Math.min(t, 16) / 16, 3.5) + bonus);

// The paper, from vibrant (k = 0, nothing missed) to cold slate (k = 1, everything missed).
export const paperRGB = k => `rgb(${PAPER_WARM.map((w, i) => Math.round(w + (PAPER_COLD[i] - w) * k)).join(",")})`;

export const rankFor = hits => (hits === 0 ? "PERFECT!" : hits <= 2 ? "GREAT!" : "NICE TRY");

// Missed her last call: too late. Picked it up: home if you'd kept in touch, otherwise only her last call.
export function endingFor(calls) {
  if (!calls[13].answered) return "late";
  return calls.slice(0, 13).some(c => c.answered) ? "home" : "last";
}

// Every text of the month, oldest first: a thank-you for each call you picked up,
// what they were trying to tell you for each one you let ring out, then the ending's last word.
export function buildThread(calls, ending) {
  const thread = calls.slice(0, 13).map((c, i) => ({ who: c.who, date: c.date, text: c.answered ? FOLLOW_UP[i] : PENDING[i] }));
  thread.push(FINAL[ending]);
  return thread;
}

export function turnBackLines(ending, missed) {
  const [loud, soft] = TURN_BACK[ending];
  const n = missed === 1 ? "ONCE" : missed === 2 ? "TWICE" : `${missed} TIMES`;
  return [loud.replace("{n}", n), soft];
}

// Distance from a point to a pill's capsule outline; negative means inside.
export function capsuleDist(p, px, py) {
  const half = p.w / 2 - p.h / 2;
  const dx = Math.max(Math.abs(px - p.x) - half, 0), dy = py - p.y;
  return Math.hypot(dx, dy) - p.h / 2;
}
