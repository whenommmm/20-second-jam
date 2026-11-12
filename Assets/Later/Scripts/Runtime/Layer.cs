using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Later
{
    // CSS fades an element and everything in it as one layer; UI Toolkit's opacity fades each part on its own, so
    // overlapping parts show through each other mid-fade. An opacity filter draws the whole element into a texture
    // first, like CSS. UI Toolkit draws that texture at one texel per local unit, which the world's scale (2–3× on phones
    // and retina screens) would blow up soft, so the content is drawn K× larger and shrunk back: `outer` carries the
    // filter and 1/K, `inner` (which fills it and has all its looks) K.
    public static class Layer
    {
        // how much larger to draw filtered content: the world's scale (canvas px per world unit), rounded up; LaterGame sets it
        public static int K = 1;

        public static int KFor(double worldScale) => Mathf.Clamp(Mathf.CeilToInt((float)worldScale - 0.01f), 1, 4);

        // An opacity filter draws an element's own rounded clipping (overflow: hidden with corners) as a solid slab, so
        // a fading inner element lets go of its clip until it's in (the lock screen's body: nothing reaches its corners
        // while it fades). Its own inline overflow, to put back.
        static readonly Dictionary<VisualElement, StyleEnum<Overflow>> clipsLifted = new Dictionary<VisualElement, StyleEnum<Overflow>>();

        // Draws outer (and everything in it) at opacity a, as one layer. `scale` is outer's own animated scale; `clipped`
        // says inner clips its content with rounded corners in the stylesheet (an inline clip is seen anyway).
        public static void Opacity(VisualElement outer, VisualElement inner, float a, float scale = 1, bool clipped = false)
        {
            outer.style.opacity = 1;
            if (a >= 1)
            {
                if (clipsLifted.TryGetValue(inner, out var clip)) { inner.style.overflow = clip; clipsLifted.Remove(inner); }
                outer.style.filter = StyleKeyword.Null;
                inner.style.scale = StyleKeyword.Null;
                outer.style.scale = scale == 1 ? (StyleScale)StyleKeyword.Null : new Scale(Vector2.one * scale);
                return;
            }
            var own = inner.style.overflow;
            bool clips = clipped || (own.keyword == StyleKeyword.Undefined && own.value == Overflow.Hidden);
            if (clips && !clipsLifted.ContainsKey(inner)) { clipsLifted[inner] = own; inner.style.overflow = Overflow.Visible; }
            var f = new FilterFunction(FilterFunctionType.Opacity);
            f.AddParameter(new FilterParameter(Mathf.Max(0, a)));
            outer.style.filter = new StyleList<FilterFunction>(new List<FilterFunction> { f });
            outer.style.scale = new Scale(Vector2.one * (scale / K));
            inner.style.scale = new Scale(Vector2.one * K);
        }

        // gives a fading inner element its clip back (the ending is cleared mid-fade)
        public static void Release(VisualElement inner)
        {
            if (clipsLifted.TryGetValue(inner, out var clip)) { inner.style.overflow = clip; clipsLifted.Remove(inner); }
        }

        // how opaque a layer is drawn: its opacity filter if it's fading, else its opacity
        public static float Alpha(VisualElement outer)
        {
            var filter = outer.style.filter;
            if (filter.keyword == StyleKeyword.Undefined && filter.value != null)
                foreach (var f in filter.value)
                    if (f.type == FilterFunctionType.Opacity) return f.GetParameter(0).floatValue * outer.resolvedStyle.opacity;
            return outer.style.opacity.keyword == StyleKeyword.Undefined ? outer.style.opacity.value : outer.resolvedStyle.opacity;
        }

        // a blur of `px` world units on content drawn k× larger: blur(px × k), or no filter at all once it's sharp
        public static StyleList<FilterFunction> Blur(double px, int k)
        {
            if (px <= 0) return StyleKeyword.Null;
            var blur = new FilterFunction(FilterFunctionType.Blur);
            blur.AddParameter(new FilterParameter((float)(px * k)));
            return new StyleList<FilterFunction>(new List<FilterFunction> { blur });
        }
    }
}
