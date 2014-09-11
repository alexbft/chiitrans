using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChiitransLite.misc;

namespace ChiitransLiteTests {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestKataHira() {
            string kata = "どー";
            string hira = TextUtils.katakanaToHiragana(kata);
            Assert.AreEqual("どう", hira);
        }
    }
}
