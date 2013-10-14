using ChiitransLite.misc;
using ChiitransLite.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class AboutForm : Form {
        private Icon appIcon;
        private bool isBigIcon = false;

        public AboutForm() {
            InitializeComponent();
            appIcon = extractLargeIcon(Application.ExecutablePath);
            labelVersion.Text = "Version " + Application.ProductVersion.ToString();
        }

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        static extern UInt32 PrivateExtractIcons(String lpszFile, int nIconIndex, int cxIcon, int cyIcon, IntPtr[] phicon, IntPtr[] piconid, UInt32 nIcons, UInt32 flags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyIcon(IntPtr hIcon);

        private Icon extractLargeIcon(string filename) {
            try {
                IntPtr[] phicon = new IntPtr[] { IntPtr.Zero };
                IntPtr[] piconid = new IntPtr[] { IntPtr.Zero };

                PrivateExtractIcons(filename, 0, 256, 256, phicon, piconid, 1, 0);

                if (phicon[0] != IntPtr.Zero) {
                    isBigIcon = true;
                    return System.Drawing.Icon.FromHandle(phicon[0]);
                } else {
                    isBigIcon = false;
                    return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            } catch {
                isBigIcon = false;
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e) {
            e.Graphics.DrawIcon(appIcon, new Rectangle(0, 0, 96, 96));
        }

        private void AboutForm_FormClosed(object sender, FormClosedEventArgs e) {
            if (isBigIcon) {
                DestroyIcon(appIcon.Handle);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start("http://alexbft.github.io/chiitrans/");
        }

    }
}
