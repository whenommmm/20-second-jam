using System;
using System.Collections.Generic;

namespace Later
{
    // The keys, read as web/later/src/main.js reads them: by the browser's key names (the letters on any keyboard layout,
    // and the number pad's arrows), a set of held keys, a keydown that only counts during play, and every keydown
    // (auto-repeats too) meaning "keys, not the pointer". Unlike the web version, losing focus releases every key.
    public sealed class KeyState
    {
        static readonly Dictionary<string, (int X, int Y)> Map = new Dictionary<string, (int, int)>
        {
            ["ArrowLeft"] = (-1, 0), ["a"] = (-1, 0), ["ArrowRight"] = (1, 0), ["d"] = (1, 0),
            ["ArrowUp"] = (0, -1), ["w"] = (0, -1), ["ArrowDown"] = (0, 1), ["s"] = (0, 1)
        };

        readonly HashSet<string> held = new HashSet<string>();

        public int X { get; private set; }
        public int Y { get; private set; }

        // a single character counts whatever its case ("A" with Shift is "a"), as in main.js
        static string Normalize(string key) => key.Length == 1 ? key.ToLowerInvariant() : key;

        // A key went down. True when it steers (during play): the pointer target should then be dropped.
        public bool Down(string key, bool playing)
        {
            string k = Normalize(key);
            if (!Map.ContainsKey(k) || !playing) return false;
            held.Add(k);
            Read();
            return true;
        }

        public void Up(string key)
        {
            held.Remove(Normalize(key));
            Read();
        }

        public void Clear()
        {
            held.Clear();
            Read();
        }

        void Read()
        {
            int x = 0, y = 0;
            foreach (var k in held) { x += Map[k].X; y += Map[k].Y; }
            X = Math.Sign(x); Y = Math.Sign(y);
        }
    }
}
