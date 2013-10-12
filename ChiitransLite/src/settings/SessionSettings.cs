using ChiitransLite.misc;
using ChiitransLite.texthook;
using ChiitransLite.texthook.ext;
using ChiitransLite.translation.edict;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChiitransLite.settings {
    class SessionSettings {

        private static object classLock = new object();

        private static Dictionary<string, SessionSettings> cache = new Dictionary<string, SessionSettings>();

        private readonly string key;
        private readonly string fileName;
        public Dictionary<string, EdictMatch> userNames { get; private set; }
        private Dictionary<long, bool> contextsEnabled;
        private List<UserHook> userHooks;
        private bool isDirty;

        internal static SessionSettings get(string key) {
            lock (classLock) {
                SessionSettings res;
                if (!cache.TryGetValue(key, out res)) {
                    res = new SessionSettings(key);
                    res.tryLoad();
                    cache[key] = res;
                }
                return res;
            }
        }

        internal static SessionSettings getByExeName(string exeName) {
            return get(getKey(exeName));
        }

        internal static SessionSettings getDefault() {
            return get("default");
        }

        internal static void saveAll() {
            lock (classLock) {
                foreach (SessionSettings sett in cache.Values) {
                    sett.save();
                }
            }
        }

        private static string getKey(string exeName) {
            string exeNameUnrooted = Path.Combine(Path.GetDirectoryName(exeName), Path.GetFileName(exeName));
            string[] parts = exeNameUnrooted.Split('\\');
            if (parts.Length > 2) {
                return string.Join("!", parts[parts.Length - 2], parts[parts.Length - 1]);
            } else {
                return exeNameUnrooted.Replace('\\', '!');
            }
        }

        public SessionSettings(string key) {
            this.key = key;
            this.fileName = Path.Combine(Utils.getAppDataPath(), key + ".json");
            this.userNames = new Dictionary<string, EdictMatch>();
            this.contextsEnabled = new Dictionary<long, bool>();
            this.userHooks = new List<UserHook>();
            this.isDirty = false;
        }

        private void tryLoad() {
            try {
                if (File.Exists(fileName)) {
                    string json = File.ReadAllText(fileName);
                    var serializer = Utils.getJsonSerializer();
                    IDictionary data = serializer.DeserializeObject(json) as IDictionary;
                    userNames.Clear();
                    IList namesJson = data["names"] as IList;
                    foreach (IDictionary nameData in namesJson.Cast<IDictionary>()) {
                        string key = nameData["key"] as string;
                        string sense = nameData["sense"] as string;
                        string nameType = nameData["type"] as string;
                        addUserName(key, sense, nameType);
                    }
                    contextsEnabled.Clear();
                    IDictionary contextsJson = data["contexts"] as IDictionary;
                    foreach (DictionaryEntry kv in contextsJson) {
                        contextsEnabled[long.Parse((string)kv.Key)] = (bool)kv.Value;
                    }
                    _newContextsBehavior = (MyContextFactory.NewContextsBehavior) Enum.Parse(typeof(MyContextFactory.NewContextsBehavior), (string)data["newContexts"]);
                    userHooks.Clear();
                    IList hooksJson = data["hooks"] as IList;
                    foreach (string code in hooksJson.Cast<string>()) {
                        UserHook hook = UserHook.fromCode(code);
                        if (hook != null) {
                            userHooks.Add(hook);
                        }
                    }
                }
            } catch (Exception e) {
                Logger.logException(e);
            }
            isDirty = false;
        }

        public void addUserName(string key, string sense, string nameType) {
            EdictMatch match = new EdictMatch(key);
            EdictEntryBuilder eb = new EdictEntryBuilder();
            eb.addKanji(new DictionaryKeyBuilder(key));
            eb.addKana(new DictionaryKeyBuilder(sense));
            DictionarySense ds = new DictionarySense();
            ds.addGloss(null, sense);
            eb.addSense(ds);
            eb.addPOS("name");
            eb.nameType = nameType;
            match.addEntry(new RatedEntry { entry = eb.build(), rate = 5.0F });
            userNames[key] = match;
            this.isDirty = true;
        }

        public void removeUserName(string key) {
            userNames.Remove(key);
            this.isDirty = true;
        }

        private void save() {
            if (isDirty) {
                try {
                    object res = serialize();
                    string resJson = Utils.toJson(res);
                    File.WriteAllText(fileName, resJson);
                } catch (IOException) {
                } catch (Exception e) {
                    Logger.logException(e);
                }
                isDirty = false;
            }
        }

        private object serialize() {
            return new {
                names = (from kv in userNames select new {
                    key = kv.Key,
                    sense = kv.Value.entries[0].entry.kana[0].text,
                    type = kv.Value.entries[0].entry.nameType
                }),
                contexts = contextsEnabled.ToDictionary((kv) => kv.Key.ToString(), (kv) => kv.Value),
                newContexts = newContextsBehavior.ToString(),
                hooks = (from h in userHooks select h.code)
            };
        }

        private MyContextFactory.NewContextsBehavior _newContextsBehavior = MyContextFactory.NewContextsBehavior.ALLOW;
        public MyContextFactory.NewContextsBehavior newContextsBehavior {
            get {
                return _newContextsBehavior;
            }
            set {
                if (value != _newContextsBehavior) {
                    isDirty = true;
                    _newContextsBehavior = value;
                }
            }
        }

        public void setContextEnabled(int addr, int sub, bool enabled) {
            long key = contextKey(addr, sub);
            bool old;
            if (contextsEnabled.TryGetValue(key, out old)) {
                if (old == enabled) {
                    return;
                }
            }
            contextsEnabled[key] = enabled;
            isDirty = true;
        }

        public bool tryGetContextEnabled(int addr, int sub, out bool enabled) {
            return contextsEnabled.TryGetValue(contextKey(addr, sub), out enabled);
        }

        private long contextKey(int addr, int sub) {
            return (((long)sub) << 32) + addr;
        }

        internal IEnumerable<UserHook> getHookList() {
            return userHooks;
        }

        internal bool isHookAlreadyInstalled(UserHook userHook) {
            return userHooks.Any((h) => h.addr == userHook.addr);
        }

        internal void addUserHook(UserHook userHook) {
            userHooks.Add(userHook);
            isDirty = true;
        }

        internal bool removeUserHook(UserHook userHook) {
            bool ok = userHooks.Remove(userHook);
            if (ok) {
                isDirty = true;
            }
            return isDirty;
        }

        internal void resetUserNames() {
            userNames.Clear();
            this.isDirty = true;
        }
    }
}
