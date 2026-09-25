using System;
using System.Collections.Generic;
using Cognex.VisionPro;
using Cognex.VisionPro.QuickBuild;
using MvCamCtrl.NET;
using WindowsFormsApplication1.Core.Camera;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 作业服务。集中持有 12 路视觉作业（<see cref="Myjob"/>）实例、作业数组与全局运行开关（yunxing），
    /// 并收口“通讯触发→相机软触发拍照”的纯路由逻辑与触发门控状态，由组合根以单例创建并注入 Form1。
    /// <para>
    /// Stage1：收口作业所有权与运行开关。
    /// Stage2：收口通讯触发路由（ApplyCommTrigger）与触发门控状态（CommTriggerArmed / 各路 pending），
    ///         以及相机软触发的回调接线（TriggerSoftwareCallback）。Form1 的 DataChange_* 事件处理器与
    ///         方案切换/UI 更新仍留在 Form1 作为胶水层，本服务不触碰任何 UI 控件。
    /// </para>
    /// <para>
    /// 真正的作业执行（InitializeJobManager 加载、getrecord 检测核心、IO/PLC 输出）属于更大的职责，
    /// 将在后续阶段增量迁入，每步编译验证、行为对照。
    /// </para>
    /// </summary>
    public sealed class JobService
    {
        public Myjob myjob1 = new Myjob();
        public Myjob myjob2 = new Myjob();
        public Myjob myjob3 = new Myjob();
        public Myjob myjob4 = new Myjob();
        public Myjob myjob5 = new Myjob();
        public Myjob myjob6 = new Myjob();
        public Myjob myjob7 = new Myjob();
        public Myjob myjob8 = new Myjob();
        public Myjob myjob9 = new Myjob();
        public Myjob myjob10 = new Myjob();
        public Myjob myjob11 = new Myjob();
        public Myjob myjob12 = new Myjob();

        /// <summary>12 路作业实例数组，便于按索引访问（与 myjob1..myjob12 一一对应）。</summary>
        public Myjob[] Myjobs;

        /// <summary>全局运行开关：true 时各路按 yun/en 状态执行检测。</summary>
        public bool yunxing;

        // ---- Stage2：通讯触发路由状态 ----
        /// <summary>运行启动完成后才允许通讯/软触发拍照，避免开软件瞬间误触发。</summary>
        public volatile bool CommTriggerArmed;

        /// <summary>由 InitializeJobManager / 方案切换写入的 VisionPro 作业管理器（ApplyCommTrigger 用它做 JobCount 门控）。</summary>
        public CogJobManager JobManager { get; set; }

        /// <summary>相机软触发回调：由 Form1 接线到其 TriggerSoftwareCamera（触发逻辑涉及相机抓取态判断，留在 Form1）。</summary>
        public Func<int, int> TriggerSoftwareCallback;

        private readonly CameraController _cameraCtrl;
        private readonly LoggingService _logger;

        public JobService(CommunicationService comm, CameraController cameraCtrl, LoggingService logger)
        {
            Myjobs = new Myjob[]
            {
                myjob1, myjob2, myjob3, myjob4, myjob5, myjob6,
                myjob7, myjob8, myjob9, myjob10, myjob11, myjob12
            };
            for (int i = 0; i < _pendingTriggers.Length; i++)
            {
                _pendingTriggers[i] = new PendingCameraTriggers();
                _continuousParameters[i] = new Dictionary<string, string>();
            }
            _cameraCtrl = cameraCtrl;
            _logger = logger;
        }

        private readonly PendingCameraTriggers[] _pendingTriggers = new PendingCameraTriggers[12];
        private readonly Dictionary<string, string>[] _continuousParameters = new Dictionary<string, string>[12];

        private static bool IsContinuous(Myjob job) => (job.triggerMode ?? "").Replace("\0", "").Trim() == "连续运行";   // ★复盘P2-7：与消费侧同口径——triggerMode 若带 '\0' 尾巴则 Trim 不掉，恒判非连续

        // Continuous cameras accept parameter updates without a software trigger.
        // Apply updates on their inspection worker, never from the communication thread during Run().
        public void StageContinuousParameter(int slot, string key, string value)
        {
            if (slot < 0 || slot >= 12 || string.IsNullOrEmpty(key) || !IsContinuous(Myjobs[slot])) return;
            var parameters = _continuousParameters[slot];
            lock (parameters) parameters[key] = value;
        }

        public void ApplyContinuousParameters(int slot, Myjob job)
        {
            if (slot < 0 || slot >= 12 || !IsContinuous(job)) return;
            var parameters = _continuousParameters[slot];
            lock (parameters)
            {
                foreach (var parameter in parameters)
                    if (job.block.Inputs.Contains(parameter.Key))
                        job.block.Inputs[parameter.Key].Value = parameter.Value;
                parameters.Clear();
            }
        }

        public bool HasPendingTrigger(int slot) => _pendingTriggers[slot].HasPending;
        /// <summary>★ 2026-09-13：某路待回帧记录的最长等待毫秒数；无在途记录返回 -1。供回帧超时监测使用。</summary>
        public double OldestPendingTriggerAgeMs(int slot)
        {
            if (slot < 0 || slot >= 12) return -1;
            return _pendingTriggers[slot].OldestAgeMs();
        }
        public CameraTriggerRecord TakeTrigger(int slot) => _pendingTriggers[slot].Take();
        public void ClearCommTriggerPending(int slot)
        {
            if (slot >= 0 && slot < 12)
            {
                _pendingTriggers[slot].Clear();
                lock (_continuousParameters[slot]) _continuousParameters[slot].Clear();
            }
        }
        public void ClearAllCommTriggerPending()
        {
            for (int i = 0; i < 12; i++) ClearCommTriggerPending(i);
        }

        public int RequestTrigger(int cameraIndex, KeyValuePair<string, string> payload, CommTriggerSource source)
        {
            if (cameraIndex < 0 || cameraIndex >= 12 || TriggerSoftwareCallback == null) return -1;
            if (IsContinuous(Myjobs[cameraIndex]) && !string.IsNullOrEmpty(payload.Key))
            {
                StageContinuousParameter(cameraIndex, payload.Key, payload.Value);
                return MyCamera.MV_OK;
            }
            return _pendingTriggers[cameraIndex].Execute(new CameraTriggerRecord(payload, source),
                () => TriggerSoftwareCallback(cameraIndex));
        }

        public void ApplyCommTrigger(Myjob job, int cameraIndex, string selection, string mode,
            string val1, string val2, string inputKey, CommTriggerSource source)
        {
            if (!CommTriggerHelper.CheckTriggerCondition(selection, mode, val1, val2)) return;
            if (JobManager == null || cameraIndex < 0 || cameraIndex >= 12) return;
            if (JobManager.JobCount <= cameraIndex || job == null || job.en != 1) return;
            if (_cameraCtrl.Cameras[cameraIndex] == null || !CommTriggerArmed) return;
            int result = RequestTrigger(cameraIndex, new KeyValuePair<string, string>(inputKey, selection), source);
            if (result != MyCamera.MV_OK)
                _logger.WriteLog("相机" + (cameraIndex + 1) + " 软触发失败或待回帧队列已满: " + result);
        }

        /// <summary>设置第 camNo 路（1 基）OK 输出状态，线程安全（替代 ok1..ok12）。</summary>
        public void SetOk(int camNo, int b)
        {
            var job = Myjobs[camNo - 1];
            lock (job.locker_ok)
            {
                job.outputok2 = b;
                job.outputok = (b > 0) ? 1 : 0;
            }
        }

        /// <summary>设置第 camNo 路（1 基）NG 输出状态，线程安全（替代 ng1..ng12）。</summary>
        public void SetNg(int camNo, int b)
        {
            var job = Myjobs[camNo - 1];
            lock (job.locker_ng)
            {
                job.outputng2 = b;
                job.outputng = (b > 0) ? 1 : 0;
            }
        }
    }
}
