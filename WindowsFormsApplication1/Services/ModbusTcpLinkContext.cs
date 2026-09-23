using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 每连接上下文实现（阶段 2-C 激活）：把一条 ModbusTcpLinkConfig 适配为 ICommLinkContext。
    ///
    /// - 配置类 getter（轮询间隔 / 数据块 / 相机绑定 / 地址基准 / 方案路径 / 使能）直接来自该连接的 cfg（实时值）。
    /// - 窗体级副作用（日志 / 表格刷新 / 触发事件 / 反馈回写 / 方案切换 / 取中段 / 重连）全部委托给宿主窗体
    ///   （hostForm，即 FormModbus 的 ICommLinkContext 实现），保持与链接 1 完全一致的 UI 与事件行为。
    ///
    /// 数据块 / 相机绑定字典逐字段复刻窗体 Load 中的 fin_dic / camera_dic 构造，保证 PollLoop 行为一致。
    /// 仅当 test.ini 存在 [modbustcpN] 段时才会被实例化（当前休眠）。
    /// </summary>
    public class ModbusTcpLinkContext : ICommLinkContext
    {
        private readonly ModbusTcpLinkConfig _cfg;
        private readonly ModbusTcpLink _link;
        private readonly ICommLinkContext _host;
        private readonly Dictionary<string, string[]> _finsDic;
        private readonly Dictionary<int, string[]> _cameraDic;
        private int _reconnecting = 0;

        public ModbusTcpLinkContext(ModbusTcpLinkConfig cfg, ICommLinkContext hostForm, ModbusTcpLink link = null)
        {
            _cfg = cfg;
            _link = link;
            _host = hostForm;

            // fin_dic[name] = { name, qishi, changdu, gaodiwei, geshi }（复刻窗体 Load 构造）
            _finsDic = new Dictionary<string, string[]>();
            foreach (var blk in cfg.DataBlocks)
            {
                _finsDic[blk.Name] = new string[]
                {
                    blk.Name,
                    blk.Qishi.ToString(),
                    blk.Changdu.ToString(),
                    blk.Gaodiwei,
                    blk.Geshi
                };
            }

            // camera_dic[camNo] = { chufa, fanhuizhi, fanhuien, fankui, "无", "无", chukufangshi, chufazhi1, chufazhi2 }
            _cameraDic = new Dictionary<int, string[]>();
            foreach (var cam in cfg.CameraBindings)
            {
                _cameraDic[cam.CameraNo] = new string[]
                {
                    cam.Chufa,
                    cam.Fanhuizhi,
                    cam.Fanhuien ? "true" : "false",
                    cam.Fankui,
                    "无",
                    "无",
                    cam.Chukufangshi,
                    cam.Chufazhi1,
                    cam.Chufazhi2
                };
            }
        }

        public bool IsInitialized { get { return _host.IsInitialized; } }
        public int PollInterval { get { return (int)_cfg.LunxunTime; } }
        public bool IsPollEnabled { get { return _cfg.ModbusLunxunen; } }
        public bool IsCommEnabled { get { return _cfg.ModbusEn; } }
        // ★#32：重连由本上下文的 OnReconnect 发起，Runtime 侧的 _reconnecting 永不被置位——
        //   把本连接真实重连状态经接口暴露，轮询循环才拦得住"重连期间继续读写"。
        public bool IsReconnecting { get { return System.Threading.Interlocked.CompareExchange(ref _reconnecting, 0, 0) != 0; } }
        public int AddressBase { get { return (int)_cfg.Qishi; } }
        public Dictionary<string, string[]> FinsBlocks { get { return _finsDic; } }
        public Dictionary<int, string[]> CameraBindings { get { return _cameraDic; } }
        public string SchemePath
        {
            get
            {
                // 取第一个非空方案路径（窗体 lujing 为运行时动态值，此处以配置首个为准；休眠路径）
                foreach (var kv in _cfg.SchemePaths)
                    if (!string.IsNullOrEmpty(kv.Value)) return kv.Value;
                return "";
            }
        }

        public void Log(string message) { _host.Log(message); }
        public void UpdatePollCell(int row, string value) { _host.UpdatePollCell(row, value); }
        public void RaiseSelectionChanged(string dataVal, string camKey, string third, int linkId) { _host.RaiseSelectionChanged(dataVal, camKey, third, linkId); }
        public void WriteTriggerFeedback(string[] block, string value, ref int xuanzhong, ref string fins) { _host.WriteTriggerFeedback(block, value, ref xuanzhong, ref fins); }
        public bool TrySchemeSwitch(string dataVal) { return _host.TrySchemeSwitch(dataVal); }
        public string MiddleValue(string str, string sta, string end) { return _host.MiddleValue(str, sta, end); }

        /// <summary>
        /// 自动重连：只重连本连接（异步执行，避免阻塞本连接的轮询线程）。
        /// 未注入本连接 <see cref="_link"/> 时退回宿主重连（向后兼容）。
        /// </summary>
        public void OnReconnect()
        {
            if (_link == null) { _host.OnReconnect(); return; }
            if (Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0) return;
            Log("Modbus-TCP连接" + _cfg.LinkId + " 通讯异常，后台自动重连中...");
            Task.Run(() =>
            {
                try
                {
                    bool ok = _link.Reconnect();
                    Log("Modbus-TCP连接" + _cfg.LinkId + (ok ? " 自动重连成功" : " 自动重连失败"));
                }
                catch (Exception ex)
                {
                    Log("Modbus-TCP连接" + _cfg.LinkId + " 自动重连异常:" + ex.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref _reconnecting, 0);
                }
            });
        }
    }
}
