using System;

namespace WindowsFormsApplication1.Core.Threading
{
    /// <summary>
    /// 跨线程 UI 派发抽象。
    /// <para>
    /// 项目现状：主窗体构造函数里设置了 <c>CheckForIllegalCrossThreadCalls = false</c>，
    /// 关闭了跨线程检查，导致后台线程直接访问控件的问题被掩盖；
    /// 同时又散落着裸 <c>this.Invoke</c> / <c>BeginInvoke</c>，窗体关闭后调用会抛
    /// ObjectDisposedException / InvalidOperationException，成为随机崩溃源。
    /// </para>
    /// 本接口提供统一、安全（可静默失败）的派发入口，供后续逐处替换。
    /// </summary>
    public interface IUiDispatcher
    {
        /// <summary>当前线程是否需要通过 marshaling 访问 UI。</summary>
        bool InvokeRequired { get; }

        /// <summary>同步派发。控件已释放时抛出，调用方需确保生命周期。</summary>
        void Invoke(Action action);

        /// <summary>异步派发。控件已释放时抛出，调用方需确保生命周期。</summary>
        void BeginInvoke(Action action);

        /// <summary>
        /// 尽力异步派发：控件已释放或句柄未创建时静默返回 false，不抛异常。
        /// 后台线程刷新界面（状态灯、表格、计数）应统一使用此方法。
        /// </summary>
        bool TryBeginInvoke(Action action);
    }
}
