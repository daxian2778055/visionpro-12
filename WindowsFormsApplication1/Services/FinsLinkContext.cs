using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 每连接上下文实现（阶段 5 激活并修正多连接串台）。
    ///
    /// 配置类 getter（轮询间隔 / 数据块 / 相机绑定 / 地址基准 / 方案路径 / 使能）来自本连接的 <see cref="FinsLinkConfig"/>。
    ///
    /// ★ 阶段 5 修正（此前多连接会串到连接 1）：
    ///   1) 反馈回写 <see cref="WriteTriggerFeedback"/>：用<b>本连接</b>的客户端写本连接 D 区，
    ///      不再委托窗体（改造前会写到连接 1 的 PLC）。
    ///   2) 切方案 <see cref="TrySchemeSwitch"/>：用<b>本连接</b>的切型表匹配字符、取本连接的方案路径，
    ///      不再用连接 1 的 textBox 表与 lujing。
    ///   3) 自动重连 <see cref="OnReconnect"/>：重连<b>本连接</b>，不再重连连 1 导致第 2~4 路断线后永不恢复。
    ///   4) 轮询表格刷新：多连接没有专属网格，直接空实现，不再污染连接 1 的显示。
    ///   5) 初始化标志：用本连接自己的 <see cref="Initialized"/>，不再依赖窗体 chushihua。
    ///
    /// 仍然委托宿主的（与连接 1 共用的全局行为）：日志、触发事件广播、取中段字符串。
    /// </summary>
    public class FinsLinkContext : ICommLinkContext
    {
        private readonly FinsLinkConfig _cfg;
        private readonly FinsLink _link;
        private readonly ICommLinkContext _host;
        private readonly Dictionary<string, string[]> _finsDic;
        private readonly Dictionary<int, string[]> _cameraDic;
        private int _reconnecting = 0;

        public FinsLinkContext(FinsLinkConfig cfg, FinsLink link, ICommLinkContext hostForm)
        {
            _cfg = cfg;
            _link = link;
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

        /// <summary>本连接初始化完成标志（原依赖窗体 chushihua；多连接下由宿主按连接设置）。</summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// 切方案触发回调（宿主赋值：RaiseSchemeSwitch(dataVal, schemePath, linkId)）。
        /// 未赋值时切方案只记录日志，不误触发连接 1 的切换。
        /// </summary>
        public Action<string, string, int> SchemeSwitchRaise { get; set; }

        // 宿主（窗体）整体初始化完成前不轮询；Initialized 为本连接自身的附加开关（默认 true）。
        public bool IsInitialized { get { return Initialized && _host.IsInitialized; } }
        public int PollInterval { get { return (int)_cfg.LunxunTime; } }
        public bool IsPollEnabled { get { return _cfg.FinsLunxunen; } }
        public bool IsCommEnabled { get { return _cfg.FinsEn; } }
        // ★#32：重连由本上下文的 OnReconnect 发起，Runtime 侧的 _reconnecting 永不被置位——
        //   把本连接真实重连状态经接口暴露，轮询循环才拦得住"重连期间继续读写"。
        public bool IsReconnecting { get { return Interlocked.CompareExchange(ref _reconnecting, 0, 0) != 0; } }
        public int AddressBase { get { return (int)_cfg.Qishi; } }
        public Dictionary<string, string[]> FinsBlocks { get { return _finsDic; } }
        public Dictionary<int, string[]> CameraBindings { get { return _cameraDic; } }

        /// <summary>本连接最近一次匹配到的方案路径（供排查与上层读取）。</summary>
        public string SchemePath
        {
            get
            {
                foreach (var kv in _cfg.SchemePaths)
                    if (!string.IsNullOrEmpty(kv.Value)) return kv.Value;
                return "";
            }
        }

        public void Log(string message) { _host.Log(message); }

        /// <summary>多连接没有专属轮询网格：空实现，避免把第 2~4 路的数据写进连接 1 的表格。</summary>
        public void UpdatePollCell(int row, string value) { }

        public void RaiseSelectionChanged(string dataVal, string camKey, string third, int linkId)
        {
            _host.RaiseSelectionChanged(dataVal, camKey, third, linkId);
        }

        /// <summary>
        /// 触发后的反馈回写：把“返回数值”写回本连接自己的 D 区，防止 PLC 保持非零值导致二次触发。
        /// 逐分支复刻窗体 WriteTriggerFanhuizhi，唯一区别是客户端与地址基准取自本连接。
        /// </summary>
        public void WriteTriggerFeedback(string[] block, string value, ref int xuanzhong, ref string fins)
        {
            if (_link == null || _link.Client == null) return;
            try
            {
                int len = int.Parse(block[2]);
                int qishi = int.Parse(block[1]);
                string geshi = block[4];
                int addressBase = (int)_cfg.Qishi;
                for (int j = 0; j < len; j++)
                {
                    xuanzhong = qishi - addressBase + j;
                    string addr = "D" + (qishi + j).ToString();
                    if (geshi == "int")
                    {
                        short sv;
                        if (short.TryParse(value, out sv)) _link.Client.Write(addr, sv);
                    }
                    else if (geshi == "string")
                    {
                        _link.Client.Write(addr, value);
                    }
                    else if (geshi == "long" && j % 2 == 0)
                    {
                        int iv;
                        if (int.TryParse(value, out iv)) _link.Client.Write(addr, iv);
                    }
                    else if (geshi == "float" && j % 2 == 0)
                    {
                        float fv;
                        if (float.TryParse(value, out fv)) _link.Client.Write(addr, fv);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("FINS连接" + _cfg.LinkId + " 反馈回写失败:" + ex.Message);
            }
        }

        /// <summary>
        /// 切方案：用本连接自己的切型表（[change_finsN]）匹配读到的字符，
        /// 命中后取本连接对应方案路径（[path_finsN]）并回调宿主切换。
        /// 返回 true 仅表示“已发出切换请求”，与连接 1 的 qiehuan 语义一致。
        /// </summary>
        public bool TrySchemeSwitch(string dataVal)
        {
            string val = (dataVal ?? "").Replace("\0", "").Trim();
            if (val.Length == 0) return false;

            foreach (var kv in _cfg.ChangeTypes)
            {
                string s = (kv.Value ?? "").Replace("\0", "").Trim();
                if (s.Length == 0 || s != val) continue;

                string path = "";
                string raw;
                if (_cfg.SchemePaths.TryGetValue(kv.Key, out raw)) path = (raw ?? "").Replace("\0", "");
                if (path.Length == 0)
                {
                    Log("FINS连接" + _cfg.LinkId + " 切型字符[" + val + "]未配置方案路径");
                    return false;
                }
                if (!System.IO.File.Exists(path))
                {
                    Log("FINS连接" + _cfg.LinkId + " 方案路径:" + path + ":不存在!");
                    return false;
                }

                var raise = SchemeSwitchRaise;
                if (raise == null) return false;
                raise(val, path, _cfg.LinkId);
                return true;
            }
            return false;
        }

        public string MiddleValue(string str, string sta, string end) { return _host.MiddleValue(str, sta, end); }

        /// <summary>
        /// 检测结果回写（阶段 5）：把 VP 方案输出值写到“本连接”配置的反馈数据块。
        /// 逐分支复刻窗体 xie() 的写入规则（int/long/float/string，逗号分隔即批量写），
        /// 唯一区别是用本连接的客户端与数据块配置，且不刷新连接 1 的轮询表格。
        /// </summary>
        public bool WriteCameraOutput(int camNo, string value)
        {
            if (_link == null || _link.Client == null) return false;

            string[] cam;
            if (!_cameraDic.TryGetValue(camNo, out cam)) return false;

            // camera_dic 布局：[3] = 反馈（数据块名），与窗体 camera_dic[key][3] 同义
            string fankui = (cam[3] ?? "").Replace("\0", "").Trim();
            if (fankui.Length == 0 || fankui == "无") return false;

            string[] blk;
            if (!_finsDic.TryGetValue(fankui, out blk)) return false;

            int addrStart = int.Parse(blk[1]);
            string fmt = blk[4];
            string addr = "D" + addrStart.ToString();

            try
            {
                if (fmt == "int")
                {
                    string[] parts = SplitValues(value);
                    var vals = new short[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                        vals[i] = (short)System.Math.Round(double.Parse(parts[i]));
                    _link.Client.Write(addr, vals);
                }
                else if (fmt == "long")
                {
                    string[] parts = SplitValues(value);
                    var vals = new int[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                        vals[i] = (int)System.Math.Round(double.Parse(parts[i]));
                    _link.Client.Write(addr, vals);
                }
                else if (fmt == "float")
                {
                    string[] parts = SplitValues(value);
                    var vals = new float[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                        vals[i] = float.Parse(parts[i]);
                    _link.Client.Write(addr, vals);
                }
                else if (fmt == "string")
                {
                    string[] parts = SplitValues(value);
                    for (int j = 0; j < parts.Length; j++)
                        _link.Client.Write("D" + (addrStart + j).ToString(), parts[j]);
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log("FINS连接" + _cfg.LinkId + " 相机" + camNo + " 结果回写失败:" + ex.Message);
                return false;
            }
        }

        private static string[] SplitValues(string value)
        {
            string[] raw = (value ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < raw.Length; i++) raw[i] = raw[i].Trim();
            return raw;
        }

        /// <summary>自动重连：只重连本连接（异步执行，避免阻塞本连接的轮询线程）。</summary>
        public void OnReconnect()
        {
            if (_link == null) return;
            if (Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0) return;
            Log("FINS连接" + _cfg.LinkId + " 通讯异常，后台自动重连中...");
            Task.Run(() =>
            {
                try
                {
                    bool ok = _link.Reconnect();
                    Log("FINS连接" + _cfg.LinkId + (ok ? " 自动重连成功" : " 自动重连失败"));
                }
                catch (Exception ex)
                {
                    Log("FINS连接" + _cfg.LinkId + " 自动重连异常:" + ex.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref _reconnecting, 0);
                }
            });
        }
    }
}
