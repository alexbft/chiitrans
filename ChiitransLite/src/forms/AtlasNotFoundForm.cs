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
    public partial class AtlasNotFoundForm : Form {
        public AtlasNotFoundForm() {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {
            Settings.app.atlasAsk = !checkBox1.Checked;
        }
    }
}
