using System;
using System.Collections.Generic;
using static Later.LaterData;

namespace Later.Editor
{
    // The web version's sounds (web/later/src/sound.js), rendered offline sample by sample: the same oscillators,
    // envelopes and filters, with Web Audio's own rules for gain automation and its biquad low-pass.
    // Every clip is already through the master gain (0.55); the ring also through RING.VOLUME.
    public static class Synth
    {
        public const int Rate = 44100;
        const double Master = 0.55;

        public static float[] Buzz()
        {
            // a phone vibrating on a table, once as each call comes in: a soft, low double pulse (bzz… bzz)
            var buf = new double[Length(0.4)];
            var g = new Param(1).Set(0, 0);
            foreach (var at in new[] { 0, 0.2 }) g.Linear(0.11, at + 0.02).Set(0.11, at + 0.1).Linear(0, at + 0.14);
            var lp = new LowPass(280);
            for (var n = 0; n < buf.Length; n++)
            {
                double t = (double)n / Rate, phase = 2 * Math.PI * 165 * t;
                buf[n] = lp.Next(2 / Math.PI * Math.Asin(Math.Sin(phase))) * g.At(t) * Master;
            }
            return ToFloats(buf);
        }

        // each dodge climbs a semitone with the streak
        public static float[] Blip(int streak)
        {
            var buf = new double[Length(0.1)];
            BlipInto(buf, 523 * Math.Pow(2, Math.Min(streak, 14) / 12.0), 0, 0.09);
            return ToFloats(buf);
        }

        public static float[] Close()
        {
            var buf = new double[Length(0.16)];
            BlipInto(buf, 880, 0, 0.1);
            BlipInto(buf, 1320, 0.06, 0.1);
            return ToFloats(buf);
        }

        // touching a call: a dull thud, not a voice
        public static float[] Thud()
        {
            var buf = new double[Length(0.3)];
            var freq = new Param(440).Set(110, 0).Exp(45, 0.25);
            Param g = Env(0, 0.38, 0.005, 0.05, 0.2), ng = Env(0, 0.22, 0.003, 0.03, 0.09);
            var lp = new LowPass(300);
            var noise = new Random(7);
            double phase = 0;
            for (var n = 0; n < buf.Length; n++)
            {
                double t = (double)n / Rate;
                double tone = Math.Sin(phase) * g.At(t);
                phase += 2 * Math.PI * freq.At(t) / Rate;
                buf[n] = (tone + lp.Next(noise.NextDouble() * 2 - 1) * ng.At(t)) * Master;
            }
            return ToFloats(buf);
        }

        // one cycle of the slow ringing under play, exactly one cycle long, so it loops seamlessly
        public static float[] Ring()
        {
            double cycle = RING.STEP * RING.MOTIF.Length + RING.REST;
            var buf = new double[Length(cycle)];
            foreach (var (at, midi) in Rules.RingNotes(0, cycle)) Marimba(buf, midi, at, RING.VOLUME * Master);
            return ToFloats(buf);
        }

        // one soft marimba note: a sine with a quick, bright knock on top
        static void Marimba(double[] buf, int midi, double t0, double scale)
        {
            double f = 440 * Math.Pow(2, (midi - 69) / 12.0);
            foreach (var (mult, peak, decay) in new[] { (1.0, 1.0, 0.7), (4.0, 0.25, 0.07) })
                Sine(buf, f * mult, t0, t0 + decay + 0.05, new Param(1).Set(0, t0).Linear(peak, t0 + 0.005).Exp(0.0001, t0 + decay), scale);
        }

        static void BlipInto(double[] buf, double freq, double at, double vol) =>
            Sine(buf, freq, at, at + 0.1, Env(at, vol, 0.004, 0.02, 0.06), Master);

        static Param Env(double t0, double peak, double attack, double hold, double release) =>
            new Param(1).Set(0, t0).Linear(peak, t0 + attack).Set(peak, t0 + attack + hold).Linear(0, t0 + attack + hold + release);

        static void Sine(double[] buf, double freq, double start, double stop, Param gain, double scale)
        {
            int to = Math.Min(buf.Length, (int)Math.Ceiling(stop * Rate));
            for (var n = (int)Math.Ceiling(start * Rate); n < to; n++)
            {
                double t = (double)n / Rate;
                buf[n] += Math.Sin(2 * Math.PI * freq * (t - start)) * gain.At(t) * scale;
            }
        }

        static int Length(double seconds) => (int)Rules.RoundHalfUp(seconds * Rate);

        static float[] ToFloats(double[] buf) => Array.ConvertAll(buf, v => (float)v);

        // An AudioParam's automation as Web Audio runs it: a ramp starts from the event before it, and the last value holds.
        sealed class Param
        {
            enum Kind { Set, Linear, Exp }

            readonly double initial;
            readonly List<(Kind Kind, double V, double T)> events = new List<(Kind, double, double)>();

            public Param(double initial) => this.initial = initial;

            public Param Set(double v, double t) { events.Add((Kind.Set, v, t)); return this; }
            public Param Linear(double v, double t) { events.Add((Kind.Linear, v, t)); return this; }
            public Param Exp(double v, double t) { events.Add((Kind.Exp, v, t)); return this; }

            public double At(double t)
            {
                double pt = 0, pv = initial;
                foreach (var (kind, v, et) in events)
                {
                    if (t < et)
                        return kind == Kind.Linear ? pv + (v - pv) * (t - pt) / (et - pt)
                            : kind == Kind.Exp ? pv * Math.Pow(v / pv, (t - pt) / (et - pt))
                            : pv;
                    pt = et; pv = v;
                }
                return pv;
            }
        }

        // Web Audio's BiquadFilterNode "lowpass" at its default Q (a 1 dB resonance), per the Audio EQ Cookbook.
        sealed class LowPass
        {
            readonly double b0, b1, b2, a1, a2;
            double x1, x2, y1, y2;

            public LowPass(double freq, double qDb = 1)
            {
                double w0 = 2 * Math.PI * freq / Rate, cos = Math.Cos(w0);
                double alpha = Math.Sin(w0) / (2 * Math.Pow(10, qDb / 20)), a0 = 1 + alpha;
                b0 = (1 - cos) / 2 / a0; b1 = (1 - cos) / a0; b2 = b0;
                a1 = -2 * cos / a0; a2 = (1 - alpha) / a0;
            }

            public double Next(double x)
            {
                double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }
    }
}
