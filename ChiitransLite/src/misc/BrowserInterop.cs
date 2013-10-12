using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ChiitransLite.misc {
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class BrowserInterop {
        private Type userMethodsType;
        private Object userMethods;
        private JavaScriptSerializer js = new JavaScriptSerializer();
        private WebBrowser browser;
        
        public BrowserInterop(WebBrowser browser, Object userMethods) {
            this.browser = browser;
            this.userMethods = userMethods;
            this.userMethodsType = userMethods.GetType();
        }

        public string getMethods() {
            return js.Serialize(userMethods.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Select((mi) => mi.Name).Except(new string[] {"Equals", "GetHashCode", "ToString", "GetType"}).ToArray());
        }

        public string query(string methodName, string args, long queryId) {
            object[] argsArray;
            if (string.IsNullOrEmpty(args)) {
                argsArray = new object[] { };
            } else {
                argsArray = js.Deserialize<object[]>(args);
            }
            bool isInline = queryId == 0;
            string resInline = null;
            var t = new Task(() => {
                string resJson;
                try {
                    object res = userMethodsType.InvokeMember(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, userMethods, argsArray);
                    resJson = js.Serialize(res);
                } catch (Exception e) {
                    while (e.InnerException != null) {
                        e = e.InnerException;
                    }
                    Logger.logException(e);
                    resJson = "null";
                }
                if (isInline) {
                    resInline = resJson;
                } else {
                    browser.Invoke(new Action(() =>
                        browser.Document.InvokeScript("hostCallback", new object[] { (double)queryId, resJson })
                    ));
                }
            });
            if (isInline) {
                t.RunSynchronously();
            } else {
                t.Start();
            }
            return resInline;
        }
    }
}
