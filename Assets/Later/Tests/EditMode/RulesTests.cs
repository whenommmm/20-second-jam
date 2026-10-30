using System;
using System.Linq;
using NUnit.Framework;
using static Later.LaterData;

namespace Later.Tests
{
    // Ported from web/later/tests/rules.test.js, plus the JavaScript number behaviour the port depends on.
    public class RulesTests
    {
        static bool[] Answered(params int[] which) => Enumerable.Range(0, 14).Select(which.Contains).ToArray();

        [Test]
        public void ShortDate()
        {
            Assert.AreEqual("Mar 5", Rules.ShortDate("MAR 05"));
            Assert.AreEqual("Apr 1", Rules.ShortDate("APR 01"));
            Assert.AreEqual("Mar 28", Rules.ShortDate("MAR 28"));
        }

        [Test]
        public void Clarity()
        {
            Assert.AreEqual(0, Rules.Clarity(0, 0));
            Assert.AreEqual(1, Rules.Clarity(16, 0));
            Assert.AreEqual(1, Rules.Clarity(20, 0));
            Assert.That(Math.Abs(Rules.Clarity(8, 0) - 0.0884), Is.LessThan(0.001));
            Assert.AreEqual(1, Rules.Clarity(8, 0.17 * 6));
        }

        [Test]
        public void Paper()
        {
            Assert.AreEqual((253, 220, 172), Rules.Paper(0));
            Assert.AreEqual((146, 158, 172), Rules.Paper(1));
            Assert.AreEqual((200, 189, 172), Rules.Paper(0.5));
        }

        [Test]
        public void RankFor()
        {
            CollectionAssert.AreEqual(new[] { "PERFECT!", "GREAT!", "GREAT!", "NICE TRY", "NICE TRY" },
                new[] { 0, 1, 2, 3, 9 }.Select(Rules.RankFor));
        }

        [Test]
        public void EndingFor()
        {
            Assert.AreEqual("late", Rules.EndingFor(Answered()));
            Assert.AreEqual("late", Rules.EndingFor(Answered(3)));
            Assert.AreEqual("last", Rules.EndingFor(Answered(13)));
            Assert.AreEqual("home", Rules.EndingFor(Answered(0, 13)));
        }

        [Test]
        public void BuildThread()
        {
            var th = Rules.BuildThread(Answered(4), "late");
            Assert.AreEqual(14, th.Count);
            Assert.AreEqual(new Message("Mom", "MAR 01", PENDING[0]), th[0]);
            Assert.AreEqual(FOLLOW_UP[4], th[4].Text);
            Assert.AreEqual(FINAL["late"], th[13]);
        }

        [Test]
        public void TurnBackLines()
        {
            Assert.AreEqual(("YOU SAID LATER ONCE.", "She would have taken any one of them."), Rules.TurnBackLines("late", 1));
            Assert.AreEqual("YOU SAID LATER TWICE.", Rules.TurnBackLines("late", 2).Loud);
            Assert.AreEqual("YOU SAID LATER 24 TIMES.", Rules.TurnBackLines("late", 24).Loud);
            Assert.AreEqual(("THERE'S NEVER ENOUGH TIME.", "But you were there."), Rules.TurnBackLines("home", 5));
        }

        [Test]
        public void CapsuleDist()
        {
            Assert.AreEqual(-21, Rules.CapsuleDist(100, 100, 100, 42, 100, 100));
            Assert.AreEqual(29, Rules.CapsuleDist(100, 100, 100, 42, 100, 150));
            Assert.AreEqual(50, Rules.CapsuleDist(100, 100, 100, 42, 200, 100));
        }

        [Test]
        public void RingNotes_ASlowTwoNoteRing_ThenAPause_Repeating()
        {
            double cycle = RING.STEP * RING.MOTIF.Length + RING.REST;
            var first = Rules.RingNotes(0, cycle);
            CollectionAssert.AreEqual(RING.MOTIF, first.Select(n => n.Midi));
            CollectionAssert.AreEqual(RING.MOTIF.Select((_, i) => Math.Round(i * RING.STEP, 3)), first.Select(n => Math.Round(n.At, 3)));
            CollectionAssert.AreEqual(new[] { Math.Round(cycle, 3) }, Rules.RingNotes(cycle - 0.01, cycle + 0.01).Select(n => Math.Round(n.At, 3)));
            Assert.That(RING.STEP, Is.GreaterThanOrEqualTo(0.25), "not slow");
            Assert.That(RING.VOLUME, Is.LessThanOrEqualTo(0.06), "not slight");
        }

        [Test]
        public void WorldWidth_WideScreensGetAWiderWorld_InsteadOfEmptyBarsAtTheSides()
        {
            Assert.AreEqual(960, Rules.WorldWidth(960, 540));
            Assert.AreEqual(960, Rules.WorldWidth(800, 600));
            Assert.AreEqual(1169, Rules.WorldWidth(844, 390));
            Assert.AreEqual(1280, Rules.WorldWidth(2000, 540));
        }

        [Test]
        public void RoundHalfUp_RoundsLikeJavaScript()
        {
            Assert.AreEqual(1, Rules.RoundHalfUp(0.5));
            Assert.AreEqual(3, Rules.RoundHalfUp(2.5));
            Assert.AreEqual(-2, Rules.RoundHalfUp(-2.5));
        }

        [Test]
        public void FormatScore_GroupsThousandsLikeToLocaleString()
        {
            Assert.AreEqual("0", Rules.FormatScore(0));
            Assert.AreEqual("1,234", Rules.FormatScore(1234));
            Assert.AreEqual("-1,234", Rules.FormatScore(-1234));
            Assert.AreEqual("-100", Rules.FormatScore(-100));
        }
    }
}
