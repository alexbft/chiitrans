using ChiitransLite.settings;
using ChiitransLite.translation.edict.inflect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class EdictMatch {
        public readonly List<RatedEntry> entries = new List<RatedEntry>();
        public readonly string stem;
        public int stemLength { get { return stem.Length; } }
        private List<String> allPOS;

        public EdictMatch(string stem) {
            this.stem = stem;
        }

        internal List<string> getAllPOS() {
            if (allPOS == null) {
                HashSet<string> tmp = new HashSet<string>();
                foreach (RatedEntry re in entries) {
                    tmp.UnionWith(re.entry.POS);
                }
                allPOS = tmp.ToList();
            }
            return allPOS;
        }

        internal void addEntry(RatedEntry newEntry) {
            entries.Add(newEntry);
        }

        internal double getMultiplier(ISet<string> allowedPOS, bool isZeroSuffix) {
            double maxMultiplier = 0;
            foreach (RatedEntry re in getRatedEntries(allowedPOS, isZeroSuffix)) {
                if (maxMultiplier < re.rate) {
                    maxMultiplier = re.rate;
                }
            }
            return maxMultiplier;
        }

        internal IEnumerable<EdictEntry> getEntries(ISet<string> allowedPOS, bool isZeroSuffix) {
            return from re in getRatedEntries(allowedPOS, isZeroSuffix) select re.entry;
        }

        internal IEnumerable<RatedEntry> getRatedEntries(ISet<string> allowedPOS, bool isZeroSuffix) {
            bool isEmpty = allowedPOS == null || allowedPOS.Count == 0;
            foreach (RatedEntry re in entries) {
                if (isZeroSuffix && (re.entry.POS.Count == 0 || !Inflector.knownPOS.Overlaps(re.entry.POS)) || (!isEmpty && allowedPOS.Overlaps(re.entry.POS))) {
                    yield return re;
                }
            }
        }

        internal IEnumerable<Tuple<int, RatedEntry>> getRatedEntriesWithPageNumber(ISet<string> allowedPOS, bool isZeroSuffix) {
            bool isEmpty = allowedPOS == null || allowedPOS.Count == 0;
            int i = 0;
            List<Tuple<int, RatedEntry>> firstMatches = new List<Tuple<int, RatedEntry>>();
            List<Tuple<int, RatedEntry>> otherMatches = new List<Tuple<int, RatedEntry>>();
            foreach (RatedEntry re in entries) {
                if (isZeroSuffix && (re.entry.POS.Count == 0 || !Inflector.knownPOS.Overlaps(re.entry.POS)) || (!isEmpty && allowedPOS.Overlaps(re.entry.POS))) {
                    firstMatches.Add(Tuple.Create(i, re));
                } else {
                    if (stem != "") { // buggy bugs :(
                        otherMatches.Add(Tuple.Create(i, re));
                    }
                }
                i += 1;
            }
            return firstMatches.OrderByDescending((t) => t.Item2).Concat(otherMatches.OrderByDescending((t) => t.Item2));
        }

        internal EdictEntry findAnyName() {
            int pageNum = Settings.app.getSelectedPage(stem) % 1000;
            if (pageNum >= 0 && pageNum < entries.Count) {
                var entry = entries[pageNum].entry;
                if (entry.POS.Contains("name")) {
                    return entry;
                }
            }
            foreach (var re in entries) {
                if (re.entry.POS.Contains("name")) {
                    return re.entry;
                }
            }
            return null;
        }
    }
}
