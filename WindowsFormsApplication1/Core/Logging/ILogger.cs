using System;

namespace WindowsFormsApplication1.Core.Logging
{
    /// <summary>
    /// 日志抽象。
    /// <para>
    /// <see cref="Write(string)"/> 走异步队列（非阻塞），适用于业务代码与相机检测热路径；
    /// <see cref="WriteCritical(string)"/> 同步直写，适用于崩溃回调、进程退出等
    /// 后台线程可能已经失效或托管堆已损坏的场景。
    /// </para>
    /// </summary>
    public interface ILogger
    {
        /// <summary>异步写一条日志。</summary>
        void Write(string message);

        /// <summary>异步写一条带分类的日志。</summary>
        void Write(string category, string message);

        /// <summary>同步直写，绕过后台队列。仅用于崩溃/退出路径，业务代码请勿使用。</summary>
        void WriteCritical(string message);

        /// <summary>等待队列中的日志全部落盘，最长等待 <paramref name="timeout"/>。</summary>
        void Flush(TimeSpan timeout);

        /// <summary>停止后台写线程并落盘剩余日志。</summary>
        void Shutdown(TimeSpan timeout);

        /// <summary>因队列已满而丢弃的日志条数。用于观测日志系统自身是否过载。</summary>
        int DroppedCount { get; }
    }
}
