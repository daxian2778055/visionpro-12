using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第51.1轮：死引用诊断的扫描状态单测。
    /// 覆盖的是"诊断会不会说谎"——第51轮初版把节点计数放在 Form1 的 static 字段且从不复位，
    /// 第二次诊断一进函数就被预算耗尽挡住，于是打出"未找到失效图像引用"这种**误导性结论**。
    /// 这类 bug 只在"第二次保存失败"时才暴露，现场极难复现，所以必须用单测钉住语义：
    ///   ① 扫描状态每次新建即归零；② 预算耗尽要标记截断；③ 结论必须区分"没查完"与"真没有"。
    /// </summary>
    [TestClass]
    public class DeadImageScanTests
    {
        [TestMethod]
        public void 新建扫描实例_节点计数从零开始_不受上一次诊断影响()
        {
            var first = new DeadImageScan();
            for (int i = 0; i < DeadImageScan.MaxNodes + 10; i++) first.CountNode();
            Assert.IsTrue(first.Truncated, "第一次扫描耗尽预算应标记截断");

            // ★回归点：原实现用 static 计数，第二次诊断会"一进来就被挡住"
            var second = new DeadImageScan();
            Assert.AreEqual(0, second.Nodes, "新扫描的节点计数必须从 0 开始");
            Assert.IsFalse(second.Truncated, "新扫描不应继承上一次的截断状态");
            Assert.IsTrue(second.CountNode(), "新扫描必须还能走节点，否则第二次诊断会谎报'未找到'");
            Assert.AreEqual(1, second.Nodes);
        }

        [TestMethod]
        public void 节点预算未耗尽_不标记截断()
        {
            var scan = new DeadImageScan();
            Assert.IsTrue(scan.CountNode());
            Assert.IsFalse(scan.Truncated);
            Assert.AreEqual(1, scan.Nodes);
        }

        [TestMethod]
        public void 节点预算耗尽_返回false并标记截断()
        {
            var scan = new DeadImageScan();
            bool last = true;
            for (int i = 0; i < DeadImageScan.MaxNodes + 1; i++) last = scan.CountNode();

            Assert.IsFalse(last, "超出 MaxNodes 后必须返回 false 让调用方停止本支");
            Assert.IsTrue(scan.Truncated);
            Assert.AreEqual(DeadImageScan.MaxNodes + 1, scan.Nodes, "节点数到上限即停，不应继续累加");

            // 已截断后继续计数应保持 false（幂等），不能"复活"
            Assert.IsFalse(scan.CountNode());
        }

        [TestMethod]
        public void 预算耗尽时_结论必须说可能不完整_而不是断言没问题()
        {
            var scan = new DeadImageScan();
            for (int i = 0; i < DeadImageScan.MaxNodes + 1; i++) scan.CountNode();

            string text = scan.Conclusion();
            StringAssert.Contains(text, "可能不完整", "截断时必须如实说结果可能漏报");
            Assert.IsFalse(text.Contains("未在工具树里找到"),
                "预算耗尽不允许输出'未找到'这种断言——诊断谎报比不诊断更糟");
        }

        [TestMethod]
        public void 报告条数达上限_标记截断且结论提示可能更多()
        {
            var scan = new DeadImageScan();
            for (int i = 0; i < DeadImageScan.MaxReport + 5; i++) scan.Add("job1/tool.TrainImage[" + i + "]");

            Assert.AreEqual(DeadImageScan.MaxReport, scan.Dead.Count, "Add 超过上限不再增长");

            Assert.IsFalse(scan.CountNode(), "已达报告上限后应停止遍历");
            Assert.IsTrue(scan.Truncated);

            string text = scan.Conclusion();
            StringAssert.Contains(text, "共 " + DeadImageScan.MaxReport + " 处");
            StringAssert.Contains(text, "可能存在更多");
        }

        [TestMethod]
        public void 有发现时_结论给出条数且不提示截断()
        {
            var scan = new DeadImageScan();
            scan.Add("job1/CogPMAlignMultiTool1.Operator.Items[2].Pattern.TrainImage  →  CogImage8Grey");

            string text = scan.Conclusion();
            StringAssert.Contains(text, "共 1 处");
            Assert.IsFalse(text.Contains("可能存在更多"), "没截断就不该提示可能存在更多");
        }

        [TestMethod]
        public void 干净扫描_结论才是真正的未找到()
        {
            var scan = new DeadImageScan();
            scan.CountNode();
            scan.Add("job1/tool.X");   // 有发现：确认下面那条"未找到"不是走这条分支
            Assert.IsFalse(scan.Conclusion().Contains("未在工具树里找到"));

            var clean = new DeadImageScan();
            clean.CountNode();
            Assert.IsFalse(clean.Truncated);
            StringAssert.Contains(clean.Conclusion(), "未在工具树里找到");
        }

        [TestMethod]
        public void 同一实例只登记一次_防环与重复下钻()
        {
            var scan = new DeadImageScan();
            int hash = 12345;
            Assert.IsTrue(scan.TryVisit(hash), "首次访问应放行");
            Assert.IsFalse(scan.TryVisit(hash), "同一实例第二次访问必须被挡下（防环）");
            Assert.IsTrue(scan.TryVisit(hash + 1), "不同实例不应被误挡");
            Assert.AreEqual(2, scan.Seen.Count);
        }

        // ===== ★第51.2轮 P2：截断必须留痕（深度/集合上限原先是静默 return/break） =====

        [TestMethod]
        public void 深度或集合截断_结论必须带出原因_而不是断言没问题()
        {
            var scan = new DeadImageScan();
            scan.MarkTruncated("下钻深度超过 10 层");

            Assert.IsTrue(scan.Truncated);
            Assert.AreEqual("下钻深度超过 10 层", scan.TruncatedReason);

            string text = scan.Conclusion();
            StringAssert.Contains(text, "下钻深度超过 10 层");
            StringAssert.Contains(text, "可能不完整");
            Assert.IsFalse(text.Contains("未在工具树里找到"),
                "深度截断不得输出'未找到'——这与第51.1轮修的 static 计数是同一类谎报");
        }

        [TestMethod]
        public void 有发现且被截断_结论同时给出条数与截断原因()
        {
            var scan = new DeadImageScan();
            scan.Add("job1/CogPMAlignMultiTool1.Operator.Items[2].Pattern.TrainImage  →  CogImage8Grey");
            scan.MarkTruncated("集合元素超过 200 个");

            string text = scan.Conclusion();
            StringAssert.Contains(text, "共 1 处");
            StringAssert.Contains(text, "集合元素超过 200 个");
            StringAssert.Contains(text, "可能存在更多");
        }

        [TestMethod]
        public void 重复标记截断_以最先撞到的原因为准()
        {
            var scan = new DeadImageScan();
            scan.MarkTruncated("第一个原因");
            scan.MarkTruncated("第二个原因");
            Assert.AreEqual("第一个原因", scan.TruncatedReason,
                "先撞到的才是把扫描挡住的那个，后续原因不应覆盖它");
        }

        // ===== ★第51.3轮 P2-1：留痕与"停止"必须是两个状态（51.2 的回归点） =====
        // 51.2 把 if (Truncated) return false 放在 CountNode 开头，于是任一子树撞深度上限
        // 都会让其余 11 路 job 全部不再遍历——留痕做到了，覆盖率反而掉到 51.1 以下。

        [TestMethod]
        public void 仅留痕不设预算_扫描必须继续走完其余分支()
        {
            var scan = new DeadImageScan();
            scan.MarkTruncated("下钻深度超过 10 层");

            for (int i = 0; i < 5; i++)
                Assert.IsTrue(scan.CountNode(), "留痕不应停整趟扫描——51.2 的回归正是这一行");

            Assert.AreEqual(5, scan.Nodes, "节点计数必须继续前进，不能被留痕冻结");
            Assert.IsFalse(scan.BudgetExhausted, "仅留痕不等于预算用尽");
            Assert.IsTrue(scan.Truncated, "但'结果不完整'这个记号要留下");

            // 记号还在，结论仍要说可能不完整——留痕与放行互不干扰
            string text = scan.Conclusion();
            StringAssert.Contains(text, "下钻深度超过 10 层");
            StringAssert.Contains(text, "可能不完整");
        }

        [TestMethod]
        public void 耗时超预算_停止扫描并留痕_原因写明耗时上限()
        {
            var scan = new DeadImageScan(0);          // 注入 0ms 时间预算（单测专用构造）
            System.Threading.Thread.Sleep(10);

            Assert.IsFalse(scan.CountNode(), "超时预算后必须停止，否则 UI 会一直冻结在 catch 里");
            Assert.IsTrue(scan.BudgetExhausted);
            StringAssert.Contains(scan.TruncatedReason, "耗时上限");
            Assert.IsFalse(scan.CountNode(), "停止是持久的");

            string text = scan.Conclusion();
            StringAssert.Contains(text, "可能不完整");
            Assert.IsFalse(text.Contains("未在工具树里找到"), "超时截断同样不得断言'未找到'");
        }

        [TestMethod]
        public void 时间预算有值时_正常小扫描不会被误判超时()
        {
            var scan = new DeadImageScan();           // 默认 1500ms
            Assert.IsTrue(scan.CountNode());
            Assert.IsFalse(scan.Truncated, "刚建好就报截断说明计时口径写错了");
            Assert.AreEqual(1500, DeadImageScan.MaxElapsedMs, "时间预算口径若改动，日志注释需同步");
        }

        // ===== ★第51.4轮 P2：留痕原因与"整趟停止原因"是两件事 =====
        // 51.3 之后留痕不再停遍历，于是"第一个原因"和"最后让 CountNode 返回 false 的原因"分叉；
        // 只印前者会让读者以为树基本走完、只有一支被挡，而实情是时间预算到点、后面几路一个节点没走。

        [TestMethod]
        public void 首个留痕原因与停止原因不同_结论必须两者并列()
        {
            var scan = new DeadImageScan(0);          // 时间预算 0ms，必停
            scan.MarkTruncated("下钻深度超过 10 层");   // 先留一个**不挡遍历**的痕
            System.Threading.Thread.Sleep(10);

            Assert.IsFalse(scan.CountNode());
            Assert.IsTrue(scan.BudgetExhausted);
            Assert.AreEqual("耗时上限 0ms", scan.BudgetStopReason, "停止原因必须单独存");
            Assert.AreEqual("下钻深度超过 10 层", scan.TruncatedReason, "首个留痕原因不得被覆盖（既有口径）");

            string text = scan.Conclusion();
            StringAssert.Contains(text, "下钻深度超过 10 层");
            StringAssert.Contains(text, "整趟停止原因：耗时上限 0ms",
                "两者不同时必须并列，否则归因会骗人——这是本轮要消灭的那一类");
        }

        [TestMethod]
        public void 未发生预算停止时_结论不得编造停止原因()
        {
            var scan = new DeadImageScan();
            scan.MarkTruncated("集合元素超过 200 个");
            Assert.IsTrue(scan.CountNode());

            string text = scan.Conclusion();
            Assert.IsFalse(text.Contains("整趟停止原因"),
                "只是留痕、整趟没停，就不该出现停止原因");
            StringAssert.Contains(text, "集合元素超过 200 个");
        }
    }
}
