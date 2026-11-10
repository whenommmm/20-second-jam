// Headless Chrome and a plain static server, for the build checks (webgl-smoke.mjs, compare-shots.mjs).
import { spawn } from "node:child_process";
import { mkdtempSync, rmSync } from "node:fs";
import { createServer } from "node:net";
import { tmpdir } from "node:os";
import { join } from "node:path";

const CHROME = process.env.CHROME || "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

export const sleep = ms => new Promise(r => setTimeout(r, ms));
export const freePort = () => new Promise(res => { const s = createServer().listen(0, "127.0.0.1", () => { const p = s.address().port; s.close(() => res(p)); }); });

export async function waitFor(fn, ms, what) {
  for (const end = Date.now() + ms; Date.now() < end; await sleep(200)) { try { const v = await fn(); if (v) return v; } catch {} }
  throw new Error("timed out waiting for " + what);
}

// Serves a folder the way itch.io does: no Content-Encoding header on the compressed files.
export async function serve(dir) {
  const port = await freePort();
  const proc = spawn("python3", ["-I", "-m", "http.server", String(port), "--bind", "127.0.0.1", "--directory", dir], { stdio: "ignore" });
  const url = `http://127.0.0.1:${port}`;
  await waitFor(async () => (await fetch(url + "/")).ok, 10_000, "the static server at " + dir);
  return { url, close: () => proc.kill() };
}

// A headless Chrome window of w×h with one page open at `url`, driven over the DevTools protocol.
// (A 960×627 window has a 960×540 page, like the web version's browser checks.)
export async function openChrome(url, w, h) {
  const port = await freePort(), profile = mkdtempSync(join(tmpdir(), "later-chrome-"));
  const chrome = spawn(CHROME, [
    "--headless=new", `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, `--window-size=${w},${h}`,
    // software WebGL by default, so runs don't depend on the machine's GPU; LATER_GPU=1 uses the real one
    ...(process.env.LATER_GPU ? [] : ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"]), "--autoplay-policy=no-user-gesture-required",
    "--hide-scrollbars", "--no-first-run", "--no-default-browser-check", "about:blank"
  ], { stdio: "ignore" });
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
  return {
    send, logs,
    eval: async expression => (await send("Runtime.evaluate", { expression, returnByValue: true }))?.result?.value,
    screenshot: async () => Buffer.from((await send("Page.captureScreenshot", { format: "png" })).data, "base64"),
    close: async () => { ws.close(); chrome.kill(); await sleep(300); rmSync(profile, { recursive: true, force: true }); }
  };
}
