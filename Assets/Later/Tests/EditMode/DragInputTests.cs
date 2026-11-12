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
            input.TouchDown(1, new Vec(-100, 270), r);
            input.TouchMove(1, new Vec(-50, 290), r);
            Assert.AreEqual(new Vec(r.Me.X + 50, r.Me.Y + 20), input.Target);
        }

        [Test]
        public void DraggingPastTheEdge_ThenBack_RespondsStraightAway()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(500, 270), r);
            double edge = r.W - 26 - r.Me.X;
            input.TouchMove(1, new Vec(500 + edge + 300, 270), r);
            Assert.AreEqual(r.W - 26, input.Target.Value.X);
            input.TouchMove(1, new Vec(500 + edge + 290, 270), r);
            Assert.AreEqual(r.W - 26 - 10, input.Target.Value.X);
        }

        [Test]
        public void ASecondFinger_IsIgnoredWhileTheFirstIsDown()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r);
            input.TouchDown(2, new Vec(600, 100), r);
            input.TouchMove(2, new Vec(700, 200), r);
            Assert.AreEqual(r.Me, input.Target);
            input.TouchUp(2);
            input.TouchMove(1, new Vec(110, 270), r);
            Assert.AreEqual(new Vec(r.Me.X + 10, r.Me.Y), input.Target);
        }

        [Test]
        public void TheMouse_SetsTheTargetWhereItIs()
        {
            var input = new DragInput();
            input.MouseMove(new Vec(400, 123));
            Assert.AreEqual(new Vec(400, 123), input.Target);
            input.Reset();
            Assert.IsNull(input.Target);
        }
    
        [Test]
        public void TheKeys_ClearThePointerTarget_SoTheMoonStaysWhereTheKeysLeftIt()
        {
            var input = new DragInput();
            input.MouseMove(new Vec(400, 123));
            input.ClearTarget();
            Assert.IsNull(input.Target);
        }

        // as in main.js: a mouse move sets the target even mid-drag, and the finger's next move takes over again
        [Test]
        public void TheMouse_TakesOverMidDrag_UntilTheFingerMovesAgain()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r);
            input.MouseMove(new Vec(700, 50));
            Assert.AreEqual(new Vec(700, 50), input.Target);
            input.TouchMove(1, new Vec(110, 270), r);
            Assert.AreEqual(new Vec(r.Me.X + 10, r.Me.Y), input.Target);
        }

        // the browser sends the game no mouse events of its own after a tap (Unity cancels the touch), so nothing needs
        // filtering: a mouse steers straight after a click or a tap
        [Test]
        public void TheMouse_SteersStraightAfterAClickOrATap()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r);
            input.TouchUp(1);
            input.MouseMove(new Vec(700, 50));
            Assert.AreEqual(new Vec(700, 50), input.Target);
            input.TouchUp(0); // a mouse button released
            input.MouseMove(new Vec(650, 60));
            Assert.AreEqual(new Vec(650, 60), input.Target);
        }

        // a finger whose lift never arrives (the player switched apps) mustn't lock out every later touch
        [Test]
        public void ALostLift_DoesntLockOutTheNextFinger()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r);
            input.CancelTouch();
            input.TouchDown(2, new Vec(300, 270), r);
            input.TouchMove(2, new Vec(350, 270), r);
            Assert.AreEqual(new Vec(r.Me.X + 50, r.Me.Y), input.Target);
        }
    }
}
