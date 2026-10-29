# 20 Second Game Jam 2026: project handover

This README is the starting point for any new chat or agent working on the user's jam games. It holds everything decided so far.

**Read it first, then read `docs/missed-calls-design.md` before building Missed Calls.**

Last updated: 2026-10-29. This copy, in the Unity project at `~/Unity/20-Second-Jam/`, is the current one. The older copy in `~/20-Second-Jam/jam/` was written before this project existed.

---

## 1. Where things stand

- **Three games are planned, in this build order:** Missed Calls, then Keep the Beat, then Unsent.
- **Missed Calls:**
  - Fully designed. The design is in `docs/missed-calls-design.md`.
  - The user approved it in chat and added one idea of their own (the blurry name).
  - A reviewer read it, and the gaps it found are settled in the doc.
  - **Three open questions are still waiting for the user** (section 6).
- **Keep the Beat and Unsent:** Only the idea exists (section 4). No design yet.
- **Unity project:** It exists. This folder, `~/Unity/20-Second-Jam/`, was created on 2026-10-29 from Unity's URP template. Only the template's sample scene is in it.
- **Nothing has been built yet.**

**Next steps:**
1. Get answers to the three open questions, or use the defaults in section 6.
2. Check that the editor connection still works (section 7).
3. Set the project up for a flat 2D game (section 7).
4. Write an implementation plan from the design doc, then build.

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
- **Warnings:** Mature content needs a warning in the page description, the Audience & Content settings and the game itself. Flashing or seizure-triggering effects must also be mentioned.
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

### Game 1: Missed Calls (fully designed, build first)
- **What the player thinks it is:** A bright dodge game. Green circles fly in, and you weave through them for points and combos.
- **The reveal:** The circles were phone calls from Mom, and her saved name ("Mom") was on every circle the whole time.
  - The name starts blurred and slowly sharpens. **The user added this.**
  - After the timer, the arcade score screen turns into a phone lock screen stacked with "Mom · Missed call" notifications and a voicemail: *"Hi, it's just me. No reason. Love you."*
- **Getting hit means answering the call.** You lose points and hear a muffled "Hi beta—".
- **Endings depend on how many calls you answered.** Answering all 14 gives a warm ending: *"Mom · Same time tomorrow?"*
- The full spec is in **`docs/missed-calls-design.md`**. Build from that document; this section is only a summary.

### Game 2: Keep the Beat (idea only)
- **What the player thinks it is:** A rhythm game. Tap in time with the pulse and build your streak.
- **The reveal:** The beat starts slipping however well you tap. A monitor beeps flat, and the screen says *"you can stop now."*
- It needs a design conversation with the user before any building.

### Game 3: Unsent (idea only)
- **What the player thinks it is:** A typing speed test. Type each word as it appears, as fast as you can.
- **The reveal:** The words you typed join into a message, and it was never sent.
- It needs a design conversation with the user before any building.

---

## 5. Missed Calls: key decisions at a glance

All of these are in the design doc too. If this list and the doc disagree, **the doc wins**.

- **Length:** Exactly 20.0 s of play, from the first frame to "TIME!". The click-to-start title screen and the reveal don't count.
- **Calls:** There are 14 calls. Each call is a short line of "ring" circles, 42 circles in total.
  - Ring counts per call: 2, 2, 3, 3, 4, 4, 5, 5, 4, 3, 3, 2, 1, 1.
  - Circles aim at where the player was when they spawned.
- **Timeline:**
  - 0–2 s: The first call comes in slowly.
  - 2–12 s: Calls speed up and come more often.
  - 12–16 s: Calls thin out. The last call is one slow ring.
  - About 16–20 s: Nothing comes. The date in the corner keeps moving.
  - All calls are off screen by 17 s.
- **Name blur:** "Mom" is shown on every circle. It's unreadable at first and sharpens with game time, roughly clarity = (t / 15 s)². A sharp-eyed player can just read it at about 11–12 s, and it's fully sharp on the last call.
- **Date in the corner:** It sits where a level number would usually be (MAR 3, MAR 9…). It jumps forward when each call spawns, covering about a year, and keeps moving month by month during the quiet seconds.
- **Scoring:**
  - +10 for each dodged ring, and +25 with "CLOSE!" for a close dodge.
  - A call is "missed" when all of its rings are dodged. The "PERFECT ×n" combo counts missed calls in a row.
  - A hit is −100 with a screen shake, the combo resets, and the call counts as answered.
- **Muffled voice:** "Hi beta—" plays muffled and full of static at first, then clears on the same curve as the name blur.
  - The user records one clean clip, and the muffled versions are made from it offline, because Unity's WebGL build has no audio filters.
  - Until then the placeholder is a non-voice sound. **Never** show "Hi beta—" as text.
- **Difficulty:** A first-time player should answer about 1–3 calls. Zero hits is rare and earns "PERFECT!".
- **The reveal:**
  1. "TIME!" and a freeze of about 0.8 s.
  2. An arcade score screen for about 2.5 s.
  3. The score panel turns into a phone lock screen. The header **"14 Missed Calls"** (or however many were missed) is where the title is revealed.
  4. The missed-call notifications appear newest first, with the voicemail on top only if call #14 was missed.
  5. "Play again" appears after about 3 s.
- **Look:** Off-white background, an ink-black player, phone-answer green circles (≈ #34C759) and one accent color. Flat, with no glow or gradients. The lock screen looks like a plain, real phone, not a cartoon.
- **Never give the twist away early.** Nothing may look like a phone during play. Sound stays upbeat until "TIME!", and the name isn't readable in the first half.
- **Platforms:** Desktop browsers and landscape touch devices. Phones held upright get a "rotate your device" message, and the itch page says "best on desktop".
- **Controls:** Mouse or touch follows the pointer, and arrow keys or WASD work too.
- **Schedule:** About 6 days, finished by **Nov 4**, which leaves time for games 2 and 3. Write the title screen, 20-second timer and WebGL settings so they can be copied into the next games. Don't build a shared framework ahead of time.

---

## 6. Open questions for the user (ask these first)

1. **Is Mom gone?** During the quiet seconds the corner date moves forward by months.
   - If the lock screen shows that **later** date, the notifications are months old and the ending says she's gone.
   - If it shows the **date of the last call**, it reads as "you just missed it."
   - The user's references (Arthur, Ezio) point toward "gone", but it's their decision.
2. **What's the public title?** "Missed Calls" on the itch page or in the browser tab gives the twist away before anyone plays.
   - Suggested: **DODGE!**, or something like "Busy", with "Missed Calls" kept for the reveal.
   - This also sets Unity's product name, which shows in the browser tab and the loading screen.
3. **Music:** Is there upbeat music during play, and who makes it?
   - The jam prefers human-made work.
   - The fallback is sound effects only, with silence when "TIME!" hits.

If the user wants to just build, use the defaults: lock screen shows the later date, public title "DODGE!", and sound effects only. Mark them in the doc as defaults.

---

## 7. Tools, setup and what was checked (as of 2026-10-29)

### The Unity project (this folder)
- **Path:** `~/Unity/20-Second-Jam/`.
- **Editor:** Unity **6000.3.2f1**, with the WebGL module installed. Other installed versions are 6000.1.14f1 and 6000.6.0f1. Keep 6000.3.2f1.
- **Template:** Unity's **URP template**. It came with:
  - PC and Mobile URP renderers
  - a volume profile
  - the new Input System, with `InputSystem_Actions`
  - a TutorialInfo folder and a Readme asset
  - `Scenes/SampleScene.unity`
- **Packages:** Notable ones are `com.unity.render-pipelines.universal` 17.3.0, `com.unity.inputsystem` 1.17.0, `com.unity.ugui` 2.0.0 (which includes TextMeshPro) and **`com.unity.pipeline` 0.9.1-exp.1**. The Pipeline package is what lets the CLI and MCP control the editor.
- **Setting it up for Missed Calls, a flat 2D game:**
  - Use an orthographic camera.
  - Turn off post-processing effects you don't need.
  - Remove the TutorialInfo folder and the Readme asset.
  - Watch the WebGL build size. URP adds weight, and the target is under 10 MB.
- **One project or three?** The project is named for the whole jam, so the three games could live here as separate scenes, each with its own WebGL build. That's a suggestion, not a decision, so confirm with the user before adding game 2.

### Controlling the editor
- **Checked on 2026-10-29:**
  - The editor was open on this project.
  - `unity pipeline list` showed it **reachable** (Pipeline server on port 7800).
  - The `unity-editor-mcp` tools (create, find, screenshot, eval, game_view, build settings and more) had loaded in the chat.
  - So the editor **can be controlled**.
- **In a new chat, check again before relying on it:** run `unity pipeline list`, and confirm the Unity MCP tools are present. If they aren't, say so honestly, and don't pretend to have editor control.
- **Unity CLI:** `~/.unity/bin/unity`, version 1.0.0-beta.12.
  - A newer version, **1.0.0-beta.14**, is available. Ask the user before running `unity self-update`.
  - The CLI can make headless builds (`unity build`), but not while this project is open in the editor.
- **Other MCP servers:**
  - `unityMCP`, the "MCP for Unity" package's HTTP server on `127.0.0.1:8080`, is a separate tool used on the Handshake project. That package is **not** installed in this project, and it isn't needed because `unity-editor-mcp` and the Pipeline package cover it.
  - `blender` isn't relevant here.

### WebGL for itch.io
- itch.io doesn't send compression headers. Use **gzip with "Decompression Fallback" on**, or no compression.
- Keep the build small (target under 10 MB) for a fast load.
- Size-saving settings: IL2CPP set to optimize for size, high managed stripping, minimal packages.
- Upload the build as a zip to itch.io.

### Audio
- Browsers block audio until the player clicks, so the click-to-start title screen is required.
- Placeholder sounds can be generated in code with `AudioClip.Create` and `SetData`, which works on WebGL.
- Unity's audio filter components don't work on WebGL.

---

## 8. About the user and how they like to work

- **They want momentum.** Twice they skipped detailed design questions and asked to start building. Keep questions few and high-value. Make sensible calls, **state them plainly**, and let them correct you.
- **They hate UI that "looks AI-made":** purple or indigo dark themes, glows, gradients, dot-grid backgrounds, glossy 3D keycap blocks and bouncy hover animations. They also rejected "hand-drawn doodles" as an overused suggestion.
  - Study real game UIs (interfaceingame.com, gameuidatabase.com) and borrow their principles.
  - Flat colors, chunky type for big moments, and restraint.
- **Code:** They sometimes have Codex (OpenAI's coding agent) write code from very explicit specs. For this jam, Claude is writing the code. Ask if that changes.
- **Other deadline:** They have a separate project with a closer deadline: **Split Keys / Risky Picks**, their Handshake "Create a Multiplayer Game" entry.
  - It lives in `~/Unity/Handshake`, is due **Oct 31, 2026**, and is deployed at https://split-keys.onrender.com.
  - It is **not** part of this jam. Don't touch it from here.
  - Its old specs are in `~/20-Second-Jam/handshake/`.

---

## 9. Folder layout

```
~/Unity/20-Second-Jam/              Unity project: work here
├── README.md                       this file: start here
├── docs/
│   └── missed-calls-design.md      full Missed Calls spec (the reference)
├── Assets/                         template content so far (SampleScene, Settings, TutorialInfo)
├── Packages/  ProjectSettings/     Unity project files
└── Library/  Temp/  Logs/          generated by Unity, don't edit

~/20-Second-Jam/                    planning folder from before the project existed
├── jam/                            older copy of this README and the design doc
└── handshake/                      old Split Keys specs: not jam work, ignore
```
