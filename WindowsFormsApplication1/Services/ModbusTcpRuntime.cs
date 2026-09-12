using System;
using System.Collections.Generic;
using System.Threading;
using HslCommunication.ModBus;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 运行时宿主接口（阶段 2-B）：引擎线程体仍由窗体提供（原 Fins_duxie），
    /// 本类只负责线程的创建 / 启动 / 停止 / Join。
    /// 仅当连接未配置 <see cref="ICommLinkContext"/>（即当前唯一连接）时使用。
    /// </summary>
    public interface ICommRuntimeHost
    {
        bool StopRequested { get; set; }
        Thread PollThread { get; set; }
        void RunPollLoop();
    }

    /// <summary>
    /// 每连接上下文接口（多实例改造 阶段 2-C per-link）。
    ///
    /// 把 Fins_duxie 循环里“随连接独立”的配置（轮询间隔 / 数据块 / 相机绑定 / 地址基准 / 方案路径等）
    /// 与“保持窗体级”的回调（UI 表格刷新 / 触发事件 / 反馈回写 / 方案切换 / 日志 / 重连）解耦。
    ///
    /// 当前唯一连接（链接 1）仍走原 Fins_duxie（窗体 RunPollLoop），本接口暂未由窗体实现、
    /// PollLoop 处于休眠；待真机验证阶段 5 多连接并联时，窗体为每个连接实现本接口即可启用 PollLoop。
    /// </summary>
    public interface ICommLinkContext
    {
        // ===== 每连接配置（链接 1 由窗体返回其现有字段，读取取实时值） =====
        /// <summary>整体初始化完成标志（原 chushihua）。</summary>
        bool IsInitialized { get; }
        /// <summary>轮询间隔（毫秒，原 lunxun_time）。</summary>
        int PollInterval { get; }
        /// <summary>轮询使能（原 fins_lunxunen）。</summary>
        bool IsPollEnabled { get; }
        /// <summary>通讯使能（原 fins_en）。</summary>
        bool IsCommEnabled { get; }
        /// <summary>地址起始偏移（原 address_qishi）。</summary>
        int AddressBase { get; }
        /// <summary>数据块配置（原 fins_dic，键为字符串）。</summary>
        Dictionary<string, string[]> FinsBlocks { get; }
        /// <summary>相机绑定配置（原 camera_dic）。</summary>
        Dictionary<int, string[]> CameraBindings { get; }
        /// <summary>方案切换路径（原 lujing）。</summary>
        string SchemePath { get; }

        // ===== 窗体级 / 共享回调（保持原有 UI 与事件不变） =====
        /// <summary>写日志（原 MsgErroeLog.WriteLog）。</summary>
        void Log(string message);
        /// <summary>刷新轮询表格单元（原 CommGridHelper.SetPollCell(_gridUi, fins_data, ...)）。</summary>
        void UpdatePollCell(int row, string value);
        /// <summary>触发选择变更事件（原 getData 事件，third 为原事件第 3 参）。</summary>
        void RaiseSelectionChanged(string dataVal, string camKey, string third, int linkId);
        /// <summary>触发反馈回写（原 WriteTriggerFanhuizhi）。</summary>
        void WriteTriggerFeedback(string[] block, string value, ref int xuanzhong, ref string fins);
        /// <summary>方案切换（封装原 qiehuanzhong/lujing/qiehuan 逻辑），返回是否触发切换。</summary>
        bool TrySchemeSwitch(string dataVal);
        /// <summary>取字符串中段（原 GetMiddleValue）。</summary>
        string MiddleValue(string str, string sta, string end);
        /// <summary>后台自动重连（原 TryAutoReconnect）。</summary>
        void OnReconnect();
    }

    /// <summary>
    /// Modbus TCP 运行时引擎（多实例改造 阶段 2-B / 2-C）。
    ///
    /// 一个 ModbusTcpRuntime 实例 = 一个 Modbus TCP 连接的“运行时”（连接 + 轮询线程生命周期 + 每连接状态）。
    /// 本阶段只实例化 1 个（与改造前行为一致）；它已是可被 new 多次的对象，为阶段 2-C 多实例铺路。
    ///
    /// 线程驱动策略：
    /// - 若 <see cref="Context"/> 为 null（当前唯一连接）：沿用窗体 RunPollLoop（即原 Fins_duxie），
    ///   算法与共享状态完全不变，行为逐字节一致。
    /// - 若配置了 Context（阶段 5 多连接并联时）：运行本类的 <see cref="PollLoop"/>，
    ///   配置经 Context 实时读取、状态收口到本实例，天然支持第 2/3/4 个连接互不干扰。
    /// </summary>
    public class ModbusTcpRuntime : ICommLink
    {
        private readonly ModbusTcpLink _link;
        private readonly ICommRuntimeHost _host;
        private ModbusTcpLinkConfig _config;

        // 每连接运行时状态（阶段 2-C 起收口到实例，避免多连接共享窗体字段）
        private readonly Dictionary<int, bool> _triggerLatch = new Dictionary<int, bool>();
        private int _commFailCount = 0;
        private int _reconnecting = 0;
        private long _lastReconnectAttemptTicks = 0;
        private volatile bool _stopPolling = false;
        private Thread _pollThread = null;

        public ModbusTcpRuntime(ModbusTcpLink link, ICommRuntimeHost host)
        {
            _link = link;
            _host = host;
            LinkId = 1;
            Name = "ModbusTCP-1";
        }

        /// <summary>每连接上下文（多实例启用时赋值；为 null 时走窗体原 Fins_duxie）。</summary>
        public ICommLinkContext Context { get; set; }

        public int LinkId { get; set; }
        public string Name { get; set; }
        public string Protocol { get { return "modbustcp"; } }

        public bool IsConnected { get { return _link != null && _link.IsConnected; } }

        /// <summary>连接参数配置（阶段 2-C 起由 ModbusTcpIniStore 按实例加载并赋值）。</summary>
        public ModbusTcpLinkConfig Config
        {
            get { return _config; }
            set { _config = value; if (_config != null) _config.LinkId = LinkId; }
        }

        /// <summary>底层连接（阶段 2-C 起对外暴露，供触发路由 / 反馈回写使用）。</summary>
        public ModbusTcpLink Link { get { return _link; } }

        /// <summary>启动轮询线程（幂等：已在跑则忽略）。按是否配置 Context 分流驱动方式。</summary>
        public void Start()
        {
            if (Context == null)
            {
                if (_host.PollThread != null && _host.PollThread.IsAlive) return;
                // ★A2 修复：必须复位停止标志，否则"停止后再启动"的新线程会在 while(!StopRequested) 处立即退出
                _host.StopRequested = false;
                _host.PollThread = new Thread(new ThreadStart(_host.RunPollLoop));
                _host.PollThread.IsBackground = true;
                _host.PollThread.Start();
            }
            else
            {
                if (_pollThread != null && _pollThread.IsAlive) return;
                _stopPolling = false;
                _pollThread = new Thread(new ThreadStart(PollLoop));
                _pollThread.IsBackground = true;
                _pollThread.Start();
            }
        }

        /// <summary>停止轮询线程：置停止标志并等待退出（最多 1 秒）。按是否配置 Context 分流。</summary>
        public void Stop()
        {
            if (Context == null)
            {
                _host.StopRequested = true;
                Thread t = _host.PollThread;
                if (t != null)
                {
                    try { t.Join(1000); } catch { }
                    // ★A2 修复：只有线程确实退出才释放引用。Join 超时仍存活时保留引用，
                    // 否则"旧线程还在跑却失去引用"，再次 Start 会起出第二条轮询线程（重复触发）。
                    if (!t.IsAlive) _host.PollThread = null;
                }
            }
            else
            {
                _stopPolling = true;
                Thread t = _pollThread;
                if (t != null)
                {
                    try { t.Join(1000); } catch { }
                    if (!t.IsAlive) _pollThread = null;
                }
            }
        }

        public void Close()
        {
            Stop();
            if (_link != null) _link.Close();
        }

        public bool Reconnect()
        {
            return _link != null && _link.Reconnect();
        }

        /// <summary>
        /// 按连接参数化的轮询循环（阶段 2-C per-link，休眠中）。
        /// 由原 Fins_duxie 逐行 port：配置经 <see cref="Context"/> 实时读取，状态收口到本实例，
        /// 窗体级副作用（UI/事件/回写/切换/日志/重连）全部经 Context 回调，保持与改造前一致。
        /// 仅在 <see cref="Context"/> 非空（多连接并联）时被 <see cref="Start"/> 调用。
        /// </summary>
        private void PollLoop()
        {
            ICommLinkContext ctx = Context;
            while (!_stopPolling)
            {
                int pollInterval = ctx.PollInterval;
                if (pollInterval <= 0) pollInterval = 100;
                Thread.Sleep(pollInterval);
                if (ctx.PollInterval <= 0)
                    continue;
                try
                {
                    if (ctx.IsInitialized)
                    {
                        if (_reconnecting != 0)
                            continue;
                        if (ctx.IsPollEnabled)
                        {
                            int xuanzhong_temp = 0;
                            string fins_temp = "";
                            string shuju_temp = "";
                            if (ctx.FinsBlocks.Count > 0)
                            {
                                foreach (var par in ctx.FinsBlocks)
                                {
                                    shuju_temp = "";
                                    for (int j = 0; j < int.Parse(par.Value[2]); j++)
                                    {
                                        if (par.Value[4] == "int")
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadInt16((int.Parse(par.Value[1]) + j).ToString()), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "string")
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadString((int.Parse(par.Value[1]) + j).ToString(), 1), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "long" && j % 2 == 0)
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadInt32((int.Parse(par.Value[1]) + j).ToString()), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "float" && j % 2 == 0)
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadFloat((int.Parse(par.Value[1]) + j).ToString()), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                    }
                                    if (par.Value[3] == "触发")
                                    {
                                        foreach (var pap in ctx.CameraBindings)
                                        {
                                            if (pap.Value[0] == par.Value[0])
                                            {
                                                if (pap.Key == 13)
                                                {
                                                    if (ctx.TrySchemeSwitch(shuju_temp.Replace("\0", "")))
                                                    {
                                                        ctx.RaiseSelectionChanged(shuju_temp, pap.Key.ToString(), "0", LinkId);
                                                    }
                                                }
                                                else if (pap.Key != 13)
                                                {
                                                    if (!ctx.CameraBindings.ContainsKey(pap.Key)) continue;
                                                    string[] cam = ctx.CameraBindings[pap.Key];
                                                    string dataVal = shuju_temp.Replace("\0", "").Trim();
                                                    if (cam[2] == "true" && dataVal != cam[1])
                                                    {
                                                        cam[4] = cam[1];
                                                        ctx.WriteTriggerFeedback(par.Value, cam[1], ref xuanzhong_temp, ref fins_temp);
                                                    }
                                                    CommTriggerHelper.GetCamTrigParams(cam, out string mode, out string tv1, out string tv2);
                                                    if (CommTriggerHelper.CheckTriggerCondition(dataVal, mode, tv1, tv2))
                                                    {
                                                        if (!_triggerLatch.TryGetValue(pap.Key, out bool latched) || !latched)
                                                        {
                                                        _triggerLatch[pap.Key] = true;
                                                        ctx.RaiseSelectionChanged(dataVal, pap.Key.ToString(), pap.Key.ToString(), LinkId);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        _triggerLatch[pap.Key] = false;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (fins_temp.Contains("Failed") || fins_temp.Contains("corrent"))
                            {
                                if (CommReconnectHelper.ShouldTriggerReconnect(
                                    ref _commFailCount, ctx.IsCommEnabled, _reconnecting, ref _lastReconnectAttemptTicks))
                                {
                                    ctx.OnReconnect();
                                }
                            }
                            else if (ctx.IsPollEnabled)
                            {
                                _commFailCount = 0;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ctx.Log(ex.Message + "modbustcp");
                }
            }
        }
    }
}
