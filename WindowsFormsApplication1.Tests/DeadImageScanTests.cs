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
    }
}
