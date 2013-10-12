using ChiitransLite.misc;
using ChiitransLite.settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    static class FormUtil {

        private class FormData {
            public int topMostSuspendLevel = 0;
        }

        internal static void restoreLocation(Form form) {
            Rectangle loc;
            if (Settings.app.getProperty("location_" + form.Name, out loc)) {
                if (Screen.GetWorkingArea(loc).Contains(loc)) {
                    form.DesktopBounds = loc;
                    return;
                }
            }
            form.StartPosition = FormStartPosition.CenterScreen;
        }

        internal static void saveLocation(Form form) {
            if (form.WindowState == FormWindowState.Normal) {
                Settings.app.setProperty("location_" + form.Name, form.DesktopBounds);
            }
        }

        internal static void SuspendTopMost(this Form form, Action action) {
            form.SuspendTopMostBegin();
            try {
                action();
            } finally {
                form.SuspendTopMostEnd();
            }
        }

        private static FormData getFormData(Form form) {
            if (form.Tag == null) {
                form.Tag = new FormData();
            }
            return form.Tag as FormData;
        }

        internal static void SuspendTopMostBegin(this Form form) {
            FormData data = getFormData(form);
            if (!form.TopMost && data.topMostSuspendLevel <= 0) {
                data.topMostSuspendLevel = 1;
            }
            data.topMostSuspendLevel += 1;
            form.TopMost = false;
        }

        internal static void SuspendTopMostEnd(this Form form) {
            FormData data = getFormData(form);
            if (data.topMostSuspendLevel <= 0) {
                throw new MyException("SuspendTopMostEnd in invalid form state");
            }
            data.topMostSuspendLevel -= 1;
            if (data.topMostSuspendLevel <= 0) {
                form.TopMost = true;
            }
        }
    
    }
}
