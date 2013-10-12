using ChiitransLite.misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ChiitransLite.translation.atlas {
    class Atlas : WithInitialization {
        private static Atlas _instance = new Atlas();

        public static Atlas instance {
            get {
                _instance.tryWaitForInitialization();
                return _instance;
            }
        }

        public bool isNotFound { get { return interop == null ? false : interop.atlasNotFound; } }

        protected Atlas() {
        }

        private AtlasInterop interop;
        private IntPtr buf1;
        private IntPtr buf2;
        private byte[] generalStr;
        private readonly Encoding encoding932 = Encoding.GetEncoding(932);

        protected override void doInitialize() {
            interop = new AtlasInterop();
            try {
                interop.initialize();
                buf1 = Marshal.AllocHGlobal(30000);
                buf2 = Marshal.AllocHGlobal(30000);
                if (interop.atlInitEngineData(0, 2, buf1, 0, buf2, 0, 0, 0, 0) != 0)
                    throw new MyException("ATLAS AtlInitEngineData failed");
                generalStr = encoding932.GetBytes("General");
                if (interop.createEngine(1, 1, 0, generalStr) != 1)
                    throw new MyException("ATLAS CreateEngine failed");
            } catch {
                interop.close();
                throw;
            }
        }

        public string translate(string text) {
            if (state != State.WORKING) {
                return null;
            }
            if (string.IsNullOrWhiteSpace(text)) {
                return text;
            }
            lock (this) {
                IntPtr outp;
                IntPtr tmp;
                uint size;
                byte[] inp = encoding932.GetBytes(text);
                interop.translatePair(inp, out outp, out tmp, out size);
                string result;
                if (outp != IntPtr.Zero) {
                    byte[] data = new byte[size];
                    Marshal.Copy(outp, data, 0, (int)size);
                    result = encoding932.GetString(data);
                    interop.freeAtlasData(outp, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                } else {
                    result = null;
                }
                return result;
            }
        }

        public void close() {
            if (state == State.WORKING) {
                interop.close();
            }
        }

        protected override void onInitializationError(Exception ex) {
        }
    }
}
