// Plays the WebGL build in a real (headless) Chrome, served the way itch.io serves it: a plain static server that
// sends the .gz/.unityweb files without a Content-Encoding header. Each run opens index.html#test-<mode>; the
// build's SmokeMode plays it and reports "OK <mode>" or "FAIL <reasons>" in the tab title.
//   node tools/webgl-smoke.mjs                 boot, dodge and late at 960×627, then dodge at 844×477
//   node tools/webgl-smoke.mjs late@844x390    any mode at any window size
import { spawn } from "node:child_process";
import { existsSync, mkdtempSync, rmSync } from "node:fs";
import { createServer } from "node:net";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = fileURLToPath(new URL("..", import.meta.url));
const BUILD = join(ROOT, "Builds/LATER-webgl");
const CHROME = process.env.CHROME || "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const LIMIT_MS = 120_000;

const runs = (process.argv.length > 2 ? process.argv.slice(2) : ["boot@960x627", "dodge@960x627", "late@960x627", "dodge@844x477"])
  .map(arg => { const [mode, size = "960x627"] = arg.split("@"), [w, h] = size.split("x").map(Number); return { mode, w, h }; });

const sleep = ms => new Promise(r => setTimeout(r, ms));
const freePort = () => new Promise(res => { const s = createServer().listen(0, "127.0.0.1", () => { const p = s.address().port; s.close(() => res(p)); }); });

async function waitFor(fn, ms, what) {
  for (const end = Date.now() + ms; Date.now() < end; await sleep(200)) { try { const v = await fn(); if (v) return v; } catch {} }
  throw new Error("timed out waiting for " + what);
}

// one page, driven over the Chrome DevTools Protocol
async function openPage(port, url) {
  const target = await waitFor(async () => (await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" })).json(), 15_000, "Chrome");
  const ws = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });
  let id = 0;
  const pending = new Map(), logs = [];
  ws.onmessage = e => {
    const m = JSON.parse(e.data);
    if (m.id && pending.has(m.id)) { pending.get(m.id)(m.result); pending.delete(m.id); }
    if (m.method === "Runtime.exceptionThrown") logs.push("exception: " + m.params.exceptionDetails.text);
    if (m.method === "Runtime.consoleAPICalled" && m.params.type === "error") logs.push("console: " + m.params.args.map(a => a.value ?? a.description).join(" "));
  };
  const send = (method, params = {}) => new Promise(res => { const n = ++id; pending.set(n, res); ws.send(JSON.stringify({ id: n, method, params })); });
  await send("Runtime.enable");
  await send("Page.navigate", { url }); // not through /json/new, which would drop the #hash
  return { send, logs, close: () => ws.close() };
}

async function run({ mode, w, h }, site) {
  const port = await freePort(), profile = mkdtempSync(join(tmpdir(), "later-smoke-"));
  const chrome = spawn(CHROME, [
    "--headless=new", `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, `--window-size=${w},${h}`,
    "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--autoplay-policy=no-user-gesture-required",
    "--no-first-run", "--no-default-browser-check", "about:blank"
  ], { stdio: "ignore" });
  const started = Date.now();
  try {
    const page = await openPage(port, `${site}/index.html#test-${mode}`);
    let title = "";
    for (const end = Date.now() + LIMIT_MS; Date.now() < end; await sleep(500)) {
      title = (await page.send("Runtime.evaluate", { expression: "document.title", returnByValue: true }))?.result?.value || "";
      if (/^(OK|FAIL)\b/.test(title)) break;
    }
    page.close();
    const secs = ((Date.now() - started) / 1000).toFixed(1);
    const ok = title === `OK ${mode}`;
    console.log(`${ok ? "PASS" : "FAIL"} ${mode} @ ${w}×${h} (${secs} s)${ok ? "" : ": " + (title || "no report within " + LIMIT_MS / 1000 + " s")}`);
    if (!ok) page.logs.slice(0, 8).forEach(l => console.log("  " + l));
    return ok;
  } finally {
    chrome.kill();
    await sleep(300);
    rmSync(profile, { recursive: true, force: true });
  }
}

if (!existsSync(join(BUILD, "index.html"))) {
  console.log(`FAIL: no build at ${BUILD} (run tools/unity.sh build)`);
  process.exit(1);
}
const port = await freePort();
const server = spawn("python3", ["-I", "-m", "http.server", String(port), "--bind", "127.0.0.1", "--directory", BUILD], { stdio: "ignore" });
const site = `http://127.0.0.1:${port}`;
let failed = 0;
try {
  await waitFor(async () => (await fetch(site + "/index.html")).ok, 10_000, "the static server");
  for (const r of runs) if (!(await run(r, site))) failed++;
} finally {
  server.kill();
}
process.exit(failed ? 1 : 0);
