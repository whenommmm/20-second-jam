using System;
using System.Collections.Generic;

namespace Later
{
    // A CSS box-shadow with no blur (box-shadow: 0 dy 0 spread) under a rounded card w × h with corner radius r: the
    // card's shape moved down by dy and grown by spread (negative: shrunk, and its radius with it), seen only where the
    // card isn't. Sampled in columns `step` wide: each the shadow's visible span there (it only ever shows below the card).
    public static class CardShadow
    {
        public static List<(float X, float Top, float Bottom)> Spans(float w, float h, float r, float dy, float spread, float step)
        {
            float sx = -spread, sy = dy - spread, sw = w + 2 * spread, sh = h + 2 * spread, sr = Math.Max(0, r + spread);
            var spans = new List<(float, float, float)>();
            for (float x = sx; x <= sx + sw + 1e-4f; x += step)
            {
                var shadow = Extent(x, sx, sy, sw, sh, sr);
                var card = Extent(x, 0, 0, w, h, r);
                if (shadow == null) continue;
                float top = card == null ? shadow.Value.Top : Math.Max(shadow.Value.Top, card.Value.Bottom);
                if (shadow.Value.Bottom > top + 1e-4f) spans.Add((x, top, shadow.Value.Bottom));
            }
            return spans;
        }

        // the vertical extent of a rounded rectangle at x
        static (float Top, float Bottom)? Extent(float x, float x0, float y0, float w, float h, float r)
        {
            if (x < x0 - 1e-4f || x > x0 + w + 1e-4f) return null;
            float cx = x < x0 + r ? x0 + r : x > x0 + w - r ? x0 + w - r : x;
            float d = r * r - (x - cx) * (x - cx);
            float dyc = d > 0 ? (float)Math.Sqrt(d) : 0;
            return (y0 + r - dyc, y0 + h - r + dyc);
        }
    }
}
