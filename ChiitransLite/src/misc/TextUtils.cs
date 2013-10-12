using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ChiitransLite.misc {
    static class TextUtils {

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

        internal static string katakanaToHiragana(string katakanaStr) {
            StringBuilder res = new StringBuilder(katakanaStr.Length);
            foreach (char ch in katakanaStr)
            {
                res.Append(katakanaToHiraganaChar(ch));
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
    }
}

