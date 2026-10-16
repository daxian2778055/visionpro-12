using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第60轮：存图 / 统计 CSV 根目录解析（PathLayout）的回归。
    /// 钉住两件事：
    ///   ① 规范化边界 —— 空/NUL/成对引号/尾斜杠补齐；
    ///   ② 默认值下与硬编码时代逐字一致 —— 没配 ini 的现场，拼出的路径必须一模一样，
    ///      否则存图目录会悄悄搬家（这类改动一旦漂移，现场不会立刻发现，几天后才从"图去哪了"里暴露）。
    /// </summary>
    [TestClass]
    public class PathLayoutTests
    {
        // 现场真实子目录名（RunLog 里用了多年的名字），用 \u 转义写死：
        // 防编辑器/复制传输环节把中文改成别的字，测试会连错名字一起固化。
        private const string 生产统计 = "\u751F\u4EA7\u7EDF\u8BA1";
        private const string 每日统计 = "\u6BCF\u65E5\u7EDF\u8BA1";

        [TestMethod]
        public void 规范化_空与NUL回退默认()
        {
            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot(null, PathLayout.DefaultImageRoot), "null 回退默认");
            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot("", PathLayout.DefaultImageRoot), "空串回退默认");
            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot("   ", PathLayout.DefaultImageRoot), "全空白回退默认");
            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot("\0\0", PathLayout.DefaultImageRoot), "纯 NUL 回退默认");
            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot(" \0\0 ", PathLayout.DefaultImageRoot), "NUL 与空白清空后回退默认");
        }

        [TestMethod]
        public void 规范化_尾斜杠补齐与保留()
        {
            Assert.AreEqual(@"E:\", PathLayout.NormalizeRoot(@"E:\", PathLayout.DefaultImageRoot), "已带尾斜杠不动");
            Assert.AreEqual(@"E:\", PathLayout.NormalizeRoot("E:", PathLayout.DefaultImageRoot), "裸盘符补斜杠");
            Assert.AreEqual(@"E:\data\", PathLayout.NormalizeRoot(@"E:\data", PathLayout.DefaultImageRoot), "目录补尾斜杠");
            Assert.AreEqual(@"\\server\share\", PathLayout.NormalizeRoot(@"\\server\share", PathLayout.DefaultImageRoot), "UNC 补尾斜杠");
            Assert.AreEqual("E:/data/", PathLayout.NormalizeRoot("E:/data/", PathLayout.DefaultImageRoot), "正斜杠原样保留");
            // 无斜杠结尾一律补反斜杠（Windows 标准；正斜杠输入属罕见情况，混用斜杠对各 Windows API 无害）
            Assert.AreEqual(@"E:/data\", PathLayout.NormalizeRoot("E:/data", PathLayout.DefaultImageRoot), "无斜杠结尾补反斜杠");
        }

        [TestMethod]
        public void 规范化_去空白与成对引号()
        {
            Assert.AreEqual(@"E:\data\", PathLayout.NormalizeRoot("  E:\\data  ", PathLayout.DefaultImageRoot), "去首尾空白");
            Assert.AreEqual(@"E:\data\", PathLayout.NormalizeRoot("\"E:\\data\"", PathLayout.DefaultImageRoot), "剥成对引号");
            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot("\"\"", PathLayout.DefaultImageRoot), "只剩一对引号回退默认");
            Assert.AreEqual("\"E:\\data\\", PathLayout.NormalizeRoot("\"E:\\data", PathLayout.DefaultImageRoot), "不成对引号不剥");
        }

        [TestMethod]
        public void 子目录_拼接与斜杠规整()
        {
            Assert.AreEqual(@"E:\fu1ok\", PathLayout.SubDirectory(@"E:\", "fu1ok"), "标准拼接");
            Assert.AreEqual(@"E:\fu1ok\", PathLayout.SubDirectory(@"E:\", @"\fu1ok\"), "两端斜杠去掉，不拼出多连斜杠");
            Assert.AreEqual(@"E:\", PathLayout.SubDirectory(@"E:\", ""), "空子目录名只有根");
            Assert.AreEqual(@"E:\", PathLayout.SubDirectory(@"E:\", null), "null 子目录名只有根");
            Assert.AreEqual(@"D:\data\fu12ng\", PathLayout.SubDirectory(@"D:\data", "fu12ng"), "自定义根不乱补");
            Assert.AreEqual(@"E:\fu1ok\", PathLayout.SubDirectory(null, "fu1ok"), "根为 null 时兜底默认根");
        }

        [TestMethod]
        public void 默认值_与硬编码时代逐字一致()
        {
            // 不配 ini 时（根=默认），拼出来的必须与改动前的 @"E:\fu1ok\" 等一模一样
            Assert.AreEqual(@"E:\fu1ok\", PathLayout.SubDirectory(PathLayout.DefaultImageRoot, "fu1ok"), "第1路 OK 目录");
            Assert.AreEqual(@"E:\fu12ng\", PathLayout.SubDirectory(PathLayout.DefaultImageRoot, "fu12ng"), "第12路 NG 目录");
            Assert.AreEqual(@"E:\" + 生产统计 + @"\", PathLayout.SubDirectory(PathLayout.DefaultStatisticsRoot, 生产统计), "生产统计目录");
            Assert.AreEqual(@"E:\" + 每日统计 + @"\", PathLayout.SubDirectory(PathLayout.DefaultStatisticsRoot, 每日统计), "每日统计目录");
            Assert.AreEqual(@"E:\", PathLayout.NormalizeRoot(PathLayout.DefaultImageRoot, "ignored"), "默认值规范化后不变");
        }

        [TestMethod]
        public void 空fallback_也不抛()
        {
            Assert.AreEqual(PathLayout.DefaultStatisticsRoot, PathLayout.NormalizeRoot(null, null), "fallback 为 null 时用统计默认值");
            Assert.AreEqual(PathLayout.DefaultStatisticsRoot, PathLayout.NormalizeRoot(null, ""), "fallback 为空时用统计默认值");
        }
    }
}
