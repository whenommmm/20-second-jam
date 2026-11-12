using NUnit.Framework;

namespace Later.Tests
{
    // The keys as web/later/src/main.js reads them: the browser's key names (so the letters, not their positions, on any
    // layout), a set of held keys, a keydown only counts during play, and every keydown (auto-repeats too) means
    // "keys, not the pointer".
    public class KeyStateTests
    {
        [Test]
        public void ArrowsAndWasd_Steer_AndOppositeKeysCancel()
        {
            var k = new KeyState();
            Assert.IsTrue(k.Down("ArrowLeft", true));
            Assert.AreEqual((-1, 0), (k.X, k.Y));
            k.Down("w", true);
            Assert.AreEqual((-1, -1), (k.X, k.Y));
            k.Down("d", true);
            Assert.AreEqual((0, -1), (k.X, k.Y));
            k.Up("ArrowLeft");
            Assert.AreEqual((1, -1), (k.X, k.Y));
        }

        [Test]
        public void ALetterWithShift_IsTheSameKey()
        {
            var k = new KeyState();
            k.Down("A", true);
            Assert.AreEqual(-1, k.X);
            k.Up("a");
            Assert.AreEqual(0, k.X);
        }

        [Test]
        public void AKeyPressedBeforePlay_OnlyCountsFromItsNextKeydown()
        {
            var k = new KeyState();
            Assert.IsFalse(k.Down("d", false));
            Assert.AreEqual(0, k.X);
            Assert.IsTrue(k.Down("d", true)); // its auto-repeat, once the round is on
            Assert.AreEqual(1, k.X);
        }

        [Test]
        public void EveryKeydownDuringPlay_IncludingRepeats_TakesOverFromThePointer()
        {
            var k = new KeyState();
            Assert.IsTrue(k.Down("ArrowUp", true));
            Assert.IsTrue(k.Down("ArrowUp", true));
            Assert.IsFalse(k.Down(" ", true));
            Assert.IsFalse(k.Down("Enter", true));
        }

        [Test]
        public void ReleasingAKey_CountsWhateverTheState()
        {
            var k = new KeyState();
            k.Down("s", true);
            k.Up("s");
            Assert.AreEqual(0, k.Y);
        }

        [Test]
        public void HeldKeys_CarryIntoTheNextRound()
        {
            var k = new KeyState();
            k.Down("ArrowRight", true);
            // TIME!, the ending, PLAY AGAIN: nothing resets the keys, as on the web
            Assert.AreEqual(1, k.X);
        }

        // not copied from the web: there, a key released while another window has focus stays held
        [Test]
        public void LosingFocus_ReleasesEveryKey()
        {
            var k = new KeyState();
            k.Down("ArrowRight", true);
            k.Down("ArrowDown", true);
            k.Clear();
            Assert.AreEqual((0, 0), (k.X, k.Y));
        }

    }
}
