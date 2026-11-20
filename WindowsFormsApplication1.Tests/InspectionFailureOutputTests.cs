using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ch:FAILSAFE：PLC 故障输出口径（InspectionFailureOutput）行为测试。
    /// 活链接生产文件，只替掉 AppHost 日志依赖（见 TestDoubles\AppHostStub.cs）；
    /// 被测的是 ForFormat 的数值/字符串分流、RepeatForRegisters 的整块重复与 long/float 折半。
    /// 工厂现场约定：数值寄存器故障值 999，非数值 Reject。
    /// </summary>
    [TestClass]
    public class InspectionFailureOutputTests
    {
        // ---------- ForFormat：大小写/空白不敏感，数值=999 否则 Reject ----------

        [TestMethod]
        public void ForFormat_数值格式大小写空白变体_都返回999()
        {
            Assert.AreEqual("999", InspectionFailureOutput.ForFormat("int"));
            Assert.AreEqual("999", InspectionFailureOutput.ForFormat("INT"));
            Assert.AreEqual("999", InspectionFailureOutput.ForFormat("  float  "));
            Assert.AreEqual("999", InspectionFailureOutput.ForFormat("Long"));
        }

        [TestMethod]
        public void ForFormat_非数值格式_返回Reject()
        {
            Assert.AreEqual("Reject", InspectionFailureOutput.ForFormat("string"));
            Assert.AreEqual("Reject", InspectionFailureOutput.ForFormat(""));
            Assert.AreEqual("Reject", InspectionFailureOutput.ForFormat(null));
        }

        // ---------- RepeatForRegisters：整块整块重复，不覆盖/不越界 ----------

        [TestMethod]
        public void RepeatForRegisters_int_按寄存器数重复999()
        {
            Assert.AreEqual("999,999,999", InspectionFailureOutput.RepeatForRegisters("int", 3));
            Assert.AreEqual("999", InspectionFailureOutput.RepeatForRegisters("int", 1));
            Assert.AreEqual("999", InspectionFailureOutput.RepeatForRegisters("int", 0));   // <=1 退化为单值
        }

        [TestMethod]
        public void RepeatForRegisters_long和float_每值占2寄存器故折半()
        {
            Assert.AreEqual("999,999", InspectionFailureOutput.RepeatForRegisters("long", 4));
            Assert.AreEqual("999,999", InspectionFailureOutput.RepeatForRegisters("float", 5));   // 5/2=2，奇数最后一寄存器不写
            Assert.AreEqual("999", InspectionFailureOutput.RepeatForRegisters("long", 2));
        }

        [TestMethod]
        public void RepeatForRegisters_非数值格式不重复_避免逐地址写互相覆盖()
        {
            // string 分支是"每值写一个独立地址"，重复 N 次会横跨 N 个地址并越过块尾踩相邻块
            Assert.AreEqual("Reject", InspectionFailureOutput.RepeatForRegisters("string", 8));
        }

        [TestMethod]
        public void RepeatForRegisters_格式归一化与ForFormat同口径()
        {
            // 大写/空白必须走同一口径，不能因为 "INT" 精确比较失败而退化成 Reject
            Assert.AreEqual("999,999", InspectionFailureOutput.RepeatForRegisters(" INT ", 2));
        }
    }
}
