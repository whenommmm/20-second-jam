using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Later.LaterData;
using static Later.Tests.LaterTestUtil;

namespace Later.Tests
{
    // The sounds are made at startup at the audio's own rate; the ringing stops as the web's does, and TIME! cuts it.
    public class SoundTests
    {
        [TearDown] public void TearDown() => Unload();

        static Sound Sound => Object.FindFirstObjectByType<Sound>();

        [UnityTest]
        public IEnumerator TheSounds_AreMadeAtTheAudiosRate()
        {
            yield return Load();
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            Assert.AreEqual(rate, Sound.SampleRate);
            Assert.AreEqual(rate, Sound.RingClip.frequency);
            double cycle = RING.STEP * RING.MOTIF.Length + RING.REST;
            Assert.AreEqual((int)Rules.RoundHalfUp(rate * cycle), Sound.RingClip.samples);
        }

        // the ringing stops into its fading last notes; TIME! silences those too, as the web's master gain does
        [UnityTest]
        public IEnumerator TheRingsFade_IsCutAtTime()
        {
            yield return Load();
            Game.StartRound();
            yield return Wait(0.5);
            Sound.RingStop();
            Assert.IsFalse(Sound.Ringing);
            Assert.IsNotNull(Sound.FadingRing, "no fading last notes");
            Assert.AreEqual((int)Rules.RoundHalfUp(1.5 * Sound.SampleRate), Sound.FadingRing.samples);
            Sound.Silence();
            Assert.IsNull(Sound.FadingRing, "TIME! left the ringing fading on");
        }
    }
}
