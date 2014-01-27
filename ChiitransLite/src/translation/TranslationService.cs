using ChiitransLite.forms;
using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.texthook;
using ChiitransLite.translation.atlas;
using ChiitransLite.translation.edict;
using ChiitransLite.translation.edict.parseresult;
using ChiitransLite.translation.po;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Clipboard = System.Windows.Forms.Clipboard;

namespace ChiitransLite.translation {
    class TranslationService {
        const int MAX_CACHE = 100;
        const int MAX_TEXT_LENGTH = 1000;

        private static TranslationService _instance = new TranslationService();

        public static TranslationService instance {
            get {
                return _instance;
            }
        }

        protected TranslationService() {
        }

        private int textId = 0;
        private ConcurrentDictionary<int, ParseResult> parseCache = new ConcurrentDictionary<int, ParseResult>();
        private ConcurrentQueue<int> parseCacheEntries = new ConcurrentQueue<int>();
        private volatile string prevText;

        public void update(string text, bool checkDifferent = true) {
            if (string.IsNullOrEmpty(text)) return;
            if (text.Length > MAX_TEXT_LENGTH) {
                text = text.Substring(0, MAX_TEXT_LENGTH);
            }
            if (!checkDifferent || text != prevText) {
                prevText = text;
                text = preParseReplacements(text);
                var curId = Interlocked.Increment(ref textId);
                bool doTranslation = Settings.app.translationDisplay == TranslationDisplay.TRANSLATION || Settings.app.translationDisplay == TranslationDisplay.BOTH;
                if (doTranslation) {
                    if (Settings.session.po != null) {
                        var poTrans = PoManager.instance.getTranslation(text);
                        if (!string.IsNullOrEmpty(poTrans)) {
                            if (onAtlasDone != null) {
                                onAtlasDone(curId, new TranslationResult(poTrans, false));
                            }
                            doTranslation = false;
                        }
                    }
                }
                bool doParse = Settings.app.translationDisplay == TranslationDisplay.PARSE || Settings.app.translationDisplay == TranslationDisplay.BOTH || doTranslation;
                if (doParse) {
                    startParse(curId, text, doTranslation, null);
                }
            }
        }

        private string preParseReplacements(string text) {
            var match = Regex.Match(text, "^(「.*」)([^「」]+)$");
            if (match.Success) {
                return match.Groups[2].Value + match.Groups[1].Value;
            }
            return text;
        }

        public Task<ParseResult> startParse(int curId, string text, bool doTranslation, ParseOptions parseOptions) {
            return Task.Factory.StartNew(() => {
                try {
                    var parseData = Edict.instance.parse(text, parseOptions);
                    if (parseData != null) {
                        parseData.id = curId;
                        if (doTranslation) {
                            startTranslation(curId, parseData);
                        }
                        parseCacheEntries.Enqueue(curId);
                        if (parseCacheEntries.Count > MAX_CACHE) {
                            int res;
                            if (parseCacheEntries.TryDequeue(out res)) {
                                ParseResult unused;
                                parseCache.TryRemove(res, out unused);
                            }
                        }
                        parseCache[curId] = parseData;
                        if (onEdictDone != null && (Settings.app.translationDisplay == TranslationDisplay.PARSE || Settings.app.translationDisplay == TranslationDisplay.BOTH)) {
                            onEdictDone(curId, parseData);
                        }
                    }
                    return parseData;
                } catch (Exception e) {
                    Logger.logException(e);
                    return null;
                }
            });
        }

        public void startTranslation(int curId, ParseResult parseData) {
            Task.Factory.StartNew(() => {
                try {
                    var text2 = Edict.instance.replaceNames(parseData);
                    string translatedText = Atlas.instance.translate(text2);
                    if (translatedText != null) {
                        var tres = new TranslationResult(translatedText, true);
                        if (onAtlasDone != null) {
                            onAtlasDone(curId, tres);
                        }
                        return tres;
                    } else {
                        return null;
                    }
                } catch (Exception e) {
                    Logger.logException(e);
                    return null;
                }
            });
        }

        public ParseResult getParseResult(int id) {
            return parseCache.GetOrDefault(id);
        }

        public WordParseResult getParseResult(int id, int num) {
            ParseResult res;
            if (parseCache.TryGetValue(id, out res)) {
                ParseResult part = null;
                if (res.type != ParseResult.ParseResultType.COMPLEX) {
                    if (num == 0) {
                        part = res;
                    }
                } else {
                    ComplexParseResult cpr = res as ComplexParseResult;
                    if (num >= 0 && num < cpr.parts.Count) {
                        part = cpr.parts[num];
                    }
                }
                if (part == null || part.type != ParseResult.ParseResultType.WORD) {
                    return null;
                } else {
                    return part as WordParseResult;
                }
            } else {
                return null;
            }
        }

        public delegate void AtlasDoneHandler(int id, TranslationResult translationResult);
        public event AtlasDoneHandler onAtlasDone;

        public delegate void EdictDoneHandler(int id, ParseResult parseResult);
        public event EdictDoneHandler onEdictDone;

    }
}
