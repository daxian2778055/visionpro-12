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
        public int ngsum;
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
        public int temptu;
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
        /// <summary>
        /// 阶段 5：本次检测由哪条通讯连接触发（0=非通讯触发/未知，1=该协议主连接，2..4=扩展连接）。
        /// 检测结果只回写给这条连接。
        /// </summary>
        public volatile int triggerLinkId;
        /// <summary>
        /// 阶段 7（2026-09-06）：触发来源的<b>协议</b>。0=非通讯触发，1=FINS，2=ModbusTCP，3=ModbusRTU。
        /// 光有 linkId 无法定位连接（FINS 连接 2 与 ModbusTCP 连接 2 是两条不同的连接），必须与 triggerLinkId 配对；
        /// 回写后由主界面清零，避免残留导致后续检测串到别的连接。
        /// </summary>
        public volatile int triggerProto;
        public bool dengluEn;
        public bool shijianEn;
        public bool jiasu;
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
        public string state;
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
