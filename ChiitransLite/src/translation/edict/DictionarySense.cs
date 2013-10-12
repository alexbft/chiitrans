using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class DictionarySense {
        public List<string> glossary = new List<string>();
        public List<string> misc = null;

        internal void addGloss(string lang, string value) {
            if (string.IsNullOrEmpty(lang)) {
                glossary.Add(value);
            }
        }

        internal void addMisc(string value) {
            if (misc == null) misc = new List<string>();
            misc.Add(value);
        }
    }
}
