using ChiitransLite.misc;
using ChiitransLite.settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.texthook.ext {
    class MyContextFactory : ContextFactory {

        private static MyContextFactory _instance = new MyContextFactory();

        public static MyContextFactory instance { get { return _instance; } }

        protected MyContextFactory() {
        }

        public enum NewContextsBehavior {
            ALLOW,
            IGNORE,
            SWITCH_TO_NEW
        }

        private volatile NewContextsBehavior _newContextsBehavior = NewContextsBehavior.ALLOW;
        public NewContextsBehavior newContextsBehavior {
            get {
                return _newContextsBehavior;
            }
            set {
                _newContextsBehavior = value;
                Settings.session.newContextsBehavior = value;
            }
        }

        public void onConnected() {
            _newContextsBehavior = Settings.session.newContextsBehavior;
        }

        public TextHookContext create(int id, string name, int hook, int context, int subcontext) {
            bool isEnabled;
            if (!Settings.session.tryGetContextEnabled(context, subcontext, out isEnabled)) {
                isEnabled = newContextsBehavior != NewContextsBehavior.IGNORE;
            }
            return new MyContext(id, name, hook, context, subcontext, isEnabled);
        }

        public void setNewContextsBehavior(string behavior) {
            switch (behavior) {
                case "ignore":
                    newContextsBehavior = NewContextsBehavior.IGNORE;
                    break;
                case "switchto":
                    newContextsBehavior = NewContextsBehavior.SWITCH_TO_NEW;
                    break;
                default:
                    newContextsBehavior = NewContextsBehavior.ALLOW;
                    break;
            }
        }

        internal string getNewContextsBehaviorAsString() {
            switch (newContextsBehavior) {
                case NewContextsBehavior.ALLOW:
                    return "allow";
                case NewContextsBehavior.IGNORE:
                    return "ignore";
                case NewContextsBehavior.SWITCH_TO_NEW:
                    return "switchto";
                default:
                    throw new MyException("unknown value");
            }
        }
    }
}
