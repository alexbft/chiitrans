using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChiitransLite.translation.po {
    class PoTranslation {
        public readonly string original;
        public readonly string translation;
        public readonly int index;

        public PoTranslation(string original, string translation, int index) {
            this.original = original;
            this.translation = translation;
            this.index = index;
        }

        internal string getOriginalClean() {
            return clean(original);
        }

        public static string clean(string s) {
            if (s == null) return null;
            return Regex.Replace(s.Trim(), @"\[.*?\]", " ");
        }

        internal string getCleanTranslation() {
            return clean(translation);
        }
    }
}
