using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChiitransLite.misc;

namespace ChiitransLite.translation.edict.inflect {
    class InflectionState {

        public readonly string suffix;
        public readonly ConjugationsVariantJson form;
        public bool isFormal { get { return form == null ? false : form.Formal; } }
        public bool isNegative { get { return form == null ? false : form.Negative; } }
        public string tense;
        public readonly ISet<string> POS;
        private bool hasOwnTense;

        public InflectionState(string suffix, ConjugationsVariantJson form = null, string prevTense = null) {
            this.suffix = suffix;
            this.form = form;
            string tense = null;
            if (form != null && !string.IsNullOrEmpty(form.Tense) && form.Tense != "Remove" && form.Tense != "Stem") {
                tense = form.Tense;
                hasOwnTense = true;
            } else {
                hasOwnTense = false;
            }
            if (!string.IsNullOrEmpty(prevTense) && prevTense != "Remove" && prevTense != "Stem") {
                if (tense == null) {
                    tense = prevTense;
                } else {
                    tense = prevTense + " &rarr; " + tense;
                }
            }
            this.tense = tense;
            this.POS = new HashSet<string>();
        }

        internal void updateTense(string t) {
            if (!hasOwnTense && !string.IsNullOrEmpty(t) && t != "Remove" && t != "Stem") {
                if (tense == null) {
                    tense = t;
                } else {
                    tense = tense + " &rarr; " + t;
                }
                hasOwnTense = true;
            }
        }
        
        public int length {
            get {
                return suffix.Length;
            }
        }

        internal void addPOS(string aPOS) {
            POS.Add(aPOS);
        }

        private static readonly Dictionary<char, string> kuruTable = new Dictionary<char, string>()
        {
            {'る', "く"},
            {'て', "き"},
            {'れ', "く"},
            {'ら', "こ"},
            {'さ', "きた"},
            {'よ', "こ"},
            {'ま', "き"},
            {'い', "こ"},
            {'た', "き"},
            {'ち', "き"},
        };

        internal string getReading() {
            if (!(suffix.Length >= 1 && suffix[0] == '来')) {
                return suffix;
            } else {
                // THIS IS FUCKED UP
                if (suffix.Length >= 2) {
                    char after = suffix[1];
                    string res = null;
                    if (after == 'れ') {
                        if (suffix.Length >= 3) {
                            res = "く";
                        } else {
                            res = "こ";
                        }
                    } else {
                        res = kuruTable.GetOrDefault(after);
                    }
                    if (res != null) {
                        return res + suffix.Substring(1);
                    } else {
                        return suffix;
                    }
                } else {
                    return "き";
                }
            }
        }

    }
}
