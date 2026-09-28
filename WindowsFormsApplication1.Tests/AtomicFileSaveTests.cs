using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// AtomicFileSave 原子写（★第47轮"方案保存失败破坏原文件"根修的可测面）。
    /// 现场事故链：CogSerializer.SaveObjectToFile 直写目标 .vpp，序列化中途抛
    /// ObjectDisposedException("无法访问已释放的对象 对象名:CogImage8Grey") →
    /// 目标被截断成残文件 → 启动加载报"流尾" → 好方案彻底打不开。
    /// 本类验证：任何写失败目标字节不动、半成品临时文件清理、旧内容滚动 .bak。
    /// 纯文件系统逻辑——无 Cognex 依赖，CI 可跑。
    /// </summary>
    [TestClass]
    public class AtomicFileSaveTests
    {
        private string _dir;

        [TestInitialize]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "AtomicFileSaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
        }

        private string P(string name)
        {
            return Path.Combine(_dir, name);
        }

        [TestMethod]
        public void Write_首次保存_目标不存在_直接落位且无临时残留()
        {
            string target = P("a.vpp");
            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "第一版", Encoding.UTF8));

            Assert.AreEqual("第一版", File.ReadAllText(target, Encoding.UTF8));
            Assert.IsFalse(File.Exists(target + ".saving"), "成功后不应残留 .saving");
            Assert.IsFalse(File.Exists(target + ".bak"), "首次保存没有旧文件，不应产生 .bak");
        }

        [TestMethod]
        public void Write_覆盖保存_旧内容完整滚动为bak_目标为新内容()
        {
            string target = P("a.vpp");
            File.WriteAllText(target, "旧方案", Encoding.UTF8);

            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "新方案", Encoding.UTF8));

            Assert.AreEqual("新方案", File.ReadAllText(target, Encoding.UTF8));
            Assert.AreEqual("旧方案", File.ReadAllText(target + ".bak", Encoding.UTF8), "旧文件必须完整滚动到 .bak");
            Assert.IsFalse(File.Exists(target + ".saving"));
        }

        [TestMethod]
        public void Write_写入中途抛异常_目标字节原封不动_无bak无残留()
        {
            string target = P("a.vpp");
            byte[] before = Encoding.UTF8.GetBytes("完好的原方案");
            File.WriteAllBytes(target, before);

            Assert.ThrowsException<InvalidOperationException>(() =>
                AtomicFileSave.Write(target, tmp =>
                {
                    File.WriteAllText(tmp, "写了一半", Encoding.UTF8);   // 模拟序列化中途已截断临时文件
                    throw new InvalidOperationException("模拟 ObjectDisposedException");
                }));

            CollectionAssert.AreEqual(before, File.ReadAllBytes(target), "保存失败绝不能动原文件");
            Assert.IsFalse(File.Exists(target + ".bak"), "失败不应产生 .bak（旧文件仍在原位）");
            Assert.IsFalse(File.Exists(target + ".saving"), "半成品临时文件必须清理");
        }

        [TestMethod]
        public void Write_写入回调一个字节都没写_同样失败且原文件不动()
        {
            string target = P("a.vpp");
            byte[] before = Encoding.UTF8.GetBytes("原方案");
            File.WriteAllBytes(target, before);

            Assert.ThrowsException<InvalidOperationException>(() =>
                AtomicFileSave.Write(target, tmp => { throw new InvalidOperationException("打开临时文件即失败"); }));

            CollectionAssert.AreEqual(before, File.ReadAllBytes(target));
            Assert.IsFalse(File.Exists(target + ".saving"));
        }

        [TestMethod]
        public void Write_目标目录不存在_自动创建()
        {
            string target = Path.Combine(_dir, "sub", "new", "a.vpp");
            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "x", Encoding.UTF8));

            Assert.IsTrue(File.Exists(target));
        }

        [TestMethod]
        public void Write_连续三次保存_bak恒为上一版内容()
        {
            string target = P("a.vpp");
            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "v1", Encoding.UTF8));
            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "v2", Encoding.UTF8));
            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "v3", Encoding.UTF8));

            Assert.AreEqual("v3", File.ReadAllText(target, Encoding.UTF8));
            Assert.AreEqual("v2", File.ReadAllText(target + ".bak", Encoding.UTF8), ".bak 只滚动一份，是上一次成功的内容");
        }

        [TestMethod]
        public void Write_上次进程被杀残留的半成品_本次被清理并正常落位()
        {
            string target = P("a.vpp");
            File.WriteAllText(target + ".saving", "上次进程被杀留下的半成品", Encoding.UTF8);

            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "本次新内容", Encoding.UTF8));

            Assert.AreEqual("本次新内容", File.ReadAllText(target, Encoding.UTF8));
            Assert.IsFalse(File.Exists(target + ".saving"));
        }
    }
}
