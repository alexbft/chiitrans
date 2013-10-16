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
    public partial class HookOptionsForm : Form {
        private static HookOptionsForm _instance = null;

        public static HookOptionsForm instance {
            get {
                if (_instance == null) {
                    _instance = new HookOptionsForm();
                }
                return _instance;
            }
        }

        private class InteropMethods {
            private readonly HookOptionsForm form;

            public InteropMethods(HookOptionsForm form) {
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

        }

        public HookOptionsForm() {
            InitializeComponent();
            FormUtil.restoreLocation(this);
            webBrowser1.ObjectForScripting = new BrowserInterop(webBrowser1, new InteropMethods(this));
            webBrowser1.Url = Utils.getUriForBrowser("hook-options.html");
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
                enableSentenceDelay = !isDefaultSession
            };
        }

        internal void saveOptions(IDictionary op) {
            bool clipboard = (bool)op["clipboard"];
            int sentenceDelay = (int)op["sentenceDelay"];
            TranslationForm.instance.setClipboardTranslation(clipboard);
            if (sentenceDelay >= 10) {
                Settings.session.sentenceDelay = TimeSpan.FromMilliseconds(sentenceDelay);
            }
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
    }
}
