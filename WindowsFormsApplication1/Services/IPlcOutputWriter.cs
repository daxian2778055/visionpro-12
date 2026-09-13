using System;

namespace WindowsFormsApplication1
{
    // PLC 输出写出的统一抽象：getrecord 中对 Omron / ModbusTCP / ModbusRTU
    // 三套近重复写出逻辑收口到各窗体的 WriteCameraOutput 实现。
    public static class InspectionFailureOutput
    {
        // Site contract: acquisition/inspection faults use 999 in numeric registers.
        public static string ForFormat(string format)
        {
            return format == "int" || format == "long" || format == "float" ? "999" : "Reject";
        }
    }

    public interface IPlcOutputWriter
    {
        void WriteCameraOutput(int camIdx, string value);
    }
}
