using System;

namespace WindowsFormsApplication1.Core.Logging
{
    /// <summary>
    /// 静态日志门面。服务代码可直接 <c>Log.Info/Error/Critical</c>，无需持有 <see cref="ILogger"/> 引用。
    /// 底层走 <see cref="LogManager.Default"/>（异步落盘），格式与旧 <c>ErrorLog</c> 一致。
    /// </summary>
    public static class Log
    {
        public static void Info(string message)
        {
            LogManager.Default.Write(message);
        }

        public static void Error(string message)
        {
            LogManager.Default.Write("ERROR", message);
        }

        public static void Critical(string message)
        {
            LogManager.Default.WriteCritical(message);
        }
    }
}
