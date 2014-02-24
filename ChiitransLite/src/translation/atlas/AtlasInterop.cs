using ChiitransLite.misc;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ChiitransLite.translation.atlas {
    class AtlasInterop {

        private IntPtr atlecont;
        public string installPath { get; private set; }

        public AtlasInterop() {
        }

        public void initialize() {
            installPath = getInstallPath();
            atlecont = loadLibrary("AtleCont.dll");
            if (atlecont == IntPtr.Zero) {
                throw new MyException("Failed to load AtleCont.dll");
            }
            initMethods();
        }

        public void close() {
            try {
                if (destroyEngine != null) {
                    destroyEngine();
                }
                if (atlecont != null && atlecont != IntPtr.Zero) {
                    Winapi.FreeLibrary(atlecont);
                }
            } catch { }
        }

        private string getInstallPath() {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Fujitsu\ATLAS\V14.0\SYSTEM INFO");
            if (key == null) {
                atlasNotFound = true;
                throw new MyException("Atlas not found");
            }
            return (string)key.GetValue("SYSTEM DIR");
        }

        private IntPtr loadLibrary(string name) {
            string path = Path.Combine(installPath, name);
            return Winapi.LoadLibraryEx(path, IntPtr.Zero, Winapi.LOAD_WITH_ALTERED_SEARCH_PATH);
        }

        private Delegate loadFunc(IntPtr module, string name, Type T) {
            IntPtr addr = Winapi.GetProcAddress(module, name);
            if (addr != IntPtr.Zero)
                return Marshal.GetDelegateForFunctionPointer(addr, T);
            else
                throw new MyException("Failed to find method " + name + "!");
        }

        private void initMethods() {
            createEngine = (CreateEngineType)loadFunc(atlecont, "CreateEngine", typeof(CreateEngineType));
            destroyEngine = (DestroyEngineType)loadFunc(atlecont, "DestroyEngine", typeof(DestroyEngineType));
            translatePair = (TranslatePairType)loadFunc(atlecont, "TranslatePair", typeof(TranslatePairType));
            freeAtlasData = (FreeAtlasDataType)loadFunc(atlecont, "FreeAtlasData", typeof(FreeAtlasDataType));
            atlInitEngineData = (AtlInitEngineDataType)loadFunc(atlecont, "AtlInitEngineData", typeof(AtlInitEngineDataType));
        }

        public delegate int CreateEngineType(int x, int dir, int x3, byte[] x4);
        public CreateEngineType createEngine;

        public delegate int DestroyEngineType();
        public DestroyEngineType destroyEngine;

        public delegate int TranslatePairType(byte[] inp, out IntPtr outp, out IntPtr dunno, out uint maybeSize);
        public TranslatePairType translatePair;

        public delegate int AtlInitEngineDataType(int x1, int x2, IntPtr x3, int x4, IntPtr x5, int x6, int x7, int x8, int x9);
        public AtlInitEngineDataType atlInitEngineData;

        //private delegate int SetTransStateType(int dunno);
        //private static SetTransStateType SetTransState;

        public delegate int FreeAtlasDataType(IntPtr mem, IntPtr noSureHowManyArgs, IntPtr x3, IntPtr x4);
        public FreeAtlasDataType freeAtlasData;
        
        public bool atlasNotFound = false;

    }
}
