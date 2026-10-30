using System;
using System.Collections.Generic;
using System.Linq;
using static Later.LaterData;
using static Later.LaterData.TUNING;

namespace Later
{
    public struct Vec
    {
        public double X, Y;
        public Vec(double x, double y) { X = x; Y = y; }
        public override string ToString() => $"({X}, {Y})";
    }

    // Where the pointer wants the moon (world units, or none), and the arrow/WASD direction.
    public struct RoundInput
    {
        public Vec? Target;
        public int KeysX, KeysY;
    }

    public enum RoundEventType { Date, Spawn, Pass, Unpass, Hit, Remove, Time }

    public sealed class RoundEvent
    {
        public RoundEventType Type;
        public Pill Pill;
        public bool Close;
        public string Date;
    }

    public sealed class Pill
    {
        public int Id, Ci, Gain;
        public string Who;
        public bool First, Passed, Locked, Dead;
        public double X, Y, W, H, Vx, Vy, MinD, Seed;
    }

    public sealed class CallState
    {
        public CallDef Def;
        public double V, Start, Gap;
        public double? Y0;
        public List<double> Band; // null until the call has started
        public int Spawned, Alive, MissedN;
        public bool Answered;
    }

    public sealed class Round
    {
        public IReadOnlyDictionary<string, double> PillW;
        public double W, K, T, Tilt, Bonus, PaperK;
        public Vec Me, Vel;
        public CallState[] Calls;
        public List<Pill> Pills = new List<Pill>();
        public int NextId = 1, NextCall, QuietIdx, Score, Combo, Best, CloseN, Missed, Dodged, Hits;
        public bool Over;

        public IReadOnlyList<bool> Answered => Array.ConvertAll(Calls, c => c.Answered);
    }

    // The 20-second round as a simulation with no UI and no sound: the view and the ending read it.
    // Ported from web/later/src/round.js, function by function.
    public static class RoundSim
    {
        static readonly int TotalPills = CALLS.Sum(c => c.N);
        const double STEP = 1.0 / 60; // long frames are split into steps this small, so nothing tunnels

        static double Clamp(double lo, double hi, double v) => Math.Max(lo, Math.Min(hi, v));

        static double NearestLane(double y)
        {
            double b = LANES[0];
            for (var i = 1; i < LANES.Length; i++) if (Math.Abs(LANES[i] - y) < Math.Abs(b - y)) b = LANES[i];
            return b;
        }

        // w is the world's width (960 up to W_MAX); calls cross a wider world faster, so they arrive on the same clock.
        public static Round Create(IReadOnlyDictionary<string, double> pillW = null, double w = LaterData.W)
        {
            double k = w / LaterData.W;
            return new Round
            {
                PillW = pillW ?? DEFAULT_PILL_W, W = w, K = k, Me = new Vec(220, 270),
                Calls = CALLS.Select(c => new CallState { Def = c, V = c.V * k }).ToArray()
            };
        }

        // Advance the round by dt seconds. Returns what happened, in order.
        public static List<RoundEvent> Step(Round r, double dt, RoundInput input)
        {
            var ev = new List<RoundEvent>();
            for (double left = dt; left > 1e-9 && !r.Over; left -= STEP) StepOnce(r, Math.Min(STEP, left), input, ev);
            return ev;
        }

        static void StepOnce(Round r, double dt, RoundInput input, List<RoundEvent> ev)
        {
            r.T = Math.Min(ROUND_TIME, r.T + dt);
            Move(r, dt, input);
            StartCalls(r, ev);
            SpawnPills(r, ev);
            while (r.QuietIdx < QUIET_DATES.Length && r.T >= QUIET_DATES[r.QuietIdx].T)
                ev.Add(new RoundEvent { Type = RoundEventType.Date, Date = QUIET_DATES[r.QuietIdx++].Date });
            MovePills(r, dt, ev);
            // the paper is your month: a step colder with every call you let ring out; answering never cools it
            r.PaperK += ((double)r.Missed / TotalPills - r.PaperK) * (1 - Math.Exp(-dt * 3));
            if (r.T >= ROUND_TIME) { r.Over = true; ev.Add(new RoundEvent { Type = RoundEventType.Time }); }
        }

        // The moon steers its velocity toward the pointer (or keys) rather than snapping, and leans into its motion.
        static void Move(Round r, double dt, RoundInput input)
        {
            int kx = input.KeysX, ky = input.KeysY;
            double wx = 0, wy = 0;
            if (kx != 0 || ky != 0)
            {
                double l = Math.Sqrt(kx * kx + ky * ky);
                wx = kx / l * KEY_SPEED; wy = ky / l * KEY_SPEED;
            }
            else if (input.Target is Vec target)
            {
                wx = (target.X - r.Me.X) * FOLLOW_GAIN; wy = (target.Y - r.Me.Y) * FOLLOW_GAIN;
                double wl = Math.Sqrt(wx * wx + wy * wy);
                if (wl > MAX_SPEED) { wx *= MAX_SPEED / wl; wy *= MAX_SPEED / wl; }
            }
            double a = 1 - Math.Exp(-dt * FOLLOW_EASE);
            r.Vel.X += (wx - r.Vel.X) * a; r.Vel.Y += (wy - r.Vel.Y) * a;
            r.Me.X = Clamp(26, r.W - 26, r.Me.X + r.Vel.X * dt);
            r.Me.Y = Clamp(TOP, BOTTOM, r.Me.Y + r.Vel.Y * dt);
            r.Tilt += (Clamp(-20, 20, r.Vel.X * 0.03) - r.Tilt) * (1 - Math.Exp(-dt * 8));
        }

        // Each call slides in near you; Mom's last call is the only one that leaves the lanes.
        static void StartCalls(Round r, List<RoundEvent> ev)
        {
            while (r.NextCall < r.Calls.Length && r.T >= r.Calls[r.NextCall].Def.T)
            {
                var c = r.Calls[r.NextCall];
                if (c.Def.Home)
                {
                    // her last call comes straight for you, so it waits (briefly) until you'd have room to get out of its way;
                    // if you still wouldn't (say you're parked at the far right), it comes in along the nearest fair lane instead
                    bool fairHere = Fair(r, NearestLane(r.Me.Y), c);
                    if (!fairHere && r.T < c.Def.T + HOME_WAIT) break;
                    c.Y0 = fairHere ? (double?)null : PickLane(r, c) ?? LANES.OrderByDescending(l => Math.Abs(l - r.Me.Y)).First();
                    c.Band = new List<double>();
                }
                else
                {
                    var lane = PickLane(r, c);
                    if (lane == null) break; // every lane busy: try again next step
                    c.Band = new List<double> { lane.Value };
                }
                c.Start = r.T; r.NextCall++;
                ev.Add(new RoundEvent { Type = RoundEventType.Date, Date = c.Def.Date });
            }
        }

        static void SpawnPills(Round r, List<RoundEvent> ev)
        {
            for (var ci = 0; ci < r.Calls.Length; ci++)
            {
                var c = r.Calls[ci];
                if (c.Band == null || c.Answered) continue;
                while (c.Spawned < c.Def.N && (c.Spawned == 0 || r.T >= c.Start + c.Spawned * c.Gap))
                {
                    double w = r.PillW[c.Def.Who], x = r.W + w / 2 + 10;
                    int id = r.NextId++;
                    // pills of one call follow each other a fixed distance apart, whatever their speed
                    if (c.Gap == 0) c.Gap = (w + PILL_SPACE) / c.V;
                    var pill = new Pill
                    {
                        Id = id, Ci = ci, Who = c.Def.Who, First = c.Spawned == 0, X = x,
                        Y = c.Def.Home ? c.Y0 ?? Clamp(TOP, BOTTOM, r.Me.Y) : c.Band[0], W = w, H = PILL_H,
                        Vx = -c.V, Vy = 0, MinD = 1e9, Seed = id * 2.399 % 10
                    };
                    r.Pills.Add(pill); c.Spawned++; c.Alive++;
                    ev.Add(new RoundEvent { Type = RoundEventType.Spawn, Pill = pill });
                }
            }
        }

        static void MovePills(Round r, double dt, List<RoundEvent> ev)
        {
            // by index: a hit marks the rest of its call dead partway through
            for (var i = 0; i < r.Pills.Count; i++)
            {
                var p = r.Pills[i];
                if (p.Dead) continue;
                var c = r.Calls[p.Ci];
                // Mom's last call drifts toward you until the middle of the screen, then commits to a gentler straight line you can dodge
                if (c.Def.Home && p.X > HOME_LOCK_X * r.K && p.X - r.Me.X > HOME_LOCK_DIST * r.K) p.Vy = Clamp(-HOME_MAX_VY, HOME_MAX_VY, (r.Me.Y - p.Y) * HOME_STEER);
                else if (c.Def.Home && !p.Locked) { p.Locked = true; p.Vy = 0; }
                p.X += p.Vx * dt; p.Y = Clamp(TOP, BOTTOM, p.Y + p.Vy * dt);
                double d = Rules.CapsuleDist(p.X, p.Y, p.W, p.H, r.Me.X, r.Me.Y);
                p.MinD = Math.Min(p.MinD, d);
                if (d < HIT_R)
                {
                    if (p.Passed) Unpass(r, p, ev); // you went back for it: answered, not missed
                    c.Answered = true; r.Hits++; r.Bonus += CLARITY_BONUS; r.Combo = 0; r.Score -= 100;
                    ev.Add(new RoundEvent { Type = RoundEventType.Hit, Pill = p });
                    foreach (var o in r.Pills)
                        if (!o.Dead && o.Ci == p.Ci) { o.Dead = true; c.Alive--; ev.Add(new RoundEvent { Type = RoundEventType.Remove, Pill = o }); }
                    continue;
                }
                if (!p.Passed && p.X + p.W / 2 < r.Me.X - 20) Pass(r, p, ev);
                if (p.X < -p.W || p.Y < -80 || p.Y > H + 80)
                {
                    if (!p.Passed) Pass(r, p, ev);
                    p.Dead = true; c.Alive--;
                    ev.Add(new RoundEvent { Type = RoundEventType.Remove, Pill = p });
                }
            }
            r.Pills.RemoveAll(p => p.Dead);
        }

        // The moment a pill gets past you it's one more missed call.
        static void Pass(Round r, Pill p, List<RoundEvent> ev)
        {
            var c = r.Calls[p.Ci];
            p.Passed = true; p.Gain = p.MinD < CLOSE_R ? 25 : 10;
            r.Score += p.Gain;
            if (p.Gain == 25) r.CloseN++;
            r.Dodged++; r.Missed++; c.MissedN++; r.Combo++; r.Best = Math.Max(r.Best, r.Combo);
            ev.Add(new RoundEvent { Type = RoundEventType.Pass, Pill = p, Close = p.Gain == 25 });
        }

        static void Unpass(Round r, Pill p, List<RoundEvent> ev)
        {
            var c = r.Calls[p.Ci];
            r.Score -= p.Gain; if (p.Gain == 25) r.CloseN--;
            r.Dodged--; r.Missed--; c.MissedN--;
            ev.Add(new RoundEvent { Type = RoundEventType.Unpass, Pill = p });
        }

        // ---- Lanes and fairness: calls come at you, but never without time to react or somewhere to go ----

        // A lane takes a new call only once the last one has fully slid in, and only if the newcomer can't catch it up.
        static bool LaneFree(Round r, double y, double v)
        {
            if (r.Calls.Any(c => c.Band != null && c.Band.Contains(y) && !c.Answered && c.Spawned < c.Def.N)) return false;
            Pill tail = null;
            foreach (var p in r.Pills) if (!p.Dead && r.Calls[p.Ci].Band.Contains(y) && (tail == null || p.X > tail.X)) tail = p;
            if (tail == null) return true;
            double back = tail.X + tail.W / 2, u = -tail.Vx;
            if (back > r.W - 40) return false;
            return v <= u || (r.W + 10 - back) / (v - u) > back / u;
        }

        // When, counting from now, a lane's pills cover the column you're in: (from, to) in seconds.
        static List<(double From, double To)> LaneWindows(Round r, double y)
        {
            double xp = r.Me.X;
            var out_ = new List<(double From, double To)>();
            foreach (var p in r.Pills)
                if (!p.Dead && r.Calls[p.Ci].Band.Contains(y))
                {
                    double u = -p.Vx;
                    out_.Add(((p.X - p.W / 2 - xp - 40) / u, (p.X + p.W / 2 - xp + 40) / u));
                }
            foreach (var c in r.Calls)
                if (c.Band != null && c.Band.Contains(y) && !c.Answered && c.Spawned < c.Def.N)
                {
                    double gap = c.Gap != 0 ? c.Gap : (100 + PILL_SPACE) / c.V;
                    for (var k = c.Spawned; k < c.Def.N; k++)
                    {
                        double ts = c.Start + k * gap - r.T;
                        out_.Add((ts + (r.W + 10 - xp - 40) / c.V, ts + (r.W + 110 - xp + 40) / c.V));
                    }
                }
            // Mom's last call leaves the lanes, so it blocks whichever lane it's in right now
            foreach (var p in r.Pills)
                if (!p.Dead && r.Calls[p.Ci].Def.Home && Math.Abs(p.Y - y) < 40)
                    out_.Add(((p.X - p.W / 2 - xp - 40) / -p.Vx, (p.X + p.W / 2 - xp + 40) / -p.Vx));
            out_.RemoveAll(w => !(w.To > 0));
            return out_;
        }

        static bool ClearDuring(Round r, double? y, double a, double b) =>
            y != null && LaneWindows(r, y.Value).All(w => w.To < a || w.From > b);

        // A call may come at you, but only if you'll see it in time and some other lane around you stays open until it's gone past.
        static bool Fair(Round r, double lane, CallState c)
        {
            int mine = Array.IndexOf(LANES, NearestLane(r.Me.Y)), i = Array.IndexOf(LANES, lane);
            if (Math.Abs(i - mine) > 1) return true;
            double reach = (r.W + 10 - r.Me.X - 40) / c.V;
            if (reach < REACT_MIN) return false;
            double b = reach + (c.Def.N * 100 + (c.Def.N - 1) * PILL_SPACE + 80) / c.V;
            for (var j = mine - 1; j <= mine + 1; j++)
                if (j != i && ClearDuring(r, j >= 0 && j < LANES.Length ? LANES[j] : (double?)null, 0, b)) return true;
            return false;
        }

        // The free, fair lane closest to you.
        static double? PickLane(Round r, CallState c)
        {
            foreach (var y in LANES.OrderBy(l => Math.Abs(l - r.Me.Y)))
                if (LaneFree(r, y, c.V) && Fair(r, y, c)) return y;
            return null;
        }
    }
}
