using System;
using System.Threading;
using MvCamCtrl.NET;

namespace WindowsFormsApplication1.Core.Camera
{
    /// <summary>
    /// 相机编排服务（薄封装）。收口 Form1 中分散的 12 路相机所有权与共享状态，
    /// 通过 <see cref="Cameras"/>[slot] 直接访问底层 MyCamera 实例，业务代码无需改动任何 SDK 调用。
    /// <para>
    /// 取代原先 Form1 直接持有 <c>MyCamera[12]</c> 数组的做法；后续可在此逐步收口创建/重连、
    /// 取流事件转发与共享缓冲池，而 Form1 调用面保持不变。
    /// </para>
    /// <para>
    /// ★ 释放所有权：本类持有哪些相机就由本类负责释放（<see cref="ReleaseAllCameras"/> / <see cref="Dispose"/>），
    /// 不再让 <c>Dispose()</c> 成为空实现。
    /// </para>
    /// </summary>
    public sealed class CameraController : IDisposable
    {
        private readonly MyCamera[] _cameras = new MyCamera[12];
        private volatile bool _disposed;

        /// <summary>底层相机数组（按槽位索引访问）。</summary>
        public MyCamera[] Cameras
        {
            get { return _cameras; }
        }

        /// <summary>该槽位相机是否已创建（非空）。</summary>
        public bool IsCreated(int slot)
        {
            return slot >= 0 && slot < 12 && _cameras[slot] != null;
        }

        /// <summary>已创建（非空）的相机数量。</summary>
        public int Count
        {
            get
            {
                int n = 0;
                for (int i = 0; i < 12; i++)
                    if (_cameras[i] != null) n++;
                return n;
            }
        }

        /// <summary>
        /// 释放全部 12 路相机：停止抓流 → 关闭设备 → 销毁句柄，并清空槽位。
        /// <para>逐路、逐操作容错：某一步失败不影响其它路；已为空的槽位直接跳过（可重复调用）。</para>
        /// <para>★ 与 Form1 原先的三处手写循环语义一致，仅把"释放所有权"收口到本类。</para>
        /// </summary>
        /// <param name="stopSleepMs">停止抓流后等待 SDK 回调线程退出的时间（毫秒，0=不等待）。</param>
        /// <param name="closeSleepMs">关闭设备后等待时间（毫秒，0=不等待）。</param>
        /// <returns>实际释放的路数。</returns>
        public int ReleaseAllCameras(int stopSleepMs = 100, int closeSleepMs = 50)
        {
            int released = 0;
            for (int i = 0; i < _cameras.Length; i++)
            {
                MyCamera cam = _cameras[i];
                if (cam == null) continue;

                try
                {
                    // 1. 停止抓流（未在抓流的相机调用此接口只会返回错误码，不会抛异常）
                    try { cam.MV_CC_StopGrabbing_NET(); } catch { }
                    if (stopSleepMs > 0) Thread.Sleep(stopSleepMs);

                    // 2. 关闭设备
                    try { cam.MV_CC_CloseDevice_NET(); } catch { }
                    if (closeSleepMs > 0) Thread.Sleep(closeSleepMs);

                    // 3. 销毁设备对象（MyCamera.Dispose 内亦为幂等实现）
                    try { cam.MV_CC_DestroyDevice_NET(); } catch { }

                    released++;
                }
                catch { }
                finally
                {
                    _cameras[i] = null;
                }
            }
            return released;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // 作为"最后兜底"的释放路径，不做逐路等待：优雅释放（含等待 SDK 回调退出）由
            // Form1 的正常关闭流程负责；这里只需保证句柄不会泄漏。
            try { ReleaseAllCameras(0, 0); }
            catch { }
        }
    }
}
