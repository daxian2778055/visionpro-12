using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HslCommunication.Profinet;
using System.Threading;
using HslCommunication;
using HslCommunication.Profinet.Omron;
using demo;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using WindowsFormsApplication1.Core.Infrastructure;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1
{
    public partial class FormOmron : Form, IPlcOutputWriter, ICommRuntimeHost, ICommLinkContext
    {
        public Label labelFallbackHint = new Label();

        private int _linkId = 1;
        public int LinkId { get { return _linkId; } }

        public FormOmron( ) : this(1) { }

        public FormOmron(int linkId)
        {
            _linkId = linkId;
            wdini.ReadINIFile(AppDomain.CurrentDomain.BaseDirectory + "//test.ini");
            InitializeComponent( );
            omronFinsNet = new OmronFinsNet( );
            omronFinsNet.ConnectTimeOut = 2000;

            // 在 tabPage2 (数据绑定页面) 底部添加提示 label（默认隐藏）
            labelFallbackHint.AutoSize = true;
            labelFallbackHint.TextAlign = ContentAlignment.MiddleLeft;
            labelFallbackHint.ForeColor = Color.Red;
            labelFallbackHint.Text = "写操作已降级为逐地址写入";
            labelFallbackHint.Visible = false;
            labelFallbackHint.Location = new Point(211, 380);
            tabPage2.Controls.Add(labelFallbackHint);
        }
        private ClassIni wdini = new ClassIni();
        // 多实例改造 阶段 3-B/3-C：omronFinsNet 代理到 _finsLink.Client，使原 Fins_duxie/xie 等引用零改动且始终指向当前连接对象。
        private FinsLink _finsLink = new FinsLink();
        private OmronFinsNet omronFinsNet { get { return _finsLink.Client; } set { _finsLink.Client = value; } }

        // 多实例改造 阶段 3-C：运行时集合与中央登记（链接 1 用 _finsLink + Context=null 走原 Fins_duxie）。
        private FinsRuntime _runtime;
        private Dictionary<int, FinsRuntime> _runtimes = new Dictionary<int, FinsRuntime>();

        // 多连接管理（阶段 5 收口到“连接设备”管理器）：管理器经此访问与主窗体同一份 test.ini 内存镜像。
        public ClassIni ConfigIni { get { return wdini; } }

        // ===== ICommRuntimeHost 实现（阶段 3-B）：引擎通过它回调轮询循环、读写停止标志与线程句柄 =====
        public bool StopRequested { get { return _stopPolling; } set { _stopPolling = value; } }
        public Thread PollThread { get { return fins_duxie; } set { fins_duxie = value; } }
        public void RunPollLoop() { Fins_duxie(); }

        // ===== ICommLinkContext 实现（阶段 3-C per-link）：供 FinsRuntime.PollLoop 按连接读取配置与回调。
        // 当前唯一连接（链接 1）的运行时 Context 为 null，仍走原 Fins_duxie，本实现作为多连接并联样板。
        public bool IsInitialized { get { return chushihua; } }
        public int PollInterval { get { return (int)lunxun_time; } }
        public bool IsPollEnabled { get { return fins_lunxunen; } }
        public bool IsCommEnabled { get { return fins_en; } }
        public bool IsReconnecting { get { return System.Threading.Volatile.Read(ref _reconnecting) != 0; } }   // ★#32：把真实重连状态暴露给 per-link 轮询门控
        public int AddressBase { get { return (int)address_qishi; } }
        public Dictionary<string, string[]> FinsBlocks { get { return fins_dic; } }
        public Dictionary<int, string[]> CameraBindings { get { return camera_dic; } }
        public string SchemePath { get { return lujing; } }
        public void Log(string message)
        {
            // 每条日志都带上协议名 + 具体连接号，多实例时能一眼看出来自哪个实例的通讯
            MsgErroeLog.WriteLog("[Omron-连接" + _linkId + "] " + message);
        }
        public void UpdatePollCell(int row, string value) { CommGridHelper.SetPollCell(_gridUi, fins_data, row, value); }
        public void RaiseSelectionChanged(string dataVal, string camKey, string third, int linkId)
        {
            // 注意：本方法由轮询线程调用，此处保持"同步派发"这一现网已验证行为。
            // 已评估过改为 BeginInvoke 投递到 UI 线程（可避免轮询线程被 UI 阻塞、订阅方在后台线程操作 UI），
            // 但当前无条件上机验证，故暂不启用；待现场可验证时再单独开启。
            if (getData != null) getData(this, new SelectionChangedEventArgs(dataVal, camKey) { LinkId = linkId });
        }

        /// <summary>
        /// 多连接改造（阶段 5）：第 2~4 路连接“切方案”专用触发。
        /// 带上该连接自己的方案路径与连接号，主界面据此加载方案（不再取连接 1 窗体的 lujing）。
        /// </summary>
        public void RaiseSchemeSwitch(string dataVal, string schemePath, int linkId)
        {
            if (getData != null)
                getData(this, new SelectionChangedEventArgs(dataVal, "13") { LinkId = linkId, SchemePath = schemePath });
        }

        // ===== 阶段 5 多连接辅助：主界面按“来源连接”读取该连接自己的配置 =====
        // 连接 2~4 由“连接设备”管理器创建为独立 FormOmron 实例。主界面仍通过 _comm.Omron 这一代理访问，
        // 因此把子实例登记到主连接单例，由主连接单例按 linkId 路由到对应实例，保证各连接互不串扰。
        private readonly Dictionary<int, FormOmron> _childLinks = new Dictionary<int, FormOmron>();
        private readonly object _childLinksSync = new object();

        /// <summary>登记一个子连接实例（由连接设备管理器调用）。</summary>
        public void RegisterChildLink(int linkId, FormOmron child)
        {
            if (linkId <= 1 || child == null) return;
            lock (_childLinksSync) { _childLinks[linkId] = child; }
        }

        /// <summary>注销子连接实例。</summary>
        public void UnregisterChildLink(int linkId)
        {
            if (linkId <= 1) return;
            lock (_childLinksSync) { _childLinks.Remove(linkId); }
        }

        /// <summary>取指定 linkId 对应的子实例；连接 1 返回自身。</summary>
        private FormOmron ResolveLinkHost(int linkId)
        {
            if (linkId <= 1) return this;
            lock (_childLinksSync)
            {
                FormOmron child;
                return _childLinks.TryGetValue(linkId, out child) ? child : null;
            }
        }

        /// <summary>
        /// 取指定连接（2~4）的相机绑定配置。连接 1 仍用窗体 camera_dic，不查这里，行为不变。
        /// </summary>
        public bool TryGetLinkCamera(int linkId, int camNo, out string[] cam)
        {
            cam = null;
            var host = ResolveLinkHost(linkId);
            if (host == null) return false;
            return host.camera_dic.TryGetValue(camNo, out cam);
        }

        /// <summary>
        /// 阶段 5：把检测结果（VP 方案输出值）回写到指定连接（2~4）<b>自己</b>配置的反馈数据块。
        /// 连接 1 仍走 <see cref="WriteCameraOutput"/>（窗体原有逻辑），行为不变。
        /// </summary>
        public bool WriteCameraOutputForLink(int linkId, int camIdx, string value, bool detectionFailed = false)
        {
            if (linkId <= 1) return false;
            var host = ResolveLinkHost(linkId);
            if (host == null) return false;
            host.WriteCameraOutput(camIdx, value, detectionFailed);
            return true;
        }

        /// <summary>
        /// 方案切换成功回执（相机13）：切型完成后由主界面统一调用，
        /// 把 UI 配置的“返回值”(camera_dic[13][1]) 写回“切换”通道(camera_dic[13][0])。
        /// linkId&gt;1 路由到对应子连接实例；未绑定切换通道/未启用/返回值未配置则忽略。
        /// </summary>
        public bool WriteSchemeSwitchAck(int linkId)
        {
            var host = ResolveLinkHost(linkId);
            if (host == null) return false;
            return host.WriteCamera13SwitchAck();
        }

        private bool WriteCamera13SwitchAck()
        {
            try
            {
                if (!fins_en || !chushihua) return false;
                string[] c13;
                if (!camera_dic.TryGetValue(13, out c13)) return false;
                if (c13[2] != "true") return false;        // 未启用（返回值/切型使能）
                if (c13[0].Contains("无")) return false;    // 未绑定切换通道
                if (c13[1].Contains("无")) return false;    // 未配置返回值 → 不回执
                foreach (var par in fins_dic)
                {
                    if (par.Value[0] != c13[0]) continue;
                    int xuanzhong_temp = 0;
                    string fins_temp = "";
                    // ★与心跳/失败回执的“设[4][5]+写出”互斥：防两组槽值交叠后发错通道
                    lock (_ioSync)
                    {
                        c13[4] = c13[1];
                        c13[5] = c13[0];
                        WriteTriggerFanhuizhi(par.Value, c13[1], ref xuanzhong_temp, ref fins_temp);
                        c13[5] = "无"; // 写后清待写标记，避免后续 xie() 误写
                    }
                    Log("方案切换成功，已回执切换通道 " + c13[0] + " = " + c13[1]);
                    return true;
                }
                Log("切换回执失败:未找到切换通道对应的数据块 " + c13[0]);
                return false;
            }
            catch (Exception ex)
            {
                Log("切换回执异常:" + ex.Message);
                return false;
            }
        }

        // 按连接的“切方案”锁：连接 1 仍用窗体 qiehuanzhong（不受影响），2~4 各自独立，互不阻塞。
        private readonly Dictionary<int, int> _switchLock = new Dictionary<int, int>();

        public int GetSwitchLock(int linkId)
        {
            if (linkId <= 1) return qiehuanzhong;
            var host = ResolveLinkHost(linkId);
            return host != null ? host.qiehuanzhong : 0;
        }

        public void SetSwitchLock(int linkId, int value)
        {
            if (linkId <= 1) { qiehuanzhong = value; return; }
            var host = ResolveLinkHost(linkId);
            if (host != null) host.qiehuanzhong = value;
        }

        /// <summary>该 FINS 协议是否有任一连接使能且已初始化（决定是否读取 VP 的 fins 输出值；
        /// 连接 1 未使能但连接 2~4 使能时仍须读取，否则连接 2~4 回写的是空值）。</summary>
        public bool CanWriteResultOutput()
        {
            if (fins_en && chushihua) return true;
            lock (_childLinksSync)
            {
                foreach (var kv in _childLinks)
                    if (kv.Value != null && kv.Value.fins_en && kv.Value.chushihua) return true;
            }
            return false;
        }
        public void WriteTriggerFeedback(string[] block, string value, ref int xuanzhong, ref string fins)
        {
            WriteTriggerFanhuizhi(block, value, ref xuanzhong, ref fins);
        }
        public bool TrySchemeSwitch(string dataVal)
        {
            if (qiehuanzhong != 0) return false;
            if (qiehuan(dataVal) == 1)
            {
                if (File.Exists(lujing.Replace("\0", "")))
                {
                    qiehuanzhong = 1;
                    return true;
                }
                Log("方案路径:" + lujing + ":不存在!");
            }
            return false;
        }
        public string MiddleValue(string str, string sta, string end) { return GetMiddleValue(str, sta, end); }
        public void OnReconnect() { TryAutoReconnect(); }
        public delegate void GetSeletionData(object Sender, SelectionChangedEventArgs e);
        public event GetSeletionData getData;
        private int x=999;
        private int y=999;
        Dictionary<string, string []> fins_dic = new Dictionary<string,string[]>();
        public  Dictionary<int, string[]> camera_dic = new Dictionary<int, string[]>();
        Dictionary<int, int[]> fins_name = new Dictionary<int, int[]>();
        Dictionary<int, int[]> fins_data = new Dictionary<int, int[]>();
        Dictionary<int, byte[]> fins_value = new Dictionary<int, byte[]>();
        private decimal address_qishi = 0;
        private decimal address_length = 0;
        private decimal fins_qishi = 0;
        private decimal fins_length = 0;
        private string ABCD = "触发";
        private string fins_style = "int";
        private string fins_mingcheng = "";
        public string zifu;
        public string lujing;
        // ★P2：切型锁会被轮询线程与线程池线程并发读写，必须是 volatile（同 FormModbus）。
        public volatile int qiehuanzhong = 0;

        public class SelectionChangedEventArgs : EventArgs
        {

            private string m_selection;

            private string m_camere;

            //本属性用于传递事件数据

            public string Selection
            {

                get { return m_selection; }

            }
            public string Camera
            {

                get { return m_camere; }

            }
            // 多连接改造（阶段 5）：本事件的来源连接号（1 = 主连接）。缺省 1，连接 1 行为逐字不变。
            public int LinkId { get; set; }

            // 多连接改造（阶段 5）：切方案事件中该连接自己的方案路径。
            // 连接 1 为 null（沿用窗体 lujing）；连接 2~4 用自身 [path_finsN] 里匹配到的路径。
            public string SchemePath { get; set; }

            public SelectionChangedEventArgs(string selection,string camera)
            {

                m_selection = selection;
                m_camere = camera;

                LinkId = 1;

                SchemePath = null;
            }
        }

        private void FormSiemens_Load( object sender, EventArgs e )
        {
            InitializeForm();
        }

        /// <summary>整窗初始化（与 Form3.InitializeForm 同构）：读 [fins]/[finsN] 段族填充界面，并创建/启动本连接运行时。
        /// 关键：WinForms 的 Load 事件只在首次 Show 时触发——管理器 Attach/添加时预建的隐藏窗（连接 2~4）
        /// 若不显式调用本方法，运行时永远不会创建：连接 2~4 完全不轮询，且 WriteCameraOutput 首行
        /// !fins_en || !chushihua 直接 return，检测结果被静默丢弃。门闩保证同一实例只初始化一次。</summary>
        internal void InitializeForm()
        {
            // 嵌入/恢复顶级会重建句柄，可能再次触发 Load；已初始化过则直接返回。
            if (_formLoaded) return;
            _formLoaded = true;

            decimal xuanzhong_temp = 0;
            comboBox1.DataSource = HslCommunication.BasicFramework.SoftBasic.GetEnumValues<HslCommunication.Core.DataFormat>( );
            comboBox1.SelectedItem = HslCommunication.Core.DataFormat.CDAB;
            panel2.Enabled = false;
            Program.Language = Settings1.Default.language;
            Language( Program.Language );

            // 多实例改造（阶段 5）：每个 FormOmron 实例只负责“本连接”（_linkId）。
            // 所有 ini 读写已按 _linkId 落到 [fins]/[finsN] 段族；运行时统一用窗体既有 _finsLink + Context=null 走原 Fins_duxie，
            // 每实例一个 FinsRuntime，彼此完全独立（启用=创建并启动该实例，禁用=释放该实例）。
            // 启动前相机关卡复查：本连接（连接 1~4 等价）若绑定了被同协议其它连接占用的相机，
            // 则阻止本连接自动启动（不建链、不轮询、不标记就绪），但仍加载界面供查看并修正。
            bool commBlocked = FinsIniStore.Load(wdini, _linkId) != null
                && (CommCameraGuard.CheckStartupFins(wdini, _linkId) != null);
            var cfg = FinsIniStore.Load(wdini, _linkId);
            if (!commBlocked)
            {
                var rt = new FinsRuntime(_finsLink, this) { LinkId = _linkId, Config = cfg, Context = null };
                rt.Start();
                _runtimes[_linkId] = rt;
                _runtime = rt;
                AppHost.Services.Resolve<CommLinkManager>().Register(rt);
            }
            for (int i = 0; i < 10; i++)
            {
                dataGridView1.Rows.Add();
            }
            _gridUi = new CommGridUiSink(dataGridView1);
            CommGridHelper.ApplyDataTabChrome(tabPage1, dataGridView1, button5, button3);
            CommGridHelper.StyleClearButton(button6);
            for (int i = 0; i < 50; i++)
            {            
                fins_data.Add(i, new int[] {i%10, i / 10*2 +1});
                fins_name.Add(i, new int[] { i % 10, i / 10*2 });
                fins_value.Add(i, new byte[] { 0x00,0x00 });
            }
          // ★第33轮：本段原为裸 bool.Parse / decimal.Parse——界面上写不出非法值（恒为下拉/复选/NumericUpDown），
          //   但手改 code.ini 即会在启动巨型 try 内抛 FormatException，后果与第32轮 A5 同型
          //   （连接1 全局崩溃弹窗；连接2~4 被管理器 catch 吞掉且 _formLoaded 已置真 = 永久不轮询）。
          //   统一改 CommGridHelper.ReadIni*：非法取默认值 + 每个位置只记一条日志。
          fins_lunxunen = CommGridHelper.ReadIniBool(wdini.ReadString(FinsIniStore.ConnSection(_linkId), "fins_lunxunen", "false"), false, "连接" + _linkId + "/fins_lunxunen", Log);
          fins_en = CommGridHelper.ReadIniBool(wdini.ReadString(FinsIniStore.ConnSection(_linkId), "fins_en", "false"), false, "连接" + _linkId + "/fins_en", Log);
            address_qishi = CommGridHelper.ReadIniDecimal(wdini.ReadString(FinsIniStore.ConnSection(_linkId), "qishi", "0"), 0, "连接" + _linkId + "/qishi(总起始地址)", Log);
            address_length = CommGridHelper.ReadIniDecimal(wdini.ReadString(FinsIniStore.ConnSection(_linkId), "zongchang", "1"), 1, "连接" + _linkId + "/zongchang(总长度)", Log);
            lunxun_time = CommGridHelper.ReadIniDecimal(wdini.ReadString(FinsIniStore.ConnSection(_linkId), "lunxun_time", "20"), 20, "连接" + _linkId + "/lunxun_time", Log);
            // ★第33轮：赋值前钳位，且让派生变量与界面同取钳后值（R7 口径）——
            //   数字合法但超出控件量程同样会在启动读回段抛 ArgumentOutOfRangeException。
            numericUpDown1.Value = CommGridHelper.ClampToNud(numericUpDown1, address_qishi);
            numericUpDown2.Value = CommGridHelper.ClampToNud(numericUpDown2, address_length);
            numericUpDown3.Value = CommGridHelper.ClampToNud(numericUpDown3, lunxun_time);
            address_qishi = numericUpDown1.Value;
            address_length = numericUpDown2.Value;
            lunxun_time = numericUpDown3.Value;
           
            comboBox1.Text = wdini.ReadString(FinsIniStore.ConnSection(_linkId), "abcd", "CDAB").Replace("\0", "");
            textBox1.Text = wdini.ReadString(FinsIniStore.ConnSection(_linkId), "ip", "127.0.0.1").Replace("\0", "");
            textBox2.Text = wdini.ReadString(FinsIniStore.ConnSection(_linkId), "port", "9600").Replace("\0", "");
            textBox16.Text = wdini.ReadString(FinsIniStore.ConnSection(_linkId), "cell", "0").Replace("\0", "");
            textBox15.Text = wdini.ReadString(FinsIniStore.ConnSection(_linkId), "local", "192").Replace("\0", "");
            if (fins_lunxunen)
            {
                checkBox1.CheckState = CheckState.Checked;
            }
            if (fins_en && !commBlocked)
            {
                checkBox2.CheckState = CheckState.Checked;
                // ★ 2026-09-11：移除此处同步 button1_Click，避免与下方 Task.Run 延迟建链重复连接（重复连接会造成闪断）。
                //   FINS 启动建链统一由 InitializeForm 末尾的 Task.Run 异步执行一次。
            }
            // ★第33轮：geshu / 每块 qishi·changdu 原为裸 Parse，手改 ini 非数字即在启动段抛
            //   FormatException（后果同下 A5 注释），改走安全读取取默认值 + 记日志。
            // ★第33轮：块个数上限 10 来自界面上"添加数据块"处的 fins_dic.Count < 10 判断，
            //   手改 geshu 成更大值只会让本循环去读不存在的节，故钳回上限，兼防日志刷屏。
            // ★第33轮复审②：原写法 (int)ReadIniDecimal(...) 的强转是受检转换，手改 3000000000
            //   解析得值但强转抛 OverflowException，钳位写在强转之后拦不住；ReadIniInt 先钳后转。
            geshu = CommGridHelper.ReadIniInt(wdini.ReadString(FinsIniStore.ConnSection(_linkId), "geshu", "0"), 0, 0, 10, "连接" + _linkId + "/geshu(块个数)", Log);
            int greenSkipped = 0;
            if (geshu > 0)
            {
                for (int i = 0; i < geshu; i++)
                {
                    fins_mingcheng= wdini.ReadString(FinsIniStore.BlockSection(_linkId, i+1), "name", "").Replace("\0", "");
                    fins_qishi = CommGridHelper.ReadIniDecimal(wdini.ReadString(FinsIniStore.BlockSection(_linkId, i + 1), "qishi", "0"), 0, "连接" + _linkId + "/块" + (i + 1) + "/qishi", Log);
                    fins_length= CommGridHelper.ReadIniDecimalBounded(wdini.ReadString(FinsIniStore.BlockSection(_linkId, i + 1), "changdu", "0"), 0, 0, 50, "连接" + _linkId + "/块" + (i + 1) + "/changdu", Log);   // ★第33轮复审③：上限 50 = 界面块长度 numericUpDown4.Maximum；手改更大的值不抛异常，只会让下方回绿循环空转（≥2^31 时 int 计数回绕=永久死循环）
                    ABCD= wdini.ReadString(FinsIniStore.BlockSection(_linkId, i + 1), "gaodiwei", "触发").Replace("\0", "");
                    fins_style = wdini.ReadString(FinsIniStore.BlockSection(_linkId, i + 1), "geshi", "int").Replace("\0", "");
                    // ★第33轮：ini 里两个块同名（含两行都缺 name → 都是空串）时 Dictionary.Add 抛
                    //   ArgumentException 打断启动 = 与 A5 同型后果，改为跳过该重复块并记日志。
                    if (fins_dic.ContainsKey(fins_mingcheng))
                    {
                        Log("第" + (i + 1) + "个数据块名 \"" + fins_mingcheng + "\" 与已有块重复，已跳过该块（请改个不重名的块）");
                        continue;
                    }
                    fins_dic.Add(fins_mingcheng, new string[] { fins_mingcheng, fins_qishi.ToString(), fins_length.ToString(), ABCD, (fins_style ?? "").Trim().ToLowerInvariant() });   // ★N5：格式列归一化（读侧裸比较 == "int"，配成 "Int" 时读回恒空→每圈误触发回写）
                    for (int j = 0; j < fins_length; j++)
                    {
                        xuanzhong_temp = fins_qishi - address_qishi + j;
                        // ★第32轮：台账只有 0..49 格（10 行 × 5 组），原实现直接 fins_name[key] 索引，
                        //   块起始/长度配到范围外即抛 KeyNotFoundException：连接1 走全局崩溃弹窗，
                        //   连接2~4 被管理器 try{EnsureHandleCreated()}catch{} 吞掉，且 _formLoaded 已置真
                        //   → 该连接永久不轮询。改与轮询侧 SetPollCell 同口径的 TryGetValue，越界只跳过。
                        int _idx;
                        int[] _nameCell, _dataCell;
                        if (!int.TryParse(xuanzhong_temp.ToString(), out _idx)
                            || !fins_name.TryGetValue(_idx, out _nameCell)
                            || !fins_data.TryGetValue(_idx, out _dataCell))
                        {
                            greenSkipped++;
                            continue;
                        }
                        dataGridView1[_nameCell[0], _nameCell[1]].Style.BackColor = Color.Green;
                        dataGridView1[_dataCell[0], _dataCell[1]].Style.BackColor = Color.Green;
                        dataGridView1[_nameCell[0], _nameCell[1]].Value = fins_mingcheng;
                    }
                }
                if (greenSkipped > 0)
                    Log("初始化: " + greenSkipped + " 个块地址落在台账(0..49)之外，已跳过回绿，请核对起始地址/块长度/总长度配置");
            }
            for(int i=0;i<13;i++)
            {
                camera_dic.Add(i+1,new string[] { wdini.ReadString(FinsIniStore.CameraSection(_linkId, i + 1), "chufa", " ").Replace("\0", ""), wdini.ReadString(FinsIniStore.CameraSection(_linkId, i + 1), "fanhuizhi", "0").Replace("\0", ""), wdini.ReadString(FinsIniStore.CameraSection(_linkId, i + 1), "fanhuien", "false").Replace("\0", "").Trim().ToLowerInvariant(), wdini.ReadString(FinsIniStore.CameraSection(_linkId, i + 1), "fankui", "0").Replace("\0", ""),"无","无", wdini.ReadString(FinsIniStore.CameraSection(_linkId, i + 1), "chukufangshi", "相等").Replace("\0", ""), wdini.ReadString(FinsIniStore.CameraSection(_linkId, i + 1), "chufazhi1", "").Replace("\0", ""), wdini.ReadString(FinsIniStore.CameraSection(_linkId, i + 1), "chufazhi2", "").Replace("\0", "") });
            }
            comboBox5.Items.Add(camera_dic[1][0]);
            comboBox5.Text = camera_dic[1][0];
            textBox14.Text = camera_dic[1][1];
            if(camera_dic[1][2]=="true")
            checkBox3.CheckState = CheckState.Checked;
            comboBox6.Items.Add(camera_dic[1][3]);
            comboBox6.Text = camera_dic[1][3];

            comboBox8.Items.Add(camera_dic[2][0]);
            comboBox8.Text = camera_dic[2][0];
            textBox17.Text = camera_dic[2][1];
            if (camera_dic[2][2] == "true")
                checkBox4.CheckState = CheckState.Checked;
            comboBox7.Items.Add(camera_dic[2][3]);
            comboBox7.Text = camera_dic[2][3];

            comboBox10.Items.Add(camera_dic[3][0]);
            comboBox10.Text = camera_dic[3][0];
            textBox19.Text = camera_dic[3][1];
            if (camera_dic[3][2] == "true")
                checkBox5.CheckState = CheckState.Checked;
            comboBox9.Items.Add(camera_dic[3][3]);
            comboBox9.Text = camera_dic[3][3];

            comboBox12.Items.Add(camera_dic[4][0]);
            comboBox12.Text = camera_dic[4][0];
            textBox18.Text = camera_dic[4][1];
            if (camera_dic[4][2] == "true")
                checkBox6.CheckState = CheckState.Checked;
            comboBox11.Items.Add(camera_dic[4][3]);
            comboBox11.Text = camera_dic[4][3];

            comboBox14.Items.Add(camera_dic[5][0]);
            comboBox14.Text = camera_dic[5][0];
            textBox23.Text = camera_dic[5][1];
            if (camera_dic[5][2] == "true")
                checkBox10.CheckState = CheckState.Checked;
            comboBox13.Items.Add(camera_dic[5][3]);
            comboBox13.Text = camera_dic[5][3];

            comboBox16.Items.Add(camera_dic[6][0]);
            comboBox16.Text = camera_dic[6][0];
            textBox22.Text = camera_dic[6][1];
            if (camera_dic[6][2] == "true")
                checkBox9.CheckState = CheckState.Checked;
            comboBox15.Items.Add(camera_dic[6][3]);
            comboBox15.Text = camera_dic[6][3];

            comboBox18.Items.Add(camera_dic[7][0]);
            comboBox18.Text = camera_dic[7][0];
            textBox21.Text = camera_dic[7][1];
            if (camera_dic[7][2] == "true")
                checkBox8.CheckState = CheckState.Checked;
            comboBox17.Items.Add(camera_dic[7][3]);
            comboBox17.Text = camera_dic[7][3];

            comboBox20.Items.Add(camera_dic[8][0]);
            comboBox20.Text = camera_dic[8][0];
            textBox20.Text = camera_dic[8][1];
            if (camera_dic[8][2] == "true")
                checkBox7.CheckState = CheckState.Checked;
            comboBox19.Items.Add(camera_dic[8][3]);
            comboBox19.Text = camera_dic[8][3];

            comboBox4.Items.Add(camera_dic[9][0]);
            comboBox4.Text = camera_dic[9][0];
            textBox24.Text = camera_dic[9][1];
            if (camera_dic[9][2] == "true")
                checkBox11.CheckState = CheckState.Checked;
            comboBox28.Items.Add(camera_dic[9][3]);
            comboBox28.Text = camera_dic[9][3];

            // camera_dic[10] → 相机10
            comboBox22.Items.Add(camera_dic[10][0]);
            comboBox22.Text = camera_dic[10][0];
            textBox42.Text = camera_dic[10][1];
            if (camera_dic[10][2] == "true")
                checkBox13.CheckState = CheckState.Checked;
            comboBox23.Items.Add(camera_dic[10][3]);
            comboBox23.Text = camera_dic[10][3];

            // camera_dic[11] → 相机11
            comboBox24.Items.Add(camera_dic[11][0]);
            comboBox24.Text = camera_dic[11][0];
            textBox43.Text = camera_dic[11][1];
            if (camera_dic[11][2] == "true")
                checkBox14.CheckState = CheckState.Checked;
            comboBox25.Items.Add(camera_dic[11][3]);
            comboBox25.Text = camera_dic[11][3];

            // camera_dic[12] → 相机12
            comboBox26.Items.Add(camera_dic[12][0]);
            comboBox26.Text = camera_dic[12][0];
            textBox44.Text = camera_dic[12][1];
            if (camera_dic[12][2] == "true")
                checkBox15.CheckState = CheckState.Checked;
            comboBox27.Items.Add(camera_dic[12][3]);
            comboBox27.Text = camera_dic[12][3];

            // camera_dic[13] → 方案切换
            comboBox21.Items.Add(camera_dic[13][0]);
            comboBox21.Text = camera_dic[13][0];
            textBox41.Text = camera_dic[13][1];
            if (camera_dic[13][2] == "true")
                checkBox12.CheckState = CheckState.Checked;

            textBox40.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "1", "");
            textBox39.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "2", "");
            textBox38.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "3", "");
            textBox37.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "4", "");
            textBox36.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "5", "");
            textBox35.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "6", "");
            textBox34.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "7", "");
            textBox33.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "8", "");
            textBox45.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "9", "");
            textBox46.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "10", "");
            textBox47.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "11", "");
            textBox48.Text = wdini.ReadString(FinsIniStore.ChangeSection(_linkId), "12", "");
            textBox25.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "1", "");
            textBox26.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "2", "");
            textBox28.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "3", "");
            textBox27.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "4", "");
            textBox32.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "5", "");
            textBox31.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "6", "");
            textBox30.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "7", "");
            textBox29.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "8", "");
            textBox49.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "9", "");
            textBox50.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "10", "");
            textBox51.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "11", "");
            textBox52.Text = wdini.ReadString(FinsIniStore.PathSection(_linkId), "12", "");

            // camera_dic[13] → 心跳（反馈通道/心跳值/心跳使能）
            // ★心跳独立存储：读独立键 xintiao/xintiaoen；旧版本两者与“切换”共用 fanhuizhi/fanhuien。
            //   ★N3 修复（2026-09-20）：读到旧值后立即幂等落盘固化——此后彻底脱钩，避免
            //   “操作员改切换返回值/使能 → 重启后心跳跟着漂移或静默停用”。使能比较大小写不敏感。
            _heartbeatValue = wdini.ReadString(FinsIniStore.CameraSection(_linkId, 13), "xintiao", camera_dic[13][1]);
            _heartbeatEnabled = string.Equals(wdini.ReadString(FinsIniStore.CameraSection(_linkId, 13), "xintiaoen", camera_dic[13][2]), "true", StringComparison.OrdinalIgnoreCase);
            wdini.WriteString(FinsIniStore.CameraSection(_linkId, 13), "xintiao", _heartbeatValue);
            wdini.WriteString(FinsIniStore.CameraSection(_linkId, 13), "xintiaoen", _heartbeatEnabled ? "true" : "false");
            textBox53.Text = _heartbeatValue;
            if (_heartbeatEnabled)
                checkBox16.CheckState = CheckState.Checked;
            comboBox29.Items.Add(camera_dic[13][3]);
            comboBox29.Text = camera_dic[13][3];

            var cboModes = new ComboBox[] { cboTrigMode1, cboTrigMode2, cboTrigMode3, cboTrigMode4, cboTrigMode5, cboTrigMode6, cboTrigMode7, cboTrigMode8, cboTrigMode9, cboTrigMode10, cboTrigMode11, cboTrigMode12 };
            var txtVals = new TextBox[] { txtTrigVal1, txtTrigVal2, txtTrigVal3, txtTrigVal4, txtTrigVal5, txtTrigVal6, txtTrigVal7, txtTrigVal8, txtTrigVal9, txtTrigVal10, txtTrigVal11, txtTrigVal12 };
            for (int i = 0; i < 12; i++)
            {
                int cam = i + 1;
                cboModes[i].Text = camera_dic[cam][6];
                txtVals[i].Text = CommTriggerHelper.FormatTrigValDisplay(camera_dic[cam][6], camera_dic[cam][7], camera_dic[cam][8]);
                int capturedCam = cam;
                ComboBox modeBox = cboModes[i];
                TextBox valBox = txtVals[i];
                Action applyTrigMode = () =>
                {
                    camera_dic[capturedCam][6] = modeBox.Text;
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, capturedCam), "chukufangshi", modeBox.Text);
                    _triggerLatch[capturedCam] = false;
                };
                modeBox.SelectedIndexChanged += (s, ev) => applyTrigMode();
                modeBox.TextChanged += (s, ev) => applyTrigMode();
                valBox.TextChanged += (s, ev) => SaveOmronTrigVal(capturedCam, valBox);
            }

            Task.Run(() =>
            {

                Thread.Sleep(1000);
                if (fins_en && !commBlocked)
                {
                    // ★P3.1：button1_Click 是 async void，其同步前缀（textBox1/2/15/16、comboBox1 读取）
                    // 若在线程池线程执行会裸访问控件（Debug 开跨线程校验直接抛、Release 下 ComboBox 并发读竞态）。
                    // 收口到 UI 线程执行同步前缀；await 之后的 UI 回显该方法内部已自带 BeginInvoke，行为不变。
                    try
                    {
                        if (IsDisposed || Disposing) return;
                        if (this.InvokeRequired) this.Invoke(new Action(() => button1_Click(null, null)));
                        else button1_Click(null, null);
                    }
                    catch { }
                }
                if (!commBlocked) chushihua = true;
            });
            // 初始化完成：使能开则锁定参数控件（运行期使能开不容许修改参数，杜绝热改冲突）
            SetParamControlsEnabled(!fins_en);
            // ★心跳：UI 线程创建 1s 定时器（chushihua 在后台 Task 里置 true，故定时器在此处挂载）
            StartHeartbeatTimer();
        }


        private void Language( int language )
        {
            if (language == 2)
            {
                Text = "Omron Read PLC Demo";
                label24.Text = "Unit Num";
                label23.Text = "PC Net Num";

                label1.Text = "Ip:";
                label3.Text = "Port:";
                button1.Text = "Connect";
                button2.Text = "Disconnect";
                label21.Text = "Address:";
                label6.Text = "address:";
                label7.Text = "result:";

                button_read_bool.Text = "Read Bit";
                button_read_short.Text = "r-short";
                button_read_ushort.Text = "r-ushort";
                button_read_int.Text = "r-int";
                button_read_uint.Text = "r-uint";
                button_read_long.Text = "r-long";
                button_read_ulong.Text = "r-ulong";
                button_read_float.Text = "r-float";
                button_read_double.Text = "r-double";
                button_read_string.Text = "r-string";
                label8.Text = "length:";
                label11.Text = "Address:";
                label12.Text = "length:";
                button25.Text = "Bulk Read";
                label13.Text = "Results:";
                label16.Text = "Message:";
                label14.Text = "Results:";
                button26.Text = "Read";

                label10.Text = "Address:";
                label9.Text = "Value:";
                label19.Text = "Note: The value of the string needs to be converted";
                button24.Text = "Write Bit";
                button22.Text = "w-short";
                button21.Text = "w-ushort";
                button20.Text = "w-int";
                button19.Text = "w-uint";
                button18.Text = "w-long";
                button17.Text = "w-ulong";
                button16.Text = "w-float";
                button15.Text = "w-double";
                button14.Text = "w-string";

                groupBox1.Text = "Single Data Read test";
                groupBox2.Text = "Single Data Write test";
                groupBox3.Text = "Bulk Read test";
                groupBox4.Text = "Message reading test, hex string needs to be filled in";
            }
        }

        private void FormSiemens_FormClosing( object sender, FormClosingEventArgs e )
        {
            if (e.CloseReason != CloseReason.UserClosing)
                return;
            e.Cancel = true;
            this.Visible = false;
        }

        #region Connect And Close

        ErrorLog MsgErroeLog = new ErrorLog();

        // ★ 2026-09-11：异步化 FINS 建链。原为同步 Connect()，网关卡顿时阻塞 UI；加 _connecting 重入守卫防止启动/重试重复建链。
        private bool _connecting;
        private async void button1_Click( object sender, EventArgs e )
        {
            if (_connecting) { Log("正在连接中，请稍候..."); return; }
            // 连接
            System.Net.IPAddress address;
            if (!System.Net.IPAddress.TryParse( textBox1.Text, out address ))
            {
                Log(DemoUtils.IpAddressInputWrong );
                return;
            }

            int port;
            if (!int.TryParse( textBox2.Text, out port ))
            {
                Log( DemoUtils.PortInputWrong );
                return;
            }

            byte SA1;
            if (!byte.TryParse( textBox15.Text, out SA1 ))
            {
                Log( "SA1 Input Wrong！" );
                return;
            }

            byte DA2;
            if (!byte.TryParse( textBox16.Text, out DA2 ))
            {
                Log( "PLC DA2 input wrong！" );
                return;
            }

            _finsLink.Ip = textBox1.Text;
            _finsLink.Port = port;
            _finsLink.SA1 = SA1;
            _finsLink.DA2 = DA2;
            // ★P4：与后台自动重连互斥，避免两条线程同时操作同一 Hsl 客户端
            if (System.Threading.Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0)
            {
                Log("正在进行后台自动重连，请稍后再试。");
                return;
            }

            _connecting = true;
            try
            {
                // ★补修复（2026-09-23）：DataFormat 取值必须在 UI 线程完成（与 FormModbus/FormModbusRtu 对齐）——
                //   原在 Task.Run 后台 lambda 内读 comboBox1.SelectedItem，Debug 挂调试器时
                //   CheckForIllegalCrossThreadCalls 直抛（被外层 catch 兜住→连接必败）。
                //   null（未选择）时拆箱抛异常同样走 catch，_reconnecting 在 catch 里复位不锁死。
                var fmt = (HslCommunication.Core.DataFormat)comboBox1.SelectedItem;
                // ★W5 修复（2026-09-23 第23轮）：原 ConnectAsync 在后台线程裸跑——Close/换 Client/ConnectServer
                //   全程不持 _ioSync，轮询读写/心跳可能正握着被 ConnectClose/Dispose 的旧客户端（P4 的
                //   _reconnecting 门控只挡"检查在开始前"的路径，已进锁者不受阻）。现在 Task.Run 内持 _ioSync
                //   跑同步 Connect：建链与全部 I/O 同锁串行；_reconnecting 仍先行置 1，等待锁的轮询醒来即见门控跳过。
                var connect = await System.Threading.Tasks.Task.Run(
                    () => { lock (_ioSync) { return _finsLink.Connect( fmt ); } } );
                // ★P4：建链动作已完成，立即释放互斥（放在此处而非 UI 回调里，避免回调异常导致永久锁死）
                System.Threading.Interlocked.Exchange(ref _reconnecting, 0);
                // 建链在后台线程完成，结果回写 UI 需切回界面线程
                if (IsDisposed || Disposing) { _connecting = false; return; }
                BeginInvoke(new Action(() =>
                {
                    _connecting = false;
                    if (connect.IsSuccess)
                    {
                        Log( HslCommunication.StringResources.Language.ConnectedSuccess );
                        button2.Enabled = true;
                        button1.Enabled = false;
                        panel2.Enabled = true;
                        userControlCurve1.ReadWriteNet = _finsLink.Client;
                        try { StartHeartbeatTimer(); } catch { }   // ★N2：连接成功恢复心跳（断开时已停）
                    }
                    else
                    {
                        Log( HslCommunication.StringResources.Language.ConnectedFailed + "(" + connect.Message + ")" );
                        button1.Enabled = true;
                    }
                }));
            }
            catch (Exception ex)
            {
                _connecting = false;
                System.Threading.Interlocked.Exchange(ref _reconnecting, 0);   // ★P4：异常也要释放互斥
                Log( "连接异常: " + ex.Message );
                try { button1.Enabled = true; } catch { }
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            // 断开连接（手动断开按钮，逻辑与使能关闭断连一致）
            DisconnectLink();
        }
        

        #endregion

        #region 单数据读取测试


        private void button_read_bool_Click( object sender, EventArgs e )
        {
            // 读取bool变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadBool( textBox3.Text ), textBox3.Text, textBox4 ));
        }
        private void button_read_short_Click( object sender, EventArgs e )
        {
            // 读取short变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadInt16( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_ushort_Click( object sender, EventArgs e )
        {
            // 读取ushort变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadUInt16( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_int_Click( object sender, EventArgs e )
        {
            // 读取int变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadInt32( textBox3.Text ), textBox3.Text, textBox4 ));
        }
        private void button_read_uint_Click( object sender, EventArgs e )
        {
            // 读取uint变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadUInt32( textBox3.Text ), textBox3.Text, textBox4 ));
        }
        private void button_read_long_Click( object sender, EventArgs e )
        {
            // 读取long变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadInt64( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_ulong_Click( object sender, EventArgs e )
        {
            // 读取ulong变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadUInt64( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_float_Click( object sender, EventArgs e )
        {
            // 读取float变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadFloat( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_double_Click( object sender, EventArgs e )
        {
            // 读取double变量
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadDouble( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_string_Click( object sender, EventArgs e )
        {
            // 读取字符串
            ManualRead(() => DemoUtils.ReadResultText( () => omronFinsNet.ReadString( textBox3.Text, ushort.Parse( textBox5.Text ) ), textBox3.Text, textBox4 ));
        }

        // ★BUG1/BUG3 修复：手动"读"按钮运行在 UI 线程，若此刻正在重连或客户端未连接，
        //   裸调 Hsl 读会因对关闭中的 socket 发请求而抛异常 → UI 线程未处理 → 整个程序崩。
        //   复用与轮询读相同的 _reconnecting 门控（重连进行中直接返回），并统一 try/catch 兜底。
        // ★全 I/O 单锁串行：所有 Hsl socket 读都经此走 _ioSync，与回写/重连互斥，
        //   杜绝轮询读/手动读与自动重连的 ConnectClose 抢同一 socket（BUG1 根因）。
        private HslCommunication.OperateResult<T> ReadLocked<T>(Func<HslCommunication.OperateResult<T>> readOp)
        {
            lock (_ioSync) { return readOp(); }
        }

        // ★BUG1/BUG3 修复 + ★W2/W5 修复（2026-09-23 第23轮）：
        //   W2：原实现把 op/readAction 整个放进 lock(_ioSync)——其内部调 DemoUtils.*Render 弹模态框，
        //       弹窗未点掉锁就不释放，轮询读写/心跳/自动重连全部排队卡死，PLC 触发脉冲成批丢失。
        //       现 op 只做 I/O 并返回"需要弹窗的文本"（null=成功静默），MessageBox 一律在放锁之后弹。
        //   W5：门控原本只在锁外查一次，"检查→拿到锁"窗口内手动建链线程可能已换掉/释放客户端（TOCTOU）。
        //       现锁内复查 _reconnecting 与客户端非空后才执行，配合手动建链持 _ioSync，两条路径彻底串行。
        private void ManualRead(Func<string> readOp)
        {
            ManualGuarded(readOp, "读取出错");
        }

        private void ManualGuarded(Func<string> op, string errorTitle)
        {
            if (_reconnecting != 0)
            {
                MessageBox.Show("通讯正在重连，请稍候再试。", "提示");
                return;
            }
            if (omronFinsNet == null)
            {
                MessageBox.Show("尚未连接 PLC，请先连接。", "提示");
                return;
            }
            string msg = null;
            bool failed = false;
            // ★审核建议②：锁等待封顶 1 秒——轮询/重连正持锁时不再让 UI 线程无限排队（最坏冻到对端超时），
            //   超时直接回"链路正忙"，操作员稍后再点即可。op 本身的同步读写保留（其内直读直写控件，异步化需全量快照改造）。
            if (!System.Threading.Monitor.TryEnter(_ioSync, 1000))
            {
                msg = "链路正忙（轮询读写或重连未返回），请稍后再试。";
            }
            else
            {
                try
                {
                    if (_reconnecting != 0) msg = "通讯正在重连，请稍候再试。";
                    else if (omronFinsNet == null) msg = "尚未连接 PLC，请先连接。";
                    else
                    {
                        try { msg = op(); }
                        catch (Exception ex) { msg = ex.Message; failed = true; }
                    }
                }
                finally { System.Threading.Monitor.Exit(_ioSync); }
            }
            // ★复审修复（2026-09-23）：failed 原只认异常——协议层失败（ResultText 返回的 "Read/Write Failed…"
            //   非异常路径）也被按"提示"弹，且成功文案曾伪装成失败。现约定 ErrorMarker 前缀=失败：
            //   识别后剥标并按 errorTitle 弹（醒目）；null=成功静默；其余无前缀文案=一般提示按"提示"弹。
            if (!string.IsNullOrEmpty(msg))
            {
                if (msg.StartsWith(DemoUtils.ErrorMarker, StringComparison.Ordinal))
                {
                    failed = true;
                    msg = msg.Substring(DemoUtils.ErrorMarker.Length);
                }
                MessageBox.Show(msg, failed ? errorTitle : "提示");
            }
        }

        #endregion

        #region 单数据写入测试


        private void button24_Click( object sender, EventArgs e )
        {
            // bool写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, bool.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }

        private void button22_Click( object sender, EventArgs e )
        {
            // short写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, short.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }

        private void button21_Click( object sender, EventArgs e )
        {
            // ushort写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, ushort.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }


        private void button20_Click( object sender, EventArgs e )
        {
            // int写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, int.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }

        private void button19_Click( object sender, EventArgs e )
        {
            // uint写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, uint.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }

        private void button18_Click( object sender, EventArgs e )
        {
            // long写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, long.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }

        private void button17_Click( object sender, EventArgs e )
        {
            // ulong写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, ulong.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }

        private void button16_Click( object sender, EventArgs e )
        {
            // float写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, float.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }

        private void button15_Click( object sender, EventArgs e )
        {
            // double写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, double.Parse( textBox7.Text ) ), textBox8.Text ), "写入出错");
        }


        private void button14_Click( object sender, EventArgs e )
        {
            // string写入
            ManualGuarded(() => DemoUtils.WriteResultText( () => omronFinsNet.Write( textBox8.Text, textBox7.Text ), textBox8.Text ), "写入出错");
        }
        
        #endregion

        #region 批量读取测试

        private void button25_Click( object sender, EventArgs e )
        {
            ManualGuarded(() => DemoUtils.BulkReadResultText( omronFinsNet, textBox6, textBox9, textBox10 ), "读取出错");
        }



        #endregion

        #region 报文读取测试


        private void button26_Click( object sender, EventArgs e )
        {
            // ★G6：原实现无 try 无锁——读失败/空引用直接把 UI 打崩
            // ★W2：lambda 只返回要弹窗的文本，MessageBox 由 ManualGuarded 在释放 _ioSync 后统一弹
            ManualGuarded(() =>
            {
                OperateResult<byte[]> read = omronFinsNet.ReadFromCoreServer( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) );
                if (read.IsSuccess)
                {
                    textBox11.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content );
                    return null;
                }
                return DemoUtils.ErrorMarker + "Read Failed：" + read.ToMessageShowString( );
            }, "读取出错");
        }


        #endregion
        
        private void test()
        {
            // 读取操作，这里的D100可以替换成C100,A100,W100,H100效果时一样的
            bool D100_7 = omronFinsNet.ReadBool( "D100.7" ).Content;  // 读取D100.7是否通断，注意D100.0等同于D100
            short short_D100 = omronFinsNet.ReadInt16( "D100" ).Content; // 读取D100组成的字
            ushort ushort_D100 = omronFinsNet.ReadUInt16( "D100" ).Content; // 读取D100组成的无符号的值
            int int_D100 = omronFinsNet.ReadInt32( "D100" ).Content;         // 读取D100-D101组成的有符号的数据
            uint uint_D100 = omronFinsNet.ReadUInt32( "D100" ).Content;      // 读取D100-D101组成的无符号的值
            float float_D100 = omronFinsNet.ReadFloat( "D100" ).Content;   // 读取D100-D101组成的单精度值
            long long_D100 = omronFinsNet.ReadInt64( "D100" ).Content;      // 读取D100-D103组成的大数据值
            ulong ulong_D100 = omronFinsNet.ReadUInt64( "D100" ).Content;   // 读取D100-D103组成的无符号大数据
            double double_D100 = omronFinsNet.ReadDouble( "D100" ).Content; // 读取D100-D103组成的双精度值
            string str_D100 = omronFinsNet.ReadString( "D100", 5 ).Content;// 读取D100-D104组成的ASCII字符串数据

            // 写入操作，这里的D100可以替换成C100,A100,W100,H100效果时一样的
            omronFinsNet.Write( "D100", (byte)0x33 );            // 写单个字节
            omronFinsNet.Write( "D100", (short)12345 );          // 写双字节有符号
            omronFinsNet.Write( "D100", (ushort)45678 );         // 写双字节无符号
            omronFinsNet.Write( "D100", (uint)3456789123 );      // 写双字无符号
            omronFinsNet.Write( "D100", 123.456f );              // 写单精度
            omronFinsNet.Write( "D100", 1234556434534545L );     // 写大整数有符号
            omronFinsNet.Write( "D100", 523434234234343UL );     // 写大整数无符号
            omronFinsNet.Write( "D100", 123.456d );              // 写双精度
            omronFinsNet.Write( "D100", "K123456789" );// 写ASCII字符串

            OperateResult<byte[]> read = omronFinsNet.Read( "D100", 5 );
            {
                if (read.IsSuccess)
                {
                    // 此处需要根据实际的情况来自定义来处理复杂的数据
                    short D100 = omronFinsNet.ByteTransform.TransInt16( read.Content, 0 );
                    short D101 = omronFinsNet.ByteTransform.TransInt16( read.Content, 2 );
                    short D102 = omronFinsNet.ByteTransform.TransInt16( read.Content, 4 );
                    short D103 = omronFinsNet.ByteTransform.TransInt16( read.Content, 6 );
                    short D104 = omronFinsNet.ByteTransform.TransInt16( read.Content, 7 );
                }
                else
                {
                    // 发生了异常
                    // 
                }
            }
        }
        private bool front = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            // 被嵌入“连接设备”管理器时不再执行置顶/抢焦点，避免切换节点卡顿和关闭时窗口乱跳
            if (!this.TopLevel) { front = false; return; }
            if (this.Visible == true && front == false)
            {
                front = true;
                this.BringToFront();
                 this.TopMost = true;
            }
            if (this.Visible == false)
            {
                front = false;
            }
        }

        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            CommGridHelper.RowPrePaintZebra(sender, e);
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.RowIndex % 2 == 1)
            {
                x = e.ColumnIndex;
                y = e.RowIndex;
                return;
            }
            x = 999;
            y = 999;
        }
        bool fins_lunxunen = false;
       // ★BUG2：与 chushihua 同款。UI 线程写、轮询线程读，需 volatile 保证跨线程可见性。
       public volatile bool fins_en = false;
        decimal lunxun_time = 0;
        // ★P2：线程池线程（InitializeForm 末尾 Task.Run）写 true，轮询线程（Fins_duxie）读；加 volatile 保证跨线程可见性
        public volatile bool chushihua = false;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _triggerLatch = new System.Collections.Concurrent.ConcurrentDictionary<int, bool>();
        private int _lastTrigReadFailLogTick; // ★R11：读失败跳过触发判定的限流日志时戳
        private int _commFailCount = 0;
        private int _reconnecting = 0;
        private long _lastReconnectAttemptTicks = 0;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox1.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
                else
                {
                    fins_lunxunen = false;
                }
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "fins_lunxunen", fins_lunxunen.ToString());
            }
        }

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            bool xuanzhong_temp = false;
           
            if (sender is DataGridView)
            {
                DataGridView dgv = (DataGridView)sender;
                if (e.RowIndex%2==1&&e.ColumnIndex>=0)//如果该行为表头
                {
                    
                    
                    x = e.ColumnIndex;
                    y = e.RowIndex;
                    foreach (var pair in fins_data)
                    {
                        if (pair.Value[0] == x&& pair.Value[1] == y)
                        {
                            foreach(var p_temp in fins_dic)
                            {
                                if((pair.Key + address_qishi)>= int.Parse( p_temp.Value[1])&& (pair.Key + address_qishi )< int.Parse(p_temp.Value[1])+ int.Parse(p_temp.Value[2]))
                                {
                                    xuanzhong_temp = true;
                                    numericUpDown5.Value = int.Parse(p_temp.Value[1]);
                                    int blockIdx = int.Parse(p_temp.Value[1]) - int.Parse(address_qishi.ToString());
                                    if (fins_data.ContainsKey(blockIdx))
                                    {
                                        x = fins_data[blockIdx][0];
                                        y = fins_data[blockIdx][1];
                                    }
                                    numericUpDown4.Value =int.Parse(p_temp.Value[2]);
                                    textBox12.Text = p_temp.Value[0];
                                    comboBox2.Text= p_temp.Value[3];
                                    comboBox3.Text = p_temp.Value[4];
                                }
                                
                            }
                            if (xuanzhong_temp == false)
                            {
                                numericUpDown5.Value = pair.Key+address_qishi;
                            }
                        }
                    }
                    dataGridView1.CurrentCell = dataGridView1[e.ColumnIndex, e.RowIndex];
                }
                else
                {
                    dataGridView1.ClearSelection();
                    x = 999;
                    y = 999;
                }
            }
        }
        int geshu = 0;
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                fins_lunxunen = false;
                decimal xuanzhong_temp = 0;
                bool chongdie = false;
                if (comboBox3.Text.Length > 1 && comboBox2.Text.Length > 1 && textBox12.Text.Length > 0)
                {
                    if ((comboBox3.Text == "string" || comboBox3.Text == "int") || (numericUpDown5.Value % 2 == 0))
                    {
                        if (numericUpDown5.Value >= numericUpDown1.Value && numericUpDown1.Value + numericUpDown2.Value >= numericUpDown4.Value + numericUpDown5.Value)
                        {
                            if (textBox12.Text.Length > 0)
                            {
                                if (fins_dic.Count < 10)
                                {
                                    if(fins_dic.Count>0)
                                    {
                                        foreach(var pat in fins_dic)
                                        {
                                            if(decimal.Parse(pat.Value[1])>= numericUpDown5.Value+ numericUpDown4.Value || decimal.Parse(pat.Value[1])+ decimal.Parse(pat.Value[2])<= numericUpDown5.Value)
                                            {
                                                
                                            }
                                            else
                                            {
                                                chongdie = true;
                                            }
                                        }
                                    }
                                    if (chongdie)
                                    {
                                        MessageBox.Show("数据有重叠");
                                    }
                                    else if (fins_dic.ContainsKey(textBox12.Text))
                                    {
                                        // ★第33轮：重叠检查只看地址段，同名两块（地址不重叠时）会一路走到
                                        //   fins_dic.Add 抛 ArgumentException = 全局"程序崩溃"弹窗。改为明确提示。
                                        MessageBox.Show("已存在同名数据块「" + textBox12.Text + "」，请换一个名称。");
                                    }
                                    else
                                    {
                                        fins_dic.Add(textBox12.Text, new string[] { textBox12.Text, numericUpDown5.Value.ToString(), numericUpDown4.Value.ToString(), comboBox2.Text, (comboBox3.Text ?? "").Trim().ToLowerInvariant() });   // ★N5：格式列归一化（同 ini 加载路径）
                                        for (int i = 0; i < numericUpDown4.Value; i++)
                                        {
                                            xuanzhong_temp = numericUpDown5.Value - numericUpDown1.Value + i;
                                            dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                                            dataGridView1[fins_data[int.Parse(xuanzhong_temp.ToString())][0], fins_data[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                                            dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Value = textBox12.Text;
                                        }
                                        geshu = fins_dic.Count;
                                        wdini.WriteString(FinsIniStore.ConnSection(_linkId), "geshu", fins_dic.Count.ToString());
                                        wdini.WriteString(FinsIniStore.BlockSection(_linkId, geshu), "name", textBox12.Text);
                                        wdini.WriteString(FinsIniStore.BlockSection(_linkId, geshu), "qishi", numericUpDown5.Value.ToString());
                                        wdini.WriteString(FinsIniStore.BlockSection(_linkId, geshu), "changdu", numericUpDown4.Value.ToString());
                                        wdini.WriteString(FinsIniStore.BlockSection(_linkId, geshu), "gaodiwei", comboBox2.Text);
                                        wdini.WriteString(FinsIniStore.BlockSection(_linkId, geshu), "geshi", comboBox3.Text);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("数据组数不能超过10组");
                                }
                            }
                            else
                            {
                                MessageBox.Show("请设置此段数据的名称");
                            }
                        }
                        else
                        {
                            MessageBox.Show("数据地址超出限定范围");
                        }
                    }
                    else
                    {
                        MessageBox.Show("数据长度设置错误");
                    }
                }
                else
                {
                    MessageBox.Show("数据不完整");
                }
                if(checkBox1.CheckState==CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                if (checkBox1.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
            }
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {

           // dataGridView1[fins_name[int.Parse(numericUpDown4.Value.ToString())][0], fins_name[int.Parse(numericUpDown4.Value.ToString())][1]].Style.BackColor = Color.Green;
            //dataGridView1[fins_data[int.Parse(numericUpDown4.Value.ToString())][0], fins_data[int.Parse(numericUpDown4.Value.ToString())][1]].Style.BackColor = Color.Green;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text=="显示")
            {
                tabControl1.Visible=true;
                button4.Text = "隐藏";
            }
            else
            {
                tabControl1.Visible = false;
                button4.Text = "显示";
            }
        }

        private void SaveOmronTrigVal(int cam, TextBox tb)
        {
            CommTriggerHelper.ParseTrigValInput(camera_dic[cam][6], tb.Text, camera_dic[cam]);
            wdini.WriteString(FinsIniStore.CameraSection(_linkId, cam), "chufazhi1", camera_dic[cam][7]);
            wdini.WriteString(FinsIniStore.CameraSection(_linkId, cam), "chufazhi2", camera_dic[cam][8]);
            _triggerLatch[cam] = false;
        }

        // ===================== 相机归属防呆（阶段 5） =====================
        // 同协议不同 FINS 连接不能把同一台物理相机（1..12）配置成自己的触发/反馈，
        // 否则两台 PLC 会“抢”同一台相机：重复触发拍照、结果回写目标不唯一。
        // 相机 13 是功能槽：触发侧=“切换方案”（全局唯一，只允许一条连接配置）；
        //                   反馈侧=“心跳”（各连接与各自 PLC 保活，互不占用）。
        // 相机首次在本连接建立绑定时，先查其它连接是否已占用；已占用则回滚选择并提示。
        // 相机已归属本连接（触发/反馈任一已有值）后允许换绑其它块，不产生新的跨连接占用。
        /// <summary>相机触发(slot=0)/反馈(slot=3)下拉的统一保存入口（带防呆）。</summary>
        private void TryBindCamera(int cam, ComboBox box, int slot)
        {
            if (!chushihua) return;
            if (fins_en) return;   // 使能开时参数锁定，不容许修改相机绑定（防运行期热改 camera_dic 竞态）
            if (cam < 1 || cam >= camera_dic.Count + 1) return;
            string val = box.Text;
            if (val == "（无绑定）") val = "";        // “（无绑定）”= 清空绑定（相机让出给别的连接）
            string prev = camera_dic[cam][slot];
            if (prev == val) return;                 // 未变化；或冲突回滚后二次回调直接放行

            bool bound = CommCameraGuard.IsBoundValue(val);
            // 相机 13 反馈侧是“心跳”：各连接与各自 PLC 保活，不参与跨连接占用。
            bool guarded = bound && !(cam == 13 && slot == 3);
            if (guarded)
            {
                // 自拥有判定：相机 13 只看触发侧（方案切换槽）；相机 1..12 触发或反馈任一已有值即视为已归本连接。
                bool selfBound = cam == 13
                    ? CommCameraGuard.IsBoundValue(camera_dic[13][0])
                    : CommCameraGuard.IsBoundValue(camera_dic[cam][0])
                      || CommCameraGuard.IsBoundValue(camera_dic[cam][3]);
                if (!selfBound)
                {
                    // 刷新为磁盘最新配置（其它连接实例可能刚写入），再做占用检查
                    int owner = 0;
                    try { wdini.ReadINIFile(wdini.FileName); } catch { }
                    try { owner = CommCameraGuard.FindOwner(wdini, _linkId, cam); } catch { }
                    if (owner > 0)
                    {
                        box.Text = prev;             // 回滚下拉，不写 ini
                        string name = CommCameraGuard.OwnerDisplayName(wdini, owner);
                        string msg = cam == 13
                            ? "“切换方案”触发块已由 FINS 连接 " + owner + "（" + name + "）配置。" + Environment.NewLine +
                              "方案切换是整条产线的全局动作，同一协议内只允许一条连接负责切方案（各连接的心跳可独立配置，互不影响）。" + Environment.NewLine +
                              "如需改到本连接：请先选中 FINS 连接 " + owner + "，把“切换方案”触发下拉选成“（无绑定）”释放。"
                            : "相机 " + cam + " 已由 FINS 连接 " + owner + "（" + name + "）配置了触发/反馈。" + Environment.NewLine +
                              "同一协议内，一台相机只能归属一条连接，否则会互相竞争拍照。" + Environment.NewLine +
                              "如需把该相机改到本连接：请先选中 FINS 连接 " + owner + "，把相机 " + cam + " 的触发/反馈下拉都选成“（无绑定）”释放，再来本连接配置。";
                        MessageBox.Show(this, msg, "相机占用冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // ★ 跨协议相机绑定软提示（不硬拦）：另一协议已绑同物理相机时，提示重复触发风险，仍允许保存
            if (cam >= 1 && cam <= 12)
            {
                try { wdini.ReadINIFile(wdini.FileName); } catch { }
                string xwarn = CommCameraGuard.CrossProtoWarning(wdini, CommCameraGuard.CommProto.Fins, _linkId, cam);
                if (!string.IsNullOrEmpty(xwarn))
                    MessageBox.Show(this, xwarn, "跨协议相机绑定提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // ★N4：反馈通道同块告警（两行写同一块会互相覆盖；心跳与相机反馈同块时会被每秒覆盖）
            if (slot == 3) WarnSameFeedbackBlock(cam, val);
            camera_dic[cam][slot] = val;
            string key = slot == 0 ? "chufa" : "fankui";
            wdini.WriteString(FinsIniStore.CameraSection(_linkId, cam), key, val);
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            FormOperationHelp.ShowHelp(this, "欧姆龙Fins");
        }

        /// <summary>断开当前连接（使能关闭时调用），不影响“使能”状态本身。</summary>
        private void DisconnectLink()
        {
            // ★M5 修复：与轮询读/写/重连共用 _ioSync 单锁——原实现直接 Close，
            //   会与在途 I/O 抢同一 socket（读路径 815/828 也持该锁）。
            lock (_ioSync) { try { _finsLink.Close(); } catch { } }
            try { StopHeartbeatTimer(); } catch { }   // ★N2：断开连接即停心跳（防对已关链路每秒空写/触发重连）
            button2.Enabled = false;
            button1.Enabled = true;
            panel2.Enabled = false;
        }

        /// <summary>
        /// 参数控件锁定：使能开(en=true)时锁定所有通讯/相机参数控件，运行时不容许修改；
        /// 使能关(en=false)时解禁，此时才可修改参数。配合“使能关立即断连”，
        /// 保证“修改参数”与“活跃连接”严格互斥，从根上消除运行期热改参数的并发冲突。
        /// </summary>
        private void SetParamControlsEnabled(bool enable)
        {
            // 连接参数
            textBox1.Enabled = enable; textBox2.Enabled = enable;
            textBox15.Enabled = enable; textBox16.Enabled = enable;
            comboBox1.Enabled = enable;
            // 轮询 / 地址参数
            numericUpDown1.Enabled = enable; numericUpDown2.Enabled = enable; numericUpDown3.Enabled = enable;
            // 相机绑定下拉 comboBox4..29
            for (int i = 4; i <= 29; i++)
            {
                var found = this.Controls.Find("comboBox" + i, true);
                if (found.Length > 0 && found[0] is ComboBox cb) cb.Enabled = enable;
            }
            // 触发值 / 返回值文本框（静态）
            foreach (var t in new TextBox[] { textBox14, textBox17, textBox19, textBox18, textBox23, textBox22, textBox21, textBox20, textBox24, textBox42, textBox43, textBox44, textBox41, textBox53 })
                if (t != null) t.Enabled = enable;
            // 触发模式 / 触发值（动态）
            foreach (var c in new ComboBox[] { cboTrigMode1, cboTrigMode2, cboTrigMode3, cboTrigMode4, cboTrigMode5, cboTrigMode6, cboTrigMode7, cboTrigMode8, cboTrigMode9, cboTrigMode10, cboTrigMode11, cboTrigMode12 })
                if (c != null) c.Enabled = enable;
            foreach (var t in new TextBox[] { txtTrigVal1, txtTrigVal2, txtTrigVal3, txtTrigVal4, txtTrigVal5, txtTrigVal6, txtTrigVal7, txtTrigVal8, txtTrigVal9, txtTrigVal10, txtTrigVal11, txtTrigVal12 })
                if (t != null) t.Enabled = enable;
            // 反馈使能 / 心跳使能开关
            checkBox3.Enabled = enable; checkBox16.Enabled = enable;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                bool en = checkBox2.CheckState == CheckState.Checked;
                fins_en = en;
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "fins_en", fins_en.ToString());
                if (!en)
                {
                    // 使能关闭：立即断连（fins_en 已置 false，轮询/重连逻辑均不会自动重连），参数控件解禁后可安全修改
                    DisconnectLink();
                    SetParamControlsEnabled(true);
                }
                else
                {
                    // 使能开启：锁定参数控件 + 确保通讯在线（立即自动建连；运行中断线由轮询后台自动重连兜底）
                    SetParamControlsEnabled(false);
                    try { button1_Click(null, null); } catch { }
                }
            }
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                lunxun_time = numericUpDown3.Value;
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "lunxun_time", lunxun_time.ToString());
               
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                address_length = numericUpDown2.Value;
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "zongchang", numericUpDown2.Value.ToString());
            }
        }

        private bool _qishiRevert;   // ★第32轮 S2：回退起始地址时防本处理器重入
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                // ★第32轮 S2：起始地址决定台账 0..49 格对应哪段绝对地址。改成让已有数据块
                //   算出负数或超 49 的格位，等于把"每次启动都越界"的坏配置写进 ini（A5 只降级了后果）。
                if (!_qishiRevert)
                {
                    string bad = CommGridHelper.FindBlockOutOfRange(fins_dic, numericUpDown1.Value, fins_data.Count);
                    if (bad != null)
                    {
                        _qishiRevert = true;
                        try { numericUpDown1.Value = address_qishi; }
                        finally { _qishiRevert = false; }
                        Log("起始地址未改：" + bad + "（请先调整/删除该数据块）");
                        MessageBox.Show("起始地址未修改：" + bad + "\r\n\r\n请先调整或删除该数据块，再改起始地址。",
                            "配置越界", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                address_qishi = numericUpDown1.Value;
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "qishi", numericUpDown1.Value.ToString());
            }
        }

        private ComboBox[] GetBindingCombos()
        {
            return new ComboBox[]
            {
                comboBox4, comboBox5, comboBox6, comboBox7, comboBox8, comboBox9,
                comboBox10, comboBox11, comboBox12, comboBox13, comboBox14, comboBox15,
                comboBox16, comboBox17, comboBox18, comboBox19, comboBox20, comboBox21,
                comboBox22, comboBox23, comboBox24, comboBox25, comboBox26, comboBox27,
                comboBox28, comboBox29
            };
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!CommGridHelper.TryPrepareFullClear(textBox12.Text.Trim(), () => textBox12.Text = ""))
                return;
            CommGridHelper.PerformFullDataClear(wdini, "fins", "", ref geshu, fins_dic, _triggerLatch);
            dataGridView1.Visible = false;
            CommGridHelper.ClearGridZebra(dataGridView1);
            dataGridView1.Visible = true;
            button6_Click(null, null);
        }
        private Thread fins_duxie;
        // ★ I/O 串行锁：同一连接实例的“网络写”共用（轮询触发回写 / 检测结果回写 / 切型回执），
        //   避免多个检测线程或轮询线程并发操作同一 Hsl 客户端导致收发串包/响应错乱。
        private readonly object _ioSync = new object();
        private volatile bool _stopPolling = false;
        // 防止嵌入/恢复顶级时句柄重建导致 Load 重复执行。
        private bool _formLoaded = false;
        private CommGridUiSink _gridUi;
        public string GetMiddleValue(string str, string sta, string end)
        {
            Regex rg = new Regex("(?<=(" + sta + "))[.\\s\\S]*?(?=(" + end + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Match(str).Value;
        }

        /// <summary>
        /// 触发寄存器写回返回值，防止 PLC 保持非零值导致二次触发
        /// </summary>
        private void WriteTriggerFanhuizhi(string[] parValue, string fanhuizhi, ref int xuanzhong_temp, ref string fins_temp)
        {
            // ★BUG1：重连进行中（_reconnecting!=0）socket 正被 ConnectClose/ConnectServer 操作，
            //   此刻写会与重连抢同一 socket → 写脏数据/抛异常。与轮询读、手动读共用同一道 _reconnecting 门控。
            if (_reconnecting != 0)
            {
                // ★F7 修复（2026-09-20）：原实现静默 return——重连期间的触发回执写被无声丢弃，
                //   现场只见"PLC 没收到回执"却无日志线索。补日志（LogXieRetry 限流）。
                try { Log("重连中，触发回执写被跳过（PLC 侧靠超时判 NG）: " + fanhuizhi); } catch { }
                return;
            }
            lock (_ioSync)   // ★ I/O 串行：与 xie()/回执写互斥，防同一 Hsl 客户端并发写
            {
            // ★F7 修复（2026-09-20）：格式串归一化——原实现精确比较 == "int"/"string"/"long"/"float"，
            //   配置成 "Int"/"INT"/带空格时全部不命中（静默不写）而调用方仍记"回执成功"；
            //   xie() 早已用 ToLowerInvariant() 归一化，此处对齐同一标准。
            string _fmt = (parValue[4] ?? "").Trim().ToLowerInvariant();
            for (int j = 0; j < int.Parse(parValue[2]); j++)
            {
                if (_fmt == "int")
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + (int.Parse(parValue[1]) + j).ToString(), short.Parse(fanhuizhi)), "D" + (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (_fmt == "string")
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + (int.Parse(parValue[1]) + j).ToString(), fanhuizhi), "D" + (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (_fmt == "long" && j % 2 == 0)
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + (int.Parse(parValue[1]) + j).ToString(), int.Parse(fanhuizhi)), "D" + (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (_fmt == "float" && j % 2 == 0)
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + (int.Parse(parValue[1]) + j).ToString(), float.Parse(fanhuizhi)), "D" + (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
            }
            }
        }

        /// <summary>停止轮询线程（程序关闭前调用，配合退出流程）</summary>
        public void StopPolling()
        {
            foreach (var rt in _runtimes.Values)
                if (rt != null) rt.Stop();
            _runtime = null;
        }

        /// <summary>
        /// 释放本实例（多实例改造阶段 5：连接 2~4 在管理器中被移除/切换时调用）。
        /// 停止本连接轮询线程、断开底层连接并从 CommLinkManager 注销，避免实例被 Dispose 后线程仍空转轮询。
        /// 连接 1（常驻单例）不走此方法，其生命周期仍随主程序退出。
        /// </summary>
        public void ReleaseInstance()
        {
            try { StopPolling(); } catch { }
            try { StopHeartbeatTimer(); } catch { }   // ★N2：释放实例即停心跳定时器（防窗体被 Tick 闭包钉住泄漏）
            try { if (_finsLink != null) _finsLink.Close(); } catch { }
            try
            {
                var mgr = AppHost.Services.Resolve<CommLinkManager>();
                if (mgr != null) mgr.Unregister(_linkId);
            }
            catch { }
        }

        /// <summary>强制创建窗体句柄并完成整窗初始化（程序启动时恢复后台已启用连接用，避免外部无法访问 protected CreateHandle）。
        /// 说明：创建句柄不会触发 Load（Load 只在首次 Show 触发），故建句柄后必须显式 InitializeForm，
        /// 否则隐藏的额外实例只有被用户点开节点（Show）才会真正启动运行时。</summary>
        public void EnsureHandleCreated()
        {
            if (!IsHandleCreated) CreateHandle();
            InitializeForm();
        }

        void  Fins_duxie()
        {
            while (!_stopPolling)
            {
                // 轮询间隔非法(≤0)时也保证休眠，避免忙等占满CPU
                int pollInterval = (int)lunxun_time;
                if (pollInterval <= 0) pollInterval = 100;
                Thread.Sleep(pollInterval);
                if (lunxun_time <= 0)
                    continue;
                try
                {
                    if (chushihua)
                        {
                            if (_reconnecting != 0)
                                continue;
                            if (fins_lunxunen)
                            {
                                int xuanzhong_temp = 0;
                                string fins_temp = "";
                                string shuju_temp = "";
                                // ★P1：UI 线程会原地增删/清空 fins_dic（"清除"会调用 Clear()），轮询直接遍历会抛
                                // "Collection was modified" 并被外层 catch 吞掉（丢一圈 + 日志刷屏）。改为先快照再遍历。
                                Dictionary<string, string[]> finsSnapshot = CommGridHelper.SnapshotFinsDic(fins_dic);
                                if (finsSnapshot != null && finsSnapshot.Count > 0)
                                {
                                    foreach (var par in finsSnapshot)
                                    {
                                        // ★P5：脏配置防御。qishi/changdu 解析不了时跳过本块并只记一次日志，
                                        // 避免轮询每圈抛异常刷屏、且该数据块永远不触发。
                                        int blkQishi;
                                        int blkChangdu;
                                        if (!int.TryParse(par.Value[1], out blkQishi)
                                            || !int.TryParse(par.Value[2], out blkChangdu)
                                            || blkChangdu <= 0)
                                        {
                                            if (CommGridHelper.ShouldLogDirtyBlockOnce(par.Key))
                                                Log("数据块 " + par.Key + " 的起始地址/长度非法，已跳过（请检查配置）");
                                            continue;
                                        }
                                        shuju_temp = "";
                                        bool blockReadFailed = false; // ★R11：本块任一次读失败即置真，失败文案不得当“值”
                                        for (int j = 0; j < blkChangdu; j++)
                                        {
                                            if (par.Value[4] == "int")
                                            {

                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取short变量
                                                DemoUtils.ReadResultRender1(ReadLocked(() => omronFinsNet.ReadInt16("D" + (int.Parse(par.Value[1]) + j).ToString())), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "string")
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => omronFinsNet.ReadString("D" + (int.Parse(par.Value[1]) + j).ToString(), 1)), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "long" && j % 2 == 0)
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => omronFinsNet.ReadInt32("D" + (int.Parse(par.Value[1]) + j).ToString())), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "float" && j % 2 == 0)
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => omronFinsNet.ReadFloat("D" + (int.Parse(par.Value[1]) + j).ToString())), "D" + (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                if (CommTriggerHelper.IsReadFailureText(fins_temp)) blockReadFailed = true; // ★R11
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                        }
                                        if (par.Value[3] == "触发" && blockReadFailed)
                                        {
                                            // ★R11（第25轮）：本块任一寄存器读取失败时，shuju_temp 混有“Read Failed”等文案，
                                            //   原实现把它当“值”判定：反馈不等→每圈向 PLC 回写；条件误命中/锁存被 else 误复位→
                                            //   网络恢复后同值重复触发。失败轮整块跳过判定（锁存原样保留），读恢复后再判。
                                            int nowF = Environment.TickCount;
                                            if (nowF - _lastTrigReadFailLogTick > 5000)
                                            {
                                                _lastTrigReadFailLogTick = nowF;
                                                Log("数据块 " + par.Key + " 本轮读取失败，已跳过触发/切型判定与回写（防读失败文案被当作值）");
                                            }
                                            continue;
                                        }
                                        if (par.Value[3] == "触发")
                                        {
                                            // ★P1：相机绑定表同样用快照遍历，避免与 UI 改绑定并发时抛 "Collection was modified"
                                            Dictionary<int, string[]> camSnapshot = CommGridHelper.SnapshotCameraDic(camera_dic);
                                            if (camSnapshot == null) continue;
                                            foreach (var pap in camSnapshot)
                                            {
                                                if (pap.Value[0] == par.Value[0])
                                                {
                                                    // ★C4 修复：与主窗"使能"门控对称——未启用切型功能时轮询侧不置锁/不发事件，
                                                    //   避免"置了锁却因主窗使能条件不满足而不切换 → 该连接切型永久锁死"。
                                                    if (pap.Key == 13 && qiehuanzhong == 0 && pap.Value.Length > 2 && pap.Value[2] == "true")
                                                    {

                                                        if (qiehuan(shuju_temp.Replace("\0", "")) == 1)
                                                        {
                                                            if (File.Exists(lujing.Replace("\0", "")))
                                                            {
                                                                // ★P0 修复（连接 2~4 切型永久失效）：
                                                                //   原实现 ① 未给事件设置 LinkId（默认 0）→ 主界面误当连接 1 的切型处理；
                                                                //   ② 在此置本窗体锁 qiehuanzhong=1，而主界面 linkId>1 分支用 GetSwitchLock(linkId)
                                                                //      读的正是同一个字段 → 条件永远不满足，且此路径无人复位 → 该子连接切型永久失效。
                                                                //   修法：连接 1 保持原行为（门控仍走本窗体 qiehuanzhong，由主界面连接 1 分支复位）；
                                                                //        连接 2~4 改走 RaiseSchemeSwitch 携带 LinkId，门控统一由主界面
                                                                //        SetSwitchLock/GetSwitchLock 负责（切换中/同路径会被其拦截，不会重复切换）。
                                                                if (_linkId <= 1)
                                                                {
                                                                    qiehuanzhong = 1;
                                                                    SelectionChangedEventArgs E = new SelectionChangedEventArgs(shuju_temp, pap.Key.ToString()) { SchemePath = lujing };
                                                                    // ★G6：订阅者异常/无人订阅时原样上抛会打断轮询，且 qiehuanzhong 永久 1
                                                                    //   把该通道切型锁死。投递失败即复位标志（PLC 可重发），并记日志。
                                                                    try
                                                                    {
                                                                        var hSw = getData;
                                                                        if (hSw == null) qiehuanzhong = 0;
                                                                        else hSw(this, E);
                                                                    }
                                                                    catch (Exception exSw)
                                                                    {
                                                                        qiehuanzhong = 0;
                                                                        Log("切型事件派发异常(已复位切换标志): " + exSw.Message);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    RaiseSchemeSwitch(shuju_temp, lujing, _linkId);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // ★C7 修复：原实现只把失败返回值写进 [4]（值），没有置 [5]（待写槽）——
                                                                //   xie() 的遍历条件是 Value[5] != "无"，永不命中该槽 → 这枚"路径不存在"
                                                                //   NG 回执永远发不出去，PLC 死等。补置待写槽并触发写出。
                                                                if (!camera_dic[13][0].Contains("无"))
                                                                {
                                                                    // ★与心跳/成功回执的“设[4][5]+写出”互斥：防两组槽值交叠后发错通道
                                                                    lock (_ioSync)
                                                                    {
                                                                        camera_dic[13][4] = "999";
                                                                        camera_dic[13][5] = camera_dic[13][0];
                                                                        xie(camera_dic[13][4]);
                                                                    }
                                                                }
                                                                Log("方案路径:" + lujing + ":不存在!");
                                                            }
                                                        }
                                                    }
                                                    else if (pap.Key != 13)
                                                    {
                                                        if (!camSnapshot.ContainsKey(pap.Key)) continue;
                                                        string[] cam = camSnapshot[pap.Key];
                                                        string dataVal = shuju_temp.Replace("\0", "").Trim();
                                                        if (cam[2] == "true" && dataVal != cam[1])
                                                        {
                                                            cam[4] = cam[1];
                                                            WriteTriggerFanhuizhi(par.Value, cam[1], ref xuanzhong_temp, ref fins_temp);
                                                        }
                                                        CommTriggerHelper.GetCamTrigParams(cam, out string mode, out string tv1, out string tv2);
                                                        if (CommTriggerHelper.CheckTriggerCondition(dataVal, mode, tv1, tv2))
                                                        {
                                                            if (!_triggerLatch.TryGetValue(pap.Key, out bool latched) || !latched)
                                                            {
                                                                // ★G6：触发事件"派发成功才置锁存"——无人订阅/订阅者抛异常时
                                                                //   原实现先把锁存置 true 再发事件：事件其实没送达，该路同值却永久不再触发；
                                                                //   且异常上抛打断本轮轮询。改为守护投递 + 成功后置锁存，失败下轮可重试。
                                                                bool trigOk = false;
                                                                try
                                                                {
                                                                    var hTrig = getData;
                                                                    if (hTrig != null)
                                                                    {
                                                                        hTrig(this, new SelectionChangedEventArgs(dataVal, pap.Key.ToString()));
                                                                        trigOk = true;
                                                                    }
                                                                }
                                                                catch (Exception exTrig) { Log("相机" + pap.Key + " 触发事件派发异常: " + exTrig.Message); }
                                                                if (trigOk) _triggerLatch[pap.Key] = true;
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
                                        ref _commFailCount, fins_en, _reconnecting, ref _lastReconnectAttemptTicks))
                                    {
                                        TryAutoReconnect();
                                    }
                                }
                                else if (fins_lunxunen)
                                {
                                    _commFailCount = 0;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // ★P3-2：Fins_duxie 轮询外层 catch。PLC 断线时每圈（~20ms）抛一次，原样 Log 会数秒刷上百条相同日志。
                        //   改限流：5s 窗口内只报首条 + 窗口到期补报累计次数，辨识 tag 含协议/连接号/方法。
                        RateLimitedLog.Throttled("[FINS-连接" + _linkId + "-Fins_duxie] 轮询读异常", m => MsgErroeLog.WriteLog(m), ex, 5000);
                    }
            }
        }

        private void TryAutoReconnect()
        {
            if (!fins_en || IsDisposed) return;
            if (System.Threading.Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0) return;
            Log("欧姆龙Fins 通讯异常，后台自动重连中...");
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    bool ok;
                    lock (_ioSync)   // ★全 I/O 单锁串行：重连的 ConnectClose/ConnectServer 与读/写互斥，杜绝抢同一 socket
                    {
                        ok = PerformReconnectCore();
                    }
                    if (ok)
                    {
                        Log("欧姆龙Fins 自动重连成功");
                        SafeApplyConnectedUiState();
                    }
                }
                catch (Exception ex)
                {
                    Log("欧姆龙Fins 自动重连失败:" + ex.Message);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref _reconnecting, 0);
                }
            });
        }

        private bool PerformReconnectCore()
        {
            try { omronFinsNet.ConnectClose(); } catch { }
            try
            {
                return omronFinsNet.ConnectServer().IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        private void SafeApplyConnectedUiState()
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed) return;
                    button2.Enabled = true;
                    button1.Enabled = false;
                    panel2.Enabled = true;
                    if (userControlCurve1 != null)
                        userControlCurve1.ReadWriteNet = omronFinsNet;
                }));
            }
            catch { }
        }

        public void WriteCameraOutput(int camIdx, string value)
        {
            WriteCameraOutput(camIdx, value, false);
        }

        public void WriteCameraOutput(int camIdx, string value, bool detectionFailed)
        {
            if (!fins_en || !chushihua || camIdx < 0 || camIdx >= 12) return;
            int key = camIdx + 1;
            // ★ 修复回写错位（2026-09-06）：置位与写入必须全程持 _ioSync。
            // 原实现置位在锁外，而 xie() 用「本次调用的 value」写「所有挂起槽」并清槽；
            // 多相机并发时 A 的结果会写进 B 的寄存器、B 的结果被清槽后永不发出。
            // 置位与 xie 原子化后任一时刻至多一个槽挂起，value 与槽必然匹配。
            lock (_ioSync)
            {
                if (camera_dic.ContainsKey(key))
                {
                    if (detectionFailed)
                    {
                        value = "Reject";
                        var blocks = CommGridHelper.SnapshotFinsDic(fins_dic);
                        if (blocks != null)
                            foreach (var block in blocks.Values)
                                if (block[0] == camera_dic[key][3])
                                {
                                    int failRegs;
                                    if (!int.TryParse(block[2], out failRegs) || failRegs <= 0)
                                        InspectionFailureOutput.NotifyRegsUnset();   // ★M3：字数未配置/非法会静默退化回单寄存器写，告警一次
                                    value = InspectionFailureOutput.RepeatForRegisters(block[4], failRegs);
                                    break;
                                }
                    }
                    camera_dic[key][4] = value;
                    camera_dic[key][5] = camera_dic[key][3];
                }
                xie(value);   // lock 可重入
            }
        }

        // ===================== ★心跳（相机13“心跳”行，2026-09-20 现场定）=====================
        // 规则：心跳勾上 + 反馈通道已配置 + 心跳值已配置 → 每 1 秒把心跳值写到反馈通道
        //   （camera_dic[13][3] 绑定的数据块）。
        // 存储独立：心跳值/使能存 _heartbeatValue/_heartbeatEnabled + ini 键 xintiao/xintiaoen，
        //   与“方案切换”的 camera_dic[13][1]/[2]（fanhuizhi/fanhuien）彻底分离——原实现两者共用槽位，
        //   改任一处互相覆盖，且 [2] 兼作切型使能（主界面判 camera_dic[13][2]），取消心跳勾会连带禁用切型。
        private string _heartbeatValue = "";
        private bool _heartbeatEnabled = false;
        private System.Windows.Forms.Timer _heartbeatTimer;
        private int _hbBusy = 0;   // ★N1：心跳写出进行中标志（0=空闲 1=进行中），PLC 慢时防任务堆积

        /// <summary>创建并启动心跳定时器（UI 线程调用；1s 周期）。</summary>
        private void StartHeartbeatTimer()
        {
            // ★N2：断开连接停表后，重新连上可再次启动（Timer 已 Dispose 时重建）
            if (_heartbeatTimer != null) { _heartbeatTimer.Start(); return; }
            _heartbeatTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _heartbeatTimer.Tick += (s, ev) => HeartbeatTick();
            _heartbeatTimer.Start();
        }

        /// <summary>★N2：停止并释放心跳定时器（释放实例/断开连接时调用，防窗体被 Tick 闭包钉住泄漏、防对已关链路空写）。</summary>
        private void StopHeartbeatTimer()
        {
            try
            {
                if (_heartbeatTimer != null)
                {
                    _heartbeatTimer.Stop();
                    _heartbeatTimer.Dispose();
                    _heartbeatTimer = null;
                }
            }
            catch { }
        }

        /// <summary>★N4：反馈通道同块告警——本连接内 [3] 与其它行（含相机13 心跳）绑同一数据块时软提示（不拦截）。</summary>
        private void WarnSameFeedbackBlock(int cam, string val)
        {
            try
            {
                if (string.IsNullOrEmpty(val) || val.Contains("无")) return;
                for (int c = 1; c <= 13; c++)
                {
                    if (c == cam || !camera_dic.ContainsKey(c)) continue;
                    if (camera_dic[c].Length > 3 && camera_dic[c][3] == val)
                    {
                        MessageBox.Show(this,
                            "本连接内已有行绑定同一反馈数据块“" + val + "”（相机 " + c + "）。" + Environment.NewLine +
                            "两路结果会写入同一寄存器互相覆盖" + ((c == 13 || cam == 13) ? "；心跳每秒写该块，会持续覆盖相机结果值" : "") + "。" + Environment.NewLine +
                            "建议改用其它数据块。", "反馈通道重复绑定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }
                }
            }
            catch { }
        }

        /// <summary>心跳：勾上 + 未重连 + 反馈/心跳值已配置 → 把心跳值写入反馈通道（后台线程）。</summary>
        private void HeartbeatTick()
        {
            try
            {
                if (!chushihua) return;
                if (!_heartbeatEnabled) return;    // 心跳勾未勾
                if (!fins_en) return;              // 通讯未使能
                if (_reconnecting != 0) return;    // 重连中不发（xie 内亦会清槽兜底）
                if (fins_dic.Count == 0) return;
                string chan = camera_dic[13][3];
                if (string.IsNullOrEmpty(chan) || chan.Contains("无")) return;   // 反馈未配置
                string val = (_heartbeatValue ?? "").Trim();
                if (val.Length == 0) return;                                     // 心跳值未配置
                // ★N1 修复（2026-09-20）：写出改投后台线程——原来在 UI 线程 Timer.Tick 里同步写，
                //   PLC 半死时（写阻塞数秒）叠加轮询线程持 _ioSync，主界面会反复冻结。
                if (System.Threading.Interlocked.CompareExchange(ref _hbBusy, 1, 0) != 0) return;  // 上轮未写完则跳过本轮
                // ★低危加固（2026-09-20）：入队也包 try——QueueUserWorkItem 极端失败（OOM/线程池拒绝）时
                //   若不复位 _hbBusy，心跳会被进行中标志永久卡住、不再发送。
                try
                {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        lock (_ioSync)   // ★与成功/失败回执的“设[4][5]+写出”互斥：防两组槽值交叠后发错通道
                        {
                            // ★第26轮#34：门控原先只在 UI 线程 Tick 判一次——入队到此刻之间操作员可取消
                            //   心跳勾、点断开、或自动重连已启动，残余任务仍会把心跳值写进反馈槽并打向
                            //   已关链路（异常虽被吞，但反馈通道已被污染一帧）。持锁后按同一组条件复查。
                            if (!_heartbeatEnabled || !fins_en || _reconnecting != 0 || !chushihua
                                || _finsLink == null || !_finsLink.IsConnected) return;
                            camera_dic[13][4] = val;
                            camera_dic[13][5] = chan;
                            xie(val);
                        }
                    }
                    catch { }
                    finally { System.Threading.Interlocked.Exchange(ref _hbBusy, 0); }
                });
                }
                catch
                {
                    System.Threading.Interlocked.Exchange(ref _hbBusy, 0);   // 入队失败 → 复位进行中标志，防心跳永久停发
                }
            }
            catch { }
        }

        public void xie(string value)
        {
            lock (_ioSync)   // ★ I/O 串行：与 WriteTriggerFanhuizhi 等互斥，防同一 Hsl 客户端并发写
            {
            try
            {
                if (!chushihua || fins_dic.Count == 0) return;
                // ★BUG1：重连进行中不写，避免与 ConnectClose/ConnectServer 抢同一 socket（与轮询读/手动读共用 _reconnecting 门控）。
                if (_reconnecting != 0)
                {
                    // ★F8 修复（2026-09-20）：原实现直接 return 会"保留待写标记"——与 ebdfabc 的
                    //   "写失败一律清槽不补发"语义矛盾（重连恢复后会把陈旧结果补发给 PLC）。
                    //   现重连中同样视为"放弃本次回写"：清掉待写标记 + 记限流日志。
                    ClearAllPendingSlots();
                    LogXieRetry("重连中，本次回写已放弃（待写标记已清，不补发）");
                    return;
                }

                string fins_temp = "";
                // ★P1：xie 会被检测/回写路径调用，同样在遍历"活字典"；UI 清配置时会撞车导致本次回写丢失。
                Dictionary<int, string[]> camSnapshot = CommGridHelper.SnapshotCameraDic(camera_dic);
                Dictionary<string, string[]> finsSnapshot = CommGridHelper.SnapshotFinsDic(fins_dic);
                if (camSnapshot == null || finsSnapshot == null) return;
                foreach (var pat in camSnapshot)
                {
                    if (pat.Value[5] == "无") continue;
                    // ★H2 修复：每槽独立 try/catch。原实现整个 foreach 共用一个 try——
                    //   任一槽取值/解析抛异常（如值为"无"/"Reject"而格式为 int）会中断当次全部回写，
                    //   且该槽未走到清槽保持"待写"，下次调用在同一槽再抛 → 字典序靠后的回写被永久堵死。
                    try
                    {
                        bool writeAllOk = true;   // 本槽所有写调用是否成功
                        foreach (var par in finsSnapshot)
                        {
                            if (pat.Value[5] != par.Value[0]) continue;
                            int addr_start = int.Parse(par.Value[1]);
                            // ★低风险加固：格式串归一化（大小写/空白不敏感）——原精确比较下 "INT" 走不进任何分支（静默什么都不写）
                            string fmt = (par.Value[4] ?? "").Trim().ToLowerInvariant();
                            // ★H2：值非法不再抛——替换为失败值并保持原值个数（个数即现场约定的一块寄存器数）
                            string dataVal = EnsureWritableValue(pat.Value[4], fmt);

                            if (fmt == "int")
                            {
                                // 逗号分隔 → short数组, 批量写 (Omron FINS memory write)
                                string[] parts = dataVal.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                short[] vals = new short[parts.Length];
                                for (int i = 0; i < parts.Length; i++)
                                    vals[i] = (short)Math.Round(double.Parse(parts[i].Trim()));
                                writeAllOk = DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + addr_start.ToString(), vals), "D" + addr_start.ToString(), out fins_temp);
                                for (int j = 0; j < vals.Length; j++)
                                {
                                    int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                }
                            }
                            else if (fmt == "long")
                            {
                                string[] parts = dataVal.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                int[] vals = new int[parts.Length];
                                for (int i = 0; i < parts.Length; i++)
                                    vals[i] = (int)Math.Round(double.Parse(parts[i].Trim()));
                                writeAllOk = DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + addr_start.ToString(), vals), "D" + addr_start.ToString(), out fins_temp);
                                for (int j = 0; j < vals.Length * 2; j++)
                                {
                                    int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                }
                            }
                            else if (fmt == "float")
                            {
                                string[] parts = dataVal.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                float[] vals = new float[parts.Length];
                                for (int i = 0; i < parts.Length; i++)
                                    vals[i] = float.Parse(parts[i].Trim());
                                writeAllOk = DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + addr_start.ToString(), vals), "D" + addr_start.ToString(), out fins_temp);
                                for (int j = 0; j < vals.Length * 2; j++)
                                {
                                    int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                }
                            }
                            else if (fmt == "string")
                            {
                                string[] parts = dataVal.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int j = 0; j < parts.Length; j++)
                                {
                                    int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                    if (!DemoUtils.WriteResultRender1(() => omronFinsNet.Write("D" + (addr_start + j).ToString(), parts[j].Trim()), "D" + (addr_start + j).ToString(), out fins_temp))
                                        writeAllOk = false;
                                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                }
                            }
                            break;
                        }
                        // ★H3 决策（2026-09-20 现场定）：写失败不再补发——无论成败一律清槽，PLC 靠超时判 NG。
                        //   原"失败保留待写、下次 xie() 补发"会使 PLC 已判 NG 后又收到迟到的旧 OK（语义矛盾），
                        //   且空闲期无主动 flush、补发时机不可控；宁可不发，由 PLC 超时兜底。
                        if (!writeAllOk)
                            LogXieRetry("D 槽 " + pat.Value[5] + " → " + fins_temp);   // 失败诊断日志（5s 限流），随后清槽
                        pat.Value[5] = "无";
                    }
                    catch (Exception exSlot)
                    {
                        // 地址非法等不可重试错误：清槽避免毒化堵死后续回写（H2），并记录
                        pat.Value[5] = "无";
                        try { Log("回写跳过(该槽配置异常，已清槽防堵死): " + exSlot.Message); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            }
        }

        /// <summary>★H2：把待写值规范成该格式可解析的串——非法值（"无"/"Reject" 配数值块等）
        /// 替换为失败值并保持原值个数（个数即现场约定的一块寄存器数）；合法则原样返回。</summary>
        private static string EnsureWritableValue(string value, string fmt)
        {
            string[] parts = (value ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) parts = new string[] { "" };
            bool ok = true;
            if (fmt == "int" || fmt == "long")
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    double d;
                    if (!double.TryParse(parts[i].Trim(), out d)) { ok = false; break; }
                }
            }
            else if (fmt == "float")
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    float f;
                    if (!float.TryParse(parts[i].Trim(), out f)) { ok = false; break; }
                }
            }
            if (ok) return value ?? "";
            string fv = InspectionFailureOutput.ForFormat(fmt);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++) { if (i > 0) sb.Append(','); sb.Append(fv); }
            return sb.ToString();
        }

        // ★H3/F8：回写失败/重连跳过一律清槽、不补发（PLC 靠超时判 NG）；日志按 5 秒限流避免刷屏
        private int _lastXieRetryLogTick;
        private void LogXieRetry(string detail)
        {
            int now = Environment.TickCount;
            if (unchecked(now - _lastXieRetryLogTick) < 5000) return;
            _lastXieRetryLogTick = now;
            try { Log("回写未发送(已清槽，不补发；PLC 靠超时判 NG)： " + detail); } catch { }
        }

        /// <summary>★F8：清空所有"待写标记"（重连中放弃本次回写时调用，与"一律清槽不补发"语义一致）。</summary>
        private void ClearAllPendingSlots()
        {
            try
            {
                Dictionary<int, string[]> snap = CommGridHelper.SnapshotCameraDic(camera_dic);
                if (snap == null) return;
                foreach (var kv in snap)
                {
                    try
                    {
                        string[] v = kv.Value;
                        if (v != null && v.Length > 5 && v[5] != "无") v[5] = "无";
                    }
                    catch { }
                }
            }
            catch { }
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            // 相机9-13 原"周期预置触发通道待写标记"已统一移除：
            // 相机1-12 业务同等，原逻辑会把任意相机的检测输出经 xie() 误写入 9-12 的触发通道
            // （与相机13切换通道同源）；移除后各相机仅由 WriteCameraOutput 回写各自反馈通道。
            // fins_xie 数组随之清理（原为只写不读的死代码）。
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "abcd", comboBox1.Text);
            }
        }

        private void comboBox5_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox5.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox5.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox5.Items.Add("（无绑定）");
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(1, comboBox5, 0);
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(1, comboBox6, 3);
        }

        private void comboBox6_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            { 
                comboBox6.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox6.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox6.Items.Add("（无绑定）");
        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[1][1] = textBox14.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 1), "fanhuizhi", textBox14.Text);
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox3.CheckState == CheckState.Checked)
                {
                    camera_dic[1][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 1), "fanhuien", "true");
                }
                else
                {
                    camera_dic[1][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 1), "fanhuien", "false");
                }
            }
        }

        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(2, comboBox8, 0);
        }

        private void comboBox10_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(3, comboBox10, 0);
        }

        private void comboBox12_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(4, comboBox12, 0);
        }

        private void comboBox14_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(5, comboBox14, 0);
        }

        private void comboBox16_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(6, comboBox16, 0);
        }

        private void comboBox18_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(7, comboBox18, 0);
        }

        private void comboBox20_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(8, comboBox20, 0);
        }

        private void comboBox8_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox8.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox8.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox8.Items.Add("（无绑定）");
        }

        private void comboBox10_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox10.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox10.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox10.Items.Add("（无绑定）");
        }

        private void comboBox12_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox12.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox12.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox12.Items.Add("（无绑定）");
        }

        private void comboBox14_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox14.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox14.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox14.Items.Add("（无绑定）");
        }

        private void comboBox16_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox16.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox16.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox16.Items.Add("（无绑定）");
        }

        private void comboBox18_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox18.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox18.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox18.Items.Add("（无绑定）");
        }

        private void comboBox20_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox20.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox20.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox20.Items.Add("（无绑定）");
        }

        private void textBox17_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[2][1] = textBox17.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 2), "fanhuizhi", textBox17.Text);
            }
        }

        private void textBox19_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[3][1] = textBox19.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 3), "fanhuizhi", textBox19.Text);
            }
        }

        private void textBox18_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[4][1] = textBox18.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 4), "fanhuizhi", textBox18.Text);
            }
        }

        private void textBox23_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[5][1] = textBox23.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 5), "fanhuizhi", textBox23.Text);
            }
        }

        private void textBox22_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[6][1] = textBox22.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 6), "fanhuizhi", textBox22.Text);
            }
        }

        private void textBox21_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[7][1] = textBox21.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 7), "fanhuizhi", textBox21.Text);
            }
        }

        private void textBox20_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[8][1] = textBox20.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 8), "fanhuizhi", textBox20.Text);
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox4.CheckState == CheckState.Checked)
                {
                    camera_dic[2][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 2), "fanhuien", "true");
                }
                else
                {
                    camera_dic[2][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 2), "fanhuien", "false");
                }
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox5.CheckState == CheckState.Checked)
                {
                    camera_dic[3][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 3), "fanhuien", "true");
                }
                else
                {
                    camera_dic[3][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 3), "fanhuien", "false");
                }
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox6.CheckState == CheckState.Checked)
                {
                    camera_dic[4][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 4), "fanhuien", "true");
                }
                else
                {
                    camera_dic[4][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 4), "fanhuien", "false");
                }
            }
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox10.CheckState == CheckState.Checked)
                {
                    camera_dic[5][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 5), "fanhuien", "true");
                }
                else
                {
                    camera_dic[5][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 5), "fanhuien", "false");
                }
            }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox9.CheckState == CheckState.Checked)
                {
                    camera_dic[6][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 6), "fanhuien", "true");
                }
                else
                {
                    camera_dic[6][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 6), "fanhuien", "false");
                }
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox8.CheckState == CheckState.Checked)
                {
                    camera_dic[7][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 7), "fanhuien", "true");
                }
                else
                {
                    camera_dic[7][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 7), "fanhuien", "false");
                }
            }
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox7.CheckState == CheckState.Checked)
                {
                    camera_dic[8][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 8), "fanhuien", "true");
                }
                else
                {
                    camera_dic[8][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 8), "fanhuien", "false");
                }
            }
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(2, comboBox7, 3);
        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(3, comboBox9, 3);
        }

        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(4, comboBox11, 3);
        }

        private void comboBox13_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(5, comboBox13, 3);
        }

        private void comboBox15_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(6, comboBox15, 3);
        }

        private void comboBox17_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(7, comboBox17, 3);
        }

        private void comboBox19_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(8, comboBox19, 3);
        }

        private void comboBox7_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox7.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox7.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox7.Items.Add("（无绑定）");
        }

        private void comboBox9_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox9.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox9.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox9.Items.Add("（无绑定）");
        }

        private void comboBox11_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox11.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox11.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox11.Items.Add("（无绑定）");
        }

        private void comboBox13_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox13.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox13.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox13.Items.Add("（无绑定）");
        }

        private void comboBox15_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox15.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox15.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox15.Items.Add("（无绑定）");
        }

        private void comboBox17_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox17.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox17.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox17.Items.Add("（无绑定）");
        }

        private void comboBox19_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox19.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox19.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox19.Items.Add("（无绑定）");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            comboBox4.Text = "";
            comboBox5.Text = "";
            comboBox6.Text = "";
            comboBox7.Text = "";
            comboBox8.Text = "";
            comboBox9.Text = "";
            comboBox10.Text = "";
            comboBox11.Text = "";
            comboBox12.Text = "";
            comboBox13.Text = "";
            comboBox14.Text = "";
            comboBox15.Text = "";
            comboBox16.Text = "";
            comboBox17.Text = "";
            comboBox18.Text = "";
            comboBox19.Text = "";
            comboBox20.Text = "";
            comboBox21.Text = "";
            comboBox22.Text = "";
            comboBox23.Text = "";
            comboBox24.Text = "";
            comboBox25.Text = "";
            comboBox26.Text = "";
            comboBox27.Text = "";
            comboBox28.Text = "";
            textBox14.Text = "";
            textBox17.Text = "";
            textBox19.Text = "";
            textBox18.Text = "";
            textBox23.Text = "";
            textBox22.Text = "";
            textBox21.Text = "";
            textBox20.Text = "";
            textBox24.Text = "";
            textBox41.Text = "";
            textBox42.Text = "";
            textBox43.Text = "";
            textBox44.Text = "";
            textBox53.Text = "";
            checkBox3.CheckState = CheckState.Unchecked;
            checkBox4.CheckState = CheckState.Unchecked;
            checkBox5.CheckState = CheckState.Unchecked;
            checkBox6.CheckState = CheckState.Unchecked;
            checkBox7.CheckState = CheckState.Unchecked;
            checkBox8.CheckState = CheckState.Unchecked;
            checkBox9.CheckState = CheckState.Unchecked;
            checkBox10.CheckState = CheckState.Unchecked;
            checkBox11.CheckState = CheckState.Unchecked;
            checkBox12.CheckState = CheckState.Unchecked;
            checkBox13.CheckState = CheckState.Unchecked;
            checkBox14.CheckState = CheckState.Unchecked;
            checkBox15.CheckState = CheckState.Unchecked;
            checkBox16.CheckState = CheckState.Unchecked;
            comboBox4_SelectedIndexChanged(null, null);
            comboBox5_SelectedIndexChanged(null, null);
            comboBox6_SelectedIndexChanged(null, null);
            comboBox7_SelectedIndexChanged(null, null);
            comboBox8_SelectedIndexChanged(null, null);
            comboBox9_SelectedIndexChanged(null, null);
            comboBox10_SelectedIndexChanged(null, null);
            comboBox11_SelectedIndexChanged(null, null);
            comboBox12_SelectedIndexChanged(null, null);
            comboBox13_SelectedIndexChanged(null, null);
            comboBox14_SelectedIndexChanged(null, null);
            comboBox15_SelectedIndexChanged(null, null);
            comboBox16_SelectedIndexChanged(null, null);
            comboBox17_SelectedIndexChanged(null, null);
            comboBox18_SelectedIndexChanged(null, null);
            comboBox19_SelectedIndexChanged(null, null);
            comboBox20_SelectedIndexChanged(null, null);
            comboBox21_SelectedIndexChanged(null, null);
            comboBox22_SelectedIndexChanged(null, null);
            comboBox23_SelectedIndexChanged(null, null);
            comboBox24_SelectedIndexChanged(null, null);
            comboBox25_SelectedIndexChanged(null, null);
            comboBox26_SelectedIndexChanged(null, null);
            comboBox27_SelectedIndexChanged(null, null);
            comboBox29.Text = "";
            comboBox29_SelectedIndexChanged(null, null);
            textBox53_TextChanged(null, null);
            checkBox16_CheckedChanged(null, null);

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "ip", textBox1.Text);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "port", textBox2.Text);
            }
        }

        private void textBox16_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "cell", textBox16.Text);
            }
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(FinsIniStore.ConnSection(_linkId), "local", textBox15.Text);
            }
        }

        private void userControlHead1_Load(object sender, EventArgs e)
        {

        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(9, comboBox4, 0);
        }

        private void comboBox4_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox4.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        // ★对齐修复：相机9 触发列原过滤“心跳”类型数据块（与相机1-8 的“触发”过滤及
                        //   ModbusRTU 版 cb12 不一致），导致相机9 触发下拉列出的是心跳类块。
                        //   现场确认相机9-12 必须与相机1-8 对齐：触发列只列“触发”类型。
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox4.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox4.Items.Add("（无绑定）");
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox11.CheckState == CheckState.Checked)
                {
                    camera_dic[9][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 9), "fanhuien", "true");
                }
                else
                {
                    camera_dic[9][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 9), "fanhuien", "false");
                }
            }
        }

        private void textBox24_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[9][1] = textBox24.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 9), "fanhuizhi", textBox24.Text);
            }
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox21_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox21.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox21.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
                        comboBox21.Items.Add("（无绑定）");
        }

        private void comboBox21_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(13, comboBox21, 0);
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox12.CheckState == CheckState.Checked)
                {
                    camera_dic[13][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 13), "fanhuien", "true");
                }
                else
                {
                    camera_dic[13][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 13), "fanhuien", "false");
                }
            }
        }

        private void textBox25_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox25.Text = openFileDialog.FileName;
            }
        }

        private void textBox26_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox26.Text = openFileDialog.FileName;
            }
        }

        private void textBox28_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox28.Text = openFileDialog.FileName;
            }
        }

        private void textBox27_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox27.Text = openFileDialog.FileName;
            }
        }

        private void textBox32_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox32.Text = openFileDialog.FileName;
            }
        }

        private void textBox31_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox31.Text = openFileDialog.FileName;
            }
        }

        private void textBox30_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox30.Text = openFileDialog.FileName;
            }
        }

        private void textBox29_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox29.Text = openFileDialog.FileName;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "1", textBox40.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "2", textBox39.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "3", textBox38.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "4", textBox37.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "5", textBox36.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "6", textBox35.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "7", textBox34.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "8", textBox33.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "9", textBox45.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "10", textBox46.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "11", textBox47.Text);
            wdini.WriteString(FinsIniStore.ChangeSection(_linkId), "12", textBox48.Text);
          
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "1", textBox25.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "2", textBox26.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "3", textBox28.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "4", textBox27.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "5", textBox32.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "6", textBox31.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "7", textBox30.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "8", textBox29.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "9", textBox49.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "10", textBox50.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "11", textBox51.Text);
            wdini.WriteString(FinsIniStore.PathSection(_linkId), "12", textBox52.Text);
        }
        private int qiehuan(string aa)
        {
            // ★ 后台轮询线程判型 → 迁回 UI 线程读取控件文本，避免轮询线程与 UI 线程并发访问控件
            if (InvokeRequired)
            {
                try
                {
                    if (IsDisposed || !IsHandleCreated) return 0;
                    return (int)Invoke(new Func<string, int>(qiehuan), aa);
                }
                catch
                {
                    return 0;
                }
            }
            if (textBox40.Text == aa)
            {
                zifu = aa;
                lujing = textBox25.Text;
                return 1;
            }
            else if (textBox39.Text == aa)
            {
                zifu = aa;
                lujing = textBox26.Text;
                return 1;
            }
            else if (textBox38.Text == aa)
            {
                zifu = aa;
                lujing = textBox28.Text;
                return 1;
            }
            else if (textBox37.Text == aa)
            {
                zifu = aa;
                lujing = textBox27.Text;
                return 1;
            }
            else if (textBox36.Text == aa)
            {
                zifu = aa;
                lujing = textBox32.Text;
                return 1;
            }
            else if (textBox35.Text == aa)
            {
                zifu = aa;
                lujing = textBox31.Text;
                return 1;
            }
            else if (textBox34.Text == aa)
            {
                zifu = aa;
                lujing = textBox30.Text;
                return 1;
            }
            else if (textBox33.Text == aa)
            {
                zifu = aa;
                lujing = textBox29.Text;
                return 1;
            }
            else if (textBox45.Text == aa)
            {
                zifu = aa;
                lujing = textBox49.Text;
                return 1;
            }
            else if (textBox46.Text == aa)
            {
                zifu = aa;
                lujing = textBox50.Text;
                return 1;
            }
            else if (textBox47.Text == aa)
            {
                zifu = aa;
                lujing = textBox51.Text;
                return 1;
            }
            else if (textBox48.Text == aa)
            {
                zifu = aa;
                lujing = textBox52.Text;
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private void textBox41_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[13][1] = textBox41.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 13), "fanhuizhi", textBox41.Text);
            }
        }

        // --- camera_dic[10] events ---
        private void comboBox22_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(10, comboBox22, 0);
        }

        private void comboBox22_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox22.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox22.Items.Add(par.Value[0]);
                        }
                    }
                }
            }
                        comboBox22.Items.Add("（无绑定）");
        }

        private void comboBox23_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(10, comboBox23, 3);
        }

        private void comboBox23_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox23.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox23.Items.Add(par.Value[0]);
                        }
                    }
                }
            }
                        comboBox23.Items.Add("（无绑定）");
        }

        private void textBox42_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[10][1] = textBox42.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 10), "fanhuizhi", textBox42.Text);
            }
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox13.CheckState == CheckState.Checked)
                {
                    camera_dic[10][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 10), "fanhuien", "true");
                }
                else
                {
                    camera_dic[10][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 10), "fanhuien", "false");
                }
            }
        }

        // --- camera_dic[11] events ---
        private void comboBox24_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(11, comboBox24, 0);
        }

        private void comboBox24_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox24.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox24.Items.Add(par.Value[0]);
                        }
                    }
                }
            }
                        comboBox24.Items.Add("（无绑定）");
        }

        private void comboBox25_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(11, comboBox25, 3);
        }

        private void comboBox25_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox25.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox25.Items.Add(par.Value[0]);
                        }
                    }
                }
            }
                        comboBox25.Items.Add("（无绑定）");
        }

        private void textBox43_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[11][1] = textBox43.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 11), "fanhuizhi", textBox43.Text);
            }
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox14.CheckState == CheckState.Checked)
                {
                    camera_dic[11][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 11), "fanhuien", "true");
                }
                else
                {
                    camera_dic[11][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 11), "fanhuien", "false");
                }
            }
        }

        // --- camera_dic[12] events ---
        private void comboBox26_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(12, comboBox26, 0);
        }

        private void comboBox26_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox26.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            comboBox26.Items.Add(par.Value[0]);
                        }
                    }
                }
            }
                        comboBox26.Items.Add("（无绑定）");
        }

        private void comboBox27_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(12, comboBox27, 3);
        }

        private void comboBox27_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox27.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox27.Items.Add(par.Value[0]);
                        }
                    }
                }
            }
                        comboBox27.Items.Add("（无绑定）");
        }

        private void comboBox28_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(9, comboBox28, 3);
        }

        private void comboBox28_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox28.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox28.Items.Add(par.Value[0]);
                        }
                    }
                }
            }
                        comboBox28.Items.Add("（无绑定）");
        }

        private void textBox44_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[12][1] = textBox44.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 12), "fanhuizhi", textBox44.Text);
            }
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox15.CheckState == CheckState.Checked)
                {
                    camera_dic[12][2] = "true";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 12), "fanhuien", "true");
                }
                else
                {
                    camera_dic[12][2] = "false";
                    wdini.WriteString(FinsIniStore.CameraSection(_linkId, 12), "fanhuien", "false");
                }
            }
        }

        // --- path 9~12 DoubleClick ---
        private void textBox49_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox49.Text = openFileDialog.FileName;
            }
        }

        private void textBox50_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox50.Text = openFileDialog.FileName;
            }
        }

        private void textBox51_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox51.Text = openFileDialog.FileName;
            }
        }

        private void textBox52_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox52.Text = openFileDialog.FileName;
            }
        }

        // --- camera_dic[13] 心跳 events ---
        // ★心跳独立存储（2026-09-20）：心跳值存 _heartbeatValue + ini 键 xintiao（不再写
        //   camera_dic[13][1]/fanhuizhi，后者专用于“方案切换”返回值，两者原共用会互相覆盖）。
        private void textBox53_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                _heartbeatValue = textBox53.Text;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 13), "xintiao", textBox53.Text);
            }
        }

        // ★心跳独立存储（2026-09-20）：心跳使能存 _heartbeatEnabled + ini 键 xintiaoen（不再写
        //   camera_dic[13][2]/fanhuien，后者兼作切型使能——原共用导致取消心跳勾连切型一起禁用）。
        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                bool on = checkBox16.CheckState == CheckState.Checked;
                _heartbeatEnabled = on;
                wdini.WriteString(FinsIniStore.CameraSection(_linkId, 13), "xintiaoen", on ? "true" : "false");
            }
        }

        private void comboBox29_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox29.Items.Clear();
                foreach (var pap in fins_dic)
                {
                    // ★N10：口径统一——与 Modbus/RTU 版一致用 Contains（原精确 == 与另两版不齐）
                    if (pap.Value[3].Contains("反馈"))
                        comboBox29.Items.Add(pap.Key);
                }
            }
                        comboBox29.Items.Add("（无绑定）");
        }

        private void comboBox29_SelectedIndexChanged(object sender, EventArgs e)
        {
            TryBindCamera(13, comboBox29, 3);
        }

        // ===================== 多连接管理入口（阶段 4，纯增量） =====================

        /// <summary>按配置新建一条 FINS 运行时（与 FormSiemens_Load 中逐条创建逻辑一致）。</summary>
        private FinsRuntime CreateLinkRuntime(FinsLinkConfig cfg)
        {
            if (cfg.LinkId == 1)
                return new FinsRuntime(_finsLink, this) { LinkId = 1, Config = cfg, Context = null };

            var link = new FinsLink();
            link.LinkId = cfg.LinkId;
            link.Name = cfg.Name;
            link.Ip = cfg.Ip;
            link.Port = int.TryParse(cfg.Port, out int p) ? p : 9600;
            link.SA1 = byte.TryParse(cfg.Local, out byte sa) ? sa : (byte)0;
            link.DA2 = byte.TryParse(cfg.Cell, out byte da) ? da : (byte)192;
            if (cfg.FinsEn) link.Connect(null);
            // 阶段 5：把本连接的 FinsLink 交给上下文，使反馈回写 / 自动重连作用在“本连接”而不是连接 1。
            var ctx = new FinsLinkContext(cfg, link, this);
            ctx.SchemeSwitchRaise = RaiseSchemeSwitch;
            return new FinsRuntime(link, this) { LinkId = cfg.LinkId, Config = cfg, Context = ctx };
        }

        /// <summary>
        /// 重载第 2~4 路 FINS 运行时（连接 1 完全不动）。
        /// 由“连接设备”管理器在保存/删除后调用，使新增或修改的连接不重启软件即生效。
        /// </summary>
        public void ReloadFinsLinks()
        {
            // 阶段 5（独立实例模型）：连接 2~4 各自是独立的 FormOmron 实例，运行时由其自身管理，
            // 主连接(链接1)不再代为持有/重建其它连接的运行时。保留为兼容占位，仅清理历史上可能残留的运行时。
            var mgr = AppHost.Services.Resolve<CommLinkManager>();
            foreach (int id in _runtimes.Keys.Where(k => k >= 2).ToList())
            {
                try { _runtimes[id].Close(); } catch { }
                _runtimes.Remove(id);
                try { if (mgr != null) mgr.Unregister(id); } catch { }
            }
        }

    }
}

