using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.translation.edict.inflect {
    class InflectionTrie {
        public List<ConjugationsVariantJson> forms = new List<ConjugationsVariantJson>();
        public List<ConjugationsVariantJson> linkForms = new List<ConjugationsVariantJson>();
        public Dictionary<char, InflectionTrie> children = new Dictionary<char, InflectionTrie>();

        private InflectionTrie get(char c) {
            InflectionTrie res;
            if (!children.TryGetValue(c, out res)) {
                res = new InflectionTrie();
                children.Add(c, res);
            }
            return res;
        }

        public void addForm(ConjugationsVariantJson form, Dictionary<string, ConjugationsJson> conjugations) {
            var cur = this;
            if (!form.Ignore) {
                foreach (char c in form.Suffix) {
                    cur = cur.get(c);
                }
                cur.forms.Add(form);
            }
            if (form.NextType != null) {
                string baseSuf = getStem(conjugations, form.Suffix, form.NextType);
                cur = this;
                foreach (char c in baseSuf) {
                    cur = cur.get(c);
                }
                cur.linkForms.Add(form);
            }
        }

        private static string getStem(Dictionary<string, ConjugationsJson> conjugations, string baseForm, string pos) {
            ConjugationsJson conj;
            if (conjugations.TryGetValue(pos, out conj)) {
                foreach (string suffix in conj.BaseFormSuffixes) {
                    if (baseForm.EndsWith(suffix)) {
                        return baseForm.Substring(0, baseForm.Length - suffix.Length);
                    }
                }
                return baseForm;
            } else {
                return baseForm;
            }
        }
    }
}
