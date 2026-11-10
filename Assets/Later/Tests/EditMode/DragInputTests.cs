using NUnit.Framework;

namespace Later.Tests
{
    // The intent of the browser checks touch, touchmargin and touchclamp: the moon moves with one finger from wherever it
    // lands, even in the bars beside the stage, and turning back from past an edge responds straight away.
    public class DragInputTests
    {
        [Test]
        public void ATouchInTheSideBar_MovesTheMoonByHowFarTheFingerMoves()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(-100, 270), r, 0);
            input.TouchMove(1, new Vec(-50, 290), r, 0);
            Assert.AreEqual(new Vec(r.Me.X + 50, r.Me.Y + 20), input.Target);
        }

        [Test]
        public void DraggingPastTheEdge_ThenBack_RespondsStraightAway()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(500, 270), r, 0);
            double edge = r.W - 26 - r.Me.X;
            input.TouchMove(1, new Vec(500 + edge + 300, 270), r, 0);
            Assert.AreEqual(r.W - 26, input.Target.Value.X);
            input.TouchMove(1, new Vec(500 + edge + 290, 270), r, 0);
            Assert.AreEqual(r.W - 26 - 10, input.Target.Value.X);
        }

        [Test]
        public void ASecondFinger_IsIgnoredWhileTheFirstIsDown()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r, 0);
            input.TouchDown(2, new Vec(600, 100), r, 0);
            input.TouchMove(2, new Vec(700, 200), r, 0);
            Assert.AreEqual(r.Me, input.Target);
            input.TouchUp(2, 0);
            input.TouchMove(1, new Vec(110, 270), r, 0);
            Assert.AreEqual(new Vec(r.Me.X + 10, r.Me.Y), input.Target);
        }

        [Test]
        public void TheMouse_SetsTheTargetWhereItIs()
        {
            var input = new DragInput();
            input.MouseMove(new Vec(400, 123), 5);
            Assert.AreEqual(new Vec(400, 123), input.Target);
            input.Reset();
            Assert.IsNull(input.Target);
        }
    
        [Test]
        public void TheKeys_ClearThePointerTarget_SoTheMoonStaysWhereTheKeysLeftIt()
        {
            var input = new DragInput();
            input.MouseMove(new Vec(400, 123), 5);
            input.ClearTarget();
            Assert.IsNull(input.Target);
        }

        [Test]
        public void TheMouse_IsIgnoredWhileAFingerIsDragging()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r, 0);
            input.MouseMove(new Vec(700, 50), 5);
            Assert.AreEqual(r.Me, input.Target);
        }
    
        // phones follow a tap with mouse events of their own; they must not pull the moon to where the finger lifted
        [Test]
        public void TheMouse_IsIgnoredForASecondAfterATouch()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r, 10);
            input.TouchUp(1, 10.1);
            input.MouseMove(new Vec(700, 50), 10.3);
            Assert.AreEqual(r.Me, input.Target, "a mouse event right after a tap moved the moon");
            input.Reset(); // TAP TO START resets the input, and the tap's own mouse events still follow it
            input.MouseMove(new Vec(700, 50), 10.5);
            Assert.IsNull(input.Target);
            input.MouseMove(new Vec(700, 50), 11.2);
            Assert.AreEqual(new Vec(700, 50), input.Target, "a real mouse, a second later, doesn't steer");
        }

        // a finger whose lift never arrives (the player switched apps) mustn't lock out every later touch
        [Test]
        public void ALostLift_DoesntLockOutTheNextFinger()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r, 0);
            input.CancelTouch();
            input.TouchDown(2, new Vec(300, 270), r, 1);
            input.TouchMove(2, new Vec(350, 270), r, 1);
            Assert.AreEqual(new Vec(r.Me.X + 50, r.Me.Y), input.Target);
        }
    }
}
