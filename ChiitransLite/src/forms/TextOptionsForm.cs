using ChiitransLite.misc;
using ChiitransLite.settings;
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
    public partial class TextOptionsForm : Form {
        private static TextOptionsForm _instance = null;

        public static TextOptionsForm instance {
            get {
                if (_instance == null) {
                    _instance = new TextOptionsForm();
                }
                return _instance;
            }
        }

        private class InteropMethods {
            private readonly TextOptionsForm form;

            public InteropMethods(TextOptionsForm form) {
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

            public void resetParsePreferences() {
                form.resetParsePreferences();
            }
        }

        public TextOptionsForm() {
            InitializeComponent();
            FormUtil.restoreLocation(this);
            webBrowser1.ObjectForScripting = new BrowserInterop(webBrowser1, new InteropMethods(this));
            webBrowser1.Url = Utils.getUriForBrowser("textform-options.html");
        }

        private void TextOptionsForm_Move(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void TextOptionsForm_Resize(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void TextOptionsForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                Hide();
                TranslationForm.instance.moveBackgroundForm();
            }
        }

        private IEnumerable<String> getThemes() {
            return Directory.GetFiles(Path.Combine(Utils.getRootPath(), "www\\themes"), "*.css").Select(Path.GetFileNameWithoutExtension);
        }

        private object getOptions() {
            return new {
                display = Settings.app.translationDisplay.ToString(),
                okuri = Settings.app.okuriganaType.ToString(),
                theme = Settings.app.cssTheme,
                themes = getThemes(),
                separateWords = Settings.app.separateWords,
                separateSpeaker = Settings.app.separateSpeaker
            };
        }

        internal void saveOptions(IDictionary op) {
            string displayStr = (string)op["display"];
            string okuriStr = (string)op["okuri"];
            string theme = (string)op["theme"];
            bool separateWords = (bool)op["separateWords"];
            bool separateSpeaker = (bool)op["separateSpeaker"];

            TranslationDisplay display;
            OkuriganaType okuri;
            if (Enum.TryParse(displayStr, out display)) {
                Settings.app.translationDisplay = display;
            }
            if (Enum.TryParse(okuriStr, out okuri)) {
                Settings.app.okuriganaType = okuri;
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

        internal void resetParsePreferences() {
            if (Utils.confirm("Reset all parse preferences?\r\nThis includes selected dictionary pages, user names and parse result bans.")) {
                Settings.app.resetSelectedPages();
                Settings.app.resetWordBans();
                Settings.session.resetUserNames();
                Utils.info("Parse preferences have been reset to default.");
            }
        }
    }
}
