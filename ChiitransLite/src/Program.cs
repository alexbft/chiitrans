using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using ChiitransLite.forms;
using ChiitransLite.translation.atlas;
using ChiitransLite.translation.edict;
using ChiitransLite.misc;
using System.Diagnostics;
using System.Threading;

namespace ChiitransLite {
    static class Program {

        public static string[] arguments;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] cmd) {
            arguments = cmd;
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                Exception ex = (Exception)args.ExceptionObject;
                Logger.logException(ex);
            };
            try {
                Logger.log("Chiitrans Lite " + Application.ProductVersion.ToString());
            } catch {
            }
            Task.Factory.StartNew(() => {
                Atlas.instance.initialize();
            });
            Task.Factory.StartNew(() => {
                Edict.instance.initialize();
            });
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ApplicationExit += new EventHandler((_1, _2) => {
                try {
                    settings.Settings.app.save();
                    Atlas.instance.close();
                    texthook.TextHookInterop.TextHookCleanup();
                } catch (Exception e) {
                    Logger.logException(e);
                }
                
                if (texthook.TextHook.instance.isCompat) {
                    Thread.Sleep(2500);
                    // goodbye, cruel ITH :(
                    Process.GetCurrentProcess().Kill();
                }
            });
            Application.Run(new MainForm());
        }
    }
}
