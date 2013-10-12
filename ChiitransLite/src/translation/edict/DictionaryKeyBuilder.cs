using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class DictionaryKeyBuilder {
        public string text { get; set; }
        public double rate { get; set; }
        public List<String> misc { get; private set; }
        
        public DictionaryKeyBuilder(string text, double rate = 1.0, List<string> misc = null) {
            this.text = text;
            this.rate = rate;
            this.misc = misc;
        }

        public DictionaryKey build() {
            return new DictionaryKey(text, misc);
        }
    }
}
