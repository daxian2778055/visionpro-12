using System;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Configuration;
using WindowsFormsApplication1.Core.Camera;

namespace WindowsFormsApplication1.Core.Infrastructure
{
    /// <summary>
    /// 组合根（Composition Root）。在程序启动早期调用一次 <see cref="Build"/> 注册全部应用服务，
    /// 业务代码（Form1 及各服务）通过 <see cref="Services"/> 解析依赖，避免散落 new。
    /// <para>注册顺序：先基础设施（配置、日志），再上层服务（相机、统计、通讯）。</para>
    /// </summary>
    public static class AppHost
    {
        private static readonly ServiceProvider _provider = new ServiceProvider();
        private static readonly object _buildLock = new object();
        private static volatile bool _built;

        /// <summary>全局服务容器。</summary>
        public static ServiceProvider Services
        {
            get { return _provider; }
        }

        /// <summary>
        /// 注册并初始化所有应用服务。幂等，可重复调用。
        /// 应在 Program.Main 中、创建主窗体之前调用。
        /// </summary>
        public static void Build()
        {
            lock (_buildLock)
            {
                if (_built) return;

                // 基础设施
                _provider.RegisterSingleton<IAppConfiguration>(new AppConfiguration());

                // 相机编排服务（薄封装 12 路 MyCamera，收口相机所有权与共享状态）
                _provider.RegisterSingleton<CameraController>(new CameraController());

                // 生产统计服务（委托既有 RunLog 写 E:\生产统计 / E:\每日统计 CSV 良率统计）
                _provider.RegisterSingleton<StatisticsService>(new StatisticsService());

                // 日志服务（委托既有 ErrorLog 异步落盘，业务与检测热路径统一使用）
                _provider.RegisterSingleton<LoggingService>(new LoggingService());

                // 配置服务（委托既有 ClassIni 读写 code.ini 等配置文件）
                _provider.RegisterSingleton<ConfigService>(new ConfigService());

                // 通讯服务（集中持有 4 类 PLC 客户端：Modbus-TCP / RTU / Omron / 三菱 FX）
                _provider.RegisterSingleton<CommunicationService>(new CommunicationService());

                // 连接运行时管理器（多实例改造 阶段 2-C）：集中登记所有连接运行时（ModbusTcpRuntime），
                // 供后续“触发来自哪个连接 / 结果回写给哪个连接”按 LinkId 查找目标。当前仅 1 个实例，
                // 行为与改造前完全一致；阶段5 配置多段化后即可容纳第 2、第 3 个连接。
                _provider.RegisterSingleton<CommLinkManager>(new CommLinkManager());

                // 作业服务（集中持有 12 路 Myjob 实例、运行开关 yunxing，并收口通讯触发路由与门控状态）
                // 注入通讯/相机/日志服务，使 ApplyCommTrigger 等纯路由逻辑可独立运行（不依赖 UI）。
                var commSvc = _provider.Resolve<CommunicationService>();
                var camSvc = _provider.Resolve<CameraController>();
                var logSvc = _provider.Resolve<LoggingService>();
                _provider.RegisterSingleton<JobService>(new JobService(commSvc, camSvc, logSvc));

                // ★ N3：全部注册成功后才置位。中途抛异常时 _built 保持 false，允许重试，
                //        与"幂等、可重复调用"的语义一致（旧实现在注册前就置位，失败后永不重试）。
                _built = true;
            }
        }

        /// <summary>
        /// 关闭组合根：释放实现 <see cref="IDisposable"/> 的单例服务（如 ConfigService → ClassIni 刷新）。
        /// 幂等，可在退出路径安全调用。
        /// </summary>
        public static void Shutdown()
        {
            try { _provider.DisposeAll(); } catch { }
        }
    }
}
