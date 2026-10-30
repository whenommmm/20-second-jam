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

        [Test]
        public void TheMouse_IsIgnoredWhileAFingerIsDragging()
        {
            var r = RoundSim.Create();
            var input = new DragInput();
            input.TouchDown(1, new Vec(100, 270), r);
            input.MouseMove(new Vec(700, 50));
            Assert.AreEqual(r.Me, input.Target);
        }
    }
}
