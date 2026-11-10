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
    // Ported from the browser checks title, dodge, widths, widephone (play), touchmargin and hidden.
    public class PlayTests
    {
        [TearDown] public void TearDown() => Unload();

        [UnityTest]
        public IEnumerator Title_ShowsLaterAndClickToStart()
        {
            yield return Load();
            Assert.AreEqual("title", Game.State);
            var name = Q("title").Q<Label>(className: "name");
            Assert.AreEqual("LATER", Plain(name.text));
            Assert.IsTrue(Shown(name));
            Assert.AreEqual("CLICK TO START", Text("go"));
            Assert.IsTrue(Shown(Q("go")));
            Assert.IsTrue(Shown(Q("fs")));
            var label = Q("fs").Q<Label>();
            Assert.AreEqual("FULLSCREEN", Plain(label.text));
            Assert.IsTrue(Shown(label));
            Assert.AreEqual("Anton SDF", name.resolvedStyle.unityFontDefinition.fontAsset.name);
            Click(Q("title"));
            yield return null;
            Assert.AreEqual("play", Game.State);
            Assert.IsFalse(Shown(Q("fs")), "the fullscreen button shows during play");
        }

        [UnityTest]
        public IEnumerator CarefulDodger_FinishesUntouched_PaperEndsCold()
        {
            yield return Load();
            yield return Play(r => Bots.DodgeTarget(r, 340));
            Assert.AreEqual(0, Game.Round.Hits);
            Assert.AreEqual(20, Game.Round.T);
            Assert.That(Game.Round.PaperK, Is.GreaterThan(0.95));
            var (r, g, b) = Rules.Paper(Game.Round.PaperK);
            var bg = Q("world").resolvedStyle.backgroundColor;
            Assert.AreEqual(r / 255f, bg.r, 3 / 255f);
            Assert.AreEqual(g / 255f, bg.g, 3 / 255f);
            Assert.AreEqual(b / 255f, bg.b, 3 / 255f);
        }

        [UnityTest]
        public IEnumerator Pills_AreDrawnAtTheFairnessWidths()
        {
            yield return Load();
            Game.StartRound();
            var seen = new System.Collections.Generic.HashSet<string>();
            while (Game.State == "play" && seen.Count < 5)
            {
                Game.ForcedTarget = Bots.DodgeTarget(Game.Round, 340);
                yield return null;
                foreach (var pill in Q("pills").Query(className: "pill").ToList())
                {
                    string who = pill.Q<Label>().text;
                    if (float.IsNaN(pill.resolvedStyle.width)) continue; // spawned this frame, not laid out yet
                    Assert.AreEqual(DEFAULT_PILL_W[who], pill.resolvedStyle.width, 0.01, who);
                    seen.Add(who);
                }
            }
            Assert.AreEqual(5, seen.Count, "not every caller's pill was seen");
        }

        [UnityTest]
        public IEnumerator WidePhone_WorldFillsTheScreen()
        {
            yield return Load(new Vector2Int(844, 390));
            Game.StartRound();
            yield return null;
            Assert.AreEqual(1169, Game.WorldW);
            Assert.AreEqual(844, Q("world").worldBound.width, 1);
            Assert.AreEqual(0, Q("world").worldBound.x, 1);
            yield return WaitFor(() => { Game.ForcedTarget = new Vec(Game.Round.W - 60, 270); return Game.Round.T >= 4; }, 6, "4 s of play");
            Assert.That(Game.Round.Me.X, Is.GreaterThan(1100));
        }

        [UnityTest]
        public IEnumerator TouchInTheSideBar_MovesTheMoon()
        {
            yield return Load(new Vector2Int(2000, 627));
            Game.StartRound();
            yield return null;
            var world = Q("world").worldBound;
            Assert.That(world.x, Is.GreaterThan(50), "no bars beside the stage");
            var down = new Vector2(world.x / 2, 300);
            double x0 = Game.Round.Me.X;
            Game.PointerDown(7, true, down);
            Game.PointerMove(7, true, down + new Vector2(80, 0));
            yield return Wait(1);
            Assert.That(Game.Round.Me.X - x0, Is.GreaterThan(40));
            Game.PointerUp(7);
        }

        [UnityTest]
        public IEnumerator UprightPhone_PausesPlay()
        {
            yield return Load();
            Game.StartRound();
            yield return null;
            Browser.UprightOverride = true;
            yield return null;
            double t = Game.Round.T;
            yield return Wait(1);
            Assert.AreEqual(t, Game.Round.T, "play went on with the phone upright");
            Assert.IsTrue(Shown(Q("rotate")));
            Browser.UprightOverride = false;
            yield return Wait(1);
            Assert.That(Game.Round.T, Is.GreaterThan(t + 0.9));
            Assert.IsFalse(Shown(Q("rotate")));
        }
    
        // the player switches apps mid-drag, so the finger's lift never arrives: the next finger must still steer
        [UnityTest]
        public IEnumerator AFingerLostToAnotherApp_DoesntFreezeTouch()
        {
            yield return Load();
            Game.StartRound();
            yield return null;
            var p = Q("world").worldBound.center;
            Game.PointerDown(1, true, p);
            Game.SendMessage("OnApplicationFocus", false, SendMessageOptions.DontRequireReceiver);
            Game.SendMessage("OnApplicationFocus", true, SendMessageOptions.DontRequireReceiver);
            double x0 = Game.Round.Me.X;
            Game.PointerDown(2, true, p);
            Game.PointerMove(2, true, p + new Vector2(80, 0));
            yield return Wait(1);
            Assert.That(Game.Round.Me.X - x0, Is.GreaterThan(40), "touch stayed locked to the lost finger");
        }
    }
}
