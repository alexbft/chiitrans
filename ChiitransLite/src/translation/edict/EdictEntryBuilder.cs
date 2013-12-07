using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class EdictEntryBuilder {
        public List<DictionaryKeyBuilder> kanji = new List<DictionaryKeyBuilder>();
        public List<DictionaryKeyBuilder> kana = new List<DictionaryKeyBuilder>();
        public List<DictionarySense> sense = new List<DictionarySense>();
        public List<string> POS = new List<string>();
        public double globalMultiplier = 1.0;
        public double globalKanaMultiplier = 0.8;
        public string nameType = null;

        public void addKanji(DictionaryKeyBuilder kanji) {
            this.kanji.Add(kanji);
        }

        public void addKana(DictionaryKeyBuilder kana) {
            this.kana.Add(kana);
        }

        internal void addPOS(string p) {
            POS.Add(p);
        }

        internal void addSense(DictionarySense sense, double globalMult = 1.0, double kanaMult = 0.8) {
            if (this.sense.Count == 0) {
                globalMultiplier = globalMult;
                globalKanaMultiplier = kanaMult;
            } else {
                globalMultiplier = Math.Max(globalMultiplier, globalMult);
                globalKanaMultiplier = Math.Max(globalKanaMultiplier, kanaMult);
            }
            this.sense.Add(sense);
        }

        internal EdictEntry build() {
            return new EdictEntry {
                kanji = kanji.Select((k) => k.build()).ToList(),
                kana = kana.Select((k) => k.build()).ToList(),
                sense = sense,
                POS = POS.Distinct().ToList(),
                nameType = nameType
            };
        }
    }
}
