using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ChiitransLite.misc;
using System.Windows.Forms;
using ChiitransLite.texthook.ext;
using ChiitransLite.settings;
using System.Threading;

namespace ChiitransLite.texthook {
    class TextHook {
        public const int OK = 0;
        public const int ERROR_ALREADY_RUNNING = 1001;
        
        private static bool isInitialized = false;

        public static int init() {
            lock (typeof(TextHook)) {
                if (isInitialized) {
                    return TextHook.OK;
                } else {
                    int res = TextHookInterop.TextHookInit();
                    isInitialized = res == TextHook.OK;
                    return res;
                }
            }
        }
        private class Singleton {
            public static readonly TextHook _instance = new TextHook();
        }

        private readonly TextHookInterop.OnCreateThreadFunc handleCreateThreadDelegate;
        private readonly TextHookInterop.OnRemoveThreadFunc handleRemoveThreadDelegate;
        private readonly TextHookInterop.CallbackFunc handleConnectDelegate;
        private readonly TextHookInterop.CallbackFunc handleDisconnectDelegate;
        private readonly TextHookInterop.OnInputFunc handleInputDelegate;

        private TextHook() {
            try {
                handleCreateThreadDelegate = new TextHookInterop.OnCreateThreadFunc(handleCreateThread);
                handleRemoveThreadDelegate = new TextHookInterop.OnRemoveThreadFunc(handleRemoveThread);
                handleConnectDelegate = new TextHookInterop.CallbackFunc(handleConnect);
                handleDisconnectDelegate = new TextHookInterop.CallbackFunc(handleDisconnect);
                handleInputDelegate = new TextHookInterop.OnInputFunc(handleInput);
                TextHookInterop.TextHookOnConnect(handleConnectDelegate);
                TextHookInterop.TextHookOnCreateThread(handleCreateThreadDelegate);
                TextHookInterop.TextHookOnRemoveThread(handleRemoveThreadDelegate);
                TextHookInterop.TextHookOnDisconnect(handleDisconnectDelegate);
                TextHookInterop.TextHookOnInput(handleInputDelegate);
            } catch (Exception ex) {
                Logger.logException(ex);
            }
        }

        public static TextHook instance {
            get {
                return Singleton._instance;
            }
        }

        private ContextFactory factory = new DefaultContextFactory();
        private IDictionary<int, TextHookContext> contexts = new ConcurrentDictionary<int, TextHookContext>();
        public String currentProcessTitle { get; private set; }
        private volatile bool isConnected = false;

        private int handleCreateThread(int id, string name, int hook, int context, int subcontext) {
            contexts[id] = factory.create(id, name, hook, context, subcontext);
            Task.Factory.StartNew(() => {
                if (onNewContext != null) {
                    onNewContext(contexts[id]);
                }
            });
            return 0;
        }

        private int handleRemoveThread(int id) {
            TextHookContext ctx;
            if (contexts.TryGetValue(id, out ctx)) {
                Task.Factory.StartNew(() => {
                    if (onRemoveContext != null) {
                        onRemoveContext(ctx);
                    }
                    contexts.Remove(id);
                });
            }
            return 0;
        }

        private int handleConnect() {
            installHooks();
            return 0;
        }
        
        private int handleDisconnect() {
            isConnected = false;
            contexts.Clear();
            Task.Factory.StartNew(() => {
                if (onDisconnect != null) {
                    onDisconnect();
                }
            });
            return 0;
        }

        private int handleInput(int id, IntPtr data, int len, int isNewline) {
            TextHookContext ctx;
            if (contexts.TryGetValue(id, out ctx)) {
                ctx.handleInput(data, len, isNewline != 0);
            }
            return 0;
        }

        public bool connect(int pid) {
            if (!isInitialized) {
                return false;
            } else {
                TextHookInterop.TextHookConnect(pid); // cannot determine success :(
                contexts.Clear();
                isConnected = true;
                return true;
            }
        }

        public void connect(int pid, string exeName) {
            int err = init();
            if (err != TextHook.OK) {
                if (err == TextHook.ERROR_ALREADY_RUNNING) {
                    throw new MyException("Cannot initialize TextHook: already running!");
                } else {
                    throw new MyException("Failed to initialize TextHook, error code = " + err);
                }
            } else {
                if (pid == 0) {
                    pid = Utils.startProcess(exeName);
                    if (pid == 0) {
                        throw new MyException("Failed to start process.");
                    }
                }
                Process currentProcess;
                try {
                    currentProcess = Process.GetProcessById(pid);
                } catch {
                    throw new MyException("Failed to connect to process.");
                }
                if (currentProcess.HasExited && !string.IsNullOrEmpty(exeName)) {
                    connect(0, exeName);
                    return;
                }
                currentProcess.WaitForInputIdle(1000);
                currentProcessTitle = currentProcess.MainWindowTitle;
                Properties.Settings.Default.lastRun = exeName;
                Settings.setCurrentSession(exeName);
                factory.onConnected();
                connect(pid);
            }
        }

        public void setContextFactory(ContextFactory f) {
            this.factory = f;
        }

        public TextHookContext getContext(int ctxId) {
            TextHookContext res;
            if (contexts.TryGetValue(ctxId, out res)) {
                return res;
            } else {
                return null;
            }
        }
        
        public delegate void TextHookContextHandler(TextHookContext ctx);
        public event TextHookContextHandler onNewContext;
        public event TextHookContextHandler onRemoveContext;
        public event Action onDisconnect;

        internal IEnumerable<TextHookContext> getContexts() {
            return contexts.Values;
        }

        internal bool addUserHook(UserHook userHook) {
            if (!isConnected) {
                return false;
            }
            if (Settings.session.isHookAlreadyInstalled(userHook)) {
                return false;
            }
            bool ok = TextHookInterop.TextHookAddHook(ref userHook.hookParam, userHook.getName()) == OK;
            if (ok) {
                Settings.session.addUserHook(userHook);
            }
            return ok;
        }

        internal bool removeUserHook(UserHook hook) {
            if (!isConnected) {
                return false;
            }
            if (!Settings.session.removeUserHook(hook)) {
                return false;
            }
            bool ok = true;
            Thread removingThread = new Thread(new ThreadStart(() => {
                ok = TextHookInterop.TextHookRemoveHook(hook.addr) == OK;
            }));
            removingThread.IsBackground = true;
            removingThread.Start();
            if (removingThread.Join(1000)) {
                return ok;
            } else {
                removingThread.Abort();
                Logger.log("Freeze trying to remove hook: " + hook.code);
                return true;
            }
        }

        private void installHooks() {
            foreach (UserHook h in Settings.session.getHookList()) {
                TextHookInterop.TextHookAddHook(ref h.hookParam, h.getName());
            }
        }

    }
}
