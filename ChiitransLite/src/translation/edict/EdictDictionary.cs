using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.translation.edict.inflect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ChiitransLite.translation.edict {
    class EdictDictionary {

        private Dictionary<string, string> entities;
        public Dictionary<string, string> definitions { get; private set; }
        public Dictionary<string, string> nameDefinitions { get; private set; }
        
        private IDictionary<string, EdictMatch> mainIndex;
        private Dictionary<string, EdictMatch> kanaIndex;
        private Dictionary<string, EdictMatch> zeroStemForms;
        private Inflector inflect;
        private const int maxKeyLength = 12;

        internal void load(Inflector inflect) {
            try {
                this.inflect = inflect;
                mainIndex = new ConcurrentDictionary<string, EdictMatch>();
                kanaIndex = new Dictionary<string, EdictMatch>();
                definitions = null;
                nameDefinitions = null;
                entities = new Dictionary<string, string>();

                using (XmlTextReader xml = new XmlTextReader(Settings.app.JMdictPath)) {
                    xml.DtdProcessing = DtdProcessing.Parse;
                    xml.WhitespaceHandling = WhitespaceHandling.None;
                    xml.EntityHandling = EntityHandling.ExpandCharEntities;
                    while (xml.Read()) {
                        switch (xml.NodeType) {
                            case XmlNodeType.Element:
                                if (xml.Name == "entry") {
                                    readEntry(xml);
                                }
                                break;
                        }
                    }
                }

                /* todo HACK */
                EdictEntry ore = mainIndex["俺"].entries[0].entry;
                addToIndex(mainIndex, "オレ", 2.0, ore);
                RatedEntry tachi = kanaIndex["たち"].entries.First((re) => re.entry.kanji[0].text == "達");
                tachi.rate = 2.0F;
                RatedEntry ii = kanaIndex["い"].entries.First((re) => re.entry.kanji[0].text == "良い");
                addToIndex(kanaIndex, "いい", 2.0, ii.entry);
                mainIndex.Remove("もの");
                var haji = mainIndex["初めまして"].entries[0];
                haji.rate = 2.0F;
                var dake = kanaIndex["だけ"].entries.First((re) => re.entry.kanji[0].text == "丈");
                dake.rate = 2.0F;
                /*var tai = mainIndex["た"].entries.First((re) => re.entry.kanji.Count == 0 && re.entry.kana[0].text == "たい");
                tai.rate = 3.0F;*/
                var gozaimasu = mainIndex["御座います"].entries[0].entry;
                gozaimasu.POS.Add("v-imasu");
                addToIndex(gozaimasu, 2.0F, 2.0F);
                mainIndex.Remove("御座います");
                kanaIndex.Remove("ございます");
                var go = mainIndex["御"].entries[0].entry;
                addToIndex(kanaIndex, "ご", 2.0, go);
                addToIndex(kanaIndex, "お", 2.0, go);
                kanaIndex["って"].entries[0].rate = 2.0F;

                zeroStemForms = new Dictionary<string, EdictMatch>();
                EdictMatch zeroStem = mainIndex[""];
                foreach (RatedEntry re in zeroStem.entries) {
                    foreach (string suffix in inflect.getAllSuffixes(re.entry.POS)) {
                        zeroStemForms[suffix] = zeroStem;
                    }
                }

                definitions = entities;
                entities = new Dictionary<string, string>();

                Task.Factory.StartNew(() => {
                    if (Settings.app.nameDict != NameDictLoading.NONE) {
                        using (XmlTextReader xml = new XmlTextReader(Settings.app.JMnedictPath)) {
                            xml.DtdProcessing = DtdProcessing.Parse;
                            xml.WhitespaceHandling = WhitespaceHandling.None;
                            xml.EntityHandling = EntityHandling.ExpandCharEntities;
                            while (xml.Read()) {
                                switch (xml.NodeType) {
                                    case XmlNodeType.Element:
                                        if (xml.Name == "entry") {
                                            readNameEntry(xml);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    nameDefinitions = entities;
                    entities = null;
                    mainIndex = new Dictionary<string, EdictMatch>(mainIndex); // no concurrency needed at this point
                    GC.Collect();
                });
                 
            } catch (Exception ex) {
                Logger.logException(ex);
            }
        }

        private void readEntry(XmlReader xml) {
            EdictEntryBuilder entry = new EdictEntryBuilder();
            while (xml.Read()) {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Name == "entry") break;
                if (xml.NodeType == XmlNodeType.Element) {
                    switch (xml.Name) {
                        case "k_ele":
                            readKanji(entry, xml);
                            break;
                        case "r_ele":
                            readReading(entry, xml);
                            break;
                        case "sense":
                            readSense(entry, xml);
                            break;
                    }
                }
            }
            addToIndex(entry);
        }

        private void readNameEntry(XmlReader xml) {
            EdictEntryBuilder entry = new EdictEntryBuilder();
            entry.addPOS("name");
            while (xml.Read()) {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Name == "entry") break;
                if (xml.NodeType == XmlNodeType.Element) {
                    switch (xml.Name) {
                        case "k_ele":
                            readKanji(entry, xml);
                            break;
                        case "r_ele":
                            readReading(entry, xml);
                            break;
                        case "trans":
                            readTrans(entry, xml);
                            break;
                    }
                }
            }
            addToNameIndex(entry);
        }

        private void readKanji(EdictEntryBuilder entry, XmlReader xml) {
            string text = null;
            double mult = 1.0;
            List<string> misc = null;
            while (xml.Read()) {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Name == "k_ele") break;
                if (xml.NodeType == XmlNodeType.Element) {
                    string name = xml.Name;
                    switch (name) {
                        case "keb":
                            text = xml.ReadString();
                            break;
                        case "ke_inf":
                            if (misc == null) misc = new List<string>();
                            misc.Add(fromEntity(xml));
                            break;
                        case "ke_pri":
                            mult = Math.Max(mult, getMultiplierFromCode(xml.ReadString()));
                            break;
                    }
                }
            }
            if (text != null) {
                DictionaryKeyBuilder s = new DictionaryKeyBuilder(text, mult, misc);
                entry.addKanji(s);
            }
        }

        private void readReading(EdictEntryBuilder entry, XmlReader xml) {
            string text = null;
            double mult = 1.0;
            List<string> misc = null;
            while (xml.Read()) {
                if (xml.NodeType == XmlNodeType.EndElement) {
                    if (xml.Name == "r_ele") {
                        break;
                    }
                }
                if (xml.NodeType == XmlNodeType.Element) {
                    string name = xml.Name;
                    switch (name) {
                        case "reb":
                            text = xml.ReadString();
                            break;
                        case "re_nokanji":
                            mult = 1.5;
                            break;
                        case "re_restr":
                            // ignore for now
                            break;
                        case "re_inf":
                            if (misc == null) misc = new List<string>();
                            misc.Add(fromEntity(xml));
                            break;
                        case "re_pri":
                            if (mult > 0) {
                                mult = Math.Max(mult, getMultiplierFromCode(xml.ReadString()));
                            }
                            break;
                    }
                }
            }
            if (text != null) {
                DictionaryKeyBuilder s = new DictionaryKeyBuilder(text, mult, misc);
                entry.addKana(s);
            }
        }

        private void readSense(EdictEntryBuilder entry, XmlReader xml) {
            DictionarySense sense = new DictionarySense();
            double globalMult = 1.0;
            double kanaMult = 0.8;
            while (xml.Read()) {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Name == "sense") break;
                if (xml.NodeType == XmlNodeType.Element) {
                    if (xml.Name == "gloss") {
                        string lang = xml.GetAttribute("xml:lang");
                        if (lang == "eng") lang = null;
                        string value = xml.ReadString();
                        sense.addGloss(lang, value);
                    } else {
                        switch (xml.Name) {
                            case "pos":
                                entry.addPOS(fromEntity(xml));
                                break;
                            case "field":
                            case "misc":
                            case "dial":
                                string v = fromEntity(xml);
                                adjustMultipliers(v, ref globalMult, ref kanaMult);
                                sense.addMisc(v);
                                break;
                        }
                    }
                }
            }
            entry.addSense(sense, globalMult, kanaMult);
        }

        private void readTrans(EdictEntryBuilder entry, XmlReader xml) {
            DictionarySense sense = new DictionarySense();
            while (xml.Read()) {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Name == "trans") break;
                if (xml.NodeType == XmlNodeType.Element) {
                    if (xml.Name == "trans_det") {
                        string lang = xml.GetAttribute("xml:lang");
                        if (lang == "eng") lang = null;
                        string value = xml.ReadString();
                        sense.addGloss(lang, value);
                    } else {
                        switch (xml.Name) {
                            case "name_type":
                                entry.nameType = fromEntity(xml);
                                break;
                        }
                    }
                }
            }
            entry.addSense(sense, 1.0, 1.0);
        }

        private void adjustMultipliers(string misc, ref double globalMult, ref double kanaMult) {
            switch (misc) {
                case "obs":
                case "rare":
                    globalMult = 0.5;
                    break;
                case "obsc":
                case "ok":
                    globalMult = 0.2;
                    break;
                case "eK":
                    kanaMult = 0;
                    break;
                case "ek":
                    kanaMult = 1.5;
                    break;
                case "uK":
                    kanaMult = 0.25;
                    break;
                case "uk":
                    kanaMult = 1.0;
                    break;
            }
        }

        private void addToIndex(IDictionary<string, EdictMatch> dict, string key, double rate, EdictEntry entry) {
            if (key.Length > maxKeyLength) {
                return;
            }
            if (key.Length == 0) {
                rate = 1.5;
            }
            EdictMatch entries;
            RatedEntry newEntry = new RatedEntry { entry = entry, rate = (float)rate };
            if (dict.TryGetValue(key, out entries)) {
                entries.addEntry(newEntry);
            } else {
                entries = new EdictMatch(key);
                entries.addEntry(newEntry);
                dict.Add(key, entries);
            }
        }

        private void addToIndex(IDictionary<string, EdictMatch> dict, string key, double rate, EdictEntryBuilder entry) {
            addToIndex(dict, key, rate, entry.build());
        }
        
        private void addToIndex(EdictEntryBuilder entry) {
            foreach (DictionaryKeyBuilder key in entry.kanji) {
                if (key.text.Length == 1 && TextUtils.isAllKatakana(key.text)) {
                    continue;
                }
                double rate = entry.globalMultiplier * key.rate;
                string stem = inflect.getStem(key.text, entry.POS);
                int kanjiCount = stem.Count((c) => TextUtils.isKanji(c));
                if (kanjiCount > 1) {
                    rate = rate * 2.0; // kanji are good
                }
                addToIndex(mainIndex, stem, rate, entry);
            }
            if (entry.kanji.Count == 0 && entry.kana.Count > 0) {
                foreach (DictionaryKeyBuilder key in entry.kana) {
                    if (key.text == "です") { // DESU DESU DESU
                        entry.addPOS("copula");    // DESU DESU DESU
                    }
                    string stem = inflect.getStem(key.text, entry.POS);
                    double rate;
                    if (key.text == "は" || key.text == "が" || key.text == "の" || key.text == "に" || key.text == "し") {
                        rate = 2.0; // these are good
                    } else {
                        rate = entry.globalMultiplier * Math.Max(key.rate, 1.3);
                        /*if (stem.Length == 1 && rate > 1.0) {
                            rate = 1.0;
                        }*/
                    }
                    addToIndex(mainIndex, stem, rate, entry);
                }
            } else {
                foreach (DictionaryKeyBuilder key in entry.kana) {
                    if (key.text.Length > 1) {
                        string stem = inflect.getStem(key.text, entry.POS);
                        double rate = entry.globalMultiplier * entry.globalKanaMultiplier * key.rate;
                        if (rate > 0) {
                            addToIndex(kanaIndex, stem, rate, entry);
                        }
                    }
                }
            }
        }

        private void addToIndex(EdictEntry entry, double kanjiRate, double kanaRate) {
            foreach (DictionaryKey key in entry.kanji) {
                if (key.text.Length == 1 && TextUtils.isAllKatakana(key.text)) {
                    continue;
                }
                string stem = inflect.getStem(key.text, entry.POS);
                addToIndex(mainIndex, stem, kanjiRate, entry);
            }
            if (entry.kanji.Count == 0 && entry.kana.Count > 0) {
                foreach (DictionaryKey key in entry.kana) {
                    string stem = inflect.getStem(key.text, entry.POS);
                    addToIndex(mainIndex, stem, kanjiRate, entry);
                }
            } else {
                foreach (DictionaryKey key in entry.kana) {
                    if (key.text.Length > 1) {
                        string stem = inflect.getStem(key.text, entry.POS);
                        if (kanaRate > 0) {
                            addToIndex(kanaIndex, stem, kanaRate, entry);
                        }
                    }
                }
            }
        }

        private void addToNameIndex(EdictEntryBuilder entry) {
            bool proceed = true;
            if (Settings.app.nameDict == NameDictLoading.NAMES) {
                string nameType = entry.nameType;
                proceed = nameType == "surname" || nameType == "masc" || nameType == "fem" || nameType == "person" || nameType == "given";
            }
            if (proceed) {
                foreach (DictionaryKeyBuilder key in entry.kanji) {
                    addToIndex(mainIndex, key.text, 0.85, entry);
                }
            }
        }

        private string fromEntity(XmlReader xr) {
            xr.Read();
            if (xr.NodeType == XmlNodeType.EntityReference) {
                string name = xr.Name;
                if (!entities.ContainsKey(name)) {
                    xr.ResolveEntity();
                    entities.Add(name, xr.ReadString());
                }
                return name;
            } else {
                throw new MyException("Wtf?" + xr.NodeType.ToString() + ": " + xr.Name + ": " + xr.ReadString());
            }
        }

        private double getMultiplierFromCode(string value) {
            if (value == "ichi1" || value == "gai1") {
                return 1.5;
            } else if (value == "news1") {
                return 1.4;
            } else if (value.StartsWith("nf")) {
                int freq;
                if (int.TryParse(value.Substring(2), out freq)) {
                    if (freq < 10) {
                        return 1.4;
                    } else {
                        return 1.2;
                    }
                }
            }
            return 1.2;
        }

        internal IEnumerable<EdictMatchWithType> findMatching(string text, int position) {
            for (int i = 1; i < maxKeyLength && position + i <= text.Length; ++i) {
                string key = text.Substring(position, i);
                EdictMatch match;
                if (zeroStemForms.TryGetValue(key, out match)) {
                    yield return new EdictMatchWithType(match, EdictMatchType.KANJI);
                }
                if (mainIndex.TryGetValue(key, out match)) {
                    yield return new EdictMatchWithType(match, EdictMatchType.KANJI);
                }
                if (kanaIndex.TryGetValue(key, out match)) {
                    yield return new EdictMatchWithType(match, EdictMatchType.READING);
                }
                if (TextUtils.isAllKatakana(key)) {
                    string hira = TextUtils.katakanaToHiragana(key);
                    if (mainIndex.TryGetValue(hira, out match)) {
                        yield return new EdictMatchWithType(match, EdictMatchType.FROM_KATAKANA);
                    }
                    if (kanaIndex.TryGetValue(hira, out match)) {
                        yield return new EdictMatchWithType(match, EdictMatchType.FROM_KATAKANA);
                    }
                }
                if (Settings.session.userNames.TryGetValue(key, out match)) {
                    yield return new EdictMatchWithType(match, EdictMatchType.KANJI);
                }
            }
        }

        internal EdictMatch lookup(string p) {
            return mainIndex.GetOrDefault(p);
        }

        internal EdictMatch findName(string name) {
            EdictMatch m = Settings.session.userNames.GetOrDefault(name);
            if (m == null) {
                m = mainIndex.GetOrDefault(name);
            }
            return m;
        }
    }
}
