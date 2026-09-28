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
            Assert.IsFalse(File.Exists(AtomicFileSave.TempPathFor(target)), "成功后不应残留临时文件");
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
            Assert.IsFalse(File.Exists(AtomicFileSave.TempPathFor(target)));
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
            Assert.IsFalse(File.Exists(AtomicFileSave.TempPathFor(target)), "半成品临时文件必须清理");
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
            Assert.IsFalse(File.Exists(AtomicFileSave.TempPathFor(target)));
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
            File.WriteAllText(AtomicFileSave.TempPathFor(target), "上次进程被杀留下的半成品", Encoding.UTF8);

            AtomicFileSave.Write(target, tmp => File.WriteAllText(tmp, "本次新内容", Encoding.UTF8));

            Assert.AreEqual("本次新内容", File.ReadAllText(target, Encoding.UTF8));
            Assert.IsFalse(File.Exists(AtomicFileSave.TempPathFor(target)));
        }

        // ---------- ★第48轮复审新增 ----------

        [TestMethod]
        public void Write_临时文件名剥离vpp_不被打开框过滤串放行()
        {
            // 打开框过滤串为 "*.vpp*"（放行 .bak 是刻意的恢复手段）。若临时名形如 xxx.vpp.saving，
            // 操作员会看到半成品文件，选中即拿它去加载 = 第47轮根因链里的"流尾"崩溃。
            string target = P("方案1.VPP");
            string temp = AtomicFileSave.TempPathFor(target);
            Assert.IsFalse(temp.IndexOf(".vpp", StringComparison.OrdinalIgnoreCase) >= 0,
                "临时文件名不得含 .vpp（含大小写），否则会被 *.vpp* 过滤串放行");

            AtomicFileSave.Write(target, tmp =>
            {
                Assert.AreEqual(temp, tmp, "Write 实际使用的临时名必须与 TempPathFor 一致");
                File.WriteAllText(tmp, "x", Encoding.UTF8);
            });
            Assert.IsTrue(File.Exists(target));
        }

        [TestMethod]
        public void Write_兜底替换第三步落位失败_原路径还原为旧内容()
        {
            // ★第48轮复审P2-1：File.Replace 与兜底 File.Move 都因临时文件被独占锁而失败，
            // 逼出 Copy→Delete→Move 的第三步失败窗口——此刻原路径是空的，必须先把 .bak 还原回去，
            // 否则调用方弹窗"原方案文件未被改动"就是假话。
            string target = P("a.vpp");
            File.WriteAllText(target, "旧方案", Encoding.UTF8);

            FileStream held = null;
            Exception caught = null;
            try
            {
                try
                {
                    AtomicFileSave.Write(target, tmp =>
                    {
                        File.WriteAllText(tmp, "新方案", Encoding.UTF8);
                        held = new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.None);   // 独占锁住临时文件
                    });
                }
                catch (Exception ex) { caught = ex; }
            }
            finally
            {
                if (held != null) held.Dispose();
            }

            Assert.IsNotNull(caught, "落位失败必须把异常抛给调用方");
            Assert.IsTrue(caught is IOException, "应为 IOException 族，实际: " + caught.GetType().FullName);
            Assert.IsFalse(caught is AtomicFileSave.IntegrityLostException,
                "还原成功就不得报'内容丢失'异常（那会让调用方文案改口）");
            Assert.AreEqual("旧方案", File.ReadAllText(target, Encoding.UTF8),
                "Move 失败必须先把 .bak 还原回原路径，'原方案文件未被改动'这句必须为真");
            Assert.IsFalse(File.Exists(target + ".bak"), "还原成功后 .bak 已被移回原路径");
        }

        [TestMethod]
        public void IsDeadImageException_直接与多层包装_中英文都认()
        {
            // ★第48轮复审P3③：现场中文消息、COM/包装层裹一层、英文消息三种形态都要认出死图像，
            // 否则弹窗退化成通用文案，丢掉"重启→重新采集→重新训练"的特化指引。
            Assert.IsTrue(AtomicFileSave.IsDeadImageException(
                new ObjectDisposedException("CogImage8Grey")), "直接 ODE");
            Assert.IsTrue(AtomicFileSave.IsDeadImageException(
                new IOException("无法访问已释放的对象 对象名:CogImage8Grey")), "现场中文消息");
            Assert.IsTrue(AtomicFileSave.IsDeadImageException(
                new InvalidOperationException("outer",
                    new InvalidOperationException("middle",
                        new ObjectDisposedException("CogImage8Grey")))), "三层内的 ODE 也要找到");
            Assert.IsTrue(AtomicFileSave.IsDeadImageException(
                new InvalidOperationException("wrapper",
                    new IOException("The object has been disposed before accessing it"))), "英文消息");
        }

        [TestMethod]
        public void IsDeadImageException_无关异常与null_不误报()
        {
            Assert.IsFalse(AtomicFileSave.IsDeadImageException(new IOException("磁盘空间不足")));
            Assert.IsFalse(AtomicFileSave.IsDeadImageException(new TimeoutException("等待在途检测静止超时(5s)")));
            Assert.IsFalse(AtomicFileSave.IsDeadImageException(null));
        }
    }
}
