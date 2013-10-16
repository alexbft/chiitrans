using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.texthook {
    interface ContextFactory {
        TextHookContext create(int id, string name, int hook, int context, int subcontext, int status);
        void onConnected();
    }
}
