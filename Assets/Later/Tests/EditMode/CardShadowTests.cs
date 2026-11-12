using System.Linq;
using NUnit.Framework;

namespace Later.Tests
{
    // The slivers under the stacked Messages card are the web's two box-shadows (0 6px 0 -3px, 0 11px 0 -6px): each the
    // card's rounded shape moved down and shrunk, seen only where it isn't behind the card.
    public class CardShadowTests
    {
        const float W = 230, H = 60, R = 15;

        static (float Top, float Bottom)? At(float x, float dy, float spread)
        {
            var spans = CardShadow.Spans(W, H, R, dy, spread, 0.25f);
            var hit = spans.Where(s => System.Math.Abs(s.X - x) < 0.13f).ToList();
            return hit.Count == 0 ? ((float, float)?)null : (hit[0].Top, hit[0].Bottom);
        }

        [Test]
        public void UnderTheMiddle_TheFirstShowsThreePixels_TheSecondFive()
        {
            var a = At(W / 2, 6, -3).Value;
            Assert.AreEqual(H, a.Top, 1e-3); Assert.AreEqual(H + 3, a.Bottom, 1e-3);
            var b = At(W / 2, 11, -6).Value;
            Assert.AreEqual(H, b.Top, 1e-3); Assert.AreEqual(H + 5, b.Bottom, 1e-3);
        }

        // where the card's corners curve away, the shadow's smaller curve shows beside them: a thin crescent
        [Test]
        public void BesideTheCorners_ThinCrescentsShow()
        {
            var c = At(5, 6, -3).Value;
            Assert.AreEqual(H - 15 + System.Math.Sqrt(15 * 15 - 10 * 10), c.Top, 0.01);
            Assert.AreEqual(H + 3 - 12 + System.Math.Sqrt(12 * 12 - 10 * 10), c.Bottom, 0.01);
            Assert.IsNull(At(3.5f, 6, -3), "the shadow shows outside its corner");
        }
    }
}
