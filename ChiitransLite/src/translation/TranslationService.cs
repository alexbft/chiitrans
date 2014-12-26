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
        const int MAX_CONCURRENT_TRANSLATION_TASKS = 4;

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
                var curId = Interlocked.Increment(ref textId);
                updateId(curId, text, null);
            }
        }

        public Task<ParseResult> updateId(int curId, string text, ParseOptions options) {
            text = preParseReplacements(text);
            bool doTranslation = Settings.app.translationDisplay == TranslationDisplay.TRANSLATION || Settings.app.translationDisplay == TranslationDisplay.BOTH;
            return startParse(curId, text, doTranslation, options);
        }

        private readonly Dictionary<char, char> openAndClose = new Dictionary<char, char> { { '『', '』' }, { '「', '」' }, { '【', '】' }, { '（', '）' } };

        private string preParseReplacements(string text) {
            var match = Regex.Match(text, "^([「『（].*[」』）])([^「『（」』）]{1,10})$");
            if (match.Success) {
                string speaker = match.Groups[2].Value;
                string speech = match.Groups[1].Value;
                if (speech.Length >= 2 && openAndClose[speech[0]] == speech[speech.Length - 1]) { // opening bracket matches closing bracket
                    return speaker + speech;
                }
            }
            return text;
        }

        private Task<ParseResult> startParse(int curId, string text, bool doTranslation, ParseOptions parseOptions) {
            return Task.Factory.StartNew(() => {
                try {
                    var parseData = Edict.instance.parse(text, parseOptions);
                    if (parseData != null) {
                        parseData.id = curId;
                        if (doTranslation) {
                            sendTranslationRequest(curId, parseData);
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

        private int translationTasksActive = 0;

        internal T limiter<T>(Func<T> func, T rejectValue = default(T)) {
            int activeTasks = Interlocked.Increment(ref translationTasksActive);
            try {
                if (activeTasks < MAX_CONCURRENT_TRANSLATION_TASKS * Settings.app.getSelectedTranslators(true).Count) {
                    return func();
                } else {
                    return rejectValue;
                }
            } finally {
                Interlocked.Decrement(ref translationTasksActive);
            }
        }

        private void sendTranslationRequest(int curId, ParseResult parseData) {
            if (onTranslationRequest != null) {
                var raw = parseData.asText();
                var src = Edict.instance.replaceNames(parseData);
                onTranslationRequest(curId, raw, src);
            }
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

        public delegate void TranslationRequestHandler(int id, string raw, string src);
        public event TranslationRequestHandler onTranslationRequest;

        public delegate void EdictDoneHandler(int id, ParseResult parseResult);
        public event EdictDoneHandler onEdictDone;

    }
}
