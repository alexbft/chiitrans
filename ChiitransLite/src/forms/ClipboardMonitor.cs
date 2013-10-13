using ChiitransLite.misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    // Must inherit Control, not Component, in order to have Handle
    [DefaultEvent("ClipboardChanged")]
    public partial class ClipboardMonitor : Control {
        IntPtr nextClipboardViewer;

        public ClipboardMonitor() {
            this.BackColor = Color.Red;
            this.Visible = false;

            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
        }

        /// <summary>
        /// Clipboard contents changed.
        /// </summary>
        public event EventHandler ClipboardChanged;

        protected override void Dispose(bool disposing) {
            if (disposing) {
                ChangeClipboardChain(this.Handle, nextClipboardViewer);
            }
            base.Dispose(disposing);
        }

        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        protected override void WndProc(ref System.Windows.Forms.Message m) {
            // defined in winuser.h
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg) {
                case WM_DRAWCLIPBOARD:
                    OnClipboardChanged();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                        nextClipboardViewer = m.LParam;
                    else
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        void OnClipboardChanged() {
            try {
                if (ClipboardChanged != null) {
                    ClipboardChanged(this, null);
                }

            } catch (Exception e) {
                // Swallow or pop-up, not sure
                // Trace.Write(e.ToString());
                Logger.logException(e);
            }
        }
    }

}
