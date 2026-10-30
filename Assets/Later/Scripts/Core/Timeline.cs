using System;
using System.Collections.Generic;

namespace Later
{
    // Stands in for setTimeout and the Web Animations the web version uses: callbacks and tweens on one clock,
    // advanced by Update. Everything due runs in time order, each at its own time, so a callback that schedules
    // another counts from when it ran, not from the end of the frame. Clear cancels everything still pending.
    public sealed class Timeline
    {
        sealed class Job { public double At; public long Seq; public Action Run; }
        sealed class Tweening { public double Start, Duration; public Func<double, double> Ease; public Action<double> Apply; }

        readonly List<Job> jobs = new List<Job>();
        readonly List<Tweening> tweens = new List<Tweening>();
        long seq;

        public double Now { get; private set; }

        public void After(double seconds, Action fn) => jobs.Add(new Job { At = Now + seconds, Seq = seq++, Run = fn });

        // Calls apply(ease(p)) every Update from delay until delay + duration, then apply(ease(1)) once, then done.
        public void Tween(double delay, double duration, Func<double, double> ease, Action<double> apply, Action done = null)
        {
            var tw = new Tweening { Start = Now + delay, Duration = duration, Ease = ease, Apply = apply };
            tweens.Add(tw);
            After(delay + duration, () =>
            {
                tweens.Remove(tw);
                apply(ease(1));
                done?.Invoke();
            });
        }

        public void Update(double dt)
        {
            double end = Now + dt;
            for (Job next; (next = NextDue(end)) != null;)
            {
                jobs.Remove(next);
                Now = Math.Max(Now, next.At);
                next.Run();
            }
            Now = end;
            foreach (var tw in tweens.ToArray())
                if (Now >= tw.Start) tw.Apply(tw.Ease((Now - tw.Start) / tw.Duration));
        }

        public void Clear()
        {
            jobs.Clear();
            tweens.Clear();
        }

        Job NextDue(double end)
        {
            Job next = null;
            foreach (var j in jobs)
                if (j.At <= end && (next == null || j.At < next.At || (j.At == next.At && j.Seq < next.Seq))) next = j;
            return next;
        }
    }
}
