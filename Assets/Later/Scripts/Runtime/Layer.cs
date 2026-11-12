using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Later
{
    // CSS fades an element and everything in it as one layer; UI Toolkit's opacity fades each part on its own, so
    // overlapping parts show through each other mid-fade. An opacity filter draws the whole element into a texture
    // first, like CSS, but at one texel per world unit: right where the world is drawn 1:1, a third of the screen's
    // resolution on a 3× phone. Drawing the content larger to make up for it gets it clipped by the phone's and the
    // stage's edges at its enlarged size. So: one layer (the filter) where the world is drawn at 1×, and each part on
    // its own, its text crisp, where it's drawn larger (there the overlap shows by a few levels of grey at most).
    public static class Layer
    {
        // how many screen pixels a world unit covers, rounded up; LaterGame sets it
        public static int K = 1;

        public static int KFor(double worldScale) => Mathf.Clamp(Mathf.CeilToInt((float)worldScale - 0.01f), 1, 4);

        // An opacity filter draws an element's own rounded clipping (overflow: hidden with corners) as a solid slab, so
        // a fading inner element lets go of its clip until it's in (the lock screen's body: nothing reaches its corners
        // while it fades). Its own inline overflow, to put back.
        static readonly Dictionary<VisualElement, StyleEnum<Overflow>> clipsLifted = new Dictionary<VisualElement, StyleEnum<Overflow>>();

        // Draws outer (and everything in it) at opacity a. `inner` fills outer and carries its looks; `scale` is outer's
        // own animated scale; `clipped` says inner clips its content with rounded corners in the stylesheet (an inline
        // clip is seen anyway).
        public static void Opacity(VisualElement outer, VisualElement inner, float a, float scale = 1, bool clipped = false)
        {
            outer.style.scale = scale == 1 ? (StyleScale)StyleKeyword.Null : new Scale(Vector2.one * scale);
            if (a >= 1 || K > 1)
            {
                Release(inner);
                outer.style.filter = StyleKeyword.Null;
                outer.style.opacity = Mathf.Clamp01(a);
                return;
            }
            outer.style.opacity = 1;
            var own = inner.style.overflow;
            bool clips = clipped || (own.keyword == StyleKeyword.Undefined && own.value == Overflow.Hidden);
            if (clips && !clipsLifted.ContainsKey(inner)) { clipsLifted[inner] = own; inner.style.overflow = Overflow.Visible; }
            var f = new FilterFunction(FilterFunctionType.Opacity);
            f.AddParameter(new FilterParameter(Mathf.Max(0, a)));
            outer.style.filter = new StyleList<FilterFunction>(new List<FilterFunction> { f });
        }

        // gives a fading inner element its clip back (the fade is over, or the ending is cleared mid-fade)
        public static void Release(VisualElement inner)
        {
            if (clipsLifted.TryGetValue(inner, out var clip)) { inner.style.overflow = clip; clipsLifted.Remove(inner); }
        }

        // how opaque a layer is drawn: its opacity filter if it's fading through one, times its opacity
        public static float Alpha(VisualElement outer)
        {
            float own = outer.style.opacity.keyword == StyleKeyword.Undefined ? outer.style.opacity.value : outer.resolvedStyle.opacity;
            var filter = outer.style.filter;
            if (filter.keyword == StyleKeyword.Undefined && filter.value != null)
                foreach (var f in filter.value)
                    if (f.type == FilterFunctionType.Opacity) return f.GetParameter(0).floatValue * own;
            return own;
        }

        // a blur of `px` world units, or no filter at all once it's sharp (a filter is drawn at the world's resolution)
        public static StyleList<FilterFunction> Blur(double px)
        {
            if (px <= 0) return StyleKeyword.Null;
            var blur = new FilterFunction(FilterFunctionType.Blur);
            blur.AddParameter(new FilterParameter((float)px));
            return new StyleList<FilterFunction>(new List<FilterFunction> { blur });
        }
    }
}
