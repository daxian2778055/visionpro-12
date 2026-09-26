using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// RunLog.WriteDate / WriteDate1 / CreateCsvPath 端到端行为。
    /// 覆盖 ★功能⑥（编码不符不删行）、功能⑧（新一天整表原子追加无空行）、
    /// ★复盘P1-2（编码不符新一天走字节级追加，历史字节不动）、P2-4（5 秒限流日志）、
    /// 性能#3（序号单槽缓存命中/失效回退）、第26轮#20（方案切换追加表头分界）。
    ///
    /// 环境约束：统计根目录在生产代码里写死 E:\ —— 无 E 盘的环境（CI 托管 runner）
    /// 自动 Assert.Inconclusive 跳过；本机（有 E 盘）真实执行。
    /// 每个用例用 GUID 隔离子目录，Cleanup 整目录删除，绝不触碰现场真实统计文件。
    /// </summary>
    [TestClass]
    public class RunLogWriteDateTests
    {
        private string _p22;
        private string _monthCsv;
        private string _prodRoot;
        private string _dayRoot;
        private RunLog _log;
        private CapturingLogger _cap;

        /// <summary>进程内日志捕获器：替换 LogManager.Default，断言限流/表头日志。</summary>
        private sealed class CapturingLogger : ILogger
        {
            public readonly List<string> Messages = new List<string>();

            public void Write(string message)
            {
                lock (Messages) Messages.Add(message ?? "");
            }

            public void Write(string category, string message)
            {
                Write(category + ":" + message);
            }

            public void WriteCritical(string message) { Write(message); }
            public void Flush(TimeSpan timeout) { }
            public void Shutdown(TimeSpan timeout) { }
            public int DroppedCount { get { return 0; } }

            public int CountOf(string fragment)
            {
                lock (Messages)
                {
                    int n = 0;
                    foreach (string m in Messages)
                        if (m.Contains(fragment)) n++;
                    return n;
                }
            }
        }

        private static string Today() { return DateTime.Now.ToString("m"); }

        /// <summary>一个必定不等于今天的"其它月份"，用于构造跨天/跨月行。</summary>
        private static string OtherMonth() { return Today() == "1月" ? "2月" : "1月"; }

        [TestInitialize]
        public void Setup()
        {
            if (!Directory.Exists(@"E:\"))
                Assert.Inconclusive("当前环境无 E: 盘（生产统计根目录写死 E 盘根），本类端到端用例跳过");

            _p22 = "vp12t_" + Guid.NewGuid().ToString("N");
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.ToString("Y");   // 与生产代码同口径："2026年9月"
            string day = DateTime.Now.ToString("m");     // 与生产代码同口径："9月"

            _prodRoot = Path.Combine(@"E:\生产统计", _p22);
            _dayRoot = Path.Combine(@"E:\每日统计", _p22);
            _monthCsv = Path.Combine(_prodRoot, year, month + ".csv");
            string dayCsv = Path.Combine(_dayRoot, year, month, day + ".csv");
            Directory.CreateDirectory(Path.GetDirectoryName(_monthCsv));
            Directory.CreateDirectory(Path.GetDirectoryName(dayCsv));
            DayCsv = dayCsv;

            _cap = new CapturingLogger();
            LogManager.Initialize(_cap);
            _log = new RunLog();
        }

        private string DayCsv { get; set; }

        [TestCleanup]
        public void Cleanup()
        {
            try { LogManager.Initialize(null); } catch { }   // 恢复默认（惰性）日志器
            try { if (Directory.Exists(_prodRoot)) Directory.Delete(_prodRoot, true); } catch { }
            try { if (Directory.Exists(_dayRoot)) Directory.Delete(_dayRoot, true); } catch { }
        }

        // ---------- 功能⑧：新一天首条 → 整表原子追加，无空行 ----------

        [TestMethod]
        public void 功能8_新一天首条_整行原子追加且无空行()
        {
            File.WriteAllLines(_monthCsv, new[]
            {
                "日期,总量,OK,NG,型号,合格率",
                OtherMonth() + ",100,60,40,AA,0.6"
            }, Encoding.Default);

            _log.WriteDate(1, 1, _p22);

            string[] lines = File.ReadAllLines(_monthCsv, Encoding.Default);
            Assert.AreEqual(3, lines.Length, "只追加一行，不得插入空行（修复前恒拼 \\r\\n 前缀会多出空行）");
            Assert.AreEqual(OtherMonth() + ",100,60,40,AA,0.6", lines[1], "历史行必须原样保留");

            string[] f = lines[2].Split(',');
            Assert.AreEqual(Today(), f[0]);
            Assert.AreEqual("2", f[1], "总量 = okss + ng1");
            Assert.AreEqual("1", f[2]);
            Assert.AreEqual("1", f[3]);
            Assert.AreEqual("mode", f[4], "首条记录型号字段以 mode 起拼");
        }

        // ---------- 功能⑥：编码不符 → 当日累计行原样保留（修复前会写成空串=删行丢数） ----------

        [TestMethod]
        public void 功能6_编码不符时当日累计行原样保留不删行()
        {
            string junk = new string('?', 21);   // 单行 >20 个 '?' → 判定编码不符
            File.WriteAllLines(_monthCsv, new[]
            {
                "日期,总量,OK,NG,型号,合格率",
                junk + ",0,0,0,0,0",
                Today() + ",10,6,4,AA,0.6"
            }, Encoding.Default);

            _log.WriteDate(1, 1, _p22);

            string[] lines = File.ReadAllLines(_monthCsv, Encoding.Default);
            Assert.AreEqual(3, lines.Length, "编码不符时不改写文件");
            Assert.AreEqual(Today() + ",10,6,4,AA,0.6", lines[2],
                "★功能⑥：当日累计行必须原样保留（修复前被写成 string.Empty = 删行丢数振荡）");
        }

        // ---------- 复盘P2-4：跳过路径必须留痕，且 5 秒限流不刷屏 ----------

        [TestMethod]
        public void P24_编码不符跳过_5秒窗口内只记一条限流日志()
        {
            string junk = new string('?', 21);
            File.WriteAllLines(_monthCsv, new[]
            {
                "日期,总量,OK,NG,型号,合格率",
                junk + ",0,0,0,0,0",
                Today() + ",10,6,4,AA,0.6"
            }, Encoding.Default);

            _log.WriteDate(1, 1, _p22);
            _log.WriteDate(1, 1, _p22);
            _log.WriteDate(1, 1, _p22);

            Assert.AreEqual(1, _cap.CountOf("编码不符"),
                "★P2-4：同窗口多次跳过只允许一条限流日志（同时验证限流初值 0 修复后首跳必触发）");

            string[] lines = File.ReadAllLines(_monthCsv, Encoding.Default);
            Assert.AreEqual(Today() + ",10,6,4,AA,0.6", lines[2]);
        }

        // ---------- 复盘P1-2：编码不符 + 新一天 → 字节级追加，历史字节不动 ----------

        [TestMethod]
        public void P12_编码不符新一天_字节级追加且历史字节不动()
        {
            string junk = new string('?', 21);
            File.WriteAllLines(_monthCsv, new[]
            {
                "日期,总量,OK,NG,型号,合格率",
                junk + ",0,0,0,0,0",
                OtherMonth() + ",88,50,38,BB,0.5"
            }, Encoding.Default);
            byte[] before = File.ReadAllBytes(_monthCsv);

            _log.WriteDate(1, 1, _p22);

            byte[] after = File.ReadAllBytes(_monthCsv);
            Assert.IsTrue(after.Length > before.Length, "应追加今日新行");

            byte[] head = new byte[before.Length];
            Array.Copy(after, head, before.Length);
            CollectionAssert.AreEqual(before, head,
                "★复盘P1-2：历史字节必须逐字节不变（有损解码结果绝不允许整表重写固化）");

            string tail = Encoding.Default.GetString(after, before.Length, after.Length - before.Length);
            Assert.IsFalse(tail.StartsWith("\r\n"), "追加不得再插入空行（功能⑧收尾口径）");
            StringAssert.StartsWith(tail, Today() + ",2,1,1,mode,", "新行应为今日行");
        }

        // ---------- 功能⑧上限语义：≥35 非空行且非今日 → 清空末行（旧滚动语义保持） ----------

        [TestMethod]
        public void 功能8_超过35行且非今日_清空末行保持滚动语义()
        {
            var rows = new List<string>();
            rows.Add("日期,总量,OK,NG,型号,合格率");
            for (int i = 0; i < 33; i++)
                rows.Add((i + 1) + "日,10,5,5,X,0.5");
            rows.Add(OtherMonth() + ",88,50,38,BB,0.5");   // 非空行合计 35 → 不再满足 <35
            File.WriteAllLines(_monthCsv, rows, Encoding.Default);

            _log.WriteDate(1, 1, _p22);

            string[] lines = File.ReadAllLines(_monthCsv, Encoding.Default);
            Assert.AreEqual(35, lines.Length, "行数不变（整体原子重写）");
            Assert.AreEqual("", lines[lines.Length - 1], "≥35 非空行且非今日 → 保持旧滚动语义：末行清空");
        }

        // ---------- 性能#3：序号单槽缓存命中连续，外部追加后正确失效回退全量 ----------

        [TestMethod]
        public void 性能3_WriteDate1序号连续_外部追加后回退全量重算()
        {
            File.WriteAllLines(DayCsv, new[] { "序号,日期,检测结果" }, Encoding.Default);

            _log.WriteDate1("AAA", _p22, 1);
            _log.WriteDate1("BBB", _p22, 0);

            string[] lines = File.ReadAllLines(DayCsv, Encoding.Default);
            Assert.AreEqual(3, lines.Length);
            StringAssert.StartsWith(lines[1], "1,", "首条从 1 起（表头视为 0）");
            StringAssert.EndsWith(lines[1], ",OK");
            StringAssert.StartsWith(lines[2], "2,", "第二条命中缓存 → 序号连续");
            StringAssert.EndsWith(lines[2], ",NG");

            // 外部（其它进程/人工）追加一行 → (路径,长度,mtime) 三元组失配 → 必须回退全量读
            File.AppendAllText(DayCsv, "55,2026-01-01T00:00:00,EXT,OK" + Environment.NewLine, Encoding.Default);
            _log.WriteDate1("CCC", _p22, 1);

            lines = File.ReadAllLines(DayCsv, Encoding.Default);
            Assert.AreEqual(5, lines.Length);
            StringAssert.StartsWith(lines[4], "56,",
                "★性能#3：外部追加后缓存必须失效，序号接外部的 55 → 56（而不是继续 3）");
        }

        // ---------- 第26轮#20：方案切换后追加新表头作分界 ----------

        [TestMethod]
        public void 方案切换_CreateCsvPath追加新表头作分界并记日志()
        {
            _log.CreateCsvPath(_p22, "方案A");
            Assert.IsTrue(File.Exists(DayCsv), "首次调用应创建当日明细文件");
            string[] l1 = File.ReadAllLines(DayCsv, Encoding.Default);
            Assert.AreEqual("序号,日期,方案A结果", l1[0]);

            _log.CreateCsvPath(_p22, "方案B");

            string[] l2 = File.ReadAllLines(DayCsv, Encoding.Default);
            Assert.AreEqual(3, l2.Length, "应为：旧表头 + 空行 + 新表头");
            Assert.AreEqual("", l2[1]);
            Assert.AreEqual("序号,日期,方案B结果", l2[2], "新表头落在文件末尾作分界");
            Assert.AreEqual(1, _cap.CountOf("列头与当前方案不一致"), "表头分界必须留痕");
        }
    }
}
