using UnityEngine;

namespace Later
{
    // Sound: no voices, a soft ringing under play, and everything goes quiet at TIME!
    // Ported from web/later/src/sound.js: every sound is made here at startup (Core/Synth), at the rate the browser's
    // audio runs at, as the web version makes each one on the fly; the page suspends it while the tab is hidden.
    public sealed class Sound : MonoBehaviour
    {
        AudioClip buzz, close, thud, ring, tailClip;
        readonly AudioClip[] blips = new AudioClip[15];
        AudioSource fx, ringer, tail;
        Synth synth;
        bool muted, ringing;

        public bool Ringing => ringing;
        // what the game made its sounds at, and the ringing's loop (for the tests)
        public int SampleRate => synth.Rate;
        public AudioClip RingClip => ring;
        // the ringing's last notes, fading out after it stopped (null once silenced)
        public AudioClip FadingRing => tail != null && tail.clip != null && !stopped ? tail.clip : null;
        bool stopped = true;

        void Awake()
        {
            int rate = Browser.AudioRate ?? AudioSettings.outputSampleRate;
            synth = new Synth(rate > 0 ? rate : 48000);
            buzz = Clip("buzz", synth.Buzz());
            for (var i = 0; i < blips.Length; i++) blips[i] = Clip($"blip{i:00}", synth.Blip(i));
            close = Clip("close", synth.Close());
            thud = Clip("thud", synth.Thud(new System.Random())); // a new noise each session, as Math.random fills it
            ring = Clip("ring", synth.Ring());

            fx = gameObject.AddComponent<AudioSource>();
            fx.playOnAwake = false;
            ringer = gameObject.AddComponent<AudioSource>();
            ringer.playOnAwake = false;
            ringer.loop = true;
            ringer.clip = ring;
            tail = gameObject.AddComponent<AudioSource>();
            tail.playOnAwake = false;
            Browser.InitAudio();
        }

        AudioClip Clip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, synth.Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // turn the volume back up for a new round
        public void Unlock() => muted = false;

        // the slow ringing under play
        public void RingStart()
        {
            StopTail();
            ringer.Stop();
            ringer.PlayDelayed(0.05f);
            ringing = true;
        }

        // The phone stops ringing (a gentle fade): the notes the web's scheduler had already queued play on through
        // its linear fade, and no later ones (Synth.RingTail), in place of the loop.
        public void RingStop(float fade = 1.5f)
        {
            if (!ringing) return;
            ringing = false;
            double pos = ringer.isPlaying ? ringer.time : 0;
            ringer.Stop();
            StopTail();
            if (fade <= 0) return;
            tailClip = Clip("ring-tail", synth.RingTail(pos, fade));
            tail.clip = tailClip;
            tail.Play();
            stopped = false;
        }

        // TIME!: everything goes quiet at once, a ringing that is fading out included
        public void Silence()
        {
            ringing = false;
            ringer.Stop();
            StopTail();
            fx.Stop();
            muted = true;
        }

        void StopTail()
        {
            tail.Stop();
            stopped = true;
            if (tailClip != null) { tail.clip = null; Destroy(tailClip); tailClip = null; }
        }

        // a phone vibrating on a table, once as each call comes in
        public void Buzz() => Play(buzz);
        // each dodge climbs a semitone with the streak
        public void Dodge(int streak) => Play(blips[Mathf.Clamp(streak, 0, blips.Length - 1)]);
        public void Close() => Play(close);
        // touching a call: a dull thud, not a voice
        public void Thud() => Play(thud);

        void Play(AudioClip clip)
        {
            if (!muted && clip != null) fx.PlayOneShot(clip);
        }
    }
}
