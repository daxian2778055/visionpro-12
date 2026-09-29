using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第53轮：帧账本单测——钉的是"归因会不会说谎"，不是数字对不对。
    ///
    /// 现场两个投诉（问题1 软触发没计数 / 问题2 我们计数比机台少）此前无法回答，
    /// 因为界面只有一个"漏帧 = 接收 - 检测"的差额，差额从哪来一个字不提。
    /// 账本把差额按原因拆开，但拆开本身可能骗人：
    ///   ① 漏记一条丢弃路径 → 差额对不上，若这时还说"已查全"就是谎报；
    ///   ② 同帧记两次 / 把接收前的丢弃算进对账 → 归因反而大于差额；
    ///   ③ 把"取图失败仍判 NG"这类备注念成丢帧 → 现场以为丢了 N 张，实际一张没少。
    /// 这四条都必须由单测钉住，因为线上没有任何别的手段能区分"真没丢"和"没查全"。
    /// </summary>
    [TestClass]
    public class FrameAccountingTests
    {
        [TestMethod]
        public void 记账按路隔离_槽位与原因非法一律静默忽略()
        {
            var acct = new FrameAccounting();
            acct.Note(3, FrameAccounting.Reason.MasterGate);
            acct.Note(3, FrameAccounting.Reason.MasterGate);
            acct.Note(4, FrameAccounting.Reason.QueueEvicted);

            Assert.AreEqual(2, acct.Get(3, FrameAccounting.Reason.MasterGate));
            Assert.AreEqual(1, acct.Get(4, FrameAccounting.Reason.QueueEvicted), "不得串路");
            Assert.AreEqual(0, acct.Get(5, FrameAccounting.Reason.MasterGate));

            // 跑在相机 SDK 回调线程上：非法入参只允许被忽略，绝不允许抛
            acct.Note(-1, FrameAccounting.Reason.ModeGate);
            acct.Note(FrameAccounting.Slots, FrameAccounting.Reason.ModeGate);
            acct.Note(0, (FrameAccounting.Reason)999);
            Assert.AreEqual(0, acct.Get(0, (FrameAccounting.Reason)999));
        }

        [TestMethod]
        public void 三组分类互斥完备_各路总账等于三组之和()
        {
            // 分组表(Groups)若漏项/重项，TotalAfterReceive 的分子就会静默错值，
            // 对账结论随之失真——这条不变量只能这样整体钉住。
            var acct = new FrameAccounting();
            int expectTotal = 0;
            for (int r = 0; r < (int)FrameAccounting.Reason.Count; r++)
            {
                var reason = (FrameAccounting.Reason)r;
                for (int k = 0; k < r + 1; k++) acct.Note(0, reason);
                expectTotal += r + 1;
            }

            int sumAll = 0;
            for (int r = 0; r < (int)FrameAccounting.Reason.Count; r++)
                sumAll += acct.Get(0, (FrameAccounting.Reason)r);
            Assert.AreEqual(expectTotal, sumAll);

            Assert.AreEqual(expectTotal,
                acct.TotalBeforeReceive(0) + acct.TotalAfterReceive(0)
                + acct.Get(0, FrameAccounting.Reason.AcquireFailed)
                + acct.Get(0, FrameAccounting.Reason.ManualTrigger)
                + acct.Get(0, FrameAccounting.Reason.ManualTriggerStopped),
                "接收前+接收后+备注必须等于全部计数——分组表漏了原因就会在这里暴露");
        }

        [TestMethod]
        public void 备注类不参与对账_取图失败与手动触发不计入任何丢弃总数()
        {
            var acct = new FrameAccounting();
            acct.Note(1, FrameAccounting.Reason.AcquireFailed);
            acct.Note(1, FrameAccounting.Reason.ManualTrigger);
            acct.Note(1, FrameAccounting.Reason.ManualTriggerStopped);

            Assert.AreEqual(0, acct.TotalAfterReceive(1));
            Assert.AreEqual(0, acct.TotalBeforeReceive(1));
            Assert.IsFalse(FrameAccounting.IsAfterReceive(FrameAccounting.Reason.AcquireFailed));
            Assert.IsTrue(FrameAccounting.GroupOf(FrameAccounting.Reason.ManualTrigger)
                == FrameAccounting.ReasonGroup.Note);
        }

        [TestMethod]
        public void 分组明细不含备注类_取图失败不会被念成丢帧()
        {
            var acct = new FrameAccounting();
            acct.Note(2, FrameAccounting.Reason.CallbackPaused);
            acct.Note(2, FrameAccounting.Reason.AcquireFailed);
            acct.Note(2, FrameAccounting.Reason.ManualTrigger);

            string pre = acct.Breakdown(2, FrameAccounting.ReasonGroup.PreReceive);
            Assert.IsTrue(pre.Contains("暂停丢(回调)1"), "接收前明细要列出该项，实际:" + pre);
            Assert.IsFalse(pre.Contains("取图失败"), "备注类不得混进接收前明细，否则现场听成丢了 1 帧");
            Assert.IsFalse(pre.Contains("手动触发"));

            string all = acct.Breakdown(2);
            Assert.IsTrue(all.Contains("取图失败(仍判NG)1") && all.Contains("手动触发1"), "全量明细应含备注类，实际:" + all);

            // 0 的类别省略
            Assert.IsFalse(acct.Breakdown(3, FrameAccounting.ReasonGroup.AfterReceive).Contains("队列满丢"));
            Assert.AreEqual("无", acct.Breakdown(3));
        }

        [TestMethod]
        public void 对账_差额为零且无归因_判已查全()
        {
            var acct = new FrameAccounting();
            string line = acct.Reconcile(0, 100, 100);
            Assert.IsTrue(line.Contains("差额0"), line);
            Assert.IsTrue(line.Contains("已查全"), line);
            Assert.IsFalse(line.Contains("未查全"), "已查全的措辞不能同时出现'未查全'子串");
        }

        [TestMethod]
        public void 对账_差额与归因逐项相符_判已查全并列出明细()
        {
            var acct = new FrameAccounting();
            acct.Note(0, FrameAccounting.Reason.MasterGate);
            acct.Note(0, FrameAccounting.Reason.MasterGate);
            acct.Note(0, FrameAccounting.Reason.QueueEvicted);

            string line = acct.Reconcile(0, 10, 7);   // 差额 3，归因 3
            Assert.IsTrue(line.Contains("已查全：差额与归因分类逐项相符"), line);
            Assert.IsTrue(line.Contains("总门不通过2"), line);
            Assert.IsTrue(line.Contains("队列满丢1"), line);
        }

        [TestMethod]
        public void 对账_有未归因差额_必须明说未查全与剩余帧数()
        {
            // ★核心反谎报用例：存在没接进账本的丢弃路径时，结论绝不能是"没丢帧"。
            var acct = new FrameAccounting();
            acct.Note(0, FrameAccounting.Reason.MasterGate);

            string line = acct.Reconcile(0, 10, 7);   // 差额 3，只归因了 1
            Assert.IsTrue(line.Contains("未查全"), line);
            Assert.IsTrue(line.Contains("还有 2 帧"), "要给出未归因的具体帧数，实际:" + line);
            Assert.IsTrue(line.Contains("不能断定『没丢帧』"), line);
        }

        [TestMethod]
        public void 对账_归因超出差额_必须报超出而不是已查全()
        {
            // 同帧重复记账、或把接收前的丢弃误记成接收后，都会让分子大于差额。
            var acct = new FrameAccounting();
            for (int i = 0; i < 4; i++) acct.Note(0, FrameAccounting.Reason.WorkerBusy);

            string line = acct.Reconcile(0, 10, 9);   // 差额 1，归因 4
            Assert.IsTrue(line.Contains("归因超出差额 3"), line);
            Assert.IsFalse(line.Contains("已查全"), line);
        }

        [TestMethod]
        public void 对账_检测完成数大于接收数_判正常不参与对账()
        {
            // 回图/手动检测不来自相机帧：sum 可以大于 m_nFrames，这是合法状态，不能报丢帧。
            var acct = new FrameAccounting();
            string line = acct.Reconcile(5, 3, 9);
            Assert.IsTrue(line.Contains("差额-6"), line);
            Assert.IsTrue(line.Contains("不参与对账"), line);
            Assert.IsFalse(line.Contains("未查全"), line);
        }

        [TestMethod]
        public void 接收前丢弃不进漏帧_漏帧差额为零时接收前仍可有账()
        {
            // 问题2 的关键口径：暂停/停采/模式门丢的帧连 m_nFrames 都没加，
            // 所以"漏帧=0"并不等于"机台拍的每张都收到了"。
            var acct = new FrameAccounting();
            for (int i = 0; i < 5; i++) acct.Note(0, FrameAccounting.Reason.CallbackPaused);

            Assert.AreEqual(5, acct.TotalBeforeReceive(0));
            Assert.AreEqual(0, acct.TotalAfterReceive(0), "接收前不得计入漏帧对账分子");
            Assert.IsTrue(acct.Reconcile(0, 20, 20).Contains("已查全"), "漏帧为 0 时仍应判已查全（接收前另列）");
        }

        [TestMethod]
        public void 清零复位所有路与所有原因()
        {
            var acct = new FrameAccounting();
            acct.Note(0, FrameAccounting.Reason.MasterGate);
            acct.Note(11, FrameAccounting.Reason.QueueEvicted);
            Assert.IsTrue(acct.HasActivity(0));
            Assert.IsTrue(acct.HasActivity(11));
            Assert.IsFalse(acct.HasActivity(5));

            acct.ResetAll();
            Assert.IsFalse(acct.HasActivity(0), "清零后不得留任何账（界面清零与此同窗口）");
            Assert.IsFalse(acct.HasActivity(11));
            Assert.AreEqual(0, acct.Get(0, FrameAccounting.Reason.MasterGate));
            Assert.AreEqual(0, acct.TotalAfterReceive(11));
        }

        // ── ★第55轮：连续运行不计件边界 ─────────────────────────────────────
        // 现场反馈"连续运行的相机为什么还在计数"。查下来不是检测链路多计，而是**界面计件那四行**
        // 原本对连续运行路做了排除（`!= "连续运行"` 才刷新），第53轮把它当成"停在种子文本无数字"的
        // 显示缺陷删掉了 ⇒ 连续运行路开始显示"检测数:2605 NG数:2605 合格率:0.000"，操作员以为在产 NG。
        // 边界本身历来只做在显示层（内部 sum 自初始提交起就无条件累加，漏帧对账要用），本轮恢复边界、
        // 并把判定挪进 FrameAccounting 由下面三条钉住——它再被当成"多余的条件"删掉，测试就红。
        [TestMethod]
        public void 连续运行的路不参与计件_含零字节与空白变体()
        {
            Assert.IsTrue(FrameAccounting.IsContinuousMode("连续运行"));
            Assert.IsTrue(FrameAccounting.IsContinuousMode("连续运行\0"), "带残留 \\0 必须仍判连续（复盘P2-7 同源坑）");
            Assert.IsTrue(FrameAccounting.IsContinuousMode("  连续运行  "), "前后空白必须归一化掉");
            Assert.IsTrue(FrameAccounting.IsContinuousMode("\0连续运行\0"));

            Assert.IsFalse(FrameAccounting.IsContinuousMode("触发拍照"));
            Assert.IsFalse(FrameAccounting.IsContinuousMode("通讯触发"));
            Assert.IsFalse(FrameAccounting.IsContinuousMode("连续运行中"), "近义字串不得误判为连续");
        }

        [TestMethod]
        public void 计件边界只排除连续运行_不得顺手改成白名单判法()
        {
            // 原口径是 `!= "连续运行"` 才显示数字：空模式（启动期连接早于载入）与不认识的字串
            // 都照旧计件。若哪天想改成"只有触发拍照/通讯触发才计件"，那是行为变更，必须另起一轮
            // 并先确认现场语义——不能借"顺手收紧"的名义改这三条断言。
            Assert.IsFalse(FrameAccounting.IsContinuousMode(""), "空模式不得被排除在计件之外（保持原 != 口径）");
            Assert.IsFalse(FrameAccounting.IsContinuousMode(null));
            Assert.IsFalse(FrameAccounting.IsContinuousMode("   "));
            Assert.IsFalse(FrameAccounting.IsContinuousMode("随便什么脏值"));
        }

        [TestMethod]
        public void 模式归一化先去零字节再Trim()
        {
            Assert.AreEqual("连续运行", FrameAccounting.NormalizeMode("\0 连续运行 \0"));
            Assert.AreEqual("", FrameAccounting.NormalizeMode(null), "null 归一化成空串，判定侧不得再抛");
            Assert.AreEqual("", FrameAccounting.NormalizeMode("  \0  "));
        }

        // ── ★第58轮（朋友复审①）：两张平行表的映射逐一对齐 ────────────────────
        // Display[] / Groups[] 靠下标与 Reason 枚举对齐，全靠手工维护；TablesAligned 只比长度，
        // 所以"把任意两项顺序写反"这种错长度检查照样过，但界面上的归因标签会张冠李戴——
        // 现场照着错标签排查，比压根没标签更坑人。下面的表是**抄自语义**的期望值（不是从被测表生成），
        // 任何一处错位/漏项都会红。标签连字面都钉住是有意的：改文案必须连带改这里，
        // 因为界面念给现场听的就是这几个字。
        [TestMethod]
        public void 全部原因_显示标签与分组逐一对齐_枚举个数钉死()
        {
            string[] labels =
            {
                "暂停丢(回调)", "停止中丢(回调)", "槽位无效丢(回调)", "模式门丢(回调)", "无在途触发丢(回调)",
                "队列满丢", "入队被拒丢", "取出后停线丢", "该路占用中丢", "停线排空丢",
                "暂停丢(检测)", "检测中停线丢", "总门不通过", "作业非Stopped", "检测流程异常", "线程异常",
                "取图失败(仍判NG)", "手动触发", "手动触发(停止态)",
            };
            FrameAccounting.ReasonGroup[] groups =
            {
                FrameAccounting.ReasonGroup.PreReceive, FrameAccounting.ReasonGroup.PreReceive,
                FrameAccounting.ReasonGroup.PreReceive, FrameAccounting.ReasonGroup.PreReceive,
                FrameAccounting.ReasonGroup.PreReceive,
                FrameAccounting.ReasonGroup.AfterReceive, FrameAccounting.ReasonGroup.AfterReceive,
                FrameAccounting.ReasonGroup.AfterReceive, FrameAccounting.ReasonGroup.AfterReceive,
                FrameAccounting.ReasonGroup.AfterReceive, FrameAccounting.ReasonGroup.AfterReceive,
                FrameAccounting.ReasonGroup.AfterReceive, FrameAccounting.ReasonGroup.AfterReceive,
                FrameAccounting.ReasonGroup.AfterReceive, FrameAccounting.ReasonGroup.AfterReceive,
                FrameAccounting.ReasonGroup.AfterReceive,
                FrameAccounting.ReasonGroup.Note, FrameAccounting.ReasonGroup.Note,
                FrameAccounting.ReasonGroup.Note,
            };

            // 先钉住"到底有几个原因"：新增 Reason 忘了扩表，这条比任何逐表检查都先红。
            Assert.AreEqual(19, (int)FrameAccounting.Reason.Count, "新增原因必须同时补 Display/Groups 与下面两张期望表");
            Assert.AreEqual((int)FrameAccounting.Reason.Count, labels.Length);
            Assert.AreEqual((int)FrameAccounting.Reason.Count, groups.Length);

            for (int r = 0; r < labels.Length; r++)
            {
                var reason = (FrameAccounting.Reason)r;
                string name = reason.ToString();
                string expect = labels[r] + "1";

                var acct = new FrameAccounting();
                acct.Note(0, reason);

                // 单独记一笔时整串明细就是这个原因的标签——标签错位在这里暴露
                Assert.AreEqual(expect, acct.Breakdown(0), name + " 的显示标签与枚举顺序不同步（Display[]）");

                var group = FrameAccounting.GroupOf(reason);
                Assert.AreEqual(groups[r], group, name + " 的分组与枚举顺序不同步（Groups[]）");

                // 分组明细必须与 GroupOf 指认同一组，其余两组为空
                foreach (FrameAccounting.ReasonGroup g in Enum.GetValues(typeof(FrameAccounting.ReasonGroup)))
                    Assert.AreEqual(g == group ? expect : "无", acct.Breakdown(0, g),
                        name + " 在分组 " + g + " 的明细里出现与否不符");

                Assert.AreEqual(FrameAccounting.ReasonGroup.AfterReceive == group,
                    FrameAccounting.IsAfterReceive(reason), name + " 的 IsAfterReceive 与分组不符");
            }
        }
    }
}
