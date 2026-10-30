using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TextCore.Text;
using static Later.LaterData;

namespace Later.Tests
{
    // Every string the game shows, in each font that draws it, must have every glyph (…, ×, ·, ’ and friends included).
    public class FontTests
    {
        [OneTimeTearDown] public void ClearAddedGlyphs() => Later.Editor.FontHygiene.Clear();

        static readonly string[] Callers = CALLS.Select(c => c.Who).Distinct().ToArray();

        static string[] Dates() => new[] { START_DATE }.Concat(CALLS.Select(c => c.Date)).Concat(QUIET_DATES.Select(q => q.Date)).ToArray();

        static void AssertDraws(string font, IEnumerable<string> texts)
        {
            var asset = AssetDatabase.LoadAssetAtPath<FontAsset>($"Assets/Later/Fonts/{font} SDF.asset");
            Assert.IsNotNull(asset, $"no font asset for {font}");
            asset.HasCharacters(string.Concat(texts), out uint[] missing, false, true);
            var names = (missing ?? new uint[0]).Distinct().Select(c => $"'{char.ConvertFromUtf32((int)c)}' U+{c:X4}");
            CollectionAssert.IsEmpty(names, $"{font} can't draw: {string.Join(", ", names)}");
        }

        [Test]
        public void Anton_DrawsTheGamesShoutedWords()
        {
            var loud = TURN_BACK.Keys.SelectMany(k => new[] { 29, 1, 2 }.Select(n => Rules.TurnBackLines(k, n).Loud));
            AssertDraws("Anton", new[] { "LATER", "CLICK TO START", "TAP TO START", "TIME!", "PERFECT ×29", "CLOSE!", "0123456789,-/×",
                "DODGED", "CLOSE CALLS", "BEST STREAK", "PERFECT!", "GREAT!", "NICE TRY", "…or is it?", "ROTATE YOUR DEVICE",
                "TURN BACK TIME", LESSON }.Concat(Dates()).Concat(loud));
        }

        [Test]
        public void ArchivoBlack_DrawsNamesAndLabels() =>
            AssertDraws("ArchivoBlack", Callers.Concat(new[] { "LATER", "RESULTS", "FULLSCREEN", "PLAY AGAIN", "0123456789" }).Concat(CAPTION_HEADERS.Values));

        [Test]
        public void NewsreaderItalic_DrawsHerWords() =>
            AssertDraws("Newsreader-Italic", new[] { VOICEMAIL, LAST_CALL }.Concat(TURN_BACK.Values.Select(v => v[1])));

        [Test]
        public void InterSemiBold_DrawsThePhone() =>
            AssertDraws("Inter-SemiBold", FOLLOW_UP.Concat(PENDING).Concat(FINAL.Values.SelectMany(m => new[] { m.Who, m.Text })).Concat(Callers)
                .Concat(new[] { VOICEMAIL, "Thursday, April 2", "7:12", "Later", "Notifications silenced", "Missed Calls", "Voicemail · 0:18",
                    "Show less", "more messages", "5G", "0123456789" })
                .Concat(Dates().Select(Rules.ShortDate)));

        [Test]
        public void InterBold_DrawsTheSheetTitle() => AssertDraws("Inter-Bold", new[] { "Messages" });
    }
}
