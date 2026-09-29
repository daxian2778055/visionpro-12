using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第56轮 P0：耗时实测器（RenderCostMeter）单测。
    ///
    /// 它服务的不是裁决，就是现场问答的那两个数字：
    ///   ①"限频裁决本身会不会反过来加重负荷"——裁决每帧都要问，只有微秒级才有资格每帧问一次；
    ///   ②"把 80Hz 降到 40Hz 省下的 CPU 是不是真金白银"——要量一次**真渲染**，
    ///     而 record 光栅化与原图贴 PictureBox 差一个量级，混在一张表里就没法用。
    /// 读数口径必须钉死，否则"均值"到底是半窗、总账还是上一窗，三种数字会互相打脸：
    ///   · 读侧只报**上一个完整窗口**（正在累加的半窗不算，否则现场把半窗当成全窗均值报）；
    ///   · 没测到 ≠ 很快（无样本时均值必须回 0，不能回一个看着像"很快"的小数字）；
    ///   · 换窗时把该窗的样本**整批**快照过来（边界那一笔归旧窗）；
    ///   · 跨 Environment.TickCount 回绕（约 25 天）窗口照样推进——本项目反复踩环形时钟的坑；
    ///   · 并发登记不吞样本、不丢峰值（三张表由检测线程与 UI 线程同时写）。
    /// 全部用手工推进的 now + 给定的 tick 驱动，不依赖真实耗时，可复现。
    /// </summary>
    [TestClass]
    public class RenderCostMeterTests
    {
        private const int WindowMs = 1000;

        /// <summary>微秒 → Stopwatch tick（与实现同一套换算，不把 QPC 频率写死进断言）。</summary>
        private static long TicksOf(long microseconds)
        {
            return microseconds * Stopwatch.Frequency / 1_000_000L;
        }

        /// <summary>tick → 微秒（期望值按同一公式算，换算本身不参与断言）。</summary>
        private static int MicrosOf(long ticks)
        {
            long micros = ticks * 1_000_000L / Stopwatch.Frequency;
            return micros > int.MaxValue ? int.MaxValue : (int)micros;
        }

        [TestMethod]
        public void 没有样本时均值峰值都回报零_不假装很快()
        {
            var m = new RenderCostMeter(WindowMs);
            Assert.AreEqual(0, m.SamplesPerWindow(), "没测过却有样本数");
            Assert.AreEqual(0, m.AvgMicroseconds(),
                "无样本时均值必须回 0：0=没测到，不能回一个看着像\"很快\"的数字");
            Assert.AreEqual(0, m.PeakMicroseconds(), "无样本时峰值应为 0");
            Assert.AreEqual(0, m.OverallAvgMicroseconds(), "无样本时总账均值应为 0");
            Assert.AreEqual(0, m.TotalSamples());
        }

        [TestMethod]
        public void 正在累加的窗不对外读_但窗口起点就是第一笔()
        {
            var m = new RenderCostMeter(WindowMs);
            m.Record(TicksOf(200), 1000);         // 窗口起点钉在 1000
            m.Record(TicksOf(400), 1999);         // 999ms < 窗口 ⇒ 还不换窗

            Assert.AreEqual(0, m.SamplesPerWindow(), "当前窗没收口就报给了读侧");
            Assert.AreEqual(0, m.AvgMicroseconds());
            Assert.AreEqual(0, m.PeakMicroseconds());
            Assert.AreEqual(2, m.TotalSamples(), "读侧不报数不等于没记账");

            // 窗口起点若被"未起始哨兵 0"带跑，这里 would 从进程启动时刻起算（生产=TickCount 回绕后
            // 的偶发早换窗）。now=1999 仍没换窗 ⇒ 第一笔已经把起点钉住了。
            m.Record(TicksOf(300), 2000);         // 距 1000 满 1000ms ⇒ 换窗
            Assert.AreEqual(2, m.SamplesPerWindow(), "上一窗快照的样本数不对");
            Assert.AreEqual(MicrosOf((TicksOf(200) + TicksOf(400)) / 2), m.AvgMicroseconds(),
                "上一窗均值口径不对");
            Assert.AreEqual(MicrosOf(TicksOf(400)), m.PeakMicroseconds(), "上一窗峰值应取窗内最大那次");
            Assert.AreEqual(3, m.TotalSamples(), "换窗不得重复计数或丢计数");
        }

        [TestMethod]
        public void 窗口没换过时总账兜底给出真实均值()
        {
            // 弹窗同时打"上一窗均值"和"总账均值"：开机头一秒还没有上一窗，读侧是 0，
            // 现场会误读成"裁决没跑"。总账兜底就是为了让这一秒也能读出量级。
            var m = new RenderCostMeter(WindowMs);
            m.Record(TicksOf(200), 1000);
            m.Record(TicksOf(400), 1200);
            Assert.AreEqual(0, m.SamplesPerWindow(), "窗口还没收过口");
            Assert.AreEqual(MicrosOf((TicksOf(200) + TicksOf(400)) / 2), m.OverallAvgMicroseconds(),
                "总账均值不是进程启动以来的全量均值");
            Assert.AreEqual(2, m.TotalSamples());
        }

        [TestMethod]
        public void 换窗先快照后入账_边界那笔归新窗()
        {
            // 实现顺序是：判满窗 → 把整窗快照给读侧 → 这一笔作为**新窗第一笔**入账。
            // 于是恰好隔一个窗口时长的稳态流（例如 1fps）读出来是"每窗 1 笔"，
            // 而边界那一笔不算进旧窗——口径写死成断言，谁改成"先入账再换窗"就会在这里失败
            // （那会让"次数/秒"整数量级地漂，现场据此判断"裁决跑没跑"就没了依据）。
            var m = new RenderCostMeter(WindowMs);
            for (int sec = 0; sec < 4; sec++) m.Record(TicksOf(100), sec * WindowMs + 500);
            Assert.AreEqual(1, m.SamplesPerWindow(),
                "每窗 1 笔的稳态读数应当是 1（实测 " + m.SamplesPerWindow() + "）");

            var edge = new RenderCostMeter(WindowMs);
            edge.Record(TicksOf(100), 500);
            edge.Record(TicksOf(900), 1500);      // 距 500 满 1000ms ⇒ 先快照旧窗
            Assert.AreEqual(1, edge.SamplesPerWindow(), "边界那笔不该算进旧窗（换窗发生在入账之前）");
            Assert.AreEqual(MicrosOf(TicksOf(100)), edge.AvgMicroseconds(), "旧窗均值不该含 900μs 那笔");
            Assert.AreEqual(MicrosOf(TicksOf(100)), edge.PeakMicroseconds(), "旧窗峰值同理");
            edge.Record(TicksOf(100), 1600);      // 新窗第二笔（尚未满窗，读侧不动）
            Assert.AreEqual(1, edge.SamplesPerWindow(), "新窗还没收口，读侧不该跟着涨");
        }

        [TestMethod]
        public void 零点时刻与未起始哨兵同值_窗口起点由第一笔非零时刻钉住()
        {
            // _windowStartTick 用 0 表示"还没起窗"，而 Environment.TickCount 恰好取到 0 的那一笔
            // （开机瞬间、以及每约 24.9 天回绕一次）撞同一个值 ⇒ 起点顺延到下一笔。
            // 后果只是这一窗对齐偏一个窗长（诊断量），不参与任何裁决——写死在这里，
            // 免得日后现场看到"某一秒的次数是 3"当成丢帧/重复计数来查。
            var m = new RenderCostMeter(WindowMs);
            m.Record(TicksOf(100), 0);
            m.Record(TicksOf(100), 500);           // 起点由这一笔钉在 500
            m.Record(TicksOf(100), 1499);          // 距 500 差 1ms ⇒ 不换窗
            Assert.AreEqual(0, m.SamplesPerWindow(),
                "若 0 时刻那笔把起点钉住，这里早已换窗（说明实现顺序变了，口径要重估）");
            m.Record(TicksOf(100), 1500);          // 满 1000ms ⇒ 换窗，旧窗=0/500/1499 三笔
            Assert.AreEqual(3, m.SamplesPerWindow(), "对齐后的这一窗应包含 0 时刻那一笔");
            Assert.AreEqual(4, m.TotalSamples(), "总账与窗口对齐无关，必须一笔不丢");
        }

        [TestMethod]
        public void 窗口未到时长不开新窗()
        {
            var m = new RenderCostMeter(WindowMs);
            m.Record(TicksOf(100), 5000);
            for (int t = 5001; t < 5999; t++) m.Record(TicksOf(100), t);   // 同一窗内灌 998 次

            Assert.AreEqual(0, m.SamplesPerWindow(), "窗口没满 1000ms 就换窗 ⇒ \"次数/秒\"会被读成半窗");
            Assert.AreEqual(999, m.TotalSamples());

            m.Record(TicksOf(100), 5999);
            Assert.AreEqual(0, m.SamplesPerWindow(), "差一毫秒也不该换窗");
            m.Record(TicksOf(100), 6000);                                  // 满 1000ms：换窗
            Assert.AreEqual(1000, m.SamplesPerWindow(), "满窗应把整窗 1000 笔快照出来");
        }

        [TestMethod]
        public void 换窗后读侧整批换新_均值与峰值都来自新快照()
        {
            // 时刻一律避开 0（0 与"未起始"哨兵同值，那条行为另有专门用例钉）。
            var m = new RenderCostMeter(WindowMs);
            m.Record(TicksOf(1000), 100);
            m.Record(TicksOf(1000), 600);      // 第一窗：2 笔 ×1000μs（起点=100）
            m.Record(TicksOf(50), 1100);       // 距 100 满 1000ms ⇒ 换窗 ⇒ 读侧=第一窗
            m.Record(TicksOf(50), 1600);
            m.Record(TicksOf(50), 2099);       // 第二窗累加中：3 笔 ×50μs，不对外读

            Assert.AreEqual(2, m.SamplesPerWindow(), "读侧把正在累加的第二窗混进来了");
            Assert.AreEqual(MicrosOf(TicksOf(1000)), m.AvgMicroseconds(), "读侧均值应仍是第一窗");
            Assert.AreEqual(MicrosOf(TicksOf(1000)), m.PeakMicroseconds(),
                "第一窗峰值 1000μs 应保留到换窗为止");

            m.Record(TicksOf(10), 2100);       // 距 1100 满 1000ms ⇒ 换窗 ⇒ 读侧整批换成第二窗
            Assert.AreEqual(3, m.SamplesPerWindow(), "换窗应把该窗全部样本快照过来");
            Assert.AreEqual(MicrosOf(TicksOf(50)), m.AvgMicroseconds(), "新快照的均值没跟上");
            Assert.AreEqual(MicrosOf(TicksOf(50)), m.PeakMicroseconds(),
                "峰值没随窗口整批切换 ⇒ 上一窗的 1000μs 还在冒充当前峰值");
        }

        [TestMethod]
        public void 负耗时样本不入账_免得污染读数()
        {
            // Stopwatch 差值理论上非负；一旦时钟源异常给出负数，入账会把均值拉负或回绕成
            // "21 亿微秒"这种没法解释的读数，现场就无法用它做判断了。
            var m = new RenderCostMeter(WindowMs);
            m.Record(-1, 3000);
            m.Record(long.MinValue, 3000);
            Assert.AreEqual(0, m.TotalSamples(), "负样本不该计入总账");
            Assert.AreEqual(0, m.OverallAvgMicroseconds());
            Assert.AreEqual(0, m.SamplesPerWindow(), "被拒绝的样本不该把窗口起点钉到 3000");

            m.Record(TicksOf(200), 4000);      // 窗口起点改由这笔钉下
            m.Record(TicksOf(200), 5000);      // 换窗 ⇒ 上一窗只有 4000 那一笔
            Assert.AreEqual(1, m.SamplesPerWindow(), "脏样本连窗口都不该影响");
            Assert.AreEqual(MicrosOf(TicksOf(200)), m.AvgMicroseconds());
        }

        [TestMethod]
        public void 跨时钟回绕窗口照样推进()
        {
            // Environment.TickCount 约 24.9 天回绕一次，回绕瞬间 now 从 int.MaxValue 跳到 int.MinValue。
            // 窗口判断走 unchecked，所以回绕前后照样算得出"过了 1000ms"。
            var m = new RenderCostMeter(WindowMs);
            int before = int.MaxValue - 500;
            int after = unchecked(before + 1000);          // 绕到 int.MinValue + 499
            Assert.IsTrue(after < 0, "样例本身要真的跨过回绕点，否则这条断言没意义");

            m.Record(TicksOf(700), before);
            m.Record(TicksOf(700), after);                 // 距窗口起点满 1000ms ⇒ 换窗
            Assert.AreEqual(1, m.SamplesPerWindow(),
                "回绕把窗口冻住了（读数会长期停在回绕前那一刻的窗）");
            Assert.AreEqual(MicrosOf(TicksOf(700)), m.AvgMicroseconds());
            Assert.AreEqual(2, m.TotalSamples());
        }

        [TestMethod]
        public void 窗口参数非法回退默认一千毫秒()
        {
            // 三个计量表现在都是代码内 new(1000)，但构造口径要能扛住以后从 ini 注入脏值：
            // 窗口 0/负数会把每次到达都判成换窗，"次数/秒"直接变成"单次耗时"。
            var zero = new RenderCostMeter(0);
            zero.Record(TicksOf(100), 100);
            zero.Record(TicksOf(100), 101);                // 若窗口=1ms 这里已经换窗，读侧会报 1
            Assert.AreEqual(0, zero.SamplesPerWindow(), "窗口 0ms 没回退默认：两次到达就被当成两窗");
            Assert.AreEqual(2, zero.TotalSamples());

            var neg = new RenderCostMeter(-5);
            neg.Record(TicksOf(100), 100);
            neg.Record(TicksOf(100), 1099);                // 999ms < 1000ms ⇒ 不换窗
            Assert.AreEqual(0, neg.SamplesPerWindow(), "负窗口同样要回退到 1000ms");
            neg.Record(TicksOf(100), 1100);                // 满 1000ms ⇒ 换窗
            Assert.AreEqual(2, neg.SamplesPerWindow(), "回退后的窗口时长应=1000ms");
        }

        [TestMethod]
        public void 并发登记不吞样本()
        {
            // 三张表（裁决 / record 显示 / 原图显示）由检测线程与 UI 线程并发写。
            // 换窗瞬间的读侧允许差 1 个样本（实现文档写明不加锁的代价），但**总账**必须一笔不丢：
            // 现场用它核对"裁决次数是否等于到达次数"。
            var m = new RenderCostMeter(WindowMs);
            const int Threads = 8;
            const int PerThread = 2000;
            long bigTicks = TicksOf(5000);
            var start = new ManualResetEventSlim(false);
            var done = new CountdownEvent(Threads);
            var names = new Thread[Threads];
            for (int k = 0; k < Threads; k++)
            {
                int id = k;
                names[k] = new Thread(() =>
                {
                    start.Wait();
                    int now = Environment.TickCount;
                    for (int i = 0; i < PerThread; i++)
                    {
                        // 只有一路登记大样本：峰值那条 CAS 循环不能把它漏掉
                        m.Record(id == 3 ? bigTicks : TicksOf(10), now + i);
                    }
                    done.Signal();
                });
                names[k].IsBackground = true;
                names[k].Start();
            }
            start.Set();
            Assert.IsTrue(done.Wait(30000), "并发线程未在 30 秒内收敛");

            Assert.AreEqual(Threads * PerThread, m.TotalSamples(), "并发登记吞了样本");
            Assert.IsTrue(m.TotalSamples() > 0);
            Assert.IsTrue(MicrosOf(bigTicks) > MicrosOf(TicksOf(10)), "对照样本必须真的有大小样本之分");
        }
    }
}
