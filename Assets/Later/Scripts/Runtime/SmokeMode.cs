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
    // The build's automated checks, like the web version's tests/browser/harness.js. Started when the page's hash is
    // #test-<mode>[&speed=N]: boot, dodge or late. Every frame is a fixed speed/60 s of game time (default 4×), any
    // logged error fails the run, and the result goes in the tab title: "OK <mode>" or "FAIL <reasons>".
    public sealed class SmokeMode : MonoBehaviour
    {
        LaterGame game;
        string mode;
        readonly List<string> fails = new List<string>();
        readonly List<(double At, Action Check)> schedule = new List<(double, Action)>();
        Func<Round, Vec> bot;
        double t;
        bool done;

        public static bool Wanted => Browser.Hash.StartsWith("#test-");

        void Start()
        {
            game = GetComponent<LaterGame>();
            var parts = Browser.Hash.Substring("#test-".Length).Split('&');
            mode = parts[0];
            float speed = 4;
            foreach (var p in parts.Skip(1))
                if (p.StartsWith("speed=")) float.TryParse(p.Substring(6), NumberStyles.Float, CultureInfo.InvariantCulture, out speed);
            Time.captureDeltaTime = speed / 60f;
            Application.logMessageReceived += OnLog;
            Browser.SetTitle("RUNNING " + mode);

            switch (mode)
            {
                case "boot":
                    At(0.5, () =>
                    {
                        Expect(game.State == "title" && Shown(Q("title")), "title screen not up");
                        Expect(Plain(Q("title").Q<Label>(className: "name").text) == "LATER", "no LATER on the title screen");
                        // every bundled font can draw: the build carries the font files the dynamic atlases fill from
                        var fonts = Resources.FindObjectsOfTypeAll<UnityEngine.TextCore.Text.FontAsset>();
                        foreach (var name in new[] { "Anton SDF", "ArchivoBlack SDF", "Newsreader-Italic SDF", "Inter-SemiBold SDF", "Inter-Bold SDF" })
                        {
                            var font = fonts.FirstOrDefault(f => f.name == name);
                            Expect(font != null && font.HasCharacters("LATERlater0123…×·", out uint[] _, false, true), $"font {name} {(font == null ? "missing" : "can't draw")}");
                        }
                        Report();
                    });
                    break;
                case "dodge":
                    Play(r => Bots.DodgeTarget(r, 340));
                    break;
                case "late":
                    Play(r => Bots.DodgeTarget(r, 340));
                    // the web harness's late schedule, in game time from the start of the round
                    At(30, () =>
                    {
                        var top = Q("items").Query(className: "tap").ToList().FirstOrDefault();
                        string who = Plain(top?.Q<Label>(className: "title")?.text), text = Plain(top?.Q<Label>(className: "text")?.text);
                        Expect(who == "John" && text.StartsWith("Funeral's on Saturday."), $"top message: {who} {text}");
                        string header = Plain(Q("caption").Q<Label>(className: "who")?.text);
                        Expect(header == CAPTION_HEADERS["voicemail"], "caption header: " + header);
                    });
                    At(40.5, () =>
                    {
                        Expect(Shown(Q("again")), "TURN BACK TIME not shown");
                        Expect(!Shown(Q("restart")), "PLAY AGAIN before TURN BACK TIME");
                        Click(Q("again"));
                    });
                    At(44.4, () =>
                    {
                        Expect(Plain(Q("again").Q<Label>(className: "lesson")?.text) == LESSON, "no lesson line after the soft line");
                        Expect(!Shown(Q("restart")), "PLAY AGAIN before the lesson has sunk in");
                    });
                    At(47.4, () =>
                    {
                        Expect(Shown(Q("restart")), "PLAY AGAIN not shown about 3 s after the lesson");
                        Report();
                    });
                    break;
                default:
                    Expect(false, "unknown mode " + mode);
                    Report();
                    break;
            }
        }

        void OnDestroy() => Application.logMessageReceived -= OnLog;

        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) fails.Add($"{type}: {message}");
        }

        void Play(Func<Round, Vec> steer)
        {
            bot = steer;
            game.StartRound();
            if (mode == "dodge")
                At(20.6, () =>
                {
                    Expect(Shown(Q("timeov")), "TIME! not shown at 20.6 s");
                    Expect(game.Round.Hits == 0, $"a careful dodger was hit {game.Round.Hits} times");
                    Report();
                });
        }

        void At(double at, Action check) => schedule.Add((at, check));

        void Update()
        {
            if (done) return;
            if (bot != null && game.State == "play") game.ForcedTarget = bot(game.Round);
            t += Time.deltaTime;
            foreach (var due in schedule.Where(s => s.At <= t).ToList())
            {
                schedule.Remove(due);
                try { due.Check(); }
                catch (Exception e) { fails.Add(e.Message); Report(); }
                if (done) return;
            }
        }

        void Expect(bool ok, string why)
        {
            if (!ok) fails.Add(why);
        }

        void Report()
        {
            if (done) return;
            done = true;
            Browser.SetTitle(fails.Count == 0 ? "OK " + mode : "FAIL " + string.Join("; ", fails.Take(5)));
        }

        VisualElement Q(string name) => game.Root.Q(name);

        static string Plain(string text) => Regex.Replace(text ?? "", "<[^>]+>", "");

        // displayed and not hidden, like the web harness's visible() (a fade still under way counts as shown)
        static bool Shown(VisualElement el)
        {
            for (var e = el; e != null; e = e.parent)
                if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return el != null;
        }

        static void Click(VisualElement el)
        {
            using var e = ClickEvent.GetPooled();
            e.target = el;
            el.SendEvent(e);
        }
    }
}
