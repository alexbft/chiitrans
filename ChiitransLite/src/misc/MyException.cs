using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChiitransLite.misc {
    class MyException : Exception {
        public MyException(string message) : base(message) { }
    }
}
