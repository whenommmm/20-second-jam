using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Later.LaterData;

namespace Later
{
    // Draws a round: pills, moon, badge, score, streak, the date pile, the paper, blurred names, shake.
    // Ported from web/later/src/view.js. Sizes and positions are world units (540 tall, 960 or more wide).
    public sealed class PlayView
    {
        readonly VisualElement world, play, pills, dates, pops, me, mewrap, mesvg;
        readonly Label score, combo, badge;
        readonly Sound sound;
        readonly Timeline tl = new Timeline();
        readonly Dictionary<int, VisualElement> pillEls = new Dictionary<int, VisualElement>();
        readonly System.Random random = new System.Random();
        double shakeUntil, nb = -1;
        int? shownScore;

        public PlayView(VisualElement world, Sound sound)
        {
            this.world = world; this.sound = sound;
            play = world.Q("play"); pills = world.Q("pills"); dates = world.Q("dates"); pops = world.Q("pops");
            me = world.Q("me"); mewrap = world.Q("mewrap"); mesvg = world.Q("mesvg");
            score = world.Q<Label>("score"); combo = world.Q<Label>("combo"); badge = world.Q<Label>("badge");
        }

        // The view's own clock: pops, slips and fading pills keep animating after TIME!.
        public void Tick(double dt) => tl.Update(dt);

        public void Reset()
        {
            tl.Clear();
            pills.Clear(); pops.Clear(); dates.Clear();
            pillEls.Clear(); shownScore = null; shakeUntil = 0; nb = -1;
            score.text = "0"; combo.text = "";
            SetBadge(0);
            mewrap.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            mewrap.style.scale = StyleKeyword.Null; mewrap.style.opacity = 1;
            play.style.opacity = 1; play.style.translate = new Translate(0, 0);
            SetPaper(Rules.Paper(0));
            Slip(START_DATE);
        }

        public void Handle(List<RoundEvent> events, Round r)
        {
            foreach (var e in events)
                switch (e.Type)
                {
                    case RoundEventType.Date:
                        Slip(e.Date);
                        break;
                    case RoundEventType.Spawn:
                        pills.Add(pillEls[e.Pill.Id] = Pill(e.Pill, r));
                        if (e.Pill.First) sound.Buzz(); // one buzz per call, not per pill
                        break;
                    case RoundEventType.Pass:
                        if (e.Close) { Pop(r.Me.X, r.Me.Y - 50, "CLOSE!"); sound.Close(); }
                        else sound.Dodge(r.Combo);
                        SetBadge(r.Missed);
                        combo.text = r.Combo > 1 ? $"PERFECT ×{r.Combo}" : "";
                        break;
                    case RoundEventType.Unpass:
                        SetBadge(r.Missed);
                        break;
                    case RoundEventType.Hit:
                        combo.text = ""; shakeUntil = tl.Now + 0.3; sound.Thud();
                        break;
                    case RoundEventType.Remove:
                        if (pillEls.TryGetValue(e.Pill.Id, out var gone))
                        {
                            pillEls.Remove(e.Pill.Id);
                            tl.Tween(0, 0.2, Ease.CssEase, p => Layer.Opacity(gone, gone[0], (float)(1 - p)));
                            tl.After(0.25, gone.RemoveFromHierarchy);
                        }
                        break;
                }
        }

        public void Draw(Round r)
        {
            foreach (var p in r.Pills)
            {
                if (!pillEls.TryGetValue(p.Id, out var el)) continue;
                double buzz = Math.Sin(r.T * 75 + p.Seed) * 1.3, lean = Math.Max(-14, Math.Min(14, Math.Atan(p.Vy / p.Vx) * 57.3 * 0.6));
                el.style.translate = new Translate(Fixed(p.X - p.W / 2 + buzz, 1), Fixed(p.Y - p.H / 2, 1));
                el.style.rotate = new Rotate(Fixed(lean, 1));
            }
            me.style.translate = new Translate(Fixed(r.Me.X, 2), Fixed(r.Me.Y, 2));
            mesvg.style.rotate = new Rotate(Fixed(r.Tilt, 1));
            if (r.Score != shownScore) { shownScore = r.Score; score.text = Format(score, r.Score); }
            // names stay unreadable through most of the round; 5–6 answered calls clear them
            double blur = Math.Round((1 - Rules.Clarity(r.T, r.Bonus)) * 8, 2);
            if (blur != nb)
            {
                nb = blur;
                foreach (var el in pillEls.Values) el.Q<Label>().style.filter = Layer.Blur(nb);
            }
            // the paper is your month: vibrant at first, colder with every call you let ring out
            SetPaper(Rules.Paper(r.PaperK));
            play.style.translate = tl.Now < shakeUntil
                ? new Translate(Fixed(random.NextDouble() * 14 - 7, 1), Fixed(random.NextDouble() * 14 - 7, 1))
                : new Translate(0, 0);
        }

        // a number to `digits` decimals, as the web writes it into a style (toFixed)
        public static float Fixed(double v, int digits) => (float)Math.Round(v, digits, MidpointRounding.AwayFromZero);

        // a score as the browser writes it (toLocaleString), unless `label`'s font can't draw that (digits of another
        // script, say): then 0–9 and commas
        public static string Format(Label label, int n)
        {
            string s = Browser.FormatNumber(n);
            var font = label.resolvedStyle.unityFontDefinition.fontAsset;
            return font == null || font.HasCharacters(s, out uint[] _, false, true) ? s : Rules.FormatScore(n);
        }

        // A pill: an outer element that moves (and fades as one layer), its body (the green capsule, its knob, the name).
        VisualElement Pill(Pill p, Round r)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.AddToClassList("pill");
            el.style.width = (float)p.W;
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("body");
            var knob = new VisualElement { pickingMode = PickingMode.Ignore };
            knob.AddToClassList("knob");
            knob.style.backgroundColor = PaperColor(r.PaperK);
            var name = new Label(p.Who) { pickingMode = PickingMode.Ignore };
            name.style.filter = Layer.Blur(nb < 0 ? 6 : nb); // the web's --nb starts at 6px until the first frame sets it
            body.Add(knob); body.Add(name);
            el.Add(body);
            return el;
        }

        public static Color PaperColor(double k)
        {
            var (r, g, b) = Rules.Paper(k);
            return new Color32((byte)r, (byte)g, (byte)b, 255);
        }

        // The paper colour on everything made of paper: the page, the world, the date cards, the pills' knobs.
        void SetPaper((int R, int G, int B) rgb)
        {
            var c = (Color)new Color32((byte)rgb.R, (byte)rgb.G, (byte)rgb.B, 255);
            Browser.SetPaper(rgb.R, rgb.G, rgb.B);
            world.style.backgroundColor = c;
            var page = world.parent?.parent;
            if (page != null) page.style.backgroundColor = c;
            foreach (var slip in dates.Children()) slip[0].style.backgroundColor = c;
            // every pill's knob, the fading ones too (the web's knobs all read var(--paper))
            foreach (var el in pills.Children()) el[0][0].style.backgroundColor = c;
        }

        void SetBadge(int missed)
        {
            badge.text = missed.ToString();
            badge.EnableInClassList("zero", missed == 0);
        }

        // a bordered date card, put down by hand on top of the last one, a little crooked
        void Slip(string text)
        {
            var slip = new VisualElement { pickingMode = PickingMode.Ignore };
            slip.AddToClassList("slip");
            var paper = new Label(text) { pickingMode = PickingMode.Ignore };
            paper.AddToClassList("paper");
            paper.style.backgroundColor = world.resolvedStyle.backgroundColor;
            slip.Add(paper);
            float x = Fixed(random.NextDouble() * 22 - 12, 1), y = Fixed(random.NextDouble() * 14 - 5, 1);
            float rot = Fixed(random.NextDouble() * 13 - 6.5, 1);
            // slap: dropped from a little above, turned and larger, in 0.17 s
            tl.Tween(0, 0.17, Ease.EaseOut, p =>
            {
                Layer.Opacity(slip, paper, (float)p, 1.25f - 0.25f * (float)p); // fading in as one card
                slip.style.translate = new Translate(x, y - 12 * (float)(1 - p));
                slip.style.rotate = new Rotate(rot + 7 * (float)(1 - p));
            });
            Layer.Opacity(slip, paper, 0, 1.25f);
            dates.Add(slip);
            while (dates.childCount > 9) dates.RemoveAt(0);
        }

        void Pop(double x, double y, string text)
        {
            var p = new Label(text) { pickingMode = PickingMode.Ignore };
            p.AddToClassList("pop");
            p.style.left = (float)x; p.style.top = (float)y;
            pops.Add(p);
            tl.Tween(0, 0.7, Ease.EaseOut, k => { p.style.opacity = (float)(1 - k); p.style.marginTop = (float)(-34 * k); });
            tl.After(0.8, p.RemoveFromHierarchy);
        }
    }
}
