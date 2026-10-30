using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Later.Tests
{
    // Replays rounds recorded from the JavaScript simulation (web/later/tools/export-unity.mjs) and checks
    // the C# round does exactly the same: the same events on the same steps, and the same state along the way.
    public class ParityTests
    {
        const string FixturePath = "Assets/Later/Tests/EditMode/Fixtures/round-traces.json";

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable] class TraceFile { public Trace[] traces; }
        [Serializable] class Trace
        {
            public string name;
            public double w, dt, meX, meY;
            public double[] tx, ty;
            public bool[] has;
            public int[] kx, ky;
            public TraceEvent[] events;
            public Checkpoint[] checkpoints;
        }
        [Serializable] class TraceEvent { public int step, pill; public string type, date; public bool close; }
        [Serializable] class Checkpoint { public int step, score, missed, hits, pills; public double t, x, y, paperK; }
#pragma warning restore 0649

        static Trace[] Traces => JsonUtility.FromJson<TraceFile>(File.ReadAllText(FixturePath)).traces;

        static IEnumerable<string> TraceNames() => Traces.Select(t => t.name);

        [Test]
        public void TheFixture_HasEveryScenario()
        {
            CollectionAssert.AreEqual(
                new[] { "dodge-340", "chase-all", "last-only", "still-long-frames", "between-lanes", "dodge-wide", "home-drift", "keys" },
                TraceNames());
        }

        [TestCaseSource(nameof(TraceNames))]
        public void ReplaysTheJavaScriptRound(string name)
        {
            var tr = Traces.Single(t => t.name == name);
            var r = RoundSim.Create(null, tr.w);
            r.Me = new Vec(tr.meX, tr.meY);
            var events = new List<string>();
            var checkpoints = new List<Checkpoint>();
            for (var step = 0; !r.Over; step++)
            {
                Assert.That(step, Is.LessThan(tr.tx.Length), "the round ran longer than the JavaScript one");
                var input = new RoundInput { Target = tr.has[step] ? new Vec(tr.tx[step], tr.ty[step]) : (Vec?)null, KeysX = tr.kx[step], KeysY = tr.ky[step] };
                foreach (var e in RoundSim.Step(r, tr.dt, input))
                    events.Add(Describe(step, e.Type.ToString().ToLowerInvariant(), e.Pill?.Id ?? 0, e.Close, e.Date ?? ""));
                if (step % 30 == 29 || r.Over)
                    checkpoints.Add(new Checkpoint { step = step, t = r.T, x = r.Me.X, y = r.Me.Y, score = r.Score, missed = r.Missed, hits = r.Hits, paperK = r.PaperK, pills = r.Pills.Count });
            }

            var want = tr.events.Select(e => Describe(e.step, e.type, e.pill, e.close, e.date)).ToList();
            for (var i = 0; i < Math.Min(want.Count, events.Count); i++)
                Assert.AreEqual(want[i], events[i], $"event {i} differs");
            Assert.AreEqual(want.Count, events.Count, "number of events");

            Assert.AreEqual(tr.checkpoints.Length, checkpoints.Count, "number of checkpoints");
            for (var i = 0; i < checkpoints.Count; i++)
            {
                Checkpoint a = tr.checkpoints[i], b = checkpoints[i];
                string at = $"checkpoint at step {a.step}";
                Assert.AreEqual(a.step, b.step, at);
                Assert.AreEqual(a.t, b.t, 1e-6, at + ": t");
                Assert.AreEqual(a.x, b.x, 1e-6, at + ": moon x");
                Assert.AreEqual(a.y, b.y, 1e-6, at + ": moon y");
                Assert.AreEqual(a.score, b.score, at + ": score");
                Assert.AreEqual(a.missed, b.missed, at + ": missed");
                Assert.AreEqual(a.hits, b.hits, at + ": hits");
                Assert.AreEqual(a.paperK, b.paperK, 1e-9, at + ": paperK");
                Assert.AreEqual(a.pills, b.pills, at + ": pills");
            }
        }

        static string Describe(int step, string type, int pill, bool close, string date) =>
            $"step {step}: {type} pill {pill}{(close ? " close" : "")}{(date != "" ? " " + date : "")}";
    }
}
