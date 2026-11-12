// Plays the WebGL build in a real (headless) Chrome, served the way itch.io serves it: a plain static server that
// sends the .gz/.unityweb files without a Content-Encoding header. Each run opens index.html#test-<mode>; the
// build's SmokeMode plays it and reports "OK <mode>" or "FAIL <reasons>" in the tab title (then its game clock).
//   node tools/webgl-smoke.mjs                 boot, dodge and late at 960×627, then dodge at 844×477
//   node tools/webgl-smoke.mjs late@844x390    any mode at any window size
import { existsSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { openChrome, serve, sleep } from "./chrome.mjs";

const ROOT = fileURLToPath(new URL("..", import.meta.url));
const BUILD = join(ROOT, "Builds/LATER-webgl");
// software WebGL (SwiftShader) is slow, and slower the more often the page is read, so look every 2 s and allow 10 minutes
// (on a real GPU, LATER_GPU=1, the build runs at a steady 60 fps)
const LIMIT_MS = 600_000, POLL_MS = 2000;

const runs = (process.argv.length > 2 ? process.argv.slice(2) : ["boot@960x627", "dodge@960x627", "late@960x627", "dodge@844x477"])
  .map(arg => { const [mode, size = "960x627"] = arg.split("@"), [w, h] = size.split("x").map(Number); return { mode, w, h }; });

async function run({ mode, w, h }, site) {
  const started = Date.now();
  const page = await openChrome(`${site}/index.html#test-${mode}`, w, h);
  try {
    let title = "";
    for (const end = Date.now() + LIMIT_MS; Date.now() < end; await sleep(POLL_MS)) {
      title = ((await page.eval("document.title")) || "").replace(/ t=[0-9.]+$/, ""); // the build adds its game clock
      if (/^(OK|FAIL)\b/.test(title)) break;
    }
    const secs = ((Date.now() - started) / 1000).toFixed(1);
    const ok = title === `OK ${mode}`;
    console.log(`${ok ? "PASS" : "FAIL"} ${mode} @ ${w}×${h} (${secs} s)${ok ? "" : ": " + (title || "no report within " + LIMIT_MS / 1000 + " s")}`);
    if (!ok) page.logs.slice(0, 8).forEach(l => console.log("  " + l));
    return ok;
  } finally {
    await page.close();
  }
}

if (!existsSync(join(BUILD, "index.html"))) {
  console.log(`FAIL: no build at ${BUILD} (run tools/unity.sh build)`);
  process.exit(1);
}
const site = await serve(BUILD);
let failed = 0;
try {
  for (const r of runs) if (!(await run(r, site.url))) failed++;
} finally {
  site.close();
}
process.exit(failed ? 1 : 0);
