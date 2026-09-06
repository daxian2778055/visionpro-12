using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 日志服务。收口原 Form1 中散落的 <see cref="ErrorLog"/> 调用（异步落盘到错误日志文件）。
    /// <para>
    /// 内部委托既有的 <see cref="ErrorLog"/> 实现，保持日志文本格式与落盘行为完全不变；
    /// Form1 只通过本服务记录日志，不再直接持有 <see cref="ErrorLog"/> 实例，便于后续统一日志配置与单测。
    /// </para>
    /// </summary>
    public sealed class LoggingService
    {
        private readonly ErrorLog _errorLog = new ErrorLog();

        /// <summary>异步写日志。业务代码与检测热路径统一使用此方法。</summary>
        public void WriteLog(string msg)
        {
            _errorLog.WriteLog(msg);
        }

        /// <summary>同步直写，仅用于崩溃回调、进程退出等后台写线程可能已停止的场景。</summary>
        public void WriteLogSync(string msg)
        {
            _errorLog.WriteLogSync(msg);
        }
    }
}
