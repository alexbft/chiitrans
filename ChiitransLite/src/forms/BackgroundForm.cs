using ChiitransLite.misc;
using ChiitransLite.settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class BackgroundForm : Form {
        private Form mainForm;
        
        public BackgroundForm() {
            InitializeComponent();
            Opacity = Settings.app.transparencyLevel * 0.01;
        }

        public void setMainForm(Form form) {
            mainForm = form;
        }

        private void BackgroundForm_Activated(object sender, EventArgs e) {
            mainForm.Activate();
        }

        private void BackgroundForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
            }
        }

        internal void updatePos() {
            Rectangle bounds = mainForm.DesktopBounds;
            Winapi.SetWindowPos(Handle, mainForm.Handle, bounds.Left, bounds.Top, bounds.Width, bounds.Height, 16);
            WindowState = mainForm.WindowState;
        }
    }
}
