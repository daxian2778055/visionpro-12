using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第54轮：显示渲染预算分摊单测——钉的是"分配会不会偏心"，不是速率数字。
    ///
    /// 现场投诉：高频下一幅画面"总是盯着某一个相机刷新"，其余相机一卡一卡。
    /// 旧口径（全局单一刻度先到先赢 + 被拒满 40 次放行一次）会锁相：与赢家同相的路**每帧都被拒**，
    /// 而且逃生阀计的是次数不是时间，低频路要等满 40 个自己的帧才轮到一次。
    /// 本文件用同一份到达序列跑三种口径（旧法 / 均分 / 按需求解），把结论钉成断言：
    ///   ① 预算没用完时旧法照样饿死路（这是 bug，不是"设计如此"）；
    ///   ② 低频路（1~3fps 的通讯/软触发路）一帧不丢——这是"不能影响感官"的硬保证；
    ///   ③ 快路不得被摊薄到闲置预算以下（现场明确否掉了简单均分）；
    ///   ④ 总放行量始终 ≤ 全局上限（"降频分摊"不能变成"多烧 CPU"）；
    ///   ⑤ 分摊只决定显不显示，与检测/计数无关（本类根本没有别的出口）。
    /// 模拟全部按整数毫秒确定性驱动（不用真实时钟、不起线程池压力），可复现可检索。
    /// </summary>
    [TestClass]
    public class RenderBudgetTests
    {
        private const int Interval = 16;          // 默认全局最小间隔（code.ini [Display]RenderMinIntervalMs）
        private const int DurationMs = 6000;      // 模拟时长

        #region 对照实现（仅测试内使用，钉住"旧口径确实偏心""均分确实摊薄快路"）

        /// <summary>旧口径逐行复刻（删除前 Form1.TryClaimRenderBudget 的全局限速部分）。</summary>
        private sealed class OldGate
        {
            private int _lastRenderStartTick;
            private readonly int[] _renderStarve = new int[RenderBudget.Slots];

            public bool TryClaim(int slot, int now, int interval)
            {
                if (interval > 0
                    && unchecked(now - Volatile.Read(ref _lastRenderStartTick)) < interval
                    && Volatile.Read(ref _renderStarve[slot]) < 40)
                {
                    Volatile.Write(ref _renderStarve[slot], Volatile.Read(ref _renderStarve[slot]) + 1);
                    return false;
                }
                Volatile.Write(ref _renderStarve[slot], 0);
                Volatile.Write(ref _lastRenderStartTick, now);
                return true;
            }
        }

        /// <summary>简单均分口径（每路 1/活跃路数）——现场已否掉，留作对照。</summary>
        private sealed class FlatSplit
        {
            private readonly int[] _dueTick = Init();
            private readonly int[] _requestTick = Init();

            private static int[] Init()
            {
                int[] a = new int[RenderBudget.Slots];
                for (int i = 0; i < RenderBudget.Slots; i++) a[i] = int.MinValue / 4;
                return a;
            }

            private int ActivePaths(int now)
            {
                int n = 0;
                for (int i = 0; i < RenderBudget.Slots; i++)
                    if (unchecked(now - Volatile.Read(ref _requestTick[i])) <= RenderBudget.ActivityWindowMs) n++;
                return n < 1 ? 1 : n;
            }

            public bool TryClaim(int slot, int now, int interval)
            {
                if (slot < 0 || slot >= RenderBudget.Slots) return false;
                Volatile.Write(ref _requestTick[slot], now);
                if (interval <= 0) return true;
                int fair = interval * ActivePaths(now);
                int due = Volatile.Read(ref _dueTick[slot]);
                if (unchecked(now - due) < 0) return false;
                int next = unchecked(due + fair + slot);
                if (unchecked(next - now) <= 0) next = unchecked(now + fair + slot);
                if (Interlocked.CompareExchange(ref _dueTick[slot], next, due) != due) return false;
                return true;
            }
        }

        #endregion

        #region 模拟驱动

        /// <summary>periodsMs[i] &lt;= 0 表示该路完全停流（一帧都不到达）；到达时刻 t = 0, p, 2p…（同相最坏场景）。</summary>
        private static int RequestCount(int periodMs)
        {
            return periodMs <= 0 ? 0 : (DurationMs + periodMs - 1) / periodMs;
        }

        private static int Total(int[] wins)
        {
            int t = 0;
            for (int i = 0; i < wins.Length; i++) t += wins[i];
            return t;
        }

        private static int[] Simulate(Func<int, int, int, bool> claim, int[] periodsMs, int durationMs, int interval)
        {
            int[] wins = new int[RenderBudget.Slots];
            for (int t = 0; t < durationMs; t++)
                for (int i = 0; i < RenderBudget.Slots; i++)
                {
                    if (periodsMs[i] <= 0 || t % periodsMs[i] != 0) continue;
                    if (claim(i, t, interval)) wins[i]++;
                }
            return wins;
        }

        private static int[] Simulate(RenderBudget rb, int[] periodsMs, int durationMs, int interval)
        {
            return Simulate((s, t, iv) => rb.TryClaim(s, t, iv), periodsMs, durationMs, interval);
        }

        private static int[] Simulate(FlatSplit gate, int[] periodsMs, int durationMs, int interval)
        {
            return Simulate((s, t, iv) => gate.TryClaim(s, t, iv), periodsMs, durationMs, interval);
        }

        private static int[] Simulate(OldGate gate, int[] periodsMs, int durationMs, int interval)
        {
            return Simulate((s, t, iv) => gate.TryClaim(s, t, iv), periodsMs, durationMs, interval);
        }

        private static int[] Periods(int fastCount, int fastMs, int slowCount, int slowMs)
        {
            int[] a = new int[RenderBudget.Slots];   // 0 = 停流
            for (int i = 0; i < fastCount; i++) a[i] = fastMs;
            for (int i = fastCount; i < fastCount + slowCount; i++) a[i] = slowMs;
            return a;
        }

        #endregion

        [TestMethod]
        public void 空表首帧一律放行_不需要首次标志()
        {
            var rb = new RenderBudget();
            for (int i = 0; i < RenderBudget.Slots; i++)
                Assert.IsTrue(rb.TryClaim(i, 1000, Interval), "相机" + (i + 1) + " 首帧被限速拒了");
        }

        [TestMethod]
        public void 预算没用完时旧法仍饿死路_这就是现场投诉的根()
        {
            // 12 路 × 3fps 同相：总需求 36 次/秒，全局上限 62.5 次/秒——预算根本没用完。
            var old = new OldGate();
            int[] wins = Simulate(old, Periods(0, 0, 12, 333), DurationMs, Interval);

            int zero = 0;
            for (int i = 0; i < wins.Length; i++) if (wins[i] == 0) zero++;
            Assert.IsTrue(zero >= 10,
                "旧口径本应暴露「同相饿死」，实测只有 " + zero + " 路饿死——对照实现走样了，断言失去意义");
            Assert.IsTrue(Total(wins) < 12 * RequestCount(333) / 3,
                "旧口径总放行 " + Total(wins) + " 远低于需求，说明是分配而非上限问题");
        }

        [TestMethod]
        public void 预算没用完时新口径低频路一帧不丢()
        {
            var rb = new RenderBudget();
            int[] wins = Simulate(rb, Periods(0, 0, 12, 333), DurationMs, Interval);
            int need = RequestCount(333);
            for (int i = 0; i < RenderBudget.Slots; i++)
                Assert.AreEqual(need, wins[i], "相机" + (i + 1) + " 在预算闲置时仍被丢帧");
        }

        [TestMethod]
        public void 超载时各路显示率齐平_不再有独赢路()
        {
            // 12 路 × 30fps：需求 364 次/秒，远超上限 ⇒ 必然要丢帧，但丢帧必须平摊。
            var rb = new RenderBudget();
            int[] wins = Simulate(rb, Periods(12, 33, 0, 0), DurationMs, Interval);
            int min = int.MaxValue, max = 0;
            for (int i = 0; i < wins.Length; i++)
            {
                if (wins[i] < min) min = wins[i];
                if (wins[i] > max) max = wins[i];
            }
            Assert.IsTrue(min > 0, "超载时出现完全不刷新的路：max=" + max + " min=" + min);
            Assert.IsTrue(max - min <= max / 5,
                "各路显示率极差过大 max=" + max + " min=" + min + "（应 ≤20%）");
        }

        [TestMethod]
        public void 快路不被摊薄_闲置份额让给高频路()
        {
            // 现场修正：2 路 30fps + 10 路 3fps。均分给每路 1/12 ⇒ 快路只剩 17% 显示率，
            // 而慢路根本用不完自己的份额（闲置）——CPU 上限没动，纯粹是分配浪费。
            int[] periods = Periods(2, 33, 10, 333);
            var flat = new FlatSplit();
            int[] flatWins = Simulate(flat, periods, DurationMs, Interval);

            var rb = new RenderBudget();
            int[] wfWins = Simulate(rb, periods, DurationMs, Interval);

            for (int i = 2; i < RenderBudget.Slots; i++)
            {
                Assert.AreEqual(RequestCount(333), flatWins[i], "均分口径下慢路本应一帧不丢");
                Assert.AreEqual(RequestCount(333), wfWins[i], "按需求解口径丢了慢路帧：相机" + (i + 1));
            }
            Assert.IsTrue(wfWins[0] > flatWins[0] * 2,
                "快路未拿到闲置份额：按需求解 " + wfWins[0] + " 次 vs 均分 " + flatWins[0] + " 次");
            Assert.IsTrue(Total(wfWins) > Total(flatWins),
                "总量没有提升说明闲置预算仍在浪费：按需求解 " + Total(wfWins) + " vs 均分 " + Total(flatWins));
        }

        [TestMethod]
        public void 总放行量不超全局上限()
        {
            // 上限口径：DurationMs/Interval 次（外加每路首帧那一次齐发，最多 Slots 次）。
            int[][] scenarios =
            {
                Periods(12, 33, 0, 0),
                Periods(0, 0, 12, 333),
                Periods(2, 33, 10, 333),
                Periods(1, 33, 10, 1000),
                Periods(6, 33, 6, 333),
            };
            int cap = DurationMs / Interval + RenderBudget.Slots;
            for (int s = 0; s < scenarios.Length; s++)
            {
                var rb = new RenderBudget();
                int total = Total(Simulate(rb, scenarios[s], DurationMs, Interval));
                Assert.IsTrue(total <= cap,
                    "场景" + s + " 总放行 " + total + " 超过上限 " + cap + "（分摊不能变成多烧 CPU）");
            }
        }

        [TestMethod]
        public void 喂饱路应得间隔等于自身周期_贪心路按剩余预算分摊()
        {
            var rb = new RenderBudget();
            int[] periods = Periods(2, 33, 10, 333);
            // 先跑 4 秒让各路周期估计收敛，再静态读应得间隔
            Simulate(rb, periods, 4000, Interval);
            int slow = rb.EntitledMs(5, 4000, Interval);
            int fast = rb.EntitledMs(0, 4000, Interval);
            Assert.IsTrue(slow >= 300 && slow <= 333,
                "慢路应得间隔应≈自身周期（留抖动余量），实测 " + slow + "ms ⇒ 会被自家帧率卡住就是丢帧");
            Assert.IsTrue(fast > Interval && fast < slow,
                "快路应得间隔应落在（全局最小间隔, 慢路周期）之间，实测 " + fast + "ms");
            Assert.IsTrue(rb.ShareMilliFps(4000, Interval) <= 1000 * 1000 / Interval,
                "份额不得超过全局上限换算值");
        }

        [TestMethod]
        public void 停流两秒后让出份额_快路应得间隔随之收紧()
        {
            var rb = new RenderBudget();
            int[] all12 = Periods(12, 33, 0, 0);
            int[] onlyOne = Periods(1, 33, 0, 0);
            Simulate(rb, all12, 3000, Interval);
            int sharedDue = rb.EntitledMs(0, 2999, Interval);
            Assert.AreEqual(12, rb.ActivePaths(2999), "12 路都在流上，活跃路数应为 12");

            // 其余 11 路停流：t=3000..9000 只有相机 1 在到达
            for (int t = 3000; t < 9000; t++)
                if (t % onlyOne[0] == 0) rb.TryClaim(0, t, Interval);

            Assert.AreEqual(1, rb.ActivePaths(8999), "停流的路超过普查窗口后不得再占份额");
            int soloDue = rb.EntitledMs(0, 8999, Interval);
            Assert.IsTrue(soloDue < sharedDue,
                "独占预算后应得间隔没收紧：分摊前 " + sharedDue + "ms，独占后 " + soloDue + "ms");

            int wins = 0;
            for (int t = 9000; t < 12000; t++)
                if (t % 33 == 0 && rb.TryClaim(0, t, Interval)) wins++;
            Assert.IsTrue(wins >= (12000 - 9000) / 33 - 2,
                "独占期单路应接近满帧显示，实测 " + wins + " 次/3000ms");
        }

        [TestMethod]
        public void 同一路并发申请只有一个赢家()
        {
            var rb = new RenderBudget();
            const int Rounds = 200;
            for (int r = 0; r < Rounds; r++)
            {
                int now = 50000 + r * 1000;          // 每轮都跨过上一轮的到期时刻，保证"确实有得抢"
                int winners = 0;
                int ready = 0;
                using (var start = new ManualResetEventSlim(false))
                using (var done = new ManualResetEventSlim(false))
                {
                    int threads = 8;
                    int finished = 0;
                    for (int k = 0; k < threads; k++)
                    {
                        var t = new Thread(() =>
                        {
                            Interlocked.Increment(ref ready);
                            start.Wait();
                            if (rb.TryClaim(3, now, Interval)) Interlocked.Increment(ref winners);
                            if (Interlocked.Increment(ref finished) == threads) done.Set();
                        });
                        t.IsBackground = true;
                        t.Start();
                    }
                    SpinWait.SpinUntil(() => ready == threads, 2000);
                    start.Set();
                    Assert.IsTrue(done.Wait(5000), "并发线程未在 5 秒内收敛");
                }
                Assert.AreEqual(1, winners, "第 " + r + " 轮同路并发赢家数不是 1（到期表 CAS 失效）");
            }
        }

        [TestMethod]
        public void 非法槽位拒绝且不污染任何账()
        {
            var rb = new RenderBudget();
            Assert.IsFalse(rb.TryClaim(-1, 7000, Interval));
            Assert.IsFalse(rb.TryClaim(RenderBudget.Slots, 7000, Interval));
            Assert.IsFalse(rb.TryClaim(9999, 7000, Interval));
            Assert.AreEqual(0, rb.ActivePaths(7000), "非法槽位不得登记请求");

            // 非法调用后，合法槽位的首帧仍必须放行（没被"借走"额度）
            Assert.IsTrue(rb.TryClaim(0, 7000, Interval), "非法槽位污染了合法路的到期表");
        }

        [TestMethod]
        public void Interval为零表示不限速_恒放行()
        {
            var rb = new RenderBudget();
            for (int k = 0; k < 50; k++)
                Assert.IsTrue(rb.TryClaim(2, 9000, 0), "不限速时第 " + k + " 次被拒");
            Assert.AreEqual(0, rb.EntitledMs(2, 9000, 0), "不限速的应得间隔应为 0");
        }

        [TestMethod]
        public void 任意启动时刻下回绕都自洽_不依赖哨兵时刻值()
        {
            // Environment.TickCount 约 25 天回绕一次。若用某个固定常量当"很久以前"，
            // 它在环形坐标上必然有半个周期位于"未来"——机器连续开机约 18.7~43.5 天这段启动的话，
            // 每路首帧都会被判"还没到期"且永不解锁（本类用"到达过/渲染过"标志规避）。
            int[] starts = { 0, 1000000000, int.MaxValue - 2000, int.MinValue + 1000 };
            for (int s = 0; s < starts.Length; s++)
            {
                var rb = new RenderBudget();
                int now = starts[s];
                int wins = 0, requests = 0;
                for (int k = 0; k < 200; k++)
                {
                    int t = unchecked(now + k * 33);
                    requests++;
                    if (rb.TryClaim(0, t, Interval)) wins++;
                }
                Assert.AreEqual(requests, wins,
                    "启动时刻=" + now + " 时单路满帧显示被打折：放行 " + wins + "/" + requests);
            }
        }

        [TestMethod]
        public void 调度器只做显示裁决_没有任何其它出口()
        {
            // RenderBudget 的全部 public 面就是"要不要渲染这一帧"与只读诊断，
            // 没有任何路径能把裁决结果回灌到检测/计数/存图——用签名清单钉住这条边界，
            // 免得将来有人往这里加"跳过检测"之类的口子（现场明确的情况1）。
            var members = typeof(RenderBudget).GetMembers(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static);
            foreach (System.Reflection.MemberInfo m in members)
            {
                if (m.DeclaringType != typeof(RenderBudget)) continue;   // object 自身成员不算
                if (m is System.Reflection.ConstructorInfo) continue;    // 隐式公共构造函数不参与裁决语义
                var mi = m as System.Reflection.MethodInfo;
                if (mi != null)
                {
                    if (mi.IsSpecialName) continue;                       // 属性/索引器访问器
                    Assert.IsTrue(mi.ReturnType == typeof(bool) || mi.ReturnType == typeof(int),
                        "公开方法返回类型超出「裁决 / 只读诊断」范围: " + mi.Name);
                    continue;
                }
                var fi = m as System.Reflection.FieldInfo;
                if (fi != null)
                {
                    Assert.IsTrue(fi.IsLiteral, "公开的可变字段不该存在: " + fi.Name);
                    continue;
                }
                Assert.Fail("RenderBudget 不得暴露方法/常量之外的公共成员: " + m.Name + "（" + m.MemberType + "）");
            }
        }
    }
}
