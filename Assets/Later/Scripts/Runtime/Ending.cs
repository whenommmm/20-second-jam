using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Later.LaterData;

namespace Later
{
    // Everything from "TIME!" to PLAY AGAIN: the results screen, the shrink into the phone, the three flights,
    // the lock screen and its Messages sheet, her words beside the phone, TURN BACK TIME.
    // Ported from web/later/src/ending.js: the same delays, durations and easings, on one Timeline that Dispose clears.
    public sealed class Ending
    {
        static readonly int TotalPills = CALLS.Sum(c => c.N);
        static readonly Color Ink = new Color32(0x14, 0x14, 0x14, 255), Red = new Color32(0xFF, 0x3B, 0x30, 255);
        static readonly Color PhoneBody = new Color32(0x0b, 0x0b, 0x0c, 255);
        static readonly Func<double, double> Flight = Ease.CubicBezier(.45, .05, .2, 1);

        readonly Round round;
        readonly VisualElement world, fly;
        readonly Sound sound;
        readonly Action onRestart;
        readonly Timeline tl = new Timeline();
        bool disposed;

        public string Key { get; }

        public Ending(Round round, VisualElement world, Sound sound, Action onRestart)
        {
            this.round = round; this.world = world; this.sound = sound; this.onRestart = onRestart;
            fly = Q("fly");
            Key = Rules.EndingFor(round.Answered);

            Q("timeov").AddToClassList("on");
            tl.After(0.8, Results);
        }

        public void Update(double dt)
        {
            if (!disposed) tl.Update(dt);
        }

        // tap twice and the rest of her words appear at once (Task 8)
        public void TapTwice() { }

        VisualElement Q(string name) => world.Q(name);
        Label L(string name) => world.Q<Label>(name);

        // ---- TIME! and the results screen ----

        void Results()
        {
            Q("timeov").RemoveFromClassList("on");
            var card = Q("card");
            card.style.display = DisplayStyle.Flex;
            card.style.backgroundColor = PlayView.PaperColor(round.PaperK);
            Q("play").style.opacity = 0;

            // the moon glides over to sit beside your score (the results are laid out 960 wide, in the middle of the world)
            var wrap = Q("mewrap");
            float hw = wrap.resolvedStyle.width / 2, hh = wrap.resolvedStyle.height / 2;
            float dx = (float)(720 + (round.W - W) / 2 - round.Me.X), dy = (float)(170 - round.Me.Y);
            tl.Tween(0, 0.7, Ease.CubicBezier(.3, 0, .2, 1), p =>
            {
                wrap.style.translate = new Translate(-hw + dx * (float)p, -hh + dy * (float)p);
                wrap.style.scale = new Scale(Vector2.one * (1 + 0.7f * (float)p));
            });

            var resnum = L("resnum");
            resnum.text = "0";
            tl.After(0.15, () => tl.Tween(0, 0.9, Ease.Linear, k => resnum.text = Rules.FormatScore((int)Rules.RoundHalfUp(round.Score * (1 - Math.Pow(1 - k, 3))))));
            L("r1").text = $"{round.Dodged} / {TotalPills}"; L("r2").text = round.CloseN.ToString(); L("r3").text = $"×{round.Best}";
            var rows = Q("res").Query(className: "row").ToList();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                tl.After(0.7 + i * 0.17, () => tl.Tween(0, 0.2, Ease.CssEase, p =>
                {
                    row.style.opacity = (float)p;
                    row.style.translate = new Translate(-14 * (1 - (float)p), 0);
                }));
            }
            var rank = L("rank");
            rank.text = Rules.RankFor(round.Hits);
            tl.After(1.55, () =>
            {
                rank.style.display = DisplayStyle.Flex;
                tl.Tween(0, 0.18, Ease.EaseOut, p =>
                {
                    rank.style.opacity = (float)p;
                    rank.style.rotate = new Rotate(-3 - 6 * (float)p);
                    rank.style.scale = new Scale(Vector2.one * (1.5f - 0.5f * (float)p));
                });
            });
            // only a perfect round earns the doubt
            if (round.Hits == 0) tl.After(2.5, () => FadeTo(Q("doubt"), 0, 1, 0.6));
            tl.After(4.1, Morph);
        }

        void FadeTo(VisualElement el, float from, float to, double seconds, Func<double, double> ease = null) =>
            tl.Tween(0, seconds, ease ?? Ease.CssEase, p => el.style.opacity = from + (to - from) * (float)p);

        // ---- the shrink into the phone, and the flights home ----

        // world-space box of any element, whatever its transforms
        Rect Box(VisualElement el) => world.WorldToLocal(el.worldBound);

        // a stand-in that starts exactly where `b` is drawn
        VisualElement Flyer(Rect b, string kind)
        {
            var d = new VisualElement { pickingMode = PickingMode.Ignore };
            d.AddToClassList("flyer"); d.AddToClassList(kind);
            d.style.left = b.x; d.style.top = b.y; d.style.width = b.width; d.style.height = b.height;
            fly.Add(d);
            return d;
        }

        static Label FlyerText(string text, string cls, float size)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(cls);
            l.style.fontSize = size; l.style.height = size; // line-height 1
            return l;
        }

        // piecewise-linear keyframes, like Web Animations between offsets
        static float Keys(double u, params (double At, float V)[] keys)
        {
            for (var i = 1; i < keys.Length; i++)
                if (u <= keys[i].At)
                    return Mathf.Lerp(keys[i - 1].V, keys[i].V, (float)((u - keys[i - 1].At) / (keys[i].At - keys[i - 1].At)));
            return keys[keys.Length - 1].V;
        }

        void Morph()
        {
            var res = Q("res");
            FadeTo(res, 1, 0, 0.25);
            tl.After(0.18, () =>
            {
                // the card shrinks into the phone, staying centred in the world
                var card = Q("card");
                var shrink = Ease.CubicBezier(.6, 0, .2, 1);
                Color from = card.resolvedStyle.backgroundColor;
                tl.Tween(0, 0.85, Ease.Linear, t =>
                {
                    float k = (float)shrink(t), e = (float)Ease.CssEase(t);
                    float w = 960 + (256 - 960) * k, h = 540 + (516 - 540) * k;
                    card.style.width = w; card.style.height = h;
                    card.style.left = 480 - w / 2; card.style.top = 270 - h / 2;
                    var r = new StyleLength(42 * e);
                    card.style.borderTopLeftRadius = r; card.style.borderTopRightRadius = r;
                    card.style.borderBottomLeftRadius = r; card.style.borderBottomRightRadius = r;
                    card.style.backgroundColor = Color.Lerp(from, PhoneBody, e);
                });
            });
            tl.After(1.0, () => FadeTo(Q("lock"), 0, 1, 0.45));
            tl.After(1.25, Flights);
        }

        void Flights()
        {
            // fill in the missed-calls group now so the badge has somewhere to land
            var missedCalls = round.Calls.Where(c => c.MissedN > 0).ToList();
            L("gcount").text = round.Missed.ToString();
            L("gword").text = round.Missed == 1 ? "Missed Call" : "Missed Calls";
            L("gnames").text = string.Join(", ", missedCalls.AsEnumerable().Reverse().Select(c => c.Def.Who).Distinct());
            L("gdate").text = missedCalls.Count > 0 ? Rules.ShortDate(missedCalls[missedCalls.Count - 1].Def.Date) : "";

            // swap the real moon, tag and badge for stand-ins, then fly each one home. The moon and its name travel with
            // a pill that grows out around them, and LATER turns into "Later" on the way: together they become "☾ Later".
            VisualElement mesvg = Q("mesvg"), tag = Q("mewrap").Q(className: "tag"), focus = Q("focus");
            Rect m = Box(mesvg), t0 = Box(tag), pillBox = Box(focus), ic = Box(Q("focusIcon")), tx = Box(Q("focusText"));
            float textScale = tx.height / t0.height;

            var pillBg = Flyer(pillBox, "pillbg"); // behind the moon and its name
            var moonF = Flyer(m, "moon");
            var tagF = Flyer(t0, "tag");
            var was = FlyerText("<cspace=2.55px>LATER</cspace>", "was", 14 * 1.7f);
            var now = FlyerText("Later", "now", 11 / textScale);
            tagF.Add(was); tagF.Add(now);
            bool hasBadge = round.Missed > 0;
            Q("group").style.display = hasBadge ? DisplayStyle.Flex : DisplayStyle.None;
            VisualElement badgeF = null;
            Rect badgeBox = Box(Q("badge"));
            if (hasBadge)
            {
                badgeF = Flyer(badgeBox, "badge");
                badgeF.Add(FlyerText(round.Missed.ToString(), "count", 14 * 1.7f));
            }
            Q("me").style.visibility = Visibility.Hidden;

            // swoop down and under the clock, then rise into the pill; turn white as they cross onto the screen.
            // One path for the moon; its name and the pill ride along with it, so the three move and change as one piece.
            Vector2 c0 = m.center, c1 = ic.center, cc = new Vector2((c0.x + c1.x) / 2 + 30, Math.Max(c0.y, c1.y) + 120);
            Vector2 tagC = t0.center, pillC = pillBox.center;
            Vector2 tagFrom = tagC - c0, tagTo = tx.center - c1, pillOff = pillC - c1;
            float tilt = (float)round.Tilt;
            tl.Tween(0, 1.3, Flight, p =>
            {
                float u = (float)p, v = 1 - u, k = 1 - Mathf.Pow(v, 1.6f), blend = u * u * (3 - 2 * u);
                Vector2 at = v * v * c0 + 2 * v * u * cc + u * u * c1;
                float sm = 1 + (ic.height / m.height - 1) * k, st = 1 + (tx.height / t0.height - 1) * k, sp = 0.2f + 0.8f * k;
                moonF.style.translate = new Translate(at.x - c0.x, at.y - c0.y);
                moonF.style.rotate = new Rotate(tilt * v);
                moonF.style.scale = new Scale(Vector2.one * sm);
                Vector2 tc = at + tagFrom + (tagTo - tagFrom) * blend;
                tagF.style.translate = new Translate(tc.x - tagC.x, tc.y - tagC.y);
                tagF.style.scale = new Scale(Vector2.one * st);
                Vector2 pc = at + pillOff * sp;
                pillBg.style.translate = new Translate(pc.x - pillC.x, pc.y - pillC.y);
                pillBg.style.scale = new Scale(Vector2.one * sp);
                pillBg.style.opacity = Keys(u, (0, 0), (.4, 0), (.9, 1), (1, 1));
                var white = Color.Lerp(Ink, Color.white, Keys(u, (0, 0), (.36f, 0), (.58f, 1), (1, 1)));
                moonF.style.unityBackgroundImageTintColor = white;
                was.style.color = white;
                was.style.opacity = Keys(u, (0, 1), (.55, 1), (.85, 0), (1, 0));
                now.style.opacity = Keys(u, (0, 0), (.55, 0), (.85, 1), (1, 1));
            }, () =>
            {
                // everything has landed exactly on the real pill: hand over in the same frame, so nothing blinks
                focus.style.opacity = 1;
                moonF.RemoveFromHierarchy(); tagF.RemoveFromHierarchy(); pillBg.RemoveFromHierarchy();
                FadeTo(Q("sbMoon"), 0, 1, 0.4);
                tl.After(0.5, () => FadeTo(Q("sil"), 0, 0.55f, 0.4));
            });

            if (hasBadge)
            {
                var group = Q("group");
                tl.Tween(1.05, 0.38, Ease.EaseOut, p =>
                {
                    group.style.opacity = (float)p;
                    group.style.translate = new Translate(0, 8 * (1 - (float)p));
                });
                var gcount = Q("gcount");
                gcount.style.opacity = 0;
                tl.Tween(1.5, 0.2, Ease.Linear, p => gcount.style.opacity = (float)p);
                FlyBadge(badgeF, badgeBox, Box(gcount));
            }
            tl.After(2.6, EndingMessages);
        }

        // the badge glides along a gentle arc and shrinks into the group's count, landing exactly on it, then fades
        void FlyBadge(VisualElement f, Rect s, Rect e)
        {
            Vector2 sc = s.center, ec = e.center, c = new Vector2(sc.x - 10, ec.y + 10);
            float end = e.height / s.height;
            tl.Tween(0.12, 1.5, Flight, p =>
            {
                float u = (float)p, v = 1 - u;
                Vector2 at = v * v * sc + 2 * v * u * c + u * u * ec;
                f.style.translate = new Translate(at.x - sc.x, at.y - sc.y);
                f.style.scale = new Scale(Vector2.one * (1 + (end - 1) * (1 - Mathf.Pow(1 - u, 1.6f))));
                var bg = Red;
                bg.a = Keys(u, (0, 1), (.75, 1), (1, 0));
                f.style.backgroundColor = bg;
            }, () => tl.Tween(0, 0.15, Ease.Linear, p => f.style.opacity = 1 - (float)p, f.RemoveFromHierarchy));
        }

        // ---- the lock screen, her words, TURN BACK TIME (Task 8) ----

        void EndingMessages() { }

        public void Dispose()
        {
            disposed = true;
            tl.Clear();
            fly.Clear();
            Q("timeov").RemoveFromClassList("on");
            var card = Q("card");
            card.style.display = StyleKeyword.Null;
            card.style.width = StyleKeyword.Null; card.style.height = StyleKeyword.Null;
            card.style.left = StyleKeyword.Null; card.style.top = StyleKeyword.Null;
            card.style.borderTopLeftRadius = StyleKeyword.Null; card.style.borderTopRightRadius = StyleKeyword.Null;
            card.style.borderBottomLeftRadius = StyleKeyword.Null; card.style.borderBottomRightRadius = StyleKeyword.Null;
            card.style.backgroundColor = StyleKeyword.Null;
            foreach (var row in Q("res").Query(className: "row").ToList()) { row.style.opacity = StyleKeyword.Null; row.style.translate = StyleKeyword.Null; }
            foreach (var id in new[] { "res", "lock", "rank", "doubt", "sil", "sbMoon", "focus", "focusText", "group", "gcount" })
            {
                var s = Q(id).style;
                s.opacity = StyleKeyword.Null; s.translate = StyleKeyword.Null; s.rotate = StyleKeyword.Null; s.scale = StyleKeyword.Null;
                s.display = StyleKeyword.Null;
            }
            Q("me").style.visibility = StyleKeyword.Null;
        }
    }
}
