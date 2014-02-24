using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.translation.atlas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class OptionsForm : Form {
        private static OptionsForm _instance = null;

        public static OptionsForm instance {
            get {
                if (_instance == null) {
                    _instance = new OptionsForm();
                }
                return _instance;
            }
        }

        private class InteropMethods {
            private readonly OptionsForm form;

            public InteropMethods(OptionsForm form) {
                this.form = form;
            }

            public object getOptions() {
                return form.getOptions();
            }

            public void saveOptions(IDictionary op) {
                form.saveOptions(op);
            }

            public void close() {
                form.Close();
            }

            public void showPoFiles() {
                form.Invoke(new Action(() => {
                    new POFilesForm().ShowDialog();
                }));
            }

            public void showHookForm() {
                form.showHookForm();
            }

            public void resetParsePreferences() {
                form.resetParsePreferences();
            }

        }

        public OptionsForm() {
            InitializeComponent();
            FormUtil.restoreLocation(this);
            webBrowser1.ObjectForScripting = new BrowserInterop(webBrowser1, new InteropMethods(this));
            webBrowser1.Url = Utils.getUriForBrowser("options.html");
        }

        private void HookOptionsForm_Move(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void HookOptionsForm_Resize(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void HookOptionsForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                Hide();
                TranslationForm.instance.moveBackgroundForm();
            }
        }

        private object getOptions() {
            bool isDefaultSession = Settings.session.processExe == null;
            return new {
                clipboard = Settings.app.clipboardTranslation,
                sentenceDelay = Settings.session.sentenceDelay.TotalMilliseconds,
                enableHooks = !isDefaultSession,
                enableSentenceDelay = !isDefaultSession,
                display = Settings.app.translationDisplay.ToString(),
                okuri = Settings.app.okuriganaType.ToString(),
                theme = Settings.app.cssTheme,
                themes = getThemes(),
                separateWords = Settings.app.separateWords,
                separateSpeaker = Settings.app.separateSpeaker,
                nameDict = Settings.app.nameDict.ToString(),
                atlasEnv = Settings.app.atlasEnv,
                atlasEnvList = getAtlasEnvList()
            };
        }

        internal void saveOptions(IDictionary op) {
            bool clipboard = (bool)op["clipboard"];
            int sentenceDelay = (int)op["sentenceDelay"];
            string displayStr = (string)op["display"];
            string okuriStr = (string)op["okuri"];
            string theme = (string)op["theme"];
            bool separateWords = (bool)op["separateWords"];
            bool separateSpeaker = (bool)op["separateSpeaker"];
            string nameDictStr = (string)op["nameDict"];
            string atlasEnv = (string)op["atlasEnv"];

            TranslationForm.instance.setClipboardTranslation(clipboard);
            if (sentenceDelay >= 10) {
                Settings.session.sentenceDelay = TimeSpan.FromMilliseconds(sentenceDelay);
            }
            TranslationDisplay display;
            OkuriganaType okuri;
            NameDictLoading nameDict;
            if (Enum.TryParse(displayStr, out display)) {
                Settings.app.translationDisplay = display;
            }
            if (Enum.TryParse(okuriStr, out okuri)) {
                Settings.app.okuriganaType = okuri;
            }
            if (Enum.TryParse(nameDictStr, out nameDict)) {
                Settings.app.nameDict = nameDict;
            }
            if (atlasEnv != Settings.app.atlasEnv) {
                Settings.app.atlasEnv = atlasEnv;
                Atlas.instance.reinitialize();
            }
            Settings.app.cssTheme = theme;
            Settings.app.separateWords = separateWords;
            Settings.app.separateSpeaker = separateSpeaker;
            TranslationForm.instance.applyCurrentSettings();
        }

        public void updateAndShow() {
            if (!Visible) {
                webBrowser1.callScript("resetOptions", Utils.toJson(getOptions()));
            }
            Show();
            TranslationForm.instance.moveBackgroundForm();
            Task.Factory.StartNew(() => { // some fcking bug, cannot make the form visible on top without deferring activation
                Invoke(new Action(() => Activate()));
            });
        }


        internal void showHookForm() {
            UserHookForm.instance.Show();
            if (UserHookForm.instance.WindowState == FormWindowState.Minimized) {
                UserHookForm.instance.WindowState = FormWindowState.Normal;
            }
            UserHookForm.instance.Activate();
        }

        private IEnumerable<string> getThemes() {
            return Directory.GetFiles(Path.Combine(Utils.getRootPath(), "www\\themes"), "*.css").Select(Path.GetFileNameWithoutExtension);
        }

        private IEnumerable<string> getAtlasEnvList() {
            var res = Atlas.instance.getEnvList().ToList();
            if (!res.Contains(Settings.app.atlasEnv)) {
                res.Add(Settings.app.atlasEnv);
            }
            return res;
        }

        internal void resetParsePreferences() {
            if (Utils.confirm("Reset all parse preferences?\r\nThis includes selected dictionary pages, user names and parse result bans.")) {
                Settings.app.resetSelectedPages();
                Settings.app.resetWordBans();
                Settings.app.resetSelectedReadings();
                Settings.session.resetUserNames();
                Utils.info("Parse preferences have been reset to default.");
            }
        }
    }
}
