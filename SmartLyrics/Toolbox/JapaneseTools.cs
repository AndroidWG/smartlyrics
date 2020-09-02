﻿using static SmartLyrics.Common.Logging;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using WanaKanaNet;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;

namespace SmartLyrics.Toolbox
{
    internal static class JapaneseTools
    {
        #region Enumerators
        public enum TargetSyllabary
        {
            Romaji,
            Hiragana,
            Katakana
        }

        public enum Mode
        {
            Normal,
            Spaced,
            Okurigana,
            Furigana
        }

        public enum RomajiSystem
        {
            Nippon,
            Passport,
            Hepburn
        }
        #endregion

        //TODO: Add clearing up of romanized text, to eliminate spaces before and after //will be done on RomanizationService
        public static async Task<string> GetTransliteration(string text,
                                                            bool useHtml,
                                                            TargetSyllabary to = TargetSyllabary.Romaji,
                                                            Mode mode = Mode.Spaced,
                                                            RomajiSystem system = RomajiSystem.Hepburn)
        {
            string logString = text.Truncate(30);
            Log(Type.Processing, $"Getting transliteration for '{logString}'");
            Analytics.TrackEvent("Getting transliteration from RomanizationService", new Dictionary<string, string> {
                { "Input", logString },
                { "TargetSyllabary", to.ToString("G") }
            });

            // ReSharper disable InconsistentNaming
            //Maybe there's a better way of doing this? This is a tad long
            string _to = "";
            switch (to)
            {
                case TargetSyllabary.Romaji:
                    _to = "romaji";
                    break;
                case TargetSyllabary.Hiragana:
                    _to = "hiragana";
                    break;
                case TargetSyllabary.Katakana:
                    _to = "katakana";
                    break;
            }

            string _mode = "";
            switch (mode)
            {
                case Mode.Normal:
                    _mode = "normal";
                    break;
                case Mode.Spaced:
                    _mode = "spaced";
                    break;
                case Mode.Okurigana:
                    _mode = "okurigana";
                    break;
                case Mode.Furigana:
                    _mode = "furigana";
                    break;
            }

            string _system = "";
            switch (system)
            {
                case RomajiSystem.Nippon:
                    _system = "nippon";
                    break;
                case RomajiSystem.Passport:
                    _system = "passport";
                    break;
                case RomajiSystem.Hepburn:
                    _system = "hepburn";
                    break;
            }
            // ReSharper restore InconsistentNaming

            string queryParams = $"?to={_to}&mode={_mode}&romajiSystem={_system}&useHTML={useHtml.ToString().ToLowerInvariant()}";
            string transliterated = await HttpRequests.PostRequest(Globals.RomanizeConvertUrl + queryParams, text);

            return transliterated;
        }
        
        public static async Task<string> StripJapanese(this string input)
        {
            string logString = input.Truncate(30);
            Log(Type.Info, $"Processing string '{logString}'");

            //TODO: Add support for titles like Bakamitai ばかみたい (Romanized)

            if (WanaKana.IsJapanese(input))
            {
                string converted = await GetTransliteration(input, false);

                input = converted;
            }
            //TODO: Fix romanization issues, like with the string "潜潜話 (Hisohiso Banashi)", which only contains kanji and is not detected as mixed
            else if (WanaKana.IsMixed(input) || WanaKana.IsKanji(input))
            {
                //checks if title follows "romaji (japanese)" format
                //and keeps only romaji.
                if (input.Contains("("))
                {
                    string inside = Regex.Match(input, @"(?<=\()(.*?)(?=\))").Value;
                    string outside = Regex.Replace(input, @" ?\(.*?\)", "");

                    if (ContainsJapanese(inside))
                    {
                        input = outside;
                    }
                    else if (ContainsJapanese(outside))
                    {
                        input = inside;
                    }
                }
            }

            logString = input.Truncate(50);
            Log(Type.Processing, $"Converted string to '{logString}'");

            //returns unchaged input if it doesn't follow format
            return input;
        }

        public static bool ContainsJapanese(this string text) => WanaKana.IsMixed(text) || WanaKana.IsJapanese(text);
    }
}