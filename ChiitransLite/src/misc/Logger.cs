using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChiitransLite.misc {
    class Logger {
        public static void logException(Exception ex) {
            string s = ex.Message + "\r\n\r\n" + ex.StackTrace;
            log(s);
            MessageBox.Show(s, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

#if true //DEBUG
        private static StreamWriter logFile;
#endif

        internal static void log(string value) {
#if true //DEBUG
            lock (typeof(Logger)) {
                if (logFile == null) {
                    logFile = File.AppendText(Path.Combine(Utils.getRootPath(), "log.txt"));
                }
                logFile.WriteLine("[{0}] {1}", DateTime.Now.ToString(), value);
                logFile.Flush();
            }
#endif
        }

    }
}