using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace Later.Editor
{
    // One-time project setup, runnable in batch mode (tools/unity.sh run Later.Editor.LaterSetup.<Method>).
    public static class LaterSetup
    {
        static readonly (string Ttf, string Name)[] Fonts =
        {
            ("Anton-Regular", "Anton"), ("ArchivoBlack-Regular", "ArchivoBlack"), ("Newsreader-Italic", "Newsreader-Italic"),
            ("Inter-SemiBold", "Inter-SemiBold"), ("Inter-Bold", "Inter-Bold")
        };

        // A dynamic TextCore font asset for each bundled font. Existing assets are kept, so the stylesheet's references hold.
        [MenuItem("LATER/Make Fonts")]
        public static void MakeFonts()
        {
            foreach (var (ttf, name) in Fonts)
            {
                string path = $"Assets/Later/Fonts/{name} SDF.asset";
                if (AssetDatabase.LoadAssetAtPath<FontAsset>(path) != null) continue;
                var font = AssetDatabase.LoadAssetAtPath<Font>($"Assets/Later/Fonts/{ttf}.ttf");
                var asset = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                asset.name = $"{name} SDF";
                AssetDatabase.CreateAsset(asset, path);
                asset.atlasTextures[0].name = $"{name} SDF Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                asset.material.name = $"{name} SDF Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
