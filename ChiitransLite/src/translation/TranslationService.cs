using ChiitransLite.forms;
using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.texthook;
using ChiitransLite.translation.atlas;
using ChiitransLite.translation.edict;
using ChiitransLite.translation.edict.parseresult;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                var curId = Interlocked.Increment(ref textId);
                startParse(curId, text, Settings.app.translationDisplay == TranslationDisplay.TRANSLATION || Settings.app.translationDisplay == TranslationDisplay.BOTH, null);
            }
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

        public Task<string> startTranslation(int curId, ParseResult parseData) {
            return Task.Factory.StartNew(() => {
                try {
                    var text2 = Edict.instance.replaceNames(parseData);
                    string translatedText = Atlas.instance.translate(text2);
                    if (translatedText != null) {
                        if (onAtlasDone != null) {
                            onAtlasDone(curId, translatedText);
                        }
                        return translatedText;
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

        private Timer clipboardTimer;
        private readonly TimeSpan clipboardTimerDelay = TimeSpan.FromMilliseconds(200);

        private void _setClipboardTranslation(bool isEnabled) {
            if (isEnabled) {
                if (clipboardTimer == null) {
                    clipboardTimer = new Timer(new TimerCallback(clipboardTimerProc));
                }
                clipboardTimer.Change(TimeSpan.Zero, clipboardTimerDelay);
            } else {
                if (clipboardTimer != null) {
                    clipboardTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private void clipboardTimerProc(object state) {
            // todo optimize / use some other instance
            try {
                MainForm.instance.Invoke(new Action(() => {
                    if (Clipboard.ContainsText()) {
                        update(TextHookContext.cleanText(Clipboard.GetText()));
                    }
                }));
            } catch {
                if (clipboardTimer != null) {
                    clipboardTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        public void setClipboardTranslation(bool isEnabled) {
            if (Settings.app.clipboardTranslation != isEnabled) {
                Settings.app.clipboardTranslation = isEnabled;
            }
            _setClipboardTranslation(isEnabled);
        }

        public delegate void AtlasDoneHandler(int id, string text);
        public event AtlasDoneHandler onAtlasDone;

        public delegate void EdictDoneHandler(int id, ParseResult parseResult);
        public event EdictDoneHandler onEdictDone;

    }
}
