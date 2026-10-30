using UnityEditor;
using UnityEngine.TextCore.Text;

namespace Later.Editor
{
    // The bundled font assets are dynamic: playing and testing in the editor adds glyphs to them, and the editor would
    // save those. Clearing them whenever play mode ends keeps the committed assets empty (the build clears them anyway).
    [InitializeOnLoad]
    public static class FontHygiene
    {
        static FontHygiene() => EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredEditMode) Clear();
        };

        public static void Clear()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:FontAsset", new[] { "Assets/Later/Fonts" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                asset.ClearFontAssetData(true);
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
