using ChiitransLite.misc;
using ChiitransLite.translation.edict;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace ChiitransLite.settings {
    class Settings {
        private static Settings _app = new Settings();

        public static Settings app {
            get {
                return _app;
            }
        }

        protected Settings() {
            initSelectedPages();
            initBannedWords();
        }

        private static volatile SessionSettings cachedSessionSettings = null;
        public static SessionSettings session {
            get {
                if (cachedSessionSettings == null) {
                    cachedSessionSettings = SessionSettings.getDefault();
                }
                return cachedSessionSettings;
            }
        }

        public readonly string JMdictPath = Path.Combine(Utils.getRootPath(), "data/JMdict.xml");
        public readonly string JMnedictPath = Path.Combine(Utils.getRootPath(), "data/JMnedict.xml");
        public readonly string ConjugationsPath = Path.Combine(Utils.getRootPath(), "data/Conjugations.txt");

        private ConcurrentDictionary<string, int> selectedPages;
        private bool selectedPagesDirty = false;
        private ConcurrentDictionary<string, bool> bannedWords;
        private ConcurrentDictionary<string, bool> bannedWordsKana;
        private bool isBannedWordsDirty = false;

        public T getProperty<T>(string prop) {
            T res;
            getProperty(prop, out res);
            return res;
        }

        public bool getProperty<T>(string prop, out T value) {
            try {
                object res = Properties.Settings.Default[prop];
                value = (T)res;
                return true;
            } catch (NullReferenceException) {
                value = default(T);
                return false;
            } catch (InvalidCastException) {
                value = default(T);
                return false;
            } catch (SettingsPropertyNotFoundException) {
                value = default(T);
                return false;
            }
        }

        public void setProperty<T>(string prop, T value) {
            Properties.Settings.Default[prop] = value;
        }

        public void save() {
            if (selectedPages != null && selectedPagesDirty) {
                Properties.Settings.Default.selectedPages = Utils.getJsonSerializer().Serialize(selectedPages);
                selectedPagesDirty = false;
            }
            if (bannedWords != null && isBannedWordsDirty) {
                Properties.Settings.Default.bannedWords = Utils.getJsonSerializer().Serialize(bannedWords.Keys);
                Properties.Settings.Default.bannedWordsKana = Utils.getJsonSerializer().Serialize(bannedWordsKana.Keys);
                isBannedWordsDirty = false;
            }
            Properties.Settings.Default.Save();
            SessionSettings.saveAll();
        }

        private void initSelectedPages() {
            string selectedPagesJson = Properties.Settings.Default.selectedPages;
            try {
                selectedPages = Utils.getJsonSerializer().Deserialize<ConcurrentDictionary<string, int>>(selectedPagesJson);
                if (selectedPages == null) selectedPages = new ConcurrentDictionary<string, int>();
            } catch {
                selectedPages = new ConcurrentDictionary<string, int>();
            }
        }

        private void initBannedWords() {
            string bannedWordsJson = Properties.Settings.Default.bannedWords;
            List<string> words;
            try {
                words = Utils.getJsonSerializer().Deserialize<List<string>>(bannedWordsJson);
                if (words == null) words = new List<string>();
            } catch {
                words = new List<string>();
            }
            bannedWords = new ConcurrentDictionary<string,bool>(words.ToDictionary((w) => w, (w) => true));
            string bannedWordsKanaJson = Properties.Settings.Default.bannedWordsKana;
            try {
                words = Utils.getJsonSerializer().Deserialize<List<string>>(bannedWordsKanaJson);
                if (words == null) words = new List<string>();
            } catch {
                words = new List<string>();
            }
            bannedWordsKana = new ConcurrentDictionary<string, bool>(words.ToDictionary((w) => w, (w) => true));
        }
        
        internal int getSelectedPage(string stem) {
            return selectedPages.GetOrDefault(stem, -1);
        }

        internal void setSelectedPage(string stem, int page) {
            selectedPages[stem] = page;
            selectedPagesDirty = true;
        }

        internal static void setCurrentSession(string exeName) {
            cachedSessionSettings = SessionSettings.getByExeName(exeName);
        }

        internal static void setDefaultSession() {
            cachedSessionSettings = SessionSettings.getDefault();
        }

        public OkuriganaType okuriganaType {
            get {
                OkuriganaType res;
                if (Enum.TryParse(Properties.Settings.Default.okuriganaType, out res)) {
                    return res;
                } else {
                    return OkuriganaType.NORMAL;
                }
            }
            set {
                Properties.Settings.Default.okuriganaType = value.ToString();
            }
        }

        public TranslationDisplay translationDisplay {
            get {
                TranslationDisplay res;
                if (Enum.TryParse(Properties.Settings.Default.translationDisplay, out res)) {
                    return res;
                } else {
                    return TranslationDisplay.BOTH;
                }
            }
            set {
                Properties.Settings.Default.translationDisplay = value.ToString();
            }
        }

        public bool clipboardTranslation {
            get {
                return Properties.Settings.Default.clipboardTranslation;
            }
            set {
                Properties.Settings.Default.clipboardTranslation = value;
            }
        }

        public NonJapaneseLocaleWatDo nonJpLocale {
            get {
                NonJapaneseLocaleWatDo res;
                if (Enum.TryParse(Properties.Settings.Default.nonJpLocale, out res)) {
                    return res;
                } else {
                    return NonJapaneseLocaleWatDo.USE_LOCALE_EMULATOR;
                }
            }
            set {
                Properties.Settings.Default.nonJpLocale = value.ToString();
            }
        }

        public bool nonJpLocaleAsk {
            get {
                return Properties.Settings.Default.nonJpLocaleAsk;
            }
            set {
                Properties.Settings.Default.nonJpLocaleAsk = value;
            }
        }

        public string cssTheme {
            get {
                return Properties.Settings.Default.cssTheme;
            }
            set {
                Properties.Settings.Default.cssTheme = value;
            }
        }

        public bool separateWords {
            get {
                return Properties.Settings.Default.separateWords;
            }
            set {
                Properties.Settings.Default.separateWords = value;
            }
        }

        public bool transparentMode {
            get {
                return Properties.Settings.Default.transparentMode;
            }
            set {
                Properties.Settings.Default.transparentMode = value;
            }
        }

        public int transparencyLevel {
            get {
                return Properties.Settings.Default.transparencyLevel;
            }
            set {
                Properties.Settings.Default.transparencyLevel = value;
            }
        }

        public int fontSize {
            get {
                return Properties.Settings.Default.fontSize;
            }
            set {
                Properties.Settings.Default.fontSize = value;
            }
        }

        internal void removeBannedWord(string word) {
            bool dontCare;
            isBannedWordsDirty = bannedWords.TryRemove(word, out dontCare) || isBannedWordsDirty;
            isBannedWordsDirty = bannedWordsKana.TryRemove(word, out dontCare) || isBannedWordsDirty;
        }

        internal void addBannedWord(string word, EdictMatchType matchType) {
            isBannedWordsDirty = (matchType == EdictMatchType.KANJI ? bannedWords : bannedWordsKana).TryAdd(word, true) || isBannedWordsDirty;
        }

        internal bool isWordBanned(string word, EdictMatchType matchType) {
            return (matchType == EdictMatchType.KANJI ? bannedWords : bannedWordsKana).ContainsKey(word);
        }

        internal void resetSelectedPages() {
            selectedPages.Clear();
            selectedPagesDirty = true;
        }

        internal void resetWordBans() {
            bannedWords.Clear();
            bannedWordsKana.Clear();
            isBannedWordsDirty = true;
        }

        public bool atlasAsk {
            get {
                return Properties.Settings.Default.atlasAsk;
            }
            set {
                Properties.Settings.Default.atlasAsk = value;
            }
        }

        public bool ieUpgradeAsk {
            get {
                return Properties.Settings.Default.ieUpgradeAsk;
            }
            set {
                Properties.Settings.Default.ieUpgradeAsk = value;
            }
        }
    }
}
