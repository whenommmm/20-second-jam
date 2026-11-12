using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Later
{
    // UI Toolkit rounds a label's font size to whole pixels (10.5 px is set as 11, 5% wider; 10.4 as 10), where the
    // browser sets text at exactly its size. Rich text's <size> isn't rounded, so a label whose size has a fraction
    // carries it in its text. The phone's text is 9.5, 10.5, 11.6 and 12.5 px on the web.
    public static class ExactSize
    {
        sealed class Kept { public string Raw; public float Size = -1; }

        static readonly Dictionary<Label, Kept> kept = new Dictionary<Label, Kept>();

        // `text` set at exactly `size` px
        public static string Wrap(string text, float size) =>
            Mathf.Approximately(size, Mathf.Round(size)) ? text
                : string.Format(CultureInfo.InvariantCulture, "<size={0:0.###}px>{1}</size>", size, text);

        // keeps the label's text at its exact size from now on, whatever size its styles give it (the small-screen style
        // changes it); change its text with SetText
        public static void Keep(Label label)
        {
            if (kept.ContainsKey(label)) return;
            var k = kept[label] = new Kept { Raw = label.text };
            label.RegisterCallback<GeometryChangedEvent>(e => Apply(label, k, false));
            label.RegisterCallback<DetachFromPanelEvent>(e => kept.Remove(label));
            Apply(label, k, false);
        }

        public static void SetText(Label label, string text)
        {
            if (!kept.TryGetValue(label, out var k)) { label.text = text; return; }
            k.Raw = text;
            Apply(label, k, true);
        }

        static void Apply(Label label, Kept k, bool force)
        {
            if (label.panel == null) { label.text = k.Raw; return; }
            float size = label.resolvedStyle.fontSize;
            if (!force && size == k.Size) return;
            k.Size = size;
            label.text = Wrap(k.Raw, size);
        }
    }
}
