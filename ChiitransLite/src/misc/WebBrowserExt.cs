using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChiitransLite.misc {
    static class WebBrowserExt {
        public static object callScript(this WebBrowser browser, String fnName, params object[] args) {
            if (!browser.IsDisposed) {
                if (browser.InvokeRequired) {
                    return browser.Invoke(new Func<object>(() => browser.Document.InvokeScript(fnName, args)));
                } else {
                    return browser.Document.InvokeScript(fnName, args);
                }
            } else {
                return null;
            }
        }
    }
}
