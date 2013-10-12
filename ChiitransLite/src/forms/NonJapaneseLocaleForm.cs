using ChiitransLite.misc;
using ChiitransLite.settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class NonJapaneseLocaleForm : Form {
        public NonJapaneseLocaleForm() {
            InitializeComponent();
            radioButtonLE.Enabled = Utils.isWindowsVistaOrLater();
            radioButtonAppLocale.Enabled = Utils.isAppLocaleInstalled();
            NonJapaneseLocaleWatDo userChoice = Settings.app.nonJpLocale;
            if (userChoice == NonJapaneseLocaleWatDo.RUN_ANYWAY) {
                radioButtonRun.Checked = true;
            } else {
                if (radioButtonLE.Enabled && (userChoice == NonJapaneseLocaleWatDo.USE_LOCALE_EMULATOR || !radioButtonAppLocale.Enabled)) {
                    radioButtonLE.Checked = true;
                } else if (radioButtonAppLocale.Enabled && (userChoice == NonJapaneseLocaleWatDo.USE_APPLOCALE || !radioButtonLE.Enabled)) {
                    radioButtonAppLocale.Checked = true;
                } else {
                    radioButtonRun.Checked = true;
                }
            }
        }

        internal static NonJapaneseLocaleWatDo show() {
            NonJapaneseLocaleWatDo result = NonJapaneseLocaleWatDo.ABORT;
            MainForm.instance.Invoke(new Action(() => {
                NonJapaneseLocaleForm form = new NonJapaneseLocaleForm();
                if (form.ShowDialog() == DialogResult.OK) {
                    result = form.getSelectedOption();
                    Settings.app.nonJpLocale = result;
                    Settings.app.nonJpLocaleAsk = form.isAskAgain();
                } else {
                    result = NonJapaneseLocaleWatDo.ABORT;
                }
            }));
            return result;
        }

        private bool isAskAgain() {
            return !checkBoxDontAsk.Checked;
        }

        private NonJapaneseLocaleWatDo getSelectedOption() {
            if (radioButtonLE.Checked) {
                return NonJapaneseLocaleWatDo.USE_LOCALE_EMULATOR;
            } else if (radioButtonAppLocale.Checked) {
                return NonJapaneseLocaleWatDo.USE_APPLOCALE;
            } else {
                return NonJapaneseLocaleWatDo.RUN_ANYWAY;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start("http://fuwanovel.org/faq/setting-windows-to-japanese-locale");
        }
    }
}
