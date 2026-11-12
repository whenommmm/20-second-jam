using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
        readonly EventCallback<ClickEvent> onAgain, onRestartTap, onSheetClose;
        List<string> lines;
        int shown, sheetOpens;
        bool disposed, captionDone;

        public string Key { get; }
        // where the missed-call badge flies to: the count's own text box (for the tests)
        public Rect BadgeTarget { get; private set; }

        public Ending(Round round, VisualElement world, Sound sound, Action onRestart)
        {
            this.round = round; this.world = world; this.sound = sound; this.onRestart = onRestart;
            fly = Q("fly");
            Key = Rules.EndingFor(round.Answered);

            onAgain = e => TurnBack();
            onRestartTap = e => onRestart();
            onSheetClose = e => { e.StopPropagation(); CloseSheet(); };
            Q("again").RegisterCallback(onAgain);
            Q("restartTap").RegisterCallback(onRestartTap);
            Q("sheetClose").RegisterCallback(onSheetClose);

            Q("timeov").AddToClassList("on");
            tl.After(0.8, Results);
        }

        public void Update(double dt)
        {
            if (!disposed) tl.Update(dt);
        }

        // tap twice and the rest of her words appear at once
        public void TapTwice()
        {
            if (lines == null || captionDone) return;
            while (shown < lines.Count) ShowLine();
        }

        // Show less: the sheet goes back down
        public void CloseSheet()
        {
            sheetOpens++; // its fade, if still running, stops with it
            Q("sheet").RemoveFromClassList("on");
        }

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
            tl.After(0.15, () => tl.Tween(0, 0.9, Ease.Linear, k => resnum.text = PlayView.Format(resnum, (int)Rules.RoundHalfUp(round.Score * (1 - Math.Pow(1 - k, 3))))));
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
            var l = new Label(ExactSize.Wrap(text, size)) { pickingMode = PickingMode.Ignore };
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
            // the lock screen fades in as one layer, as CSS fades #lock
            VisualElement lockEl = Q("lock"), lockBody = Q("lockbody");
            tl.After(1.0, () => tl.Tween(0, 0.45, Ease.CssEase, p => Layer.Opacity(lockEl, lockBody, (float)p, clipped: true)));
            tl.After(1.25, Flights);
        }

        void Flights()
        {
            // fill in the missed-calls group now so the badge has somewhere to land
            var missedCalls = round.Calls.Where(c => c.MissedN > 0).ToList();
            ExactSize.SetText(L("gcount"), round.Missed.ToString());
            ExactSize.SetText(L("gword"), round.Missed == 1 ? "Missed Call" : "Missed Calls");
            ExactSize.SetText(L("gnames"), string.Join(", ", missedCalls.AsEnumerable().Reverse().Select(c => c.Def.Who).Distinct()));
            ExactSize.SetText(L("gdate"), missedCalls.Count > 0 ? Rules.ShortDate(missedCalls[missedCalls.Count - 1].Def.Date) : "");

            // swap the real moon, tag and badge for stand-ins, then fly each one home. The moon and its name travel with
            // a pill that grows out around them, and LATER turns into "Later" on the way: together they become "☾ Later".
            VisualElement mesvg = Q("mesvg"), tag = Q("mewrap").Q(className: "tag"), focus = Q("focus");
            Rect m = Box(mesvg), t0 = Box(tag), pillBox = Box(focus), ic = Box(Q("focusIcon")), tx = Box(Q("focusText"));
            float textScale = tx.height / t0.height;

            var pillBg = Flyer(pillBox, "pillbg"); // behind the moon and its name
            var moonF = Flyer(m, "moon");
            var tagF = Flyer(t0, "tag");
            var was = FlyerText(Spaced("LATER", 2.55), "was", 14 * 1.7f);
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
                VisualElement group = Q("group"), groupBody = Q("groupbody");
                tl.Tween(1.05, 0.38, Ease.EaseOut, p =>
                {
                    Layer.Opacity(group, groupBody, (float)p);
                    group.style.translate = new Translate(0, 8 * (1 - (float)p));
                });
                var gcount = Q("gcount");
                gcount.style.opacity = 0;
                tl.Tween(1.5, 0.2, Ease.Linear, p => gcount.style.opacity = (float)p);
                FlyBadge(badgeF, badgeBox, CountText((Label)gcount));
            }
            tl.After(2.6, EndingMessages);
        }

        // The count's text box, as the web's getBoundingClientRect gives it for the count's <span>: as wide as the new
        // count (measured now: its label's own box is laid out later, still at the old count), as tall as the font's
        // ascent and descent, centred on its line.
        Rect CountText(Label count)
        {
            Rect line = Box(count);
            var text = count.MeasureTextSize(count.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            return new Rect(line.x, line.center.y - text.y / 2, text.x, text.y);
        }

        // the badge glides along a gentle arc and shrinks into the group's count, landing exactly on it, then fades
        void FlyBadge(VisualElement f, Rect s, Rect e)
        {
            BadgeTarget = e;
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

        // ---- the lock screen ----

        // a line of text; text that may wrap gets the web version's line-height (font size × lineHeight)
        static Label Text(string text, string cls, float lineHeight = 0, Action<float, float> then = null)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(cls);
            if (lineHeight > 0) LineHeight(l, lineHeight, then);
            else ExactSize.Keep(l); // the phone's half-pixel sizes, exact
            return l;
        }

        // CSS line-height, which USS lacks. Once the label has its font, and again whenever its font size changes (the
        // small-screen style): its lines go `factor` × the font size apart, and its box grows or shrinks by the leading,
        // so it is as tall as CSS makes it, each line where CSS draws it. `then` gets the line height and the font's own.
        static void LineHeight(Label label, float factor, Action<float, float> then = null)
        {
            string raw = label.text;
            float applied = -1, lh = 0, natural = 0, height = -1;
            // one at a time: a label given new text (TURN BACK TIME's answer, each round) drops its old line height
            if (label.userData is EventCallback<GeometryChangedEvent> old) label.UnregisterCallback(old);
            EventCallback<GeometryChangedEvent> fit = null;
            label.userData = fit = e =>
            {
                float size = label.resolvedStyle.fontSize;
                if (size != applied)
                {
                    applied = size;
                    lh = size * factor;
                    // the font's own line, measured as the label lays its text out (tags and all)
                    natural = label.MeasureTextSize(LineTag(lh) + ExactSize.Wrap("Hg", size), 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
                    label.text = LineTag(lh) + ExactSize.Wrap(raw, size); // at its exact size: UI Toolkit rounds font sizes
                    label.style.unityTextAlign = Middle(label.resolvedStyle.unityTextAlign);
                    then?.Invoke(lh, natural);
                }
                // the label is exactly its lines' line boxes with the text centred in them (the half-leading above and
                // below), as CSS lays it out; one height, so UI Toolkit's whole-unit layout rounds it once, not three times
                int lines = (int)Math.Round((label.MeasureTextSize(label.text, label.contentRect.width, VisualElement.MeasureMode.AtMost, 0, VisualElement.MeasureMode.Undefined).y - natural) / lh) + 1;
                float h = Math.Max(1, lines) * lh;
                if (Math.Abs(h - height) > 0.001f) { height = h; label.style.height = h; }
            };
            label.RegisterCallback(fit);
        }

        // the same horizontal alignment, centred vertically
        static TextAnchor Middle(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft or TextAnchor.LowerLeft => TextAnchor.MiddleLeft,
            TextAnchor.UpperRight or TextAnchor.LowerRight => TextAnchor.MiddleRight,
            TextAnchor.UpperCenter or TextAnchor.LowerCenter => TextAnchor.MiddleCenter,
            _ => a
        };

        static string LineTag(float lh) => string.Format(CultureInfo.InvariantCulture, "<line-height={0:0.###}px>", lh);

        // -webkit-line-clamp: the text wrapped as usual and cut after `lines` lines, with "…" at the end of the last
        // (letters come off its end only if the ellipsis wouldn't fit), as Chrome does
        static void Clamp(Label label, string text, int lines, float lh, float natural)
        {
            float size = label.resolvedStyle.fontSize;
            string tag = LineTag(lh);
            float width = label.contentRect.width;
            int Lines(string t) => (int)Math.Round((label.MeasureTextSize(tag + ExactSize.Wrap(t, size), width, VisualElement.MeasureMode.AtMost, 0, VisualElement.MeasureMode.Undefined).y - natural) / lh) + 1;
            if (Lines(text) <= lines) { label.text = tag + ExactSize.Wrap(text, size); return; }
            var words = text.Split(' ');
            int k = words.Length;
            while (k > 0 && Lines(string.Join(" ", words.Take(k))) > lines) k--;
            string shown = string.Join(" ", words.Take(k)).TrimEnd();
            while (shown.Length > 0 && Lines(shown + "…") > lines) shown = shown.Substring(0, shown.Length - 1).TrimEnd();
            label.text = tag + ExactSize.Wrap(shown + "…", size);
        }

        // CSS letter-spacing, which runs on past the last letter: rich text's <cspace> only counts the spacing after the
        // last letter when something follows it, so a zero-width space does (UI Toolkit measures no trailing whitespace)
        public static string Spaced(string text, double px) =>
            string.Format(CultureInfo.InvariantCulture, "<cspace={0}px>{1}\u200B</cspace>", px, text);

        // a notification: the card (icon, title and time, then its text) inside an element that fades it as one layer
        static VisualElement Note(VisualElement into, string icon, string title, string when, params VisualElement[] body)
        {
            var n = new VisualElement();
            n.AddToClassList("n");
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList("card");
            var ic = new VisualElement { pickingMode = PickingMode.Ignore };
            ic.AddToClassList("ic");
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("icon"); glyph.AddToClassList(icon);
            ic.Add(glyph);
            var col = new VisualElement { pickingMode = PickingMode.Ignore };
            col.AddToClassList("body");
            var tt = new VisualElement { pickingMode = PickingMode.Ignore };
            tt.AddToClassList("tt");
            var b = new VisualElement { pickingMode = PickingMode.Ignore };
            b.AddToClassList("b");
            b.Add(Text(title, "title"));
            tt.Add(b); tt.Add(Text(when, "when"));
            col.Add(tt);
            foreach (var el in body) col.Add(el);
            card.Add(ic); card.Add(col);
            n.Add(card);
            if (into.childCount > 0) n.style.marginTop = 6; // the list's gap
            into.Add(n);
            return n;
        }

        // laid out but hidden until it's revealed
        // (not under the pointer either, so it neither lights up nor takes a tap: pointer-events: none on the web). The
        // card, not the notification around it, takes the pointer: drawn K× inside its fading layer, it covers the
        // whole card even mid-fade, and its taps and hover reach the notification.
        static VisualElement Pending(VisualElement n)
        {
            n.AddToClassList("pending");
            n.pickingMode = PickingMode.Ignore;
            n.Q(className: "card").pickingMode = PickingMode.Ignore;
            return n;
        }

        void Reveal(VisualElement n)
        {
            n.RemoveFromClassList("pending");
            n.Q(className: "card").pickingMode = PickingMode.Position;
            n.AddToClassList("in");
            var card = n.Q(className: "card");
            tl.Tween(0, 0.35, Ease.CubicBezier(.2, .8, .2, 1), p =>
            {
                Layer.Opacity(n, card, (float)p, 0.97f + 0.03f * (float)p);
                n.style.translate = new Translate(0, -12 * (1 - (float)p));
            });
        }

        void EndingMessages()
        {
            // the lock screen shows one Messages notification, the newest, stacked on the rest; tap to open
            var items = Q("items");
            var thread = Rules.BuildThread(round.Answered, Key);
            var newest = thread[thread.Count - 1];
            int more = thread.Count - 1;
            var body = new List<VisualElement> { Text(newest.Text, "text", 1.32f) };
            if (more > 0)
            {
                var sub = Text($"{more} more message{(more == 1 ? "" : "s")}", "sub");
                sub.AddToClassList("more");
                body.Add(sub);
            }
            var msgs = Pending(Note(items, "bubble", newest.Who, Rules.ShortDate(newest.Date), body.ToArray()));
            msgs.AddToClassList("tap"); msgs.AddToClassList("clicky");
            if (more > 0)
            {
                msgs.AddToClassList("stacked");
                // the rest of the thread, peeking out under it as the web's box-shadows (drawn with the card, so they fade with it)
                msgs.Q(className: "card").Insert(0, new CardShadows());
            }
            // not tappable until it has appeared
            msgs.RegisterCallback<ClickEvent>(e => { if (!msgs.ClassListContains("pending")) OpenThread(thread); });

            VisualElement vm = null;
            if (Key == "late")
            {
                // three lines of her voicemail, or two if the cards would reach "Notifications silenced"
                Label text = null;
                text = Text(VOICEMAIL, "vm", 1.3f, (lh, natural) =>
                    Clamp(text, VOICEMAIL, Box(items).yMin < Box(Q("sil")).yMax + 6 ? 2 : 3, lh, natural));
                vm = Pending(Note(items, "handset", "Mom", "Mar 28", Text("Voicemail · 0:18", "sub"), text));
            }

            if (vm != null) { Reveal(vm); PlayCaption(CAPTION_HEADERS["voicemail"], VOICEMAIL); }
            else PlayCaption(CAPTION_HEADERS["call"], LAST_CALL);
            tl.After(2.6, () => Reveal(msgs));
        }

        // tapping the Messages notification opens every message, newest first, in a sheet over the lock screen
        void OpenThread(List<Message> thread)
        {
            var sheet = Q("sheet");
            if (sheet.ClassListContains("on")) return;
            var list = world.Q<ScrollView>("sheetList");
            list.Clear();
            foreach (var m in thread.AsEnumerable().Reverse())
                Note(list.contentContainer, "bubble", m.Who, Rules.ShortDate(m.Date), Text(m.Text, "text", 1.32f));
            list.scrollOffset = Vector2.zero;
            // it opens from a tap, outside the timeline: start it invisible, so no frame shows it before its fade; it fades
            // in as one layer, and opening it again restarts the fade, as the web's animation restarts
            var body = Q("sheetbody");
            int mine = ++sheetOpens;
            Layer.Opacity(sheet, body, 0);
            sheet.style.translate = new Translate(0, 16);
            sheet.AddToClassList("on");
            tl.Tween(0, 0.28, Ease.CubicBezier(.2, .8, .2, 1), p =>
            {
                if (mine != sheetOpens) return;
                Layer.Opacity(sheet, body, (float)p);
                sheet.style.translate = new Translate(0, 16 * (1 - (float)p));
            });
        }

        // ---- her words, read out line by line beside the phone ----

        static readonly Regex Sentences = new Regex("[^.?!…]+[.?!…]+");

        void PlayCaption(string header, string text)
        {
            var caption = Q("caption");
            caption.Clear();
            caption.Add(Text(Spaced(header, 2), "who"));
            caption.AddToClassList("on");
            lines = Sentences.Matches(text).Cast<Match>().Select(m => m.Value.Trim()).ToList();
            shown = 0;
            for (var i = 0; i < lines.Count; i++) tl.After(0.5 + i * 1.1, ShowLine);
        }

        void ShowLine()
        {
            if (disposed || lines == null || shown >= lines.Count) return;
            var line = Text(lines[shown++], "line", 1.25f);
            Q("caption").Add(line);
            CapIn(line, 0.6);
            if (shown == lines.Count && !captionDone)
            {
                captionDone = true;
                tl.After(1.2, () => Q("again").AddToClassList("on")); // only once she's finished
                tl.After(16.2, ShowRestart); // in case TURN BACK TIME is never pressed
            }
        }

        // fade up from a little below
        void CapIn(VisualElement el, double seconds, float to = 1)
        {
            el.style.opacity = 0;
            tl.Tween(0, seconds, Ease.EaseOut, p =>
            {
                el.style.opacity = to * (float)p;
                el.style.translate = new Translate(0, 4 * (1 - (float)p));
            });
        }

        void ShowRestart()
        {
            var tap = Q("restartTap");
            if (tap.ClassListContains("on")) return;
            tap.AddToClassList("on");
            CapIn(Q("restart"), 0.8);
        }

        // ---- TURN BACK TIME: there is no going back; it answers, in words that fit how the month ended ----

        void TurnBack()
        {
            var again = Q("again");
            if (again.ClassListContains("tried") || !again.ClassListContains("on")) return;
            var (loud, soft) = Rules.TurnBackLines(Key, round.Missed);
            again.AddToClassList("tried");
            var l = L("loud");
            l.text = loud;
            LineHeight(l, 1.1f, (_, __) => Balance(l));
            tl.After(1.8, () => Answer(soft, "soft", 1.2f));
            tl.After(3.6, () => Answer(Spaced(LESSON, 0.5), "lesson", 1.1f));
            tl.After(6.6, ShowRestart); // the way out, only once the lesson has sunk in
        }

        void Answer(string text, string cls, float lineHeight)
        {
            Label line = null;
            line = Text(text, cls, lineHeight, (_, __) => Balance(line));
            Q("again").Add(line);
            CapIn(line, 0.9);
        }

        // text-wrap: balance — narrow the text to the least width that keeps its number of lines (as browsers do)
        static void Balance(Label label)
        {
            if (!Browser.SupportsBalance) return; // where the browser doesn't balance, the web's lines wrap greedily
            const float max = 290;
            float Height(float w) => label.MeasureTextSize(label.text, w, VisualElement.MeasureMode.AtMost, 0, VisualElement.MeasureMode.Undefined).y;
            float h = Height(max);
            float lo = 0, hi = max;
            while (hi - lo > 0.5f)
            {
                float mid = (lo + hi) / 2;
                if (Height(mid) > h + 0.5f) lo = mid; else hi = mid;
            }
            label.style.maxWidth = Mathf.Ceil(hi);
        }

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
            foreach (var id in new[] { "res", "lock", "lockbody", "rank", "doubt", "sil", "sbMoon", "focus", "focusText", "group", "groupbody", "gcount", "sheetbody" })
            {
                var s = Q(id).style;
                s.opacity = StyleKeyword.Null; s.translate = StyleKeyword.Null; s.rotate = StyleKeyword.Null; s.scale = StyleKeyword.Null;
                s.display = StyleKeyword.Null; s.filter = StyleKeyword.Null;
            }
            Q("me").style.visibility = StyleKeyword.Null;
            Layer.Release(Q("lockbody"));

            Q("again").UnregisterCallback(onAgain);
            Q("restartTap").UnregisterCallback(onRestartTap);
            Q("sheetClose").UnregisterCallback(onSheetClose);
            var caption = Q("caption");
            caption.Clear(); caption.RemoveFromClassList("on");
            Q("items").Clear();
            world.Q<ScrollView>("sheetList").Clear();
            var sheet = Q("sheet");
            sheet.RemoveFromClassList("on");
            sheet.style.opacity = StyleKeyword.Null; sheet.style.translate = StyleKeyword.Null;
            sheet.style.filter = StyleKeyword.Null; sheet.style.scale = StyleKeyword.Null;
            var again = Q("again");
            again.RemoveFromClassList("on"); again.RemoveFromClassList("tried");
            while (again.childCount > 1) again.RemoveAt(1);
            var loud = L("loud");
            if (loud.userData is EventCallback<GeometryChangedEvent> fit) { loud.UnregisterCallback(fit); loud.userData = null; }
            loud.text = "TURN BACK TIME"; loud.style.maxWidth = StyleKeyword.Null;
            loud.style.height = StyleKeyword.Null; loud.style.unityTextAlign = StyleKeyword.Null;
            loud.style.marginTop = StyleKeyword.Null; loud.style.marginBottom = StyleKeyword.Null;
            Q("restartTap").RemoveFromClassList("on");
            Q("restart").style.opacity = StyleKeyword.Null; Q("restart").style.translate = StyleKeyword.Null;
        }
    }
}
