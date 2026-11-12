using NUnit.Framework;
using Unity.VectorGraphics;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Later.Tests
{
    // The web version's inline SVG icons, imported as UI Toolkit Vector Images that keep their 24×24 viewBox.
    public class IconTests
    {
        [TestCase("moon"), TestCase("handset"), TestCase("bubble"), TestCase("flashlight"), TestCase("camera"), TestCase("fullscreen")]
        public void Icon_ImportsAsAVectorImage_KeepingItsViewBox(string name)
        {
            string path = $"Assets/Later/UI/Icons/{name}.svg";
            var importer = (SVGImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(SVGType.VectorImage, importer.SvgType);
            Assert.AreEqual(ViewportOptions.PreserveViewport, importer.ViewportOptions, "the icon would be cropped to its path and sit off-centre");
            var image = AssetDatabase.LoadAssetAtPath<VectorImage>(path);
            Assert.IsNotNull(image);
            Assert.AreEqual(24, image.width, 0.01);
            Assert.AreEqual(24, image.height, 0.01);
        }

        // the web's icon is stroked with square caps and sharp corners; the vector importer rounds strokes off, so this
        // one is the same shape as filled outlines
        [Test]
        public void TheFullscreenIcon_IsFilledOutlines_NotAStroke()
        {
            string svg = System.IO.File.ReadAllText("Assets/Later/UI/Icons/fullscreen.svg");
            StringAssert.DoesNotContain("stroke", svg);
            StringAssert.Contains("M2.8 10.2V2.8h7.4v2.4H5.2v5z", svg);
        }
    }
}
