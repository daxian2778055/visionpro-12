using System;
using System.IO;

namespace WindowsFormsApplication1.Core.Logging
{
    /// <summary>
    /// 全局日志入口。业务代码通过 <see cref="Default"/> 取日志器，
    /// 后续接入 DI 时只需替换 <see cref="Initialize"/> 注入的实例。
    /// </summary>
    public static class LogManager
    {
        private static readonly object Sync = new object();
        private static ILogger _default;
        private static bool _shutdown;

        /// <summary>
        /// 默认日志器：写入 应用程序目录\Log\yyyy-MM-dd.txt。
        /// 惰性创建，首次访问时启动后台写线程。
        /// </summary>
        public static ILogger Default
        {
            get
            {
                if (_default == null)
                {
                    lock (Sync)
                    {
                        if (_default == null)
                        {
                            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                            _default = new AsyncFileLogger(dir);
                        }
                    }
                }
                return _default;
            }
        }

        /// <summary>替换默认日志器（测试或 DI 场景使用）。传入 null 恢复默认实现。</summary>
        public static void Initialize(ILogger logger)
        {
            lock (Sync)
            {
                if (_shutdown) return;

                ILogger old = _default;
                _default = logger;

                if (old != null && !object.ReferenceEquals(old, logger))
                {
                    try { old.Shutdown(TimeSpan.FromSeconds(2)); }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 程序退出时调用：落盘剩余日志并停止后台线程。可重复调用（幂等）。
        /// </summary>
        public static void Shutdown()
        {
            Shutdown(TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// 指定收尾超时。ProcessExit 等有执行时限的场合应传入较短的超时。
        /// </summary>
        public static void Shutdown(TimeSpan timeout)
        {
            lock (Sync)
            {
                if (_shutdown) return;
                _shutdown = true;

                if (_default != null)
                {
                    try { _default.Shutdown(timeout); }
                    catch { }
                }
            }
        }
    }
}
