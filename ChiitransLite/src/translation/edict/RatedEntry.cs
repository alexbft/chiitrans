using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class RatedEntry : IComparable<RatedEntry> {
        public EdictEntry entry;
        public float rate;

        public int CompareTo(RatedEntry other) {
            return rate.CompareTo(other.rate);
            /*
            if (res == 0) {
                res = -entry.kana[0].text.CompareTo(other.entry.kana[0].text);
            }
            return res;
            */
        }
    }
}
