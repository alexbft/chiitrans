using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.misc {
    static class IDictionaryExt {
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue)) {
            TValue result;
            if (dict.TryGetValue(key, out result)) {
                return result;
            } else {
                return defaultValue;
            }
        }
    }
}
