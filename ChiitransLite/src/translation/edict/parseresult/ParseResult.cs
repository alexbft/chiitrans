using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.translation.edict.inflect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict.parseresult {
    abstract class ParseResult {

        public enum ParseResultType {
            UNPARSED,
            WORD,
            COMPLEX
        }

        internal static ParseResult unparsed(string p) {
            return new UnparsedParseResult(p);
        }

        internal static ParseResult concat(IEnumerable<ParseResult> results) {
            List<ParseResult> resultList = results.ToList();
            if (resultList.Count == 0) {
                throw new ArgumentException("Empty list", "results");
            } else if (resultList.Count == 1) {
                return resultList.First();
            } else {
                return new ComplexParseResult(resultList);
            }
        }

        public abstract int length { get; }
        public abstract ParseResultType type { get; }
        public int id { get; set; }

        public abstract string asText();
        internal abstract object serializeFull();
        internal abstract object serialize(OkuriganaType okType);

        public string asJsonFull() {
            return Utils.toJson(serializeFull());
        }

        public abstract IEnumerable<ParseResult> getParts();

        public static string serializeResult(ParseResult p) {
            object parts;
            if (p.type == ParseResultType.COMPLEX) {
                parts = p.serialize(Settings.app.okuriganaType);
            } else {
                parts = new object[] { p.serialize(Settings.app.okuriganaType) };
            }
            var serializedObj = new {
                parts = parts,
                okuri = Settings.app.okuriganaType.ToString()
            };
            return Utils.toJson(serializedObj);
        }

        internal abstract EdictMatchType? getMatchTypeOf(string text);
    }
}
