using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class UserNameForm : Form {
        public UserNameForm() {
            InitializeComponent();
        }

        internal bool Open(string key, string sense, string nameType) {
            textBoxKey.Text = key;
            textBoxSense.Text = sense;
            switch (nameType) {
                case "masc":
                    radioMale.Checked = true;
                    break;
                case "fem":
                    radioFemale.Checked = true;
                    break;
                case "surname":
                    radioSurname.Checked = true;
                    break;
                default:
                    radioOther.Checked = true;
                    break;
            }
            return ShowDialog() == DialogResult.OK;
        }

        internal string getKey() {
            return textBoxKey.Text;
        }

        internal string getSense() {
            return textBoxSense.Text;
        }

        internal string getNameType() {
            if (radioMale.Checked) return "masc";
            else if (radioFemale.Checked) return "fem";
            else if (radioSurname.Checked) return "surname";
            else return null;
        }
    }
}
