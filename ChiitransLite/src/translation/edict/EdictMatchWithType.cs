using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict {
    class EdictMatchWithType {
        public readonly EdictMatch match;
        public readonly EdictMatchType matchType;

        public EdictMatchWithType(EdictMatch match, EdictMatchType matchType) {
            this.match = match;
            this.matchType = matchType;
        }
    }
}
