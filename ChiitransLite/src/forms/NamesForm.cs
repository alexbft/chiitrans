using ChiitransLite.misc;
using ChiitransLite.settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class NamesForm : Form {
        private SessionSettings settings;
        private static readonly Regex lineRegex = new Regex(@"(.*?)\s*=\s*(.*?)(?:\s*\((.*?)\))?\s*$");

        public NamesForm() {
            InitializeComponent();
            FormUtil.restoreLocation(this);
        }

        private void NamesForm_Move(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void NamesForm_Resize(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void NamesForm_Load(object sender, EventArgs e) {
            settings = Settings.session;
            Text = "User Defined Names (" + settings.key + ")";
            textBox1.Text = "";
            textBox1.AppendText(toTextFormat(settings.serializeNames()));
            buttonApply.Enabled = false;
        }

        private string toTextFormat(IEnumerable<object> names) {
            return
                "# The list of user defined names.\r\n" +
                "# Format: <name> = <translation> [(<type>)]\r\n" +
                "#\r\n" + string.Join("\r\n", (from name in names select toTextLine(name)));
        }

        private string toTextLine(dynamic name) {
            string typeStr;
            if (name.type != null) {
                typeStr = " (" + name.type + ")";
            } else {
                typeStr = "";
            }
            return name.key + " = " + name.sense + typeStr;
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            Close();
        }

        private void buttonOk_Click(object sender, EventArgs e) {
            if (Save()) {
                Close();
            }
        }

        private bool Save() {
            List<object> res = new List<object>();
            int lineNo = 0;
            foreach (string line in textBox1.Text.Split(new string[] {"\r\n"}, StringSplitOptions.None)) {
                lineNo += 1;
                if (line == "" || line.StartsWith("#")) {
                    continue;
                }
                Match m = lineRegex.Match(line);
                if (m.Success) {
                    res.Add(new {
                        key = m.Groups[1].Value,
                        sense = m.Groups[2].Value,
                        type = m.Groups[3].Success ? m.Groups[3].Value : null
                    });
                } else {
                    MessageBox.Show(string.Format("Invalid line format in line {0}: {1}", lineNo, line));
                    return false;
                }
            }
            settings.loadNames(res);
            return true;
        }

        private void button1_Click(object sender, EventArgs e) {
            if (Save()) {
                buttonApply.Enabled = false;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            buttonApply.Enabled = true;
        }

    }
}
