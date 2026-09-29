using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第56轮：显示降频"阈值保护带 + 权重主动降幅"口径单测。
    ///
    /// 现场提的两条口径，逐条钉成断言：
    ///   ① 阈值以下的路一帧不丢（RenderProtectHz）——并证明**第54轮在同样场景下确实会挤它**，
    ///      否则这条保护是空承诺（max-min 只在需求 ≤ 水位时全显，水位=封顶/路数时 10Hz 的路照样被压）。
    ///   ② 权重越大、频率越高的路降得越狠（RenderWeight + 高/中频带最大降幅）——而且是**主动降**：
    ///      预算有余也照样压（Pressure=0 时应得间隔仍是周期的两倍）。这条正是本轮推翻上一版
    ///      "显示下限"草案的原因：下限口径在预算闲置时会把人抬回满显，80Hz→40Hz 根本不可能发生。
    ///   ③ 只减不加：权重拉满也**不越过封顶**——按稳态窗口核（总量 ≤ 窗口长度/RenderMinIntervalMs
    ///      + 每路一次的对齐余量）。启动瞬态除外：只到过一帧的路按"未知需求"处理、不预约额度，
    ///      所以在它交出第一个周期样本之前已测得的路能用满那份闲额度（实测最长的一条 6 秒 393
    ///      帧 > 封顶 375 帧，一次性、不随时间累积）。这条如实写进断言，不写成无条件承诺。
    ///   ④ 阈值与权重同时为 0 ⇒ 逐帧退回第54轮（现场总开关 / 回归基线）。
    ///
    /// 还要如实记下两条代价（写进类文档时也写过一遍）：保护带是**零和**的——低频路多显示的
    /// 必然来自快路少显示的；封顶放不下时保护会被剥夺，此时只有"总量仍封顶"这条承诺成立。
    ///
    /// 模拟一律按整数毫秒确定性驱动（不起线程、不读真实时钟），到达时刻 t = 0, p, 2p…（同相最坏场景）。
    /// </summary>
    [TestClass]
    public class DisplayThrottleWeightTests
    {
        private const int Interval = 16;          // 全局最小间隔 16ms ⇒ 封顶 62.5 次/秒
        private const int DurationMs = 6000;

        /// <summary>
        /// 稳态起点：取调度器判定"停流"的同一个普查窗口（2000ms）。到此刻任何仍在流上的路都已
        /// 至少采到两个到达点（周期样本存在），封顶占用与水位才是"稳态数字"。
        /// 在这之前总量允许临时超过封顶——见类文档与 尚无周期样本的路不占分母也不被分摊限速。
        /// </summary>
        private const int WarmupMs = 2000;

        /// <summary>
        /// 稳态窗口 [startMs, DurationMs) 内不越封顶的帧数上界。
        /// 推导：封顶 = 1000/Interval 次/秒 ⇒ 窗口长度/Interval 次；每路再放 1 次的余量
        /// （到期表按"应得时刻"顺延，窗口边界那一路的那一帧可前可后 ⇒ 最多一路一次 = Slots 次）。
        /// </summary>
        private static int SteadyCapFrames(int startMs)
        {
            return (DurationMs - startMs) / Interval + RenderBudget.Slots;
        }

        private static DisplayThrottleSettings Cfg(int protectHz, int weight)
        {
            // 高频带分界 50Hz、高带降幅上限 50%、中带降幅上限 25%（= code.ini [Display] 默认值）
            return new DisplayThrottleSettings(protectHz, weight,
                DisplayThrottleSettings.HighBandHzDefault,
                DisplayThrottleSettings.HighMaxCutPercentDefault,
                DisplayThrottleSettings.MidMaxCutPercentDefault);
        }

        private static int[] BuildPeriods(int n1, int p1, int n2, int p2)
        {
            int[] a = new int[RenderBudget.Slots];      // 0 = 该路停流
            for (int i = 0; i < n1; i++) a[i] = p1;
            for (int i = 0; i < n2; i++) a[n1 + i] = p2;
            return a;
        }

        private static int RequestCount(int periodMs)
        {
            return periodMs <= 0 ? 0 : (DurationMs + periodMs - 1) / periodMs;
        }

        /// <summary>按到达序列驱动一遍裁决，回报各路放行次数（与 RenderBudgetTests 的 Simulate 同一套时基）。</summary>
        private static int[] Drive(RenderBudget rb, int[] periods, int durationMs)
        {
            return DriveFrom(rb, periods, 0, durationMs);
        }

        /// <summary>
        /// 同上，但只统计 [startMs, durationMs) 内的放行。裁决仍从 0 起跑——否则周期估计是空的，
        /// 读到的就不是稳态分摊结果。startMs 用于把"启动瞬态"排除在稳态断言之外。
        /// </summary>
        private static int[] DriveFrom(RenderBudget rb, int[] periods, int startMs, int durationMs)
        {
            int[] wins = new int[RenderBudget.Slots];
            for (int t = 0; t < durationMs; t++)
                for (int i = 0; i < RenderBudget.Slots; i++)
                {
                    if (periods[i] <= 0 || t % periods[i] != 0) continue;
                    if (rb.TryClaim(i, t, Interval) && t >= startMs) wins[i]++;
                }
            return wins;
        }

        private static int Total(int[] wins)
        {
            int s = 0;
            for (int i = 0; i < wins.Length; i++) s += wins[i];
            return s;
        }

        /// <summary>只跑一条路、 warmed-up 之后读应得间隔（单路时水位解到自己的上限 ⇒ 读到的就是降幅本身）。</summary>
        private static int EntitledAfterWarmup(int periodMs, DisplayThrottleSettings cfg)
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(cfg);
            int[] one = new int[RenderBudget.Slots];
            one[0] = periodMs;
            Drive(rb, one, 2000);
            return rb.EntitledMs(0, 1999, Interval);
        }

        [TestMethod]
        public void 阈值以下的路饱和时一帧不丢_而第54轮同样场景会挤它()
        {
            // 5 路 8fps（125ms）+ 7 路 30fps（33ms）：需求 40+210=250 次/秒，远超封顶 62.5。
            // 第54轮：max-min 的水位解到 5.2fps（=封顶/路数）⇒ 8fps 的触发路应得间隔 192ms，
            //         6 秒里 48 帧只显示 32 帧（丢 1/3）——它并不知道自己低于阈值。
            // 本轮：8fps ≤ 阈值 10Hz ⇒ 进保护带、先从封顶扣掉 ⇒ 8fps 一帧不丢，被压的是 30fps 那 7 路。
            int[] periods = BuildPeriods(5, 125, 7, 33);

            var legacy = new RenderBudget();                       // 默认=第54轮（阈值0 权重0）
            int[] legacyWins = Drive(legacy, periods, DurationMs);
            Assert.IsTrue(legacyWins[0] <= RequestCount(125) * 3 / 4,
                "对照场景失效：第54轮的水位（本场景 5.2fps）本应压住 8fps 的路（48 帧里至少丢 1/4），"
                + "实测 " + legacyWins[0] + "/" + RequestCount(125) + "——对照实现走样了，这条断言就失去意义");

            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));                            // 不启用主动降幅（保护带仍要从快路那份封顶里出）
            int[] wins = Drive(rb, periods, DurationMs);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(RequestCount(125), wins[i],
                    "相机" + (i + 1) + "（8fps）在阈值以下却被丢了帧：显示 " + wins[i]
                    + "／应到 " + RequestCount(125));
            for (int i = 5; i < RenderBudget.Slots; i++)
                Assert.IsTrue(wins[i] > 0, "相机" + (i + 1) + " 被保护带饿死：一次都没显示");
            Assert.AreEqual(0, rb.ProtectionDemoted(DurationMs - 1, Interval),
                "保护带总额 40fps ≤ 封顶 62.5fps，不该有路被剥夺保护");
        }

        [TestMethod]
        public void 保护带是从快路嘴里拿的_零和要说清()
        {
            // 与上一条同一场景：低频路拿到的必须来自快路让出的——"只加保护、不动任何人"这句话
            // 只在**降幅**维度成立，封顶是零和的，别把默认参数写成免费的。
            int[] periods = BuildPeriods(5, 125, 7, 33);
            var legacy = new RenderBudget();
            int[] legacyWins = Drive(legacy, periods, DurationMs);

            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));
            int[] wins = Drive(rb, periods, DurationMs);

            for (int i = 0; i < 5; i++)
                Assert.IsTrue(wins[i] > legacyWins[i],
                    "相机" + (i + 1) + " 在阈值以下却没有比第54轮多显示：" + legacyWins[i] + "→" + wins[i]);
            for (int i = 5; i < RenderBudget.Slots; i++)
                Assert.IsTrue(wins[i] <= legacyWins[i],
                    "相机" + (i + 1) + "（30fps）比第54轮还多显示，保护带凭空多出了额度："
                    + legacyWins[i] + "→" + wins[i]);

            // 稳态口径下的总量对照（启动瞬态不计，理由见 SteadyCapFrames）。
            // 实测本场景：第54轮稳态 242 帧，本轮稳态 250 帧=封顶（250 帧/4 秒）——
            // 保护带把 30fps 那 7 路从每人 31 帧压到 21 帧，自己从 32 帧提到 48 帧，总额一度没多。
            var steadyLegacy = new RenderBudget();
            int steadyLegacyTotal = Total(DriveFrom(steadyLegacy, periods, WarmupMs, DurationMs));
            var steadyNew = new RenderBudget();
            steadyNew.ApplyConfig(Cfg(10, 0));
            int[] steadyWins = DriveFrom(steadyNew, periods, WarmupMs, DurationMs);
            int steadyTotal = Total(steadyWins);

            Assert.IsTrue(steadyTotal <= SteadyCapFrames(WarmupMs),
                "保护带把总显示量抬过了封顶：" + steadyTotal + " > " + SteadyCapFrames(WarmupMs)
                + "（低频路多显示的必须来自快路让出的）");
            Assert.IsTrue(steadyTotal >= steadyLegacyTotal,
                "稳态总量反而低于第54轮基准（" + steadyLegacyTotal + "→" + steadyTotal
                + "）：保护带成了净损失，额度被白白浪费");
        }

        [TestMethod]
        public void 阈值附近的抖动不反复进出保护带_迟滞()
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));
            int[] one = new int[RenderBudget.Slots];

            one[0] = 100;                       // 10fps = 阈值，进保护带
            Drive(rb, one, 2000);
            Assert.AreEqual(1, rb.IsPathProtected(0, 1999, Interval), "10fps 的路没进保护带");

            // 11.1fps（90ms）：超阈值但未超 1.25 倍 ⇒ 仍应在保护带内，否则阈值附近一抖就一阵卡
            for (int t = 2000; t < 4000; t++)
                if (t % 90 == 0) rb.TryClaim(0, t, Interval);
            Assert.AreEqual(1, rb.IsPathProtected(0, 3999, Interval),
                "≈11fps 被踢出保护带（实测周期 " + rb.MeasuredPeriodMs(0) + "ms）");

            // 14.3fps（70ms）：> 阈值×1.25 ⇒ 才真正出保护带
            for (int t = 4000; t < 7000; t++)
                if (t % 70 == 0) rb.TryClaim(0, t, Interval);
            Assert.AreEqual(0, rb.IsPathProtected(0, 6999, Interval),
                "14.3fps（周期 " + rb.MeasuredPeriodMs(0) + "ms）仍占着保护带，封顶会被未保护路抢走");
        }

        [TestMethod]
        public void 权重拉满时高频路的显示速率恰降到需求一半_且预算有余也降()
        {
            // 单路 100fps（10ms）：封顶 62.5fps 根本用不完，第54轮会全显；
            // 本轮 W=100 ⇒ 高带降幅上限 50% ⇒ 显示上限 50fps ⇒ 应得间隔 20ms = 周期的两倍。
            // 这一条正是"下限草案"办不到的地方：预算闲置时下限口径会把人抬回满显。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            int[] one = new int[RenderBudget.Slots];
            one[0] = 10;
            int[] wins = Drive(rb, one, DurationMs);

            Assert.AreEqual(20, rb.EntitledMs(0, DurationMs - 1, Interval),
                "100fps 在 W=100 下应得间隔应为 20ms（显示 50fps），实测周期 "
                + rb.MeasuredPeriodMs(0) + "ms");
            Assert.AreEqual(RequestCount(10) / 2, wins[0],
                "显示次数不等于需求的一半：放行 " + wins[0] + "／到达 " + RequestCount(10));
            Assert.AreEqual(0, rb.PressureCode(DurationMs - 1, Interval),
                "单路显示 50fps 低于封顶 62.5fps，压力档应为 0（预算闲置，降幅仍在主动生效）");
            Assert.AreEqual(80, rb.CapUsedPercent(DurationMs - 1, Interval),
                "封顶占用应 = 50fps/62.5fps = 80%（省下的 20% 就是主动降频换来的 CPU 余量）");
        }

        [TestMethod]
        public void 八十赫的路权重拉满显示约四十赫_现场原话口径()
        {
            // 现场原话：80Hz 降到 40Hz。周期取 12ms（≈83fps），显示上限=一半 ⇒ 应得间隔≈2 个周期。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            int[] one = new int[RenderBudget.Slots];
            one[0] = 12;
            int[] wins = Drive(rb, one, DurationMs);

            int entitled = rb.EntitledMs(0, DurationMs - 1, Interval);
            Assert.IsTrue(entitled >= 24 && entitled <= 26,
                "83fps 的路在 W=100 下应得间隔应≈2 个周期（40fps 显示），实测 " + entitled + "ms");
            int arrive = RequestCount(12);
            Assert.IsTrue(Math.Abs(wins[0] * 2 - arrive) <= arrive / 10,
                "显示率不是需求的一半：放行 " + wins[0] + "／到达 " + arrive);
        }

        [TestMethod]
        public void 降幅随频率单调递增_五十赫以下不超中带上限()
        {
            // 单路 warmed-up 应得间隔（只有一条路 ⇒ 水位解到自己的上限 ⇒ 读到的就是降幅本身）：
            //   100ms=10fps  保护带 ⇒ 90ms（=周期×0.9，一帧不丢）
            //    60ms=16.7fps 中带  ⇒ 63ms（降 4%）
            //    30ms=33.3fps 中带  ⇒ 36ms（降 15%）
            //    20ms=50fps   高带  ⇒ 40ms（降 50%）
            //    10ms=100fps  高带  ⇒ 20ms（降 50%）
            var cfg = Cfg(10, 100);
            int p10 = EntitledAfterWarmup(100, cfg);
            int p16 = EntitledAfterWarmup(60, cfg);
            int p33 = EntitledAfterWarmup(30, cfg);
            int p50 = EntitledAfterWarmup(20, cfg);
            int p100 = EntitledAfterWarmup(10, cfg);

            Assert.AreEqual(90, p10, "阈值以下的保护带路应得间隔=周期×0.9（一帧不丢）");
            Assert.AreEqual(63, p16, "16.7fps 的中带降幅应≈4%");
            Assert.AreEqual(36, p33, "33.3fps 的中带降幅应≈15%");
            Assert.AreEqual(40, p50, "50fps 落在高频带 ⇒ 降幅上限 50%");
            Assert.AreEqual(20, p100, "100fps 高频带 ⇒ 降幅上限 50%");

            double r10 = Math.Min(1.0, 100.0 / p10);
            double r16 = Math.Min(1.0, 60.0 / p16);
            double r33 = Math.Min(1.0, 30.0 / p33);
            double r50 = Math.Min(1.0, 20.0 / p50);
            Assert.IsTrue(r10 >= r16 && r16 >= r33 && r33 >= r50,
                "「频率越高降得越多」不成立，显示率依次为 " + r10 + " / " + r16 + " / " + r33 + " / " + r50);

            // 中带（<50Hz）降幅不超 25% 上限；高带不超 50% 上限（取整余量放 5%）
            Assert.IsTrue(r16 >= 0.70 && r33 >= 0.70,
                "中带显示率跌破 25% 降幅上限：" + r16 + " / " + r33);
            Assert.IsTrue(r50 > 0.45 && r50 < 0.55, "高带显示率应≈50%，实测 " + r50);

            // 只减不加：非保护路的应得间隔绝不会短于自己的周期（不可能"比满显还快"）
            Assert.IsTrue(p16 >= 60 && p33 >= 30 && p50 >= 20 && p100 >= 10,
                "出现比满显还快的应得间隔，说明降幅口径被写成抬人");
        }

        [TestMethod]
        public void 权重为零时不启用主动降幅_保护带之外与第54轮同形()
        {
            // W=0 ⇒ 各路显示上限=需求本身 ⇒ 除了保护带先扣额度，分摊形状与第54轮一致：
            // 未被保护的路仍按同一个绝对水位被压（不是"不降频"，而是"不额外降"）。
            // 本场景 12 路全在阈值以上 ⇒ 保护带为空 ⇒ 稳态应逐路对齐第54轮。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));
            int[] periods = BuildPeriods(0, 0, 12, 33);          // 12 路 30fps，全部在阈值以上
            int[] wins = Drive(rb, periods, DurationMs);
            int min = int.MaxValue, max = 0;
            for (int i = 0; i < RenderBudget.Slots; i++)
            {
                if (wins[i] < min) min = wins[i];
                if (wins[i] > max) max = wins[i];
            }
            Assert.IsTrue(min > 0, "W=0 时出现完全不刷新的路：min=" + min);
            Assert.IsTrue(max - min <= max / 5, "同需求的路显示率不齐：max=" + max + " min=" + min);
            for (int i = 0; i < RenderBudget.Slots; i++)
                Assert.AreEqual(0, rb.IsPathProtected(i, DurationMs - 1, Interval),
                    "相机" + (i + 1) + "（30fps）高于阈值 10Hz 却进了保护带");

            // 稳态逐路对齐第54轮。实测 [2000,6000)：两代总量 241 vs 243，各路差 ≤1 帧。
            // 启动瞬态**不在**这条承诺里（全窗口 384 vs 375）：本轮不再把"无周期样本"的路按
            // 100fps 记账（见 尚无周期样本的路不占分母也不被分摊限速），代价是第一笔样本到位前
            // 已测得的路可以把闲着的额度用满——一次性、不随时间累积。
            var legacy = new RenderBudget();
            int[] a = DriveFrom(legacy, periods, WarmupMs, DurationMs);
            var rbSteady = new RenderBudget();
            rbSteady.ApplyConfig(Cfg(10, 0));
            int[] b = DriveFrom(rbSteady, periods, WarmupMs, DurationMs);
            for (int i = 0; i < RenderBudget.Slots; i++)
                Assert.IsTrue(System.Math.Abs(a[i] - b[i]) <= 1,
                    "相机" + (i + 1) + " 稳态显示量与第54轮不同形：" + a[i] + " vs " + b[i]);

            // 只读诊断：无账时不该回报"封顶已用满"（两份全新预算，两代口径同值）
            Assert.AreEqual(0, new RenderBudget().CapUsedPercent(0, Interval),
                "第54轮口径无账时封顶占用应为 0");
            var freshNew = new RenderBudget();
            freshNew.ApplyConfig(Cfg(10, 0));
            Assert.AreEqual(0, freshNew.CapUsedPercent(0, Interval),
                "新口径无账时封顶占用应为 0");
        }

        [TestMethod]
        public void 阈值与权重同时为零时逐帧等价第54轮_回退基线()
        {
            // 现场总开关：ProtectHz=0 且 Weight=0 必须走第54轮原分支，逐帧一致（不是"近似一致"）。
            int[][] scenarios =
            {
                BuildPeriods(12, 33, 0, 0),
                BuildPeriods(0, 0, 12, 333),
                BuildPeriods(2, 33, 10, 333),
                BuildPeriods(1, 33, 10, 1000),
                BuildPeriods(5, 125, 7, 33),
            };
            DisplayThrottleSettings off;
            int fixedCount = DisplayThrottleSettings.Normalize(0, 0, 50, 50, 25, out off);
            Assert.AreEqual(0, fixedCount, "这组参数本就该在合法区间内");
            Assert.IsTrue(off.IsLegacy, "阈值与权重同时为 0 必须判为第54轮口径");

            for (int s = 0; s < scenarios.Length; s++)
            {
                var baseline = new RenderBudget();                       // 不注入任何参数
                var explicitOff = new RenderBudget();
                explicitOff.ApplyConfig(off);
                int[] a = Drive(baseline, scenarios[s], DurationMs);
                int[] b = Drive(explicitOff, scenarios[s], DurationMs);
                for (int i = 0; i < RenderBudget.Slots; i++)
                    Assert.AreEqual(a[i], b[i],
                        "场景" + s + " 相机" + (i + 1) + " 在「阈值0权重0」下与第54轮不等价");
            }
        }

        [TestMethod]
        public void 权重拉满也不越过封顶_多场景总量校验()
        {
            // 只减不加（显示上限 = 需求×(1−降幅)，水位 λ 只会往下压）在**稳态**成立。
            // 断言用三个稳态窗口起点（1000/2000/3000ms）分别核一次：越靠后越能证明
            // "超过封顶的那部分是启动瞬态、不随时间累积"。
            //
            // 瞬态要如实记下来：全窗口（从 0 起算）本组场景最多的是「2 路 30fps + 10 路 3fps」
            // 那条，6 秒 393 帧 > 封顶 375 帧。机理是本轮的需求口径——只到过一帧的路按"未知"
            // 处理、不预约额度（第54轮把它按 100fps 记账，反而虚占封顶），所以在它交出第一个
            // 周期样本之前，已测得的路可以把那份闲额度用满。瞬态长度=最慢那路的一个周期，
            // 一次性；同一场景的稳态窗口实测 248/250 帧 ≤ 上界 324/262 帧。
            int[][] scenarios =
            {
                BuildPeriods(12, 33, 0, 0),
                BuildPeriods(0, 0, 12, 333),
                BuildPeriods(2, 33, 10, 333),
                BuildPeriods(5, 125, 7, 33),
                BuildPeriods(1, 10, 11, 1000),
                BuildPeriods(12, 100, 0, 0),
            };
            int[] starts = { 1000, WarmupMs, 3000 };
            for (int s = 0; s < scenarios.Length; s++)
            {
                for (int k = 0; k < starts.Length; k++)
                {
                    var rb = new RenderBudget();
                    rb.ApplyConfig(Cfg(10, 100));
                    int total = Total(DriveFrom(rb, scenarios[s], starts[k], DurationMs));
                    int cap = SteadyCapFrames(starts[k]);
                    Assert.IsTrue(total <= cap,
                        "场景" + s + " 权重拉满后稳态窗口[" + starts[k] + "," + DurationMs
                        + ") 总放行 " + total + " 越过上界 " + cap
                        + "（主动降幅只减不加，越顶就是实现走样）");
                }
            }
        }

        [TestMethod]
        public void 保护带总额超过封顶时剥夺保护而不越顶()
        {
            // 12 路 10fps（100ms）＝需求 120fps，保护带本身放不下 ⇒ 必须剥夺，而不是把封顶打穿。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));
            int[] wins = Drive(rb, BuildPeriods(12, 100, 0, 0), DurationMs);
            Assert.IsTrue(Total(wins) <= DurationMs / Interval + RenderBudget.Slots,
                "保护带放不下时总放行越过封顶：" + Total(wins));
            Assert.AreEqual(1, rb.ProtectionDemoted(DurationMs - 1, Interval),
                "12 路 10fps 的保护需求 120fps 远超封顶，必须回报「有路被剥夺保护」，"
                + "否则弹窗会说谎（一帧不丢的前提是封顶放得下）");
            int min = int.MaxValue;
            for (int i = 0; i < RenderBudget.Slots; i++) if (wins[i] < min) min = wins[i];
            Assert.IsTrue(min > 0, "剥夺保护之后仍有路完全不刷新：min=" + min);
        }

        [TestMethod]
        public void 脏参数一律回默认并回报被修正的项数()
        {
            DisplayThrottleSettings s;
            // 五项全部越界：阈值>1000、权重>100、分界<2、两个降幅>90/为负
            int fixedCount = DisplayThrottleSettings.Normalize(1001, 101, 1, 91, -1, out s);
            Assert.AreEqual(5, fixedCount, "五项越界应全部回报，实测 " + fixedCount);
            Assert.AreEqual(DisplayThrottleSettings.ProtectHzDefault, s.ProtectHz);
            Assert.AreEqual(DisplayThrottleSettings.WeightDefault, s.Weight);
            Assert.AreEqual(DisplayThrottleSettings.HighBandHzDefault, s.HighBandHz);
            Assert.AreEqual(DisplayThrottleSettings.HighMaxCutPercentDefault, s.HighMaxCutPercent);
            Assert.AreEqual(DisplayThrottleSettings.MidMaxCutPercentDefault, s.MidMaxCutPercent);
            Assert.IsFalse(s.IsLegacy, "回默认后阈值=10Hz 仍是保护带口径（默认值不启用主动降幅，保护带照旧从快路那份封顶里出）");

            DisplayThrottleSettings ok;
            Assert.AreEqual(0, DisplayThrottleSettings.Normalize(0, 0, 2, 0, 0, out ok),
                "边界内的极值不该被回报为脏值");
        }

        [TestMethod]
        public void 预算闲置时保护带不产生任何限速()
        {
            // 阈值 50Hz、W=0，场景全是 3fps 的路（总需求 36fps < 封顶 62.5）：
            // 谁都喂得饱 ⇒ 与第54轮逐路一致，且一帧不丢。
            var legacy = new RenderBudget();
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(50, 0));
            int[] periods = BuildPeriods(0, 0, 12, 333);
            int[] a = Drive(legacy, periods, DurationMs);
            int[] b = Drive(rb, periods, DurationMs);
            for (int i = 0; i < RenderBudget.Slots; i++)
            {
                Assert.AreEqual(RequestCount(333), a[i], "第54轮基准：预算闲置时慢路一帧不丢");
                Assert.AreEqual(a[i], b[i], "相机" + (i + 1) + " 在预算闲置时被保护带口径丢了帧");
            }
            Assert.AreEqual(0, rb.PressureCode(DurationMs - 1, Interval), "总需求低于封顶 ⇒ 压力档应为 0");
        }

        [TestMethod]
        public void 阈值高于高频带分界属配置写反_一律按高频带降幅()
        {
            // ProtectHz=60 而 HighBand=50：斜坡没有起点（span ≤ 0）。此时对未保护的路一律按高频带
            // 降幅处理，绝不出现"配了阈值却既没保护也不降频"或"除以负数"的静默走样。
            // 取 12ms（≈83fps）：既不在保护带（> 60×1.25=75fps）又落在高带 ⇒ 显示上限 41.6fps。
            var cfg = new DisplayThrottleSettings(60, 100, 50, 50, 25);
            int entitled = EntitledAfterWarmup(12, cfg);
            Assert.AreEqual(25, entitled,
                "阈值≥分界时应按高频带 50% 降幅（应得≈2 个周期），实测 " + entitled + "ms");
        }

        [TestMethod]
        public void 帧率变慢的跳变一次采信_小幅变化走平滑()
        {
            // 30fps 切 3fps 若仍按 3:1 EMA 收敛，会长时间按 30fps 记账白占封顶把别人挤死。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));
            int[] one = new int[RenderBudget.Slots];
            one[0] = 33;
            Drive(rb, one, 2000);                      // 最后一帧 1980
            Assert.AreEqual(33, rb.MeasuredPeriodMs(0), "30fps 稳态周期估计应=33ms");

            // 之后每 300ms 一帧（1980+300=2280 起，保证第一笔间隔就是 300 而不是过渡值）
            for (int t = 2280; t < 4000; t++)
                if ((t - 2280) % 300 == 0) rb.TryClaim(0, t, Interval);
            Assert.AreEqual(300, rb.MeasuredPeriodMs(0),
                "帧率变慢的跳变没有被一次采信，实测周期估计 " + rb.MeasuredPeriodMs(0) + "ms");

            // 小幅变化（33→40，1.2 倍 < 1.5 倍阈值）走 EMA：既不跳到 40 也不原地不动
            var smooth = new RenderBudget();
            smooth.ApplyConfig(Cfg(10, 0));
            int[] q = new int[RenderBudget.Slots];
            q[0] = 33;
            Drive(smooth, q, 2000);
            int[] jitter = { 2013, 2053, 2093 };        // 相对上一笔 1980 的间隔依次为 33/40/40 的到达点
            foreach (int t in jitter) smooth.TryClaim(0, t, Interval);
            int p = smooth.MeasuredPeriodMs(0);
            Assert.IsTrue(p > 33 && p < 40,
                "小幅抖动应走 EMA 平滑（33<周期<40），实测 " + p + "ms");
        }

        [TestMethod]
        public void 断流超过普查窗口后周期作废重采样()
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));
            int[] one = new int[RenderBudget.Slots];
            one[0] = 33;
            Drive(rb, one, 2000);
            Assert.AreEqual(33, rb.MeasuredPeriodMs(0));

            // 停流 2 秒以上后以 1000ms（1fps）复流：必须作废旧估计重新采样。
            // （原实现是"这一笔样本跳过"，旧值永久残留 ⇒ 一路会按 30fps 记账白占封顶。）
            for (int t = 4500; t < 7000; t++)
                if ((t - 4500) % 1000 == 0) rb.TryClaim(0, t, Interval);
            Assert.AreEqual(1000, rb.MeasuredPeriodMs(0),
                "断流复流之后周期估计仍是陈旧值：" + rb.MeasuredPeriodMs(0) + "ms");
            Assert.AreEqual(1, rb.MeasuredFps(0, 6999), "实测帧率应重采样为 1fps");
            Assert.AreEqual(1, rb.IsPathProtected(0, 6999, Interval), "1fps 应重新进保护带");
        }

        [TestMethod]
        public void 尚无周期样本的路不占分母也不被分摊限速()
        {
            // 11 路 30fps + 1 路只到过一帧（没有周期样本）。
            // 该路既不该进分母（别人的份额不被它压），也不该被分摊限速（只受全局最小间隔约束）。
            // 对照：第12路从未到达时，前 11 路的应得间隔必须一字不差——这就是"不计入分母"。
            var withUnknown = new RenderBudget();
            withUnknown.ApplyConfig(Cfg(10, 0));
            var control = new RenderBudget();
            control.ApplyConfig(Cfg(10, 0));
            for (int t = 0; t < 2000; t += 33)
            {
                for (int i = 0; i < 11; i++) withUnknown.TryClaim(i, t, Interval);
                for (int i = 0; i < 11; i++) control.TryClaim(i, t, Interval);
            }
            withUnknown.TryClaim(11, 0, Interval);          // 只到一帧，之后再不到

            Assert.AreEqual(Interval, withUnknown.EntitledMs(11, 1999, Interval),
                "无周期样本的路应只受全局最小间隔约束，实测 "
                + withUnknown.EntitledMs(11, 1999, Interval) + "ms");
            Assert.AreEqual(control.EntitledMs(0, 1999, Interval), withUnknown.EntitledMs(0, 1999, Interval),
                "已到帧但无样本的路占了分母，把别人份额压下去了");
        }

        [TestMethod]
        public void 换触发模式清空帧率账_陈旧需求不再占封顶()
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 0));
            int[] one = new int[RenderBudget.Slots];
            one[0] = 33;
            Drive(rb, one, 2000);
            Assert.AreEqual(33, rb.MeasuredPeriodMs(0));
            Assert.AreEqual(1, rb.ResetPathAccounting(0), "复位应回报生效");
            Assert.AreEqual(0, rb.MeasuredPeriodMs(0), "复位后周期估计应清空");
            Assert.AreEqual(0, rb.IsPathProtected(0, 1999, Interval), "复位后保护带标记应清空");
            Assert.AreEqual(0, rb.ResetPathAccounting(-1), "非法槽位不得回报生效");
            Assert.AreEqual(0, rb.ResetPathAccounting(RenderBudget.Slots), "非法槽位不得回报生效");
        }

        [TestMethod]
        public void 只读诊断不改变裁决()
        {
            // 弹窗/日志会穿插读诊断（实测、水位、压力、封顶占用、剥夺、保护带、应得间隔）。
            // 同一到达序列，穿插读与不穿插读，各路放行结果必须逐路一致，否则诊断本身就是副作用。
            int[] periods = BuildPeriods(5, 125, 7, 33);
            var plain = new RenderBudget();
            plain.ApplyConfig(Cfg(10, 100));
            var probed = new RenderBudget();
            probed.ApplyConfig(Cfg(10, 100));

            for (int t = 0; t < DurationMs; t++)
            {
                for (int i = 0; i < RenderBudget.Slots; i++)
                {
                    if (periods[i] <= 0 || t % periods[i] != 0) continue;
                    plain.TryClaim(i, t, Interval);
                }
                if (t % 500 == 0)
                {
                    probed.ActivePaths(t);
                    probed.WaterLevelFps(t, Interval);
                    probed.PressureCode(t, Interval);
                    probed.CapUsedPercent(t, Interval);
                    probed.ProtectionDemoted(t, Interval);
                    probed.MeasuredFps(0, t);
                    probed.MeasuredPeriodMs(0);
                    probed.IsPathProtected(0, t, Interval);
                    probed.EntitledMs(0, t, Interval);
                    probed.GrantTotal(0);
                    probed.GrantedPerSec(0);
                    probed.ShareMilliFps(t, Interval);
                }
                for (int i = 0; i < RenderBudget.Slots; i++)
                {
                    if (periods[i] <= 0 || t % periods[i] != 0) continue;
                    probed.TryClaim(i, t, Interval);
                }
            }
            for (int i = 0; i < RenderBudget.Slots; i++)
                Assert.AreEqual(plain.GrantTotal(i), probed.GrantTotal(i),
                    "相机" + (i + 1) + " 的裁决被只读诊断改变了：诊断接口不再安全");
        }

        [TestMethod]
        public void 参数换引用是原子的_重复注入回报无变化()
        {
            // ApplyConfig 只换一个不可变对象的引用：检测线程要么整份看到旧参数、要么整份看到新参数，
            // 不会看到"阈值已改、权重还没改"的半套组合。
            var rb = new RenderBudget();
            DisplayThrottleSettings a;
            DisplayThrottleSettings.Normalize(10, 40, 50, 50, 25, out a);
            Assert.AreEqual(1, rb.ApplyConfig(a), "首次注入应回报生效");
            Assert.AreEqual(0, rb.ApplyConfig(a), "同一份参数重复注入不应回报变化");
            Assert.AreEqual(0, rb.ApplyConfig(null), "null 一律忽略");
            DisplayThrottleSettings b;
            DisplayThrottleSettings.Normalize(10, 41, 50, 50, 25, out b);
            Assert.AreEqual(1, rb.ApplyConfig(b), "内容不同应回报生效");
        }
    }
}
