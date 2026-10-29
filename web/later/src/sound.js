// Sound, made on the fly: no voices, a soft ringing under play, and everything goes quiet at TIME!
// Every method is a silent no-op if Web Audio is missing or fails.
import { RING } from "./data.js";
import { ringNotes } from "./rules.js";

export function createSound() {
  let ac = null, master = null, noise = null;
  let ring = null; // { bus, start, until, timer } while the phone is ringing
  const safe = fn => (...args) => { if (!ac) return; try { fn(...args); } catch (e) { /* sound is optional */ } };

  function env(g, t0, peak, attack, hold, release) {
    g.gain.setValueAtTime(0, t0); g.gain.linearRampToValueAtTime(peak, t0 + attack);
    g.gain.setValueAtTime(peak, t0 + attack + hold); g.gain.linearRampToValueAtTime(0, t0 + attack + hold + release);
  }
  function blip(freq, at, vol) {
    const t0 = ac.currentTime + at, o = ac.createOscillator(), g = ac.createGain();
    o.type = "sine"; o.frequency.value = freq; o.connect(g); g.connect(master);
    env(g, t0, vol, 0.004, 0.02, 0.06); o.start(t0); o.stop(t0 + 0.1);
  }

  // create or resume the audio context; browsers (iOS especially) only allow this inside a tap or click
  function wake() {
    try {
      const AC = window.AudioContext || window.webkitAudioContext;
      if (!AC) return false;
      ac = ac || new AC();
      if (ac.state === "suspended") ac.resume();
      return true;
    } catch (e) { ac = null; return false; }
  }

  // one soft marimba note: a sine with a quick, bright knock on top
  function marimba(bus, midi, t0) {
    const f = 440 * Math.pow(2, (midi - 69) / 12);
    for (const [mult, peak, decay] of [[1, 1, 0.7], [4, 0.25, 0.07]]) {
      const o = ac.createOscillator(), g = ac.createGain();
      o.type = "sine"; o.frequency.value = f * mult; o.connect(g); g.connect(bus);
      g.gain.setValueAtTime(0, t0); g.gain.linearRampToValueAtTime(peak, t0 + 0.005);
      g.gain.exponentialRampToValueAtTime(0.0001, t0 + decay);
      o.start(t0); o.stop(t0 + decay + 0.05);
    }
  }

  function stopRing(fade) {
    if (!ring) return;
    clearInterval(ring.timer);
    const now = ac.currentTime;
    ring.bus.gain.cancelScheduledValues(now);
    ring.bus.gain.setValueAtTime(ring.bus.gain.value, now);
    ring.bus.gain.linearRampToValueAtTime(0, now + fade);
    ring = null;
  }

  return {
    wake,
    // the slow ringing under play; schedules a little ahead so it never stutters
    ringStart: safe(() => {
      stopRing(0);
      const bus = ac.createGain(); bus.gain.value = RING.VOLUME; bus.connect(master);
      ring = { bus, start: ac.currentTime + 0.05, until: 0, timer: 0 };
      const schedule = () => {
        if (!ring) return;
        const ahead = ac.currentTime - ring.start + 0.4;
        for (const n of ringNotes(ring.until, ahead)) marimba(ring.bus, n.midi, ring.start + n.at);
        ring.until = Math.max(ring.until, ahead);
      };
      schedule(); ring.timer = setInterval(schedule, 100);
    }),
    // the phone stops ringing (a gentle fade)
    ringStop: safe((fade = 1.5) => stopRing(fade)),
    ringing: () => !!ring,
    // a hidden tab pauses everything; waking the context resumes it
    suspend: safe(() => ac.suspend()),
    // wake, and turn the volume back up for a new round
    unlock() {
      try {
        if (!wake()) return;
        if (master) master.disconnect();
        master = ac.createGain(); master.gain.value = 0.55; master.connect(ac.destination);
        if (!noise) {
          noise = ac.createBuffer(1, ac.sampleRate * 0.3, ac.sampleRate);
          const d = noise.getChannelData(0);
          for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
        }
      } catch (e) { ac = null; }
    },
    silence: safe(() => { stopRing(0); master.gain.cancelScheduledValues(ac.currentTime); master.gain.setValueAtTime(0, ac.currentTime); }),
    // a phone vibrating on a table, once as each call comes in: a soft, low double pulse (bzz… bzz)
    buzz: safe(() => {
      const t0 = ac.currentTime, o = ac.createOscillator(), lp = ac.createBiquadFilter(), g = ac.createGain();
      o.type = "triangle"; o.frequency.value = 165; lp.type = "lowpass"; lp.frequency.value = 280;
      o.connect(lp); lp.connect(g); g.connect(master);
      g.gain.setValueAtTime(0, t0);
      for (const at of [0, 0.2]) {
        g.gain.linearRampToValueAtTime(0.11, t0 + at + 0.02);
        g.gain.setValueAtTime(0.11, t0 + at + 0.1);
        g.gain.linearRampToValueAtTime(0, t0 + at + 0.14);
      }
      o.start(t0); o.stop(t0 + 0.4);
    }),
    // each dodge climbs a semitone with the streak
    dodge: safe(streak => blip(523 * Math.pow(2, Math.min(streak, 14) / 12), 0, 0.09)),
    close: safe(() => { blip(880, 0, 0.1); blip(1320, 0.06, 0.1); }),
    // touching a call: a dull thud, not a voice
    thud: safe(() => {
      const t0 = ac.currentTime, o = ac.createOscillator(), g = ac.createGain(), n = ac.createBufferSource(), lp = ac.createBiquadFilter(), ng = ac.createGain();
      o.type = "sine"; o.frequency.setValueAtTime(110, t0); o.frequency.exponentialRampToValueAtTime(45, t0 + 0.25);
      o.connect(g); g.connect(master); env(g, t0, 0.38, 0.005, 0.05, 0.2); o.start(t0); o.stop(t0 + 0.3);
      n.buffer = noise; lp.type = "lowpass"; lp.frequency.value = 300;
      n.connect(lp); lp.connect(ng); ng.connect(master); env(ng, t0, 0.22, 0.003, 0.03, 0.09); n.start(t0);
    })
  };
}
