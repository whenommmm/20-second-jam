# LATER: design

Game 1 of 3 for the 20 Second Game Jam 2026. The working name was "Missed Calls". The jam rules and the other two games are in `../README.md`.

**Reference build.** Everything below was worked out by playing a browser mockup with the user. That mockup is the reference: when this document and the mockup disagree on a number or a line of text, check the mockup, and when they disagree on intent, this document wins.
- Mockup: `docs/reference/later-mockup.html`. It is a page fragment written for a brainstorming viewer, so a few colours come from that viewer's stylesheet, but its game code runs on its own.
- All tuning numbers in this document are the mockup's current values. They were tuned by feel and can still change.

Last updated: 2026-10-30.

---

## Intent

The player should feel something real in 20 seconds, the way Arthur Morgan's death or Ezio's family's execution felt. A 20-second game can't build that attachment through story, so this one uses a twist at the end.

For 20 seconds the player enjoys a bright, easy arcade dodge game. At the end, a phone lock screen shows what they were dodging: a whole month of calls from their family while their mom was dying, and her last calls. The skill they were proud of was ignoring everyone.

The emotion comes from the player's own actions. The reveal changes what **they did**; it doesn't just show a sad picture.

## Goals

- It plays as a real, fun, easy dodge game, so a first-time player tries to dodge everything.
- The truth leaks out slowly and only partly before the end: blurred names, a phone-ish look to the projectiles, a background that goes cold.
- The ending lands within a few seconds of the timer and differs depending on what the player did.
- Players who replay knowing the truth can choose to answer calls and get a different ending.

## Non-goals

- No lives, game over or difficulty settings.
- No levels, upgrades or progress that carries over between runs (the jam forbids carry-over).
- No endless mode, no settings menu beyond what's needed to start.
- **No voices during play.** Touching a call never plays speech.

## Anti-goals (these ruin it even if everything else works)

- **Confirming the twist early.** The projectiles may hint at phone calls ("wait… that looks like a call… nah"), but nothing during play confirms it. Names stay unreadable for most of the round.
- **Unfair hits.** A player who wants to dodge must always be able to. A forced touch breaks the whole premise. (See "Fairness".)
- **The "AI look".** No purple or indigo dark themes, glows, gradients, dot-grid backgrounds or bouncy hover effects. Flat colours, few of them, held back.
- **Playtime over 20 seconds**, measured from the first frame of play to "TIME!".
- **A slow or broken web build.** A long load spoils a 20-second game, and a broken build is disqualified.

---

## Constraints

- **Two versions, same game:** a **web version** (HTML/JavaScript, built from the mockup) and a **Unity version** (Unity 6000.3.2f1 WebGL, in this project). Both must play the same way and show the same text.
  - **The Unity version is the jam entry** (the user's decision).
  - The web version is built first (my call): it's closest to the mockup, so it's quick, and it becomes the finished reference the Unity version is checked against. Where the web version is published is up to the user later; it is not the jam entry.
- **No content warning**, in the game or on the page (the user's decision).
- Plays in the browser on itch.io. The game area is 960×540 (16:9) and scales to fit the frame. Include a fullscreen button.
- Browsers block audio until the player interacts, so the game starts with a click-to-start screen. It doesn't count toward the 20 seconds.
- Controls: mouse, touch, and keyboard (arrow keys or WASD) all move the player.
- Platforms (my call, unchanged): desktop browsers and touch devices held landscape. Phones held portrait get a "rotate your device" message, and the itch.io page says "best on desktop".
- Keep the web build tiny and the Unity build under 10 MB.

---

## The story

Over one month (Mar 1 to the start of April), the player's mom is dying. All month her family keeps calling to tell the player how bad it's getting: Mom herself at first, then Dad, the player's sister Jessie, an aunt and the player's older brother John. The player never picks up. The calls are the green pills they've been dodging for points.

She dies around Mar 29. The quiet seconds at the end of play are the days after. The lock screen shows the date **Thursday, April 2**.

Callers, saved in the phone exactly like this (the user's choice):

| Saved as | Who |
|---|---|
| Mom | Mom |
| Dad | Dad |
| Jessie | Sister (texts make this clear) |
| Aunt | Aunt |
| John | Older brother |

---

## Title screen (not timed)

- Title **LATER** at the top centre, chunky and not too tall.
- **CLICK TO START** in the middle (the user called this "perfect": keep it).
- Nothing on the title screen hints at phones.
- The public title is **LATER**, everywhere: itch.io page, browser tab, loading screen.

## Play (exactly 20.0 seconds)

### The player
- A small **black crescent moon** with the word **LATER** under it, like a name tag.
  - During play it's just an arcade hero.
  - It's also the phone's Do Not Disturb icon, and "Later" turns out to be the name of the player's silent mode.
- A **red badge** on the moon counts **missed calls**: it goes up by 1 the moment each pill gets past the player (not when the pill leaves the screen, and not per group). At the end it lands on the phone's "N Missed Calls".
- Movement glides: the moon steers toward the pointer with a little inertia and leans into its motion. Keyboard moves it at a fixed speed. (Mockup: max speed 720 px/s, keys 460 px/s.)
- The moon can go anywhere on screen, including under the score and date. There is no visible or invisible limiter line (the user rejected one).

### The calls (pills)
- Each pill is a **green rounded capsule** (answer-button green, about `#34C759`) with a **thin black border** (2 px ink, so it holds up on the cold grey paper; the user's request), a round knob on the left and the caller's name on it. It looks a bit like something off a phone (a switch or a call banner) on purpose.
- Pills shake slightly, like a phone buzzing on a table.
- There are **14 calls** and **29 pills**. Each call is a short line of pills from one caller:

| # | Time | Caller | Pills | Speed (px/s) | Date |
|---|---|---|---|---|---|
| 1 | 0.4 s | Mom | 1 | 170 | Mar 1 |
| 2 | 1.6 | Mom | 2 | 190 | Mar 5 |
| 3 | 2.6 | Mom | 2 | 210 | Mar 9 |
| 4 | 3.5 | Dad | 2 | 230 | Mar 12 |
| 5 | 4.4 | Jessie | 3 | 250 | Mar 15 |
| 6 | 5.3 | Dad | 3 | 265 | Mar 17 |
| 7 | 6.2 | Aunt | 3 | 275 | Mar 19 |
| 8 | 7.1 | Jessie | 3 | 285 | Mar 21 |
| 9 | 8.0 | John | 3 | 290 | Mar 23 |
| 10 | 8.9 | Dad | 2 | 280 | Mar 24 |
| 11 | 9.8 | Jessie | 2 | 265 | Mar 25 |
| 12 | 10.7 | John | 1 | 250 | Mar 26 |
| 13 | 11.4 | Dad | 1 | 240 | Mar 27 |
| 14 | 12.4 | **Mom (last call)** | 1 | 280 | Mar 28 |

- **It must be easy.** The user wants every first-time player to try to dodge everything. Tune toward easier if in doubt.
- **Lanes (my call).** There are 8 horizontal lanes covering the full height (mockup: y = 54 + 62·i). Every call except Mom's last slides straight in from the right edge along one lane, like a notification sliding across. Pills in a call are a fixed gap apart (26 px between pills). The user asked for no weaving or swerving patterns.
- **Aimed at the player.** A call takes the free lane closest to the player when it starts, so standing still loses.
- **Pills never overlap.** A lane takes a new call only after the previous one has fully slid in, and only if the new one can't catch up with it.
- **Calls pass underneath the score and date**, so there's nowhere to hide.
- **Mom's last call (#14)** is the one exception. It enters at the player's height and steers toward the player at first. Then, while still well away (when it's past the middle of the screen or within 420 px of the player), it commits to a gentle straight line the player can dodge (its sideways speed halves when it commits). If the player stands still, it reaches them.
- All calls are off screen by 17 s. Seconds 17–20 are quiet.

### Fairness (no forced touches)
A player who wants to dodge must always be able to. The mockup enforces three rules:
1. **Time to react.** A call may only use the player's lane or a neighbouring lane if its first pill will take at least 1.1 s to reach the player. Otherwise it picks a lane further away.
2. **Never boxed in.** A call may only use the player's lane or a neighbour if at least one other lane right around the player stays clear from that moment until the whole call has gone past.
3. **Mom's last call waits** up to 0.6 s for a moment when rule 2 holds, then enters.

With these, a careful automated dodger finished every test round untouched. Any change to speeds, lanes or the schedule must keep this true.

### Touching a call = answering it
- −100 points, a short screen shake, the streak resets, and a dull thud (no voice).
- The rest of that call's pills disappear. The call counts as **answered**.
- If a pill has already passed and the player goes back and touches it, it switches from missed to answered (the badge goes down by 1).

### Names on the pills
- Every pill shows its caller's name, blurred.
- It sharpens with game time on a slow curve: clarity = (t / 16)^3.5. Unreadable through most of the round, readable around 14 s, sharp at 16 s.
- Each answered call adds 0.17 clarity on top, so about **5–6 answered calls** fully clear the names in the busy middle (the user's number).

### The date (top right)
- Where an arcade game shows a level, a date sits in a **black-bordered rectangle** in the same chunky type.
- Each new date is **put down by hand on top of the previous one**: a little crooked and offset, so a pile builds up over the month.
- A new date is stamped each time a call starts, then during the quiet seconds: Mar 29, Mar 30, Mar 31, Apr 1, Apr 2 (mockup times 14.6, 16.0, 17.4, 18.7, 19.6 s). The round starts on Feb 27.

### The paper is your month
- There is no countdown number. The date pile shows time passing.
- The background paper starts **vibrant** (warm apricot `rgb(253,220,172)`) and goes a step **colder with every pill the player lets go by**, toward a dark, despairing slate `rgb(146,158,172)`. The amount of cold is missed pills ÷ 29, eased so each step is visible but doesn't flicker.
- **Answering a call never cools it** (the user's rule: going cold only makes sense for missed calls). If a passed pill is answered after all, the paper warms back by that step.
- So a player who dodges everything ends on cold slate, and one who answers ends on warm paper. The results screen and the paper around the phone keep whatever colour the round ended on.
- The change must be clearly noticeable (the user asked twice). Date cards and the results screen use the same paper colour.

### Score (top left)
- Large chunky number. A pill that passes the player gives +10, or +25 with a red "CLOSE!" if it passed close.
- "PERFECT ×n" in red under the score counts pills dodged in a row.

### Sound (no voices, no music for now)
- Each pill makes a short phone-vibration buzz as it slides in (a line of three goes bzz, bzz, bzz).
- A small blip as each pill passes the player, one semitone higher for each pill in the streak.
- A brighter double blip on CLOSE!, and a dull thud on a touch.
- **At "TIME!" everything cuts to silence.** The results and the phone stay silent (the phone's notifications are silenced).
- Placeholders are generated in code (Web Audio in the web version). Real recorded sounds can replace them.

### End of play
At 20.0 s, "TIME!" appears and the game freezes for about 0.8 s.

---

## After the timer (not counted toward the 20 s)

### 1. Results screen (about 4 s)
- Full screen, poster style: "RESULTS", the score counting up in huge type, then three rows sliding in: **DODGED** (x / 29), **CLOSE CALLS** and **BEST STREAK**.
- The moon (with its badge) glides over to sit beside the score.
- A rank stamp in **black ink** (like the date cards) is slapped on: **PERFECT!** (no touches), **GREAT!** (1–2) or **NICE TRY** (3+). It isn't red: red on the cold paper looked wrong to the user, and ink matches the date cards.
- **Only after a perfect round**, a smaller black line fades in under the stamp a second later: **"…or is it?"**

### 2. The results screen shrinks into a phone
- The whole screen shrinks into a portrait phone in the middle and turns dark.
- The phone looks **plain and real**, not like a cartoon: camera notch, signal, 5G, a low battery, "Thursday, April 2", a big **7:12**, flashlight and camera buttons at the bottom, and a home bar.
- Three things fly from the game into the phone on smooth curves, dipping under the clock rather than across it:
  - **The moon** turns white as it crosses onto the screen and lands exactly as the silent-mode icon. A pill reading **☾ Later** grows out from behind it, with "Notifications silenced" underneath. A small moon also appears in the status bar.
  - **LATER** (its name tag) follows and fades into the word "Later" on the pill.
  - **The red badge** drops and lands on the missed-call count.

### 3. The lock screen
Notifications sit at the bottom, newest on top:
- **One Messages notification**: the newest text, stacked on the rest, with "N more messages". **Tapping it opens a Messages list** of every text from the month, newest first, scrollable, with "Show less" to close.
- **Mom's voicemail** (only if her last call was missed): a short preview card.
- **The missed-calls stack**: "N Missed Calls" (N = the badge number), with the callers' names. Hidden if nothing was missed.

The Messages list has one text per call (13 texts) plus the ending's final text. A call the player **answered** gives a thank-you text from that caller; a call the player **let ring out** gives what they were trying to say. Both endings show all of them (the user's call). The exact texts are in the appendix.

### 4. Her words, beside the phone
On the paper to the right of the phone, her words appear one line at a time (about 1.1 s per line), text only:
- If her last call was **missed**: header **VOICEMAIL · MOM · MAR 28**, then her voicemail.
- If her last call was **answered**: header **CALL · MOM · MAR 28 · 4:12**, then what she said on that call.
- **Double-tap (or double-click) anywhere** while her words are appearing shows all the remaining lines at once. TURN BACK TIME then follows as usual. This is the ending's only skip (the user's choice); a single tap does nothing.

### 5. The three endings

| Ending | When | Lock-screen message (top) | Beside the phone |
|---|---|---|---|
| **Too late** | Mom's last call missed | John · Apr 1: "Funeral's on Saturday." | Her voicemail |
| **Came home** | Mom's last call answered, and at least one other call answered | Dad · Mar 29: "Thank you for coming home, kiddo. She knew you were there." | Her last call |
| **Only her last call** | Mom's last call answered, no other call answered | Dad · Mar 29: "She told everyone you called. It was the last thing she talked about." | Her last call |

In every ending, she still dies. The better endings are about whether the player was there or reachable, not about saving her.

### 6. TURN BACK TIME
- About 1.2 s after her last line appears, a big **TURN BACK TIME** shows at the bottom right.
- Clicking it **does not restart**. It answers, in words that fit the ending: a loud line, then a beat later (about 1.8 s) a soft line in her captions' font.

| Ending | Loud line | Soft line |
|---|---|---|
| Too late | **YOU SAID LATER 24 TIMES.** (the real missed count; "ONCE" / "TWICE" for 1 / 2) | She would have taken any one of them. |
| Came home | **THERE'S NEVER ENOUGH TIME.** | But you were there. |
| Only her last call | **YOU GAVE HER FOUR MINUTES.** (her last call lasts 4:12) | She'd waited all month for them. |

- Clicking it again does nothing.

### 7. PLAY AGAIN
- A small, quiet **PLAY AGAIN** appears at the bottom left about 3 s after the soft line.
- Fallback: if TURN BACK TIME is never clicked, PLAY AGAIN appears 15 s after TURN BACK TIME showed.
- PLAY AGAIN starts a fresh round straight away (no title screen). Nothing carries over.

---

## Look

- **Palette (hidden in plain sight):** answer-button green for every call, decline-button red for things about the player saying no during play (badge, CLOSE!, streak), ink black `#141414`, and the paper that goes from vibrant to cold as calls are missed. During play it reads as a bright arcade palette; afterwards, as a phone's call screen. From the results screen onward, new elements use ink, not red.
- **Three voices in type:**
  - **The game** speaks in flat Swiss-poster type: huge condensed numbers and headings (Anton) and chunky labels and names (Archivo Black), with thick ink rules on the results screen.
  - **Mom** speaks in a warm italic serif (Newsreader Italic): her voicemail or call beside the phone, and the soft second line after TURN BACK TIME. The user found the poster type next to a plain sans-serif jarring; the serif contrasts with it on purpose.
  - **The phone** uses the system UI font and looks like a real phone.
- No glows, gradients or bouncy effects.
- Fonts are free Google Fonts. Both builds should bundle them rather than load them at runtime (my call), so the game works offline on itch and loads predictably.

---

## Left to the builder

- Code structure for each version, and how much code the two versions share (for example a shared data file for the schedule and texts).
- Exact easing curves, the screen-shake feel, the "close" distance, pill sizes and spacing, as long as the fairness rules hold and play still feels easy.
- The placeholder sounds' exact synthesis.
- How the Unity version builds the phone and the flights (UI Toolkit or uGUI, TextMeshPro).
- WebGL/itch.io settings for the Unity version: gzip with "Decompression Fallback" on, or no compression; size-optimised IL2CPP; high managed stripping.
- The fullscreen button and "rotate your device" message designs, within the look above.

## Open questions (for the user)

1. **Music.** The user wants it sad and will decide later. Sad music during play would hint at the twist, so it may belong only after "TIME!". Who makes it is also open (the jam prefers human-made).
2. **Recorded voice.** Her voicemail and her last call are text only right now. Should someone record them to play after the timer? (Never during play.)

Not open, just a reminder: the itch.io page's AI-generation disclosure must be filled in at submission.

## Schedule

- Submissions close **Nov 17, 2026 at 00:59:59** (plan for UTC). That's 18 days from Oct 30, and the user plans two more small games before then.
- My call: **web version by about Nov 1, Unity jam entry by about Nov 6**, leaving about 10 days for the other two games.

---

## Appendix: all the text

**Texts after each call.** Index = call number. Call 14 (Mom's last) has no text; it has the voicemail or the call instead.

| # | Caller | If answered | If missed |
|---|---|---|---|
| 1 | Mom | So good to hear your voice. Talk soon. | Did you eat? Don't skip dinner again. |
| 2 | Mom | Loved our chat. Eat something proper! | Are you well? Call when you're free, no rush. |
| 3 | Mom | Thanks for calling, sweetheart. Love you. | Doctor's appointment went fine. Don't worry. Love you. |
| 4 | Dad | Thanks for picking up. She was so happy. | Your mom's had a few rough days. Give her a call? |
| 5 | Jessie | Thank you for answering. Room 412, if you come. | Mom's back in the hospital. Can you call me? |
| 6 | Dad | Good to talk, kiddo. Come when you can. | Doctors want to run more tests. Please call. |
| 7 | Aunt | Your mom was so happy you called, sweetie. | Sweetie, your mother keeps asking about you. |
| 8 | Jessie | Thanks for picking up. She keeps asking for you. | Why aren't you picking up?? It's serious. |
| 9 | John | Okay. See you soon. | Call Dad. Now. |
| 10 | Dad | Drive safe. We'll be here. | They're saying weeks, maybe less. Please come home. |
| 11 | Jessie | Tell me when you land. | She asked for you again today. |
| 12 | John | Good. Hurry. | If you're coming, come now. |
| 13 | Dad | She smiled when I said you're coming. | She's sleeping most of the day now. We're all here. |

**Her voicemail** (her last call missed), approved by the user:
> Hi sweetheart, it's just me. No reason… I'm a bit tired, so I'll keep it short. I just wanted to hear your voice. Call your dad sometime, okay? And be good to Jessie and John. Don't worry about me. I love you so much. I'm so proud of you.

**Her last call** (answered):
> Hi sweetheart… you picked up. No, nothing's wrong. I just wanted to hear you. Tell me about your day. Take your time… I love you. I'm so proud of you.

**Final texts:** see "The three endings". **TURN BACK TIME responses:** see section 6.
