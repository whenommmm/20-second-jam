import test from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

test("the Unity version's generated data matches data.js", () => {
  const tool = fileURLToPath(new URL("../tools/export-unity.mjs", import.meta.url));
  const run = spawnSync(process.execPath, [tool, "--check"], { encoding: "utf8" });
  assert.equal(run.status, 0, `${run.stdout}${run.stderr}\n(run \`npm run export-unity\` in web/later to regenerate)`);
});
