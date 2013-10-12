using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChiitransLite.misc {
    class HiraganaConvertor {
        private static HiraganaConvertor _instance = new HiraganaConvertor();
        public static HiraganaConvertor instance {
            get {
                return _instance;
            }
        }

        private Dict a;
        private Dict b;

        private class Dict {
            public Dictionary<string, string> dict;

            public Dict() {
                dict = new Dictionary<string, string>();
            }

            public void push(string value, string key) {
                dict[key] = value;
            }
        }

        public HiraganaConvertor() {
            a = new Dict();
            b = new Dict();
            FillDicts();
        }

        public string Convert(string src) {
            if (string.IsNullOrEmpty(src))
                return src;
            StringBuilder res = new StringBuilder();
            for (int i = 0; i < src.Length; ++i) {
                if (i + 1 < src.Length) {
                    string tmp = "" + src[i] + src[i + 1];
                    string bRes;
                    if (b.dict.TryGetValue(tmp, out bRes)) {
                        res.Append(bRes);
                        i += 1;
                        continue;
                    }
                }
                char srcCh = src[i];
                string ch = srcCh.ToString();
                string aRes;
                if (a.dict.TryGetValue(ch, out aRes)) {
                    res.Append(aRes);
                } else {
                    res.Append(srcCh);
                    if (srcCh != 'っ' && char.IsLetterOrDigit(srcCh)) {
                        res.Append('\0');
                    }
                }
            }
            string result = res.ToString();
            result = Regex.Replace(result, @"っ\s?(\w)", match => match.Groups[1].Value + match.Groups[1].Value);
            result = result.Replace("っ", "");
            result = result.Replace("u゜", "ū").Replace("o゜", "ō").Replace("i゜", "ī").Replace("a゜", "ā").Replace("e゜", "ē");
            result = result.Replace("np", "mp").Replace("nb", "mb").Replace("nm", "mm");
            result = result.Replace("\0", "");
            return result;
        }

        public string ConvertLetter(char ch) {
            string res;
            if (a.dict.TryGetValue(ch.ToString(), out res))
                return res;
            else
                return null;
        }

        private void FillDicts() {
            a.push("a", "あ");
            a.push("i", "い");
            a.push("u", "う");
            a.push("e", "え");
            a.push("o", "お");
            a.push("ka", "か");
            a.push("ki", "き");
            a.push("ku", "く");
            a.push("ke", "け");
            a.push("ko", "こ");
            a.push("sa", "さ");
            a.push("shi", "し");
            a.push("su", "す");
            a.push("se", "せ");
            a.push("so", "そ");
            a.push("ta", "た");
            a.push("chi", "ち");
            a.push("tsu", "つ");
            a.push("te", "て");
            a.push("to", "と");
            a.push("na", "な");
            a.push("ni", "に");
            a.push("nu", "ぬ");
            a.push("ne", "ね");
            a.push("no", "の");
            a.push("ha", "は");
            a.push("hi", "ひ");
            a.push("fu", "ふ");
            a.push("he", "へ");
            a.push("ho", "ほ");
            a.push("ma", "ま");
            a.push("mi", "み");
            a.push("mu", "む");
            a.push("me", "め");
            a.push("mo", "も");
            a.push("ya", "や");
            a.push("yu", "ゆ");
            a.push("yo", "よ");
            a.push("ra", "ら");
            a.push("ri", "り");
            a.push("ru", "る");
            a.push("re", "れ");
            a.push("ro", "ろ");
            a.push("wa", "わ");
            a.push("wi", "ゐ");
            a.push("we", "ゑ");
            a.push("wo", "を");
            a.push("ga", "が");
            a.push("gi", "ぎ");
            a.push("gu", "ぐ");
            a.push("ge", "げ");
            a.push("go", "ご");
            a.push("za", "ざ");
            a.push("ji", "じ");
            a.push("zu", "ず");
            a.push("ze", "ぜ");
            a.push("zo", "ぞ");
            a.push("da", "だ");
            a.push("de", "で");
            a.push("do", "ど");
            a.push("ba", "ば");
            a.push("bi", "び");
            a.push("bu", "ぶ");
            a.push("be", "べ");
            a.push("bo", "ぼ");
            a.push("pa", "ぱ");
            a.push("pi", "ぴ");
            a.push("pu", "ぷ");
            a.push("pe", "ぺ");
            a.push("po", "ぽ");
            a.push("vu", "ゔ");
            a.push("n", "ん");
            a.push("a", "ぁ");
            a.push("i", "ぃ");
            a.push("u", "ぅ");
            a.push("e", "ぇ");
            a.push("o", "ぉ");
            a.push("ya", "ゃ");
            a.push("yu", "ゅ");
            a.push("yo", "ょ");
            a.push("ji", "ぢ");
            a.push("zu", "づ");

            a.push("{", "｛");
            a.push("}", "｝");
            a.push("(", "（");
            a.push(")", "）");
            a.push("[", "［");
            a.push("]", "］");
            a.push(",", "、");
            a.push(".", "。");
            a.push(":", "：");
            a.push("!", "！");
            a.push("?", "？");
            a.push("゜", "");

            b.push("kya", "きゃ");
            b.push("kyu", "きゅ");
            b.push("kyo", "きょ");
            b.push("sha", "しゃ");
            b.push("shu", "しゅ");
            b.push("sho", "しょ");
            b.push("cha", "ちゃ");
            b.push("chu", "ちゅ");
            b.push("cho", "ちょ");
            b.push("nya", "にゃ");
            b.push("nyu", "にゅ");
            b.push("nyo", "にょ");
            b.push("hya", "ひゃ");
            b.push("hyu", "ひゅ");
            b.push("hyo", "ひょ");
            b.push("mya", "みゃ");
            b.push("myu", "みゅ");
            b.push("myo", "みょ");
            b.push("rya", "りゃ");
            b.push("ryu", "りゅ");
            b.push("ryo", "りょ");
            b.push("gya", "ぎゃ");
            b.push("gyu", "ぎゅ");
            b.push("gyo", "ぎょ");
            b.push("ja", "じゃ");
            b.push("ju", "じゅ");
            b.push("jo", "じょ");
            b.push("bya", "びゃ");
            b.push("byu", "びゅ");
            b.push("byo", "びょ");
            b.push("pya", "ぴゃ");
            b.push("pyu", "ぴゅ");
            b.push("pyo", "ぴょ");

            b.push("yi", "いぃ");
            b.push("ye", "いぇ");
            b.push("va", "ゔぁ");
            b.push("vi", "ゔぃ");
            b.push("ve", "ゔぇ");
            b.push("vo", "ゔぉ");
            b.push("vya", "ゔゃ");
            b.push("vyu", "ゔゅ");
            b.push("vyo", "ゔょ");
            b.push("she", "しぇ");
            b.push("je", "じぇ");
            b.push("che", "ちぇ");
            b.push("swa", "すぁ");
            b.push("si", "すぃ");
            b.push("swu", "すぅ");
            b.push("swe", "すぇ");
            b.push("swo", "すぉ");
            b.push("sya", "すゃ");
            b.push("syu", "すゅ");
            b.push("syo", "すょ");
            b.push("zwa", "ずぁ");
            b.push("zi", "ずぃ");
            b.push("zwu", "ずぅ");
            b.push("zwe", "ずぇ");
            b.push("zwo", "ずぉ");
            b.push("zya", "ずゃ");
            b.push("zyu", "ずゅ");
            b.push("zyo", "ずょ");
            b.push("tsa", "つぁ");
            b.push("tsi", "つぃ");
            b.push("tse", "つぇ");
            b.push("tso", "つぉ");
            b.push("tha", "てぁ");
            b.push("ti", "てぃ");
            b.push("thu", "てぅ");
            b.push("tye", "てぇ");
            b.push("tho", "てぉ");
            b.push("tya", "てゃ");
            b.push("tyu", "てゅ");
            b.push("tyo", "てょ");
            b.push("dha", "でぁ");
            b.push("di", "でぃ");
            b.push("dhu", "でぅ");
            b.push("dye", "でぇ");
            b.push("dho", "でぉ");
            b.push("dya", "でゃ");
            b.push("dyu", "でゅ");
            b.push("dyo", "でょ");
            b.push("twa", "とぁ");
            b.push("twi", "とぃ");
            b.push("tu", "とぅ");
            b.push("twe", "とぇ");
            b.push("two", "とぉ");
            b.push("dwa", "どぁ");
            b.push("dwi", "どぃ");
            b.push("du", "どぅ");
            b.push("dwe", "どぇ");
            b.push("dwo", "どぉ");
            b.push("hwa", "ほぁ");
            b.push("hwi", "ほぃ");
            b.push("hu", "ほぅ");
            b.push("hwe", "ほぇ");
            b.push("hwo", "ほぉ");
            b.push("fa", "ふぁ");
            b.push("fi", "ふぃ");
            b.push("fe", "ふぇ");
            b.push("fo", "ふぉ");
            b.push("fya", "ふゃ");
            b.push("fyu", "ふゅ");
            b.push("fyo", "ふょ");
            b.push("ryi", "りぃ");
            b.push("rye", "りぇ");
            b.push("wi", "うぃ");
            b.push("we", "うぇ");
            b.push("wo", "うぉ");
            b.push("wya", "うゃ");
            b.push("wyu", "うゅ");
            b.push("wyo", "うょ");
            b.push("kwa", "くぁ");
            b.push("kwi", "くぃ");
            b.push("kwu", "くぅ");
            b.push("kwe", "くぇ");
            b.push("kwo", "くぉ");
            b.push("gwa", "ぐぁ");
            b.push("gwi", "ぐぃ");
            b.push("gwu", "ぐぅ");
            b.push("gwe", "ぐぇ");
            b.push("gwo", "ぐぉ");
            b.push("mwa", "むぁ");
            b.push("mwi", "むぃ");
            b.push("mwu", "むぅ");
            b.push("mwe", "むぇ");
            b.push("mwo", "むぉ");
            b.push("kwa", "くゎ");
            b.push("gwa", "ぐゎ");


            b.push("n'a", "んあ");
            b.push("n'i", "んい");
            b.push("n'u", "んう");
            b.push("n'e", "んえ");
            b.push("n'o", "んお");
            b.push("n'ya", "んや");
            b.push("n'yu", "んゆ");
            b.push("n'yo", "んよ");
        }

    }

}
