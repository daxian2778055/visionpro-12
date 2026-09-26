using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public delegate void delegateHanndler();

public class Myjob
    {
        public int trriger;
        public int yun;
        public CogJob job;
        public CogToolBlock block;
        public int sum;
        public int oksum;
        // ★第26轮#30：原 ngsum 字段已删除——全仓从未自增过（NG 数界面按 sum-oksum 计算），
        //   留着只会让人误以为它是可信的 NG 计数来源。
        public float rate;
        public int number;
        public int numberng;
        public string pathhead_ok;
        public string pathhead_ng;
        public FileInfo fileok;
        public FileInfo fileng;
        public bool Color;
        public CogImageFileBMP Cogbmp;
        public bool trrigerEn;
        public int out_end;
        public int trrigersum;
        public Bitmap img;
        public int master;
        public int timespace;
        public string path_number;
        public bool cunok;
        public bool cunng;
        public ICogRecord newrecod;
        // ★G1 修复：同一 CogToolBlock 双线程进入标记。UI 手动回图与检测线程都经 getrecord 汇流到
        //   block.Run()，同一流程并发 Run 会破坏 VisionPro 工具内部状态（异常/垃圾结果/渲染错乱）。
        //   每槽 CAS 互斥：占用中则本次调用丢弃（通讯帧由 PLC 超时判 NG，手动回图重试点一次即可）。
        public int recordBusy;
        public int index;
        public int xianshi;
        public int fit;
        public int ok1;
        public int ng1;
        public double runtime;
        public int runcishu;
        public int en;
        public string triggerMode;
        public string triggerZifu;
        public string jieshouZifu;
        /// <summary>通讯触发：仅在为 true 时处理下一帧回调，避免开流/残留帧自动检测。</summary>
        public volatile bool commTriggerPending;
        /// <summary>★ 2026-09-06 ⑤：待处理触发计数（门控改用此字段替代 bool）。触发时 +1，回帧被接收时 -1，
        /// 检测完成不再清零，避免冲掉在途新触发的置位。&gt;0 表示仍有触发在等回帧。</summary>
        public volatile int commTriggerPendingCount;
        /// <summary>
        /// BUG A 修复（2026-09-13 重建）：触发来源的<b>不可变快照</b>。
        /// 原 triggerLinkId / triggerProto 是两个独立 volatile int，多协议并发写、检测线程两次独立读会交错成配对错乱；
        /// 改为单个引用一次性原子替换，proto/link 配对永真；读取方取出后立刻置 null，避免粘滞到下一次检测。
        /// </summary>
        public volatile CommTriggerSource triggerSrc;
        public bool dengluEn;
        public bool shijianEn;
        public string danwu_time;
        public int danwu_cishu;
        public DataTable myTable;
        public DataTable myTable1;
        public bool tishi;
        public bool tcp;
        public bool serial;
        public bool modbustemp;
        public bool cuntu;
        public bool xuanran;
        public bool IO;
        public FolderBrowserDialog dlg;
        public int IOyanshi;
        // ★第26轮#2：默认空串——守卫里 `job.state.Contains("相")` 在 InitializeJobManager 赋值前
        //   就可能被下拉框初始化触发，null 直接 NRE。
        public string state = "";
        public Stopwatch timewatch;
        public long time;
        public bool roi;
        public int changdu;
        public string address;
        public string biaotou;
        public string tianbiao;
        public CogCalibNPointToNPointTool calib;
        public float[] record = new float[] { 0.99f, 0.99f, 0.99f, 0.24f };
        public int records = 0;
        public bool zidongbaoguang = false;
        public float baoguang = 0;
        /// <summary>
        /// 每帧相机采集耗时（包括曝光+读出+传输，微秒），从 pFrameInfo.fExposureTime 获取
        /// </summary>
        public float cameraAcqTicks = 0;

        // 使用Interlocked或lock保护的字段，通过属性访问
        private int _changel1 = 0;
        private int _changel2 = 0;
        public int outputok2 = 0;
        public int outputng2 = 0;
        public object locker_ok = new object();
        public object locker_ng = new object();
        public delegateHanndler myhandle;
        public delegateHanndler myhandle1;
        public string output3 = "空";
        public Dictionary<int, CogToolBlock> list_block = new Dictionary<int, CogToolBlock>();

        // 使用锁保护outputok和outputng的set操作
        private readonly object _outputLock = new object();

        public int outputok
        {
            get { return _changel1; }
            set
            {
                lock (_outputLock)
                {
                    _changel1 = value;
                }
                // 委托调用放在锁外部，防止死锁
                myhandle?.Invoke();
            }
        }

        public int outputng
        {
            get { return _changel2; }
            set
            {
                lock (_outputLock)
                {
                    _changel2 = value;
                }
                myhandle1?.Invoke();
            }
        }

        /// <summary>
        /// 安全释放资源
        /// </summary>
        public void SafeRelease()
        {
            try
            {
                if (dlg != null)
                {
                    try { dlg.Dispose(); } catch { }
                    dlg = null;
                }
                if (myTable != null)
                {
                    try { myTable.Dispose(); } catch { }
                    myTable = null;
                }
                if (myTable1 != null)
                {
                    try { myTable1.Dispose(); } catch { }
                    myTable1 = null;
                }
                if (img != null)
                {
                    try { img.Dispose(); } catch { }
                    img = null;
                }
                if (timewatch != null)
                {
                    try { timewatch.Stop(); } catch { }
                    timewatch = null;
                }
                if (newrecod != null)
                {
                    try { newrecod = null; } catch { }
                }
                if (Cogbmp != null)
                {
                    try { Cogbmp = null; } catch { }
                }
                block = null;
                job = null;
                calib = null;
                fileok = null;
                fileng = null;
            }
            catch
            {
                // 资源释放时的异常不做处理
            }
        }
    }
}
