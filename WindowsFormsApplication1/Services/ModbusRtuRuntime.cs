using System;
using System.Collections.Generic;
using System.Threading;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus RTU 运行时引擎（多实例改造 阶段 4-B / 4-C）。
    ///
    /// 一个 ModbusRtuRuntime 实例 = 一个 Modbus RTU COM 口连接的“运行时”（连接 + 轮询线程生命周期 + 每连接状态）。
    /// 本阶段只实例化 1 个（与改造前行为一致）；它已是可被 new 多次的对象，为多 COM 口并联铺路。
    ///
    /// 线程驱动策略（与 ModbusTcpRuntime 一致）：
    /// - 若 Context 为 null（当前唯一连接）：沿用窗体 RunPollLoop（即原 Fins_duxie），算法与共享状态完全不变。
    /// - 若配置了 Context（多连接并联）：运行本类的 PollLoop，配置经 Context 实时读取、状态收口到本实例。
    ///
    /// 说明：Modbus TCP / Modbus RTU 同属 Modbus 寄存器语义（地址为数字寄存器号、无 "D" 前缀），
    /// 故 PollLoop 与 ModbusTcpRuntime.PollLoop 完全一致，仅底层连接换为 ModbusRtuLink。
    /// </summary>
    public class ModbusRtuRuntime : ICommLink
    {
        private readonly ModbusRtuLink _link;
        private readonly ICommRuntimeHost _host;
        private ModbusRtuLinkConfig _config;

        // 每连接运行时状态（多实例并联时收口到实例，避免多连接共享窗体字段）
        private readonly Dictionary<int, bool> _triggerLatch = new Dictionary<int, bool>();
        private int _lastTrigReadFailLogTick; // ★R11：读失败跳过触发判定的限流日志时戳
        private int _commFailCount = 0;
        private int _reconnecting = 0;
        private long _lastReconnectAttemptTicks = 0;
        private volatile bool _stopPolling = false;
        private Thread _pollThread = null;

        public ModbusRtuRuntime(ModbusRtuLink link, ICommRuntimeHost host)
        {
            _link = link;
            _host = host;
            LinkId = 1;
            Name = "ModbusRTU-1";
        }

        /// <summary>每连接上下文（多实例启用时赋值；为 null 时走窗体原 Fins_duxie）。</summary>
        public ICommLinkContext Context { get; set; }

        public int LinkId { get; set; }
        public string Name { get; set; }
        public string Protocol { get { return "modbusrtu"; } }

        public bool IsConnected { get { return _link != null && _link.IsConnected; } }

        /// <summary>连接参数配置（由 ModbusRtuIniStore 按实例加载并赋值）。</summary>
        public ModbusRtuLinkConfig Config
        {
            get { return _config; }
            set { _config = value; if (_config != null) _config.LinkId = LinkId; }
        }

        /// <summary>底层连接（对外暴露，供触发路由 / 反馈回写使用）。</summary>
        public ModbusRtuLink Link { get { return _link; } }

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
                    else LogStopTimeout();   // ★#33：超时不再静默
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
                    else LogStopTimeout();   // ★#33：超时不再静默
                }
            }
        }

        /// <summary>
        /// ★第26轮#33：Join(1000) 超时说明线程还卡在一次阻塞的 PLC 读写里。引用按 A2 规则保留
        /// （防再起第二条轮询线程），但原实现不留任何痕迹，事后无法解释"已停止却仍有读写"。
        /// </summary>
        private void LogStopTimeout()
        {
            try { new ErrorLog().WriteLog("[" + Name + "] 停止轮询等待 1 秒超时：线程仍卡在阻塞读写中（引用已保留，Start 不会另起第二条）"); }
            catch { }
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
        /// 按连接参数化的轮询循环（多连接并联，休眠中）。
        /// 由原 Fins_duxie 逐行 port：配置经 Context 实时读取，状态收口到本实例，
        /// 窗体级副作用（UI/事件/回写/切换/日志/重连）全部经 Context 回调，保持与改造前一致。
        /// 仅在 Context 非空（多连接并联）时被 Start 调用。
        ///
        /// ★第26轮#32 遗留前置条件（接线启用前必须落实，本轮只修了门控）：
        ///   现网 Fins_duxie 每次读都持 _ioSync（读/写/重连同一把锁串行）；本循环内 _link.Client 的读写
        ///   与上下文 OnReconnect 的重连**不共任何锁**。启用多连接并联时须给每条连接一把专属 I/O 锁，
        ///   读、写、重连三处同锁。本上下文的回写仍委托宿主连接 1 的客户端，直接复用宿主 _ioSync
        ///   只会得到"看似加锁、实则不同对象"的假安全，故不在本轮半套插桩。
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
                        // ★#32 修复（第26轮）：本实例的 _reconnecting 从无人置位（重连由 Context 侧发起并维护），
                        //   原"重连中不读写"门控形同虚设。改读 ctx.IsReconnecting（宿主窗体/本连接上下文各自的真实标志）。
                        int reconnecting = (_reconnecting != 0 || ctx.IsReconnecting) ? 1 : 0;
                        if (reconnecting != 0)
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
                                    int baseAddr = int.Parse(par.Value[1]);   // ★性能#4（复盘P2-3补齐连接2~4）：台账基准每块 hoist——原每寄存器重复解析 par.Value[1]
                                    for (int j = 0; j < int.Parse(par.Value[2]); j++)
                                    {
                                        if (par.Value[4] == "int")
                                        {
                                            xuanzhong_temp = baseAddr - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadInt16((baseAddr + j).ToString()), (baseAddr + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(xuanzhong_temp, fins_temp);
                                            if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "string")
                                        {
                                            xuanzhong_temp = baseAddr - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadString((baseAddr + j).ToString(), 1), (baseAddr + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(xuanzhong_temp, fins_temp);
                                            if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "long" && j % 2 == 0)
                                        {
                                            xuanzhong_temp = baseAddr - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadInt32((baseAddr + j).ToString()), (baseAddr + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(xuanzhong_temp, fins_temp);
                                            if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                            shuju_temp += ctx.MiddleValue(fins_temp, " ", "\r");
                                        }
                                        else if (par.Value[4] == "float" && j % 2 == 0)
                                        {
                                            xuanzhong_temp = baseAddr - ctx.AddressBase + j;
                                            DemoUtils.ReadResultRender1(_link.Client.ReadFloat((baseAddr + j).ToString()), (baseAddr + j).ToString(), out fins_temp);
                                            ctx.UpdatePollCell(xuanzhong_temp, fins_temp);
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
                                    ref _commFailCount, ctx.IsCommEnabled, reconnecting, ref _lastReconnectAttemptTicks))
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
                    ctx.Log(ex.Message + "modbusrtu");
                    // ★第32轮 A4（与 RTU 窗体侧同口径）：异常出口原先不参与重连判定——首次建链失败时
                    //   _link.Client 恒 null，本循环每圈抛 NRE，连接永久僵死。现异常也计入失败计数，
                    //   按同一阈值/冷却触发本连接重连（Reconnect 已能在 Client 缺失时整体重建）。
                    //   try 内的 reconnecting 局部量在此不可见，重连状态改读 ctx.IsReconnecting。
                    int rc = ctx.IsReconnecting ? 1 : 0;
                    if (CommReconnectHelper.ShouldTriggerReconnect(
                        ref _commFailCount, ctx.IsCommEnabled, rc, ref _lastReconnectAttemptTicks))
                    {
                        ctx.OnReconnect();
                    }
                }
            }
        }
    }
}
