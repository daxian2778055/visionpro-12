using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第56轮 P5：停止/暂停态的**末帧一次性旁路**单测。
    ///
    /// 现场问题：操作员按停止后画面定住，定在哪一帧取决于最后一次的裁决运气——被限速挡掉的那一帧
    /// 已经跑完检测、像素还在手上，再不画就永远没机会（后面没有新帧了）。
    ///
    /// 要钉住的不是"能补显"这一件好事，而是它**不失控**的四条边界：
    ///   ① 运行态一帧都不补（否则限速整个被绕过，正是本轮 P0/P1 防的性能反转）；
    ///   ② 每路每次停止只补一帧（额度 CAS 消费，重新运行才复位）；
    ///   ③ 本来就要显示的帧不消耗额度（否则补到的是"停止后第一帧"而不是"第一个被挡的帧"）；
    ///   ④ 补显不动分摊到期表（旁路是显示层的额外一帧，不该把别人下一帧的时刻往前挪）。
    /// 另有一条**不写成本类承诺**的边界放在注释里：接收前就被丢弃的帧（取图失败、队列溢出、
    /// 停止排空）根本没有像素可画，任何显示层手段都救不回来——所以本口径不能宣称"保证显示最后一帧"。
    /// </summary>
    [TestClass]
    public class RenderTailFrameTests
    {
        private const int Interval = 16;

        private static DisplayThrottleSettings Cfg(int protectHz, int weight)
        {
            // 最大降幅取默认 50%；高频带分界(50Hz)与中带降幅(一半)第57轮起是派生值，与旧 ini 默认同值
            return new DisplayThrottleSettings(protectHz, weight, DisplayThrottleSettings.MaxCutPercentDefault);
        }

        [TestMethod]
        public void 运行态一帧都不补显_稳态零成本()
        {
            // 一路 80Hz 的快路在被主动降幅压着跑（大量帧本来就被挡），补显口子必须一次都不开：
            // 否则"每帧都能破例"=限速失效，本轮要治的负荷反转就从这里回来。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            int rejected = 0;
            for (int t = 0; t < 6000; t += 12)
            {
                if (!rb.TryClaim(0, t, Interval))
                {
                    rejected++;
                    Assert.IsFalse(rb.TryClaimTailFrame(0, t, true, true),
                        "运行态的帧被允许补显，t=" + t);
                }
            }
            Assert.IsTrue(rejected > 20, "前置条件不成立：这段场景根本没产生被挡的帧（" + rejected + "）");
            Assert.AreEqual(0, rb.TailFlushTotal(0), "运行态不该留下任何补显账");
        }

        [TestMethod]
        public void 停止后第一个被挡的帧补显一次_之后不再补()
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            Assert.IsTrue(rb.TryClaimTailFrame(3, 1000, false, true), "停止后第一个被挡的帧应补显");
            Assert.IsFalse(rb.TryClaimTailFrame(3, 1010, false, true), "同一次停止不得补第二帧");
            Assert.IsFalse(rb.TryClaimTailFrame(3, 1020, false, true), "额度用完后一直拒绝到重新运行为止");
            Assert.AreEqual(1, rb.TailFlushTotal(3));
        }

        [TestMethod]
        public void 重新运行一帧就把额度还给下一次停止()
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            Assert.IsTrue(rb.TryClaimTailFrame(5, 1000, false, true), "第一次停止补显");
            Assert.IsFalse(rb.TryClaimTailFrame(5, 1010, false, true));

            // 运行态的那一帧只负责复位（自己不被补显），所以下一次停止仍有一次额度。
            Assert.IsFalse(rb.TryClaimTailFrame(5, 1020, true, true), "运行态不得走补显");
            Assert.IsTrue(rb.TryClaimTailFrame(5, 1030, false, true), "复位后下一次停止应还能补显");
            Assert.AreEqual(2, rb.TailFlushTotal(5));
        }

        [TestMethod]
        public void 本来就要显示的帧不消耗补显额度()
        {
            // 停止态里份额裁决仍可能放行（份额空了、或该路本来没被压）。那种帧不需要额度，
            // 也不能把额度吃掉——否则真正被挡的末帧就没得补了。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            Assert.IsFalse(rb.TryClaimTailFrame(7, 1000, false, false), "未被挡的帧不该回报补显");
            Assert.IsTrue(rb.TryClaimTailFrame(7, 1010, false, true), "额度还在，第一个被挡的帧能补显");
            Assert.AreEqual(1, rb.TailFlushTotal(7));
        }

        [TestMethod]
        public void 各路额度相互独立_最多十二路各补一帧()
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            for (int i = 0; i < RenderBudget.Slots; i++)
                Assert.IsTrue(rb.TryClaimTailFrame(i, 1000 + i, false, true), "相机" + (i + 1) + " 应各有一次额度");
            for (int i = 0; i < RenderBudget.Slots; i++)
            {
                Assert.AreEqual(1, rb.TailFlushTotal(i));
                Assert.IsFalse(rb.TryClaimTailFrame(i, 2000 + i, false, true), "每路上限就是 1 帧");
            }
        }

        [TestMethod]
        public void 补显不挪任何人的到期表()
        {
            // 对照组：同一段到达序列，一路正常裁决 + 各路补显一次；各路放行次数必须与"完全没有补显"
            // 的对照实例逐路相同——补显只多画那一帧，不改变任何人的应得节奏（否则就是绕过限速）。
            int[] periods = new int[RenderBudget.Slots];
            for (int i = 0; i < RenderBudget.Slots; i++) periods[i] = 33;
            var plain = new RenderBudget();
            plain.ApplyConfig(Cfg(10, 100));
            var flushed = new RenderBudget();
            flushed.ApplyConfig(Cfg(10, 100));

            int flushAt = -1;
            for (int t = 0; t < 6000 && flushAt < 0; t++)
                for (int i = 0; i < RenderBudget.Slots; i++)
                {
                    if (t % periods[i] != 0) continue;
                    bool a = plain.TryClaim(i, t, Interval);
                    bool b = flushed.TryClaim(i, t, Interval);
                    Assert.AreEqual(a, b, "相机" + (i + 1) + " 在 t=" + t + " 的份额裁决与对照组不一致");
                    // 在**第一个真正被挡的帧**上开一次补显（时刻不写死：写死的那一帧可能本来就被放行，
                    // 那样补显根本没发生过，后面的次数对照就成了假命题——本轮实测踩过一次）。
                    if (!b && i == 0)
                    {
                        Assert.IsTrue(flushed.TryClaimTailFrame(0, t, false, true), "停止末帧没补上");
                        flushAt = t;
                    }
                }
            Assert.IsTrue(flushAt >= 0, "前置条件不成立：整段场景一路都没有被挡的帧");

            // 补显之后的帧继续对照：份额裁决必须照旧逐帧一致（补显不动 _dueTick）。
            for (int t = flushAt + 1; t < 6000; t++)
                for (int i = 0; i < RenderBudget.Slots; i++)
                {
                    if (t % periods[i] != 0) continue;
                    Assert.AreEqual(plain.TryClaim(i, t, Interval), flushed.TryClaim(i, t, Interval),
                        "相机" + (i + 1) + " 在补显之后 t=" + t + " 的份额裁决被挪动了");
                }
            for (int i = 0; i < RenderBudget.Slots; i++)
                Assert.AreEqual(plain.GrantTotal(i) + (i == 0 ? 1 : 0), flushed.GrantTotal(i),
                    "相机" + (i + 1) + " 的累计显示次数与补显账对不上");
            Assert.AreEqual(1, flushed.TailFlushTotal(0));
        }

        [TestMethod]
        public void 非法槽位一律拒绝_不留账()
        {
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            Assert.IsFalse(rb.TryClaimTailFrame(-1, 1000, false, true));
            Assert.IsFalse(rb.TryClaimTailFrame(RenderBudget.Slots, 1000, false, true));
            Assert.AreEqual(0, rb.TailFlushTotal(-1));
            Assert.AreEqual(0, rb.TailFlushTotal(RenderBudget.Slots));
        }

        [TestMethod]
        public void 补显次数记进显示账_弹窗才看得见多出来的那一帧()
        {
            // GrantTotal 是"上一秒显示 N 次/累计 N 次"的数据源：补显那一帧真的渲染了，
            // 必须进这本账，否则弹窗报的显示次数比屏幕实际刷新的次数少 1，对账时反而更糊涂。
            var rb = new RenderBudget();
            rb.ApplyConfig(Cfg(10, 100));
            int before = rb.GrantTotal(2);
            Assert.IsTrue(rb.TryClaimTailFrame(2, 1000, false, true));
            Assert.AreEqual(before + 1, rb.GrantTotal(2));
            Assert.AreEqual(1, rb.TailFlushTotal(2));
        }
    }
}
