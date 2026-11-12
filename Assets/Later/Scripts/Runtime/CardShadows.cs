using UnityEngine;
using UnityEngine.UIElements;

namespace Later
{
    // The stacked Messages card's slivers, as the web draws them: box-shadow: 0 6px 0 -3px rgba(255,255,255,.08),
    // 0 11px 0 -6px rgba(255,255,255,.05), on a card with 15 px corners. Fills its parent, draws below it.
    public sealed class CardShadows : VisualElement
    {
        static readonly (float Dy, float Spread, Color Colour)[] Shadows =
        {
            (11, -6, new Color(1, 1, 1, 0.05f)), // the web's second shadow, drawn first (CSS paints the first on top)
            (6, -3, new Color(1, 1, 1, 0.08f))
        };

        public CardShadows()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0; style.top = 0; style.right = 0; style.bottom = 0;
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(e => MarkDirtyRepaint());
        }

        void Draw(MeshGenerationContext mgc)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w <= 0 || h <= 0) return;
            var p = mgc.painter2D;
            foreach (var (dy, spread, colour) in Shadows)
            {
                var spans = CardShadow.Spans(w, h, 15, dy, spread, 0.25f);
                p.fillColor = colour;
                // each run of neighbouring columns is one shape: along the tops, then back along the bottoms
                for (int i = 0; i < spans.Count;)
                {
                    int j = i;
                    while (j + 1 < spans.Count && spans[j + 1].X - spans[j].X < 0.3f) j++;
                    p.BeginPath();
                    p.MoveTo(new Vector2(spans[i].X, spans[i].Top));
                    for (int k = i + 1; k <= j; k++) p.LineTo(new Vector2(spans[k].X, spans[k].Top));
                    for (int k = j; k >= i; k--) p.LineTo(new Vector2(spans[k].X, spans[k].Bottom));
                    p.ClosePath();
                    p.Fill();
                    i = j + 1;
                }
            }
        }
    }
}
