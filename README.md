# 20 Second Game Jam 2026: project handover

This README is the starting point for any new chat or agent working on the user's jam games. It holds everything decided so far.

**Read it first, then read `docs/missed-calls-design.md` before working on LATER.**

Last updated: 2026-10-30. This copy, in the Unity project at `~/Unity/20-Second-Jam/`, is the current one. The older copy in `~/20-Second-Jam/jam/` was written before this project existed and is out of date.

---

## 1. Where things stand

- **Three games are planned, in this build order:** LATER (was "Missed Calls"), then Keep the Beat, then Unsent.
- **LATER:**
  - Fully designed in `docs/missed-calls-design.md`, rewritten on 2026-10-30 after the user played and refined a browser mockup.
  - The playable reference is `docs/reference/later-mockup.html`. Build "exactly like the showcase": that's the user's instruction.
  - **The Unity version is the jam entry.** A web version (`web/later/`) is built first from the mockup and becomes the reference the Unity version is checked against.
  - The web build follows `docs/superpowers/plans/2026-10-30-later-web.md`. A separate plan for the Unity version comes after it.
- **Keep the Beat and Unsent:** only the idea exists (section 4). No design yet.
- **Unity project:** this folder, created on 2026-10-29 from Unity's URP template. Nothing game-specific is in it yet.
- **Git:** a private GitHub repo, `github.com/whenommmm/20-second-jam`. Work happens on branches; `main` holds reviewed work.

**Next steps:**
1. Finish the LATER web version (the plan above).
2. Write and carry out the Unity plan for LATER (the jam entry).
3. Design and build Keep the Beat and Unsent.

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
- **Template:** Unity's **URP template** (PC and Mobile URP renderers, a volume profile, the new Input System with `InputSystem_Actions`, a TutorialInfo folder, a Readme asset, `Scenes/SampleScene.unity`).
- **Packages:** notable ones are `com.unity.render-pipelines.universal` 17.3.0, `com.unity.inputsystem` 1.17.0, `com.unity.ugui` 2.0.0 (includes TextMeshPro) and **`com.unity.pipeline` 0.9.1-exp.1**, which lets the CLI and MCP control the editor.
- **Setting it up for LATER, a flat 2D game:** orthographic camera, no unneeded post-processing, remove TutorialInfo and the Readme asset, and keep the WebGL build under 10 MB.
- **One project or three?** The three games could live here as separate scenes, each with its own WebGL build. That's a suggestion, not a decision: confirm with the user before adding game 2.

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
- **Unity CLI:** `~/.unity/bin/unity`, version 1.0.0-beta.12. A newer version, **1.0.0-beta.14**, is available. Ask the user before running `unity self-update`. The CLI can make headless builds (`unity build`), but not while this project is open in the editor.
- **Other MCP servers:** `unityMCP` (the "MCP for Unity" package's HTTP server on `127.0.0.1:8080`) belongs to the Handshake project and isn't needed here. `blender` isn't relevant.

### WebGL for itch.io (Unity version)
- itch.io doesn't send compression headers. Use **gzip with "Decompression Fallback" on**, or no compression.
- Keep the build small (target under 10 MB): IL2CPP optimised for size, high managed stripping, minimal packages.
- Upload the build as a zip to itch.io.

### Audio
- Browsers block audio until the player clicks or taps, so every game starts with a click-to-start screen.
- Placeholder sounds can be generated in code (Web Audio on the web; `AudioClip.Create` and `SetData` in Unity, which works on WebGL).
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
├── Assets/                         Unity content (template content so far)
├── Packages/  ProjectSettings/     Unity project files
└── Library/  Temp/  Logs/          generated by Unity, not in git

~/20-Second-Jam/                    planning folder from before the project existed
├── jam/                            older copy of this README and the design doc (stale)
└── handshake/                      old Split Keys specs: not jam work, ignore
```

## Schedule

- Submissions close **Nov 17** (plan for UTC).
- LATER: web version by about **Nov 1**, Unity jam entry by about **Nov 6**.
- Then Keep the Beat and Unsent in the remaining ~10 days.
