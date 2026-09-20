using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cognex.VisionPro.PMAlign;
using demo;
using System.Security.Cryptography;
using QRCodeUtil;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.CalibFix;
using System.Globalization;
using WindowsFormsApplication1.Core.Camera;
using WindowsFormsApplication1.Core.Infrastructure;
using WindowsFormsApplication1.Core.Threading;

namespace WindowsFormsApplication1
{
    public delegate void initialize_form();
    public delegate void dispaly_record(ICogRecord record_dispaly);
    public partial class Form1 : Form
    {
        
        private enum ManagerState { Loading, Loaded, Failed }
        private ManagerState managerState = ManagerState.Loading;
        Form9[] f9 = new Form9[12];
        public int cuntu;
        public string shuchuqufan = "Accept";
        public int tongji;
        public decimal zhangshu;
        private double feng;
        public bool zhuanpanEn = false;
        private static readonly object Lock = new object();
        private int datajilu = 0;
        private int gongjujilu = 0;
        string wenjianjia = "";
        public UInt32 m_nBufSizeForSaveImage = 0;
        public UInt32 m_nBufSizeForSaveImage2 = 0;
        public UInt32 m_nBufSizeForSaveImage3 = 0;
        public UInt32 m_nBufSizeForSaveImage4 = 0;
        public UInt32 m_nBufSizeForSaveImage5 = 0;
        public UInt32 m_nBufSizeForSaveImage6 = 0;
        public UInt32 m_nBufSizeForSaveImage7 = 0;
        public UInt32 m_nBufSizeForSaveImage8 = 0;
        public UInt32 m_nBufSizeForSaveImage9 = 0;
        public UInt32 m_nBufSizeForSaveImage10 = 0;
        public UInt32 m_nBufSizeForSaveImage11 = 0;
        public UInt32 m_nBufSizeForSaveImage12 = 0;
        public IntPtr m_pBufForSaveImage = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage2 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage3 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage4 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage5 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage6 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage7 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage8 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage9 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage10 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage11 = IntPtr.Zero;         // 用于保存图像的缓存
        public IntPtr m_pBufForSaveImage12 = IntPtr.Zero;         // 用于保存图像的缓存
        MyCamera.cbOutputExdelegate cbImage;
        // ★P0 修复：SDK 异常回调委托必须由字段长期持有。
        //   原实现以方法组直传（MV_CC_RegisterExceptionCallBack_NET(ExceptionCallBack, …)），
        //   每次隐式 new 一个委托且注册后无任何托管引用 → 可被 GC 回收，原生回调 thunk 失效；
        //   恰在掉线/带宽不足触发异常回调时进入已释放地址（AccessViolation 随机崩溃、极难复现）。
        MyCamera.cbExceptiondelegate cbException;
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
        MyCamera.MV_CC_DEVICE_INFO[] m_pDeviceInfo = new MyCamera.MV_CC_DEVICE_INFO[12];
        private readonly CameraController _cameraCtrl = AppHost.Services.Resolve<CameraController>();
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        bool m_bGrabbing1 = false;
        bool m_bGrabbing2 = false;
        bool m_bGrabbing3 = false;
        bool m_bGrabbing4 = false;
        bool m_bGrabbing5 = false;
        bool m_bGrabbing6 = false;
        bool m_bGrabbing7 = false;
        bool m_bGrabbing8 = false;
        bool m_bGrabbing9 = false;
        bool m_bGrabbing10 = false;
        bool m_bGrabbing11 = false;
        bool m_bGrabbing12 = false;
        int m_nCanOpenDeviceNum;        // ch:设备使用数量 | en:Used Device Number
        int m_nDevNum;        // ch:在线设备数量 | en:Online Device Number
        MyCamera.MV_FRAME_OUT_INFO_EX[] m_stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX[12];
        public UInt32[] m_nSaveImageBufSize = new UInt32[12] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public string[] camera_name = new string[12] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
        public IntPtr[] m_pSaveImageBuf = new IntPtr[12] { IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero };
        private Object[] m_BufForSaveImageLock = new Object[12];
        // 以下死字段已删除（全工程仅声明、无任何读写；相机对象由 CameraController 统一持有）：
        //   private MyCamera mycamera, mycamera1..mycamera12;
        MyCamera.MV_CC_DEVICE_INFO[] device1 = new MyCamera.MV_CC_DEVICE_INFO[12];
        int[] m_nFrames = new int[12];      // ch:帧数 | en:Frame Number
        // ch:用于从驱动获取图像的缓存 | en:Buffer for getting image from driver
        UInt32[] m_nBufSizeForDriver = new uint[12];
        IntPtr m_BufForDriver1;

        IntPtr m_BufForDriver2;

        IntPtr m_BufForDriver3;

        IntPtr m_BufForDriver4;

        IntPtr m_BufForDriver5;

        IntPtr m_BufForDriver6;

        IntPtr m_BufForDriver7;
        IntPtr m_BufForDriver8;
        IntPtr m_BufForDriver9;
        IntPtr m_BufForDriver10;
        IntPtr m_BufForDriver11;
        IntPtr m_BufForDriver12;
        // 以下死字段已删除（全工程仅声明、零使用）：private static Object BufForDriverLock1..12;
        bool[] m_bSaveImg = new bool[12];    // ch:保存图片标志位 | en:Save Image Flag Bit
        IntPtr[] m_hDisplayHandle = new IntPtr[12];
        private readonly LoggingService _logger = AppHost.Services.Resolve<LoggingService>();
        // ★P2-1：每路相机输出/统计的有界任务队列（每路容量 4，满则丢最旧）。
        // 替代 getrecord 里每帧 4 处无条件 Task.Run，避免 12 路 × 高帧率时线程池任务堆积；
        // 慢 PLC/慢操作只拖自己那一路，不影响其它路。队列线程为后台线程，进程退出自然回收；
        // SafeCleanupBeforeDispose 里主动 Dispose 保证退出路径无残留。
        private volatile CameraWorkQueue[] _cameraOutWork;   // ★ volatile：惰性初始化发布后保证其它线程立即可见完整数组
        // ★F9 修复（2026-09-20）：结果回写专用队列——原实现把"结果回写"与"可丢弃的 IO 脉冲/TCP串口输出"
        //   混投同一个容量 4、满时丢旧的 _cameraOutWork（4668/4702 注释自称"不涉及可丢弃策略"，
        //   但实际用的就是那条会丢的队列）；节拍较快时（每帧含 IO 脉冲 Sleep(timespace)≈100ms 的任务）
        //   队列满 → PLC 结果回写被丢弃（统计计数不受影响，但 PLC 漏收该帧结果，可能误判）。
        //   独立队列容量 64（任务仅通信写、耗时短，几乎不会满）；IO 脉冲仍走 _cameraOutWork（可丢弃语义不变）。
        private volatile CameraWorkQueue[] _cameraResultWork;
        private static readonly object _camWorkLock = new object();
        // ★P2-2：丢帧数缓存 + 后台采样。GetLostFrame 的 P/Invoke（MV_CC_GetAllMatchInfo_NET + AllocHGlobal）
        // 原先在 UI 线程（SafeBeginInvoke 回调）每圈 12 次执行 → 界面卡顿。改为 UI_monitor 后台线程每 3 圈
        // （~105ms）采一次写 _lostFrameCache；UI 线程只读缓存字符串赋 label，不再每圈 12 次 P/Invoke。
        private readonly string[] _lostFrameCache = new[] { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0" };
        private int _lostFrameSampleCounter;
        /// <summary>★P2-1：惰性初始化 12 路相机输出队列（lock 双重检查，多线程首调安全）。</summary>
        private void EnsureCameraOutWork()
        {
            if (_cameraOutWork != null) return;
            lock (_camWorkLock)
            {
                if (_cameraOutWork != null) return;
                // ★ 竞态修复：先在局部数组里填满元素，再一次性发布给 volatile 字段。
                //   原实现先发布空数组、后逐个填元素——并发首帧可能在元素仍为 null 时读到数组，
                //   该帧输出/统计/存图整体丢失。
                var arr = new CameraWorkQueue[12];
                var arrRes = new CameraWorkQueue[12];   // ★F9：结果回写专用（容量 64，不丢结果）
                for (int i = 0; i < 12; i++)
                {
                    // ★fix③：注入日志落点（写 LoggingService），任务失败经 CameraWorkQueue.WorkerLoop 用 RateLimitedLog 限流记录，不再空 catch 吞
                    arr[i] = new CameraWorkQueue("cam" + (i + 1), 4, m => _logger.WriteLog(m));
                    arrRes[i] = new CameraWorkQueue("res" + (i + 1), 64, m => _logger.WriteLog(m));
                }
                // ★N8：先发布结果队列、再发布哨兵 _cameraOutWork——锁外快判（EnsureCameraOutWork 首行）只看
                //   _cameraOutWork，原顺序下并发线程可能在其已非空、_cameraResultWork 仍为 null 时直接 return → 首帧 NRE。
                _cameraResultWork = arrRes;
                _cameraOutWork = arr;
            }
        }
        public double jiankongshijian;
        public string zhen = "";
        public bool Cun = false;
        public bool NG = false;
        private bool dahua = false;
        private volatile bool _disposingFlag = false;
        private volatile bool _isFormClosing = false;  // ★ 防止关闭对话框重复弹出
        private volatile bool _switchingScheme = false;  // ★ 方案切换进行中：丢弃回调帧，避免旧帧打到新block
        private readonly InspectionLifecycle _inspectionLifecycle = new InspectionLifecycle();

        // ★ 2026-09-13：触发回帧超时恢复。
        //   PendingCameraTriggers 满员后不驱逐在途记录（保证「记录-图像」配对），
        //   若相机「命令返回成功却一直不回帧」且未触发设备级重连，pending 会占满并持续拒绝新触发。
        //   这里做超时监测：超时则 停采 → 清 pending → 重建取流，让配对整条链路归零。
        private const int TriggerFrameTimeoutMs = 3000;
        private readonly int[] _triggerRecovering = new int[12];
        // ★ 2026-09-13：采集恢复未完成标志。独立于 pending 与设备在线状态：
        //   清 pending 后若重建取流失败，pending 已空、设备仍在线，原逻辑不会再重试；
        //   保留此标志即可每轮重试重建，直到成功。
        private readonly int[] _grabRecoveryPending = new int[12];
        private readonly object[] _inspectLocks = new object[12];
        private readonly AutoResetEvent[] _inspectSignal = new AutoResetEvent[12];
        private readonly Thread[] _inspectThreads = new Thread[12];
        private readonly int[] _saveFlying = new int[12];   // ★ 性能优先：每相机存图任务"单飞"标志（0/1，忙则丢弃本帧存图，允许漏存）
        private readonly int[] _renderBusy = new int[12];   // ★ 性能优先：每相机渲染请求"单飞"标志（0/1，UI 未消化完则丢弃中间帧，允许漏显示）
        // ★ 显示降载（2026-09-19）：全局渲染速率门控。UI 线程光栅化天然串行，缺的是总量上限——
        //   12 路各自单飞叠加仍可能把 UI 打满。限制两次渲染启动的最小间隔（code.ini 可调），
        //   间隔内到达的请求走各自丢帧路径；每相机饥饿计数防某路被高频路永久挤占。
        private int _renderMinIntervalMs = 16;
        private int _lastRenderStartTick;
        private readonly int[] _renderStarve = new int[12];
        // ★ 原图快速路径（弱机现场开关，code.ini [Display]RawImageMode 持久化）：
        //   只贴原图，跳过 VisionPro record 深拷贝与 overlay 光栅化，CPU 降一个量级。
        private volatile bool _displayRawImage;
        private readonly PictureBox[] _rawBox = new PictureBox[12];
        // ★ 交互优先（2026-09-19）：检测到鼠标/键盘活动时临时放宽渲染限速，
        //   操作员看屏时画面跟手；无人交互时按 RenderMinIntervalMs 正常节流省 CPU。
        //   初值回拨 1 秒，避免启动瞬间被误判为"正在交互"。
        private volatile int _lastUserTick = Environment.TickCount - 1000;
        private int _renderInteractiveIntervalMs = 0;   // ★F6：默认 0=交互时不限速（原 20 与 RenderMinIntervalMs 默认 16 组合下"放宽"条件恒假，交互节流成摆设）
        private UserActivityFilter _userActivityFilter;
        private volatile bool _inspectStop;
        private Cognex.VisionPro.CogRecordDisplay[] _cogDisplay;
        private ListBox[] _recentListBox;
        private ListBox[] _recentListBox2;
        private CheckBox[] _recentCheckBox;
        private CheckBox[] _recentCheckBox2;
        private Action<string, int>[] _recentSetbox;
        private Action<string, int>[] _recentSetbox2;
        private CheckBox[] _statCheckBox;
        private DataGridView[] _statDgv;
        private Dictionary<string, int>[] _statD;
        private string _layoutMode = "Grid";
        public Form1()
        {

            InitializeComponent();
            //  this.skinEngine1 = new Sunisoft.IrisSkin.SkinEngine(((System.ComponentModel.Component)(this)));
            // this.skinEngine1.SkinFile = Application.StartupPath + "//Skins//GlassGreen.ssk";
            // skinEngine1.DisableTag = 9999;      //设置不需要被渲染的控件Tag值为9999
            // ★ S3 分阶段守卫：Debug 保留 WinForms 跨线程校验（后台线程误改 UI 会立即抛异常暴露 bug），
            //   Release 维持原有"关闭校验"行为（不改变已部署产线的运行表现）。
            //   待 12 路检测线程改 UI 的调用点全部套 BeginInvoke 收口后，可移除 #if 让 Release 也开启校验。
#if DEBUG
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = true;
#else
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
#endif
            // 初始化相机作业数组，便于按索引访问（现由 JobService 持有，已在组合根构建）
            tbExposure = new TextBox[] { tbExposure1, tbExposure2, tbExposure3, tbExposure4, tbExposure5, tbExposure6, tbExposure7, tbExposure8, tbExposure9, tbExposure10, tbExposure11, tbExposure12 };
            // 相机渲染控件按索引访问，用于收口渲染界面块（_cogDisplay[0] 对应相机1）
            _cogDisplay = new Cognex.VisionPro.CogRecordDisplay[] { cogRecordDisplay1, cogRecordDisplay2, cogRecordDisplay3, cogRecordDisplay4, cogRecordDisplay5, cogRecordDisplay6, cogRecordDisplay7, cogRecordDisplay8, cogRecordDisplay9, cogRecordDisplay10, cogRecordDisplay11, cogRecordDisplay12 };
            // 最近5次记录：相机 -> 控件映射（非顺序，故建映射表；相机9-12 另有第二组列表）
            _recentListBox = new ListBox[] { listBox1, listBox3, listBox7, listBox6, listBox14, listBox15, listBox18, listBox17, listBox19, listBox20, listBox21, listBox22 };
            _recentListBox2 = new ListBox[] { listBox16, listBox23, listBox24, listBox25 };
            _recentCheckBox = new CheckBox[] { checkBox7, checkBox6, checkBox14, checkBox12, checkBox35, checkBox41, checkBox55, checkBox53, checkBox72, checkBox78, checkBox84, checkBox90 };
            _recentCheckBox2 = new CheckBox[] { checkBox47, checkBox96, checkBox97, checkBox98 };
            _recentSetbox = new Action<string, int>[] { setbox5, setbox6, setbox7, setbox8, setbox10, setbox11, setbox12, setbox13, setbox14, setbox15, setbox16, setbox17 };
            _recentSetbox2 = new Action<string, int>[] { setbox18, setbox19, setbox20, setbox21 };
            // 界面表格统计：相机 -> 控件映射（checkBox 非顺序；dataGridView/d 为 1:1）
            _statCheckBox = new CheckBox[] { checkBox11, checkBox8, checkBox56, checkBox57, checkBox58, checkBox59, checkBox60, checkBox61, checkBox73, checkBox79, checkBox85, checkBox91 };
            _statDgv = new DataGridView[] { dataGridView1, dataGridView2, dataGridView3, dataGridView4, dataGridView5, dataGridView6, dataGridView7, dataGridView8, dataGridView9, dataGridView10, dataGridView11, dataGridView12 };
            _statD = new Dictionary<string, int>[] { d1, d2, d3, d4, d5, d6, d7, d8, d9, d10, d11, d12 };
            _jobs.TriggerSoftwareCallback = SendSoftwareTrigger;
            _config.ReadINIFile(AppDomain.CurrentDomain.BaseDirectory + "//code.ini");
            string savedLayout = _config.ReadString("Display", "LayoutMode", "Grid");
            _layoutMode = (savedLayout == "Row") ? "Row" : "Grid";
            // ★ 显示降载配置（2026-09-19）：全局渲染最小间隔（ms，0=不限速）与原图快速路径开关。
            //   弱机现场可把间隔调大（如 33）或开 RawImageMode=1；配置坏值回退默认。
            if (!int.TryParse(_config.ReadString("Display", "RenderMinIntervalMs", "16"), out _renderMinIntervalMs)
                || _renderMinIntervalMs < 0 || _renderMinIntervalMs > 1000)
                _renderMinIntervalMs = 16;
            _displayRawImage = _config.ReadString("Display", "RawImageMode", "0") == "1";
            // 交互期间渲染间隔（ms）：只在用户活动时生效，比 RenderMinIntervalMs 小才有意义
            //（弱机把常规间隔调大省 CPU 时，操作员一动鼠标就临时放宽到本值保流畅）；坏值回退默认
            // ★F6 修复（2026-09-20）：原默认 20 与 RenderMinIntervalMs 默认 16 组合下，
            //   使用条件（_renderInteractiveIntervalMs < interval）恒假 → 交互节流永不生效（摆设）。
            //   默认改为 0（交互时不限速）：弱机把常规间隔调大（如 33）后，用户活动即放宽到不限速保流畅。
            if (!int.TryParse(_config.ReadString("Display", "RenderInteractiveIntervalMs", "0"), out _renderInteractiveIntervalMs)
                || _renderInteractiveIntervalMs < 0 || _renderInteractiveIntervalMs > 1000)
                _renderInteractiveIntervalMs = 0;
            comboBoxLayoutMode.SelectedIndexChanged -= comboBoxLayoutMode_SelectedIndexChanged;
            comboBoxLayoutMode.Items.Clear();
            comboBoxLayoutMode.Items.AddRange(new object[] { "方格布局", "行布局" });
            comboBoxLayoutMode.SelectedItem = (_layoutMode == "Row") ? "行布局" : "方格布局";
            comboBoxLayoutMode.SelectedIndexChanged += comboBoxLayoutMode_SelectedIndexChanged;
            // ★ 修复【启动即崩】：相机枚举放在构造函数且无保护时，SDK DLL 缺失/驱动异常会直接中止启动。
            //   枚举失败仅记录日志并跳过，软件仍可进入界面（相机相关功能不可用，但可排查修复后再开）。
            try
            {
                DeviceListAcq();
            }
            catch (Exception exCamEnum)
            {
                try { _logger.WriteLog("相机枚举失败（已跳过，不影响启动）: " + exCamEnum.Message); } catch { }
            }
            cbImage = new MyCamera.cbOutputExdelegate(ImageCallBack);
            // ★P0：异常回调委托同样只创建一次并常驻（后续不再重建，避免已注册的旧委托失去引用被 GC）
            cbException = new MyCamera.cbExceptiondelegate(ExceptionCallBack);
            for (int i = 0; i < 12; ++i)
            {
                m_BufForSaveImageLock[i] = new Object();
            }
            for (int i = 0; i < 12; ++i)
            {
                m_nBufSizeForDriver[i] = 0;
            }
            for (int i = 0; i < 12; ++i)
            {
                m_nFrames[i] = 0;
            }
            for (int i = 0; i < 12; ++i)
            {
                _inspectLocks[i] = new object();
                _inspectSignal[i] = new AutoResetEvent(false);
            }
            StartInspectWorkers();
            Thread jindu = new Thread(new ThreadStart(InitializeJobManager));
            jindu.IsBackground = true;
            frm3 = new Form3(this);//把Form1当参数传过去,在Form2中就可以使用Form1的变量和控件了

            f1 = new SubSet();

            frm5 = new Form5();
            jindu.Start();
            LoadUiThemeFromIni();
            CreateTabNav();
            ApplyModernUiTheme();
        }
        protected override Point ScrollToControl(Control activeControl)
        {
            return this.AutoScrollPosition;
        }

        private TextBox[] tbExposure;
        public int qqqq;
        int item_sum;
        CogJobManager manager1;
        CogToolGroup group_1;
        CogToolBlock block_1;
        CogToolGroup group_2;
        CogToolBlock block_2;
        CogToolGroup group_3;
        CogToolBlock block_3;
        CogToolGroup group_4;
        CogToolBlock block_4;
        CogToolGroup group_5;
        CogToolBlock block_5;
        CogToolGroup group_6;
        CogToolBlock block_6;
        CogToolGroup group_7;
        CogToolBlock block_7;
        CogToolGroup group_8;
        CogToolBlock block_8;
        CogToolGroup group_9;
        CogToolBlock block_9;
        CogToolGroup group_10;
        CogToolBlock block_10;
        CogToolGroup group_11;
        CogToolBlock block_11;
        CogToolGroup group_12;
        public CogToolBlock block_12;
        public CogToolBlock block_14;
        public CogToolBlock block_21;
        public CogToolBlock block_22;
        public CogToolBlock block_23;
        public CogToolBlock block_24;
        public CogToolBlock block_15;
        public CogToolBlock block_16;
        public CogToolBlock block_17;
        public CogToolBlock block_18;
        public CogToolBlock block_25;
        public CogToolBlock block_26;
        public CogToolBlock block_27;
        public CogToolBlock block_28;
        CogJobIndependent myIndependentJob;
        CogJobIndependent myIndependentJob2;
        CogJobIndependent myIndependentJob3;
        CogJobIndependent myIndependentJob4;
        CogJobIndependent myIndependentJob5;
        CogJobIndependent myIndependentJob6;
        CogJobIndependent myIndependentJob7;
        CogJobIndependent myIndependentJob8;
        CogJobIndependent myIndependentJob9;
        CogJobIndependent myIndependentJob10;
        CogJobIndependent myIndependentJob11;
        CogJobIndependent myIndependentJob12;
        string path_1;
        Stopwatch timewatch2;
        Stopwatch timewatch1;
        /// <summary>运行启动完成后才允许通讯/软触发拍照，避免开软件瞬间误触发（已收口到 JobService.CommTriggerArmed）。</summary>
        private string daoqi = "";
        private bool daoqi_popup_shown = false; // 防止重复弹窗
        int start1;
        private readonly StatisticsService _statistics = AppHost.Services.Resolve<StatisticsService>();
        private readonly JobService _jobs = AppHost.Services.Resolve<JobService>();
        Frm2 Frm2 = new Frm2();
        TimeSpan ti;
        int time;
        DirectoryInfo info1ok;
        DirectoryInfo info1ng;
        DirectoryInfo info2ok;
        DirectoryInfo info2ng;
        DirectoryInfo info3ok;
        DirectoryInfo info3ng;
        DirectoryInfo info4ok;
        DirectoryInfo info4ng;
        DirectoryInfo info3ok1;
        DirectoryInfo info4ok1;
        DirectoryInfo info1ok1;
        DirectoryInfo info2ok1;
        DirectoryInfo info5ok;
        DirectoryInfo info5ng;
        DirectoryInfo info6ok;
        DirectoryInfo info6ng;
        DirectoryInfo info7ok;
        DirectoryInfo info7ng;
        DirectoryInfo info8ok;
        DirectoryInfo info8ng;
        DirectoryInfo info9ok;
        DirectoryInfo info10ok;
        DirectoryInfo info11ok;
        DirectoryInfo info12ok;
        DirectoryInfo info9ng;
        DirectoryInfo info10ng;
        DirectoryInfo info11ng;
        DirectoryInfo info12ng;
        DirectoryInfo info9ok1;
        DirectoryInfo info10ok1;
        DirectoryInfo info11ok1;
        DirectoryInfo info12ok1;
        DirectoryInfo info5ok1;
        DirectoryInfo info6ok1;
        DirectoryInfo info7ok1;
        DirectoryInfo info8ok1;
        int timecount;
        Dictionary<string, int> d1 = new Dictionary<string, int>();
        Dictionary<string, int> d2 = new Dictionary<string, int>();
        Dictionary<string, int> d3 = new Dictionary<string, int>();
        Dictionary<string, int> d4 = new Dictionary<string, int>();
        Dictionary<string, int> d5 = new Dictionary<string, int>();
        Dictionary<string, int> d6 = new Dictionary<string, int>();
        Dictionary<string, int> d7 = new Dictionary<string, int>();
        Dictionary<string, int> d8 = new Dictionary<string, int>();
        Dictionary<string, int> d9 = new Dictionary<string, int>();
        Dictionary<string, int> d10 = new Dictionary<string, int>();
        Dictionary<string, int> d11 = new Dictionary<string, int>();
        Dictionary<string, int> d12 = new Dictionary<string, int>();
        List<FileInfo> ls1 = new List<FileInfo>();
        List<FileInfo> ls2 = new List<FileInfo>();
        string day1;
        string day2;

        private FlowLayoutPanel flowTabNav;
        private const int TabNavScrollBarGap = 16;
        private const int TabNavExtraLift = 4;
        private Button[] tabNavButtons;
        private TabPage[] tabNavPages;
        private Color[] _tabNavCameraBaseColors;
        private Label[] monitorTotalTimeLabels;
        private readonly ConfigService _config = AppHost.Services.Resolve<ConfigService>();
        private ClassIni duini = new ClassIni();
        #region 初始化
        /// <summary>
        /// 更新欢迎界面进度
        /// </summary>
        private void UpdateSplashProgress(int value, string status)
        {
            try
            {
                if (Frm2 != null)
                {
                    if (value >= 0 && value <= 100)
                    {
                        Frm2._progressValue = value;
                    }
                    Frm2._statusText = status ?? "";
                }
            }
            catch { }
        }

        private void InitializeJobManager()
        {
            try
            {
                try
                {
                    feng = 0;
                    cuntu = 0;
                    tongji = 0;
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].ok1 = 0; _jobs.Myjobs[_i].roi = false; }
                    for (int _i = 0; _i < 12; _i++) _jobs.Myjobs[_i].biaotou = "";
                    for (int _i = 0; _i < 12; _i++) _jobs.Myjobs[_i].ng1 = 0;
                    for (int _i = 0; _i < 12; _i++) _jobs.Myjobs[_i].IOyanshi = 0;
                    for (int _i = 0; _i < 12; _i++) _jobs.Myjobs[_i].timewatch = new Stopwatch();
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].en = 0; _jobs.Myjobs[_i].address = ""; }
                    _jobs.myjob1.dlg = new FolderBrowserDialog();
                    _jobs.myjob2.dlg = new FolderBrowserDialog();
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].cuntu = true; _jobs.Myjobs[_i].xuanran = true; _jobs.Myjobs[_i].IO = true; }
                    for (int _i = 0; _i < 12; _i++) _jobs.Myjobs[_i].myTable = new DataTable();
                    _jobs.myjob1.myTable1 = new DataTable();
                    _jobs.myjob2.myTable1 = new DataTable();
                    int[] _changdu = { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110 };
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].danwu_cishu = 0; _jobs.Myjobs[_i].time = 0; _jobs.Myjobs[_i].changdu = _changdu[_i]; _jobs.Myjobs[_i].state = ""; _jobs.Myjobs[_i].triggerMode = ""; _jobs.Myjobs[_i].triggerZifu = ""; _jobs.Myjobs[_i].jieshouZifu = "null"; _jobs.Myjobs[_i].commTriggerPending = false; _jobs.Myjobs[_i].commTriggerPendingCount = 0; }
                    _jobs.myjob1.danwu_time = "";
                    _jobs.myjob2.danwu_time = "";
                    _jobs.myjob3.danwu_time = "";
                    _jobs.myjob4.danwu_time = "";
                    _jobs.myjob1.myhandle += new delegateHanndler(IO1OK);
                    _jobs.myjob1.myhandle1 += new delegateHanndler(IO1NG);
                    _jobs.myjob2.myhandle += new delegateHanndler(IO2OK);
                    _jobs.myjob2.myhandle1 += new delegateHanndler(IO2NG);
                    _jobs.myjob3.myhandle += new delegateHanndler(IO3OK);
                    _jobs.myjob3.myhandle1 += new delegateHanndler(IO3NG);
                    _jobs.myjob4.myhandle += new delegateHanndler(IO4OK);
                    _jobs.myjob4.myhandle1 += new delegateHanndler(IO4NG);
                    _jobs.myjob5.myhandle += new delegateHanndler(IO5OK);
                    _jobs.myjob5.myhandle1 += new delegateHanndler(IO5NG);
                    _jobs.myjob6.myhandle += new delegateHanndler(IO6OK);
                    _jobs.myjob6.myhandle1 += new delegateHanndler(IO6NG);
                    _jobs.myjob7.myhandle += new delegateHanndler(IO7OK);
                    _jobs.myjob7.myhandle1 += new delegateHanndler(IO7NG);
                    _jobs.myjob8.myhandle += new delegateHanndler(IO8OK);
                    _jobs.myjob8.myhandle1 += new delegateHanndler(IO8NG);
                    _jobs.myjob9.myhandle += new delegateHanndler(IO9OK);
                    _jobs.myjob9.myhandle1 += new delegateHanndler(IO9NG);
                    _jobs.myjob10.myhandle += new delegateHanndler(IO10OK);
                    _jobs.myjob10.myhandle1 += new delegateHanndler(IO10NG);
                    _jobs.myjob11.myhandle += new delegateHanndler(IO11OK);
                    _jobs.myjob11.myhandle1 += new delegateHanndler(IO11NG);
                    _jobs.myjob12.myhandle += new delegateHanndler(IO12OK);
                    _jobs.myjob12.myhandle1 += new delegateHanndler(IO12NG);
                    for (int _i = 0; _i < 12; _i++) _jobs.Myjobs[_i].Color = false;
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].tishi = false; _jobs.Myjobs[_i].modbustemp = false; _jobs.Myjobs[_i].dengluEn = false; }
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].runtime = 0; _jobs.Myjobs[_i].runcishu = 0; _jobs.Myjobs[_i].index = -1; _jobs.Myjobs[_i].xianshi = 0; }
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].shijianEn = false; _jobs.Myjobs[_i].tcp = false; _jobs.Myjobs[_i].serial = false; }
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].newrecod = null; _jobs.Myjobs[_i].timespace = 100; }
                    int[] _master = { 0, 6, 12, 18, 24, 30, 36, 42, 48, 54, 60, 66 };
                    for (int _i = 0; _i < 12; _i++) { _jobs.Myjobs[_i].master = _master[_i]; _jobs.Myjobs[_i].trrigersum = 0; _jobs.Myjobs[_i].trriger = 0; }
                    jiankongshijian = 1000;
                    _jobs.myjob1.pathhead_ok = @"E:\fu1ok\";
                    _jobs.myjob2.pathhead_ok = @"E:\fu2ok\";
                    _jobs.myjob3.pathhead_ok = @"E:\fu3ok\";
                    _jobs.myjob4.pathhead_ok = @"E:\fu4ok\";
                    _jobs.myjob1.pathhead_ng = @"E:\fu1ng\";
                    _jobs.myjob2.pathhead_ng = @"E:\fu2ng\";
                    _jobs.myjob3.pathhead_ng = @"E:\fu3ng\";
                    _jobs.myjob4.pathhead_ng = @"E:\fu4ng\";
                    _jobs.myjob5.pathhead_ok = @"E:\fu5ok\";
                    _jobs.myjob6.pathhead_ok = @"E:\fu6ok\";
                    _jobs.myjob7.pathhead_ok = @"E:\fu7ok\";
                    _jobs.myjob8.pathhead_ok = @"E:\fu8ok\";
                    _jobs.myjob5.pathhead_ng = @"E:\fu5ng\";
                    _jobs.myjob6.pathhead_ng = @"E:\fu6ng\";
                    _jobs.myjob7.pathhead_ng = @"E:\fu7ng\";
                    _jobs.myjob8.pathhead_ng = @"E:\fu8ng\";
                    _jobs.myjob9.pathhead_ok = @"E:\fu9ok\";
                    _jobs.myjob10.pathhead_ok = @"E:\fu10ok\";
                    _jobs.myjob11.pathhead_ok = @"E:\fu11ok\";
                    _jobs.myjob12.pathhead_ok = @"E:\fu12ok\";
                    _jobs.myjob9.pathhead_ng = @"E:\fu9ng\";
                    _jobs.myjob10.pathhead_ng = @"E:\fu10ng\";
                    _jobs.myjob11.pathhead_ng = @"E:\fu11ng\";
                    _jobs.myjob12.pathhead_ng = @"E:\fu12ng\";
                    day1 = DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);   // ★ 存图子目录按日期命名（原为星期名，每周复用导致误删/堆叠）
                    _jobs.myjob1.fileng = new FileInfo(_jobs.myjob1.pathhead_ng + day1 + "\\");
                    _jobs.myjob2.fileng = new FileInfo(_jobs.myjob2.pathhead_ng + day1 + "\\");
                    _jobs.myjob1.fileok = new FileInfo(_jobs.myjob1.pathhead_ok + day1 + "\\");
                    _jobs.myjob2.fileok = new FileInfo(_jobs.myjob2.pathhead_ok + day1 + "\\");
                    _jobs.myjob3.fileng = new FileInfo(_jobs.myjob3.pathhead_ng + day1 + "\\");
                    _jobs.myjob3.fileok = new FileInfo(_jobs.myjob3.pathhead_ok + day1 + "\\");
                    _jobs.myjob4.fileng = new FileInfo(_jobs.myjob4.pathhead_ng + day1 + "\\");
                    _jobs.myjob4.fileok = new FileInfo(_jobs.myjob4.pathhead_ok + day1 + "\\");
                    _jobs.myjob5.fileng = new FileInfo(_jobs.myjob5.pathhead_ng + day1 + "\\");
                    _jobs.myjob6.fileng = new FileInfo(_jobs.myjob6.pathhead_ng + day1 + "\\");
                    _jobs.myjob5.fileok = new FileInfo(_jobs.myjob5.pathhead_ok + day1 + "\\");
                    _jobs.myjob6.fileok = new FileInfo(_jobs.myjob6.pathhead_ok + day1 + "\\");
                    _jobs.myjob7.fileng = new FileInfo(_jobs.myjob7.pathhead_ng + day1 + "\\");
                    _jobs.myjob7.fileok = new FileInfo(_jobs.myjob7.pathhead_ok + day1 + "\\");
                    _jobs.myjob8.fileng = new FileInfo(_jobs.myjob8.pathhead_ng + day1 + "\\");
                    _jobs.myjob8.fileok = new FileInfo(_jobs.myjob8.pathhead_ok + day1 + "\\");
                    _jobs.myjob9.fileng = new FileInfo(_jobs.myjob9.pathhead_ng + day1 + "\\");
                    _jobs.myjob9.fileok = new FileInfo(_jobs.myjob9.pathhead_ok + day1 + "\\");
                    _jobs.myjob10.fileng = new FileInfo(_jobs.myjob10.pathhead_ng + day1 + "\\");
                    _jobs.myjob10.fileok = new FileInfo(_jobs.myjob10.pathhead_ok + day1 + "\\");
                    _jobs.myjob11.fileng = new FileInfo(_jobs.myjob11.pathhead_ng + day1 + "\\");
                    _jobs.myjob11.fileok = new FileInfo(_jobs.myjob11.pathhead_ok + day1 + "\\");
                    _jobs.myjob12.fileng = new FileInfo(_jobs.myjob12.pathhead_ng + day1 + "\\");
                    _jobs.myjob12.fileok = new FileInfo(_jobs.myjob12.pathhead_ok + day1 + "\\");
                    timecount = 0;
                    info1ok = new DirectoryInfo(_jobs.myjob1.pathhead_ok + day1 + "\\");
                    info1ng = new DirectoryInfo(_jobs.myjob1.pathhead_ng + day1 + "\\");
                    info1ok1 = new DirectoryInfo(_jobs.myjob1.pathhead_ok);
                    info2ok1 = new DirectoryInfo(_jobs.myjob2.pathhead_ok);
                    info3ok1 = new DirectoryInfo(_jobs.myjob3.pathhead_ok);
                    info4ok1 = new DirectoryInfo(_jobs.myjob4.pathhead_ok);
                    info5ok1 = new DirectoryInfo(_jobs.myjob5.pathhead_ok);
                    info6ok1 = new DirectoryInfo(_jobs.myjob6.pathhead_ok);
                    info7ok1 = new DirectoryInfo(_jobs.myjob7.pathhead_ok);
                    info8ok1 = new DirectoryInfo(_jobs.myjob8.pathhead_ok);
                    info9ok1 = new DirectoryInfo(_jobs.myjob9.pathhead_ok);
                    info10ok1 = new DirectoryInfo(_jobs.myjob10.pathhead_ok);
                    info11ok1 = new DirectoryInfo(_jobs.myjob11.pathhead_ok);
                    info12ok1 = new DirectoryInfo(_jobs.myjob12.pathhead_ok);
                    // ★ F13: 用 culture-invariant 的 yyyyMMdd 格式替代默认 ToString+Split，
                    //        避免区域设置（如英文系统 M/d/yyyy）导致日期数字拼接顺序不同、误删当日文件夹
                    int bb1 = int.Parse(info1ng.CreationTime.ToString("yyyyMMdd"));
                    int bbnow = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb1 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob1.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob1.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob1.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    info2ok = new DirectoryInfo(_jobs.myjob2.pathhead_ok + day1 + "\\");
                    info2ng = new DirectoryInfo(_jobs.myjob2.pathhead_ng + day1 + "\\");
                    int bb2 = int.Parse(info2ng.CreationTime.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb2 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob2.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob2.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob2.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    info3ok = new DirectoryInfo(_jobs.myjob3.pathhead_ok + day1 + "\\");
                    info3ng = new DirectoryInfo(_jobs.myjob3.pathhead_ng + day1 + "\\");
                    int bb3 = int.Parse(info3ng.CreationTime.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb3 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob3.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob3.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob3.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    info4ok = new DirectoryInfo(_jobs.myjob4.pathhead_ok + day1 + "\\");
                    info4ng = new DirectoryInfo(_jobs.myjob4.pathhead_ng + day1 + "\\");
                    int bb4 = int.Parse(info4ng.CreationTime.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb4 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob4.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob4.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob4.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    info5ok = new DirectoryInfo(_jobs.myjob5.pathhead_ok + day1 + "\\");
                    info5ng = new DirectoryInfo(_jobs.myjob5.pathhead_ng + day1 + "\\");
                    int bb5 = int.Parse(info5ng.CreationTime.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb5 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob5.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob5.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob5.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    info6ok = new DirectoryInfo(_jobs.myjob6.pathhead_ok + day1 + "\\");
                    info6ng = new DirectoryInfo(_jobs.myjob6.pathhead_ng + day1 + "\\");
                    int bb6 = int.Parse(info6ng.CreationTime.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb6 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob6.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob6.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob6.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    info7ok = new DirectoryInfo(_jobs.myjob7.pathhead_ok + day1 + "\\");
                    info7ng = new DirectoryInfo(_jobs.myjob7.pathhead_ng + day1 + "\\");
                    int bb7 = int.Parse(info7ng.CreationTime.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb7 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob7.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob7.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob7.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    info8ok = new DirectoryInfo(_jobs.myjob8.pathhead_ok + day1 + "\\");
                    info8ng = new DirectoryInfo(_jobs.myjob8.pathhead_ng + day1 + "\\");
                    info9ok = new DirectoryInfo(_jobs.myjob9.pathhead_ok + day1 + "\\");
                    info9ng = new DirectoryInfo(_jobs.myjob9.pathhead_ng + day1 + "\\");
                    info10ok = new DirectoryInfo(_jobs.myjob10.pathhead_ok + day1 + "\\");
                    info10ng = new DirectoryInfo(_jobs.myjob10.pathhead_ng + day1 + "\\");
                    info11ok = new DirectoryInfo(_jobs.myjob11.pathhead_ok + day1 + "\\");
                    info11ng = new DirectoryInfo(_jobs.myjob11.pathhead_ng + day1 + "\\");
                    info12ok = new DirectoryInfo(_jobs.myjob12.pathhead_ok + day1 + "\\");
                    info12ng = new DirectoryInfo(_jobs.myjob12.pathhead_ng + day1 + "\\");
                    int bb8 = int.Parse(info8ng.CreationTime.ToString("yyyyMMdd"));
                    try
                    {
                        if (bb8 != bbnow)
                        {
                            if (Directory.Exists(_jobs.myjob8.pathhead_ng + day1))
                            {
                                foreach (string f in Directory.GetFileSystemEntries(_jobs.myjob8.pathhead_ng + day1))
                                {
                                    if (File.Exists(f))
                                    {
                                        //如果有子文件删除文件
                                        File.Delete(f);
                                    }
                                }
                                //删除空文件夹
                                Directory.Delete(_jobs.myjob8.pathhead_ng + day1);
                            }
                        }
                    }
                    catch { }
                    try
                    {
                        _jobs.myjob1.number = info1ok.GetFileSystemInfos().Length;
                        _jobs.myjob1.numberng = info1ng.GetFileSystemInfos().Length;
                        _jobs.myjob2.number = info2ok.GetFileSystemInfos().Length;
                        _jobs.myjob2.numberng = info2ng.GetFileSystemInfos().Length;
                        _jobs.myjob3.number = info3ok.GetFileSystemInfos().Length;
                        _jobs.myjob3.numberng = info3ng.GetFileSystemInfos().Length;
                        _jobs.myjob4.number = info4ok.GetFileSystemInfos().Length;
                        _jobs.myjob4.numberng = info4ng.GetFileSystemInfos().Length;
                        _jobs.myjob5.number = info5ok.GetFileSystemInfos().Length;
                        _jobs.myjob5.numberng = info5ng.GetFileSystemInfos().Length;
                        _jobs.myjob6.number = info6ok.GetFileSystemInfos().Length;
                        _jobs.myjob6.numberng = info6ng.GetFileSystemInfos().Length;
                        _jobs.myjob7.number = info7ok.GetFileSystemInfos().Length;
                        _jobs.myjob7.numberng = info7ng.GetFileSystemInfos().Length;
                        _jobs.myjob8.number = info8ok.GetFileSystemInfos().Length;
                        _jobs.myjob8.numberng = info8ng.GetFileSystemInfos().Length;
                        _jobs.myjob9.number = info9ok.GetFileSystemInfos().Length;
                        _jobs.myjob10.number = info10ok.GetFileSystemInfos().Length;
                        _jobs.myjob11.number = info11ok.GetFileSystemInfos().Length;
                        _jobs.myjob12.number = info12ok.GetFileSystemInfos().Length;
                        _jobs.myjob9.numberng = info9ng.GetFileSystemInfos().Length;
                        _jobs.myjob10.numberng = info10ng.GetFileSystemInfos().Length;
                        _jobs.myjob11.numberng = info11ng.GetFileSystemInfos().Length;
                        _jobs.myjob12.numberng = info12ng.GetFileSystemInfos().Length;

                    }
                    catch { }
                }
                catch (Exception ex)
                { _logger.WriteLog(ex.Message + "tu"); };

                time = 6;
                ti = new TimeSpan(time);

                _jobs.myjob1.out_end = 0;
                _jobs.myjob2.out_end = 0;
                _jobs.myjob3.out_end = 0;
                _jobs.myjob4.out_end = 0;
                _jobs.myjob5.out_end = 0;
                _jobs.myjob6.out_end = 0;
                _jobs.myjob7.out_end = 0;
                _jobs.myjob8.out_end = 0;
                _jobs.myjob9.out_end = 0;
                _jobs.myjob10.out_end = 0;
                _jobs.myjob11.out_end = 0;
                _jobs.myjob12.out_end = 0;
                _jobs.myjob1.path_number = "1";
                _jobs.myjob2.path_number = "2";
                _jobs.myjob3.path_number = "3";
                _jobs.myjob4.path_number = "4";
                _jobs.myjob5.path_number = "5";
                _jobs.myjob6.path_number = "6";
                _jobs.myjob7.path_number = "7";
                _jobs.myjob8.path_number = "8";
                _jobs.myjob9.path_number = "9";
                _jobs.myjob10.path_number = "10";
                _jobs.myjob11.path_number = "11";
                _jobs.myjob12.path_number = "12";
                start1 = 0;
                _jobs.myjob1.yun = 0;
                _jobs.myjob2.yun = 0;
                _jobs.myjob3.yun = 0;
                _jobs.myjob4.yun = 0;
                _jobs.myjob5.yun = 0;
                _jobs.myjob6.yun = 0;
                _jobs.myjob7.yun = 0;
                _jobs.myjob8.yun = 0;
                _jobs.myjob9.yun = 0;
                _jobs.myjob10.yun = 0;
                _jobs.myjob11.yun = 0;
                _jobs.myjob12.yun = 0;
                _jobs.yunxing = false;

                UpdateSplashProgress(5, "正在读取方案...");
                // ★ 方案加载是耗时大头：加载期间进度条缓慢爬升（5%→14%），避免进度条长时间停住
                System.Threading.CancellationTokenSource splashCts = new System.Threading.CancellationTokenSource();
                System.Threading.Tasks.Task splashTask = System.Threading.Tasks.Task.Run(() =>
                {
                    int v = 5;
                    while (!splashCts.IsCancellationRequested && v < 14)
                    {
                        System.Threading.Thread.Sleep(250);
                        v++;
                        UpdateSplashProgress(v, "正在加载方案...");
                    }
                });

                string pppp = AppDomain.CurrentDomain.BaseDirectory + "di.vpp";
                path_1 = _config.ReadString("path", "path_1", pppp).Replace("\0", "");
                // string ph = "QuickBuild1附件6.vpp";
                //path_1 = AppDomain.CurrentDomain.BaseDirectory + "di.vpp";
                _statistics.CreateDirectoryCsvPath(_jobs.myjob1.path_number);
                if (File.Exists(path_1))
                {
                    try
                    {
                        manager1 = (CogJobManager)CogSerializer.LoadObjectFromFile(path_1);
                _jobs.JobManager = manager1;
                        managerState = ManagerState.Loaded;
                        UpdateSplashProgress(15, "方案加载完成，正在初始化作业...");
                    }
                    catch (Exception ex)
                    {
                        // ★F4 修复（2026-09-20）：原实现把提示文字拼进 path_1（"xxx.vpp方案已损坏"）——
                        //   path_1 随后会写进 Menu.ini 历史（菜单留下可点必败的坏项）、显示到界面，
                        //   并污染后续 Path.GetDirectoryName 等用途。现 path_1 保持纯净，提示走日志与界面。
                        managerState = ManagerState.Failed;
                        _logger.WriteLog(ex.Message + "方案加载失败!");
                    }
                    finally
                    {
                        splashCts.Cancel();   // 停止爬升动画
                    }
                }
                else
                {
                    managerState = ManagerState.Failed;
                    _logger.WriteLog("方案文件不存在: " + path_1);
                }
                UpdateSplashProgress(18, "正在读取配置...");
                // ★ 修复（2026-09-06）：yanshi 非数字时 int.Parse 会抛异常，被下方大 catch 吞掉，
                //   导致 12 路作业绑定整段跳过、软件“假运行”（相机取流但永不判定）。改用 TryParse，失败时回退默认 5。
                int _yanshiMs;
                if (!int.TryParse(_config.ReadString("camera", "yanshi", "5"), out _yanshiMs))
                    _yanshiMs = 5;
                Thread.Sleep(_yanshiMs);
                StreamReader sr = null;
                // ★N6：菜单项增删属 UI 操作，整段收口到 UI 线程——原实现后台线程裸操作 DropDownItems（Debug 抛跨线程异常/Release 竞态；F1 围栏未覆盖此段）
                this.Invoke(new Action(() =>
                {
                    item_sum = this.设置ToolStripMenuItem.DropDownItems.Count;
                    try
                    {
                        int i = this.设置ToolStripMenuItem.DropDownItems.Count;
                        if (i > item_sum)
                        {
                            for (int j = 0; j < i; j++)
                            {
                                if (j >= item_sum)
                                {
                                    this.设置ToolStripMenuItem.DropDownItems.RemoveAt(item_sum);
                                }
                            }
                        }
                        sr = new StreamReader(Path.GetDirectoryName(path_1) + "\\Menu.ini");
                        i = item_sum;
                        while (sr.Peek() >= 0)
                        {
                            menuitem = new ToolStripMenuItem(sr.ReadLine());
                            this.设置ToolStripMenuItem.DropDownItems.Insert(i, menuitem);
                            i++;
                            menuitem.Click += new EventHandler(menuitem_Click);
                        }
                        sr.Dispose();
                        sr.Close();
                    }
                    catch
                    {
                        try
                        {
                            sr.Dispose();
                            sr.Close();
                        }
                        catch { }
                    }
                    sr = null;
                }));
                try
                {
                    sr = new StreamReader(Path.GetDirectoryName(path_1) + "\\Menu.ini");
                    int i = 0;
                    while (sr.Peek() >= 0)
                    {
                        i++;
                        sr.ReadLine();
                    }
                    sr.Dispose();
                    sr.Close();
                    if (i > 5)
                    {
                        FileStream stream = null;
                        try
                        {
                            stream = File.Open(Path.GetDirectoryName(path_1) + "\\Menu.ini", FileMode.OpenOrCreate, FileAccess.Write);
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.SetLength(0);
                            stream.Flush();
                            stream.Close();
                        }
                        catch
                        {
                            stream.Flush();
                            stream.Close();
                        }
                    }
                }
                catch
                {
                    try
                    {
                        sr.Dispose();
                        sr.Close();
                    }
                    catch { }

                };
                // ★F1 修复（2026-09-20）：本段原为裸代码（无就近 try）——一旦抛异常（方案目录只读/无写权限、
                //   DropDownItems 为空时 [Count-1] 越界等），异常会被外层 catch(1148) 吞掉并【跳过
                //   999-1140 的 12 路作业绑定】→ block 全空（Form6 空白/Form9 崩溃/运行结果缺组），
                //   与 95b73da 修的是同一类病。现单独包 try 只记日志，绝不让它阻断后续绑定。
                try
                {
                    if (this.设置ToolStripMenuItem.DropDownItems[this.设置ToolStripMenuItem.DropDownItems.Count - 1].Text != path_1)
                    {
                        StreamWriter s = new StreamWriter(Path.GetDirectoryName(path_1) + "\\Menu.ini", true);
                        s.WriteLine(path_1);
                        s.Flush();
                        s.Close();
                    }
                }
                catch (Exception exMenu)
                {
                    _logger.WriteLog("Menu.ini 历史记录写入失败（已忽略，不影响后续作业绑定）: " + exMenu.Message);
                }
                // ★修复（2026-09-20 · 启动初始化被打断的根因）：
                //   本方法由独立后台线程启动（Form1.cs:297 new Thread(InitializeJobManager)），
                //   且 Debug 构建显式开启 Control.CheckForIllegalCrossThreadCalls=true（Form1.cs:227 S3 守卫）——
                //   后台线程直接设置界面控件 label75/label173 会抛"线程间操作无效"（日志中 ...111），
                //   该异常被下方外层 catch(1148) 吞掉后会【跳过 999-1140 的 12 路作业绑定】，
                //   最终 initialize_FormSet 里 myjobN.job 全为 null → block 全空：
                //   表现为 Form6 空白 / Form9 枚举崩溃 / 运行结果缺组 / 画面无流程数据。
                //   改为线程安全投递（SafeBeginInvoke 内部已 try/catch，句柄销毁时静默）。
                string _p1ForUi = path_1;
                try
                {
                    SafeBeginInvoke(new Action(() =>
                    {
                        try
                        {
                            label75.Text = _p1ForUi.Split('\\').Last();
                            label173.Text = _p1ForUi;
                            // ★F4：加载失败时在界面提示（不改动 path_1 本身，保持路径纯净可写回历史）
                            if (managerState == ManagerState.Failed)
                                label75.Text = label75.Text + "（方案已损坏或不存在）";
                        }
                        catch { }
                    }));
                }
                catch { }
                try
                {
                    wenjianjia = Path.GetDirectoryName(path_1);
                    _logger.WriteLog("方案文件夹:" + wenjianjia);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                }
                try
                {
                    UpdateSplashProgress(19, "正在加载VisionPro作业...");
                    if (manager1.JobCount > 0)
                    {
                        manager1.UserQueueFlush();
                        manager1.FailureQueueFlush();
                        try
                        {
                            _jobs.myjob1.job = manager1.Job(0);
                            myIndependentJob = _jobs.myjob1.job.OwnedIndependent;
                            _jobs.myjob1.job.ImageQueueFlush();
                            _jobs.myjob1.Cogbmp = new CogImageFileBMP();
                            myIndependentJob.RealTimeQueueFlush();
                        }
                        catch (Exception ex1) { _logger.WriteLog("相机1 作业绑定失败: " + ex1.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(20, "正在加载作业 (1/12)...");

                        // path_1 = @".\test.vpp";

                    }
                    frm3.jobsum = manager1.JobCount;
                    if (manager1.JobCount > 1)
                    {


                        try
                        {
                            _jobs.myjob2.job = manager1.Job(1);
                            myIndependentJob2 = _jobs.myjob2.job.OwnedIndependent;
                            _jobs.myjob2.job.ImageQueueFlush();
                            _jobs.myjob2.Cogbmp = new CogImageFileBMP();
                            myIndependentJob2.RealTimeQueueFlush();
                        }
                        catch (Exception ex2) { _logger.WriteLog("相机2 作业绑定失败: " + ex2.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(21, "正在加载作业 (2/12)...");

                    }
                    if (manager1.JobCount > 2)
                    {

                        try
                        {
                            _jobs.myjob3.job = manager1.Job(2);
                            myIndependentJob3 = _jobs.myjob3.job.OwnedIndependent;
                            _jobs.myjob3.job.ImageQueueFlush();
                            _jobs.myjob3.Cogbmp = new CogImageFileBMP();
                            myIndependentJob3.RealTimeQueueFlush();
                        }
                        catch (Exception ex3) { _logger.WriteLog("相机3 作业绑定失败: " + ex3.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(22, "正在加载作业 (3/12)...");

                    }

                    if (manager1.JobCount > 3)
                    {
                        try
                        {
                            _jobs.myjob4.job = manager1.Job(3);

                            myIndependentJob4 = _jobs.myjob4.job.OwnedIndependent;
                            _jobs.myjob4.job.ImageQueueFlush();
                            _jobs.myjob4.Cogbmp = new CogImageFileBMP();
                            myIndependentJob4.RealTimeQueueFlush();
                        }
                        catch (Exception ex4) { _logger.WriteLog("相机4 作业绑定失败: " + ex4.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(23, "正在加载作业 (4/12)...");

                    }
                    if (manager1.JobCount > 4)
                    {
                        try
                        {
                            _jobs.myjob5.job = manager1.Job(4);

                            myIndependentJob5 = _jobs.myjob5.job.OwnedIndependent;
                            _jobs.myjob5.job.ImageQueueFlush();
                            _jobs.myjob5.Cogbmp = new CogImageFileBMP();
                            myIndependentJob5.RealTimeQueueFlush();
                        }
                        catch (Exception ex5) { _logger.WriteLog("相机5 作业绑定失败: " + ex5.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(24, "正在加载作业 (5/12)...");

                    }
                    if (manager1.JobCount > 5)
                    {
                        try
                        {
                            _jobs.myjob6.job = manager1.Job(5);

                            myIndependentJob6 = _jobs.myjob6.job.OwnedIndependent;
                            _jobs.myjob6.job.ImageQueueFlush();
                            _jobs.myjob6.Cogbmp = new CogImageFileBMP();
                            myIndependentJob6.RealTimeQueueFlush();
                        }
                        catch (Exception ex6) { _logger.WriteLog("相机6 作业绑定失败: " + ex6.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(25, "正在加载作业 (6/12)...");

                    }
                    if (manager1.JobCount > 6)
                    {
                        try
                        {
                            _jobs.myjob7.job = manager1.Job(6);

                            myIndependentJob7 = _jobs.myjob7.job.OwnedIndependent;
                            _jobs.myjob7.job.ImageQueueFlush();
                            _jobs.myjob7.Cogbmp = new CogImageFileBMP();
                            myIndependentJob7.RealTimeQueueFlush();
                        }
                        catch (Exception ex7) { _logger.WriteLog("相机7 作业绑定失败: " + ex7.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(26, "正在加载作业 (7/12)...");

                    }
                    if (manager1.JobCount > 7)
                    {
                        try
                        {
                            _jobs.myjob8.job = manager1.Job(7);

                            myIndependentJob8 = _jobs.myjob8.job.OwnedIndependent;
                            _jobs.myjob8.job.ImageQueueFlush();
                            _jobs.myjob8.Cogbmp = new CogImageFileBMP();
                            myIndependentJob8.RealTimeQueueFlush();
                        }
                        catch (Exception ex8) { _logger.WriteLog("相机8 作业绑定失败: " + ex8.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(27, "正在加载作业 (8/12)...");

                    }
                    if (manager1.JobCount > 8)
                    {
                        try
                        {
                            _jobs.myjob9.job = manager1.Job(8);

                            myIndependentJob9 = _jobs.myjob9.job.OwnedIndependent;
                            _jobs.myjob9.job.ImageQueueFlush();
                            _jobs.myjob9.Cogbmp = new CogImageFileBMP();
                            myIndependentJob9.RealTimeQueueFlush();
                        }
                        catch (Exception ex9) { _logger.WriteLog("相机9 作业绑定失败: " + ex9.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(28, "正在加载作业 (9/12)...");

                    }
                    if (manager1.JobCount > 9)
                    {
                        try
                        {
                            _jobs.myjob10.job = manager1.Job(9);

                            myIndependentJob10 = _jobs.myjob10.job.OwnedIndependent;
                            _jobs.myjob10.job.ImageQueueFlush();
                            _jobs.myjob10.Cogbmp = new CogImageFileBMP();
                            myIndependentJob10.RealTimeQueueFlush();
                        }
                        catch (Exception ex10) { _logger.WriteLog("相机10 作业绑定失败: " + ex10.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(29, "正在加载作业 (10/12)...");

                    }
                    if (manager1.JobCount > 10)
                    {
                        try
                        {
                            _jobs.myjob11.job = manager1.Job(10);

                            myIndependentJob11 = _jobs.myjob11.job.OwnedIndependent;
                            _jobs.myjob11.job.ImageQueueFlush();
                            _jobs.myjob11.Cogbmp = new CogImageFileBMP();
                            myIndependentJob11.RealTimeQueueFlush();
                        }
                        catch (Exception ex11) { _logger.WriteLog("相机11 作业绑定失败: " + ex11.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(30, "正在加载作业 (11/12)...");

                    }
                    if (manager1.JobCount > 11)
                    {
                        try
                        {
                            _jobs.myjob12.job = manager1.Job(11);

                            myIndependentJob12 = _jobs.myjob12.job.OwnedIndependent;
                            _jobs.myjob12.job.ImageQueueFlush();
                            _jobs.myjob12.Cogbmp = new CogImageFileBMP();
                            myIndependentJob12.RealTimeQueueFlush();
                        }
                        catch (Exception ex12) { _logger.WriteLog("相机12 作业绑定失败: " + ex12.Message); }   // ★N6：逐路独立 try，单路失败不再带走后续各路
                        UpdateSplashProgress(31, "正在加载作业 (12/12)...");

                    }
                }
                catch (Exception exBind)
                {
                    // ★F5 修复（2026-09-20）：原实现只记"无流程"——无法知道是哪一路/什么异常导致
                    //   12 路作业绑定中断（其后各路被整段跳过，且现场看不出）。补异常详情便于定位。
                    _logger.WriteLog("启动作业绑定中断（其后各相机路未绑定）: " + exBind.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex.Message + "111");
            };
            UpdateSplashProgress(35, "正在初始化UI配置...");
            // ★ 安全网：无论方案是否加载成功，都投递 initialize_FormSet，同时设置超时兜底
            initialize_form initialize_form1 = new initialize_form(initialize_FormSet);
            SafeBeginInvoke(initialize_form1);
            try
            {
                int waitMs = 0;
                while (qiehuanzhong == 1 && waitMs < 10000)
                {
                    Thread.Sleep(100);
                    waitMs += 100;
                }
                // ★ 兜底：如果 UI 线程 10 秒内没处理 initialize_FormSet，直接解除阻塞
                if (qiehuanzhong == 1)
                {
                    _logger.WriteLog("initialize_FormSet 超时未执行，兜底解除阻塞");
                    Frm2.start = 1;
                    qiehuanzhong = 0;
                }
                UpdateSplashProgress(40, "正在配置相机触发...");
                trriger_set();
                Thread.Sleep(50);
                Thread ui = new Thread(new ThreadStart(UI_monitor));
                ui.IsBackground = true;
                ui.Start();
                // comboBox8_SelectedIndexChanged(null, null);
                // ★修复（2026-09-20）：同 988——本方法在后台线程且 Debug 开启跨线程校验，
                //   直接设置控件属性会抛"线程间操作无效"，抛出后将跳过本 try 剩余步骤（打开相机等）。改线程安全投递。
                try { SafeBeginInvoke(new Action(() => { try { bnClose.Enabled = true; } catch { } })); } catch { }
                UpdateSplashProgress(45, "正在打开相机...");
            }

            catch (Exception ex)
            {
                _logger.WriteLog(ex.Message + "222");
            };
            // ★F5 修复（2026-09-20）：启动侧补"方案绑定诊断"日志（与切型侧同款）——
            //   原启动侧无绑定诊断，出现"Form6 空白/Form9 崩溃/运行结果缺组"时无法从日志判断
            //   是 JobCount 不足还是某路 block 绑定失败。此处位于方法末（initialize_FormSet 已执行完
            //   或已超时兜底），无条件记录一次。
            try
            {
                int _diagJc0 = (manager1 != null) ? manager1.JobCount : -1;
                string _diagBind0 = "";
                for (int _bi0 = 0; _bi0 < 12; _bi0++)
                {
                    bool _okBind0 = _bi0 < _diagJc0 && _jobs.Myjobs[_bi0] != null
                        && _jobs.Myjobs[_bi0].job != null && _jobs.Myjobs[_bi0].block != null;
                    _diagBind0 += "相机" + (_bi0 + 1) + "=" + (_okBind0 ? "OK" : "null") + (_bi0 == 11 ? "" : " ");
                }
                _logger.WriteLog("启动方案绑定诊断: JobCount=" + _diagJc0 + " | block: " + _diagBind0);
            }
            catch { }
        }
        private void initialize_FormSet()
        {
            // ★ 无方案文件时跳过所有依赖 manager1 的初始化，让用户进配置窗加载方案
            if (manager1 == null || managerState == ManagerState.Failed)
            {
                this.WindowState = FormWindowState.Maximized;
                label133.Text = "未加载到方案文件，请在【配置窗】中加载方案后重新打开相机";
                _logger.WriteLog("方案未加载成功，跳过流程初始化，等待用户手动加载方案");
                // 必须解除阻塞：qiehuanzhong=0 解除后台线程等待，Frm2.start=1 关闭启动画面
                Frm2.start = 1;
                qiehuanzhong = 0;
                return;
            }
            string pppp = "";
            try
            {
                try
                {
                    UpdateSplashProgress(50, "正在初始化产品流程1...");
                    pppp = _config.ReadString("camera1", "fen", pppp).Replace("\0", "");
                    if (pppp.Contains("vpp"))
                    {
                        try
                        {
                            _jobs.myjob1.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\1\\" + pppp);
                            comboBox7.Text = pppp;
                        }
                        catch (Exception ex)
                        {
                            group_1 = _jobs.myjob1.job.VisionTool as CogToolGroup;
                            block_1 = group_1.Tools["CogToolBlock1"] as CogToolBlock;
                            _jobs.myjob1.block = block_1.Tools["CogToolBlock1"] as CogToolBlock;
                            _logger.WriteLog("分流程" + ex.Message);
                        }
                    }

                    else
                    {
                        group_1 = _jobs.myjob1.job.VisionTool as CogToolGroup;
                        block_1 = group_1.Tools["CogToolBlock1"] as CogToolBlock;
                        _jobs.myjob1.block = block_1.Tools["CogToolBlock1"] as CogToolBlock;
                    }
                    try
                    {
                        string aatemp = _jobs.myjob1.block.Outputs["tishi"].Value.ToString();
                        _jobs.myjob1.tishi = true;
                    }
                    catch
                    {
                        _jobs.myjob1.tishi = false;
                    }
                    if ((_jobs.myjob1.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                        _jobs.myjob1.Color = false;
                    else
                    {
                        _jobs.myjob1.Color = true;
                    }
                    _jobs.myjob1.output3 = "空";
                    for (int i = 0; i < _jobs.myjob1.block.Outputs.Count; i++)
                    {
                        if (_jobs.myjob1.block.Outputs[i].Name.Contains("ji"))
                        {
                            _jobs.myjob1.biaotou += _jobs.myjob1.block.Outputs[i].Name + ",";
                        }
                        if (_jobs.myjob1.block.Outputs[i].Name.Contains("buchang"))
                        {
                            _jobs.myjob1.zidongbaoguang = true;
                        }
                        if (_jobs.myjob1.block.Outputs[i].Name.Contains("Output3"))
                        {
                            _jobs.myjob1.output3 = "有";
                        }
                    }
                    _statistics.CreateDirectoryCsvPath(_jobs.myjob1.path_number);
                    //if(_jobs.myjob1.biaotou.Contains(","))
                    _statistics.CreateCsvPath(_jobs.myjob1.path_number, _jobs.myjob1.biaotou);
                }
                catch
                {
                    _logger.WriteLog("流程1初始化失败");
                }
                UpdateSplashProgress(55, "正在初始化产品流程2-8...");
                //td1 = new Thread(new ThreadStart(getrecord_1));
                //td1.Start();
                timewatch2 = new Stopwatch();
                timewatch1 = new Stopwatch();
                listBox2.Items.Add("产品类型:相机1");
                listBox2.Items.Add("检测数:");
                listBox2.Items.Add("OK数:");
                listBox2.Items.Add("NG数:");
                listBox2.Items.Add("合格率:");
                listBox2.Items.Add("~~~~~~~~~");
                listBox1.Items.Add("record");
                listBox1.Items.Add("record");
                listBox1.Items.Add("record");
                listBox1.Items.Add("record");
                listBox1.Items.Add("record");
                listBox3.Items.Add("record");
                listBox3.Items.Add("record");
                listBox3.Items.Add("record");
                listBox3.Items.Add("record");
                listBox3.Items.Add("record");
                listBox7.Items.Add("record");
                listBox7.Items.Add("record");
                listBox7.Items.Add("record");
                listBox7.Items.Add("record");
                listBox7.Items.Add("record");
                listBox6.Items.Add("record");
                listBox6.Items.Add("record");
                listBox6.Items.Add("record");
                listBox6.Items.Add("record");
                listBox6.Items.Add("record");
                listBox14.Items.Add("record");
                listBox14.Items.Add("record");
                listBox14.Items.Add("record");
                listBox14.Items.Add("record");
                listBox14.Items.Add("record");
                listBox15.Items.Add("record");
                listBox15.Items.Add("record");
                listBox15.Items.Add("record");
                listBox15.Items.Add("record");
                listBox15.Items.Add("record");
                listBox18.Items.Add("record");
                listBox18.Items.Add("record");
                listBox18.Items.Add("record");
                listBox18.Items.Add("record");
                listBox18.Items.Add("record");
                listBox17.Items.Add("record");
                listBox17.Items.Add("record");
                listBox17.Items.Add("record");
                listBox17.Items.Add("record");
                listBox17.Items.Add("record");
                listBox16.Items.Add("record");
                listBox16.Items.Add("record");
                listBox16.Items.Add("record");
                listBox16.Items.Add("record");
                listBox16.Items.Add("record");
                listBox23.Items.Add("record");
                listBox23.Items.Add("record");
                listBox23.Items.Add("record");
                listBox23.Items.Add("record");
                listBox23.Items.Add("record");
                listBox24.Items.Add("record");
                listBox24.Items.Add("record");
                listBox24.Items.Add("record");
                listBox24.Items.Add("record");
                listBox24.Items.Add("record");
                listBox25.Items.Add("record");
                listBox25.Items.Add("record");
                listBox25.Items.Add("record");
                listBox25.Items.Add("record");
                listBox25.Items.Add("record");
                dataGridView1.ReadOnly = true;
                dataGridView1.AllowUserToAddRows = false;
                // dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                try
                {
                    if (manager1.JobCount > 1)
                    {
                        listBox2.Items.Add("产品类型:相机2");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera2", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob2.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\2\\" + pppp);
                                comboBox9.Text = pppp;
                            }
                            catch (Exception ex)
                            {
                                group_2 = _jobs.myjob2.job.VisionTool as CogToolGroup;
                                block_2 = group_2.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob2.block = block_2.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_2 = _jobs.myjob2.job.VisionTool as CogToolGroup;
                            block_2 = group_2.Tools["CogToolBlock1"] as CogToolBlock;
                            _jobs.myjob2.block = block_2.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob2.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob2.Color = false;
                        else
                            _jobs.myjob2.Color = true;
                        //  _jobs.myjob2.CogFifo = block_2.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                        //td2 = new Thread(new ThreadStart(getrecord_2));
                        // td2.Start();
                        dataGridView2.ReadOnly = true;
                        dataGridView2.AllowUserToAddRows = false;
                        //  dataGridView2.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                        try
                        {
                            string aatemp = _jobs.myjob2.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob2.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob2.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob2.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob2.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob2.biaotou += _jobs.myjob2.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob2.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob2.zidongbaoguang = true;
                            }
                            if (_jobs.myjob2.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob2.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob2.path_number);
                        //if (_jobs.myjob2.biaotou.Contains(","))
                        _statistics.CreateCsvPath(_jobs.myjob2.path_number, _jobs.myjob2.biaotou);
                    }
                    if (manager1.JobCount > 2)
                    {
                        listBox2.Items.Add("产品类型:相机3");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera3", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob3.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\3\\" + pppp);
                                comboBox10.Text = pppp;
                            }
                            catch (Exception ex)
                            {
                                group_3 = _jobs.myjob3.job.VisionTool as CogToolGroup;
                                block_3 = group_3.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob3.block = block_3.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_3 = _jobs.myjob3.job.VisionTool as CogToolGroup;
                            block_3 = group_3.Tools["CogToolBlock1"] as CogToolBlock;
                            //  _jobs.myjob3.CogFifo = block_3.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                            _jobs.myjob3.block = block_3.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob3.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob3.Color = false;
                        else
                            _jobs.myjob3.Color = true;
                        //td3 = new Thread(new ThreadStart(getrecord_3));
                        // td3.Start();
                        try
                        {
                            string aatemp = _jobs.myjob3.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob3.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob3.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob3.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob3.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob3.biaotou += _jobs.myjob3.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob3.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob3.zidongbaoguang = true;
                            }
                            if (_jobs.myjob3.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob3.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob3.path_number);
                        //if (_jobs.myjob3.biaotou.Contains(","))
                        _statistics.CreateCsvPath(_jobs.myjob3.path_number, _jobs.myjob3.biaotou);
                    }
                    if (manager1.JobCount > 3)
                    {
                        listBox2.Items.Add("产品类型:相机4");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera4", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob4.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\4\\" + pppp);
                                comboBox11.Text = pppp;
                            }
                            catch (Exception ex)
                            {
                                group_4 = _jobs.myjob4.job.VisionTool as CogToolGroup;
                                block_4 = group_4.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob4.block = block_4.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_4 = _jobs.myjob4.job.VisionTool as CogToolGroup;
                            block_4 = group_4.Tools["CogToolBlock1"] as CogToolBlock;

                            // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                            _jobs.myjob4.block = block_4.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob4.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob4.Color = false;
                        else
                            _jobs.myjob4.Color = true;
                        // td4 = new Thread(new ThreadStart(getrecord_4));
                        // td4.Start();
                        try
                        {
                            string aatemp = _jobs.myjob4.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob4.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob4.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob4.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob4.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob4.biaotou += _jobs.myjob4.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob4.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob4.zidongbaoguang = true;
                            }
                            if (_jobs.myjob4.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob4.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob4.path_number);
                        // if (_jobs.myjob4.biaotou.Contains(","))
                        _statistics.CreateCsvPath(_jobs.myjob4.path_number, _jobs.myjob4.biaotou);
                    }
                    if (manager1.JobCount > 4)
                    {
                        listBox2.Items.Add("产品类型:相机5");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera5", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob5.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\5\\" + pppp);
                                comboBox12.Text = pppp;
                            }
                            catch (Exception ex)
                            {
                                group_5 = _jobs.myjob5.job.VisionTool as CogToolGroup;
                                block_5 = group_5.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob5.block = block_5.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }

                        }
                        else
                        {
                            group_5 = _jobs.myjob5.job.VisionTool as CogToolGroup;
                            block_5 = group_5.Tools["CogToolBlock1"] as CogToolBlock;

                            // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                            _jobs.myjob5.block = block_5.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob5.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob5.Color = false;
                        else
                            _jobs.myjob5.Color = true;
                        // td5 = new Thread(new ThreadStart(getrecord_5));
                        //  td5.Start();
                        try
                        {
                            string aatemp = _jobs.myjob5.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob5.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob5.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob5.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob5.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob5.biaotou += _jobs.myjob5.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob5.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob5.zidongbaoguang = true;
                            }
                            if (_jobs.myjob5.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob5.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob5.path_number);
                        //   if (_jobs.myjob5.biaotou.Contains(","))
                        _statistics.CreateCsvPath(_jobs.myjob5.path_number, _jobs.myjob5.biaotou);
                    }
                    if (manager1.JobCount > 5)
                    {
                        listBox2.Items.Add("产品类型:相机6");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera6", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob6.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\6\\" + pppp);
                                comboBox13.Text = pppp;
                            }
                            catch (Exception ex)
                            {
                                group_6 = _jobs.myjob6.job.VisionTool as CogToolGroup;
                                block_6 = group_6.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob6.block = block_6.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_6 = _jobs.myjob6.job.VisionTool as CogToolGroup;
                            block_6 = group_6.Tools["CogToolBlock1"] as CogToolBlock;

                            // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                            _jobs.myjob6.block = block_6.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob6.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob6.Color = false;
                        else
                            _jobs.myjob6.Color = true;
                        // td6 = new Thread(new ThreadStart(getrecord_6));
                        // td6.Start();
                        try
                        {
                            string aatemp = _jobs.myjob6.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob6.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob6.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob6.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob6.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob6.biaotou += _jobs.myjob6.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob6.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob6.zidongbaoguang = true;
                            }
                            if (_jobs.myjob6.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob6.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob6.path_number);
                        // if (_jobs.myjob6.biaotou.Contains(","))
                        _statistics.CreateCsvPath(_jobs.myjob6.path_number, _jobs.myjob6.biaotou);
                    }
                    if (manager1.JobCount > 6)
                    {
                        listBox2.Items.Add("产品类型:相机7");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera7", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob7.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\7\\" + pppp);
                                comboBox14.Text = pppp;
                            }
                            catch (Exception ex)
                            {
                                group_7 = _jobs.myjob7.job.VisionTool as CogToolGroup;
                                block_7 = group_7.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob7.block = block_7.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_7 = _jobs.myjob7.job.VisionTool as CogToolGroup;
                            block_7 = group_7.Tools["CogToolBlock1"] as CogToolBlock;

                            // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                            _jobs.myjob7.block = block_7.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob7.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob7.Color = false;
                        else
                            _jobs.myjob7.Color = true;
                        //  td7 = new Thread(new ThreadStart(getrecord_7));
                        //  td7.Start();
                        try
                        {
                            string aatemp = _jobs.myjob7.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob7.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob7.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob7.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob7.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob7.biaotou += _jobs.myjob7.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob7.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob7.zidongbaoguang = true;
                            }
                            if (_jobs.myjob7.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob7.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob7.path_number);
                        //  if (_jobs.myjob7.biaotou.Contains(","))
                        _statistics.CreateCsvPath(_jobs.myjob7.path_number, _jobs.myjob7.biaotou);
                    }
                    if (manager1.JobCount > 7)
                    {
                        listBox2.Items.Add("产品类型:相机8");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera8", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob8.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\8\\" + pppp);
                                comboBox15.Text = pppp;
                            }
                            catch (Exception ex)
                            {
                                group_8 = _jobs.myjob8.job.VisionTool as CogToolGroup;
                                block_8 = group_8.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob8.block = block_8.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_8 = _jobs.myjob8.job.VisionTool as CogToolGroup;
                            block_8 = group_8.Tools["CogToolBlock1"] as CogToolBlock;

                            // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                            _jobs.myjob8.block = block_8.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob8.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob8.Color = false;
                        else
                            _jobs.myjob8.Color = true;
                        // td8 = new Thread(new ThreadStart(getrecord_8));
                        // td8.Start();
                        try
                        {
                            string aatemp = _jobs.myjob8.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob8.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob8.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob8.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob8.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob8.biaotou += _jobs.myjob8.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob8.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob8.zidongbaoguang = true;
                            }
                            if (_jobs.myjob8.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob8.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob8.path_number);
                        // if (_jobs.myjob8.biaotou.Contains(","))
                        _statistics.CreateCsvPath(_jobs.myjob8.path_number, _jobs.myjob8.biaotou);
                    }
                    if (manager1.JobCount > 8)
                    {
                        listBox2.Items.Add("产品类型:相机9");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera9", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob9.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\9\\" + pppp);
                            }
                            catch (Exception ex)
                            {
                                group_9 = _jobs.myjob9.job.VisionTool as CogToolGroup;
                                block_9 = group_9.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob9.block = block_9.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_9 = _jobs.myjob9.job.VisionTool as CogToolGroup;
                            block_9 = group_9.Tools["CogToolBlock1"] as CogToolBlock;
                            _jobs.myjob9.block = block_9.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob9.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob9.Color = false;
                        else
                            _jobs.myjob9.Color = true;
                        try
                        {
                            string aatemp = _jobs.myjob9.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob9.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob9.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob9.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob9.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob9.biaotou += _jobs.myjob9.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob9.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob9.zidongbaoguang = true;
                            }
                            if (_jobs.myjob9.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob9.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob9.path_number);
                        _statistics.CreateCsvPath(_jobs.myjob9.path_number, _jobs.myjob9.biaotou);
                    }
                    if (manager1.JobCount > 9)
                    {
                        listBox2.Items.Add("产品类型:相机10");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera10", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob10.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\10\\" + pppp);
                            }
                            catch (Exception ex)
                            {
                                group_10 = _jobs.myjob10.job.VisionTool as CogToolGroup;
                                block_10 = group_10.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob10.block = block_10.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_10 = _jobs.myjob10.job.VisionTool as CogToolGroup;
                            block_10 = group_10.Tools["CogToolBlock1"] as CogToolBlock;
                            _jobs.myjob10.block = block_10.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob10.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob10.Color = false;
                        else
                            _jobs.myjob10.Color = true;
                        try
                        {
                            string aatemp = _jobs.myjob10.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob10.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob10.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob10.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob10.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob10.biaotou += _jobs.myjob10.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob10.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob10.zidongbaoguang = true;
                            }
                            if (_jobs.myjob10.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob10.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob10.path_number);
                        _statistics.CreateCsvPath(_jobs.myjob10.path_number, _jobs.myjob10.biaotou);
                    }
                    if (manager1.JobCount > 10)
                    {
                        listBox2.Items.Add("产品类型:相机11");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera11", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob11.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\11\\" + pppp);
                            }
                            catch (Exception ex)
                            {
                                group_11 = _jobs.myjob11.job.VisionTool as CogToolGroup;
                                block_11 = group_11.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob11.block = block_11.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_11 = _jobs.myjob11.job.VisionTool as CogToolGroup;
                            block_11 = group_11.Tools["CogToolBlock1"] as CogToolBlock;
                            _jobs.myjob11.block = block_11.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob11.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob11.Color = false;
                        else
                            _jobs.myjob11.Color = true;
                        try
                        {
                            string aatemp = _jobs.myjob11.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob11.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob11.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob11.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob11.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob11.biaotou += _jobs.myjob11.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob11.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob11.zidongbaoguang = true;
                            }
                            if (_jobs.myjob11.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob11.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob11.path_number);
                        _statistics.CreateCsvPath(_jobs.myjob11.path_number, _jobs.myjob11.biaotou);
                    }
                    if (manager1.JobCount > 11)
                    {
                        listBox2.Items.Add("产品类型:相机12");
                        listBox2.Items.Add("检测数:");
                        listBox2.Items.Add("OK数:");
                        listBox2.Items.Add("NG数:");
                        listBox2.Items.Add("合格率:");
                        listBox2.Items.Add("~~~~~~~~~");
                        pppp = "";
                        pppp = _config.ReadString("camera12", "fen", pppp).Replace("\0", "");
                        if (pppp.Contains("vpp"))
                        {
                            try
                            {
                                _jobs.myjob12.block = (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\12\\" + pppp);
                            }
                            catch (Exception ex)
                            {
                                group_12 = _jobs.myjob12.job.VisionTool as CogToolGroup;
                                block_12 = group_12.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob12.block = block_12.Tools["CogToolBlock1"] as CogToolBlock;
                                _logger.WriteLog("分流程" + ex.Message);
                            }
                        }
                        else
                        {
                            group_12 = _jobs.myjob12.job.VisionTool as CogToolGroup;
                            block_12 = group_12.Tools["CogToolBlock1"] as CogToolBlock;
                            _jobs.myjob12.block = block_12.Tools["CogToolBlock1"] as CogToolBlock;
                        }
                        if ((_jobs.myjob12.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                            _jobs.myjob12.Color = false;
                        else
                            _jobs.myjob12.Color = true;
                        try
                        {
                            string aatemp = _jobs.myjob12.block.Outputs["tishi"].Value.ToString();
                            _jobs.myjob12.tishi = true;
                        }
                        catch
                        {
                            _jobs.myjob12.tishi = false;
                        }
                        for (int i = 0; i < _jobs.myjob12.block.Outputs.Count; i++)
                        {
                            if (_jobs.myjob12.block.Outputs[i].Name.Contains("ji"))
                            {
                                _jobs.myjob12.biaotou += _jobs.myjob12.block.Outputs[i].Name + ",";
                            }
                            if (_jobs.myjob12.block.Outputs[i].Name.Contains("buchang"))
                            {
                                _jobs.myjob12.zidongbaoguang = true;
                            }
                            if (_jobs.myjob12.block.Outputs[i].Name.Contains("Output3"))
                            {
                                _jobs.myjob12.output3 = "有";
                            }
                        }
                        _statistics.CreateDirectoryCsvPath(_jobs.myjob12.path_number);
                        _statistics.CreateCsvPath(_jobs.myjob12.path_number, _jobs.myjob12.biaotou);
                    }
                }
                catch
                {
                    _logger.WriteLog("无流程2");
                }
                decimal devalue;
                decimal.TryParse(_config.ReadString("camera", "yanshi", "5"), out devalue);
                numericUpDown1.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name1", "1"), out devalue);
                camera_name[0] = devalue.ToString();
                numericUpDown9.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name2", "2"), out devalue);
                camera_name[1] = devalue.ToString();
                numericUpDown10.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name3", "3"), out devalue);
                camera_name[2] = devalue.ToString();
                numericUpDown11.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name4", "4"), out devalue);
                camera_name[3] = devalue.ToString();
                numericUpDown12.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name5", "5"), out devalue);
                camera_name[4] = devalue.ToString();
                numericUpDown13.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name6", "6"), out devalue);
                camera_name[5] = devalue.ToString();
                numericUpDown14.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name7", "7"), out devalue);
                camera_name[6] = devalue.ToString();
                numericUpDown15.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name8", "8"), out devalue);
                camera_name[7] = devalue.ToString();
                numericUpDown16.Value = devalue;

                decimal.TryParse(_config.ReadString("camera", "name9", "9"), out devalue);
                camera_name[8] = devalue.ToString();

                decimal.TryParse(_config.ReadString("camera", "name10", "10"), out devalue);
                camera_name[9] = devalue.ToString();

                decimal.TryParse(_config.ReadString("camera", "name11", "11"), out devalue);
                camera_name[10] = devalue.ToString();

                decimal.TryParse(_config.ReadString("camera", "name12", "12"), out devalue);
                camera_name[11] = devalue.ToString();


                UpdateSplashProgress(90, "正在读取相机参数...");
                Thread.Sleep(10);
                Frm2.start = 1;
                this.WindowState = FormWindowState.Maximized;
                checkedListBox1.Enabled = false;
                //CogFrameGrabberGigEs mf2 = new CogFrameGrabberGigEs();//获取已连接相机列表
                //if (mf2.Count == 0)
                //    MessageBox.Show("没有连接到相机！");
                Thread zhenlv = new Thread(new ThreadStart(zhenlv_1));
                zhenlv.IsBackground = true;
                zhenlv.Start();
                // ★ 2026-09-06 ④：授权过期时禁止自动打开相机（2037 门控）
                if (!_authExpired)
                    bnOpen_Click(null, null);
                else
                    _logger.WriteLog("启动流程: 授权未通过，跳过自动开相机");
                display();
                try
                {
                    button1_Click(null, null);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("启动运行失败:" + ex.Message);
                }
                textBox19.Text = _config.ReadString("zhendongpan", "path", "").Replace("\0", "");
                if (_config.ReadString("camera", "cuntu", "存图限制").Replace("\0", "") == "存图限制")
                {
                    comboBox21.SelectedIndex = 0;
                    cuntu = 0;
                }
                else
                {
                    comboBox21.SelectedIndex = 1;
                    cuntu = 1;
                }
                if (_config.ReadString("camera", "tongji", "统计限制").Replace("\0", "") == "统计限制")
                {
                    comboBox22.SelectedIndex = 0;
                    tongji = 0;
                }
                else
                {
                    comboBox22.SelectedIndex = 1;
                    tongji = 1;
                }
                // Replace("\0", "")
                if (_config.ReadString("camera", "qufan", "IO未反转").Replace("\0", "") == "IO未反转")
                {
                    shuchuqufan = "Accept";
                }
                else
                {
                    button31.Text = "IO已反转";
                    shuchuqufan = "Reject";
                }
                // this.dataGridView1.DataSource = _jobs.myjob1.myTable;//将List的数据绑定到DataGridView中
                // this.dataGridView2.DataSource = _jobs.myjob2.myTable;//将List的数据绑定到DataGridView中
                if (_config.ReadString("camera1", "biaoge", "false") == "true")
                {

                    checkBox11.CheckState = CheckState.Checked;
                }
                else
                {
                    dataGridView1.ReadOnly = false;
                    dataGridView1.DataSource = _jobs.myjob1.myTable1;
                    checkBox11.CheckState = CheckState.Unchecked;
                }
                if (_config.ReadString("camera2", "biaoge", "false") == "true")
                    checkBox8.CheckState = CheckState.Checked;
                else
                {
                    dataGridView2.ReadOnly = false;
                    checkBox8.CheckState = CheckState.Unchecked;
                    dataGridView2.DataSource = _jobs.myjob2.myTable1;
                }

                if (_config.ReadString("camera3", "biaoge", "false") == "true")
                    checkBox56.CheckState = CheckState.Checked;
                else
                    checkBox56.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera4", "biaoge", "false") == "true")
                    checkBox57.CheckState = CheckState.Checked;
                else
                    checkBox57.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera5", "biaoge", "false") == "true")
                    checkBox58.CheckState = CheckState.Checked;
                else
                    checkBox58.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera6", "biaoge", "false") == "true")
                    checkBox59.CheckState = CheckState.Checked;
                else
                    checkBox59.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera7", "biaoge", "false") == "true")
                    checkBox60.CheckState = CheckState.Checked;
                else
                    checkBox60.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera8", "biaoge", "false") == "true")
                    checkBox61.CheckState = CheckState.Checked;
                else
                    checkBox61.CheckState = CheckState.Unchecked;

                if (_config.ReadString("camera1", "shijianEn", "false") == "true")
                    checkBox3.CheckState = CheckState.Checked;
                else
                    checkBox3.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera2", "shijianEn", "false") == "true")
                    checkBox13.CheckState = CheckState.Checked;
                else
                    checkBox13.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera3", "shijianEn", "false") == "true")
                    checkBox18.CheckState = CheckState.Checked;
                else
                    checkBox18.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera4", "shijianEn", "false") == "true")
                    checkBox22.CheckState = CheckState.Checked;
                else
                    checkBox22.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera5", "shijianEn", "false") == "true")
                    checkBox36.CheckState = CheckState.Checked;
                else
                    checkBox36.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera6", "shijianEn", "false") == "true")
                    checkBox42.CheckState = CheckState.Checked;
                else
                    checkBox42.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera7", "shijianEn", "false") == "true")
                    checkBox48.CheckState = CheckState.Checked;
                else
                    checkBox48.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera8", "shijianEn", "false") == "true")
                    checkBox54.CheckState = CheckState.Checked;
                else
                    checkBox54.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera", "NG", "false") == "true")
                    checkBox71.CheckState = CheckState.Checked;
                else
                    checkBox71.CheckState = CheckState.Unchecked;

                if (_config.ReadString("camera", "NG", "false") == "true")
                    checkBox7.CheckState = CheckState.Checked;
                else
                    checkBox7.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera2", "chatu", "false") == "true")
                    checkBox6.CheckState = CheckState.Checked;
                else
                    checkBox6.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera3", "chatu", "false") == "true")
                    checkBox14.CheckState = CheckState.Checked;
                else
                    checkBox14.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera4", "chatu", "false") == "true")
                    checkBox12.CheckState = CheckState.Checked;
                else
                    checkBox12.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera5", "chatu", "false") == "true")
                    checkBox35.CheckState = CheckState.Checked;
                else
                    checkBox35.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera6", "chatu", "false") == "true")
                    checkBox41.CheckState = CheckState.Checked;
                else
                    checkBox41.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera7", "chatu", "false") == "true")
                    checkBox55.CheckState = CheckState.Checked;
                else
                    checkBox55.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera8", "chatu", "false") == "true")
                    checkBox53.CheckState = CheckState.Checked;
                else
                    checkBox53.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera9", "chatu", "false") == "true")
                    checkBox73.CheckState = CheckState.Checked;
                else
                    checkBox73.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera10", "chatu", "false") == "true")
                    checkBox79.CheckState = CheckState.Checked;
                else
                    checkBox79.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera11", "chatu", "false") == "true")
                    checkBox85.CheckState = CheckState.Checked;
                else
                    checkBox85.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera12", "chatu", "false") == "true")
                    checkBox91.CheckState = CheckState.Checked;
                else
                    checkBox91.CheckState = CheckState.Unchecked;
                tbExposure1.Text = _config.ReadString("camera1", "exposure", "1000");
                tbGain1.Text = _config.ReadString("camera1", "gain", "1");
                tbFrameRate1.Text = _config.ReadString("camera1", "rate", "500");
                if (_config.ReadString("camera1", "triggeren", "true") == "true")
                    checkBox5.CheckState = CheckState.Checked;
                else
                    checkBox5.CheckState = CheckState.Unchecked;


                if (_config.ReadString("camera1", "1", "false") == "true")
                    checkBox1.CheckState = CheckState.Checked;
                else
                    checkBox1.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera1", "2", "false") == "true")
                    checkBox2.CheckState = CheckState.Checked;
                else
                    checkBox2.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera", "datajilu", "false") == "true")
                    checkBox25.CheckState = CheckState.Checked;
                else
                {
                    checkBox25.CheckState = CheckState.Unchecked;
                    checkBox25_CheckedChanged(null, null);
                }
                //if (_config.ReadString("camera", "gongjujilu", "false") == "true")
                //    checkBox70.CheckState = CheckState.Checked;
                //else
                //{
                //    checkBox70.CheckState = CheckState.Unchecked;
                //    checkBox70_CheckedChanged(null, null);
                //}
                textBox3.Text = _config.ReadString("camera1", "outtime", "100");
                if (_config.ReadString("camera1", "serial", "true") == "true")
                    checkBox27.CheckState = CheckState.Checked;
                else
                    checkBox27.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera2", "serial", "true") == "true")
                    checkBox28.CheckState = CheckState.Checked;
                else
                    checkBox28.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera3", "serial", "true") == "true")
                    checkBox29.CheckState = CheckState.Checked;
                else
                    checkBox29.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera4", "serial", "true") == "true")
                    checkBox30.CheckState = CheckState.Checked;
                else
                    checkBox30.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera5", "serial", "true") == "true")
                    checkBox31.CheckState = CheckState.Checked;
                else
                    checkBox31.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera6", "serial", "true") == "true")
                    checkBox37.CheckState = CheckState.Checked;
                else
                    checkBox37.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera7", "serial", "true") == "true")
                    checkBox43.CheckState = CheckState.Checked;
                else
                    checkBox43.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera8", "serial", "true") == "true")
                    checkBox49.CheckState = CheckState.Checked;
                else
                    checkBox49.CheckState = CheckState.Unchecked;
                tbExposure2.Text = _config.ReadString("camera2", "exposure", "1000");
                tbGain2.Text = _config.ReadString("camera2", "gain", "1");
                tbFrameRate2.Text = _config.ReadString("camera2", "rate", "500");
                if (_config.ReadString("camera2", "triggeren", "true") == "true")
                    checkBox9.CheckState = CheckState.Checked;
                else
                    checkBox9.CheckState = CheckState.Unchecked;


                tbExposure3.Text = _config.ReadString("camera3", "exposure", "1000");
                tbGain3.Text = _config.ReadString("camera3", "gain", "1");
                tbFrameRate3.Text = _config.ReadString("camera3", "rate", "500");
                if (_config.ReadString("camera3", "triggeren", "true") == "true")
                    checkBox15.CheckState = CheckState.Checked;
                else
                    checkBox15.CheckState = CheckState.Unchecked;

                tbExposure4.Text = _config.ReadString("camera4", "exposure", "1000");
                tbGain4.Text = _config.ReadString("camera4", "gain", "1");
                tbFrameRate4.Text = _config.ReadString("camera4", "rate", "500");
                if (_config.ReadString("camera4", "triggeren", "true") == "true")
                    checkBox19.CheckState = CheckState.Checked;
                else
                    checkBox19.CheckState = CheckState.Unchecked;


                tbExposure5.Text = _config.ReadString("camera5", "exposure", "1000");
                tbGain5.Text = _config.ReadString("camera5", "gain", "1");
                tbFrameRate5.Text = _config.ReadString("camera5", "rate", "500");
                if (_config.ReadString("camera5", "triggeren", "true") == "true")
                    checkBox33.CheckState = CheckState.Checked;
                else
                    checkBox33.CheckState = CheckState.Unchecked;


                tbExposure6.Text = _config.ReadString("camera6", "exposure", "1000");
                tbGain6.Text = _config.ReadString("camera6", "gain", "1");
                tbFrameRate6.Text = _config.ReadString("camera6", "rate", "500");
                if (_config.ReadString("camera6", "triggeren", "true") == "true")
                    checkBox39.CheckState = CheckState.Checked;
                else
                    checkBox39.CheckState = CheckState.Unchecked;



                tbExposure7.Text = _config.ReadString("camera7", "exposure", "1000");
                tbGain7.Text = _config.ReadString("camera7", "gain", "1");
                tbFrameRate7.Text = _config.ReadString("camera7", "rate", "500");
                if (_config.ReadString("camera7", "triggeren", "true") == "true")
                    checkBox45.CheckState = CheckState.Checked;
                else
                    checkBox45.CheckState = CheckState.Unchecked;


                tbExposure8.Text = _config.ReadString("camera8", "exposure", "1000");
                tbGain8.Text = _config.ReadString("camera8", "gain", "1");
                tbFrameRate8.Text = _config.ReadString("camera8", "rate", "500");
                if (_config.ReadString("camera8", "triggeren", "true") == "true")
                    checkBox51.CheckState = CheckState.Checked;
                else
                    checkBox51.CheckState = CheckState.Unchecked;

                if (_config.ReadString("camera9", "biaoge", "false") == "true")
                    checkBox73.CheckState = CheckState.Checked;
                else
                    checkBox73.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera10", "biaoge", "false") == "true")
                    checkBox79.CheckState = CheckState.Checked;
                else
                    checkBox79.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera11", "biaoge", "false") == "true")
                    checkBox85.CheckState = CheckState.Checked;
                else
                    checkBox85.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera12", "biaoge", "false") == "true")
                    checkBox91.CheckState = CheckState.Checked;
                else
                    checkBox91.CheckState = CheckState.Unchecked;

                if (_config.ReadString("camera9", "jiankong", "true") == "true")
                    checkBox47.CheckState = CheckState.Checked;
                else
                    checkBox47.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera10", "jiankong", "true") == "true")
                    checkBox96.CheckState = CheckState.Checked;
                else
                    checkBox96.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera11", "jiankong", "true") == "true")
                    checkBox97.CheckState = CheckState.Checked;
                else
                    checkBox97.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera12", "jiankong", "true") == "true")
                    checkBox98.CheckState = CheckState.Checked;
                else
                    checkBox98.CheckState = CheckState.Unchecked;

                if (_config.ReadString("camera9", "shijianEn", "false") == "true")
                    checkBox77.CheckState = CheckState.Checked;
                else
                    checkBox77.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera10", "shijianEn", "false") == "true")
                    checkBox83.CheckState = CheckState.Checked;
                else
                    checkBox83.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera11", "shijianEn", "false") == "true")
                    checkBox89.CheckState = CheckState.Checked;
                else
                    checkBox89.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera12", "shijianEn", "false") == "true")
                    checkBox95.CheckState = CheckState.Checked;
                else
                    checkBox95.CheckState = CheckState.Unchecked;

                if (_config.ReadString("camera9", "serial", "true") == "true")
                    checkBox74.CheckState = CheckState.Checked;
                else
                    checkBox74.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera10", "serial", "true") == "true")
                    checkBox80.CheckState = CheckState.Checked;
                else
                    checkBox80.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera11", "serial", "true") == "true")
                    checkBox86.CheckState = CheckState.Checked;
                else
                    checkBox86.CheckState = CheckState.Unchecked;
                if (_config.ReadString("camera12", "serial", "true") == "true")
                    checkBox92.CheckState = CheckState.Checked;
                else
                    checkBox92.CheckState = CheckState.Unchecked;

                tbExposure9.Text = _config.ReadString("camera9", "exposure", "1000");
                tbGain9.Text = _config.ReadString("camera9", "gain", "1");
                tbFrameRate9.Text = _config.ReadString("camera9", "rate", "500");
                if (_config.ReadString("camera9", "triggeren", "true") == "true")
                    checkBox75.CheckState = CheckState.Checked;
                else
                    checkBox75.CheckState = CheckState.Unchecked;

                tbExposure10.Text = _config.ReadString("camera10", "exposure", "1000");
                tbGain10.Text = _config.ReadString("camera10", "gain", "1");
                tbFrameRate10.Text = _config.ReadString("camera10", "rate", "500");
                if (_config.ReadString("camera10", "triggeren", "true") == "true")
                    checkBox81.CheckState = CheckState.Checked;
                else
                    checkBox81.CheckState = CheckState.Unchecked;

                tbExposure11.Text = _config.ReadString("camera11", "exposure", "1000");
                tbGain11.Text = _config.ReadString("camera11", "gain", "1");
                tbFrameRate11.Text = _config.ReadString("camera11", "rate", "500");
                if (_config.ReadString("camera11", "triggeren", "true") == "true")
                    checkBox87.CheckState = CheckState.Checked;
                else
                    checkBox87.CheckState = CheckState.Unchecked;

                tbExposure12.Text = _config.ReadString("camera12", "exposure", "1000");
                tbGain12.Text = _config.ReadString("camera12", "gain", "1");
                tbFrameRate12.Text = _config.ReadString("camera12", "rate", "500");
                if (_config.ReadString("camera12", "triggeren", "true") == "true")
                    checkBox93.CheckState = CheckState.Checked;
                else
                    checkBox93.CheckState = CheckState.Unchecked;

                decimal.TryParse(_config.ReadString("time", "feng", "100"), out devalue);
                numericUpDown22.Value = devalue;
                feng = double.Parse(devalue.ToString());

                decimal.TryParse(_config.ReadString("time", "IOyanshi", "0"), out devalue);
                numericUpDown5.Value = devalue;

                decimal.TryParse(_config.ReadString("cuntu", "zhangshu", "200"), out devalue);
                zhangshu = devalue;
                numericUpDown4.Value = devalue;



                //this.FormBorderStyle = FormBorderStyle.FixedSingle;
                decimal.TryParse(_config.ReadString("time", "NG", "1000"), out devalue);
                jiankongshijian = (double)devalue;
                numericUpDown2.Value = devalue;


                for (int _i = 0; _i < 8; _i++)
                {
                    _jobs.Myjobs[_i].myTable.Clear();
                    DataRow dr = _jobs.Myjobs[_i].myTable.NewRow();
                    _jobs.Myjobs[_i].myTable.Rows.Add(dr);
                    _jobs.Myjobs[_i].myTable.Columns.Add("种类", typeof(String));
                    _jobs.Myjobs[_i].myTable.Columns.Add("数量", typeof(String));
                }
                // this.dataGridView1.DataSource = _jobs.myjob1.myTable;//将List的数据绑定到DataGridView中
                // this.dataGridView2.DataSource = _jobs.myjob2.myTable;//将List的数据绑定到DataGridView中
                this.dataGridView3.DataSource = _jobs.myjob3.myTable;//将List的数据绑定到DataGridView中
                this.dataGridView4.DataSource = _jobs.myjob4.myTable;//将List的数据绑定到DataGridView中
                this.dataGridView5.DataSource = _jobs.myjob5.myTable;//将List的数据绑定到DataGridView中
                this.dataGridView6.DataSource = _jobs.myjob6.myTable;//将List的数据绑定到DataGridView中
                this.dataGridView7.DataSource = _jobs.myjob7.myTable;//将List的数据绑定到DataGridView中
                this.dataGridView8.DataSource = _jobs.myjob8.myTable;//将List的数据绑定到DataGridView中
                baoguang_set();
                bnSetParam_Click(null, null);
                bnGetParam_Click(null, null);// ch:获取参数 | en:Get parameters
                bnStartGrab1.Enabled = false;
                bnStopGrab1.Enabled = true;
                try
                {
                    if (manager1.JobCount > 1)
                    {
                        bnStartGrab2.Enabled = false;
                        bnStopGrab2.Enabled = true;
                        bnSetParam2_Click(null, null);
                        bnGetParam2_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 2)
                    {
                        bnStartGrab3.Enabled = false;
                        bnStopGrab3.Enabled = true;
                        bnSetParam3_Click(null, null);
                        bnGetParam3_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 3)
                    {
                        bnStartGrab4.Enabled = false;
                        bnStopGrab4.Enabled = true;
                        bnSetParam4_Click(null, null);
                        bnGetParam4_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 4)
                    {
                        bnStartGrab5.Enabled = false;
                        bnStopGrab5.Enabled = true;
                        bnSetParam5_Click(null, null);
                        bnGetParam5_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 5)
                    {
                        bnStartGrab6.Enabled = false;
                        bnStopGrab6.Enabled = true;
                        bnSetParam6_Click(null, null);
                        bnGetParam6_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 6)
                    {
                        bnStartGrab7.Enabled = false;
                        bnStopGrab7.Enabled = true;
                        bnSetParam7_Click(null, null);
                        bnGetParam7_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 7)
                    {
                        bnStartGrab8.Enabled = false;
                        bnStopGrab8.Enabled = true;
                        bnSetParam8_Click(null, null);
                        bnGetParam8_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 8)
                    {
                        bnStartGrab9.Enabled = false;
                        bnStopGrab9.Enabled = true;
                        bnSetParam9_Click(null, null);
                        bnGetParam9_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 9)
                    {
                        bnStartGrab10.Enabled = false;
                        bnStopGrab10.Enabled = true;
                        bnSetParam10_Click(null, null);
                        bnGetParam10_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 10)
                    {
                        bnStartGrab11.Enabled = false;
                        bnStopGrab11.Enabled = true;
                        bnSetParam11_Click(null, null);
                        bnGetParam11_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                    if (manager1.JobCount > 11)
                    {
                        bnStartGrab12.Enabled = false;
                        bnStopGrab12.Enabled = true;
                        bnSetParam12_Click(null, null);
                        bnGetParam12_Click(null, null);// ch:获取参数 | en:Get parameters
                    }
                }
                catch
                {
                    _logger.WriteLog("无流程3");
                }

                int geshu = int.Parse(_config.ReadString("canshu", "geshu", "0"));
                if (geshu > 0)
                {
                    for (int i = 0; i < geshu; i++)
                    {
                        comboBox38.Items.Add(_config.ReadString("canshu", (i + 1).ToString(), " "));
                    }
                    comboBox38.Text = _config.ReadString("canshu", "xuanze", " ");
                }
                qiehuanzhong = 0;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex.Message + "000");
                // Frm2.start = 1;
                Thread.Sleep(100);
                //if(ex.Message.Contains("未能找到文件"))
                //textBoxSolutionPath.Text = "无方案!!!!";
                Frm2.start = 1;
                qiehuanzhong = 0;


            };
        }
        Dictionary<string, string> dict1 = new Dictionary<string, string>();
        #endregion
        #region 删除存图
        // ★ 2026-09-11：存图保留天数（默认7），可在配置窗"存图限制"旁的可编辑控件设置，写回 ini [存图] baocun_tianshu。
        private int _saveImageKeepDays = 7;

        private void delete12()
        {
            // 使用Timer替代while+Sleep，避免永久阻塞线程池
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer((state) =>
            {
                try
                {
                    // ★ 存图子目录已按 yyyyMMdd 命名，按"目录名日期早于今天-N 天"清理，OK/NG 两侧 12 路统一处理。
                    int keepDays = _saveImageKeepDays;
                    for (int i = 0; i < 12; i++)
                    {
                        var m = _jobs.Myjobs[i];
                        if (m == null) continue;
                        CleanupOldDayFolders(m.pathhead_ok, keepDays);
                        CleanupOldDayFolders(m.pathhead_ng, keepDays);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message + "删除存图");
                }
            }, null, 100000, 100000);
        }

        /// <summary>清理指定存图根目录下、目录名日期早于"今天-keepDays天"的子目录。</summary>
        private void CleanupOldDayFolders(string basePath, int keepDays)
        {
            if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath)) return;
            try
            {
                DateTime today = DateTime.Today;
                foreach (string dir in Directory.GetDirectories(basePath))
                {
                    string name = Path.GetFileName(dir);
                    DateTime dirDate;
                    // 只处理 yyyyMMdd 日期目录，其它名字（非日期）不删，避免误删用户自建目录
                    if (DateTime.TryParseExact(name, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out dirDate))
                    {
                        if ((today - dirDate.Date).TotalDays > keepDays)
                        {
                            try { Directory.Delete(dir, true); }
                            catch (Exception ex) { _logger.WriteLog("删除旧存图失败: " + dir + " " + ex.Message); }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog("清理存图目录异常: " + basePath + " " + ex.Message);
            }
        }
        #endregion
        #region 帧率检测
        private decimal? _cachedNumericUpDown6Value;
        private void zhenlv_1()
        {
            while (!_disposingFlag)
            {
                int now = 0;
                int forword = _jobs.myjob1.sum;
                // 使用缓存值避免在后台线程直接读 UI 控件
                decimal sleepSeconds = _cachedNumericUpDown6Value ?? 1;
                Thread.Sleep(decimal.ToInt32(sleepSeconds) * 1000 - 5);
                if (_disposingFlag) break;
                now = _jobs.myjob1.sum;
                zhen = ((now - forword) * 1.00F / decimal.ToInt32(sleepSeconds)).ToString();
            }

        }
        #endregion
        private void jinduqiao()
        {
            // start1 = 0;

            Frm2.Show();
            Frm2.start = 0;
            while (!_disposingFlag)
                start1 = Frm2.end;

        }
        #region 创建设备列表
        private void DeviceListAcq()
        {
            // ch:创建设备列表 | en:Create Device List
            System.GC.Collect();
            cbDeviceList.Items.Clear();
            m_pDeviceList.nDeviceNum = 0;
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
            if (0 != nRet)
            {
                ShowErrorMsg("Enumerate devices fail!", 0);
                return;
            }
            m_nDevNum = (int)m_pDeviceList.nDeviceNum;
            tbDevNum.Text = m_nDevNum.ToString("d");
            tbUseNum.Text = m_nDevNum.ToString("d");
            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {

                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                    if (gigeInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("GEV: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("GEV: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("U3V: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                    }
                }
            }

            // ch:选择第一项 | en:Select the first item
            if (m_pDeviceList.nDeviceNum != 0)
            {
                cbDeviceList.SelectedIndex = 0;
            }


        }
        #endregion

        #region 启动流程与运行主控
        protected void getCode()
        {
            this.Invoke(new Action(() =>
            {
                pictureBox1.Image = QRCodeHelper.GetQRCodeBmp(identifier("Win32_DiskDrive", "Signature") + "M" + identifier("Win32_DiskDrive", "TotalHeads"));
            }));
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // ★ 交互优先（2026-09-19）：挂只读消息过滤器，记录最后鼠标/键盘活动时间，
            //   供 TryClaimRenderBudget 在有人操作时临时放宽渲染限速；恒返回 false 不吞消息。
            _userActivityFilter = new UserActivityFilter(this);
            Application.AddMessageFilter(_userActivityFilter);

            int dayt = 0;
            int dayz = 0;
            int authV1 = 0;   // ★ 2026-09-07：授权到期日期（用于统一计算 _authExpired，见下方）
            string zhongjian = "22";

            // ★ 读取加密码，带重试
            string code = _config.ReadString("code1", "code2", "");
            if (code == "")
            {
                Thread.Sleep(200);
                code = _config.ReadString("code1", "code2", "");
                if (code == "")
                {
                    _logger.WriteLog("启动解码: 未读到code2，code.ini可能损坏");
                }
            }

            // ★ 读取授权文件，带重试和目录创建
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    duini.ReadINIFile("C:\\Program Files\\test.ini");
                    break; // 成功
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("读取test.ini失败(第" + (attempt + 1) + "次): " + ex.Message);
                    if (attempt < 2)
                    {
                        // 尝试确保目录存在后重试
                        try
                        {
                            string dir = Path.GetDirectoryName("C:\\Program Files\\test.ini");
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        }
                        catch { }
                        Thread.Sleep(100);
                    }
                }
            }

            string code1 = duini.ReadString("1", "2", "");
            string code2_val = duini.ReadString("1", "3", "");
            string code3 = duini.ReadString("1", "4", "");
            textBox7.Text = duini.ReadString("1", "1", "");
            _logger.WriteLog("启动解码: test.ini读取完成");

            // ★ 过期警告检查，用 TryParse 替代 Parse 避免空串异常
            // 用 today 作为基准，而非 c3（c3 是上次运行日期，检查时尚未更新）
            Thread.Sleep(20);
            try
            {
                int c1 = 0;
                int today = (int)DateTime.Now.ToOADate();
                int.TryParse(code1, out c1);
                if (c1 > 0 && (c1 - today) <= 1)
                {
                    daoqi = "软件剩余时间" + Math.Max(0, c1 - today) + "天，请联系厂家!";
                    _logger.WriteLog("启动解码: 过期警告触发, 剩余" + Math.Max(0, c1 - today) + "天");
                }
                else
                    daoqi = "";
            }
            catch (Exception ex)
            {
                _logger.WriteLog("过期警告检查异常: " + ex.Message);
            }

            // ★ 解码核心逻辑
            if (code != "" && code.Length >= 7)
            {
                try
                {
                    // 安全提取到期日期偏移量
                    int offset = 0;
                    string last2 = code.Substring(code.Length - 2);
                    if (int.TryParse(last2, out offset) && offset >= 0 && offset + 5 <= code.Length)
                    {
                        string dateStr = code.Substring(offset, 5);
                        int parsedDate = 0;
                        if (int.TryParse(dateStr, out parsedDate))
                        {
                            dayt = parsedDate - ((int)DateTime.Now.ToOADate());
                        }
                        else
                        {
                            _logger.WriteLog("启动解码: code中日期段解析失败, offset=" + offset);
                            // 重试一次
                            code = _config.ReadString("code1", "code2", "");
                            if (code != "" && code.Length >= 7)
                            {
                                last2 = code.Substring(code.Length - 2);
                                if (int.TryParse(last2, out offset) && offset >= 0 && offset + 5 <= code.Length)
                                {
                                    int.TryParse(code.Substring(offset, 5), out parsedDate);
                                    dayt = parsedDate - ((int)DateTime.Now.ToOADate());
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.WriteLog("启动解码: code偏移量无效, codeLen=" + code.Length);
                        // 重试一次
                        code = _config.ReadString("code1", "code2", "");
                        if (code != "" && code.Length >= 7)
                        {
                            last2 = code.Substring(code.Length - 2);
                            if (int.TryParse(last2, out offset) && offset >= 0 && offset + 5 <= code.Length)
                            {
                                int parsedDate2 = 0;
                                int.TryParse(code.Substring(offset, 5), out parsedDate2);
                                dayt = parsedDate2 - ((int)DateTime.Now.ToOADate());
                            }
                        }
                    }

                    if (dayt >= 0 && code.Length >= 7)
                    {
                        int off = 0;
                        if (int.TryParse(code.Substring(code.Length - 2), out off) && off >= 0 && off + 5 <= code.Length)
                            zhongjian = code.Substring(off, 5);
                    }
                    else
                        zhongjian = DateTime.Now.ToOADate().ToString();

                    // ★ 解锁判断，用 TryParse 替代 Parse
                    int v1 = 0, v2 = 0, v3 = 0, now = (int)DateTime.Now.ToOADate();
                    int.TryParse(code1, out v1);
                    int.TryParse(code2_val, out v2);
                    int.TryParse(code3, out v3);
                    authV1 = v1;
                    if (v1 > now && v2 <= now && v3 <= now)
                    {
                        dayz = 1;
                        _logger.WriteLog("启动解码: 授权通过, dayz=1");
                    }
                    else
                    {
                        dayz = 0;
                        _logger.WriteLog("启动解码: 授权未通过, v1=" + v1 + " v2=" + v2 + " v3=" + v3 + " now=" + now);
                    }

                    // ★ 更新运行日期，确保下次启动仍能正常判断
                    if (v3 > 0 && v3 <= now)
                    {
                        try
                        {
                            duini.WriteString("1", "4", now.ToString());
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog("启动解码: 更新运行日期失败: " + ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("启动解码异常: " + ex.Message);
                }
            }
            else if (code != "" && code.Length < 7)
            {
                _logger.WriteLog("启动解码: code长度不足7位, 无法解码");
            }
            // ★ 2026-09-07：统一计算授权门控。
            //   必须限定「确实读到有效到期日期 authV1>0」才算过期 —— 否则 test.ini 读取失败或字段为空时
            //   v1=0 会被误判成过期，进而阻止自动开相机（改动前无论授权结果如何都会自动开）。
            //   授权数据不可读时保持"未过期"，仍由下方 dayz==0 的界面流程显示"加密中"并禁用按钮。
            _authExpired = (authV1 > 0 && dayz == 0);
            if (_authExpired)
                _logger.WriteLog("启动解码: 授权门控生效，将跳过自动开相机");
            try
            {
                day2 = GetCPUSerialnumber(zhongjian);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex.Message + ":对比");
            };
            if (dayz == 0)
            {
                checkedListBox1.SetItemChecked(0, false);
                checkedListBox1.SetItemChecked(1, false);
                checkedListBox1.SetItemChecked(2, false);
                checkedListBox1.SetItemChecked(3, false);
                checkedListBox1.SetItemChecked(4, false);
                checkedListBox1.SetItemChecked(5, false);
                checkedListBox1.SetItemChecked(6, false);
                checkedListBox1.SetItemChecked(7, false);
                checkedListBox1.SetItemChecked(8, false);
                checkedListBox1.SetItemChecked(9, false);
                checkedListBox1.SetItemChecked(10, false);
                checkedListBox1.SetItemChecked(11, false);
                button2.Enabled = false;
                button1.Enabled = false;
                label12.Text = "加密中";
                button5.Visible = true;
                label172.Visible = true;
                textBox7.Visible = true;
                textBox4.Visible = true;
                pictureBox1.Visible = true;
                tableLayoutPanel1.Visible = false;
                getCode();
            }
            else
            {
                checkedListBox1.SetItemChecked(0, true);
                checkedListBox1.SetItemChecked(1, true);
                checkedListBox1.SetItemChecked(2, true);
                checkedListBox1.SetItemChecked(3, true);
                checkedListBox1.SetItemChecked(4, true);
                checkedListBox1.SetItemChecked(5, true);
                checkedListBox1.SetItemChecked(6, true);
                checkedListBox1.SetItemChecked(7, true);
                checkedListBox1.SetItemChecked(8, true);
                checkedListBox1.SetItemChecked(9, true);
                checkedListBox1.SetItemChecked(10, true);
                checkedListBox1.SetItemChecked(11, true);
                button2.Enabled = true;
                button1.Enabled = true;
                Frm2.Show();
                Frm2.start = 0;
            }
            this.DesktopLocation = new Point(150, 150);

            label8.BringToFront();
            f1.getData += new SubSet.GetSeletionData(DataChangef1);
            f1.getData2 += new SubSet.GetSeletionData2(DataChangef2);
            f1.Show();
            f1.Visible = false;
            // —— 各协议“连接 1”启动加载（首次 Show 触发 Load：启动通道 / 自动连接），随后静默隐藏 ——
            // 连接 1 已收归“连接设备”管理器（Form1 旧入口全部改为打开管理器定位连接 1）：
            // 加载完成后立即 TopLevel=false 定型为“子窗体 + 隐藏”态。此后管理器嵌入连接 1 只是
            // 挂 Parent + Show，不再发生运行期 TopLevel 翻转（顶级→子级）引起的句柄重建；
            // 唯一一次降级成本被吸收在程序启动阶段。该 TopLevel=false 在进程内保持，不可再改回顶级。
            frm3.getData += new Form3.GetSeletionData(DataChange);
            frm3.Show();
            frm3.Visible = false;
            frm3.TopLevel = false;             // 无协议连接 1（Form3 主窗单例）
            _comm.Omron.getData += new FormOmron.GetSeletionData(DataChange_fins);
            _comm.Omron.Show();
            _comm.Omron.Visible = false;
            _comm.Omron.TopLevel = false;      // FINS 连接 1
            _comm.Modbustcp.getData += new FormModbus.GetSeletionData(DataChange_modbustcp);
            _comm.Modbustcp.Show();
            _comm.Modbustcp.Visible = false;
            _comm.Modbustcp.TopLevel = false;  // Modbus-TCP 连接 1
            _comm.ModbusRtu.getData += new FormModbusRtu.GetSeletionData(DataChange_modbusrtu);
            _comm.ModbusRtu.Show();
            _comm.ModbusRtu.Visible = false;
            _comm.ModbusRtu.TopLevel = false;  // Modbus-RTU 连接 1
            // 三菱 FX 编程口：预留项未接入管理器，仍由“三菱Fx编程口”菜单以顶级窗开合
            _comm.Melsec.Show();
            _comm.Melsec.Visible = false;
            frm5.Show();
            frm5.Visible = false;

            // 在监控画面动态添加总耗时标签
            CreateMonitorTimeLabels();

            // 初始化帧率间隔缓存（供后台线程安全读取）
            _cachedNumericUpDown6Value = numericUpDown6.Value;
            numericUpDown6.ValueChanged += (s_, e_) => _cachedNumericUpDown6Value = numericUpDown6.Value;

            // —— 启动前相机关卡复查（三个协议的连接 1）——
            // 兜底"误改 test.ini"导致的同协议相机归属冲突：启动阶段即写日志并明示，避免触发/反馈回写互相覆盖。
            try
            {
                if (_comm != null && _comm.Omron != null && _comm.Omron.ConfigIni != null)
                {
                    var startupIni = _comm.Omron.ConfigIni;
                    var conn1Conflicts = new System.Collections.Generic.List<string>();
                    string fc = CommCameraGuard.CheckStartupFins(startupIni, 1);
                    if (fc != null) conn1Conflicts.Add("FINS 连接 1：\r\n" + fc);
                    string mc = CommCameraGuard.CheckStartupModbusTcp(startupIni, 1);
                    if (mc != null) conn1Conflicts.Add("Modbus-TCP 连接 1：\r\n" + mc);
                    string rc = CommCameraGuard.CheckStartupModbusRtu(startupIni, 1);
                    if (rc != null) conn1Conflicts.Add("Modbus-RTU 连接 1：\r\n" + rc);
                    if (conn1Conflicts.Count > 0)
                    {
                        foreach (var m in conn1Conflicts)
                        {
                            try { _logger.WriteLog("启动复查-相机归属冲突: " + m.Replace("\r\n", " | ")); } catch { }
                        }
                        System.Windows.Forms.MessageBox.Show(
                            "检测到「相机归属冲突」，以下协议连接 1 配置被同协议其它连接占用，连接 1 已阻止自动启动。\r\n"
                            + "为避免触发/反馈回写互相覆盖，请在占用相机的连接里清空冲突项并保存，再重启软件使配置生效。\r\n\r\n"
                            + string.Join("\r\n\r\n", conn1Conflicts),
                            "相机归属冲突 - 连接已阻止启动",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Warning);
                    }
                }
            }
            catch { }

            // ★ 2026-09-11：读取存图保留天数（配置窗可编辑，写回 [存图] baocun_tianshu；默认7）
            try
            {
                int keep;
                if (int.TryParse(_config.ReadString("存图", "baocun_tianshu", "7"), out keep) && keep >= 1)
                {
                    _saveImageKeepDays = keep;
                    if (numericUpDown_saveDays != null && !numericUpDown_saveDays.IsDisposed)
                        numericUpDown_saveDays.Value = keep;
                }
            }
            catch { }

            // ★ 2026-09-11：启动自动清理任务，删除超过 7 天的存图目录（原 delete12 从未被调用，导致目录只增不减占满磁盘）
            //   info*ok1 已在 InitializeJobManager 中赋值，safe 挂在此处；Timer 首跑延时 100 秒，避开启动期 IO 高峰。
            try { delete12(); }
            catch (Exception ex) { _logger.WriteLog("启动删除存图任务失败: " + ex.Message); }
        }

        /// <summary>
        /// 动态创建总耗时标签（叠加在监控画面每个相机 groupBox 标题栏，位于耗时右侧）
        /// </summary>
        private void CreateMonitorTimeLabels()
        {
            monitorTotalTimeLabels = new Label[12];
            // 监控画面对应的 groupBox (按相机1~12顺序)
            GroupBox[] boxes = new GroupBox[]
            {
                groupBox5,  groupBox1,  groupBox8,  groupBox7,
                groupBox18, groupBox19, groupBox22, groupBox21,
                groupBox20, groupBox27, groupBox28, groupBox29
            };
            for (int i = 0; i < 12; i++)
            {
                var lb = new Label();
                lb.AutoSize = true;
                // 放在 runtime 标签(54,0)右侧，预留足够空间
                lb.Location = new System.Drawing.Point(115, 0);
                lb.Margin = new Padding(2, 0, 2, 0);
                lb.Name = "lblTotalTime" + (i + 1);
                lb.Size = new System.Drawing.Size(53, 12);
                lb.Text = "总:0ms";
                lb.ForeColor = Color.Blue;
                boxes[i].Controls.Add(lb);
                monitorTotalTimeLabels[i] = lb;
            }
        }

        SubSet f1;
        private static string GetCPUSerialnumber(string ttt)
        {
            string cpuSerialnumber = string.Empty;
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(identifier("Win32_DiskDrive", "Signature") + "M" + identifier("Win32_DiskDrive", "TotalHeads")));
                byte[] data1 = new byte[] { 0x16, 0xa2, 0xa8 };
                // byte[] data2 = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(strID));
                byte[] data2 = md5Hash.ComputeHash(Encoding.UTF8.GetBytes("123"));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {

                    sBuilder.Append(data[i].ToString("x2"));
                }
                sBuilder.Append(ttt);
                for (int i = 0; i < data2.Length; i++)
                {
                    sBuilder.Append(data2[i].ToString("x2"));
                }
                sBuilder.Append(data.Length * 2);
                string id = sBuilder.ToString();
                cpuSerialnumber = id;
            }
            return cpuSerialnumber;
        }
        /// <summary>
        /// 获取硬件标识符
        /// </summary>
        /// <param name="wmiClass"></param>
        /// <param name="wmiProperty"></param>
        /// <returns></returns>
        private static string identifier(string wmiClass, string wmiProperty)
        {
            string result = "";
            try
            {
                System.Management.ManagementClass mc =
             new System.Management.ManagementClass(wmiClass);
                System.Management.ManagementObjectCollection moc = mc.GetInstances();
                foreach (System.Management.ManagementObject mo in moc)
                {
                    //Only get the first one
                    if (result == "")
                    {
                        try
                        {
                            result = mo[wmiProperty]?.ToString();
                            break;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ":获取");
            };
            return result;
        }
        private static readonly object _locker_open = new object();
        // ★M12：运行按钮启动流程哨兵——连点/重入时直接忽略，防两次启动流程排队重复执行
        private static int _startFlowRunning = 0;
        private static readonly object _close = new object();
        // ★ 相机SDK对象串行化锁：保护 _cameraCtrl.Cameras[i] 的 Create/Open/Stop/Close/Destroy 操作
        // 原则：锁内绝不 Invoke、绝不弹 MessageBox（避免与UI线程互等死锁）
        private readonly object _cameraLock = new object();
        // ★B7：相机 SDK 异常回调置位的"待重连"标志（元素一律用 Volatile 读写；SDK 线程回调只置位、不碰句柄/控件）
        private readonly int[] _cameraSdkFault = new int[12];
        // ★B7：连续运行回帧看门狗采样（上次帧计数 / 计数变化时刻 TickCount；0=未初始化，首次只建基线不判定）
        private readonly int[] _contFrameLast = new int[12];
        private readonly int[] _contFrameLastMs = new int[12];
        // ★B7：连续运行模式"无新帧"判定阈值（毫秒）。取 15 秒：正常连续运行路帧率远高于此；
        //   极低帧率(≤0.5fps)或长曝光(数秒)场景也不会误判。
        private const int ContFrameStallTimeoutMs = 15000;
        /// <summary>
        /// 运行/启动检测主流程（防重入，启动前全量复位 IO 输出）
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            // ★ 无方案时不允许运行
            if (manager1 == null)
            {
                _logger.WriteLog("方案未加载，无法运行");
                this.Invoke(new Action(() => { label133.Text = "未加载方案，请先在配置窗加载方案"; }));
                return;
            }
            // ★M12：启动流程哨兵——上一次启动流程未结束时忽略本次点击（防连点排队重复启动）
            if (Interlocked.CompareExchange(ref _startFlowRunning, 1, 0) != 0)
            {
                _logger.WriteLog("运行请求被忽略：上一次启动流程仍在进行");
                return;
            }
            // ★ 在 UI 线程预缓存 checkedListBox 状态，避免后台线程直接访问 UI 控件
            bool[] checkedCameras = new bool[12];
            try { for (int __i = 0; __i < 12; __i++) checkedCameras[__i] = checkedListBox1.GetItemChecked(__i); } catch { }
            Task.Run(() =>
            {
                try
                {
                lock (_locker_open)
                {
                    DisarmCommTrigger();
                    // 启动前全量复位 IO 输出标志（原只复位 _jobs.myjob1，job2~12 残留输出状态无法恢复）
                    ResetMyJobOutputs();
                    this.Invoke(new Action(() =>
                    {
                        label133.Text = "预备清除流程窗体";
                    }));
                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            listBox4.Items.Clear();
                            listBox5.Items.Clear();
                            listBox8.Items.Clear();
                            listBox9.Items.Clear();
                            listBox10.Items.Clear();
                            listBox11.Items.Clear();
                            listBox12.Items.Clear();
                            listBox13.Items.Clear();
                            _jobs.myjob1.dlg.Dispose();
                        }));

                        Task.Run(() =>
                        {
                            lock (_close)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    for (int i = 0; i < frm6.Count; i++)
                                    {
                                        frm6[i].Close();
                                    }
                                    frm6.Clear();
                                }));
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() =>
                        {
                            label133.Text = ex.Message;
                        }));
                    }

                    try

                    {
                        if (manager1.JobCount > 0)

                        {
                            this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button1.BackColor = UiSuccess;
                            button1.Text = "运行中";
                            button11.BackColor = Color.FromArgb(72, 82, 98);
                            button11.Enabled = true;
                            label133.Text = "预备相机1运行";
                        }));
                            if (checkedCameras[0] && _jobs.myjob1.yun == 0)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    bnStopGrab1.Enabled = false;
                                }));
                                //  cbSoftTrigger1.Enabled = false;

                                _jobs.myjob1.yun = 1;
                                _jobs.myjob1.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(0, out nRet))
                                {
                                    _jobs.myjob1.yun = 0;
                                }
                                else
                                {
                                    nPayloadSize1 = m_nBufSizeForDriver[0];
                                    this.Invoke(new Action(() => { bnStartGrab1.Enabled = false; bnStopGrab1.Enabled = true; }));
                                }
                            }

                                this.Invoke(new Action(() =>
                                {
                                    // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                                    //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                                    try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel1.Enabled = true; bnSetLineSel1.Enabled = true; bnGetLineMode1.Enabled = true; bnSetLineMode1.Enabled = true; checkBox4.Enabled = true; } catch { } })); } catch { }
                                }));
                            // cbSoftTrigger1.Enabled = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _jobs.myjob1.yun = 0;
                        // _jobs.yunxing = false;
                        _logger.WriteLog("相机1运行" + ex.Message);
                    }
                    ;
                    if (manager1.JobCount > 1)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机2运行";
                            }));
                            if (checkedCameras[1] && _jobs.myjob2.yun == 0 && manager1.JobCount > 1)
                            {
                                this.Invoke(new Action(() => { bnStopGrab2.Enabled = false; }));
                                _jobs.myjob2.yun = 1;
                                _jobs.myjob2.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(1, out nRet))
                                {
                                    _jobs.myjob2.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机2 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize2 = m_nBufSizeForDriver[1];
                                    this.Invoke(new Action(() => { bnStartGrab2.Enabled = false; bnStopGrab2.Enabled = true; }));
                                }
                            }
                            this.Invoke(new Action(() =>
                            {
                                // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                                //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                                try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel2.Enabled = true; bnSetLineSel2.Enabled = true; bnGetLineMode2.Enabled = true; bnSetLineMode2.Enabled = true; checkBox10.Enabled = true; } catch { } })); } catch { }
                            }));
                            // cbSoftTrigger2.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob2.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机2运行" + ex.Message);
                        }
                        ;
                    }
                
                    if (manager1.JobCount > 2)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机3运行";
                            }));
                            if (checkedCameras[2] && _jobs.myjob3.yun == 0 && manager1.JobCount > 2)
                            {
                                this.Invoke(new Action(() => { bnStopGrab3.Enabled = false; }));
                                _jobs.myjob3.yun = 1;
                                _jobs.myjob3.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(2, out nRet))
                                {
                                    _jobs.myjob3.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机3 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize3 = m_nBufSizeForDriver[2];
                                    this.Invoke(new Action(() => { bnStartGrab3.Enabled = false; bnStopGrab3.Enabled = true; }));
                                }
                            }
                            this.Invoke(new Action(() =>
                            {
                                // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                                //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                                try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel3.Enabled = true; bnSetLineSel3.Enabled = true; bnGetLineMode3.Enabled = true; bnSetLineMode3.Enabled = true; checkBox16.Enabled = true; } catch { } })); } catch { }
                            }));
                            //  cbSoftTrigger3.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob3.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机3运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 3)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机4运行";
                            }));
                            if (checkedCameras[3] && _jobs.myjob4.yun == 0 && manager1.JobCount > 3)
                            {
                                this.Invoke(new Action(() => { bnStopGrab4.Enabled = false; }));
                                _jobs.myjob4.yun = 1;
                                _jobs.myjob4.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(3, out nRet))
                                {
                                    _jobs.myjob4.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机4 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize4 = m_nBufSizeForDriver[3];
                                    this.Invoke(new Action(() => { bnStartGrab4.Enabled = false; bnStopGrab4.Enabled = true; }));
                                }
                            }
                            this.Invoke(new Action(() =>
                            {
                                // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                                //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                                try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel4.Enabled = true; bnSetLineSel4.Enabled = true; bnGetLineMode4.Enabled = true; bnSetLineMode4.Enabled = true; checkBox20.Enabled = true; } catch { } })); } catch { }
                            }));
                            //  cbSoftTrigger4.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob4.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机4运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 4)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机5运行";
                            }));
                            if (checkedCameras[4] && _jobs.myjob5.yun == 0 && manager1.JobCount > 4)
                            {
                                this.Invoke(new Action(() => { bnStopGrab5.Enabled = false; }));
                                _jobs.myjob5.yun = 1;
                                _jobs.myjob5.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(4, out nRet))
                                {
                                    _jobs.myjob5.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机5 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize5 = m_nBufSizeForDriver[4];
                                    this.Invoke(new Action(() => { bnStartGrab5.Enabled = false; bnStopGrab5.Enabled = true; }));
                                }
                            }

                            this.Invoke(new Action(() =>
                            {
                                // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                                //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                                try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel5.Enabled = true; bnSetLineSel5.Enabled = true; bnGetLineMode5.Enabled = true; bnSetLineMode5.Enabled = true; checkBox34.Enabled = true; } catch { } })); } catch { }
                            }));
                            // cbSoftTrigger1.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob5.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机5运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 5)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机6运行";
                            }));
                            if (checkedCameras[5] && _jobs.myjob6.yun == 0 && manager1.JobCount > 5)
                            {
                                this.Invoke(new Action(() => { bnStopGrab6.Enabled = false; }));
                                _jobs.myjob6.yun = 1;
                                _jobs.myjob6.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(5, out nRet))
                                {
                                    _jobs.myjob6.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机6 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize6 = m_nBufSizeForDriver[5];
                                    this.Invoke(new Action(() => { bnStartGrab6.Enabled = false; bnStopGrab6.Enabled = true; }));
                                }
                            }
                            this.Invoke(new Action(() =>
                            {
                                // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                                //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                                try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel6.Enabled = true; bnSetLineSel6.Enabled = true; bnGetLineMode6.Enabled = true; bnSetLineMode6.Enabled = true; checkBox40.Enabled = true; } catch { } })); } catch { }
                            }));
                            // cbSoftTrigger2.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob6.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机6运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 6)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机7运行";
                            }));
                            if (checkedCameras[6] && _jobs.myjob7.yun == 0 && manager1.JobCount > 6)
                            {
                                this.Invoke(new Action(() => { bnStopGrab7.Enabled = false; }));
                                _jobs.myjob7.yun = 1;
                                _jobs.myjob7.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(6, out nRet))
                                {
                                    _jobs.myjob7.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机7 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize7 = m_nBufSizeForDriver[6];
                                    this.Invoke(new Action(() => { bnStartGrab7.Enabled = false; bnStopGrab7.Enabled = true; }));
                                }
                            }
                            this.Invoke(new Action(() =>
                            {
                                // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                                //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                                try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel7.Enabled = true; bnSetLineSel7.Enabled = true; bnGetLineMode7.Enabled = true; bnSetLineMode7.Enabled = true; checkBox46.Enabled = true; } catch { } })); } catch { }
                            }));
                            //  cbSoftTrigger3.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob7.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机7运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 7)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机8运行";
                            }));
                            if (checkedCameras[7] && _jobs.myjob8.yun == 0 && manager1.JobCount > 7)
                            {
                                this.Invoke(new Action(() => { bnStopGrab8.Enabled = false; }));
                                _jobs.myjob8.yun = 1;
                                _jobs.myjob8.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(7, out nRet))
                                {
                                    _jobs.myjob8.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机8 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize8 = m_nBufSizeForDriver[7];
                                    this.Invoke(new Action(() => { bnStartGrab8.Enabled = false; bnStopGrab8.Enabled = true; }));
                                }
                            }
                            // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                            //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                            try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel8.Enabled = true; bnSetLineSel8.Enabled = true; bnGetLineMode8.Enabled = true; bnSetLineMode8.Enabled = true; checkBox52.Enabled = true; } catch { } })); } catch { }
                            //  cbSoftTrigger4.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob8.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机8运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 8)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机9运行";
                            }));
                            if (checkedCameras[8] && _jobs.myjob9.yun == 0 && manager1.JobCount > 8)
                            {
                                this.Invoke(new Action(() => { bnStopGrab9.Enabled = false; }));
                                _jobs.myjob9.yun = 1;
                                _jobs.myjob9.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(8, out nRet))
                                {
                                    _jobs.myjob9.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机9 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize9 = m_nBufSizeForDriver[8];
                                    this.Invoke(new Action(() => { bnStartGrab9.Enabled = false; bnStopGrab9.Enabled = true; }));
                                }
                            }
                            // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                            //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                            try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel9.Enabled = true; bnSetLineSel9.Enabled = true; bnGetLineMode9.Enabled = true; bnSetLineMode9.Enabled = true; checkBox76.Enabled = true; } catch { } })); } catch { }
                            //  cbSoftTrigger4.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob9.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机9运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 9)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机10运行";
                            }));
                            if (checkedCameras[9] && _jobs.myjob10.yun == 0 && manager1.JobCount > 9)
                            {
                                this.Invoke(new Action(() => { bnStopGrab10.Enabled = false; }));
                                _jobs.myjob10.yun = 1;
                                _jobs.myjob10.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(9, out nRet))
                                {
                                    _jobs.myjob10.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机10 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize10 = m_nBufSizeForDriver[9];
                                    this.Invoke(new Action(() => { bnStartGrab10.Enabled = false; bnStopGrab10.Enabled = true; }));
                                }
                            }
                            // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                            //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                            try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel10.Enabled = true; bnSetLineSel10.Enabled = true; bnGetLineMode10.Enabled = true; bnSetLineMode10.Enabled = true; checkBox82.Enabled = true; } catch { } })); } catch { }
                            //  cbSoftTrigger4.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob10.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机10运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 10)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机11运行";
                            }));
                            if (checkedCameras[10] && _jobs.myjob11.yun == 0 && manager1.JobCount > 10)
                            {
                                this.Invoke(new Action(() => { bnStopGrab11.Enabled = false; }));
                                _jobs.myjob11.yun = 1;
                                _jobs.myjob11.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(10, out nRet))
                                {
                                    _jobs.myjob11.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机11 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize11 = m_nBufSizeForDriver[10];
                                    this.Invoke(new Action(() => { bnStartGrab11.Enabled = false; bnStopGrab11.Enabled = true; }));
                                }
                            }
                            // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                            //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                            try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel11.Enabled = true; bnSetLineSel11.Enabled = true; bnGetLineMode11.Enabled = true; bnSetLineMode11.Enabled = true; checkBox88.Enabled = true; } catch { } })); } catch { }
                            //  cbSoftTrigger4.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob11.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机11运行" + ex.Message);
                        }
                        ;
                    }
                    if (manager1.JobCount > 11)
                    {
                        try
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "预备相机12运行";
                            }));
                            if (checkedCameras[11] && _jobs.myjob12.yun == 0 && manager1.JobCount > 11)
                            {
                                this.Invoke(new Action(() => { bnStopGrab12.Enabled = false; }));
                                _jobs.myjob12.yun = 1;
                                _jobs.myjob12.trriger = 0;
                                _jobs.yunxing = true;
                                int nRet;
                                if (!PrepareCameraGrab(11, out nRet))
                                {
                                    _jobs.myjob12.yun = 0;
                                    this.Invoke(new Action(() => ShowErrorMsg("相机12 Get PayloadSize failed", nRet)));
                                }
                                else
                                {
                                    nPayloadSize12 = m_nBufSizeForDriver[11];
                                    this.Invoke(new Action(() => { bnStartGrab12.Enabled = false; bnStopGrab12.Enabled = true; }));
                                }
                            }
                            // ★F2 修复（2026-09-20）：原为裸写——后台线程直接设置控件属性，Debug 跨线程校验(Form1.cs:227)下
                            //   一旦抛『线程间操作无效』会被 per-slot catch 吞掉并置该路 yun=0（静默不检测）。改线程安全投递。
                            try { SafeBeginInvoke(new Action(() => { try { bnGetLineSel12.Enabled = true; bnSetLineSel12.Enabled = true; bnGetLineMode12.Enabled = true; bnSetLineMode12.Enabled = true; checkBox94.Enabled = true; } catch { } })); } catch { }
                            //  cbSoftTrigger4.Enabled = true;
                        }
                        catch (Exception ex)
                        {
                            _jobs.myjob12.yun = 0;
                            // _jobs.yunxing = false;
                            _logger.WriteLog("相机12运行" + ex.Message);
                        }
                        ;
                    }
                    _jobs.yunxing = true;
                    this.Invoke(new Action(() =>
                    {
                        numericUpDown3.Enabled = false;
                        checkedListBox1.Enabled = false;
                    }));
                    if (_jobs.myjob1.yun == 0 && _jobs.myjob2.yun == 0 && _jobs.myjob3.yun == 0 && _jobs.myjob4.yun == 0 && _jobs.myjob5.yun == 0 && _jobs.myjob6.yun == 0 && _jobs.myjob7.yun == 0 && _jobs.myjob8.yun == 0 && _jobs.myjob9.yun == 0 && _jobs.myjob10.yun == 0 && _jobs.myjob11.yun == 0 && _jobs.myjob12.yun == 0)
                    {
                        _jobs.yunxing = false;
                        DisarmCommTrigger();
                        _jobs.myjob1.yun = 0;
                        _jobs.myjob2.yun = 0;
                        _jobs.myjob3.yun = 0;
                        _jobs.myjob4.yun = 0;
                        _jobs.myjob5.yun = 0;
                        _jobs.myjob6.yun = 0;
                        _jobs.myjob7.yun = 0;
                        _jobs.myjob8.yun = 0;
                        _jobs.myjob9.yun = 0;
                        _jobs.myjob10.yun = 0;
                        _jobs.myjob11.yun = 0;
                        _jobs.myjob12.yun = 0;
                        this.Invoke(new Action(() =>
                        {
                            checkedListBox1.Enabled = true;
                            numericUpDown3.Enabled = true;
                        }));
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            label133.Text = "启动运行失败";
                            button1.BackColor = UiSuccess;
                            button1.Text = "运行";
                            button11.BackColor = UiDanger;
                            存图测试1ToolStripMenuItem.Checked = false;
                            存图测试1ToolStripMenuItem_Click(null, null);
                            // 存图测试1ToolStripMenuItem.Checked = true;
                        }));

                        // ★N7：原实现在 Task 线程裸读控件——Debug 下抛跨线程异常且被 Task 静默吞，
                        //   其后的 EnableCameraReconnect 被整段跳过（相机掉线不再自动重连）。改 UI 线程读取。
                        bool _noneChecked = false;
                        this.Invoke(new Action(() => { _noneChecked = checkedListBox1.SelectedIndices.Count == 0; }));
                        if (_noneChecked)
                            _logger.WriteLog("请选择要开启的相机");
                    }
                    else
                    {
                            ArmCommTriggerAfterRunReady();
                            try
                            {
                                this.Invoke(new Action(() =>
                                {
                                    label133.Text = "预备处理图像测试和工具显示";
                                    存图测试1ToolStripMenuItem.Checked = true;
                                    存图测试1ToolStripMenuItem_Click(null, null);
                                    checkBox70.CheckState = CheckState.Unchecked;
                                    checkBox70_CheckedChanged(null, null);
                                    label133.Text = "启动运行成功";
                                }));
                            }
                            catch { }


                    }
                    EnableCameraReconnect();
                }
                }
                catch (Exception exStart)
                {
                    // ★N7：启动流程异常兜底——原实现只有 try/finally，异常被 Task 静默吞（后续步骤整段跳过）
                    _logger.WriteLog("启动运行流程异常: " + exStart.Message);
                }
                finally
                {
                    Interlocked.Exchange(ref _startFlowRunning, 0);
                }
            });
        }
        public static int Diaohuan(int cc)
        {
            int da;
            int xiao;
            xiao = cc / 65536;
            da = cc % 65536;
            cc = da * 65536 + xiao;
            return cc;
        }
        /// <summary>
        /// 安全解析相机编号（path_number 形如 "1".."12"），返回0基索引，非法时返回-1
        /// </summary>
        private int ParsePathNumberIndex(Myjob myjob)
        {
            int slot;
            if (myjob == null || !int.TryParse(myjob.path_number, out slot))
                return -1;
            slot -= 1;
            return (slot >= 0 && slot < 12) ? slot : -1;
        }

        /// <summary>
        /// 按相机索引（0 基）取该路的 OK 存图目录，替代原 12 路 switch 里的 info1ok..info12ok。
        /// 注意：这些字段会在日期变更时被重新 new（初始化处与日期刷新处各一次），
        /// 因此这里每次返回字段当前值，不做数组快照，避免拿到过期引用。
        /// </summary>
        private DirectoryInfo GetOkDirInfo(int camIdx)
        {
            switch (camIdx)
            {
                case 0: return info1ok;
                case 1: return info2ok;
                case 2: return info3ok;
                case 3: return info4ok;
                case 4: return info5ok;
                case 5: return info6ok;
                case 6: return info7ok;
                case 7: return info8ok;
                case 8: return info9ok;
                case 9: return info10ok;
                case 10: return info11ok;
                case 11: return info12ok;
                default: return null;
            }
        }

        /// <summary>
        /// 按相机索引（0 基）取该路的 NG 存图目录，替代原 12 路 switch 里的 info1ng..info12ng。
        /// 同上：每次返回字段当前值，不做快照。
        /// </summary>
        private DirectoryInfo GetNgDirInfo(int camIdx)
        {
            switch (camIdx)
            {
                case 0: return info1ng;
                case 1: return info2ng;
                case 2: return info3ng;
                case 3: return info4ng;
                case 4: return info5ng;
                case 5: return info6ng;
                case 6: return info7ng;
                case 7: return info8ng;
                case 8: return info9ng;
                case 9: return info10ng;
                case 10: return info11ng;
                case 11: return info12ng;
                default: return null;
            }
        }

        // 释放相机帧 Bitmap（方案切换提前退出时使用，防止 GDI 句柄泄漏）
        /// <summary>
        /// 释放相机帧 Bitmap（方案切换提前退出时防 GDI 泄漏）
        /// </summary>
        private void ReleaseFrameBitmap(int camIdx)
        {
            if (camIdx >= 0 && camIdx < 12)
            {
                if (bmp[camIdx] != null)
                {
                    try { bmp[camIdx].Dispose(); } catch { }
                }
                bmp[camIdx] = null;
            }
        }

        /// <summary>
        /// 每路独立检测线程：回调只入队最新帧，检测不占用海康 SDK 线程。
        /// </summary>
        private void StartInspectWorkers()
        {
            _inspectStop = false;
            for (int i = 0; i < 12; i++)
            {
                int slot = i;
                _inspectThreads[slot] = new Thread(() => InspectWorker(slot));
                _inspectThreads[slot].IsBackground = true;
                _inspectThreads[slot].Name = "InspectCam" + (slot + 1);
                _inspectThreads[slot].Start();
            }
        }

        /// <summary>
        /// ★ F14: 切换方案前显式释放各相机持有的 VisionPro COM 对象（block/job/newrecod/calib/Cogbmp）。
        /// 调用时机：manager1.Shutdown() 之后、加载新方案之前；此时检测线程已因 _switchingScheme 丢弃帧，
        /// 不会再访问 block，可安全置空并强制 GC 回收 RCW，避免频繁切方案导致 COM 句柄/内存累积。
        /// </summary>
        private void ReleaseAllMyjobVisionObjects()
        {
            if (_jobs.Myjobs == null) return;
            for (int i = 0; i < _jobs.Myjobs.Length; i++)
            {
                var j = _jobs.Myjobs[i];
                if (j == null) continue;
                try { j.newrecod = null; } catch { }
                try { j.block = null; } catch { }
                try { j.job = null; } catch { }
                try { j.calib = null; } catch { }
                try { j.Cogbmp = null; } catch { }
            }
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch { }
        }

        private bool StopInspectWorkers()
        {
            _inspectionLifecycle.StopAccepting();
            _inspectStop = true;
            for (int i = 0; i < 12; i++)
            {
                try { if (_inspectSignal[i] != null) _inspectSignal[i].Set(); } catch { }
            }
            for (int i = 0; i < 12; i++)
            {
                try
                {
                    if (_inspectThreads[i] != null && _inspectThreads[i].IsAlive)
                        _inspectThreads[i].Join(300);
                }
                catch { }
                if (_inspectLocks[i] == null) continue;
                lock (_inspectLocks[i])
                {
                    // 帧清理原保护 _pendingFrame（死字段已删），锁在此保持对称
                }
            }
            return _inspectionLifecycle.WaitForIdle(10000);
        }

        private sealed class InspectionFrame
        {
            public readonly Bitmap Image;
            public readonly CameraTriggerRecord Trigger;
            public InspectionFrame(Bitmap image, CameraTriggerRecord trigger)
            {
                Image = image;
                Trigger = trigger;
            }
        }

        private const int InspectQueueDepth = 3;
        private readonly System.Collections.Generic.Queue<InspectionFrame>[] _frameQueue =
            new System.Collections.Generic.Queue<InspectionFrame>[12];
        private readonly int[] _droppedFrameCount = new int[12];
        private readonly DateTime[] _lastDropLogAt = new DateTime[12];

        private void InspectWorker(int slot)
        {
            while (!_inspectStop && !_disposingFlag)
            {
                InspectionFrame frame = null;
                lock (_inspectLocks[slot])
                {
                    if (_frameQueue[slot] != null && _frameQueue[slot].Count > 0)
                        frame = _frameQueue[slot].Dequeue();
                }
                if (frame == null)
                {
                    try { _inspectSignal[slot].WaitOne(200); }
                    catch { break; }
                    continue;
                }
                try
                {
                    if (_switchingScheme || _disposingFlag) continue;
                    ReleaseFrameBitmap(slot);
                    bmp[slot] = frame.Image;
                    var payload = frame.Trigger != null ? frame.Trigger.Payload : default(System.Collections.Generic.KeyValuePair<string, string>);
                    getrecord(_jobs.Myjobs[slot], payload, frame.Trigger?.Source, frame.Image == null);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("检测线程异常 相机" + (slot + 1) + ": " + ex.Message);
                }
                finally
                {
                    if (bmp[slot] == frame.Image) ReleaseFrameBitmap(slot);
                    else if (frame.Image != null) frame.Image.Dispose();
                }
            }
        }

        private void DrainPendingFrames()
        {
            for (int i = 0; i < 12; i++)
            {
                if (_inspectLocks[i] == null) continue;
                lock (_inspectLocks[i])
                {
                    if (_frameQueue[i] == null) continue;
                    while (_frameQueue[i].Count > 0)
                        _frameQueue[i].Dequeue().Image?.Dispose();
                }
            }
        }

        private void EnqueueInspectFrame(int slot, Bitmap owned, CameraTriggerRecord trigger)
        {
            // A null image represents a failed acquisition, processed in order as an NG transaction.
            if (_inspectStop || _disposingFlag || _switchingScheme || slot < 0 || slot >= 12)
            {
                owned?.Dispose();
                return;
            }
            lock (_inspectLocks[slot])
            {
                if (_frameQueue[slot] == null) _frameQueue[slot] = new System.Collections.Generic.Queue<InspectionFrame>();
                while (_frameQueue[slot].Count >= InspectQueueDepth)
                {
                    _frameQueue[slot].Dequeue().Image?.Dispose();
                    int count = ++_droppedFrameCount[slot];
                    DateTime now = DateTime.Now;
                    if ((now - _lastDropLogAt[slot]).TotalSeconds >= 1)
                    {
                        _lastDropLogAt[slot] = now;
                        _logger.WriteLog("相机" + (slot + 1) + "检测过载：待检队列满，丢弃最旧帧（累计" + count + "）");
                    }
                }
                _frameQueue[slot].Enqueue(new InspectionFrame(owned, trigger));
            }
            try { _inspectSignal[slot].Set(); } catch { }
        }

        private void EnsureConvertBuffer(int camIndex, uint needSize)
        {
            if (camIndex < 0 || camIndex >= 12 || needSize == 0) return;
            if (m_pSaveImageBuf[camIndex] != IntPtr.Zero && m_nSaveImageBufSize[camIndex] >= needSize)
                return;
            if (m_pSaveImageBuf[camIndex] != IntPtr.Zero)
            {
                try { Marshal.FreeHGlobal(m_pSaveImageBuf[camIndex]); } catch { }
                m_pSaveImageBuf[camIndex] = IntPtr.Zero;
                m_nSaveImageBufSize[camIndex] = 0;
            }
            m_pSaveImageBuf[camIndex] = Marshal.AllocHGlobal((int)needSize);
            m_nSaveImageBufSize[camIndex] = needSize;
        }

        private static void TryDisposeCogImage(object old)
        {
            IDisposable d = old as IDisposable;
            if (d == null) return;
            try { d.Dispose(); } catch { }
        }

        private void AssignBlockInputImage(CogToolBlock block, Bitmap src, bool color)
        {
            if (block == null || src == null || !block.Inputs.Contains("Input"))
                throw new InvalidOperationException("检测输入图像或 Input 端口不存在");
            object old = null;
            try { old = block.Inputs["Input"].Value; } catch { }
            ICogImage next = color
                ? (ICogImage)new CogImage24PlanarColor(src)
                : new CogImage8Grey(src);
            block.Inputs["Input"].Value = next;
            if (!object.ReferenceEquals(old, next))
                TryDisposeCogImage(old);
        }

        /// <summary>
        /// 保存方案前清洗失效的图像引用。
        /// VisionPro 在程序长时间运行、或相机未采集时，工具输入图像的底层句柄可能已被系统回收，
        /// 此时直接序列化保存整个工作管理器，会因访问"已释放对象"(ObjectDisposedException)而崩溃
        /// （客户现场表现为"保存方案失败/一闪崩溃"，重启后重新采集句柄有效即恢复）。
        /// <para>
        /// ★ N1：不再只清洗名为 "Input" 的那一个终端 —— 遍历**全部输入终端**，把"已失效"的图像统一
        /// 替换成有效、自拥有的空白图（保存 .vpp 本就不包含运行时输入帧，替换不影响方案内容，
        /// 加载后再喂真实帧）；**输出终端只检测并记录**（输出通常只读，改输出会破坏结果语义）。
        /// </para>
        /// </summary>
        private void SanitizeSchemeImagesForSave()
        {
            if (_jobs?.Myjobs == null || manager1 == null) return;
            int cleaned = 0;
            int suspiciousOutputs = 0;

            foreach (var mj in _jobs.Myjobs)
            {
                CogToolBlock block = mj?.block;
                if (block == null) continue;

                // 1) 输入终端：失效图像替换为自拥有的空白图
                try
                {
                    for (int i = 0; i < block.Inputs.Count; i++)
                    {
                        object v;
                        try { v = block.Inputs[i].Value; } catch { continue; }
                        ICogImage img = v as ICogImage;
                        if (img == null || IsCogImageUsable(img)) continue;

                        ICogImage replacement = mj.Color
                            ? (ICogImage)new CogImage24PlanarColor(640, 480)
                            : new CogImage8Grey(640, 480);
                        try { block.Inputs[i].Value = replacement; } catch { }
                        TryDisposeCogImage(v);
                        cleaned++;
                    }
                }
                catch { }

                // 2) 输出终端：只检测，不修改
                try
                {
                    for (int i = 0; i < block.Outputs.Count; i++)
                    {
                        object v;
                        try { v = block.Outputs[i].Value; } catch { continue; }
                        ICogImage img = v as ICogImage;
                        if (img != null && !IsCogImageUsable(img)) suspiciousOutputs++;
                    }
                }
                catch { }
            }

            if (cleaned > 0)
                _logger.WriteLog("保存前已替换 " + cleaned + " 张失效图像引用，避免保存序列化崩溃");
            if (suspiciousOutputs > 0)
                _logger.WriteLog("保存前检测到 " + suspiciousOutputs + " 个输出终端仍引用失效图像（未修改；若保存失败请先重启软件）");
        }

        /// <summary>图像对象是否仍可访问（底层句柄未被释放）。</summary>
        private static bool IsCogImageUsable(ICogImage img)
        {
            try { int w = img.Width; int h = img.Height; return true; }
            catch { return false; }
        }

        #endregion
        #region 检测主流程（getrecord）
        /// <summary>
        /// 检测主流程：取图→block.Run 检测→输出 OK/NG→统计/存图（检测线程执行，不在海康回调里跑）
        /// ★ 2026-09-06（②参数错位修复）：增加 payload 参数，将通讯触发时刻的 inputKey/selection 快照
        /// 在检测前写入 block 输入，避免多触发连续到达时共享 block.Inputs 被覆盖。
        /// </summary>
        private void getrecord(Myjob myjob, System.Collections.Generic.KeyValuePair<string, string> payload, CommTriggerSource frameSrc = null, bool acquisitionFailed = false)
        {
            if (!_inspectionLifecycle.TryEnter()) return;
            try
            {
                if (_switchingScheme || _disposingFlag) return;
                GetRecordCore(myjob, payload, frameSrc, acquisitionFailed);
            }
            finally { _inspectionLifecycle.Exit(); }
        }

        private void GetRecordCore(Myjob myjob, System.Collections.Generic.KeyValuePair<string, string> payload, CommTriggerSource frameSrc, bool acquisitionFailed)
        {
            EnsureCameraOutWork(); // ★P2-1：本帧用输出队列，惰性初始化一次
            try
            {
                if (myjob?.block == null) return;
                int jobnumber = 0;
                int camIdx = ParsePathNumberIndex(myjob);
                string cuowu1;
                string fins = "无";
                string temptime = "";
                string tempout1;
                string tempout2;
                string tempout3;
                string modbustcps = "无";
                string modbusrtus = "无";
                CogImageFileBMP tempfilebmp = myjob.Cogbmp;
                //if (myjob.IOyanshi == 0)
                //    tempyanshi = false;
                if (((_jobs.yunxing == true && myjob.yun == 1) || myjob.trriger == 1) && myjob.en == 1)
                {
                    if (_jobs.yunxing && myjob.yun == 1 && myjob.trriger == 0
                        && IsCommTriggerMode(myjob.triggerMode) && !_jobs.CommTriggerArmed)
                        return;

                    if (myjob.job.State == CogJobStateConstants.Stopped)
                    {

                        // ★ 输入准备是否成功：取图 / 传图 / payload 写入任一失败则为 false。
                        //   为 false 时不再执行 block.Run()（原实现会拿上一件的图像/参数生成本件结果）。
                        bool inputOk = !acquisitionFailed;
                        if ((!myjob.trrigerEn) && (myjob.trriger == 0) || (myjob.trriger == 0 && myjob.trrigerEn))
                        {

                            try
                            {

                                _jobs.ApplyContinuousParameters(camIdx, myjob);
                                #region 取图
                                if (camIdx >= 0 && camIdx < 12 && bmp[camIdx] != null)
                                {
                                    AssignBlockInputImage(myjob.block, bmp[camIdx], myjob.Color);
                                    if (zhuanpanEn == true && camIdx == 0)
                                    {
                                        decimal jiajutemp = 0;
                                        if (numericUpDown3.Value == 16)
                                        {
                                            jiajutemp = 1;
                                            SafeBeginInvoke(new Action(() =>
                                            {
                                                numericUpDown3.Value = jiajutemp;
                                            }));
                                            _jobs.myjob1.block.Inputs["jiajuhao"].Value = jiajutemp;
                                        }
                                        else
                                        {
                                            jiajutemp = numericUpDown3.Value + 1;
                                            SafeBeginInvoke(new Action(() =>
                                            {
                                                numericUpDown3.Value = jiajutemp;
                                            }));
                                            _jobs.myjob1.block.Inputs["jiajuhao"].Value = jiajutemp;
                                        }
                                    }
                                }
                                #endregion

                                // ★ 2026-09-06（②参数错位修复）：把随帧携带的 payload 写入 block 输入
                                if (!string.IsNullOrEmpty(payload.Key)
                                    && !(frameSrc?.Proto == NoProtoProto && !myjob.block.Inputs.Contains(payload.Key)))
                                {
                                    if (!myjob.block.Inputs.Contains(payload.Key))
                                        throw new InvalidOperationException("触发参数端口不存在: " + payload.Key);
                                    try { myjob.block.Inputs[payload.Key].Value = payload.Value; }
                                    catch (Exception exPayload) { inputOk = false; _logger.WriteLog("相机" + myjob.path_number + " payload 写入失败: " + exPayload.Message); }
                                }
                            }
                            catch (Exception ex)
                            {
                                inputOk = false;
                                _logger.WriteLog(ex.Message + "相机" + myjob.path_number + "取图");
                            };

                        }

                        if (acquisitionFailed || myjob.trriger == 1 || (camIdx >= 0 && camIdx < 12 && bmp[camIdx] != null))
                        {
                            jobnumber = myjob.numberng;

                            cuowu1 = "";
                            if (myjob.trriger == 1)
                            {
                                try
                                {
                                    AssignBlockInputImage(myjob.block, myjob.img, myjob.Color);
                                    myjob.img.Dispose();
                                }
                                catch (Exception exIn)
                                {
                                    // ★ 原为空 catch：输入图准备失败被静默吞掉，后续仍拿旧输入跑 Run
                                    inputOk = false;
                                    _logger.WriteLog("相机" + myjob.path_number + " 输入图像准备失败: " + exIn.Message);
                                }
                            }
                            else
                            {
                                #region 传图（序号递增）
                                if (myjob.triggerMode != "连续运行")
                                {
                                    myjob.numberng++;
                                    if (myjob.numberng > 999)
                                        myjob.numberng = 0;
                                    if (zhuanpanEn == true && camIdx == 0)
                                        myjob.numberng = decimal.ToInt32(numericUpDown3.Value);
                                    jobnumber = myjob.numberng;
                                }
                                #endregion
                            }
                            if (_switchingScheme)
                            {
                                ReleaseFrameBitmap(camIdx);
                                return;   // 方案切换中：丢弃旧帧，停止检测
                            }
                            // getrecord 的生命周期保护覆盖输入、运行和结果快照。
                            bool runOk = false;
                            if (!inputOk)
                            {
                                // ★ 输入准备失败：终止本帧，不跑检测，后续统一按失败输出（Reject/NG），
                                //   避免用上一件的图像或参数生成当前件的结果。
                                _logger.WriteLog("相机" + myjob.path_number + " 输入准备失败，本帧判失败（不再检测）");
                            }
                            else
                            {
                                try
                                {
                                    myjob.block.Run();
                                    runOk = myjob.block.RunStatus.Result != CogToolResultConstants.Error;
                                    if (!runOk) _logger.WriteLog("相机" + myjob.path_number + " 工具块运行状态为 Error");
                                }
                                catch (Exception exRun)
                                {
                                    _logger.WriteLog("相机" + myjob.path_number + " block.Run异常: " + exRun.Message);
                                }
                            }
                            if (_switchingScheme)
                            {
                                ReleaseFrameBitmap(camIdx);
                                return;   // 检测期间发起切换：丢弃该帧结果，不再输出
                            }

                            if (myjob.tishi && runOk)
                            {
                                try
                                {
                                    cuowu1 = myjob.block.Outputs["tishi"].Value.ToString();
                                }
                                catch
                                {
                                    cuowu1 = "NG";
                                }
                            }

                            else
                                cuowu1 = "NG";
                            if (!runOk)
                            {
                                tempout1 = "Reject";
                                tempout2 = "Reject";
                                tempout3 = "空";
                                cuowu1 = "NG";
                            }
                            else if (myjob.block.Outputs.Contains("Output"))
                                tempout1 = myjob.block.Outputs["Output"].Value.ToString();
                            else
                                tempout1 = "Reject";
                            if (runOk && myjob.block.Outputs.Contains("Output1"))
                                tempout2 = myjob.block.Outputs["Output1"].Value.ToString();
                            else if (!runOk)
                                tempout2 = "Reject";
                            else
                                tempout2 = "Reject";

                            if (runOk && myjob.block.Outputs.Contains("Output3"))
                                tempout3 = myjob.block.Outputs["Output3"].Value.ToString();
                            else if (!runOk)
                                tempout3 = "空";
                            else
                                tempout3 = "空";

                            // ★ 修复【存图静默失效】：此处原先提前 Dispose 并置空 bmp[camIdx]，
                            //   导致后续存图块 4583 的 if(bmp!=null).Clone() 恒为 null，OK/NG 图从不落盘。
                            //   本帧 Bitmap 已由 InspectWorker 的 finally（bmp[slot]==frame 时 ReleaseFrameBitmap）
                            //   在 getrecord 返回后统一释放，故这里无需（也不能）提前释放。
                            // Thread.Sleep(15);
                            try
                            {
                                myjob.runtime = runOk ? myjob.block.RunStatus.ProcessingTime : 0;
                            }
                            catch
                            {
                                myjob.runtime = 0;
                            }
                            myjob.time = (myjob.timewatch != null ? myjob.timewatch.ElapsedMilliseconds : 0) + (long)(myjob.cameraAcqTicks / 1000f);
                            if (NG)
                            {
                                if (myjob.time >= jiankongshijian)
                                {
                                    tempout1 = "Reject";
                                    tempout2 = "Reject";
                                    cuowu1 = "NG";
                                }
                            }
                       
                            #region 流程封

                            if (myjob.IO)
                            {


                                #region IO输出
                                #endregion
                                // ★ 输出任务瘦身（性能优先）：IO 脉冲与 TCP/串口仅在真正需要输出时才创建 Task，
                                //   不再每帧生成空转任务 + 12 次 path_number 字符串循环比较。
                                if (myjob.time >= jiankongshijian)
                                    myjob.runcishu++;
                                if (camIdx >= 0 && tempout1 == shuchuqufan && myjob.outputok < 3)
                                {
                                    int io = camIdx + 1;
                                    _cameraOutWork[io - 1].Enqueue(() =>
                                    {
                                        try
                                        {
                                            _jobs.SetOk(io, 1);
                                            try { Thread.Sleep(myjob.timespace); } catch { Thread.Sleep(100); }
                                            _jobs.SetOk(io, -1);
                                        }
                                        catch { }
                                    });
                                }
                                if (camIdx >= 0 && tempout2 == shuchuqufan && myjob.outputng < 3)
                                {
                                    int io = camIdx + 1;
                                    _cameraOutWork[io - 1].Enqueue(() =>
                                    {
                                        try
                                        {
                                            _jobs.SetNg(io, 1);
                                            try { Thread.Sleep(myjob.timespace); } catch { Thread.Sleep(100); }
                                            _jobs.SetNg(io, -1);
                                        }
                                        catch { }
                                    });
                                }
                            }
                            // 阶段：无协议连接2~4 的结果回写“发出触发的那条无协议连接”的端口，而非固定 connection 1
                            Form3 nprotoTarget = null;
                            // BUG A 修复：一次性取出触发来源快照并置 null（引用赋值原子，proto/link 配对永真，防粘滞）
                            // 用随帧携带的来源（入队时绑定），不再读 Myjob 共享字段：
                            // 多协议触发同一相机时，后触发不会再覆盖先触发的来源，结果不会回错连接。
                            var trigSrc = frameSrc;
                            int trigLink = trigSrc != null ? trigSrc.LinkId : 0;
                            int trigProto = trigSrc != null ? trigSrc.Proto : 0;
                            if (trigProto == NoProtoProto && trigLink > 1)
                                nprotoTarget = frmCommManager.GetNoProtoLink(trigLink);
                            Form3 noProtoOut = nprotoTarget ?? frm3;
                            // ★修复（现场确认）：本帧来自无协议连接2~4但目标实例已释放（如该连接被删除）时，
                            //   不再回落到连接1端口发送（原实现静默错发会污染连接1的数据），
                            //   本帧的无协议输出整体跳过——PLC 靠超时判 NG（安全侧）。
                            bool noProtoTargetMissing = (trigProto == NoProtoProto && trigLink > 1 && nprotoTarget == null);
                            if (noProtoTargetMissing)
                                _logger.WriteLog("无协议连接" + trigLink + "的实例已释放，本帧无协议结果不再发送（靠 PLC 超时判 NG），请检查该连接是否被删除");
                            if (myjob.tcp && !noProtoTargetMissing)
                            {
                                // 在检测线程上快照输出值，避免后台任务跨线程读 VisionPro COM(block.Outputs) 引发竞态
                                string tcpVal;
                                try { tcpVal = runOk ? myjob.block.Outputs["tcp"].Value.ToString() : "Reject"; }
                                catch (Exception ex) { _logger.WriteLog("相机" + (camIdx + 1) + " TCP输出失败: " + ex.Message); tcpVal = "无"; }
                                // ★ 2026-09-13：同样接入 per-相机 FIFO 队列，保证同一相机结果按帧序发送
                                //   （Task.Run 逐帧并发不保证获取 changeok 锁的先后，A/B 可能乱序）。
                                _cameraOutWork[camIdx >= 0 && camIdx < 12 ? camIdx : 0].Enqueue(() =>
                                {
                                    try
                                    {
                                        noProtoOut.changeok(tcpVal);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.WriteLog("相机" + (camIdx + 1) + " TCP输出失败: " + ex.Message);
                                    }
                                });
                            }
                            if (myjob.serial && !noProtoTargetMissing)
                            {
                                string serialVal;
                                try { serialVal = runOk ? myjob.block.Outputs["serial"].Value.ToString() : "Reject"; }
                                catch (Exception ex) { _logger.WriteLog("相机" + (camIdx + 1) + " 串口输出失败: " + ex.Message); serialVal = "无"; }
                                // ★ 2026-09-13：同 TCP，接入 per-相机 FIFO 队列保证发送顺序。
                                _cameraOutWork[camIdx >= 0 && camIdx < 12 ? camIdx : 0].Enqueue(() =>
                                {
                                    try
                                    {
                                        noProtoOut.changeok(serialVal);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.WriteLog("相机" + (camIdx + 1) + " 串口输出失败: " + ex.Message);
                                    }
                                });
                            }
                            if (_comm.Omron.CanWriteResultOutput())
                            {
                                try
                                {
                                    fins = runOk ? myjob.block.Outputs["fins"].Value.ToString() : "Reject";
                                }
                                catch
                                {
                                    fins = "无";
                                }
                            }
                            if (_comm.Modbustcp.CanWriteResultOutput())
                            {
                                try
                                {
                                    modbustcps = runOk ? myjob.block.Outputs["modbustcp"].Value.ToString() : "Reject";
                                }
                                catch
                                {
                                    modbustcps = "无";
                                }
                            }
                            if (_comm.ModbusRtu.CanWriteResultOutput())
                            {
                                try
                                {
                                    modbusrtus = runOk ? myjob.block.Outputs["modbusrtu"].Value.ToString() : "Reject";
                                }
                                catch
                                {
                                    modbusrtus = "无";
                                }
                            }
                            // 阶段 5：检测结果只回写给“触发这次检测的那条连接”。
                            // triggerLinkId：0=非通讯触发（手动/连续采集，保持原广播行为），1=主连接（原逻辑不变），2~4=扩展连接。
                            // triggerProto：0=非通讯触发，1=FINS，2=ModbusTCP，3=ModbusRTU（与 linkId 配对唯一定位一条连接）。
                            // ★ 修复跨协议串扰（2026-09-06）：原实现只有 FINS 记录来源，Modbus TCP/RTU 的连接 2~4 触发时
                            //   triggerLinkId 为 0，结果被广播到所有协议的连接 1；且来源用后不清除会粘滞到下一次检测。
                            if (trigLink > 1 && trigProto > 0)
                            {
                                // 只回写给触发本次检测的那一条连接（协议 + 连接号），不广播、不串到别的协议。
                                // ★ 2026-09-13 修复：原来每帧独立 Task.Run 发送，A/B 两帧不保证获得发送锁的先后，
                                //   可能 B 先写、A 后写覆盖寄存器（结果顺序颠倒）。改为投递到既有 per-相机 FIFO 队列，
                                //   保证同一相机的结果严格按帧序发送（不涉及"可丢弃"策略）。
                                // ★F9：结果回写走专用队列（不丢），IO 脉冲仍走 _cameraOutWork（可丢弃语义不变）
                                _cameraResultWork[camIdx >= 0 && camIdx < 12 ? camIdx : 0].Enqueue(() =>
                                {
                                    try
                                    {
                                        if (trigProto == 1) _comm.Omron.WriteCameraOutputForLink(trigLink, camIdx, fins, !runOk);
                                        else if (trigProto == 2) _comm.Modbustcp.WriteCameraOutputForLink(trigLink, camIdx, modbustcps, !runOk);
                                        else if (trigProto == 3) _comm.ModbusRtu.WriteCameraOutputForLink(trigLink, camIdx, modbusrtus, !runOk);
                                    }
                                    catch (Exception ex) { _logger.WriteLog("相机" + (camIdx + 1) + " 连接(proto" + trigProto + ",link" + trigLink + ")输出失败: " + ex.Message); }
                                });
                            }
                            else
                            {
                                // ★ 三协议输出合并为单个后台任务顺序写（行为等价），减少线程池任务数
                                // ★F9：结果回写走专用队列（不丢）
                                _cameraResultWork[camIdx >= 0 && camIdx < 12 ? camIdx : 0].Enqueue(() =>
                                {
                                    try { _comm.Omron.WriteCameraOutput(camIdx, fins, !runOk); } catch (Exception ex) { _logger.WriteLog("相机" + (camIdx + 1) + " OMRON输出失败: " + ex.Message); }
                                    try { _comm.Modbustcp.WriteCameraOutput(camIdx, modbustcps, !runOk); } catch (Exception ex) { _logger.WriteLog("相机" + (camIdx + 1) + " ModbusTCP输出失败: " + ex.Message); }
                                    try { _comm.ModbusRtu.WriteCameraOutput(camIdx, modbusrtus, !runOk); } catch (Exception ex) { _logger.WriteLog("相机" + (camIdx + 1) + " ModbusRTU输出失败: " + ex.Message); }
                                });
                            }
                            if (runOk && myjob.zidongbaoguang && camIdx >= 0 && camIdx < 12 && _cameraCtrl.Cameras[camIdx] != null)
                            {
                                // ★ 按需创建（性能优先）：未启用自动曝光的相机不再每帧生成空转任务
                                // 在调用线程快照 UI 曝光基准与补偿输出值，避免后台任务跨线程读控件/VisionPro COM
                                // ★fix①②：① 基准曝光按本路相机取值（tbExposure[camIdx]），原恒取 tbExposure1 导致 2~12 路开自动曝光时
                                //   共用相机1的输入值（12 路各有独立 cameraN.exposure 配置，属长期漏写）；
                                //   ② 检测线程跨线程读控件包 try/catch（P3.1 Debug 开校验时避免 InvalidOperationException 冒泡到 getrecord
                                //   外层 catch 跳过整帧的统计/数据记录/存图），失败给配置默认值 1000。
                                string exposureText;
                                try { exposureText = tbExposure[camIdx].Text; }
                                catch { exposureText = "1000"; }
                                string buchangOut;
                                try { buchangOut = myjob.block.Outputs["buchang"].Value.ToString(); }
                                catch { buchangOut = "无"; }
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        float baseExposure;
                                        if (!float.TryParse(exposureText.Trim(), out baseExposure)) baseExposure = 0f;
                                        float compens;
                                        if (!float.TryParse(buchangOut, out compens)) compens = 0f;
                                        float baoguang_temp = baseExposure * (255 - compens / 255);
                                        _cameraCtrl.Cameras[camIdx].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                                        int nRet = _cameraCtrl.Cameras[camIdx].MV_CC_SetFloatValue_NET("ExposureTime", baoguang_temp);
                                        if (nRet != MyCamera.MV_OK)
                                        {
                                            _logger.WriteLog("Set Exposure Time Fail!+1+" + nRet);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.WriteLog("相机" + (camIdx + 1) + " 自动曝光调节失败: " + ex.Message);
                                    }
                                });
                            }
                            if (gongjujilu == 1 && (myjob.trrigerEn == true || myjob.triggerMode == "通讯触发"))
                            {
                                // ★ 按需创建（性能优先）：未启用工具记录的帧不再生成空转任务
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        gongjukuai(myjob.path_number, tempout1);
                                    }
                                    catch { }
                                });
                            }
                           
                            if (myjob.trrigerEn == true || myjob.triggerMode == "通讯触发" || cuntu == 1)
                            {
                                temptime = DateTime.Now.ToLongTimeString().ToString();
                            }
                            #region 总数计算
                            myjob.trriger = 0;
                            Interlocked.Increment(ref myjob.sum);
                            #endregion
                            // P2-1 fix: oksum/NG count are critical stats, not queueable (queue drop = miscount).
                            // Back to detection-thread sync; live COM myjob.block.Outputs snapshotted to string first.
                            string _outputSnap;
                            _outputSnap = tempout1;
                            try
                            {
                                if (_outputSnap == "Accept")
                                {
                                    Interlocked.Increment(ref myjob.oksum);
                                }
                                else
                                {
                                    if (_jobs.yunxing)
                                    {
                                        if (myjob.trrigerEn == true || myjob.triggerMode == "通讯触发")
                                        {
                                            int _n = int.Parse(myjob.path_number);
                                            // ★A fix：检测线程不再直读 WinForms 控件（P3.1 Debug 开跨线程校验时，读 CheckState/Items 会抛
                                            //  InvalidOperationException 被外层 catch 吞掉，连带跳过 out_end=8 与 rate 计算）。
                                            // 检测线程只快照所需字符串，把"读 CheckBox/ListBox + 写 ListBox/表格"整段收口到 UI 线程执行。
                                            string _recentMsg = String.Join(":", temptime, cuowu1, jobnumber);
                                            string _statErr = cuowu1 == null ? "" : cuowu1.ToString();
                                            void RecordRecentFailure(ListBox lb, CheckBox cb, Action<string, int> setbox)
                                            {
                                                if (cb.CheckState == CheckState.Checked)
                                                {
                                                    setbox(lb.Items[3].ToString(), 4);
                                                    setbox(lb.Items[2].ToString(), 3);
                                                    setbox(lb.Items[1].ToString(), 2);
                                                    setbox(lb.Items[0].ToString(), 1);
                                                    setbox(_recentMsg, 0);
                                                }
                                            }
                                            SafeBeginInvoke(() =>
                                            {
                                                try
                                                {
                                                    RecordRecentFailure(_recentListBox[_n - 1], _recentCheckBox[_n - 1], _recentSetbox[_n - 1]);
                                                    if (_n >= 9)
                                                    {
                                                        // 相机9-12 在原代码中额外写入第二组最近记录列表
                                                        RecordRecentFailure(_recentListBox2[_n - 9], _recentCheckBox2[_n - 9], _recentSetbox2[_n - 9]);
                                                    }
                                                    var _dgv = _statDgv[_n - 1];
                                                    var _d = _statD[_n - 1];
                                                    var _tbl = _jobs.Myjobs[_n - 1].myTable;
                                                    if (_statCheckBox[_n - 1].CheckState == CheckState.Checked)
                                                    {
                                                        _dgv.Visible = false;
                                                        UpdateJobErrorTable(_tbl, _d, _statErr);
                                                        _dgv.Visible = true;
                                                    }
                                                }
                                                catch { }
                                            });
                                            myjob.out_end = 8;
                                        }
                                    }
                                }
                                myjob.rate = myjob.oksum * 1.000f / myjob.sum;
                            }
                            catch (Exception ex)
                            {
                                _logger.WriteLog("统计" + ex.Message + "相机" + myjob.path_number);
                            }
                            if (_jobs.yunxing || !_jobs.yunxing)
                            {
                                if (datajilu == 1)   // ★ 前置开关（性能优先）：默认 datajilu=0 时不再每帧创建空转记录任务
                                {
                                // ★B fix：检测线程先快照 tianbiao 字符串（活读 block.Outputs），Task.Run 里不再活 COM，
                                // 切型释放 block 后残留任务不会访问已释放 COM 对象。
                                string _tianbiaoSnap;
                                try { _tianbiaoSnap = ""; for (int _ti = 0; _ti < myjob.block.Outputs.Count; _ti++) { if (myjob.block.Outputs[_ti].Name.Contains("ji")) _tianbiaoSnap += myjob.block.Outputs[_ti].Value + ","; } }
                                catch { _tianbiaoSnap = ""; }
                                Task.Run(() =>
                                {
                                    #region 统计
                                    if (datajilu == 1)
                                    {
                                        try
                                        {
                                            if (myjob.trrigerEn == true || myjob.triggerMode == "通讯触发")
                                            {
                                                if (tempout1 == "Accept")
                                                {
                                                    runlog1(1, 0, myjob.path_number);
                                                    myjob.tianbiao = _tianbiaoSnap;   // ★B fix: 用检测线程快照，不活读 COM
                                                    if (myjob.tianbiao.Contains(","))
                                                    {
                                                        myjob.tianbiao = myjob.tianbiao.Remove(myjob.tianbiao.Length - 1, 1);
                                                        runlog2(myjob.tianbiao, myjob.tianbiao, myjob.path_number, 1);
                                                    }
                                                }
                                                else
                                                {
                                                    runlog1(0, 1, myjob.path_number);
                                                    // ★ 2026-09-13 修复：同 Accept 分支，改用任务局部字符串，避免共享字段交错串帧。
                                                    string _tb = _tianbiaoSnap;
                                                    bool _tbHasComma = _tb.Contains(",");
                                                    if (_tbHasComma) _tb = _tb.Remove(_tb.Length - 1, 1);
                                                    myjob.tianbiao = _tb;
                                                    if (_tbHasComma) runlog2(_tb, _tb, myjob.path_number, 0);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            try
                                            {
                                                // 原代码此处写死 myjob1（原作者遗留笔误）：异常重建 CSV 时会去建"相机1"的目录、
                                                // 并套用"相机1"的表头，而紧跟其后的统计写入却用当前 myjob，前后不一致。
                                                // 各路 biaotou 在初始化时按自身 block.Outputs 独立赋值，故改为当前相机。
                                                _statistics.CreateDirectoryCsvPath(myjob.path_number);
                                                //  if (myjob.biaotou.Contains(","))
                                                _statistics.CreateCsvPath(myjob.path_number, myjob.biaotou);
                                                if (tempout1 == "Accept")
                                                {
                                                    runlog1(1, 0, myjob.path_number);
                                                    myjob.tianbiao = _tianbiaoSnap;   // ★B fix: 用检测线程快照，不活读 COM
                                                    if (myjob.tianbiao.Contains(","))
                                                        runlog2(myjob.tianbiao, myjob.tianbiao, myjob.path_number, 1);
                                                }
                                                else
                                                {
                                                    runlog1(0, 1, myjob.path_number);
                                                    myjob.tianbiao = _tianbiaoSnap;   // ★B fix: 用检测线程快照，不活读 COM
                                                    if (myjob.tianbiao.Contains(","))
                                                        runlog2(myjob.tianbiao, myjob.tianbiao, myjob.path_number, 0);
                                                }
                                            }
                                            catch { }
                                            _logger.WriteLog(ex.Message + "相机" + myjob.path_number + "-22");
                                        };
                                    }
                                    #endregion
                                });
                                }
                            }
                            if (myjob.xuanran && !myjob.roi)
                            {
                                // ★ 渲染节流（性能优先）：每相机至多 1 个渲染请求在飞。
                                //   UI 尚未消化完上一帧渲染时本帧直接丢弃（允许漏显示中间帧），
                                //   避免高频下渲染委托在 UI 消息队列无限积压、拖垮界面线程与 CPU。
                                // ★ 显示降载（2026-09-19）：单飞之上再叠全局速率门控（两次渲染启动最小间隔，
                                //   code.ini [Display]RenderMinIntervalMs 可调），给光栅化总量封顶；
                                //   每相机饥饿计数防某路被高频路永久挤占。
                                int slotRender = int.Parse(myjob.path_number) - 1;
                                if (slotRender >= 0 && slotRender < 12
                                    && Interlocked.CompareExchange(ref _renderBusy[slotRender], 1, 0) == 0)
                                {
                                    if (!TryClaimRenderBudget(slotRender))
                                    {
                                        Volatile.Write(ref _renderBusy[slotRender], 0);
                                    }
                                    else if (_displayRawImage)
                                    {
                                        // ★ 原图快速路径：UI 只换 PictureBox 的 Image，跳过 record 深拷贝与 overlay 光栅化。
                                        //   像素在检测线程自包含复制（与存图同纪律），UI 不碰 VisionPro COM。
                                        Bitmap rawCopy = null;
                                        try
                                        {
                                            if (camIdx >= 0 && camIdx < 12 && bmp[camIdx] != null)
                                            {
                                                Bitmap src = bmp[camIdx];
                                                rawCopy = src.Clone(new Rectangle(0, 0, src.Width, src.Height), src.PixelFormat);
                                            }
                                        }
                                        catch (Exception exRaw)
                                        {
                                            _logger.WriteLog("相机" + (slotRender + 1) + " 原图拷贝失败: " + exRaw.Message);
                                            rawCopy = null;
                                        }
                                        if (rawCopy == null)
                                        {
                                            Volatile.Write(ref _renderBusy[slotRender], 0);
                                        }
                                        else if (!TryBeginInvoke(() => ShowRawFrame(slotRender, rawCopy)))
                                        {
                                            rawCopy.Dispose();
                                            Volatile.Write(ref _renderBusy[slotRender], 0);
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            // ★ 仅在真正需要渲染本帧时才生成运行记录（原代码无论渲染开关、无论本帧是否被节流丢弃，每帧都执行 CreateLastRunRecord）
                                            ICogRecord rec = myjob.block.CreateLastRunRecord().SubRecords[0];
                                            if (!TryBeginInvoke(() => RenderCameraFrame(slotRender, rec, myjob, camIdx)))
                                                Volatile.Write(ref _renderBusy[slotRender], 0);
                                        }
                                        catch (Exception exRender)
                                        {
                                            // ★ 修复：创建记录或投递失败时必须释放单飞标志。
                                            //   标志只在 RenderCameraFrame 内部复位，若此处抛异常则渲染函数根本不会被调用，
                                            //   该相机后续所有帧都会被 CompareExchange 挡住 —— 表现为这一路永久停止刷新。
                                            Volatile.Write(ref _renderBusy[slotRender], 0);
                                            _logger.WriteLog("相机" + (slotRender + 1) + " 渲染记录创建/投递失败: " + exRender.Message);
                                        }
                                    }
                                }
                            }
                            if ((myjob.cuntu || cuntu == 1) && _jobs.yunxing)
                            {
                                // ★ 性能优先策略（2026-09-04）：存图改"每相机单飞 + 忙则丢"。
                                //   同一相机同时至多 1 张在后台写盘；上一张未写完时本帧存图直接丢弃（允许漏存），
                                //   避免线程池任务积压与追赶式写盘拉高 CPU 峰值、避免无界 COM 图像对象滞留。
                                //   帧像素在检测线程此刻（bmp[camIdx] 仍有效）自包含复制，后台写盘不再依赖
                                //   block.Inputs 的引用生命周期（消除旧 tempimage 被下帧 AssignBlockInputImage 释放的竞态）。
                                bool gate = myjob.trrigerEn == true || myjob.triggerMode == "通讯触发" || cuntu == 1;
                                bool wantOk = gate && myjob.cunok && (tempout3 == "空" ? tempout1 == "Accept" : tempout3 == "Accept");
                                bool wantNg = gate && myjob.cunng && (tempout3 == "空" ? tempout1 != "Accept" : tempout3 == "Reject");
                                if ((wantOk || wantNg) && camIdx >= 0 && camIdx < 12
                                    && Interlocked.CompareExchange(ref _saveFlying[camIdx], 1, 0) == 0)
                                {
                                    Bitmap saveCopy = null;
                                    try
                                    {
                                        if (bmp[camIdx] != null)
                                        {
                                            // ★ 存图修复：Bitmap.Clone(Rectangle, PixelFormat) 为真深拷贝，
                                            //   生成完全独立的像素副本，与源 Bitmap 生命周期解耦；对黑白 Mono8
                                            //   (Format8bppIndexed) 与彩色(24bpp) 均有效。
                                            //   原 DrawImage 方案对索引格式调用 Graphics.FromImage 每帧必抛
                                            //   （GDI+ 不允许为 8bppIndexed 创建 Graphics），导致黑白相机存图静默失败。
                                            Bitmap src = bmp[camIdx];
                                            saveCopy = src.Clone(new Rectangle(0, 0, src.Width, src.Height), src.PixelFormat);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.WriteLog("相机" + (camIdx + 1) + " 存图拷贝失败: " + ex.Message);
                                        saveCopy = null;
                                    }
                                    if (saveCopy == null)
                                    {
                                        Volatile.Write(ref _saveFlying[camIdx], 0);
                                    }
                                    else
                                    {
                                        Task.Run(() =>
                                        {
                                            try
                                            {
                                                try
                                                {
                                                    if (!myjob.fileok.Directory.Exists)
                                                    {
                                                        Directory.CreateDirectory(myjob.pathhead_ok + day1 + "\\");
                                                    }
                                                    if (!myjob.fileng.Directory.Exists)
                                                    {
                                                        Directory.CreateDirectory(myjob.pathhead_ng + day1 + "\\");
                                                    }
                                                    using (saveCopy)
                                                    {
                                                        // wrapper 引用 saveCopy 像素（只读），同一帧内 OK/NG 共用；写完后一并释放
                                                        // ICogImage 接口未实现 IDisposable，故按具体类型分别 using
                                                        Action<ICogImage> doSave = imgSave =>
                                                        {
                                                            if (wantOk)
                                                            {
                                                                DirectoryInfo dirOk = GetOkDirInfo(camIdx);
                                                                if (myjob.ok1 == 0)
                                                                {
                                                                    myjob.ok1 = 1;
                                                                    try
                                                                    {
                                                                        cuntu_fangfa(tempfilebmp, dirOk != null ? dirOk.GetFileSystemInfos().Length : 0, jobnumber, myjob.pathhead_ok, "OK", temptime, imgSave);
                                                                    }
                                                                    finally { myjob.ok1 = 0; }
                                                                }
                                                            }
                                                            if (wantNg)
                                                            {
                                                                DirectoryInfo dirNg = GetNgDirInfo(camIdx);
                                                                if (myjob.ng1 == 0)
                                                                {
                                                                    myjob.ng1 = 1;
                                                                    try
                                                                    {
                                                                        cuntu_fangfa(tempfilebmp, dirNg != null ? dirNg.GetFileSystemInfos().Length : 0, jobnumber, myjob.pathhead_ng, cuowu1, temptime, imgSave);
                                                                    }
                                                                    finally { myjob.ng1 = 0; }
                                                                }
                                                            }
                                                        };
                                                        if (myjob.Color)
                                                        {
                                                            using (var imgSave = new CogImage24PlanarColor(saveCopy))
                                                                doSave(imgSave);
                                                        }
                                                        else
                                                        {
                                                            using (var imgSave = new CogImage8Grey(saveCopy))
                                                                doSave(imgSave);
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    // ★低风险修复：原实现跨通道清全部 12 路的 ok1/ng1——
                                                    //   一台相机存图失败会误清其它 11 路尚未处理的输出标志。
                                                    //   与上方 finally 一致，只清本路。
                                                    myjob.ng1 = 0;
                                                    myjob.ok1 = 0;
                                                    _logger.WriteLog(ex.Message + "相机" + myjob.path_number + "存图");
                                                }
                                            }
                                            finally
                                            {
                                                Volatile.Write(ref _saveFlying[camIdx], 0);
                                            }
                                        });
                                    }
                                }
                            }

                        }
                        else
                        {
                            // switch (myjob.path_number)
                            //  {
                            // case "1":
                            if (!myjob.trrigerEn)
                            {
                                _jobs.myjob1.danwu_cishu++;
                                // ★ 2026-09-05：同步 Invoke 改异步，避免"未触发"高频帧每帧阻塞检测线程
                                string dwcTxt = _jobs.myjob1.danwu_cishu.ToString();
                                SafeBeginInvoke(() =>
                                {
                                    label133.Text = dwcTxt;
                                });
                            }
                            //   break;
                            // }

                        }
                        // Thread.Sleep(1);
                    }
                }
            }
            catch (Exception ex)
            {

                _logger.WriteLog(ex.Message + "相机" + myjob.path_number);
            };
        }

        public int zhuanhuan = 0;

        #endregion
        #endregion
        #region 界面监控与统计
        /// <summary>
        /// 界面监控：35ms 周期刷新各相机状态/丢帧数/统计显示
        /// </summary>
        private void UI_monitor()
        {
            while (!_disposingFlag)
            {
                Thread.Sleep(35);
                SampleLostFrames(); // ★P2-2：后台线程采样丢帧数（UI 线程不再 P/Invoke）
                if (_disposingFlag) break;
                if (zhuanhuan == 0)
                {
                    zhuanhuan = 1;
                    if (tongji == 1)
                    {

                        SafeBeginInvoke(() =>
                        {
                            if (manager1 == null) return;
                            for (int _i = 0; _i < 12; _i++)
                            {
                                if ((_i == 0 || manager1.JobCount > _i) && _jobs.Myjobs[_i].triggerMode != "连续运行" && listBox2.Items.Count >= 6 + _i * 6)
                                {
                                    int _base = 1 + _i * 6;
                                    listBox2.Items[_base] = "检测数:" + _jobs.Myjobs[_i].sum.ToString();
                                    listBox2.Items[_base + 1] = "OK数:" + _jobs.Myjobs[_i].oksum.ToString();
                                    listBox2.Items[_base + 2] = "NG数:" + (_jobs.Myjobs[_i].sum - _jobs.Myjobs[_i].oksum).ToString();
                                    listBox2.Items[_base + 3] = "合格率:" + _jobs.Myjobs[_i].rate.ToString("F3");
                                }
                            }
                        });
                    }
                }
                else
                {
                    zhuanhuan = 0;

                    SafeBeginInvoke(() =>
                    {
                        if (tongji == 1)
                        {
                            if (_jobs.myjob1.shijianEn && m_nCanOpenDeviceNum > 0)
                            {
                                label153.Text = _jobs.myjob1.outputok2.ToString();
                                // label71.Text = _jobs.myjob1.outputok.ToString();
                                label85.Text = zhen;
                                label86.Text = _lostFrameCache[0];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label134.Text = (m_nFrames[0] - _jobs.myjob1.sum).ToString();
                            }
                            if (_jobs.myjob2.shijianEn && m_nCanOpenDeviceNum > 1)
                            {
                                label62.Text = _jobs.myjob2.address;
                                label52.Text = _lostFrameCache[1];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label49.Text = (m_nFrames[1] - _jobs.myjob2.sum).ToString();
                            }
                            if (_jobs.myjob3.shijianEn && m_nCanOpenDeviceNum > 2)
                            {
                                label23.Text = _jobs.myjob3.address;
                                label53.Text = _lostFrameCache[2];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label50.Text = (m_nFrames[2] - _jobs.myjob3.sum).ToString();
                            }
                            if (_jobs.myjob4.shijianEn && m_nCanOpenDeviceNum > 3)
                            {
                                label16.Text = _jobs.myjob4.address;
                                label54.Text = _lostFrameCache[3];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label51.Text = (m_nFrames[3] - _jobs.myjob4.sum).ToString();
                            }
                            if (_jobs.myjob5.shijianEn && m_nCanOpenDeviceNum > 4)
                            {
                                label93.Text = _jobs.myjob5.address;
                                label90.Text = _lostFrameCache[4];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label135.Text = (m_nFrames[4] - _jobs.myjob5.sum).ToString();
                            }
                            if (_jobs.myjob6.shijianEn && m_nCanOpenDeviceNum > 5)
                            {
                                label103.Text = _jobs.myjob6.address;
                                label100.Text = _lostFrameCache[5];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label136.Text = (m_nFrames[5] - _jobs.myjob6.sum).ToString();
                            }
                            if (_jobs.myjob7.shijianEn && m_nCanOpenDeviceNum > 6)
                            {
                                label114.Text = _jobs.myjob7.address;
                                label110.Text = _lostFrameCache[6];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label137.Text = (m_nFrames[6] - _jobs.myjob7.sum).ToString();
                            }
                            if (_jobs.myjob8.shijianEn && m_nCanOpenDeviceNum > 7)
                            {
                                label124.Text = _jobs.myjob8.address;
                                label121.Text = _lostFrameCache[7];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label138.Text = (m_nFrames[7] - _jobs.myjob8.sum).ToString();
                            }
                            if (_jobs.myjob9.shijianEn && m_nCanOpenDeviceNum > 8)
                            {
                                label235.Text = _jobs.myjob9.address;
                                label236.Text = _lostFrameCache[8];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label237.Text = (m_nFrames[8] - _jobs.myjob9.sum).ToString();
                            }
                            if (_jobs.myjob10.shijianEn && m_nCanOpenDeviceNum > 9)
                            {
                                label241.Text = _jobs.myjob10.address;
                                label242.Text = _lostFrameCache[9];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label243.Text = (m_nFrames[9] - _jobs.myjob10.sum).ToString();
                            }
                            if (_jobs.myjob11.shijianEn && m_nCanOpenDeviceNum > 10)
                            {
                                label247.Text = _jobs.myjob11.address;
                                label248.Text = _lostFrameCache[10];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label249.Text = (m_nFrames[10] - _jobs.myjob11.sum).ToString();
                            }
                            if (_jobs.myjob12.shijianEn && m_nCanOpenDeviceNum > 11)
                            {
                                label253.Text = _jobs.myjob12.address;
                                label254.Text = _lostFrameCache[11];   // ★P2-2 读后台采样缓存（原 GetLostFrame 每圈 P/Invoke）
                                label255.Text = (m_nFrames[11] - _jobs.myjob12.sum).ToString();
                            }
                            label74.Text = cameraState;
                            label77.Text = daoqi;
                            // 过期警告弹窗（只弹一次）
                            if (!string.IsNullOrEmpty(daoqi) && !daoqi_popup_shown)
                            {
                                daoqi_popup_shown = true;
                                MessageBox.Show(daoqi + "\n\n请尽快联系厂家续期！", "授权到期提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            try
                            {
                                label14.Text = frm3.monitor;
                                label15.Text = _jobs.myjob1.time.ToString("F2");
                                label24.Text = _jobs.myjob2.time.ToString("F2");
                                label31.Text = _jobs.myjob3.time.ToString("F2");
                                label39.Text = _jobs.myjob4.time.ToString("F2");
                                label8.Text = _jobs.myjob1.runtime.ToString("F2");
                                label19.Text = _jobs.myjob2.runtime.ToString("F2");
                                label30.Text = _jobs.myjob3.runtime.ToString("F2");
                                label29.Text = _jobs.myjob4.runtime.ToString("F2");
                                label128.Text = _jobs.myjob5.runtime.ToString("F2");
                                label129.Text = _jobs.myjob6.runtime.ToString("F2");
                                label132.Text = _jobs.myjob7.runtime.ToString("F2");
                                label131.Text = _jobs.myjob8.runtime.ToString("F2");
                                label130.Text = _jobs.myjob9.runtime.ToString("F2");
                                label232.Text = _jobs.myjob10.runtime.ToString("F2");
                                label233.Text = _jobs.myjob11.runtime.ToString("F2");
                                label234.Text = _jobs.myjob12.runtime.ToString("F2");
                                // 更新监控画面总耗时标签
                                for (int _ti = 0; _ti < 12; _ti++)
                                {
                                    if (monitorTotalTimeLabels != null && monitorTotalTimeLabels[_ti] != null)
                                    {
                                        long t = 0;
                                        t = _jobs.Myjobs[_ti].time;
                                        monitorTotalTimeLabels[_ti].Text = "总:" + t.ToString() + "ms";
                                    }
                                }
                                label58.Text = _jobs.myjob1.runcishu.ToString();
                                label59.Text = _jobs.myjob2.runcishu.ToString();
                                label60.Text = _jobs.myjob3.runcishu.ToString();
                                label61.Text = _jobs.myjob4.runcishu.ToString();
                            }
                            catch { }
                        }
                        if (_jobs.yunxing == true)
                        {
                            
                            打开ToolStripMenuItem.Enabled = false;
                            保存ToolStripMenuItem.Enabled = false;
                            另存为ToolStripMenuItem.Enabled = false;
                            rOI设置ToolStripMenuItem.CheckState = CheckState.Unchecked;
                        }
                        else
                        {
                            
                            打开ToolStripMenuItem.Enabled = true;
                            保存ToolStripMenuItem.Enabled = true;
                            另存为ToolStripMenuItem.Enabled = true;
                        }
                        if (frm5.mark == 1)
                        {
                            设置ToolStripMenuItem.Enabled = true;
                            numericUpDown22.Enabled = true;
                            button31.Enabled = true;
                        }
                        else
                        {
                            设置ToolStripMenuItem.Enabled = false;
                            numericUpDown22.Enabled = false;
                            button31.Enabled = false;
                            textBox3.Enabled = false;
                            comboBox1.Enabled = false;
                            comboBox4.Enabled = false;
                            comboBox5.Enabled = false;
                            comboBox8.Enabled = false;
                            comboBox25.Enabled = false;
                            comboBox28.Enabled = false;
                            comboBox31.Enabled = false;
                            comboBox34.Enabled = false;
                            comboBox43.Enabled = false;
                            comboBox47.Enabled = false;
                            comboBox51.Enabled = false;
                            comboBox55.Enabled = false;
                            触发设置ToolStripMenuItem.Checked = false;
                            输出时间ToolStripMenuItem.Checked = false;
                            存图测试1ToolStripMenuItem.Checked = false;

                            rOI设置ToolStripMenuItem.Checked = false;
                        }
                    });
                }
            }
        }

        /// <summary>
        /// UI 线程执行单相机渲染（原 getrecord 内联的渲染块提取）。
        /// 由 _renderBusy 门控保证每相机同时至多 1 个渲染请求；无论成败 finally 都会释放标志。
        /// </summary>
        private void RenderCameraFrame(int slot, ICogRecord temprecord, Myjob myjob, int camIdx)
        {
            try
            {
                Cognex.VisionPro.CogRecordDisplay _cd = _cogDisplay[slot];
                var _mj = _jobs.Myjobs[slot];
                try
                {
                    _cd.DrawingEnabled = false;
                    _cd.Record = temprecord;
                    _cd.BackColor = Color.FromArgb(255, 60, 60, 60);
                    _cd.DrawingEnabled = true;
                    // ★ 降载：Refresh() 强制 UI 线程同步光栅化完才返回；Invalidate() 回到消息泵异步绘制，
                    //   高频下多次失效自动合并成一次绘制。
                    _cd.Invalidate();
                    if (_mj.fit == 0)
                    {
                        _cd.Fit(true);
                        _mj.fit = 1;
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message + "相机" + (slot + 1) + "record");
                }
                try
                {
                    if (camIdx >= 0 && camIdx < f9.Length && f9[camIdx] != null)
                    {
                        if (f9[camIdx].Inspect1 != null)
                        {
                            if (f9[camIdx].input_tu == null)
                            {
                                //   MessageBox.Show(f9[camIdx].Inspect1.CreateCurrentRecord().SubRecords.IndexOfKey(f9[camIdx].comboBox1.Text).ToString());
                                f9[camIdx].input_tu = f9[camIdx].Inspect1.CreateCurrentRecord().SubRecords["TrainedPatternImage"];
                                f9[camIdx].trian_tu = f9[camIdx].Inspect1.CreateLastRunRecord().SubRecords[f9[camIdx].comboBox1.Text];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // MessageBox.Show( f9[0].Inspect1.CreateLastRunRecord().SubRecords[2].RecordKey);
                    _logger.WriteLog("缺陷:" + ex.Message);
                }
            }
            finally
            {
                Volatile.Write(ref _renderBusy[slot], 0);
            }
        }

        /// <summary>
        /// 全局渲染速率门控（检测线程并发调用，全 Interlocked/Volatile，无锁）。
        /// 两次渲染启动之间不足最小间隔则拒绝本次（走丢帧路径），给 UI 光栅化总量封顶；
        /// UI 线程本身串行，无需再加互斥。被拒相机累计饥饿计数，超阈值放行一次，
        /// 防止低频相机（如通讯触发路）被高频连续采集路永久挤占显示。
        /// Environment.TickCount 为 int 毫秒，差值运算对回绕自洽。
        /// </summary>
        private bool TryClaimRenderBudget(int slot)
        {
            int now = Environment.TickCount;
            int interval = _renderMinIntervalMs;
            // 交互优先：最近 120ms 内有鼠标/键盘活动时，把限速临时放宽到交互间隔
            //（仅当交互间隔更小时生效；interval=0 表示不限速，不参与）
            if (interval > 0
                && _renderInteractiveIntervalMs < interval
                && unchecked(now - _lastUserTick) < 120)
                interval = _renderInteractiveIntervalMs;
            if (interval > 0
                && unchecked(now - Volatile.Read(ref _lastRenderStartTick)) < interval
                && Volatile.Read(ref _renderStarve[slot]) < 40)
            {
                Volatile.Write(ref _renderStarve[slot], Volatile.Read(ref _renderStarve[slot]) + 1);
                return false;
            }
            Volatile.Write(ref _renderStarve[slot], 0);
            Volatile.Write(ref _lastRenderStartTick, now);
            return true;
        }

        /// <summary>
        /// 只读消息过滤器：仅记录最后一次用户交互的 tick，永不吞消息（恒返回 false）。
        /// PreFilterMessage 在 UI 线程执行；_lastUserTick 为 volatile，检测线程直接读。
        /// </summary>
        private sealed class UserActivityFilter : IMessageFilter
        {
            private readonly Form1 _owner;
            public UserActivityFilter(Form1 owner) { _owner = owner; }
            // ★ 必须写全名：本工程 Datas.cs 里有自定义 class Message（同命名空间会遮蔽），
            //   写 ref Message 会解析到它 → 不匹配 IMessageFilter.PreFilterMessage → CS0535 编译失败。
            public bool PreFilterMessage(ref System.Windows.Forms.Message m)
            {
                switch (m.Msg)
                {
                    case 0x0200: // WM_MOUSEMOVE
                    case 0x0201: // WM_LBUTTONDOWN
                    case 0x0204: // WM_RBUTTONDOWN
                    case 0x00A1: // WM_NCLBUTTONDOWN
                    case 0x0100: // WM_KEYDOWN
                    case 0x020A: // WM_MOUSEWHEEL
                        _owner._lastUserTick = Environment.TickCount;
                        break;
                }
                return false;
            }
        }

        /// <summary>
        /// 原图快速路径 UI 侧：直接换 PictureBox 的 Image，不走 VisionPro record/光栅化。
        /// 控件首帧惰性创建，与对应 CogRecordDisplay 同父容器同位置同锚定，盖在其上。
        /// 单飞标志在 finally 释放；未成功交给控件的位图由本方法负责 Dispose。
        /// </summary>
        private void ShowRawFrame(int slot, Bitmap frame)
        {
            bool assigned = false;
            try
            {
                PictureBox pb = _rawBox[slot];
                if (pb == null)
                {
                    pb = new PictureBox();
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                    pb.BackColor = Color.FromArgb(255, 60, 60, 60);
                    var cd = _cogDisplay[slot];
                    var tlp = cd.Parent as TableLayoutPanel;
                    if (tlp != null)
                    {
                        // ★H1 修复（RawImageMode 错位）：父容器是 TableLayoutPanel 时，
                        //   必须用 Add(control,col,row) 直接放入 cd 所在单元格并 Dock=Fill——
                        //   原实现只设 Parent+Bounds：TLP 布局器会无视手动 Bounds，
                        //   并把 pb 塞进自动分配的新单元格，导致画面跑到错误位置并挤压原布局。
                        var cell = tlp.GetCellPosition(cd);
                        tlp.Controls.Add(pb, cell.Column, cell.Row);
                        pb.Dock = DockStyle.Fill;
                    }
                    else
                    {
                        pb.Parent = cd.Parent;
                        pb.Bounds = cd.Bounds;
                        pb.Anchor = cd.Anchor;
                    }
                    pb.BringToFront();   // 覆盖在 CogRecordDisplay 之上
                    _rawBox[slot] = pb;
                }
                pb.Visible = true;
                var old = pb.Image as Bitmap;
                pb.Image = frame;
                assigned = true;
                if (old != null) old.Dispose();
            }
            catch (Exception ex)
            {
                if (!assigned) frame.Dispose();
                _logger.WriteLog("相机" + (slot + 1) + " 原图显示失败: " + ex.Message);
            }
            finally
            {
                Volatile.Write(ref _renderBusy[slot], 0);
            }
        }

        /// <summary>
        /// 安全执行 BeginInvoke，防止窗口句柄销毁时抛出异常
        /// </summary>
        private void SafeBeginInvoke(Action action)
        {
            TryBeginInvoke(action);
        }

        private bool TryBeginInvoke(Action action)
        {
            try
            {
                if (_disposingFlag || !IsHandleCreated || IsDisposed) return false;
                BeginInvoke(action);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 安全执行 BeginInvoke（接受自定义委托）
        /// </summary>
        private void SafeBeginInvoke(Delegate method, params object[] args)
        {
            try
            {
                if (!_disposingFlag && this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(method, args);
                }
            }
            catch (InvalidOperationException)
            {
                // 窗口句柄已销毁或正在释放，静默忽略
            }
            catch
            {
                // 其他异常不做处理
            }
        }

        private object locker_1 = new object();
        private object locker_2 = new object();
        private object locker_3 = new object();
        private object locker_4 = new object();
        private object locker_5 = new object();
        private object locker_6 = new object();
        private object locker_7 = new object();
        private object locker_8 = new object();
        private object locker = new object();
        private object locker1 = new object();
        private object locker2 = new object();
        private object locker3 = new object();
        private object locker4 = new object();
        private object locker5 = new object();
        private object locker6 = new object();
        private object locker7 = new object();
        private object locker8 = new object();
        private object locker9 = new object();
        private object locker10 = new object();
        private object locker11 = new object();
        private object locker12 = new object();
        private object lockern = new object();
        private object locker1ng = new object();

        private void runlog1(int a, int b, string path)
        {
            lock (locker)
            {
                _statistics.WriteDate(a, b, path);

            }
        }

        #endregion
        #region IO 输出控制
        /// <summary>
        /// 相机1 OK 线输出（由 outputok 属性赋值触发，异步线程执行）
        /// </summary>
        public void IO1OK()
        {
            try
            {
                if (_jobs.myjob1.outputok == 0)
                {
                    output_camera(1, false);
                    SafeBeginInvoke(() => {
                        button55.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob1.outputok >= 1)
                {
                    if (_jobs.myjob1.outputok2 == 1)
                    {
                        if (_jobs.myjob1.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob1.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob1.outputok >= 1 && _jobs.myjob1.outputok2 == 1)
                        {
                            output_camera(1, true);
                            SafeBeginInvoke(() => {
                                button55.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLog("IO1OK异常: " + ex.Message);
            }
        }
        public void IO2OK()
        {
            try
            {
                if (_jobs.myjob2.outputok == 0)
                {
                    output_camera2(1, false);
                    SafeBeginInvoke(() => {
                        button59.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob2.outputok >= 1)
                {
                    if (_jobs.myjob2.outputok2 == 1)
                    {
                        if (_jobs.myjob2.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob2.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob2.outputok >= 1 && _jobs.myjob2.outputok2 == 1)
                        {
                            output_camera2(1, true);
                            SafeBeginInvoke(() => {
                                button59.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLog("IO2OK异常: " + ex.Message);
            }
        }
        public void IO3OK()
        {
            try
            {
                if (_jobs.myjob3.outputok == 0)
                {
                    output_camera3(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button61.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob3.outputok >= 1)
                {
                    if (_jobs.myjob3.outputok2 == 1)
                    {
                        if (_jobs.myjob3.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob3.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob3.outputok >= 1 && _jobs.myjob3.outputok2 == 1)
                        {
                            output_camera3(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button61.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO3OK异常: " + ex.Message); }
        }
        public void IO4OK()
        {
            try
            {
                if (_jobs.myjob4.outputok == 0)
                {
                    output_camera4(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button63.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob4.outputok >= 1)
                {
                    if (_jobs.myjob4.outputok2 == 1)
                    {
                        if (_jobs.myjob4.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob4.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob4.outputok >= 1 && _jobs.myjob4.outputok2 == 1)
                        {
                            output_camera4(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button63.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO4OK异常: " + ex.Message); }
        }
        public void IO5OK()
        {
            try
            {
                if (_jobs.myjob5.outputok == 0)
                {
                    output_camera5(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button68.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob5.outputok >= 1)
                {
                    if (_jobs.myjob5.outputok2 == 1)
                    {
                        if (_jobs.myjob5.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob5.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob5.outputok >= 1 && _jobs.myjob5.outputok2 == 1)
                        {
                            output_camera5(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button68.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO5OK异常: " + ex.Message); }
        }
        public void IO6OK()
        {
            try
            {
                if (_jobs.myjob6.outputok == 0)
                {
                    output_camera6(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button71.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob6.outputok >= 1)
                {
                    if (_jobs.myjob6.outputok2 == 1)
                    {
                        if (_jobs.myjob6.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob6.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob6.outputok >= 1 && _jobs.myjob6.outputok2 == 1)
                        {
                            output_camera6(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button71.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO6OK异常: " + ex.Message); }
        }
        public void IO7OK()
        {
            try
            {
                if (_jobs.myjob7.outputok == 0)
                {

                    output_camera7(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button73.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob7.outputok >= 1)
                {
                    if (_jobs.myjob7.outputok2 == 1)
                    {
                        if (_jobs.myjob7.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob7.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob7.outputok >= 1 && _jobs.myjob7.outputok2 == 1)
                        {
                            output_camera7(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button73.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO7OK异常: " + ex.Message); }
        }
        public void IO8OK()
        {
            try
            {
                if (_jobs.myjob8.outputok == 0)
                {
                    output_camera8(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button75.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob8.outputok >= 1)
                {
                    if (_jobs.myjob8.outputok2 == 1)
                    {
                        if (_jobs.myjob8.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob8.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob8.outputok >= 1 && _jobs.myjob8.outputok2 == 1)
                        {
                            output_camera8(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button75.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO8OK异常: " + ex.Message); }
        }
        public void IO9OK()
        {
            try
            {
                if (_jobs.myjob9.outputok == 0)
                {
                    output_camera9(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button87.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob9.outputok >= 1)
                {
                    if (_jobs.myjob9.outputok2 == 1)
                    {
                        if (_jobs.myjob9.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob9.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob9.outputok >= 1 && _jobs.myjob9.outputok2 == 1)
                        {
                            output_camera9(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button87.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO9OK异常: " + ex.Message); }
        }
        public void IO10OK()
        {
            try
            {
                if (_jobs.myjob10.outputok == 0)
                {
                    output_camera10(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button94.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob10.outputok >= 1)
                {
                    if (_jobs.myjob10.outputok2 == 1)
                    {
                        if (_jobs.myjob10.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob10.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob10.outputok >= 1 && _jobs.myjob10.outputok2 == 1)
                        {
                            output_camera10(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button94.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO10OK异常: " + ex.Message); }
        }
        public void IO11OK()
        {
            try
            {
                if (_jobs.myjob11.outputok == 0)
                {
                    output_camera11(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button101.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob11.outputok >= 1)
                {
                    if (_jobs.myjob11.outputok2 == 1)
                    {
                        if (_jobs.myjob11.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob11.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob11.outputok >= 1 && _jobs.myjob11.outputok2 == 1)
                        {
                            output_camera11(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button101.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO11OK异常: " + ex.Message); }
        }
        public void IO12OK()
        {
            try
            {
                if (_jobs.myjob12.outputok == 0)
                {
                    output_camera12(1, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button108.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob12.outputok >= 1)
                {
                    if (_jobs.myjob12.outputok2 == 1)
                    {
                        if (_jobs.myjob12.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob12.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob12.outputok >= 1 && _jobs.myjob12.outputok2 == 1)
                        {
                            output_camera12(1, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button108.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO12OK异常: " + ex.Message); }
        }
        public void IO1NG()
        {
            try
            {
                if (_jobs.myjob1.outputng == 0)
                {
                    output_camera1ng(2, false);
                    SafeBeginInvoke(() => {
                        button56.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob1.outputng >= 1)
                {
                    if (_jobs.myjob1.outputng2 == 1)
                    {
                        if (_jobs.myjob1.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob1.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob1.outputng >= 1 && _jobs.myjob1.outputng2 == 1)
                        {
                            output_camera1ng(2, true);
                            SafeBeginInvoke(() => {
                                button56.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLog("IO1NG异常: " + ex.Message);
            }
        }
        public void IO2NG()
        {
            try
            {
                if (_jobs.myjob2.outputng == 0)
                {
                    output_camera2(2, false);
                    SafeBeginInvoke(() => {
                        button58.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob2.outputng >= 1)
                {
                    if (_jobs.myjob2.outputng2 == 1)
                    {
                        if (_jobs.myjob2.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob2.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob2.outputng >= 1 && _jobs.myjob2.outputng2 == 1)
                        {
                            output_camera2(2, true);
                            SafeBeginInvoke(() => {
                                button58.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.WriteLog("IO2NG异常: " + ex.Message);
            }
        }
        public void IO3NG()
        {
            try
            {
                if (_jobs.myjob3.outputng == 0)
                {
                    output_camera3(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button60.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob3.outputng >= 1)
                {
                    if (_jobs.myjob3.outputng2 == 1)
                    {
                        if (_jobs.myjob3.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob3.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob3.outputng >= 1 && _jobs.myjob3.outputng2 == 1)
                        {
                            output_camera3(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button60.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO3NG异常: " + ex.Message); }
        }
        public void IO4NG()
        {
            try
            {
                if (_jobs.myjob4.outputng == 0)
                {
                    output_camera4(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button62.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob4.outputng >= 1)
                {
                    if (_jobs.myjob4.outputng2 == 1)
                    {
                        if (_jobs.myjob4.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob4.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob4.outputng >= 1 && _jobs.myjob4.outputng2 == 1)
                        {
                            output_camera4(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button62.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO4NG异常: " + ex.Message); }
        }
        public void IO5NG()
        {
            try
            {
                if (_jobs.myjob5.outputng == 0)
                {
                    output_camera5(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button64.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob5.outputng >= 1)
                {
                    if (_jobs.myjob5.outputng2 == 1)
                    {
                        if (_jobs.myjob5.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob5.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob5.outputng >= 1 && _jobs.myjob5.outputng2 == 1)
                        {
                            output_camera5(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button64.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO5NG异常: " + ex.Message); }
        }
        public void IO6NG()
        {
            try
            {
                if (_jobs.myjob6.outputng == 0)
                {
                    output_camera6(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button69.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob6.outputng >= 1)
                {
                    if (_jobs.myjob6.outputng2 == 1)
                    {
                        if (_jobs.myjob6.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob6.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob6.outputng >= 1 && _jobs.myjob6.outputng2 == 1)
                        {
                            output_camera6(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button69.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO6NG异常: " + ex.Message); }
        }
        public void IO7NG()
        {
            try
            {
                if (_jobs.myjob7.outputng == 0)
                {
                    output_camera7(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button72.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob7.outputng >= 1)
                {
                    if (_jobs.myjob7.outputng2 == 1)
                    {
                        if (_jobs.myjob7.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob7.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob7.outputng >= 1 && _jobs.myjob7.outputng2 == 1)
                        {
                            output_camera7(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button72.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO7NG异常: " + ex.Message); }
        }
        public void IO8NG()
        {
            try
            {
                if (_jobs.myjob8.outputng == 0)
                {
                    output_camera8(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button74.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob8.outputng >= 1)
                {
                    if (_jobs.myjob8.outputng2 == 1)
                    {
                        if (_jobs.myjob8.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob8.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob8.outputng >= 1 && _jobs.myjob8.outputng2 == 1)
                        {
                            output_camera8(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button74.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO8NG异常: " + ex.Message); }
        }
        public void IO9NG()
        {
            try
            {
                if (_jobs.myjob9.outputng == 0)
                {
                    output_camera9(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button86.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob9.outputng >= 1)
                {
                    if (_jobs.myjob9.outputng2 == 1)
                    {
                        if (_jobs.myjob9.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob9.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob9.outputng >= 1 && _jobs.myjob9.outputng2 == 1)
                        {
                            output_camera9(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button86.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO9NG异常: " + ex.Message); }
        }
        public void IO10NG()
        {
            try
            {
                if (_jobs.myjob10.outputng == 0)
                {
                    output_camera10(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button93.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob10.outputng >= 1)
                {
                    if (_jobs.myjob10.outputng2 == 1)
                    {
                        if (_jobs.myjob10.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob10.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob10.outputng >= 1 && _jobs.myjob10.outputng2 == 1)
                        {
                            output_camera10(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button93.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO10NG异常: " + ex.Message); }
        }
        public void IO11NG()
        {
            try
            {
                if (_jobs.myjob11.outputng == 0)
                {
                    output_camera11(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button100.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob11.outputng >= 1)
                {
                    if (_jobs.myjob11.outputng2 == 1)
                    {
                        if (_jobs.myjob11.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob11.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob11.outputng >= 1 && _jobs.myjob11.outputng2 == 1)
                        {
                            output_camera11(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button100.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO11NG异常: " + ex.Message); }
        }
        public void IO12NG()
        {
            try
            {
                if (_jobs.myjob12.outputng == 0)
                {
                    output_camera12(2, false);
                    SafeBeginInvoke(() => {
                        // 更新UI操作
                        button107.BackColor = Color.Red;
                    });

                }
                else if (_jobs.myjob12.outputng >= 1)
                {
                    if (_jobs.myjob12.outputng2 == 1)
                    {
                        if (_jobs.myjob12.IOyanshi != 0)
                            Thread.Sleep(_jobs.myjob12.IOyanshi);
                        // 延时期间可能有更新的复位/更新请求，重新校验后再置位，防止过期请求误输出（输出卡死）
                        if (_jobs.myjob12.outputng >= 1 && _jobs.myjob12.outputng2 == 1)
                        {
                            output_camera12(2, true);
                            SafeBeginInvoke(() => {
                                // 更新UI操作
                                button107.BackColor = Color.Green;
                            });
                        }

                    }
                }

            }
            catch (Exception ex) { _logger.WriteLog("IO12NG异常: " + ex.Message); }
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private void runlog2(string a, string b, string path, int c)
        {
            lock (lockern)
            {
                _statistics.WriteDate1(b, path, c);

            }
        }
        private void output_camera(uint a, Boolean b)
        {
            // ★ 关键修复：检查是否正在释放或相机对象是否为空
            if (_disposingFlag || _cameraCtrl.Cameras[0] == null) return;

            lock (locker1)
            {
                if (dahua)
                    a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (MyCamera.MV_OK != nRet)
                    {
                        _logger.WriteLog("Set Fail1!" + nRet);
                        return;
                    }
                    nRet = _cameraCtrl.Cameras[0].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (MyCamera.MV_OK != nRet)
                    {
                        _logger.WriteLog("Set Fail2!" + nRet);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("IOO1~~~~~~~~~" + b + ex.Message);
                }

            }
        }
        private void output_camera1ng(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[0] == null) return;

            lock (locker1)
            {
                if (dahua)
                    a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出1-NG: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[0].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出1-NG: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出1-NG异常: " + ex.Message); }
            }
        }
        private void output_camera2(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[1] == null) return;
            if (manager1 == null || manager1.JobCount <= 1) return;

            lock (locker2)
            {
                if (dahua)
                    a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[1].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出2: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16) + " line=" + a);
                        return;
                    }
                    nRet = _cameraCtrl.Cameras[1].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出2: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16) + " val=" + b);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("IO输出2异常: " + ex.Message);
                }
            }
        }
        private void output_camera3(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[2] == null) return;
            if (manager1 == null || manager1.JobCount <= 2) return;

            lock (locker3)
            {
                if (dahua)
                    a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[2].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出3: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16) + " line=" + a);
                        return;
                    }
                    nRet = _cameraCtrl.Cameras[2].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出3: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16) + " val=" + b);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("IO输出3异常: " + ex.Message);
                }
            }
        }
        private void output_camera4(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[3] == null) return;
            if (manager1 == null || manager1.JobCount <= 3) return;

            lock (locker4)
            {
                if (dahua)
                    a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[3].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出4: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16) + " line=" + a);
                        return;
                    }
                    nRet = _cameraCtrl.Cameras[3].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出4: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16) + " val=" + b);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("IO输出4异常: " + ex.Message);
                }
            }
        }
        private void output_camera5(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[4] == null) return;
            if (manager1 == null || manager1.JobCount <= 4) return;

            lock (locker5)
            {
                if (dahua)
                    a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[4].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出5: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16) + " line=" + a);
                        return;
                    }
                    nRet = _cameraCtrl.Cameras[4].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK)
                    {
                        _logger.WriteLog("IO输出5: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16) + " val=" + b);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("IO输出5异常: " + ex.Message);
                }
            }
        }
        private void output_camera6(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[5] == null) return;
            if (manager1 == null || manager1.JobCount <= 5) return;

            lock (locker6)
            {
                if (dahua) a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[5].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出6: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[5].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出6: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出6异常: " + ex.Message); }
            }
        }
        private void output_camera7(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[6] == null) return;
            if (manager1 == null || manager1.JobCount <= 6) return;

            lock (locker7)
            {
                if (dahua) a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[6].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出7: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[6].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出7: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出7异常: " + ex.Message); }
            }
        }
        private void output_camera8(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[7] == null) return;
            if (manager1 == null || manager1.JobCount <= 7) return;

            lock (locker8)
            {
                if (dahua) a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[7].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出8: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[7].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出8: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出8异常: " + ex.Message); }
            }
        }
        private void output_camera9(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[8] == null) return;
            if (manager1 == null || manager1.JobCount <= 8) return;

            lock (locker9)
            {
                if (dahua) a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出9: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[8].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出9: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出9异常: " + ex.Message); }
            }
        }
        private void output_camera10(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[9] == null) return;
            if (manager1 == null || manager1.JobCount <= 9) return;

            lock (locker10)
            {
                if (dahua) a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出10: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[9].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出10: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出10异常: " + ex.Message); }
            }
        }
        private void output_camera11(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[10] == null) return;
            if (manager1 == null || manager1.JobCount <= 10) return;

            lock (locker11)
            {
                if (dahua) a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出11: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[10].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出11: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出11异常: " + ex.Message); }
            }
        }
        private void output_camera12(uint a, Boolean b)
        {
            if (_disposingFlag || _cameraCtrl.Cameras[11] == null) return;
            if (manager1 == null || manager1.JobCount <= 11) return;

            lock (locker12)
            {
                if (dahua) a = a - 1;
                try
                {
                    int nRet;
                    nRet = _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("LineSelector", a);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出12: LineSelector失败 ret=0x" + Convert.ToString(nRet, 16)); return; }
                    nRet = _cameraCtrl.Cameras[11].MV_CC_SetBoolValue_NET("LineInverter", b);
                    if (nRet != MyCamera.MV_OK) { _logger.WriteLog("IO输出12: LineInverter失败 ret=0x" + Convert.ToString(nRet, 16)); }
                }
                catch (Exception ex) { _logger.WriteLog("IO输出12异常: " + ex.Message); }
            }
        }
        // 在 Form1_FormClosed 后，WinForms 会调用 Dispose(bool)
        // 避免在 Dispose 中 base.Dispose(disposing) 释放 CogRecordDisplay 的 COM RCW 时报
        // "RaceOnRCWCleanup" 错误，需要先停止所有后台活动
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SafeCleanupBeforeDispose();
            }
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            _disposingFlag = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// 在 Dispose 前安全停止所有后台活动，防止 RaceOnRCWCleanup（RCW 竞态）。
        /// 注意：相机资源已在 Form1_FormClosing → ReleaseAllCameras() 中释放
        /// 此方法只做最终的兜底清理，避免重复操作导致异常
        /// </summary>
        #endregion
        #region 资源清理与程序退出
        private void SafeCleanupBeforeDispose()
        {
            // ★低风险加固：释放原图显示（RawImageMode）各路最后一张位图——
            //   ShowRawFrame 只 Dispose 被替换的旧图，退出时最后一张无人释放（进程兜底），此处收口。
            for (int i = 0; i < 12; i++)
            {
                try
                {
                    var pb = _rawBox[i];
                    if (pb == null) continue;
                    var img = pb.Image as Bitmap;
                    pb.Image = null;
                    if (img != null) img.Dispose();
                }
                catch { }
            }
            // ★P2-1：退出路径主动释放 12 路相机输出队列，避免残留后台线程
            try
            {
                if (_cameraOutWork != null)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        if (_cameraOutWork[i] != null) _cameraOutWork[i].Dispose();
                    }
                        // ★fix②：不置 null。Dispose 后每个 Enqueue 因 _disposed=true 自动 no-op，
                        // 保留数组引用，检测线程若有残留 _cameraOutWork[i].Enqueue 调用也不会 NRE。
                        }
                        // ★F9：结果回写专用队列同样释放（不置 null，理由同上）
                        if (_cameraResultWork != null)
                        {
                        for (int i = 0; i < 12; i++)
                        {
                            if (_cameraResultWork[i] != null) _cameraResultWork[i].Dispose();
                        }
                        }
            }
            catch { }
            _disposingFlag = true;
            try
            {
                if (!StopInspectWorkers())
                {
                    _logger.WriteLog("检测仍未结束，跳过 VisionPro/相机资源释放");
                    return;
                }
                // ★ F14: 退出时释放各相机持有的 VisionPro COM 对象
                ReleaseAllMyjobVisionObjects();
                // 1. 释放全部相机（停止抓流 → 关闭设备 → 销毁句柄），释放所有权收口到 CameraController。
                //    此处不逐路等待，由后面的统一 Sleep 等待 SDK 回调线程退出。
                _cameraCtrl.ReleaseAllCameras(0, 0);

                // ★ 关键：给足够时间让相机 SDK 的回调线程完全退出（至少500ms）
                Thread.Sleep(500);

                // 2. 清空 bmp 引用（防止 ImageCallBack 残留引用）
                for (int i = 0; i < 12; i++)
                {
                    if (bmp[i] != null)
                    {
                        try { bmp[i].Dispose(); } catch { }
                        bmp[i] = null;
                    }
                }

                // 3. 释放非托管内存（防止泄漏）
                for (int i = 0; i < 12; i++)
                {
                    FreeDriverBuffer(i);
                    if (m_pSaveImageBuf[i] != IntPtr.Zero)
                    {
                        try { Marshal.FreeHGlobal(m_pSaveImageBuf[i]); } catch { }
                        m_pSaveImageBuf[i] = IntPtr.Zero;
                    }
                }

                // 4. 强制 GC 回收 RCW（确保 COM 对象被正确释放）
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                _logger.WriteLog("SafeCleanupBeforeDispose异常: " + ex.Message);
            }
        }

        /// <summary>
        /// FormClosed 事件处理 - 由 Designer 绑定
        /// SafeCleanupBeforeDispose 已在 Dispose 中执行
        /// </summary>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                if (_userActivityFilter != null)
                    Application.RemoveMessageFilter(_userActivityFilter);
                CrashMonitor.MarkExitedCleanly();
            }
            catch { }
            // 子窗体（Form3/Form5/通讯窗）会 Cancel 关闭只隐藏，前台线程也会拖住进程。
            // 主窗已关，直接结束进程，否则 VS 会一直停在“正在调试”。
            Environment.Exit(0);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // ★ 关键修复：防止关闭对话框重复弹出
                // Application.Exit() 会再次触发 FormClosing 事件
                if (_isFormClosing)
                {
                    // 已经在关闭过程中，直接允许关闭，不再弹出对话框
                    e.Cancel = false;
                    return;
                }

                // 第一次进入，标记正在关闭
                _isFormClosing = true;

                if (MessageBox.Show("将要关闭检测，是否继续？", "询问", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _logger.WriteLog("软件关闭");

                    // ★ 立即设置释放标志，阻止 ImageCallBack、timer 等后台操作访问 UI
                    _disposingFlag = true;

                    // 停止检测入队线程，避免关相机后仍消费旧帧
                    if (!StopInspectWorkers())
                    {
                        _logger.WriteLog("关闭中止：检测事务仍在执行，未释放资源");
                        // ★P0 修复：中止关闭必须回滚，否则系统被永久卡死——
                        //   原实现把"停定时器 / 停通讯轮询 / 重置IO输出"放在本检查之前执行，
                        //   一旦在此中止，这些不可逆副作用已经发生，且 _disposingFlag 未复位
                        //   → 窗口还在但界面永久不再刷新、定时器与通讯全停，只能杀进程。
                        //   现把所有不可逆步骤后移到本检查之后；中止时按"能否安全恢复"分两种处理：
                        bool anyAlive = false;
                        for (int i = 0; i < 12; i++)
                        {
                            if (_inspectThreads[i] != null && _inspectThreads[i].IsAlive) { anyAlive = true; break; }
                        }

                        if (!anyAlive)
                        {
                            // ① 检测线程已全部退出：可安全完整恢复（线程可重启；定时器/通讯尚未被动过）
                            // ★ 竞态修复：_disposingFlag 必须在 StartInspectWorkers() 之前复位。
                            //   新 worker 线程入口判 while (!_inspectStop && !_disposingFlag)，
                            //   若标志仍为 true，新线程会在启动瞬间立即退出 → 界面看似正常但该路永久无检测。
                            _inspectStop = false;
                            _inspectionLifecycle.Resume();
                            _disposingFlag = false;
                            StartInspectWorkers();
                            _isFormClosing = false;
                            e.Cancel = true;
                            MessageBox.Show("已取消关闭：检测已恢复运行。", "关闭已取消");
                        }
                        else
                        {
                            // ② 仍有线程卡在不可中断的检测中：不能重启（避免同槽位出现重复消费者），
                            //    保持停检态；但必须复位 _disposingFlag 让界面恢复刷新，并明确告知需重启，
                            //    避免"窗口看似正常、实则已停止检测"的欺骗状态。
                            _disposingFlag = false;
                            _isFormClosing = false;
                            e.Cancel = true;
                            MessageBox.Show("关闭已取消。注意：检测线程尚未结束，系统当前处于停止状态且无法自动恢复，请稍候再次关闭，或重启软件。", "关闭已取消");
                        }
                        return;
                    }

                    // ===== 以下为"确定关闭"的不可逆步骤（必须在停止检测成功之后执行）=====
                    // 先禁用所有计时器，停止后台任务
                    try { timer1.Enabled = false; } catch { }
                    try { timer2.Enabled = false; } catch { }
                    try { timer17.Enabled = false; } catch { }

                    // 停止通讯轮询线程，释放HslCommunication连接
                    StopAllCommPolling();

                    // 重置IO输出
                    try { button11_Click_1(null, null); } catch { }
                    Thread.Sleep(100);
                    ResetMyJobOutputs();

                    // ★ 核心修复：按正确顺序释放相机资源（只做这一件事）
                    ReleaseAllCameras();

                    // ★ 强制退出应用程序（终止所有后台线程和消息循环）
                    // 这一步确保进程完全退出，不会残留
                    Application.Exit();
                }
                else
                {
                    // 用户取消关闭，重置标志位以便下次还能弹出对话框
                    _isFormClosing = false;
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLog("关闭时异常：" + ex.Message);
                e.Cancel = false;
            }
        }

        /// <summary>
        /// 安全释放所有已连接的相机资源（停止抓流→关闭设备→销毁对象）
        /// </summary>
        private void ReleaseAllCameras()
        {
            // ★ F24：先设置释放标志，让 ImageCallBack 入口立即返回，避免回调进入正在释放的 SDK
            _disposingFlag = true;

            // ★ S4：释放所有权收口到 CameraController —— 停止抓流(等100ms) → 关闭设备(等50ms) → 销毁句柄。
            //    逐路逐操作容错；已为空的槽位自动跳过（可重复调用）。
            _cameraCtrl.ReleaseAllCameras(100, 50);

            // ★ F24：所有相机释放后，等待足够长时间让 SDK 内部回调线程完全退出
            Thread.Sleep(300);

            // ★ F24：注销回调委托引用，断开 SDK 与托管回调的连接，防止 GC 延迟回收委托导致回调进入已释放资源
            cbImage = null;
            cbException = null;   // ★P0：异常回调委托同样注销（与 cbImage 对称）
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // 重置抓取状态标志
            m_bGrabbing1 = false;
            m_bGrabbing2 = false;
            m_bGrabbing3 = false;
            m_bGrabbing4 = false;
            m_bGrabbing5 = false;
            m_bGrabbing6 = false;
            m_bGrabbing7 = false;
            m_bGrabbing8 = false;
            m_bGrabbing9 = false;
            m_bGrabbing10 = false;
            m_bGrabbing11 = false;
            m_bGrabbing12 = false;

            // 释放非托管内存
            for (int i = 0; i < 12; i++)
            {
                FreeDriverBuffer(i);
                if (m_pSaveImageBuf[i] != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(m_pSaveImageBuf[i]); } catch { }
                    m_pSaveImageBuf[i] = IntPtr.Zero;
                }
            }

            // 释放Bitmap资源
            for (int i = 0; i < 12; i++)
            {
                if (bmp[i] != null)
                {
                    try { bmp[i].Dispose(); } catch { }
                    bmp[i] = null;
                }
            }

            _logger.WriteLog("所有相机资源已安全释放");
        }

        /// <summary>
        /// 全量复位 12 个相机的 IO 输出标志（停止/启动时调用，输出线回断态）
        /// </summary>
        private void ResetMyJobOutputs()
        {
            try
            {
                _jobs.myjob1.outputok = 0; _jobs.myjob1.outputng = 0; _jobs.myjob1.outputok2 = -1; _jobs.myjob1.outputng2 = -1;
                _jobs.myjob2.outputok = 0; _jobs.myjob2.outputng = 0; _jobs.myjob2.outputok2 = -1; _jobs.myjob2.outputng2 = -1;
                _jobs.myjob3.outputok = 0; _jobs.myjob3.outputng = 0; _jobs.myjob3.outputok2 = -1; _jobs.myjob3.outputng2 = -1;
                _jobs.myjob4.outputok = 0; _jobs.myjob4.outputng = 0; _jobs.myjob4.outputok2 = -1; _jobs.myjob4.outputng2 = -1;
                _jobs.myjob5.outputok = 0; _jobs.myjob5.outputng = 0; _jobs.myjob5.outputok2 = -1; _jobs.myjob5.outputng2 = -1;
                _jobs.myjob6.outputok = 0; _jobs.myjob6.outputng = 0; _jobs.myjob6.outputok2 = -1; _jobs.myjob6.outputng2 = -1;
                _jobs.myjob7.outputok = 0; _jobs.myjob7.outputng = 0; _jobs.myjob7.outputok2 = -1; _jobs.myjob7.outputng2 = -1;
                _jobs.myjob8.outputok = 0; _jobs.myjob8.outputng = 0; _jobs.myjob8.outputok2 = -1; _jobs.myjob8.outputng2 = -1;
                _jobs.myjob9.outputok = 0; _jobs.myjob9.outputng = 0; _jobs.myjob9.outputok2 = -1; _jobs.myjob9.outputng2 = -1;
                _jobs.myjob10.outputok = 0; _jobs.myjob10.outputng = 0; _jobs.myjob10.outputok2 = -1; _jobs.myjob10.outputng2 = -1;
                _jobs.myjob11.outputok = 0; _jobs.myjob11.outputng = 0; _jobs.myjob11.outputok2 = -1; _jobs.myjob11.outputng2 = -1;
                _jobs.myjob12.outputok = 0; _jobs.myjob12.outputng = 0; _jobs.myjob12.outputok2 = -1; _jobs.myjob12.outputng2 = -1;
            }
            catch { }
        }

        /// <summary>
        /// 停止所有通讯窗体的轮询线程（程序关闭前调用，避免后台线程在进程退出时操作半释放资源）
        /// </summary>
        private void StopAllCommPolling()
        {
            try { _comm.Modbustcp?.StopPolling(); } catch { }
            try { _comm.ModbusRtu?.StopPolling(); } catch { }
            try { _comm.Omron?.StopPolling(); } catch { }
        }



        #endregion
        #region 显示控件事件与相机重连
        private void cogRecordDisplay1_MouseMove(object sender, MouseEventArgs e)
        {
            //x1 = e.X;
            //y1 = e.Y;

        }




        private void cogRecordDisplay1_Click(object sender, EventArgs e)
        {

            //try
            //{
            //    if (x1 != out_x && y1 != out_y)
            //    {
            //        ICogTransform2D myTranform1 = cogRecordDisplay1.GetTransform(cogRecordDisplay1.Image.SelectedSpaceName, ".");
            //        myTranform1.MapPoint(x1, y1, out out_x, out out_y);
            //        Color c = new Color();
            //        Bitmap bitmap = new Bitmap(cogRecordDisplay1.Image.ToBitmap());
            //        c = bitmap.GetPixel((int)out_x, (int)out_y);
            //        label6.Text = string.Format("X={0},Y={1},R={2},G={3},B={4}", out_x, out_y, c.R, c.G, c.B);
            //        bitmap.Dispose();
            //    }
            //}
            //catch (Exception ex)
            //{ MessageBox.Show(ex.Message); }; 
        }



        private delegate void ltbox2(string sum1, string ok1, string rate1, string sum2, string ok2, string rate2, string sum3, string ok3, string rate3, string sum4, string ok4, string rate4);
        private void SetBox(ListBox lb, string aa, int bb)
        {
            if (lb.InvokeRequired)
                lb.Invoke(new Action<string, int>((x, y) => lb.Items[y] = x), aa, bb);
            else
                lb.Items[bb] = aa;
        }

        private void UpdateJobErrorTable(DataTable table, Dictionary<string, int> d, string cuowu)
        {
            if (d.ContainsKey(cuowu))
            {
                d[cuowu]++;
                try { table.Rows.Clear(); } catch { }
                foreach (string aaa1 in d.Keys)
                    table.Rows.Add(aaa1, d[aaa1].ToString());
            }
            else
            {
                d.Add(cuowu, 1);
                table.Rows.Add(cuowu, "1");
            }
        }

        private void setbox6(string aa, int bb) => SetBox(listBox3, aa, bb);
        private void setbox5(string aa, int bb) => SetBox(listBox1, aa, bb);
        private void setbox7(string aa, int bb) => SetBox(listBox7, aa, bb);
        private void setbox8(string aa, int bb) => SetBox(listBox6, aa, bb);
        private void setbox10(string aa, int bb) => SetBox(listBox14, aa, bb);
        private void setbox11(string aa, int bb) => SetBox(listBox15, aa, bb);
        private void setbox12(string aa, int bb) => SetBox(listBox18, aa, bb);
        private void setbox13(string aa, int bb) => SetBox(listBox17, aa, bb);
        private void setbox14(string aa, int bb) => SetBox(listBox19, aa, bb);
        private void setbox15(string aa, int bb) => SetBox(listBox20, aa, bb);
        private void setbox16(string aa, int bb) => SetBox(listBox21, aa, bb);
        private void setbox17(string aa, int bb) => SetBox(listBox22, aa, bb);
        private void setbox18(string aa, int bb) => SetBox(listBox16, aa, bb);
        private void setbox19(string aa, int bb) => SetBox(listBox23, aa, bb);
        private void setbox20(string aa, int bb) => SetBox(listBox24, aa, bb);
        private void setbox21(string aa, int bb) => SetBox(listBox25, aa, bb);

        private void Form1_MinimumSizeChanged(object sender, EventArgs e)
        {
            // tabControl1.Size.Width = 212;
            // tabControl1.Size.Height= 212;
            timer1.Enabled = true;

        }


        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool FlashWindow(IntPtr handle, bool bInvert);
        private void timer1_Tick(object sender, EventArgs e)
        {
            FlashWindow(this.Handle, true);
        }

        private void Form1_MaximumSizeChanged(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        private void Form1_Move(object sender, EventArgs e)
        {
            //if (Frm2.start == 1)
            //{
            //    Thread.Sleep(10);
            //    this.WindowState = FormWindowState.Maximized;
            //}
        }
        string cameraState = "";
        volatile bool chonglianzhong = false;   // ★低风险加固：UI 线程(timer2_Tick)读写、Task.Run 后台 finally 复位，跨线程需 volatile 保证可见性
        /// <summary>运行或打开设备后启用，用于断线/热插拔重连。</summary>
        private bool _cameraReconnectEnabled = false;

        /// <summary>与 bnOpen_Click 相同：chUserDefinedName==camera_name[N] 绑定流程槽位</summary>
        private bool TryResolveCameraSlotByName(string devName, int deviceArrayIndex, out int slot)
        {
            slot = -1;
            string nnn = "";
            for (int i = 0; i < 12; i++)
            {
                if (devName == camera_name[i]) { _jobs.Myjobs[i].index = deviceArrayIndex; nnn = i.ToString(); }
            }
            if (string.IsNullOrEmpty(nnn)) return false;
            slot = int.Parse(nnn);
            return true;
        }

        private bool IsConfiguredCameraName(string devName)
        {
            for (int i = 0; i < 12; i++)
                if (devName == camera_name[i]) return true;
            return false;
        }

        /// <summary>热插拔重连：遍历枚举列表，绑定规则与 bnOpen_Click 一致</summary>
        private bool TryReconnectDeviceLikeBnOpen(MyCamera.MV_CC_DEVICE_INFO devInfo, int deviceArrayIndex, out int slot, out int nRet, out bool nameMatched)
        {
            slot = -1;
            nRet = -1;
            nameMatched = false;
            device1[deviceArrayIndex] = devInfo;
            string devName = "";

            if (devInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(devInfo.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                devName = gigeInfo.chUserDefinedName;
                if (!IsConfiguredCameraName(devName)) return false;
                nameMatched = true;
                if (!TryResolveCameraSlotByName(devName, deviceArrayIndex, out slot)) return false;
            }
            else if (devInfo.nTLayerType == MyCamera.MV_USB_DEVICE)
            {
                MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(devInfo.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                devName = usbInfo.chUserDefinedName;
                if (!IsConfiguredCameraName(devName)) return false;
                nameMatched = true;
                if (!TryResolveCameraSlotByName(devName, deviceArrayIndex, out slot)) return false;
            }
            else
            {
                return false;
            }

            if (_cameraCtrl.Cameras[slot] != null) return false;

            lock (_cameraLock)
            {
                if (_cameraCtrl.Cameras[slot] == null)
                    _cameraCtrl.Cameras[slot] = new MyCamera();
                if (_cameraCtrl.Cameras[slot] == null) return false;

                nRet = _cameraCtrl.Cameras[slot].MV_CC_CreateDevice_NET(ref device1[deviceArrayIndex]);
                if (nRet != MyCamera.MV_OK) return false;

                nRet = _cameraCtrl.Cameras[slot].MV_CC_OpenDevice_NET();
                if (nRet != MyCamera.MV_OK)
                {
                    // 打开失败时销毁已创建的设备句柄，防止句柄泄漏
                    try { _cameraCtrl.Cameras[slot].MV_CC_DestroyDevice_NET(); } catch { }
                    return false;
                }

                m_nCanOpenDeviceNum++;
                m_pDeviceInfo[deviceArrayIndex] = device1[deviceArrayIndex];

                if (device1[deviceArrayIndex].nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = _cameraCtrl.Cameras[slot].MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                        _cameraCtrl.Cameras[slot].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                }
                _cameraCtrl.Cameras[slot].MV_CC_RegisterExceptionCallBack_NET(cbException, (IntPtr)slot);   // 2026-09-06：注册 SDK 异常回调（★P0：传字段持有委托，防 GC）
                nRet = _cameraCtrl.Cameras[slot].MV_CC_RegisterImageCallBackEx_NET(cbImage, (IntPtr)slot);
                return nRet == MyCamera.MV_OK;
            }
        }

        private int CountOpenedCameras()
        {
            int count = 0;
            int max = manager1 == null ? 12 : Math.Min(12, manager1.JobCount);
            for (int i = 0; i < max; i++)
            {
                if (_cameraCtrl.Cameras[i] == null) continue;
                try
                {
                    if (_cameraCtrl.Cameras[i].MV_CC_IsDeviceConnected_NET())
                        count++;
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("相机" + (i + 1) + " 连接检测异常: " + ex.Message);
                }
            }
            return count;
        }

        /// <summary>启用相机断线及热插拔重连定时器（方案已加载且点击运行后调用）。</summary>
        private void EnableCameraReconnect()
        {
            if (manager1 == null || manager1.JobCount <= 0)
                return;
            _cameraReconnectEnabled = true;
            try
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(() => { if (!IsDisposed && !_disposingFlag) timer2.Enabled = true; }));
                else if (!IsDisposed && !_disposingFlag)
                    timer2.Enabled = true;
            }
            catch { }
        }

        private void SyncCameraParamsAfterConnect(int slot)
        {
            try
            {
                switch (slot)
                {
                    case 0: bnSetParam_Click(null, null); bnGetParam_Click(null, null); break;
                    case 1: bnSetParam2_Click(null, null); bnGetParam2_Click(null, null); break;
                    case 2: bnSetParam3_Click(null, null); bnGetParam3_Click(null, null); break;
                    case 3: bnSetParam4_Click(null, null); bnGetParam4_Click(null, null); break;
                    case 4: bnSetParam5_Click(null, null); bnGetParam5_Click(null, null); break;
                    case 5: bnSetParam6_Click(null, null); bnGetParam6_Click(null, null); break;
                    case 6: bnSetParam7_Click(null, null); bnGetParam7_Click(null, null); break;
                    case 7: bnSetParam8_Click(null, null); bnGetParam8_Click(null, null); break;
                    case 8: bnSetParam9_Click(null, null); bnGetParam9_Click(null, null); break;
                    case 9: bnSetParam10_Click(null, null); bnGetParam10_Click(null, null); break;
                    case 10: bnSetParam11_Click(null, null); bnGetParam11_Click(null, null); break;
                    case 11: bnSetParam12_Click(null, null); bnGetParam12_Click(null, null); break;
                }
            }
            catch { }
        }

        private void ApplyTriggerModeAfterConnect(int slot)
        {
            if (dahua) return;
            ApplyTriggerModeToCameraHardware(slot);
        }

        private void ApplyCameraUiAfterConnect(int slot)
        {
            SyncCameraParamsAfterConnect(slot);
            ApplyTriggerModeAfterConnect(slot);
        }

        private void EnableAllConnectedCameraControls()
        {
            if (CountOpenedCameras() > 0)
            {
                bnOpen.Enabled = false;
                bnClose.Enabled = true;
            }
        }

        /// <summary>重连成功后刷新在线/使用设备数等 UI（须在 UI 线程调用）。</summary>
        private void RefreshCameraDeviceCountUi()
        {
            try
            {
                m_nCanOpenDeviceNum = CountOpenedCameras();
                // ★B8 修复：不再调用 DeviceListAcq()——它内含 GC.Collect()（全停顿）与全网设备枚举
                //   （数十~数百 ms），本方法是重连收尾在 UI 线程执行的，每次重连成功都会卡一次界面。
                //   设备下拉列表留给用户手动打开相机时枚举；这里只做轻量计数与按钮态刷新。
                tbUseNum.Text = m_nCanOpenDeviceNum.ToString("d");
                EnableAllConnectedCameraControls();
                // ★B8：同步各路"开始/停止采集"按钮态，避免重连恢复采流后 UI 仍显示"开始采集"，
                //   用户误点触发 StartGrabFor 对已采集相机重复 StartGrabbing（已知误清标志缺陷）。
                int _jc = (manager1 != null) ? manager1.JobCount : 0;
                for (int _gi = 0; _gi < 12 && _gi < _jc; _gi++)
                {
                    try
                    {
                        if (_cameraCtrl.Cameras[_gi] != null && IsCameraGrabbing(_gi))
                            SetStartGrabButtonState(_gi, false);
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// ★ 2026-09-13：触发回帧超时恢复（详见 TriggerFrameTimeoutMs 字段注释）。
        /// 针对「软触发命令返回成功、相机却一直不回帧，且 IsDeviceConnected 仍为 true 因而不会触发设备级重连」
        /// 这一盲区：pending 占满(容量 3)后将持续拒绝该路新触发。
        /// 约束：
        ///   ① 不能删除旧记录继续触发（会重新引入「记录-图像」配对错位），必须整条链路归零；
        ///   ② 恢复期间必须禁止该路新触发（_triggerRecovering 同时被触发路径检查），
        ///      否则新登记成功的记录会被本流程的 Clear 清掉，再次破坏配对；
        ///   ③ 停采必须确认成功后才清记录；重建失败要保留状态下轮重试（独立于 pending/在线状态）。
        /// </summary>
        private void RecoverStaleTriggers()
        {
            for (int i = 0; i < 12; i++)
            {
                if (_switchingScheme || _disposingFlag) return;
                // ★B7 修复：连续运行(相机自由跑帧)回帧看门狗——触发模式已有 pending 超时看门狗，但"连续运行"
                //   模式没有任何 pending 记录：取流静默停摆(帧不再回调)时无任何检测，且 IsDeviceConnected 仍为
                //   true 时设备级重连也不触发（与 TriggerFrameTimeoutMs 注释所述盲区同源）。
                //   判据：软件认为在采集(IsCameraGrabbing=true) + 该槽启用(yun=1) + 模式为"连续运行"
                //         + 相机在线 + 非恢复中 + SDK 未报断连 + 帧计数 ContFrameStallTimeoutMs 内无增长 → 重建取流。
                if (_jobs.Myjobs[i] != null && _jobs.Myjobs[i].yun == 1 && _jobs.Myjobs[i].triggerMode == "连续运行"
                    && IsCameraGrabbing(i)
                    && System.Threading.Volatile.Read(ref _grabRecoveryPending[i]) == 0
                    && System.Threading.Volatile.Read(ref _cameraSdkFault[i]) == 0
                    && _cameraCtrl.Cameras[i] != null)
                {
                    int _cfNow = System.Threading.Volatile.Read(ref m_nFrames[i]);
                    int _tickNow = Environment.TickCount;
                    int _cfLast = System.Threading.Volatile.Read(ref _contFrameLast[i]);
                    if (_cfNow != _cfLast)
                    {
                        // 有新帧：刷新基线
                        System.Threading.Volatile.Write(ref _contFrameLast[i], _cfNow);
                        System.Threading.Volatile.Write(ref _contFrameLastMs[i], _tickNow);
                    }
                    else
                    {
                        int _cfLastMs = System.Threading.Volatile.Read(ref _contFrameLastMs[i]);
                        if (_cfLastMs != 0 && (uint)(_tickNow - _cfLastMs) > (uint)ContFrameStallTimeoutMs)
                        {
                            if (System.Threading.Interlocked.CompareExchange(ref _triggerRecovering[i], 1, 0) == 0)
                            {
                                try
                                {
                                    _logger.WriteLog("相机" + (i + 1) + " 连续运行 " + (ContFrameStallTimeoutMs / 1000) + " 秒无新帧，判定取流停摆：停采重建取流");
                                    System.Threading.Volatile.Write(ref _grabRecoveryPending[i], 1);
                                    RebuildGrabFor(i, "连续停摆");
                                    // 重置基线，避免下一轮立即重复触发（重建失败则由 _grabRecoveryPending 接力重试）
                                    System.Threading.Volatile.Write(ref _contFrameLast[i], System.Threading.Volatile.Read(ref m_nFrames[i]));
                                    System.Threading.Volatile.Write(ref _contFrameLastMs[i], Environment.TickCount);
                                }
                                finally
                                {
                                    System.Threading.Volatile.Write(ref _triggerRecovering[i], 0);
                                }
                            }
                            continue;
                        }
                    }
                }
                // ★ 优先重试「上次恢复未完成」的相机：不依赖 pending（已被清空），也独立于设备在线状态。
                if (System.Threading.Volatile.Read(ref _grabRecoveryPending[i]) != 0)
                {
                    if (System.Threading.Interlocked.CompareExchange(ref _triggerRecovering[i], 1, 0) == 0)
                    {
                        try { RebuildGrabFor(i, "重试"); }
                        finally { System.Threading.Volatile.Write(ref _triggerRecovering[i], 0); }
                    }
                    continue;
                }
                double age = _jobs.OldestPendingTriggerAgeMs(i);
                if (age < 0 || age < TriggerFrameTimeoutMs) continue;
                if (System.Threading.Interlocked.CompareExchange(ref _triggerRecovering[i], 1, 0) != 0) continue;
                try
                {
                    if (_cameraCtrl.Cameras[i] == null)
                    {
                        _jobs.ClearCommTriggerPending(i);
                        continue;
                    }
                    _logger.WriteLog("相机" + (i + 1) + " 触发后 " + (int)age + "ms 未回帧（且未触发设备重连），判定采集异常：禁止新触发 -> 停采 -> 清记录 -> 重建取流");
                    System.Threading.Volatile.Write(ref _grabRecoveryPending[i], 1);  // 先标记：恢复未完成(同时用于禁止新触发)
                    RebuildGrabFor(i, "首次");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("相机" + (i + 1) + " 触发回帧超时恢复异常: " + ex.Message);
                }
                finally
                {
                    System.Threading.Volatile.Write(ref _triggerRecovering[i], 0);
                }
            }
        }

        /// <summary>停采并确认成功 → 排空在途回调 → 清 pending → 重建取流；成功才清除"恢复未完成"标志。</summary>
        private void RebuildGrabFor(int i, string phase)
        {
            // ★ 2026-09-13：仅当仍处于采集态时才停采。若上一轮已停采成功、只是重建失败，
            //   此处对"已停止"的设备重复调 StopGrabbing 的返回行为不确定，可能被误判为停采失败，
            //   从而永远卡在重试前。用 IsCameraGrabbing 判断后，已停则直接进入重建。
            if (IsCameraGrabbing(i))
            {
                int stopRet = -1;
                try { stopRet = _cameraCtrl.Cameras[i].MV_CC_StopGrabbing_NET(); } catch { stopRet = -1; }
                if (stopRet != MyCamera.MV_OK)
                {
                    // ★ 2026-09-13：停采失败时绝不能改软件采集标志（IsCameraGrabbing 读的是软件布尔值）。
                    //   否则下一轮会误判"已停止"而跳过停采、直接清记录并重建 —— 而此时设备可能仍在采集，
                    //   会留下「帧在路上、记录已删」的错配。保留原状态，下轮继续确认停采。
                    _logger.WriteLog("相机" + (i + 1) + " 触发回帧恢复(" + phase + ")：停止采集失败 " + stopRet + "，保留待回帧记录与采集状态，下轮重试");
                    return;
                }
                SetCameraGrabbing(i, false);
                // 排空在途回调：StopGrabbing 返回后，仍在途的回调可能稍后才结束，短暂等待避免旧帧迟到污染新记录
                try { System.Threading.Thread.Sleep(100); } catch { }
            }
            _jobs.ClearCommTriggerPending(i);   // 此时该路新触发已被 _grabRecoveryPending 挡住，不会误清新记录
            int nRet;
            bool ok;
            try { ok = PrepareCameraGrab(i, out nRet); }
            catch (Exception ex) { ok = false; nRet = -1; _logger.WriteLog("相机" + (i + 1) + " 重建取流异常: " + ex.Message); }
            if (ok)
            {
                System.Threading.Volatile.Write(ref _grabRecoveryPending[i], 0);
                _logger.WriteLog("相机" + (i + 1) + " 触发回帧恢复(" + phase + ")完成：已重建取流");
            }
            else
            {
                _logger.WriteLog("相机" + (i + 1) + " 触发回帧恢复(" + phase + ")：重建取流失败 " + nRet + "，保留状态下轮重试");
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // 在UI线程判断并发标志，避免Task.Run内非原子check-then-set导致重连任务重叠
            if (!_cameraReconnectEnabled || manager1 == null)
                return;
            if (dakaizhong || chonglianzhong)
                return;
            chonglianzhong = true;
            Task.Run(() =>
            {
                try
                {
                    // ★ 触发回帧超时恢复（命令成功但不回帧、又未触发设备重连时的兜底）
                    try { RecoverStaleTriggers(); } catch (Exception exT) { _logger.WriteLog("触发回帧超时恢复异常: " + exT.Message); }
                    cameraState = "";
                    int nRet = 1;
                    bool reconnectedThisTick = false;
                    try
                    {
                        if (manager1.JobCount > 0)
                        {
                                // 先检查是否有尚未打开的相机（_cameraCtrl.Cameras[i] == null）
                                bool hasNullCamera = false;
                                for (int _checkNull = 0; _checkNull < 12 && _checkNull < manager1.JobCount; _checkNull++)
                                {
                                    if (_cameraCtrl.Cameras[_checkNull] == null)
                                    {
                                        hasNullCamera = true;
                                        break;
                                    }
                                }
                                if (hasNullCamera)
                                {
                                    // 只在有未打开的相机时才重新枚举，避免不必要的性能开销
                                    MyCamera.MV_CC_DEVICE_INFO_LIST reconnectList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
                                    int enumRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref reconnectList);
                                    if (enumRet == 0 && reconnectList.nDeviceNum > 0)
                                    {
                                        m_nDevNum = (int)reconnectList.nDeviceNum;
                                        int i_temp = 0;
                                        for (int j = 0; j < reconnectList.nDeviceNum; j++)
                                        {
                                            try
                                            {
                                                MyCamera.MV_CC_DEVICE_INFO devInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(reconnectList.pDeviceInfo[j], typeof(MyCamera.MV_CC_DEVICE_INFO));
                                                bool nameMatched;
                                                int boundSlot;
                                                if (TryReconnectDeviceLikeBnOpen(devInfo, i_temp, out boundSlot, out nRet, out nameMatched))
                                                {
                                                    int capturedSlot = boundSlot;
                                                    this.Invoke(new Action(() => ApplyCameraUiAfterConnect(capturedSlot)));
                                                    if (_jobs.yunxing)
                                                    {
                                                        _jobs.Myjobs[capturedSlot].yun = 1;
                                                        int grabRet;
                                                        // ★B2 修复：原实现丢弃 PrepareCameraGrab 返回值——GigE 回归后
                                                        //   StartGrabbing/PayloadSize 失败时该槽"已连接但不采集"永久僵死
                                                        //   （下一轮 IsDeviceConnected 直接 return，再无重试路径），软触发恒被拒。
                                                        //   失败则回滚 yun 并完整销毁置 null，交给下一轮按名重扫（可自愈）。
                                                        if (!PrepareCameraGrab(capturedSlot, out grabRet))
                                                        {
                                                            _jobs.Myjobs[capturedSlot].yun = 0;
                                                            lock (_cameraLock)
                                                            {
                                                                try { _cameraCtrl.Cameras[capturedSlot].MV_CC_StopGrabbing_NET(); } catch { }
                                                                SetCameraGrabbing(capturedSlot, false);
                                                                try { _cameraCtrl.Cameras[capturedSlot].MV_CC_CloseDevice_NET(); } catch { }
                                                                try { _cameraCtrl.Cameras[capturedSlot].MV_CC_DestroyDevice_NET(); } catch { }
                                                                _cameraCtrl.Cameras[capturedSlot] = null;
                                                            }
                                                            _logger.WriteLog("相机" + (capturedSlot + 1) + "重连后启动取流失败(nRet=" + grabRet + ")，已销毁句柄待按名重扫");
                                                            cameraState += "相机" + (capturedSlot + 1) + "重连后启动取流失败\r\n";
                                                        }
                                                        else
                                                        {
                                                            // ★B8 修复：重连恢复采流后同步"开始/停止采集"按钮态，
                                                            //   否则 UI 仍显示"开始采集"，用户再点会触发 StartGrabFor 已知误清标志缺陷。
                                                            try { this.Invoke(new Action(() => { try { SetStartGrabButtonState(capturedSlot, false); } catch { } })); } catch { }
                                                        }
                                                    }
                                                    cameraState += "相机" + (capturedSlot + 1) + "已重连\r\n";
                                                    _logger.WriteLog("相机" + (capturedSlot + 1) + "重连成功，deviceIndex=" + i_temp);
                                                    reconnectedThisTick = true;
                                                }
                                                if (nameMatched)
                                                    i_temp++;
                                            }
                                            catch { }
                                        }
                                    }
                                }

                                                                for (int _ci = 0; _ci < 12 && _ci < manager1.JobCount; _ci++)
                                    CheckAndReconnectCamera(_ci);

                                // 原 12 路复制粘贴的重连逻辑统一抽取为下方局部函数；顺手修复历史不一致：
                                // 原相机4/8/9/10/11/12 失败字符串缺 "\r\n"、相机8/9 缺"断线开始重连/重连失败"日志，现已统一为规范格式。
                                void CheckAndReconnectCamera(int slot)
                                {
                                    // ★B1 修复：每路执行前复查中止标志——本 Task 可能在关闭/切型/开机中才跑到这里，
                                    //   原实现只在 timer2_Tick 入口判一次，Task 体内不再复查。
                                    if (!_cameraReconnectEnabled || _disposingFlag || _switchingScheme || dakaizhong) return;
                                    // ★B3 修复：per-slot 异常隔离——原实现任一路抛异常会中断本轮其余路，
                                    //   且被外层空 catch 吞掉（无日志）。包一层保证单路异常只影响该路。
                                    try { CheckAndReconnectCameraCore(slot); }
                                    catch (Exception exSlot)
                                    {
                                        try { _logger.WriteLog("相机" + (slot + 1) + "重连处理异常(不影响其它路): " + exSlot.Message); } catch { }
                                    }
                                }
                                void CheckAndReconnectCameraCore(int slot)
                                {
                                    var job = _jobs.Myjobs[slot];
                                    if (_cameraCtrl.Cameras[slot] == null)
                                    {
                                        cameraState += "相机" + (slot + 1) + "不在线\r\n";
                                        return;
                                    }
                                    if (_cameraCtrl.Cameras[slot].MV_CC_IsDeviceConnected_NET())
                                    {
                                        // ★B7：SDK 异常回调报过"设备断连"的槽，即使 IsDeviceConnected 仍报 true 也按断线处理
                                        //   （回调是权威信号；否则该路会因判据失真静默停摆，且 RecoverStaleTriggers 也不覆盖连续模式）。
                                        if (System.Threading.Volatile.Read(ref _cameraSdkFault[slot]) == 1)
                                        {
                                            System.Threading.Volatile.Write(ref _cameraSdkFault[slot], 0);
                                            _logger.WriteLog("相机" + (slot + 1) + " SDK 断连标志置位（IsDeviceConnected 仍为 true），强制走重连流程");
                                        }
                                        else
                                        {
                                            cameraState += "\r\n";
                                            return;
                                        }
                                    }
                                    string camName = "相机" + (slot + 1);
                                    _logger.WriteLog(camName + "断线，开始重连...");
                                    if (!ReconnectCameraSlot(slot, job.index, out nRet))
                                    {
                                        _logger.WriteLog(camName + "重连失败: 0x" + Convert.ToString(nRet, 16));
                                        cameraState += Convert.ToString(nRet, 16) + camName + "断线\r\n";
                                    }
                                    else
                                    {
                                        _logger.WriteLog(camName + "重连成功");
                                        reconnectedThisTick = true;
                                        job.state = camName + "断线\r\n";
                                        this.Invoke(new Action(() => ApplyCameraUiAfterConnect(slot)));
                                        if (_jobs.yunxing)
                                        {
                                            job.yun = 1;
                                            int grabRet;
                                            // ★B2 修复（与 capturedSlot 分支同）：启动取流失败则回滚 yun 并销毁置 null，交给按名重扫自愈
                                            if (!PrepareCameraGrab(slot, out grabRet))
                                            {
                                                job.yun = 0;
                                                lock (_cameraLock)
                                                {
                                                    try { _cameraCtrl.Cameras[slot].MV_CC_StopGrabbing_NET(); } catch { }
                                                    SetCameraGrabbing(slot, false);
                                                    try { _cameraCtrl.Cameras[slot].MV_CC_CloseDevice_NET(); } catch { }
                                                    try { _cameraCtrl.Cameras[slot].MV_CC_DestroyDevice_NET(); } catch { }
                                                    _cameraCtrl.Cameras[slot] = null;
                                                }
                                                _logger.WriteLog("相机" + (slot + 1) + "重连后启动取流失败(nRet=" + grabRet + ")，已销毁句柄待按名重扫");
                                                cameraState += "相机" + (slot + 1) + "重连后启动取流失败\r\n";
                                            }
                                            else
                                            {
                                                // ★B8 修复：重连恢复采流后同步"开始/停止采集"按钮态，
                                                //   否则 UI 仍显示"开始采集"，用户再点会触发 StartGrabFor 已知误清标志缺陷。
                                                try { this.Invoke(new Action(() => { try { SetStartGrabButtonState(slot, false); } catch { } })); } catch { }
                                            }
                                        }
                                        job.state = "";
                                    }
                                }
                        }
                        }
                    catch (Exception exTick)
                    {
                        // ★B3 修复：原空 catch——本轮重连异常被完全吞掉，无任何日志可查
                        try { _logger.WriteLog("自动重连本轮异常: " + exTick.Message); } catch { }
                    }

                    if (!cameraState.Contains("相"))
                        cameraState = "";
                    if (reconnectedThisTick)
                    {
                        try { this.Invoke(new Action(() => { RefreshCameraDeviceCountUi(); display(); })); } catch { }
                    }
                }
                finally
                {
                    chonglianzhong = false;
                }
            });
            if (day1 != DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture))
            {
                day1 = DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                _jobs.myjob1.fileng = new FileInfo(_jobs.myjob1.pathhead_ng + day1 + "\\");
                _jobs.myjob2.fileng = new FileInfo(_jobs.myjob2.pathhead_ng + day1 + "\\");
                _jobs.myjob1.fileok = new FileInfo(_jobs.myjob1.pathhead_ok + day1 + "\\");
                _jobs.myjob2.fileok = new FileInfo(_jobs.myjob2.pathhead_ok + day1 + "\\");
                _jobs.myjob3.fileng = new FileInfo(_jobs.myjob3.pathhead_ng + day1 + "\\");
                _jobs.myjob4.fileng = new FileInfo(_jobs.myjob4.pathhead_ng + day1 + "\\");
                _jobs.myjob3.fileok = new FileInfo(_jobs.myjob3.pathhead_ok + day1 + "\\");
                _jobs.myjob4.fileok = new FileInfo(_jobs.myjob4.pathhead_ok + day1 + "\\");
                info1ok = new DirectoryInfo(_jobs.myjob1.pathhead_ok + day1 + "\\");
                info1ng = new DirectoryInfo(_jobs.myjob1.pathhead_ng + day1 + "\\");
                info2ok = new DirectoryInfo(_jobs.myjob2.pathhead_ok + day1 + "\\");
                info2ng = new DirectoryInfo(_jobs.myjob2.pathhead_ng + day1 + "\\");
                info3ok = new DirectoryInfo(_jobs.myjob3.pathhead_ok + day1 + "\\");
                info3ng = new DirectoryInfo(_jobs.myjob3.pathhead_ng + day1 + "\\");
                info4ok = new DirectoryInfo(_jobs.myjob4.pathhead_ok + day1 + "\\");
                info4ng = new DirectoryInfo(_jobs.myjob4.pathhead_ng + day1 + "\\");
                _jobs.myjob5.fileng = new FileInfo(_jobs.myjob5.pathhead_ng + day1 + "\\");
                _jobs.myjob6.fileng = new FileInfo(_jobs.myjob6.pathhead_ng + day1 + "\\");
                _jobs.myjob5.fileok = new FileInfo(_jobs.myjob5.pathhead_ok + day1 + "\\");
                _jobs.myjob6.fileok = new FileInfo(_jobs.myjob6.pathhead_ok + day1 + "\\");
                _jobs.myjob7.fileng = new FileInfo(_jobs.myjob7.pathhead_ng + day1 + "\\");
                _jobs.myjob8.fileng = new FileInfo(_jobs.myjob8.pathhead_ng + day1 + "\\");
                _jobs.myjob7.fileok = new FileInfo(_jobs.myjob7.pathhead_ok + day1 + "\\");
                _jobs.myjob8.fileok = new FileInfo(_jobs.myjob8.pathhead_ok + day1 + "\\");
                info5ok = new DirectoryInfo(_jobs.myjob5.pathhead_ok + day1 + "\\");
                info5ng = new DirectoryInfo(_jobs.myjob5.pathhead_ng + day1 + "\\");
                info6ok = new DirectoryInfo(_jobs.myjob6.pathhead_ok + day1 + "\\");
                info6ng = new DirectoryInfo(_jobs.myjob6.pathhead_ng + day1 + "\\");
                info7ok = new DirectoryInfo(_jobs.myjob7.pathhead_ok + day1 + "\\");
                info7ng = new DirectoryInfo(_jobs.myjob7.pathhead_ng + day1 + "\\");
                info8ok = new DirectoryInfo(_jobs.myjob8.pathhead_ok + day1 + "\\");
                info8ng = new DirectoryInfo(_jobs.myjob8.pathhead_ng + day1 + "\\");
                info9ok = new DirectoryInfo(_jobs.myjob9.pathhead_ok + day1 + "\\");
                info9ng = new DirectoryInfo(_jobs.myjob9.pathhead_ng + day1 + "\\");
                info10ok = new DirectoryInfo(_jobs.myjob10.pathhead_ok + day1 + "\\");
                info10ng = new DirectoryInfo(_jobs.myjob10.pathhead_ng + day1 + "\\");
                info11ok = new DirectoryInfo(_jobs.myjob11.pathhead_ok + day1 + "\\");
                info11ng = new DirectoryInfo(_jobs.myjob11.pathhead_ng + day1 + "\\");
                info12ok = new DirectoryInfo(_jobs.myjob12.pathhead_ok + day1 + "\\");
                info12ng = new DirectoryInfo(_jobs.myjob12.pathhead_ng + day1 + "\\");
                Task.Run(() =>
                {
                    daoqi_jiankong();
                });

            }
            if (this.WindowState == FormWindowState.Minimized)
            {

                timer1.Enabled = true;
            }
            else
                timer1.Enabled = false;
            // });
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            button11.Enabled = true;
            _jobs.myjob1.trriger = 0;
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (button11.Enabled == true)
                timecount++;
            if (timecount >= 3)
            {
                button11.Enabled = false;
                timecount = 0;
            }
        }


        private void timer4_Tick(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                label3.Text = DateTime.Now.ToLongTimeString().ToString();
            }));
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            int now = 0;
            int forword = _jobs.myjob1.sum;
            // Thread.Sleep(10000);
            now = _jobs.myjob1.sum;
            this.Invoke(new Action(() =>
            {
            }));
        }
        Form3 frm3;
        Form5 frm5;
        FormMySQL frmMySQL;   // MySQL 数据库查询窗体
        FormMES frmMES;       // MES 通讯窗体（Apifox 式 HTTP 接口调试等）
        private readonly CommunicationService _comm = AppHost.Services.Resolve<CommunicationService>();

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob1.state.Contains("相"))
            {
                try
                {
                    if (comboBox1.Text == "连续运行")
                    {
                        _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger1.Enabled = false;
                        bnTriggerExec1.Enabled = false;
                    }
                    else if (comboBox1.Text == "触发拍照" || comboBox1.Text == "通讯触发")
                    {

                        _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger1.Checked || comboBox1.Text == "通讯触发")
                        {
                            _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                            if (m_bGrabbing1)
                            {
                                bnTriggerExec1.Enabled = true;
                            }
                        }
                        else
                        {
                            _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                        }
                        cbSoftTrigger1.Enabled = true;

                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message + "触发切换1");
                }
                _jobs.myjob1.triggerMode = comboBox1.Text;
            }
        }
        private void RecvInfo(string str)
        {
            textBox1.Text = str;
        }
        #endregion
        #region 通讯触发与切型
        // 无协议（串口/TCP）触发协议号：与 FINS=1/ModbusTCP=2/ModbusRTU=3 配对，唯一定位“发出触发的那条无协议连接”
        private const int NoProtoProto = 4;

        private void DataChange(object sender, Form3.SelectionChangedEventArgs e)
        {

            textBox1.Text = e.Selection;
            // 阶段：无协议连接 2~4 触发经主窗宿主转发，事件内已通过 e.LinkId 携带“原始来源链路号”，
            // 记录到相机使检测结果能回到发出触发的那条无协议连接（修复结果串到连接 1）。
            int nopSrcLink = e.LinkId;
            for (int slot = 0; slot < 12; slot++)
                _jobs.StageContinuousParameter(slot, "jieshou", e.Selection);
            if (string.IsNullOrEmpty(_jobs.myjob1.triggerZifu) || e.Selection == _jobs.myjob1.triggerZifu)
            {
                _jobs.myjob1.jieshouZifu = _jobs.myjob1.triggerZifu;
                // ch:触发命令 | en:Trigger command
                int nRet = _jobs.RequestTrigger(0, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob2.triggerZifu) || e.Selection == _jobs.myjob2.triggerZifu)
            {
                _jobs.myjob2.jieshouZifu = _jobs.myjob2.triggerZifu;
                // ch:触发命令 | en:Trigger command
                int nRet = _jobs.RequestTrigger(1, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob3.triggerZifu) || e.Selection == _jobs.myjob3.triggerZifu)
            {
                _jobs.myjob3.jieshouZifu = _jobs.myjob3.triggerZifu;
                // ch:触发命令 | en:Trigger command
                int nRet = _jobs.RequestTrigger(2, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob4.triggerZifu) || e.Selection == _jobs.myjob4.triggerZifu)
            {
                _jobs.myjob4.jieshouZifu = _jobs.myjob4.triggerZifu;
                // ch:触发命令 | en:Trigger command
                int nRet = _jobs.RequestTrigger(3, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob5.triggerZifu) || e.Selection == _jobs.myjob5.triggerZifu)
            {
                _jobs.myjob5.jieshouZifu = _jobs.myjob5.triggerZifu;
                // ch:触发命令 | en:Trigger command
                int nRet = _jobs.RequestTrigger(4, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob6.triggerZifu) || e.Selection == _jobs.myjob6.triggerZifu)
            {
                _jobs.myjob6.jieshouZifu = _jobs.myjob6.triggerZifu;
                // ch:触发命令 | en:Trigger command
                int nRet = _jobs.RequestTrigger(5, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob7.triggerZifu) || e.Selection == _jobs.myjob7.triggerZifu)
            {
                _jobs.myjob7.jieshouZifu = _jobs.myjob7.triggerZifu;
                // ch:触发命令 | en:Trigger command
                int nRet = _jobs.RequestTrigger(6, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob8.triggerZifu) || e.Selection == _jobs.myjob8.triggerZifu)
            {
                _jobs.myjob8.jieshouZifu = _jobs.myjob8.triggerZifu;
                int nRet = _jobs.RequestTrigger(7, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob9.triggerZifu) || e.Selection == _jobs.myjob9.triggerZifu)
            {
                _jobs.myjob9.jieshouZifu = _jobs.myjob9.triggerZifu;
                int nRet = _jobs.RequestTrigger(8, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob10.triggerZifu) || e.Selection == _jobs.myjob10.triggerZifu)
            {
                _jobs.myjob10.jieshouZifu = _jobs.myjob10.triggerZifu;
                int nRet = _jobs.RequestTrigger(9, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob11.triggerZifu) || e.Selection == _jobs.myjob11.triggerZifu)
            {
                _jobs.myjob11.jieshouZifu = _jobs.myjob11.triggerZifu;
                int nRet = _jobs.RequestTrigger(10, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
            if (string.IsNullOrEmpty(_jobs.myjob12.triggerZifu) || e.Selection == _jobs.myjob12.triggerZifu)
            {
                _jobs.myjob12.jieshouZifu = _jobs.myjob12.triggerZifu;
                int nRet = _jobs.RequestTrigger(11, new System.Collections.Generic.KeyValuePair<string, string>("jieshou", e.Selection), new CommTriggerSource(nopSrcLink, NoProtoProto, e.Selection));
                if (MyCamera.MV_OK != nRet)
                {
                    _logger.WriteLog("Trigger Software Fail!---" + nRet);
                }
            }
        }

        /// <summary>
        /// 无协议通讯（串口/TCP）：由 Form3 界面"拍照触发字符"文本框写入，
        /// 直接赋给所有相机的 triggerZifu，不再依赖 VP 方案。
        /// </summary>
        public void SetTriggerZifu(string zifu)
        {
            if (_jobs == null) return;
            string v = zifu ?? "";
            if (_jobs.Myjobs != null)
            {
                for (int i = 0; i < _jobs.Myjobs.Length && i < 12; i++)
                {
                    if (_jobs.Myjobs[i] != null)
                        _jobs.Myjobs[i].triggerZifu = v;
                }
            }
        }

        /// <summary>
        /// 设置单台相机（0-based 索引）的拍照触发字符。
        /// </summary>
        public void SetTriggerZifu(int idx, string zifu)
        {
            if (_jobs == null || idx < 0 || idx >= 12) return;
            if (_jobs.Myjobs != null && idx < _jobs.Myjobs.Length && _jobs.Myjobs[idx] != null)
                _jobs.Myjobs[idx].triggerZifu = zifu ?? "";
        }

        /// <summary>
        /// 检查触发条件是否满足
        /// </summary>
        /// <param name="receivedValue">接收到的数据值</param>
        /// <param name="triggerChar">配置的触发字符</param>
        /// <param name="mode">触发模式: 相等/包含/范围</param>
        /// <param name="val1">触发值（相等/包含时用；范围时为最小值）</param>
        /// <param name="val2">范围模式时的最大值</param>
        /// <returns>是否满足触发条件</returns>


        private static void GetCamTrigParams(string[] camRow, out string mode, out string val1, out string val2)
        {
            CommTriggerHelper.GetCamTrigParams(camRow, out mode, out val1, out val2);
        }

        /// <summary>
        /// 方案切换成功后（xinghao_qiehuan 成功收尾）统一发送通讯回执：
        /// 由触发切换的协议把“相机13 返回值”写回“切换”通道；未配置/未启用则不写。
        /// 手动切换与失败路径不会走到这里（proto 已被清零）。
        /// </summary>
        private void SendSchemeSwitchAck(int ackProto, int ackLinkId)
        {
            // ★M4 修复：来源（proto/linkId）由本次切型随参数传入，不再读写全局单槽——
            //   原全局单槽 _schemeAckProto/_schemeAckLinkId 在两条连接几乎同时切型时后写覆盖先写：
            //   一条连接的回执会被另一条连接的收尾提前发出/发到另一条连接（ModbusTCP+RTU 混用时存在）。
            if (ackProto == 0) return;
            try
            {
                if (ackProto == 1 && _comm.Omron != null)
                    _comm.Omron.WriteSchemeSwitchAck(ackLinkId);
                else if (ackProto == 2 && _comm.Modbustcp != null)
                    _comm.Modbustcp.WriteSchemeSwitchAck(ackLinkId);
                else if (ackProto == 3 && _comm.ModbusRtu != null)
                    _comm.ModbusRtu.WriteSchemeSwitchAck(ackLinkId);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("方案切换回执失败:" + ex.Message);
            }
        }

        /// <summary>
        /// OMRON Fins 通讯触发：收到切型指令后异步执行方案切换
        /// </summary>
        private void DataChange_fins(object sender, FormOmron.SelectionChangedEventArgs e)
        {
            if (_jobs.yunxing)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (e.Camera != (i + 1).ToString()) continue;

                    // 阶段 5：记下本次检测由哪条连接触发，检测结果只回写给这条连接。
                    // 阶段 7：同时记下协议（1=FINS），与 linkId 配对才能唯一定位一条连接。

                    // 阶段 5 多连接：触发条件取自“发出触发的那条连接”自己的相机绑定；
                    // 连接 1（LinkId<=1）继续走下面的原路径，行为逐字不变。
                    string[] linkCam;
                    if (e.LinkId > 1 && _comm.Omron.TryGetLinkCamera(e.LinkId, i + 1, out linkCam))
                    {
                        GetCamTrigParams(linkCam, out string lmode, out string ltv1, out string ltv2);
                        _jobs.ApplyCommTrigger(_jobs.Myjobs[i], i, e.Selection, lmode, ltv1, ltv2, "fins", new CommTriggerSource(e.LinkId, 1, e.Selection));
                        continue;
                    }

                    if (!_comm.Omron.camera_dic.ContainsKey(i + 1)) continue;
                    GetCamTrigParams(_comm.Omron.camera_dic[i + 1], out string mode, out string tv1, out string tv2);
                    _jobs.ApplyCommTrigger(_jobs.Myjobs[i], i, e.Selection, mode, tv1, tv2, "fins", new CommTriggerSource(e.LinkId, 1, e.Selection));
                }
            }
            if (e.Camera.Contains("13"))
            {
                // 阶段 5 多连接：连接 2~4 的切方案走独立分支——
                // 用该连接自己命中的方案路径（e.SchemePath），并用独立的切换锁；
                // 不占用连接 1 的 lujing / qiehuanzhong / camera_dic[13]，避免互相串扰。
                if (e.LinkId > 1)
                {
                    string linkPath = (e.SchemePath ?? "").Replace("\0", "");
                    if (linkPath.Length > 0 && path_1 != linkPath && _comm.Omron.GetSwitchLock(e.LinkId) == 0)
                    {
                        _comm.Omron.SetSwitchLock(e.LinkId, 1);
                        // ★M4：回执来源随调用参数传递（连接 1=FINS）
                        int ackProto = 1;
                        int ackLinkId = e.LinkId;
                        Task.Run(() =>
                        {
                            try
                            {
                                // 方案切换涉及大量 UI 操作，收口到 UI 线程执行
                                if (InvokeRequired)
                                    Invoke(new Action(() => xinghao_qiehuan(linkPath, ackProto, ackLinkId)));
                                else
                                    xinghao_qiehuan(linkPath, ackProto, ackLinkId);
                            }
                            finally
                            {
                                // 无论成功失败都复位，防止该通道切换被永久锁死
                                _comm.Omron.SetSwitchLock(e.LinkId, 0);
                            }
                        });
                    }
                }
                else if (path_1 != _comm.Omron.lujing.Replace("\0", "") && qiehuanzhong == 0)
                {
                    if (_comm.Omron.camera_dic[13][2] == "true")
                    {
                        // ★M4：回执来源随调用参数传递（连接 1=FINS）
                        int ackProto = 1;
                        int ackLinkId = e.LinkId;
                        Task.Run(() =>
                        {
                            try
                            {
                                // 方案切换涉及大量 UI 操作，收口到 UI 线程执行（原在后台线程直接操作控件）
                                if (InvokeRequired)
                                    Invoke(new Action(() => xinghao_qiehuan(_comm.Omron.lujing.Replace("\0", ""), ackProto, ackLinkId)));
                                else
                                    xinghao_qiehuan(_comm.Omron.lujing.Replace("\0", ""), ackProto, ackLinkId);
                            }
                            finally
                            {
                                // 无论成功失败都复位，防止该通道切换被永久锁死
                                _comm.Omron.qiehuanzhong = 0;
                            }
                        });
                    }
                    else
                    {
                        // ★C4 修复：轮询侧命中切型已置 qiehuanzhong=1 并发了事件，但"使能"非 true 时
                        //   原实现既不建 Task 也不复位 → 该连接 qiehuanzhong 永久 1，TrySchemeSwitch 恒 false、
                        //   该通道切型静默死。与使能门控对称：未启用切换功能就释放请求锁。
                        _comm.Omron.qiehuanzhong = 0;
                    }
                }
                else
                {
                    // 同路径或切换中：不触发切换，也无需预置回执（fins_xie 无消费者），只复位切换锁
                    _comm.Omron.qiehuanzhong = 0;
                }
            }
            this.Invoke(new Action(() =>
            {
                textBox1.Text = e.Selection;
            }));
        }
        private void DataChange_modbustcp(object sender, FormModbus.SelectionChangedEventArgs e)
        {
            if (_jobs.yunxing)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (e.Camera != (i + 1).ToString()) continue;
                    // 阶段 7：补齐触发来源（原缺失，导致连接 2~4 触发的结果被广播回连接 1）
                    // 阶段 7 补强：连接2~4 触发参数取该连接自己 camera_dic 的绑定（对齐 FINS TryGetLinkCamera）
                    string[] linkCam;
                    if (e.LinkId > 1 && _comm.Modbustcp.TryGetLinkCamera(e.LinkId, i + 1, out linkCam))
                    {
                        GetCamTrigParams(linkCam, out string lmode, out string ltv1, out string ltv2);
                        _jobs.ApplyCommTrigger(_jobs.Myjobs[i], i, e.Selection, lmode, ltv1, ltv2, "modbustcp", new CommTriggerSource(e.LinkId, 2, e.Selection));
                        continue;
                    }
                    if (!_comm.Modbustcp.camera_dic.ContainsKey(i + 1)) continue;
                    GetCamTrigParams(_comm.Modbustcp.camera_dic[i + 1], out string mode, out string tv1, out string tv2);
                    _jobs.ApplyCommTrigger(_jobs.Myjobs[i], i, e.Selection, mode, tv1, tv2, "modbustcp", new CommTriggerSource(e.LinkId, 2, e.Selection));
                }
            }
            if (e.Camera.Contains("13"))
            {
                // 阶段 7 补强：连接 2~4 切方案走独立分支——用该连接自己的方案路径与切换锁，不占用连接 1 的 lujing/qiehuanzhong。
                if (e.LinkId > 1)
                {
                    string linkPath = (_comm.Modbustcp.GetLinkSchemePath(e.LinkId) ?? "").Replace("\0", "");
                    // 连接2~4 的切型锁已由本连接 TrySchemeSwitch 命中切型时置 1；此处以“仍持有锁”作为防重入保单，
                    // 切换完成后在 finally 复位（与连接 1 锁互不干扰）。
                    if (linkPath.Length > 0 && path_1 != linkPath && _comm.Modbustcp.GetSwitchLock(e.LinkId) != 0)
                    {
                        // ★M4：回执来源随调用参数传递（连接 1=ModbusTCP）
                        int ackProto = 2;
                        int ackLinkId = e.LinkId;
                        Task.Run(() =>
                        {
                            try
                            {
                                // 方案切换涉及大量 UI 操作，收口到 UI 线程执行
                                if (InvokeRequired)
                                    Invoke(new Action(() => xinghao_qiehuan(linkPath, ackProto, ackLinkId)));
                                else
                                    xinghao_qiehuan(linkPath, ackProto, ackLinkId);
                            }
                            finally
                            {
                                // 无论成功失败都复位，防止该通道切换被永久锁死
                                _comm.Modbustcp.SetSwitchLock(e.LinkId, 0);
                            }
                        });
                    }
                    else
                    {
                        // ★C4 修复：子实例轮询侧命中切型已置锁（qiehuanzhong=1），但主窗判定
                        //   "路径已相同/无需切换"时不进入上方分支 → 该连接锁永不复位，
                        //   TrySchemeSwitch 首行恒 false → 该通道切型静默死。此处补 else 复位本连接锁。
                        _comm.Modbustcp.SetSwitchLock(e.LinkId, 0);
                    }
                }
                else if (path_1 != _comm.Modbustcp.lujing.Replace("\0", "") && qiehuanzhong == 0)
                {
                    if (_comm.Modbustcp.camera_dic[13][2] == "true")
                    {
                        // ★M4：回执来源随调用参数传递（连接 1=ModbusTCP）
                        int ackProto = 2;
                        int ackLinkId = e.LinkId;
                        Task.Run(() =>
                        {
                            try
                            {
                                // 方案切换涉及大量 UI 操作，收口到 UI 线程执行（原在后台线程直接操作控件）
                                if (InvokeRequired)
                                    Invoke(new Action(() => xinghao_qiehuan(_comm.Modbustcp.lujing.Replace("\0", ""), ackProto, ackLinkId)));
                                else
                                    xinghao_qiehuan(_comm.Modbustcp.lujing.Replace("\0", ""), ackProto, ackLinkId);
                            }
                            finally
                            {
                                // 无论成功失败都复位，防止该通道切换被永久锁死
                                _comm.Modbustcp.qiehuanzhong = 0;
                            }
                        });
                    }
                    else
                    {
                        // ★C4 修复（同 FINS）：使能非 true 时释放请求锁，防该通道 qiehuanzhong 永久 1
                        _comm.Modbustcp.qiehuanzhong = 0;
                    }
                }
                else
                {
                    // 同路径或切换中：不触发切换，也无需预置回执（fins_xie 无消费者），只复位切换锁
                    _comm.Modbustcp.qiehuanzhong = 0;
                }
            }
            this.Invoke(new Action(() =>
            {
                textBox1.Text = e.Selection;
            }));
        }
        private void DataChange_modbusrtu(object sender, FormModbusRtu.SelectionChangedEventArgs e)
        {
            if (_jobs.yunxing)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (e.Camera != (i + 1).ToString()) continue;
                    // 阶段 7：补齐触发来源（原缺失，导致连接 2~4 触发的结果被广播回连接 1）
                    // 阶段 7 补强：连接2~4 触发参数取该连接自己 camera_dic 的绑定（对齐 FINS TryGetLinkCamera）
                    string[] linkCam;
                    if (e.LinkId > 1 && _comm.ModbusRtu.TryGetLinkCamera(e.LinkId, i + 1, out linkCam))
                    {
                        GetCamTrigParams(linkCam, out string lmode, out string ltv1, out string ltv2);
                        _jobs.ApplyCommTrigger(_jobs.Myjobs[i], i, e.Selection, lmode, ltv1, ltv2, "modbusrtu", new CommTriggerSource(e.LinkId, 3, e.Selection));
                        continue;
                    }
                    if (!_comm.ModbusRtu.camera_dic.ContainsKey(i + 1)) continue;
                    GetCamTrigParams(_comm.ModbusRtu.camera_dic[i + 1], out string mode, out string tv1, out string tv2);
                    _jobs.ApplyCommTrigger(_jobs.Myjobs[i], i, e.Selection, mode, tv1, tv2, "modbusrtu", new CommTriggerSource(e.LinkId, 3, e.Selection));
                }
            }
            if (e.Camera.Contains("13"))
            {
                // 阶段 7 补强：连接 2~4 切方案走独立分支——用该连接自己的方案路径与切换锁，不占用连接 1 的 lujing/qiehuanzhong。
                if (e.LinkId > 1)
                {
                    string linkPath = (_comm.ModbusRtu.GetLinkSchemePath(e.LinkId) ?? "").Replace("\0", "");
                    // 连接2~4 的切型锁已由本连接 TrySchemeSwitch 命中切型时置 1；此处以“仍持有锁”作为防重入保单，
                    // 切换完成后在 finally 复位（与连接 1 锁互不干扰）。
                    if (linkPath.Length > 0 && path_1 != linkPath && _comm.ModbusRtu.GetSwitchLock(e.LinkId) != 0)
                    {
                        // ★M4：回执来源随调用参数传递（连接 1=ModbusRTU）
                        int ackProto = 3;
                        int ackLinkId = e.LinkId;
                        Task.Run(() =>
                        {
                            try
                            {
                                // 方案切换涉及大量 UI 操作，收口到 UI 线程执行
                                if (InvokeRequired)
                                    Invoke(new Action(() => xinghao_qiehuan(linkPath, ackProto, ackLinkId)));
                                else
                                    xinghao_qiehuan(linkPath, ackProto, ackLinkId);
                            }
                            finally
                            {
                                // 无论成功失败都复位，防止该通道切换被永久锁死
                                _comm.ModbusRtu.SetSwitchLock(e.LinkId, 0);
                            }
                        });
                    }
                    else
                    {
                        // ★C4 修复（同 ModbusTCP）：已置锁但主窗判定无需切换时补复位，防该通道切型静默死
                        _comm.ModbusRtu.SetSwitchLock(e.LinkId, 0);
                    }
                }
                else if (path_1 != _comm.ModbusRtu.lujing.Replace("\0", "") && qiehuanzhong == 0)
                {
                    if (_comm.ModbusRtu.camera_dic[13][2] == "true")
                    {
                        // ★M4：回执来源随调用参数传递（连接 1=ModbusRTU）
                        int ackProto = 3;
                        int ackLinkId = e.LinkId;
                        Task.Run(() =>
                        {
                            try
                            {
                                // 方案切换涉及大量 UI 操作，收口到 UI 线程执行（原在后台线程直接操作控件）
                                if (InvokeRequired)
                                    Invoke(new Action(() => xinghao_qiehuan(_comm.ModbusRtu.lujing.Replace("\0", ""), ackProto, ackLinkId)));
                                else
                                    xinghao_qiehuan(_comm.ModbusRtu.lujing.Replace("\0", ""), ackProto, ackLinkId);
                            }
                            finally
                            {
                                // 无论成功失败都复位，防止该通道切换被永久锁死
                                _comm.ModbusRtu.qiehuanzhong = 0;
                            }
                        });
                    }
                    else
                    {
                        // ★C4 修复（同 FINS/ModbusTCP）：使能非 true 时释放请求锁
                        _comm.ModbusRtu.qiehuanzhong = 0;
                    }
                }
                else
                {
                    // 同路径或切换中：不触发切换，也无需预置回执（fins_xie 无消费者），只复位切换锁
                    _comm.ModbusRtu.qiehuanzhong = 0;
                }
            }
            this.Invoke(new Action(() =>
            {
                textBox1.Text = e.Selection;
            }));
        }
        public delegate void changedata(string data);
        public event changedata changedata_event;

        private void button9_Click(object sender, EventArgs e)
        {
            changedata_event?.Invoke(textBox1.Text);
            frm3.textBox9.Text = textBox1.Text;
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            ShowPictureList(textBox2, listBox4);
        }
        private void ShowPictureList(TextBox text, ListBox list)
        {
            if (_jobs.yunxing == false)
            {
                _jobs.myjob1.dlg.Dispose();
                if (_jobs.myjob1.dlg.ShowDialog() == DialogResult.OK)
                {
                    string dir = _jobs.myjob1.dlg.SelectedPath;

                    text.Text = dir;
                    // 清空显示
                    list.Items.Clear();

                    // 遍历所有的文件，检查文件名后缀
                    string[] fff = Directory.GetFiles(dir);
                    foreach (string f in fff)
                    {
                        if (f.EndsWith(".jpg")
                            || f.EndsWith(".jpeg")
                            || f.EndsWith(".png") || f.EndsWith(".bmp"))
                        {
                            // 取得文件名
                            PictureListItem item = new PictureListItem();
                            item.filePath = f;
                            item.name = Path.GetFileName(f);
                            // 加到列表框显示
                            list.Items.Add(item);
                        }
                    }

                    // 默认打开第一个文件显示
                    if (list.Items.Count > 0)
                        list.SetSelected(0, true);
                }

            }

        }
        private void Showjob(ComboBox box, int job_number)
        {

            // 遍历所有的文件，检查文件名后缀
            box.Items.Clear();
            string[] fff = Directory.GetFiles(wenjianjia + "\\" + job_number);
            foreach (string f in fff)
            {
                if (f.EndsWith(".vpp"))
                {
                    // 加到列表框显示
                    box.Items.Add(Path.GetFileName(f));
                }
            }
        }
        class PictureListItem
        {
            public string name;
            public string filePath;

            public override string ToString()
            {
                return name;
            }
        }

        private void listBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox4.SelectedItem;
                if (item == null) return;
                _jobs.myjob1.img = new Bitmap(item.filePath);
                if (_jobs.yunxing == false)
                {
                    _jobs.myjob1.trriger = 1;
                    getrecord(_jobs.myjob1, default(System.Collections.Generic.KeyValuePair<string, string>));
                }
            }
            catch { }

        }
        int trriger1_temp = 0;
        int trriger2_temp = 0;
        int trriger3_temp = 0;
        int trriger4_temp = 0;
        int trriger5_temp = 0;
        int trriger6_temp = 0;
        int trriger7_temp = 0;
        int trriger8_temp = 0;
        int trriger9_temp = 0;
        int trriger10_temp = 0;
        int trriger11_temp = 0;
        int trriger12_temp = 0;
        private void button10_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox4.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    _jobs.myjob1.img = new Bitmap(item.filePath);
                    if (_jobs.myjob1.trriger == 0)
                    {
                        _jobs.myjob1.trriger = 1;
                        getrecord(_jobs.myjob1, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger1_temp = 1;
                    timer7.Interval = int.Parse(textBox6.Text);
                    timer7.Enabled = true;
                }
            }
            // picture_trigger(listBox4,ref timer7, textBox6,ref _jobs.myjob1, out int trriger1_temp);
        }
        private void picture_trigger(ListBox list, ref System.Windows.Forms.Timer tim, TextBox text, ref Myjob myjob, out int trriger_temp)
        {
            trriger_temp = 0;
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)list.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                }
                else
                {
                    myjob.img = new Bitmap(item.filePath);
                    if (myjob.trriger == 0)
                    {
                        myjob.trriger = 1;
                        getrecord(myjob, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger_temp = 1;
                    tim.Interval = int.Parse(text.Text);
                    tim.Enabled = true;
                }
            }
        }
        CogPMAlignTool pma;//PMA工具全局变量
        private void 设置ROIToolStripMenuItem_Click(object sender, EventArgs e)
        {


        }
        private void menuitem_Click(object sender, EventArgs e)
        {
            label75.Text = sender.ToString().Split('\\').Last();
            label173.Text = sender.ToString();
            // ★修复（2026-09-20）：path_1 必须先更新为本次选中的方案，再取 wenjianjia——
            //   原顺序在 path_1 更新前取 Path.GetDirectoryName(path_1)（旧方案目录），
            //   导致日志"方案文件夹:"与分流程 vpp 目录（wenjianjia\N\xxx.vpp）都指向旧方案目录。
            path_1 = sender.ToString();
            try
            {
                wenjianjia = Path.GetDirectoryName(path_1);
                _logger.WriteLog("方案文件夹:" + wenjianjia);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex.Message);
            }
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(Path.GetDirectoryName(path_1) + "\\Menu.ini");
                int i = 0;
                while (sr.Peek() >= 0)
                {
                    i++;
                    sr.ReadLine();
                }
                sr.Dispose();
                sr.Close();
                if (i > 5)
                {
                    FileStream stream = null;
                    try
                    {
                        stream = File.Open(Path.GetDirectoryName(path_1) + "\\Menu.ini", FileMode.OpenOrCreate, FileAccess.Write);
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.SetLength(0);
                        stream.Flush();
                        stream.Close();
                    }
                    catch
                    {
                        stream.Flush();
                        stream.Close();
                    }
                }
            }
            catch
            {
                try
                {
                    sr.Dispose();
                    sr.Close();
                }
                catch { }

            };
            if (this.设置ToolStripMenuItem.DropDownItems[this.设置ToolStripMenuItem.DropDownItems.Count - 1].Text != path_1)
            {
                StreamWriter s = new StreamWriter(Path.GetDirectoryName(path_1) + "\\Menu.ini", true);
                s.WriteLine(path_1);
                s.Flush();
                s.Close();
            }
            // ★M4：手动切换不产生通讯回执（来源参数默认 0，无全局状态需清零）
            xinghao_qiehuan("");
        }
        ToolStripMenuItem menuitem;
        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        private void cogRecordDisplay2_MouseDown(object sender, MouseEventArgs e)
        {
            //try
            //{
            //    if (!blnDraw)
            //    {
            //        myTranform = cogRecordDisplay2.GetTransform(cogRecordDisplay2.Image.SelectedSpaceName, "*");
            //        myTranform.MapPoint(x2, y2, out out_x, out out_y);
            //        start.X = (int)out_x;
            //        start.Y = (int)out_y;
            //        //Invalidate();
            //        start_1 = e.Location;

            //        blnDraw = true;
            //    }
            //}
            //catch
            //{ }
        }
        private void cogRecordDisplay2_MouseUp(object sender, MouseEventArgs e)
        {
            //if (blnDraw)
            //{
            //    try
            //    {
            //        if (pma != null)
            //        {
            //            myTranform = cogRecordDisplay2.GetTransform(cogRecordDisplay2.Image.SelectedSpaceName, "*");
            //            myTranform.MapPoint(x2, y2, out out_x, out out_y);
            //            end222.X = (int)out_x;
            //            end222.Y = (int)out_y;
            //            tempEndPoint = end222; //记录框的位置和大小


            //            rect.SetXYWidthHeight(Math.Min(start.X, tempEndPoint.X), Math.Min(start.Y, tempEndPoint.Y), Math.Abs(start.X - tempEndPoint.X), Math.Abs(start.Y - tempEndPoint.Y));
            //            // rect.SetCenterWidthHeight(Math.Min(start.X, tempEndPoint.X), Math.Min(start.Y, tempEndPoint.Y), Math.Abs(start.X - tempEndPoint.X), Math.Abs(start.Y - tempEndPoint.Y));
            //            pma.SearchRegion = rect;
            //            //允许调整搜索区域大小
            //            rect.GraphicDOFEnable = CogRectangleDOFConstants.Position | CogRectangleDOFConstants.Size;
            //            //允许鼠标选择搜索区域
            //            rect.Interactive = true;
            //            CogRectangle mRectangle = new CogRectangle();
            //            mRectangle.X = Math.Min(start.X, tempEndPoint.X);
            //            mRectangle.Y = Math.Min(start.Y, tempEndPoint.Y);
            //            mRectangle.Width = Math.Abs(start.X - tempEndPoint.X);
            //            mRectangle.Height = Math.Abs(start.Y - tempEndPoint.Y);
            //            //添加到图像中
            //            cogRecordDisplay2.StaticGraphics.Add(mRectangle, "mRectangle");
            //            pma = null;
            //        }
            //    }
            //    catch(Exception ex)
            //    { _logger.WriteLog(ex.Message); };
            //}
            //blnDraw = false;

        }
        bool check = false;
        bool check2 = false;
        private void DataChangef1(object sender, WindowsFormsApplication1.SubSet.SelectionChangedEventArgs e)
        {
            check = e.Check;
            string select = e.Selection;
            CogToolBlock toolblock = e.Toolblock;
            Thread.Sleep(20);
            if (toolblock != null)
            {
                roiset1(toolblock, select, check);
            }
        }
        private void DataChangef2(object sender, WindowsFormsApplication1.SubSet.SelectionChangedEventArgs2 e)
        {
            check2 = e.Check;
            string select = e.Selection;
            CogToolBlock toolblock = e.Toolblock;
            Thread.Sleep(20);
            if (toolblock != null)
            {
                roiset1(toolblock, select, check2);
            }
        }
        private void cogRecordDisplay2_MouseMove(object sender, MouseEventArgs e)
        {
            //x2 = e.X;
            //y2 = e.Y;
            //try
            //{
            //    if (blnDraw)
            //    {
            //        if (e.Button != MouseButtons.Left)//判断是否按下左键
            //            return;

            //        end_1 = e.Location;
            //        //设置搜索区域





            //      //rect_1.Location = new Point(
            //      //Math.Min(start_1.X, end_1.X),
            //      //Math.Min(start_1.Y, end_1.Y));
            //      //rect_1.Size = new Size(
            //      //Math.Abs(start_1.X - end_1.X),
            //      //Math.Abs(start_1.Y - end_1.Y));
            //     // Invalidate();
            //      }
            //    }
            //    catch(Exception ex)
            //{
            //    _logger.WriteLog(ex.Message);     
            //};


        }

        private void cogRecordDisplay2_Click(object sender, EventArgs e)
        {
            //try
            //{
            //    if (x1 != out_x && y1 != out_y)
            //    {
            //        ICogTransform2D myTranform1 = cogRecordDisplay2.GetTransform(cogRecordDisplay2.Image.SelectedSpaceName, "*");
            //        myTranform1.MapPoint(x2, y2, out out_x, out out_y);
            //        Color c = new Color();
            //        Bitmap bitmap = new Bitmap(cogRecordDisplay2.Image.ToBitmap());
            //        c = bitmap.GetPixel((int)out_x, (int)out_y);
            //        label6.Text = string.Format("X={0},Y={1},R={2},G={3},B={4}", out_x, out_y, c.R, c.G, c.B);
            //        bitmap.Dispose();
            //    }
            //}
            //catch (Exception ex)
            //{ MessageBox.Show(ex.Message); }; 
        }

        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                保存ToolStripMenuItem.Enabled = false;
                SanitizeSchemeImagesForSave();
                CogSerializer.SaveObjectToFile(manager1, path_1);
                MessageBox.Show("保存方案成功!");
            }
            catch (Exception ex)
            {
                _logger.WriteLog("保存方案失败: " + ex.Message);
                MessageBox.Show("保存方案失败！若反复失败，请先重启软件后再保存。\r\n" + ex.Message);
            }
            finally
            {
                保存ToolStripMenuItem.Enabled = true;
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button11_Click_1(object sender, EventArgs e)
        {
                try
                {
                    checkBox70.CheckState = CheckState.Checked;
                    if (m_bGrabbing1 == true)
                    {
                        // ch:标志位设为false | en:Set flag bit false
                        m_bGrabbing1 = false;
                        // m_hReceiveThread.Join();
                        // ch:停止采集 | en:Stop Grabbing
                        int nRet = _cameraCtrl.Cameras[0].MV_CC_StopGrabbing_NET();
                        if (nRet != MyCamera.MV_OK)
                        {
                            ShowErrorMsg("Stop Grabbing Fail!", nRet);
                        }
                    }

                    bnStartGrab1.Enabled = false;
                    bnStopGrab1.Enabled = false;
                    this.Invoke(new Action(() =>
                    {
                        // 更新UI操作
                        button11.BackColor = Color.Green;
                        button1.Text = "运行";
                        button1.BackColor = Color.LightGreen;
                    }));

                    _jobs.myjob1.yun = 0;
                    // 停止时复位所有相机 IO 输出（防止输出线保持高电平无法恢复）
                    ResetMyJobOutputs();

                }
                catch (Exception ex)
                {
                    bnStartGrab1.Enabled = false;
                    bnStopGrab1.Enabled = false;
                    _jobs.myjob1.yun = 0;
                    _logger.WriteLog("相机1停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 1)
                    {
                        if (m_bGrabbing2 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing2 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[1].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab2.Enabled = false;
                        bnStopGrab2.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.Text = "运行";
                            button1.BackColor = Color.LightGreen;
                        }));


                        _jobs.myjob2.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab2.Enabled = false;
                    bnStopGrab2.Enabled = false;
                    _jobs.myjob2.yun = 0;
                    _logger.WriteLog("相机2停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 2)
                    {
                        if (m_bGrabbing3 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing3 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[2].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab3.Enabled = false;
                        bnStopGrab3.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob3.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab3.Enabled = false;
                    bnStopGrab3.Enabled = false;
                    _jobs.myjob3.yun = 0;
                    _logger.WriteLog("相机3停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 3)
                    {
                        if (m_bGrabbing4 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing4 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[3].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab4.Enabled = false;
                        bnStopGrab4.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob4.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab4.Enabled = false;
                    bnStopGrab4.Enabled = false;
                    _jobs.myjob4.yun = 0;
                    _logger.WriteLog("相机4停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 4)
                    {
                        if (m_bGrabbing5 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing5 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[4].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab5.Enabled = false;
                        bnStopGrab5.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob5.yun = 0;
                    }
                }
                catch (Exception ex)
                {
                    bnStartGrab5.Enabled = false;
                    bnStopGrab5.Enabled = false;
                    _jobs.myjob5.yun = 0;
                    _logger.WriteLog("相机5停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 5)
                    {
                        if (m_bGrabbing6 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing6 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[5].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab6.Enabled = false;
                        bnStopGrab6.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob6.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab6.Enabled = false;
                    bnStopGrab6.Enabled = false;
                    _jobs.myjob6.yun = 0;
                    _logger.WriteLog("相机6停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 6)
                    {
                        if (m_bGrabbing7 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing7 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[6].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab7.Enabled = false;
                        bnStopGrab7.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob7.yun = 0;
                    }
                }
                catch (Exception ex)
                {
                    bnStartGrab7.Enabled = false;
                    bnStopGrab7.Enabled = false;
                    _jobs.myjob7.yun = 0;
                    _logger.WriteLog("相机7停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 7)
                    {
                        if (m_bGrabbing8 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing8 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[7].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab8.Enabled = false;
                        bnStopGrab8.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob8.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab8.Enabled = false;
                    bnStopGrab8.Enabled = false;
                    _jobs.myjob8.yun = 0;
                    _logger.WriteLog("相机8停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 8)
                    {
                        if (m_bGrabbing9 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing9 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[8].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab9.Enabled = false;
                        bnStopGrab9.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob9.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab9.Enabled = false;
                    bnStopGrab9.Enabled = false;
                    _jobs.myjob9.yun = 0;
                    _logger.WriteLog("相机9停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 9)
                    {
                        if (m_bGrabbing10 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing10 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[9].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab10.Enabled = false;
                        bnStopGrab10.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob10.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab10.Enabled = false;
                    bnStopGrab10.Enabled = false;
                    _jobs.myjob10.yun = 0;
                    _logger.WriteLog("相机10停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 10)
                    {
                        if (m_bGrabbing11 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing11 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[10].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab11.Enabled = false;
                        bnStopGrab11.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob11.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab11.Enabled = false;
                    bnStopGrab11.Enabled = false;
                    _jobs.myjob11.yun = 0;
                    _logger.WriteLog("相机11停止" + ex.Message);
                }
                ;
                try
                {
                    if (manager1.JobCount > 11)
                    {
                        if (m_bGrabbing12 == true)
                        {
                            // ch:标志位设为false | en:Set flag bit false
                            m_bGrabbing12 = false;
                            // ch:停止采集 | en:Stop Grabbing
                            int nRet = _cameraCtrl.Cameras[11].MV_CC_StopGrabbing_NET();
                            if (nRet != MyCamera.MV_OK)
                            {
                                ShowErrorMsg("Stop Grabbing Fail!", nRet);
                            }
                        }

                        bnStartGrab12.Enabled = false;
                        bnStopGrab12.Enabled = false;
                        this.Invoke(new Action(() =>
                        {
                            // 更新UI操作
                            button11.BackColor = Color.Green;
                            button1.BackColor = Color.LightGreen;
                            button1.Text = "运行";
                        }));


                        _jobs.myjob12.yun = 0;
                    }

                }
                catch (Exception ex)
                {
                    bnStartGrab12.Enabled = false;
                    bnStopGrab12.Enabled = false;
                    _jobs.myjob12.yun = 0;
                    _logger.WriteLog("相机12停止" + ex.Message);
                }
                ;
                _jobs.yunxing = false;
                DisarmCommTrigger();
                numericUpDown3.Enabled = true;
                checkedListBox1.Enabled = true;
                if (存图测试1ToolStripMenuItem.Checked == false)
                    存图测试1ToolStripMenuItem_Click(null, null);
        }        

        private void button3_KeyDown(object sender, KeyEventArgs e)
        {
            // button3.BackColor = Color.Green;
        }

        private void button3_KeyUp(object sender, KeyEventArgs e)
        {
            // button3.BackColor = Color.LightGreen;
        }

        private void 触发设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (触发设置ToolStripMenuItem.CheckState == CheckState.Checked)
            {
                comboBox1.Enabled = false;
                comboBox4.Enabled = false;
                comboBox5.Enabled = false;
                comboBox8.Enabled = false;
                comboBox25.Enabled = false;
                comboBox28.Enabled = false;
                comboBox31.Enabled = false;
                comboBox34.Enabled = false;
                comboBox43.Enabled = false;
                comboBox47.Enabled = false;
                comboBox51.Enabled = false;
                comboBox55.Enabled = false;
                触发设置ToolStripMenuItem.Checked = false;
            }
            else
            {
                comboBox1.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
                comboBox8.Enabled = true;
                comboBox25.Enabled = true;
                comboBox28.Enabled = true;
                comboBox31.Enabled = true;
                comboBox34.Enabled = true;
                comboBox43.Enabled = true;
                comboBox47.Enabled = true;
                comboBox51.Enabled = true;
                comboBox55.Enabled = true;
                触发设置ToolStripMenuItem.Checked = true;

            }

        }

        private void 存图测试1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (存图测试1ToolStripMenuItem.CheckState == CheckState.Checked)
            {
                listBox4.Visible = false; listBox5.Visible = false;
                textBox2.Visible = false;
                listBox5.Visible = false;
                textBox10.Visible = false;
                listBox8.Visible = false;
                textBox12.Visible = false;
                listBox9.Visible = false;
                textBox17.Visible = false;
                listBox10.Visible = false;
                listBox11.Visible = false;
                listBox12.Visible = false;
                listBox13.Visible = false;
                textBox22.Visible = false;
                textBox28.Visible = false;
                textBox34.Visible = false;
                textBox40.Visible = false;
                listBox19.Visible = false;
                textBox43.Visible = false;
                listBox20.Visible = false;
                textBox46.Visible = false;
                listBox21.Visible = false;
                textBox49.Visible = false;
                listBox22.Visible = false;
                textBox52.Visible = false;
                button8.Visible = false;
                button10.Visible = false;
                button13.Visible = false;
                button23.Visible = false;
                button24.Visible = false;
                button25.Visible = false;
                button22.Visible = false;
                button26.Visible = false;
                button27.Visible = false;
                button37.Visible = false;
                button38.Visible = false;
                button39.Visible = false;
                button36.Visible = false;
                button40.Visible = false;
                button41.Visible = false;
                button52.Visible = false;
                button53.Visible = false;
                button54.Visible = false;
                button65.Visible = false;
                button66.Visible = false;
                button67.Visible = false;
                button78.Visible = false;
                button79.Visible = false;
                button80.Visible = false;
                button85.Visible = false;
                button90.Visible = false;
                button91.Visible = false;
                button92.Visible = false;
                button97.Visible = false;
                button98.Visible = false;
                button99.Visible = false;
                button104.Visible = false;
                button105.Visible = false;
                button106.Visible = false;
                button111.Visible = false;
                button112.Visible = false;
                dataGridView1.Visible = true;
                dataGridView2.Visible = true;
                dataGridView3.Visible = true;
                dataGridView4.Visible = true;
                dataGridView5.Visible = true;
                dataGridView6.Visible = true;
                dataGridView7.Visible = true;
                dataGridView8.Visible = true;
                checkBox11.Visible = true;
                checkBox8.Visible = true;
                checkBox56.Visible = true;
                checkBox57.Visible = true;
                checkBox58.Visible = true;
                checkBox59.Visible = true;
                checkBox60.Visible = true;
                checkBox61.Visible = true;
                dataGridView9.Visible = true;
                dataGridView10.Visible = true;
                dataGridView11.Visible = true;
                dataGridView12.Visible = true;
                checkBox73.Visible = true;
                checkBox79.Visible = true;
                checkBox85.Visible = true;
                checkBox91.Visible = true;
                存图测试1ToolStripMenuItem.Checked = false;
            }
            else
            {
                listBox5.Visible = true;
                textBox10.Visible = true;
                listBox4.Visible = true;
                textBox2.Visible = true;
                listBox8.Visible = true;
                textBox12.Visible = true;
                listBox9.Visible = true;
                textBox17.Visible = true;
                listBox10.Visible = true;
                listBox11.Visible = true;
                listBox12.Visible = true;
                listBox13.Visible = true;
                textBox22.Visible = true;
                textBox28.Visible = true;
                textBox34.Visible = true;
                textBox40.Visible = true;
                button8.Visible = true;
                button10.Visible = true;
                button13.Visible = true;
                button23.Visible = true;
                button24.Visible = true;
                button25.Visible = true;
                button22.Visible = true;
                button26.Visible = true;
                button27.Visible = true;
                button37.Visible = true;
                button38.Visible = true;
                button39.Visible = true;
                button36.Visible = true;
                button40.Visible = true;
                button41.Visible = true;
                button52.Visible = true;
                button53.Visible = true;
                button54.Visible = true;
                button65.Visible = true;
                button66.Visible = true;
                button67.Visible = true;
                button78.Visible = true;
                button79.Visible = true;
                button80.Visible = true;
                listBox19.Visible = true;
                textBox43.Visible = true;
                listBox20.Visible = true;
                textBox46.Visible = true;
                listBox21.Visible = true;
                textBox49.Visible = true;
                listBox22.Visible = true;
                textBox52.Visible = true;
                button85.Visible = true;
                button90.Visible = true;
                button91.Visible = true;
                button92.Visible = true;
                button97.Visible = true;
                button98.Visible = true;
                button99.Visible = true;
                button104.Visible = true;
                button105.Visible = true;
                button106.Visible = true;
                button111.Visible = true;
                button112.Visible = true;
                dataGridView1.Visible = false;
                dataGridView2.Visible = false;
                dataGridView3.Visible = false;
                dataGridView4.Visible = false;
                dataGridView5.Visible = false;
                dataGridView6.Visible = false;
                dataGridView7.Visible = false;
                dataGridView8.Visible = false;
                checkBox11.Visible = false;
                checkBox8.Visible = false;
                checkBox56.Visible = false;
                checkBox57.Visible = false;
                checkBox58.Visible = false;
                checkBox59.Visible = false;
                checkBox60.Visible = false;
                checkBox61.Visible = false;
                dataGridView9.Visible = false;
                dataGridView10.Visible = false;
                dataGridView11.Visible = false;
                dataGridView12.Visible = false;
                checkBox73.Visible = false;
                checkBox79.Visible = false;
                checkBox85.Visible = false;
                checkBox91.Visible = false;
                存图测试1ToolStripMenuItem.Checked = true;

            }
        }

        private void 输出时间ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (输出时间ToolStripMenuItem.CheckState == CheckState.Checked)
            {
                textBox3.Enabled = false;
                输出时间ToolStripMenuItem.Checked = false;
            }
            else
            {
                textBox3.Enabled = true;
                输出时间ToolStripMenuItem.Checked = true;

            }
        }

        private void rOI设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (rOI设置ToolStripMenuItem.CheckState == CheckState.Checked)
            {
                pma = null;
                if (cra != null)   // 未设置过 ROI 时 cra 为 null，避免空引用
                {
                    cra.Interactive = false;
                    cra = null;
                }
                rOI设置ToolStripMenuItem.Checked = false;
            }
            else
            {
                try
                {
                    pma = _jobs.myjob1.block.Tools["CogPMAlignTool1"] as CogPMAlignTool;
                }
                catch
                {
                    pma = _jobs.myjob1.block.Tools["模版匹配"] as CogPMAlignTool;
                }
                rOI设置ToolStripMenuItem.Checked = true;
                cra = new CogRectangleAffine();
                cra = (CogRectangleAffine)pma.SearchRegion;
                cra.Interactive = true;
                cra.SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                cra.SelectedSpaceName = "#";//设置形状的坐标空间
                cra.GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
                cogRecordDisplay1.InteractiveGraphics.Add(cra, "roi", false);
            }
        }
        private void roiset2(CogToolBlock blocktool, ICogTool tool, bool check, CogRecordDisplay record)
        {
            if (record == null || tool == null) return;   // 显示目标或工具为空时跳过
            CogPMAlignTool Pmatemp1;
            CogBlobTool Blobtemp1;
            CogPMAlignMultiTool multipm;
            Dictionary<string, ICogRegion> cra2 = new Dictionary<string, ICogRegion>();
            cra2.Add("Cognex.VisionPro.CogRectangleAffine", new CogRectangleAffine());
            cra2.Add("Cognex.VisionPro.CogPolygon", new CogPolygon());
            cra2.Add("Cognex.VisionPro.CogRectangle", new CogRectangle());
            cra2.Add("Cognex.VisionPro.CogCircle", new CogCircle());
            if (check == true)
            {
                if (tool is CogPMAlignTool)
                {
                    Pmatemp1 = tool as CogPMAlignTool;
                    if (Pmatemp1.SearchRegion == null || Pmatemp1.InputImage == null)
                    {
                        MessageBox.Show("该工具未设置搜索区域或未运行检测，无法显示ROI");
                        return;
                    }
                    switch (Pmatemp1.SearchRegion.GetType().ToString())

                    {
                        case "Cognex.VisionPro.CogRectangleAffine":
                            cra2[Pmatemp1.SearchRegion.GetType().ToString()] = (CogRectangleAffine)Pmatemp1.SearchRegion;
                            ((CogRectangleAffine)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogRectangleAffine)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogRectangleAffine)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedSpaceName = Pmatemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogRectangleAffine)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogRectangleAffine)cra2[Pmatemp1.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogPolygon":
                            cra2[Pmatemp1.SearchRegion.GetType().ToString()] = (CogPolygon)Pmatemp1.SearchRegion;
                            ((CogPolygon)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogPolygon)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogPolygon)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedSpaceName = Pmatemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogPolygon)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogPolygonDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogPolygon)cra2[Pmatemp1.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogRectangle":
                            cra2[Pmatemp1.SearchRegion.GetType().ToString()] = (CogRectangle)Pmatemp1.SearchRegion;
                            ((CogRectangle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogRectangle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogRectangle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedSpaceName = Pmatemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogRectangle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogRectangleDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogRectangle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogCircle":
                            cra2[Pmatemp1.SearchRegion.GetType().ToString()] = (CogCircle)Pmatemp1.SearchRegion;
                            ((CogCircle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogCircle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogCircle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).SelectedSpaceName = Pmatemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogCircle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogCircleDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogCircle)cra2[Pmatemp1.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        default:
                            break;
                    };

                }
                else if (tool is CogBlobTool)
                {
                    Blobtemp1 = tool as CogBlobTool;
                    if (Blobtemp1.Region == null || Blobtemp1.InputImage == null)
                    {
                        MessageBox.Show("该工具未设置区域或未运行检测，无法显示ROI");
                        return;
                    }
                    switch (Blobtemp1.Region.GetType().ToString())

                    {
                        case "Cognex.VisionPro.CogRectangleAffine":
                            cra2[Blobtemp1.Region.GetType().ToString()] = (CogRectangleAffine)Blobtemp1.Region;
                            ((CogRectangleAffine)cra2[Blobtemp1.Region.GetType().ToString()]).Interactive = true;
                            ((CogRectangleAffine)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogRectangleAffine)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedSpaceName = Blobtemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogRectangleAffine)cra2[Blobtemp1.Region.GetType().ToString()]).GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogRectangleAffine)cra2[Blobtemp1.Region.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogPolygon":
                            cra2[Blobtemp1.Region.GetType().ToString()] = (CogPolygon)Blobtemp1.Region;
                            ((CogPolygon)cra2[Blobtemp1.Region.GetType().ToString()]).Interactive = true;
                            ((CogPolygon)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogPolygon)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedSpaceName = Blobtemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogPolygon)cra2[Blobtemp1.Region.GetType().ToString()]).GraphicDOFEnable = CogPolygonDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogPolygon)cra2[Blobtemp1.Region.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogRectangle":
                            cra2[Blobtemp1.Region.GetType().ToString()] = (CogRectangle)Blobtemp1.Region;
                            ((CogRectangle)cra2[Blobtemp1.Region.GetType().ToString()]).Interactive = true;
                            ((CogRectangle)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogRectangle)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedSpaceName = Blobtemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogRectangle)cra2[Blobtemp1.Region.GetType().ToString()]).GraphicDOFEnable = CogRectangleDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogRectangle)cra2[Blobtemp1.Region.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogCircle":
                            cra2[Blobtemp1.Region.GetType().ToString()] = (CogCircle)Blobtemp1.Region;
                            ((CogCircle)cra2[Blobtemp1.Region.GetType().ToString()]).Interactive = true;
                            ((CogCircle)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogCircle)cra2[Blobtemp1.Region.GetType().ToString()]).SelectedSpaceName = Blobtemp1.InputImage.SelectedSpaceName;//设置形状的坐标空间
                            ((CogCircle)cra2[Blobtemp1.Region.GetType().ToString()]).GraphicDOFEnable = CogCircleDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogCircle)cra2[Blobtemp1.Region.GetType().ToString()]), "roi", false);
                            break;
                        default:
                            break;
                    };
                }
                else if (tool is CogPMAlignMultiTool)
                {
                    multipm = tool as CogPMAlignMultiTool;
                    if (multipm.SearchRegion == null)
                    {
                        MessageBox.Show("该工具未设置搜索区域，无法显示ROI");
                        return;
                    }
                    switch (multipm.SearchRegion.GetType().ToString())

                    {
                        case "Cognex.VisionPro.CogRectangleAffine":
                            cra2[multipm.SearchRegion.GetType().ToString()] = (CogRectangleAffine)multipm.SearchRegion;
                            ((CogRectangleAffine)cra2[multipm.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogRectangleAffine)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogRectangleAffine)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedSpaceName = ".";//设置形状的坐标空间
                            ((CogRectangleAffine)cra2[multipm.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogRectangleAffine)cra2[multipm.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogPolygon":
                            cra2[multipm.SearchRegion.GetType().ToString()] = (CogPolygon)multipm.SearchRegion;
                            ((CogPolygon)cra2[multipm.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogPolygon)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogPolygon)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedSpaceName = ".";//设置形状的坐标空间
                            ((CogPolygon)cra2[multipm.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogPolygonDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogPolygon)cra2[multipm.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogRectangle":
                            cra2[multipm.SearchRegion.GetType().ToString()] = (CogRectangle)multipm.SearchRegion;
                            ((CogRectangle)cra2[multipm.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogRectangle)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogRectangle)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedSpaceName = ".";//设置形状的坐标空间
                            ((CogRectangle)cra2[multipm.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogRectangleDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogRectangle)cra2[multipm.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        case "Cognex.VisionPro.CogCircle":
                            cra2[multipm.SearchRegion.GetType().ToString()] = (CogCircle)multipm.SearchRegion;
                            ((CogCircle)cra2[multipm.SearchRegion.GetType().ToString()]).Interactive = true;
                            ((CogCircle)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                            ((CogCircle)cra2[multipm.SearchRegion.GetType().ToString()]).SelectedSpaceName = ".";//设置形状的坐标空间
                            ((CogCircle)cra2[multipm.SearchRegion.GetType().ToString()]).GraphicDOFEnable = CogCircleDOFConstants.All;
                            record.InteractiveGraphics.Add(((CogCircle)cra2[multipm.SearchRegion.GetType().ToString()]), "roi", false);
                            break;
                        default:
                            break;
                    };
                }
            }
        }
        private void roiset1(CogToolBlock blocktool, string name, bool check)
        {
            CogPMAlignTool Pmatemp1;
            CogBlobTool Blobtemp1;
            if (check == true)
            {
                if (name.Contains("CogPMAlignTool") || name.Contains("定位") || name.Contains("模板"))
                {
                    if (!blocktool.Tools.Cast<ICogTool>().Any(t => t.Name == name)) return;
                    Pmatemp1 = blocktool.Tools[name] as CogPMAlignTool;
                    if (Pmatemp1 == null || Pmatemp1.SearchRegion == null) return;   // 未设置搜索区域时跳过
                    cra = Pmatemp1.SearchRegion as CogRectangleAffine;
                    if (cra == null) return;   // 非矩形搜索区域暂不在此界面显示
                    cra.Interactive = true;
                    cra.SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                    cra.SelectedSpaceName = "#";//设置形状的坐标空间
                    cra.GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
                    cogRecordDisplay1.InteractiveGraphics.Add(cra, "roi", false);
                }
                if (name.Contains("CogBlobTool") || name.Contains("斑点"))
                {
                    if (!blocktool.Tools.Cast<ICogTool>().Any(t => t.Name == name)) return;
                    Blobtemp1 = blocktool.Tools[name] as CogBlobTool;
                    if (Blobtemp1 == null || Blobtemp1.Region == null) return;   // 未设置区域时跳过
                    cra = Blobtemp1.Region as CogRectangleAffine;
                    if (cra == null) return;   // 非矩形区域暂不在此界面显示
                    cra.Interactive = true;
                    cra.SelectedColor = CogColorConstants.Yellow;//选中时图形的颜色
                    cra.SelectedSpaceName = "#";//设置形状的坐标空间
                    cra.GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
                    cogRecordDisplay1.InteractiveGraphics.Add(cra, "roi", false);
                }
            }
            else
            {
                if (cra != null)
                {
                    cra.Interactive = false;
                    cra = null;
                }
            }
        }
        private void 注销ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frm5.mark = 0;
            this.Invoke(new Action(() =>
            {
                设置ToolStripMenuItem.Enabled = false;
                numericUpDown22.Enabled = false;
                button31.Enabled = false;
                textBox3.Enabled = false;
                comboBox1.Enabled = false;
                comboBox4.Enabled = false;
                comboBox5.Enabled = false;
                comboBox8.Enabled = false;
                触发设置ToolStripMenuItem.Checked = false;
                输出时间ToolStripMenuItem.Checked = false;
                存图测试1ToolStripMenuItem.Checked = false;

                rOI设置ToolStripMenuItem.Checked = false;
                foreach (Control item in this.panel1.Controls)
                {
                    if (item is Form)
                    {
                        ((Form)item).Close();
                    }
                }

                groupBox1.Visible = true;
                groupBox5.Visible = true;
                groupBox7.Visible = true;
                groupBox8.Visible = true;
                groupBox18.Visible = true;
                groupBox19.Visible = true;
                groupBox20.Visible = true;
                groupBox21.Visible = true;
                groupBox22.Visible = true;
                groupBox27.Visible = true;
                groupBox28.Visible = true;
                groupBox29.Visible = true;
            }));
        }

        private void 用户登录ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frm5.Visible == false)
                frm5.Visible = true;
            else
                frm5.Visible = false;
        }

        private void timer6_Tick(object sender, EventArgs e)
        {
            if (button1.Text == "运行中")
                frm5.monitor = 1;
            else
                frm5.monitor = 0;
            //this.Invoke(new Action(() =>
            // {

            // }));
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            if ((int)DateTime.Now.ToOADate() > 44460 && textBox4.Text.Trim().Length > 10)
            {
                try
                {
                    if (textBox7.Text.Trim().Length > 2)
                    {
                        string inputCode = textBox4.Text.Trim();

                        // ★ 安全解析到期日期
                        int time111 = 0;
                        string last2 = inputCode.Substring(inputCode.Length - 2);
                        if (!int.TryParse(last2, out time111) || time111 < 0 || time111 + 5 > inputCode.Length)
                        {
                            MessageBox.Show("解码失败：加密码格式不正确！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        string dateStr = inputCode.Substring(time111, 5);
                        int expiryDate = 0;
                        if (!int.TryParse(dateStr, out expiryDate) || expiryDate <= 0)
                        {
                            MessageBox.Show("解码失败：加密码中的日期无效！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // ★ 确保 duini 已初始化
                        try
                        {
                            if (string.IsNullOrEmpty(duini.FileName))
                                duini.ReadINIFile("C:\\Program Files\\test.ini");
                        }
                        catch { }

                        // ★ 先写 test.ini（授权数据），再写 code.ini（加密码）
                        bool testIniOk = false;
                        try
                        {
                            duini.WriteString("1", "4", ((int)DateTime.Now.ToOADate()).ToString());
                            duini.WriteString("1", "3", ((int)DateTime.Now.ToOADate()).ToString());
                            duini.WriteString("1", "2", expiryDate.ToString());
                            duini.WriteString("1", "1", textBox7.Text.Trim());
                            testIniOk = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog("解码: 写test.ini失败: " + ex.Message);
                        }

                        // ★ 再写 code.ini（加密码）
                        bool codeIniOk = false;
                        try
                        {
                            _config.WriteString("code1", "code2", inputCode);
                            codeIniOk = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog("解码: 写code.ini失败: " + ex.Message);
                        }

                        if (testIniOk && codeIniOk)
                        {
                            MessageBox.Show("解码结束，请重启软件!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (testIniOk && !codeIniOk)
                        {
                            MessageBox.Show("授权数据已写入，但加密码写入失败，请重试!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            MessageBox.Show("授权数据写入失败，请以管理员身份运行权限开通工具后重试！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("解码失败，请填写备注!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("解码按钮异常: " + ex.Message);
                    MessageBox.Show("解码异常: " + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        CogRectangleAffine cra;

        private void rOIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cra = new CogRectangleAffine();
            cra = (CogRectangleAffine)pma.SearchRegion;
            // cra.SetCenterLengthsRotationSkew(50, 50, 100, 100, 0, 0);
            cra.Interactive = true;
            cra.GraphicDOFEnable = CogRectangleAffineDOFConstants.All;
            cogRecordDisplay1.InteractiveGraphics.Add(cra, "roi", false);
        }

        private void 存图测试2ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void 另存为ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog openFileDialog = new SaveFileDialog();
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                if (openFileDialog.FileName != "")
                {
                    if (!openFileDialog.FileName.Contains(".vpp"))
                    {
                        openFileDialog.FileName = openFileDialog.FileName + ".vpp";
                        path_1 = openFileDialog.FileName;
                    }
                    else
                        path_1 = openFileDialog.FileName;
                }
                if (label75.Text != openFileDialog.FileName.Split('\\').Last())
                {
                    label75.Text = openFileDialog.FileName.Split('\\').Last();
                    label173.Text = openFileDialog.FileName;
                }
                try
                {
                    wenjianjia = Path.GetDirectoryName(path_1);
                    _logger.WriteLog("方案文件夹:" + wenjianjia);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                }
            }
            else
                return;
            try
            {
                SanitizeSchemeImagesForSave();
                CogSerializer.SaveObjectToFile(manager1, path_1);
                _config.WriteString("path", "path_1", path_1);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("另存为方案失败: " + ex.Message);
                MessageBox.Show("保存方案失败！若反复失败，请先重启软件后再保存。\r\n" + ex.Message);
            }
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VP vpp File|*.vpp*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                label75.Text = openFileDialog.FileName.Split('\\').Last();
                label173.Text = openFileDialog.FileName;
                path_1 = openFileDialog.FileName;
                try
                {
                    wenjianjia = Path.GetDirectoryName(path_1);
                    _logger.WriteLog("方案文件夹:" + wenjianjia);
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                }
            }
            else
                return;
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(Path.GetDirectoryName(path_1) + "\\Menu.ini");
                int i = 0;
                while (sr.Peek() >= 0)
                {
                    i++;
                    sr.ReadLine();
                }
                sr.Dispose();
                sr.Close();
                if (i > 5)
                {
                    FileStream stream = null;
                    try
                    {
                        stream = File.Open(Path.GetDirectoryName(path_1) + "\\Menu.ini", FileMode.OpenOrCreate, FileAccess.Write);
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.SetLength(0);
                        stream.Flush();
                        stream.Close();
                    }
                    catch
                    {
                        stream.Flush();
                        stream.Close();
                    }
                }
            }
            catch
            {
                try
                {
                    sr.Dispose();
                    sr.Close();
                }
                catch { }

            };
            if (this.设置ToolStripMenuItem.DropDownItems[this.设置ToolStripMenuItem.DropDownItems.Count - 1].Text != path_1)
            {
                StreamWriter s = new StreamWriter(Path.GetDirectoryName(path_1) + "\\Menu.ini", true);
                s.WriteLine(path_1);
                s.Flush();
                s.Close();
            }
            // ★M4：手动切换不产生通讯回执（来源参数默认 0，无全局状态需清零）
            xinghao_qiehuan("");
        }
        private volatile int qiehuanzhong = 1;
        // ===== 方案切换回执（相机13）：由哪条通讯链路发起 → 随 xinghao_qiehuan/SendSchemeSwitchAck
        //   参数显式传递（★M4：原全局单槽 回执协议号/连接号 在两条连接同时切型时后写覆盖先写，
        //   会丢回执/把回执发到另一条连接；传参后每条切型自带来源，互不覆盖、无残留）=====
        #endregion
        #region 方案加载与切换
        /// <summary>
        /// 方案切换：停止→丢帧→关相机→卸载旧方案→加载新方案→重开相机→恢复参数→自动运行
        /// </summary>
        /// <summary>
        /// ★P0：切型中止 / 提前退出时统一回滚切换门控。
        ///   不回滚会永久卡死：_switchingScheme=true → EnqueueInspectFrame/InspectWorker 丢弃所有帧（全系统停检）；
        ///   qiehuanzhong=1 → 后续切型被永久拒绝。
        /// </summary>
        private void RollbackSchemeSwitch()
        {
            _switchingScheme = false;
            _inspectionLifecycle.Resume();
            Interlocked.Exchange(ref qiehuanzhong, 0);
            // ★M4：来源随参数传递，无全局状态需清零
            // 恢复切换开始时被隐藏的按钮，否则中止后用户点不到【运行】、无法继续生产；
            // 本方法可能在后台线程调用 → 走 SafeBeginInvoke（关闭流程中 _disposingFlag=true 时会被安全丢弃）
            try { SafeBeginInvoke(new Action(() => { try { button1.Visible = true; } catch { } })); } catch { }
        }

        /// <summary>★C5：切方案前言——隐藏运行按钮 →（停运行）→ 停检测/解触发/排空帧 → 弹切换进度窗。
        /// 原实现直接在 xinghao_qiehuan 里裸执行且无 try：任一处抛异常（控件访问/事件回调/Show 窗体）
        /// 会让本次切换死在半途——qiehuanzhong 永久 1（后续切型被永久拒绝）+ button1 永久隐藏
        /// + _switchingScheme 永久 true（全系统停止检测）。返回 false 表示前言失败、调用方须回滚。
        /// （本方法由 UI 线程调用，可安全访问控件。）</summary>
        private bool SchemeSwitchPreface(string a)
        {
            try
            {
                button1.Visible = false;
                if (a.Contains(".vpp"))
                {
                    label75.Text = a.Split('\\').Last();
                    label173.Text = a;
                    path_1 = a;
                    try
                    {
                        wenjianjia = Path.GetDirectoryName(path_1);
                        _logger.WriteLog("方案文件夹:" + wenjianjia);
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog(ex.Message);
                    }
                }
                if (_jobs.yunxing == true)
                    button11_Click_1(null, null);
                _jobs.yunxing = false;

                // ★ 切换开始：丢弃回调帧 + 解除通讯触发，防止旧帧/旧触发打到新方案
                _switchingScheme = true;
                _inspectionLifecycle.StopAccepting();
                DisarmCommTrigger();
                DrainPendingFrames();

                if (_comm.Omron.qiehuanzhong == 0)
                {
                    Frm2 = new Frm2();
                    Frm2.Show();
                    Frm2.start = 0;
                    UpdateSplashProgress(5, "正在准备切换方案...");
                }
                // DeviceListAcq();
                // bnClose_Click(null, null);
                return true;
            }
            catch (Exception exPre)
            {
                _logger.WriteLog("切方案前言异常，将回滚切换门控（未卸载方案）：" + exPre.Message);
                return false;
            }
        }

        private void xinghao_qiehuan(string a, int ackProto = 0, int ackLinkId = 1)
        {
            if (Interlocked.CompareExchange(ref qiehuanzhong, 1, 0) == 0)
            {
                qiehuanzhong = 1;
                // ★C6 修复：入参校验——路径必须含 .vpp（VisionPro 方案文件）。
                //   原实现把"设置 path_1"放在 if 里、不含 .vpp 时跳过设置却继续切换：
                //   会完整卸载/重载旧方案（几十秒）并照样回执"切换成功"，PLC 以为换型完成实际没换（质量逃逸）。
                // ★修复（2026-09-20）：手动切换入口（menuitem_Click 8494/9972 等）在调用前已把目标路径写入 path_1，
                //   并以空参数调用本方法；而 C6 校验会直接拒绝空参数 → 手动切换整体失效
                //   （现场日志："切方案请求被拒绝：路径参数无效（不含 .vpp）："）。此处把空参数归一化为 path_1 再校验。
                if (string.IsNullOrWhiteSpace(a)) a = path_1;
                if (string.IsNullOrWhiteSpace(a) || !a.Contains(".vpp"))
                {
                    _logger.WriteLog("切方案请求被拒绝：路径参数无效（不含 .vpp）：" + a);
                    try { RollbackSchemeSwitch(); } catch { }
                    return;
                }
                // ★C1：旧路径快照——两段式切型在"新方案加载失败回退"时用它在还原 path_1/label，
                //   避免把无效新路径写进配置与界面（加载成功时用新路径，本快照弃用）。
                string oldPathSnapshot = path_1;
                // ★C5 修复：前言移入 SchemeSwitchPreface 并整体包 try——失败即回滚门控并退出本次切换，
                //   不再出现"切换死在半途导致切型永久拒绝 + 运行按钮永久隐藏 + 全系统停止检测"。
                if (!SchemeSwitchPreface(a))
                {
                    // ★③修复：SchemeSwitchPreface 已把 label75/label173/path_1 改成新方案，
                    //   前言失败回滚时必须恢复为切型前的旧方案，否则界面方案名与实际不符（纯显示，重启自愈）。
                    try
                    {
                        if (!string.IsNullOrEmpty(oldPathSnapshot))
                        {
                            label75.Text = oldPathSnapshot.Split('\\').Last();
                            label173.Text = oldPathSnapshot;
                            path_1 = oldPathSnapshot;
                        }
                    }
                    catch { }
                    try { RollbackSchemeSwitch(); } catch { }
                    return;
                }
                Task.Run(() =>
                {
                    // ★ 排空：等待正在执行的检测帧结束（最长 10 秒）。
                    //   避免 block.Run 未结束就 Shutdown manager1，造成切方案线程与检测线程
                    //   并发操作同一批 VisionPro block/CogJobManager（COM 冲突/卡死）。
                    if (!_inspectionLifecycle.WaitForIdle(10000))
                    {
                        // ★ 2026-09-13 修复：等待 10 秒后仍有检测帧未结束，说明该帧耗时异常甚至卡死。
                        //   原实现只记一条警告就继续 manager1.Shutdown()，会与仍在执行的 block.Run 并发操作
                        //   同一批 VisionPro block/CogJobManager（COM 冲突/卡死风险）。
                        //   改为「中止本次切换」：不卸载方案、不并发 Shutdown，fail-stop 并提示用户重启，
                        //   避免把系统拖入不可恢复的卡死状态。
                        _logger.WriteLog("切方案中止：等待 10 秒后仍有检测帧未结束，已中止切换（未卸载方案、未并发 Shutdown），请检查方案单帧耗时或重启软件。");
                        // ★P0 修复：中止路径必须回滚切换门控，否则系统被永久卡死：
                        //   ① _switchingScheme 永久为 true → EnqueueInspectFrame/InspectWorker 丢弃所有帧（全系统停止检测）；
                        //   ② qiehuanzhong 永久为 1 → 之后任何切型都被永久拒绝；
                        //   ③ 不发"切换成功"假回执（中止路径不调用 SendSchemeSwitchAck），PLC 侧靠超时判 NG。
                        RollbackSchemeSwitch();
                        try
                        {
                            SafeBeginInvoke(new Action(() =>
                            {
                                try { if (Frm2 != null && !Frm2.IsDisposed) Frm2.Close(); } catch { }
                                try { button1.Visible = true; } catch { }
                                try { display(); } catch { }
                                try { MessageBox.Show("方案切换已中止：检测线程长时间未结束，本次未卸载方案（仍在用原方案）。请点击【运行】继续生产，或重启软件后重试切换。", "切换中止", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch { }
                            }));
                        }
                        catch { }
                        return;   // 不再执行 Shutdown / 加载新方案
                    }
                    // Callbacks are drained; the close handler also updates WinForms controls.
                    // ★P0 修复：这两条提前退出路径同样必须回滚切换门控，否则 _switchingScheme 永久为 true
                    //   → 全系统停止检测、后续切型被永久拒绝。关闭流程被取消时尤其致命：
                    //   _disposingFlag 已被复位，而本方法早已 return，无人再复位这些标志。
                    if (_disposingFlag)
                    {
                        RollbackSchemeSwitch();
                        return;
                    }
                    try { Invoke(new Action(() => bnClose_Click(null, null))); }
                    catch (Exception exClose)
                    {
                        _logger.WriteLog("切方案中止：关闭相机失败 " + exClose.Message);
                        RollbackSchemeSwitch();
                        return;
                    }
                    UpdateSplashProgress(10, "正在停止检测...");
                    _jobs.myjob1.baoguang = 0;
                    _jobs.myjob2.baoguang = 0;
                    _jobs.myjob3.baoguang = 0;
                    _jobs.myjob4.baoguang = 0;
                    _jobs.myjob5.baoguang = 0;
                    _jobs.myjob6.baoguang = 0;
                    _jobs.myjob7.baoguang = 0;
                    _jobs.myjob8.baoguang = 0;
                    int bbtemp = 7368;
                    try
                    {
                        listBox2.Visible = false;
                        listBox2.Items.Clear();
                        // ★C1 修复：两段式切换——先独立加载新方案（旧 manager1 及其 job/block 引用在此期间保持可用），
                        //   加载成功后【才】Shutdown 旧方案并替换。原实现"先 Shutdown 再加载"：加载失败时旧方案
                        //   已被拆、新方案又没有（manager1=null）→ 相机已关、系统停在无方案状态，只能重启软件。
                        //   加载失败现走完整回退：还原 path_1 + 重开相机 + 恢复运行，继续用原方案生产。
                        try
                        {
                            UpdateSplashProgress(20, "正在加载新方案...");
                            CogJobManager newMgr = null;
                            Exception loadEx = null;
                            try
                            {
                                // ★ 加载是耗时点：期间进度条缓慢爬升（20%→40%）
                                System.Threading.CancellationTokenSource switchCts = new System.Threading.CancellationTokenSource();
                                System.Threading.Tasks.Task switchTask = System.Threading.Tasks.Task.Run(() =>
                                {
                                    int v = 20;
                                    while (!switchCts.IsCancellationRequested && v < 40)
                                    {
                                        System.Threading.Thread.Sleep(250);
                                        v++;
                                        UpdateSplashProgress(v, "正在加载新方案...");
                                    }
                                });
                                managerState = ManagerState.Loading; // ★ 切换方案时重置状态
                                try
                                {
                                    newMgr = (CogJobManager)CogSerializer.LoadObjectFromFile(path_1);
                                }
                                finally
                                {
                                    switchCts.Cancel();   // 停止爬升动画
                                }
                            }
                            catch (Exception exLoad)
                            {
                                loadEx = exLoad;
                            }
                            if (newMgr == null)
                            {
                                // ── 加载失败：旧方案从未被卸载 → 完整回退，重开相机继续用原方案 ──
                                managerState = ManagerState.Loaded;   // 旧 manager1 仍是可用状态
                                string _oldP = string.IsNullOrEmpty(oldPathSnapshot) ? path_1 : oldPathSnapshot;
                                path_1 = _oldP;
                                try { wenjianjia = Path.GetDirectoryName(path_1); } catch { }
                                _logger.WriteLog("切方案失败（新方案加载异常），已回退继续使用原方案：" + (loadEx == null ? "加载返回空" : loadEx.Message));
                                // 先复位门控（不阻塞）；回退不发"切换成功"回执（PLC 靠超时判 NG）
                                _switchingScheme = false;
                                _inspectionLifecycle.Resume();
                                Interlocked.Exchange(ref qiehuanzhong, 0);
                                this.Invoke(new Action(() =>
                                {
                                    try { label75.Text = _oldP.Split('\\').Last(); } catch { }
                                    try { label173.Text = _oldP; } catch { }
                                    try { label133.Text = "切方案失败，已继续使用原方案"; } catch { }
                                    try { if (Frm2 != null && !Frm2.IsDisposed) Frm2.start = 1; } catch { }
                                    try { button1.Visible = true; } catch { }
                                    // 重开相机并恢复运行（复用成功收尾同一套动作；此时 block 引用仍是旧方案的）
                                    try { bnOpen_Click(null, null); } catch (Exception exOp) { _logger.WriteLog("切方案回退重开相机失败：" + exOp.Message); }
                                    try { display(); } catch { }
                                    try { trriger_set(); } catch { }
                                    try { bnSetParam_Click(null, null); bnGetParam_Click(null, null); } catch { }
                                    try { comboBox38_TextChanged(null, null); } catch { }
                                    try { button1_Click(null, null); } catch { }
                                    try { MessageBox.Show("方案切换失败：新方案无法加载，已回退继续使用原方案（相机已重新打开、运行已恢复）。请检查方案文件后重试。\n\n" + (loadEx == null ? "" : loadEx.Message), "切换失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch { }
                                }));
                                return;   // 结束本次切换（不执行新方案绑定/收尾）
                            }
                            // ── 加载成功：现在才拆旧、换新 ──
                            UpdateSplashProgress(35, "正在卸载旧方案...");
                            try
                            {
                                try { manager1.Shutdown(); }
                                catch (Exception exSh) { _logger.WriteLog("关闭旧方案失败!--" + exSh.Message); }
                                // ★ F14: Shutdown 后立即释放各相机持有的旧 block/job/newrecod 等 VisionPro 对象，
                                // 强制 GC 回收 RCW，避免频繁切方案导致 COM 句柄/内存累积
                                ReleaseAllMyjobVisionObjects();
                                Thread.Sleep(500);
                            }
                            catch (Exception exRc)
                            {
                                _logger.WriteLog("卸载旧方案收尾异常（已忽略，继续使用新方案）：" + exRc.Message);
                            }
                            manager1 = newMgr;
                            try { _jobs.JobManager = manager1; } catch { }
                            managerState = ManagerState.Loaded; // ★ 标记加载成功
                            try { _config.WriteString("path", "path_1", path_1); }
                            catch (Exception exCfg) { _logger.WriteLog("保存方案路径失败：" + exCfg.Message); }
                            UpdateSplashProgress(45, "方案加载完成，正在绑定作业...");
                        }
                        catch (Exception ex)
                        {
                            // ★C1：守卫——若方案实际已可用（新方案已换上，或旧方案仍在用），收尾异常不应把
                            //   系统判为"无方案"（置 Failed/null 会丢弃已加载的 manager，只能重启软件）。
                            if (manager1 != null && managerState == ManagerState.Loaded)
                            {
                                _logger.WriteLog("切方案收尾异常（方案已可用，忽略不影响生产）：" + ex.Message);
                            }
                            else
                            {
                                managerState = ManagerState.Failed;
                                manager1 = null;
                                _jobs.JobManager = null;
                                label75.Text = label75.Text + "方案已损坏";
                                label173.Text = path_1;
                                _logger.WriteLog(ex.Message + "加载方案失败");
                            }
                        }
                        if (manager1 != null && managerState == ManagerState.Loaded)
                        {
                        try
                        {
                            manager1.UserQueueFlush();
                            manager1.FailureQueueFlush();
                            _jobs.myjob1.job = manager1.Job(0);
                            myIndependentJob = _jobs.myjob1.job.OwnedIndependent;
                            _jobs.myjob1.job.ImageQueueFlush();
                            _jobs.myjob1.Cogbmp = new CogImageFileBMP();
                            myIndependentJob.RealTimeQueueFlush();
                            listBox2.Items.Add("产品类型:相机1");
                            listBox2.Items.Add("检测数:");
                            listBox2.Items.Add("OK数:");
                            listBox2.Items.Add("NG数:");
                            listBox2.Items.Add("合格率:");
                            listBox2.Items.Add("~~~~~~~~~");

                            group_1 = _jobs.myjob1.job.VisionTool as CogToolGroup;
                            block_1 = group_1.Tools["CogToolBlock1"] as CogToolBlock;
                            _jobs.myjob1.block = block_1.Tools["CogToolBlock1"] as CogToolBlock;
                            // path_1 = @".\test.vpp";
                            cogRecordDisplay1.Refresh();
                            if ((_jobs.myjob1.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                _jobs.myjob1.Color = false;
                            else
                                _jobs.myjob1.Color = true;
                            try
                            {
                                string aatemp = _jobs.myjob1.block.Outputs["tishi"].Value.ToString();
                                _jobs.myjob1.tishi = true;
                            }
                            catch
                            {
                                _jobs.myjob1.tishi = false;
                            }
                            _jobs.myjob1.output3 = "空";
                            _jobs.myjob1.zidongbaoguang = false;
                            _jobs.myjob1.biaotou = "";
                            for (int i = 0; i < _jobs.myjob1.block.Outputs.Count; i++)
                            {
                                if (_jobs.myjob1.block.Outputs[i].Name.Contains("ji"))
                                {
                                    _jobs.myjob1.biaotou += _jobs.myjob1.block.Outputs[i].Name + ",";
                                }
                                if (_jobs.myjob1.block.Outputs[i].Name.Contains("buchang"))
                                {
                                    _jobs.myjob1.zidongbaoguang = true;
                                }
                                if (_jobs.myjob1.block.Outputs[i].Name.Contains("Output3"))
                                {
                                    _jobs.myjob1.output3 = "有";
                                }
                            }
                            if (manager1.JobCount > 1)
                            {
                                _jobs.myjob2.job = manager1.Job(1);
                                myIndependentJob = _jobs.myjob2.job.OwnedIndependent;
                                _jobs.myjob2.job.ImageQueueFlush();
                                _jobs.myjob2.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机2");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");

                                group_2 = _jobs.myjob2.job.VisionTool as CogToolGroup;
                                block_2 = group_2.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob2.block = block_2.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob2.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob2.Color = false;
                                else
                                    _jobs.myjob2.Color = true;
                                //  _jobs.myjob2.CogFifo = block_2.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                                //td2 = new Thread(new ThreadStart(getrecord_2));
                                //td2.Start();
                                // dataGridView2.ReadOnly = true;
                                // dataGridView2.AllowUserToAddRows = false;
                                //  dataGridView2.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                                try
                                {
                                    string aatemp = _jobs.myjob2.block.Outputs["tishi"].Value.ToString();
                                    _jobs.myjob2.tishi = true;
                                }
                                catch
                                {
                                    _jobs.myjob2.tishi = false;
                                }
                                _jobs.myjob2.output3 = "空";
                                _jobs.myjob2.zidongbaoguang = false;
                                _jobs.myjob2.biaotou = "";
                                for (int i = 0; i < _jobs.myjob2.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob2.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob2.biaotou += _jobs.myjob2.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob2.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob2.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob2.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob2.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 2)
                            {
                                _jobs.myjob3.job = manager1.Job(2);
                                myIndependentJob = _jobs.myjob3.job.OwnedIndependent;
                                _jobs.myjob3.job.ImageQueueFlush();
                                _jobs.myjob3.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机3");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                group_3 = _jobs.myjob3.job.VisionTool as CogToolGroup;
                                bbtemp = 7462;
                                block_3 = group_3.Tools["CogToolBlock1"] as CogToolBlock;
                                bbtemp = 7464;
                                //  _jobs.myjob3.CogFifo = block_3.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                                _jobs.myjob3.block = block_3.Tools["CogToolBlock1"] as CogToolBlock;
                                bbtemp = 7467;
                                if ((_jobs.myjob3.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob3.Color = false;
                                else
                                    _jobs.myjob3.Color = true;
                                bbtemp = 7472;
                                //td3 = new Thread(new ThreadStart(getrecord_3));
                                //td3.Start();
                                bbtemp = 7475;
                                try
                                {
                                    string aatemp = _jobs.myjob3.block.Outputs["tishi"].Value.ToString();
                                    _jobs.myjob3.tishi = true;
                                }
                                catch
                                {
                                    _jobs.myjob3.tishi = false;
                                }
                                _jobs.myjob3.output3 = "空";
                                _jobs.myjob3.zidongbaoguang = false;
                                _jobs.myjob3.biaotou = "";
                                for (int i = 0; i < _jobs.myjob3.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob3.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob3.biaotou += _jobs.myjob3.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob3.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob3.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob3.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob3.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 3)
                            {
                                _jobs.myjob4.job = manager1.Job(3);
                                myIndependentJob = _jobs.myjob4.job.OwnedIndependent;
                                _jobs.myjob4.job.ImageQueueFlush();
                                _jobs.myjob4.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机4");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                group_4 = _jobs.myjob4.job.VisionTool as CogToolGroup;
                                block_4 = group_4.Tools["CogToolBlock1"] as CogToolBlock;

                                // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                                _jobs.myjob4.block = block_4.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob4.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob4.Color = false;
                                else
                                    _jobs.myjob4.Color = true;
                                // td4 = new Thread(new ThreadStart(getrecord_4));
                                // td4.Start();
                                try
                                {
                                    string aatemp = _jobs.myjob4.block.Outputs["tishi"].Value.ToString();
                                    _jobs.myjob4.tishi = true;
                                }
                                catch
                                {
                                    _jobs.myjob4.tishi = false;
                                }
                                _jobs.myjob4.output3 = "空";
                                _jobs.myjob4.zidongbaoguang = false;
                                _jobs.myjob4.biaotou = "";
                                for (int i = 0; i < _jobs.myjob4.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob4.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob4.biaotou += _jobs.myjob4.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob4.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob4.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob4.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob4.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 4)
                            {
                                _jobs.myjob5.job = manager1.Job(4);
                                myIndependentJob = _jobs.myjob5.job.OwnedIndependent;
                                _jobs.myjob5.job.ImageQueueFlush();
                                _jobs.myjob5.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机5");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                //listBox14.Items.Add("record");
                                //listBox14.Items.Add("record");
                                //listBox14.Items.Add("record");
                                //listBox14.Items.Add("record");
                                //listBox14.Items.Add("record");
                                group_5 = _jobs.myjob5.job.VisionTool as CogToolGroup;
                                block_5 = group_5.Tools["CogToolBlock1"] as CogToolBlock;

                                // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                                _jobs.myjob5.block = block_5.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob5.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob5.Color = false;
                                else
                                    _jobs.myjob5.Color = true;
                                // td5 = new Thread(new ThreadStart(getrecord_5));
                                // td5.Start();
                                try
                                {
                                    string aatemp = _jobs.myjob5.block.Outputs["tishi"].Value.ToString();
                                    _jobs.myjob5.tishi = true;
                                }
                                catch
                                {
                                    _jobs.myjob5.tishi = false;
                                }
                                _jobs.myjob5.output3 = "空";
                                _jobs.myjob5.zidongbaoguang = false;
                                _jobs.myjob5.biaotou = "";
                                for (int i = 0; i < _jobs.myjob5.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob5.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob5.biaotou += _jobs.myjob5.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob5.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob5.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob5.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob5.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 5)
                            {
                                _jobs.myjob6.job = manager1.Job(5);
                                myIndependentJob = _jobs.myjob6.job.OwnedIndependent;
                                _jobs.myjob6.job.ImageQueueFlush();
                                _jobs.myjob6.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机6");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                //listBox15.Items.Add("record");
                                //listBox15.Items.Add("record");
                                //listBox15.Items.Add("record");
                                //listBox15.Items.Add("record");
                                //listBox15.Items.Add("record");
                                group_6 = _jobs.myjob6.job.VisionTool as CogToolGroup;
                                block_6 = group_6.Tools["CogToolBlock1"] as CogToolBlock;

                                // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                                _jobs.myjob6.block = block_6.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob6.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob6.Color = false;
                                else
                                    _jobs.myjob6.Color = true;
                                // td6 = new Thread(new ThreadStart(getrecord_6));
                                // td6.Start();
                                try
                                {
                                    string aatemp = _jobs.myjob6.block.Outputs["tishi"].Value.ToString();
                                    _jobs.myjob6.tishi = true;
                                }
                                catch
                                {
                                    _jobs.myjob6.tishi = false;
                                }
                                _jobs.myjob6.output3 = "空";
                                _jobs.myjob6.zidongbaoguang = false;
                                _jobs.myjob6.biaotou = "";
                                for (int i = 0; i < _jobs.myjob6.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob6.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob6.biaotou += _jobs.myjob6.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob6.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob6.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob6.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob6.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 6)
                            {
                                _jobs.myjob7.job = manager1.Job(6);
                                myIndependentJob = _jobs.myjob7.job.OwnedIndependent;
                                _jobs.myjob7.job.ImageQueueFlush();
                                _jobs.myjob7.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机7");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                //listBox18.Items.Add("record");
                                //listBox18.Items.Add("record");
                                //listBox18.Items.Add("record");
                                //listBox18.Items.Add("record");
                                //listBox18.Items.Add("record");
                                group_7 = _jobs.myjob7.job.VisionTool as CogToolGroup;
                                block_7 = group_7.Tools["CogToolBlock1"] as CogToolBlock;

                                // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                                _jobs.myjob7.block = block_7.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob7.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob7.Color = false;
                                else
                                    _jobs.myjob7.Color = true;
                                // td7 = new Thread(new ThreadStart(getrecord_7));
                                // td7.Start();
                                try
                                {
                                    string aatemp = _jobs.myjob7.block.Outputs["tishi"].Value.ToString();
                                    _jobs.myjob7.tishi = true;
                                }
                                catch
                                {
                                    _jobs.myjob7.tishi = false;
                                }
                                _jobs.myjob7.output3 = "空";
                                _jobs.myjob7.zidongbaoguang = false;
                                _jobs.myjob7.biaotou = "";
                                for (int i = 0; i < _jobs.myjob7.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob7.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob7.biaotou += _jobs.myjob7.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob7.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob7.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob7.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob7.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 7)
                            {
                                _jobs.myjob8.job = manager1.Job(7);
                                myIndependentJob = _jobs.myjob8.job.OwnedIndependent;
                                _jobs.myjob8.job.ImageQueueFlush();
                                _jobs.myjob8.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机8");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                //listBox17.Items.Add("record");
                                //listBox17.Items.Add("record");
                                //listBox17.Items.Add("record");
                                //listBox17.Items.Add("record");
                                //listBox17.Items.Add("record");
                                group_8 = _jobs.myjob8.job.VisionTool as CogToolGroup;
                                block_8 = group_8.Tools["CogToolBlock1"] as CogToolBlock;

                                // _jobs.myjob4.CogFifo = block_4.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                                _jobs.myjob8.block = block_8.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob8.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob8.Color = false;
                                else
                                    _jobs.myjob8.Color = true;
                                // td8 = new Thread(new ThreadStart(getrecord_8));
                                // td8.Start();
                                _jobs.myjob8.output3 = "空";
                                _jobs.myjob8.zidongbaoguang = false;
                                _jobs.myjob8.biaotou = "";
                                for (int i = 0; i < _jobs.myjob8.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob8.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob8.biaotou += _jobs.myjob8.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob8.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob8.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob8.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob8.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 8)
                            {
                                _jobs.myjob9.job = manager1.Job(8);
                                myIndependentJob = _jobs.myjob9.job.OwnedIndependent;
                                _jobs.myjob9.job.ImageQueueFlush();
                                _jobs.myjob9.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机9");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                group_9 = _jobs.myjob9.job.VisionTool as CogToolGroup;
                                block_9 = group_9.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob9.block = block_9.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob9.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob9.Color = false;
                                else
                                    _jobs.myjob9.Color = true;
                                _jobs.myjob9.output3 = "空";
                                _jobs.myjob9.zidongbaoguang = false;
                                _jobs.myjob9.biaotou = "";
                                for (int i = 0; i < _jobs.myjob9.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob9.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob9.biaotou += _jobs.myjob9.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob9.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob9.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob9.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob9.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 9)
                            {
                                _jobs.myjob10.job = manager1.Job(9);
                                myIndependentJob = _jobs.myjob10.job.OwnedIndependent;
                                _jobs.myjob10.job.ImageQueueFlush();
                                _jobs.myjob10.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机10");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                group_10 = _jobs.myjob10.job.VisionTool as CogToolGroup;
                                block_10 = group_10.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob10.block = block_10.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob10.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob10.Color = false;
                                else
                                    _jobs.myjob10.Color = true;
                                _jobs.myjob10.output3 = "空";
                                _jobs.myjob10.zidongbaoguang = false;
                                _jobs.myjob10.biaotou = "";
                                for (int i = 0; i < _jobs.myjob10.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob10.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob10.biaotou += _jobs.myjob10.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob10.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob10.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob10.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob10.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 10)
                            {
                                _jobs.myjob11.job = manager1.Job(10);
                                myIndependentJob = _jobs.myjob11.job.OwnedIndependent;
                                _jobs.myjob11.job.ImageQueueFlush();
                                _jobs.myjob11.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机11");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                group_11 = _jobs.myjob11.job.VisionTool as CogToolGroup;
                                block_11 = group_11.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob11.block = block_11.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob11.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob11.Color = false;
                                else
                                    _jobs.myjob11.Color = true;
                                _jobs.myjob11.output3 = "空";
                                _jobs.myjob11.zidongbaoguang = false;
                                _jobs.myjob11.biaotou = "";
                                for (int i = 0; i < _jobs.myjob11.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob11.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob11.biaotou += _jobs.myjob11.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob11.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob11.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob11.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob11.output3 = "有";
                                    }
                                }
                            }
                            if (manager1.JobCount > 11)
                            {
                                _jobs.myjob12.job = manager1.Job(11);
                                myIndependentJob = _jobs.myjob12.job.OwnedIndependent;
                                _jobs.myjob12.job.ImageQueueFlush();
                                _jobs.myjob12.Cogbmp = new CogImageFileBMP();
                                myIndependentJob.RealTimeQueueFlush();
                                listBox2.Items.Add("产品类型:相机12");
                                listBox2.Items.Add("检测数:");
                                listBox2.Items.Add("OK数:");
                                listBox2.Items.Add("NG数:");
                                listBox2.Items.Add("合格率:");
                                listBox2.Items.Add("~~~~~~~~~");
                                group_12 = _jobs.myjob12.job.VisionTool as CogToolGroup;
                                block_12 = group_12.Tools["CogToolBlock1"] as CogToolBlock;
                                _jobs.myjob12.block = block_12.Tools["CogToolBlock1"] as CogToolBlock;
                                if ((_jobs.myjob12.block.Inputs["Input"]).ValueType.ToString().Contains("8"))
                                    _jobs.myjob12.Color = false;
                                else
                                    _jobs.myjob12.Color = true;
                                _jobs.myjob12.output3 = "空";
                                _jobs.myjob12.zidongbaoguang = false;
                                _jobs.myjob12.biaotou = "";
                                for (int i = 0; i < _jobs.myjob12.block.Outputs.Count; i++)
                                {
                                    if (_jobs.myjob12.block.Outputs[i].Name.Contains("ji"))
                                    {
                                        _jobs.myjob12.biaotou += _jobs.myjob12.block.Outputs[i].Name + ",";
                                    }
                                    if (_jobs.myjob12.block.Outputs[i].Name.Contains("buchang"))
                                    {
                                        _jobs.myjob12.zidongbaoguang = true;
                                    }
                                    if (_jobs.myjob12.block.Outputs[i].Name.Contains("Output3"))
                                    {
                                        _jobs.myjob12.output3 = "有";
                                    }
                                }
                            }
                            
                        }
                        catch
                        {
                            _logger.WriteLog("无流程4");
                        }
                        }
                        else
                        {
                            _logger.WriteLog("切换方案: 加载失败，跳过流程初始化");
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "方案加载失败，请检查方案文件";
                            }));
                        }
                        // ★诊断（2026-09-20）：记录方案流程数与各相机流程块绑定情况——
                        //   现场若出现"运行结果组数少于预期 / 点 Form6 为空 / Form9 枚举工具崩溃"，
                        //   查这条日志即可区分是 JobCount 不足还是某相机 block 绑定失败
                        //   （与现有"无流程N / 流程N初始化失败 / 分流程"日志配合定位）。
                        try
                        {
                            int _diagJc = (manager1 != null) ? manager1.JobCount : -1;
                            string _diagBind = "";
                            for (int _bi = 0; _bi < 12; _bi++)
                            {
                                bool _okBind = _bi < _diagJc && _jobs.Myjobs[_bi] != null
                                    && _jobs.Myjobs[_bi].job != null && _jobs.Myjobs[_bi].block != null;
                                _diagBind += "相机" + (_bi + 1) + "=" + (_okBind ? "OK" : "null") + (_bi == 11 ? "" : " ");
                            }
                            _logger.WriteLog("方案绑定诊断: JobCount=" + _diagJc + " | block: " + _diagBind);
                        }
                        catch { }
                        listBox2.Visible = true;
                        UpdateSplashProgress(80, "正在恢复相机与参数...");

                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog(ex.Message + "切换方案1:" + bbtemp);
                    };
                    try
                    {
                        if (manager1 == null || managerState != ManagerState.Loaded)
                        {
                            this.Invoke(new Action(() =>
                            {
                                label133.Text = "方案加载失败，请检查方案文件";
                                checkedListBox1.Enabled = false;
                                if (_comm.Omron.qiehuanzhong == 0)
                                    Frm2.start = 1;
                                _switchingScheme = false;
                                _inspectionLifecycle.Resume();
                                Interlocked.Exchange(ref qiehuanzhong, 0);
                                button1.Visible = true;
                            }));
                        }
                        else
                        {
                        // ★ 成功块整体迁回 UI 线程执行：原代码在 Task.Run 后台线程里直接改大量控件
                        //   （Enabled/Visible/ComboBox/按钮方法），仅靠全局关闭跨线程检查掩盖，属数据竞争。
                        //   Invoke 统一串行到 UI 线程，行为与“按钮手动执行”一致。
                        this.Invoke(new Action(() =>
                        {
                        checkedListBox1.Enabled = false;
                        if (_comm.Omron.qiehuanzhong == 0)
                        {
                            Frm2.start = 1;
                        }
                        //  Frm2.Close();
                        // ★ 重新打开相机（已先关闭）
                        bnOpen_Click(null, null);
                        UpdateSplashProgress(85, "正在打开相机...");
                        this.Invoke(new Action(() => { display(); }));

                        //this.FormBorderStyle = FormBorderStyle.FixedSingle;
                        UpdateSplashProgress(90, "正在恢复参数...");
                        trriger_set();
                        _jobs.myjob1.baoguang = 0;
                        _jobs.myjob2.baoguang = 0;
                        _jobs.myjob3.baoguang = 0;
                        _jobs.myjob4.baoguang = 0;
                        _jobs.myjob5.baoguang = 0;
                        _jobs.myjob6.baoguang = 0;
                        _jobs.myjob7.baoguang = 0;
                        _jobs.myjob8.baoguang = 0;
                        _jobs.myjob9.baoguang = 0;
                        _jobs.myjob10.baoguang = 0;
                        _jobs.myjob11.baoguang = 0;
                        _jobs.myjob12.baoguang = 0;
                        baoguang_set();
                        Thread.Sleep(100);
                        bnClose.Enabled = true;

                        bnSetParam_Click(null, null);
                        bnGetParam_Click(null, null);// ch:获取参数 | en:Get parameters
                        bnStartGrab1.Enabled = false;
                        bnStopGrab1.Enabled = true;
                        if (manager1.JobCount > 1)
                        {
                            bnStartGrab2.Enabled = false;
                            bnStopGrab2.Enabled = true;
                            bnSetParam2_Click(null, null);
                            bnGetParam2_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 2)
                        {
                            bnStartGrab3.Enabled = false;
                            bnStopGrab3.Enabled = true;
                            bnSetParam3_Click(null, null);
                            bnGetParam3_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 3)
                        {
                            bnStartGrab4.Enabled = false;
                            bnStopGrab4.Enabled = true;
                            bnSetParam4_Click(null, null);
                            bnGetParam4_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 4)
                        {
                            bnStartGrab5.Enabled = false;
                            bnStopGrab5.Enabled = true;
                            bnSetParam5_Click(null, null);
                            bnGetParam5_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 5)
                        {
                            bnStartGrab6.Enabled = false;
                            bnStopGrab6.Enabled = true;
                            bnSetParam6_Click(null, null);
                            bnGetParam6_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 6)
                        {
                            bnStartGrab7.Enabled = false;
                            bnStopGrab7.Enabled = true;
                            bnSetParam7_Click(null, null);
                            bnGetParam7_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 7)
                        {
                            bnStartGrab8.Enabled = false;
                            bnStopGrab8.Enabled = true;
                            bnSetParam8_Click(null, null);
                            bnGetParam8_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 8)
                        {
                            bnStartGrab9.Enabled = false;
                            bnStopGrab9.Enabled = true;
                            bnSetParam9_Click(null, null);
                            bnGetParam9_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 9)
                        {
                            bnStartGrab10.Enabled = false;
                            bnStopGrab10.Enabled = true;
                            bnSetParam10_Click(null, null);
                            bnGetParam10_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 10)
                        {
                            bnStartGrab11.Enabled = false;
                            bnStopGrab11.Enabled = true;
                            bnSetParam11_Click(null, null);
                            bnGetParam11_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        if (manager1.JobCount > 11)
                        {
                            bnStartGrab12.Enabled = false;
                            bnStopGrab12.Enabled = true;
                            bnSetParam12_Click(null, null);
                            bnGetParam12_Click(null, null);// ch:获取参数 | en:Get parameters
                        }
                        comboBox38_TextChanged(null, null);

                        button1_Click(null, null);
                        // ★C3 修复：放门控与"切换完成"回执下移到检测真正 Arm（_jobs.CommTriggerArmed=true）之后——
                        //   原实现 button1_Click 只是把 run 排成 Task 就立即发 ACK：PLC 收到"完成"后立刻发的
                        //   第一件产品触发会被 CommTriggerArmed 门控静默丢弃（SendSoftwareTrigger 直接 return -1）。
                        //   此处异步等待（不阻塞 UI 线程），Arm 完成或超时后再放门控+回执。
                        Task.Run(() =>
                        {
                            for (int _w = 0; _w < 100 && !_jobs.CommTriggerArmed; _w++) Thread.Sleep(100);   // 最多等 10 秒
                            bool _armed = _jobs.CommTriggerArmed;
                            try
                            {
                                this.Invoke(new Action(() =>
                                {
                                    _switchingScheme = false;
                                    _inspectionLifecycle.Resume();
                                    Interlocked.Exchange(ref qiehuanzhong, 0);
                                    button1.Visible = true;
                                    display();
                                    // ★C2 修复：ACK 前逐路校验 job/block 绑定完整性——12 路重建共用一个 try，
                                    //   某路异常（如缺 CogToolBlock1）时该路及后续路 block 为 null，但原实现
                                    //   照发"切换完成"回执 → PLC 开始触发而后段相机静默不检测（质量逃逸）。
                                    //   任一应加载路未绑定成功即抑制回执（PLC 靠超时判 NG）。
                                    bool flowBindOk = true;
                                    int _jc = (manager1 != null) ? manager1.JobCount : 0;
                                    for (int _bi = 0; _bi < _jc && _bi < 12; _bi++)
                                    {
                                        if (_jobs.Myjobs[_bi] == null || _jobs.Myjobs[_bi].job == null || _jobs.Myjobs[_bi].block == null)
                                        {
                                            flowBindOk = false;
                                            _logger.WriteLog("切换方案: 相机" + (_bi + 1) + " 流程/block 绑定不完整，本次切换判失败");
                                        }
                                    }
                                    if (!_armed)
                                        _logger.WriteLog("切换方案: 等待检测 Arm 超时(10s)，回执已抑制（PLC 靠超时判 NG）");
                                    else if (flowBindOk)
                                        SendSchemeSwitchAck(ackProto, ackLinkId);   // ★ 切换成功回执：把相机13 配置的“返回值”写回 PLC 切换通道（若已配置）
                                    else
                                        _logger.WriteLog("切换方案: 回执已抑制（流程绑定不完整），PLC 靠超时判 NG");
                                }));
                            }
                            catch { }
                        });
                        }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(2000);
                        this.Invoke(new Action(() =>
                        {
                            if (_comm.Omron.qiehuanzhong == 0)
                            {
                                Frm2.start = 1;
                            }
                            _switchingScheme = false;
                                _inspectionLifecycle.Resume();
                            Interlocked.Exchange(ref qiehuanzhong, 0);
                            // ★M4：切换失败不发回执（仅成功收尾调用 SendSchemeSwitchAck，来源参数默认 0）
                            button1.Visible = true;
                            display();
                        }));
                        _logger.WriteLog(ex.Message + "切换方案2");
                    };
                });
            }
            else
            {
                // ★ 已在途：后到的切方案请求被原子门控拒绝，记录日志便于排查"PLC 发了切型但没反应"
                _logger.WriteLog("切方案请求被忽略：已有切换在途：" + a);
            }
        }
        private void 配置工具ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            frm6.Add(new Form6(_jobs.myjob1.block));
            frm6[frm6.Count - 1].Show();
        }

        private void 配置相机2ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            frm6.Add(new Form6(_jobs.myjob2.block));
            frm6[frm6.Count - 1].Show();

        }


        private void 设置ToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            //pma = _jobs.myjob2.block.Tools["CogPMAlignTool1"] as CogPMAlignTool;
            //rect= new CogRectangle();
            StreamReader sr = null;
            try
            {

                int i = this.设置ToolStripMenuItem.DropDownItems.Count;
                if (i > item_sum)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (j >= item_sum)
                        {
                            this.设置ToolStripMenuItem.DropDownItems.RemoveAt(item_sum);
                        }
                    }
                }
                // Thread.Sleep(10);
                sr = new StreamReader(Path.GetDirectoryName(path_1) + "\\Menu.ini");
                i = item_sum;
                while (sr.Peek() >= 0)
                {
                    //string item_temp = sr.ReadLine();
                    //int end_temp=0;
                    //for (int j = 0; j < this.设置ToolStripMenuItem.DropDownItems.Count; j++)
                    //{
                    //    end_temp = 0;
                    //    if (item_temp == this.设置ToolStripMenuItem.DropDownItems[j].Text)
                    //    {
                    //        end_temp = 1;
                    //    }

                    //    if (end_temp == 0)
                    //    {
                    menuitem = new ToolStripMenuItem(sr.ReadLine());
                    this.设置ToolStripMenuItem.DropDownItems.Insert(i, menuitem);
                    i++;
                    menuitem.Click += new EventHandler(menuitem_Click);
                    //    }
                    //}

                }
                sr.Dispose();
                sr.Close();
            }
            catch
            {
                try
                {
                    sr.Dispose();
                    sr.Close();
                }
                catch { }
            }
        }


        private void button13_Click(object sender, EventArgs e)
        {
            trriger1_temp = 0;
            timer7.Enabled = false;
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            try
            {
                timer7.Interval = int.Parse(textBox6.Text);
            }
            catch { }
        }

        private void timer7_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob1.trriger == 0)
            {
                if (trriger1_temp == 1)
                {
                    int count = listBox4.Items.Count;
                    int select = listBox4.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox4.SelectedIndex = select + 1;
                        }
                        else
                            listBox4.SelectedIndex = 0;
                    }));
                }

            }
        }


        private void timer8_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob2.trriger == 0)
            {
                if (trriger2_temp == 1)
                {
                    int count = listBox5.Items.Count;
                    int select = listBox5.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox5.SelectedIndex = select + 1;
                        }
                        else
                            listBox5.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void button7_Click_3(object sender, EventArgs e)
        {
            trriger2_temp = 0;
            timer8.Enabled = false;
        }


        private void timer9_Tick(object sender, EventArgs e)
        {
            // 相机9 定时翻图（原为空方法，与 button90/91 翻图按钮配套；此前按钮误用 timer17 导致功能失效）
            if (_jobs.myjob9.trriger == 0)
            {
                if (trriger9_temp == 1)
                {
                    int count = listBox19.Items.Count;
                    int select = listBox19.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox19.SelectedIndex = select + 1;
                        }
                        else
                            listBox19.SelectedIndex = 0;
                    }));
                }

            }
        }
        private void timer10_Tick(object sender, EventArgs e)
        {
            try
            {
                if (frm3.lujing.Length >= 3)
                {
                    // ★M4：Form3（无协议串口/TCP）切换不产生通讯回执（来源参数默认 0）
                    xinghao_qiehuan(frm3.lujing);
                    frm3.lujing = "";
                    frm3.zifu = "";
                }
            }
            catch { }
        }

        private void timer11_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob3.trriger == 0)
            {
                if (trriger3_temp == 1)
                {
                    int count = listBox8.Items.Count;
                    int select = listBox8.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox8.SelectedIndex = select + 1;
                        }
                        else
                            listBox8.SelectedIndex = 0;
                    }));
                }

            }
        }
        #endregion
        #region 相机控制（枚举/打开/取流/回调）
        /// <summary>
        /// 枚举相机设备列表
        /// </summary>
        private void bnEnum_Click(object sender, EventArgs e)
        {
            DeviceListAcq();
        }
        int diyici = 0;
        bool dakaizhong = false;
        // ★ 2026-09-06 授权过期门控（④）：替代局部 dayz，供 initialize_FormSet 自动开相机前判断。
        // ★ 2026-09-07：后台监控线程（daoqi_jiankong）也会写它，故声明 volatile。
        private volatile bool _authExpired = false;
        private void bnOpen_Click(object sender, EventArgs e)
        {
            // ★M10 修复：原实现 dakaizhong=true 后仅在"参数错误"与"正常结尾"两处复位，
            //   中途任何异常（枚举/开相机等）都会让标志永久为 true → 重连线程永久停摆。
            //   改为包装方法：无论本体如何退出（提前 return / 异常）都由 finally 复位。
            try { bnOpenCore(); }
            finally { dakaizhong = false; }
        }

        private void bnOpenCore()
        {
            if (manager1 == null || manager1.JobCount <= 0)
                return;

            dakaizhong = true;

            int nCameraUsingNum;
            if (!int.TryParse(tbUseNum.Text, out nCameraUsingNum) || nCameraUsingNum <= 0)
            {
                if (diyici == 0)
                {
                    diyici = 1;
                    ShowErrorMsg("Please enter correct format!", 0);
                }
                dakaizhong = false;
                return;
            }
            if (nCameraUsingNum > 12) nCameraUsingNum = 12;

            // 枚举设备并按 UserDefinedName 映射到槽位（取代 12 段复制 if）
            List<CameraDeviceInfo> devices = HikCameraEnumerator.Enumerate();
            Dictionary<int, CameraDeviceInfo> slotMap = HikCameraEnumerator.MapToSlots(devices, camera_name);
            if (devices.Count == 0 || slotMap.Count == 0)
            {
                if (diyici == 0)
                {
                    diyici = 1;
                    ShowErrorMsg("No device, please select", 0);
                }
                dakaizhong = false;
                return;
            }

            m_nDevNum = devices.Count;
            tbDevNum.Text = m_nDevNum.ToString("d");
            tbUseNum.Text = slotMap.Count.ToString("d");

            int[] temp = new int[12];
            int deviceArrayIndex = 0;
            int nRet = -1;
            int packetSizeFailed = 0;   // ★A3：包大小设置失败计数（锁内不弹窗，收尾汇总一次）

            lock (_cameraLock)
            {
                for (int slot = 0; slot < 12; slot++)
                {
                    CameraDeviceInfo devInfo;
                    if (!slotMap.TryGetValue(slot, out devInfo))
                    {
                        // ★A1 修复：名不匹配的槽若仍有上次残留的活设备，必须先完整销毁再丢弃——
                        //   原实现直接置 null 丢句柄：GigE 独占控制下该设备被卡死，
                        //   后续打开恒返回 0x80000007（只能重启软件或给相机断电）。
                        if (_cameraCtrl.Cameras[slot] != null)
                        {
                            _logger.WriteLog("相机" + (slot + 1) + "本次未匹配到设备，销毁残留句柄后跳过");
                            ClearCameraSlotOnOpenFailed(slot.ToString(), ref temp[0], ref temp[1], ref temp[2], ref temp[3], ref temp[4], ref temp[5], ref temp[6], ref temp[7], ref temp[8], ref temp[9], ref temp[10], ref temp[11]);
                        }
                        continue;
                    }

                    _jobs.Myjobs[slot].index = deviceArrayIndex;
                    device1[deviceArrayIndex] = devInfo.NativeInfo;
                    m_pDeviceInfo[deviceArrayIndex] = devInfo.NativeInfo;

                    try
                    {
                        // ★A1 修复：槽位若已有残留活对象（上次打开未清理），先完整销毁再创建——
                        //   原实现在仍打开的设备上直接 CreateDevice，会失败/冲突。
                        if (_cameraCtrl.Cameras[slot] != null)
                            ClearCameraSlotOnOpenFailed(slot.ToString(), ref temp[0], ref temp[1], ref temp[2], ref temp[3], ref temp[4], ref temp[5], ref temp[6], ref temp[7], ref temp[8], ref temp[9], ref temp[10], ref temp[11]);
                        if (_cameraCtrl.Cameras[slot] == null)
                            _cameraCtrl.Cameras[slot] = new MyCamera();

                        nRet = _cameraCtrl.Cameras[slot].MV_CC_CreateDevice_NET(ref device1[deviceArrayIndex]);
                        if (MyCamera.MV_OK != nRet)
                        {
                            _logger.WriteLog("相机" + (slot + 1) + "CreateDevice失败:" + Convert.ToString(nRet, 16));
                            ClearCameraSlotOnOpenFailed(slot.ToString(), ref temp[0], ref temp[1], ref temp[2], ref temp[3], ref temp[4], ref temp[5], ref temp[6], ref temp[7], ref temp[8], ref temp[9], ref temp[10], ref temp[11]);
                            continue;
                        }

                        nRet = _cameraCtrl.Cameras[slot].MV_CC_OpenDevice_NET();
                        if (MyCamera.MV_OK != nRet)
                        {
                            _logger.WriteLog("相机" + (slot + 1) + "打开失败:" + Convert.ToString(nRet, 16));
                            ClearCameraSlotOnOpenFailed(slot.ToString(), ref temp[0], ref temp[1], ref temp[2], ref temp[3], ref temp[4], ref temp[5], ref temp[6], ref temp[7], ref temp[8], ref temp[9], ref temp[10], ref temp[11]);
                            continue;
                        }

                        m_nCanOpenDeviceNum++;
                        temp[slot] = slot + 1;

                        if (device1[deviceArrayIndex].nTLayerType == MyCamera.MV_GIGE_DEVICE)
                        {
                            int nPacketSize = _cameraCtrl.Cameras[slot].MV_CC_GetOptimalPacketSize_NET();
                            if (nPacketSize > 0)
                            {
                                nRet = _cameraCtrl.Cameras[slot].MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                                if (nRet != MyCamera.MV_OK)
                                    packetSizeFailed++;   // ★A3：锁内禁弹 modal（会泵消息让排队中的重连 Invoke 插进半开状态执行），改计数+收尾汇总
                            }
                            else
                            {
                                packetSizeFailed++;
                            }
                        }

                        _cameraCtrl.Cameras[slot].MV_CC_RegisterExceptionCallBack_NET(cbException, (IntPtr)slot);   // 2026-09-06：注册 SDK 异常回调（★P0：传字段持有委托，防 GC）
                        nRet = _cameraCtrl.Cameras[slot].MV_CC_RegisterImageCallBackEx_NET(cbImage, (IntPtr)slot);
                        if (nRet != MyCamera.MV_OK)
                        {
                            _logger.WriteLog("相机" + (slot + 1) + "注册回调失败:" + Convert.ToString(nRet, 16));
                            ClearCameraSlotOnOpenFailed(slot.ToString(), ref temp[0], ref temp[1], ref temp[2], ref temp[3], ref temp[4], ref temp[5], ref temp[6], ref temp[7], ref temp[8], ref temp[9], ref temp[10], ref temp[11]);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog("相机" + (slot + 1) + "打开异常:" + ex.Message);
                        ClearCameraSlotOnOpenFailed(slot.ToString(), ref temp[0], ref temp[1], ref temp[2], ref temp[3], ref temp[4], ref temp[5], ref temp[6], ref temp[7], ref temp[8], ref temp[9], ref temp[10], ref temp[11]);
                        continue;   // ★A5：原异常分支缺 continue——会误执行下方 deviceArrayIndex++ 造成后续槽编号错位（与其它失败路径语义不一致）
                    }

                    _logger.WriteLog("相机" + deviceArrayIndex + "名称:" + devInfo.UserDefinedName + " 槽位=" + slot + " 打开中=" + dakaizhong.ToString());
                    deviceArrayIndex++;
                }
            }

            // ★A1 修复：未成功槽的清理统一走 ClearCameraSlotOnOpenFailed（Stop→Close→Destroy→清 pending），
            //   原实现在锁外直接置 null：若槽内仍有残留活对象会丢句柄（GigE 独占下后续打开恒失败），
            //   且锁外访问与重连线程存在竞态。
            lock (_cameraLock)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (temp[i] == 0 && _cameraCtrl.Cameras[i] != null)
                        ClearCameraSlotOnOpenFailed(i.ToString(), ref temp[0], ref temp[1], ref temp[2], ref temp[3], ref temp[4], ref temp[5], ref temp[6], ref temp[7], ref temp[8], ref temp[9], ref temp[10], ref temp[11]);
                }
            }

            // ★A4 修复：按实际打开成功数决定按钮态——原实现无条件禁用"打开设备"（Designer 默认也是 false）：
            //   相机上电慢导致 0 台成功时，用户连"打开设备"都点不了，只能重启软件。
            //   与既有 EnableAllConnectedCameraControls 同口径（在线数 > 0 才切换按钮态）。
            if (CountOpenedCameras() > 0)
            {
                bnOpen.Enabled = false;
                bnClose.Enabled = true;
            }
            else
            {
                bnOpen.Enabled = true;     // 保持可点，允许直接重试
                bnClose.Enabled = false;
                _logger.WriteLog("本次未打开任何相机（可能上电慢/被占用），保持“打开设备”可点以便重试");
                try { label133.Text = "未打开任何相机，请检查相机上电后重试"; } catch { }
            }
            for (int i = 0; i < 12; i++)
            {
                // ★ 2026-09-11：修复相机1启停按钮名。Designer 中 12 个按钮均为 bnStartGrab1..12，
                //   原 i==0 用无后缀 "bnStartGrab"（不存在）导致相机1按钮永不启用。
                string name = "bnStartGrab" + (i + 1);
                Control[] arr = Controls.Find(name, true);
                if (arr.Length > 0)
                    arr[0].Enabled = (_cameraCtrl.Cameras[i] != null);
            }

            ApplyTriggerModesForOpenedCameras();
            dakaizhong = false;
            // ★A4 修复：0 台在线不启用自动重连（否则 timer2 每 400ms 全网 GigE 广播枚举）
            if (CountOpenedCameras() > 0)
                EnableCameraReconnect();
            // ★A3：包大小设置失败收尾汇总（锁内不再弹 modal，避免让排队中的重连 Invoke 插进半开状态）
            if (packetSizeFailed > 0)
                _logger.WriteLog("有 " + packetSizeFailed + " 台相机包大小设置失败（不影响继续运行，请检查网卡巨帧/网络设置）");
        }


        private TextBox GetParamTextBox(string name)
        {
            Control[] arr = Controls.Find(name, true);
            return (arr.Length > 0) ? (arr[0] as TextBox) : null;
        }

        private string ParamBoxName(string prefix, int index)
        {
            return prefix + ((index == 0) ? "1" : (index + 1).ToString());
        }

        // 读取指定相机的曝光/增益/帧率并显示到文本框（取代 12 段 bnGetParamN）
        private void GetParamFor(int index)
        {
            if (_cameraCtrl.Cameras[index] == null) return;

            TextBox tbExp = GetParamTextBox(ParamBoxName("tbExposure", index));
            TextBox tbGain = GetParamTextBox(ParamBoxName("tbGain", index));
            TextBox tbRate = GetParamTextBox(ParamBoxName("tbFrameRate", index));
            string startName = ParamBoxName("bnStartGrab", index);

            try
            {
                MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();

                int nRet = _cameraCtrl.Cameras[index].MV_CC_GetFloatValue_NET("ExposureTime", ref stParam);
                if (MyCamera.MV_OK == nRet && tbExp != null)
                    tbExp.Text = stParam.fCurValue.ToString("F1");

                nRet = _cameraCtrl.Cameras[index].MV_CC_GetFloatValue_NET("Gain", ref stParam);
                if (MyCamera.MV_OK == nRet && tbGain != null)
                    tbGain.Text = stParam.fCurValue.ToString("F1");

                nRet = _cameraCtrl.Cameras[index].MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam);
                if (MyCamera.MV_OK == nRet && tbRate != null)
                    tbRate.Text = stParam.fCurValue.ToString("F1");
            }
            catch
            {
                Control[] startArr = Controls.Find(startName, true);
                if (startArr.Length > 0) startArr[0].Enabled = false;
            }
        }

        // 设置指定相机的曝光/增益/帧率，并回写 Vision 流程输入（取代 12 段 bnSetParamN）
        private void SetParamFor(int index)
        {
            // ★修复（2026-09-20）：未连接的相机（Cameras[index]==null，含 code.ini 中"屏蔽中"未接入的相机）
            //   直接跳过——原实现继续走 _cameraCtrl.Cameras[index].MV_CC_SetXxx → NullReferenceException，
            //   日志刷"设置参数异常/参数解析失败"误导现场（现场日志：相机1-4 设置参数异常、
            //   相机5 参数解析失败，实为未连接/屏蔽中，非软件故障）。已连接相机参数框为空时仍会提示解析失败（保留）。
            if (_cameraCtrl.Cameras[index] == null) return;
            TextBox tbExp = GetParamTextBox(ParamBoxName("tbExposure", index));
            TextBox tbGain = GetParamTextBox(ParamBoxName("tbGain", index));
            TextBox tbRate = GetParamTextBox(ParamBoxName("tbFrameRate", index));

            _logger.WriteLog("bnSetParam: 相机" + (index + 1) + " 进入设置 tbExp=[" + (tbExp == null ? "" : tbExp.Text) + "] tbGain=[" + (tbGain == null ? "" : tbGain.Text) + "] tbRate=[" + (tbRate == null ? "" : tbRate.Text) + "] _cameraCtrl.Cameras=" + (_cameraCtrl.Cameras[index] == null ? "null" : "OK"));

            try
            {
                float.Parse(tbExp.Text);
                float.Parse(tbGain.Text);
                float.Parse(tbRate.Text);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("bnSetParam: 相机" + (index + 1) + " 参数解析失败: " + ex.Message + " exp=[" + (tbExp == null ? "" : tbExp.Text) + "] gain=[" + (tbGain == null ? "" : tbGain.Text) + "] rate=[" + (tbRate == null ? "" : tbRate.Text) + "]");
                return;
            }

            try
            {
                _logger.WriteLog("bnSetParam: 相机" + (index + 1) + " 设置曝光=" + tbExp.Text + " 增益=" + tbGain.Text + " 帧率=" + tbRate.Text);

                _cameraCtrl.Cameras[index].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                float baoguang_temp = float.Parse(tbExp.Text);
                Myjob job = _jobs.Myjobs[index];
                if (job != null && job.baoguang != 0 && job.block != null && job.block.Inputs.Contains("baoguang"))
                {
                    job.block.Inputs["baoguang"].Value = baoguang_temp;
                }
                int nRet = _cameraCtrl.Cameras[index].MV_CC_SetFloatValue_NET("ExposureTime", baoguang_temp);
                if (nRet != MyCamera.MV_OK)
                    _logger.WriteLog("Set Exposure Time Fail!" + nRet);

                _cameraCtrl.Cameras[index].MV_CC_SetEnumValue_NET("GainAuto", 0);
                nRet = _cameraCtrl.Cameras[index].MV_CC_SetFloatValue_NET("Gain", float.Parse(tbGain.Text));
                if (nRet != MyCamera.MV_OK)
                    _logger.WriteLog("Set Gain Fail!" + nRet);

                nRet = _cameraCtrl.Cameras[index].MV_CC_SetFloatValue_NET("AcquisitionFrameRate", float.Parse(tbRate.Text));
                if (nRet != MyCamera.MV_OK)
                    _logger.WriteLog("Set Frame Rate Fail!" + nRet);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("bnSetParam: 相机" + (index + 1) + " 设置参数异常: " + ex.Message);
            }
        }

        private void bnGetParam_Click(object sender, EventArgs e)
        {
            GetParamFor(0);

        }

        private void bnSetParam_Click(object sender, EventArgs e)
        {
            SetParamFor(0);

        }

        private void StartGrabFor(int index)
        {
            try
            {
                // ★M12 修复：幂等——对已采集的相机误点"开始采集"时，原实现仍会再 StartGrabbing：
                //   设备已采集会返回失败，随后 SetCameraGrabbing(false) 把"正在采集"标志误清
                //   （实际仍在采集）→ 触发/停采/重连判定错乱。已在采集则只同步按钮态后返回。
                if (IsCameraGrabbing(index))
                {
                    SetStartGrabButtonState(index, false);
                    return;
                }
                SetCameraGrabbing(index, true);
                m_stFrameInfo[index].nFrameLen = 0;
                m_stFrameInfo[index].enPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Undefined;
                int nRet = _cameraCtrl.Cameras[index].MV_CC_StartGrabbing_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    SetCameraGrabbing(index, false);
                    ShowErrorMsg("Start Grabbing Fail!", nRet);
                    return;
                }
                SetStartGrabButtonState(index, false);
            }
            catch
            {
                SetCameraGrabbing(index, false);
                _logger.WriteLog("相机" + (index + 1) + "开始采集异常");
            }
        }

        private void StopGrabFor(int index)
        {
            try
            {
                SetCameraGrabbing(index, false);
                int nRet = _cameraCtrl.Cameras[index].MV_CC_StopGrabbing_NET();
                if (nRet != MyCamera.MV_OK)
                {
                    ShowErrorMsg("Stop Grabbing Fail!", nRet);
                }
                SetStartGrabButtonState(index, true);
            }
            catch
            {
                _logger.WriteLog("相机" + (index + 1) + "停止采集");
            }
        }

        private void TriggerExecFor(int index)
        {
            if (!IsCameraGrabbing(index))
            {
                _logger.WriteLog("相机" + (index + 1) + "未采集，无法触发");
                return;
            }
            int nRet = TriggerSoftwareCamera(index);
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Trigger Software Fail!", nRet);
            }
        }

        private void SetStartGrabButtonState(int index, bool startEnabled)
        {
            // Designer 中 12 个启停按钮均命名为 bnStartGrab1..12 / bnStopGrab1..12（无无后缀版本），
            // 相机1(index==0) 同样要用带后缀名，否则 Controls.Find 找不到、按钮状态永不更新
            string startName = "bnStartGrab" + (index + 1);
            string stopName = "bnStopGrab" + (index + 1);
            Control[] arr = Controls.Find(startName, true);
            if (arr.Length > 0) arr[0].Enabled = startEnabled;
            arr = Controls.Find(stopName, true);
            if (arr.Length > 0) arr[0].Enabled = !startEnabled;
        }

        private void bnStartGrab_Click(object sender, EventArgs e)
        {
            StartGrabFor(0);

        }
        #region 图像采集
        UInt32 nPayloadSize1;
        UInt32 nPayloadSize2;
        UInt32 nPayloadSize3;
        UInt32 nPayloadSize4;
        UInt32 nPayloadSize5;
        UInt32 nPayloadSize6;
        UInt32 nPayloadSize7;
        UInt32 nPayloadSize8;
        UInt32 nPayloadSize9;
        UInt32 nPayloadSize10;
        UInt32 nPayloadSize11;
        UInt32 nPayloadSize12;
        Bitmap[] bmp = new Bitmap[12];

        /// <summary>
        /// 2026-09-06：SDK 异常回调（掉线/带宽不足等），每个相机独立 pUser=slot。
        /// </summary>
        private void ExceptionCallBack(UInt32 nMsgType, IntPtr pUser)
        {
            int camIndex = (int)pUser;
            if (camIndex < 0 || camIndex >= 12) return;
            _logger.WriteLog(String.Format("相机{0} SDK 异常：0x{1:X8}", camIndex + 1, nMsgType));
            // ★B7 修复：设备断连异常主动置"待重连"标志——原实现只记日志。
            //   盲区：某些断连场景 IsDeviceConnected_NET() 仍返回 true（SDK 未及时更新），
            //   设备级重连（CheckAndReconnectCameraCore 以其为判据）永不触发，该路静默停摆。
            //   本回调在 SDK 线程：只置 volatile 标志（不碰句柄/控件），由 timer2 重连线程消费。
            try
            {
                if ((int)nMsgType == MyCamera.MV_EXCEPTION_DEV_DISCONNECT)
                {
                    System.Threading.Volatile.Write(ref _cameraSdkFault[camIndex], 1);
                    _logger.WriteLog("相机" + (camIndex + 1) + " SDK 报告设备断连，已标记待重连");
                }
            }
            catch { }
        }

        // ch:取流回调函数 | en:Aquisition Callback Function
        /// <summary>
        /// 相机帧回调（SDK 线程）：只做格式转换与像素拷贝入队，检测在独立线程执行
        /// </summary>
        // 静态灰度调色板：所有 Mono8 帧复用同一份，避免每帧 256 次 Color.FromArgb 构造（性能优化）
        private static readonly ColorPalette _grayPalette = CreateGrayPalette();
        private static ColorPalette CreateGrayPalette()
        {
            using (var tmp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                ColorPalette cp = tmp.Palette;
                for (int i = 0; i < 256; i++)
                    cp.Entries[i] = Color.FromArgb(i, i, i);
                return cp; // ColorPalette 为值拷贝，与 tmp 解耦，Dispose 后依然有效
            }
        }

        private void ImageCallBack(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            if (!_inspectionLifecycle.TryEnter()) return;
            try { ProcessImageCallback(pData, ref pFrameInfo, pUser); }
            finally { _inspectionLifecycle.Exit(); }
        }

        private void ProcessImageCallback(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            if (_disposingFlag || _switchingScheme || _inspectStop) return;

            int nIndex = (int)pUser;
            if (nIndex < 0 || nIndex >= _jobs.Myjobs.Length || _jobs.Myjobs[nIndex] == null) return;

            if (!ShouldProcessImageCallback(nIndex))
                return;

            // Consume the trigger even when conversion fails, so its metadata cannot shift to the next frame.
            CameraTriggerRecord trigger = _jobs.TakeTrigger(nIndex);
            if (IsCommTriggerMode(_jobs.Myjobs[nIndex].triggerMode) && trigger == null) return;
            Interlocked.Increment(ref m_nFrames[nIndex]);

            bool frameQueued = false;
            Bitmap owned = null;
            try
            {
                // 2026-09-06：更新帧信息，供相机设置页手动保存按钮使用
                m_stFrameInfo[nIndex] = pFrameInfo;

                CameraPixelFormat srcFmt = CameraPixelFormatHelper.FromHik(pFrameInfo.enPixelType);
                if (srcFmt == CameraPixelFormat.Unknown)
                {
                    if (pFrameInfo.nFrameLen == 0)
                        return;
                    if (_cameraCtrl.Cameras[nIndex] == null) return;
                    if (CameraPixelFormatHelper.IsHikMonoData(pFrameInfo.enPixelType))
                    {
                        uint need = (uint)pFrameInfo.nWidth * pFrameInfo.nHeight;
                        EnsureConvertBuffer(nIndex, need);
                        int convRet = CameraPixelFormatHelper.ConvertToMono8(_cameraCtrl.Cameras[nIndex], pData, m_pSaveImageBuf[nIndex], pFrameInfo.nHeight, pFrameInfo.nWidth, pFrameInfo.enPixelType);
                        if (convRet != MyCamera.MV_OK)
                        {
                            _logger.WriteLog("相机" + (nIndex + 1) + " 转 Mono8 失败: " + convRet);
                            return;
                        }
                        pData = m_pSaveImageBuf[nIndex];
                        srcFmt = CameraPixelFormat.Mono8;
                    }
                    else if (CameraPixelFormatHelper.IsHikColorData(pFrameInfo.enPixelType))
                    {
                        uint need = (uint)pFrameInfo.nWidth * pFrameInfo.nHeight * 3;
                        EnsureConvertBuffer(nIndex, need);
                        int convRet = CameraPixelFormatHelper.ConvertToRGB(_cameraCtrl.Cameras[nIndex], pData, pFrameInfo.nHeight, pFrameInfo.nWidth, pFrameInfo.enPixelType, m_pSaveImageBuf[nIndex]);
                        if (convRet != MyCamera.MV_OK)
                        {
                            _logger.WriteLog("相机" + (nIndex + 1) + " 转 RGB 失败: " + convRet);
                            return;
                        }
                        pData = m_pSaveImageBuf[nIndex];
                        srcFmt = CameraPixelFormat.Rgb8;   // ConvertToRGB 目标为 RGB8_Packed（内存 R,G,B），需转成 B,G,R
                    }
                    else
                    {
                        _logger.WriteLog("相机" + (nIndex + 1) + " 不支持的像素格式");
                        return;
                    }
                }

                // 直接从原始缓冲构造独立位图：内部逐行复制，一次性解决
                // ① stride 未做 4 字节对齐（原实现宽度非 4 倍数时会取图失败）
                // ② 彩色把 RGB 序数据按 Format24bppRgb（内存实为 B,G,R）解释导致红蓝颠倒
                // ③ 不再用 pData 零拷贝包装中间 Bitmap，省掉每帧一次 Bitmap 分配
                owned = CameraPixelFormatHelper.BuildOwnedBitmap(
                    pData, pFrameInfo.nWidth, pFrameInfo.nHeight, srcFmt, _grayPalette);
                if (owned == null)
                {
                    _logger.WriteLog("相机" + (nIndex + 1) + " 构造图像失败");
                    return;
                }

                _jobs.Myjobs[nIndex].cameraAcqTicks = pFrameInfo.fExposureTime;
                if (_jobs.Myjobs[nIndex].timewatch == null)
                    _jobs.Myjobs[nIndex].timewatch = new Stopwatch();
                _jobs.Myjobs[nIndex].timewatch.Restart();
                _jobs.Myjobs[nIndex].jieshouZifu = "null";
                EnqueueInspectFrame(nIndex, owned, trigger);
                frameQueued = true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog("相机" + (nIndex + 1) + " 取图回调异常: " + ex.Message);
            }
            finally
            {
                if (!frameQueued)
                {
                    owned?.Dispose();
                    EnqueueInspectFrame(nIndex, null, trigger);
                }
            }
        }
        /// <summary>
        /// 其他黑白格式转为Mono8
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pInData">输出图片数据</param>
        /// <param name="pOutData">输出图片数据</param>
        /// <param name="nHeight">高</param>
        /// <param name="nWidth">宽</param>
        /// <param name="nPixelType">像素格式</param>
        /// <returns></returns>

        /// <summary>
        /// 其他彩色格式转为RGB8
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pSrc"></param>
        /// <param name="nHeight"></param>
        /// <param name="nWidth"></param>
        /// <param name="nPixelType"></param>
        /// <param name="pDst"></param>
        /// <returns></returns>

        // ch:获取丢帧数 | en:Get Throw Frame Number
        /// <summary>★P2-2：后台采样 12 路丢帧数写 _lostFrameCache（每 3 圈一次降频）。UI_monitor（后台线程）调用。</summary>
        private void SampleLostFrames()
        {




            _lostFrameSampleCounter++;
            if (_lostFrameSampleCounter % 3 != 0) return;   // 降频：每 3 圈（~105ms）采一次
            for (int i = 0; i < 12; i++)
            {
                try { _lostFrameCache[i] = GetLostFrame(i); } catch { }
            }
        }
        
        private string GetLostFrame(int nIndex)
        {
            if (_cameraCtrl.Cameras[nIndex] == null)
                return "0";

            MyCamera.MV_ALL_MATCH_INFO pstInfo = new MyCamera.MV_ALL_MATCH_INFO();
            IntPtr allocated = IntPtr.Zero;   // 记录实际分配，finally 中统一释放，避免中途异常泄漏
            try
            {
                if (m_pDeviceInfo[nIndex].nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_MATCH_INFO_NET_DETECT MV_NetInfo = new MyCamera.MV_MATCH_INFO_NET_DETECT();
                    pstInfo.nInfoSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MyCamera.MV_MATCH_INFO_NET_DETECT));
                    pstInfo.nType = MyCamera.MV_MATCH_TYPE_NET_DETECT;
                    int size = Marshal.SizeOf(MV_NetInfo);
                    pstInfo.pInfo = Marshal.AllocHGlobal(size);
                    allocated = pstInfo.pInfo;
                    Marshal.StructureToPtr(MV_NetInfo, pstInfo.pInfo, false);

                    _cameraCtrl.Cameras[nIndex].MV_CC_GetAllMatchInfo_NET(ref pstInfo);
                    MV_NetInfo = (MyCamera.MV_MATCH_INFO_NET_DETECT)Marshal.PtrToStructure(pstInfo.pInfo, typeof(MyCamera.MV_MATCH_INFO_NET_DETECT));

                    return MV_NetInfo.nLostFrameCount.ToString();
                }
                else if (m_pDeviceInfo[nIndex].nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    MyCamera.MV_MATCH_INFO_USB_DETECT MV_NetInfo = new MyCamera.MV_MATCH_INFO_USB_DETECT();
                    pstInfo.nInfoSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MyCamera.MV_MATCH_INFO_USB_DETECT));
                    pstInfo.nType = MyCamera.MV_MATCH_TYPE_USB_DETECT;
                    int size = Marshal.SizeOf(MV_NetInfo);
                    pstInfo.pInfo = Marshal.AllocHGlobal(size);
                    allocated = pstInfo.pInfo;
                    Marshal.StructureToPtr(MV_NetInfo, pstInfo.pInfo, false);

                    _cameraCtrl.Cameras[nIndex].MV_CC_GetAllMatchInfo_NET(ref pstInfo);
                    MV_NetInfo = (MyCamera.MV_MATCH_INFO_USB_DETECT)Marshal.PtrToStructure(pstInfo.pInfo, typeof(MyCamera.MV_MATCH_INFO_USB_DETECT));

                    return MV_NetInfo.nErrorFrameCount.ToString();
                }
                else
                {
                    return "0";
                }
            }
            finally
            {
                if (allocated != IntPtr.Zero)
                    Marshal.FreeHGlobal(allocated);
            }
        }
        // ch:去除自定义的像素格式 | en:Remove custom pixel formats
        private bool RemoveCustomPixelFormats(MyCamera.MvGvspPixelType enPixelFormat)
        {
            Int32 nResult = ((int)enPixelFormat) & (unchecked((Int32)0x80000000));
            if (0x80000000 == nResult)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        private void bnStopGrab_Click(object sender, EventArgs e)
        {
            StopGrabFor(0);

        }

        private void bnClose_Click(object sender, EventArgs e)
        {
            // ★B1 修复：关闭前先停用自动重连、停 timer2，并有限等待在途重连 Task 落——
            //   原实现把"停用重连"放在所有关闭动作 + ResetMember 之后：在途 Task 可能拿旧 device1 缓存
            //   把刚关掉的槽重新打开（幽灵相机继续进帧），且 ResetMember 重赋 cbImage 后旧委托失去
            //   字段 root，GC 回收 thunk → 原生回调打空指针的崩溃窗口。
            _cameraReconnectEnabled = false;
            try { timer2.Enabled = false; } catch { }
            for (int w = 0; w < 40 && chonglianzhong; w++) Thread.Sleep(50);   // 最多等 2 秒（有界）
            for (int i = 0; i < 12; ++i)
            {
                int nRet;

                try
                {
                    // ★H4 修复：原实现只在 JobCount 以内的槽位真正关设备，却无条件把 12 个槽全置 null——
                    //   槽位 ≥ JobCount 的已开设备（例如此前方案数量调小过）句柄被丢弃但设备未关：
                    //   设备资源被占用直到进程退出、回调仍推帧，且旧委托失去 rooting 有打到已回收委托的崩溃窗口。
                    //   现改为按"槽位是否真有相机对象"无条件关闭全部 12 槽。
                    if (_cameraCtrl.Cameras[i] != null)
                    {
                        // ★ 关键修复：先停止抓流，再关闭设备（防止回调线程冲突）
                        lock (_cameraLock)
                        {
                        bool[] isGrabbing = new bool[] { m_bGrabbing1, m_bGrabbing2, m_bGrabbing3, m_bGrabbing4,
                                                          m_bGrabbing5, m_bGrabbing6, m_bGrabbing7, m_bGrabbing8, m_bGrabbing9, m_bGrabbing10, m_bGrabbing11, m_bGrabbing12 };
                        if (i < isGrabbing.Length && isGrabbing[i])
                        {
                            _cameraCtrl.Cameras[i].MV_CC_StopGrabbing_NET();
                            Thread.Sleep(30);
                        }

                        nRet = _cameraCtrl.Cameras[i].MV_CC_CloseDevice_NET();
                        nRet = _cameraCtrl.Cameras[i].MV_CC_DestroyDevice_NET();
                        }
                    }
                }
                catch { }
                // 关闭后立即置空，防止后续对无效句柄调用 SDK
                _cameraCtrl.Cameras[i] = null;
            }
            // ★ 修复（关设备顺序）：所有相机已停止取流并关闭后，再释放非托管缓冲，
            //   避免海康 SDK 回调线程仍在用 m_pSaveImageBuf 做像素转换时被 FreeHGlobal 导致崩溃
            // ★A2 修复：仅"设备都已关"还不够——最后几帧可能仍在回调线程里做像素转换（毫秒级）。
            //   回调全程被 _inspectionLifecycle 包着（ImageCallBack TryEnter/Exit），
            //   此处等它归零即证明"无回调正在使用缓冲"，再做 Free/Destroy
            //   （原先 FreeHGlobal 与 EnsureConvertBuffer 的无锁 free+realloc 存在 use-after-free/双重释放）。
            if (!_inspectionLifecycle.WaitForIdle(2000))
                _logger.WriteLog("bnClose：等待回调静止超时(2s)，仍继续释放缓冲（设备已关、无新回调，窗口已极小）");
            for (int i = 0; i < 12; i++)
            {
                FreeDriverBuffer(i);
                if (m_pSaveImageBuf[i] != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(m_pSaveImageBuf[i]); } catch { }
                    m_pSaveImageBuf[i] = IntPtr.Zero;
                }
            }
            // ch:重置成员变量 | en:Reset member variable
            m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            m_bGrabbing1 = false;
            m_bGrabbing2 = false;
            m_bGrabbing3 = false;
            m_bGrabbing4 = false;
            m_bGrabbing5 = false;
            m_bGrabbing6 = false;
            m_bGrabbing7 = false;
            m_bGrabbing8 = false;
            m_bGrabbing9 = false;
            m_bGrabbing10 = false;
            m_bGrabbing11 = false;
            m_bGrabbing12 = false;
            m_nCanOpenDeviceNum = 0;
            m_nDevNum = 0;

            //try
            //{
            //    // ch:取流标志位清零 | en:Reset flow flag bit
            //if (m_bGrabbing1 == true)
            //{
            //    m_bGrabbing1 = false;
            //    m_hReceiveThread.Join();
            //}
            //}
            //catch { }


            try
            {

                bnOpen.Enabled = true;
                bnClose.Enabled = false;
                bnTriggerExec1.Enabled = false;
                bnStartGrab1.Enabled = false;
                bnStopGrab1.Enabled = false;
                bnTriggerExec2.Enabled = false;
                bnStartGrab2.Enabled = false;
                bnTriggerExec3.Enabled = false;
                bnStartGrab3.Enabled = false;
                bnTriggerExec4.Enabled = false;
                bnStartGrab4.Enabled = false;
                bnTriggerExec5.Enabled = false;
                bnStartGrab5.Enabled = false;
                bnTriggerExec6.Enabled = false;
                bnStartGrab6.Enabled = false;
                bnTriggerExec7.Enabled = false;
                bnStartGrab7.Enabled = false;
                bnTriggerExec8.Enabled = false;
                bnStartGrab8.Enabled = false;
                bnTriggerExec9.Enabled = false;
                bnStartGrab9.Enabled = false;
                bnStopGrab9.Enabled = false;
                bnTriggerExec10.Enabled = false;
                bnStartGrab10.Enabled = false;
                bnStopGrab10.Enabled = false;
                bnTriggerExec11.Enabled = false;
                bnStartGrab11.Enabled = false;
                bnStopGrab11.Enabled = false;
                bnTriggerExec12.Enabled = false;
                bnStartGrab12.Enabled = false;
                bnStopGrab12.Enabled = false;
                ResetMember();
                _cameraReconnectEnabled = false;
                try { timer2.Enabled = false; } catch { }
            }
            catch { }
        }
        public void ResetMember()
        {
            m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            // m_bGrabbing = false;
            m_nCanOpenDeviceNum = 0;
            m_nDevNum = 0;
            DeviceListAcq();
            m_nFrames = new int[12];
            m_bSaveImg = new bool[12];
            cbImage = new MyCamera.cbOutputExdelegate(ImageCallBack);
            //   m_bTimerFlag = false;
            m_hDisplayHandle = new IntPtr[12];
            m_pDeviceInfo = new MyCamera.MV_CC_DEVICE_INFO[12];
        }

        // 各相机“通讯触发”下拉框编号（设计器命名非连续）；相机9-12 无通讯触发项为 -1
        private static readonly int[] SoftTriggerCommComboBox = { 1, 4, 5, 8, 25, 28, 31, 34, -1, -1, -1, -1 };

        // 软触发 CheckBox 切换：设置 TriggerSource 并联动触发执行按钮（取代 12 段 cbSoftTriggerN_CheckedChanged）
        private void SoftTriggerChangedFor(int index)
        {
            if (_cameraCtrl.Cameras[index] == null) return;
            try
            {
                CheckBox cb = Controls.Find(ParamBoxName("cbSoftTrigger", index), true).FirstOrDefault() as CheckBox;
                bool comm = false;
                if (index < SoftTriggerCommComboBox.Length && SoftTriggerCommComboBox[index] >= 0)
                {
                    ComboBox combo = Controls.Find("comboBox" + SoftTriggerCommComboBox[index], true).FirstOrDefault() as ComboBox;
                    comm = (combo != null && combo.Text == "通讯触发");
                }
                if (cb != null && (cb.Checked || comm))
                {
                    _cameraCtrl.Cameras[index].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                    if (IsCameraGrabbing(index))
                        SetTriggerExecEnabled(index, true);
                }
                else
                {
                    _cameraCtrl.Cameras[index].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                    SetTriggerExecEnabled(index, false);
                }
            }
            catch { }
        }

        private void cbSoftTrigger_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(0);

        }

        private void bnTriggerExec_Click(object sender, EventArgs e)
        {
            TriggerExecFor(0);

        }

        #endregion

        private void UpdateFlowTabNavBounds()
        {
            if (flowTabNav == null || tabControl1 == null) return;
            flowTabNav.Width = tabControl1.Width;
            flowTabNav.Location = new Point(
                tabControl1.Left,
                tabControl1.Top - flowTabNav.Height - TabNavExtraLift);
        }

        private void CreateTabNav()
        {
            flowTabNav = new FlowLayoutPanel();
            flowTabNav.FlowDirection = FlowDirection.LeftToRight;
            flowTabNav.WrapContents = false;
            flowTabNav.AutoScroll = true;
            flowTabNav.AutoSize = false;
            flowTabNav.Height = 34 + TabNavScrollBarGap + 6;
            flowTabNav.Width = tabControl1.Width;
            flowTabNav.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            flowTabNav.BackColor = UiBgNav;
            flowTabNav.Padding = new Padding(4, 3, 4, TabNavScrollBarGap);
            flowTabNav.Name = "flowTabNav";
            UpdateFlowTabNavBounds();

            tabNavButtons = new Button[12];
            _tabNavCameraBaseColors = new Color[12];
            tabNavPages = new TabPage[] { tabPage1, tabPage2, tabPage3, tabPage4, tabPage7, tabPage8, tabPage9, tabPage10, tabPage11, tabPage12, tabPage13, tabPage14 };
            string[] labels = { "相机1", "相机2", "相机3", "相机4", "相机5", "相机6", "相机7", "相机8", "相机9", "相机10", "相机11", "相机12" };

            _btnNavConfig = StyleTabNavButton("配置窗", _p.TabNavUtility, 76);
            _btnNavConfig.Tag = tabPage5;
            _btnNavConfig.Click += (s, e) => { tabControl1.SelectedTab = tabPage5; };
            flowTabNav.Controls.Add(_btnNavConfig);

            _btnNavMonitor = StyleTabNavButton("监控画面", _p.TabNavUtility, 88);
            _btnNavMonitor.Tag = tabPage6;
            _btnNavMonitor.Click += (s, e) => { tabControl1.SelectedTab = tabPage6; };
            flowTabNav.Controls.Add(_btnNavMonitor);

            Label sep = new Label();
            sep.Text = "|";
            sep.ForeColor = UiBorder;
            sep.AutoSize = true;
            sep.TextAlign = ContentAlignment.MiddleCenter;
            sep.Height = 28;
            sep.Margin = new Padding(6, 8, 6, 0);
            sep.Font = UiFont;
            flowTabNav.Controls.Add(sep);

            Color[] colors = {
                Color.FromArgb(58, 118, 178), Color.FromArgb(52, 152, 120), Color.FromArgb(196, 148, 48),
                Color.FromArgb(178, 72, 72),  Color.FromArgb(108, 92, 190), Color.FromArgb(178, 118, 58),
                Color.FromArgb(48, 138, 88),  Color.FromArgb(88, 138, 210), Color.FromArgb(188, 108, 48),
                Color.FromArgb(138, 102, 198), Color.FromArgb(48, 148, 148), Color.FromArgb(168, 118, 118)
            };

            for (int i = 0; i < 12; i++)
            {
                _tabNavCameraBaseColors[i] = colors[i];
                Button btn = StyleTabNavButton(labels[i], colors[i], 72);
                btn.Tag = tabNavPages[i];
                btn.Click += (s, e) =>
                {
                    tabControl1.SelectedTab = (TabPage)btn.Tag;
                };
                flowTabNav.Controls.Add(btn);
                tabNavButtons[i] = btn;
            }
            tabControl1.SelectedIndexChanged += (s, e) => RefreshTabNavSelection();
            RefreshTabNavSelection();
            // 隐藏原生Tab headers（1像素高，看不见）
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.Padding = new Point(0, 0);
            // 把tabControl1往下移，腾出导航栏空间
            tabControl1.Top += 1;

            this.Controls.Add(flowTabNav);
            this.Controls.SetChildIndex(flowTabNav, 0);
            // 默认跟随tabControl1的可见性（设计器默认tabControl1.Visible=false）
            flowTabNav.Visible = tabControl1.Visible;
            // 当tabControl1位置/大小变化时同步导航栏
            tabControl1.Resize += (s, e) => UpdateFlowTabNavBounds();
            tabControl1.LocationChanged += (s, e) => UpdateFlowTabNavBounds();
        }

        private void cogRecordDisplay10_Enter(object sender, EventArgs e)
        {

        }

        private void label150_Click(object sender, EventArgs e)
        {

        }
    }
}
