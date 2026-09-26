using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using demo;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ClassIni 读写/缓存语义回归。核心是 ★复盘P1-1：
    /// 「读过缺键（默认值路径）后，再写入同值」必须真正落盘到文件，
    /// 不能被同值跳过逻辑误判为"文件里已有"——心跳类键（xintiao/xintiaoen）
    /// 就是靠这个机制补进 code.ini 的。
    /// 全部用例走临时文件，不碰仓库与现场配置。
    /// </summary>
    [TestClass]
    public class ClassIniTests
    {
        private string _path;
        private ClassIni _ini;

        [TestInitialize]
        public void Setup()
        {
            _path = Path.Combine(Path.GetTempPath(), "vp12_clsi_" + Guid.NewGuid().ToString("N") + ".ini");
            _ini = new ClassIni();
            _ini.ReadINIFile(_path);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try { if (_ini != null) _ini.Dispose(); } catch { }
            _ini = null;
            try { if (File.Exists(_path)) File.Delete(_path); } catch { }
        }

        private string FileText()
        {
            return File.ReadAllText(_path, Encoding.Default);
        }

        [TestMethod]
        public void ReadString_缺键返回默认_且不向文件写键()
        {
            Assert.AreEqual("D6", _ini.ReadString("secA", "camera_name", "D6"));
            if (File.Exists(_path))
                Assert.IsFalse(FileText().Contains("camera_name="), "纯读缺键不应在文件中产生键");
        }

        // ★复盘P1-1 回归用例（根修 = FileCache.SynthKeys 区分值来源）
        [TestMethod]
        public void P11_读缺键后写同值_键必须真正落盘()
        {
            string def = _ini.ReadString("secB", "xintiao", "D6");   // 缺键读：值只走过默认值路径
            Assert.AreEqual("D6", def);

            _ini.WriteString("secB", "xintiao", def);                 // 写"同值"：不得被同值跳过吞掉

            Assert.IsTrue(File.Exists(_path), "ini 文件应存在");
            StringAssert.Contains(FileText(), "xintiao=D6",
                "★P1-1：键必须写进文件，而不是只留在内存缓存（修复前这里会在开窗心跳时丢键）");
        }

        [TestMethod]
        public void WriteString_同值二次写被跳过_文件不动()
        {
            _ini.WriteString("secC", "k1", "V1");
            DateTime m1 = File.GetLastWriteTimeUtc(_path);
            Thread.Sleep(120);                                        // 拉开 NTFS mtime 粒度，使"落盘"可观测
            _ini.WriteString("secC", "k1", "V1");
            DateTime m2 = File.GetLastWriteTimeUtc(_path);
            Assert.AreEqual(m1, m2, "同值写应被跳过：文件 mtime 不应变化");
            StringAssert.Contains(FileText(), "k1=V1");
        }

        [TestMethod]
        public void WriteString_异值必须落盘覆盖()
        {
            _ini.WriteString("secD", "k2", "V1");
            _ini.WriteString("secD", "k2", "V2");
            Assert.AreEqual("V2", _ini.ReadString("secD", "k2", "DEF"));
            string text = FileText();
            StringAssert.Contains(text, "k2=V2");
            Assert.IsFalse(text.Contains("k2=V1"), "旧值应被覆盖");
        }

        [TestMethod]
        public void ReadString_外部手改ini_经mtime失效即时可见()
        {
            _ini.WriteString("secE", "k3", "V1");
            Assert.AreEqual("V1", _ini.ReadString("secE", "k3", "DEF"));

            Thread.Sleep(150);   // 拉开 mtime 时钟刻度：同刻度内改写按设计不可见（现场手改是秒级，不受此限）
            string text = FileText();
            File.WriteAllText(_path, text.Replace("V1", "V2"), Encoding.Default);

            Assert.AreEqual("V2", _ini.ReadString("secE", "k3", "DEF"),
                "外部手改 ini 后（P7 路径：mtime 失配 → 统计缓存失效）应读到新值");
        }

        [TestMethod]
        public void WriteInteger_ReadInteger_往返与缺键默认()
        {
            _ini.WriteInteger("secF", "geshu", 7);
            Assert.AreEqual(7, _ini.ReadInteger("secF", "geshu", -1));
            Assert.AreEqual(9, _ini.ReadInteger("secF", "absent", 9));
        }

        [TestMethod]
        public void WriteBool_ReadBool_往返与缺键默认()
        {
            _ini.WriteBool("secG", "en", true);
            Assert.IsTrue(_ini.ReadBool("secG", "en", false));
            Assert.IsFalse(_ini.ReadBool("secG", "absent", false));
        }

        [TestMethod]
        public void DeleteKey_物理删除并同步清理合成标记()
        {
            _ini.WriteString("secH", "k4", "V4");
            Assert.IsTrue(_ini.ValueExists("secH", "k4"));

            _ini.DeleteKey("secH", "k4");

            Assert.IsFalse(FileText().Contains("k4="), "键应被物理删除");
            Assert.AreEqual("DEF", _ini.ReadString("secH", "k4", "DEF"));
            Assert.IsFalse(_ini.ValueExists("secH", "k4"),
                "★P1-1：DeleteKey 必须同步摘除 SynthKeys 标记，否则缓存与文件永久分叉");
        }

        [TestMethod]
        public void EraseSection_整段物理清除()
        {
            _ini.WriteString("secI", "a1", "1");
            _ini.WriteString("secI", "b1", "2");

            _ini.EraseSection("secI");

            string text = FileText();
            Assert.IsFalse(text.Contains("a1="), "整段键应被清除");
            Assert.IsFalse(text.Contains("b1="), "整段键应被清除");
        }

        [TestMethod]
        public void ClearCache_后读到外部新值()
        {
            _ini.WriteString("secJ", "k5", "V1");
            string text = FileText();
            File.WriteAllText(_path, text.Replace("V1", "V2"), Encoding.Default);

            _ini.ClearCache();

            Assert.AreEqual("V2", _ini.ReadString("secJ", "k5", "DEF"),
                "★P1-1：ClearCache 必须同步作废 SynthKeys，整体作废后读回应以文件为准");
        }
    }
}
