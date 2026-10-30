using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Later.LaterData;

namespace Later
{
    // Small pure rules shared by the round, the view and the ending. Ported from web/later/src/rules.js.
    public static class Rules
    {
        // JavaScript's Math.round: halves round up, toward +∞ (C# rounds them to even).
        public static double RoundHalfUp(double x) => Math.Floor(x + 0.5);

        // How wide the world is (it is always 540 tall) for a w×h screen area: as wide as the screen, between 960 and W_MAX.
        public static double WorldWidth(double w, double h) => Math.Min(W_MAX, Math.Max(W, RoundHalfUp(H * w / h)));

        // "MAR 05" → "Mar 5"
        public static string ShortDate(string d) =>
            d[0] + d.Substring(1, 2).ToLowerInvariant() + " " + int.Parse(d.Substring(4), CultureInfo.InvariantCulture);

        // How readable the names are: unreadable most of the round, sharp at 16 s; each answered call clears them more.
        public static double Clarity(double t, double bonus) => Math.Min(1, Math.Pow(Math.Min(t, 16) / 16, 3.5) + bonus);

        // The paper, from vibrant (k = 0, nothing missed) to cold slate (k = 1, everything missed).
        public static (int R, int G, int B) Paper(double k)
        {
            int Mix(int i) => (int)RoundHalfUp(PAPER_WARM[i] + (PAPER_COLD[i] - PAPER_WARM[i]) * k);
            return (Mix(0), Mix(1), Mix(2));
        }

        public static string RankFor(int hits) => hits == 0 ? "PERFECT!" : hits <= 2 ? "GREAT!" : "NICE TRY";

        // Missed her last call: too late. Picked it up: home if you'd kept in touch, otherwise only her last call.
        public static string EndingFor(IReadOnlyList<bool> answered)
        {
            if (!answered[13]) return "late";
            return answered.Take(13).Any(a => a) ? "home" : "last";
        }

        // Every text of the month, oldest first: a thank-you for each call you picked up,
        // what they were trying to tell you for each one you let ring out, then the ending's last word.
        public static List<Message> BuildThread(IReadOnlyList<bool> answered, string ending)
        {
            var thread = Enumerable.Range(0, 13)
                .Select(i => new Message(CALLS[i].Who, CALLS[i].Date, answered[i] ? FOLLOW_UP[i] : PENDING[i]))
                .ToList();
            thread.Add(FINAL[ending]);
            return thread;
        }

        public static (string Loud, string Soft) TurnBackLines(string ending, int missed)
        {
            var lines = TURN_BACK[ending];
            string n = missed == 1 ? "ONCE" : missed == 2 ? "TWICE" : $"{missed} TIMES";
            return (lines[0].Replace("{n}", n), lines[1]);
        }

        // Distance from a point to a pill's capsule outline (pill centred at x, y); negative means inside.
        public static double CapsuleDist(double x, double y, double w, double h, double px, double py)
        {
            double half = w / 2 - h / 2;
            double dx = Math.Max(Math.Abs(px - x) - half, 0), dy = py - y;
            return Math.Sqrt(dx * dx + dy * dy) - h / 2;
        }

        // When the ring's notes fall between `from` and `to` (seconds since the ringing started).
        public static List<(double At, int Midi)> RingNotes(double from, double to)
        {
            double cycle = RING.STEP * RING.MOTIF.Length + RING.REST;
            var notes = new List<(double At, int Midi)>();
            for (var k = Math.Max(0, Math.Floor(from / cycle)); k * cycle < to; k++)
                for (var i = 0; i < RING.MOTIF.Length; i++)
                {
                    double at = k * cycle + i * RING.STEP;
                    if (at >= from && at < to) notes.Add((at, RING.MOTIF[i]));
                }
            return notes;
        }

        // The score as en-US toLocaleString writes it: "1,234", "-1,234".
        public static string FormatScore(int n) => n.ToString("#,0", CultureInfo.InvariantCulture);
    }
}
