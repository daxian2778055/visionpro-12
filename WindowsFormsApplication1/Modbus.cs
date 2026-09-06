using System;
using System.IO.Ports;
using System.Threading;

namespace WindowsFormsApplication1
{
    public class Modbus
    {
        public SerialPort port;
        private ErrorLog MsgErroeLog = new ErrorLog();

        // 读取超时设置（毫秒）
        private const int DefaultReadTimeout = 1000;
        private const int DefaultWriteTimeout = 1000;

        /// <summary>
        /// 读取数据并对齐帧头
        /// </summary>
        protected int ReadData(byte[] buffer, out string b, int expectCount)
        {
            b = "";
            try
            {
                if (port == null || !port.IsOpen)
                {
                    MsgErroeLog.WriteLog("Modbus串口未打开");
                    return 0;
                }

                int oldReadTimeout = port.ReadTimeout;
                port.ReadTimeout = DefaultReadTimeout;

                try
                {
                    // 使用ReadTimeout而非阻塞读取，避免永久卡死
                    int offset = 0;
                    while (offset < expectCount && offset < buffer.Length)
                    {
                        int read = port.Read(buffer, offset, Math.Min(expectCount - offset, buffer.Length - offset));
                        if (read == 0)
                        {
                            Thread.Sleep(10); // 等待数据到达
                            continue;
                        }
                        offset += read;
                    }

                    int r = offset;
                    byte[] buffer1 = buffer;

                    if (expectCount == 8)
                    {
                        port.DiscardInBuffer();
                    }
                    else if (buffer1.Length > 0 && buffer1[0] == 0X01 && buffer1[1] == 0X03)
                    {
                        for (int i = 0; i < buffer1.Length; i++)
                        {
                            if (i >= 3 && i <= buffer1.Length - 3)
                                b += buffer1[i].ToString("X2") + " ";
                        }
                        port.DiscardInBuffer();
                    }
                    else
                    {
                        // 尝试对齐到标准帧头
                        int maxAttempts = 20;
                        while ((buffer1.Length <= 0 || buffer1[0] != 0X01 || (buffer1.Length > 1 && buffer1[1] != 0X03))
                               && maxAttempts-- > 0)
                        {
                            int n = port.Read(buffer, 0, buffer1.Length);
                            buffer1 = buffer;
                            if (n == 0)
                            {
                                Thread.Sleep(10);
                                continue;
                            }
                            if (buffer1.Length > 1 && buffer1[0] == 0X01 && buffer1[1] == 0X03)
                            {
                                for (int i = 0; i < buffer1.Length; i++)
                                {
                                    b += buffer1[i].ToString("X2") + " ";
                                }
                                port.DiscardInBuffer();
                                break;
                            }
                        }
                    }
                    return r;
                }
                finally
                {
                    port.ReadTimeout = oldReadTimeout;
                }
            }
            catch (TimeoutException)
            {
                b = "";
                MsgErroeLog.WriteLog("Modbus读取超时");
                return 0;
            }
            catch (Exception ex)
            {
                b = "";
                MsgErroeLog.WriteLog("Modbus读取异常: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 读取保持型寄存器 功能码03
        /// </summary>
        public int ReadRegister(int stationID, ushort addr, int length, out string a, out string b, byte[] buffer)
        {
            a = "";
            b = "";
            try
            {
                if (port == null || !port.IsOpen)
                {
                    MsgErroeLog.WriteLog("Modbus串口未打开，无法读取");
                    return 0;
                }

                byte[] cmdHead = new byte[6];
                cmdHead[0] = (byte)stationID;
                cmdHead[1] = 0x03;
                cmdHead[2] = (byte)((addr >> 8) & 0xFF);
                cmdHead[3] = (byte)(addr & 0xFF);
                cmdHead[4] = (byte)((length >> 8) & 0xFF);
                cmdHead[5] = (byte)(length & 0xFF);
                byte[] command = GetCRC16(cmdHead);

                for (int i = 0; i < command.Length; i++)
                {
                    a += command[i].ToString("X2") + " ";
                }

                // 发送指令
                port.Write(command, 0, command.Length);

                // 等待响应
                Thread.Sleep(200);

                int bytes = ReadData(buffer, out b, 5 + 2 * length);
                return bytes;
            }
            catch (TimeoutException)
            {
                MsgErroeLog.WriteLog("Modbus读寄存器超时");
                return 0;
            }
            catch (Exception ex)
            {
                MsgErroeLog.WriteLog("Modbus读寄存器异常: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 写入保持型寄存器 功能码06
        /// </summary>
        public int WriteRegister(int stationID, ushort addr, int value, out string a, byte[] buffer = null)
        {
            a = "";
            try
            {
                if (port == null || !port.IsOpen)
                {
                    MsgErroeLog.WriteLog("Modbus串口未打开，无法写入");
                    return 0;
                }

                byte[] cmdHead = new byte[6];
                cmdHead[0] = (byte)stationID;
                cmdHead[1] = 0x06;
                cmdHead[2] = (byte)((addr >> 8) & 0xFF);
                cmdHead[3] = (byte)(addr & 0xFF);
                cmdHead[4] = (byte)((value >> 8) & 0xFF);
                cmdHead[5] = (byte)(value & 0xFF);
                byte[] command = GetCRC16(cmdHead);

                for (int i = 0; i < command.Length; i++)
                {
                    a += command[i].ToString("X2") + " ";
                }

                port.Write(command, 0, command.Length);
                string b;
                int bytes = ReadData(command, out b, 8);
                return bytes;
            }
            catch (TimeoutException)
            {
                MsgErroeLog.WriteLog("Modbus写寄存器超时");
                return 0;
            }
            catch (Exception ex)
            {
                MsgErroeLog.WriteLog("Modbus写寄存器异常: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 写入一段连续的寄存器 功能码10
        /// </summary>
        public int WriteRegisters(int stationID, ushort addr, byte[] value, out string a, byte[] buffer = null)
        {
            a = "";
            try
            {
                if (port == null || !port.IsOpen)
                {
                    MsgErroeLog.WriteLog("Modbus串口未打开，无法批量写入");
                    return 0;
                }

                int registerCount = value.Length / 2;
                byte[] cmdHead = new byte[7 + value.Length];
                cmdHead[0] = (byte)stationID;
                cmdHead[1] = 0x10;
                cmdHead[2] = (byte)((addr >> 8) & 0xFF);
                cmdHead[3] = (byte)(addr & 0xFF);
                cmdHead[4] = (byte)((registerCount >> 8) & 0xFF);
                cmdHead[5] = (byte)(registerCount & 0xFF);
                cmdHead[6] = (byte)(value.Length & 0xFF);
                for (int i = 0; i < value.Length; i++)
                {
                    cmdHead[7 + i] = value[i];
                }
                byte[] command = GetCRC16(cmdHead);

                for (int i = 0; i < command.Length; i++)
                {
                    a += command[i].ToString("X2") + " ";
                }

                port.Write(command, 0, command.Length);
                string b;
                int bytes = ReadData(command, out b, 8);
                return bytes;
            }
            catch (TimeoutException)
            {
                MsgErroeLog.WriteLog("Modbus批量写入超时");
                return 0;
            }
            catch (Exception ex)
            {
                MsgErroeLog.WriteLog("Modbus批量写入异常: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// CRC校验，参数data为byte数组
        /// </summary>
        public static byte[] GetCRC16(byte[] data)
        {
            byte[] data1 = new byte[data.Length + 2];
            int crc = 0xffff;
            for (int i = 0; i < data.Length; i++)
            {
                crc = crc ^ data[i];
                for (int j = 0; j < 8; j++)
                {
                    int temp = crc & 1;
                    crc = crc >> 1;
                    crc = crc & 0x7fff;
                    if (temp == 1)
                    {
                        crc = crc ^ 0xa001;
                    }
                    crc = crc & 0xffff;
                }
            }
            byte[] crc16 = new byte[2];
            crc16[1] = (byte)((crc >> 8) & 0xff);
            crc16[0] = (byte)(crc & 0xff);
            Array.Copy(data, 0, data1, 0, data.Length);
            Array.Copy(crc16, 0, data1, data.Length, crc16.Length);
            return data1;
        }
    }
}
