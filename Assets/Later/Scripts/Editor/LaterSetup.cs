using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Later.Editor
{
    // One-time project setup, runnable in batch mode (tools/unity.sh run Later.Editor.LaterSetup.<Method>).
    public static class LaterSetup
    {
        static readonly (string Ttf, string Name)[] Fonts =
        {
            ("Anton-Regular", "Anton"), ("ArchivoBlack-Regular", "ArchivoBlack"), ("Newsreader-Italic", "Newsreader-Italic"),
            ("Inter-Regular", "Inter-Regular"), ("Inter-SemiBold", "Inter-SemiBold"), ("Inter-Bold", "Inter-Bold")
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

        const string Ui = "Assets/Later/UI", ScenePath = "Assets/Later/Scenes/Later.unity";

        // The game's one scene: a camera that clears to the paper, an EventSystem for UI Toolkit input,
        // and LATER (the UI document, the game and its sound). Rebuilt from scratch every time.
        [MenuItem("LATER/Rebuild Scene")]
        public static void RebuildScene()
        {
            string themePath = $"{Ui}/LaterTheme.tss";
            if (!File.Exists(themePath)) File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(themePath);
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>($"{Ui}/LaterPanel.asset");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, $"{Ui}/LaterPanel.asset");
            }
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.scale = 1;
            panel.sortingOrder = 0;
            EditorUtility.SetDirty(panel);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(253, 220, 172, 255);
            cam.cullingMask = 0;
            cam.orthographic = true;
            cam.gameObject.AddComponent<AudioListener>();

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var later = new GameObject("LATER");
            var doc = later.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{Ui}/Later.uxml");
            var sound = later.AddComponent<Sound>();
            AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Later/Audio/{name}.wav");
            sound.buzz = Clip("buzz"); sound.close = Clip("close"); sound.thud = Clip("thud"); sound.ring = Clip("ring");
            sound.blips = Enumerable.Range(0, 15).Select(i => Clip($"blip{i:00}")).ToArray();
            later.AddComponent<LaterGame>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
