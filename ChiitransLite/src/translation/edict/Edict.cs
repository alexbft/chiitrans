using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.translation.edict.inflect;
using ChiitransLite.translation.edict.parseresult;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.translation.edict {
    class Edict : WithInitialization {
        private static Edict _instance = new Edict();

        public static Edict instance {
            get {
                _instance.tryWaitForInitialization();
                return _instance;
            }
        }

        private EdictDictionary dict;
        private Inflector inflect;

        protected override void doInitialize() {
            inflect = new Inflector();
            inflect.load();
            dict = new EdictDictionary();
            dict.load(inflect);
        }

        private bool isAcceptableChar(char c) {
            return char.IsLetterOrDigit(c) || c == '－';
        }

        public ParseResult parse(string text, ParseOptions parseOptions) {
            if (state != State.WORKING) {
                return null;
            }
            IList<ParseResult> results = new List<ParseResult>();
            int i = 0;
            while (i < text.Length) {
                StringBuilder part = new StringBuilder();
                if (!isAcceptableChar(text[i])) {
                    do {
                        part.Append(text[i]);
                        i += 1;
                    } while (i < text.Length && !isAcceptableChar(text[i]));
                    results.Add(ParseResult.unparsed(part.ToString()));
                } else {
                    do {
                        part.Append(text[i]);
                        i += 1;
                    } while (i < text.Length && isAcceptableChar(text[i]));
                    results.Add(parseTextPart(part.ToString(), parseOptions));
                }
            }
            return ParseResult.concat(results);
        }

        private class DynamicParseResult {
            public double score;
            public ParseResult parsed;
        }
        
        private ParseResult parseTextPart(string text, ParseOptions parseOptions) {
            DynamicParseResult[] scoreTable = new DynamicParseResult[text.Length + 1];
            for (int i = 0; i <= text.Length; ++i) {
                scoreTable[i] = new DynamicParseResult { score = i * (-10000), parsed = null };
            }
            for (int i = 0; i < text.Length; ++i) {
                double skipScore = scoreTable[i].score - (("ぁぃぅぇぉゃゅょっ".IndexOf(text[i]) != -1) ? -1 : 10000); // ignore lower letters
                DynamicParseResult oldSkipScore = scoreTable[i + 1];
                if (oldSkipScore.score < skipScore) {
                    oldSkipScore.score = skipScore;
                    oldSkipScore.parsed = null;
                }
                IEnumerable<EdictMatchWithType> matches = dict.findMatching(text, i);
                foreach (EdictMatchWithType match in matches) {
                    IEnumerable<InflectionState> inflections;
                    inflections = inflect.findMatching(match.matchType == EdictMatchType.FROM_KATAKANA, match.match.getAllPOS(), text, i + match.match.stemLength);
                    foreach (InflectionState inf in inflections) {
                        int totalLength = match.match.stemLength + inf.length;
                        string matchText = text.Substring(i, totalLength);
                        if (Settings.app.isWordBanned(matchText, match.matchType)) {
                            continue;
                        }
                        double baseFormMult;
                        if (match.match.entries[0].entry.getText() == matchText) {
                            baseFormMult = 1.05;
                        } else {
                            baseFormMult = 1.0;
                        }
                        double score = scoreTable[i].score + match.match.getMultiplier(inf.POS, inf.suffix.Length == 0) * getLengthMultiplier(totalLength) * baseFormMult;
                        if (parseOptions != null) {
                            score += parseOptions.bonusRating(matchText);
                        }
                        DynamicParseResult oldScore = scoreTable[i + totalLength];
                        if (oldScore.parsed != null && oldScore.parsed.asText() == matchText) {
                            (oldScore.parsed as WordParseResult).addMatch(match, inf, score);
                            if (oldScore.score < score) {
                                oldScore.score = score;
                            }
                        } else {
                            if (oldScore.score < score) {
                                oldScore.score = score;
                                oldScore.parsed = new WordParseResult(matchText, match, inf, score);
                            }
                        }
                    }
                }
            }

            IList<ParseResult> results = new List<ParseResult>();
            int x = text.Length;
            while (x > 0) {
                DynamicParseResult res = scoreTable[x];
                if (res.parsed == null) {
                    results.Add(ParseResult.unparsed(text[x - 1].ToString()));
                    x -= 1;
                } else {
                    results.Add(res.parsed);
                    x -= res.parsed.length;
                }
            }
            return ParseResult.concat(results.Reverse());
        }

        private double getLengthMultiplier(int len) {
            return Math.Pow(len, 2.1);
        }

        internal EdictMatch lookup(string p) {
            return dict.lookup(p);
        }

        internal IDictionary<string, string> getDefinitions() {
            return dict.definitions;
        }

        internal IDictionary<string, string> getNameDefinitions() {
            return dict.nameDefinitions;
        }

        private readonly Dictionary<string, string> nameSuffixes = new Dictionary<string, string> {
            { "さん", "-san" },
            { "はん", "-han" },
            { "様", "-sama" },
            { "公", "-kou" },
            { "君", "-kun" },
            { "ちゃん", "-chan" },
            { "ちん", "-chin" },
            { "たん", "-tan" },
            { "坊", "-bou" },
            { "ちゃま", "-chama" },
            { "たま", "-tama" },
            { "先輩", "-sempai" },
            { "先生", "-sensei" },
            { "氏", "-shi" },
            { "っち", "cchi" },
            { "殿", "-dono" },
            { "陛下", "-heika" },
            { "姫", "-hime" },
            { "兄", "-nii" }, 
            { "姉", "-nee" },
            { "ねぇ", "-nee" },
            { "姉ぇ", "-nee" },
            { "卿", "-kyo" }
        };

        internal string replaceNames(ParseResult parseData) {
            StringBuilder sb = new StringBuilder();
            IEnumerable<ParseResult> parts;
            if (parseData.type == ParseResult.ParseResultType.COMPLEX) {
                parts = (parseData as ComplexParseResult).parts;
            } else {
                parts = Enumerable.Repeat(parseData, 1);
            }
            bool prevName = false;
            foreach (var part in parts) {
                if (part.type == ParseResult.ParseResultType.UNPARSED) {
                    prevName = false;
                    sb.Append(part.asText());
                } else if (part.type == ParseResult.ParseResultType.WORD) {
                    WordParseResult wp = part as WordParseResult;
                    bool found = false;
                    bool isName = wp.isName();
                    bool isReplacement = wp.isReplacement();
                    if (isName || isReplacement) {
                        var entry = wp.getSelectedEntry();
                        DictionarySense sense = entry.sense.FirstOrDefault();
                        if (sense != null && sense.glossary.Count > 0) {
                            if (prevName) sb.Append(' ');
                            sb.Append(sense.glossary.First());
                            found = true;
                        } else {
                            if (entry.kana.Count > 0) {
                                if (prevName) sb.Append(' ');
                                sb.Append(entry.kana.First().text);
                                found = true;
                            }
                        }
                    } else if (prevName) {
                        foreach (EdictEntry entry in wp.getEntries()) {
                            if (nameSuffixes.ContainsKey(part.asText())) {
                                string reading = nameSuffixes[part.asText()];
                                sb.Append(reading);
                                found = true;
                                break;
                            }
                            else if (nameSuffixes.ContainsKey(entry.getText())) {
                                string reading = nameSuffixes[entry.getText()];
                                sb.Append(reading);
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) {
                        prevName = isName || prevName;
                    } else {
                        prevName = false;
                        sb.Append(part.asText());
                    }
                }
            }
            return sb.ToString();
        }

        internal EdictMatch findName(string name) {
            return dict.findName(name);
        }

        protected override void onInitializationError(Exception ex) {
            Logger.logException(ex);
        }
    }
}
