using ChiitransLite.misc;
using ChiitransLite.settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict.parseresult {
    class ComplexParseResult : ParseResult {

        public ReadOnlyCollection<ParseResult> parts { get; private set; }

        private readonly int totalLength;
        
        internal ComplexParseResult(List<ParseResult> parts) {
            List<ParseResult> my = new List<ParseResult>();
            StringBuilder sb = new StringBuilder();
            int totalLength = 0;
            foreach (ParseResult part in flatten(parts)) {
                if (part.type == ParseResultType.UNPARSED) {
                    sb.Append(((UnparsedParseResult)part).text);
                } else {
                    if (sb.Length > 0) {
                        my.Add(ParseResult.unparsed(sb.ToString()));
                        totalLength += sb.Length;
                        sb.Clear();
                    }
                    my.Add(part);
                    totalLength += part.length;
                }
            }
            if (sb.Length > 0) {
                my.Add(ParseResult.unparsed(sb.ToString()));
                totalLength += sb.Length;
                sb.Clear();
            }
            this.totalLength = totalLength;
            this.parts = new ReadOnlyCollection<ParseResult>(my);
        }

        private IEnumerable<ParseResult> flatten(List<ParseResult> parts) {
            foreach (ParseResult part in parts) {
                if (part.type == ParseResultType.COMPLEX) {
                    foreach (ParseResult innerPart in ((ComplexParseResult)part).parts) {
                        yield return innerPart;
                    }
                } else {
                    yield return part;
                }
            }
        }

        public override int length {
            get { return totalLength; }
        }

        public override ParseResult.ParseResultType type {
            get { return ParseResultType.COMPLEX; }
        }

        public override string asText() {
            return string.Join("", (from p in parts select p.asText()));
        }

        internal override object serialize(OkuriganaType okType) {
            return from p in parts select p.serialize(okType);
        }

        internal override object serializeFull() {
            return from p in parts select p.serializeFull();
        }

        public override IEnumerable<ParseResult> getParts() {
            return parts;
        }

        internal override EdictMatchType? getMatchTypeOf(string text) {
            return parts.Select((p) => p.getMatchTypeOf(text)).FirstOrDefault((t) => t.HasValue);
        }
    }
}
