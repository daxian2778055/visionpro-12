using System;
using System.Linq;
using WindowsFormsApplication1.Core.Infrastructure;   // ★M2/M3 告警落日志用 AppHost

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
        // ★M1 修复：string（非 int/long/float）不重复——xie 的 string 分支是"每个值写一个独立地址"，
        //   重复 N 次会横跨 N 个地址互相覆盖并越过块尾踩相邻块，比修复前更糟，故保持单值。
        // ★M2 说明：long/float 且寄存器数为奇数时，最后一个寄存器（通常是完成位）不写——
        //   多写一个会越过块尾踩相邻块；"批量+尾部单写"补齐方案待现场确认完成位语义后再定，当前仅告警一次。
        public static string RepeatForRegisters(string format, int registerCount)
        {
            string v = ForFormat(format);
            if (registerCount <= 1) return v;
            if (format != "int" && format != "long" && format != "float")
                return v;   // string 等非数值格式不适用整块重复（逐地址写语义）
            if ((format == "long" || format == "float") && (registerCount % 2) != 0)
                NotifyOddRegisters(format, registerCount);
            int values = (format == "long" || format == "float")
                ? Math.Max(1, registerCount / 2)
                : registerCount;
            return string.Join(",", Enumerable.Repeat(v, values));
        }

        // ★M3：block[2]（寄存器字数）未配置/非法 → TryParse 得 0，会静默退回"只写第 1 寄存器"的旧行为。
        // 由三协议在解析失败处调用；首次触发记日志（避免每帧刷屏）。
        private static int _regsUnsetNotice;
        public static void NotifyRegsUnset()
        {
            if (System.Threading.Interlocked.Exchange(ref _regsUnsetNotice, 1) == 0)
                SafeLog("失败帧整块填充：反馈块未配置有效寄存器字数(block[2])，本次只写第 1 个寄存器，请检查该块配置");
        }

        // ★M2：long/float 寄存器数为奇数时最后一个（疑似完成位）不写，首次告警提示现场确认。
        private static int _oddRegNotice;
        private static void NotifyOddRegisters(string format, int registerCount)
        {
            if (System.Threading.Interlocked.Exchange(ref _oddRegNotice, 1) == 0)
                SafeLog("失败帧整块填充：格式 " + format + " 的块寄存器数为奇数(" + registerCount
                    + ")，最后一个寄存器(疑似完成位)未写入，请确认现场约定");
        }

        // 告警落日志：服务未就绪时静默（告警本身不得影响回写主流程）
        private static void SafeLog(string msg)
        {
            try { AppHost.Services.Resolve<LoggingService>().WriteLog(msg); } catch { }
        }
    }

    public interface IPlcOutputWriter
    {
        void WriteCameraOutput(int camIdx, string value);
    }
}
