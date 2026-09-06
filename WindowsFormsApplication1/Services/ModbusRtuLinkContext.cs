using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 每连接上下文实现（阶段 4-C 激活）：把一条 ModbusRtuLinkConfig 适配为 ICommLinkContext。
    ///
    /// - 配置类 getter（轮询间隔 / 数据块 / 相机绑定 / 地址基准 / 方案路径 / 使能）直接来自该连接的 cfg（实时值）。
    /// - 窗体级副作用（日志 / 表格刷新 / 触发事件 / 反馈回写 / 方案切换 / 取中段 / 重连）全部委托给宿主窗体
    ///   （hostForm，即 FormModbusRtu 的 ICommLinkContext 实现），保持与链接 1 完全一致的 UI 与事件行为。
    ///
    /// 数据块 / 相机绑定字典逐字段复刻窗体 Load 中的 fins_dic / camera_dic 构造，保证 PollLoop 行为一致。
    /// 仅当 test.ini 存在 [modbusrtuN] 段（N>=2）时才会被实例化（当前休眠）。
    /// </summary>
    public class ModbusRtuLinkContext : ICommLinkContext
    {
        private readonly ModbusRtuLinkConfig _cfg;
        private readonly ICommLinkContext _host;
        private readonly Dictionary<string, string[]> _finsDic;
        private readonly Dictionary<int, string[]> _cameraDic;

        public ModbusRtuLinkContext(ModbusRtuLinkConfig cfg, ICommLinkContext hostForm)
        {
            _cfg = cfg;
            _host = hostForm;

            // fins_dic[name] = { name, qishi, changdu, gaodiwei, geshi }（复刻窗体 Load 构造）
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
        public void OnReconnect() { _host.OnReconnect(); }
    }
}
