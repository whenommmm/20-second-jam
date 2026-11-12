using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Later.Tests.LaterTestUtil;

namespace Later.Tests
{
    // UI Toolkit draws a filtered element into a texture at one texel per local unit, so under the world's scale (2–3×
    // on phones and retina screens) blurred names and fades would come out soft. Filtered content is drawn K× larger
    // and shrunk back; and a fade is one opacity filter over the whole element, as CSS opacity fades it as one layer.
    public class LayerTests
    {
        [TearDown] public void TearDown() => Unload();

        // the names' blur is a filter, which UI Toolkit draws at the world's own resolution: once the names are sharp the
        // filter comes off, so on a 2–3× screen they're as crisp as the web's (drawing them larger instead would have the
        // stage's edge clip them at their enlarged size)
        [UnityTest]
        public IEnumerator BlurredNames_LoseTheirFilterOnceSharp()
        {
            yield return Load(new Vector2Int(2880, 1620)); // the world drawn 3×
            Game.StartRound();
            yield return WaitFor(() => Q("pills").childCount > 0, 5, "a pill");
            yield return null;
            var name = Q("pills")[0].Q<Label>();
            Assert.AreEqual(16, name.resolvedStyle.fontSize, 0.01);
            var blur = name.resolvedStyle.filter.ToList();
            Assert.AreEqual(1, blur.Count);
            double nb = System.Math.Round((1 - Rules.Clarity(Game.Round.T, Game.Round.Bonus)) * 8, 2);
            Assert.AreEqual(nb, blur[0].GetParameter(0).floatValue, 0.05);
            yield return WaitFor(() => Game.Round.T > 16.2 && Q("pills").childCount > 0, 20, "sharp names on a pill");
            yield return null;
            Assert.AreEqual(0, Q("pills")[0].Q<Label>().resolvedStyle.filter.Count(), "a sharp name still goes through a filter");
        }

        [Test]
        public void AFade_IsOneOpacityFilterOverTheWholeElement_ClearedWhenOpaque()
        {
            var outer = new VisualElement();
            var inner = new VisualElement();
            outer.Add(inner);
            Layer.K = 1;
            Layer.Opacity(outer, inner, 0.4f, 1.1f);
            Assert.AreEqual(0.4f, Layer.Alpha(outer), 1e-5);
            Assert.AreEqual(1.1f, outer.style.scale.value.value.x, 1e-5);
            Layer.Opacity(outer, inner, 1);
            var filter = outer.style.filter;
            Assert.IsTrue(filter.value == null || filter.value.Count == 0, "the filter stays on an opaque layer");
            Assert.AreEqual(1, Layer.Alpha(outer));
        }

        // where the world is drawn 2–3×, a filter would draw the fading text at a third of the screen's resolution (or,
        // drawn larger, be clipped by the phone's edges), so there each part fades on its own and the text stays crisp
        [Test]
        public void OnA2xScreen_AFadeIsPlainOpacity()
        {
            var outer = new VisualElement();
            var inner = new VisualElement();
            outer.Add(inner);
            Layer.K = 2;
            Layer.Opacity(outer, inner, 0.4f, 1.1f);
            var filter = outer.style.filter;
            Assert.IsTrue(filter.value == null || filter.value.Count == 0, "a filter on a 2× screen");
            Assert.AreEqual(0.4f, outer.style.opacity.value, 1e-5);
            Assert.AreEqual(1.1f, outer.style.scale.value.value.x, 1e-5);
            Assert.AreEqual(0.4f, Layer.Alpha(outer), 1e-5);
            Layer.K = 1;
        }

        // an opacity filter draws rounded clipping (overflow: hidden with corners) as a solid slab, so the lock screen's
        // body stops clipping while it fades (nothing on it reaches its corners then) and clips again once it's in
        [Test]
        public void AFadingLayer_LetsGoOfItsRoundedClip_UntilItsIn()
        {
            var outer = new VisualElement();
            var inner = new VisualElement();
            inner.style.overflow = Overflow.Hidden;
            outer.Add(inner);
            Layer.K = 1;
            Layer.Opacity(outer, inner, 0.3f);
            Assert.AreEqual(Overflow.Visible, inner.style.overflow.value, "the clip stays on through the filter");
            Layer.Opacity(outer, inner, 1);
            Assert.AreEqual(Overflow.Hidden, inner.style.overflow.value, "the clip didn't come back");
        }

        // while the Messages card fades in (drawn K× inside its layer), its slivers stay under it and all of it takes taps
        [UnityTest]
        public IEnumerator TheMessagesCard_IsWholeWhileItFadesIn()
        {
            yield return Load(new Vector2Int(1920, 1080)); // the world drawn 2×
            yield return Play(r => Bots.DodgeTarget(r, 340));
            VisualElement card = null;
            yield return WaitFor(() => (card = Q("items").Query(className: "tap").ToList().FirstOrDefault()) != null
                && !card.ClassListContains("pending") && Layer.Alpha(card) < 0.9f, 15, "the Messages card mid-reveal");
            yield return null;
            var shadows = card.Q<CardShadows>();
            Assert.AreEqual(card.Q(className: "card"), shadows.parent, "the slivers aren't drawn with the card");
            var box = card.Q(className: "card").worldBound;
            var picked = card.panel.Pick(new Vector2(box.xMin + 6, box.yMin + 6));
            Assert.IsTrue(picked != null && card.Contains(picked), "a tap near the card's corner misses it mid-reveal");
        }

        // the Messages sheet opens as one layer: the cards don't show through as patches mid-fade
        [UnityTest]
        public IEnumerator TheMessagesSheet_FadesInAsOneLayer()
        {
            yield return Load();
            yield return Play(r => Bots.DodgeTarget(r, 340));
            yield return WaitFor(() => Q("items").Query(className: "tap").ToList().Any(n => !n.ClassListContains("pending")), 15, "the Messages card");
            yield return Wait(0.5);
            Click(Q("items").Query(className: "tap").First());
            yield return Wait(0.1);
            float a = Layer.Alpha(Q("sheet"));
            Assert.That(a, Is.GreaterThan(0).And.LessThan(1), "the sheet isn't mid-fade as one layer");
            Assert.AreEqual(1, Q("sheet").resolvedStyle.opacity, "the sheet's own opacity fades it part by part");
            yield return Wait(0.3);
            Assert.AreEqual(1, Layer.Alpha(Q("sheet")));
            Assert.AreEqual(0, Q("sheet").resolvedStyle.filter.Count(), "the opened sheet still goes through a filter");
            Assert.AreEqual(1, Q("sheet").resolvedStyle.scale.value.x, 1e-5);
        }

        // her words in Newsreader at the optical size the browser uses at 19 px: as wide as the web sets them
        [UnityTest]
        public IEnumerator HerWords_AreAsWideAsTheWebSetsThem()
        {
            yield return Load();
            var line = new Label("I'm a bit tired, so I'll keep it short.");
            line.AddToClassList("line");
            Q("caption").Add(line);
            Q("caption").AddToClassList("on");
            yield return null;
            yield return null;
            float w = line.MeasureTextSize(line.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            Assert.AreEqual(239, w, 0.01, "the web measures 238.34 px (Unity measures text to the next whole pixel)");
        }

        // CSS letter-spacing runs on past the last letter: PLAY AGAIN's underline is as long as the web's
        [UnityTest]
        public IEnumerator LetterSpacing_RunsPastTheLastLetter_LikeCss()
        {
            yield return Load();
            Q("restartTap").AddToClassList("on");
            yield return null;
            yield return null;
            Assert.AreEqual(102, Q("restart").resolvedStyle.width, 0.01, "the web's PLAY AGAIN is 101.44 px wide (to the next whole pixel)");
        }

        // Show less is as tall as the web's button: 10.5 px Inter's normal line (1.21) plus 5 px above and below
        [UnityTest]
        public IEnumerator ShowLess_IsAsTallAsTheWebsButton()
        {
            yield return Load();
            foreach (var id in new[] { "card", "lock", "sheet" }) Q(id).style.display = DisplayStyle.Flex;
            Q("lock").style.opacity = 1;
            yield return null;
            yield return null;
            Assert.AreEqual(22.71, Q("sheetClose").Q<Label>().resolvedStyle.height, 0.3, "layout snaps to whole pixels at this scale");
        }

        // the score as the browser writes numbers (toLocaleString), unless the font can't draw that: then 0–9 and commas
        [UnityTest]
        public IEnumerator TheScore_IsWrittenTheBrowsersWay_WhereTheFontCanDrawIt()
        {
            yield return Load();
            var score = Q("score") as Label;
            Browser.NumberFormatForTests = n => n.ToString("#,0", new System.Globalization.CultureInfo("de-DE"));
            Assert.AreEqual("-1.400", PlayView.Format(score, -1400));
            Browser.NumberFormatForTests = n => "\u0667\u0662\u0665"; // ٧٢٥, Arabic-Indic digits
            Assert.AreEqual("725", PlayView.Format(score, 725));
        }

        // positions and angles to a tenth, as the web writes them (toFixed(1))
        [UnityTest]
        public IEnumerator Pills_AreDrawnToATenthOfAUnit()
        {
            yield return Load();
            Game.StartRound();
            yield return WaitFor(() => Q("pills").childCount > 0, 5, "a pill");
            yield return null;
            var t = Q("pills")[0].style.translate.value;
            Assert.AreEqual(System.Math.Round(t.x.value * 10), t.x.value * 10, 1e-3);
            Assert.AreEqual(System.Math.Round(t.y.value * 10), t.y.value * 10, 1e-3);
            float a = Q("pills")[0].style.rotate.value.angle.value;
            Assert.AreEqual(System.Math.Round(a * 10), a * 10, 1e-3);
        }

        // UI Toolkit rounds a label's font size to whole pixels (10.5 px is set as 11, 5% wider); the phone's half-pixel
        // sizes are kept exact, so its text is as wide as the web's and wraps where the web's does
        [UnityTest]
        public IEnumerator HalfPixelFontSizes_AreKeptExact()
        {
            yield return Load();
            var line = new Label("They're saying weeks, maybe less.");
            line.style.unityFontDefinition = Q("sil").resolvedStyle.unityFontDefinition;
            line.style.fontSize = 10.5f;
            Game.Root.Add(line);
            ExactSize.Keep(line);
            yield return null;
            yield return null;
            float w = line.MeasureTextSize(line.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            Assert.AreEqual(174, w, 0.01, "the web sets it 173.23 px wide, kerned (to the next whole pixel); rounded to 11 px it was 182");
            StringAssert.Contains("They're saying weeks, maybe less.", Plain(line.text));
            ExactSize.SetText(line, "Funeral's on Saturday.");
            Assert.AreEqual("Funeral's on Saturday.", Plain(line.text));
        }
    }
}
