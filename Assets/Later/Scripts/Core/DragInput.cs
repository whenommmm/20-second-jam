using System;
using static Later.LaterData;

namespace Later
{
    // Mouse: the moon follows the pointer over the stage. Touch: the moon moves with one finger from wherever it lands,
    // anywhere on the screen (thumbs rest in the margins beside the stage on phones), so a thumb never covers it.
    // Ported from the pointer handlers in web/later/src/main.js. Positions are in world units.
    public sealed class DragInput
    {
        int id;
        bool dragging;
        Vec finger, moon;

        public Vec? Target { get; private set; }

        public void Reset()
        {
            Target = null;
            dragging = false;
        }

        // a mouse can't steer while a finger is dragging (some browsers send both for one touch)
        public void MouseMove(Vec world)
        {
            if (!dragging) Target = world;
        }

        // after using keys, the moon stays where the keys left it
        public void ClearTarget() => Target = null;

        public void TouchDown(int pointerId, Vec world, Round r)
        {
            if (dragging) return;
            dragging = true; id = pointerId; finger = world; moon = r.Me;
            Target = r.Me;
        }

        public void TouchMove(int pointerId, Vec world, Round r)
        {
            if (!dragging || pointerId != id) return;
            var want = new Vec(moon.X + world.X - finger.X, moon.Y + world.Y - finger.Y);
            var target = new Vec(Math.Max(26, Math.Min(r.W - 26, want.X)), Math.Max(TOP, Math.Min(BOTTOM, want.Y)));
            Target = target;
            // past an edge, move the anchor instead, so turning back responds straight away
            finger.X += want.X - target.X; finger.Y += want.Y - target.Y;
        }

        public void TouchUp(int pointerId)
        {
            if (dragging && pointerId == id) dragging = false;
        }
    }
}
