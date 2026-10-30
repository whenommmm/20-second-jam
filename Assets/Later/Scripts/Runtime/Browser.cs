using System.Runtime.InteropServices;
using UnityEngine;

namespace Later
{
    // The page around the game (Plugins/WebGL/Later.jslib). In the Editor: fullscreen works, a mouse, held sideways.
    public static class Browser
    {
        // tests hold the phone upright (or not) with this
        public static bool? UprightOverride;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int LaterCanFullscreen();
        [DllImport("__Internal")] static extern int LaterCoarsePointer();
        [DllImport("__Internal")] static extern int LaterUpright();
        [DllImport("__Internal")] static extern double LaterPixelRatio();
        [DllImport("__Internal")] static extern void LaterSetTitle(string title);

        public static bool CanFullscreen => LaterCanFullscreen() != 0;
        public static bool CoarsePointer => LaterCoarsePointer() != 0;
        public static bool Upright => UprightOverride ?? LaterUpright() != 0;
        public static double PixelRatio => LaterPixelRatio();
        public static void SetTitle(string title) => LaterSetTitle(title);
#else
        public static bool CanFullscreen => true;
        public static bool CoarsePointer => false;
        public static bool Upright => UprightOverride ?? false;
        public static double PixelRatio => 1;
        public static void SetTitle(string title) { }
#endif

        // The page's #hash, like "#test-dodge", or "".
        public static string Hash
        {
            get
            {
                string url = Application.absoluteURL ?? "";
                int i = url.IndexOf('#');
                return i < 0 ? "" : url.Substring(i);
            }
        }
    }
}
