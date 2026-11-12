using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Later
{
    // The page around the game: Plugins/WebGL/Later.jslib, and the template's later.js, which handles the browser the way
    // web/later/src/main.js does. In the Editor: fullscreen works, a mouse, held sideways, keys from the Input System.
    public static class Browser
    {
        // tests hold the phone upright (or not) with this
        public static bool? UprightOverride;
        // tests press keys with this: "+key" down, "-key" up, "!" the window lost focus (key names as the browser's e.key)
        public static readonly Queue<string> KeyEventsForTests = new Queue<string>();
        // tests give a button keyboard focus with this ("fs", "close" or "")
        public static string FocusForTests = "";

        // what the game last told the page: the cursor, and where the buttons the page handles itself are (CSS px)
        public static string Cursor { get; private set; } = "";
        public static Rect FullscreenButton { get; private set; }
        public static Rect CloseButton { get; private set; }

        public static void SetCursor(string cursor)
        {
            Cursor = cursor;
            SetCursorOnPage(cursor);
        }

        public static void SetButtons(Rect fullscreen, Rect close)
        {
            FullscreenButton = fullscreen; CloseButton = close;
            SetButtonsOnPage(fullscreen, close);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int LaterCanFullscreen();
        [DllImport("__Internal")] static extern int LaterCoarsePointer();
        [DllImport("__Internal")] static extern int LaterUpright();
        [DllImport("__Internal")] static extern double LaterPixelRatio();
        [DllImport("__Internal")] static extern void LaterSetTitle(string title);
        [DllImport("__Internal")] static extern string LaterTakeKeys();
        [DllImport("__Internal")] static extern void LaterSetPlaying(int playing);
        [DllImport("__Internal")] static extern double LaterPointerTime();
        [DllImport("__Internal")] static extern void LaterSetCursor(string cursor);
        [DllImport("__Internal")] static extern void LaterSetButtons(float fx, float fy, float fw, float fh, float cx, float cy, float cw, float ch);
        [DllImport("__Internal")] static extern int LaterFocused();

        public static bool CanFullscreen => LaterCanFullscreen() != 0;
        public static bool CoarsePointer => LaterCoarsePointer() != 0;
        public static bool Upright => UprightOverride ?? LaterUpright() != 0;
        public static double PixelRatio => LaterPixelRatio();
        public static void SetTitle(string title) => LaterSetTitle(title);

        // the key events since the last call, in order
        public static IEnumerable<string> TakeKeyEvents()
        {
            string s = LaterTakeKeys();
            return string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Split('\n');
        }

        // the page stops arrow keys scrolling only during play, as main.js does
        public static void SetPlaying(bool playing) => LaterSetPlaying(playing ? 1 : 0);

        // when the browser saw the latest press (seconds on the page's clock), for the double-tap
        public static double? PointerTime => LaterPointerTime() / 1000.0;

        // which button has keyboard focus: "close" (Show less), "fs" or ""
        public static string Focused => LaterFocused() switch { 1 => "close", 2 => "fs", _ => "" };
        static void SetCursorOnPage(string cursor) => LaterSetCursor(cursor);
        static void SetButtonsOnPage(Rect f, Rect c) => LaterSetButtons(f.x, f.y, f.width, f.height, c.x, c.y, c.width, c.height);
#else
        public static bool CanFullscreen => true;
        public static bool CoarsePointer => false;
        public static bool Upright => UprightOverride ?? false;
        public static double PixelRatio => 1;
        public static void SetTitle(string title) { }

        static readonly (Key Key, string Name)[] EditorKeys =
        {
            (Key.LeftArrow, "ArrowLeft"), (Key.A, "a"), (Key.RightArrow, "ArrowRight"), (Key.D, "d"),
            (Key.UpArrow, "ArrowUp"), (Key.W, "w"), (Key.DownArrow, "ArrowDown"), (Key.S, "s")
        };

        public static IEnumerable<string> TakeKeyEvents()
        {
            var events = new List<string>();
            while (KeyEventsForTests.Count > 0) events.Add(KeyEventsForTests.Dequeue());
            var kb = Keyboard.current;
            if (kb != null)
                foreach (var (key, name) in EditorKeys)
                {
                    if (kb[key].wasPressedThisFrame) events.Add("+" + name);
                    if (kb[key].wasReleasedThisFrame) events.Add("-" + name);
                }
            return events;
        }

        public static void SetPlaying(bool playing) { }
        public static double? PointerTime => null;
        public static string Focused => FocusForTests;
        static void SetCursorOnPage(string cursor) { }
        static void SetButtonsOnPage(Rect f, Rect c) { }
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
