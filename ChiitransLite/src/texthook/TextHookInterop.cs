using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using ChiitransLite.texthook;

namespace ChiitransLite.texthook {
    class TextHookInterop {
        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookInit();

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookConnect(Int32 pid);

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookDisconnect();

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookCleanup();

        public delegate Int32 CallbackFunc();

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookOnConnect(CallbackFunc callback);

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookOnDisconnect(CallbackFunc callback);
        
        public delegate Int32 OnCreateThreadFunc(
            Int32 thread_id,
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            Int32 hook,
            Int32 context,
            Int32 subcontext
        );

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookOnCreateThread(OnCreateThreadFunc callback);

        public delegate Int32 OnRemoveThreadFunc(Int32 thread_id);
        
        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookOnRemoveThread(OnRemoveThreadFunc callback);

        public delegate Int32 OnInputFunc(Int32 thread_id, IntPtr data, Int32 len, Int32 is_newline);
        
        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookOnInput(OnInputFunc callback);

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookAddHook(ref HookParam p, [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport("ithwrapper.dll")]
        public static extern Int32 TextHookRemoveHook(Int32 addr);
    }
}
