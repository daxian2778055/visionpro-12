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
                if (t != null) { try { t.Join(1000); } catch { } }
                _host.PollThread = null;
            }
            else
            {
                _stopPolling = true;
                Thread t = _pollThread;
                if (t != null) { try { t.Join(1000); } catch { } }
                _pollThread = null;
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
        /// 按连接参数化的轮询循环（多连接并联，休眠中）。
        /// 由原 Fins_duxie 逐行 port：配置经 Context 实时读取，状态收口到本实例，
        /// 窗体级副作用（UI/事件/回写/切换/日志/重连）全部经 Context 回调，保持与改造前一致。
        /// 仅在 Context 非空（多连接并联）时被 Start 调用。
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
                    ctx.Log(ex.Message + "modbusrtu");
                }
            }
        }
    }
}
