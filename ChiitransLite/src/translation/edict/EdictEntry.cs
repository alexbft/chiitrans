using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class EdictEntry {
        public List<DictionaryKey> kanji;
        public List<DictionaryKey> kana;
        public List<DictionarySense> sense;
        public List<string> POS;
        public string nameType;

        internal string getText() {
            if (kanji.Count > 0) {
                return kanji.First().text;
            } else if (kana.Count > 0) {
                return kana.First().text;
            } else {
                return "";
            }
        }

        internal string getNameReading() {
            if (sense.Count > 0 && sense[0].glossary.Count > 0) {
                return sense[0].glossary[0];
            } else {
                if (kana.Count > 0) {
                    return kana[0].text;
                } else {
                    return "";
                }
            }
        }
    }
}
