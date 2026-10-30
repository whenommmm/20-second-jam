using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Later.Tests.LaterTestUtil;

namespace Later.Tests
{
    // Not a check: renders the game to Builds/shots/dev-*.png for a look while porting.
    // Run with: GRAPHICS=1 tools/unity.sh test playmode Later.Tests.DevShots
    [Explicit]
    public class DevShots
    {
        [TearDown] public void TearDown() => Unload();

        static IEnumerator Shot(string name, int w, int h)
        {
            var doc = Object.FindFirstObjectByType<UIDocument>();
            var rt = doc.panelSettings.targetTexture;
            for (var i = 0; i < 3; i++) yield return null;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("Builds/shots");
            File.WriteAllBytes($"Builds/shots/dev-{name}.png", tex.EncodeToPNG());
        }

        static IEnumerator LoadInto(int w, int h)
        {
            yield return Load(new Vector2Int(w, h));
            var doc = Object.FindFirstObjectByType<UIDocument>();
            var panel = Object.Instantiate(doc.panelSettings);
            panel.targetTexture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            panel.clearColor = true;
            panel.colorClearValue = Color.magenta;
            doc.panelSettings = panel;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TitleAndPlay([Values(960)] int w)
        {
            yield return LoadInto(w, 627);
            yield return Shot($"{w}-title", w, 627);
            Game.StartRound();
            while (Game.Round.T < 10) { Game.ForcedTarget = Bots.DodgeTarget(Game.Round, 340); yield return null; }
            yield return Shot($"{w}-play10", w, 627);
        }
    }
}
