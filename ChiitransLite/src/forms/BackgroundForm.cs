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

        protected override bool ShowWithoutActivation {
            get {
                return true;
            }
        }

        public BackgroundForm() {
            InitializeComponent();
            Opacity = Settings.app.transparencyLevel * 0.01;
            Utils.setWindowNoActivate(this.Handle);
        }

        public void setMainForm(Form form) {
            mainForm = form;
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

        protected override void WndProc(ref Message m) {
            const uint WM_NCHITTEST = 0x84;

            const int HTTRANSPARENT = -1;
            //const int HTCLIENT = 1;
            //const int HTCAPTION = 2;
            // ... or define an enum with all the values

            if (m.Msg == WM_NCHITTEST) {
                m.Result = new IntPtr(HTTRANSPARENT);
                return;  // bail out because we've handled the message
            }

            // Otherwise, call the base class implementation for default processing.
            base.WndProc(ref m);
        }
    }
}
