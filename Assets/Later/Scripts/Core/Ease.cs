using System;

namespace Later
{
    // CSS easing curves, solved the way browsers do (Newton's method, then bisection if that doesn't settle).
    public static class Ease
    {
        public static readonly Func<double, double> Linear = x => x;
        public static readonly Func<double, double> CssEase = CubicBezier(.25, .1, .25, 1);
        public static readonly Func<double, double> EaseOut = CubicBezier(0, 0, .58, 1);

        public static Func<double, double> CubicBezier(double x1, double y1, double x2, double y2)
        {
            double cx = 3 * x1, bx = 3 * (x2 - x1) - cx, ax = 1 - cx - bx;
            double cy = 3 * y1, by = 3 * (y2 - y1) - cy, ay = 1 - cy - by;
            double SampleX(double t) => ((ax * t + bx) * t + cx) * t;
            double SampleY(double t) => ((ay * t + by) * t + cy) * t;
            double SlopeX(double t) => (3 * ax * t + 2 * bx) * t + cx;

            double SolveX(double x)
            {
                double t = x;
                for (var i = 0; i < 8; i++)
                {
                    double err = SampleX(t) - x;
                    if (Math.Abs(err) < 1e-7) return t;
                    double d = SlopeX(t);
                    if (Math.Abs(d) < 1e-6) break;
                    t -= err / d;
                }
                double lo = 0, hi = 1;
                t = x;
                while (lo < hi)
                {
                    double v = SampleX(t);
                    if (Math.Abs(v - x) < 1e-7) return t;
                    if (x > v) lo = t; else hi = t;
                    double mid = (lo + hi) / 2;
                    if (mid == t) break;
                    t = mid;
                }
                return t;
            }

            return x => x <= 0 ? 0 : x >= 1 ? 1 : SampleY(SolveX(x));
        }
    }
}
