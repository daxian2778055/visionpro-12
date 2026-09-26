using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// FinsIniStore 的 ini 分段名映射（链 1 无后缀 = 与历史单链文件兼容；链 2+ 带 _finsN 后缀）。
    /// 这组映射一旦错位，多链配置互相覆盖，且现场看不出问题——纯函数回归价值高。
    /// </summary>
    [TestClass]
    public class FinsIniStoreSectionTests
    {
        [TestMethod]
        public void ConnSection_链1无后缀_其余带链号()
        {
            Assert.AreEqual("fins", FinsIniStore.ConnSection(1));
            Assert.AreEqual("fins2", FinsIniStore.ConnSection(2));
            Assert.AreEqual("fins4", FinsIniStore.ConnSection(4));
        }

        [TestMethod]
        public void BlockSection_链1纯块号_其余块号_fins链()
        {
            Assert.AreEqual("5", FinsIniStore.BlockSection(1, 5));
            Assert.AreEqual("5_fins2", FinsIniStore.BlockSection(2, 5));
            Assert.AreEqual("12_fins3", FinsIniStore.BlockSection(3, 12));
        }

        [TestMethod]
        public void CameraSection_链1无后缀_其余_fins链()
        {
            Assert.AreEqual("c13", FinsIniStore.CameraSection(1, 13));
            Assert.AreEqual("c7_fins3", FinsIniStore.CameraSection(3, 7));
        }

        [TestMethod]
        public void PathSection_链1path_其余path_fins链()
        {
            Assert.AreEqual("path", FinsIniStore.PathSection(1));
            Assert.AreEqual("path_fins2", FinsIniStore.PathSection(2));
        }
    }
}
