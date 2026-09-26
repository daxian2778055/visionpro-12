using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// CommGridHelper 的 ini → 控件值安全读取三件套（★第33轮）：
    /// 非法回退、范围钳位、整数取整——窗体读回与三个 IniStore 统一收口到这里。
    /// 每个用例用唯一 where 键，避免 LogOnce（同键只报一次）跨用例互扰。
    /// </summary>
    [TestClass]
    public class CommGridHelperTests
    {
        private List<string> _log;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();
        }

        private void L(string m) { _log.Add(m); }

        /// <summary>唯一配置项名（LogOnce 按 where 去重，跨用例必须互不相同）。</summary>
        private string Where(string tag)
        {
            return "ut." + tag + "." + Guid.NewGuid().ToString("N");
        }

        [TestMethod]
        public void Integral_合法整数原样()
        {
            Assert.AreEqual(12m, CommGridHelper.ReadIniDecimalIntegral("12", 9, 1, 255, Where("v"), L));
            Assert.AreEqual(0, _log.Count);
        }

        [TestMethod]
        public void Integral_小数取整截断()
        {
            Assert.AreEqual(2m, CommGridHelper.ReadIniDecimalIntegral("2.9", 9, 1, 255, Where("t"), L));
            Assert.AreEqual(0, _log.Count, "范围内合法值不记日志");
        }

        [TestMethod]
        public void Integral_低于下限钳位并记日志()
        {
            string w = Where("lo");
            Assert.AreEqual(1m, CommGridHelper.ReadIniDecimalIntegral("0", 9, 1, 255, w, L));
            Assert.AreEqual(1, _log.Count);
            StringAssert.Contains(_log[0], w, "钳位日志必须带配置项名");
            StringAssert.Contains(_log[0], "1");
        }

        [TestMethod]
        public void Integral_高于上限钳位并记日志()
        {
            string w = Where("hi");
            Assert.AreEqual(255m, CommGridHelper.ReadIniDecimalIntegral("9999", 9, 1, 255, w, L));
            Assert.AreEqual(1, _log.Count);
            StringAssert.Contains(_log[0], w);
        }

        [TestMethod]
        public void Integral_非法串回退默认并记日志()
        {
            string w = Where("bad");
            Assert.AreEqual(9m, CommGridHelper.ReadIniDecimalIntegral("abc", 9, 1, 255, w, L));
            Assert.AreEqual(1, _log.Count);
            StringAssert.Contains(_log[0], w, "回退日志必须带配置项名");
        }

        [TestMethod]
        public void Integral_空串与null均回退默认()
        {
            Assert.AreEqual(9m, CommGridHelper.ReadIniDecimalIntegral(null, 9, 1, 255, Where("n1"), L));
            Assert.AreEqual(9m, CommGridHelper.ReadIniDecimalIntegral("", 9, 1, 255, Where("n2"), L));
            Assert.AreEqual(2, _log.Count);
        }

        [TestMethod]
        public void Integral_剥离历史NUL填充()
        {
            Assert.AreEqual(7m, CommGridHelper.ReadIniDecimalIntegral("7\0", 9, 1, 255, Where("nul"), L));
            Assert.AreEqual(0, _log.Count, "NUL 剥离后应正常解析");
        }

        // ★第33轮：超长数字必须"先钳后转"——修复前直接 (int) 强转会 OverflowException 全局崩溃
        [TestMethod]
        public void ReadIniInt_超长数字先钳后转不溢出()
        {
            string w = Where("ovf");
            Assert.AreEqual(255, CommGridHelper.ReadIniInt("3000000000", 9, 1, 255, w, L));
            Assert.AreEqual(1, _log.Count, "越界已钳位并记日志");
            StringAssert.Contains(_log[0], w, "日志应指向该配置项");
        }

        [TestMethod]
        public void ReadIniInt_界内原样()
        {
            Assert.AreEqual(33, CommGridHelper.ReadIniInt("33", 0, 0, 65535, Where("i"), L));
            Assert.AreEqual(0, _log.Count);
        }

        [TestMethod]
        public void ReadIniBool_解析与回退()
        {
            Assert.IsTrue(CommGridHelper.ReadIniBool("true", false, Where("b1"), L));
            Assert.AreEqual(0, _log.Count);

            string w = Where("b2");
            Assert.IsFalse(CommGridHelper.ReadIniBool("xyz", false, w, L));
            Assert.AreEqual(1, _log.Count);
            StringAssert.Contains(_log[0], w);

            Assert.IsTrue(CommGridHelper.ReadIniBool(null, true, Where("b3"), L), "null 回退到默认值");
        }

        // LogOnce：同一配置项（同 where）只报一次，坏值不刷屏
        [TestMethod]
        public void LogBadIni_同一配置项只报一次()
        {
            string w = Where("dup");
            CommGridHelper.ReadIniDecimalIntegral("x", 9, 1, 255, w, L);
            CommGridHelper.ReadIniDecimalIntegral("y", 9, 1, 255, w, L);
            Assert.AreEqual(1, _log.Count, "★第33轮：同一 where 只允许一条坏值日志");
        }
    }
}
