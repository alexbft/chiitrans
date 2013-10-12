using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.texthook;
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
    public partial class UserHookForm : Form {
        private static UserHookForm _instance = null;

        public static UserHookForm instance {
            get {
                if (_instance == null) {
                    _instance = new UserHookForm();
                }
                return _instance;
            }
        }

        public UserHookForm() {
            InitializeComponent();
            linkLabel1.Links.Add(0, linkLabel1.Text.Length, "http://agthdb.bakastyle.com/");
            FormUtil.restoreLocation(this);
            refreshHookList();
        }

        private void refreshHookList() {
            IEnumerable<UserHook> hooks = Settings.session.getHookList();
            listBox1.Items.Clear();
            listBox1.BeginUpdate();
            try {
                foreach (UserHook hook in hooks) {
                    listBox1.Items.Add(hook);
                }
            } finally {
                listBox1.EndUpdate();
            }
        }

        private void UserHookForm_Move(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void UserHookForm_Resize(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void buttonOk_Click(object sender, EventArgs e) {
            Close();
        }

        private void UserHookForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void buttonAdd_Click(object sender, EventArgs e) {
            string code = textBox1.Text;
            code = code.Trim().ToUpper();
            if (code == "") {
                return;
            }
            UserHook userHook = UserHook.fromCode(code);
            if (userHook == null) {
                Utils.error("Cannot parse hook code. Please verify code syntax and try again.");
                return;
            }
            if (!TextHook.instance.addUserHook(userHook)) {
                Utils.error("Failed to install hook.");
                return;
            }
            listBox1.Items.Add(userHook);
            textBox1.Text = "";
        }

        private void buttonRemove_Click(object sender, EventArgs e) {
            if (listBox1.Items.Count == 0) return;
            List<UserHook> selected = listBox1.SelectedItems.OfType<UserHook>().ToList();
            if (selected.Count == 0) {
                if (!Utils.confirm("Remove all hooks?")) {
                    return;
                }
                selected = Settings.session.getHookList().ToList();
            }
            foreach (UserHook hook in selected) {
                TextHook.instance.removeUserHook(hook);
            }
            refreshHookList();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start(e.Link.LinkData.ToString());
        }
    }
}
