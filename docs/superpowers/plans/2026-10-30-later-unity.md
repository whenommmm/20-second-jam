# LATER (Unity version) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the jam entry: LATER in Unity 6000.3.2f1 for WebGL. It must play and read exactly like the finished web version, fit in a zip of at most 10 MB, and load on itch.io.

**Architecture:** A faithful port of `web/later/`, file by file:
- **Pure logic.** The data, the rules, the round simulation, the drag input, a scheduler for timers and tweens, and the test players are plain C# in a `Later.Core` assembly with no UnityEngine references. They are tested in EditMode.
- **Parity with the web version.** The round is replayed against traces recorded from the JavaScript simulation, so both versions play identically.
- **Visuals.** Everything on screen is UI Toolkit, with UXML/USS ported from `index.html`/`style.css`. `LaterGame`, `PlayView`, `Ending` and `Sound` mirror `main.js`, `view.js`, `ending.js` and `sound.js`.
- **Data.** Every number and line of text is generated from `web/later/src/data.js`.

**Tech Stack:**
- Unity 6000.3.2f1 with C# (.NET Standard 2.1).
- UI Toolkit: UXML, USS, `filter: blur`, SVGs imported as Vector Images. TextCore font assets for text.
- Input System 1.17 with EventSystem and InputSystemUIInputModule.
- Unity Test Framework 1.6, EditMode and PlayMode, run in batch mode.
- WebGL build with IL2CPP.
- Node 26 for the exporter and the Chrome DevTools Protocol (CDP) smoke test, headless Chrome, and Python 3 `http.server`.

**Spec:** `docs/missed-calls-design.md`.

**Reference:** the web version in `web/later/`, live at https://later-alpha.vercel.app.
- When the spec and the web version disagree on a number, a line of text or the look, the web version wins. On intent, the spec wins.
- Each task names the web code it ports. Copy that code's behaviour, timing and look exactly, changing only what the substitutions table below forces.

## Global Constraints

**Project and workflow**
- Unity 6000.3.2f1, this project, WebGL target. The Editor binary is `/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/MacOS/Unity`.
- Work on the branch `later-unity`. Cut it from the newest LATER web commit: `later-polish` (90b05cb), or `main` if that has been merged by then.
- Batch-mode Unity can't run while this project is open in the Editor. That covers tests, setup, baking and building. `tools/unity.sh` checks for this and says so.
- This plan doesn't need the Unity MCP/Editor connection.
- Commit every `.meta` file with its asset. Files created outside the Editor get their `.meta` on the next `tools/unity.sh` run, so commit after that run.
- `npm test` in `web/later` stays green throughout.
- Ask the user before `unity self-update`, any push, or any merge. Don't touch `~/Unity/Handshake`.

**Content**
- All data comes from `web/later/src/data.js`, through the generated `Assets/Later/Scripts/Core/LaterData.g.cs`. Never hand-edit the generated file; regenerate it with `npm run export-unity` in `web/later`.
- No voices. Sound cuts to silence at TIME!.
- The public title is **LATER** everywhere: product name, browser tab, loading page.

**Layout and timing**
- The world is 540 tall and 960–1280 wide, from `worldWidth`.
  - The width is chosen at round start and stays fixed until the next round.
  - Screens wider than 1280 or narrower than 16:9 are letterboxed, with paper-coloured bars.
  - The ending is laid out 960 wide in the middle of the world.
- 20.0 s of play: 14 calls and 29 pills, all gone by 17 s. TIME! freezes the screen for 0.8 s.
- A careful dodger is never forced to touch a call. This is checked by a sweep every 15 px at widths 960, 1169 and 1280.

**Look**
- Colours:
  - green `#34C759`;
  - red `#FF3B30`, during play only;
  - ink `#141414`;
  - paper `rgb(253,220,172)` → `rgb(146,158,172)`;
  - phone body `#0b0b0c`;
  - lock screen `#1d2025`.
- Fonts are bundled:
  - Anton 400, Archivo Black 400 and Newsreader Italic 400 for the game's three voices.
  - Inter 600/700 for the phone. This is my call: a Unity build can't use the device's system font, and Inter is the closest open font to the iPhone's.
- No glows, gradients or bouncy effects.

**Build**
- The upload zip is at most 10 MB (10,485,760 bytes).
- The build must run from a plain static server. itch.io serves `.gz` files without a `Content-Encoding` header.

## Unity substitutions (decided; everything else is a straight port)

| Web | Unity |
|---|---|
| HTML + CSS | UXML + USS in `Assets/Later/UI/`. Some CSS features have no USS equivalent; each is replaced like this:<br>• positions that use `calc()` are set from C#;<br>• `@keyframes` and custom `cubic-bezier` transitions run as `Timeline` tweens;<br>• `box-shadow` stack peeks become extra elements;<br>• `-webkit-line-clamp` becomes `max-height` + `overflow: hidden`;<br>• `text-wrap: balance` becomes a fixed `max-width` that reproduces the web version's line breaks. |
| CSS variable `--paper` | Every draw, C# sets the paper colour on each element that uses it: the root, the world, the results card, the date cards and the pill knobs. |
| `setTimeout`, `setInterval` and Web Animations | One `Timeline` per owner (`After` / `Tween` / `Clear`), driven by `Time.deltaTime`. |
| `filter: blur()` on names | A USS `filter` blur on each pill's name `Label` (UI Toolkit filters, new in 6.3). Fall back to 8 offset copies at 1/8 opacity only if the WebGL check shows the blur broken or slow. |
| Measured pill widths | Pills are drawn exactly `DEFAULT_PILL_W` wide with the name centred, so the widths in the fairness tests are the real ones. |
| SVG `<use>` icons | `.svg` files imported as UI Toolkit Vector Images with a white fill, tinted with `-unity-background-image-tint-color`. |
| Web Audio synthesis | WAV clips baked at edit time by `SoundBaker` from the same envelopes, played through AudioSources. |
| Pointer events | UI Toolkit pointer events feed a pure `DragInput`. Keys are polled from `Keyboard.current`, so keys can't get stuck after the window loses focus. |
| `matchMedia` and the fullscreen API | `Later.jslib` exports `LaterCanFullscreen`, `LaterCoarsePointer`, `LaterUpright` and `LaterSetTitle`. In the Editor they are stubs returning true, false and false, and `LaterSetTitle` does nothing. Fullscreen toggles `Screen.fullScreen` from the button's `PointerDownEvent`, so Unity requests it on the pointer-up gesture that follows. |
| `Math.round`, `Array.prototype.sort` | `Rules.RoundHalfUp` (`Math.Floor(x + 0.5)`) and stable sorts (`OrderBy`), so values and ties resolve exactly as in JavaScript. `Math.hypot` becomes `Math.Sqrt(dx*dx + dy*dy)`. |
| `#test-<mode>` page hash | The same idea in the build: `#test-boot`, `#test-dodge` and `#test-late`, read from `Application.absoluteURL` and reported through the tab title. |

## Review Focus

Each of these failure modes is pinned by a test added to the task named.

- **Restarting mid-ending** (PLAY AGAIN, or a restart during the flights): every pending callback, tween and flyer from the old ending must be cancelled, so nothing from it shows in the new round. Tested in Task 8.
- **itch.io serving `.gz` with no `Content-Encoding` header:** the build must use Decompression Fallback. The smoke test serves with `python3 -m http.server`, which behaves the same way. Tested in Task 9.
- **A glyph missing from a bundled font** (`…`, `×`, `·`, `'`): every string in `LaterData`, in each font that draws it, must have every glyph. Tested in Task 5.
- **Differences between C# and JavaScript numbers:** C# rounds half to even by default, `Array.Sort` is unstable on lane ties, and `Math.hypot` has no direct equivalent. The parity traces include a player parked exactly between two lanes, and there is a `RoundHalfUp` test. Tested in Tasks 2–3.
- **Touch starting in the side bars, and dragging past an edge then back:** `DragInput` tests in Task 3, and a PlayMode drag in Task 6.

---

## File structure

```
Assets/Later/
├── Scripts/
│   ├── Core/                 asmdef Later.Core (noEngineReferences: true)
│   │   ├── DataTypes.cs      CallDef, Message
│   │   ├── LaterData.g.cs    GENERATED from web/later/src/data.js
│   │   ├── Rules.cs          rules.js
│   │   ├── Round.cs          round.js: Round, Pill, CallState, Vec, RoundInput, RoundEvent, RoundSim
│   │   ├── DragInput.cs      main.js pointer/touch logic
│   │   ├── Timeline.cs       setTimeout/setInterval/Web Animations replacement
│   │   ├── Ease.cs           cubic-bezier easing
│   │   └── Bots.cs           tests/bots.js (shared by tests and the build's #test modes; ~2 KB)
│   ├── Runtime/              asmdef Later.Runtime → Later.Core, Unity.InputSystem
│   │   ├── LaterGame.cs      main.js: boot, fit, input, title, loop, pause, fullscreen, test hooks
│   │   ├── PlayView.cs       view.js
│   │   ├── Ending.cs         ending.js
│   │   ├── Sound.cs          sound.js (plays baked clips)
│   │   ├── Browser.cs        jslib bindings + Editor stubs
│   │   └── SmokeMode.cs      the build's #test-boot/#test-dodge/#test-late
│   ├── Plugins/WebGL/Later.jslib
│   └── Editor/               asmdef Later.Editor (Editor only) → Later.Core, Later.Runtime
│       ├── LaterSetup.cs     RebuildScene, MakeFonts, PanelSettings, theme, build scenes
│       ├── LaterSvgImport.cs AssetPostprocessor: Icons/*.svg → UI Toolkit Vector Image
│       ├── Synth.cs          pure sample generation (sound.js envelopes)
│       ├── SoundBaker.cs     writes Assets/Later/Audio/*.wav
│       └── LaterBuild.cs     WebGL build + zip + size limit
├── UI/  Later.uxml, Later.uss, LaterPanel.asset, LaterTheme.tss, Icons/*.svg
├── Fonts/  Anton-Regular.ttf, ArchivoBlack-Regular.ttf, Newsreader-Italic.ttf, Inter-SemiBold.ttf, Inter-Bold.ttf, OFL.txt, *SDF.asset
├── Audio/  buzz.wav, blip00–14.wav, close.wav, thud.wav, ring.wav
├── Scenes/Later.unity
└── Tests/
    ├── EditMode/  asmdef Later.Tests.EditMode (Editor) → Core, Editor; DataTests, RulesTests, RoundTests,
    │              ParityTests, DragInputTests, TimelineTests, FontTests, SynthTests, Fixtures/round-traces.json
    └── PlayMode/  asmdef Later.Tests.PlayMode → Core, Runtime; LaterTestUtil, PlayTests, ResultsTests, EndingTests
Assets/WebGLTemplates/Later/index.html
tools/unity.sh             batch-mode runner (repo root)
tools/webgl-smoke.mjs      serves the build, drives Chrome over CDP, reads the tab title
tools/compare-shots.mjs    side-by-side screenshots of the web and Unity builds
web/later/tools/export-unity.mjs   writes LaterData.g.cs and the parity traces; --check for drift
web/later/tests/unity-export.test.js
```

Element order in `Later.uxml` gives the z-order. It matches the web version's z-indexes:
- `frame` (root), then `world`, which holds:
  - `play`: `pills`, `score`, `combo`, `dates`, `pops`;
  - `centre` (960×540, centred): `card` (`res`, `lock`), `caption`;
  - `me`;
  - `title`;
  - `timeov`;
  - `centre-top` (960×540, centred): `again`, `restart`;
  - `fly`;
  - `fs`.
- `rotate` comes last, on the root.
- Element names are the web version's ids.

---

### Task 1: Project setup and the generated data

**Files:**
- Modify: `Packages/manifest.json`. Remove `com.unity.visualscripting`, `com.unity.ai.navigation`, `com.unity.multiplayer.center` and `com.unity.collab-proxy`. Keep `com.unity.pipeline`, which the Unity CLI uses.
- Delete: `Assets/Scenes/`, `Assets/TutorialInfo/` and `Assets/Readme.asset`, with their metas.
- Create: the five asmdefs from the file structure.
  - The test asmdefs reference `UnityEngine.TestRunner`, `UnityEditor.TestRunner` and `nunit.framework.dll`, with `defineConstraints: ["UNITY_INCLUDE_TESTS"]`.
- Create: `tools/unity.sh`, `web/later/tools/export-unity.mjs`, `web/later/tests/unity-export.test.js`, `Assets/Later/Scripts/Core/DataTypes.cs` and the generated `LaterData.g.cs`.
- Modify: `web/later/package.json`. Add the script `"export-unity": "node tools/export-unity.mjs"`.
- Test: `Assets/Later/Tests/EditMode/DataTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  namespace Later {
    public readonly struct CallDef { public readonly double T; public readonly string Who; public readonly int N; public readonly double V; public readonly string Date; public readonly bool Home; /* ctor in that order */ }
    public readonly struct Message { public readonly string Who, Date, Text; }
    public static class LaterData {   // generated; member names are data.js's export names, verbatim
      public const double W, H, W_MAX, TOP, BOTTOM;
      public static readonly double[] LANES; public static readonly CallDef[] CALLS;
      public const string START_DATE; public static readonly (double T, string Date)[] QUIET_DATES;
      public static class TUNING { public const double PILL_H, PILL_SPACE, HIT_R, CLOSE_R, MAX_SPEED, KEY_SPEED, FOLLOW_GAIN, FOLLOW_EASE, REACT_MIN, HOME_WAIT, HOME_LOCK_X, HOME_LOCK_DIST, HOME_STEER, HOME_MAX_VY, CLARITY_BONUS, ROUND_TIME; }
      public static readonly IReadOnlyDictionary<string, double> DEFAULT_PILL_W;
      public static readonly int[] PAPER_WARM, PAPER_COLD;
      public static readonly string[] FOLLOW_UP, PENDING; public const string VOICEMAIL, LAST_CALL, LESSON;
      public static readonly IReadOnlyDictionary<string, string> CAPTION_HEADERS;   // "voicemail", "call"
      public static readonly IReadOnlyDictionary<string, Message> FINAL;             // "late", "home", "last"
      public static readonly IReadOnlyDictionary<string, string[]> TURN_BACK;        // [loud, soft]
      public static class RING { public static readonly int[] MOTIF; public const double STEP, REST, VOLUME; }
    }
  }
  ```
- `tools/unity.sh`:
  - `test editmode|playmode [filter]` runs `-batchmode -runTests -testPlatform <P> -testResults Logs/<p>.xml -logFile Logs/<p>.log` and then:
    - on success, prints `PASS <n>/<n>`;
    - on failure, prints each failing test's name and message and exits 1;
    - on a compile error, prints the first compiler error from the log and exits 1.
  - `run <Type.Method>` runs `-batchmode -quit -executeMethod`.
  - `build` runs `Later.Editor.LaterBuild.WebGL`.
  - Before any of these, if `Temp/UnityLockfile` is held (`lsof`), it prints "Close the Unity Editor (this project is open), then run again." and exits 2.

- [ ] **Step 1:** Create the branch `later-unity`. Remove the packages and template leftovers. Create the asmdefs and `tools/unity.sh` (made executable).
- [ ] **Step 2: Write the failing tests.**
  - `unity-export.test.js` asserts that `node tools/export-unity.mjs --check` exits 0, printing its output on failure.
  - `DataTests.cs` ports every assertion in `web/later/tests/data.test.js`:
    - `CALLS.Length == 14`, the sum of `N` is 29, and `LANES` is `{54,116,178,240,302,364,426,488}`;
    - `CALLS[13]` is `(12.4,"Mom",1,280,"MAR 28",true)` and `CALLS[4]` is `(4.4,"Jessie",3,250,"MAR 15",false)`;
    - the 13/13 text counts, `FOLLOW_UP[0]`, `PENDING[8]` and `PENDING[12]`;
    - the `VOICEMAIL` regex `^Hi sweetheart, it's just me\. No reason… .* I'm so proud of you\.$` and the `LAST_CALL` prefix;
    - `FINAL["late"].Text`, `TURN_BACK["last"][0]` and `LESSON`.
- [ ] **Step 3:** Run `cd web/later && npm test` and `tools/unity.sh test editmode`. Expected: the Node test FAILS (no exporter), and EditMode FAILS to compile (`LaterData` is missing).
- [ ] **Step 4:** Write `export-unity.mjs`.
  - It imports `../src/data.js` and writes `LaterData.g.cs` with a header saying it's generated and must not be edited.
  - Numbers are printed with JavaScript's `String(n)`. Strings get C# escapes; non-ASCII stays UTF-8.
  - It throws on any `data.js` export it doesn't know how to write, so new data can't be silently dropped.
  - With `--check`, it compares its output with the files on disk instead of writing them, and exits 1 on any difference, naming the file.
  - Generate the file.
- [ ] **Step 5:** Run both commands again. Expected: `npm test` all pass (now 35), EditMode `PASS 3/3`.
- [ ] **Step 6:** Commit: "Unity: project setup, assemblies, test runner, data generated from data.js". Include the metas and `Packages/packages-lock.json`.

### Task 2: Rules

**Files:**
- Create: `Assets/Later/Scripts/Core/Rules.cs`
- Test: `Assets/Later/Tests/EditMode/RulesTests.cs`

**Interfaces:**
- Consumes: `LaterData`, `Message` (Task 1).
- Produces:
  ```csharp
  public static class Rules {
    public static double RoundHalfUp(double x);                       // JS Math.round
    public static double WorldWidth(double w, double h);
    public static string ShortDate(string d);
    public static double Clarity(double t, double bonus);
    public static (int R, int G, int B) Paper(double k);
    public static string RankFor(int hits);
    public static string EndingFor(IReadOnlyList<bool> answered);     // 14 entries → "late" | "home" | "last"
    public static List<Message> BuildThread(IReadOnlyList<bool> answered, string ending);
    public static (string Loud, string Soft) TurnBackLines(string ending, int missed);
    public static double CapsuleDist(double x, double y, double w, double h, double px, double py); // pill centre x,y
    public static List<(double At, int Midi)> RingNotes(double from, double to);
    public static string FormatScore(int n);                          // en-US toLocaleString: "1,234", "-1,234"
  }
  ```

- [ ] **Step 1: Write the failing tests.** Port every test in `web/later/tests/rules.test.js` with the same values (`Paper` returns a tuple where the web returns an `rgb()` string). Add:
  - `RoundHalfUp`: `0.5 → 1`, `2.5 → 3`, `-2.5 → -2`.
  - `FormatScore`: `0 → "0"`, `1234 → "1,234"`, `-1234 → "-1,234"`, `-100 → "-100"`.
- [ ] **Step 2:** Run `tools/unity.sh test editmode`. Expected: FAIL to compile (`Rules` is missing).
- [ ] **Step 3:** Implement `Rules.cs` by porting `rules.js` function by function. Use `RoundHalfUp` wherever the JavaScript calls `Math.round`.
- [ ] **Step 4:** Run it again. Expected: PASS.
- [ ] **Step 5:** Commit: "Unity: rules, ported from rules.js".

### Task 3: The round, the test players, drag input, and parity with the JavaScript

**Files:**
- Create: `Assets/Later/Scripts/Core/Round.cs`, `DragInput.cs` and `Bots.cs`
- Modify: `web/later/tools/export-unity.mjs`. It also writes `Assets/Later/Tests/EditMode/Fixtures/round-traces.json`.
- Test: `Assets/Later/Tests/EditMode/RoundTests.cs`, `ParityTests.cs` and `DragInputTests.cs`

**Interfaces:**
- Consumes: `LaterData`, `Rules.CapsuleDist`.
- Produces:
  ```csharp
  public struct Vec { public double X, Y; public Vec(double x, double y); }
  public struct RoundInput { public Vec? Target; public int KeysX, KeysY; }
  public enum RoundEventType { Date, Spawn, Pass, Unpass, Hit, Remove, Time }
  public sealed class RoundEvent { public RoundEventType Type; public Pill Pill; public bool Close; public string Date; }
  public sealed class Pill { public int Id, Ci, Gain; public string Who; public bool First, Passed, Locked, Dead; public double X, Y, W, H, Vx, Vy, MinD, Seed; }
  public sealed class CallState { public CallDef Def; public double V, Start, Gap; public double? Y0; public List<double> Band; public int Spawned, Alive, MissedN; public bool Answered; }
  public sealed class Round {
    public IReadOnlyDictionary<string, double> PillW; public double W, K, T, Tilt, Bonus, PaperK;
    public Vec Me, Vel; public CallState[] Calls; public List<Pill> Pills;
    public int NextId, NextCall, QuietIdx, Score, Combo, Best, CloseN, Missed, Dodged, Hits; public bool Over;
    public IReadOnlyList<bool> Answered { get; }   // per call, for Rules.EndingFor / BuildThread
  }
  public static class RoundSim {
    public static Round Create(IReadOnlyDictionary<string, double> pillW = null, double w = LaterData.W);
    public static List<RoundEvent> Step(Round r, double dt, RoundInput input);
  }
  public sealed class DragInput {
    public Vec? Target { get; }
    public void Reset();
    public void MouseMove(Vec world);
    public void TouchDown(int id, Vec world, Round r);
    public void TouchMove(int id, Vec world, Round r);
    public void TouchUp(int id);
  }
  public static class Bots { public static Vec DodgeTarget(Round r, double x); public static Vec ChaseTarget(Round r, Func<Pill, bool> wants = null); }
  ```
- `round-traces.json` is laid out for `JsonUtility`:
  ```json
  { "traces": [ { "name": "", "w": 960, "dt": 0.016666666666666666,
      "tx": [], "ty": [], "has": [], "kx": [], "ky": [],
      "events": [ { "step": 0, "type": "spawn", "pill": 1, "close": false, "date": "" } ],
      "checkpoints": [ { "step": 29, "t": 0, "x": 0, "y": 0, "score": 0, "missed": 0, "hits": 0, "paperK": 0, "pills": 0 } ] } ] }
  ```
  - `tx`, `ty`, `has`, `kx` and `ky` hold the input for each step. Targets are rounded to 3 decimals before the JavaScript round sees them, so the replay feeds the identical numbers.
  - There is a checkpoint every 30 steps, plus one at the end.

- [ ] **Step 1: Write the failing tests.**
  - `Bots.cs` ports `tests/bots.js` exactly.
  - `RoundTests.cs` ports every test in `web/later/tests/round.test.js`, keeping their names and values: the three-width sweep at 15 px steps, the wide-world tests, the homing test at 960 and 1280, the long-frame test, and the others.
- [ ] **Step 2:** Run `tools/unity.sh test editmode`. Expected: FAIL to compile.
- [ ] **Step 3:** Implement `Round.cs`, porting `round.js` function by function:
  - `createRound`, `stepRound`, `step`, `move`, `startCalls`, `spawnPills`, `movePills`, `pass`, `unpass`, `laneFree`, `laneWindows`, `clearDuring`, `fair`, `pickLane`.
  - Keep `STEP = 1.0/60` and the loop `for (left = dt; left > 1e-9 && !over; left -= STEP)`.
  - Sort lanes with `OrderBy`, which is stable. `nearestLane` keeps the first lane among ties.
  - A missing lane is `double?` null.
  - Iterate the pills by index, because a hit marks other pills dead inside the loop.
- [ ] **Step 4:** Run it again. Expected: PASS, including the sweep at all three widths.
- [ ] **Step 5:** Extend the exporter to write `round-traces.json` from the JavaScript round with these scenarios:

  | name | w | dt | input each step |
  |---|---|---|---|
  | dodge-340 | 960 | 1/60 | `dodgeTarget(r, 340)` |
  | chase-all | 960 | 1/60 | `chaseTarget(r)` |
  | last-only | 960 | 1/60 | chase call 13 once it spawns, else `dodgeTarget(r, 340)` |
  | still-long-frames | 960 | 0.2 | `(340, 302)` |
  | between-lanes | 960 | 1/60 | `(340, 85)`, exactly between two lanes |
  | dodge-wide | 1169 | 1/60 | `dodgeTarget(r, 1100)` |
  | home-drift | 1280 | 1/60 | `(200, 302 + 150·sin(3t))` |
  | keys | 1280 | 1/60 | no target; `kx = sign(sin(1.3t))`, `ky = sign(cos(0.9t))` |

  Write `ParityTests.cs`. For each trace, replay its inputs and check:
  - the events are identical: step, type, pill id, close and date;
  - every checkpoint matches within 1e-6, and `paperK` within 1e-9.

  Run `npm test` (the `--check` covers the fixture) and `tools/unity.sh test editmode`. Expected: both PASS.
- [ ] **Step 6:** Write `DragInputTests.cs`, porting the intent of the browser checks `touch`, `touchmargin` and `touchclamp`:
  - (a) A touch that goes down at world `(-100, 270)`, in the left bar, then moves by `(+50, +20)`, sets `Target = me + (50, 20)`.
  - (b) Dragging 300 px past the right edge and then 10 px back sets `Target.X == r.W - 26 - 10`, so turning back responds straight away.
  - (c) A second finger is ignored while the first is down.
  - (d) `MouseMove(p)` sets `Target = p`.

  Run them: FAIL. Implement `DragInput` by porting the pointer handlers in `main.js`. Run them: PASS.
- [ ] **Step 7:** Commit: "Unity: the round, test players and drag input; replays the JavaScript traces exactly".

### Task 4: Timeline and easing

**Files:**
- Create: `Assets/Later/Scripts/Core/Timeline.cs` and `Ease.cs`
- Test: `Assets/Later/Tests/EditMode/TimelineTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public sealed class Timeline {
    public double Now { get; }
    public void After(double seconds, Action fn);              // relative to Now when called, like setTimeout
    public void Tween(double delay, double duration, Func<double, double> ease, Action<double> apply, Action done = null);
    public void Update(double dt);                             // runs everything due, in time order
    public void Clear();                                       // cancels everything pending; nothing runs afterwards
  }
  public static class Ease {
    public static Func<double, double> CubicBezier(double x1, double y1, double x2, double y2);
    public static readonly Func<double, double> Linear, CssEase /* .25,.1,.25,1 */, EaseOut /* 0,0,.58,1 */;
  }
  ```
- `Tween` calls `apply(ease(p))` on every `Update` while running. When it finishes it calls `apply(ease(1))` exactly once, then `done`.

- [ ] **Step 1: Write the failing tests.**
  - `After(2,a); After(1,b); Update(10)` runs b then a.
  - A callback scheduled with `After(0, …)` from inside another callback runs in the same `Update`. One scheduled with `After(1, …)` from a callback at t = 1 runs at t = 2, not earlier, even inside one `Update(10)`.
  - `Clear()` stops all pending callbacks and tweens.
  - A tween with delay 0.5 and duration 1 doesn't apply before 0.5, ends on exactly 1, and calls `done` once.
  - `CubicBezier(.25,.1,.25,1)(0.5)` is within 1e-4 of 0.8024. It returns 0 at 0 and 1 at 1, and `Linear(x) == x`.
- [ ] **Step 2:** Run `tools/unity.sh test editmode`. Expected: FAIL to compile.
- [ ] **Step 3:** Implement both files. `CubicBezier` uses Newton's method with a bisection fallback to solve for x, as browsers do.
- [ ] **Step 4:** Run it again. Expected: PASS.
- [ ] **Step 5:** Commit: "Unity: timeline and cubic-bezier easing (setTimeout and Web Animations stand-ins)".

### Task 5: Fonts, icons and sounds

**Files:**
- Create: `Assets/Later/Fonts/*.ttf` and `OFL.txt`; `Assets/Later/UI/Icons/{moon,handset,bubble,flashlight,camera,fullscreen}.svg`; `Editor/LaterSvgImport.cs`, `Synth.cs` and `SoundBaker.cs`; `Assets/Later/Audio/*.wav`
- Create: `Editor/LaterSetup.cs`, with only `MakeFonts()` for now
- Test: `Assets/Later/Tests/EditMode/FontTests.cs` and `SynthTests.cs`

**Interfaces:**
- Produces:
  - Font assets `Assets/Later/Fonts/{Anton,ArchivoBlack,Newsreader-Italic,Inter-SemiBold,Inter-Bold} SDF.asset`. Each is a dynamic TextCore `FontAsset` with its source font included in the build.
  - Vector Images at `Assets/Later/UI/Icons/<name>.svg`.
  - Clips `Assets/Later/Audio/{buzz, blip00…blip14, close, thud, ring}.wav`.
  - `public static class Synth { public const int Rate = 44100; public static float[] Buzz(), Blip(int streak), Close(), Thud(), Ring(); }`. Every clip is already multiplied by the master gain 0.55, and `Ring` also by `RING.VOLUME`.
  - `SoundBaker.Bake()` and `LaterSetup.MakeFonts()`, both static and batch-callable.

- [ ] **Step 1:** Fetch the fonts. Request the stylesheet with `curl -A "Mozilla/4.0" "https://fonts.googleapis.com/css2?family=Anton&family=Archivo+Black&family=Newsreader:ital,wght@1,400&family=Inter:wght@600;700"`; with a non-woff2 user agent it lists TTF URLs.
  - Download each TTF under the names in the file structure.
  - Copy `web/later/fonts/OFL.txt` and append Inter's copyright line, "Copyright (c) 2016 The Inter Project Authors (https://github.com/rsms/inter)".
- [ ] **Step 2: Write the failing tests.**
  - `FontTests` checks `HasCharacters(text, out missing, false, true)` for each pairing below, and the failure message lists the missing characters:
    - **Anton:** "LATER", "CLICK TO START", "TAP TO START", "TIME!", "PERFECT ×29", "CLOSE!", `START_DATE`, every `CALLS` date and `QUIET_DATES` date, "0123456789,-/×", "DODGED", "CLOSE CALLS", "BEST STREAK", "PERFECT!", "GREAT!", "NICE TRY", "…or is it?", "ROTATE YOUR DEVICE", every loud `TURN_BACK` line with "29 TIMES"/"ONCE"/"TWICE", and `LESSON`.
    - **Archivo Black:** every caller name, "LATER", "RESULTS", "FULLSCREEN", "PLAY AGAIN", and both `CAPTION_HEADERS`.
    - **Newsreader Italic:** `VOICEMAIL`, `LAST_CALL`, and every soft line.
    - **Inter SemiBold:** every `FOLLOW_UP`, `PENDING` and `FINAL` text, `VOICEMAIL`, "Thursday, April 2", "7:12", "Later", "Notifications silenced", "Missed Calls", "Voicemail · 0:18", "Show less", "more messages", "5G", and every `ShortDate`.
    - **Inter Bold:** "Messages".
  - `SynthTests`:
    - **Ring length:** `Ring().Length == RoundHalfUp(44100 × (STEP·6 + REST))`.
    - **Ring timing:** each of the six note onsets in `Ring()` falls within 2 ms of `RingNotes(0, cycle)`. An onset is the first sample after the note time whose amplitude exceeds 1e-3.
    - **Peaks:** the largest sample in `Ring()` is at most `RING.VOLUME·0.55·1.3`. Every clip's largest sample is at most 0.55.
    - **Durations:** `Buzz()` lasts 0.4 s.
    - **Pitch:** the zero-crossing frequency of `Blip(14)` is within 2% of 523·2^(14/12).
    - **No clicks:** every clip starts and ends within 1e-3 of zero.
- [ ] **Step 3:** Run `tools/unity.sh test editmode`. Expected: FAIL to compile.
- [ ] **Step 4:** Implement the fonts, icons and sounds.
  - **Fonts:** `MakeFonts()` creates the font assets (atlas 1024, padding 9, dynamic).
  - **Icons:** write the SVGs.
    - Use the exact path data from `web/later/index.html` (`moon`, `handset`, `bubble`, and the inline flashlight, camera and fullscreen icons), on a 24×24 viewBox with a white fill.
    - The camera keeps its `#3a3e45` lens circle. The fullscreen icon keeps its stroke (2.4, square caps).
    - `LaterSvgImport` sets the Generated Asset Type to UI Toolkit Vector Image for files in `UI/Icons`.
  - **Sounds:** `Synth` ports `sound.js`, one function per sound:
    - `env` and `blip`;
    - `marimba` (partials `[1,1,.7]` and `[4,.25,.07]`, exponential decay to 0.0001);
    - `buzz`: a triangle wave at 165 Hz through a 280 Hz two-pole low-pass (Q 0.707), with pulses at 0 and 0.2 s;
    - `thud`: a sine sweeping exponentially from 110 to 45 Hz over 0.25 s, plus seeded white noise through a 300 Hz low-pass;
    - `close`: 880 Hz, then 1320 Hz 0.06 s later;
    - `Ring`: one cycle of `RingNotes(0, cycle)`, exactly one cycle long, so it loops seamlessly.
  - **Baking:** `SoundBaker.Bake()` writes 16-bit mono WAVs. Set their importers to Force To Mono and Decompress On Load.
  - Run `tools/unity.sh run Later.Editor.LaterSetup.MakeFonts` and `tools/unity.sh run Later.Editor.SoundBaker.Bake`.
- [ ] **Step 5:** Run `tools/unity.sh test editmode`. Expected: PASS.
- [ ] **Step 6:** Commit: "Unity: bundled fonts, SVG icons, sounds baked from the web version's synthesis".

### Task 6: The scene, the title screen and play

**Files:**
- Modify: `Editor/LaterSetup.cs`. Add `RebuildScene()`, which:
  - creates `UI/LaterPanel.asset` (Constant Pixel Size, sort 0) and `UI/LaterTheme.tss` (`@import url("unity-theme://default");`);
  - creates `Scenes/Later.unity` containing a Camera (solid clear, paper colour), an EventSystem with InputSystemUIInputModule, and a `LATER` object with UIDocument, `LaterGame` and `Sound`;
  - sets the build scenes to only `Later.unity`.
- Create: `UI/Later.uxml`, `UI/Later.uss`, `Runtime/LaterGame.cs`, `PlayView.cs`, `Sound.cs`, `Browser.cs` and `Plugins/WebGL/Later.jslib`
- Test: `Assets/Later/Tests/PlayMode/LaterTestUtil.cs` and `PlayTests.cs`

**Interfaces:**
- Consumes: Tasks 1–5.
- Produces:
  ```csharp
  public sealed class LaterGame : MonoBehaviour {
    public static LaterGame Instance;
    public static Vector2Int? ScreenOverride;      // tests pretend the screen is this size (pixels)
    public string State { get; }                   // "title" | "play" | "over"
    public Round Round { get; } public double WorldW { get; }   // Task 7 adds: public Ending Ending { get; }
    public VisualElement Root { get; }
    public Vec? ForcedTarget;                      // like the web's __later.setTarget
    public void StartRound();
    public void PointerDown(int id, bool touch, Vector2 panelPos);   // the UI callbacks forward here
    public void PointerMove(int id, bool touch, Vector2 panelPos);
    public void PointerUp(int id);
  }
  public sealed class PlayView { public PlayView(VisualElement world, Sound sound); public void Reset(); public void Handle(List<RoundEvent> events, Round r); public void Draw(Round r); }
  public sealed class Sound : MonoBehaviour { public void Unlock(); public void RingStart(); public void RingStop(float fade = 1.5f); public bool Ringing { get; } public void Silence(); public void Buzz(); public void Dodge(int streak); public void Close(); public void Thud(); }
  public static class Browser { public static bool CanFullscreen, CoarsePointer, Upright; public static bool? UprightOverride; public static void SetTitle(string t); public static string Hash { get; } }
  ```
- `LaterTestUtil` (PlayMode):
  - `IEnumerator Load(Vector2Int? screen = null)` loads the scene and sets `Time.captureDeltaTime = 1/30f`.
  - `IEnumerator Wait(double gameSeconds)`.
  - `IEnumerator WaitFor(Func<bool> cond, double maxGameSeconds, string what)` fails the test with `what` on timeout.
  - `IEnumerator Play(Func<Round, Vec> bot)` starts a round and sets `ForcedTarget` every frame until `State == "over"`.
  - `void Click(VisualElement el)` sends a `ClickEvent` to `el`.
  - `bool Shown(VisualElement el)`: the element and all its parents have display, visibility and opacity above 0.5.

- [ ] **Step 1: Write the failing tests** in `PlayTests`, porting the browser checks `title`, `dodge`, `widths`, `widephone` (the play part), `touchmargin` and `hidden`:
  - `Title_ShowsLaterAndClickToStart`: "LATER" and "CLICK TO START" are shown, and so is `fs`, with "FULLSCREEN".
  - `CarefulDodger_FinishesUntouched_PaperEndsCold`: `Play(r => Bots.DodgeTarget(r, 340))`. Then `Hits == 0` and `T == 20`, and the world's background is within 3/255 of `Paper(Round.PaperK)` with `PaperK > 0.95`.
  - `Pills_AreDrawnAtTheFairnessWidths`: during play, each pill's `resolvedStyle.width` equals `DEFAULT_PILL_W[who]`.
  - `WidePhone_WorldFillsTheScreen`: with screen (844, 390), `WorldW == 1169` and the world spans the panel's full width. The moon, steered to `(r.W - 60, 270)` for 4 s, ends up with `Me.X > 1100`.
  - `TouchInTheSideBar_MovesTheMoon`: with screen (2000, 627) there are bars. A `PointerDown` touch in the left bar, then a move of +80 px, moves `Me.X` by more than 40 px within 1 s.
  - `UprightPhone_PausesPlay`: with `UprightOverride = true`, `T` doesn't advance over 1 s. With false, it does.
- [ ] **Step 2:** Run `tools/unity.sh test playmode`. Expected: FAIL to compile.
- [ ] **Step 3:** Run `tools/unity.sh run Later.Editor.LaterSetup.RebuildScene`. Then port the following.
  - **Markup:** `index.html` → `Later.uxml`, in the element order from the file structure. `style.css` → `Later.uss` for the title, play, moon, badge, tag, pills, dates, pops, TIME!, fullscreen button and rotate overlay.
  - **`main.js` → `LaterGame`:**
    - `fit` sets the panel scale to `min(Sw/ww, Sh/540)` and places the world centred.
    - `start`, `endRound` and the pointer wiring go through `DragInput`.
    - `readKeys` becomes polling: arrows and WASD, and a press clears the pointer target.
    - The fullscreen button's visibility follows `.nofs` and the play state.
    - The loop clamps dt to 0.1 and pauses while upright. A hidden tab already pauses, because run in background is off, and the clamp keeps the first frame back short. The ring stops once every call has come and gone.
    - TAP TO START shows when `CoarsePointer` is true.
    - The cursor is hidden during play.
  - **`view.js` → `PlayView`:** the slip, pop, badge, buzz, shake, `--nb` blur, paper and score formatting, via `Rules.FormatScore`.
  - **`sound.js` → `Sound`:** baked clips, a looping ring with a 1.5 s volume fade, and Silence stops everything.
  - **`Later.jslib` and `Browser`:** implement them.
- [ ] **Step 4:** Run `tools/unity.sh test playmode` and `tools/unity.sh test editmode`. Expected: PASS.
- [ ] **Step 5:** Commit: "Unity: scene, title screen and play, ported from main.js and view.js".

### Task 7: Results, the shrink into the phone, and the flights

**Files:**
- Create: `Runtime/Ending.cs`, the part covering `ending.js` lines 18–154: TIME!, the results, `countUp`, `morph`, `box`, `flyer`, `fly`, the moon+tag+pill flight and the badge flight.
- Modify: `UI/Later.uxml` and `Later.uss`, porting the `#timeov`, `#card`, `#res`, `#lock` and `.flyer` CSS; `Runtime/LaterGame.cs`, which gains `public Ending Ending { get; }`. Its `endRound` creates the `Ending` and calls `Update(dt)` on it each frame, and `StartRound` disposes the old one first.
- Test: `Assets/Later/Tests/PlayMode/ResultsTests.cs`

**Interfaces:**
- Consumes: `Timeline`, `Ease`, `Rules`, `Round`.
- Produces:
  ```csharp
  public sealed class Ending {
    public Ending(Round round, VisualElement world, Sound sound, Action onRestart);
    public string Key { get; }          // "late" | "home" | "last"
    public void Update(double dt);
    public void TapTwice();
    public void Dispose();
  }
  ```
- Boxes come from `worldBound` converted into world coordinates. Flights evaluate the web version's Bezier formulas at the eased progress, instead of 40 sampled keyframes. The colour keyframes (`toWhite`, the badge background) are piecewise over the same offsets.

- [ ] **Step 1: Write the failing tests**, porting the browser checks `morphpill` and the results part of `late`:
  - `Results_ShowTheRound`: after a dodge-all round, by 2.1 s after TIME!:
    - `resnum` shows `FormatScore(score)`;
    - `r1` reads "29 / 29", `r2` equals `CloseN`, and `r3` reads "×" + `Best`;
    - the rank reads "PERFECT!".
  - `OrIsIt_OnlyAfterAPerfectRound`: `doubt` is shown 3.4 s after TIME! after a perfect round, and not after a round with one hit (steer into call 0, then dodge).
  - `Phone_LandsCentred` at screens (960, 540) and (844, 390):
    - 6 s after TIME!, `card` is 256×516, and its centre is within 1 px of the world's centre;
    - by 8 s, `fly` has no children, `focus` has opacity 1, and `sbMoon` is shown;
    - `gcount` equals `Missed`, and `gnames` is the distinct callers, newest first.
- [ ] **Step 2:** Run `tools/unity.sh test playmode`. Expected: FAIL.
- [ ] **Step 3:** Implement the ending up to `later(2600, endingMessages)`, with `endingMessages` left as a stub. Use the same delays (800, 150, 700 + 170·i, 1550, 2500, 4100, 180, 1000, 1250, 500, 1050, 1500, 2600 ms), durations and easings:
  - the card shrink: 0.85 s, `cubic-bezier(.6,0,.2,1)`;
  - the moon glide: 0.7 s, `cubic-bezier(.3,0,.2,1)`;
  - the flights: 1.3 s and 1.5 s, `cubic-bezier(.45,.05,.2,1)`.
- [ ] **Step 4:** Run it again. Expected: PASS. Also run the EditMode suite and check it's still green.
- [ ] **Step 5:** Commit: "Unity: results screen, the shrink into the phone, and the flights home".

### Task 8: The lock screen, her words, TURN BACK TIME and PLAY AGAIN

**Files:**
- Modify: `Runtime/Ending.cs`, the part covering `ending.js` lines 156–246: `note`, `reveal`, `endingMessages`, `openThread`, `playCaption`, `showLine`, `showAllLines`, TURN BACK TIME, PLAY AGAIN and `dispose`.
- Modify: `UI/Later.uxml` and `Later.uss`, porting `.n`, `#items`, `#group`, `#sheet`, `#caption`, `#again` and `#restart`, with the small-screen sizes from `@media (max-height: 460px)` applied when the world scale puts it below 460 px tall.
- Modify: `Runtime/LaterGame.cs`. A double tap within 350 ms calls `Ending.TapTwice()`, and PLAY AGAIN calls `StartRound()`.
- Test: `Assets/Later/Tests/PlayMode/EndingTests.cs`

**Interfaces:**
- Consumes: Task 7's `Ending`.
- Click handlers check their own guards, because a dispatched `ClickEvent` doesn't respect `pickingMode`. A pending card ignores clicks, and TURN BACK TIME answers only once.
- Captions are split with the regex `[^.?!…]+[.?!…]+`.

- [ ] **Step 1: Write the failing tests**, porting the browser checks with conditions, not fixed sleeps. Each test waits with `WaitFor` for a named state, then checks.
  - **Ending checks:**
    - `Late`: dodge everything.
      - The top message is John, "Funeral's on Saturday."
      - The voicemail card is shown, and the caption header is `CAPTION_HEADERS["voicemail"]` followed by all nine voicemail lines.
      - TURN BACK TIME appears 1.2 s after the last line.
      - Clicking it shows "YOU SAID LATER 29 TIMES.", then the soft line 1.8 s later, then `LESSON` 3.6 s after the click. PLAY AGAIN is hidden until 6.6 s after the click and shown after that.
    - `Home`: answer call 0 and her last call. The top message is `FINAL["home"]` and the caption is the call version. The loud line reads "THERE'S NEVER ENOUGH TIME."
    - `LastOnly`: answer only her last call. The top message is `FINAL["last"]`, and the loud line reads "YOU GAVE HER FOUR MINUTES."
  - **Messages sheet:**
    - `Thread`: clicking the Messages card opens the sheet with 14 messages, newest first. "Show less" closes it, and clicking the card again reopens it.
    - `PendingCard`: clicking the Messages card before it's revealed opens nothing.
  - **Timing and repeat clicks:**
    - `DoubleTap`: two taps 200 ms apart, during the second caption line, show every line at once. TURN BACK TIME still follows 1.2 s later.
    - `Fallback`: if TURN BACK TIME is never clicked, PLAY AGAIN shows 15 s after it appeared, not before.
    - `TurnBackTwice`: a second click changes nothing; the text stays the same 5 s later.
  - **Restarts:**
    - `Restart_AfterTheLesson`: clicking PLAY AGAIN starts a fresh round (`State == "play"`, `T < 1`). At 2, 10 and 18 s later, none of these are shown: the caption, again, restart, the card, `timeov` or any `fly` child. The world also has the warm paper colour and a single date card.
    - `Restart_MidFlight`: calling `StartRound()` 6.6 s after TIME!, during the flights, leaves no `fly` children. `focus` has opacity 0, `lock` is not shown, and the round plays normally for 5 s.
  - **Wide phone:**
    - `WidePhone_EndingLayout`: at (844, 390), after TURN BACK TIME and the lesson:
      - the caption is entirely right of the phone;
      - `again` is right of the phone and doesn't overlap the caption;
      - `restart` is left of the phone;
      - the phone is centred within 1 px.
- [ ] **Step 2:** Run `tools/unity.sh test playmode`. Expected: FAIL.
- [ ] **Step 3:** Implement the rest of the ending.
  - The voicemail card is clamped to 3 lines, or 2 when it would reach the silenced line, matching the web version.
  - The sheet list is a `ScrollView` with touch scrolling and hidden scrollbars.
- [ ] **Step 4:** Run both suites. Expected: PASS.
- [ ] **Step 5:** Commit: "Unity: lock screen, her words, TURN BACK TIME and PLAY AGAIN".

### Task 9: The WebGL build, and the itch.io check

**Files:**
- Create: `Assets/WebGLTemplates/Later/index.html`, `Editor/LaterBuild.cs`, `Runtime/SmokeMode.cs` and `tools/webgl-smoke.mjs`
- Modify: `Runtime/LaterGame.cs`, which starts `SmokeMode` when `Browser.Hash` starts with `#test-`.

**Interfaces:**
- `LaterBuild.WebGL()`:
  - **Player settings:** product name "LATER"; template `PROJECT:Later`; Gzip compression with Decompression Fallback on; IL2CPP code generation Optimize Size; managed stripping High; splash screen off; run in background off.
  - **Output:** builds `Later.unity` to `Builds/LATER-webgl/`, then zips it to `Builds/LATER-webgl.zip` with `index.html` at the zip's root.
  - **Size check:** prints the zip size, and throws (exit code 1) if it's over 10,485,760 bytes.
- `SmokeMode`:
  - Sets `Time.timeScale` from the hash's `&speed=`, default 4.
  - Turns any logged error or exception into a failure.
  - Reports `OK <mode>` or `FAIL <reasons>` through `Browser.SetTitle`.
  - Modes:
    - `boot`: the title screen is up and the fonts have loaded.
    - `dodge`: dodges at x = 340; OK if there were no hits and TIME! fired.
    - `late`: follows the web harness's `late` schedule in game time.
      - Start and dodge at 340.
      - At 30 s, check the top message and the caption header.
      - At 40.5 s, click TURN BACK TIME.
      - At 44.4 s, check the lesson is shown and PLAY AGAIN isn't.
      - At 47.4 s, check PLAY AGAIN is shown.
- `webgl-smoke.mjs [mode…]`:
  - Serves `Builds/LATER-webgl` with `python3 -I -m http.server` on a free port.
  - Launches headless Chrome with `--remote-debugging-port`, `--use-angle=swiftshader` and `--enable-unsafe-swiftshader`.
  - Opens `index.html#test-<mode>` and polls `document.title` over CDP (Node's built-in WebSocket) every 500 ms, with a 120 s limit.
  - Prints `PASS`/`FAIL` per mode and exits non-zero on any failure.
  - Default runs: `boot`, `dodge`, and `late` at 960×627; `dodge` at 844×477.

- [ ] **Step 1:** Write `webgl-smoke.mjs`. Run `node tools/webgl-smoke.mjs`. Expected: FAIL (no build).
- [ ] **Step 2:** Write the template.
  - Base it on Unity's Minimal template, with `<title>LATER</title>` and the web version's viewport meta.
  - The body background is `rgb(253,220,172)`, and the canvas fills the window (100vw × 100dvh).
  - The loading bar is a thin ink bar, with no Unity logo.
  - The page sets `touch-action: none` and `user-select: none`.
- [ ] **Step 3:** Write `LaterBuild` and `SmokeMode`. Run `tools/unity.sh build`, then `node tools/webgl-smoke.mjs`. Expected:
  - The build prints a zip size under 10 MB.
  - All four smoke runs PASS.
- [ ] **Step 4:** Run both Unity suites and `npm test`. Expected: PASS.
- [ ] **Step 5:** Commit: "Unity: WebGL build for itch.io, with a real-browser smoke check".

### Task 10: Side by side with the web version, and the docs

**Files:**
- Create: `tools/compare-shots.mjs`
- Modify: `README.md`, `docs/missed-calls-design.md` (Constraints: the Inter font, how the Unity version is built and checked), and `web/later/README` (only if it exists)

- [ ] **Step 1:** Write `compare-shots.mjs`.
  - It runs both builds in headless Chrome at 960×627 and 844×477: the web version from `web/later` with `#test-late`, and the Unity build with `#test-late&speed=1`. Both follow the same schedule, so the same moments line up.
  - It saves screenshots to `Builds/shots/<size>-<moment>-{web,unity}.png` at these moments:
    - the title;
    - 10 s into play;
    - 2 s after TIME! (results);
    - 12 s after TIME! (the lock screen and her words);
    - after the lesson.
- [ ] **Step 2:** Run it. Look at every pair. List each visible difference in placement, size, colour, type, timing or blur, then fix each one. After each fix, rerun the PlayMode test that covers that screen and the shots. Expected: no differences beyond font rendering and the Inter-for-system-font swap.
- [ ] **Step 3:** Update the docs.
  - **README:** the Unity version's status; how to test (`tools/unity.sh test editmode|playmode`, `npm test`) and build (`tools/unity.sh build`, then `node tools/webgl-smoke.mjs`); and that batch mode needs the Editor closed.
  - **itch.io upload notes:**
    - Upload `Builds/LATER-webgl.zip` as an HTML project, with "This file will be played in the browser" ticked.
    - Set the viewport to 960×540, turn on mobile-friendly and landscape, and turn off itch's fullscreen button (the game has its own).
    - Reminder: fill in the AI generation disclosure.
- [ ] **Step 4:** Run every check one last time: `npm test`, both Unity suites, the build and the smoke test. Commit: "Unity: side-by-side check against the web version; docs and itch.io notes".
