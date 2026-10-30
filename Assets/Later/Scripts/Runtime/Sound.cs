using UnityEngine;

namespace Later
{
    // Sound: no voices, a soft ringing under play, and everything goes quiet at TIME!
    // Ported from web/later/src/sound.js; the clips were baked from its synthesis (Editor/SoundBaker).
    public sealed class Sound : MonoBehaviour
    {
        public AudioClip buzz, close, thud, ring;
        public AudioClip[] blips;

        AudioSource fx, ringer;
        bool muted, ringing;
        float fadeLeft, fadeTotal, fadeFrom;

        public bool Ringing => ringing;

        void Awake()
        {
            fx = gameObject.AddComponent<AudioSource>();
            fx.playOnAwake = false;
            ringer = gameObject.AddComponent<AudioSource>();
            ringer.playOnAwake = false;
            ringer.loop = true;
            ringer.clip = ring;
        }

        // turn the volume back up for a new round
        public void Unlock() => muted = false;

        // the slow ringing under play
        public void RingStart()
        {
            fadeLeft = 0;
            ringer.Stop();
            ringer.volume = 1;
            ringer.PlayDelayed(0.05f);
            ringing = true;
        }

        // the phone stops ringing (a gentle fade)
        public void RingStop(float fade = 1.5f)
        {
            if (!ringing) return;
            ringing = false;
            if (fade <= 0) { ringer.Stop(); fadeLeft = 0; return; }
            fadeFrom = ringer.volume; fadeTotal = fadeLeft = fade;
        }

        public void Silence()
        {
            RingStop(0);
            fx.Stop();
            muted = true;
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

        void Update()
        {
            if (fadeLeft <= 0) return;
            fadeLeft -= Time.deltaTime;
            ringer.volume = fadeFrom * Mathf.Max(0, fadeLeft / fadeTotal);
            if (fadeLeft <= 0) ringer.Stop();
        }
    }
}
