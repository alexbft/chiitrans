using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChiitransLite.texthook {
    class UserHook {
        public readonly string code;
        public HookParam hookParam;
        public int addr { get { return hookParam.addr; } }

        protected UserHook(string code, HookParam hookParam) {
            this.code = code;
            this.hookParam = hookParam;
        }

        public static UserHook fromCode(string code) {
            HookParam hp = new HookParam();
            if (tryParseCode(code, ref hp)) {
                return new UserHook(code, hp);
            } else {
                return null;
            }
        }

        private static bool tryParseCode(string code, ref HookParam hp) {
            if (string.IsNullOrWhiteSpace(code)) {
                return false;
            }
            code = code.Trim().ToUpper();
            if (code.StartsWith("/H")) {
                code = code.Substring(2);
            }
            if (code.StartsWith("X")) {
                return false; // hardware breakpoints? NO THANK YOU
            }
            StringReader sr = new StringReader(code);
            int cmd = sr.Read();
            switch (cmd) {
                case 'A':
                    hp.length_offset = 1;
                    hp.type |= HookParamType.BIG_ENDIAN; // agth has mixed up descriptions for [A] and [B]
                    break;
                case 'B':
                    hp.length_offset = 1;
                    break;
                case 'W':
                    hp.length_offset = 1;
                    hp.type |= HookParamType.USING_UNICODE;
                    break;
                case 'S':
                    hp.type |= HookParamType.USING_STRING;
                    break;
                case 'Q':
                    hp.type |= HookParamType.USING_STRING | HookParamType.USING_UNICODE;
                    break;
                case 'H':
                    hp.type |= HookParamType.PRINT_DWORD;
                    break;
                case 'L':
                    hp.type |= HookParamType.STRING_LAST_CHAR | HookParamType.USING_UNICODE;
                    break;
                case 'E':
                    hp.type |= HookParamType.STRING_LAST_CHAR;
                    break;
                default:
                    return false;
            }
            if (sr.Peek() == 'N') {
                hp.type |= HookParamType.NO_CONTEXT;
                sr.Read();
            }
            int hex;
            if (tryReadHex(sr, true, out hex)) {
                hp.off = hex;
                if (sr.Peek() == '*') {
                    sr.Read();
                    if (!tryReadHex(sr, false, out hex)) {
                        return false;
                    }
                    hp.type |= HookParamType.DATA_INDIRECT;
                    hp.ind = hex;
                }
                if (sr.Peek() == ':') {
                    sr.Read();
                    if (!tryReadHex(sr, true, out hex)) {
                        return false;
                    }
                    hp.type |= HookParamType.USING_SPLIT;
                    hp.split = hex;
                    if (sr.Peek() == '*') {
                        sr.Read();
                        if (!tryReadHex(sr, false, out hex)) {
                            return false;
                        }
                        hp.type |= HookParamType.SPLIT_INDIRECT;
                        hp.split_ind = hex;
                    }
                }
            }
            if (sr.Read() != '@') {
                return false;
            }
            if (!tryReadHex(sr, false, out hp.addr)) {
                return false;
            }
            if (sr.Peek() == ':') {
                sr.Read();
                string module = readUntil(sr, ':');
                hp.type |= HookParamType.MODULE_OFFSET;
                hp.module = calculateHash(module);
                if (sr.Peek() == ':') {
                    sr.Read();
                    string func = sr.ReadToEnd();
                    hp.type |= HookParamType.FUNCTION_OFFSET;
                    hp.function = calculateHash(func);
                }
            } else if (sr.Peek() == '!') {
                sr.Read();
                if (!tryReadHex(sr, false, out hp.module)) {
                    return false;
                }
                hp.type |= HookParamType.MODULE_OFFSET;
                if (sr.Peek() == '!') {
                    sr.Read();
                    if (!tryReadHex(sr, false, out hp.function)) {
                        return false;
                    }
                    hp.type |= HookParamType.FUNCTION_OFFSET;
                }
            }
            if (sr.Peek() != -1) {
                return false;
            }
            return true;
        }

        private static string readUntil(StringReader sr, char delimiter) {
            StringBuilder sb = new StringBuilder();
            while (true) {
                int ch = sr.Peek();
                if (ch == -1 || ch == delimiter) {
                    break;
                }
                sb.Append((char)sr.Read());
            }
            return sb.ToString();
        }

        private static bool tryReadHex(StringReader sr, bool useCorrectionForNegative, out int result) {
            string validHex = "0123456789ABCDEF";
            StringBuilder sb = new StringBuilder();
            bool negative = false;
            if (sr.Peek() == '-') {
                negative = true;
                sr.Read();
            }
            while (true) {
                int next = sr.Peek();
                if (next == -1 || validHex.IndexOf((char)next) == -1) {
                    break;
                } else {
                    sb.Append((char)sr.Read());
                }
            }
            if (sb.Length == 0) {
                result = 0;
                return false;
            }
            int delim = sr.Peek();
            if (!(delim == -1 || ":*@!".IndexOf((char)delim) != -1)) {
                result = 0;
                return false;
            }
            int res = Convert.ToInt32(sb.ToString(), 16);
            if (negative) {
                res = -res;
                if (useCorrectionForNegative) {
                    res -= 4;
                }
            }
            result = res;
            return true;
        }

        private static uint rotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }

        private static int calculateHash(string s) {
            s = s.ToLower();
            uint hash = 0;
            foreach (char ch in s) {
                hash = rotateRight(hash, 7) + ch;
            }
            return (int)hash;
        }

        public override string ToString() {
            return code;
        }

        internal string getName() {
            return (hookParam.type.HasFlag(HookParamType.USING_UNICODE)) ? "UserHookW" : "UserHook";
        }
    }
}
