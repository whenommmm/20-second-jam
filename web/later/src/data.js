// Every number and line of text in LATER, copied from docs/missed-calls-design.md.
// No logic lives here, so the Unity version can read the same values.

export const W = 960, H = 540;

// 8 lanes covering the full height; the moon can move between TOP and BOTTOM.
export const LANES = Array.from({ length: 8 }, (_, i) => 54 + i * 62);
export const TOP = 30, BOTTOM = 512;

// The month of calls: when each starts, who, how many pills, how fast, and the date it stamps.
export const CALLS = [
  { t: 0.4,  who: "Mom",    n: 1, v: 170, date: "MAR 01" },
  { t: 1.6,  who: "Mom",    n: 2, v: 190, date: "MAR 05" },
  { t: 2.6,  who: "Mom",    n: 2, v: 210, date: "MAR 09" },
  { t: 3.5,  who: "Dad",    n: 2, v: 230, date: "MAR 12" },
  { t: 4.4,  who: "Jessie", n: 3, v: 250, date: "MAR 15" },
  { t: 5.3,  who: "Dad",    n: 3, v: 265, date: "MAR 17" },
  { t: 6.2,  who: "Aunt",   n: 3, v: 275, date: "MAR 19" },
  { t: 7.1,  who: "Jessie", n: 3, v: 285, date: "MAR 21" },
  { t: 8.0,  who: "John",   n: 3, v: 290, date: "MAR 23" },
  { t: 8.9,  who: "Dad",    n: 2, v: 280, date: "MAR 24" },
  { t: 9.8,  who: "Jessie", n: 2, v: 265, date: "MAR 25" },
  { t: 10.7, who: "John",   n: 1, v: 250, date: "MAR 26" },
  { t: 11.4, who: "Dad",    n: 1, v: 240, date: "MAR 27" },
  { t: 12.4, who: "Mom",    n: 1, v: 280, date: "MAR 28", home: true }
];

// The date pile starts here, then keeps moving through the quiet seconds after the last call.
export const START_DATE = "FEB 27";
export const QUIET_DATES = [[14.6, "MAR 29"], [16.0, "MAR 30"], [17.4, "MAR 31"], [18.7, "APR 01"], [19.6, "APR 02"]];

export const TUNING = {
  PILL_H: 42, PILL_SPACE: 26,           // pill height; gap between pills in one call
  HIT_R: 8, CLOSE_R: 30,                // how close counts as a touch / a close call
  MAX_SPEED: 720, KEY_SPEED: 460,       // the moon, px/s
  FOLLOW_GAIN: 5.5, FOLLOW_EASE: 8,     // how eagerly the moon chases the pointer, and how smoothly it speeds up and slows down
  REACT_MIN: 1.1,                       // seconds a call near you must give you to react
  HOME_WAIT: 0.6,                       // how long Mom's last call may wait for you to have room
  // her last call drifts toward you until the middle of the screen (or 240 px from you, whichever comes first), then goes straight
  HOME_LOCK_X: 480, HOME_LOCK_DIST: 240, HOME_STEER: 1.5, HOME_MAX_VY: 90,
  CLARITY_BONUS: 0.17,                  // how much each answered call clears the names
  ROUND_TIME: 20
};

// Pill widths: the Node tests use these, the browser measures its own (a check keeps the two within 2 px).
export const DEFAULT_PILL_W = { Mom: 105, Dad: 97, Aunt: 104, John: 105, Jessie: 119 }; // as Chrome draws them with the bundled fonts

// The paper: vibrant with nothing missed, cold slate with everything missed.
export const PAPER_WARM = [253, 220, 172], PAPER_COLD = [146, 158, 172];

// What each caller texts afterwards, one per call (call 14, Mom's last, has her voicemail or call instead).
export const FOLLOW_UP = [ // you picked up
  "So good to hear your voice. Talk soon.",
  "Loved our chat. Eat something proper!",
  "Thanks for calling, sweetheart. Love you.",
  "Thanks for picking up. She was so happy.",
  "Thank you for answering. Room 412, if you come.",
  "Good to talk, kiddo. Come when you can.",
  "Your mom was so happy you called, sweetie.",
  "Thanks for picking up. She keeps asking for you.",
  "Okay. See you soon.",
  "Drive safe. We'll be here.",
  "Tell me when you land.",
  "Good. Hurry.",
  "She smiled when I said you're coming."
];
export const PENDING = [ // you let it ring out
  "Did you eat? Don't skip dinner again.",
  "Are you well? Call when you're free, no rush.",
  "Doctor's appointment went fine. Don't worry. Love you.",
  "Your mom's had a few rough days. Give her a call?",
  "Mom's back in the hospital. Can you call me?",
  "Doctors want to run more tests. Please call.",
  "Sweetie, your mother keeps asking about you.",
  "Why aren't you picking up?? It's serious.",
  "Call Dad. Now.",
  "They're saying weeks, maybe less. Please come home.",
  "She asked for you again today.",
  "If you're coming, come now.",
  "She's sleeping most of the day now. We're all here."
];

export const VOICEMAIL = "Hi sweetheart, it's just me. No reason… I'm a bit tired, so I'll keep it short. I just wanted to hear your voice. Call your dad sometime, okay? And be good to Jessie and John. Don't worry about me. I love you so much. I'm so proud of you.";
export const LAST_CALL = "Hi sweetheart… you picked up. No, nothing's wrong. I just wanted to hear you. Tell me about your day. Take your time… I love you. I'm so proud of you.";
export const CAPTION_HEADERS = { voicemail: "VOICEMAIL · MOM · MAR 28", call: "CALL · MOM · MAR 28 · 4:12" };

// The last text of the month, by ending.
export const FINAL = {
  late: { who: "John", date: "APR 01", text: "Funeral's on Saturday." },
  home: { who: "Dad", date: "MAR 29", text: "Thank you for coming home, kiddo. She knew you were there." },
  last: { who: "Dad", date: "MAR 29", text: "She told everyone you called. It was the last thing she talked about." }
};

// What TURN BACK TIME answers, by ending: a loud line, then a soft one. {n} is how many times you said later.
export const TURN_BACK = {
  late: ["YOU SAID LATER {n}.", "She would have taken any one of them."],
  home: ["THERE'S NEVER ENOUGH TIME.", "But you were there."],
  last: ["YOU GAVE HER FOUR MINUTES.", "She'd waited all month for them."]
};

// The ringing under play: an original, soft marimba "ring-ring" (not Apple's ringtone), slow and quiet.
// MOTIF is MIDI notes played STEP seconds apart, then REST seconds of silence, repeating.
export const RING = { MOTIF: [79, 74, 79, 74, 79, 74], STEP: 0.28, REST: 2.0, VOLUME: 0.05 };
