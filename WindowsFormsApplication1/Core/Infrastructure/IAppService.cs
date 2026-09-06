using System;

namespace WindowsFormsApplication1.Core.Infrastructure
{
    /// <summary>
    /// 应用服务生命周期契约。所有由组合根（<see cref="ServiceProvider"/>）管理、需要统一
    /// 初始化/释放的服务实现此接口，便于在程序启动/退出时集中管控。
    /// </summary>
    public interface IAppService : IDisposable
    {
        /// <summary>初始化服务（建立连接、加载配置等）。须在 Start 之前调用一次。</summary>
        void Initialize();
    }
}
