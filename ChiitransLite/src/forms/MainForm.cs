using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using ChiitransLite.misc;
using System.Diagnostics;
using System.Threading;
using ChiitransLite.texthook;
using System.Runtime.InteropServices;
using ChiitransLite.texthook.ext;
using ChiitransLite.translation;
using ChiitransLite.translation.edict;
using ChiitransLite.settings;
using System.Globalization;
using ChiitransLite.translation.atlas;

namespace ChiitransLite.forms {
    public partial class MainForm : Form {

        public class InteropMethods {
            private MainForm form;

            internal InteropMethods(MainForm form) {
                this.form = form;
            }

            public object getProcesses() {
                int curPid = Process.GetCurrentProcess().Id;
                var lastRun = Properties.Settings.Default.lastRun;
                var pid = 0;
                var res = Process.GetProcesses().SelectMany((p) => {
                    String name;
                    try {
                        name = p.MainModule.FileName;
                    } catch {
                        return Enumerable.Empty<object>();
                    }
                    if (p.Id == curPid || string.IsNullOrEmpty(p.MainWindowTitle)) {
                        return Enumerable.Empty<object>();
                    } else {
                        if (name == lastRun) {
                            pid = p.Id;
                        }
                        return Enumerable.Repeat(new {
                            pid = p.Id,
                            name = name
                        }, 1);
                    }
                }).ToList();
                return new {
                    procs = res,
                    defaultPid = pid,
                    defaultName = lastRun
                };
            }

            public void selectWindowClick(bool isSelectWindow) {
                form.startSelectWindow(isSelectWindow);
            }

            public void browseClick() {
                form.showBrowseDialog();
            }

            public void connectClick(int pid, string exeName) {
                try {
                    TextHook.instance.connect(pid, exeName);
                    form.connectSuccess();
                } catch (Exception ex) {
                    form.connectError(ex.Message);
                }
            }

            public void setContextEnabled(int ctxId, bool isEnabled) {
                var ctx = (MyContext)TextHook.instance.getContext(ctxId);
                if (ctx != null) {
                    ctx.enabled = isEnabled;
                }
            }

            public void setContextEnabledOnly(int ctxId) {
                foreach (MyContext ctx in TextHook.instance.getContexts().Cast<MyContext>()) {
                    if (ctx.id == ctxId) {
                        ctx.enabled = true;
                    } else {
                        ctx.enabled = false;
                    }
                }
            }

            public void showTranslationForm() {
                form.showTranslationForm();
            }

            public void setNewContextsBehavior(string b) {
                MyContextFactory.instance.setNewContextsBehavior(b);
            }

            public void showLog(int ctxId) {
                form.showLog(ctxId);
            }

            public void translate(string text) {
                form.translate(text);
            }

            public object getContext(int ctxId) {
                var ctx = (MyContext)TextHook.instance.getContext(ctxId);
                if (ctx != null) {
                    return new {
                        id = ctx.id,
                        name = ctx.name,
                        addr = ctx.context,
                        sub = ctx.subcontext,
                        enabled = ctx.enabled
                    };
                } else {
                    return null;
                }
            }

            public void showAbout() {
                form.Invoke(new Action(() => {
                    TranslationForm.instance.SuspendTopMost(() => {
                        new AboutForm().ShowDialog();
                    });
                }));
            }

            public void showOptions() {
                form.showOptions();
            }

        }

        private static MainForm _instance;
        public static MainForm instance { get { return _instance; } }

        public MainForm() {
            _instance = this;
            InitializeComponent();
            FormUtil.restoreLocation(this);
            webBrowser1.ObjectForScripting = new BrowserInterop(webBrowser1, new InteropMethods(this));
            webBrowser1.Url = Utils.getUriForBrowser("index.html");
            TextHook.instance.setContextFactory(MyContextFactory.instance);
            /*Logger.onLog += (text) => {
                webBrowser1.callScript("log", "DEBUG: " + text);
            };*/
        }

        private bool isSelectWindow = false;

        private void startSelectWindow(bool isSelectWindow) {
            this.isSelectWindow = isSelectWindow;
        }

        private void MainForm_Deactivate(object sender, EventArgs e) {
            if (isSelectWindow) {
                isSelectWindow = false;
                webBrowser1.callScript("selectWindowEnd");
                Task.Factory.StartNew(() => {
                    IntPtr newWindow = Winapi.GetForegroundWindow();
                    if (newWindow != IntPtr.Zero) {
                        uint pid;
                        Winapi.GetWindowThreadProcessId(newWindow, out pid);
                        setDefaultProcess((int)pid);
                    } else {
                        Thread.Sleep(100);
                        newWindow = Winapi.GetForegroundWindow();
                        if (newWindow != IntPtr.Zero) {
                            uint pid;
                            Winapi.GetWindowThreadProcessId(newWindow, out pid);
                            setDefaultProcess((int)pid);
                        } else {
                            Thread.Sleep(500);
                            newWindow = Winapi.GetForegroundWindow();
                            if (newWindow != IntPtr.Zero) {
                                uint pid;
                                Winapi.GetWindowThreadProcessId(newWindow, out pid);
                                setDefaultProcess((int)pid);
                            } else {
                            }
                        }
                    }
                });
            }
        }

        private void setDefaultProcess(int pid, string name = null) {
            try {
                if (name == null)
                    name = Process.GetProcessById(pid).MainModule.FileName;
                webBrowser1.callScript("setDefaultProcess", pid, name);
            } catch {
            }
        }

        private void showBrowseDialog() {
            TranslationForm.instance.SuspendTopMost(() => {
                if (openExeFile.ShowDialog() == DialogResult.OK) {
                    setDefaultProcess(0, openExeFile.FileName);
                }
            });
        }

        private void connectError(string errMsg) {
            webBrowser1.callScript("connectError", errMsg);
        }

        private void connectSuccess() {
            webBrowser1.callScript("connectSuccess", MyContextFactory.instance.getNewContextsBehaviorAsString());
            TextHook.instance.onNewContext += (ctx) => {
                webBrowser1.callScript("newContext", ctx.id, ctx.name, ctx.context, ctx.subcontext, (ctx as MyContext).enabled);
                ctx.onSentence += ctx_onSentence;
                List<int> disabledContexts = MyContextFactory.instance.disableContextsIfNeeded(TextHook.instance, ctx);
                if (disabledContexts != null && disabledContexts.Count > 0) {
                    webBrowser1.callScript("disableContexts", Utils.toJson(disabledContexts));
                }
            };
            TextHook.instance.onDisconnect += () => {
                webBrowser1.callScript("disconnect");
                TranslationForm.instance.Close();
            };
            Invoke(new Action(() => {
                TranslationForm.instance.setCaption(TextHook.instance.currentProcessTitle + " - Chiitrans Lite");
                showTranslationForm();
            }));
        }

        private void ctx_onSentence(TextHookContext sender, string text) {
            var ctx = (MyContext) sender;
            if (ctx.enabled) {
                TranslationService.instance.update(text);
            }
            webBrowser1.callScript("newSentence", sender.id, text);
        }

        private void showTranslationForm() {
            // a nasty bug with disappearing form workaround
            if (TranslationForm.instance.Visible && Settings.app.transparentMode && TranslationForm.instance.WindowState != FormWindowState.Minimized) {
                TranslationForm.instance.TransparencyKey = Color.Empty;
                TranslationForm.instance.setTransparentMode(true, false);
            }
            TranslationForm.instance.Show();
            if (TranslationForm.instance.WindowState == FormWindowState.Minimized) {
                TranslationForm.instance.WindowState = FormWindowState.Normal;
            }
            TranslationForm.instance.Activate();
        }

        private void MainForm_Move(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        private void MainForm_Resize(object sender, EventArgs e) {
            FormUtil.saveLocation(this);
        }

        internal void showLog(int ctxId) {
            var ctx = (MyContext)TextHook.instance.getContext(ctxId);
            if (ctx != null) {
                ContextLogForm form = ContextLogForm.getForContext(ctx, this);
                form.Show();
                form.Activate();
            }
        }

        private void MainForm_Shown(object sender, EventArgs e) {
            var ieVer = webBrowser1.Version.Major;
            string mm = webBrowser1.Version.Major + "." + webBrowser1.Version.Minor;
            if (ieVer < 8) {
                MessageBox.Show("You are using an outdated version of Internet Explorer: " + mm + "\r\n\r\n" +
                    "Chiitrans Lite requires Internet Explorer 8 or later to be installed. Please upgrade.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            } else {
                if (Atlas.instance.isNotFound && Settings.app.atlasAsk) {
                    new AtlasNotFoundForm().ShowDialog();
                }
                if (ieVer == 8 && Utils.isWindowsVistaOrLater() && Settings.app.ieUpgradeAsk) {
                    new UpgradeIEForm().ShowDialog();
                }
            }
        }

        internal void showOptions() {
            OptionsForm.instance.updateAndShow();
        }

        internal void translate(string text) {
            TranslationService.instance.update(TextHookContext.cleanText(text), false);
        }
    }
}
