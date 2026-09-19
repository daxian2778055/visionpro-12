using System;
using System.Linq;

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

        // 失败帧整块填充（现场约定 2026-09-19）：一个相机可绑多个反馈寄存器（如 8 个判定结果 + 1 个完成位）。
        // 原失败路径只发单个值 → xie 按逗号拆值只写第一个寄存器，其余寄存器与完成位停留在旧值，PLC 误读。
        // 本方法把故障值重复成 registerCount 个逗号分隔值，整块一次写齐；完成位不特殊处理，同样写故障值。
        // long/float 每个值占 2 个寄存器，故值个数按寄存器数折半。
        public static string RepeatForRegisters(string format, int registerCount)
        {
            string v = ForFormat(format);
            if (registerCount <= 1) return v;
            int values = (format == "long" || format == "float")
                ? Math.Max(1, registerCount / 2)
                : registerCount;
            return string.Join(",", Enumerable.Repeat(v, values));
        }
    }

    public interface IPlcOutputWriter
    {
        void WriteCameraOutput(int camIdx, string value);
    }
}
