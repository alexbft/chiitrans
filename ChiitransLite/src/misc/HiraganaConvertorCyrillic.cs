using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChiitransLite.misc {
    class HiraganaConvertorCyrillic {
        private static HiraganaConvertorCyrillic _instance = new HiraganaConvertorCyrillic();
        public static HiraganaConvertorCyrillic instance {
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

        public HiraganaConvertorCyrillic() {
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
                    if (srcCh != 'っ' && srcCh != '゜' && char.IsLetterOrDigit(srcCh)) {
                        res.Append('\0');
                    }
                }
            }
            string result = res.ToString();
            result = Regex.Replace(result, @"っ\s?(\w)", match => match.Groups[1].Value + match.Groups[1].Value);
            result = Regex.Replace(result, @"(\w)\s?゜", match => match.Groups[1].Value + match.Groups[1].Value);
            result = result.Replace("っ", "").Replace("゜", "");
            result = result.Replace("нп", "мп").Replace("нб", "мб").Replace("нм", "мм").Replace("оу", "оо").Replace("ёу", "ё");
            result = Regex.Replace(result, @"([аеёоуыэюя])и\b", "$1й");
            result = result.Replace("\0", "");
            if (result == "ха") result = "ва";
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
            a.push("а", "あ");
            a.push("и", "い");
            a.push("у", "う");
            a.push("э", "え");
            a.push("о", "お");
            a.push("ка", "か");
            a.push("ки", "き");
            a.push("ку", "く");
            a.push("ке", "け");
            a.push("ко", "こ");
            a.push("са", "さ");
            a.push("щи", "し");
            a.push("су", "す");
            a.push("сэ", "せ");
            a.push("со", "そ");
            a.push("та", "た");
            a.push("чи", "ち");
            a.push("цу", "つ");
            a.push("тэ", "て");
            a.push("то", "と");
            a.push("на", "な");
            a.push("ни", "に");
            a.push("ну", "ぬ");
            a.push("нэ", "ね");
            a.push("но", "の");
            a.push("ха", "は");
            a.push("хи", "ひ");
            a.push("фу", "ふ");
            a.push("хэ", "へ");
            a.push("хо", "ほ");
            a.push("ма", "ま");
            a.push("ми", "み");
            a.push("му", "む");
            a.push("мэ", "め");
            a.push("мо", "も");
            a.push("я", "や");
            a.push("ю", "ゆ");
            a.push("ё", "よ");
            a.push("ра", "ら");
            a.push("ри", "り");
            a.push("ру", "る");
            a.push("рэ", "れ");
            a.push("ро", "ろ");
            a.push("ва", "わ");
            a.push("ви", "ゐ");
            a.push("вэ", "ゑ");
            a.push("во", "を");
            a.push("га", "が");
            a.push("ги", "ぎ");
            a.push("гу", "ぐ");
            a.push("ге", "げ");
            a.push("го", "ご");
            a.push("за", "ざ");
            a.push("джи", "じ");
            a.push("зу", "ず");
            a.push("зэ", "ぜ");
            a.push("зо", "ぞ");
            a.push("да", "だ");
            a.push("дэ", "で");
            a.push("до", "ど");
            a.push("ба", "ば");
            a.push("би", "び");
            a.push("бу", "ぶ");
            a.push("бэ", "べ");
            a.push("бо", "ぼ");
            a.push("па", "ぱ");
            a.push("пи", "ぴ");
            a.push("пу", "ぷ");
            a.push("пе", "ぺ");
            a.push("по", "ぽ");
            a.push("ву", "ゔ");
            a.push("н", "ん");
            a.push("а", "ぁ");
            a.push("и", "ぃ");
            a.push("у", "ぅ");
            a.push("э", "ぇ");
            a.push("o", "ぉ");
            a.push("я", "ゃ");
            a.push("ю", "ゅ");
            a.push("ё", "ょ");
            a.push("джи", "ぢ");
            a.push("дзу", "づ");

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

            b.push("кя", "きゃ");
            b.push("кю", "きゅ");
            b.push("кё", "きょ");
            b.push("ща", "しゃ");
            b.push("щу", "しゅ");
            b.push("щё", "しょ");
            b.push("ча", "ちゃ");
            b.push("чу", "ちゅ");
            b.push("чо", "ちょ");
            b.push("ня", "にゃ");
            b.push("ню", "にゅ");
            b.push("нё", "にょ");
            b.push("хя", "ひゃ");
            b.push("хю", "ひゅ");
            b.push("хё", "ひょ");
            b.push("мя", "みゃ");
            b.push("мю", "みゅ");
            b.push("мё", "みょ");
            b.push("ря", "りゃ");
            b.push("рю", "りゅ");
            b.push("рё", "りょ");
            b.push("гя", "ぎゃ");
            b.push("гю", "ぎゅ");
            b.push("гё", "ぎょ");
            b.push("джя", "じゃ");
            b.push("джю", "じゅ");
            b.push("джё", "じょ");
            b.push("бя", "びゃ");
            b.push("бю", "びゅ");
            b.push("бё", "びょ");
            b.push("пя", "ぴゃ");
            b.push("пю", "ぴゅ");
            b.push("пё", "ぴょ");

            b.push("йи", "いぃ");
            b.push("йе", "いぇ");
            b.push("ва", "ゔぁ");
            b.push("ви", "ゔぃ");
            b.push("ве", "ゔぇ");
            b.push("во", "ゔぉ");
            b.push("вя", "ゔゃ");
            b.push("вю", "ゔゅ");
            b.push("вё", "ゔょ");
            b.push("ще", "しぇ");
            b.push("дже", "じぇ");
            b.push("че", "ちぇ");
            b.push("сва", "すぁ");
            b.push("си", "すぃ");
            b.push("сву", "すぅ");
            b.push("све", "すぇ");
            b.push("сво", "すぉ");
            b.push("ся", "すゃ");
            b.push("сю", "すゅ");
            b.push("сё", "すょ");
            b.push("зва", "ずぁ");
            b.push("зи", "ずぃ");
            b.push("зву", "ずぅ");
            b.push("зве", "ずぇ");
            b.push("зво", "ずぉ");
            b.push("зя", "ずゃ");
            b.push("зю", "ずゅ");
            b.push("зё", "ずょ");
            b.push("ца", "つぁ");
            b.push("ци", "つぃ");
            b.push("це", "つぇ");
            b.push("цо", "つぉ");
            b.push("та", "てぁ");
            b.push("ти", "てぃ");
            b.push("ту", "てぅ");
            b.push("те", "てぇ");
            b.push("то", "てぉ");
            b.push("тя", "てゃ");
            b.push("тю", "てゅ");
            b.push("тё", "てょ");
            b.push("да", "でぁ");
            b.push("ди", "でぃ");
            b.push("ду", "でぅ");
            b.push("де", "でぇ");
            b.push("до", "でぉ");
            b.push("дя", "でゃ");
            b.push("дю", "でゅ");
            b.push("дё", "でょ");
            b.push("тва", "とぁ");
            b.push("тви", "とぃ");
            b.push("ту", "とぅ");
            b.push("тве", "とぇ");
            b.push("тво", "とぉ");
            b.push("два", "どぁ");
            b.push("дви", "どぃ");
            b.push("ду", "どぅ");
            b.push("две", "どぇ");
            b.push("дво", "どぉ");
            b.push("хва", "ほぁ");
            b.push("хви", "ほぃ");
            b.push("ху", "ほぅ");
            b.push("хве", "ほぇ");
            b.push("хво", "ほぉ");
            b.push("фа", "ふぁ");
            b.push("фи", "ふぃ");
            b.push("фе", "ふぇ");
            b.push("фо", "ふぉ");
            b.push("фя", "ふゃ");
            b.push("фю", "ふゅ");
            b.push("фё", "ふょ");
            b.push("рьи", "りぃ");
            b.push("ре", "りぇ");
            b.push("ви", "うぃ");
            b.push("ве", "うぇ");
            b.push("во", "うぉ");
            b.push("вя", "うゃ");
            b.push("вю", "うゅ");
            b.push("вё", "うょ");
            b.push("ква", "くぁ");
            b.push("кви", "くぃ");
            b.push("кву", "くぅ");
            b.push("кве", "くぇ");
            b.push("кво", "くぉ");
            b.push("гва", "ぐぁ");
            b.push("гви", "ぐぃ");
            b.push("гву", "ぐぅ");
            b.push("гве", "ぐぇ");
            b.push("гво", "ぐぉ");
            b.push("мва", "むぁ");
            b.push("мви", "むぃ");
            b.push("мву", "むぅ");
            b.push("мве", "むぇ");
            b.push("мво", "むぉ");
            b.push("ква", "くゎ");
            b.push("гва", "ぐゎ");


            b.push("нъа", "んあ");
            b.push("нъи", "んい");
            b.push("нъу", "んう");
            b.push("нъэ", "んえ");
            b.push("нъо", "んお");
            b.push("нъя", "んや");
            b.push("нъю", "んゆ");
            b.push("нъё", "んよ");
        }

    }

}
