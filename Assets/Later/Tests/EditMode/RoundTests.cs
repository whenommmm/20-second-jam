using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Later.LaterData;

namespace Later.Tests
{
    // Ported from web/later/tests/round.test.js, same names and values.
    public class RoundTests
    {
        const double Dt = 1.0 / 60;

        sealed class Played
        {
            public Round Round;
            public List<RoundEvent> Events;
            public int Overlaps;
            public int? LeftAt17;
        }

        // Play one whole round with a test player steering every step; record what happened.
        static Played Play(Func<Round, Vec> bot, double dt = Dt, double w = W)
        {
            var round = RoundSim.Create(null, w);
            var events = new List<RoundEvent>();
            int overlaps = 0;
            int? leftAt17 = null;
            while (!round.Over)
            {
                events.AddRange(RoundSim.Step(round, dt, new RoundInput { Target = bot(round) }));
                var ps = round.Pills;
                for (var i = 0; i < ps.Count; i++)
                    for (var j = i + 1; j < ps.Count; j++)
                    {
                        Pill a = ps[i], b = ps[j];
                        double ow = Math.Min(a.X + a.W / 2, b.X + b.W / 2) - Math.Max(a.X - a.W / 2, b.X - b.W / 2);
                        double oh = Math.Min(a.Y + a.H / 2, b.Y + b.H / 2) - Math.Max(a.Y - a.H / 2, b.Y - b.H / 2);
                        if (ow > 4 && oh > 4) overlaps++;
                    }
                if (leftAt17 == null && round.T >= 17) leftAt17 = round.Pills.Count(p => p.X + p.W / 2 > 0); // still visible
            }
            return new Played { Round = round, Events = events, Overlaps = overlaps, LeftAt17 = leftAt17 };
        }

        // across the whole width, every 15 px, right up to the far edge, in the plain world and the wide ones phones get
        [TestCase(960), TestCase(1169), TestCase(1280)]
        public void ACarefulDodger_AnywhereAcrossTheWorld_IsNeverForcedToAnswer(double w)
        {
            var caught = new List<double>();
            for (double x = 26; x <= w - 26; x += 15)
                if (Play(r => Bots.DodgeTarget(r, x), Dt, w).Round.Hits > 0) caught.Add(x);
            CollectionAssert.IsEmpty(caught);
        }

        [TestCase(1169), TestCase(1280)]
        public void WideWorld_PillsNeverOverlap_AndAreAllGoneBy17s(double w)
        {
            Assert.AreEqual(0, Play(r => Bots.DodgeTarget(r, 200), Dt, w).Overlaps);
            Assert.AreEqual(0, Play(r => Bots.ChaseTarget(r), Dt, w).Overlaps);
            Assert.AreEqual(0, Play(r => Bots.DodgeTarget(r, 340), Dt, w).LeftAt17);
        }

        [TestCase(1169), TestCase(1280)]
        public void WideWorld_PillsComeInFromItsRightEdge_AndTheMoonCanGoAllTheWayOver(double w)
        {
            var round = RoundSim.Create(null, w);
            var spawns = new List<double>();
            while (round.T < 3)
                foreach (var e in RoundSim.Step(round, Dt, new RoundInput { Target = new Vec(w, 270) }))
                    if (e.Type == RoundEventType.Spawn) spawns.Add(e.Pill.X - e.Pill.W / 2);
            Assert.That(spawns.Count > 0 && spawns.All(left => left > w - 6), "a pill appeared inside the screen: " + string.Join(",", spawns.Select(Rules.RoundHalfUp)));
            Assert.AreEqual(w - 26, Rules.RoundHalfUp(round.Me.X));
        }

        [Test]
        public void PillsNeverOverlap()
        {
            Assert.AreEqual(0, Play(r => Bots.DodgeTarget(r, 200)).Overlaps);
            Assert.AreEqual(0, Play(r => Bots.ChaseTarget(r)).Overlaps);
        }

        [Test]
        public void EveryPill_IsGoneBy17s() => Assert.AreEqual(0, Play(r => Bots.DodgeTarget(r, 340)).LeftAt17);

        [Test]
        public void EachPillThatGetsPast_CountsOnce()
        {
            var res = Play(r => Bots.DodgeTarget(r, 340));
            Assert.AreEqual(29, res.Round.Missed);
            Assert.AreEqual(29, res.Round.Dodged);
            Assert.AreEqual(29, res.Events.Count(e => e.Type == RoundEventType.Pass));
        }

        [Test]
        public void AnsweringNeverCoolsThePaper()
        {
            var res = Play(r => Bots.ChaseTarget(r));
            Assert.AreEqual(0, res.Round.Missed);
            Assert.That(res.Round.PaperK, Is.LessThan(0.001));
        }

        [Test]
        public void DodgingEverything_CoolsItFully() => Assert.That(Play(r => Bots.DodgeTarget(r, 340)).Round.PaperK, Is.GreaterThan(0.95));

        [Test]
        public void HerLastCall_ReachesAPlayerWhoNeverMoves() =>
            Assert.IsTrue(Play(r => new Vec(340, 302)).Round.Calls[13].Answered);

        [Test]
        public void DodgeAll_AnswerOnlyHerLastCall()
        {
            var res = Play(r => r.Calls[13].Spawned > 0 ? Bots.ChaseTarget(r, p => p.Ci == 13) : Bots.DodgeTarget(r, 340));
            Assert.AreEqual(1, res.Round.Hits);
            Assert.AreEqual("last", Rules.EndingFor(res.Round.Answered));
        }

        [Test]
        public void TimeFiresOnce_AndTStopsAt20()
        {
            var res = Play(r => Bots.DodgeTarget(r, 340));
            Assert.AreEqual(1, res.Events.Count(e => e.Type == RoundEventType.Time));
            Assert.AreEqual(20, res.Round.T);
        }

        [Test]
        public void LongFrames_PlayOutTheSameAsSmoothOnes_NoTunnelling()
        {
            Func<Round, Vec> still = r => new Vec(340, 302);
            Assert.AreEqual(Play(still, Dt).Round.Hits, Play(still, 0.2).Round.Hits);
            Assert.That(Play(still, 0.2).Round.Hits, Is.GreaterThan(0));
        }

        [Test]
        public void EachCallBuzzesOnce_OnItsFirstPill_NotEveryPill()
        {
            var spawns = Play(r => Bots.DodgeTarget(r, 340)).Events.Where(e => e.Type == RoundEventType.Spawn).ToList();
            Assert.AreEqual(29, spawns.Count);
            Assert.AreEqual(14, spawns.Count(e => e.Pill.First));
        }

        [TestCase(960), TestCase(1280)]
        public void HerLastCall_DriftsGentlyTowardYou_UntilTheMiddle_ThenGoesDeadStraight(double w)
        {
            var round = RoundSim.Create(null, w);
            List<double> before = new List<double>(), after = new List<double>();
            while (!round.Over)
            {
                double y = 302 + 150 * Math.Sin(round.T * 3); // a player who keeps moving up and down on the left
                RoundSim.Step(round, Dt, new RoundInput { Target = new Vec(200, y) });
                var mom = round.Pills.FirstOrDefault(p => p.Ci == 13);
                if (mom != null) (mom.X > w / 2 ? before : after).Add(mom.Vy);
            }
            Assert.That(before.Count > 0 && after.Count > 0, "her last call never crossed the middle");
            Assert.That(before.Max(Math.Abs), Is.LessThanOrEqualTo(90), "steers harder than 90 px/s");
            Assert.That(before.Skip(before.Count - 30).Select(v => v.ToString("F3")).Distinct().Count(), Is.GreaterThan(1), "stopped steering before the middle");
            // the commit happens on the step after it crosses the middle (one frame, ~5 px), so skip that crossing frame
            Assert.That(after.Skip(1).All(v => v == 0), "doesn't go dead straight after the middle");
        }

        [Test]
        public void TheMoonEases_TowardASuddenNewTarget_InsteadOfSnapping_AndStillGetsThereQuickly()
        {
            var round = RoundSim.Create();
            var start = round.Me;
            var target = new Vec(start.X + 300, start.Y);
            RoundSim.Step(round, Dt, new RoundInput { Target = target });
            Assert.That(round.Me.X - start.X, Is.LessThan(1.7), $"jumped {round.Me.X - start.X:F2} px in the first frame");
            double t = Dt;
            while (Math.Abs(target.X - round.Me.X) > 8 && t < 2) { RoundSim.Step(round, Dt, new RoundInput { Target = target }); t += Dt; }
            Assert.That(t, Is.LessThanOrEqualTo(0.8), $"took {t:F2} s to arrive");
        }
    }
}
