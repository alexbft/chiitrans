using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ChiitransLite.misc {
    public static class TextUtils {

        public static bool isKatakana(char ch) {
            return (ch >= '\u30A0' && ch <= '\u30FF');
        }

        public static bool isHiragana(char ch) {
            return (ch >= '\u3040' && ch <= '\u309F');
        }

        public static bool isKanji(char ch) {
            return char.GetUnicodeCategory(ch) == UnicodeCategory.OtherLetter && !isKatakana(ch) && !isHiragana(ch);
        }

        public static bool isAllKatakana(string s) {
            foreach (char c in s) {
                if (!isKatakana(c)) {
                    return false;
                }
            }
            return true;
        }

        internal static bool isAllKatakanaOrHasLongVowel(string s) {
            bool isAllKatakana = true;
            bool hasLong = false;
            bool hasKatakanaExceptLong = false;
            foreach (char c in s) {
                if (c == 'ー') {
                    hasLong = true;
                } else if (isKatakana(c)) {
                    hasKatakanaExceptLong = true;
                } else {
                    isAllKatakana = false;
                }
            }
            return isAllKatakana || (hasLong && !hasKatakanaExceptLong);
        }

        internal static bool isAnyKatakana(string s) {
            foreach (char c in s) {
                if (isKatakana(c)) {
                    return true;
                }
            }
            return false;
        }

        public static string katakanaToHiragana(string katakanaStr) {
            StringBuilder res = new StringBuilder(katakanaStr.Length);
            char prev = '\0';
            foreach (char ch in katakanaStr)
            {
                if (ch == 'ー') {
                    string prevRomaji = HiraganaConvertor.instance.ConvertLetter(prev);
                    prev = '゜';
                    if (prevRomaji != null) {
                        if (prevRomaji.EndsWith("a")) {
                            prev = 'あ';
                        } else if (prevRomaji.EndsWith("e")) {
                            prev = 'え';
                        } else if (prevRomaji.EndsWith("i")) {
                            prev = 'い';
                        } else if (prevRomaji.EndsWith("o") || prevRomaji.EndsWith("u")) {
                            prev = 'う';
                        }
                    }
                } else {
                    prev = katakanaToHiraganaChar(ch);
                }
                res.Append(prev);
            }
            return res.ToString();
        }

        internal static string kanaToRomaji(string p) {
            return HiraganaConvertor.instance.Convert(katakanaToHiragana(p));
        }

        internal static string kanaToCyrillic(string p) {
            return HiraganaConvertorCyrillic.instance.Convert(katakanaToHiragana(p));
        }

        internal static string maxCommonPrefix(string s1, string s2) {
            int x = 0;
            while (x < s1.Length && x < s2.Length && s1[x] == s2[x]) {
                x += 1;
            }
            return s1.Substring(0, x);
        }

        internal static char katakanaToHiraganaChar(char ch) {
            if (isKatakana(ch) && ch != '・')
                return (char)(ch - 0x60);
            else
                return ch;
        }

        internal static bool isKana(char c) {
            return isKatakana(c) || isHiragana(c);
        }

        internal static bool containsJapanese(string text) {
            return text.Any((c) => isJapanese(c));
        }

        private static bool isJapanese(char c) {
            return (c >= '\u3000' && c <= '\u303f') || // punctuation
                (c >= '\u3040' && c <= '\u309f') || // hiragana
                (c >= '\u30a0' && c <= '\u30ff') || // katakana
                (c >= '\uff00' && c <= '\uffef') || // full-width roman or half-width
                (c >= '\u4e00' && c <= '\u9faf'); // kanji
        }
    }
}

