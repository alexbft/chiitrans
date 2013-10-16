using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.translation.po;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class POFilesForm : Form {
        private bool isProgressBarSet = false;

        public POFilesForm() {
            InitializeComponent();
            textBox1.Text = Settings.session.po;
            if (Settings.session.processExe != null) {
                folderBrowserDialog1.SelectedPath = Path.GetDirectoryName(Settings.session.processExe);
            }
        }

        private void buttonClear_Click(object sender, EventArgs e) {
            textBox1.Text = "";
        }

        private void buttonFile_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void buttonDirectory_Click(object sender, EventArgs e) {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void buttonOk_Click(object sender, EventArgs e) {
            string path = textBox1.Text;
            labelProgress.Text = "Loading...";
            buttonOk.Enabled = false;
            PoManager.instance.loadFrom(path, onProgress).ContinueWith((t) => {
                Invoke(new Action(() => {
                    if (t.IsFaulted) {
                        labelProgress.Text = "Failed";
                        var ex = t.Exception.InnerException;
                        if (ex is MyException) {
                            Utils.error(ex.Message);
                        } else {
                            Logger.logException(ex);
                        }
                        buttonOk.Enabled = true;
                    } else if (t.IsCompleted) {
                        Settings.session.po = path;
                        DialogResult = System.Windows.Forms.DialogResult.OK;
                    }
                }));
            });
        }

        private void onProgress(int progress, int total) {
            Invoke(new Action(() => {
                if (!isProgressBarSet) {
                    progressBar1.Maximum = total;
                }
                labelProgress.Text = string.Format("Loading ({0}/{1})...", progress, total);
                progressBar1.Value = progress;
            }));
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            if (buttonOk.Enabled) {
                DialogResult = System.Windows.Forms.DialogResult.Cancel;
            } else {
                PoManager.instance.cancel();
                labelProgress.Text = "Cancelled";
                buttonOk.Enabled = true;
            }
        }
    }
}
