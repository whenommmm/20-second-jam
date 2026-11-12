using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Later.Tests.LaterTestUtil;

namespace Later.Tests
{
    // The page's own bits the web version gets from CSS and HTML buttons: the cursor, the fullscreen button the page
    // handles itself (a real click, the whole page), and keyboard focus.
    public class ChromeTests
    {
        [TearDown] public void TearDown() => Unload();

        // a hand over what can be clicked; hidden only over the stage during play, as #stage.playing { cursor: none }
        [UnityTest]
        public IEnumerator TheCursor_IsAHandOverButtons_AndHiddenOnlyOverTheStageInPlay()
        {
            yield return Load(new Vector2Int(2000, 627));
            Game.PointerMove(0, false, Q("fs").worldBound.center);
            yield return null;
            Assert.AreEqual("pointer", Browser.Cursor, "over FULLSCREEN");
            Game.PointerMove(0, false, Q("world").worldBound.center);
            yield return null;
            Assert.AreEqual("", Browser.Cursor, "over the title");
            Game.StartRound();
            yield return null;
            Assert.AreEqual("none", Browser.Cursor, "over the stage in play");
            Game.PointerMove(0, false, new Vector2(Q("world").worldBound.x / 2, 300));
            yield return null;
            Assert.AreEqual("", Browser.Cursor, "beside the stage in play");
        }

        // the page toggles fullscreen itself on a real click of the button, so it tells the page where the button is
        [UnityTest]
        public IEnumerator TheFullscreenButton_IsReportedToThePage_OnlyWhileItShows()
        {
            yield return Load();
            yield return null;
            var fs = Q("fs").worldBound;
            Assert.AreEqual(fs, Browser.FullscreenButton, "on the title");
            Game.StartRound();
            yield return null;
            yield return null;
            Assert.AreEqual(Rect.zero, Browser.FullscreenButton, "during play");
        }

        // Tab (handled by the page) puts a focus ring on the button that has focus
        [UnityTest]
        public IEnumerator KeyboardFocus_DrawsARing()
        {
            yield return Load();
            var ring = Q("fs").Q(className: "ring");
            Assert.IsFalse(Shown(ring));
            Browser.FocusForTests = "fs";
            yield return null; // the game sets the class in Update; styles resolve later in the frame
            yield return null;
            Assert.IsTrue(Shown(ring), "no ring on the focused FULLSCREEN");
            Browser.FocusForTests = "";
            yield return null;
            yield return null;
            Assert.IsFalse(Shown(ring));
        }
    }
}
