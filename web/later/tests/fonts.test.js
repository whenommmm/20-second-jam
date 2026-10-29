import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

test("the bundled fonts ship with the full SIL Open Font License text", () => {
  const ofl = readFileSync(new URL("../fonts/OFL.txt", import.meta.url), "utf8");
  assert.match(ofl, /SIL OPEN FONT LICENSE Version 1\.1/);
  assert.match(ofl, /PERMISSION & CONDITIONS/);
  assert.match(ofl, /DISCLAIMER/);
  for (const name of ["Anton", "Archivo Black", "Newsreader"]) assert.match(ofl, new RegExp(`Copyright.*${name}`, "i"), `no copyright line for ${name}`);
});
