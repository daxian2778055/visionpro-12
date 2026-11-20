using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ch:CRC-16：交叉验证生产代码 Modbus.GetCRC16。
    /// 参考实现按 Modbus 教科书独立写出（poly 0xA001 / init 0xFFFF / 低位先出），
    /// 不调用也不复刻生产实现——避免"复刻只在写下那一刻一致、之后生产改了测试跟着失效"的陷阱。
    /// GetCRC16 返回 data + 2 字节 CRC（低字节在前），本类同时钉住该布局与"原字节不被改动"。
    /// </summary>
    [TestClass]
    public class ModbusCrcTests
    {
        // 独立参考实现：返回标准 Modbus CRC-16 值（整数），与生产 return 布局无关
        private static ushort ReferenceCrc(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int j = 0; j < 8; j++)
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
            }
            return crc;
        }

        private static void AssertMatchesReference(byte[] payload, string why)
        {
            byte[] result = Modbus.GetCRC16(payload);
            ushort expect = ReferenceCrc(payload);
            Assert.AreEqual(payload.Length + 2, result.Length, why + "：只追加 2 字节 CRC");
            byte lo = (byte)(expect & 0xFF);
            byte hi = (byte)(expect >> 8);
            Assert.AreEqual(lo, result[payload.Length], why + "：CRC 低字节在前");
            Assert.AreEqual(hi, result[payload.Length + 1], why + "：CRC 高字节在后");
            for (int i = 0; i < payload.Length; i++)
                Assert.AreEqual(payload[i], result[i], why + "：原始字节必须原样保留");
        }

        [TestMethod]
        public void GetCRC16_全零两字节_与参考实现一致()
        {
            AssertMatchesReference(new byte[] { 0x00, 0x00 }, "全零");
        }

        [TestMethod]
        public void GetCRC16_FC03读寄存器请求_与参考实现一致()
        {
            // 从站1 读保持寄存器 0x0000 起 2 个：01 03 00 00 00 02
            AssertMatchesReference(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02 }, "FC03 读");
        }

        [TestMethod]
        public void GetCRC16_FC10写多寄存器请求_与参考实现一致()
        {
            // 从站1 写保持寄存器 0x0000 起 2 个，字节数4，值 00 01 00 02
            AssertMatchesReference(new byte[] { 0x01, 0x10, 0x00, 0x00, 0x00, 0x02, 0x04, 0x00, 0x01, 0x00, 0x02 }, "FC10 写");
        }

        [TestMethod]
        public void GetCRC16_不同输入必须产生不同CRC()
        {
            byte[] a = Modbus.GetCRC16(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02 });
            byte[] b = Modbus.GetCRC16(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x03 });
            Assert.IsFalse(a[6] == b[6] && a[7] == b[7], "末尾一个数据字节不同，CRC 必须不同");
        }

        [TestMethod]
        public void GetCRC16_空数组_只产出两字节CRC()
        {
            byte[] result = Modbus.GetCRC16(new byte[0]);
            ushort expect = ReferenceCrc(new byte[0]);   // 0xFFFF
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual((byte)(expect & 0xFF), result[0]);
            Assert.AreEqual((byte)(expect >> 8), result[1]);
        }
    }
}
