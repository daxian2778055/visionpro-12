using System;
using System.Collections.Generic;
using System.Threading;
using HslCommunication.Profinet.Omron;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// FINS 运行时引擎（多实例改造 阶段 3-B / 3-C）。
    ///
    /// 与 ModbusTcpRuntime 完全同构；仅底层客户端为 OmronFinsNet、且读写地址带 "D" 前缀
    /// （FINS 以 "D" + 地址访问 D 区，Modbus TCP 地址无前缀）。
    ///
    /// 线程驱动策略（与 ModbusTcpRuntime 一致）：
    /// - 若 <see cref="Context"/> 为 null（当前唯一连接）：沿用窗体 RunPollLoop（即原 Fins_duxie），
    ///   算法与共享状态完全不变，行为逐字节一致。
    /// - 若配置了 Context（阶段 3-C 多连接并联时）：运行本类的 <see cref="PollLoop"/>，
    ///   配置经 Context 实时读取、状态收口到本实例，天然支持第 2/3/4 个连接互不干扰。
    /// </summary>
    public class FinsRuntime : ICommLink
    {
        private readonly FinsLink _link;
        private readonly ICommRuntimeHost _host;
        private FinsLinkConfig _config;

        // 每连接运行时状态（阶段 3-C 起收口到实例，避免多连接共享窗体字段）
        private readonly Dictionary<int, bool> _triggerLatch = new Dictionary<int, bool>();
        private int _lastTrigReadFailLogTick; // ★R11：读失败跳过触发判定的限流日志时戳
        private int _commFailCount = 0;
        private int _reconnecting = 0;
        private long _lastReconnectAttemptTicks = 0;
        private volatile bool _stopPolling = false;
        private Thread _pollThread = null;

        public FinsRuntime(FinsLink link, ICommRuntimeHost host)
        {
            _link = link;
            _host = host;
            LinkId = 1;
            Name = "FINS-1";
        }

        /// <summary>每连接上下文（多实例启用时赋值；为 null 时走窗体原 Fins_duxie）。</summary>
        public ICommLinkContext Context { get; set; }

        public int LinkId { get; set; }
        public string Name { get; set; }
        public string Protocol { get { return "fins"; } }

        public bool IsConnected { get { return _link != null && _link.IsConnected; } }

        /// <summary>连接参数配置（阶段 3-C 起由 FinsIniStore 按实例加载并赋值）。</summary>
        public FinsLinkConfig Config
        {
            get { return _config; }
            set { _config = value; if (_config != null) _config.LinkId = LinkId; }
        }

        /// <summary>底层连接（供触发路由 / 反馈回写使用）。</summary>
        public FinsLink Link { get { return _link; } }

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
        /// 按连接参数化的轮询循环（阶段 3-C per-link，休眠中）。
        /// 由原 Fins_duxie 逐行 port：配置经 <see cref="Context"/> 实时读取，状态收口到本实例，
        /// 窗体级副作用（UI/事件/回写/切换/日志/重连）全部经 Context 回调，保持与改造前一致。
        /// 仅在 <see cref="Context"/> 非空（多连接并联）时被 <see cref="Start"/> 调用。
        /// 与原 Fins_duxie 的唯一差别：读写地址带 "D" 前缀（FINS D 区）。
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
                                    bool blockReadFailed = false; // ★R11：本块任一次读失败即置真，失败文案不得当“值”
                                    for (int j = 0; j < int.Parse(par.Value[2]); j++)
                                    {
                                        if (par.Value[4] == "int")
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadInt16("D" + (int.Parse(par.Value[1]) + j).ToString()), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "string")
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadString("D" + (int.Parse(par.Value[1]) + j).ToString(), 1), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "long" && j % 2 == 0)
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadInt32("D" + (int.Parse(par.Value[1]) + j).ToString()), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "float" && j % 2 == 0)
                                        {
                                            xuanzhong_temp = int.Parse(par.Value[1]) - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadFloat("D" + (int.Parse(par.Value[1]) + j).ToString()), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                            if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                    }
                                    if (par.Value[3] == "触发" && blockReadFailed)
                                    {
                                        // ★R11（第25轮）：本块任一寄存器读取失败时，shuju_temp 混有“Read Failed”等文案，
                                        //   不能当“值”判定（否则同值重复触发 + 每圈回写反馈）。失败轮整块跳过（锁存保留）。
                                        int nowF = Environment.TickCount;
                                        if (nowF - _lastTrigReadFailLogTick > 5000)
                                        {
                                            _lastTrigReadFailLogTick = nowF;
                                            ctx.Log("数据块 " + par.Key + " 本轮读取失败，已跳过触发/切型判定与回写（防读失败文案被当作值）");
                                        }
                                        continue;
                                    }
                                    if (par.Value[3] == "触发")
                                    {
                                        foreach (var pap in ctx.CameraBindings)
                                        {
                                            if (pap.Value[0] == par.Value[0])
                                            {
                                                // ★C4 修复：与主窗"使能"门控对称——未启用切型功能时轮询侧不置锁/不发事件
                                                if (pap.Key == 13 && pap.Value.Length > 2 && pap.Value[2] == "true")
                                                {
                                                    // TrySchemeSwitch 内部已通过 SchemeSwitchRaise 触发带 SchemePath 的切方案事件，
                                                    // 无需再 RaiseSelectionChanged（否则会多抛一次无 SchemePath 的空事件（无实际消费者））。
                                                    ctx.TrySchemeSwitch(shuju_temp.Replace("\0", ""));
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
                    ctx.Log(ex.Message + "fins");
                }
            }
        }
    }
}
