using System;

namespace WindowsFormsApplication1
{
    // PLC 输出写出的统一抽象：getrecord 中对 Omron / ModbusTCP / ModbusRTU
    // 三套近重复写出逻辑收口到各窗体的 WriteCameraOutput 实现。
    public interface IPlcOutputWriter
    {
        void WriteCameraOutput(int camIdx, string value);
    }
}
