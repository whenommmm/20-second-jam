using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Later.Tests
{
    public class TimelineTests
    {
        [Test]
        public void After_RunsInTimeOrder()
        {
            var tl = new Timeline();
            var ran = new List<string>();
            tl.After(2, () => ran.Add("a"));
            tl.After(1, () => ran.Add("b"));
            tl.Update(10);
            CollectionAssert.AreEqual(new[] { "b", "a" }, ran);
        }

        [Test]
        public void After_FromInsideACallback_CountsFromThatCallbacksTime()
        {
            var tl = new Timeline();
            var ran = new List<(string, double)>();
            tl.After(1, () =>
            {
                ran.Add(("first", tl.Now));
                tl.After(0, () => ran.Add(("now", tl.Now)));
                tl.After(1, () => ran.Add(("later", tl.Now)));
            });
            tl.Update(1);
            CollectionAssert.AreEqual(new[] { ("first", 1.0), ("now", 1.0) }, ran);
            tl.Update(9);
            CollectionAssert.AreEqual(new[] { ("first", 1.0), ("now", 1.0), ("later", 2.0) }, ran);
            Assert.AreEqual(10, tl.Now);
        }

        [Test]
        public void After_ScheduledFromACallback_RunsNoEarlier_EvenInsideOneLongUpdate()
        {
            var tl = new Timeline();
            double at = -1;
            tl.After(1, () => tl.After(1, () => at = tl.Now));
            tl.Update(10);
            Assert.AreEqual(2, at);
        }

        [Test]
        public void Clear_StopsEverythingPending()
        {
            var tl = new Timeline();
            int runs = 0, applies = 0;
            tl.After(1, () => runs++);
            tl.Tween(0, 2, Ease.Linear, p => applies++, () => runs++);
            tl.Update(0.5);
            Assert.AreEqual(1, applies);
            tl.Clear();
            tl.Update(10);
            Assert.AreEqual(0, runs);
            Assert.AreEqual(1, applies);
        }

        [Test]
        public void Clear_FromInsideACallback_StopsTheRestOfThatUpdate()
        {
            var tl = new Timeline();
            int runs = 0;
            tl.After(1, () => tl.Clear());
            tl.After(2, () => runs++);
            tl.Update(10);
            Assert.AreEqual(0, runs);
        }

        [Test]
        public void Tween_WaitsForItsDelay_EndsOnExactlyOne_AndCallsDoneOnce()
        {
            var tl = new Timeline();
            var applied = new List<double>();
            int done = 0;
            tl.Tween(0.5, 1, Ease.Linear, applied.Add, () => done++);
            tl.Update(0.25);
            CollectionAssert.IsEmpty(applied);
            tl.Update(0.5);
            Assert.AreEqual(0.25, applied[applied.Count - 1], 1e-9);
            tl.Update(10);
            tl.Update(1);
            Assert.AreEqual(1, applied[applied.Count - 1]);
            Assert.AreEqual(1, applied.FindAll(p => p == 1).Count);
            Assert.AreEqual(1, done);
        }

        [Test]
        public void CubicBezier_MatchesTheCssEase()
        {
            var ease = Ease.CubicBezier(.25, .1, .25, 1);
            Assert.AreEqual(0.8024, ease(0.5), 1e-4);
            Assert.AreEqual(0, ease(0));
            Assert.AreEqual(1, ease(1));
            Assert.AreEqual(0.8024, Ease.CssEase(0.5), 1e-4);
            foreach (var x in new[] { 0, 0.1, 0.37, 0.5, 0.99, 1 }) Assert.AreEqual(x, Ease.Linear(x));
        }

        [Test]
        public void CubicBezier_EaseOut_IsMonotonic()
        {
            double last = 0;
            for (var i = 1; i <= 100; i++)
            {
                double v = Ease.EaseOut(i / 100.0);
                Assert.That(v, Is.GreaterThanOrEqualTo(last));
                last = v;
            }
            Assert.AreEqual(1, last, 1e-9);
        }
    }
}
