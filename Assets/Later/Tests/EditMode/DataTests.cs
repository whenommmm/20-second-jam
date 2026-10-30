using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using static Later.LaterData;

namespace Later.Tests
{
    // Ported from web/later/tests/data.test.js.
    public class DataTests
    {
        [Test]
        public void Schedule_MatchesTheSpec()
        {
            Assert.AreEqual(14, CALLS.Length);
            Assert.AreEqual(29, CALLS.Sum(c => c.N));
            Assert.AreEqual(new CallDef(12.4, "Mom", 1, 280, "MAR 28", true), CALLS[13]);
            Assert.AreEqual(new CallDef(4.4, "Jessie", 3, 250, "MAR 15", false), CALLS[4]);
            CollectionAssert.AreEqual(new double[] { 54, 116, 178, 240, 302, 364, 426, 488 }, LANES);
        }

        [Test]
        public void AllTheText_IsThere_Verbatim()
        {
            Assert.AreEqual(13, FOLLOW_UP.Length);
            Assert.AreEqual(13, PENDING.Length);
            Assert.AreEqual("So good to hear your voice. Talk soon.", FOLLOW_UP[0]);
            Assert.AreEqual("Call Dad. Now.", PENDING[8]);
            Assert.AreEqual("She's sleeping most of the day now. We're all here.", PENDING[12]);
            StringAssert.IsMatch(@"^Hi sweetheart, it's just me\. No reason… .* I'm so proud of you\.$", VOICEMAIL);
            StringAssert.IsMatch(@"^Hi sweetheart… you picked up\.", LAST_CALL);
            Assert.AreEqual("Funeral's on Saturday.", FINAL["late"].Text);
            Assert.AreEqual("YOU GAVE HER FOUR MINUTES.", TURN_BACK["last"][0]);
        }

        [Test]
        public void EveryEnding_TurnBackTime_ClosesOnTheSameLesson()
        {
            Assert.AreEqual("SOMEONE IS WAITING FOR YOUR CALL.", LESSON);
        }
    }
}
