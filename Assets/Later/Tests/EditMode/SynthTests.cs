using System;
using System.Linq;
using Later.Editor;
using NUnit.Framework;
using static Later.LaterData;

namespace Later.Tests
{
    // The baked clips follow the web version's Web Audio synthesis (web/later/src/sound.js).
    public class SynthTests
    {
        const double Master = 0.55;
        static double Cycle => RING.STEP * RING.MOTIF.Length + RING.REST;

        static float Peak(float[] s) => s.Max(Math.Abs);

        static (string Name, float[] Samples)[] Clips() => new[]
        {
            ("buzz", Synth.Buzz()), ("blip0", Synth.Blip(0)), ("blip14", Synth.Blip(14)), ("close", Synth.Close()),
            ("thud", Synth.Thud()), ("ring", Synth.Ring())
        };

        [Test]
        public void Ring_IsExactlyOneCycleLong_SoItLoopsSeamlessly() =>
            Assert.AreEqual((int)Rules.RoundHalfUp(Synth.Rate * Cycle), Synth.Ring().Length);

        [Test]
        public void Ring_NotesStartWhenRingNotesSays()
        {
            var ring = Synth.Ring();
            var notes = Rules.RingNotes(0, Cycle);
            Assert.AreEqual(6, notes.Count);
            foreach (var (at, _) in notes)
            {
                int i = (int)Math.Ceiling(at * Synth.Rate);
                while (i < ring.Length && Math.Abs(ring[i]) <= 1e-3) i++;
                Assert.AreEqual(at, (double)i / Synth.Rate, 0.002, $"note at {at:F2} s");
            }
        }

        [Test]
        public void Peaks_StayQuiet()
        {
            Assert.That(Peak(Synth.Ring()), Is.LessThanOrEqualTo(RING.VOLUME * Master * 1.3));
            foreach (var (name, s) in Clips()) Assert.That(Peak(s), Is.LessThanOrEqualTo(Master), name);
            foreach (var (name, s) in Clips()) Assert.That(Peak(s), Is.GreaterThan(0.001), name + " is silent");
        }

        [Test]
        public void Buzz_LastsFourTenthsOfASecond() => Assert.AreEqual((int)Rules.RoundHalfUp(0.4 * Synth.Rate), Synth.Buzz().Length);

        [Test]
        public void Blip_ClimbsASemitoneWithTheStreak()
        {
            var s = Synth.Blip(14);
            int first = -1, last = -1, crossings = 0;
            for (var i = 1; i < s.Length; i++)
                if ((s[i - 1] < 0) != (s[i] < 0) && (Math.Abs(s[i]) > 1e-6 || Math.Abs(s[i - 1]) > 1e-6))
                {
                    if (first < 0) first = i;
                    last = i; crossings++;
                }
            double freq = (crossings - 1) / 2.0 / ((double)(last - first) / Synth.Rate);
            double want = 523 * Math.Pow(2, 14 / 12.0);
            Assert.AreEqual(want, freq, want * 0.02);
        }

        [Test]
        public void EveryClip_StartsAndEndsAtSilence_NoClicks()
        {
            foreach (var (name, s) in Clips())
            {
                Assert.That(Math.Abs(s[0]), Is.LessThanOrEqualTo(1e-3), name + " starts with a click");
                Assert.That(Math.Abs(s[s.Length - 1]), Is.LessThanOrEqualTo(1e-3), name + " ends with a click");
            }
        }
    }
}
