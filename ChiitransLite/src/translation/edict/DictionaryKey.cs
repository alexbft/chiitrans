using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class DictionaryKey {
        public readonly string text;
        public readonly List<String> misc;

        public DictionaryKey(string text, List<string> misc) {
            this.text = text;
            this.misc = misc;
        }
    }
}
