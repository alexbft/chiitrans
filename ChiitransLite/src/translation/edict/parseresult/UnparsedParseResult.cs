using ChiitransLite.misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict.parseresult {
    class UnparsedParseResult : ParseResult {
        public string text { get; private set; }

        internal UnparsedParseResult(string text) {
            this.text = text == null ? string.Empty : text;
        }

        public override int length {
            get { return text.Length; }
        }

        public override ParseResultType type {
            get { return ParseResultType.UNPARSED; }
        }

        public override string asText() {
            return text;
        }

        internal override object serializeFull() {
            return new { text = text };
        }

        public override IEnumerable<ParseResult> getParts() {
            return Enumerable.Repeat(this, 1);
        }

        internal override object serialize(settings.OkuriganaType okType) {
            return text;
        }

        internal override EdictMatchType? getMatchTypeOf(string text) {
            return null;
        }
    }
}
