using ChiitransLite.misc;
using ChiitransLite.settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChiitransLite.translation.po {
    class PoManager {
        private static PoManager _instance = new PoManager();

        public static PoManager instance {
            get {
                return _instance;
            }
        }

        private CancellationTokenSource cancelToken;
        private string path;
        private volatile string completedPath;
        private volatile Dictionary<string, PoTranslation> translations;
        private List<PoTranslation> translationsByIndex;
        private int lastTranslationIndex;

        public Task loadFrom(string path, Action<int, int> progressHandler = null) {
            this.path = path;
            this.completedPath = null;
            cancelToken = new CancellationTokenSource();
            return Task.Factory.StartNew(loadingProc, progressHandler, cancelToken.Token);
        }

        private void loadingProc(object state) {
            lock (this) {
                if (string.IsNullOrEmpty(path)) {
                    reset();
                    completedPath = null;
                    return;
                }
                Action<int, int> progressHandler = state as Action<int, int>;
                var token = cancelToken.Token;
                bool isFile = File.Exists(path);
                bool isDir = Directory.Exists(path);
                if (!isFile && !isDir) {
                    throw new MyException("Specified path does not exist.");
                }
                reset();
                token.ThrowIfCancellationRequested();
                if (isFile) {
                    if (progressHandler != null) {
                        progressHandler(0, 1);
                    }
                    loadFile(path, 1, 1, progressHandler);
                } else {
                    List<string> poFiles = Directory.EnumerateFiles(path, "*.po", SearchOption.AllDirectories).ToList();
                    int total = poFiles.Count;
                    int cur = 0;
                    if (progressHandler != null) {
                        progressHandler(cur, total);
                    }
                    foreach (string fn in poFiles) {
                        token.ThrowIfCancellationRequested();
                        cur += 1;
                        loadFile(fn, cur, total, progressHandler);
                    }
                }
                completedPath = path;
            }
        }

        private void reset() {
            translationsByIndex = new List<PoTranslation>();
            translations = new Dictionary<string, PoTranslation>();
            lastTranslationIndex = -1;
        }

        private void loadFile(string fn, int cur, int total, Action<int, int> progressHandler) {
            int lineNum = 0;
            try {
                string msgid = null;
                foreach (var line in File.ReadLines(fn, Encoding.UTF8)) {
                    lineNum += 1;
                    if (line.StartsWith("msgid")) {
                        msgid = unescape(line);
                    } else if (line.StartsWith("msgstr")) {
                        if (!string.IsNullOrEmpty(msgid)) {
                            string msgstr = unescape(line);
                            var tr = new PoTranslation(msgid, msgstr, translationsByIndex.Count);
                            translationsByIndex.Add(tr);
                            translations[getKey(tr.getOriginalClean())] = tr;
                            msgid = null;
                        }
                    }
                }
                if (progressHandler != null) {
                    progressHandler(cur, total);
                }
            } catch (Exception ex) {
                throw new MyException("Exception when loading file: " + fn + " on line " + lineNum + ": " + ex.Message);
            }
        }

        private string getKey(string p) {
            string cleaned = new string(p.Where((c) => char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter).ToArray());
            if (cleaned != "") {
                return cleaned;
            } else {
                return p;
            }
        }

        private string unescape(string line) {
            var firstQ = line.IndexOf('"');
            var secondQ = line.LastIndexOf('"');
            if (firstQ != -1) {
                if (secondQ == firstQ) {
                    secondQ = line.Length;
                }
                return Regex.Unescape(line.Substring(firstQ + 1, secondQ - firstQ - 1));
            } else {
                return null;
            }
        }

        internal void cancel() {
            if (cancelToken != null) {
                cancelToken.Cancel();
            }
        }

        internal string getTranslation(string key) {
            if (string.IsNullOrEmpty(completedPath) || completedPath != Settings.session.po || translations == null) {
                return null;
            }
            lock (this) {
                key = getKey(key);
                var t = translations.GetOrDefault(key);
                if (t == null && lastTranslationIndex != -1) {
                    for (int trying = lastTranslationIndex; trying < lastTranslationIndex + 3 && trying < translationsByIndex.Count; ++trying) {
                        var tt = translationsByIndex[trying];
                        var ttKey = getKey(tt.getOriginalClean());
                        if (ttKey.Contains(key)) {
                            t = tt;
                            break;
                        }
                        var id = key.IndexOf(ttKey);
                        if (id != -1) {
                            t = tt;
                            var sub = key.Substring(id + ttKey.Length);
                            if (sub != "") {
                                lastTranslationIndex = t.index;
                                var rec = getTranslation(sub);
                                if (rec == null) {
                                    return t.getCleanTranslation();
                                } else {
                                    return t.getCleanTranslation() + " " + rec;
                                }
                            }
                            break;
                        }
                    }
                }
                if (t != null) {
                    lastTranslationIndex = t.index;
                    return t.getCleanTranslation();
                } else {
                    return null;
                }
            }
        }
    }
}
