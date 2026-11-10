// Screenshots of the web version and the Unity build at the same moments, to compare by eye.
//   node tools/compare-shots.mjs
// Both play the same schedule (the web's #test-late, the build's #test-late&speed=1): dodge every call, TURN BACK
// TIME at 40.5 s. Writes Builds/shots/<size>-<moment>-{web,unity}.png, and <size>-<moment>-pair.png with the web
// version above the Unity build.
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { openChrome, serve, sleep, waitFor } from "./chrome.mjs";
import { readPng, writeStacked } from "./png.mjs";

const ROOT = fileURLToPath(new URL("..", import.meta.url));
const OUT = join(ROOT, "Builds/shots");
const SIZES = [[960, 627], [844, 477]];
// seconds from the start of the round: mid-play, the results, the lock screen with her words, after the lesson
const MOMENTS = [["play10", 10], ["results", 22], ["words", 32.3], ["lesson", 48.5]];

// the round's clock in each version: the web's round time, then wall time after TIME!; the build reports its own
const CLOCK = {
  web: `(() => {
    const L = window.__later; if (!L) return -1;
    if (!window.__overAt && L.state() === "over") window.__overAt = performance.now();
    return window.__overAt ? 20 + (performance.now() - window.__overAt) / 1000 : L.round().t;
  })()`,
  unity: `(+((document.title.match(/ t=([0-9.]+)/) || [])[1] ?? -1))`
};

async function shoot(name, page) {
  const file = join(OUT, `${name}.png`);
  writeFileSync(file, await page.screenshot());
  return file;
}

async function session(kind, url, w, h, size) {
  const shots = {};
  // the title screen, from a page that isn't in a test mode
  let page = await openChrome(url, w, h);
  await waitFor(async () => (await page.eval("document.readyState")) === "complete" && !(await page.eval("document.getElementById('bar')")), 60_000, kind + " to load");
  await sleep(1500);
  shots.title = await shoot(`${size}-title-${kind}`, page);
  await page.close();

  page = await openChrome(url + (kind === "web" ? "#test-late" : "#test-late&speed=1"), w, h);
  try {
    for (const [moment, at] of MOMENTS) {
      await waitFor(async () => {
        if (kind === "web") await page.eval(CLOCK.web); // keeps the TIME! mark fresh
        return (await page.eval(CLOCK[kind])) >= at;
      }, 120_000, `${kind} at ${at} s`);
      shots[moment] = await shoot(`${size}-${moment}-${kind}`, page);
    }
  } finally {
    await page.close();
  }
  return shots;
}

mkdirSync(OUT, { recursive: true });
const web = await serve(join(ROOT, "web/later")), unity = await serve(join(ROOT, "Builds/LATER-webgl"));
try {
  for (const [w, h] of SIZES) {
    const size = `${w}x${h}`;
    const a = await session("web", web.url + "/index.html", w, h, size);
    const b = await session("unity", unity.url + "/index.html", w, h, size);
    for (const moment of ["title", ...MOMENTS.map(m => m[0])]) {
      const pair = join(OUT, `${size}-${moment}-pair.png`);
      writeStacked(pair, [readPng(a[moment]), readPng(b[moment])]);
      console.log(pair);
    }
  }
} finally {
  web.close(); unity.close();
}
