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

        // ===== ★第51.5轮：覆盖口径"走到第 N/M 路"本身也会骗人 =====
        // 分子在走进去之前赋值、break 只挡后面的 job ⇒ 预算在最后一路中途到点时，
        // 会打出"走到第 12/12 路"却伴随 BudgetExhausted，读者以为全走完了
        //（外部模拟实测：Nodes=20001、BudgetExhausted=True 正是这个组合）。
        // 故文案移进 DeadImageScan.CoverageText，让这三件事各有一条单测钉住。

        private static DeadImageScan ExhaustedScan()
        {
            var s = new DeadImageScan(0);             // 时间预算 0ms，必停
            System.Threading.Thread.Sleep(10);
            s.CountNode();
            return s;
        }

        [TestMethod]
        public void 预算在最后一路中途耗尽_覆盖文案必须把该路标成未完()
        {
            var scan = ExhaustedScan();
            Assert.IsTrue(scan.BudgetExhausted);

            string text = scan.CoverageText(12, 12, 12);
            StringAssert.Contains(text, "挂树 12 路中走到第 12 路");
            StringAssert.Contains(text, "未完",
                "走到第 12/12 ≠ 第 12 路走完了；不标未完就是让日志冒充'查全了'");
        }

        [TestMethod]
        public void 预算未耗尽时_覆盖文案不得出现未完()
        {
            var scan = new DeadImageScan();
            string text = scan.CoverageText(12, 12, 12);

            Assert.IsFalse(text.Contains("未完"),
                "走完了却说没走完，同样是谎报（会白白引发一轮排查）");
            StringAssert.Contains(text, "挂树 12 路中走到第 12 路");
        }

        [TestMethod]
        public void 分母只算真挂了工具树的路_与定长12无关()
        {
            // Myjobs 恒为 12 个非空元素，未挂树的 job 一进门就 return、零成本，不该进分母。
            var scan = new DeadImageScan();
            string text = scan.CoverageText(3, 1, 5);

            StringAssert.Contains(text, "挂树 3 路中走到第 1 路", "分母必须是挂树路数，不是 12");
            StringAssert.Contains(text, "myjob5", "停在第几路要用 myjob 序号好定位");
            Assert.IsFalse(text.Contains("12"), "不得退回定长 12 这个假分母");
        }

        [TestMethod]
        public void 预算在进第一棵树之前耗尽_覆盖文案要说明未进入()
        {
            var scan = ExhaustedScan();
            string text = scan.CoverageText(12, 0, 0);

            StringAssert.Contains(text, "未进入任何一棵",
                "第 0 路若不解释，会被读成'走了 0 路还查完了'");
            Assert.IsFalse(text.Contains("myjob0"), "没走到任何一路就不该编造 myjob 序号");
        }

        // ===== ★第51.6轮：手动诊断入口的守卫（菜单"已释放图像诊断"） =====
        // 从保存失败的 catch 里搬出来做成可测判定，是为了不让它变成"无人测试的 UI 胶水"——
        // 这几轮的缺陷无一例外出在没被测试覆盖的接缝上。拒绝的理由是**可信度**，不是安全。

        [TestMethod]
        public void 检测运行中_手动诊断必须拒绝并要求先停检测()
        {
            string why = DeadImageScan.ManualEntryRefusal(detectionRunning: true, schemeSwitching: false,
                nothingLoaded: false, formDisposing: false);
            Assert.IsNotNull(why, "运行中反射读到的是执行到一半的瞬时状态，扫出来的结论可能是假的");
            StringAssert.Contains(why, "停止检测");
        }

        [TestMethod]
        public void 方案切换中_手动诊断必须拒绝()
        {
            string why = DeadImageScan.ManualEntryRefusal(detectionRunning: false, schemeSwitching: true,
                nothingLoaded: false, formDisposing: false);
            Assert.IsNotNull(why, "切换时整棵工具树正在被替换，扫到的是新旧混合");
            StringAssert.Contains(why, "稍候");
        }

        [TestMethod]
        public void 两种状态都空闲_手动诊断必须放行()
        {
            Assert.IsNull(DeadImageScan.ManualEntryRefusal(false, false, false, false),
                "常态（保存成功、检测已停）正是本入口要服务的场景，不能被自己挡掉");
        }

        [TestMethod]
        public void 两种拒绝理由同时成立_以运行中优先()
        {
            string why = DeadImageScan.ManualEntryRefusal(true, true, false, false);
            Assert.IsNotNull(why);
            StringAssert.Contains(why, "停止检测",
                "先答更本质的那条：即便切完方案，运行中依然不可信");
        }

        // ===== ★第51.7轮：守卫补两条 + 节点级沉默记账（P1） =====
        // 本轮的洞是"结论行自己会说谎"： walker 里结构性 catch{ } 吞掉异常后状态仍是
        // "未截断 + 预算未用尽 + 覆盖走到最后一棵"，Conclusion() 于是照样断言"没找到"。
        // 单测钉的是**记账与措辞的对应关系**——这类缺陷读代码与跑现场都抓不到。

        [TestMethod]
        public void 方案未加载_必须拒绝_且不得与切方案中的说法混用()
        {
            string why = DeadImageScan.ManualEntryRefusal(false, false, nothingLoaded: true, formDisposing: false);
            Assert.IsNotNull(why, "此刻压根没有稳定工具树，扫出来只能是空树");
            StringAssert.Contains(why, "加载方案", "要给的是动作，不是等待");
            Assert.IsFalse(why.Contains("稍候"),
                "与'方案切换中，请稍候'必须是两句——后者把人引向等待，前者要求先加载");
        }

        [TestMethod]
        public void 窗体正在关闭_优先于其它拒绝理由()
        {
            string why = DeadImageScan.ManualEntryRefusal(true, true, true, formDisposing: true);
            StringAssert.Contains(why, "正在关闭", "关窗途中再跑 1.5 秒反射只会拖死退出路径");
        }

        [TestMethod]
        public void 扫描期间检测被后台启动_必须给出复查标注()
        {
            // yunxing 是 button1_Click 里 Task.Run 在后台线程置真的，诊断在 UI 线程冻结挡不住它。
            Assert.IsNull(DeadImageScan.MidScanStateChangeNote(false, false), "状态没变就不该多一行");
            string note = DeadImageScan.MidScanStateChangeNote(true, false);
            StringAssert.Contains(note, "扫描期间");
            StringAssert.Contains(note, "按扫描开始时的状态得出",
                "结论不推翻，但前提必须标掉——否则守卫形同给过期快照背书");
            StringAssert.Contains(DeadImageScan.MidScanStateChangeNote(true, true), "检测启动且方案切换开始");
        }

        [TestMethod]
        public void 结构性中断_结论不得断言未找到_并带出跳过账目()
        {
            var scan = new DeadImageScan();
            scan.CountNode();
            scan.NoteSkippedBranch("job1/CogToolBlock1 属性环（TargetInvocationException）");

            Assert.AreEqual(1, scan.SkippedBranches);
            Assert.IsFalse(scan.IsConclusive, "有一整支没走完就不能算结论成立");
            string text = scan.Conclusion();
            Assert.IsFalse(text.Contains("未在工具树里找到"),
                "本轮修的正是这一条：沉默的 catch 不许再换来一句'没找到'");
            StringAssert.Contains(text, "跳过账目");
            StringAssert.Contains(text, "结构性中断 1 处");
            StringAssert.Contains(text, "TargetInvocationException", "首因要能指出断在哪一支");
        }

        [TestMethod]
        public void 整路跳过_原因用调用方给的整句_不得统称为异常()
        {
            var scan = new DeadImageScan();
            scan.NoteSkippedTree("job5 未取到根工具（job 未挂上且 block 为空）");

            Assert.AreEqual(1, scan.SkippedTrees);
            string text = scan.Conclusion();
            Assert.IsFalse(text.Contains("未在工具树里找到"));
            StringAssert.Contains(text, "未取到根工具",
                "这一路并没有抛异常，把'未取到根'说成'遍历中途异常'是又一处归因骗人");
        }

        [TestMethod]
        public void 没有任何树可扫_结论必须说什么都没看到()
        {
            var scan = new DeadImageScan();
            scan.NoteNothingToScan();

            Assert.IsFalse(scan.IsConclusive);
            string text = scan.Conclusion();
            Assert.IsFalse(text.Contains("未在工具树里找到"),
                "挂树数为 0 时'没找到'是空话——原写法正是在这里给出一句断言");
            StringAssert.Contains(text, "不能据此判断");
        }

        [TestMethod]
        public void 属性读失败只计软账_不否掉结论_但必须看得见()
        {
            var scan = new DeadImageScan();
            scan.CountNode();
            for (int i = 0; i < 3; i++) scan.NoteSkippedRead();

            Assert.IsTrue(scan.IsConclusive,
                "单属性读失败在 Cognex 上是常态，算成不结论会让这道闸恒为假（本轮明示的取舍）");
            string text = scan.Conclusion();
            StringAssert.Contains(text, "未在工具树里找到", "软口径不得把可信结论降级");
            StringAssert.Contains(text, "属性/元素读值失败 3 处", "但也不能不吭声");
        }

        [TestMethod]
        public void 干净结论_不得带跳过账目的噪音()
        {
            var scan = new DeadImageScan();
            scan.CountNode();
            StringAssert.Contains(scan.Conclusion(), "未在工具树里找到");
            Assert.IsFalse(scan.Conclusion().Contains("跳过账目"),
                "全是 0 时输出账目会让读的人以为有东西要查");
        }

        [TestMethod]
        public void 分子大于分母_覆盖口径必须自认不可信()
        {
            // 分母与分子来自两次枚举，中间并发重绑会出现"挂树 3 路中走到第 5 路"。
            string text = new DeadImageScan().CoverageText(3, 5, 7);
            StringAssert.Contains(text, "不可信", "自相矛盾的分数不能当进度读");
        }
    }
}
