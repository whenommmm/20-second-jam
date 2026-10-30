using System;
using System.Linq;
using static Later.LaterData;

namespace Later
{
    // Test players, shared by the tests and the build's #test modes. They read the round directly (world units).
    // Ported from web/later/tests/bots.js.
    public static class Bots
    {
        static readonly double[] Steps = { -93, -62, -31, -15, 15, 31, 62, 93 };

        // How long (in px of approach) until a pill reaches the moon at height y; -1 means off the play area.
        static double Safety(Round round, double y, Func<Pill, bool> ignore)
        {
            if (y < TOP || y > BOTTOM) return -1;
            double mx = round.Me.X, s = 2000;
            foreach (var p in round.Pills)
            {
                if (ignore(p) || Math.Abs(p.Y - y) >= 34 || p.X + p.W / 2 <= mx - 34) continue;
                s = Math.Min(s, Math.Max(0, p.X - p.W / 2 - mx - 34));
            }
            return s;
        }

        // The best nearby height, judging each move by where it lands and the route to get there.
        static double BestHeight(Round round, Func<double, double, double> score, Func<Pill, bool> ignore)
        {
            double my = round.Me.Y;
            double best = score(my, Safety(round, my, ignore)) + 40, ty = my;
            foreach (var step in Steps)
            {
                double d = Math.Max(TOP, Math.Min(BOTTOM, my + step)) - my; // pressing against the top or bottom edge is a move too
                if (d == 0) continue;
                double route = 2000;
                for (var k = 1; k <= Math.Abs(d) / 10; k++) route = Math.Min(route, Safety(round, my + Math.Sign(d) * k * 10, ignore));
                double v = score(my + d, Math.Min(Safety(round, my + d, ignore), route * 3)) - Math.Abs(d) * 0.5;
                if (v > best) { best = v; ty = my + d; }
            }
            return ty;
        }

        // A careful dodger that holds x and moves up or down to stay clear.
        public static Vec DodgeTarget(Round r, double x) => new Vec(x, BestHeight(r, (y, safe) => safe, p => false));

        // Go for the leftmost wanted pill on screen. If some pills are unwanted, steer toward the wanted one's
        // height only along routes that stay clear of the others.
        public static Vec ChaseTarget(Round r, Func<Pill, bool> wants = null)
        {
            wants ??= p => true;
            var goal = r.Pills.Where(p => p.X - p.W / 2 < r.W && wants(p)).OrderBy(p => p.X).FirstOrDefault();
            if (goal == null) return new Vec(200, 300);
            if (r.Pills.All(wants)) return new Vec(goal.X, goal.Y);
            return new Vec(r.Me.X, BestHeight(r, (y, safe) => safe < 60 ? -1e6 : -Math.Abs(y - goal.Y), wants));
        }
    }
}
