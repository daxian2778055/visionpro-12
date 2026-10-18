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

        [TestMethod]
        public void 带回报_采纳配置与取了回退必须分得开()
        {
            // ★第61轮P2-4：启动那行「存图/统计根目录」的三分支（未配置 / 脏值已回退 / 按配置）
            // 全靠这个回报位分诊。它判错的方向就是「现场以为换了盘、其实一个字没生效」，逐分支钉住。
            bool used;
            Assert.AreEqual(@"D:\data\", PathLayout.NormalizeRoot(@"D:\data", PathLayout.DefaultImageRoot, out used),
                "值被采纳时结果就是规范化值");
            Assert.IsFalse(used, "采纳配置值不得报成「取了回退」");

            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot(null, PathLayout.DefaultImageRoot, out used),
                "null 取默认");
            Assert.IsTrue(used, "null 要报成回退（调用方据此写「未配置」）");

            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot("  \0 ", PathLayout.DefaultImageRoot, out used),
                "去 NUL+空白后为空取默认");
            Assert.IsTrue(used, "★配了但识别不了必须报成回退——现场最容易误判的就是这一支");

            Assert.AreEqual(PathLayout.DefaultImageRoot, PathLayout.NormalizeRoot("\"\"", PathLayout.DefaultImageRoot, out used),
                "成对空引号取默认");
            Assert.IsTrue(used, "引号里没内容同样算回退");

            Assert.AreEqual(@"D:\data\", PathLayout.NormalizeRoot(@"D:\data", PathLayout.DefaultImageRoot),
                "两参旧签名语义不变（三参只多一个回报，不改判词）");
        }

        [TestMethod]
        public void 方案路径等价_写法差异必须判等()
        {
            // ★第62轮自 Form1.cs 搬入的判据。它守的是"同路径不重复切换"——漏判的代价是多起一次
            // 整机卸载/重载（几十秒停检），所以四种"人眼看起来同一个文件"的写法都要判等。
            string cur = @"E:\方案\20A.vpp";
            Assert.IsTrue(PathLayout.SchemePathEquals(cur, cur), "完全相同");
            Assert.IsTrue(PathLayout.SchemePathEquals(cur, @"E:\方案\20a.vpp"), "仅大小写不同");
            Assert.IsTrue(PathLayout.SchemePathEquals(cur, @"E:/方案/20A.vpp"), "正斜杠写法");
            Assert.IsTrue(PathLayout.SchemePathEquals(cur, "   E:\\方案\\20A.vpp   "), "两端空白");
            Assert.IsTrue(PathLayout.SchemePathEquals(cur, "E:\\方案\\20A.vpp\0"), "尾部 NUL（ini 脏值）");
            Assert.IsTrue(PathLayout.SchemePathEquals(cur, @"E:\方案\20A.vpp\"), "只有末尾斜杠之差");
            Assert.IsTrue(PathLayout.SchemePathEquals(null, ""), "null 与空串同为空键");
        }

        [TestMethod]
        public void 方案路径等价_相近路径必须判不等()
        {
            // 反方向同样要钉：这条判据一旦把两个不同方案判成相等，PLC 要的换型会被"静默跳过"，
            // 现场看到的就是"发了换型字符但画面还是旧方案"——比漏判更坑人，所以逐条钉死。
            string cur = @"E:\方案\20A.vpp";
            Assert.IsFalse(PathLayout.SchemePathEquals(cur, @"E:\方案2\20A.vpp"), "不同目录");
            Assert.IsFalse(PathLayout.SchemePathEquals(cur, @"E:\方案\20B.vpp"), "不同文件");
            Assert.IsFalse(PathLayout.SchemePathEquals(cur, @"E:\方案\20AB.vpp"), "前缀相似（20A vs 20AB）");
            Assert.IsFalse(PathLayout.SchemePathEquals(cur, @"E:\方案\20A.vppx"), "扩展名相似（.vpp vs .vppx）");
            Assert.IsFalse(PathLayout.SchemePathEquals(cur, ""), "空目标不得被判成与当前方案相同");
            // 连续分隔符**刻意不折叠**：折叠会把 UNC 开头（\\server\share）一并改坏，
            // 而这一写法在现场从未出现过；记在这里，将来要折叠的人得先解决 UNC 那条。
            Assert.IsFalse(PathLayout.SchemePathEquals(cur, @"E:\\方案\\20A.vpp"), "双反斜杠不折叠（保 UNC）");
        }
    }
}
