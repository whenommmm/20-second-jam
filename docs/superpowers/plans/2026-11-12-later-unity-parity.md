# LATER Unity: match the web build exactly

**Spec:** the difference list from the 2026-11-10 comparison (web/later vs the Unity WebGL build), all items to fix.
**User decisions (2026-11-11):** the phone font is Inter in *both* versions (bundle it in the web build, redeploy to Vercel);
Unity does not copy two web bugs (a key released in another window stays held; Space re-toggles fullscreen after a click).

**Verified facts this plan rests on:**
- Unity's player cancels touchstart/touchend, so the browser never sends it follow-up mouse events after a tap.
- UI Toolkit draws a filtered element into a texture at 1 texel per local unit, so under the world's scale (2–3× on
  phones/retina) filters come out soft. Drawing the content k× larger and shrinking it back (wrapper scale 1/k,
  inner scale k, filter on the wrapper or on the k×-sized content) renders sharp. An Opacity filter exists, so a
  wrapper with filter opacity(α) fades a subtree as one layer, like CSS opacity.
- Unity's player can create PCM clips at runtime (_JS_Sound_Load_PCM); the loader accepts disabledCanvasEvents and
  errorHandler; on context resume it jumps sounds ahead by the time away (audioContextSuspendedTime).

## Tasks (each: failing test first where testable, then fix, suite green, commit)

1. **Input.** Drop MouseQuiet (mouse steers right after any click, and takes over during a finger drag until the
   finger moves, like the web). A touch starts the round only if it moved ≤ 15 CSS px (a browser tap). Keyboard
   handled like main.js via the page (e.key map, held set, keydown counts only in play, every keydown incl. repeats
   clears the pointer target, arrows preventDefault only in play, Unity's own key handling bypassed), held keys
   cleared on window blur. Double-tap timed from the browser's event timestamps.
2. **Pointer chrome.** Cursor set on the canvas from Unity: pointer over FULLSCREEN, TURN BACK TIME (until pressed),
   PLAY AGAIN, the Messages card and Show less; none only over the stage during play. Messages card hover (.13→.19,
   .15 s). FULLSCREEN: toggled by the page on a real click (press+release on it, primary button) or Enter/Space when
   focused, fullscreening the whole page; Tab focus between Show less and FULLSCREEN with a focus ring; sticky hover
   after a tap. Right-click menu allowed; Unity's error alert off; engine console chatter off.
3. **Page shell.** ROTATE YOUR DEVICE as HTML in the template, like the web (Anton, paper colour via --paper, works
   while loading, taps don't reach the game). Safe-area insets and CSS-pixel viewport sizing exactly like fit().
   Pill widths measured by the page in Archivo Black like measurePillWidths().
4. **Rendering.** Sharp blurred names (supersample k = ceil(scale)); filter removed at blur 0. One-layer fades for
   pills, date slips, the lock screen, the missed-calls group, notifications and the Messages sheet. MSAA 4×.
5. **Timing.** Run In Background on (no freeze on focus loss). Ending and view animations on wall-clock time
   (uncapped), the round on capped dt, dt 0 on the first frame after the tab comes back. The ending starts the frame
   after TIME!. Reopening the sheet restarts its fade cleanly.
6. **Sound.** Synthesise every sound at startup at the device's sample rate (no AAC, no lazy loading, thud noise
   random per session, band-limited triangle); remove the baked WAVs. Ring stop keeps only the notes the web's
   0.4 s look-ahead would have scheduled. Silence cuts a fading ring. Hidden tab suspends audio and resumes in place;
   tap/click unlock and resume on every press, like the web.
7. **Text & layout.** Newsreader instanced at the web's optical sizes (19, 21). Trailing letter-spacing like CSS.
   Voicemail clamp like -webkit-line-clamp (word wrap, ellipsis on the last line). Line heights follow .small flips.
   Badge lands on the new count's text box. Notification time on the title's baseline. "N more messages" bottom
   margin. Messages-card slivers drawn as the web's box-shadows. Fullscreen icon as filled square-capped outlines.
   Show less height from Inter's normal line height. Balance only where the browser supports text-wrap: balance.
   Score formatted by the browser's locale. Fading knobs recoloured. 0.1 px rounding like toFixed.
8. **Web.** Inter 400/600/700 bundled (latin woff2) for the phone, 650 → 600; tests; redeploy to Vercel.
9. **Verify.** All suites, build (≤ 10 MB), smoke, screenshot sweep at 960×540, 844×390@3 and 1000×900@2, final
   review by a fresh reviewer, README.

**Not fixable (report):** screen readers / find-in-page / translate (canvas text), the 9 MB download and loading bar,
SDF vs browser text rasterisation, OS-specific baselines, ≤ 1 frame input/sound latency, the web's own timer drift.
