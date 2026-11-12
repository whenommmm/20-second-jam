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

        [UnityTest]
        public IEnumerator BlurredNames_AreDrawnAtTheScreensResolution_AndUnblurredWhenSharp()
        {
            yield return Load(new Vector2Int(2880, 1620)); // the world drawn 3×
            Assert.AreEqual(3, Layer.K);
            Game.StartRound();
            yield return WaitFor(() => Q("pills").childCount > 0, 5, "a pill");
            yield return null;
            var name = Q("pills")[0].Q<Label>();
            Assert.AreEqual(48, name.resolvedStyle.fontSize, 0.01, "the name isn't drawn 3× larger");
            Assert.AreEqual(1 / 3f, name.parent.resolvedStyle.scale.value.x, 1e-4, "the name isn't shrunk back");
            var blur = name.resolvedStyle.filter.ToList();
            Assert.AreEqual(1, blur.Count);
            double nb = System.Math.Round((1 - Rules.Clarity(Game.Round.T, Game.Round.Bonus)) * 8, 2);
            Assert.AreEqual(nb * 3, blur[0].GetParameter(0).floatValue, 0.05, "the blur isn't scaled with the name");
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
            Layer.K = 2;
            Layer.Opacity(outer, inner, 0.4f, 1.1f);
            Assert.AreEqual(0.4f, Layer.Alpha(outer), 1e-5);
            Assert.AreEqual(1.1f / 2, outer.style.scale.value.value.x, 1e-5);
            Assert.AreEqual(2, inner.style.scale.value.value.x, 1e-5);
            Layer.Opacity(outer, inner, 1);
            var filter = outer.style.filter;
            Assert.IsTrue(filter.value == null || filter.value.Count == 0, "the filter stays on an opaque layer");
            Assert.AreEqual(new VisualElement().style.scale, inner.style.scale);
            Assert.AreEqual(1, Layer.Alpha(outer));
            Layer.K = 1;
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
    }
}
