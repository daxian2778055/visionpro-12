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
using HslCommunication;
using HslCommunication.ModBus;
using System.Threading;
using System.IO.Ports;
using demo;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using WindowsFormsApplication1.Core.Infrastructure;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1
{
    public partial class FormModbusRtu : Form, IPlcOutputWriter, ICommRuntimeHost, ICommLinkContext
    {
        public Label labelFallbackHint = new Label();

        // 多实例整窗化（阶段 7，仿 FormOmron）：每个实例 = 一个连接的完整调试窗体。
        private int _linkId = 1;
        public int LinkId { get { return _linkId; } }

        // 数据块/相机段族的段名后缀（连接 1 = "modbusrtu"，连接 N = "modbusrtuN"，RTU 段名不带下划线），供 CommGridHelper 段级操作使用。
        private string DataSuffix { get { return "modbusrtu" + (_linkId == 1 ? "" : _linkId.ToString()); } }

        public FormModbusRtu() : this(1) { }

        public FormModbusRtu(int linkId)
        {
            _linkId = linkId;
            wdini.ReadINIFile(AppDomain.CurrentDomain.BaseDirectory + "//test.ini");
            InitializeComponent( );
            // COM 互斥防呆：让底层连接对象携带正确的连接号，占用提示才能区分“连接 N”
            _rtuLink.LinkId = _linkId;

            // 在 tabPage2 (数据绑定页面) label39 "1拖多需重启软件" 下方添加提示 label（默认隐藏）
            labelFallbackHint.AutoSize = true;
            labelFallbackHint.TextAlign = ContentAlignment.MiddleLeft;
            labelFallbackHint.ForeColor = Color.Red;
            labelFallbackHint.Text = "写操作已降级为逐地址写入(FC06)";
            labelFallbackHint.Visible = false;
            labelFallbackHint.Location = new Point(label39.Left, label39.Bottom + 8);
            tabPage2.Controls.Add(labelFallbackHint);
        }
        private ClassIni wdini = new ClassIni();
        public delegate void GetSeletionData(object Sender, SelectionChangedEventArgs e);
        public event GetSeletionData getData;
        private int x = 999;
        private int y = 999;
        Dictionary<string, string[]> fins_dic = new Dictionary<string, string[]>();
        public Dictionary<int, string[]> camera_dic = new Dictionary<int, string[]>();
        Dictionary<int, int[]> fins_name = new Dictionary<int, int[]>();
        Dictionary<int, int[]> fins_data = new Dictionary<int, int[]>();
        Dictionary<int, byte[]> fins_value = new Dictionary<int, byte[]>();
        Dictionary<string, string> fins_zuhe = new Dictionary<string, string>();
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
        ErrorLog MsgErroeLog = new ErrorLog();
        bool fins_lunxunen = false;
        // ★BUG2：与 chushihua 同款。UI 线程写、轮询线程读，需 volatile 保证跨线程可见性。
        public volatile bool fins_en = false;
        decimal lunxun_time = 0;
        // ★P2：线程池线程（InitializeForm 末尾 Task.Run）写 true，轮询线程（Fins_duxie）读；加 volatile 保证跨线程可见性
        public volatile bool chushihua = false;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _triggerLatch = new System.Collections.Concurrent.ConcurrentDictionary<int, bool>();
        private int _commFailCount = 0;
        private int _reconnecting = 0;
        private long _lastReconnectAttemptTicks = 0;
        public class SelectionChangedEventArgs : EventArgs
        {

            private string m_selection;

            private string m_camere;
            private string m_all;
            private int m_linkId;

            //本属性用于传递事件数据

            public string Selection
            {

                get { return m_selection; }

            }
            public string Camera
            {

                get { return m_camere; }

            }
            public string all
            {

                get { return m_all; }

            }
            /// <summary>
            /// 事件来源的连接标识（多实例改造 阶段7）。
            /// 用于区分“这条触发数据是哪个连接收来的”，后续据此路由与回写反馈。
            /// 连接 1 主窗体事件恒为 1，行为与改造前完全一致。
            /// </summary>
            public int LinkId
            {

                get { return m_linkId; }

            }
            public SelectionChangedEventArgs(string selection, string camera, string all)
                : this(selection, camera, all, 1)
            {

            }
            public SelectionChangedEventArgs(string selection, string camera, string all, int linkId)
            {

                m_selection = selection;
                m_camere = camera;
                m_all = all;
                m_linkId = linkId;
            }
        }

        // 多实例改造 阶段 4-B/4-C：busRtuClient 代理到 _rtuLink.Client，使原 Fins_duxie/xie 等引用零改动且始终指向当前连接对象。
        private ModbusRtuLink _rtuLink = new ModbusRtuLink();
        private ModbusRtu busRtuClient { get { return _rtuLink.Client; } set { _rtuLink.Client = value; } }

        // 多实例改造 阶段 4-C：运行时集合与中央登记（链接 1 用 _rtuLink + Context=null 走原 Fins_duxie）。
        private ModbusRtuRuntime _runtime;
        private Dictionary<int, ModbusRtuRuntime> _runtimes = new Dictionary<int, ModbusRtuRuntime>();

        // 多连接 child host（阶段 7，仿 FormOmron/FormModbus）：连接 1 主窗体登记各连接整窗实例，对外 API 按 linkId 路由到对应 child。
        // ★A4 修复：该字典会被 UI 线程（注册/注销）与轮询线程（路由查询）并发访问，必须加锁（对齐 FormOmron）。
        private readonly Dictionary<int, FormModbusRtu> _childLinks = new Dictionary<int, FormModbusRtu>();
        private readonly object _childLinksSync = new object();

        internal void RegisterChildLink(int linkId, FormModbusRtu host)
        {
            if (host == null) return;
            lock (_childLinksSync) { _childLinks[linkId] = host; }
        }

        internal void UnregisterChildLink(int linkId)
        {
            lock (_childLinksSync) { _childLinks.Remove(linkId); }
        }

        // ---- 阶段 7 补强：多连接触发/切方案的按连接访问器（对齐 FormModbus / FormOmron，供 Form1 处理器按 linkId 路由） ----

        /// <summary>连接N普通触发参数：link&gt;1 取该连接自己 camera_dic 的相机绑定；取不到返回 false（调用方回退连接 1）。</summary>
        public bool TryGetLinkCamera(int linkId, int camNo, out string[] cam)
        {
            cam = null;
            var host = ResolveLinkHost(linkId);
            if (host == null) return false;
            return host.camera_dic.TryGetValue(camNo, out cam);
        }

        /// <summary>连接N切方案锁（每连接独立，各自 qiehuanzhong 互不干扰）。</summary>
        public int GetSwitchLock(int linkId)
        {
            var host = ResolveLinkHost(linkId);
            return host != null ? host.qiehuanzhong : 0;
        }

        public void SetSwitchLock(int linkId, int value)
        {
            var host = ResolveLinkHost(linkId);
            if (host != null) host.qiehuanzhong = value;
        }

        /// <summary>连接N的方案路径（对齐 FINS 的 e.SchemePath）：link&gt;1 取该连接窗体的 lujing。</summary>
        public string GetLinkSchemePath(int linkId)
        {
            var host = ResolveLinkHost(linkId);
            return host != null ? host.lujing : "";
        }

        /// <summary>该 ModbusRTU 协议是否有任一连接使能且已初始化（决定是否读取 VP 的 modbusrtu 输出值；
        /// 连接 1 未使能但连接 2~4 使能时仍须读取，否则连接 2~4 回写的是空值）。</summary>
        public bool CanWriteResultOutput()
        {
            if (fins_en && chushihua) return true;
            List<FormModbusRtu> children;
            lock (_childLinksSync) { children = new List<FormModbusRtu>(_childLinks.Values); }
            foreach (var child in children)
                if (child != null && child.fins_en && child.chushihua) return true;
            return false;
        }

        /// <summary>按 linkId 解析承载该连接的整窗实例；linkId==1 返回本窗体。</summary>
        private FormModbusRtu ResolveLinkHost(int linkId)
        {
            if (linkId == 1) return this;
            lock (_childLinksSync)
            {
                FormModbusRtu host;
                return _childLinks.TryGetValue(linkId, out host) ? host : null;
            }
        }

        // ===== ICommRuntimeHost 实现（阶段 4-B）：引擎通过它回调轮询循环、读写停止标志与线程句柄 =====
        public bool StopRequested { get { return _stopPolling; } set { _stopPolling = value; } }
        public Thread PollThread { get { return fins_duxie; } set { fins_duxie = value; } }
        public void RunPollLoop() { Fins_duxie(); }

        // ===== ICommLinkContext 实现（阶段 4-C per-link）：供 ModbusRtuRuntime.PollLoop 按连接读取配置与回调。
        // 当前唯一连接（链接 1）的运行时 Context 为 null，仍走原 Fins_duxie，本实现作为多连接并联样板。
        public bool IsInitialized { get { return chushihua; } }
        public int PollInterval { get { return (int)lunxun_time; } }
        public bool IsPollEnabled { get { return fins_lunxunen; } }
        public bool IsCommEnabled { get { return fins_en; } }
        public int AddressBase { get { return (int)address_qishi; } }
        public Dictionary<string, string[]> FinsBlocks { get { return fins_dic; } }
        public Dictionary<int, string[]> CameraBindings { get { return camera_dic; } }
        public string SchemePath { get { return lujing; } }
        public void Log(string message)
        {
            // 每条日志都带上协议名 + 具体连接号，多实例时能一眼看出来自哪个实例的通讯
            MsgErroeLog.WriteLog("[ModbusRTU-连接" + _linkId + "] " + message);
        }
        public void UpdatePollCell(int row, string value) { CommGridHelper.SetPollCell(_gridUi, fins_data, row, value); }
        public void RaiseSelectionChanged(string dataVal, string camKey, string third, int linkId)
        {
            // 注意：本方法由轮询线程调用，此处保持"同步派发"这一现网已验证行为。
            // 已评估过改为 BeginInvoke 投递到 UI 线程（可避免轮询线程被 UI 阻塞、订阅方在后台线程操作 UI），
            // 但当前无条件上机验证，故暂不启用；待现场可验证时再单独开启。
            if (getData != null) getData(this, new SelectionChangedEventArgs(dataVal, camKey, third, linkId));
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

        /// <summary>
        /// 相机归属防呆（Modbus-RTU 版）：同一协议内，一台相机（触发/反馈任一有值）只能归属一条连接。
        /// 占用时回滚下拉并提示；相机 13 反馈侧（心跳）每连接独立，由调用方不调用本方法实现。
        /// </summary>
        /// <param name="cameraNo">相机编号 1..13</param>
        /// <param name="newVal">下拉新选值</param>
        /// <param name="slot">camera_dic[cameraNo] 对应下标：0=触发(chufa)，3=反馈(fankui)</param>
        /// <param name="cb">触发该改动的下拉控件，用于回滚 Text</param>
        /// <returns>true=放行；false=被占用，已回滚并提示</returns>
        private bool GuardCamera(int cameraNo, string newVal, int slot, System.Windows.Forms.ComboBox cb)
        {
            if (fins_en) return false;   // 使能开时参数锁定，不容许修改相机绑定（防运行期热改 camera_dic 竞态）
            // 空值（清空）不占资源，其它连接即使占着本连接也可选择“无”
            if (!CommCameraGuard.IsBoundValue(newVal)) return true;
            int owner = CommCameraGuard.FindOwnerModbusRtu(wdini, _linkId, cameraNo);
            if (owner == 0)
            {
                // 跨协议相机绑定软提示（不硬拦）：另一协议已绑同物理相机时提示，仍放行保存
                if (cameraNo >= 1 && cameraNo <= 12)
                {
                    try { wdini.ReadINIFile(wdini.FileName); } catch { }
                    string xwarn = CommCameraGuard.CrossProtoWarning(wdini, CommCameraGuard.CommProto.ModbusRtu, _linkId, cameraNo);
                    if (!string.IsNullOrEmpty(xwarn))
                        System.Windows.Forms.MessageBox.Show(this, xwarn, "跨协议相机绑定提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return true;
            }
            string old = camera_dic[cameraNo][slot];
            cb.Text = old;             // 回滚到原值
            string ownerName = CommCameraGuard.OwnerDisplayNameModbusRtu(wdini, owner);
            System.Windows.Forms.MessageBox.Show(
                CommCameraGuard.OccupiedMsg("ModbusRTU", cameraNo, owner, ownerName),
                "相机占用冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }


        // 防止“管理器预建隐藏窗显式初始化”后，首次 Show 又触发 Load 重复建链。
        private bool _formLoaded = false;

        private void FormSiemens_Load( object sender, EventArgs e )
        {
            InitializeForm();
        }

        /// <summary>整窗初始化（与 FINS / 无协议同构）：读 [modbusrtu]/[modbusrtuN] 段族填充界面，
        /// 并创建/启动本连接运行时（窗体自身 _rtuLink + Context=null，与连接 1 完全同构）。
        /// 关键：WinForms 的 Load 只在首次 Show 时触发；管理器 Attach/添加时预建的隐藏窗（连接 2~4）
        /// 必须显式调用本方法，否则运行时永不创建、连接 2~4 完全不轮询。</summary>
        internal void InitializeForm()
        {
            if (_formLoaded) return;
            _formLoaded = true;
            panel2.Enabled = false;
            comboBox1.SelectedIndex = 0;



            comboBox2.SelectedIndex = 0;
            comboBox2.SelectedIndexChanged += ComboBox2_SelectedIndexChanged;
            checkBox3.CheckedChanged += CheckBox3_CheckedChanged;

            comboBox3.DataSource = SerialPort.GetPortNames( );
            try
            {
                comboBox3.SelectedIndex = 0;
            }
            catch
            {
                comboBox3.Text = "COM3";
            }

            Language( Program.Language );


            // 多实例整窗化（阶段 7，仿 FormOmron/FormModbus）：每个窗体实例只承载 _linkId 这一个连接的运行时。
            // 连接 2~4 由 FormCommManager 各自 new FormModbusRtu(link) 整窗实例承载并嵌入，不再由主窗体 LoadAll 后台启动。
            // 启动前相机关卡复查：本连接（连接 1~4 等价）若绑定了被同协议其它连接占用的相机，
            // 则阻止本连接自动启动（不建链、不轮询、不标记就绪），但仍加载界面供查看并修正。
            bool commBlocked = _linkId >= 1 && ModbusRtuIniStore.Load(wdini, _linkId) != null
                && (CommCameraGuard.CheckStartupModbusRtu(wdini, _linkId) != null);

            var cfg = ModbusRtuIniStore.Load(wdini, _linkId);
            if (cfg != null && !_runtimes.ContainsKey(_linkId) && !commBlocked)
            {
                var rt = new ModbusRtuRuntime(_rtuLink, this) { LinkId = _linkId, Config = cfg, Context = null };
                rt.Start();
                _runtimes[_linkId] = rt;
                AppHost.Services.Resolve<CommLinkManager>().Register(rt);
            }
            _runtime = _runtimes.ContainsKey(_linkId) ? _runtimes[_linkId] : null;
            decimal xuanzhong_temp = 0;
            for (int i = 0; i < 10; i++)
            {
                dataGridView1.Rows.Add();
            }
            _gridUi = new CommGridUiSink(dataGridView1);
            CommGridHelper.ApplyDataTabChrome(tabPage1, dataGridView1, b1, b2);
            CommGridHelper.StyleClearButton(b4);
            for (int i = 0; i < 50; i++)
            {
                fins_data.Add(i, new int[] { i % 10, i / 10 * 2 + 1 });
                fins_name.Add(i, new int[] { i % 10, i / 10 * 2 });
                fins_value.Add(i, new byte[] { 0x00, 0x00 });
            }

            fins_lunxunen = bool.Parse(wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "modbusrtu_lunxunen", "false"));
            fins_en = bool.Parse(wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "modbusrtu_en", "false"));
            address_qishi = decimal.Parse(wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "qishi", "0"));
            address_length = decimal.Parse(wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "zongchang", "1"));
            lunxun_time = decimal.Parse(wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "lunxun_time", "20"));
            numericUpDown1.Value = address_qishi;
            numericUpDown2.Value = address_length;
            numericUpDown3.Value = lunxun_time;

            comboBox1.Text = wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "Parity", "无").Replace("\0", "");
            comboBox3.Text = wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "PortName", "COM3").Replace("\0", "");
            comboBox2.Text = wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "abcd", "CDAB").Replace("\0", "");
            textBox16.Text = wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "dataBits", "8").Replace("\0", "");
            textBox2.Text = wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "baudRate", "9600").Replace("\0", "");
            textBox17.Text = wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "stopBits", "1").Replace("\0", "");
            textBox15.Text = wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "station", "1").Replace("\0", "");
            if (fins_lunxunen)
            {
                c1.CheckState = CheckState.Checked;
            }
            if (fins_en && !commBlocked)
            {
                c2.CheckState = CheckState.Checked;
                // ★ 2026-09-12：移除此处同步 button1_Click，避免与下方 Task.Run 延迟建链重复建链（重复建链会造成闪断）。
                //   ModbusRTU 连接1 启动建链统一由本方法末尾 Task.Run 异步执行一次（对齐 FINS 的修复）。
            }
            geshu = int.Parse(wdini.ReadString(ModbusRtuIniStore.ConnSection(_linkId), "geshu", "0"));
            if (geshu > 0)
            {
                for (int i = 0; i < geshu; i++)
                {
                    fins_mingcheng = wdini.ReadString(ModbusRtuIniStore.BlockSection(_linkId, i + 1), "name", "").Replace("\0", "");
                    fins_qishi = decimal.Parse(wdini.ReadString(ModbusRtuIniStore.BlockSection(_linkId, i + 1), "qishi", "0"));
                    fins_length = decimal.Parse(wdini.ReadString(ModbusRtuIniStore.BlockSection(_linkId, i + 1), "changdu", "0"));
                    ABCD = wdini.ReadString(ModbusRtuIniStore.BlockSection(_linkId, i + 1), "gaodiwei", "触发").Replace("\0", "");
                    fins_style = wdini.ReadString(ModbusRtuIniStore.BlockSection(_linkId, i + 1), "geshi", "int").Replace("\0", "");
                    fins_dic.Add(fins_mingcheng, new string[] { fins_mingcheng, fins_qishi.ToString(), fins_length.ToString(), ABCD, fins_style });
                    for (int j = 0; j < fins_length; j++)
                    {
                        xuanzhong_temp = fins_qishi - address_qishi + j;
                        dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                        dataGridView1[fins_data[int.Parse(xuanzhong_temp.ToString())][0], fins_data[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                        dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Value = fins_mingcheng;
                    }
                }
            }
            for (int i = 0; i < 13; i++)
            {
                string camSec = ModbusRtuIniStore.CameraSection(_linkId, i + 1);
                string temp_jian = wdini.ReadString(camSec, "chufa", " ").Replace("\0", "");
                camera_dic.Add(i + 1, new string[] { temp_jian, wdini.ReadString(camSec, "fanhuizhi", "0").Replace("\0", ""), wdini.ReadString(camSec, "fanhuien", "false").Replace("\0", ""), wdini.ReadString(camSec, "fankui", "0").Replace("\0", ""), "无", "无", wdini.ReadString(camSec, "chukufangshi", "相等").Replace("\0", ""), wdini.ReadString(camSec, "chufazhi1", "").Replace("\0", ""), wdini.ReadString(camSec, "chufazhi2", "").Replace("\0", "") });

                if (!fins_zuhe.ContainsKey(temp_jian))
                {
                    fins_zuhe.Add(temp_jian, (i + 1).ToString());
                }
                else
                    fins_zuhe[temp_jian] += (i + 1).ToString();

            }
            int cccc = 0;
            foreach (var f in fins_zuhe.Keys)
            {
                if (cccc == 0)
                {
                    label43.Text = fins_zuhe[f].ToString() + "_" + f.ToString();
                }
                if (cccc == 1)
                {
                    label42.Text = fins_zuhe[f].ToString() + "_" + f.ToString();
                }
                if (cccc == 2)
                {
                    label41.Text = fins_zuhe[f].ToString() + "_" + f.ToString();
                }
                if (cccc == 3)
                {
                    label40.Text = fins_zuhe[f].ToString() + "_" + f.ToString();
                };
                cccc++;
            }
            cb4.Items.Add(camera_dic[1][0]);
            cb4.Text = camera_dic[1][0];
            t2.Text = camera_dic[1][1];
            if (camera_dic[1][2] == "true")
                c3.CheckState = CheckState.Checked;
            cb13.Items.Add(camera_dic[1][3]);
            cb13.Text = camera_dic[1][3];

            cb5.Items.Add(camera_dic[2][0]);
            cb5.Text = camera_dic[2][0];
            t3.Text = camera_dic[2][1];
            if (camera_dic[2][2] == "true")
                c4.CheckState = CheckState.Checked;
            cb14.Items.Add(camera_dic[2][3]);
            cb14.Text = camera_dic[2][3];

            cb6.Items.Add(camera_dic[3][0]);
            cb6.Text = camera_dic[3][0];
            t4.Text = camera_dic[3][1];
            if (camera_dic[3][2] == "true")
                c5.CheckState = CheckState.Checked;
            cb15.Items.Add(camera_dic[3][3]);
            cb15.Text = camera_dic[3][3];

            cb7.Items.Add(camera_dic[4][0]);
            cb7.Text = camera_dic[4][0];
            t5.Text = camera_dic[4][1];
            if (camera_dic[4][2] == "true")
                c6.CheckState = CheckState.Checked;
            cb16.Items.Add(camera_dic[4][3]);
            cb16.Text = camera_dic[4][3];

            cb8.Items.Add(camera_dic[5][0]);
            cb8.Text = camera_dic[5][0];
            t6.Text = camera_dic[5][1];
            if (camera_dic[5][2] == "true")
                c7.CheckState = CheckState.Checked;
            cb17.Items.Add(camera_dic[5][3]);
            cb17.Text = camera_dic[5][3];

            cb9.Items.Add(camera_dic[6][0]);
            cb9.Text = camera_dic[6][0];
            t7.Text = camera_dic[6][1];
            if (camera_dic[6][2] == "true")
                c8.CheckState = CheckState.Checked;
            cb18.Items.Add(camera_dic[6][3]);
            cb18.Text = camera_dic[6][3];

            cb10.Items.Add(camera_dic[7][0]);
            cb10.Text = camera_dic[7][0];
            t8.Text = camera_dic[7][1];
            if (camera_dic[7][2] == "true")
                c9.CheckState = CheckState.Checked;
            cb19.Items.Add(camera_dic[7][3]);
            cb19.Text = camera_dic[7][3];

            cb11.Items.Add(camera_dic[8][0]);
            cb11.Text = camera_dic[8][0];
            t9.Text = camera_dic[8][1];
            if (camera_dic[8][2] == "true")
                c10.CheckState = CheckState.Checked;
            cb20.Items.Add(camera_dic[8][3]);
            cb20.Text = camera_dic[8][3];

            cb12.Items.Add(camera_dic[9][0]);
            cb12.Text = camera_dic[9][0];
            t10.Text = camera_dic[9][1];
            if (camera_dic[9][2] == "true")
                c11.CheckState = CheckState.Checked;
            cb27.Items.Add(camera_dic[9][3]);
            cb27.Text = camera_dic[9][3];
            cb28.Items.Add(camera_dic[12][3]);
            cb28.Text = camera_dic[12][3];

            cb21.Items.Add(camera_dic[13][0]);
            cb21.Text = camera_dic[13][0];
            t11.Text = camera_dic[13][1];
            if (camera_dic[13][2] == "true")
                c12.CheckState = CheckState.Checked;

            // Camera 10
            cb22.Items.Add(camera_dic[10][0]);
            cb22.Text = camera_dic[10][0];
            t28.Text = camera_dic[10][1];
            if (camera_dic[10][2] == "true")
                c13.CheckState = CheckState.Checked;
            cb23.Items.Add(camera_dic[10][3]);
            cb23.Text = camera_dic[10][3];

            // Camera 11
            cb24.Items.Add(camera_dic[11][0]);
            cb24.Text = camera_dic[11][0];
            t29.Text = camera_dic[11][1];
            if (camera_dic[11][2] == "true")
                c14.CheckState = CheckState.Checked;
            cb25.Items.Add(camera_dic[11][3]);
            cb25.Text = camera_dic[11][3];

            // Camera 12
            cb26.Items.Add(camera_dic[12][0]);
            cb26.Text = camera_dic[12][0];
            t30.Text = camera_dic[12][1];
            if (camera_dic[12][2] == "true")
                c15.CheckState = CheckState.Checked;

            // Camera 13 (Heartbeat)
            t39.Text = camera_dic[13][1];
            if (camera_dic[13][2] == "true")
                c16.CheckState = CheckState.Checked;
            cb29.Items.Add(camera_dic[13][3]);
            cb29.Text = camera_dic[13][3];

            string chgSec = ModbusRtuIniStore.ChangeSection(_linkId);
            string pthSec = ModbusRtuIniStore.PathSection(_linkId);
            t12.Text = wdini.ReadString(chgSec, "1", "");
            t13.Text = wdini.ReadString(chgSec, "2", "");
            t14.Text = wdini.ReadString(chgSec, "3", "");
            t15.Text = wdini.ReadString(chgSec, "4", "");
            t16.Text = wdini.ReadString(chgSec, "5", "");
            t17.Text = wdini.ReadString(chgSec, "6", "");
            t18.Text = wdini.ReadString(chgSec, "7", "");
            t19.Text = wdini.ReadString(chgSec, "8", "");
            t31.Text = wdini.ReadString(chgSec, "9", "");
            t32.Text = wdini.ReadString(chgSec, "10", "");
            t33.Text = wdini.ReadString(chgSec, "11", "");
            t34.Text = wdini.ReadString(chgSec, "12", "");
            t20.Text = wdini.ReadString(pthSec, "1", "");
            t21.Text = wdini.ReadString(pthSec, "2", "");
            t22.Text = wdini.ReadString(pthSec, "3", "");
            t23.Text = wdini.ReadString(pthSec, "4", "");
            t24.Text = wdini.ReadString(pthSec, "5", "");
            t25.Text = wdini.ReadString(pthSec, "6", "");
            t26.Text = wdini.ReadString(pthSec, "7", "");
            t27.Text = wdini.ReadString(pthSec, "8", "");
            t35.Text = wdini.ReadString(pthSec, "9", "");
            t36.Text = wdini.ReadString(pthSec, "10", "");
            t37.Text = wdini.ReadString(pthSec, "11", "");
            t38.Text = wdini.ReadString(pthSec, "12", "");


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
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, capturedCam), "chukufangshi", modeBox.Text);
                    _triggerLatch[capturedCam] = false;
                };
                modeBox.SelectedIndexChanged += (s, ev) => applyTrigMode();
                modeBox.TextChanged += (s, ev) => applyTrigMode();
                valBox.TextChanged += (s, ev) => SaveModbusRtuTrigVal(capturedCam, valBox);
            }

            Task.Run(() =>
            {

                Thread.Sleep(1000);
                if (fins_en && !commBlocked)
                {
                    // ★P3.1：button1_Click 是 async void，其同步前缀（读 comboBox/textBox/checkBox）
                    // 若在线程池线程执行会裸访问控件（Debug 开跨线程校验抛 InvalidOperationException，Release 下竞态）。
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
        }


        private void Language( int language )
        {
            if (language == 2)
            {
                Text = "Modbus Rtu Read Demo";

                label1.Text = "Com:";
                label3.Text = "baudRate:";
                label22.Text = "DataBit";
                label23.Text = "StopBit";
                label24.Text = "parity";
                label21.Text = "station";
                checkBox1.Text = "address from 0";
                checkBox3.Text = "string reverse";
                button1.Text = "Connect";
                button2.Text = "Disconnect";

                label6.Text = "address:";
                label7.Text = "result:";

                button_read_bool.Text = "r-coil";
                button4.Text = "r-discrete";
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
                groupBox4.Text = "Message reading test, hex string needs to be filled in,without crc";
                

                comboBox1.DataSource = new string[] { "None", "Odd", "Even" };
            }
        }

        private void CheckBox3_CheckedChanged( object sender, EventArgs e )
        {
            if (busRtuClient != null)
            {
                busRtuClient.IsStringReverse = checkBox3.Checked;
            }
        }

        private void ComboBox2_SelectedIndexChanged( object sender, EventArgs e )
        {
            if (busRtuClient != null)
            {
                switch (comboBox2.SelectedIndex)
                {
                    case 0: busRtuClient.DataFormat = HslCommunication.Core.DataFormat.ABCD; break;
                    case 1: busRtuClient.DataFormat = HslCommunication.Core.DataFormat.BADC; break;
                    case 2: busRtuClient.DataFormat = HslCommunication.Core.DataFormat.CDAB; break;
                    case 3: busRtuClient.DataFormat = HslCommunication.Core.DataFormat.DCBA; break;
                    default: break;
                }
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



        private bool _connecting;   // ★ 2026-09-12：建链防重入。启动 Task.Run 与手动点击并发时只建一次链，避免重复建链闪断。
        private async void button1_Click( object sender, EventArgs e )
        {
            if (_connecting) { Log( "正在连接中，请稍候..." ); return; }
            int baudRate;
            if(!int.TryParse(textBox2.Text,out baudRate ))
            {
                Log( DemoUtils.BaudRateInputWrong );
                return;
            }
            int dataBits;
            if (!int.TryParse( textBox16.Text, out dataBits ))
            {
                Log( DemoUtils.DataBitsInputWrong );
                return;
            }
            int stopBits;
            if (!int.TryParse( textBox17.Text, out stopBits ))
            {
                Log( DemoUtils.StopBitInputWrong );
                return;
            }

            byte station;
            if (!byte.TryParse(textBox15.Text,out station))
            {
                Log( "Station input wrong！" );
                return;
            }

            if (_rtuLink.Client != null) _rtuLink.Close( );
            _rtuLink.PortName = comboBox3.Text;
            _rtuLink.BaudRate = baudRate;
            _rtuLink.DataBits = dataBits;
            _rtuLink.StopBitsValue = stopBits;
            _rtuLink.Station = station;
            _rtuLink.AddressStartWithZero = checkBox1.Checked;
            _rtuLink.ParityName = comboBox1.SelectedIndex == 0 ? "None" : (comboBox1.SelectedIndex == 1 ? "Odd" : "Even");
            HslCommunication.Core.DataFormat? fmt = null;
            if (comboBox2.SelectedIndex >= 0 && comboBox2.SelectedIndex <= 3)
                fmt = (HslCommunication.Core.DataFormat)comboBox2.SelectedIndex;


            _rtuLink.IsStringReverse = checkBox3.Checked;

            // ★P4：手动建链与后台自动重连互斥，避免两条线程同时 Close/Open 同一个 Hsl 客户端
            if (System.Threading.Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0)
            {
                Log("正在进行后台自动重连，请稍后再试。");
                return;
            }

            button1.Enabled = false;   // 2026-09-06：防重复点击
            _connecting = true;
            try
            {
                OperateResult connect = await _rtuLink.ConnectAsync(fmt);
                if (connect.IsSuccess)
                {
                    button2.Enabled = true;
                    button1.Enabled = false;
                    panel2.Enabled = true;

                    userControlCurve1.ReadWriteNet = busRtuClient;
                }
                else
                {
                    Log(connect.Message);
                    button1.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                button1.Enabled = true;
            }
            finally
            {
                _connecting = false;
                System.Threading.Interlocked.Exchange(ref _reconnecting, 0);   // ★P4：释放与自动重连的互斥
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            DisconnectLink();
        }
        
        #endregion

        #region 单数据读取测试


        private void button_read_bool_Click( object sender, EventArgs e )
        {
            // 读取bool变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadCoil( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button4_Click_1( object sender, EventArgs e )
        {
            // 离散输入读取
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadDiscrete( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_short_Click( object sender, EventArgs e )
        {
            // 读取short变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadInt16( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_ushort_Click( object sender, EventArgs e )
        {
            // 读取ushort变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadUInt16( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_int_Click( object sender, EventArgs e )
        {
            // 读取int变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadInt32(  textBox3.Text ), textBox3.Text, textBox4 ));
        }
        private void button_read_uint_Click( object sender, EventArgs e )
        {
            // 读取uint变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadUInt32( textBox3.Text ), textBox3.Text, textBox4 ));
        }
        private void button_read_long_Click( object sender, EventArgs e )
        {
            // 读取long变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadInt64( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_ulong_Click( object sender, EventArgs e )
        {
            // 读取ulong变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadUInt64( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_float_Click( object sender, EventArgs e )
        {
            // 读取float变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadFloat( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_double_Click( object sender, EventArgs e )
        {
            // 读取double变量
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadDouble( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_string_Click( object sender, EventArgs e )
        {
            // 读取字符串
            ManualRead(() => DemoUtils.ReadResultRender( busRtuClient.ReadString( textBox3.Text , ushort.Parse( textBox5.Text ) ), textBox3.Text, textBox4 ));
        }

        // ★BUG1/BUG3 修复：手动"读"按钮运行在 UI 线程，若此刻正在重连或客户端未连接，
        //   裸调 Hsl 读会因对关闭中的 socket 发请求而抛异常 → UI 线程未处理 → 整个程序崩。
        //   复用与轮询读相同的 _reconnecting 门控（重连进行中直接返回），并统一 try/catch 兜底。
        // ★全 I/O 单锁串行：所有 Hsl socket 读都经此走 _ioSync，与回写/重连互斥，
        //   杜绝轮询读/手动读与自动重连的 ConnectClose 抢同一串口（BUG1 根因）。
        private HslCommunication.OperateResult<T> ReadLocked<T>(Func<HslCommunication.OperateResult<T>> readOp)
        {
            lock (_ioSync) { return readOp(); }
        }

        private void ManualRead(Action readAction)
        {
            if (_reconnecting != 0)
            {
                MessageBox.Show("通讯正在重连，请稍候再试。", "提示");
                return;
            }
            try
            {
                // ★全 I/O 单锁串行：手动读与轮询读/回写/重连共用 _ioSync
                lock (_ioSync)
                {
                    readAction();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "读取出错");
            }
        }


        #endregion

        #region 单数据写入测试


        private void button24_Click( object sender, EventArgs e )
        {
            // bool写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.WriteCoil( textBox8.Text, bool.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button22_Click( object sender, EventArgs e )
        {
            // short写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , short.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button21_Click( object sender, EventArgs e )
        {
            // ushort写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , ushort.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }


        private void button20_Click( object sender, EventArgs e )
        {
            // int写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , int.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button19_Click( object sender, EventArgs e )
        {
            // uint写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , uint.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button18_Click( object sender, EventArgs e )
        {
            // long写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , long.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button17_Click( object sender, EventArgs e )
        {
            // ulong写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , ulong.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button16_Click( object sender, EventArgs e )
        {
            // float写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , float.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button15_Click( object sender, EventArgs e )
        {
            // double写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , double.Parse( textBox7.Text ) ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }


        private void button14_Click( object sender, EventArgs e )
        {
            // string写入
            try
            {
                DemoUtils.WriteResultRender( busRtuClient.Write( textBox8.Text , textBox7.Text ), textBox8.Text );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        
        #endregion

        #region 批量读取测试

        private void button25_Click( object sender, EventArgs e )
        {
            DemoUtils.BulkReadRenderResult( busRtuClient, textBox6, textBox9, textBox10 );
        }



        #endregion

        #region 报文读取测试


        private void button26_Click( object sender, EventArgs e )
        {
            OperateResult<byte[]> read = busRtuClient.ReadBase( HslCommunication.Serial.SoftCRC16.CRC16( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) ) );
            if (read.IsSuccess)
            {
                textBox11.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content );
            }
            else
            {
                MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
            }
        }


        #endregion
        
        #region Test Function


        private void Test1()
        {
            OperateResult<bool[]> read = busRtuClient.ReadCoil( "100", 10 );
            if(read.IsSuccess)
            {
                bool coil_100 = read.Content[0];
                // and so on 
                bool coil_109 = read.Content[9];
            }
            else
            {
                // failed
                string err = read.Message;
            }
        }


        private void Test2()
        {
            bool[] values = new bool[] { true, false, false, false, true, true, false, true, false, false };
            OperateResult write = busRtuClient.WriteCoil( "100", values );
            if (write.IsSuccess)
            {
                // success
            }
            else
            {
                // failed
                string err = write.Message;
            }

            HslCommunication.Core.IByteTransform ByteTransform = new HslCommunication.Core.ReverseWordTransform( );
        }



        #endregion

        private void b5_Click(object sender, EventArgs e)
        {
            if (b5.Text == "显示")
            {
                tabControl1.Visible = true;
                b5.Text = "隐藏";
            }
            else
            {
                tabControl1.Visible = false;
                b5.Text = "显示";
            }
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

        private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            bool xuanzhong_temp = false;

            if (sender is DataGridView)
            {
                DataGridView dgv = (DataGridView)sender;
                if (e.RowIndex % 2 == 1 && e.ColumnIndex >= 0)//如果该行为表头
                {


                    x = e.ColumnIndex;
                    y = e.RowIndex;
                    foreach (var pair in fins_data)
                    {
                        if (pair.Value[0] == x && pair.Value[1] == y)
                        {
                            foreach (var p_temp in fins_dic)
                            {
                                if ((pair.Key + address_qishi) >= int.Parse(p_temp.Value[1]) && (pair.Key + address_qishi) < int.Parse(p_temp.Value[1]) + int.Parse(p_temp.Value[2]))
                                {
                                    xuanzhong_temp = true;
                                    numericUpDown5.Value = int.Parse(p_temp.Value[1]);
                                    int blockIdx = int.Parse(p_temp.Value[1]) - int.Parse(address_qishi.ToString());
                                    if (fins_data.ContainsKey(blockIdx))
                                    {
                                        x = fins_data[blockIdx][0];
                                        y = fins_data[blockIdx][1];
                                    }
                                    numericUpDown4.Value = int.Parse(p_temp.Value[2]);
                                    t1.Text = p_temp.Value[0];
                                    cb2.Text = p_temp.Value[3];
                                    cb3.Text = p_temp.Value[4];
                                }

                            }
                            if (xuanzhong_temp == false)
                            {
                                numericUpDown5.Value = pair.Key + address_qishi;
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

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                address_qishi = numericUpDown1.Value;
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "qishi", numericUpDown1.Value.ToString());
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                address_length = numericUpDown2.Value;
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "zongchang", numericUpDown2.Value.ToString());
            }
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                lunxun_time = numericUpDown3.Value;
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "lunxun_time", lunxun_time.ToString());

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

        private void timer2_Tick(object sender, EventArgs e)
        {
            // 相机9-13 原"周期预置触发通道待写标记"已统一移除：
            // 相机1-12 业务同等，原逻辑会把任意相机的检测输出经 xie() 误写入 9-12 的触发通道
            // （与相机13切换通道同源）；移除后各相机仅由 WriteCameraOutput 回写各自反馈通道。
            // fins_xie 数组随之清理（原为只写不读的死代码）。
        }

        private void c1_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c1.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
                else
                {
                    fins_lunxunen = false;
                }
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "modbusrtu_lunxunen", fins_lunxunen.ToString());
            }
        }

        private void DisconnectLink()
        {
            // ★M5 修复：与轮询读/写/重连共用 _ioSync 单锁——原实现直接 Close，
            //   会与在途 I/O 抢同一串口/socket。
            lock (_ioSync) { try { if (_rtuLink.Client != null) _rtuLink.Close(); } catch { } }
            button2.Enabled = false;
            button1.Enabled = true;
            panel2.Enabled = false;
        }

        /// <summary>
        /// 参数控件锁定：使能开(en=true)时锁定所有通讯/相机参数控件，运行时不容许修改；
        /// 使能关(en=false)时解禁。配合“使能关立即断连”，保证“修改参数”与“活跃连接”严格互斥。
        /// 控件按名称查找（RTU 相机绑定下拉为 cb4..cb29，兼容控件命名差异）。
        /// </summary>
        private void SetParamControlsEnabled(bool enable)
        {
            foreach (var name in new[] { "textBox1", "textBox2", "textBox15", "textBox16", "comboBox1", "numericUpDown1", "numericUpDown2", "numericUpDown3" })
            {
                var c = this.Controls.Find(name, true);
                if (c.Length > 0) c[0].Enabled = enable;
            }
            for (int i = 4; i <= 29; i++) { var c = this.Controls.Find("cb" + i, true); if (c.Length > 0) c[0].Enabled = enable; }
            foreach (var name in new[] { "textBox14", "textBox17", "textBox19", "textBox18", "textBox23", "textBox22", "textBox21", "textBox20", "textBox24", "textBox42", "textBox43", "textBox44", "textBox41", "textBox53" })
            { var c = this.Controls.Find(name, true); if (c.Length > 0) c[0].Enabled = enable; }
            for (int i = 1; i <= 12; i++)
            {
                var m = this.Controls.Find("cboTrigMode" + i, true); if (m.Length > 0) m[0].Enabled = enable;
                var v = this.Controls.Find("txtTrigVal" + i, true); if (v.Length > 0) v[0].Enabled = enable;
            }
            foreach (var name in new[] { "checkBox3", "checkBox16" })
            { var c = this.Controls.Find(name, true); if (c.Length > 0) c[0].Enabled = enable; }
        }

        private void c2_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                bool en = c2.CheckState == CheckState.Checked;
                fins_en = en;
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "modbusrtu_en", fins_en.ToString());
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

        private ComboBox[] GetBindingCombos()
        {
            return new ComboBox[]
            {
                cb4, cb5, cb6, cb7, cb8, cb9, cb10, cb11, cb12, cb13, cb14, cb15,
                cb16, cb17, cb18, cb19, cb20, cb21, cb22, cb23, cb24, cb25, cb26,
                cb27, cb28, cb29
            };
        }

        private void b1_Click(object sender, EventArgs e)
        {
            if (!CommGridHelper.TryPrepareFullClear(t1.Text.Trim(), () => t1.Text = ""))
                return;
            CommGridHelper.PerformFullDataClear(wdini, ModbusRtuIniStore.ConnSection(_linkId), DataSuffix, ref geshu, fins_dic, _triggerLatch);
            fins_zuhe.Clear();
            dataGridView1.Visible = false;
            CommGridHelper.ClearGridZebra(dataGridView1);
            dataGridView1.Visible = true;
            b4_Click(null, null);
        }
        private Thread fins_duxie;
        // ★ I/O 串行锁：同一连接实例的“网络写”共用（轮询触发回写 / 检测结果回写 / 切型回执 / 极速写），
        //   避免多个检测线程或轮询线程并发操作同一 Hsl/串口客户端导致收发串包/响应错乱。
        private readonly object _ioSync = new object();
        private volatile bool _stopPolling = false;
        private CommGridUiSink _gridUi;

        private void b2_Click(object sender, EventArgs e)
        {
            try
            {
                fins_lunxunen = false;
                decimal xuanzhong_temp = 0;
                bool chongdie = false;
                if (cb3.Text.Length > 1 && cb2.Text.Length > 1 && t1.Text.Length > 0)
                {
                    if ((cb3.Text == "string" || cb3.Text == "int") || (numericUpDown5.Value % 2 == 0))
                    {
                        if (numericUpDown5.Value >= numericUpDown1.Value && numericUpDown1.Value + numericUpDown2.Value >= numericUpDown4.Value + numericUpDown5.Value)
                        {
                            if (t1.Text.Length > 0)
                            {
                                if (fins_dic.Count < 10)
                                {
                                    if (fins_dic.Count > 0)
                                    {
                                        foreach (var pat in fins_dic)
                                        {
                                            if (decimal.Parse(pat.Value[1]) >= numericUpDown5.Value + numericUpDown4.Value || decimal.Parse(pat.Value[1]) + decimal.Parse(pat.Value[2]) <= numericUpDown5.Value)
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
                                    else
                                    {
                                        fins_dic.Add(t1.Text, new string[] { t1.Text, numericUpDown5.Value.ToString(), numericUpDown4.Value.ToString(), cb2.Text, cb3.Text });
                                        for (int i = 0; i < numericUpDown4.Value; i++)
                                        {
                                            xuanzhong_temp = numericUpDown5.Value - numericUpDown1.Value + i;
                                            dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                                            dataGridView1[fins_data[int.Parse(xuanzhong_temp.ToString())][0], fins_data[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                                            dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Value = t1.Text;
                                        }
                                        geshu = fins_dic.Count;
                                        wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "geshu", fins_dic.Count.ToString());
                                        wdini.WriteString(ModbusRtuIniStore.BlockSection(_linkId, geshu), "name", t1.Text);
                                        wdini.WriteString(ModbusRtuIniStore.BlockSection(_linkId, geshu), "qishi", numericUpDown5.Value.ToString());
                                        wdini.WriteString(ModbusRtuIniStore.BlockSection(_linkId, geshu), "changdu", numericUpDown4.Value.ToString());
                                        wdini.WriteString(ModbusRtuIniStore.BlockSection(_linkId, geshu), "gaodiwei", cb2.Text);
                                        wdini.WriteString(ModbusRtuIniStore.BlockSection(_linkId, geshu), "geshi", cb3.Text);
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
                if (c1.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                if (c1.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
            }
        }

        private void cb4_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb4.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb4.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb5_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb5.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb5.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb6_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb6.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb6.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb7_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb7.Text;
                if (GuardCamera(4, nv, 0, cb7))
                {
                    camera_dic[4][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 4), "chufa", nv);
                }
            }
        }

        private void cb8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb8.Text;
                if (GuardCamera(5, nv, 0, cb8))
                {
                    camera_dic[5][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 5), "chufa", nv);
                }
            }
        }

        private void cb8_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb8.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb8.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb9_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb9.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb9.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb10_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb10.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb10.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb11_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb11.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb11.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb12_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb12.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb12.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb13_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb13.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb13.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb14_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb14.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb14.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb15_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb15.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb15.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb16_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb16.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb16.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb17_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb17.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb17.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb18_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb18.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb18.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb19_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb19.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb19.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb20_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb20.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb20.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb21_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb21.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb21.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb22_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb22.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb22.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb23_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb23.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb23.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb24_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb24.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb24.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb25_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb25.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb25.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb26_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb26.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb26.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb27_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb27.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb27.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb28_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb28.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            cb28.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb4.Text;
                if (GuardCamera(1, nv, 0, cb4))
                {
                    camera_dic[1][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 1), "chufa", nv);
                }
            }
        }

        private void cb5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb5.Text;
                if (GuardCamera(2, nv, 0, cb5))
                {
                    camera_dic[2][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 2), "chufa", nv);
                }
            }
        }

        private void cb6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb6.Text;
                if (GuardCamera(3, nv, 0, cb6))
                {
                    camera_dic[3][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 3), "chufa", nv);
                }
            }
        }

        private void cb7_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb7.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("触发"))
                        {
                            cb7.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void cb9_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb9.Text;
                if (GuardCamera(6, nv, 0, cb9))
                {
                    camera_dic[6][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 6), "chufa", nv);
                }
            }
        }
        private void cb10_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb10.Text;
                if (GuardCamera(7, nv, 0, cb10))
                {
                    camera_dic[7][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 7), "chufa", nv);
                }
            }
        }
        private void cb11_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb11.Text;
                if (GuardCamera(8, nv, 0, cb11))
                {
                    camera_dic[8][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 8), "chufa", nv);
                }
            }
        }
        private void cb12_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb12.Text;
                if (GuardCamera(9, nv, 0, cb12))
                {
                    camera_dic[9][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 9), "chufa", nv);
                }
            }
        }

        private void cb13_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb13.Text;
                if (GuardCamera(1, nv, 3, cb13))
                {
                    camera_dic[1][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 1), "fankui", nv);
                }
            }
        }

        private void cb14_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb14.Text;
                if (GuardCamera(2, nv, 3, cb14))
                {
                    camera_dic[2][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 2), "fankui", nv);
                }
            }
        }

        private void cb15_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb15.Text;
                if (GuardCamera(3, nv, 3, cb15))
                {
                    camera_dic[3][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 3), "fankui", nv);
                }
            }
        }

        private void cb16_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb16.Text;
                if (GuardCamera(4, nv, 3, cb16))
                {
                    camera_dic[4][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 4), "fankui", nv);
                }
            }
        }

        private void cb17_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb17.Text;
                if (GuardCamera(5, nv, 3, cb17))
                {
                    camera_dic[5][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 5), "fankui", nv);
                }
            }
        }

        private void cb18_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb18.Text;
                if (GuardCamera(6, nv, 3, cb18))
                {
                    camera_dic[6][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 6), "fankui", nv);
                }
            }
        }

        private void cb19_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb19.Text;
                if (GuardCamera(7, nv, 3, cb19))
                {
                    camera_dic[7][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 7), "fankui", nv);
                }
            }
        }

        private void cb20_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb20.Text;
                if (GuardCamera(8, nv, 3, cb20))
                {
                    camera_dic[8][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 8), "fankui", nv);
                }
            }
        }

        private void cb21_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb21.Text;
                if (GuardCamera(13, nv, 0, cb21))
                {
                    camera_dic[13][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "chufa", nv);
                }
            }
        }

        private void cb22_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb22.Text;
                if (GuardCamera(10, nv, 0, cb22))
                {
                    camera_dic[10][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 10), "chufa", nv);
                }
            }
        }

        private void cb23_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb23.Text;
                if (GuardCamera(10, nv, 3, cb23))
                {
                    camera_dic[10][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 10), "fankui", nv);
                }
            }
        }

        private void cb24_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb24.Text;
                if (GuardCamera(11, nv, 0, cb24))
                {
                    camera_dic[11][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 11), "chufa", nv);
                }
            }
        }

        private void cb25_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb25.Text;
                if (GuardCamera(11, nv, 3, cb25))
                {
                    camera_dic[11][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 11), "fankui", nv);
                }
            }
        }

        private void cb26_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb26.Text;
                if (GuardCamera(12, nv, 0, cb26))
                {
                    camera_dic[12][0] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 12), "chufa", nv);
                }
            }
        }

        private void cb27_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb27.Text;
                if (GuardCamera(9, nv, 3, cb27))
                {
                    camera_dic[9][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 9), "fankui", nv);
                }
            }
        }

        private void cb28_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = cb28.Text;
                if (GuardCamera(12, nv, 3, cb28))
                {
                    camera_dic[12][3] = nv;
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 12), "fankui", nv);
                }
            }
        }

        private void t39_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[13][1] = t39.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "fanhuizhi", t39.Text);
            }
        }

        private void c16_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c16.CheckState == CheckState.Checked)
                {
                    camera_dic[13][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "fanhuien", "true");
                }
                else
                {
                    camera_dic[13][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "fanhuien", "false");
                }
            }
        }

        private void cb29_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                cb29.Items.Clear();
                foreach (var pap in fins_dic)
                {
                    if (pap.Value[3].Contains("反馈"))
                        cb29.Items.Add(pap.Key);
                }
            }
        }

        private void cb29_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[13][3] = cb29.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "fankui", cb29.Text);
            }
        }

        private void t2_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[1][1] = t2.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 1), "fanhuizhi", t2.Text);
            }
        }

        private void t3_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[2][1] = t3.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 2), "fanhuizhi", t3.Text);
            }
        }

        private void t4_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[3][1] = t4.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 3), "fanhuizhi", t4.Text);
            }
        }

        private void t5_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[4][1] = t5.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 4), "fanhuizhi", t5.Text);
            }
        }

        private void t6_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[5][1] = t6.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 5), "fanhuizhi", t6.Text);
            }
        }

        private void t7_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[6][1] = t7.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 6), "fanhuizhi", t7.Text);
            }
        }

        private void t8_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[7][1] = t8.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 7), "fanhuizhi", t8.Text);
            }
        }

        private void t9_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[8][1] = t9.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 8), "fanhuizhi", t9.Text);
            }
        }

        private void t10_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[9][1] = t10.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 9), "fanhuizhi", t10.Text);
            }
        }

        private void t11_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[13][1] = t11.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "fanhuizhi", t11.Text);
            }
        }

        private void t28_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[10][1] = t28.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 10), "fanhuizhi", t28.Text);
            }
        }

        private void t29_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[11][1] = t29.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 11), "fanhuizhi", t29.Text);
            }
        }

        private void t30_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[12][1] = t30.Text;
                wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 12), "fanhuizhi", t30.Text);
            }
        }

        private void t20_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t20.Text = openFileDialog.FileName;
            }
        }

        private void t21_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t21.Text = openFileDialog.FileName;
            }
        }

        private void t22_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t22.Text = openFileDialog.FileName;
            }
        }

        private void t23_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t23.Text = openFileDialog.FileName;
            }
        }

        private void t24_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t24.Text = openFileDialog.FileName;
            }
        }

        private void t25_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t25.Text = openFileDialog.FileName;
            }
        }

        private void t26_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t26.Text = openFileDialog.FileName;
            }
        }

        private void t27_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t27.Text = openFileDialog.FileName;
            }
        }

        private void t35_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t35.Text = openFileDialog.FileName;
            }
        }

        private void t36_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t36.Text = openFileDialog.FileName;
            }
        }

        private void t37_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t37.Text = openFileDialog.FileName;
            }
        }

        private void t38_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                t38.Text = openFileDialog.FileName;
            }
        }

        private void c3_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c3.CheckState == CheckState.Checked)
                {
                    camera_dic[1][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 1), "fanhuien", "true");
                }
                else
                {
                    camera_dic[1][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 1), "fanhuien", "false");
                }
            }
        }

        private void c4_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c4.CheckState == CheckState.Checked)
                {
                    camera_dic[2][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 2), "fanhuien", "true");
                }
                else
                {
                    camera_dic[2][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 2), "fanhuien", "false");
                }
            }
        }

        private void c5_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c5.CheckState == CheckState.Checked)
                {
                    camera_dic[3][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 3), "fanhuien", "true");
                }
                else
                {
                    camera_dic[3][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 3), "fanhuien", "false");
                }
            }
        }

        private void c6_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c6.CheckState == CheckState.Checked)
                {
                    camera_dic[4][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 4), "fanhuien", "true");
                }
                else
                {
                    camera_dic[4][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 4), "fanhuien", "false");
                }
            }
        }

        private void c7_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c7.CheckState == CheckState.Checked)
                {
                    camera_dic[5][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 5), "fanhuien", "true");
                }
                else
                {
                    camera_dic[5][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 5), "fanhuien", "false");
                }
            }
        }

        private void c8_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c8.CheckState == CheckState.Checked)
                {
                    camera_dic[6][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 6), "fanhuien", "true");
                }
                else
                {
                    camera_dic[6][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 6), "fanhuien", "false");
                }
            }
        }

        private void c9_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c9.CheckState == CheckState.Checked)
                {
                    camera_dic[7][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 7), "fanhuien", "true");
                }
                else
                {
                    camera_dic[7][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 7), "fanhuien", "false");
                }
            }
        }

        private void c10_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c10.CheckState == CheckState.Checked)
                {
                    camera_dic[8][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 8), "fanhuien", "true");
                }
                else
                {
                    camera_dic[8][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 8), "fanhuien", "false");
                }
            }
        }

        private void c11_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c11.CheckState == CheckState.Checked)
                {
                    camera_dic[9][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 9), "fanhuien", "true");
                }
                else
                {
                    camera_dic[9][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 9), "fanhuien", "false");
                }
            }
        }

        private void c12_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c12.CheckState == CheckState.Checked)
                {
                    camera_dic[13][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "fanhuien", "true");
                }
                else
                {
                    camera_dic[13][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 13), "fanhuien", "false");
                }
            }
        }

        private void c13_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c13.CheckState == CheckState.Checked)
                {
                    camera_dic[10][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 10), "fanhuien", "true");
                }
                else
                {
                    camera_dic[10][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 10), "fanhuien", "false");
                }
            }
        }

        private void c14_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c14.CheckState == CheckState.Checked)
                {
                    camera_dic[11][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 11), "fanhuien", "true");
                }
                else
                {
                    camera_dic[11][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 11), "fanhuien", "false");
                }
            }
        }

        private void c15_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (c15.CheckState == CheckState.Checked)
                {
                    camera_dic[12][2] = "true";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 12), "fanhuien", "true");
                }
                else
                {
                    camera_dic[12][2] = "false";
                    wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, 12), "fanhuien", "false");
                }
            }
        }

        private void b4_Click(object sender, EventArgs e)
        {
            cb12.Text = "";
            cb4.Text = "";
            cb13.Text = "";
            cb14.Text = "";
            cb5.Text = "";
            cb15.Text = "";
            cb6.Text = "";
            cb16.Text = "";
            cb7.Text = "";
            cb17.Text = "";
            cb8.Text = "";
            cb18.Text = "";
            cb9.Text = "";
            cb19.Text = "";
            cb10.Text = "";
            cb20.Text = "";
            cb11.Text = "";
            cb21.Text = "";
            cb22.Text = "";
            cb23.Text = "";
            cb24.Text = "";
            cb25.Text = "";
            cb26.Text = "";
            cb27.Text = "";
            cb28.Text = "";
            t2.Text = "";
            t3.Text = "";
            t4.Text = "";
            t5.Text = "";
            t6.Text = "";
            t7.Text = "";
            t8.Text = "";
            t9.Text = "";
            t10.Text = "";
            t11.Text = "";
            t28.Text = "";
            t29.Text = "";
            t30.Text = "";
            c3.CheckState = CheckState.Unchecked;
            c4.CheckState = CheckState.Unchecked;
            c5.CheckState = CheckState.Unchecked;
            c6.CheckState = CheckState.Unchecked;
            c10.CheckState = CheckState.Unchecked;
            c9.CheckState = CheckState.Unchecked;
            c8.CheckState = CheckState.Unchecked;
            c7.CheckState = CheckState.Unchecked;
            c11.CheckState = CheckState.Unchecked;
            c12.CheckState = CheckState.Unchecked;
            c13.CheckState = CheckState.Unchecked;
            c14.CheckState = CheckState.Unchecked;
            c15.CheckState = CheckState.Unchecked;
            t39.Text = "";
            c16.CheckState = CheckState.Unchecked;
            cb29.Text = "";
            cb4_SelectedIndexChanged(null, null);
            cb5_SelectedIndexChanged(null, null);
            cb6_SelectedIndexChanged(null, null);
            cb7_SelectedIndexChanged(null, null);
            cb8_SelectedIndexChanged(null, null);
            cb9_SelectedIndexChanged(null, null);
            cb10_SelectedIndexChanged(null, null);
            cb11_SelectedIndexChanged(null, null);
            cb12_SelectedIndexChanged(null, null);
            cb13_SelectedIndexChanged(null, null);
            cb14_SelectedIndexChanged(null, null);
            cb15_SelectedIndexChanged(null, null);
            cb16_SelectedIndexChanged(null, null);
            cb17_SelectedIndexChanged(null, null);
            cb18_SelectedIndexChanged(null, null);
            cb19_SelectedIndexChanged(null, null);
            cb20_SelectedIndexChanged(null, null);
            cb21_SelectedIndexChanged(null, null);
            cb22_SelectedIndexChanged(null, null);
            cb23_SelectedIndexChanged(null, null);
            cb24_SelectedIndexChanged(null, null);
            cb25_SelectedIndexChanged(null, null);
            cb26_SelectedIndexChanged(null, null);
            cb27_SelectedIndexChanged(null, null);
            cb28_SelectedIndexChanged(null, null);
            cb29_SelectedIndexChanged(null, null);
            t39_TextChanged(null, null);
            c16_CheckedChanged(null, null);
        }

        private void b3_Click(object sender, EventArgs e)
        {
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "1", t12.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "2", t13.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "3", t14.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "4", t15.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "5", t16.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "6", t17.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "7", t18.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "8", t19.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "9", t31.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "10", t32.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "11", t33.Text);
            wdini.WriteString(ModbusRtuIniStore.ChangeSection(_linkId), "12", t34.Text);

            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "1", t20.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "2", t21.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "3", t22.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "4", t23.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "5", t24.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "6", t25.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "7", t26.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "8", t27.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "9", t35.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "10", t36.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "11", t37.Text);
            wdini.WriteString(ModbusRtuIniStore.PathSection(_linkId), "12", t38.Text);
        }

        private void SaveModbusRtuTrigVal(int cam, TextBox tb)
        {
            CommTriggerHelper.ParseTrigValInput(camera_dic[cam][6], tb.Text, camera_dic[cam]);
            wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, cam), "chufazhi1", camera_dic[cam][7]);
            wdini.WriteString(ModbusRtuIniStore.CameraSection(_linkId, cam), "chufazhi2", camera_dic[cam][8]);
            _triggerLatch[cam] = false;
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            FormOperationHelp.ShowHelp(this, "Modbus RTU");
        }

        public string GetMiddleValue(string str, string sta, string end)
        {
            Regex rg = new Regex("(?<=(" + sta + "))[.\\s\\S]*?(?=(" + end + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Match(str).Value;
        }

        /// <summary>
        /// 触发寄存器写回返回值，防止 PLC 保持非零值导致二次触发
        /// </summary>
        /// <summary>
        /// 方案切换成功回执（相机13）：切型完成后由主界面统一调用，
        /// 把该连接 UI 配置的“返回值”(camera_dic[13][1]) 写回“切换”通道(camera_dic[13][0])。
        /// 多连接整窗化（阶段7）：连接 2~4 由主窗体登记 child 整窗实例，按 linkId 路由到对应实例回执。
        /// </summary>
        public bool WriteSchemeSwitchAck(int linkId)
        {
            FormModbusRtu host = ResolveLinkHost(linkId);
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
                    c13[4] = c13[1];
                    c13[5] = c13[0];
                    WriteTriggerFanhuizhi(par.Value, c13[1], ref xuanzhong_temp, ref fins_temp);
                    c13[5] = "无"; // 写后清待写标记，避免后续 xie() 误写
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

        private void WriteTriggerFanhuizhi(string[] parValue, string fanhuizhi, ref int xuanzhong_temp, ref string fins_temp)
        {
            // ★BUG1：重连进行中（_reconnecting!=0）串口正被 ConnectClose/ConnectServer 操作，
            //   此刻写会与重连抢同一串口 → 写脏数据/抛异常。与轮询读、手动读共用同一道 _reconnecting 门控。
            if (_reconnecting != 0) return;
            lock (_ioSync)   // ★ I/O 串行：与 xie()/xie_wu()/回执写互斥，防同一串口客户端并发写
            {
            for (int j = 0; j < int.Parse(parValue[2]); j++)
            {
                if (parValue[4] == "int")
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busRtuClient.Write((int.Parse(parValue[1]) + j).ToString(), short.Parse(fanhuizhi)), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (parValue[4] == "string")
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busRtuClient.Write((int.Parse(parValue[1]) + j).ToString(), fanhuizhi), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (parValue[4] == "long" && j % 2 == 0)
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busRtuClient.Write((int.Parse(parValue[1]) + j).ToString(), int.Parse(fanhuizhi)), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (parValue[4] == "float" && j % 2 == 0)
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busRtuClient.Write((int.Parse(parValue[1]) + j).ToString(), float.Parse(fanhuizhi)), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
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

        /// <summary>强制创建窗体句柄并完成整窗初始化（程序启动/管理器恢复后台已启用连接时用，避免外部无法访问 protected CreateHandle）。
        /// 说明：创建句柄不会触发 Load（Load 只在首次 Show 触发），故建句柄后必须显式 InitializeForm，
        /// 否则隐藏的额外实例只有被点开节点才会真正启动运行时。</summary>
        public void EnsureHandleCreated()
        {
            if (!IsHandleCreated) CreateHandle();
            InitializeForm();
        }

        /// <summary>
        /// 释放本实例（多连接整窗化 阶段7：连接 2~4 在管理器中被删除/关闭时调用）。
        /// 停止本连接轮询线程、关闭底层客户端并从 CommLinkManager 注销，避免串口/端口残留占用。
        /// 连接 1（常驻单例）不走此方法，其生命周期仍随主程序退出。
        /// </summary>
        public void ReleaseInstance()
        {
            try { StopPolling(); } catch { }
            foreach (var rt in _runtimes.Values)
                try { if (rt != null) rt.Close(); } catch { }
            try
            {
                var mgr = AppHost.Services.Resolve<CommLinkManager>();
                if (mgr != null) mgr.Unregister(_linkId);
            }
            catch { }
        }

        /// <summary>
        /// 阶段 6（连接设备管理器增删多连接）：按 ini [modbusrtuN] 当前配置重建并启动连接 N 的运行时。
        /// 先幂等停掉旧的，再按配置新起；modbusrtu_en=false 或段不存在时仅停止不启动。
        /// 连接 1 由窗体自身管理，不受影响。
        /// </summary>
        public async void ApplyExtraLinkRuntime(int linkId)
        {
            if (linkId < 2 || linkId > ModbusRtuIniStore.MaxLinks) return;
            StopExtraLinkRuntime(linkId);
            var cfg = ModbusRtuIniStore.Load(wdini, linkId);
            if (cfg == null || !cfg.ModbusEn) return;

            var link = new ModbusRtuLink();
            link.LinkId = cfg.LinkId;
            link.Name = cfg.Name;
            link.PortName = cfg.PortName;
            link.BaudRate = int.TryParse(cfg.BaudRate, out int baud) ? baud : 9600;
            link.DataBits = int.TryParse(cfg.DataBits, out int db) ? db : 8;
            link.StopBitsValue = int.TryParse(cfg.StopBits, out int sb) ? sb : 1;
            link.ParityName = cfg.Parity;
            link.Station = byte.TryParse(cfg.Station, out byte st) ? st : (byte)1;
            await link.ConnectAsync(null);   // 2026-09-06：异步建链，不阻塞 UI
            var rt = new ModbusRtuRuntime(link, this)
            {
                LinkId = cfg.LinkId,
                Config = cfg,
                Context = new ModbusRtuLinkContext(cfg, this, link)
            };
            rt.Start();
            _runtimes[cfg.LinkId] = rt;
            AppHost.Services.Resolve<CommLinkManager>().Register(rt);
        }

        /// <summary>停止并移除指定连接 N 的运行时（管理器“删除连接”时调用）。</summary>
        public void StopExtraLinkRuntime(int linkId)
        {
            if (linkId < 2) return;
            ModbusRtuRuntime old;
            if (!_runtimes.TryGetValue(linkId, out old) || old == null) return;
            try { old.Close(); } catch { }
            _runtimes.Remove(linkId);
            try { AppHost.Services.Resolve<CommLinkManager>().Unregister(linkId); } catch { }
        }

        /// <summary>指定连接 N 的后台运行时是否已在运行（管理器按需补启动用）。</summary>
        public bool IsExtraLinkRunning(int linkId)
        {
            ModbusRtuRuntime rt;
            return _runtimes.TryGetValue(linkId, out rt) && rt != null;
        }

        void Fins_duxie()
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
                                        for (int j = 0; j < blkChangdu; j++)
                                        {
                                            if (par.Value[4] == "int")
                                            {

                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取short变量
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busRtuClient.ReadInt16((int.Parse(par.Value[1]) + j).ToString())), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "string")
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busRtuClient.ReadString((int.Parse(par.Value[1]) + j).ToString(), 1)), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "long" && j % 2 == 0)
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busRtuClient.ReadInt32((int.Parse(par.Value[1]) + j).ToString())), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "float" && j % 2 == 0)
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busRtuClient.ReadFloat((int.Parse(par.Value[1]) + j).ToString())), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
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
                                                                qiehuanzhong = 1;
                                                                SelectionChangedEventArgs E = new SelectionChangedEventArgs(shuju_temp, pap.Key.ToString(), "0", _linkId);
                                                                getData(this, E);
                                                            }
                                                            else
                                                            {
                                                                // ★C7 修复：路径不存在时补写失败回执——原实现连 [4] 都不写、
                                                                //   更不置 [5] 待写槽，PLC 侧永远等不到这枚 NG 回执（死等超时）。
                                                                if (camera_dic.ContainsKey(13) && !camera_dic[13][1].Contains("无") && !camera_dic[13][0].Contains("无"))
                                                                {
                                                                    camera_dic[13][4] = camera_dic[13][1];
                                                                    camera_dic[13][5] = camera_dic[13][0];
                                                                    xie(camera_dic[13][4]);
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
                                                                _triggerLatch[pap.Key] = true;
                                                                SelectionChangedEventArgs E = new SelectionChangedEventArgs(dataVal, pap.Key.ToString(), pap.Key.ToString(), _linkId);
                                                                getData(this, E);
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
                        RateLimitedLog.Throttled("[ModbusRTU-连接" + _linkId + "-Fins_duxie] 轮询读异常", m => MsgErroeLog.WriteLog(m), ex, 5000);
                    }
            }
        }

        private void TryAutoReconnect()
        {
            if (!fins_en || IsDisposed) return;
            if (System.Threading.Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0) return;
            Log("Modbus RTU 通讯异常，后台自动重连中...");
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    bool ok;
                    lock (_ioSync)   // ★全 I/O 单锁串行：重连的 ConnectClose/ConnectServer 与读/写互斥，杜绝抢同一串口
                    {
                        ok = PerformReconnectCore();
                    }
                    if (ok)
                    {
                        Log("Modbus RTU 自动重连成功");
                        SafeApplyConnectedUiState();
                    }
                }
                catch (Exception ex)
                {
                    Log("Modbus RTU 自动重连失败:" + ex.Message);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref _reconnecting, 0);
                }
            });
        }

        /// <summary>后台线程执行，仅 Close+Open，不访问 UI 控件。走 _rtuLink.Reconnect 以带 COM 互斥防呆。</summary>
        private bool PerformReconnectCore()
        {
            if (busRtuClient == null) return false;
            return _rtuLink.Reconnect();
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
                        userControlCurve1.ReadWriteNet = busRtuClient;
                }));
            }
            catch { }
        }

        /// <summary>
        /// 阶段 7：把检测结果回写到指定连接（2~4）<b>自己</b>配置的反馈数据块。
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

        public void xie(string value)
        {
            lock (_ioSync)   // ★ I/O 串行：与 WriteTriggerFanhuizhi/xie_wu 互斥，防同一串口客户端并发写
            {
            try
            {
                if (!chushihua || fins_dic.Count == 0) return;
                // ★BUG1：重连进行中不写，避免与 ConnectClose/ConnectServer 抢同一串口（与轮询读/手动读共用 _reconnecting 门控）。
                if (_reconnecting != 0) return;

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
                                // 逗号分隔 → short数组, FC16批量写
                                string[] parts = dataVal.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                short[] vals = new short[parts.Length];
                                for (int i = 0; i < parts.Length; i++)
                                    vals[i] = (short)Math.Round(double.Parse(parts[i].Trim()));
                                writeAllOk = DemoUtils.WriteResultRender1(() => busRtuClient.Write(addr_start.ToString(), vals), addr_start.ToString(), out fins_temp);
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
                                writeAllOk = DemoUtils.WriteResultRender1(() => busRtuClient.Write(addr_start.ToString(), vals), addr_start.ToString(), out fins_temp);
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
                                writeAllOk = DemoUtils.WriteResultRender1(() => busRtuClient.Write(addr_start.ToString(), vals), addr_start.ToString(), out fins_temp);
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
                                    if (!DemoUtils.WriteResultRender1(() => busRtuClient.Write((addr_start + j).ToString(), parts[j].Trim()), (addr_start + j).ToString(), out fins_temp))
                                        writeAllOk = false;
                                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                }
                            }
                            break;
                        }
                        // ★H3 修复：写成功才清槽；失败保留"待写"，下次 xie() 自动重试。
                        if (writeAllOk)
                            pat.Value[5] = "无";
                        else
                            LogXieRetry("槽 " + pat.Value[5] + " → " + fins_temp);
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
                Log(ex.Message + "modbusrtu");
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

        // ★H3：回写失败保留待写会随每次 xie() 重试，日志按 5 秒限流避免刷屏
        private int _lastXieRetryLogTick;
        private void LogXieRetry(string detail)
        {
            int now = Environment.TickCount;
            if (unchecked(now - _lastXieRetryLogTick) < 5000) return;
            _lastXieRetryLogTick = now;
            try { Log("回写失败(不清槽，下次 xie 自动重试)： " + detail); } catch { }
        }
        /// <summary>★当前全工程无调用点（死代码）：免握手的 Modbus-RTU 极速写。
        /// 保留为功能储备；确认不再启用后可整体删除。</summary>
        public void xie_wu(string value)
        {
            lock (_ioSync)   // ★ I/O 串行：免握手极速写与普通写互斥，防同一串口客户端并发写
            {
            try
            {
                if (!chushihua || fins_dic.Count == 0) return;
                // ★BUG1：重连进行中不写，避免与 ConnectClose/ConnectServer 抢同一串口（与轮询读/手动读共用 _reconnecting 门控）。
                if (_reconnecting != 0) return;

                foreach (var pat in camera_dic)
                {
                    if (pat.Value[5] != "无")
                    {
                        foreach (var par in fins_dic)
                        {
                            if (pat.Value[5] == par.Value[0])
                            {
                                int addr_start = int.Parse(par.Value[1]);
                                string fmt = par.Value[4];

                                if (fmt == "int")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    short[] vals = new short[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = (short)Math.Round(double.Parse(parts[i].Trim()));
                                    busRtuClient.Write(addr_start.ToString(), vals);
                                    for (int j = 0; j < vals.Length; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, "已发送");
                                    }
                                }
                                else if (fmt == "long")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    int[] vals = new int[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = (int)Math.Round(double.Parse(parts[i].Trim()));
                                    busRtuClient.Write(addr_start.ToString(), vals);
                                    for (int j = 0; j < vals.Length * 2; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, "已发送");
                                    }
                                }
                                else if (fmt == "float")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    float[] vals = new float[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = float.Parse(parts[i].Trim());
                                    busRtuClient.Write(addr_start.ToString(), vals);
                                    for (int j = 0; j < vals.Length * 2; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, "已发送");
                                    }
                                }
                                else if (fmt == "string")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    for (int j = 0; j < parts.Length; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        busRtuClient.Write((addr_start + j).ToString(), parts[j].Trim());
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, "已发送");
                                    }
                                }
                                break;
                            }
                        }
                        pat.Value[5] = "无";
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message + "modbusrtu_xie_wu");
            }
            }
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
            if (t12.Text == aa)
            {
                zifu = aa;
                lujing = t20.Text;
                return 1;
            }
            else if (t13.Text == aa)
            {
                zifu = aa;
                lujing = t21.Text;
                return 1;
            }
            else if (t14.Text == aa)
            {
                zifu = aa;
                lujing = t22.Text;
                return 1;
            }
            else if (t15.Text == aa)
            {
                zifu = aa;
                lujing = t23.Text;
                return 1;
            }
            else if (t16.Text == aa)
            {
                zifu = aa;
                lujing = t24.Text;
                return 1;
            }
            else if (t17.Text == aa)
            {
                zifu = aa;
                lujing = t25.Text;
                return 1;
            }
            else if (t18.Text == aa)
            {
                zifu = aa;
                lujing = t26.Text;
                return 1;
            }
            else if (t19.Text == aa)
            {
                zifu = aa;
                lujing = t27.Text;
                return 1;
            }
            else if (t31.Text == aa)
            {
                zifu = aa;
                lujing = t35.Text;
                return 1;
            }
            else if (t32.Text == aa)
            {
                zifu = aa;
                lujing = t36.Text;
                return 1;
            }
            else if (t33.Text == aa)
            {
                zifu = aa;
                lujing = t37.Text;
                return 1;
            }
            else if (t34.Text == aa)
            {
                zifu = aa;
                lujing = t38.Text;
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "abcd", comboBox2.Text);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "baudRate", textBox2.Text);
            }
        }

        private void textBox16_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "dataBits", textBox16.Text);
            }
        }

        private void textBox17_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "stopBits", textBox17.Text);
            }
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "station", textBox15.Text);
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "PortName", comboBox3.Text);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusRtuIniStore.ConnSection(_linkId), "Parity", comboBox1.Text);
            }
        }

        private void dataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            CommGridHelper.RowPrePaintZebra(sender, e);
        }
    }
}

