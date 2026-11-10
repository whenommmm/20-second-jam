using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Later.Tests
{
    public class BuildSettingsTests
    {
        // browsers blend colours in gamma space: in linear space every see-through thing (the phone's cards, the
        // "Later" pill, her words fading in) comes out lighter than the web version
        [Test]
        public void ColorSpace_IsGamma_LikeTheBrowser() => Assert.AreEqual(ColorSpace.Gamma, PlayerSettings.colorSpace);
    }
}
