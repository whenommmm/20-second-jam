using System.IO;
using System.Text.RegularExpressions;
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
    
        // browsers without dvh (Safari before 15.4, Chrome before 108) drop that height and draw the canvas 150 px tall
        [Test]
        public void Template_CanvasFillsTheWindow_EvenWhereDvhIsUnsupported()
        {
            var css = Regex.Match(File.ReadAllText("Assets/WebGLTemplates/Later/index.html"), @"#unity-canvas\s*\{([^}]*)\}").Groups[1].Value;
            var heights = Regex.Matches(css, @"height:\s*([^;]+);");
            Assert.That(heights.Count, Is.GreaterThanOrEqualTo(2), "the canvas has no height to fall back on: " + css);
            Assert.AreEqual("100vh", heights[0].Groups[1].Value.Trim(), "the fallback must come first");
            Assert.AreEqual("100dvh", heights[heights.Count - 1].Groups[1].Value.Trim());
        }
    }
}
