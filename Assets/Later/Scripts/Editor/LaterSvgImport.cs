using Unity.VectorGraphics;
using Unity.VectorGraphics.Editor;
using UnityEditor;

namespace Later.Editor
{
    // The icons in UI/Icons import as UI Toolkit Vector Images. Each SVG starts with a transparent 24×24 rect: a Vector Image
    // is cropped to what it draws whatever the viewport option, and the rect keeps the viewBox, so icons sit as they do on the web.
    public class LaterSvgImport : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith("Assets/Later/UI/Icons/") || !(assetImporter is SVGImporter svg)) return;
            svg.SvgType = SVGType.VectorImage;
            svg.ViewportOptions = ViewportOptions.PreserveViewport;
        }
    }
}
