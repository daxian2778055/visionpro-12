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
using demo;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Sockets;
using WindowsFormsApplication1.Core.Infrastructure;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1
{
    public partial class FormModbus : Form, IPlcOutputWriter, ICommRuntimeHost, ICommLinkContext
    {
        public Label labelFallbackHint = new Label();

        // 多实例整窗化（阶段 7，仿 FormOmron）：每个实例 = 一个连接的完整调试窗体。
        private int _linkId = 1;
        public int LinkId { get { return _linkId; } }

        // 数据块/相机段族的段名后缀（连接 1 = "_modbustcp"，连接 N = "_modbustcpN"），供 CommGridHelper 段级操作使用。
        private string DataSuffix { get { return "_modbustcp" + (_linkId == 1 ? "" : _linkId.ToString()); } }

        public FormModbus() : this(1) { }

        public FormModbus(int linkId)
        {
            _linkId = linkId;
            // ★D1 修复：把连接号同步给底层连接对象。
            // 旧实现只设了窗体的 _linkId，而 Fins_duxie 派发触发事件时用的是 _modbusLink.LinkId（恒为默认值 1），
            // 连接 2~4 的事件里 LinkId 会是错的（目前被 FormCommManager 桥接层用捕获的 link 掩盖）。
            _modbusLink.LinkId = linkId;
            wdini.ReadINIFile(AppDomain.CurrentDomain.BaseDirectory + "//test.ini");
            InitializeComponent( );

#if DEBUG
            // 阶段 2-A 冒烟验证：确认 ModbusTcpIniStore 能正确加载既有 test.ini（只读、不改写、异常忽略）。
            try
            {
                var _dbgLinks = ModbusTcpIniStore.LoadAll(wdini);
                System.Diagnostics.Debug.WriteLine("[ModbusTcpIniStore] DEBUG load: links=" + _dbgLinks.Count
                    + ", link1 blocks=" + (_dbgLinks.Count > 0 ? _dbgLinks[0].DataBlocks.Count : 0)
                    + ", cameras=" + (_dbgLinks.Count > 0 ? _dbgLinks[0].CameraBindings.Count : 0));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ModbusTcpIniStore] DEBUG load error: " + ex.Message);
            }
#endif

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
        // 多实例改造 阶段1：连接所有权收口到 ModbusTcpLink（一个实例 = 一个 Modbus TCP 连接）。
        // 下面这个属性是过渡用的“直通门”，让原有数十处 busTcpClient 读写代码一行不改继续工作。
        private ModbusTcpLink _modbusLink = new ModbusTcpLink();
        private ModbusTcpNet busTcpClient { get { return _modbusLink.Client; } set { _modbusLink.Client = value; } }

        // 多实例改造 阶段 2-B：轮询线程生命周期收口到 ModbusTcpRuntime（单实例，行为不变）。
        private ModbusTcpRuntime _runtime;
        // 多实例改造 阶段 2-C（激活）：全部连接的运行时集合，StopPolling 时统一停止；链接 1 仍保留 _runtime 引用以兼容。
        private Dictionary<int, ModbusTcpRuntime> _runtimes = new Dictionary<int, ModbusTcpRuntime>();

        // 多连接 child host（阶段 7，仿 FormOmron）：连接 1 主窗体登记各连接整窗实例，对外 API 按 linkId 路由到对应 child。
        // ★A4 修复：本字典会被 UI 线程（注册/注销）与轮询线程（路由查询）并发访问，必须加锁（对齐 FormOmron）。
        private readonly Dictionary<int, FormModbus> _childLinks = new Dictionary<int, FormModbus>();
        private readonly object _childLinksSync = new object();

        /// <summary>对外暴露本窗体持有的连接实例，供 CommLinkManager 登记与多实例调度。</summary>
        public ModbusTcpLink Link { get { return _modbusLink; } }

        // ===== ICommRuntimeHost 实现（阶段 2-B）：引擎通过它回调轮询循环、读写停止标志与线程句柄 =====
        public bool StopRequested { get { return _stopPolling; } set { _stopPolling = value; } }
        public Thread PollThread { get { return fins_duxie; } set { fins_duxie = value; } }
        public void RunPollLoop() { Fins_duxie(); }

        // ===== ICommLinkContext 实现（阶段 2-C per-link）：供 ModbusTcpRuntime.PollLoop 按连接读取配置与回调。
        // 当前唯一连接（链接 1）的运行时 Context 为 null，仍走原 Fins_duxie，本实现处于休眠——
        // 仅作为多连接并联时的“标准上下文”样板；下列 getter 直接返回本窗体现有字段（实时值），与原算法一致。
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
            MsgErroeLog.WriteLog("[ModbusTCP-连接" + _linkId + "] " + message);
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
        private ushort xieWuTransactionId = 0;
        public delegate void GetSeletionData(object Sender, SelectionChangedEventArgs e);
        public event GetSeletionData getData;
        private int x = 999;
        private int y = 999;
        Dictionary<string, string[]> fins_dic = new Dictionary<string, string[]>();
        public Dictionary<int, string[]> camera_dic = new Dictionary<int, string[]>();
        Dictionary<int, int[]> fins_name = new Dictionary<int, int[]>();
        Dictionary<int, int[]> fins_data = new Dictionary<int, int[]>();
        Dictionary<int, byte[]> fins_value = new Dictionary<int, byte[]>();

        /// <summary>
        /// Modbus-TCP 相机归属防呆：改相机触发/反馈下拉前调用。
        /// 若该相机已被本协议其它连接占用，回滚下拉到原值并提示，返回 false（本次不保存）；
        /// 空闲则返回 true（放行保存）。
        /// </summary>
        /// <param name="cameraNo">相机编号 1..13</param>
        /// <param name="newVal">下拉新选值</param>
        /// <param name="slot">camera_dic[cameraNo] 对应下标：0=触发(chufa)，3=反馈(fankui)</param>
        /// <param name="cb">触发该改动的下拉控件，用于回滚 Text</param>
        /// <returns>true=放行；false=被占用，已回滚并提示</returns>
        private bool GuardCamera(int cameraNo, string newVal, int slot, System.Windows.Forms.ComboBox cb)
        {
            // 空值（清空）不占资源，其它连接即使占着本连接也可选择“无”
            if (!CommCameraGuard.IsBoundValue(newVal)) return true;
            int owner = CommCameraGuard.FindOwnerModbusTcp(wdini, _linkId, cameraNo);
            if (owner == 0) return true;
            string old = camera_dic[cameraNo][slot];
            cb.Text = old;             // 回滚到原值（新选项可能来自其它连接，不能从 Items 里删）
            string ownerName = CommCameraGuard.OwnerDisplayNameModbusTcp(wdini, owner);
            System.Windows.Forms.MessageBox.Show(
                CommCameraGuard.OccupiedMsg("ModbusTCP", cameraNo, owner, ownerName),
                "相机占用冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
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
        // ★P2：切型锁会被轮询线程（置 1）与线程池线程（Form1 的 Task.Run finally 置 0）并发读写，
        // 必须是 volatile，否则极端情况下轮询线程长期读到过期值 → 切型"锁死"或锁不生效。
        public volatile int qiehuanzhong = 0;
       
        ErrorLog MsgErroeLog = new ErrorLog();
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
            /// 事件来源的连接标识（多实例改造 阶段3）。
            /// 用于区分“这条触发数据是哪个连接收来的”，后续据此路由与回写反馈。
            /// 当前只有 1 个连接，恒为 1，且尚无任何代码读取它，因此行为与改造前完全一致。
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

        // 防止“管理器预建隐藏窗显式初始化”后，首次 Show 又触发 Load 重复建链。
        private bool _formLoaded = false;

        private void FormSiemens_Load( object sender, EventArgs e )
        {
            InitializeForm();
        }

        /// <summary>整窗初始化（与 FINS / 无协议同构）：读 [modbustcp]/[modbustcpN] 段族填充界面，
        /// 并创建/启动本连接运行时（窗体自身 _modbusLink + Context=null，与连接 1 完全同构）。
        /// 关键：WinForms 的 Load 只在首次 Show 时触发；管理器 Attach/添加时预建的隐藏窗（连接 2~4）
        /// 必须显式调用本方法，否则运行时永不创建、连接 2~4 完全不轮询。</summary>
        internal void InitializeForm()
        {
            if (_formLoaded) return;
            _formLoaded = true;
            panel2.Enabled = false;

            comboBox1.SelectedIndex = 0;

            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            checkBox3.CheckedChanged += CheckBox3_CheckedChanged;

            Language( Program.Language );

            // 多实例整窗化（阶段 7，仿 FormOmron）：每个窗体实例只承载 _linkId 这一个连接。
            // 连接 2~4 由 FormCommManager 各自 new FormModbus(link) 整窗实例承载并嵌入，不再由主窗 LoadAll 后台启动。
            // 启动前相机关卡复查：本连接（连接 1~4 等价）若绑定了被同协议其它连接占用的相机，
            // 则阻止本连接自动启动（不建链、不轮询、不标记就绪），但仍加载界面供查看并修正。
            // 连接 1 不能像连接 2~4 那样在 FormCommManager 提前拦截，故在此统一兜底。
            bool commBlocked = _linkId >= 1 && ModbusTcpIniStore.Load(wdini, _linkId) != null
                && (CommCameraGuard.CheckStartupModbusTcp(wdini, _linkId) != null);

            var cfg = ModbusTcpIniStore.Load(wdini, _linkId);
            if (cfg != null && !_runtimes.ContainsKey(_linkId) && !commBlocked)
            {
                var rt = new ModbusTcpRuntime(_modbusLink, this) { LinkId = _linkId, Config = cfg, Context = null };
                rt.Start();
                _runtimes[_linkId] = rt;
                AppHost.Services.Resolve<CommLinkManager>().Register(rt);
            }
            _runtime = _runtimes.ContainsKey(_linkId) ? _runtimes[_linkId] : null;
            decimal xuanzhong_temp = 0;

           // comboBox1.DataSource = HslCommunication.BasicFramework.SoftBasic.GetEnumValues<HslCommunication.Core.DataFormat>();
           // comboBox1.SelectedItem = HslCommunication.Core.DataFormat.CDAB;
           

            for (int i = 0; i < 10; i++)
            {
                dataGridView1.Rows.Add();
            }
            _gridUi = new CommGridUiSink(dataGridView1);
            CommGridHelper.ApplyDataTabChrome(tabPage1, dataGridView1, button5, button6);
            CommGridHelper.StyleClearButton(button8);
            for (int i = 0; i < 50; i++)
            {
                fins_data.Add(i, new int[] { i % 10, i / 10 * 2 + 1 });
                fins_name.Add(i, new int[] { i % 10, i / 10 * 2 });
                fins_value.Add(i, new byte[] { 0x00, 0x00 });
            }

            fins_lunxunen = bool.Parse(wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "modbus_lunxunen", "false"));
            fins_en = bool.Parse(wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "modbus_en", "false"));
            address_qishi = decimal.Parse(wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "qishi", "0"));
            address_length = decimal.Parse(wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "zongchang", "1"));
            lunxun_time = decimal.Parse(wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "lunxun_time", "20"));
            numericUpDown1.Value = address_qishi;
            numericUpDown2.Value = address_length;
            numericUpDown3.Value = lunxun_time;

            comboBox1.Text = wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "abcd", "CDAB").Replace("\0", "");
            textBox1.Text = wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "ip", "127.0.0.1").Replace("\0", "");
            textBox2.Text = wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "port", "9600").Replace("\0", "");
            textBox15.Text = wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "cell", "0").Replace("\0", "");
            //textBox15.Text = wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "local", "192").Replace("\0", "");
            if (fins_lunxunen)
            {
                checkBox4.CheckState = CheckState.Checked;
            }
            if (fins_en && !commBlocked)
            {
                checkBox2.CheckState = CheckState.Checked;
                // ★ 2026-09-12：移除此处同步 button1_Click，避免与下方 Task.Run 延迟建链重复建链（重复建链会造成闪断）。
                //   ModbusTCP 连接1 启动建链统一由本方法末尾 Task.Run 异步执行一次（对齐 FINS 的修复）。
            }
            geshu = int.Parse(wdini.ReadString(ModbusTcpIniStore.ConnSection(_linkId), "geshu", "0"));
            if (geshu > 0)
            {
                for (int i = 0; i < geshu; i++)
                {
                    fins_mingcheng = wdini.ReadString(ModbusTcpIniStore.BlockSection(_linkId, i + 1), "name", "").Replace("\0", "");
                    fins_qishi = decimal.Parse(wdini.ReadString(ModbusTcpIniStore.BlockSection(_linkId, i + 1), "qishi", "0"));
                    fins_length = decimal.Parse(wdini.ReadString(ModbusTcpIniStore.BlockSection(_linkId, i + 1), "changdu", "0"));
                    ABCD = wdini.ReadString(ModbusTcpIniStore.BlockSection(_linkId, i + 1), "gaodiwei", "触发").Replace("\0", "");
                    fins_style = wdini.ReadString(ModbusTcpIniStore.BlockSection(_linkId, i + 1), "geshi", "int").Replace("\0", "");
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
                string temp_jian = wdini.ReadString(ModbusTcpIniStore.CameraSection(_linkId, i + 1), "chufa", " ").Replace("\0", "");
                camera_dic.Add(i + 1, new string[] { temp_jian, wdini.ReadString(ModbusTcpIniStore.CameraSection(_linkId, i + 1), "fanhuizhi", "0").Replace("\0", ""), wdini.ReadString(ModbusTcpIniStore.CameraSection(_linkId, i + 1), "fanhuien", "false").Replace("\0", ""), wdini.ReadString(ModbusTcpIniStore.CameraSection(_linkId, i + 1), "fankui", "0").Replace("\0", ""), "无", "无", wdini.ReadString(ModbusTcpIniStore.CameraSection(_linkId, i + 1), "chukufangshi", "相等").Replace("\0", ""), wdini.ReadString(ModbusTcpIniStore.CameraSection(_linkId, i + 1), "chufazhi1", "").Replace("\0", ""), wdini.ReadString(ModbusTcpIniStore.CameraSection(_linkId, i + 1), "chufazhi2", "").Replace("\0", "") });

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
            comboBox5.Items.Add(camera_dic[1][0]);
            comboBox5.Text = camera_dic[1][0];
            textBox42.Text = camera_dic[1][1];
            if (camera_dic[1][2] == "true")
                checkBox14.CheckState = CheckState.Checked;
            comboBox6.Items.Add(camera_dic[1][3]);
            comboBox6.Text = camera_dic[1][3];

            comboBox8.Items.Add(camera_dic[2][0]);
            comboBox8.Text = camera_dic[2][0];
            textBox17.Text = camera_dic[2][1];
            if (camera_dic[2][2] == "true")
                checkBox13.CheckState = CheckState.Checked;
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

            comboBox22.Items.Add(camera_dic[10][0]);
            comboBox22.Text = camera_dic[10][0];
            textBox43.Text = camera_dic[10][1];
            if (camera_dic[10][2] == "true")
                checkBox15.CheckState = CheckState.Checked;
            comboBox23.Items.Add(camera_dic[10][3]);
            comboBox23.Text = camera_dic[10][3];

            comboBox24.Items.Add(camera_dic[11][0]);
            comboBox24.Text = camera_dic[11][0];
            textBox44.Text = camera_dic[11][1];
            if (camera_dic[11][2] == "true")
                checkBox16.CheckState = CheckState.Checked;
            comboBox25.Items.Add(camera_dic[11][3]);
            comboBox25.Text = camera_dic[11][3];

            comboBox26.Items.Add(camera_dic[12][0]);
            comboBox26.Text = camera_dic[12][0];
            textBox45.Text = camera_dic[12][1];
            if (camera_dic[12][2] == "true")
                checkBox17.CheckState = CheckState.Checked;
            comboBox27.Items.Add(camera_dic[12][3]);
            comboBox27.Text = camera_dic[12][3];

            textBox54.Text = camera_dic[13][1];
            if (camera_dic[13][2] == "true")
                checkBox18.CheckState = CheckState.Checked;
            comboBox29.Items.Add(camera_dic[13][3]);
            comboBox29.Text = camera_dic[13][3];

            comboBox21.Items.Add(camera_dic[13][0]);
            comboBox21.Text = camera_dic[13][0];
            textBox41.Text = camera_dic[13][1];
            if (camera_dic[13][2] == "true")
                checkBox12.CheckState = CheckState.Checked;

            textBox40.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "1", "");
            textBox39.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "2", "");
            textBox38.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "3", "");
            textBox37.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "4", "");
            textBox36.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "5", "");
            textBox35.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "6", "");
            textBox34.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "7", "");
            textBox33.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "8", "");
            textBox46.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "9", "");
            textBox47.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "10", "");
            textBox48.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "11", "");
            textBox49.Text = wdini.ReadString(ModbusTcpIniStore.ChangeSection(_linkId), "12", "");
            textBox25.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "1", "");
            textBox26.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "2", "");
            textBox28.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "3", "");
            textBox27.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "4", "");
            textBox32.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "5", "");
            textBox31.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "6", "");
            textBox30.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "7", "");
            textBox29.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "8", "");
            textBox50.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "9", "");
            textBox51.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "10", "");
            textBox52.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "11", "");
            textBox53.Text = wdini.ReadString(ModbusTcpIniStore.PathSection(_linkId), "12", "");

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
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, capturedCam), "chukufangshi", modeBox.Text);
                    _triggerLatch[capturedCam] = false;
                };
                modeBox.SelectedIndexChanged += (s, ev) => applyTrigMode();
                modeBox.TextChanged += (s, ev) => applyTrigMode();
                valBox.TextChanged += (s, ev) => SaveModbusTrigVal(capturedCam, valBox);
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
        }


        private void Language( int language )
        {
            if (language == 2)
            {
                Text = "Modbus Tcp Read Demo";

                label1.Text = "Ip:";
                label3.Text = "Port:";
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
                button24.Text = "w-coil";
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

                button3.Text = "Pressure test, r/w 3,000s";

                label4.Text = "Account";
                label2.Text = "Pwd";
                label5.Text = "When the server is a server built by hsl, login with account name and password is supported.";

            }
        }
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (busTcpClient != null)
            {
                HslCommunication.Core.DataFormat fmt;
                if (TryGetSelectedDataFormat(out fmt))
                    busTcpClient.DataFormat = fmt;
            }
        }

        /// <summary>
        /// 把界面“数据格式”下拉框的选中项转成 DataFormat。
        /// 返回 false 表示未选中 0~3（此时保持原值不变），与原先 switch 无 default 的行为一致。
        /// 建链（ModbusTcpLink.Connect）与界面切换（ComboBox1_SelectedIndexChanged）共用此逻辑。
        /// </summary>
        private bool TryGetSelectedDataFormat(out HslCommunication.Core.DataFormat fmt)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0: fmt = HslCommunication.Core.DataFormat.ABCD; return true;
                case 1: fmt = HslCommunication.Core.DataFormat.BADC; return true;
                case 2: fmt = HslCommunication.Core.DataFormat.CDAB; return true;
                case 3: fmt = HslCommunication.Core.DataFormat.DCBA; return true;
                default: fmt = HslCommunication.Core.DataFormat.ABCD; return false;
            }
        }
        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (busTcpClient != null)
            {
                busTcpClient.IsStringReverse = checkBox3.Checked;
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
            // 连接
            System.Net.IPAddress address;
            if (!System.Net.IPAddress.TryParse( textBox1.Text, out address ))
            {
                Log( DemoUtils.IpAddressInputWrong );
                return;
            }

            int port;
            if(!int.TryParse(textBox2.Text,out port))
            {
                Log( DemoUtils.PortInputWrong );
                return;
            }

            byte station;
            if(!byte.TryParse(textBox15.Text,out station))
            {
                Log( "Station input is wrong！" );
                return;
            }

            // 多实例改造 阶段1：连接参数收口到 ModbusTcpLink。
            // 等价于原先：关旧连接 → new ModbusTcpNet → 逐项赋值 → 设数据格式 → 设字符串反转
            _modbusLink.Ip = textBox1.Text;
            _modbusLink.Port = port;
            _modbusLink.Station = station;
            _modbusLink.AddressStartWithZero = checkBox1.Checked;
            _modbusLink.UserName = textBox14.Text;
            _modbusLink.Password = textBox12.Text;
            _modbusLink.IsStringReverse = checkBox3.Checked;
            HslCommunication.Core.DataFormat fmt;
            bool hasFmt = TryGetSelectedDataFormat(out fmt);   // 原先由 ComboBox1_SelectedIndexChanged 完成

            // ★P4：手动建链与后台自动重连互斥。否则两条线程会同时 Close/Open 同一个 Hsl 客户端，
            // 可能导致其内部状态错乱（现场表现为"重连成功但读全 Failed"）。
            if (Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0)
            {
                Log("正在进行后台自动重连，请稍后再试。");
                return;
            }

            button1.Enabled = false;   // 2026-09-06：防重复点击
            _connecting = true;
            try
            {
                var fmtParam = hasFmt ? (HslCommunication.Core.DataFormat?)fmt : null;
                OperateResult connect = await _modbusLink.ConnectAsync(fmtParam);
                if (connect.IsSuccess)
                {
                    xieWuTransactionId = 0;

                    Log( HslCommunication.StringResources.Language.ConnectedSuccess );
                    button2.Enabled = true;
                    button1.Enabled = false;
                    panel2.Enabled = true;

                    userControlCurve1.ReadWriteNet = busTcpClient;
                }
                else
                {
                    Log( HslCommunication.StringResources.Language.ConnectedFailed + connect.Message );
                    button1.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Log( ex.Message );
                button1.Enabled = true;
            }
            finally
            {
                _connecting = false;
                Interlocked.Exchange(ref _reconnecting, 0);   // ★P4：释放与自动重连的互斥
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            // 断开连接
            _modbusLink.Close( );
            button2.Enabled = false;
            button1.Enabled = true;
            panel2.Enabled = false;
        }
        
        #endregion

        #region 单数据读取测试


        private void button_read_bool_Click( object sender, EventArgs e )
        {
            // 读取bool变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadCoil( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button4_Click_1( object sender, EventArgs e )
        {
            // 读取离散输入bool变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadDiscrete( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_short_Click( object sender, EventArgs e )
        {
            // 读取short变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadInt16( textBox3.Text ), textBox3.Text, textBox4 ));

            // 这一行是测试读取short数组的代码，忽略就行
            // short[] values = busTcpClient.ReadInt16( "100", 2 ).Content;
        }

        private void button_read_ushort_Click( object sender, EventArgs e )
        {
            // 读取ushort变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadUInt16( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_int_Click( object sender, EventArgs e )
        {
            // 读取int变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadInt32(  textBox3.Text ), textBox3.Text, textBox4 ));
        }
        private void button_read_uint_Click( object sender, EventArgs e )
        {
            // 读取uint变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadUInt32( textBox3.Text ), textBox3.Text, textBox4 ));
        }
        private void button_read_long_Click( object sender, EventArgs e )
        {
            // 读取long变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadInt64( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_ulong_Click( object sender, EventArgs e )
        {
            // 读取ulong变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadUInt64( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_float_Click( object sender, EventArgs e )
        {
            // 读取float变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadFloat( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_double_Click( object sender, EventArgs e )
        {
            // 读取double变量
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadDouble( textBox3.Text ), textBox3.Text, textBox4 ));
        }

        private void button_read_string_Click( object sender, EventArgs e )
        {
            // 读取字符串
            ManualRead(() => DemoUtils.ReadResultRender( busTcpClient.ReadString( textBox3.Text , ushort.Parse( textBox5.Text ) ), textBox3.Text, textBox4 ));
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
                DemoUtils.WriteResultRender( busTcpClient.WriteCoil( textBox8.Text, bool.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , short.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , ushort.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , int.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , uint.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , long.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , ulong.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , float.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , double.Parse( textBox7.Text ) ), textBox8.Text );
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
                DemoUtils.WriteResultRender( busTcpClient.Write( textBox8.Text , textBox7.Text ), textBox8.Text );
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
            DemoUtils.BulkReadRenderResult( busTcpClient, textBox6, textBox9, textBox10 );
        }



        #endregion

        #region 报文读取测试


        private void button26_Click( object sender, EventArgs e )
        {
            OperateResult<byte[]> read = busTcpClient.ReadFromCoreServer( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) );
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

        #region 压力测试

        private void button4_Click( object sender, EventArgs e )
        {
            PressureTest2( );
        }

        private int thread_status = 0;
        private int failed = 0;
        private DateTime thread_time_start = DateTime.Now;
        // 压力测试，开3个线程，每个线程进行读写操作，看使用时间
        private void PressureTest2( )
        {
            thread_status = 3;
            failed = 0;
            thread_time_start = DateTime.Now;
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            button3.Enabled = false;
        }

        private void thread_test2( )
        {
            int count = 500;
            while (count > 0)
            {
                if (!busTcpClient.Write( "100", (short)1234 ).IsSuccess) failed++;
                if (!busTcpClient.ReadInt16( "100" ).IsSuccess) failed++;
                count--;
            }
            thread_end( );
        }

        private void thread_end( )
        {
            if (Interlocked.Decrement( ref thread_status ) == 0)
            {
                // 执行完成
                Invoke( new Action( ( ) =>
                {
                    button3.Enabled = true;
                    MessageBox.Show( "Spend：" + (DateTime.Now - thread_time_start).TotalSeconds + Environment.NewLine + " Read Failed：" + failed );
                } ) );
            }
        }
        
        #endregion

        #region Test Function


        private void Test1()
        {
            OperateResult<bool[]> read = busTcpClient.ReadCoil( "100", 10 );
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
            OperateResult write = busTcpClient.WriteCoil( "100", values );
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
        // ★BUG2：与 chushihua 同款。UI 线程（勾选"通讯使能"）写、轮询线程（Fins_duxie / TryAutoReconnect）读，
        //   必须 volatile 保证跨线程可见性，否则极端下轮询线程长期读旧值 → "勾了使能但轮询空转"。
        public volatile bool fins_en = false;
        decimal lunxun_time = 0;
        // ★P2：线程池线程（InitializeForm 末尾 Task.Run）写 true，轮询线程（Fins_duxie）读；加 volatile 保证跨线程可见性
        public volatile bool chushihua = false;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _triggerLatch = new System.Collections.Concurrent.ConcurrentDictionary<int, bool>();
        private int _commFailCount = 0;
        private int _reconnecting = 0;
        private long _lastReconnectAttemptTicks = 0;

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
                                    textBox16.Text = p_temp.Value[0];
                                    comboBox2.Text = p_temp.Value[3];
                                    comboBox3.Text = p_temp.Value[4];
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
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "qishi", numericUpDown1.Value.ToString());
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                address_length = numericUpDown2.Value;
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "zongchang", numericUpDown2.Value.ToString());
            }
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                lunxun_time = numericUpDown3.Value;
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "lunxun_time", lunxun_time.ToString());

            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox4.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
                else
                {
                    fins_lunxunen = false;
                }
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "modbus_lunxunen", fins_lunxunen.ToString());
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox2.CheckState == CheckState.Checked)
                {
                    fins_en = true;
                }
                else
                {
                    fins_en = false;
                }
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "modbus_en", fins_en.ToString());

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
            if (!CommGridHelper.TryPrepareFullClear(textBox16.Text.Trim(), () => textBox16.Text = ""))
                return;
            CommGridHelper.PerformFullDataClear(wdini, ModbusTcpIniStore.ConnSection(_linkId), DataSuffix, ref geshu, fins_dic, _triggerLatch);
            fins_zuhe.Clear();
            dataGridView1.Visible = false;
            CommGridHelper.ClearGridZebra(dataGridView1);
            dataGridView1.Visible = true;
            button8_Click(null, null);
        }
        private Thread fins_duxie;
        // ★ I/O 串行锁：同一连接实例的“网络写”共用（轮询触发回写 / 检测结果回写 / 切型回执），
        //   避免多个检测线程或轮询线程并发操作同一 Hsl 客户端导致收发串包/响应错乱。
        private readonly object _ioSync = new object();
        private volatile bool _stopPolling = false;
        private CommGridUiSink _gridUi;

        private void button9_Click(object sender, EventArgs e)
        {
            if (button9.Text == "显示")
            {
                tabControl1.Visible = true;
                button9.Text = "隐藏";
            }
            else
            {
                tabControl1.Visible = false;
                button9.Text = "显示";
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                fins_lunxunen = false;
                decimal xuanzhong_temp = 0;
                bool chongdie = false;
                if (comboBox3.Text.Length > 1 && comboBox2.Text.Length > 1 && textBox16.Text.Length > 0)
                {
                    if ((comboBox3.Text == "string" || comboBox3.Text == "int") || (numericUpDown5.Value % 2 == 0))
                    {
                        if (numericUpDown5.Value >= numericUpDown1.Value && numericUpDown1.Value + numericUpDown2.Value >= numericUpDown4.Value + numericUpDown5.Value)
                        {
                            if (textBox16.Text.Length > 0)
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
                                        fins_dic.Add(textBox16.Text, new string[] { textBox16.Text, numericUpDown5.Value.ToString(), numericUpDown4.Value.ToString(), comboBox2.Text, comboBox3.Text });
                                        for (int i = 0; i < numericUpDown4.Value; i++)
                                        {
                                            xuanzhong_temp = numericUpDown5.Value - numericUpDown1.Value + i;
                                            dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                                            dataGridView1[fins_data[int.Parse(xuanzhong_temp.ToString())][0], fins_data[int.Parse(xuanzhong_temp.ToString())][1]].Style.BackColor = Color.Green;
                                            dataGridView1[fins_name[int.Parse(xuanzhong_temp.ToString())][0], fins_name[int.Parse(xuanzhong_temp.ToString())][1]].Value = textBox16.Text;
                                        }
                                        geshu = fins_dic.Count;
                                        wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "geshu", fins_dic.Count.ToString());
                                        wdini.WriteString(ModbusTcpIniStore.BlockSection(_linkId, geshu), "name", textBox16.Text);
                                        wdini.WriteString(ModbusTcpIniStore.BlockSection(_linkId, geshu), "qishi", numericUpDown5.Value.ToString());
                                        wdini.WriteString(ModbusTcpIniStore.BlockSection(_linkId, geshu), "changdu", numericUpDown4.Value.ToString());
                                        wdini.WriteString(ModbusTcpIniStore.BlockSection(_linkId, geshu), "gaodiwei", comboBox2.Text);
                                        wdini.WriteString(ModbusTcpIniStore.BlockSection(_linkId, geshu), "geshi", comboBox3.Text);
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
                if (checkBox4.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                if (checkBox4.CheckState == CheckState.Checked)
                {
                    fins_lunxunen = true;
                }
            }
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
        /// 把 UI 配置的“返回值”(camera_dic[13][1]) 写回“切换”通道(camera_dic[13][0])。
        /// 多实例整窗化（阶段 7）后连接 2~4 为独立整窗实例（child host），此处按 linkId 路由到对应实例。
        /// </summary>
        public bool WriteSchemeSwitchAck(int linkId)
        {
            var host = ResolveLinkHost(linkId);
            if (host == null) return false;
            return host.WriteCamera13SwitchAck();
        }

        // ---- 阶段 7：child host 注册与路由（仿 FormOmron） ----
        /// <summary>登记某连接（2~4）的整窗实例到主连接窗体，供对外 API 按 linkId 路由。</summary>
        public void RegisterChildLink(int linkId, FormModbus host)
        {
            if (host == null) return;
            lock (_childLinksSync) { _childLinks[linkId] = host; }
        }

        public void UnregisterChildLink(int linkId)
        {
            lock (_childLinksSync) { _childLinks.Remove(linkId); }
        }

        // ---- 阶段 7 补强：多连接触发/切方案的按连接访问器（对齐 FormOmron，供 Form1 处理器按 linkId 路由） ----

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

        /// <summary>该 ModbusTCP 协议是否有任一连接使能且已初始化（决定是否读取 VP 的 modbustcp 输出值；
        /// 连接 1 未使能但连接 2~4 使能时仍须读取，否则连接 2~4 回写的是空值）。</summary>
        public bool CanWriteResultOutput()
        {
            if (fins_en && chushihua) return true;
            List<FormModbus> children;
            lock (_childLinksSync) { children = new List<FormModbus>(_childLinks.Values); }
            foreach (var child in children)
                if (child != null && child.fins_en && child.chushihua) return true;
            return false;
        }

        /// <summary>linkId==1 返回自身；linkId&gt;1 查 child host；找不到返回 null。</summary>
        private FormModbus ResolveLinkHost(int linkId)
        {
            if (linkId <= 1) return this;
            lock (_childLinksSync)
            {
                FormModbus host;
                if (_childLinks.TryGetValue(linkId, out host) && host != null) return host;
            }
            return null;
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
            // ★BUG1：重连进行中（_reconnecting!=0）socket 正被 ConnectClose/ConnectServer 操作，
            //   此刻写会与重连抢同一 socket → 写脏数据/抛异常。与轮询读、手动读共用同一道 _reconnecting 门控。
            if (_reconnecting != 0) return;
            lock (_ioSync)   // ★ I/O 串行：与 xie()/回执写互斥，防同一 Hsl 客户端并发写
            {
            for (int j = 0; j < int.Parse(parValue[2]); j++)
            {
                if (parValue[4] == "int")
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busTcpClient.Write((int.Parse(parValue[1]) + j).ToString(), short.Parse(fanhuizhi)), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (parValue[4] == "string")
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busTcpClient.Write((int.Parse(parValue[1]) + j).ToString(), fanhuizhi), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (parValue[4] == "long" && j % 2 == 0)
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busTcpClient.Write((int.Parse(parValue[1]) + j).ToString(), int.Parse(fanhuizhi)), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
                    CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                }
                else if (parValue[4] == "float" && j % 2 == 0)
                {
                    xuanzhong_temp = int.Parse(parValue[1]) - int.Parse(address_qishi.ToString()) + j;
                    DemoUtils.WriteResultRender1(() => busTcpClient.Write((int.Parse(parValue[1]) + j).ToString(), float.Parse(fanhuizhi)), (int.Parse(parValue[1]) + j).ToString(), out fins_temp);
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

        /// <summary>强制创建窗体句柄（程序启动/管理器恢复后台已启用连接时用，避免外部无法访问 protected CreateHandle）。</summary>
        /// <summary>强制创建窗体句柄并完成整窗初始化。说明：创建句柄不会触发 Load（Load 只在首次 Show 触发），
        /// 故建句柄后必须显式 InitializeForm，否则隐藏的额外实例只有被点开节点才会真正启动运行时。</summary>
        public void EnsureHandleCreated()
        {
            if (!IsHandleCreated) CreateHandle();
            InitializeForm();
        }

        /// <summary>
        /// 释放本实例（多连接整窗化 阶段7：连接 2~4 在管理器中被删除/关闭时调用）。
        /// 停止本连接轮询线程、关闭底层客户端并从 CommLinkManager 注销，避免端口残留占用。
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
        /// 阶段 6（连接设备管理器增删多连接）：按 ini [modbustcpN] 当前配置重建并启动连接 N 的运行时。
        /// 先幂等停掉旧的，再按配置新起；modbus_en=false 或段不存在时仅停止不启动。
        /// 连接 1 由窗体自身管理，不受影响。
        /// </summary>
        public async void ApplyExtraLinkRuntime(int linkId)
        {
            if (linkId < 2 || linkId > ModbusTcpIniStore.MaxLinks) return;
            StopExtraLinkRuntime(linkId);
            var cfg = ModbusTcpIniStore.Load(wdini, linkId);
            if (cfg == null || !cfg.ModbusEn) return;

            var link = new ModbusTcpLink();
            link.LinkId = cfg.LinkId;
            link.Name = cfg.Name;
            link.Ip = cfg.Ip;
            link.Port = int.TryParse(cfg.Port, out int p) ? p : 9600;
            link.Station = byte.TryParse(cfg.Cell, out byte st) ? st : (byte)0;
            link.AddressStartWithZero = true;
            await link.ConnectAsync(null);   // 2026-09-06：异步建链，不阻塞 UI
            var rt = new ModbusTcpRuntime(link, this)
            {
                LinkId = cfg.LinkId,
                Config = cfg,
                Context = new ModbusTcpLinkContext(cfg, this, link)
            };
            rt.Start();
            _runtimes[cfg.LinkId] = rt;
            AppHost.Services.Resolve<CommLinkManager>().Register(rt);
        }

        /// <summary>停止并移除指定连接 N 的运行时（管理器“删除连接”时调用）。</summary>
        public void StopExtraLinkRuntime(int linkId)
        {
            if (linkId < 2) return;
            ModbusTcpRuntime old;
            if (!_runtimes.TryGetValue(linkId, out old) || old == null) return;
            try { old.Close(); } catch { }
            _runtimes.Remove(linkId);
            try { AppHost.Services.Resolve<CommLinkManager>().Unregister(linkId); } catch { }
        }

        /// <summary>指定连接 N 的后台运行时是否已在运行（管理器按需补启动用）。</summary>
        public bool IsExtraLinkRunning(int linkId)
        {
            ModbusTcpRuntime rt;
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
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busTcpClient.ReadInt16((int.Parse(par.Value[1]) + j).ToString())), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "string")
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busTcpClient.ReadString((int.Parse(par.Value[1]) + j).ToString(), 1)), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "long" && j % 2 == 0)
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busTcpClient.ReadInt32((int.Parse(par.Value[1]) + j).ToString())), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
                                                CommGridHelper.SetPollCell(_gridUi, fins_data, int.Parse(xuanzhong_temp.ToString()), fins_temp);
                                                shuju_temp += GetMiddleValue(fins_temp, " ", "\r");
                                            }
                                            else if (par.Value[4] == "float" && j % 2 == 0)
                                            {
                                                xuanzhong_temp = int.Parse(par.Value[1]) - int.Parse(address_qishi.ToString()) + j;
                                                // 读取字符串
                                                DemoUtils.ReadResultRender1(ReadLocked(() => busTcpClient.ReadFloat((int.Parse(par.Value[1]) + j).ToString())), (int.Parse(par.Value[1]) + j).ToString(), out fins_temp);
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
                                                    if (pap.Key == 13 && qiehuanzhong == 0)
                                                    {

                                                        if (qiehuan(shuju_temp.Replace("\0", "")) == 1)
                                                        {
                                                            if (File.Exists(lujing.Replace("\0", "")))
                                                            {
                                                                qiehuanzhong = 1;
                                                                SelectionChangedEventArgs E = new SelectionChangedEventArgs(shuju_temp, pap.Key.ToString(), "0", _modbusLink.LinkId);
                                                                getData(this, E);
                                                            }
                                                            else
                                                            {
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
                                                                SelectionChangedEventArgs E = new SelectionChangedEventArgs(dataVal, pap.Key.ToString(), pap.Key.ToString(), _modbusLink.LinkId);
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
                        // ★P3-2：Fins_duxie 轮询外层 catch。PLC/PLC 断线时每圈（~20ms）抛一次，
                        //   原样 Log 会数秒刷上百条几乎相同日志。改限流：5s 窗口内只报首条+累计次数，辨识 tag 含协议/连接号/方法。
                        RateLimitedLog.Throttled("[ModbusTCP-连接" + _linkId + "-Fins_duxie] 轮询读异常", m => MsgErroeLog.WriteLog(m), ex, 5000);
                    }
            }
        }

        private void TryAutoReconnect()
        {
            if (!fins_en || IsDisposed) return;
            if (System.Threading.Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0) return;
            Log("Modbus TCP 通讯异常，后台自动重连中...");
            Task.Run(() =>
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
                        Log("Modbus TCP 自动重连成功");
                        SafeApplyConnectedUiState();
                    }
                }
                catch (Exception ex)
                {
                    Log("Modbus TCP 自动重连失败:" + ex.Message);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref _reconnecting, 0);
                }
            });
        }

        private bool PerformReconnectCore()
        {
            // 多实例改造 阶段1：重连逻辑收口到 ModbusTcpLink
            return _modbusLink.Reconnect();
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
                        userControlCurve1.ReadWriteNet = busTcpClient;
                }));
            }
            catch { }
        }

        /// <summary>
        /// 阶段 7：把检测结果回写到指定连接（2~4）<b>自己</b>配置的反馈数据块。
        /// 连接 1 仍走 <see cref="WriteCameraOutput"/>（窗体原有逻辑），行为不变。
        /// </summary>
        public bool WriteCameraOutputForLink(int linkId, int camIdx, string value)
        {
            if (linkId <= 1) return false;
            var host = ResolveLinkHost(linkId);
            if (host == null) return false;
            host.WriteCameraOutput(camIdx, value);
            return true;
        }

        public void WriteCameraOutput(int camIdx, string value)
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
                    camera_dic[key][4] = value;
                    camera_dic[key][5] = camera_dic[key][3];
                }
                xie(value);   // lock 可重入
            }
        }

        public void xie(string value)
        {
            lock (_ioSync)   // ★ I/O 串行：与 WriteTriggerFanhuizhi 等互斥，防同一 Hsl 客户端并发写
            {
            try
            {
                if (!chushihua || fins_dic.Count == 0) return;
                // ★BUG1：重连进行中不写，避免与 ConnectClose/ConnectServer 抢同一 socket（与轮询读/手动读共用 _reconnecting 门控）。
                if (_reconnecting != 0) return;

                string fins_temp = "";
                // ★P1：xie 会被检测/回写路径调用，同样在遍历"活字典"；UI 清配置时会撞车导致本次回写丢失。
                Dictionary<int, string[]> camSnapshot = CommGridHelper.SnapshotCameraDic(camera_dic);
                Dictionary<string, string[]> finsSnapshot = CommGridHelper.SnapshotFinsDic(fins_dic);
                if (camSnapshot == null || finsSnapshot == null) return;
                foreach (var pat in camSnapshot)
                {
                    if (pat.Value[5] != "无")
                    {
                        foreach (var par in finsSnapshot)
                        {
                            if (pat.Value[5] == par.Value[0])
                            {
                                int addr_start = int.Parse(par.Value[1]);
                                string fmt = par.Value[4];

                                if (fmt == "int")
                                {
                                    // 逗号分隔 → short数组, FC16批量写
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    short[] vals = new short[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = (short)Math.Round(double.Parse(parts[i].Trim()));
                                    // 先尝试FC16批量写，失败则降级为FC06逐地址写
                                    OperateResult batchResult = busTcpClient.Write(addr_start.ToString(), vals);
                                    if (!batchResult.IsSuccess && vals.Length > 1)
                                    {
                                        for (int j = 0; j < vals.Length; j++)
                                        {
                                            busTcpClient.Write((addr_start + j).ToString(), vals[j]);
                                        }
                                        DemoUtils.WriteResultRender1(() => OperateResult.CreateSuccessResult(), addr_start.ToString(), out fins_temp);
                                    }
                                    else
                                    {
                                        DemoUtils.WriteResultRender1(() => batchResult, addr_start.ToString(), out fins_temp);
                                    }
                                    for (int j = 0; j < vals.Length; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                    }
                                }
                                else if (fmt == "long")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    int[] vals = new int[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = (int)Math.Round(double.Parse(parts[i].Trim()));
                                    DemoUtils.WriteResultRender1(() => busTcpClient.Write(addr_start.ToString(), vals), addr_start.ToString(), out fins_temp);
                                    for (int j = 0; j < vals.Length * 2; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                    }
                                }
                                else if (fmt == "float")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    float[] vals = new float[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = float.Parse(parts[i].Trim());
                                    DemoUtils.WriteResultRender1(() => busTcpClient.Write(addr_start.ToString(), vals), addr_start.ToString(), out fins_temp);
                                    for (int j = 0; j < vals.Length * 2; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
                                    }
                                }
                                else if (fmt == "string")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    for (int j = 0; j < parts.Length; j++)
                                    {
                                        int xuanzhong_temp = addr_start - int.Parse(address_qishi.ToString()) + j;
                                        DemoUtils.WriteResultRender1(() => busTcpClient.Write((addr_start + j).ToString(), parts[j].Trim()), (addr_start + j).ToString(), out fins_temp);
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, fins_temp);
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
                Log(ex.Message + "modbustcp");
            }
            }
        }

        #region 无需握手的极速写入 (Send-and-Forget)

        private byte[] BuildWriteMultipleRegistersFrame(ushort startAddress, short[] values, byte station)
        {
            int byteCount = values.Length * 2;
            byte[] frame = new byte[13 + byteCount];
            int offset = 0;

            frame[offset++] = (byte)(xieWuTransactionId >> 8);
            frame[offset++] = (byte)xieWuTransactionId;
            frame[offset++] = 0x00;
            frame[offset++] = 0x00;
            int len = 7 + byteCount;   // ★ 修复 MBAP 长度字段（2026-09-06）：Length 含 UnitId(1)，应为 7+byteCount
            frame[offset++] = (byte)(len >> 8);
            frame[offset++] = (byte)len;
            frame[offset++] = station;
            frame[offset++] = 0x10;
            frame[offset++] = (byte)(startAddress >> 8);
            frame[offset++] = (byte)startAddress;
            frame[offset++] = (byte)(values.Length >> 8);
            frame[offset++] = (byte)values.Length;
            frame[offset++] = (byte)byteCount;
            for (int i = 0; i < values.Length; i++)
            {
                frame[offset++] = (byte)(values[i] >> 8);
                frame[offset++] = (byte)values[i];
            }

            return frame;
        }

        private byte[] BuildWriteMultipleRegistersFrame(ushort startAddress, int[] values, byte station, int dataFmt)
        {
            int byteCount = values.Length * 4;
            byte[] frame = new byte[13 + byteCount];
            int offset = 0;

            frame[offset++] = (byte)(xieWuTransactionId >> 8);
            frame[offset++] = (byte)xieWuTransactionId;
            frame[offset++] = 0x00;
            frame[offset++] = 0x00;
            ushort qty = (ushort)(values.Length * 2);
            int len = 7 + byteCount;   // ★ 修复 MBAP 长度字段（2026-09-06）：Length 含 UnitId(1)，应为 7+byteCount
            frame[offset++] = (byte)(len >> 8);
            frame[offset++] = (byte)len;
            frame[offset++] = station;
            frame[offset++] = 0x10;
            frame[offset++] = (byte)(startAddress >> 8);
            frame[offset++] = (byte)startAddress;
            frame[offset++] = (byte)(qty >> 8);
            frame[offset++] = (byte)qty;
            frame[offset++] = (byte)byteCount;

            byte[] b = new byte[4];
            for (int i = 0; i < values.Length; i++)
            {
                b[0] = (byte)(values[i] >> 24);
                b[1] = (byte)(values[i] >> 16);
                b[2] = (byte)(values[i] >> 8);
                b[3] = (byte)values[i];
                Order4Bytes(b, dataFmt);
                frame[offset++] = b[0];
                frame[offset++] = b[1];
                frame[offset++] = b[2];
                frame[offset++] = b[3];
            }

            return frame;
        }

        private byte[] BuildWriteMultipleRegistersFrame(ushort startAddress, float[] values, byte station, int dataFmt)
        {
            int byteCount = values.Length * 4;
            byte[] frame = new byte[13 + byteCount];
            int offset = 0;

            frame[offset++] = (byte)(xieWuTransactionId >> 8);
            frame[offset++] = (byte)xieWuTransactionId;
            frame[offset++] = 0x00;
            frame[offset++] = 0x00;
            ushort qty = (ushort)(values.Length * 2);
            int len = 7 + byteCount;   // ★ 修复 MBAP 长度字段（2026-09-06）：Length 含 UnitId(1)，应为 7+byteCount
            frame[offset++] = (byte)(len >> 8);
            frame[offset++] = (byte)len;
            frame[offset++] = station;
            frame[offset++] = 0x10;
            frame[offset++] = (byte)(startAddress >> 8);
            frame[offset++] = (byte)startAddress;
            frame[offset++] = (byte)(qty >> 8);
            frame[offset++] = (byte)qty;
            frame[offset++] = (byte)byteCount;

            byte[] b = new byte[4];
            for (int i = 0; i < values.Length; i++)
            {
                b = BitConverter.GetBytes(values[i]);
                Order4Bytes(b, dataFmt);
                frame[offset++] = b[0];
                frame[offset++] = b[1];
                frame[offset++] = b[2];
                frame[offset++] = b[3];
            }

            return frame;
        }

        /// <summary>
        /// 根据 dataFmt（comboBox1.SelectedIndex）重新排列 4 个字节
        /// </summary>
        private void Order4Bytes(byte[] b, int dataFmt)
        {
            byte t;
            switch (dataFmt)
            {
                case 0: // ABCD: 保持原样
                    break;
                case 1: // BADC: 交换字节对内的两个字节
                    t = b[0]; b[0] = b[1]; b[1] = t;
                    t = b[2]; b[2] = b[3]; b[3] = t;
                    break;
                case 2: // CDAB: 交换高16位和低16位
                    t = b[0]; b[0] = b[2]; b[2] = t;
                    t = b[1]; b[1] = b[3]; b[3] = t;
                    break;
                case 3: // DCBA: 完全反转
                    t = b[0]; b[0] = b[3]; b[3] = t;
                    t = b[1]; b[1] = b[2]; b[2] = t;
                    break;
            }
        }

        public void xie_wu(string value)
        {
            try
            {
                if (busTcpClient == null) return;
                byte station = byte.Parse(textBox15.Text);
                int port = int.Parse(textBox2.Text);
                string ip = textBox1.Text;
                int dataFmtIndex = comboBox1.SelectedIndex;

                foreach (var pat in camera_dic)
                {
                    if (pat.Value[5] != "无")
                    {
                        foreach (var par in fins_dic)
                        {
                            if (pat.Value[5] == par.Value[0])
                            {
                                ushort addr_start = ushort.Parse(par.Value[1]);
                                string fmt = par.Value[4];
                                byte[] frame;

                                if (fmt == "int")
                                {
                                    string[] parts = (value+",0").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    short[] vals = new short[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = (short)Math.Round(double.Parse(parts[i].Trim()));
                                    xieWuTransactionId++;
                                    frame = BuildWriteMultipleRegistersFrame(addr_start, vals, station);

                                    using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                                    {
                                        s.NoDelay = true;
                                        s.Connect(ip, port);
                                        int totalSent = 0;
                                        while (totalSent < frame.Length)
                                        {
                                            int sent = s.Send(frame, totalSent, frame.Length - totalSent, SocketFlags.None);
                                            if (sent <= 0) break;
                                            totalSent += sent;
                                        }
                                        byte[] respBuf = new byte[256];
                                        int totalRecv = 0;
                                        while (totalRecv < 12)
                                        {
                                            int r = s.Receive(respBuf, totalRecv, respBuf.Length - totalRecv, SocketFlags.None);
                                            if (r <= 0) break;
                                            totalRecv += r;
                                        }
                                    }

                                    for (int j = 0; j < vals.Length; j++)
                                    {
                                        int xuanzhong_temp = addr_start - ushort.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, "已发送");
                                    }
                                }
                                else if (fmt == "long")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    int[] vals = new int[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = (int)Math.Round(double.Parse(parts[i].Trim()));
                                    xieWuTransactionId++;
                                    frame = BuildWriteMultipleRegistersFrame(addr_start, vals, station, dataFmtIndex);

                                    using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                                    {
                                        s.NoDelay = true;
                                        s.Connect(ip, port);
                                        int totalSent = 0;
                                        while (totalSent < frame.Length)
                                        {
                                            int sent = s.Send(frame, totalSent, frame.Length - totalSent, SocketFlags.None);
                                            if (sent <= 0) break;
                                            totalSent += sent;
                                        }
                                        byte[] respBuf = new byte[256];
                                        int totalRecv = 0;
                                        while (totalRecv < 12)
                                        {
                                            int r = s.Receive(respBuf, totalRecv, respBuf.Length - totalRecv, SocketFlags.None);
                                            if (r <= 0) break;
                                            totalRecv += r;
                                        }
                                    }

                                    for (int j = 0; j < vals.Length * 2; j++)
                                    {
                                        int xuanzhong_temp = addr_start - ushort.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, "已发送");
                                    }
                                }
                                else if (fmt == "float")
                                {
                                    string[] parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    float[] vals = new float[parts.Length];
                                    for (int i = 0; i < parts.Length; i++)
                                        vals[i] = float.Parse(parts[i].Trim());
                                    xieWuTransactionId++;
                                    frame = BuildWriteMultipleRegistersFrame(addr_start, vals, station, dataFmtIndex);

                                    using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                                    {
                                        s.NoDelay = true;
                                        s.Connect(ip, port);
                                        int totalSent = 0;
                                        while (totalSent < frame.Length)
                                        {
                                            int sent = s.Send(frame, totalSent, frame.Length - totalSent, SocketFlags.None);
                                            if (sent <= 0) break;
                                            totalSent += sent;
                                        }
                                        byte[] respBuf = new byte[256];
                                        int totalRecv = 0;
                                        while (totalRecv < 12)
                                        {
                                            int r = s.Receive(respBuf, totalRecv, respBuf.Length - totalRecv, SocketFlags.None);
                                            if (r <= 0) break;
                                            totalRecv += r;
                                        }
                                    }

                                    for (int j = 0; j < vals.Length * 2; j++)
                                    {
                                        int xuanzhong_temp = addr_start - ushort.Parse(address_qishi.ToString()) + j;
                                        CommGridHelper.SetPollCell(_gridUi, fins_data, xuanzhong_temp, "已发送");
                                    }
                                }
                                else
                                {
                                    break;
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
                Log(ex.Message + "xie_wu");
            }
        }

        #endregion

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
            else if (textBox49.Text == aa)
            {
                zifu = aa;
                lujing = textBox53.Text;
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // 相机9-13 原"周期预置触发通道待写标记"已统一移除：
            // 相机1-12 业务同等，原逻辑会把任意相机的检测输出经 xie() 误写入 9-12 的触发通道
            // （与相机13切换通道同源）；移除后各相机仅由 WriteCameraOutput 回写各自反馈通道。
            // fins_xie 数组随之清理（原为只写不读的死代码）。
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
        }

        private void comboBox20_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox20.Text;
                if (GuardCamera(8, nv, 0, comboBox20))
                {
                    camera_dic[8][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 8), "chufa", nv);
                }
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox5.Text;
                if (GuardCamera(1, nv, 0, comboBox5))
                {
                    camera_dic[1][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 1), "chufa", nv);
                }
            }
        }

        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox8.Text;
                if (GuardCamera(2, nv, 0, comboBox8))
                {
                    camera_dic[2][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 2), "chufa", nv);
                }
            }
        }

        private void comboBox10_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox10.Text;
                if (GuardCamera(3, nv, 0, comboBox10))
                {
                    camera_dic[3][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 3), "chufa", nv);
                }
            }
        }

        private void comboBox12_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox12.Text;
                if (GuardCamera(4, nv, 0, comboBox12))
                {
                    camera_dic[4][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 4), "chufa", nv);
                }
            }
        }

        private void comboBox14_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox14.Text;
                if (GuardCamera(5, nv, 0, comboBox14))
                {
                    camera_dic[5][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 5), "chufa", nv);
                }
            }
        }

        private void comboBox16_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox16.Text;
                if (GuardCamera(6, nv, 0, comboBox16))
                {
                    camera_dic[6][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 6), "chufa", nv);
                }
            }
        }

        private void comboBox18_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox18.Text;
                if (GuardCamera(7, nv, 0, comboBox18))
                {
                    camera_dic[7][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 7), "chufa", nv);
                }
            }
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
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox6.Text;
                if (GuardCamera(1, nv, 3, comboBox6))
                {
                    camera_dic[1][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 1), "fankui", nv);
                }
            }
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox7.Text;
                if (GuardCamera(2, nv, 3, comboBox7))
                {
                    camera_dic[2][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 2), "fankui", nv);
                }
            }
        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox9.Text;
                if (GuardCamera(3, nv, 3, comboBox9))
                {
                    camera_dic[3][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 3), "fankui", nv);
                }
            }
        }

        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox11.Text;
                if (GuardCamera(4, nv, 3, comboBox11))
                {
                    camera_dic[4][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 4), "fankui", nv);
                }
            }
        }

        private void comboBox13_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox13.Text;
                if (GuardCamera(5, nv, 3, comboBox13))
                {
                    camera_dic[5][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 5), "fankui", nv);
                }
            }
        }

        private void comboBox15_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox15.Text;
                if (GuardCamera(6, nv, 3, comboBox15))
                {
                    camera_dic[6][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 6), "fankui", nv);
                }
            }
        }

        private void comboBox17_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox17.Text;
                if (GuardCamera(7, nv, 3, comboBox17))
                {
                    camera_dic[7][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 7), "fankui", nv);
                }
            }
        }

        private void comboBox19_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox19.Text;
                if (GuardCamera(8, nv, 3, comboBox19))
                {
                    camera_dic[8][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 8), "fankui", nv);
                }
            }
        }

        private void textBox42_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[1][1] = textBox42.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 1), "fanhuizhi", textBox42.Text);
            }
        }

        private void textBox17_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[2][1] = textBox17.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 2), "fanhuizhi", textBox17.Text);
            }
        }

        private void textBox19_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[3][1] = textBox19.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 3), "fanhuizhi", textBox19.Text);
            }
        }

        private void textBox18_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[4][1] = textBox18.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 4), "fanhuizhi", textBox18.Text);
            }
        }

        private void textBox23_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[5][1] = textBox23.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 5), "fanhuizhi", textBox23.Text);
            }
        }

        private void textBox22_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[6][1] = textBox22.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 6), "fanhuizhi", textBox22.Text);
            }
        }

        private void textBox21_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[7][1] = textBox21.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 7), "fanhuizhi", textBox21.Text);
            }
        }

        private void textBox20_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[8][1] = textBox20.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 8), "fanhuizhi", textBox20.Text);
            }
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox14.CheckState == CheckState.Checked)
                {
                    camera_dic[1][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 1), "fanhuien", "true");
                }
                else
                {
                    camera_dic[1][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 1), "fanhuien", "false");
                }
            }
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox13.CheckState == CheckState.Checked)
                {
                    camera_dic[2][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 2), "fanhuien", "true");
                }
                else
                {
                    camera_dic[2][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 2), "fanhuien", "false");
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
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 3), "fanhuien", "true");
                }
                else
                {
                    camera_dic[3][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 3), "fanhuien", "false");
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
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 4), "fanhuien", "true");
                }
                else
                {
                    camera_dic[4][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 4), "fanhuien", "false");
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
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 5), "fanhuien", "true");
                }
                else
                {
                    camera_dic[5][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 5), "fanhuien", "false");
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
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 6), "fanhuien", "true");
                }
                else
                {
                    camera_dic[6][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 6), "fanhuien", "false");
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
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 7), "fanhuien", "true");
                }
                else
                {
                    camera_dic[7][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 7), "fanhuien", "false");
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
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 8), "fanhuien", "true");
                }
                else
                {
                    camera_dic[8][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 8), "fanhuien", "false");
                }
            }
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
                        if (par.Value[3].Contains("心跳"))
                        {
                            comboBox4.Items.Add(par.Value[0]);

                        }
                    }
                }
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox4.Text;
                if (GuardCamera(9, nv, 0, comboBox4))
                {
                    camera_dic[9][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 9), "chufa", nv);
                }
            }
        }

        private void textBox24_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[9][1] = textBox24.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 9), "fanhuizhi", textBox24.Text);
            }
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox11.CheckState == CheckState.Checked)
                {
                    camera_dic[9][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 9), "fanhuien", "true");
                }
                else
                {
                    camera_dic[9][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 9), "fanhuien", "false");
                }
            }
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
        }

        private void comboBox28_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox28.Text;
                if (GuardCamera(9, nv, 3, comboBox28))
                {
                    camera_dic[9][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 9), "fankui", nv);
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
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
            textBox42.Text = "";
            textBox17.Text = "";
            textBox19.Text = "";
            textBox18.Text = "";
            textBox23.Text = "";
            textBox22.Text = "";
            textBox21.Text = "";
            textBox20.Text = "";
            textBox24.Text = "";
            textBox41.Text = "";
            textBox43.Text = "";
            textBox44.Text = "";
            textBox45.Text = "";
            textBox54.Text = "";
            checkBox18.CheckState = CheckState.Unchecked;
            comboBox29.Text = "";
            checkBox14.CheckState = CheckState.Unchecked;
            checkBox13.CheckState = CheckState.Unchecked;
            checkBox5.CheckState = CheckState.Unchecked;
            checkBox6.CheckState = CheckState.Unchecked;
            checkBox7.CheckState = CheckState.Unchecked;
            checkBox8.CheckState = CheckState.Unchecked;
            checkBox9.CheckState = CheckState.Unchecked;
            checkBox10.CheckState = CheckState.Unchecked;
            checkBox11.CheckState = CheckState.Unchecked;
            checkBox12.CheckState = CheckState.Unchecked;
            checkBox15.CheckState = CheckState.Unchecked;
            checkBox16.CheckState = CheckState.Unchecked;
            checkBox17.CheckState = CheckState.Unchecked;
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
            comboBox28_SelectedIndexChanged(null, null);
            comboBox29_SelectedIndexChanged(null, null);
            textBox54_TextChanged(null, null);
            checkBox18_CheckedChanged(null, null);
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
        }

        private void comboBox21_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox21.Text;
                if (GuardCamera(13, nv, 0, comboBox21))
                {
                    camera_dic[13][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "chufa", nv);
                }
            }
        }

        private void textBox41_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[13][1] = textBox41.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "fanhuizhi", textBox41.Text);
            }
        }

        private void textBox25_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox25.Text = openFileDialog.FileName;
            }
        }

        private void textBox26_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox26.Text = openFileDialog.FileName;
            }
        }

        private void textBox28_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox28.Text = openFileDialog.FileName;
            }
        }

        private void textBox27_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox27.Text = openFileDialog.FileName;
            }
        }

        private void textBox32_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox32.Text = openFileDialog.FileName;
            }
        }

        private void textBox31_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox31.Text = openFileDialog.FileName;
            }
        }

        private void textBox30_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox30.Text = openFileDialog.FileName;
            }
        }

        private void textBox29_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox29.Text = openFileDialog.FileName;
            }
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox12.CheckState == CheckState.Checked)
                {
                    camera_dic[13][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "fanhuien", "true");
                }
                else
                {
                    camera_dic[13][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "fanhuien", "false");
                }
            }
        }

        private void textBox54_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[13][1] = textBox54.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "fanhuizhi", textBox54.Text);
            }
        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox18.CheckState == CheckState.Checked)
                {
                    camera_dic[13][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "fanhuien", "true");
                }
                else
                {
                    camera_dic[13][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "fanhuien", "false");
                }
            }
        }

        private void comboBox29_DropDown(object sender, EventArgs e)
        {
            if (chushihua)
            {
                comboBox29.Items.Clear();
                if (fins_dic.Count > 0)
                {
                    foreach (var par in fins_dic)
                    {
                        if (par.Value[3].Contains("反馈"))
                        {
                            comboBox29.Items.Add(par.Key);
                        }
                    }
                }
            }
        }

        private void comboBox29_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[13][3] = comboBox29.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 13), "fankui", comboBox29.Text);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "1", textBox40.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "2", textBox39.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "3", textBox38.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "4", textBox37.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "5", textBox36.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "6", textBox35.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "7", textBox34.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "8", textBox33.Text);

            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "9", textBox46.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "10", textBox47.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "11", textBox48.Text);
            wdini.WriteString(ModbusTcpIniStore.ChangeSection(_linkId), "12", textBox49.Text);

            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "1", textBox25.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "2", textBox26.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "3", textBox28.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "4", textBox27.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "5", textBox32.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "6", textBox31.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "7", textBox30.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "8", textBox29.Text);

            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "9", textBox50.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "10", textBox51.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "11", textBox52.Text);
            wdini.WriteString(ModbusTcpIniStore.PathSection(_linkId), "12", textBox53.Text);
        }

        private void SaveModbusTrigVal(int cam, TextBox tb)
        {
            CommTriggerHelper.ParseTrigValInput(camera_dic[cam][6], tb.Text, camera_dic[cam]);
            wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, cam), "chufazhi1", camera_dic[cam][7]);
            wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, cam), "chufazhi2", camera_dic[cam][8]);
            _triggerLatch[cam] = false;
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            FormOperationHelp.ShowHelp(this, "Modbus TCP");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "ip", textBox1.Text);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "port", textBox2.Text);
            }
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "cell", textBox15.Text);
            }
        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (chushihua)
            {
                wdini.WriteString(ModbusTcpIniStore.ConnSection(_linkId), "abcd", comboBox1.Text);
            }
        }

        #region Camera 10 events
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
        }

        private void comboBox22_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox22.Text;
                if (GuardCamera(10, nv, 0, comboBox22))
                {
                    camera_dic[10][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 10), "chufa", nv);
                }
            }
        }

        private void textBox43_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[10][1] = textBox43.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 10), "fanhuizhi", textBox43.Text);
            }
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox15.CheckState == CheckState.Checked)
                {
                    camera_dic[10][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 10), "fanhuien", "true");
                }
                else
                {
                    camera_dic[10][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 10), "fanhuien", "false");
                }
            }
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
        }

        private void comboBox23_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox23.Text;
                if (GuardCamera(10, nv, 3, comboBox23))
                {
                    camera_dic[10][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 10), "fankui", nv);
                }
            }
        }
        #endregion

        #region Camera 11 events
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
        }

        private void comboBox24_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox24.Text;
                if (GuardCamera(11, nv, 0, comboBox24))
                {
                    camera_dic[11][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 11), "chufa", nv);
                }
            }
        }

        private void textBox44_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[11][1] = textBox44.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 11), "fanhuizhi", textBox44.Text);
            }
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox16.CheckState == CheckState.Checked)
                {
                    camera_dic[11][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 11), "fanhuien", "true");
                }
                else
                {
                    camera_dic[11][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 11), "fanhuien", "false");
                }
            }
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
        }

        private void comboBox25_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox25.Text;
                if (GuardCamera(11, nv, 3, comboBox25))
                {
                    camera_dic[11][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 11), "fankui", nv);
                }
            }
        }
        #endregion

        #region Camera 12 events
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
        }

        private void comboBox26_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox26.Text;
                if (GuardCamera(12, nv, 0, comboBox26))
                {
                    camera_dic[12][0] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 12), "chufa", nv);
                }
            }
        }

        private void textBox45_TextChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                camera_dic[12][1] = textBox45.Text;
                wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 12), "fanhuizhi", textBox45.Text);
            }
        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                if (checkBox17.CheckState == CheckState.Checked)
                {
                    camera_dic[12][2] = "true";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 12), "fanhuien", "true");
                }
                else
                {
                    camera_dic[12][2] = "false";
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 12), "fanhuien", "false");
                }
            }
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
        }

        private void comboBox27_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chushihua)
            {
                string nv = comboBox27.Text;
                if (GuardCamera(12, nv, 3, comboBox27))
                {
                    camera_dic[12][3] = nv;
                    wdini.WriteString(ModbusTcpIniStore.CameraSection(_linkId, 12), "fankui", nv);
                }
            }
        }
        #endregion

        #region Path textBox MouseDoubleClick events (9-12)
        private void textBox50_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox50.Text = openFileDialog.FileName;
            }
        }

        private void textBox51_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox51.Text = openFileDialog.FileName;
            }
        }

        private void textBox52_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox52.Text = openFileDialog.FileName;
            }
        }

        private void textBox53_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox53.Text = openFileDialog.FileName;
            }
        }
        #endregion
    }
}

