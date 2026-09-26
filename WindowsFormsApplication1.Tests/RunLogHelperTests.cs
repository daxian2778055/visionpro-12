using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// RunLog 私有静态工具方法的字节级语义（均为纯函数/纯 IO，可在任意机器跑）。
    /// 覆盖 ★复盘P0（AppendLineSafe 在 FileMode.Append 下必抛的修复）、
    /// P1-2 的字节不动契约、功能⑧的收尾换行口径、WriteAllLinesAtomic 原子替换。
    /// 反射调用而非改生产代码访问级——被测程序集就是本测试程序集（源码链接），类型可直接拿到。
    /// </summary>
    [TestClass]
    public class RunLogHelperTests
    {
        private string _file;

        [TestInitialize]
        public void Setup()
        {
            _file = Path.Combine(Path.GetTempPath(), "vp12_als_" + Guid.NewGuid().ToString("N") + ".csv");
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (File.Exists(_file)) File.Delete(_file); } catch { }
            try { if (File.Exists(_file + ".tmp")) File.Delete(_file + ".tmp"); } catch { }
        }

        private static object Call(string method, params object[] args)
        {
            MethodInfo mi = typeof(RunLog).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "私有静态方法不存在（被重构改名？）: " + method);
            try
            {
                return mi.Invoke(null, args);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                throw;
            }
        }

        // 单参调用专用：避免 string[] 被 params 数组协变吞成"多个参数"
        private static object Call1(string method, object arg)
        {
            return Call(method, new object[] { arg });
        }

        [TestMethod]
        public void AppendLineSafe_文件不存在_按默认编码创建并写入()
        {
            string text = "2026-09-25,10,6,4,mode,0.6";
            Call("AppendLineSafe", _file, text);
            CollectionAssert.AreEqual(Encoding.Default.GetBytes(text + Environment.NewLine),
                File.ReadAllBytes(_file));
        }

        [TestMethod]
        public void AppendLineSafe_文件尾已有换行_不再补换行不产生空行()
        {
            byte[] prefix = Encoding.Default.GetBytes("h1,h2\r\nr1,r2\r\n");
            File.WriteAllBytes(_file, prefix);
            Call("AppendLineSafe", _file, "r3");
            byte[] expected = new byte[prefix.Length + Encoding.Default.GetByteCount("r3" + Environment.NewLine)];
            Array.Copy(prefix, expected, prefix.Length);
            Array.Copy(Encoding.Default.GetBytes("r3" + Environment.NewLine), 0, expected, prefix.Length,
                expected.Length - prefix.Length);
            CollectionAssert.AreEqual(expected, File.ReadAllBytes(_file), "尾已有换行时只追加本行，不得多拼换行");
        }

        [TestMethod]
        public void AppendLineSafe_文件尾无换行_先补CRLF再写()
        {
            byte[] prefix = Encoding.Default.GetBytes("AAA,BBB");
            File.WriteAllBytes(_file, prefix);
            Call("AppendLineSafe", _file, "CCC");
            byte[] suffix = Encoding.Default.GetBytes("\r\nCCC" + Environment.NewLine);
            byte[] expected = new byte[prefix.Length + suffix.Length];
            Array.Copy(prefix, expected, prefix.Length);
            Array.Copy(suffix, 0, expected, prefix.Length, suffix.Length);
            CollectionAssert.AreEqual(expected, File.ReadAllBytes(_file));
        }

        [TestMethod]
        public void AppendLineSafe_历史字节逐字节不动_含中文前缀()
        {
            byte[] prefix = Encoding.Default.GetBytes("[历史段]\r\n型号AA,3\r\n中文,含逗号,行\r\n");
            File.WriteAllBytes(_file, prefix);

            Call("AppendLineSafe", _file, "新记录,1");

            byte[] all = File.ReadAllBytes(_file);
            Assert.IsTrue(all.Length > prefix.Length, "应有新内容追加");
            byte[] head = new byte[prefix.Length];
            Array.Copy(all, head, prefix.Length);
            CollectionAssert.AreEqual(prefix, head, "★复盘P1-2/P0 契约：历史字节必须逐字节不变");

            byte[] tail = new byte[all.Length - prefix.Length];
            Array.Copy(all, prefix.Length, tail, 0, tail.Length);
            CollectionAssert.AreEqual(Encoding.Default.GetBytes("新记录,1" + Environment.NewLine), tail);
        }

        [TestMethod]
        public void AppendLineSafe_连续两次追加_无空行累积()
        {
            Call("AppendLineSafe", _file, "L1");
            Call("AppendLineSafe", _file, "L2");
            CollectionAssert.AreEqual(
                Encoding.Default.GetBytes("L1" + Environment.NewLine + "L2" + Environment.NewLine),
                File.ReadAllBytes(_file), "两次追加应首尾相接（消除旧实现恒拼前缀的空行累积）");
        }

        [TestMethod]
        public void WriteAllLinesAtomic_覆盖已有文件_无tmp残留()
        {
            File.WriteAllLines(_file, new[] { "old1", "old2" }, Encoding.Default);
            Call("WriteAllLinesAtomic", _file, new[] { "new1", "new2", "new3" });
            CollectionAssert.AreEqual(new[] { "new1", "new2", "new3" },
                File.ReadAllLines(_file, Encoding.Default));
            Assert.IsFalse(File.Exists(_file + ".tmp"), "临时文件应已被替换消耗");
        }

        [TestMethod]
        public void WriteAllLinesAtomic_目标不存在_由tmp迁入()
        {
            Call("WriteAllLinesAtomic", _file, new[] { "a", "b" });
            CollectionAssert.AreEqual(new[] { "a", "b" }, File.ReadAllLines(_file, Encoding.Default));
            Assert.IsFalse(File.Exists(_file + ".tmp"));
        }

        [TestMethod]
        public void HasTooManyUnmappableChars_单行超20个问号才判编码不符()
        {
            Assert.IsTrue((bool)Call1("HasTooManyUnmappableChars", new[] { "ok", new string('?', 21) }));
            Assert.IsFalse((bool)Call1("HasTooManyUnmappableChars", new[] { "ok", new string('?', 20) }),
                "边界：恰好 20 个不判");
            Assert.IsFalse((bool)Call1("HasTooManyUnmappableChars", new string[] { null, "" }),
                "空行不计");
            Assert.IsFalse((bool)Call1("HasTooManyUnmappableChars", new[] { new string('?', 10), new string('?', 10) }),
                "判据按单行统计，跨行不累加");
            Assert.IsFalse((bool)Call1("HasTooManyUnmappableChars", new[] { "ASCII,100,60,40,AA,0.6" }));
        }

        [TestMethod]
        public void CountNonEmptyLines_非空行统计()
        {
            Assert.AreEqual(2, (int)Call1("CountNonEmptyLines", new string[] { null, "", "x", "", "y", null }));
            Assert.AreEqual(0, (int)Call1("CountNonEmptyLines", new string[] { null, "" }));
        }
    }
}
