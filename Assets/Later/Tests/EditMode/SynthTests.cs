using System;
using System.Linq;
using NUnit.Framework;
using static Later.LaterData;

namespace Later.Tests
{
    // The game makes its sounds at startup the way the web version's Web Audio graph makes them (web/later/src/sound.js).
    public class SynthTests
    {
        const double Master = 0.55;
        static double Cycle => RING.STEP * RING.MOTIF.Length + RING.REST;
        static readonly Synth S = new Synth(44100);

        static float Peak(float[] s) => s.Max(Math.Abs);
        static float Peak(float[] s, double from, double to, int rate) =>
            s.Skip((int)(from * rate)).Take((int)((to - from) * rate)).Max(Math.Abs);

        static (string Name, float[] Samples)[] Clips() => new[]
        {
            ("buzz", S.Buzz()), ("blip0", S.Blip(0)), ("blip14", S.Blip(14)), ("close", S.Close()),
            ("thud", S.Thud(new Random(7))), ("ring", S.Ring())
        };

        [Test]
        public void Ring_IsExactlyOneCycleLong_SoItLoopsSeamlessly() =>
            Assert.AreEqual((int)Rules.RoundHalfUp(S.Rate * Cycle), S.Ring().Length);

        [Test]
        public void Ring_NotesStartWhenRingNotesSays()
        {
            var ring = S.Ring();
            var notes = Rules.RingNotes(0, Cycle);
            Assert.AreEqual(6, notes.Count);
            foreach (var (at, _) in notes)
            {
                int i = (int)Math.Ceiling(at * S.Rate);
                while (i < ring.Length && Math.Abs(ring[i]) <= 1e-3) i++;
                Assert.AreEqual(at, (double)i / S.Rate, 0.002, $"note at {at:F2} s");
            }
        }

        [Test]
        public void Peaks_StayQuiet()
        {
            Assert.That(Peak(S.Ring()), Is.LessThanOrEqualTo(RING.VOLUME * Master * 1.3));
            foreach (var (name, s) in Clips()) Assert.That(Peak(s), Is.LessThanOrEqualTo(Master), name);
            foreach (var (name, s) in Clips()) Assert.That(Peak(s), Is.GreaterThan(0.001), name + " is silent");
        }

        [Test]
        public void Buzz_LastsFourTenthsOfASecond() => Assert.AreEqual((int)Rules.RoundHalfUp(0.4 * S.Rate), S.Buzz().Length);

        [Test]
        public void Blip_ClimbsASemitoneWithTheStreak()
        {
            var s = S.Blip(14);
            int first = -1, last = -1, crossings = 0;
            for (var i = 1; i < s.Length; i++)
                if ((s[i - 1] < 0) != (s[i] < 0) && (Math.Abs(s[i]) > 1e-6 || Math.Abs(s[i - 1]) > 1e-6))
                {
                    if (first < 0) first = i;
                    last = i; crossings++;
                }
            double freq = (crossings - 1) / 2.0 / ((double)(last - first) / S.Rate);
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

        // made at the rate the browser's audio runs at (48 kHz on most devices), so nothing is resampled
        [Test]
        public void TheSounds_AreMadeAtTheDevicesRate()
        {
            var s48 = new Synth(48000);
            Assert.AreEqual(19200, s48.Buzz().Length);
            Assert.AreEqual((int)Rules.RoundHalfUp(48000 * Cycle), s48.Ring().Length);
        }

        // the web fills its thud's noise with Math.random once a session
        [Test]
        public void TheThudsNoise_IsNewEachSession() => CollectionAssert.AreNotEqual(S.Thud(new Random(1)), S.Thud(new Random(2)));

        // an OscillatorNode's "triangle": its odd harmonics below the Nyquist frequency, 8 / (π n)² each with
        // alternating sign, scaled to a peak of 1 (Web Audio normalises its waves)
        [Test]
        public void TheBuzzsTriangle_IsBandLimited_LikeAnOscillatorNode()
        {
            const double f = 165;
            double Want(double phase, int rate)
            {
                double sum = 0, norm = 0;
                for (int n = 1; n * f < rate / 2.0; n += 2)
                {
                    double b = 8 / (Math.PI * Math.PI * n * n) * ((n / 2) % 2 == 0 ? 1 : -1);
                    sum += b * Math.Sin(2 * Math.PI * n * phase);
                    norm += Math.Abs(b);
                }
                return sum / norm;
            }
            foreach (var rate in new[] { 44100, 48000 })
                foreach (var phase in new[] { 0, 0.1, 0.25, 0.37, 0.8 })
                    Assert.AreEqual(Want(phase, rate), new Synth(rate).Triangle(f, phase), 2e-3, $"{rate} Hz, phase {phase}");
        }

        // When the phone stops ringing, the web's scheduler has already queued the notes starting in the next ~0.35 s
        // (it looks 0.4 s ahead every 0.1 s): they play on through the 1.5 s fade, and no later ones do.
        [Test]
        public void RingStop_KeepsOnlyTheNotesTheWebHadQueued_AndFadesToNothing()
        {
            // stopped 0.36 s into a cycle: the note at 0.56 s was queued, the one at 0.84 s wasn't
            var tail = S.RingTail(0.36, 1.5);
            var all = S.RingTail(0.36, 1.5, 10);
            Assert.That(Peak(tail, 0.2, 0.24, S.Rate), Is.GreaterThan(Peak(tail, 0.15, 0.19, S.Rate) * 1.5), "the queued note at 0.56 s didn't play");
            Assert.That(Peak(tail, 0.48, 0.52, S.Rate), Is.LessThan(Peak(tail, 0.43, 0.47, S.Rate)), "a note the web hadn't queued played");
            Assert.That(Peak(all, 0.48, 0.52, S.Rate), Is.GreaterThan(Peak(all, 0.43, 0.47, S.Rate) * 1.5), "the test's later note isn't there to leave out");
            Assert.AreEqual((int)Rules.RoundHalfUp(1.5 * S.Rate), tail.Length);
            Assert.That(Math.Abs(tail[tail.Length - 1]), Is.LessThan(1e-4), "the fade doesn't reach silence");
        }
    }
}
