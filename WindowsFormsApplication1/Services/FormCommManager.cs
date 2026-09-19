using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// “连接设备”总管理器（阶段 5 收口）。
    ///
    /// 左侧：协议分组树；右侧：直接嵌入原协议窗口。
    ///
    /// 设计逻辑：
    ///   · FINS 连接 1：主连接，右侧直接嵌入 CommunicationService.Omron 单例（原“查找 → Fins”窗口）。
    ///   · FINS 连接 2~4：独立 FormOmron 实例，按 [finsN] 段族配置。
    ///       “添加设备”=在 ini 创建默认配置（fins_en=true）并启动实例，实例常驻内存；
    ///       “删除设备”=删 ini 段并 ReleaseInstance+Dispose 释放实例。
    ///     只要添加了设备，右侧就有对应画面，窗体内部自己决定是否连接 PLC；切换节点只是隐藏/显示。
    ///   · Modbus-TCP / Modbus-RTU：连接 1 直接嵌入 CommunicationService.Modbustcp / ModbusRtu 单例（阶段 6 接入）；
    ///     连接 2~4 与 FINS 统一（阶段 7）：右侧直接嵌入独立整窗实例（FormModbus(link) / FormModbusRtu(link)），
    ///     每实例按自身 [modbustcpN]/[modbusrtuN] 段族装载并只运行自己的运行时；
    ///     触发/切型等对外调用经主连接单例按 linkId 路由（RegisterChildLink）。
    ///   · 无协议（阶段 8A 整窗实例化）：连接 1 直接嵌入 Form1.frm3（Form3 主窗三通道 UI）；
    ///     连接 2~4 与 Modbus 同构——右侧嵌入与连接 1 完全同构的完整 Form3 整窗实例，每实例读写独立
    ///     test_noprotoN.ini（[noprotoN] 段仅作树节点成员标记），自持串口/TCP客户机/TCP服务器三通道运行时；
    ///     收到帧经 DispatchReceivedFrame 把命中/未命中结果并入主窗共享切型/getData 管线（HostMain 转发）。
    ///     所有串口（Modbus-RTU / 无协议各连接）打开/重开统一经 SerialPortGuard COM 互斥表，配置期与运行期均有防呆。
    ///   · 三菱 FX：预留，后续按相同模式接入。
    /// </summary>
    public partial class FormCommManager : Form
    {
        private const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        // 常量公开：Form1 旧“查找”菜单连接 1 入口收归管理器时需按协议组定位节点
        public const string FinsGroupKey = "FINS";
        public const string ModbusTcpGroupKey = "MODBUSTCP";
        public const string ModbusRtuGroupKey = "MODBUSRTU";
        public const string NoProtoGroupKey = "NOPROTO";
        private const string MelsecGroupKey = "MELSEC";

        private CommunicationService _comm;
        private ClassIni _ini;
        // 启动前相机关卡复查中被阻止的连接（连接 2~4 的"相机归属冲突"项），Attach 末尾统一提示一次。
        private readonly List<string> _startupBlocked = new List<string>();
        private Form _currentHost;
        // 当前嵌入的是否为“仅配置”编辑器（是则切换时只 Hide+Remove，不恢复顶级）。
        private bool _currentIsEditor = false;
        // 已挂滚轮转发的事件源集合：同一嵌入窗体（尤其连接 1 共享单例）反复切换只挂一次，防止滚轮事件多播叠加。
        // ★B1：控件 Dispose 时会从集合移除（见 WheelHookedControlDisposed），故访问统一加锁。
        private readonly HashSet<Control> _wheelHooked = new HashSet<Control>();
        private readonly object _wheelSync = new object();
        // 连接 1 直接嵌入 CommunicationService.Omron 单例。
        private Form _finsConn1Host;
        // Modbus-TCP / Modbus-RTU 各 1 路主连接：直接嵌入 CommunicationService 中的对应单例窗口（阶段 6）。
        private Form _modbusTcpHost;
        private Form _modbusRtuHost;
        // 连接 2~4 实例常驻池：添加后驻留，切换节点只是隐藏/显示，删除/程序关闭时才释放。
        // 阶段 7：Modbus-TCP / Modbus-RTU 与 FINS 统一——连接 2~4 也是原窗体整窗实例池（不再是轻量“连接配置”编辑器）。
        private Dictionary<int, FormOmron> _finsEditors = new Dictionary<int, FormOmron>();
        private Dictionary<int, FormModbus> _modbusTcpEditors = new Dictionary<int, FormModbus>();
        private Dictionary<int, FormModbusRtu> _modbusRtuEditors = new Dictionary<int, FormModbusRtu>();
        // 无协议宿主：Form3 主窗单例（由 Form1 注入：连接 1 嵌入 + 整窗实例共享切型转发）
        private Form3 _noProtoHost;
        // 无协议连接 2~4 完整整窗实例池（阶段 8A：每实例 = 与连接 1 同构的 Form3 大窗，读写独立 test_noprotoN.ini，自持三通道运行时）
        // ★低风险加固：改 ConcurrentDictionary——检测线程经 GetNoProtoLink 无锁读、UI 线程增删（Show/Release/全部释放），
        //   原 Dictionary 并发读写可能损坏内部结构（.NET Framework 下表现为死循环/索引异常）。
        private System.Collections.Concurrent.ConcurrentDictionary<int, Form3> _noProtoEditors
            = new System.Collections.Concurrent.ConcurrentDictionary<int, Form3>();

        public FormCommManager()
        {
            InitializeComponent();

            // 默认打开时尽量把高度撑满，以容纳最高的嵌窗（无协议 Form3 内容高约 890），避免右侧出现内部滚动条。
            // 若屏幕工作区不够（如小屏笔记本），则按屏幕高度自适应，不会顶出任务栏。
            FitHeightToWorkingArea();
        }

        public FormCommManager(CommunicationService comm) : this()
        {
            Attach(comm);
        }

        /// <summary>带无协议主窗体（Form3 单例）的构造：无协议连接 1 可直接嵌入该主窗，连接 2~4 走 ini 段引擎。</summary>
        public FormCommManager(CommunicationService comm, Form3 noProtoHost) : this()
        {
            _noProtoHost = noProtoHost;
            Attach(comm);
        }

        /// <summary>
        /// 按屏幕工作区自动拉高窗体，使右侧 pnlHost 能直接放下最高的嵌窗（无协议 Form3 高约 890），
        /// 从根本上避免“内外双滚”。若屏幕不够高则取最大可用高度，保留滚动条兜底。
        /// </summary>
        private void FitHeightToWorkingArea()
        {
            const int desiredClientHeight = 980; // 890 内容 + 边距余量
            try
            {
                var workingArea = Screen.FromControl(this).WorkingArea;
                int nonClientHeight = this.Height - this.ClientSize.Height; // 标题栏+边框
                int maxClientHeight = workingArea.Height - nonClientHeight;
                int newClientHeight = Math.Min(desiredClientHeight, maxClientHeight);

                if (newClientHeight > this.ClientSize.Height)
                {
                    this.ClientSize = new Size(this.ClientSize.Width, newClientHeight);
                }
            }
            catch
            {
                // 屏显异常时保持设计尺寸，不阻塞启动
            }
        }

        /// <summary>注入通讯服务并初始化设备树。</summary>
        public void Attach(CommunicationService comm)
        {
            _comm = comm;
            _ini = (comm != null && comm.Omron != null) ? comm.Omron.ConfigIni : null;
            _finsConn1Host = (comm != null) ? comm.Omron : null;
            _modbusTcpHost = (comm != null) ? comm.Modbustcp : null;
            _modbusRtuHost = (comm != null) ? comm.ModbusRtu : null;

            // 工具栏只保留“添加/删除”两个按钮
            btnAdd.Width = 80; btnAdd.Location = new Point(6, 8);
            btnDel.Width = 80; btnDel.Location = new Point(92, 8);

            ReloadTree();

            // 自动启动 test.ini 中已添加的连接 2~4（fins_en / modbus_en / modbusrtu_en 统一为 true）
            if (_ini != null)
            {
                foreach (var cfg in FinsIniStore.LoadAll(_ini))
                    if (cfg.LinkId >= 2)
                    {
                        string hit = CommCameraGuard.CheckStartupFins(_ini, cfg.LinkId);
                        if (hit != null) { BlockStartup("FINS 连接 " + cfg.LinkId, hit); continue; }
                        EnsureFinsLinkRunning(cfg.LinkId);
                    }
                foreach (var cfg in ModbusTcpIniStore.LoadAll(_ini))
                    if (cfg.LinkId >= 2)
                    {
                        string hit = CommCameraGuard.CheckStartupModbusTcp(_ini, cfg.LinkId);
                        if (hit != null) { BlockStartup("Modbus-TCP 连接 " + cfg.LinkId, hit); continue; }
                        EnsureModbusTcpLinkRunning(cfg.LinkId);
                    }
                foreach (var cfg in ModbusRtuIniStore.LoadAll(_ini))
                    if (cfg.LinkId >= 2)
                    {
                        string hit = CommCameraGuard.CheckStartupModbusRtu(_ini, cfg.LinkId);
                        if (hit != null) { BlockStartup("Modbus-RTU 连接 " + cfg.LinkId, hit); continue; }
                        EnsureModbusRtuLinkRunning(cfg.LinkId);
                    }
                foreach (var cfg in NoProtoIniStore.LoadAll(_ini))
                    if (cfg.LinkId >= 2)
                        EnsureNoProtoLinkRunning(cfg.LinkId, cfg);   // 传入旧配置做首次迁移：升级前已启用的无协议连接自动落入新整窗独立配置
            }

            // 启动前相机关卡复查结果：有被阻止的连接则统一提示一次（也便于操作员知道为何没启动）
            if (_startupBlocked.Count > 0)
            {
                System.Windows.Forms.MessageBox.Show(
                    "以下连接因「相机归属冲突」被阻止启动（请在占用该相机的连接里清空并保存，再重新打开本窗口）：\r\n\r\n"
                    + string.Join("\r\n\r\n", _startupBlocked),
                    "相机归属冲突 - 连接已阻止",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _startupBlocked.Clear();
            }

            ShowHint("请在左侧选择要查看/编辑的连接设备。\r\n\r\n" +
                     "· 连接 1（FINS / Modbus-TCP / Modbus-RTU / 无协议）：右侧直接嵌入各自主连接原窗口（单例，程序启动即加载配置）；\r\n" +
                     "· FINS / Modbus-TCP / Modbus-RTU 连接 2~4：左侧“＋添加”创建独立整窗实例并显示画面；“－删除”移除配置并释放实例；\r\n" +
                     "  每实例按自身 ini 段族装载，右侧画面与连接 1 完全一致（连接参数 / 数据块 / 相机绑定 / 切型方案 / 读写测试），\r\n" +
                     "  实例在后台持续运行，切换节点仅隐藏/显示，不会反复断连；\r\n" +
                     "· 无协议连接 2~4：＋添加即创建与连接 1 完全一致的 Form3 整窗实例（三通道 / 发送测试 / 切型触发表 / 实时收发状态），\r\n" +
                     "  每实例读写独立 test_noprotoN.ini，独立运行互不干扰；收到帧的命中/未命中结果并入主窗切型/getData 管线；\r\n" +
                     "  勾选通道 en 并保存后，重启软件或下次打开节点时自动连接（与连接 1 自动开通道行为一致）；\r\n" +
                     "· 各协议串口（Modbus-RTU / 无协议连接 1~4）同一 COM 已被其它连接占用时会明确提示占用方，\r\n" +
                     "  配置期添加默认串口也自动避开已占用 COM。");
        }

        // ===================== 左侧设备树 =====================
        private void ReloadTree()
        {
            treeDevices.BeginUpdate();
            try
            {
                treeDevices.Nodes.Clear();

                // ---- FINS 组：连接 1 始终显示；2..MaxLinks 只有已添加（ini 段存在）才显示 ----
                var gFins = new TreeNode("FINS-TCP 通讯（欧姆龙 PLC）") { Tag = FinsGroupKey };
                var existing = new HashSet<int>();
                if (_ini != null)
                    foreach (var c in FinsIniStore.LoadAll(_ini)) existing.Add(c.LinkId);
                for (int link = 1; link <= FinsIniStore.MaxLinks; link++)
                {
                    if (link >= 2 && !existing.Contains(link)) continue;
                    var child = new TreeNode(link + "　" + FinsNodeStatus(link))
                    {
                        Tag = FinsGroupKey + ":" + link
                    };
                    gFins.Nodes.Add(child);
                }
                treeDevices.Nodes.Add(gFins);

                // ---- Modbus-TCP / Modbus-RTU：连接 1 始终显示；连接 2..4 仅当 ini 段存在才显示（阶段 6 起支持增删） ----
                var gTcp = CreateModbusLinkGroup("Modbus-TCP 通讯", ModbusTcpGroupKey, _modbusTcpHost);
                var gRtu = CreateModbusLinkGroup("Modbus-RTU 通讯", ModbusRtuGroupKey, _modbusRtuHost);
                treeDevices.Nodes.Add(gTcp);
                treeDevices.Nodes.Add(gRtu);

                // ---- 无协议（串口/TCP）：连接 1 嵌入 Form3 主窗单例；连接 2..4 仅当 ini 段存在才显示（阶段 6 起支持增删） ----
                var gNoProto = CreateNoProtoLinkGroup("无协议（串口/TCP）通讯", NoProtoGroupKey, _noProtoHost);
                treeDevices.Nodes.Add(gNoProto);

                // ---- 占位组（后续按相同模式接入） ----
                AddPlaceholderGroup("三菱 FX 编程口（预留）", MelsecGroupKey);

                gFins.Expand();
                gTcp.Expand();
                gRtu.Expand();
                gNoProto.Expand();
            }
            finally
            {
                treeDevices.EndUpdate();
            }
        }

        private string FinsNodeStatus(int link)
        {
            if (_ini == null) return "（未配置）";
            var cfg = FinsIniStore.Load(_ini, link);
            return string.IsNullOrEmpty(cfg.Name) ? "FINS-" + link : cfg.Name;
        }

        private void AddPlaceholderGroup(string name, string key)
        {
            var g = new TreeNode(name + "　（待接入）") { Tag = key, ForeColor = Color.Gray };
            g.Nodes.Add(new TreeNode("选中无编辑界面，请走原菜单入口配置") { Tag = key, ForeColor = Color.Gray });
            treeDevices.Nodes.Add(g);
        }

        /// <summary>
        /// 构建 Modbus 协议组（阶段 6 起支持多连接管理）：
        /// 连接 1 始终显示（“连接 1”，选中即嵌入共享单例原窗口）；
        /// 连接 2..MaxLinks 仅当 ini 中对应段存在（已通过“＋添加”创建）才显示。
        /// </summary>
        private TreeNode CreateModbusLinkGroup(string name, string groupKey, Form host)
        {
            var g = new TreeNode(name) { Tag = groupKey };
            var existing = new HashSet<int>();
            int maxLinks = 4;
            if (_ini != null)
            {
                if (groupKey == ModbusTcpGroupKey)
                {
                    foreach (var c in ModbusTcpIniStore.LoadAll(_ini)) existing.Add(c.LinkId);
                    maxLinks = ModbusTcpIniStore.MaxLinks;
                }
                else
                {
                    foreach (var c in ModbusRtuIniStore.LoadAll(_ini)) existing.Add(c.LinkId);
                    maxLinks = ModbusRtuIniStore.MaxLinks;
                }
            }

            for (int link = 1; link <= maxLinks; link++)
            {
                if (link >= 2 && !existing.Contains(link)) continue;
                string text;
                if (link == 1)
                    text = (host == null) ? "连接 1　（未就绪）" : "连接 1";
                else
                    text = "连接 " + link + "　" + ModbusNodeStatus(groupKey, link);
                var child = new TreeNode(text) { Tag = groupKey + ":" + link };
                if (host == null && link == 1) { g.ForeColor = Color.Gray; child.ForeColor = Color.Gray; }
                g.Nodes.Add(child);
            }
            return g;
        }

        /// <summary>取 Modbus 连接 N 的显示名（未配置/缺 name 时给默认名）。</summary>
        private string ModbusNodeStatus(string groupKey, int link)
        {
            if (_ini == null) return "（未配置）";
            if (groupKey == ModbusTcpGroupKey)
            {
                var cfg = ModbusTcpIniStore.Load(_ini, link);
                return string.IsNullOrEmpty(cfg.Name) ? "ModbusTCP-" + link : cfg.Name;
            }
            var rcfg = ModbusRtuIniStore.Load(_ini, link);
            return string.IsNullOrEmpty(rcfg.Name) ? "ModbusRTU-" + link : rcfg.Name;
        }

        /// <summary>构建无协议组：连接 1 常显（宿主未注入则整组置灰）；连接 2..4 仅当 ini [noprotoN] 段存在才显示。</summary>
        private TreeNode CreateNoProtoLinkGroup(string name, string groupKey, Form3 host)
        {
            var g = new TreeNode(name) { Tag = groupKey };
            var existing = new HashSet<int>();
            if (_ini != null)
                for (int _lp = 1; _lp <= NoProtoIniStore.MaxLinks; _lp++) if (NoProtoIniStore.Load(_ini, _lp) != null) existing.Add(_lp);   // ★与"启用"解耦：段存在即显示节点，en=false 的新连接不再从树里消失

            for (int link = 1; link <= NoProtoIniStore.MaxLinks; link++)
            {
                if (link >= 2 && !existing.Contains(link)) continue;
                string text;
                if (link == 1)
                    text = (host == null) ? "连接 1　（未就绪）" : "连接 1";
                else
                    text = "连接 " + link + "　" + NoProtoNodeStatus(link);
                var child = new TreeNode(text) { Tag = groupKey + ":" + link };
                if (host == null && link == 1) { g.ForeColor = Color.Gray; child.ForeColor = Color.Gray; }
                g.Nodes.Add(child);
            }
            return g;
        }

        /// <summary>取无协议连接 N 的显示名（按 kind/参数自动生成；未配置给默认名）。</summary>
        private string NoProtoNodeStatus(int link)
        {
            if (_ini == null) return "（未配置）";
            var cfg = NoProtoIniStore.Load(_ini, link);
            return cfg == null ? "无协议-" + link : cfg.Name;
        }

        // ===================== 选中节点 → 右侧嵌入 =====================
        private void treeDevices_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode n = e.Node;
            if (n == null) return;
            string tag = n.Tag as string;
            if (string.IsNullOrEmpty(tag)) return;

            // FINS:N
            if (tag.StartsWith(FinsGroupKey + ":"))
            {
                int link;
                if (int.TryParse(tag.Substring(FinsGroupKey.Length + 1), out link))
                {
                    ShowFinsLink(link);
                }
                return;
            }

            // FINS 整组：与其它协议保持一致，选中标题节点即嵌入主连接窗口（CommunicationService.Omron）
            if (tag == FinsGroupKey)
            {
                ShowSharedHost(_finsConn1Host, "FINS-TCP 主连接窗口未就绪（通讯服务未创建）。");
                return;
            }

            // Modbus-TCP：整组或“连接 1”都直接嵌入共享单例原窗口；连接 2~4 打开完整原窗实例（阶段 7）
            if (tag == ModbusTcpGroupKey || tag.StartsWith(ModbusTcpGroupKey + ":"))
            {
                int link;
                if (tag != ModbusTcpGroupKey && int.TryParse(tag.Substring(ModbusTcpGroupKey.Length + 1), out link))
                {
                    if (link == 1)
                        ShowSharedHost(_modbusTcpHost, "Modbus-TCP 主连接窗口未就绪（通讯服务未创建）。");
                    else
                        ShowModbusTcpLink(link);
                }
                else
                    ShowSharedHost(_modbusTcpHost, "Modbus-TCP 主连接窗口未就绪（通讯服务未创建）。");
                return;
            }

            // Modbus-RTU：整组或“连接 1”都直接嵌入共享单例原窗口；连接 2~4 打开完整原窗实例（阶段 7）
            if (tag == ModbusRtuGroupKey || tag.StartsWith(ModbusRtuGroupKey + ":"))
            {
                int link;
                if (tag != ModbusRtuGroupKey && int.TryParse(tag.Substring(ModbusRtuGroupKey.Length + 1), out link))
                {
                    if (link == 1)
                        ShowSharedHost(_modbusRtuHost, "Modbus-RTU 主连接窗口未就绪（通讯服务未创建）。");
                    else
                        ShowModbusRtuLink(link);
                }
                else
                    ShowSharedHost(_modbusRtuHost, "Modbus-RTU 主连接窗口未就绪（通讯服务未创建）。");
                return;
            }

            // 无协议：整组或“连接 1”嵌入 Form3 主窗（三通道 UI）；连接 2~4 打开独立整窗实例（自持 NoProtoLink，阶段 7）
            if (tag == NoProtoGroupKey || tag.StartsWith(NoProtoGroupKey + ":"))
            {
                int link;
                if (tag != NoProtoGroupKey && int.TryParse(tag.Substring(NoProtoGroupKey.Length + 1), out link))
                {
                    if (link == 1)
                        ShowSharedHost(_noProtoHost, "无协议主连接窗口未就绪（未注入 Form3 主窗）。");
                    else
                        ShowNoProtoLink(link);
                }
                else
                    ShowSharedHost(_noProtoHost, "无协议主连接窗口未就绪（未注入 Form3 主窗）。");
                return;
            }

            // 占位组 / 占位子项（三菱 FX）
            ShowHint("该协议类型的独立窗口仍在原入口可用（主界面“查找”菜单）；\r\n" +
                     "后续版本将像 FINS / Modbus / 无协议一样在本管理器中嵌入对应窗口。");
        }

        private void ShowFinsLink(int link)
        {
            if (link == 1)
            {
                if (_finsConn1Host != null)
                    ShowHostForm(_finsConn1Host, false);
                else
                    ShowHint("主连接窗口未就绪。");
                return;
            }

            // 连接 2~4：每个连接是独立的 FormOmron 实例（复用老窗体），按 [finsN] 段族配置。
            // 实例启用后常驻内存，切换节点只是隐藏/显示；禁用/删除/程序关闭时才 ReleaseInstance+Dispose。
            if (_comm == null || _comm.Omron == null) { ShowHint("主通讯未就绪。"); return; }

            FormOmron host;
            if (!_finsEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                host = CreateFinsEditor(link);
                _finsEditors[link] = host;
            }
            ShowHostForm(host, true);
        }

        /// <summary>把共享单例原窗口嵌入右侧容器（与 FINS 连接 1 相同模式）。host 为空时给出提示。</summary>
        private void ShowSharedHost(Form host, string notReadyHint)
        {
            if (host == null) { ShowHint(notReadyHint); return; }
            ShowHostForm(host, false);
        }

        // ===================== Modbus-TCP / Modbus-RTU 连接 2~4 整窗实例（阶段 7，仿 FINS） =====================
        private void ShowModbusTcpLink(int link)
        {
            if (_ini == null) { ShowHint("配置文件未就绪。"); return; }
            if (_comm == null || _comm.Modbustcp == null) { ShowHint("Modbus-TCP 主连接未就绪。"); return; }
            FormModbus host;
            if (!_modbusTcpEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                host = CreateModbusTcpLink(link);
                _modbusTcpEditors[link] = host;
            }
            ShowHostForm(host, true);
        }

        private void ShowModbusRtuLink(int link)
        {
            if (_ini == null) { ShowHint("配置文件未就绪。"); return; }
            if (_comm == null || _comm.ModbusRtu == null) { ShowHint("Modbus-RTU 主连接未就绪。"); return; }
            FormModbusRtu host;
            if (!_modbusRtuEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                host = CreateModbusRtuLink(link);
                _modbusRtuEditors[link] = host;
            }
            ShowHostForm(host, true);
        }

        /// <summary>创建某 Modbus-TCP 连接 N 的整窗实例：按 [modbustcpN] 段族装载、只运行自己 runtime；child 触发桥接到主连接单例。</summary>
        private FormModbus CreateModbusTcpLink(int link)
        {
            var host = new FormModbus(link);
            FormModbus.GetSeletionData handler = (s, e) =>
            {
                if (_comm == null || _comm.Modbustcp == null) return;
                try { _comm.Modbustcp.RaiseSelectionChanged(e.Selection, e.Camera, e.all, link); }
                catch { }
            };
            host.Tag = handler;
            host.getData += handler;
            // 把子实例登记到主连接单例，使主界面能通过 _comm.Modbustcp 路由到该连接的相机绑定/反馈/切方案锁。
            try { _comm.Modbustcp.RegisterChildLink(link, host); } catch { }
            return host;
        }

        private FormModbusRtu CreateModbusRtuLink(int link)
        {
            var host = new FormModbusRtu(link);
            FormModbusRtu.GetSeletionData handler = (s, e) =>
            {
                if (_comm == null || _comm.ModbusRtu == null) return;
                try { _comm.ModbusRtu.RaiseSelectionChanged(e.Selection, e.Camera, e.all, link); }
                catch { }
            };
            host.Tag = handler;
            host.getData += handler;
            try { _comm.ModbusRtu.RegisterChildLink(link, host); } catch { }
            return host;
        }

        /// <summary>记录一条"启动时被阻止的连接"（相机归属冲突），Attach 末尾统一提示。</summary>
        private void BlockStartup(string connLabel, string msg)
        {
            _startupBlocked.Add(connLabel + "：\r\n" + msg);
        }

        /// <summary>保证指定 Modbus-TCP 连接处于运行状态（程序启动/添加时用于恢复已启用连接）。</summary>
        private void EnsureModbusTcpLinkRunning(int link)
        {
            if (_comm == null || _comm.Modbustcp == null) return;
            FormModbus host;
            if (!_modbusTcpEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                host = CreateModbusTcpLink(link);
                _modbusTcpEditors[link] = host;
            }
            // EnsureHandleCreated = 建句柄 + 显式 InitializeForm（Load 只在首次 Show 触发，隐藏窗不会自动 Load）
            try { if (!host.IsHandleCreated) host.EnsureHandleCreated(); } catch { }
        }

        private void EnsureModbusRtuLinkRunning(int link)
        {
            if (_comm == null || _comm.ModbusRtu == null) return;
            FormModbusRtu host;
            if (!_modbusRtuEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                host = CreateModbusRtuLink(link);
                _modbusRtuEditors[link] = host;
            }
            try { if (!host.IsHandleCreated) host.EnsureHandleCreated(); } catch { }
        }

        /// <summary>释放指定 Modbus-TCP 连接 N 的整窗实例（删除连接时调用）。</summary>
        private void ReleaseModbusTcpLink(int link)
        {
            FormModbus host;
            if (!_modbusTcpEditors.TryGetValue(link, out host) || host == null || host.IsDisposed) return;
            if (_currentHost == host) RestoreFromHost();
            try
            {
                if (host.Tag is FormModbus.GetSeletionData h)
                    host.getData -= h;
            }
            catch { }
            try { if (_comm != null && _comm.Modbustcp != null) _comm.Modbustcp.UnregisterChildLink(link); } catch { }
            try { host.ReleaseInstance(); } catch { }
            try { host.Dispose(); } catch { }
            _modbusTcpEditors.Remove(link);
        }

        private void ReleaseModbusRtuLink(int link)
        {
            FormModbusRtu host;
            if (!_modbusRtuEditors.TryGetValue(link, out host) || host == null || host.IsDisposed) return;
            if (_currentHost == host) RestoreFromHost();
            try
            {
                if (host.Tag is FormModbusRtu.GetSeletionData h)
                    host.getData -= h;
            }
            catch { }
            try { if (_comm != null && _comm.ModbusRtu != null) _comm.ModbusRtu.UnregisterChildLink(link); } catch { }
            try { host.ReleaseInstance(); } catch { }
            try { host.Dispose(); } catch { }
            _modbusRtuEditors.Remove(link);
        }

        // ===================== 无协议连接 2~4 完整整窗实例（阶段 8A：与连接 1 同构的 Form3 大窗，读写独立 test_noprotoN.ini） =====================
        private void ShowNoProtoLink(int link)
        {
            if (_ini == null) { ShowHint("配置文件未就绪。"); return; }
            Form3 host;
            if (!_noProtoEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                Form3.SeedLinkIniFile(link, NoProtoIniStore.Load(_ini, link));
                host = CreateNoProtoLink(link);
                _noProtoEditors[link] = host;
            }
            // 首次显示时才创建句柄并初始化运行时（读取 test_noprotoN.ini 并启停三通道）
            host.EnsureWindowHandle();
            ShowHostForm(host, true);
        }

        /// <summary>创建无协议连接 N 的完整整窗实例：读 test_noprotoN.ini（段与连接 1 同构），
        /// HostMain 指向 Form3 主窗——本窗收帧后的命中/未命中结果并入主窗共享管线（相机触发与切型统一入口）。</summary>
        private Form3 CreateNoProtoLink(int link)
        {
            Form1 f1 = (_noProtoHost != null) ? _noProtoHost.f1 : null;
            return new Form3(f1, link) { HostMain = _noProtoHost };
        }

        /// <summary>保证指定无协议连接实例存在（不立即创建句柄/初始化 UI）。
        /// UI 句柄与运行时推迟到第一次 ShowNoProtoLink 时创建，避免管理器启动时一次性初始化所有已启用连接导致卡顿。</summary>
        private void EnsureNoProtoLinkRunning(int link, NoProtoLinkConfig legacy = null)
        {
            if (_ini == null) return;
            Form3 host;
            if (!_noProtoEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                // 独立文件缺省时迁移 legacy（旧 [noprotoN] 引擎配置）→ 升级前已启用的无协议连接自动落到新整窗并保持启用
                Form3.SeedLinkIniFile(link, legacy);
                host = CreateNoProtoLink(link);
                _noProtoEditors[link] = host;
            }
            // 注意：不在这里 EnsureWindowHandle，首次显示时由 ShowNoProtoLink 触发。
        }

        /// <summary>释放指定无协议连接的整窗实例（ReleaseInstance 停三通道运行时、Dispose、从字典移除；
        /// 调用方负责清除 ini 段与独立配置文件）。</summary>
        private void ReleaseNoProtoLink(int link)
        {
            Form3 host;
            if (!_noProtoEditors.TryGetValue(link, out host) || host == null || host.IsDisposed) return;
            if (_currentHost == host) RestoreFromHost();
            try { host.ReleaseInstance(); } catch { }
            try { host.Dispose(); } catch { }
            Form3 removed;   // ★ConcurrentDictionary 无单参 Remove，用 TryRemove（返回值不使用）
            _noProtoEditors.TryRemove(link, out removed);
        }

        /// <summary>按链路号取无协议连接整窗实例：link==1 返回主窗宿主，link&gt;=2 返回独立整窗。
        /// 供 Form1 结果回写路由使用（让结果回到发出触发的那条无协议连接的端口）。不存在或已释放返回 null。</summary>
        public Form3 GetNoProtoLink(int link)
        {
            if (link <= 1) return _noProtoHost;
            Form3 host;
            if (_noProtoEditors != null && _noProtoEditors.TryGetValue(link, out host) && host != null && !host.IsDisposed)
                return host;
            return null;
        }

        /// <summary>在左侧设备树中查找 groupKey 组下连接 link 的节点。</summary>
        private TreeNode FindGroupNode(string groupKey, int link)
        {
            string target = groupKey + ":" + link;
            foreach (TreeNode g in treeDevices.Nodes)
            {
                if (g.Tag as string != groupKey) continue;
                foreach (TreeNode c in g.Nodes)
                    if (c.Tag as string == target) return c;
            }
            return null;
        }

        /// <summary>选中指定 groupKey 组的连接节点并展开到可视（触发 AfterSelect → 打开对应界面）。</summary>
        private void SelectGroupNode(string groupKey, int link)
        {
            var node = FindGroupNode(groupKey, link);
            if (node == null) return;
            treeDevices.SelectedNode = node;
            node.EnsureVisible();
        }

        /// <summary>公共定位入口：Form1 旧“查找”菜单的连接 1 入口统一收归管理器后调用——
        /// 打开本管理器并选中指定协议组的连接 1（右侧嵌入对应共享单例原窗口）。节点不存在时忽略。</summary>
        public void SelectGroupLink(string groupKey, int link)
        {
            if (IsDisposed) return;
            SelectGroupNode(groupKey, link);
        }

        /// <summary>取当前选中节点的协议组 key（“MODBUSTCP:2”→“MODBUSTCP”；组头→自身 key；无 →空串）。</summary>
        private string GetSelectedGroupKey()
        {
            var sel = treeDevices.SelectedNode;
            string tag = sel != null ? (sel.Tag as string) : null;
            if (string.IsNullOrEmpty(tag)) return "";
            int ci = tag.IndexOf(':');
            return ci > 0 ? tag.Substring(0, ci) : tag;
        }

        /// <summary>取当前选中的 groupKey 组连接号，未选中或不是该组返回 -1。</summary>
        private int GetSelectedLink(string groupKey)
        {
            var sel = treeDevices.SelectedNode;
            string tag = sel != null ? (sel.Tag as string) : null;
            if (string.IsNullOrEmpty(tag) || !tag.StartsWith(groupKey + ":")) return -1;
            int link;
            if (int.TryParse(tag.Substring(groupKey.Length + 1), out link)) return link;
            return -1;
        }

        private FormOmron CreateFinsEditor(int link)
        {
            var host = new FormOmron(link);
            // 桥接：本实例的触发/切型事件经主连接 comm.Omron 的 getData 通知主界面（带 LinkId + SchemePath）。
            FormOmron.GetSeletionData handler = (s, e) =>
            {
                try { _comm.Omron.RaiseSelectionChanged(e.Selection, e.Camera, e.SchemePath, link); }
                catch { }
            };
            host.Tag = handler;
            host.getData += handler;

            // 把子实例登记到主连接单例，使主界面能通过 _comm.Omron 路由到该连接的相机绑定/反馈/切方案锁。
            try { _comm.Omron.RegisterChildLink(link, host); } catch { }
            return host;
        }

        /// <summary>保证指定 FINS 连接处于运行状态（程序启动时用于恢复已启用连接）。</summary>
        private void EnsureFinsLinkRunning(int link)
        {
            if (_comm == null || _comm.Omron == null) return;
            FormOmron host;
            if (!_finsEditors.TryGetValue(link, out host) || host == null || host.IsDisposed)
            {
                host = CreateFinsEditor(link);
                _finsEditors[link] = host;
            }
            // EnsureHandleCreated = 建句柄 + 显式 InitializeForm（Load 只在首次 Show 触发，隐藏窗不会自动 Load）
            host.EnsureHandleCreated();
        }

        /// <summary>把原协议窗体（或配置编辑器）作为非顶级子窗体嵌入右侧容器。
        /// 不使用 Dock=Fill，而是保留窗体原始设计尺寸并启用宿主 Panel 的 AutoScroll，
        /// 解决 Form3（设计高度 890）被截断且没有滚动条的问题；同时 SuspendLayout 减少切换闪烁。</summary>
        /// <param name="isEditor">true=仅配置编辑器（生命周期由管理器持有，摘除仅 Hide）；false=共享单例/连接1 主窗
        /// （摘除仅 Hide、不恢复顶级，生命周期由 Form1 侧单例持有，进程结束才释放）。</param>
        private void ShowHostForm(Form f, bool isEditor = false)
        {
            if (f == null) { ShowHint("窗体为空"); return; }
            if (_currentHost == f) { lblHint.Visible = false; return; }

            // 先把之前嵌入的实例摘下隐藏（避免同一 Form 同时挂在面板与其它位置）
            RestoreFromHost();

            // 统一降级为子窗体再挂入面板。连接 1 已在 Form1 启动加载后定型 TopLevel=false，
            // 此处对它们与连接 N 完全同路径、不会发生句柄重建；个别仍以顶级隐藏态存在的窗体（历史遗留）
            // 则在此一次性降级并保持隐藏，不会弹成独立窗口。
            // FormOmron.StartPosition=CenterParent：若不改 Manual，嵌入时会被“顶”成独立窗口，故强制 Manual。
            try { f.Hide(); } catch { }
            f.TopLevel = false;
            f.FormBorderStyle = FormBorderStyle.None;
            f.StartPosition = FormStartPosition.Manual;
            f.TopMost = false;               // 禁止子窗体置顶/抢夺焦点
            f.Dock = DockStyle.None;
            f.Parent = pnlHost;

            // 记录窗体自然设计尺寸（首次嵌入时），后续用 AutoScrollMinSize 保证内容完整
            if (f.AutoScrollMinSize.IsEmpty)
                f.AutoScrollMinSize = f.ClientSize;
            Size natural = f.AutoScrollMinSize;

            // 挂 Parent 后、真正 Show 之前禁用目标窗体重绘，避免嵌入瞬间大面积布局/重绘闪烁。
            // （历史：连接 1 首次嵌入时 TopLevel 翻转会重建整棵子控件句柄并触发重绘，特别慢；
            //  现已由 Form1 启动定型 TopLevel=false，正常路径无句柄重建，此抑制仅兜底用。）
            if (f.IsHandleCreated)
                SendMessage(f.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            pnlHost.SuspendLayout();
            try
            {
                pnlHost.AutoScroll = true;
                f.Location = new Point(0, 0);
                // 宽度取面板当前宽度与自然宽度之大者，避免出现横向滚动条；高度保持自然高度，由 Panel 提供垂直滚动条
                f.Size = new Size(Math.Max(natural.Width, pnlHost.ClientSize.Width), natural.Height);
                f.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                pnlHost.Controls.Add(f);
                f.Show();
                f.BringToFront();
            }
            finally
            {
                pnlHost.ResumeLayout(true);
                if (f.IsHandleCreated)
                {
                    SendMessage(f.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                    f.Invalidate(true);
                }
            }
            // 切换嵌入窗体后复位滚动位置：各连接窗体高度不同，残留偏移会让新画面停在“半截/乱跳”位置。
            pnlHost.AutoScrollPosition = new Point(0, 0);
            // 统一收口嵌入窗体的滚轮：自身无滚动需求的控件把滚轮交还 pnlHost，保证滚轮可一滚到底；
            // 表格/多行文本框等自持滚动控件除外（保留它们滚内容的能力）。同一控件只挂一次。
            WireWheelRedirect(f);
            _currentHost = f;
            _currentIsEditor = isEditor;
            lblHint.Visible = false;
        }

        private void ShowHint(string msg)
        {
            RestoreFromHost();
            lblHint.Text = msg;
            lblHint.Visible = true;
        }

        /// <summary>把当前嵌入到右侧的窗体隐藏并移出面板，切换节点时不弹成独立窗口。</summary>
        private void RestoreFromHost()
        {
            if (_currentHost == null) return;
            try
            {
                // 切换节点时，旧窗体只需 Hide+Remove，不要恢复成顶级弹出来。
                // 程序关闭/管理器释放时由 OnFormClosing 统一处理。
                pnlHost.Controls.Remove(_currentHost);
                try { _currentHost.Hide(); } catch { }
            }
            catch { }
            _currentHost = null;
            _currentIsEditor = false;
        }

        // ===================== 嵌入窗体滚轮统一收口 =====================
        // 症状：嵌窗高度大于面板时，滚轮“经常滚不全、手动拖滚动条却能到底”。
        // 原因：滚轮消息能否到达 pnlHost 取决于焦点/悬停控件链；多行文本框、表格等自滚控件会吞掉滚轮；
        //       同时切换窗体后滚动位置残留，造成“滚到半截/乱跳”的观感。
        // 收口策略：嵌入时给窗体及其全部后代控件挂同一 MouseWheel 转发器——
        //   ① 自身有滚动需求的控件（DataGridView / 多行文本框 / RichTextBox / 自滚容器等）放行，保持滚内容能力；
        //   ② 其余普通控件（按钮/标签/单行框/GroupBox…）的滚轮一律转交 pnlHost 原生滚动
        //      （WM_VSCROLL SB_LINE*，与系统滚轮同语义，自动在“底”处停止），保证可一滚到底。
        private void WireWheelRedirect(Control root)
        {
            var list = new List<Control>();
            CollectScrollChildren(root, list);
            foreach (var c in list)
            {
                bool added;
                lock (_wheelSync) { added = _wheelHooked.Add(c); }
                if (!added) continue;
                c.MouseWheel += HostMouseWheel;
                // ★B1 修复：控件被释放时自动从登记表移除。
                // 旧实现只 Add 从不 Remove，已 Dispose 的子窗体控件被 HashSet 长期强引用，阻止 GC（内存泄漏）。
                c.Disposed += WheelHookedControlDisposed;
            }
        }

        /// <summary>★B1：被登记控件释放时自动反注册，避免强引用泄漏。</summary>
        private void WheelHookedControlDisposed(object sender, EventArgs e)
        {
            var c = sender as Control;
            if (c == null) return;
            try { c.MouseWheel -= HostMouseWheel; } catch { }
            lock (_wheelSync) { _wheelHooked.Remove(c); }
        }

        private static void CollectScrollChildren(Control c, List<Control> list)
        {
            list.Add(c);
            foreach (Control ch in c.Controls)
                CollectScrollChildren(ch, list);
        }

        private void HostMouseWheel(object sender, MouseEventArgs e)
        {
            // 自滚控件（表格/多行框等）悬停时优先滚自身内容，不外传
            if (IsSelfScrolling(sender as Control)) return;

            var he = e as HandledMouseEventArgs;
            if (he != null) he.Handled = true;
            if (pnlHost.VerticalScroll.Visible)
            {
                // SB_LINEUP=0 / SB_LINEDOWN=1，语义与系统滚轮一致，到达内容底自动停
                SendMessage(pnlHost.Handle, 0x0115 /*WM_VSCROLL*/,
                    new IntPtr(e.Delta > 0 ? 0 : 1), IntPtr.Zero);
            }
        }

        /// <summary>控件自身是否有独立滚动需求（有则悬停其上时滚轮先滚它，避免误抢）。</summary>
        private static bool IsSelfScrolling(Control c)
        {
            if (c == null) return false;
            if (c is DataGridView) return true;
            if (c is RichTextBox) return true;
            if (c is ListBox) return true;
            if (c is TextBox tb) return tb.Multiline;
            if (c is ScrollableControl sc) return sc.AutoScroll && sc.VerticalScroll.Visible;
            return false;
        }

        // ===================== ＋添加 / －删除（FINS / Modbus-TCP / Modbus-RTU 连接 2~4，阶段 6） =====================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (_ini == null || _comm == null) { ShowHint("配置文件未就绪。"); return; }

            switch (GetSelectedGroupKey())
            {
                case FinsGroupKey: AddFinsLink(); break;
                case ModbusTcpGroupKey: AddModbusTcpLink(); break;
                case ModbusRtuGroupKey: AddModbusRtuLink(); break;
                case NoProtoGroupKey: AddNoProtoLink(); break;
                default:
                    MessageBox.Show(this,
                        "请先在左侧选中要新增连接的协议组（FINS / Modbus-TCP / Modbus-RTU / 无协议）下的任一节点，再点“＋添加”。\r\n" +
                        "三菱 FX 编程口为预留项，暂不支持在此新增连接。",
                        "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        /// <summary>在当前 FINS 组添加一台默认设备（fins_en=true，添加即视为启用）。</summary>
        private void AddFinsLink()
        {
            int newLink = NextFreeFinsLink();
            if (newLink < 0)
            {
                MessageBox.Show(this,
                    "已达每协议最大连接数（" + FinsIniStore.MaxLinks + "），无法再添加 FINS 连接。",
                    "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 写入默认段（fins_en=true，保存后 LoadAll 才会纳入该连接）
            var cfg = new FinsLinkConfig
            {
                LinkId = newLink,
                Name = "FINS-" + newLink,
                Ip = "127.0.0.1",
                Port = "9600",
                Cell = "0",
                Local = "192",
                Abcd = "CDAB",
                FinsEn = false,   // ★默认关闭：新建 FINS 连接不自动启用，用户手动勾选使能后才建连/轮询
                Zongchang = 1,
                LunxunTime = 20
            };
            FinsIniStore.Save(_ini, cfg);
            ReloadTree();

            // 触发 AfterSelect → 打开编辑器（节点不存在则直接显示主连接窗口）
            SelectGroupNode(FinsGroupKey, newLink);
            if (FindGroupNode(FinsGroupKey, newLink) == null) ShowFinsLink(newLink);
        }

        /// <summary>在 Modbus-TCP 组添加一台默认设备（写 ini + 即时启动后台运行时）。</summary>
        private void AddModbusTcpLink()
        {
            int newLink = NextFreeModbusTcpLink();
            if (newLink < 0)
            {
                MessageBox.Show(this,
                    "已达每协议最大连接数（" + ModbusTcpIniStore.MaxLinks + "），无法再添加 Modbus-TCP 连接。",
                    "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cfg = new ModbusTcpLinkConfig
            {
                LinkId = newLink,
                Name = "ModbusTCP-" + newLink,
                Ip = "127.0.0.1",
                Port = "9600",
                Cell = "0",
                Abcd = "CDAB",
                ModbusEn = false,   // ★默认关闭：新建 Modbus 连接不自动启用，用户手动勾选使能后才建连/轮询
                Zongchang = 1,
                LunxunTime = 20
            };
            ModbusTcpIniStore.Save(_ini, cfg);
            ReloadTree();

            // 触发 AfterSelect → 打开“连接配置”编辑器
            if (FindGroupNode(ModbusTcpGroupKey, newLink) != null)
                SelectGroupNode(ModbusTcpGroupKey, newLink);
            else
                ShowHint("已添加 Modbus-TCP 连接 " + newLink + "。");
        }

        /// <summary>在 Modbus-RTU 组添加一台默认设备（写 ini + 即时启动后台运行时）。</summary>
        private void AddModbusRtuLink()
        {
            int newLink = NextFreeModbusRtuLink();
            if (newLink < 0)
            {
                MessageBox.Show(this,
                    "已达每协议最大连接数（" + ModbusRtuIniStore.MaxLinks + "），无法再添加 Modbus-RTU 连接。",
                    "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cfg = new ModbusRtuLinkConfig
            {
                LinkId = newLink,
                Name = "ModbusRTU-" + newLink,
                PortName = PickFreeCom(_ini),      // COM 互斥配置期防呆：自动避开已被启用连接占用的串口
                BaudRate = "9600",
                DataBits = "8",
                StopBits = "1",
                Parity = "None",
                Station = "1",
                Abcd = "CDAB",
                ModbusEn = false,   // ★默认关闭：新建 Modbus 连接不自动启用，用户手动勾选使能后才建连/轮询
                Zongchang = 1,
                LunxunTime = 20
            };
            ModbusRtuIniStore.Save(_ini, cfg);
            ReloadTree();

            // 触发 AfterSelect → 打开“连接配置”编辑器
            if (FindGroupNode(ModbusRtuGroupKey, newLink) != null)
                SelectGroupNode(ModbusRtuGroupKey, newLink);
            else
                ShowHint("已添加 Modbus-RTU 连接 " + newLink + "。");
        }

        /// <summary>＋添加：写入默认 [noprotoN] 段并即时启动对应后台链路，随后刷新并选中新节点。</summary>
        private void AddNoProtoLink()
        {
            int newLink = NextFreeNoProtoLink();
            if (newLink < 0)
            {
                MessageBox.Show(this,
                    "已达每协议最大连接数（" + NoProtoIniStore.MaxLinks + "），无法再添加无协议连接。",
                    "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 默认参数跟随当前选中的无协议连接类型：
            //   · 选中连接 1 时，按 Form3 主窗当前可见模式（Serial/Tcp_client/Tcp_server）创建；
            //   · 选中连接 2~4 时，复制该连接的类型（便于快速建同类设备）；
            //   · 未选中具体连接（选中分组节点）时，也尽量跟随 Form3 主窗的当前模式。
            // 未指定或无法判断时，回退到串口模式 + 自动选空闲 COM。
            NoProtoKind kind = NoProtoKind.Serial;
            string tcpIp = "127.0.0.1";
            int tcpPort = 5020;

            int selected = GetSelectedLink(NoProtoGroupKey);
            if (selected == 1)
            {
                var f3 = _noProtoHost as Form3;
                string mode = f3 != null ? f3.qiehuan_fangshi : null;
                if (mode == "Tcp_client") kind = NoProtoKind.TcpClient;
                else if (mode == "Tcp_server") kind = NoProtoKind.TcpServer;
            }
            else if (selected > 1)
            {
                var src = NoProtoIniStore.Load(_ini, selected);
                if (src != null)
                {
                    kind = src.Kind;
                    tcpIp = src.Ip;
                    tcpPort = src.Port;
                }
            }
            else
            {
                var f3 = _noProtoHost as Form3;
                string mode = f3 != null ? f3.qiehuan_fangshi : null;
                if (mode == "Tcp_client") kind = NoProtoKind.TcpClient;
                else if (mode == "Tcp_server") kind = NoProtoKind.TcpServer;
            }

            var cfg = new NoProtoLinkConfig { LinkId = newLink, Kind = kind, Enabled = false };
            if (kind == NoProtoKind.Serial)
            {
                cfg.PortName = PickFreeCom(_ini);   // COM 互斥配置期防呆：自动避开已被启用连接占用的串口
                cfg.BaudRate = 9600;
                cfg.DataBits = 8;
                cfg.StopBits = 1;
                cfg.Jiaoyan = 0;
            }
            else
            {
                cfg.Ip = kind == NoProtoKind.TcpClient ? tcpIp : "";
                cfg.Port = tcpPort;
            }
            NoProtoIniStore.Save(_ini, cfg);

            // 生成本连接独立配置文件（test_noprotoN.ini：三通道默认 en=false；并按 Add 选择的类型把对应通道迁移为 en=true），
            // 再创建完整整窗实例启动本连接运行时
            Form3.SeedLinkIniFile(newLink, cfg);
            EnsureNoProtoLinkRunning(newLink);
            ReloadTree();

            if (FindGroupNode(NoProtoGroupKey, newLink) != null)
                SelectGroupNode(NoProtoGroupKey, newLink);
            else
                ShowHint("已添加无协议连接 " + newLink + "。");
        }

        /// <summary>在 2..MaxLinks 中找空闲连接号（段存在即占用，包括 en=false 的保留段）。</summary>
        private int NextFreeNoProtoLink()
        {
            for (int l = 2; l <= NoProtoIniStore.MaxLinks; l++)
                if (NoProtoIniStore.Load(_ini, l) == null) return l;
            return -1;
        }

        private int NextFreeFinsLink()
        {
            var existing = new HashSet<int>();
            foreach (var c in FinsIniStore.LoadAll(_ini)) existing.Add(c.LinkId);
            for (int l = 2; l <= FinsIniStore.MaxLinks; l++)
                if (!existing.Contains(l)) return l;
            return -1;
        }

        private int NextFreeModbusTcpLink()
        {
            var existing = new HashSet<int>();
            foreach (var c in ModbusTcpIniStore.LoadAll(_ini)) existing.Add(c.LinkId);
            for (int l = 2; l <= ModbusTcpIniStore.MaxLinks; l++)
                if (!existing.Contains(l)) return l;
            return -1;
        }

        private int NextFreeModbusRtuLink()
        {
            var existing = new HashSet<int>();
            foreach (var c in ModbusRtuIniStore.LoadAll(_ini)) existing.Add(c.LinkId);
            for (int l = 2; l <= ModbusRtuIniStore.MaxLinks; l++)
                if (!existing.Contains(l)) return l;
            return -1;
        }

        /// <summary>COM 互斥配置期防呆：汇总 ini 中已启用串口连接的占用后，推荐一个空闲默认串口（含 Modbus-RTU / 无协议各连接）。</summary>
        private static string PickFreeCom(demo.ClassIni ini)
        {
            var used = SerialPortGuard.ScanIniOwners(ini);
            return SerialPortGuard.PickFreeComPort(used.Keys);
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            switch (GetSelectedGroupKey())
            {
                case FinsGroupKey:
                    {
                        int link = GetSelectedLink(FinsGroupKey);
                        if (link < 0 || link == 1)
                        {
                            MessageBox.Show(this,
                                "请在左侧 FINS 节点下选择要删除的连接（连接 1 为主连接，不可删除）。",
                                "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        if (MessageBox.Show(this,
                                "确定删除 FINS 连接 " + link + " 的全部配置并卸载其运行时？",
                                "连接设备", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                            return;
                        ReleaseFinsEditor(link);
                        FinsIniStore.Delete(_ini, link);
                        ReloadTree();
                        ShowHint("已删除 FINS 连接 " + link + "（ini 段已清空，运行时已卸载）。");
                        break;
                    }
                case ModbusTcpGroupKey:
                    DeleteModbusTcpLink();
                    break;
                case ModbusRtuGroupKey:
                    DeleteModbusRtuLink();
                    break;
                case NoProtoGroupKey:
                    DeleteNoProtoLink();
                    break;
                default:
                    MessageBox.Show(this,
                        "请先选中要删除的连接节点（连接 1 为主连接，不可删除）。",
                        "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        private void DeleteModbusTcpLink()
        {
            int link = GetSelectedLink(ModbusTcpGroupKey);
            if (link < 0 || link == 1)
            {
                MessageBox.Show(this,
                    "请在左侧 Modbus-TCP 节点下选择要删除的连接（连接 1 为主连接，不可删除）。",
                    "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this,
                    "确定删除 Modbus-TCP 连接 " + link + " 的全部配置并停止其后台运行时？",
                    "连接设备", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ReleaseModbusTcpLink(link);
            ModbusTcpIniStore.Delete(_ini, link);
            ReloadTree();
            ShowHint("已删除 Modbus-TCP 连接 " + link + "（ini 段已清空，整窗实例已释放）。");
        }

        private void DeleteModbusRtuLink()
        {
            int link = GetSelectedLink(ModbusRtuGroupKey);
            if (link < 0 || link == 1)
            {
                MessageBox.Show(this,
                    "请在左侧 Modbus-RTU 节点下选择要删除的连接（连接 1 为主连接，不可删除）。",
                    "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this,
                    "确定删除 Modbus-RTU 连接 " + link + " 的全部配置并停止其后台运行时？",
                    "连接设备", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ReleaseModbusRtuLink(link);
            ModbusRtuIniStore.Delete(_ini, link);
            ReloadTree();
            ShowHint("已删除 Modbus-RTU 连接 " + link + "（ini 段已清空，整窗实例已释放）。");
        }

        /// <summary>－删除：确认后释放整窗实例（其自持运行时随之停止）、清空 ini 段并刷新树。</summary>
        private void DeleteNoProtoLink()
        {
            int link = GetSelectedLink(NoProtoGroupKey);
            if (link < 0 || link == 1)
            {
                MessageBox.Show(this,
                    "请在左侧无协议组下选择要删除的连接（连接 1 为主连接，不可删除）。",
                    "连接设备", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this,
                    "确定删除无协议连接 " + link + " 的全部配置并停止其独立运行时？",
                    "连接设备", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ReleaseNoProtoLink(link);
            NoProtoIniStore.Delete(_ini, link);
            Form3.DeleteLinkIniFile(link);
            ReloadTree();
            ShowHint("已删除无协议连接 " + link + "（ini 段与独立配置文件已清空，整窗实例已释放）。");
        }

        /// <summary>释放指定 FINS 连接的常驻实例（取消桥接、ReleaseInstance、Dispose、从字典移除）。</summary>
        private void ReleaseFinsEditor(int link)
        {
            FormOmron host;
            if (!_finsEditors.TryGetValue(link, out host) || host == null || host.IsDisposed) return;

            // 如果当前正嵌入，先移出面板
            if (_currentHost == host)
                RestoreFromHost();

            try
            {
                if (host.Tag is FormOmron.GetSeletionData h)
                    host.getData -= h;
            }
            catch { }
            try { _comm.Omron.UnregisterChildLink(link); } catch { }
            try { host.ReleaseInstance(); } catch { }
            try { host.Dispose(); } catch { }
            _finsEditors.Remove(link);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 程序关闭时释放全部连接 2~4 实例
            var list = new List<FormOmron>(_finsEditors.Values);
            foreach (var host in list)
            {
                try
                {
                    if (host != null && !host.IsDisposed)
                    {
                        if (host.Tag is FormOmron.GetSeletionData h)
                            host.getData -= h;
                        // 必须反注册：否则主连接单例的 _childLinks 残留已 Dispose 实例，
                        // 关闭管理器后主界面仍会把相机绑定/反馈/切方案锁路由到它（Modbus 两个分支已有此行）。
                        if (_comm != null && _comm.Omron != null) _comm.Omron.UnregisterChildLink(host.LinkId);
                        host.ReleaseInstance();
                        host.Dispose();
                    }
                }
                catch { }
            }
            _finsEditors.Clear();

            // 程序关闭时释放 Modbus-TCP 连接 2~4 整窗实例
            foreach (var host in new List<FormModbus>(_modbusTcpEditors.Values))
            {
                try
                {
                    if (host == null || host.IsDisposed) continue;
                    if (_currentHost == host) RestoreFromHost();
                    if (host.Tag is FormModbus.GetSeletionData h)
                        host.getData -= h;
                    if (_comm != null && _comm.Modbustcp != null) _comm.Modbustcp.UnregisterChildLink(host.LinkId);
                    host.ReleaseInstance();
                    host.Dispose();
                }
                catch { }
            }
            _modbusTcpEditors.Clear();

            // 程序关闭时释放 Modbus-RTU 连接 2~4 整窗实例
            foreach (var host in new List<FormModbusRtu>(_modbusRtuEditors.Values))
            {
                try
                {
                    if (host == null || host.IsDisposed) continue;
                    if (_currentHost == host) RestoreFromHost();
                    if (host.Tag is FormModbusRtu.GetSeletionData h)
                        host.getData -= h;
                    if (_comm != null && _comm.ModbusRtu != null) _comm.ModbusRtu.UnregisterChildLink(host.LinkId);
                    host.ReleaseInstance();
                    host.Dispose();
                }
                catch { }
            }
            _modbusRtuEditors.Clear();

            // 程序关闭时释放无协议连接 2~4 完整整窗实例（ReleaseInstance 停三通道运行时、Dispose）
            foreach (var host in new List<Form3>(_noProtoEditors.Values))
            {
                try
                {
                    if (host == null || host.IsDisposed) continue;
                    if (_currentHost == host) RestoreFromHost();
                    host.ReleaseInstance();
                    host.Dispose();
                }
                catch { }
            }
            _noProtoEditors.Clear();

            // 关闭管理器时，若任一共享单例（FINS 连接1 / Modbus-TCP / Modbus-RTU / 无协议主窗）正被嵌入，
            // 仅将其从面板摘下并保持“子窗体 + 隐藏”态。
            // 不再恢复 TopLevel=true：Form1 旧“查找”菜单的连接 1 入口已全部收归本管理器（SelectGroupLink），
            // 连接 1 与连接 2~4 一样只在管理器内查看（唯一差别是不可删除）；保持 TopLevel=false 可避免下次
            // 重新打开管理器嵌入连接 1 时重建全部句柄导致的卡顿。
            foreach (var host in new Form[] { _finsConn1Host, _modbusTcpHost, _modbusRtuHost, _noProtoHost })
            {
                if (host == null || _currentHost != host) continue;
                try
                {
                    pnlHost.Controls.Remove(host);
                    host.Hide();          // TopLevel/FormBorderStyle/StartPosition 均保持嵌入态，下次嵌入无需重建句柄
                }
                catch { }
            }
            base.OnFormClosing(e);
        }
    }
}
