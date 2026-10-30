using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Later.Editor
{
    // Writes the game's sounds (Synth) to Assets/Later/Audio as 16-bit mono WAVs.
    public static class SoundBaker
    {
        const string Dir = "Assets/Later/Audio";

        [MenuItem("LATER/Bake Sounds")]
        public static void Bake()
        {
            Directory.CreateDirectory(Dir);
            Write("buzz", Synth.Buzz());
            for (var i = 0; i <= 14; i++) Write($"blip{i:00}", Synth.Blip(i));
            Write("close", Synth.Close());
            Write("thud", Synth.Thud());
            Write("ring", Synth.Ring());
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Dir }))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.forceToMono = true;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        static void Write(string name, float[] samples)
        {
            using var w = new BinaryWriter(File.Create($"{Dir}/{name}.wav"));
            int bytes = samples.Length * 2;
            w.Write("RIFF".ToCharArray()); w.Write(36 + bytes); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Synth.Rate); w.Write(Synth.Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(bytes);
            foreach (var s in samples) w.Write((short)Math.Round(Math.Max(-1, Math.Min(1, s)) * short.MaxValue));
        }
    }
}
