using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Later.LaterData;
using static Later.Tests.LaterTestUtil;

namespace Later.Tests
{
    // TIME!, the results, the shrink into the phone and the flights home.
    // Ported from the browser checks morphpill and the results part of late. Times are from TIME!.
    public class ResultsTests
    {
        [TearDown] public void TearDown() => Unload();

        static IEnumerator DodgeAll() => Play(r => Bots.DodgeTarget(r, 340));

        [UnityTest]
        public IEnumerator Results_ShowTheRound()
        {
            yield return Load();
            yield return DodgeAll();
            yield return null; // styles resolve once per frame
            Assert.IsTrue(Shown(Q("timeov")), "TIME! isn't shown");
            yield return Wait(2.1);
            var r = Game.Round;
            Assert.IsFalse(Shown(Q("timeov")), "TIME! still shown");
            Assert.AreEqual(Rules.FormatScore(r.Score), Text("resnum"));
            Assert.AreEqual("29 / 29", Text("r1"));
            Assert.AreEqual(r.CloseN.ToString(), Text("r2"));
            Assert.AreEqual("×" + r.Best, Text("r3"));
            Assert.AreEqual("PERFECT!", Text("rank"));
            Assert.IsTrue(Q("res").Query(className: "row").ToList().All(Shown), "a results row isn't shown");
        }

        [UnityTest]
        public IEnumerator OrIsIt_OnlyAfterAPerfectRound()
        {
            yield return Load();
            yield return DodgeAll();
            yield return Wait(4.0);
            Assert.IsTrue(Shown(Q("doubt")), "no doubt after a perfect round");

            yield return Load();
            bool hit = false;
            yield return Play(r =>
            {
                hit |= r.Hits > 0;
                return hit ? Bots.DodgeTarget(r, 340) : Bots.ChaseTarget(r, p => p.Ci == 0);
            });
            Assert.AreEqual(1, Game.Round.Hits);
            yield return Wait(4.0);
            Assert.AreEqual("GREAT!", Text("rank"));
            Assert.IsFalse(Shown(Q("doubt")), "doubt after a round with a hit");
        }

        [UnityTest]
        public IEnumerator Phone_LandsCentred([Values(0, 1)] int wide)
        {
            yield return Load(wide == 1 ? new Vector2Int(844, 390) : new Vector2Int(960, 540));
            yield return DodgeAll();
            yield return Wait(6);
            var card = Q("card").worldBound;
            var world = Q("world");
            float s = world.worldBound.height / (float)H;
            Assert.AreEqual(256, card.width / s, 0.5);
            Assert.AreEqual(516, card.height / s, 0.5);
            Assert.AreEqual(world.worldBound.center.x, card.center.x, 1);
            Assert.AreEqual(world.worldBound.center.y, card.center.y, 1);
            yield return Wait(2);
            Assert.AreEqual(0, Q("fly").childCount, "flyers left behind");
            Assert.AreEqual(1, Q("focus").resolvedStyle.opacity, 0.001);
            Assert.IsTrue(Shown(Q("sbMoon")), "the status bar moon isn't on");
            var r = Game.Round;
            Assert.AreEqual(r.Missed.ToString(), Text("gcount"));
            var names = r.Calls.Where(c => c.MissedN > 0).Reverse().Select(c => c.Def.Who).Distinct();
            Assert.AreEqual(string.Join(", ", names), Text("gnames"));
        }

        // the moon and its name turn into the "☾ Later" pill without ever disappearing
        [UnityTest]
        public IEnumerator MoonAndName_BecomeThePill_WithoutDisappearing()
        {
            yield return Load();
            yield return DodgeAll();
            yield return WaitFor(() => Q("fly").childCount > 0, 7, "the flights");
            double start = Time.timeAsDouble;
            var gaps = new System.Collections.Generic.List<string>();
            while (Time.timeAsDouble - start < 2.2)
            {
                var flyers = Q("fly").Query(className: "flyer").ToList();
                var moon = flyers.FirstOrDefault(f => f.ClassListContains("moon"));
                var tag = flyers.FirstOrDefault(f => f.ClassListContains("tag"));
                bool pill = Q("focus").resolvedStyle.opacity > 0.5f;
                bool tagShows = tag != null && tag.resolvedStyle.opacity > 0.3f && tag.Query<Label>().ToList().Any(l => l.resolvedStyle.opacity > 0.3f);
                string at = $"{Time.timeAsDouble - start:F2} s";
                if (!(moon != null && moon.resolvedStyle.opacity > 0.3f) && !pill) gaps.Add(at + ": no moon and no pill");
                if (!tagShows && !(pill && Q("focusText").resolvedStyle.opacity > 0.5f)) gaps.Add(at + ": no LATER and no 'Later'");
                yield return null;
            }
            CollectionAssert.IsEmpty(gaps.Take(4));
        }
    }
}
