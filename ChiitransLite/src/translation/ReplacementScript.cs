using ChiitransLite.misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace ChiitransLite.translation {
    class ReplacementScript {
        private class Replacement {
            public Regex key;
            public string replacement;
            public Replacement(string key, string replacement) {
                this.key = new Regex(key);
                this.replacement = replacement;
            }
        }

        private static List<Replacement> replacements;
        private static readonly object _lock = new object();

        public static ReplacementScript get() {
            lock(_lock) {
                if (replacements == null) {
                    loadReplacements();
                }
            }
            return new ReplacementScript();
        }

        private static void loadReplacements() {
            replacements = new List<Replacement>();
            foreach (string ln in File.ReadAllLines(settings.Settings.app.ReplacementScriptPath)) {
                if (string.IsNullOrWhiteSpace(ln) || ln.StartsWith("*")) {
                    continue;
                }
                string[] parts = ln.Split('\t');
                if (parts.Length == 1) {
                    replacements.Add(new Replacement(parts[0], ""));
                } else
                if (parts.Length == 2) {
                    replacements.Add(new Replacement(parts[0], parts[1]));
                } else {
                    Logger.log("(TAHelper script) Ignoring line: " + ln);
                }
            }
        }

        internal string process(string src) {
            string res = src;
            foreach (var repl in replacements) {
                res = repl.key.Replace(res, repl.replacement);
            }
            /*if (src != res) {
                Logger.log(string.Format("(TAHelper script) Fixed {0} -> {1}.", src, res));
            }*/
            return res;
        }
    }
}
