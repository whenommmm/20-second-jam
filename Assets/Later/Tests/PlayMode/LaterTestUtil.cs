using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Later.Tests
{
    // Loads the game scene and drives it in game time (a fixed 1/60 s per frame, as fast as the editor can go).
    public static class LaterTestUtil
    {
        public static LaterGame Game => LaterGame.Instance;
        public static VisualElement Root => LaterGame.Instance.Root;
        public static VisualElement Q(string name) => Root.Q(name) ?? throw new AssertionException($"no element #{name}");

        // Pretends the screen is this size (pixels); 960×540 unless a test says otherwise.
        public static IEnumerator Load(Vector2Int? screen = null)
        {
            LaterGame.ScreenOverride = screen ?? new Vector2Int(960, 540);
            Browser.UprightOverride = false;
            Time.timeScale = 1;
            Time.captureDeltaTime = 1 / 60f;
            SceneManager.LoadScene("Later");
            yield return null;
            yield return null;
            Assert.IsNotNull(LaterGame.Instance, "the scene has no LaterGame");
        }

        public static void Unload()
        {
            Time.captureDeltaTime = 0;
            LaterGame.ScreenOverride = null;
            Browser.UprightOverride = null;
            Browser.KeyEventsForTests.Clear();
            Browser.FocusForTests = "";
            Browser.FrameForTests = null;
            Browser.PillWidthsForTests = null;
            Browser.ResumedForTests = false;
            LaterGame.WallSecondsForTests = null;
            Browser.BalanceForTests = true;
            Browser.NumberFormatForTests = null;
        }

        public static IEnumerator Wait(double gameSeconds)
        {
            double end = Time.timeAsDouble + gameSeconds;
            while (Time.timeAsDouble < end - 1e-6) yield return null;
        }

        public static IEnumerator WaitFor(Func<bool> cond, double maxGameSeconds, string what)
        {
            double end = Time.timeAsDouble + maxGameSeconds;
            while (!cond())
            {
                if (Time.timeAsDouble > end) Assert.Fail($"timed out after {maxGameSeconds} s waiting for: {what}");
                yield return null;
            }
        }

        // Starts a round and steers the moon with a test player every frame until TIME!.
        public static IEnumerator Play(Func<Round, Vec> bot)
        {
            Game.StartRound();
            while (Game.State == "play")
            {
                Game.ForcedTarget = bot(Game.Round);
                yield return null;
            }
        }

        public static void Click(VisualElement el)
        {
            using var e = ClickEvent.GetPooled();
            e.target = el;
            el.SendEvent(e);
        }

        // The element and every parent are displayed, visible and mostly opaque (a layer fading by its filter counts as it's drawn).
        public static bool Shown(VisualElement el)
        {
            for (var e = el; e != null; e = e.parent)
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden || Layer.Alpha(e) <= 0.5f)
                    return false;
            return true;
        }

        // What a text element reads, without its rich-text tags (letter-spaced text carries <cspace>).
        public static string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text ?? "", "<[^>]+>|\u200B", "");
        public static string Text(string name) => Plain(((TextElement)Q(name)).text);
    }
}
