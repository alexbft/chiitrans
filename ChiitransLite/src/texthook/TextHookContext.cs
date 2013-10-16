using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ChiitransLite.misc;
using ChiitransLite.settings;

namespace ChiitransLite.texthook {
    class TextHookContext {
        private static int internalIdCounter = 0;

        public int id { get; private set; }
        public int internalId { get; private set; }
        public string name { get; private set; }
        public int context { get; private set; }
        public int subcontext { get; private set; }

        public Encoding encoding { get; set; }
        public bool isListening { get; set; }

        private StringBuilder textBuffer = new StringBuilder();
        private Timer flushTimer = null;
        private volatile Task task;

        private int charsRepeat; // like aabbccbbddeeff, number of repeating characters
        private int charsRepeatConfidence = 0;
        private bool charsRepeatDetermined { get { return charsRepeatConfidence >= 20; } }
        private int phrasesRepeat; // like AbcdefgAbcdefgAbcd, number of phrases repeating (-1 is infinite repeat)
        private int phrasesRepeatConfidence = 0;
        private bool phrasesRepeatDetermined { get { return phrasesRepeatConfidence >= 20; } }
        private int irregularRepeat; // like AbAbAbCdeCdeCdeFghFghFgh, number of repeats of each part
        private int irregularRepeatConfidence = 0;
        private bool irregularRepeatDetermined { get { return irregularRepeatConfidence >= 20; } }

        public TextHookContext(int id, string name, int hook, int context, int subcontext, int status) {
            this.id = id;
            this.internalId = Interlocked.Increment(ref internalIdCounter);
            this.name = name;
            this.context = context;
            this.subcontext = subcontext;
            this.encoding = status != 0 ? Encoding.Unicode : Encoding.GetEncoding(932);
            this.isListening = true;
        }

        public string getFullName() {
            StringBuilder res = new StringBuilder();
            res.Append(name);
            if (subcontext != 0) {
                res.Append(" (" + subcontext + ")");
            }
            uint addr = (uint)context;
            res.AppendFormat(" at {0:X4}:{1:X4}", addr / 65536, addr % 65536);
            return res.ToString();
        }

        internal void handleInput(IntPtr data, int len, bool isNewLine) {
            if (isListening) {
                byte[] buf = new byte[len];
                Marshal.Copy(data, buf, 0, len);
                Action work = () => {
                    string text =encoding.GetString(buf);
                    if (!string.IsNullOrEmpty(text)) {
                        lock (textBuffer) {
                            textBuffer.Append(text);
                            /*if (isNewLine) {
                                textBuffer.Append("<br>");
                            }*/
                        }
                        if (flushTimer == null) {
                            flushTimer = new Timer(new TimerCallback((_) => flush()));
                        }
                        flushTimer.Change((long)Settings.session.sentenceDelay.TotalMilliseconds, Timeout.Infinite);
                        if (onInput != null) {
                            onInput(this, text);
                        }
                    }
                };
                lock (this) {
                    if (task == null) {
                        task = Task.Factory.StartNew(work);
                    } else {
                        task = task.ContinueWith((_) => work());
                    }
                }
            }
        }

        private void flush() {
            lock (textBuffer) {
                if (isListening) {
                    if (onSentence != null) {
                        onSentence(this, cleanTextInternal(textBuffer.ToString()));
                    }
                }
                textBuffer.Clear();
            }
        }

        private string cleanTextInternal(string p) {
            int charsRepeat;
            int phrasesRepeat;
            int irregularRepeat;

            if (!charsRepeatDetermined) {
                charsRepeat = tryDetermineCharsRepeat(p);
            } else {
                charsRepeat = this.charsRepeat;
            }
            p = cleanCharsRepeat(p, charsRepeat);
            if (!phrasesRepeatDetermined) {
                phrasesRepeat = tryDeterminePhrasesRepeat(p);
            } else {
                phrasesRepeat = this.phrasesRepeat;
            }
            p = cleanPhrasesRepeat(p, phrasesRepeat);
            if (!irregularRepeatDetermined) {
                irregularRepeat = tryDetermineIrregularRepeat(p);
            } else {
                irregularRepeat = this.irregularRepeat;
            }
            p = cleanIrregularRepeat(p, irregularRepeat);
            return p;
        }

        public static string cleanText(string p) {
            Tuple<int, bool> res;
            res = guessCharsRepeat(p);
            int charsRepeat = res.Item2 ? res.Item1 : 1;
            p = cleanCharsRepeat(p, charsRepeat);
            res = guessPhrasesRepeat(p);
            int phrasesRepeat = res.Item2 ? res.Item1 : 1;
            p = cleanPhrasesRepeat(p, phrasesRepeat);
            res = guessIrregularRepeat(p);
            int irregularRepeat = res.Item2 ? res.Item1 : 1;
            p = cleanIrregularRepeat(p, irregularRepeat);
            return p;
        }

        private int tryDetermineCharsRepeat(string p) {
            return tryDetermine(guessCharsRepeat, p, ref charsRepeat, ref charsRepeatConfidence); 
        }

        private int tryDeterminePhrasesRepeat(string p) {
            return tryDetermine(guessPhrasesRepeat, p, ref phrasesRepeat, ref phrasesRepeatConfidence);
        }

        private int tryDetermineIrregularRepeat(string p) {
            return tryDetermine(guessIrregularRepeat, p, ref irregularRepeat, ref irregularRepeatConfidence);
        }

        private int tryDetermine(Func<string, Tuple<int, bool>> guessFunc, string p, ref int res, ref int confidence) {
            Tuple<int, bool> t = guessFunc(p);
            int guess = t.Item1;
            bool isSure = t.Item2;
            if (!isSure) {
                return guess;
            } else {
                if (guess == res) {
                    confidence += 1;
                } else {
                    res = guess;
                    confidence = 1;
                }
                return guess;
            }
        }

        public static Tuple<int, bool> guessCharsRepeat(string p) {
            if (p.Length < 3) {
                return Tuple.Create(1, false);
            }
            Dictionary<int, int> repeats = new Dictionary<int, int>();
            int count = 1;
            char prev = '\0';
            int prevVal;
            foreach (char c in p) {
                if (c == prev) {
                    count += 1;
                } else {
                    if (repeats.TryGetValue(count, out prevVal)) {
                        repeats[count] = prevVal + 1;
                    } else {
                        repeats[count] = 1;
                    }
                    prev = c;
                    count = 1;
                }
            }
            if (repeats.TryGetValue(count, out prevVal)) {
                repeats[count] = prevVal + 1;
            } else {
                repeats[count] = 1;
            }
            int mostCommonValue = repeats.Values.Max();
            int mostCommonRepeats = repeats.First(x => x.Value == mostCommonValue).Key;
            if (p.Length < mostCommonRepeats * 2) {
                return Tuple.Create(1, false);
            }
            return Tuple.Create(mostCommonRepeats, mostCommonValue >= 3);
        }

        public static string cleanCharsRepeat(string p, int charsRepeat) {
            if (charsRepeat == 1) return p;
            StringBuilder res = new StringBuilder();
            int repeat = 1;
            char prev = '\0';
            foreach (char c in p) {
                if (c == prev) {
                    repeat += 1;
                    if (repeat > charsRepeat) {
                        res.Append(c);
                        repeat = 1;
                    }
                } else {
                    res.Append(c);
                    prev = c;
                    repeat = 1;
                }
            }
            return res.ToString();
        }

        public static Tuple<int, bool> guessPhrasesRepeat(string p) {
            if (p.Length <= 5) {
                return Tuple.Create(1, false);
            }
            Dictionary<char, List<int>> charPositions = new Dictionary<char, List<int>>();
            for (int i = 0; i < p.Length; ++i) {
                char c = p[i];
                List<int> positions;
                if (!charPositions.TryGetValue(c, out positions)) {
                    positions = new List<int>();
                    charPositions.Add(c, positions);
                }
                positions.Add(i);
            }
            int maxInterval = 0;
            int singleChars = 0;
            foreach (var pos in charPositions.Values) {
                if (pos.Count < 2) {
                    singleChars += 1;
                    if (singleChars >= 3) {
                        return Tuple.Create(1, true);
                    }
                    continue;
                }
                int interval = pos[1] - pos[0];
                if (interval > maxInterval) {
                    maxInterval = interval;
                }
            }
            if (maxInterval == 0) {
                return Tuple.Create(1, false);
            }
            //testing for repeats of length = maxInterval
            var start = -1; // assume garbage in few first characters
            for (var iStart = 0; iStart <= 3; ++iStart) {
                if (iStart + 2 * maxInterval > p.Length) {
                    break;
                }
                if (p.Substring(iStart, maxInterval) == p.Substring(iStart + maxInterval, maxInterval)) {
                    start = iStart;
                    break;
                }
            }
            if (start == -1) {
                return Tuple.Create(1, true);
            }
            var template = p.Substring(start, maxInterval);
            var cur = start + maxInterval;
            var repeats = 1;
            while (cur + maxInterval <= p.Length) {
                if (p.Substring(cur, maxInterval) == template) {
                    repeats += 1;
                } else {
                    return Tuple.Create(1, true);
                }
                cur += maxInterval;
            }
            if (repeats > 1) {
                string remainder = p.Substring(cur);
                if (p.Substring(start, remainder.Length) != remainder) {
                    return Tuple.Create(1, true); // fail to match remainder
                }
            }
            if (repeats > 4) {
                repeats = -1; // must be infinite repeat
            }
            return Tuple.Create(repeats, true);
        }

        public static string cleanPhrasesRepeat(string p, int phrasesRepeat, bool noGarbage = false, bool noTrailingGarbage = false) {
            if (phrasesRepeat == 1 || p.Length == 0) return p;
            for (var start = 0; start <= (noGarbage ? 0 : 3); ++start) {
                if (p.Length <= start) {
                    break;
                }
                if (phrasesRepeat > 1) {
                    int minTemplateLen;
                    int maxTemplateLen;
                    if (noTrailingGarbage) {
                        int preciseTemplateLen = (p.Length - start) / phrasesRepeat;
                        if (preciseTemplateLen * phrasesRepeat != p.Length - start) {
                            break;
                        }
                        maxTemplateLen = preciseTemplateLen;
                        minTemplateLen = preciseTemplateLen;
                    } else {
                        maxTemplateLen = (p.Length - start) / phrasesRepeat;
                        minTemplateLen = ((p.Length - start) / (phrasesRepeat + 1)) + 1;
                    }
                    if (maxTemplateLen == 0) {
                        break;
                    }
                    for (int templateLen = maxTemplateLen; templateLen >= minTemplateLen; --templateLen) {
                        var template = p.Substring(start, templateLen);
                        var cur = start + templateLen;
                        bool ok = true;
                        while (cur + templateLen <= p.Length) {
                            if (p.Substring(cur, templateLen) != template) {
                                // FAIL
                                ok = false;
                                break;
                            }
                            cur += templateLen;
                        }
                        if (ok) return template;
                    }
                } else {
                    // infinite repeat
                    string s = p.Substring(start);
                    Dictionary<char, int> charactersCount = getCharactersCount(s);
                    int guessRepeats = charactersCount.Values.Min();
                    string guess = cleanPhrasesRepeat(s, guessRepeats, true, noTrailingGarbage);
                    if (guess == s && guessRepeats % 2 == 0) {
                        guess = cleanPhrasesRepeat(s, guessRepeats / 2, true, noTrailingGarbage);
                    }
                    return guess;
                }
            }
            return p;
        }

        public static Tuple<int, bool> guessIrregularRepeat(string p) {
            if (p.Length <= 5) {
                return Tuple.Create(1, false);
            }
            int guessRepeats = getCharactersCount(p).Values.Min();
            if (guessRepeats <= 1) {
                return Tuple.Create(1, true);
            }
            if (cleanIrregularRepeat(p, guessRepeats) != p) {
                return Tuple.Create(guessRepeats, true);
            } else {
                return Tuple.Create(1, false);
            }
        }

        public static string cleanIrregularRepeat(string p, int irregularRepeat) {
            if (irregularRepeat == 1) return p;
            StringBuilder res = new StringBuilder();
            int ptr = 0;
            while (ptr < p.Length) {
                char x = p[ptr];
                int nextPos = p.IndexOf(x, ptr + 1);
                bool found = false;
                while (nextPos != -1) {
                    int len = nextPos - ptr;
                    int repeatSetLength = len * irregularRepeat;
                    if (ptr + repeatSetLength > p.Length) {
                        // FAIL
                        return p;
                    }
                    string subs = p.Substring(ptr, repeatSetLength);
                    string cleaned = cleanPhrasesRepeat(subs, irregularRepeat, true, true);
                    if (subs != cleaned) {
                        // GOOD!
                        found = true;
                        res.Append(cleaned);
                        ptr += repeatSetLength;
                        break;
                    }
                    nextPos = p.IndexOf(x, nextPos + 1);
                }
                if (!found) {
                    // FAIL
                    return p;
                }
            }
            return res.ToString();
        }
        
        private static Dictionary<char, int> getCharactersCount(string s) {
            Dictionary<char, int> res = new Dictionary<char, int>();
            foreach (char c in s) {
                res[c] = res.GetOrDefault(c, 0) + 1;
            }
            return res;
        }

        public delegate void TextHandler(TextHookContext sender, string text);
        public event TextHandler onInput;
        public event TextHandler onSentence;
    }
}
