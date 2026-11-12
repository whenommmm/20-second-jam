using System;
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
    // The lock screen, her words, TURN BACK TIME and PLAY AGAIN. Ported from the browser checks late, home, lastonly,
    // thread, pendingcard, doubletap, fallback, dblturnback, restart and widephone; each waits for a state, then checks.
    public class EndingTests
    {
        [TearDown] public void TearDown() => Unload();

        static IEnumerator DodgeAll() => Play(r => Bots.DodgeTarget(r, 340));
        static IEnumerator PlayLastOnly() => Play(r => r.Calls[13].Spawned > 0 ? Bots.ChaseTarget(r, p => p.Ci == 13) : Bots.DodgeTarget(r, 340));

        static VisualElement MessagesCard => Q("items").Query(className: "tap").ToList().FirstOrDefault();
        static string TopWho => Plain(MessagesCard?.Q<Label>(className: "title")?.text);
        static string TopText => Plain(MessagesCard?.Q<Label>(className: "text")?.text);
        static VisualElement Again => Q("again");
        static string AgainText => string.Concat(Again.Query<Label>().ToList().Select(l => Plain(l.text)));
        static int CaptionLines => Q("caption").Query<Label>(className: "line").ToList().Count;
        static string CaptionHeader => Plain(Q("caption").Q<Label>(className: "who")?.text);
        static bool Revealed(VisualElement n) => n != null && !n.ClassListContains("pending") && Layer.Alpha(n) > 0.99f;
        static Rect R(string name) => Q(name).worldBound;

        static IEnumerator ToHerWords() => WaitFor(() => Revealed(MessagesCard), 15, "the Messages card");
        static IEnumerator ToTurnBack() => WaitFor(() => Shown(Again), 25, "TURN BACK TIME");

        [UnityTest]
        public IEnumerator Late()
        {
            yield return Load();
            yield return DodgeAll();
            yield return ToHerWords();
            Assert.AreEqual("John", TopWho);
            StringAssert.StartsWith("Funeral's on Saturday.", TopText);
            var voicemail = Q("items").Query(className: "n").ToList().FirstOrDefault(n => n.Q(className: "vm") != null);
            Assert.IsTrue(voicemail != null && Revealed(voicemail), "no voicemail card");
            Assert.AreEqual(CAPTION_HEADERS["voicemail"], CaptionHeader);
            yield return WaitFor(() => CaptionLines == 9, 15, "all nine voicemail lines");
            double last = Time.timeAsDouble;
            yield return ToTurnBack();
            Assert.AreEqual(1.2, Time.timeAsDouble - last, 0.1, "TURN BACK TIME didn't follow her last line by 1.2 s");
            Assert.IsFalse(Shown(Q("restart")), "PLAY AGAIN before TURN BACK TIME");

            Click(Again);
            double click = Time.timeAsDouble;
            yield return null;
            Assert.AreEqual("YOU SAID LATER 29 TIMES.", AgainText);
            yield return WaitFor(() => Again.Q<Label>(className: "soft") != null, 3, "the soft line");
            Assert.AreEqual(1.8, Time.timeAsDouble - click, 0.05);
            Assert.AreEqual("YOU SAID LATER 29 TIMES.She would have taken any one of them.", AgainText);
            yield return WaitFor(() => Again.Q<Label>(className: "lesson") != null, 3, "the lesson");
            Assert.AreEqual(3.6, Time.timeAsDouble - click, 0.05);
            Assert.AreEqual(LESSON, Plain(Again.Q<Label>(className: "lesson").text));
            yield return Wait(6.4 - (Time.timeAsDouble - click));
            Assert.IsFalse(Shown(Q("restart")), "PLAY AGAIN before the lesson has sunk in");
            yield return Wait(0.6);
            Assert.IsTrue(Shown(Q("restart")), "PLAY AGAIN not shown 6.6 s after TURN BACK TIME");
            Rect a = R("again"), c = R("caption");
            Assert.IsTrue(a.yMin >= c.yMax - 1 || a.xMax <= c.xMin || a.xMin >= c.xMax, "the response overlaps her words");
        }

        // CSS line-height, which USS lacks: her lines 19 px × 1.25 + 4 px apart, the reply 26 px × 1.1 tall, the phone's text regular weight
        [UnityTest]
        public IEnumerator Text_IsSpacedAndWeightedLikeTheWeb()
        {
            yield return Load();
            yield return DodgeAll();
            yield return WaitFor(() => CaptionLines == 3, 15, "three of her lines");
            yield return Wait(0.7);
            var lines = Q("caption").Query<Label>(className: "line").ToList();
            float s = Q("world").worldBound.height / (float)H;
            Assert.AreEqual(19 * 1.25 + 4, (lines[1].worldBound.yMin - lines[0].worldBound.yMin) / s, 0.5, "her lines aren't spaced as on the web");
            Assert.AreEqual(19 * 1.25 + 4, (lines[2].worldBound.yMin - lines[1].worldBound.yMin) / s, 0.5);
            Assert.AreEqual("Inter-Regular SDF", Q("sil").resolvedStyle.unityFontDefinition.fontAsset.name);
            Assert.AreEqual("Inter-Regular SDF", MessagesCard.Q<Label>(className: "text").resolvedStyle.unityFontDefinition.fontAsset.name);
            Assert.AreEqual("Inter-SemiBold SDF", MessagesCard.Q<Label>(className: "title").resolvedStyle.unityFontDefinition.fontAsset.name);
            yield return ToTurnBack();
            Click(Again);
            yield return Wait(0.5);
            var loud = Q("loud");
            float lineBox = loud.layout.height + loud.resolvedStyle.marginTop + loud.resolvedStyle.marginBottom; // the space it takes, as CSS lays it out
            Assert.AreEqual(26 * 1.1, lineBox, 1, "the reply's line doesn't take the height it does on the web");
        }

        [UnityTest]
        public IEnumerator Home()
        {
            yield return Load();
            yield return Play(r => Bots.ChaseTarget(r));
            Assert.AreEqual("home", Game.Ending.Key);
            yield return ToHerWords();
            Assert.AreEqual(FINAL["home"].Who, TopWho);
            StringAssert.StartsWith(FINAL["home"].Text, TopText);
            Assert.AreEqual(CAPTION_HEADERS["call"], CaptionHeader);
            Assert.IsFalse(Q("items").Query(className: "vm").ToList().Any(), "a voicemail when she got through");
            yield return ToTurnBack();
            Click(Again);
            yield return Wait(4);
            Assert.AreEqual("THERE'S NEVER ENOUGH TIME.But you were there." + LESSON, AgainText);
        }

        [UnityTest]
        public IEnumerator LastOnly()
        {
            yield return Load();
            yield return PlayLastOnly();
            Assert.AreEqual("last", Game.Ending.Key);
            yield return ToHerWords();
            Assert.AreEqual(FINAL["last"].Who, TopWho);
            StringAssert.StartsWith(FINAL["last"].Text, TopText);
            yield return ToTurnBack();
            Click(Again);
            yield return Wait(4);
            Assert.AreEqual("YOU GAVE HER FOUR MINUTES.She'd waited all month for them." + LESSON, AgainText);
        }

        [UnityTest]
        public IEnumerator Thread()
        {
            yield return Load();
            yield return DodgeAll();
            yield return ToHerWords();
            Click(MessagesCard); Click(MessagesCard);
            yield return Wait(0.5);
            Assert.IsTrue(Shown(Q("sheet")), "the sheet didn't open");
            var cards = Q("sheetList").Query(className: "n").ToList();
            Assert.AreEqual(14, cards.Count);
            Assert.AreEqual("John", Plain(cards[0].Q<Label>(className: "title").text), "newest first");
            Assert.AreEqual("Mom", Plain(cards[13].Q<Label>(className: "title").text));
            Assert.IsTrue(MessagesCard.ClassListContains("clicky"), "no hand cursor over the Messages card");
            Browser.KeyEventsForTests.Enqueue("@close"); // Enter on a focused Show less
            yield return null;
            yield return null;
            Assert.IsFalse(Shown(Q("sheet")), "Enter on Show less didn't close the sheet");
            Click(Q("sheetClose"));
            yield return Wait(0.2);
            Assert.IsFalse(Shown(Q("sheet")), "Show less didn't close the sheet");
            Click(MessagesCard);
            yield return Wait(0.5);
            Assert.IsTrue(Shown(Q("sheet")), "the sheet didn't open again");
        }

        // the sheet appears with the next frame drawn; it must not show fully for a frame before its fade starts
        [UnityTest]
        public IEnumerator MessagesSheet_StartsInvisible_ThenFadesIn()
        {
            yield return Load();
            yield return DodgeAll();
            yield return ToHerWords();
            for (var open = 0; open < 2; open++)
            {
                Click(MessagesCard);
                Assert.AreEqual(0, Layer.Alpha(Q("sheet")), $"the sheet shows before its fade (opening {open + 1})");
                yield return Wait(0.5);
                Assert.IsTrue(Shown(Q("sheet")));
                Click(Q("sheetClose"));
                yield return Wait(0.2);
            }
        }

        // three playthroughs in a row: the second ending is the second round's, with nothing left over from the first
        [UnityTest]
        public IEnumerator SecondEnding_AfterPlayAgain_ShowsTheNewRound()
        {
            yield return Load();
            yield return DodgeAll();
            yield return ToTurnBack();
            Click(Again);
            yield return WaitFor(() => Shown(Q("restart")), 8, "PLAY AGAIN");
            Click(Q("restart"));
            yield return null;
            // this time pick up her first call (one pill), then dodge the rest: 28 missed, and still too late
            yield return Play(r => r.Calls[0].Answered ? Bots.DodgeTarget(r, 340) : Bots.ChaseTarget(r, p => p.Ci == 0));
            Assert.AreEqual(28, Game.Round.Missed);
            Assert.AreEqual("late", Game.Ending.Key);
            yield return ToHerWords();
            Assert.AreEqual("28", Text("gcount"));
            Assert.IsTrue(Shown(Q("group")));
            Assert.AreEqual(2, Q("items").childCount, "cards left over from the first ending");
            yield return ToTurnBack();
            var loud = Q("loud");
            Assert.AreEqual("TURN BACK TIME", Text("loud"));
            Assert.AreEqual(0, loud.resolvedStyle.marginTop, 0.01, "the first answer's spacing is still on TURN BACK TIME");
            Assert.AreEqual(StyleKeyword.Null, loud.style.maxWidth.keyword, "the first answer's width is still on TURN BACK TIME");
            Assert.AreEqual(9, CaptionLines);
            Click(Again);
            yield return Wait(4);
            Assert.AreEqual("YOU SAID LATER 28 TIMES.She would have taken any one of them." + LESSON, AgainText);
        }

        [UnityTest]
        public IEnumerator PendingCard_OpensNothing()
        {
            yield return Load();
            yield return DodgeAll();
            yield return WaitFor(() => MessagesCard != null, 12, "the Messages card, still hidden");
            Assert.IsFalse(Revealed(MessagesCard));
            Assert.AreEqual(UnityEngine.UIElements.PickingMode.Ignore, MessagesCard.pickingMode, "the hidden card takes the pointer (and its hover)");
            Click(MessagesCard);
            yield return Wait(0.3);
            Assert.IsFalse(Shown(Q("sheet")), "a tap on the still-hidden Messages card opened the sheet");
        }

        [UnityTest]
        public IEnumerator DoubleTap_ShowsEveryLineAtOnce()
        {
            yield return Load();
            yield return DodgeAll();
            yield return WaitFor(() => CaptionLines == 2, 15, "her second line");
            var middle = Q("world").worldBound.center;
            Game.PointerDown(1, false, middle); Game.PointerUp(1);
            yield return Wait(0.2);
            Game.PointerDown(1, false, middle); Game.PointerUp(1);
            yield return null;
            Assert.AreEqual(9, CaptionLines, "lines after a double tap");
            double shown = Time.timeAsDouble;
            yield return ToTurnBack();
            Assert.AreEqual(1.2, Time.timeAsDouble - shown, 0.1);
        }

        [UnityTest]
        public IEnumerator Fallback_PlayAgainShows15sAfterTurnBackTime()
        {
            yield return Load();
            yield return DodgeAll();
            yield return ToTurnBack();
            yield return Wait(14.8);
            Assert.IsFalse(Shown(Q("restart")), "PLAY AGAIN showed before the 15 s fallback");
            yield return Wait(1.1); // it fades in over 0.8 s
            Assert.IsTrue(Shown(Q("restart")), "PLAY AGAIN fallback didn't show");
        }

        [UnityTest]
        public IEnumerator TurnBackTwice_ChangesNothing()
        {
            yield return Load();
            yield return DodgeAll();
            yield return ToTurnBack();
            Click(Again); Click(Again);
            yield return Wait(5);
            Assert.AreEqual(1, Again.Query<Label>(className: "soft").ToList().Count, "soft lines");
            Assert.AreEqual("YOU SAID LATER 29 TIMES.She would have taken any one of them." + LESSON, AgainText);
        }

        static void AssertNoOldEnding(string at)
        {
            Assert.AreEqual("play", Game.State, at);
            foreach (var id in new[] { "card", "caption", "again", "restart", "timeov", "sheet" })
                Assert.IsFalse(Shown(Q(id)), $"#{id} still showing {at}");
            Assert.AreEqual(0, Q("caption").Query<Label>().ToList().Count, "her words still there " + at);
            Assert.AreEqual(0, Q("fly").childCount, "a leftover flyer " + at);
        }

        [UnityTest]
        public IEnumerator Restart_AfterTheLesson()
        {
            yield return Load();
            yield return DodgeAll();
            yield return ToTurnBack();
            Click(Again);
            yield return WaitFor(() => Shown(Q("restart")), 8, "PLAY AGAIN");
            Click(Q("restart"));
            yield return null;
            Assert.AreEqual("play", Game.State);
            Assert.That(Game.Round.T, Is.LessThan(1));
            Assert.AreEqual(1, Q("dates").childCount, "the date pile didn't start over");
            var bg = Q("world").resolvedStyle.backgroundColor;
            Assert.AreEqual(253 / 255f, bg.r, 3 / 255f, "the paper isn't warm again");
            foreach (var t in new[] { 2.0, 10, 18 })
            {
                yield return WaitFor(() => { Game.ForcedTarget = Bots.DodgeTarget(Game.Round, 340); return Game.Round.T >= t; }, t + 2, $"{t} s of the new round");
                AssertNoOldEnding($"{t} s into the new round");
                Assert.IsTrue(Shown(Q("me")), "the moon is hidden in the new round");
            }
        }

        [UnityTest]
        public IEnumerator Restart_MidFlight()
        {
            yield return Load();
            yield return DodgeAll();
            yield return Wait(6.6);
            Assert.That(Q("fly").childCount, Is.GreaterThan(0), "not mid-flight");
            Game.StartRound();
            yield return null;
            yield return null;
            Assert.AreEqual(0, Q("fly").childCount);
            Assert.AreEqual(0, Q("focus").resolvedStyle.opacity, 0.001);
            Assert.IsFalse(Shown(Q("lock")));
            Assert.IsTrue(Shown(Q("me")));
            yield return WaitFor(() => { Game.ForcedTarget = Bots.DodgeTarget(Game.Round, 340); return Game.Round.T >= 5; }, 7, "5 s of the new round");
            AssertNoOldEnding("5 s into the new round");
            Assert.AreEqual(0, Game.Round.Hits);
        }

        [UnityTest]
        public IEnumerator WidePhone_EndingLayout()
        {
            yield return Load(new Vector2Int(844, 390));
            yield return DodgeAll();
            yield return ToTurnBack();
            Click(Again);
            yield return WaitFor(() => Shown(Q("restart")), 8, "PLAY AGAIN");
            yield return Wait(1);
            Rect world = R("world"), phone = R("card"), cap = R("caption"), again = R("again"), restart = R("restart");
            Func<Rect, Rect, bool> apart = (a, b) => a.xMax <= b.xMin || b.xMax <= a.xMin || a.yMax <= b.yMin || b.yMax <= a.yMin;
            Assert.AreEqual(world.center.x, phone.center.x, 1, "the phone isn't in the middle of the screen");
            Assert.That(cap.xMin > phone.xMax + 10 && cap.xMax < world.xMax, $"her words ({cap}) aren't beside the phone ({phone})");
            Assert.That(restart.xMax < phone.xMin && restart.xMin > world.xMin, $"PLAY AGAIN ({restart}) isn't left of the phone ({phone})");
            Assert.That(again.xMin > phone.xMax && again.xMax < world.xMax && apart(again, cap), $"the response ({again}) isn't beside the phone, clear of her words ({cap})");
            // the browser check smallphone: on a landscape phone the cards still clear "Notifications silenced"
            Assert.That(R("items").yMin, Is.GreaterThanOrEqualTo(R("sil").yMax - 1), "notifications overlap 'Notifications silenced'");
            Assert.IsTrue(Q("frame").ClassListContains("small"), "a 390 px tall screen didn't get the larger phone text");
        }
    }
}
