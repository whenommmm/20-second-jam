import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

test("the bundled fonts ship with the full SIL Open Font License text", () => {
  const ofl = readFileSync(new URL("../fonts/OFL.txt", import.meta.url), "utf8");
  assert.match(ofl, /SIL OPEN FONT LICENSE Version 1\.1/);
  assert.match(ofl, /PERMISSION & CONDITIONS/);
  assert.match(ofl, /DISCLAIMER/);
  for (const name of ["Anton", "Archivo Black", "Newsreader", "Inter"]) assert.match(ofl, new RegExp(`Copyright.*${name}`, "i"), `no copyright line for ${name}`);
});

// the phone's text is Inter, bundled, so it looks the same on every device (and the same as the Unity version), where
// the system font differed from browser to browser (SF Pro in Safari, Helvetica Neue in Chrome on a Mac, Arial on Windows)
test("the phone's text is the bundled Inter, at the weights the Unity version has", () => {
  const css = readFileSync(new URL("../fonts/fonts.css", import.meta.url), "utf8");
  for (const weight of [400, 600, 700]) {
    const face = css.match(new RegExp(`@font-face \\{[^}]*"Inter"[^}]*font-weight: ${weight};[^}]*\\}`));
    assert.ok(face, `no Inter ${weight} face`);
    assert.match(face[0], /url\("inter-[a-z]+-latin\.woff2"\)/);
  }
  const style = readFileSync(new URL("../style.css", import.meta.url), "utf8");
  assert.doesNotMatch(style, /-apple-system|SF Pro|Helvetica Neue/, "a system font is still asked for");
  assert.doesNotMatch(style, /font-weight: 650/, "Inter is bundled at 400, 600 and 700");
  for (const sel of ["#lock {", "#lock .clock {", "#sheet button {", ".flyer .now {"]) {
    const rules = style.split(sel).slice(1).map(r => r.slice(0, r.indexOf("}")));
    assert.ok(rules.some(r => /"Inter"/.test(r)), `${sel} isn't Inter`);
  }
});
