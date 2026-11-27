using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    [TestClass]
    public class CommTriggerHelperTests
    {
        private static string[] NewRow()
        {
            return new[] { "P2", "0", "true", "F2", "无", "无", "相等", "", "" };
        }

        [TestMethod]
        public void Range_兼容未拆分的旧值()
        {
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("1", "范围", "1~2", ""));
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("2", "范围", "1~2", ""));
            Assert.IsFalse(CommTriggerHelper.CheckTriggerCondition("3", "范围", "1~2", ""));
        }

        [TestMethod]
        public void Range_全角波浪线也应拆分()
        {
            string[] row = NewRow();
            CommTriggerHelper.ParseTrigValInput("范围", "1～2", row);
            Assert.AreEqual("1", row[7]);
            Assert.AreEqual("2", row[8]);
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("2", "范围", row[7], row[8]));
        }

        [TestMethod]
        public void Range_先填值再切模式_重新解析后应触发()
        {
            string[] row = NewRow();
            CommTriggerHelper.ParseTrigValInput("相等", "1~2", row);
            Assert.AreEqual("1~2", row[7]);

            CommTriggerHelper.ParseTrigValInput("范围", "1~2", row);
            Assert.AreEqual("1", row[7]);
            Assert.AreEqual("2", row[8]);
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("1", "范围", row[7], row[8]));
        }

        [TestMethod]
        public void Format_模式与值应去NUL和空白()
        {
            Assert.AreEqual("1~2", CommTriggerHelper.FormatTrigValDisplay(" 范围\0", " 1\0", " 2 "));
            Assert.AreEqual("1", CommTriggerHelper.FormatTrigValDisplay("范围", "1", "1"));
        }

        [TestMethod]
        public void GetCamTrigParams_模式与值应去NUL和空白()
        {
            string[] row = NewRow();
            row[6] = " 范围\0";
            row[7] = " 1\0";
            row[8] = " 2 ";
            CommTriggerHelper.GetCamTrigParams(row, out string mode, out string val1, out string val2);
            Assert.AreEqual("范围", mode);
            Assert.AreEqual("1", val1);
            Assert.AreEqual("2", val2);
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("1", mode, val1, val2));
        }

        [TestMethod]
        public void Range_空值与单字符不得抛异常()
        {
            string[] row = NewRow();
            CommTriggerHelper.ParseTrigValInput("范围", "", row);
            Assert.AreEqual("", row[7]);
            Assert.AreEqual("", row[8]);
            CommTriggerHelper.ParseTrigValInput("范围", "1", row);
            Assert.AreEqual("1", row[7]);
            Assert.AreEqual("1", row[8]);
            Assert.IsFalse(CommTriggerHelper.CheckTriggerCondition("1", "范围", "", ""));
        }

        [TestMethod]
        public void Range_负数与多种分隔符应正确解析()
        {
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("-3", "范围", "-5~-1", ""));
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("2", "范围", "1-2", ""));
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("2", "范围", "1–2", ""));
        }

        [TestMethod]
        public void Range_非法值不得抛异常且不触发()
        {
            Assert.IsFalse(CommTriggerHelper.CheckTriggerCondition("1", "范围", "abc", "2"));
            Assert.IsFalse(CommTriggerHelper.CheckTriggerCondition("1", "范围", "1", "abc"));
            Assert.IsFalse(CommTriggerHelper.CheckTriggerCondition("x", "范围", "1", "2"));
        }

        [TestMethod]
        public void Equal_保持原行为()
        {
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("1", "相等", "1", ""));
            Assert.IsTrue(CommTriggerHelper.CheckTriggerCondition("01", "相等", "1", ""));
            Assert.IsFalse(CommTriggerHelper.CheckTriggerCondition("2", "相等", "1", ""));
        }
    }
}
