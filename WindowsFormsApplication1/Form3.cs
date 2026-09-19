using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Globalization;
using demo;

namespace WindowsFormsApplication1
{
    public partial class Form3 : Form
    {

        public Form1 f1;
        /// <summary>共享管线宿主：主窗（连接1）实例。额外整窗（连接 2~4）收到帧后命中/未命中结果并入宿主，
        /// 使相机触发与切型都走与连接1完全相同的整机唯一入口（getData/Form1.timer10）。</summary>
        internal Form3 HostMain;
        /// <summary>整窗一次性初始化门闩：主窗经 Form3_Load（首次 Show 触发）执行；额外整窗（连接 2~4）由管理器
        /// EnsureWindowHandle 在创建句柄后显式触发（隐藏窗不会自动 Load）。二者共用 InitializeForm 且只执行一次，
        /// 避免“预启动后再 Show”导致 chushihua 双跑重复建链。</summary>
        private volatile bool _initialized = false;
        // COM 互斥防呆（阶段 7 补充）：Form3 主串口作为“无协议主串口”参与全连接 COM 占用表登记（与 Modbus-RTU / 无协议整窗统一互斥）
        private const string SerialMainOwner = "无协议主串口";
        private string _lastMainSerialLog = "";
        private TextBox[] _photoBoxes;
        public string aa;
        public string monitor;
        public delegate void GetSeletionData(object Sender, SelectionChangedEventArgs e);
        public event GetSeletionData getData;
        /// <summary>整窗化：链接编号。连接 1 = 主窗（默认），2~4 = “连接设备”管理器创建的同构整窗。</summary>
        private int _linkId = 1;
        public int LinkId { get { return _linkId; } }
        /// <summary>连接 1 主窗为 true；额外整窗（连接 2~4）读各自独立配置文件并自持运行时。</summary>
        internal bool IsMainWindow { get { return _linkId <= 1; } }
        /// <summary>本窗配置源：连接 1 = 主 test.ini；连接 2~4 = test_noprotoN.ini（同目录，由管理器增删时创建/删除）。</summary>
        internal string IniFilePath
        {
            get { return AppDomain.CurrentDomain.BaseDirectory + "//test" + (IsMainWindow ? "" : "_noproto" + _linkId) + ".ini"; }
        }
        /// <summary>COM 互斥属主名：主窗沿用既有常量；额外整窗以“无协议连接N”登记（与 SerialPortGuard 提示口径一致）。</summary>
        internal string SerialOwner { get { return IsMainWindow ? SerialMainOwner : "无协议连接" + _linkId; } }
        public Form3(Form1 f1) : this(f1, 1) { }
        public Form3(Form1 f1, int linkId)
        {
            _linkId = linkId;
            wdini.ReadINIFile(IniFilePath);
            this.f1 = f1;
            InitializeComponent();
        }
        private ClassIni wdini = new ClassIni();
        private Thread getRecevice;
        private Thread tcpclientmo;
        protected volatile bool stop = false;
        protected bool constant = false;
        private readonly byte[] _tcpRecvBuffer = new byte[64 * 1024];   // TCP 接收缓冲（复用，避免每帧分配 3MB）
        private volatile bool _tcpClientNeedReconnect = false;           // TCP 客户机断线重连标志（独立，不依赖 monitor 字符串）
        private string _serialBuf = "";                                  // 串口无协议组帧缓冲
        private string _tcpBuf = "";                                     // TCP 客户机无协议组帧缓冲（单连接用字段；服务器每连接用局部变量）
        // ★ P3.1：切型匹配串缓存。qiehuan() 由 TCP 客户机/服务器接收线程（后台）高频调用，
        // 直接读 textBox18~25.Text 在后台线程访问控件（Debug 开跨线程校验会抛异常）。
        // 改为后台读这份缓存；UI 线程在 InitializeForm 载入与 8 个框 TextChanged 时同步它（线程安全）。
        private volatile string[] _trigPath = new string[8];
        private readonly object _trigSync = new object();
        private void SyncTrigCache(TextBox b1, TextBox b2, TextBox b3, TextBox b4, TextBox b5, TextBox b6, TextBox b7, TextBox b8)
        {
            lock (_trigSync)
                _trigPath = new[]
                {
                    b1?.Text ?? "", b2?.Text ?? "", b3?.Text ?? "", b4?.Text ?? "",
                    b5?.Text ?? "", b6?.Text ?? "", b7?.Text ?? "", b8?.Text ?? ""
                };
        }
        private StreamReader sRead;
        public string gongnengma="06";
        public string qiehuan_fangshi = "";
        private int xiangji = 1;
        string strReceive;
        bool bAccpet = false;
       // SerialPortmdcan.port = new SerialPort();
       public  Modbus mdcan = new Modbus();
        public int modbus_qufan = 0;
        public static string strportName = "";
        public static string strbaudRate = "";
        public static string strDataBits = "";
        public static string strStopBits = "";
        public static string strjiaoyan = "";
        // 额外整窗（连接 2~4）的串口参数实例化：主窗静态字段仍供 Form4/既有调用零改动，额外窗互不串扰。
        private string _portName = "";
        private string _baudRate = "";
        private string _stopBits = "";
        private string _dataBits = "";
        private string _jiaoyan = "";
        public string CurPortName { get { return IsMainWindow ? strportName : _portName; } set { if (IsMainWindow) strportName = value; else _portName = value; } }
        public string CurBaudRate { get { return IsMainWindow ? strbaudRate : _baudRate; } set { if (IsMainWindow) strbaudRate = value; else _baudRate = value; } }
        public string CurStopBits { get { return IsMainWindow ? strStopBits : _stopBits; } set { if (IsMainWindow) strStopBits = value; else _stopBits = value; } }
        public string CurDataBits { get { return IsMainWindow ? strDataBits : _dataBits; } set { if (IsMainWindow) strDataBits = value; else _dataBits = value; } }
        public string CurParity { get { return IsMainWindow ? strjiaoyan : _jiaoyan; } set { if (IsMainWindow) strjiaoyan = value; else _jiaoyan = value; } }
        private bool HasSerialCfg { get { return CurPortName != "" && CurBaudRate != "" && CurDataBits != "" && CurStopBits != ""; } }
        Socket socketClient;
        int oks;
        int ngs;
        string out0ok;
        string out1ok;
        string out2ok;
        string out3ok;
        string out0ng;
        string out1ng;
        string out2ng;
        string out3ng;
        int jinzhi;
        public string zifu;
        public string lujing="a";
        public int jobsum=0;
        ErrorLog MsgErroeLog = new ErrorLog();
        public string xie = "0";
        public string du = "0";
        #region 字段与初始化
        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form3_Load(object sender, EventArgs e)
        {
            // 统一入口：主窗首次 Show 触发 Load → InitializeForm；额外整窗（连接 2~4）由管理器 EnsureWindowHandle
            // 在创建句柄后显式触发。二者共用同一实现且只执行一次（_initialized 门闩），隐藏窗预启动后再 Show 不会重复建链。
            InitializeForm();
        }

        /// <summary>整窗初始化：读本窗配置文件（连接1=test.ini / 连接N=test_noprotoN.ini）填充界面控件，
        /// 并按三通道 en 开关经 chushihua() 自动启停串口 / TCP 客户机 / TCP 服务器（与连接 1 自动开通道行为一致）。</summary>
        internal void InitializeForm()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                mdcan.port= new SerialPort();
                textBox18.Text = wdini.ReadString("change", "1", "");
                textBox19.Text = wdini.ReadString("change", "2", "");
                textBox21.Text = wdini.ReadString("change", "3", "");
                textBox20.Text = wdini.ReadString("change", "4", "");
                textBox23.Text = wdini.ReadString("change", "5", "");
                textBox22.Text = wdini.ReadString("change", "6", "");
                textBox25.Text = wdini.ReadString("change", "7", "");
                textBox24.Text = wdini.ReadString("change", "8", "");
                // ★ P3.1：切型匹配串缓存——载入后同步一次，并挂 8 个框的 TextChanged 实时同步，
                // 供 qiehuan() 在后台接收线程安全读取（替代直接读 .Text，避免跨线程访问控件）。
                SyncTrigCache(textBox18, textBox19, textBox21, textBox20, textBox23, textBox22, textBox25, textBox24);
                foreach (TextBox tb in new[] { textBox18, textBox19, textBox21, textBox20, textBox23, textBox22, textBox25, textBox24 })
                {
                    if (tb == null) continue;
                    tb.TextChanged += (s, ev) =>
                        SyncTrigCache(textBox18, textBox19, textBox21, textBox20, textBox23, textBox22, textBox25, textBox24);
                }
                // 拍照触发字符：从 ini 读取，并实时同步到各相机 triggerZifu（不依赖 VP 方案）
                textBox26.Text = wdini.ReadString("change", "triggerzifu", "");
                textBox26.TextChanged += textBox26_TextChanged;
                if (f1 != null && IsMainWindow) f1.SetTriggerZifu(textBox26.Text);
                // 各相机独立拍照触发字符
                _photoBoxes = new TextBox[] { textBoxP1, textBoxP2, textBoxP3, textBoxP4, textBoxP5, textBoxP6, textBoxP7, textBoxP8, textBoxP9, textBoxP10, textBoxP11, textBoxP12 };
                for (int _pi = 0; _pi < 12; _pi++)
                {
                    string _batch = textBox26.Text;
                    _photoBoxes[_pi].Text = wdini.ReadString("change", "triggerzifu" + (_pi + 1), _batch);
                    _photoBoxes[_pi].TextChanged += photoTrigger_TextChanged;
                    if (f1 != null && IsMainWindow) f1.SetTriggerZifu(_pi, _photoBoxes[_pi].Text);
                }
                textBox10.Text = wdini.ReadString("path", "1", "");
                textBox11.Text = wdini.ReadString("path", "2", "");
                textBox12.Text = wdini.ReadString("path", "3", "");
                textBox13.Text = wdini.ReadString("path", "4", "");
                textBox14.Text = wdini.ReadString("path", "5", "");
                textBox15.Text = wdini.ReadString("path", "6", "");
                textBox16.Text = wdini.ReadString("path", "7", "");
                textBox17.Text = wdini.ReadString("path", "8", "");
            
                comboBox2.Text = wdini.ReadString("change", "all", "");
                // 显式同步一次：ini 缺 [change]all 时给 Text 赋值不触发 SelectedIndexChanged，qiehuan_fangshi 会恒空
                SyncQiehuanFangshi();
               
            }
            catch { }
            try
            {
                CurPortName = wdini.ReadString("serial", "strportName", "Com1");
                CurBaudRate = wdini.ReadString("serial", "strbaudRate", "9600");
                CurStopBits = wdini.ReadString("serial", "strStopBits", "1");
                CurDataBits = wdini.ReadString("serial", "strDataBits", "8");
                CurParity = wdini.ReadString("serial", "strjiaoyan", "0");
                mdcan.port.PortName = CurPortName;
                mdcan.port.BaudRate = int.Parse(CurBaudRate);
                mdcan.port.StopBits = (StopBits)int.Parse(CurStopBits);
                mdcan.port.DataBits = int.Parse(CurDataBits);
                if (CurParity == "0")
                {
                    mdcan.port.Parity = Parity.None;
                }
                else if (CurParity == "1")
                {
                    mdcan.port.Parity = Parity.Odd;
                }
                else if (CurParity == "2")
                {
                    mdcan.port.Parity = Parity.Even;
                }

                mdcan.port.ReadTimeout = 500;
            }
            catch (Exception ex)
            {
                monitor = "错误：" + ex.Message;

            };
            tcpclientmo = new Thread(new ThreadStart(clientmonitor));
            tcpclientmo.IsBackground = true;
            tcpclientmo.Start();
            monitor = "";
            textBox3.Text = wdini.ReadString("tcpserver", "port1", textBox3.Text);
            textBox4.Text = wdini.ReadString("tcpserver", "ip1", textBox4.Text);
            textBox5.Text = wdini.ReadString("tcpclient", "port2", textBox5.Text);
            textBox6.Text = wdini.ReadString("tcpclient", "ip2", textBox6.Text);
            textBox2.Text = wdini.ReadString("tcpsend", "send1", textBox2.Text);
       
            textBox10.Text = wdini.ReadString("out0", "ok", textBox10.Text);
            textBox11.Text = wdini.ReadString("out1", "ok", textBox11.Text);
            textBox12.Text = wdini.ReadString("out2", "ok", textBox12.Text);
            textBox13.Text = wdini.ReadString("out3", "ok", textBox13.Text);
            textBox14.Text = wdini.ReadString("out0", "ng", textBox14.Text);
            textBox15.Text = wdini.ReadString("out1", "ng", textBox15.Text);
            textBox16.Text = wdini.ReadString("out2", "ng", textBox16.Text);
            textBox17.Text = wdini.ReadString("out3", "ng", textBox17.Text);
            out0ok = textBox10.Text;
            out1ok = textBox11.Text;
            out2ok = textBox12.Text;
            out3ok = textBox13.Text;
            out0ng = textBox14.Text;
            out1ng = textBox15.Text;
            out2ng = textBox16.Text;
            out3ng = textBox17.Text;
        
            if (wdini.ReadString("jinzhi", "16en", "true") == "true")
            {
                jinzhi = 1;
                checkBox1.CheckState = CheckState.Checked;

            }
            else
                checkBox1.CheckState = CheckState.Unchecked;
   
            if (wdini.ReadString("tcpclient", "en", "true") == "true")
            {
                checkBox3.CheckState = CheckState.Checked;
            }
            else
                checkBox3.CheckState = CheckState.Unchecked;
            if (wdini.ReadString("tcpserver", "en", "true") == "true")
            {
                checkBox2.CheckState = CheckState.Checked;
            }
            else
                checkBox2.CheckState = CheckState.Unchecked;
            if (wdini.ReadString("serial", "en", "true") == "true")
            {
                checkBox4.CheckState = CheckState.Checked;
            }
            else
                checkBox4.CheckState = CheckState.Unchecked;
            oks = 0;
            ngs = 0;
            // Form1 frm1 = (Form1)this.Owner;
            //  frm1.changedata_event+= new Form1.changedata(DataChange);
            groupBox1.Enabled = false;
            groupBox2.Enabled = false;
            this.label5.Text = "端口号：端口未打开|";
            this.label6.Text = "波特率：端口未打开|";
            this.label7.Text = "数据位：端口未打开|";
            this.label8.Text = "停止位：端口未打开|";
            this.label9.Text = "校验: 端口未打开";
            Task.Run(() =>
            {
                chushihua();
            });
        }
        void chushihua()
        {
            Thread.Sleep(50);
            // 释放竞态守卫：管理器可能在 chushihua 建链前就删除/释放本窗（ReleaseInstance 已置 stop 并关串口），
            // 必须按 stop 退出，否则会把刚释放的连接重新打开——串口会被 COM 占用表永久登记、接收线程泄漏。
            if (stop) return;
            if (SafeRead(() => checkBox4.CheckState) == CheckState.Checked)
            {
                if (HasSerialCfg)
                {
                    try
                    {
                        Thread.Sleep(300);
                        if (stop) return;
                        string serr;
                        if (TryOpenSerialMain(mdcan.port.PortName, out serr))
                        {
                            Thread.Sleep(300);
                            if (!mdcan.port.IsOpen)
                                TryOpenSerialMain(mdcan.port.PortName, out serr);
                            // ★P3.1：chushihua 在 Task.Run 后台线程执行，回显控件与"自动开始接收"统一走 UI 线程
                            SafeUi(() =>
                            {
                                button6.Text = "关闭串口";
                                groupBox1.Enabled = true;
                                groupBox2.Enabled = true;
                                this.label5.Text = "端口号：" + mdcan.port.PortName + "|";
                                this.label6.Text = "波特率：" + mdcan.port.BaudRate + "|";
                                this.label7.Text = "数据位：" + mdcan.port.DataBits + "|";
                                this.label8.Text = "停止位：" + mdcan.port.StopBits + "|";
                                this.label9.Text = "校验:" + mdcan.port.Parity;
                                // Form3 串口仅处理无协议通讯（modbus-RTU 由 FormModbusRtu 负责），直接自动开始接收
                                btnReceive_Click(null, null);
                            });
                        }
                        else
                        {
                            monitor = serr;
                            MsgErroeLog.WriteLog("[Form3-串口] " + serr);
                        }
                    }
                    catch (Exception ex)
                    {
                        monitor = "错误：" + ex.Message;

                    };
                }
                else
                    monitor = "请先设置串口!" + "通讯";
            }
            if (stop) return;
            if (SafeRead(() => checkBox2.CheckState) == CheckState.Checked)
            {
                try
                {
                    if (!_listenStarted)
                    {
                    socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPAddress ip = IPAddress.Any;
                    //IPAddress ip = IPAddress.Parse("192.168.88.1");
                    IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(SafeRead(() => textBox3.Text)));
                    socketWatch.Bind(point);
                    //监听
                    ShowMsg("监听成功");
                    socketWatch.Listen(10);
                    _listenStarted = true;   // Bind/Listen 成功后才置位，失败可重试
                    Thread td = new Thread(Listen);
                    td.IsBackground = true;
                    td.Start(socketWatch);
                    }
                    //等待客户端连接
                }
                catch
                { }
            }
            if (stop) return;
            if (SafeRead(() => checkBox3.CheckState) == CheckState.Checked)
            {
                try
                {
                    socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPAddress ip = IPAddress.Parse(SafeRead(() => textBox6.Text).Trim());
                    IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(SafeRead(() => textBox5.Text)));
                    //获得要连接的远程IP和端口号（带超时）
                    if (!ConnectWithTimeout(socketClient, point, 3000))
                        throw new Exception("连接超时");
                    ShowMsgClient(socketClient.RemoteEndPoint + "连接成功,我是客户机");
                    //开启一个线程，不断的接收服务端发来的消息
                    Thread th = new Thread(receiveClient);
                    th.IsBackground = true;
                    th.Start();
                }
                catch { monitor = "Tcpclient连接失败"; }
            }
            // 阶段 7（整窗化）：无协议连接 2~4 的运行时已由“连接设备”管理器的独立整窗实例持有，
            // 此处不再宿主启动；连接 1 的 UI 三通道行为保持不变。
        }
        #endregion
        void DataChange(string data)
        {
            textBox9.Text = data;
        }
        /// <summary>
        /// 通讯监控：500ms 周期检查 TCP 重连标志与串口自动重开
        /// </summary>
        void clientmonitor()
        {
            while (!stop)
            {
                Thread.Sleep(500);
                // ★P3.1：后台监控线程读控件一律 SafeRead（Invoke），写回显控件一律 SafeUi（BeginInvoke），
                // 避免 Debug 开跨线程校验时后台直接访问控件抛异常被 catch 吞掉、导致自动重连/重开失效。
                if (SafeRead(() => checkBox3.CheckState) == CheckState.Checked)
                {
                    try
                    {
                        if (_tcpClientNeedReconnect)
                        {
                            // 重连前先关闭旧 socket，防止 FD 泄漏
                            try { if (socketClient != null) { socketClient.Close(); socketClient = null; } } catch { }
                            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            IPAddress ip = IPAddress.Parse(SafeRead(() => textBox6.Text).Trim());
                            IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(SafeRead(() => textBox5.Text)));
                            //获得要连接的远程IP和端口号（带超时，目标不可达时 3 秒内放弃本次重试）
                            if (ConnectWithTimeout(socketClient, point, 3000))
                            {
                                ShowMsgClient(socketClient.RemoteEndPoint + "连接成功,我是客户机");
                                //开启一个线程，不断的接收服务端发来的消息
                                Thread th = new Thread(receiveClient);
                                th.IsBackground = true;
                                th.Start();
                                _tcpClientNeedReconnect = false;
                            }
                            else
                            {
                                try { socketClient.Close(); socketClient = null; } catch { }
                                // 下次轮询继续重试
                            }
                        }
                    }
                    catch { }
                }
                if (SafeRead(() => checkBox4.CheckState) == CheckState.Checked)
                {
                    try
                    {
                        if (HasSerialCfg)
                        {
                            // 仅当用户已启用串口（按钮显示"关闭串口"）且串口意外关闭时才自动重开，避免与手动关闭冲突
                            if (!mdcan.port.IsOpen && SafeRead(() => button6.Text) == "关闭串口")
                            {
                                string serr;
                                if (TryOpenSerialMain(mdcan.port.PortName, out serr))
                                {
                                    SafeUi(() =>
                                    {
                                        button6.Text = "关闭串口";
                                        groupBox1.Enabled = true;
                                        groupBox2.Enabled = true;
                                        this.label5.Text = "端口号：" + mdcan.port.PortName + "|";
                                        this.label6.Text = "波特率：" + mdcan.port.BaudRate + "|";
                                        this.label7.Text = "数据位：" + mdcan.port.DataBits + "|";
                                        this.label8.Text = "停止位：" + mdcan.port.StopBits + "|";
                                        this.label9.Text = "校验:" + mdcan.port.Parity;
                                    });
                                }
                                else if (!string.IsNullOrEmpty(serr) && serr != _lastMainSerialLog)
                                {
                                    // COM 占用 / 打开失败：只记一次（占用解除后由本循环自动重开），避免每 500ms 刷日志
                                    _lastMainSerialLog = serr;
                                    MsgErroeLog.WriteLog("[Form3-串口] 自动重开失败: " + serr);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        #region TCP 通讯（客户机/服务器）
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(textBox6.Text.Trim());
                IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(textBox5.Text));
                //获得要连接的远程IP和端口号（带超时）
                if (!ConnectWithTimeout(socketClient, point, 3000))
                    throw new Exception("连接超时");
                ShowMsgClient(socketClient.RemoteEndPoint + "连接成功,我是客户机");
                //开启一个线程，不断的接收服务端发来的消息
                Thread th = new Thread(receiveClient);
                th.IsBackground = true;
                th.Start();
            }
            catch {
                monitor = "Tcpclient连接失败";
            }
        }
        /// <summary>
        /// TCP 客户机接收线程：断线/异常时置重连标志并退出（不再空转）
        /// </summary>
        private void receiveClient()
        {
            // ★ 句柄竞态修复：线程入口快照本次连接的 socket，全程只操作本快照。
            // 原实现线程内直接用字段 socketClient，若 clientmonitor 已重建新 socket，
            // 旧线程 Close() 会把新连接一起关掉，造成新老连接来回闪断。
            Socket sk = socketClient;
            if (sk == null) return;
            _tcpBuf = "";   // 新连接从零开始组帧，避免上次连接残留的半包与新数据拼成错帧
            while (true)
            {
                try
                {
                    int r = sk.Receive(_tcpRecvBuffer);
                    if (r == 0)
                    {
                        // 服务端正常关闭连接：关闭本连接并标记失败状态，触发 clientmonitor 自动重连
                        // ★ 仅当字段仍指向本连接时才 Close/置空，避免误关已重建的新连接
                        try { if (socketClient == sk) { sk.Close(); socketClient = null; } } catch { }
                        _tcpBuf = "";
                        _tcpClientNeedReconnect = true;
                        break;
                    }
                    string sss = System.Text.Encoding.Default.GetString(_tcpRecvBuffer, 0, r);
                    if (jinzhi == 1)
                        sss = sss.Replace('\0', '0');
                    ShowMsgClient(sk.RemoteEndPoint + ":" + sss+"\r\n");
                    if (qiehuan_fangshi == "Tcp_client")
                    {
                        DispatchTcpFrame(sss, ref _tcpBuf);
                    }
                }
                catch
                {
                    // 断线/异常：关闭本连接并标记失败状态，让 clientmonitor 感知后自动重连
                    // ★ 仅当字段仍指向本连接时才 Close/置空，避免误关已重建的新连接
                    _tcpClientNeedReconnect = true;
                    try { if (socketClient == sk) { sk.Close(); socketClient = null; } } catch { }
                    _tcpBuf = "";
                    break;
                }
            }
        }

        /// <summary>
        /// 无协议 TCP 组帧派发：修复"拆包漏触发 / 粘包误匹配"。
        /// 规则与串口侧一致（串口用 _serialBuf + '\n' 分帧，本方法沿用同一约定）：
        ///   ① 缓冲里已有半包，或本次数据含换行 → 累积到 buffer 后按行派发
        ///      （半包留在 buffer 等下一段；超 512 字节强制成帧，与串口一致）；
        ///   ② 缓冲为空且本次数据不含换行 → 按原有行为整段直接派发，兼容不使用换行分隔的协议，避免吞帧。
        /// buffer 由调用方持有：客户机用实例字段 _tcpBuf（单连接）；服务器每连接用方法局部变量（多连接不串扰）。
        /// </summary>
        private void DispatchTcpFrame(string data, ref string buffer)
        {
            if (buffer.Length == 0 && data.IndexOf('\n') < 0)
            {
                DispatchReceivedFrame(data);   // 原有行为：整段直接派发
                return;
            }
            buffer += data;
            while (buffer.Length > 0)
            {
                int idx = buffer.IndexOf('\n');
                string line;
                if (idx >= 0)
                {
                    line = buffer.Substring(0, idx);
                    buffer = buffer.Substring(idx + 1);
                }
                else if (buffer.Length >= 512)
                {
                    line = buffer;
                    buffer = "";
                }
                else
                    break;   // 半包，等待下一次数据
                line = line.Trim().TrimEnd('\r', '\0');
                if (line.Length > 0)
                {
                    DispatchReceivedFrame(line);
                }
            }
        }
        /// <summary>线程安全地追加文本，并限制最大长度，防止无协议主窗长期运行后接收框过大导致切换/渲染卡顿。</summary>
        /// <summary>★ P3.1：后台线程安全写 UI（BeginInvoke + 已释放/未建句柄守卫）。UI 线程直接执行。
        /// 用于 Listen/clientmonitor/chushihua 等后台线程刷新 button/label/groupBox/comboBox 等回显控件。</summary>
        private void SafeUi(Action a)
        {
            if (a == null) return;
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    if (IsHandleCreated) BeginInvoke(a);   // 未建句柄则丢弃（本窗尚在预建期）
                    return;
                }
                a();
            }
            catch { }
        }

        /// <summary>★ P3.1：后台线程安全读 UI 控件值（Invoke 同步）。UI 线程直接读；句柄未建/已释放返回默认值。
        /// 用于 clientmonitor/chushihua 读 checkBox/button/TextBox 等状态，避免后台直接访问控件在 Debug 校验下抛异常。</summary>
        private T SafeRead<T>(Func<T> f)
        {
            try
            {
                if (IsDisposed) return default(T);
                if (InvokeRequired)
                {
                    if (!IsHandleCreated) return default(T);
                    return (T)this.Invoke(f);
                }
                return f();
            }
            catch { return default(T); }
        }

        private void AppendLimited(TextBox tb, string text, bool scrollToCaret = true, int maxChars = 200000)
        {
            if (tb == null || tb.IsDisposed) return;
            if (tb.InvokeRequired)
            {
                tb.BeginInvoke(new Action<TextBox, string, bool, int>(AppendLimited), tb, text, scrollToCaret, maxChars);
                return;
            }
            tb.AppendText(text);
            if (tb.TextLength > maxChars)
            {
                int keep = tb.TextLength - maxChars;
                int cut = tb.Text.IndexOf('\n', keep);
                if (cut < 0) cut = keep;
                tb.Text = tb.Text.Substring(cut + 1);
            }
            if (scrollToCaret) tb.ScrollToCaret();
        }

        private void ShowMsgClient(string str)
        {
            AppendLimited(textBox7, str + "\r\n");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                string str = textBox8.Text;
                byte[] buffer;
                buffer = null;
                if (jinzhi==1)
                {
                    // ★ 2026-09-11：修复 hex 手动发送发 00 00 00 00。
                    //   旧代码 int.TryParse("A5 01")必失败→0，再 Getint→发出四个零字节。
                    //   统一复用 BuildSendBuffer，与 changeok/changeng/监听线程路径保持一致。
                    buffer = BuildSendBuffer(str, true);
                }
                else
                    buffer = System.Text.Encoding.Default.GetBytes(str);
                List<byte> list = new List<byte>();
                //list.Add(0);
                list.AddRange(buffer);
                //将泛型集合转换为数组
                byte[] newBuffer = list.ToArray();
                socketClient.Send(newBuffer);
            }
            catch
            { }
        }
        Socket socketWatch;
        private volatile bool _listenStarted = false;   // TCP 服务器监听防重（chushihua 与 button1 都可启动）
        System.Collections.Concurrent.ConcurrentDictionary<string, Socket> serverSocket = new System.Collections.Concurrent.ConcurrentDictionary<string, Socket>();
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (_listenStarted) return;
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Any;
                //IPAddress ip = IPAddress.Parse("192.168.88.1");
                IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(textBox3.Text));
                socketWatch.Bind(point);
                //监听
                ShowMsg("监听成功");
                socketWatch.Listen(10);
                _listenStarted = true;   // Bind/Listen 成功后才置位，失败可重试
                Thread td = new Thread(Listen);
                td.IsBackground = true;
                td.Start(socketWatch);
                //等待客户端连接
            }
            catch (Exception ex)
            {
                // TCP 服务器启动失败，最常见是端口被占。把真实原因打出来，避免“点了没反应”
                string msg = "TCP服务器(端口" + textBox3.Text + ")启动失败：" + ex.GetType().Name + "，" + ex.Message;
                ShowMsg(msg);
                MsgErroeLog.WriteLog(msg + "，来自Form3.TCP服务器 button1");
                try { if (socketWatch != null) socketWatch.Close(); } catch { }
            }
        }
        private void ShowMsg(string str)
        {
            AppendLimited(textBox1, str + "\r\n");
        }
        // Connect 带超时（目标不可达时不阻塞线程数秒~20s）
        private static bool ConnectWithTimeout(Socket s, IPEndPoint ep, int timeoutMs)
        {
            try
            {
                IAsyncResult ar = s.BeginConnect(ep, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(timeoutMs) || !s.Connected)
                {
                    try { s.EndConnect(ar); } catch { }   // 释放 AsyncResult 句柄
                    try { s.Close(); } catch { }
                    return false;
                }
                s.EndConnect(ar);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// TCP 服务器监听线程：异常退出并记录日志，避免空转
        /// </summary>
        private void Listen(object o)
        {
            socketWatch = o as Socket;
            while (true)
            {
                Socket accepted = null;
                try
                {
                    accepted = socketWatch.Accept();
                }
                catch (Exception ex)
                {
                    // accept 异常退出监听线程，避免死循环空转 CPU 100%
                    _listenStarted = false;   // 允许重新启动监听
                    try { if (socketWatch != null) socketWatch.Close(); } catch { }
                    // 区分“主动关闭服务器”(正常停止，不算故障) 与 “端口冲突/被关闭等真故障”
                    bool isShutdown = ex is SocketException
                        && (((SocketException)ex).SocketErrorCode == SocketError.Interrupted
                            || ((SocketException)ex).SocketErrorCode == SocketError.OperationAborted
                            || ((SocketException)ex).SocketErrorCode == SocketError.ConnectionReset);
                    if (isShutdown)
                    {
                        ShowMsg("TCP服务器(端口" + SafeRead(() => textBox3.Text) + ")已停止监听");
                        MsgErroeLog.WriteLog("TCP服务器(端口" + SafeRead(() => textBox3.Text) + ")监听线程正常退出，服务器已停止");
                    }
                    else
                    {
                        ShowMsg("TCP服务器(端口" + SafeRead(() => textBox3.Text) + ")监听异常：" + ex.Message);
                        MsgErroeLog.WriteLog("TCP服务器(端口" + SafeRead(() => textBox3.Text) + ",本机监听)监听线程异常退出，原因：" + ex.GetType().Name
                            + "，" + ex.Message + "，来自Form3.TCP服务器");
                    }
                    break;
                }
                // ★ 单连接后处理独立 try：登记/回显/起接收线程任一异常只丢弃该连接，绝不允许打死监听线程。
                // 原实现与 Accept 共用一个 catch：扫码枪类客户端秒连秒断时 RemoteEndPoint 为 null 抛 NRE，
                // 整个 Listen 直接 break —— 服务器永久停听，只能手工重启。
                try
                {
                    // 取远端地址字符串（客户端秒连秒断时 RemoteEndPoint 可能失效，取不到即视为无效连接丢弃）
                    string ep = null;
                    try { ep = accepted.RemoteEndPoint == null ? null : accepted.RemoteEndPoint.ToString(); } catch { }
                    if (string.IsNullOrEmpty(ep))
                    {
                        try { accepted.Close(); } catch { }
                        continue;   // 丢弃该无效连接，监听继续
                    }
                    // 同地址客户端重连时先移除旧连接，避免字典残留旧 socket
                    Socket old;
                    serverSocket.TryRemove(ep, out old);
                    if (old != null) { try { old.Close(); } catch { } }
                    serverSocket.TryAdd(ep, accepted);
                    //将远程连接的IP地址和端口号填入下拉菜单（★P3.1：Listen 线程后台改 comboBox1，包 SafeUi 走 BeginInvoke，
                    // 避免 Debug 开跨线程校验时后台直接改控件抛异常被 catch 吞掉导致监听线程退出）
                    SafeUi(() =>
                    {
                        comboBox1.Items.Add(ep);
                        comboBox1.Text = ep;
                    });
                    ShowMsg(ep + "连接成功，我是服务器");
                    Thread th1 = new Thread(Receive);
                    th1.IsBackground = true;
                    th1.Start(accepted);
                }
                catch (Exception exConn)
                {
                    // 单连接初始化失败：记录日志并关闭该连接，监听线程继续（不再因单个客户端异常而停听）
                    try { accepted.Close(); } catch { }
                    try { MsgErroeLog.WriteLog("TCP服务器单连接初始化异常(监听未受影响，继续监听)：" + exConn.GetType().Name + "，" + exConn.Message + "，来自Form3.TCP服务器"); } catch { }
                }
            }
        }
        string rcv1;
        /// <summary>
        /// TCP 服务器接收线程：单连接独立缓冲，断开自动清理
        /// </summary>
        private void Receive(object o)
        {
            Socket sk = o as Socket;
            byte[] buf = new byte[64 * 1024];   // 每个连接独立缓冲，避免多连接共享字段竞态
            string connBuf = "";                // ★ 本连接独立组帧缓冲（方法局部变量，多连接天然不串扰）
            while (true)
            {
                try
                {

                    //连接成功后，接受客户端发过来的消息
                    //实际接收到的有效字节数
                    int r = sk.Receive(buf);
                    if (r == 0)
                    {
                        // 服务端正常关闭连接：从列表移除并关闭，避免 FD 泄漏和残留死连接
                        CleanupServerSocket(sk);
                        break;
                    }
                    //发送的文字消息
                    string str = Encoding.Default.GetString(buf, 0, r);
                    if (jinzhi == 1)
                        str = str.Replace('\0', '0');
                    ShowMsg(sk.RemoteEndPoint + ";" + str+"\r\n");
                    rcv1 = str;
                    if (qiehuan_fangshi == "Tcp_server")
                    {
                        DispatchTcpFrame(str, ref connBuf);
                    }
                }
                catch
                {
                    // 连接断开/异常：退出接收线程并从列表移除，避免死循环空转 CPU 100%
                    CleanupServerSocket(sk);
                    break;
                }
            }
        }

        // TCP 服务器：连接关闭/异常时统一清理（从字典移除并关闭 socket）
        private void CleanupServerSocket(Socket sk)
        {
            try
            {
                Socket rm;
                serverSocket.TryRemove(sk.RemoteEndPoint.ToString(), out rm);
                if (rm != null) { try { rm.Close(); } catch { } }
                else { try { sk.Close(); } catch { } }
            }
            catch { }
        }
        #endregion
        #region 切型匹配与串口通讯
        /// <summary>
        /// 切型匹配：接收内容与配置的切型字符串比较，命中返回 1
        /// </summary>
        private int qiehuan(string aa)
        {
            // ★ P3.1：后台接收线程高频调用本方法，改读同步缓存（UI 线程 Initialize/TextChanged 写入），
            // 避免后台线程直接访问 8 个 TextBox 控件（Debug 开跨线程校验会抛异常）。行为与逐框比较一致。
            string[] p;
            lock (_trigSync) p = _trigPath;
            // 命中才读对应方案路径框（低频）；后台线程读控件经 SafeRead 走 Invoke，未命中则不产生任何 Invoke。
            if (p[0] == aa) { zifu = aa; lujing = SafeRead(() => textBox10.Text); return 1; }
            if (p[1] == aa) { zifu = aa; lujing = SafeRead(() => textBox11.Text); return 1; }
            if (p[2] == aa) { zifu = aa; lujing = SafeRead(() => textBox12.Text); return 1; }
            if (p[3] == aa) { zifu = aa; lujing = SafeRead(() => textBox13.Text); return 1; }
            if (p[4] == aa) { zifu = aa; lujing = SafeRead(() => textBox14.Text); return 1; }
            if (p[5] == aa) { zifu = aa; lujing = SafeRead(() => textBox15.Text); return 1; }
            if (p[6] == aa) { zifu = aa; lujing = SafeRead(() => textBox16.Text); return 1; }
            if (p[7] == aa) { zifu = aa; lujing = SafeRead(() => textBox17.Text); return 1; }
            return 0;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                string str = textBox2.Text;
                byte[] buffer;
                buffer = null;
                if (jinzhi==1)
                {
                    // hex 模式直接按 16 进制串解析；旧的 int.TryParse+Getint 会发 00 00 00 00
                    buffer = BuildSendBuffer(str, true);
                }
                else
                {
                    // byte[] buffer=Convert.ToByte(StringToHexOrDec(str));
                    buffer = System.Text.Encoding.Default.GetBytes(str);
                }
                if (comboBox1.SelectedItem == null) { MsgErroeLog.WriteLog("[Form3-TCP服务器] 发送失败: 未选择客户端"); return; }
                string ip = comboBox1.SelectedItem.ToString();
                Socket s;
                if (serverSocket.TryGetValue(ip, out s) && s != null)
                    s.Send(buffer);
                else
                    MsgErroeLog.WriteLog("[Form3-TCP服务器] 发送失败: 客户端已断开 " + ip);
                //  soketSend.Send(buffer);
            }
            catch { }
        }

        private void btnSetSp_Click(object sender, EventArgs e)
        {
            try
            {
                timer1.Enabled = false;
               CloseSerialMain();
                button6.Text = "打开串口";   // 同步按钮状态，防止 clientmonitor 自动重开旧参数串口
                Form4 frm4 = new Form4();
                frm4.Target = this;   // 确认结果直接写回本窗 Cur*（主窗=静态字段/额外整窗=实例字段），不再经共享静态中转串扰其它窗
                if (frm4.ShowDialog() == DialogResult.OK)
                {
                   mdcan.port.PortName = CurPortName;
                   mdcan.port.BaudRate = int.Parse(CurBaudRate);
                   mdcan.port.StopBits = (StopBits)int.Parse(CurStopBits);
                   mdcan.port.DataBits = int.Parse(CurDataBits);
                    if (CurParity == "0")
                    {
                        mdcan.port.Parity = Parity.None;
                    }
                    else if (CurParity == "1")
                    {
                        mdcan.port.Parity = Parity.Odd;
                    }
                    else if (CurParity == "2")
                    {
                        mdcan.port.Parity = Parity.Even;
                    }
                    mdcan.port.ReadTimeout = 500;
                }
            }
            catch { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // ★ P3.1：原"按文件逐行发送"功能已废弃（sRead 赋值点被注释、timer1 无启动点）。
            // 加空保护：即使将来误启用 timer1，也不对 null 的 sRead.ReadLine() 抛 NRE。
            if (sRead == null) return;
            string str1;
            str1 = sRead.ReadLine();
            if (str1 != null)
            {
                timer1.Stop();
                sRead.Close();
                MessageBox.Show("发送文件成功！", "c#串口通讯");
              //this.label9.Text = "";
                return;
            }
            byte[] data = Encoding.Default.GetBytes(str1);
           mdcan.port.Write(data, 0, data.Length);
          //  this.label9.Text = "数据发送中······";
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            string str =mdcan.port.ReadExisting();
            string str2 = str.Replace("\r", "\r\n");
            AppendLimited(txtReceive, str2);
        }

        private void btnclear_Click(object sender, EventArgs e)
        {
            try
            {
                string path = Directory.GetCurrentDirectory() + @"\output.txt";
                string content = this.txtReceive.Text;
                FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter write = new StreamWriter(fs);
                write.Write(content);
                write.Flush();
                write.Close();
                fs.Close();
                MessageBox.Show("接收数据在：" + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (button6.Text == "打开串口")
            {
                if (HasSerialCfg)
                {
                    try
                    {
                        string serr;
                        if (mdcan.port.IsOpen)
                            CloseSerialMain();
                        if (TryOpenSerialMain(mdcan.port.PortName, out serr))
                        {
                            button6.Text = "关闭串口";
                            groupBox1.Enabled = true;
                            groupBox2.Enabled = true;
                            this.label5.Text = "端口号：" +mdcan.port.PortName + "|";
                            this.label6.Text = "波特率：" +mdcan.port.BaudRate + "|";
                            this.label7.Text = "数据位：" +mdcan.port.DataBits + "|";
                            this.label8.Text = "停止位：" +mdcan.port.StopBits + "|";
                            this.label9.Text = "校验:" + mdcan.port.Parity;
                        }
                        else
                        {
                            MessageBox.Show(serr, "c#串口通讯");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("错误：" + ex.Message, "c#串口通讯");

                    }
                }
                else
                    MessageBox.Show("请先设置串口!", "RS232串口通讯");
            }
            else
            {
                timer1.Enabled = false;
                timer2.Enabled = false;
                button6.Text = "打开串口";
                CloseSerialMain();
                groupBox1.Enabled = false;
                groupBox2.Enabled = false;
                this.label5.Text = "端口号：端口未打开|";
                this.label6.Text = "波特率：端口未打开|";
                this.label7.Text = "数据位：端口未打开|";
                this.label8.Text = "停止位：端口未打开|";
                this.label9.Text = "校验: 端口未打开";
            }
        }

        private void btnSendData_Click(object sender, EventArgs e)
        {
            if (mdcan.port.IsOpen)
            {
                try
                {
                    string send = "";
                   // byte[] vbyte = null;
                   mdcan.port.Encoding = System.Text.Encoding.GetEncoding("GB2312");
                    if (jinzhi==1)
                    {
                        //string sHex = texSend.Text.Replace(" ", "");
                        //if (sHex.Length > 0 && (sHex.Length % 2 == 0))
                        //{
                        //    vbyte = new byte[sHex.Length / 2];
                        //    for (int i = 0; i < sHex.Length; i = i + 2)
                        //    {
                        //        if (!byte.TryParse(sHex.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                        //            vbyte[i / 2] = 0;
                        //    }
                        //    send = ASCIIEncoding.Default.GetString(vbyte);
                        //   mdcan.port.Write(vbyte, 0, 8);

                        //}
                        // hex 模式直接按 16 进制串解析；旧的 int.TryParse+Getint 会发 00 00 00 00
                        byte[] tempdata = BuildSendBuffer(texSend.Text, true);
                        mdcan.port.Write(tempdata, 0, tempdata.Length);
                    }
                    else
                    {
                        send = texSend.Text;
                       mdcan.port.Write(send);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("错误", ex.Message);
                }
            }
            else
            {
                MessageBox.Show("请先打开串口！");
            }
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog oFD = new OpenFileDialog();
            oFD.InitialDirectory = "C\\";
            oFD.RestoreDirectory = true;
            oFD.FilterIndex = 1;
            oFD.Filter = "txt文件(*.txt)|*.txt";
            if (oFD.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (oFD.OpenFile() != null)
                        txtFileName.Text = oFD.FileName;
                }
                catch (Exception err1)
                {
                    MessageBox.Show("文件打开错误！" + err1.Message, "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            //string filename = txtFileName.Text.Trim();
            //if (filename == "")
            //{
            //    MessageBox.Show("请选择要发送的文件！", "Error");
            //    return;
            //}
            //else
            //{
            //    sRead = new StreamReader(filename, Encoding.Default);
            //}
            //timer1.Start();
        }

        private void btnReceive_Click(object sender, EventArgs e)
        {
            if (btnReceive.Text == "接收数据")
            {
               mdcan.port.Encoding = Encoding.GetEncoding("GB2312");
                if (mdcan.port.IsOpen)
                {
                    bAccpet = true;
                    getRecevice = new Thread(new ThreadStart(testdelegate));
                    getRecevice.IsBackground = true;
                    getRecevice.Start();
                    btnReceive.Text = "停止接收";
                }
                else
                    MessageBox.Show("请打开串口!");
            }
            else
            {
                bAccpet = false;
                try
                {
                    if (getRecevice != null)
                    {
                        if (getRecevice.IsAlive)
                        {
                            // ReadExisting 非阻塞，标志复位后线程很快退出；不再使用 Abort（中断时机不可控）
                            // 循环等待线程真正退出，防止旧线程未退出时再次启动导致双接收线程并发读串口
                            for (int w = 0; w < 20 && getRecevice.IsAlive; w++)
                                getRecevice.Join(50);
                        }
                        getRecevice = null;
                    }
                }
                catch { }
                btnReceive.Text = "接收数据";
            }
        }
        private void testdelegate()
        {
            reaction r = new reaction(fun);
            r();
        }
        delegate void DelegateAcceptData();
        delegate void reaction();
        void fun()
        {
            while (bAccpet)
            {
                AcceptData();
                Thread.Sleep(10);   // 无数据时避免 ReadExisting 空转 CPU 100%
            }
        }
        void AcceptData()
        {
            if (txtReceive.InvokeRequired)
            {
                try
                {
                    DelegateAcceptData ddd = new DelegateAcceptData(AcceptData);
                    this.Invoke(ddd, new object[] { });
                }
                catch { }
            }
            else
            {
                try
                {
                    strReceive = mdcan.port.ReadExisting();
                    if (strReceive.Length > 0)
                    {
                        AppendLimited(txtReceive, strReceive, scrollToCaret: false);
                        if (qiehuan_fangshi == "Serial")
                        {
                            // 组帧：按换行/累积缓冲匹配，解决串口分片导致切型失败
                            _serialBuf += strReceive;
                            while (_serialBuf.Length > 0)
                            {
                                int idx = _serialBuf.IndexOf('\n');
                                string line;
                                if (idx >= 0)
                                {
                                    line = _serialBuf.Substring(0, idx);
                                    _serialBuf = _serialBuf.Substring(idx + 1);
                                }
                                else if (_serialBuf.Length >= 512)
                                {
                                    line = _serialBuf;
                                    _serialBuf = "";
                                }
                                else
                                    break;   // 半包，等待下一次数据
                                line = line.Trim().TrimEnd('\r', '\0');
                                if (line.Length > 0)
                                {
                                    DispatchReceivedFrame(line);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void btn_Ok_Click(object sender, EventArgs e)
        {
            if (getData != null)
            {
                SelectionChangedEventArgs E = new SelectionChangedEventArgs(textBox9.Text);
                E.LinkId = _linkId;
                getData(this, E);
            }
        }
        public class SelectionChangedEventArgs : EventArgs
        {

            private string m_selection;



            //本属性用于传递事件数据

            public string Selection
            {

                get { return m_selection; }

            }

            // 来源链路号：连接 2~4 触发经主窗宿主转发时保留“是谁发的”这一身份，
            // 供 Form1.DataChange 记录到相机，使检测结果能回到发出触发的那条无协议连接的端口。
            public int LinkId;
            public SelectionChangedEventArgs(string selection)
            {

                m_selection = selection;

            }
        }
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (checkBox1.CheckState == CheckState.Checked)
            {
                //e.Handled = e.KeyChar < '0' || e.KeyChar > '9';  //允许输入数字
                e.Handled = !((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar >= 'a' && e.KeyChar <= 'f') || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || (e.KeyChar == ' '));
                if (e.KeyChar == (char)8)  //允许输入回退键
                {
                    e.Handled = false;
                }
                if (e.KeyChar == 'x')  //允许输入‘x’
                {
                    e.Handled = false;
                }
                if (e.KeyChar == 'X')  //允许输入'X'
                {
                    e.Handled = false;
                }
            }
        }

        private void textBox8_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (checkBox1.CheckState == CheckState.Checked)
            {
                //e.Handled = e.KeyChar < '0' || e.KeyChar > '9';  //允许输入数字
                e.Handled = !((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar >= 'a' && e.KeyChar <= 'f') || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || (e.KeyChar == ' '));
                if (e.KeyChar == (char)8)  //允许输入回退键
                {
                    e.Handled = false;
                }
                if (e.KeyChar == 'x')  //允许输入‘x’
                {
                    e.Handled = false;
                }
                if (e.KeyChar == 'X')  //允许输入'X'
                {
                    e.Handled = false;
                }
            }
        }
        public static int StringToHexOrDec(string strData)
        {
            int dData = -1;
            try
            {
                if ((strData.Length > 2))
                {
                    if ((strData.Substring(0, 2).Equals("0x")) || (strData.Substring(0, 2).Equals("0X")))
                    {
                        string str_sub = strData.Substring(2, strData.Length - 2);
                        dData = int.Parse(str_sub, System.Globalization.NumberStyles.HexNumber);
                    }
                    else
                    {
                        dData = int.Parse(strData, System.Globalization.NumberStyles.Integer);
                    }
                }
                else
                {
                    dData = int.Parse(strData, System.Globalization.NumberStyles.Integer);
                }
            }
            catch (Exception)
            {
                //MessageBox.Show("输入错误: " + strData, "错误");
            }
            return dData;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
                if (checkBox1.CheckState == CheckState.Checked)
                {
                    jinzhi = 1;
                    textBox2.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox2.Text)).Replace("-", " ");
                    textBox8.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox8.Text)).Replace("-", " ");
                    texSend.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(texSend.Text)).Replace("-", " ");
                    //textBox10.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox10.Text)).Replace("-", " ");
                    //textBox11.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox11.Text)).Replace("-", " ");
                    //textBox12.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox12.Text)).Replace("-", " ");
                    //textBox13.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox13.Text)).Replace("-", " ");
                    //textBox14.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox14.Text)).Replace("-", " ");
                    //textBox15.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox15.Text)).Replace("-", " ");
                    //textBox16.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox16.Text)).Replace("-", " ");
                    //textBox17.Text = BitConverter.ToString(ASCIIEncoding.Default.GetBytes(textBox17.Text)).Replace("-", " ");
                }
                else
                {
                    jinzhi = 0;
                    string sHex = textBox2.Text.Replace(" ", "");
                    if (sHex.Length > 0 && (sHex.Length % 2 == 0))
                    {
                        byte[] vbyte = new byte[sHex.Length / 2];
                        for (int i = 0; i < sHex.Length; i = i + 2)
                        {
                            if (!byte.TryParse(sHex.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                                vbyte[i / 2] = 0;
                        }
                        textBox2.Text = ASCIIEncoding.Default.GetString(vbyte);
                    }
                    string sHex1 = textBox8.Text.Replace(" ", "");
                    if (sHex1.Length > 0 && (sHex1.Length % 2 == 0))
                    {
                        byte[] vbyte = new byte[sHex1.Length / 2];
                        for (int i = 0; i < sHex1.Length; i = i + 2)
                        {
                            if (!byte.TryParse(sHex1.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                                vbyte[i / 2] = 0;
                        }
                        textBox8.Text = ASCIIEncoding.Default.GetString(vbyte);
                    }
                    string sHex2 = texSend.Text.Replace(" ", "");
                    if (sHex2.Length > 0 && (sHex2.Length % 2 == 0))
                    {
                        byte[] vbyte = new byte[sHex2.Length / 2];
                        for (int i = 0; i < sHex2.Length; i = i + 2)
                        {
                            if (!byte.TryParse(sHex2.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                                vbyte[i / 2] = 0;
                        }
                        texSend.Text = ASCIIEncoding.Default.GetString(vbyte);
                    }
                    //string sHex3 = textBox10.Text.Replace(" ", "");
                    //if (sHex3.Length > 0 && (sHex3.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex3.Length / 2];
                    //    for (int i = 0; i < sHex3.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex3.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox10.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                    //string sHex4 = textBox11.Text.Replace(" ", "");
                    //if (sHex4.Length > 0 && (sHex4.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex4.Length / 2];
                    //    for (int i = 0; i < sHex4.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex4.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox11.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                    //string sHex5 = textBox12.Text.Replace(" ", "");
                    //if (sHex5.Length > 0 && (sHex5.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex5.Length / 2];
                    //    for (int i = 0; i < sHex5.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex5.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox12.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                    //string sHex6 = textBox13.Text.Replace(" ", "");
                    //if (sHex6.Length > 0 && (sHex6.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex6.Length / 2];
                    //    for (int i = 0; i < sHex6.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex6.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox13.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                    //string sHex7 = textBox14.Text.Replace(" ", "");
                    //if (sHex7.Length > 0 && (sHex7.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex7.Length / 2];
                    //    for (int i = 0; i < sHex7.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex7.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox14.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                    //string sHex8 = textBox15.Text.Replace(" ", "");
                    //if (sHex8.Length > 0 && (sHex8.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex8.Length / 2];
                    //    for (int i = 0; i < sHex8.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex8.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox15.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                    //string sHex9 = textBox16.Text.Replace(" ", "");
                    //if (sHex9.Length > 0 && (sHex9.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex9.Length / 2];
                    //    for (int i = 0; i < sHex9.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex9.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox16.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                    //string sHex10 = textBox17.Text.Replace(" ", "");
                    //if (sHex10.Length > 0 && (sHex10.Length % 2 == 0))
                    //{
                    //    byte[] vbyte = new byte[sHex10.Length / 2];
                    //    for (int i = 0; i < sHex10.Length; i = i + 2)
                    //    {
                    //        if (!byte.TryParse(sHex10.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
                    //            vbyte[i / 2] = 0;
                    //    }
                    //    textBox17.Text = ASCIIEncoding.Default.GetString(vbyte);
                    //}
                }
        }

        private void texSend_TextChanged(object sender, EventArgs e)
        {

        }

        private void texSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (checkBox1.CheckState == CheckState.Checked)
            {
                //e.Handled = e.KeyChar < '0' || e.KeyChar > '9';  //允许输入数字
                e.Handled = !((e.KeyChar >= '0' && e.KeyChar <= '9') || (e.KeyChar >= 'a' && e.KeyChar <= 'f') || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || (e.KeyChar == ' '));
                if (e.KeyChar == (char)8)  //允许输入回退键
                {
                    e.Handled = false;
                }
                if (e.KeyChar == 'x')  //允许输入‘x’
                {
                    e.Handled = false;
                }
                if (e.KeyChar == 'X')  //允许输入'X'
                {
                    e.Handled = false;
                }
            }
        }

        private void textBox26_TextChanged(object sender, EventArgs e)
        {
            // 批量框：把值应用到全部 12 台相机，并同步各相机独立输入框
            if (f1 != null && IsMainWindow) f1.SetTriggerZifu(textBox26.Text);
            if (_photoBoxes != null)
            {
                for (int _pi = 0; _pi < 12; _pi++)
                    _photoBoxes[_pi].Text = textBox26.Text;
            }
        }

        private void photoTrigger_TextChanged(object sender, EventArgs e)
        {
            // 单相机拍照触发字符变动，按控件名解析相机索引（textBoxP1..P12 -> 0..11）
            var _tb = sender as TextBox;
            if (_tb == null) return;
            int _idx;
            if (int.TryParse(_tb.Name.Substring("textBoxP".Length), out _idx))
            {
                _idx -= 1;
                if (_idx >= 0 && _idx < 12 && f1 != null && IsMainWindow)
                    f1.SetTriggerZifu(_idx, _tb.Text);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
        
            wdini.WriteString("change", "1", textBox18.Text);
            wdini.WriteString("change", "2", textBox19.Text);
            wdini.WriteString("change", "3", textBox21.Text);
            wdini.WriteString("change", "4", textBox20.Text);
            wdini.WriteString("change", "5", textBox23.Text);
            wdini.WriteString("change", "6", textBox22.Text);
            wdini.WriteString("change", "7", textBox25.Text);
            wdini.WriteString("change", "8", textBox24.Text);
            wdini.WriteString("change", "triggerzifu", textBox26.Text);
            if (_photoBoxes != null)
                for (int _pi = 0; _pi < 12; _pi++)
                    wdini.WriteString("change", "triggerzifu" + (_pi + 1), _photoBoxes[_pi].Text);
            wdini.WriteString("change", "all", comboBox2.Text);
            wdini.WriteString("path", "1", textBox10.Text);
            wdini.WriteString("path", "2", textBox11.Text);
            wdini.WriteString("path", "3", textBox12.Text);
            wdini.WriteString("path", "4", textBox13.Text);
            wdini.WriteString("path", "5", textBox14.Text);
            wdini.WriteString("path", "6", textBox15.Text);
            wdini.WriteString("path", "7", textBox16.Text);
            wdini.WriteString("path", "8", textBox17.Text);
     
        
            wdini.WriteString("serial", "strportName", CurPortName);
            wdini.WriteString("serial", "strbaudRate", CurBaudRate);
            wdini.WriteString("serial", "strStopBits", CurStopBits);
            wdini.WriteString("serial", "strDataBits", CurDataBits);
            wdini.WriteString("serial","strjiaoyan",CurParity);
            // WritePrivateProfileString("Test", "id", "xym", "d://vc//Ex1//ex1.ini");
            wdini.WriteString("tcpserver", "port1", textBox3.Text);
            wdini.WriteString("tcpserver", "ip1", textBox4.Text);
       
            wdini.WriteString("tcpsend", "send1", textBox2.Text);
            wdini.WriteString("tcpclient", "port2", textBox5.Text);
            wdini.WriteString("tcpclient", "ip2", textBox6.Text);
            wdini.WriteString("out0", "ok", textBox10.Text);
            wdini.WriteString("out1", "ok", textBox11.Text);
            wdini.WriteString("out2", "ok", textBox12.Text);
            wdini.WriteString("out3", "ok", textBox13.Text);
            wdini.WriteString("out0", "ng", textBox14.Text);
            wdini.WriteString("out1", "ng", textBox15.Text);
            wdini.WriteString("out2", "ng", textBox16.Text);
            wdini.WriteString("out3", "ng", textBox17.Text);
            if(checkBox1.CheckState==CheckState.Checked)
            wdini.WriteString("jinzhi", "16en", "true");
            else
            wdini.WriteString("jinzhi", "16en", "false");
        
            if (checkBox3.CheckState == CheckState.Checked)
                wdini.WriteString("tcpclient", "en", "true");
            else
                wdini.WriteString("tcpclient", "en", "false");    
            if (checkBox2.CheckState == CheckState.Checked)
                wdini.WriteString("tcpserver", "en", "true");
            else
                wdini.WriteString("tcpserver", "en", "false");
            if (checkBox4.CheckState == CheckState.Checked)
                wdini.WriteString("serial", "en", "true");
            else
                wdini.WriteString("serial", "en", "false");
           

        }

     
      

        private void Form3_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
                return;
            if (MessageBox.Show("将要关闭检测，是否继续？", "询问", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                e.Cancel = true;
                this.Visible = false;
            }
            else
            {
                e.Cancel = true;
            }
        }
        private delegate void lttext1(string aa);
        private void settext1(string aa)
        {
            if (textBox9.InvokeRequired)
            {
                lttext1 l1 = settext1;
                textBox9.Invoke(l1, aa);
            }
            else
            {
                textBox9.Text = aa;
            }
        }
        private object locker_1 = new object();
        // 阶段 7（整窗化）：无协议连接 2~4 的运行时已移入“连接设备”管理器各自的独立整窗实例（每实例自持 NoProtoLink）。
        // 本窗体仅保留共享的切型/getData 管线，供各整窗实例把收到的帧并入：
        // 命中切型记录 zifu/lujing，未命中抛给 Form1 的 getData/DataChange（与既有接收线程同一条管线）。
        // isTcp=true 表示帧来自 TCP 客户机/服务器链路（与窗体既有 TCP 接收路径一致：16 进制下 \0→'0'）；串口帧不回退。
        public void RouteReceivedFrame(string frame, bool isTcp)
        {
            if (string.IsNullOrEmpty(frame)) return;
            try
            {
                if (isTcp && jinzhi == 1)
                    frame = frame.Replace('\0', '0');
                if (qiehuan(frame) == 0)
                {
                    SelectionChangedEventArgs E = new SelectionChangedEventArgs(frame);
                    E.LinkId = _linkId;
                    if (getData != null) getData(this, E);
                }
            }
            catch { }
        }

        /// <summary>
        /// 接收帧统一分发（串口 / TCP 客户机 / TCP 服务器共用）：
        /// 命中切型由 qiehuan() 记 zifu/lujing（连接 1 与既有行为完全一致）；
        /// 未命中抛给 getData 触发相机。额外整窗（连接 2~4）把结果并入主窗宿主：
        /// 未命中→宿主 getData（相机触发与连接1同一条管线）；命中→把 lujing 并入宿主，
        /// 由 Form1.timer10 完成与连接 1 一致的切型（保持整机单一切型入口）。
        /// </summary>
        private void DispatchReceivedFrame(string frame)
        {
            try
            {
                if (qiehuan(frame) == 0)
                {
                    if (IsMainWindow || HostMain == null)
                    {
                        SelectionChangedEventArgs E = new SelectionChangedEventArgs(frame);
                        E.LinkId = _linkId;
                        if (getData != null) getData(this, E);
                    }
                    else
                    {
                        HostMain.RaiseCameraTrigger(frame, _linkId);
                    }
                }
                else if (!IsMainWindow && HostMain != null && !string.IsNullOrEmpty(lujing))
                {
                    HostMain.lujing = lujing;
                    HostMain.zifu = zifu;
                }
            }
            catch { }
        }

        /// <summary>把“未命中切型”的外部链路帧并入本窗 getData（相机触发）入口；额外整窗经此把帧并入主窗宿主。</summary>
        public void RaiseCameraTrigger(string frame, int sourceLink)
        {
            try
            {
                if (string.IsNullOrEmpty(frame)) return;
                SelectionChangedEventArgs E = new SelectionChangedEventArgs(frame);
                E.LinkId = sourceLink;
                if (getData != null) getData(this, E);
            }
            catch { }
        }

        // ===================== 整窗实例生命周期（阶段 8A：无协议连接 2~4 由管理器持有的完整 Form3 实例） =====================
        /// <summary>创建连接 N（N&gt;=2）的独立配置文件 test_noprotoN.ini（与连接 1 的 test.ini 同构的段/键）。
        /// 三通道默认 en=false，避免新窗口一打开就用缺省串口自动连接。文件已存在则保留原配置不动。</summary>
        public static void SeedLinkIniFile(int linkId)
        {
            if (linkId < 2) return;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "//test_noproto" + linkId + ".ini";
                if (System.IO.File.Exists(path)) return;
                var ini = new ClassIni();
                ini.ReadINIFile(path);
                ini.WriteString("serial", "strportName", "Com1");
                ini.WriteString("serial", "strbaudRate", "9600");
                ini.WriteString("serial", "strStopBits", "1");
                ini.WriteString("serial", "strDataBits", "8");
                ini.WriteString("serial", "strjiaoyan", "0");
                ini.WriteString("serial", "en", "false");
                ini.WriteString("tcpclient", "ip2", "127.0.0.1");
                ini.WriteString("tcpclient", "port2", "5020");
                ini.WriteString("tcpclient", "en", "false");
                ini.WriteString("tcpserver", "ip1", "");
                ini.WriteString("tcpserver", "port1", "9001");
                ini.WriteString("tcpserver", "en", "false");
                ini.WriteString("tcpsend", "send1", "");
                ini.WriteString("jinzhi", "16en", "false");
                // “切换方式”默认串口：三通道收帧以 qiehuan_fangshi 为前提，缺省必须给一个可用值，
                // 否则该窗即使收到帧也不触发相机/切型（有 legacy 时由下面的重载按实际类型改写）。
                ini.WriteString("change", "all", "Serial");
            }
            catch { }
        }

        /// <summary>创建连接 N 独立配置文件并迁移旧 [noprotoN] 引擎配置（阶段 5/7 升级场景）：
        /// legacy 携带旧 kind/en/串口或 IP 参数 → 写入对应通道并置 en，使升级前已启用的无协议连接继续可用；
        /// legacy 为 null 时等价于 SeedLinkIniFile(int)。</summary>
        public static void SeedLinkIniFile(int linkId, NoProtoLinkConfig legacy)
        {
            if (linkId < 2) return;
            string path = AppDomain.CurrentDomain.BaseDirectory + "//test_noproto" + linkId + ".ini";
            // 独立文件已存在（用户配置过或已迁移过）：绝不用旧 [noprotoN] 段覆盖，直接返回。
            if (System.IO.File.Exists(path)) return;
            SeedLinkIniFile(linkId);
            if (legacy == null) return;
            try
            {
                var ini = new ClassIni();
                ini.ReadINIFile(path);
                if (legacy.Kind == NoProtoKind.Serial)
                {
                    ini.WriteString("change", "all", "Serial");
                    ini.WriteString("serial", "strportName", string.IsNullOrEmpty(legacy.PortName) ? "Com1" : legacy.PortName);
                    ini.WriteString("serial", "strbaudRate", legacy.BaudRate.ToString());
                    ini.WriteString("serial", "strStopBits", legacy.StopBits.ToString());
                    ini.WriteString("serial", "strDataBits", legacy.DataBits.ToString());
                    ini.WriteString("serial", "strjiaoyan", legacy.Jiaoyan.ToString());
                    ini.WriteString("serial", "en", legacy.Enabled ? "true" : "false");
                }
                else if (legacy.Kind == NoProtoKind.TcpClient)
                {
                    ini.WriteString("change", "all", "Tcp_client");
                    ini.WriteString("tcpclient", "ip2", string.IsNullOrEmpty(legacy.Ip) ? "127.0.0.1" : legacy.Ip);
                    ini.WriteString("tcpclient", "port2", (legacy.Port > 0 ? legacy.Port : 5020).ToString());
                    ini.WriteString("tcpclient", "en", legacy.Enabled ? "true" : "false");
                }
                else if (legacy.Kind == NoProtoKind.TcpServer)
                {
                    ini.WriteString("change", "all", "Tcp_server");
                    ini.WriteString("tcpserver", "port1", (legacy.Port > 0 ? legacy.Port : 9001).ToString());
                    ini.WriteString("tcpserver", "en", legacy.Enabled ? "true" : "false");
                }
            }
            catch { }
        }

        /// <summary>删除连接 N（N&gt;=2）的独立配置文件（管理器“－删除”时调用）。</summary>
        public static void DeleteLinkIniFile(int linkId)
        {
            if (linkId < 2) return;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "//test_noproto" + linkId + ".ini";
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { }
        }

        /// <summary>供管理器启动/恢复调用：确保窗口句柄已创建并完成整窗初始化。
        /// 说明：WinForms 的 Load 事件只在首次 Show 时触发——管理器预建的隐藏窗（连接 2~4）不会自动 Load；
        /// 故本方法创建句柄后显式调用 InitializeForm（_initialized 门闩防重），按本窗三通道 en 自动启链，
        /// 与主窗“程序启动即建句柄、Load 自动开通道”的既有行为一致。</summary>
        internal void EnsureWindowHandle()
        {
            try { if (!IsHandleCreated) { IntPtr dummy = Handle; } } catch { }
            InitializeForm();
        }

        /// <summary>整窗实例释放（管理器删除/关闭时调用）：停三通道运行时并释放 COM 占用登记。
        /// 与 Form3_FormClosing 不同——不弹“关闭检测”询问，直接收尾；窗口本身由调用方 Dispose。</summary>
        internal void ReleaseInstance()
        {
            try
            {
                // 用 Invoke（同步）而非 BeginInvoke：调用方紧随其后就 Dispose，异步清理可能被整个跳过。
                if (InvokeRequired) { this.Invoke(new Action(ReleaseInstance)); return; }
                stop = true;
                _listenStarted = false;
                _tcpClientNeedReconnect = false;
                // 停串口接收线程（fun() 的 while(bAccpet)）：先复位标志再 Join，
                // 否则串口关闭/窗体 Dispose 后该线程仍每 10ms 读已释放串口（异常被吞）→ 空转线程泄漏。
                bAccpet = false;
                try
                {
                    if (getRecevice != null)
                    {
                        for (int w = 0; w < 10 && getRecevice.IsAlive; w++) getRecevice.Join(50);
                        getRecevice = null;
                    }
                }
                catch { }
                // 先把按钮复位为“打开串口”：阻止 clientmonitor 从 500ms 睡眠唤醒后按“关闭串口”旧条件自动重开
                try { if (button6 != null) button6.Text = "打开串口"; } catch { }
                // 串口：以 IsOpen 为准（不依赖按钮文本，覆盖自动开链后文本尚未刷新的竞态窗口），关闭并释放 COM 占用登记
                try
                {
                    if (mdcan != null && mdcan.port != null && mdcan.port.IsOpen)
                    {
                        timer1.Enabled = false;
                        timer2.Enabled = false;
                        CloseSerialMain();
                    }
                }
                catch { }
                // TCP 客户机
                try { if (socketClient != null) { socketClient.Close(); socketClient = null; } } catch { }
                // TCP 服务器监听与已接入客户端
                try { if (socketWatch != null) { socketWatch.Close(); socketWatch = null; } } catch { }
                try
                {
                    foreach (var kv in serverSocket)
                        try { kv.Value.Close(); } catch { }
                    serverSocket.Clear();
                }
                catch { }
            }
            catch { }
        }

        // ===================== COM 互斥防呆（阶段 7 补充）：Form3 主串口打开/关闭统一走占用表 =====================
        /// <summary>登记后打开主串口；被其它连接占用时不打开并返回提示文案。返回是否打开成功。</summary>
        private bool TryOpenSerialMain(string portName, out string errMsg)
        {
            errMsg = "";
            try
            {
                if (mdcan.port.IsOpen) return true;
                if (SerialPortGuard.IsComPort(portName))
                {
                    string busy;
                    if (!SerialPortGuard.TryAcquire(portName, SerialOwner, out busy))
                    {
                        errMsg = SerialPortGuard.OccupiedMessage(portName, busy);
                        return false;
                    }
                }
                mdcan.port.Open();
                if (mdcan.port.IsOpen)
                {
                    _lastMainSerialLog = "";
                    return true;
                }
                errMsg = "串口 " + portName + " 打开后未就绪";
                if (SerialPortGuard.IsComPort(portName)) SerialPortGuard.Release(portName, SerialOwner);
                return false;
            }
            catch (Exception ex)
            {
                if (SerialPortGuard.IsComPort(portName)) SerialPortGuard.Release(portName, SerialOwner);
                errMsg = "错误：" + ex.Message;
                return false;
            }
        }

        /// <summary>关闭主串口并释放 COM 互斥登记。</summary>
        private void CloseSerialMain()
        {
            string p = mdcan.port != null ? (mdcan.port.PortName ?? "") : "";
            try { if (mdcan.port != null && mdcan.port.IsOpen) mdcan.port.Close(); } catch { }
            if (SerialPortGuard.IsComPort(p)) SerialPortGuard.Release(p, SerialOwner);
        }
        private object locker_2 = new object();
        #endregion
        #region 发送接口（changeok/changeng）
        /// <summary>
        /// 合格(OK)发送：按勾选通道（TCP客户机/TCP服务器/串口）发送，统一互斥锁
        /// </summary>
                // 按 16 进制开关解析发送内容：勾选时按 hex 字节发送（修复原十进制解析 bug），否则按文本发送
        private static byte[] BuildSendBuffer(string text, bool hexMode)
        {
            if (hexMode)
            {
                string sHex = (text ?? "").Replace(" ", "");
                if (sHex.Length > 0 && sHex.Length % 2 == 0)
                {
                    byte[] vbyte = new byte[sHex.Length / 2];
                    for (int i = 0; i < sHex.Length; i = i + 2)
                    {
                        if (!byte.TryParse(sHex.Substring(i, 2), System.Globalization.NumberStyles.HexNumber, null, out vbyte[i / 2]))
                            vbyte[i / 2] = 0;
                    }
                    return vbyte;
                }
            }
            return System.Text.Encoding.Default.GetBytes(text ?? "");
        }

        /// <summary>
        /// 合格(OK)发送：按勾选通道（TCP客户机/TCP服务器/串口）同步发送，统一互斥锁保证顺序
        /// </summary>
        public void changeok(string aa)
        {
            lock (locker_1)
            {
                if (checkBox3.CheckState == CheckState.Checked)
                {
                    try { textBox8.Text = aa; } catch { }
                    oks++;
                    try
                    {
                        // ★ F21: 发送前校验连接，避免 socketClient 为 null 或断线时 NullRef/异常被吞
                        if (socketClient == null || !socketClient.Connected)
                        {
                            MsgErroeLog.WriteLog("[Form3-TCP客户机] 发送合格失败: 未连接 " + aa);
                        }
                        else
                        {
                            byte[] buffer = BuildSendBuffer(aa, checkBox1.CheckState == CheckState.Checked);
                            socketClient.SendTimeout = 2000;   // 对端不读时不无限阻塞
                            socketClient.Send(buffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        MsgErroeLog.WriteLog("[Form3-TCP客户机] 发送合格失败: " + aa + " " + ex.Message);
                    }
                }
                try { textBox9.Text = "1"; } catch { }
                if (checkBox2.CheckState == CheckState.Checked)
                {
                    try { textBox2.Text = aa; } catch { }
                    oks++;
                    try
                    {
                        byte[] buffer = BuildSendBuffer(aa, checkBox1.CheckState == CheckState.Checked);
                        string ip = comboBox1.SelectedItem == null ? "" : comboBox1.SelectedItem.ToString();
                        Socket s;
                        if (ip.Length > 0 && serverSocket.TryGetValue(ip, out s) && s != null)
                        {
                            // ★ 2026-09-13：TCP 服务端发送同样设置超时。客户机已有 SendTimeout，
                            //   服务端原来裸 Send()：对端连接正常但停止读取时，发送缓冲耗尽会永久阻塞；
                            //   该调用已在相机顺序队列内，阻塞会连带拖住后续 PLC/IO 输出，且占着本窗体发送锁。
                            try { s.SendTimeout = 2000; } catch { }
                            int sent = s.Send(buffer);
                            if (sent != buffer.Length)
                                MsgErroeLog.WriteLog("[Form3-TCP服务器] 发送不完整: " + sent + "/" + buffer.Length + " " + aa);
                        }
                        else
                            MsgErroeLog.WriteLog("[Form3-TCP服务器] 发送合格失败: 无客户端连接 " + aa);
                    }
                    catch (Exception ex)
                    {
                        MsgErroeLog.WriteLog("[Form3-TCP服务器] 发送合格失败: " + aa + " " + ex.Message);
                    }
                }
                if (checkBox4.CheckState == CheckState.Checked)
                {
                    if (mdcan.port.IsOpen)
                    {
                        try
                        {
                            byte[] buffer = BuildSendBuffer(aa, checkBox1.CheckState == CheckState.Checked);
                            mdcan.port.Encoding = System.Text.Encoding.GetEncoding("GB2312");
                            mdcan.port.Write(buffer, 0, buffer.Length);
                        }
                        catch (Exception ex)
                        {
                            MsgErroeLog.WriteLog("[Form3-串口] 发送合格失败: " + aa + " " + ex.Message);
                        }
                    }
                    else
                    {
                        monitor = "请先打开串口！";
                    }
                }
            }
        }

        /// <summary>
        /// 不合格(NG)发送：与 changeok 共用互斥锁，同步发送保证顺序
        /// </summary>
        public void changeng(string aa)
        {
            lock (locker_1)
            {
                if (checkBox3.CheckState == CheckState.Checked)
                {
                    try { textBox8.Text = aa; } catch { }
                    oks++;
                    try
                    {
                        // ★ F21: 发送前校验连接，避免 socketClient 为 null 或断线时 NullRef/异常被吞
                        if (socketClient == null || !socketClient.Connected)
                        {
                            MsgErroeLog.WriteLog("[Form3-TCP客户机] 发送NG失败: 未连接 " + aa);
                        }
                        else
                        {
                            byte[] buffer = BuildSendBuffer(aa, checkBox1.CheckState == CheckState.Checked);
                            socketClient.SendTimeout = 2000;   // 对端不读时不无限阻塞
                            socketClient.Send(buffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        MsgErroeLog.WriteLog("[Form3-TCP客户机] 发送NG失败: " + aa + " " + ex.Message);
                    }
                }
                try { textBox9.Text = "0"; } catch { }
                if (checkBox2.CheckState == CheckState.Checked)
                {
                    try { textBox2.Text = aa; } catch { }
                    ngs++;
                    try
                    {
                        byte[] buffer = BuildSendBuffer(aa, checkBox1.CheckState == CheckState.Checked);
                        string ip = comboBox1.SelectedItem == null ? "" : comboBox1.SelectedItem.ToString();
                        Socket s;
                        if (ip.Length > 0 && serverSocket.TryGetValue(ip, out s) && s != null)
                            s.Send(buffer);
                        else
                            MsgErroeLog.WriteLog("[Form3-TCP服务器] 发送NG失败: 无客户端连接 " + aa);
                    }
                    catch (Exception ex)
                    {
                        MsgErroeLog.WriteLog("[Form3-TCP服务器] 发送NG失败: " + aa + " " + ex.Message);
                    }
                }
                if (checkBox4.CheckState == CheckState.Checked)
                {
                    if (mdcan.port.IsOpen)
                    {
                        try
                        {
                            byte[] buffer = BuildSendBuffer(aa, checkBox1.CheckState == CheckState.Checked);
                            mdcan.port.Encoding = System.Text.Encoding.GetEncoding("GB2312");
                            mdcan.port.Write(buffer, 0, buffer.Length);
                        }
                        catch (Exception ex)
                        {
                            MsgErroeLog.WriteLog("[Form3-串口] 发送NG失败: " + aa + " " + ex.Message);
                        }
                    }
                    else
                    {
                        monitor = "请先打开串口！";
                    }
                }
            }
        }

        #endregion
        #region 窗体事件与工具方法
        private void textBox9_TextChanged_1(object sender, EventArgs e)
        {
      
            //try
            //{
                
            //    if (textBox9.Text == "0")
            //    {
            //        textBox2.Text = "A5 01 00 00 5A";
            //        ngs++;
            //        label10.Text = ngs.ToString();
            //    }
            //    if (textBox9.Text == "1")
            //    {
            //        textBox2.Text = "A5 01 00 01 5A";
            //        oks++;
            //        label11.Text = oks.ToString();
            //    }
            //      Task.Run(() =>
            //                           {
            //    try
            //    {
            //        string str = textBox2.Text;
            //        byte[] buffer;
            //        buffer = null;
            //        if (checkBox1.CheckState == CheckState.Checked)
            //        {
            //            string sHex = textBox2.Text.Replace(" ", "");
            //            if (sHex.Length > 0 && (sHex.Length % 2 == 0))
            //            {
            //                byte[] vbyte = new byte[sHex.Length / 2];
            //                for (int i = 0; i < sHex.Length; i = i + 2)
            //                {
            //                    if (!byte.TryParse(sHex.Substring(i, 2), NumberStyles.HexNumber, null, out vbyte[i / 2]))
            //                        vbyte[i / 2] = 0;
            //                }
            //                buffer = vbyte;
            //            }
            //        }
            //        else
            //        {
            //            // byte[] buffer=Convert.ToByte(StringToHexOrDec(str));
            //            buffer = System.Text.Encoding.Default.GetBytes(str);
            //        }
                   
            //        string ip = comboBox1.SelectedItem.ToString();
            //        serverSocket[ip].Send(buffer);
                                      
            //        //  soketSend.Send(buffer);
            //    }
            //    catch { }
            //                           });
            //}
            //catch { }
                                                  
        }

        private void button7_Click(object sender, EventArgs e)
        {
            monitor = "";
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            // ★ P3.1 备注：timer3 在 Designer 中未 Enabled、全工程无启动点，本为休眠的 TCP 客户机定时重连兜底，
            // 当前不会触发。若将来启用：它跑在 UI 线程，原先裸 Connect 同步阻塞可达 ~20s 会卡死界面，
            // 故改为 ConnectWithTimeout(3s)，且仅当 monitor 非空（上次连接失败）时重试。
            try
            {
                if (monitor != "" && checkBox3.CheckState == CheckState.Checked)
                {
                    socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPAddress ip = IPAddress.Parse(textBox6.Text.Trim());
                    IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(textBox5.Text));
                    if (!ConnectWithTimeout(socketClient, point, 3000))
                    {
                        try { socketClient.Close(); } catch { }
                        monitor = "Tcpclient连接失败";
                        return;
                    }
                    ShowMsgClient(socketClient.RemoteEndPoint + "连接成功,我是客户机");
                    //开启一个线程，不断的接收服务端发来的消息
                    Thread th = new Thread(receiveClient);
                    th.IsBackground = true;
                    th.Start();
                    monitor = "";
                }
            }
            catch { monitor = "Tcpclient连接失败"; }
        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox10_KeyPress(object sender, KeyPressEventArgs e)
        {
           
        }

        private void textBox11_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void textBox12_KeyPress(object sender, KeyPressEventArgs e)
        {
           
        }

        private void textBox13_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void textBox14_KeyPress(object sender, KeyPressEventArgs e)
        {
           
        }

        private void textBox15_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void textBox16_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void textBox17_KeyPress(object sender, KeyPressEventArgs e)
        {
           
        }

        private void textBox10_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox10.Text = openFileDialog.FileName;
            }
        }

        private void textBox11_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox11.Text = openFileDialog.FileName;
            }
        }

        private void textBox12_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox12.Text = openFileDialog.FileName;
            }
        }

        private void textBox13_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox13.Text = openFileDialog.FileName;
            }
        }

        private void textBox14_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox14.Text = openFileDialog.FileName;
            }
        }

        private void textBox15_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox15.Text = openFileDialog.FileName;
            }
        }

        private void textBox16_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox16.Text = openFileDialog.FileName;
            }
        }

        private void textBox17_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox17.Text = openFileDialog.FileName;
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
   
        }

        private void numericUpDown18_ValueChanged(object sender, EventArgs e)
        {
           
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
           
        }

        private void button8_Click(object sender, EventArgs e)
        {
           
        }
        public int Getint(int values,out byte [] data1)
        {
            int[] value = new int[] { values };
            var result = new byte[value.Length * sizeof(int)];
            Buffer.BlockCopy(value, 0, result, 0, result.Length);
            byte[] data;
            if (modbus_qufan==1)
            {
                data1 = new byte[4] { result[1], result[0], result[3], result[2] };
                data = new byte[4] { result[2], result[3], result[0], result[1] };
            }
            else
            {
                data1 = new byte[4] { result[3], result[2], result[1], result[0] };
                data = new byte[4] { result[0], result[1], result[2], result[3] };
            }
            value[0] = BitConverter.ToInt32(data, 0);
            return value[0];
        }
        private void button9_Click(object sender, EventArgs e)
        {
         
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

        private void button11_Click(object sender, EventArgs e)
        {
              
        }

        private void button10_Click(object sender, EventArgs e)
        {
     
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
        
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
        
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SyncQiehuanFangshi();
        }

        /// <summary>按界面“切换方式”下拉（comboBox2）同步 qiehuan_fangshi——三通道收帧的分发开关：
        /// 串口接收/TCP客户机/TCP服务器三处收帧代码均以它为前提，为空则收到帧后既不触发相机也不切型。
        /// 抽成方法的原因：ini 里 [change]all 缺失或值不在下拉项内时，给 comboBox2.Text 赋值不会触发
        /// SelectedIndexChanged（额外整窗 seed 出的 ini 就可能没有该键），必须在初始化时显式同步一次。
        /// 对连接 1 幂等：ini 有值时事件已设过同一结果，再同步一次结果相同。</summary>
        private void SyncQiehuanFangshi()
        {
            if (comboBox2.Text == "Serial")
                qiehuan_fangshi = "Serial";
            else if (comboBox2.Text == "Tcp_client")
                qiehuan_fangshi = "Tcp_client";
            else if (comboBox2.Text == "Tcp_server")
                qiehuan_fangshi = "Tcp_server";
        }
        private bool front = false;

        private void timer4_Tick(object sender, EventArgs e)
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

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
       
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
          
        }
    }
        #endregion
}
