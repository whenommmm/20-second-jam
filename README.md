# 20 Second Game Jam 2026: project handover

This README is the starting point for any new chat or agent working on the user's jam games. It holds everything decided so far.

**Read it first, then read `docs/missed-calls-design.md` before working on LATER.**

Last updated: 2026-11-13. This copy, in the Unity project at `~/Unity/20-Second-Jam/`, is the current one. The older copy in `~/20-Second-Jam/jam/` was written before this project existed and is out of date.

---

## 1. Where things stand

- **Three games are planned, in this build order:** LATER (was "Missed Calls"), then Keep the Beat, then Unsent.
- **LATER:**
  - Fully designed in `docs/missed-calls-design.md`, rewritten on 2026-10-30 after the user played and refined a browser mockup.
  - The playable reference is `docs/reference/later-mockup.html`. Build "exactly like the showcase": that's the user's instruction.
  - **The Unity version is the jam entry.** A web version (`web/later/`) was built first from the mockup and is the reference the Unity version is checked against.
  - **The web version is finished** (`docs/superpowers/plans/2026-10-30-later-web.md`).
  - **The Unity version is built** on the branch `later-unity` (`docs/superpowers/plans/2026-10-30-later-unity.md`), and on 2026-11-12 was made to match the web build in every detail a player can see, hear or do (`docs/superpowers/plans/2026-11-12-later-unity-parity.md`): its WebGL zip is 9.2 MB and it passes every check (section 7). Not yet merged to `main` or uploaded to itch.io: both wait for the user.
  - **The phone's font is Inter in both versions** (the user's choice, 2026-11-11), so they look the same on every device.
- **Keep the Beat and Unsent:** only the idea exists (section 4). No design yet.
- **Unity project:** this folder. LATER lives in `Assets/Later/`.
- **Git:** a private GitHub repo, `github.com/whenommmm/20-second-jam`. Work happens on branches; `main` holds reviewed work.

**Next steps:**
1. The user plays the Unity build, then merges `later-unity` and uploads it to itch.io (section 7, "Uploading to itch.io").
2. Design and build Keep the Beat and Unsent.

---

## 2. The jam

- **Page:** https://itch.io/jam/20-second-game-jam-2026
- **Tagline:** "The long jam for short games".
- **Hosts:** Akzidenz, xenobrain and marc.
- **Dates:**
  - The jam runs **Oct 23 to Nov 16, 2026**.
  - Submissions close **Nov 17, 2026 at 00:59:59**. The timezone isn't stated, so plan for UTC.
- **Judging:** None. There are no judges, rankings or prizes, so it's a fun jam. About 1,100 people had joined by Oct 24.
- **Theme:** Optional. The suggested themes are "The End of the World is Nice", "Double or Quits", "You Are What You Eat" and "Out of Order". Our games don't follow any of them.
- **Sponsor:** DragonRuby offers a free web-only license. We aren't using it.

### Rules that affect every game
- **20 seconds or less**, start to finish. Title screens, menus and cutscenes don't count, but keep them short or skippable.
- **Plays in the browser, free, and hosted on itch.io itself**, not linked from another site. A broken web build is disqualified by default.
- **Each playthrough stands alone.** No items, health or upgrades carry over. Multiple 20-second levels are allowed. Longer modes are allowed only if the 20-second version is the default.
- **Multiple entries are allowed**, but low-effort entries can be removed. That's why we're making three polished games, not three rushed ones.
- **Generative AI use must be declared** in the "AI generation disclosure" field of each project page. The hosts prefer human-made games ("We want to see games made by our fellow humans rather than AI"). Claude is writing code for these games, so **remind the user to fill in the disclosure** when submitting.
- **Warnings:** Mature content needs a warning in the page description, the Audience & Content settings and the game itself. Flashing or seizure-triggering effects must also be mentioned. (The user decided LATER gets no content warning.)
- **Engines:** Any engine is fine if it exports to the web. Unreal 5 doesn't.
- **Social:** Posts can use the #20SecondGameJam tag.

---

## 3. What the user wants, and why

The user wants players to **feel something real**, like Arthur Morgan's death in Red Dead Redemption 2 or Ezio watching his father and brothers executed in Assassin's Creed II. Those moments rely on dozens of hours of attachment that 20 seconds can't build.

Four ways a short game can still hit hard:
1. **The player makes something, then loses it.** People get attached to anything they made themselves.
2. **The timer is the story.** Everyone knows the game ends at 20 s.
3. **Use feelings players already have.** Parents, pets, a last phone call.
4. **A twist in the last few seconds.** The ending shows what the player was really doing.

**The user picked approach 4 for all three games.**
- Each game looks like an ordinary, fun arcade game, and the ending shows what the player was actually doing.
- The key principle: the reveal must change **what the player did**, not just show a sad picture. The skill they were proud of turns out to be the tragedy.
- All three are about someone you didn't make time for or couldn't keep, so they work as a loose **trilogy** and can share an art style and some code.

**Pitched but not chosen** (don't pitch these again unless the user asks):
- **Ready or Not:** Hide-and-seek, counting to 20, and nobody ever comes to find you.
- **Let Me Win:** Pong where Dad lets you win.
- **Last Wave:** A shooter where the ships turn out to be refugee escape pods.

---

## 4. The three games

### Game 1: LATER (fully designed, being built)
- **What the player thinks it is:** an easy, bright dodge game. Green pills slide in along lanes; you weave your little black moon (named LATER) through them for points and streaks.
- **What it really is:** a month of calls from your family (Mom, Dad, your sister Jessie, an aunt, your brother John) while your mom is dying. Every pill you dodged was you saying "later". The paper behind everything goes colder with each call you let ring out.
- **The reveal:** after "TIME!" the results screen shrinks into a phone. Your moon becomes its Do Not Disturb pill ("☾ Later · Notifications silenced") and your red badge becomes "N Missed Calls". You can open a month of their texts.
- **Three endings:** missed her last call (John: "Funeral's on Saturday." and her voicemail), answered it after keeping in touch ("Thank you for coming home"), or answered only her last call ("She told everyone you called").
- **TURN BACK TIME** never restarts the game. It answers, for example "YOU SAID LATER 24 TIMES. / She would have taken any one of them." A quiet PLAY AGAIN follows.
- Everything else, including every number and line of text, is in **`docs/missed-calls-design.md`**.

### Game 2: Keep the Beat (idea only)
- **What the player thinks it is:** A rhythm game. Tap in time with the pulse and build your streak.
- **The reveal:** The beat starts slipping however well you tap. A monitor beeps flat, and the screen says *"you can stop now."*
- It needs a design conversation with the user before any building.

### Game 3: Unsent (idea only)
- **What the player thinks it is:** A typing speed test. Type each word as it appears, as fast as you can.
- **The reveal:** The words you typed join into a message, and it was never sent.
- It needs a design conversation with the user before any building.

---

## 5. LATER: decisions

All of LATER's decisions live in `docs/missed-calls-design.md` (with the user's choices marked as theirs and Claude's as "my call"). Don't duplicate them here. If the README and the doc ever disagree, **the doc wins**.

---

## 6. Open questions for the user

1. **Music for LATER.** The user wants it sad and will decide later. Sad music during play would hint at the twist. Who makes it is open (the jam prefers human-made).
2. **A recorded voice** for Mom's voicemail and her last call, to play after the timer (never during play). Right now they're text only.

---

## 7. Tools, setup and what was checked (as of 2026-10-30)

### The Unity project (this folder)
- **Path:** `~/Unity/20-Second-Jam/`.
- **Editor:** Unity **6000.3.2f1**, with the WebGL module installed. Other installed versions are 6000.1.14f1 and 6000.6.0f1. Keep 6000.3.2f1.
- **Render pipeline:** the built-in one. The project started from Unity's URP template, but LATER is all UI Toolkit, and URP alone put the WebGL zip over 10 MB, so URP and the template's leftovers are gone. Colour space is **Gamma**, so see-through things blend as they do in a browser.
- **Packages:** notable ones are `com.unity.inputsystem` 1.17.0, `com.unity.ugui` 2.0.0, `com.unity.test-framework` 1.6.0 and **`com.unity.pipeline` 0.9.1-exp.1**, which lets the CLI and MCP control the editor.
- **One project or three?** The three games could live here as separate scenes, each with its own WebGL build. That's a suggestion, not a decision: confirm with the user before adding game 2.

### The Unity version of LATER (`Assets/Later/`)
- **A faithful port of `web/later/`**, file by file: `Rules.cs` ← `rules.js`, `Round.cs` ← `round.js`, `PlayView.cs` ← `view.js`, `Ending.cs` ← `ending.js`, `LaterGame.cs` ← `main.js`, `Later.uxml`/`Later.uss` ← `index.html`/`style.css`. Everything on screen is UI Toolkit.
- **Every number and line of text comes from `web/later/src/data.js`**, through the generated `Assets/Later/Scripts/Core/LaterData.g.cs`. Never edit that file: change `data.js`, then run `npm run export-unity` in `web/later/`. `npm test` fails if the two drift apart.
- **Fonts:** the web version's Anton and Archivo Black; Newsreader Italic instanced at the optical sizes the browser picks for 19 and 21 px (the web's font is variable); and Inter (Regular, SemiBold, Bold) for the phone. The web version bundles the same Inter files (kerning only), so the phone looks the same in both, on every device.
- **Sounds** are made at startup by `Core/Synth.cs`, the web version's synthesis sample by sample, at the browser's own audio rate. No audio files ship.
- **The page around the game** (`Assets/WebGLTemplates/Later/`): `later.js` does what `main.js` does with the browser itself (keys by the browser's key names, FULLSCREEN as a real button, Tab focus, the safe area, pill widths measured in the page's fonts, audio suspended in a hidden tab), and the page draws ROTATE YOUR DEVICE itself, as the web's CSS does. The game reaches it through `Plugins/WebGL/Later.jslib`.
- **Matching the web exactly** was checked side by side in the browser (2026-11-12). What can't match: Unity draws text with SDF fonts rather than the browser's rasteriser (slightly lighter at 9–11 px), it's a 9 MB download with a loading bar where the web starts at once, and screen readers, find-in-page and auto-translate can't read a canvas.
- **Close the Unity Editor before testing or building:** everything runs in batch mode, which can't open a project the Editor has open. `tools/unity.sh` says so if it is.
- **Checks** (run from the project folder):
  - `tools/unity.sh test editmode`: the rules, the round, and replays of rounds recorded from the JavaScript simulation (same events, same positions), fonts, icons, sounds.
  - `tools/unity.sh test playmode`: the game itself, from the title screen through all three endings, restarts and the wide-phone layout.
  - `cd web/later && npm test`: the web version's checks, and that the Unity data still matches `data.js`.
- **Build:** `tools/unity.sh build` makes `Builds/LATER-webgl/` and `Builds/LATER-webgl.zip` (it fails if the zip is over 10 MB). Then `node tools/webgl-smoke.mjs` plays the build in headless Chrome, served the way itch.io serves it. It uses software WebGL, which is slow (a few minutes); `LATER_GPU=1` uses the real GPU.
- **Side by side with the web version:** `node tools/compare-shots.mjs` writes screenshots of both at the same moments to `Builds/shots/` (`*-pair.png`: web above, Unity below). Note its web side runs in the web's test mode, which turns CSS transitions off, so compare still moments with it, not motion.

### Uploading to itch.io
- Upload `Builds/LATER-webgl.zip` as an **HTML** project and tick **"This file will be played in the browser"**.
- **Embed options:** viewport **960 × 540**; tick **Mobile friendly**, orientation **Landscape**; leave itch's fullscreen button off (the game has its own).
- **Fill in the "AI generation disclosure"** on the project page (Claude wrote the code).
- After uploading, play it once on itch.io on a desktop and on a phone held sideways.

### The web version of LATER (`web/later/`)
- Plain HTML/CSS/JavaScript modules, no build step, no dependencies. Needs Node (26 is installed) for tests and Google Chrome for the headless checks.
- From `web/later/`: `npm test` (logic tests), `npm run smoke` (headless-browser checks), `npm run package` (makes `dist/later-web.zip` for itch.io).
- **Live at https://later-alpha.vercel.app** (Vercel, public). Redeploy with `npm run deploy` from `web/later/` (the Vercel CLI is installed and logged in on this Mac). The user can't open claude.ai Artifacts, so don't publish there.

### Git and GitHub
- Repo: `github.com/whenommmm/20-second-jam` (private), logged in through the GitHub CLI as `whenommmm`.
- Plain `git push` needs a credential helper on this machine. Either push with `git -c credential.helper= -c 'credential.helper=!gh auth git-credential' push`, or have the user run `gh auth setup-git` once (it changes global git settings, so ask first).
- `.gitignore` leaves out Unity's generated folders, IDE project files, `.superpowers/` (brainstorming and plan-execution scratch) and `web/later/dist/`.

### Controlling the editor
- **Checked on 2026-10-29 and 2026-10-30:** the editor was open on this project, `unity pipeline list` showed it reachable (Pipeline server on port 7800), and the `unity-editor-mcp` tools could read the live scene. So the editor **can be controlled**.
- **In a new chat, check again before relying on it:** run `unity pipeline list`, and confirm the Unity MCP tools are present. If they aren't, say so honestly, and don't pretend to have editor control.
- **Unity CLI:** `~/.unity/bin/unity`, version 1.0.0-beta.12. A newer version, **1.0.0-beta.17**, is available. Ask the user before running `unity self-update`. The CLI can make headless builds (`unity build`), but not while this project is open in the editor.
- **Other MCP servers:** `unityMCP` (the "MCP for Unity" package's HTTP server on `127.0.0.1:8080`) belongs to the Handshake project and isn't needed here. `blender` isn't relevant.

### WebGL for itch.io (Unity version)
- itch.io doesn't send compression headers, so the build uses **gzip with "Decompression Fallback" on** (`Editor/LaterBuild.cs` sets this and the rest: IL2CPP optimised for size, high managed stripping, no splash).

### Audio
- Browsers block audio until the player clicks or taps, so every game starts with a click-to-start screen.
- Sounds are generated in code (Web Audio on the web; in Unity, rendered sample by sample at startup into PCM clips at the browser's own rate).
- Unity's audio filter components don't work on WebGL.

---

## 8. About the user and how they like to work

- **They want momentum.** Keep questions few and high-value. Make sensible calls, **state them plainly**, and let them correct you.
- **They decide by playing.** Interactive browser mockups worked far better than descriptions: they tune feel, colour and wording by trying it and reacting in short messages, often several in a row.
- **Subtle over spelled out.** Hints should be vague ("wait… that looks like a call… nah"). They pushed back whenever a line or visual gave too much away or felt "basic", and they want lines with real emotion.
- **Fairness matters.** No forced hits; first-time players should be able to dodge everything. They reject limiters, visible or invisible.
- **They hate UI that "looks AI-made":** purple or indigo dark themes, glows, gradients, dot-grid backgrounds, glossy 3D keycap blocks and bouncy hover animations. They also rejected "hand-drawn doodles" as an overused suggestion. Flat colours, chunky type for big moments, restraint.
- **Code:** Claude is writing the code for this jam. They sometimes use Codex from very explicit specs elsewhere.
- **Other deadline:** a separate project, **Split Keys / Risky Picks** (their Handshake "Create a Multiplayer Game" entry) lives in `~/Unity/Handshake`, is due **Oct 31, 2026**, and is deployed at https://split-keys.onrender.com. It is **not** part of this jam. Don't touch it from here. Its old specs are in `~/20-Second-Jam/handshake/`.

---

## 9. Folder layout

```
~/Unity/20-Second-Jam/              Unity project + jam repo: work here
├── README.md                       this file: start here
├── .gitignore
├── docs/
│   ├── missed-calls-design.md      LATER's full design (the reference)
│   ├── reference/later-mockup.html the playable mockup LATER was designed from
│   └── superpowers/plans/          implementation plans
├── web/later/                      the web version of LATER
├── Assets/Later/                   the Unity version of LATER (scripts, UI, fonts, sounds, tests)
├── Assets/WebGLTemplates/Later/    the page around the WebGL build
├── tools/                          unity.sh (tests, build), webgl-smoke.mjs, compare-shots.mjs
├── Packages/  ProjectSettings/     Unity project files
└── Library/  Temp/  Logs/  Builds/ generated, not in git

~/20-Second-Jam/                    planning folder from before the project existed
├── jam/                            older copy of this README and the design doc (stale)
└── handshake/                      old Split Keys specs: not jam work, ignore
```

## Schedule

- Submissions close **Nov 17** (plan for UTC).
- LATER: web version done Nov 1; Unity jam entry built Nov 10, waiting for the user to play it and upload it.
- Then Keep the Beat and Unsent in the remaining ~10 days.
