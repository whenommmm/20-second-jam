using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Later.Editor
{
    // The jam build: WebGL for itch.io, zipped with index.html at the zip's root, at most 10 MB.
    public static class LaterBuild
    {
        const string Dir = "Builds/LATER-webgl", Zip = "Builds/LATER-webgl.zip";
        const long MaxZip = 10_485_760;

        [MenuItem("LATER/Build WebGL")]
        public static void WebGL()
        {
            PlayerSettings.productName = "LATER";
            PlayerSettings.WebGL.template = "PROJECT:Later";
            // itch.io serves the compressed files without a Content-Encoding header, so the loader decompresses them itself
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            // a hidden tab pauses the game, as on the web
            PlayerSettings.runInBackground = false;

            if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Later/Scenes/Later.unity" },
                locationPathName = Dir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"WebGL build {report.summary.result} with {report.summary.totalErrors} errors");

            if (File.Exists(Zip)) File.Delete(Zip);
            ZipFile.CreateFromDirectory(Dir, Zip, System.IO.Compression.CompressionLevel.Optimal, false);
            long size = new FileInfo(Zip).Length;
            var en = System.Globalization.CultureInfo.InvariantCulture;
            Debug.Log(string.Format(en, "LATER-webgl.zip: {0:N0} bytes (limit {1:N0})", size, MaxZip));
            if (size > MaxZip) throw new Exception(string.Format(en, "{0} is {1:N0} bytes, over the jam's {2:N0}", Zip, size, MaxZip));
        }
    }
}
