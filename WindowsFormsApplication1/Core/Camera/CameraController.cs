using System;
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
