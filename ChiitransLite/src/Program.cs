using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using ChiitransLite.forms;
using ChiitransLite.translation.atlas;
using ChiitransLite.translation.edict;
using ChiitransLite.misc;

namespace ChiitransLite {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                Exception ex = (Exception)args.ExceptionObject;
                Logger.logException(ex);
            };
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
                    texthook.TextHookInterop.TextHookCleanup();
                    Atlas.instance.close();
                } catch (Exception e) {
                    Logger.logException(e);
                }
            });
            Application.Run(new MainForm());
        }
    }
}
