using System;
using System.Collections.Generic;
using static Later.LaterData;

namespace Later
{
    // The web version's sounds (web/later/src/sound.js), made sample by sample at the rate the browser's audio runs at:
    // the same oscillators (a band-limited triangle, like an OscillatorNode's), envelopes and filters, with Web Audio's
    // own rules for gain automation and its biquad low-pass. Every sound is already through the master gain (0.55);
    // the ring also through RING.VOLUME.
    public sealed class Synth
    {
        const double Master = 0.55;

        public readonly int Rate;
        readonly Dictionary<double, double[]> triangles = new Dictionary<double, double[]>();

        public Synth(int rate) => Rate = rate;

        public float[] Buzz()
        {
            // a phone vibrating on a table, once as each call comes in: a soft, low double pulse (bzz… bzz)
            var buf = new double[Length(0.4)];
            var g = new Param(1).Set(0, 0);
            foreach (var at in new[] { 0, 0.2 }) g.Linear(0.11, at + 0.02).Set(0.11, at + 0.1).Linear(0, at + 0.14);
            var lp = new LowPass(280, Rate);
            for (var n = 0; n < buf.Length; n++)
            {
                double t = (double)n / Rate;
                buf[n] = lp.Next(Triangle(165, 165 * t)) * g.At(t) * Master;
            }
            return ToFloats(buf);
        }

        // each dodge climbs a semitone with the streak
        public float[] Blip(int streak)
        {
            var buf = new double[Length(0.1)];
            BlipInto(buf, 523 * Math.Pow(2, Math.Min(streak, 14) / 12.0), 0, 0.09);
            return ToFloats(buf);
        }

        public float[] Close()
        {
            var buf = new double[Length(0.16)];
            BlipInto(buf, 880, 0, 0.1);
            BlipInto(buf, 1320, 0.06, 0.1);
            return ToFloats(buf);
        }

        // touching a call: a dull thud, not a voice; `noise` fills its 0.3 s of noise (the web uses Math.random, once a session)
        public float[] Thud(Random noise)
        {
            var buf = new double[Length(0.3)];
            var freq = new Param(440).Set(110, 0).Exp(45, 0.25);
            Param g = Env(0, 0.38, 0.005, 0.05, 0.2), ng = Env(0, 0.22, 0.003, 0.03, 0.09);
            var lp = new LowPass(300, Rate);
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
        public float[] Ring()
        {
            double cycle = RING.STEP * RING.MOTIF.Length + RING.REST;
            var buf = new double[Length(cycle)];
            foreach (var (at, midi) in Rules.RingNotes(0, cycle)) Marimba(buf, midi, at, RING.VOLUME * Master);
            return ToFloats(buf);
        }

        // The ringing from `pos` seconds into its cycle as it stops: the notes still sounding and those the web's
        // scheduler had already queued (it looks 0.4 s ahead every 0.1 s, so on average those starting in the next
        // `lookAhead` s), through the web's linear fade to silence over `fade` s.
        public float[] RingTail(double pos, double fade, double lookAhead = 0.35)
        {
            var buf = new double[Length(fade)];
            foreach (var (at, midi) in Rules.RingNotes(Math.Max(0, pos - 0.8), pos + lookAhead)) Marimba(buf, midi, at - pos, RING.VOLUME * Master);
            for (var n = 0; n < buf.Length; n++) buf[n] *= Math.Max(0, 1 - (double)n / Rate / fade);
            return ToFloats(buf);
        }

        // An OscillatorNode's "triangle" at `freq`, `cycles` periods in: its odd harmonics below the Nyquist frequency,
        // 8 / (π n)² each with alternating sign, scaled to a peak of 1; read from one period's table, as browsers do.
        public double Triangle(double freq, double cycles)
        {
            if (!triangles.TryGetValue(freq, out var table))
            {
                const int size = 4096;
                table = new double[size + 1];
                double norm = 0;
                for (int k = 1; k * freq < Rate / 2.0; k += 2) norm += 8 / (Math.PI * Math.PI * k * k);
                for (var i = 0; i <= size; i++)
                {
                    double sum = 0, x = (double)i / size;
                    for (int k = 1; k * freq < Rate / 2.0; k += 2)
                        sum += 8 / (Math.PI * Math.PI * k * k) * ((k / 2) % 2 == 0 ? 1 : -1) * Math.Sin(2 * Math.PI * k * x);
                    table[i] = sum / norm;
                }
                triangles[freq] = table;
            }
            double p = (cycles - Math.Floor(cycles)) * (table.Length - 1);
            int j = (int)p;
            return j >= table.Length - 1 ? table[j] : table[j] + (table[j + 1] - table[j]) * (p - j);
        }

        // one soft marimba note: a sine with a quick, bright knock on top
        void Marimba(double[] buf, int midi, double t0, double scale)
        {
            double f = 440 * Math.Pow(2, (midi - 69) / 12.0);
            foreach (var (mult, peak, decay) in new[] { (1.0, 1.0, 0.7), (4.0, 0.25, 0.07) })
                Sine(buf, f * mult, t0, t0 + decay + 0.05, new Param(1).Set(0, t0).Linear(peak, t0 + 0.005).Exp(0.0001, t0 + decay), scale);
        }

        void BlipInto(double[] buf, double freq, double at, double vol) =>
            Sine(buf, freq, at, at + 0.1, Env(at, vol, 0.004, 0.02, 0.06), Master);

        static Param Env(double t0, double peak, double attack, double hold, double release) =>
            new Param(1).Set(0, t0).Linear(peak, t0 + attack).Set(peak, t0 + attack + hold).Linear(0, t0 + attack + hold + release);

        void Sine(double[] buf, double freq, double start, double stop, Param gain, double scale)
        {
            int to = Math.Min(buf.Length, (int)Math.Ceiling(stop * Rate));
            for (var n = Math.Max(0, (int)Math.Ceiling(start * Rate)); n < to; n++)
            {
                double t = (double)n / Rate;
                buf[n] += Math.Sin(2 * Math.PI * freq * (t - start)) * gain.At(t) * scale;
            }
        }

        int Length(double seconds) => (int)Rules.RoundHalfUp(seconds * Rate);

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

            public LowPass(double freq, int rate, double qDb = 1)
            {
                double w0 = 2 * Math.PI * freq / rate, cos = Math.Cos(w0);
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
