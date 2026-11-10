using System;
using static Later.LaterData;

namespace Later
{
    // Mouse: the moon follows the pointer over the stage. Touch: the moon moves with one finger from wherever it lands,
    // anywhere on the screen (thumbs rest in the margins beside the stage on phones), so a thumb never covers it.
    // Ported from the pointer handlers in web/later/src/main.js. Positions are in world units; `now` is in seconds.
    public sealed class DragInput
    {
        // phones follow a tap with mouse events of their own (browsers' compatibility events); a mouse only steers
        // once touches have been quiet this long, so a tap never pulls the moon to where the finger lifted
        const double MouseQuiet = 1.0;

        int id;
        bool dragging;
        Vec finger, moon;
        double lastTouch = double.NegativeInfinity;

        public Vec? Target { get; private set; }

        // a new round: no target and no drag (but a tap's mouse events, still to come, stay ignored)
        public void Reset()
        {
            Target = null;
            dragging = false;
        }

        public void MouseMove(Vec world, double now)
        {
            if (!dragging && now - lastTouch >= MouseQuiet) Target = world;
        }

        // after using keys, the moon stays where the keys left it
        public void ClearTarget() => Target = null;

        public void TouchDown(int pointerId, Vec world, Round r, double now)
        {
            lastTouch = now;
            if (dragging) return;
            dragging = true; id = pointerId; finger = world; moon = r.Me;
            Target = r.Me;
        }

        public void TouchMove(int pointerId, Vec world, Round r, double now)
        {
            lastTouch = now;
            if (!dragging || pointerId != id) return;
            var want = new Vec(moon.X + world.X - finger.X, moon.Y + world.Y - finger.Y);
            var target = new Vec(Math.Max(26, Math.Min(r.W - 26, want.X)), Math.Max(TOP, Math.Min(BOTTOM, want.Y)));
            Target = target;
            // past an edge, move the anchor instead, so turning back responds straight away
            finger.X += want.X - target.X; finger.Y += want.Y - target.Y;
        }

        public void TouchUp(int pointerId, double now)
        {
            lastTouch = now;
            if (dragging && pointerId == id) dragging = false;
        }

        // the finger's lift will never arrive (the app lost focus mid-drag): let the next finger take over
        public void CancelTouch() => dragging = false;
    }
}
