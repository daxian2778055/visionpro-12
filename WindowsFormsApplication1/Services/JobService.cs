using System;
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
            // ★ 2026-09-07 修复（②链路静默失效）：TriggerPayloads 12 个槽位此前从未初始化，全为 null，
            //   导致入队/出队两处 "!= null" 守卫恒 false —— payload 既写不进也取不出，
            //   通讯触发的参数快照实际上从未写入 block 输入（旧代码至少是即时写入，属功能倒退）。
            //   在此统一构造，与 Myjobs 同步完成，避免引用处再做惰性判空。
            for (int i = 0; i < TriggerPayloads.Length; i++)
                TriggerPayloads[i] = new System.Collections.Concurrent.ConcurrentQueue<System.Collections.Generic.KeyValuePair<string, string>>();
            for (int i = 0; i < TriggerSources.Length; i++)
                TriggerSources[i] = new System.Collections.Concurrent.ConcurrentQueue<CommTriggerSource>();
            _cameraCtrl = cameraCtrl;
            _logger = logger;
        }

        // ---- Stage2：触发门控与路由（纯逻辑，无 UI 依赖） ----

        /// <summary>★ 2026-09-06（②参数错位修复）：通讯触发 payload 快照队列。ApplyCommTrigger 在触发时刻把
        /// (inputKey, selection) 压入对应相机队列，帧回调 EnqueueInspectFrame 时按 FIFO 取出随帧携带，检测前写入
        /// block 输入，避免直接写共享 block 输入导致“前帧用后一件参数跑检测”的件/结果错位。触发与软触发回帧严格
        /// 按相机 FIFO 配对，故队列长度与未处理触发数一致，无需额外上限。</summary>
        public System.Collections.Concurrent.ConcurrentQueue<System.Collections.Generic.KeyValuePair<string, string>>[] TriggerPayloads =
            new System.Collections.Concurrent.ConcurrentQueue<System.Collections.Generic.KeyValuePair<string, string>>[12];

        /// <summary>★ 2026-09-13（多协议触发同一相机·来源覆盖修复）：通讯触发来源快照队列。
        /// 与 TriggerPayloads 同机制：触发时刻把 (协议, 连接号, 接收字符) 压入对应相机队列，
        /// 帧回调 EnqueueInspectFrame 按 FIFO 取出、随帧携带到检测阶段，结果回写用帧自带的来源。
        /// 原实现把来源存在 Myjob 单个字段上，多协议先后触发同一相机会互相覆盖，导致结果回错连接。</summary>
        public System.Collections.Concurrent.ConcurrentQueue<CommTriggerSource>[] TriggerSources =
            new System.Collections.Concurrent.ConcurrentQueue<CommTriggerSource>[12];

        /// <summary>★ 2026-09-07：单相机 payload 快照队列上限（与 Form1 的待检帧队列深度一致）。
        /// 防止「触发成功但相机未回帧」时快照无限滞留导致后续帧取到过期参数。</summary>
        public const int TriggerPayloadQueueDepth = 3;

        private static string NormalizeTriggerMode(string mode) => (mode ?? "").Replace("\0", "").Trim();

        private static bool IsCommTriggerMode(string mode) => NormalizeTriggerMode(mode) == "通讯触发";

        private static bool CheckTriggerCondition(string receivedValue, string mode, string val1, string val2)
            => CommTriggerHelper.CheckTriggerCondition(receivedValue, mode, val1, val2);

        public void MarkCommTriggerPending(int slot)
        {
            if (slot >= 0 && slot < 12)
            {
                Myjobs[slot].commTriggerPending = true;
                // ★ 2026-09-06 ⑤：待处理触发计数 +1（门控改用计数，回帧被接收时 -1）
                System.Threading.Interlocked.Increment(ref Myjobs[slot].commTriggerPendingCount);
            }
        }

        public void ClearCommTriggerPending(int slot)
        {
            if (slot >= 0 && slot < 12)
            {
                Myjobs[slot].commTriggerPending = false;
                // ★ ⑤ 仅在开关机/切型等显式重置场景清零计数；检测完成不再调用本方法（见 InspectWorker.finally）
                // ★ 原子清零：避免与轮询线程的 Interlocked.Increment 形成 lost-update（裸 =0 会吞掉在途增量）
                System.Threading.Interlocked.Exchange(ref Myjobs[slot].commTriggerPendingCount, 0);
            }
        }

        public void ClearAllCommTriggerPending()
        {
            for (int i = 0; i < 12; i++)
            {
                Myjobs[i].commTriggerPending = false;
                // ★ 原子清零：避免与轮询线程的 Interlocked.Increment 形成 lost-update（裸 =0 会吞掉在途增量）
                System.Threading.Interlocked.Exchange(ref Myjobs[i].commTriggerPendingCount, 0);
            }
        }

        /// <summary>
        /// 把本次通讯触发的来源（协议 + 连接号 + 接收字符）入队到对应相机，随帧携带到检测阶段。
        /// ★ 队列上限与 TriggerPayloadQueueDepth 一致，防止「触发成功但未回帧」时来源无限滞留。
        /// </summary>
        public void EnqueueTriggerSource(int cameraIndex, CommTriggerSource src)
        {
            if (src == null || cameraIndex < 0 || cameraIndex >= 12) return;
            var q = TriggerSources != null ? TriggerSources[cameraIndex] : null;
            if (q == null) return;
            CommTriggerSource stale;
            while (q.Count >= TriggerPayloadQueueDepth && q.TryDequeue(out stale)) { }
            q.Enqueue(src);
        }

        /// <summary>
        /// 仅在触发条件成立时把 payload 快照入队并软触发拍照（原 Form1.ApplyCommTrigger）。
        /// ★ 2026-09-06（②）：不再直接写共享 block.Inputs（会与下一触发 payload 错位），改为入队 TriggerPayloads，
        ///   由帧回调 EnqueueInspectFrame 取出随帧携带，getrecord 检测前写入 block 输入。
        /// </summary>
        public void ApplyCommTrigger(Myjob job, int cameraIndex, string selection, string mode, string val1, string val2, string inputKey)
        {
            if (!CheckTriggerCondition(selection, mode, val1, val2)) return;
            if (JobManager == null || cameraIndex < 0 || cameraIndex >= 12) return;
            if (JobManager.JobCount <= cameraIndex) return;
            if (job == null || job.en != 1) return;
            if (_cameraCtrl.Cameras[cameraIndex] == null) return;
            if (!CommTriggerArmed) return;
            job.jieshouZifu = selection;
            int nRet = TriggerSoftwareCallback != null ? TriggerSoftwareCallback(cameraIndex) : -1;
            if (MyCamera.MV_OK != nRet)
            {
                _logger.WriteLog("Trigger Software Fail!---" + nRet);
                // ★ 2026-09-07：软触发失败 → 本次触发不会回帧，「随帧携带」机制不适用。
                //   此时必须退化为改造前的「即时写入」，保证 PLC 下发的型号/参数不丢失：
                //   连续运行模式下相机本就不接受软触发命令（失败是常态），
                //   若只做回滚，参数就永远写不进 block —— 那比改造前是功能倒退。
                try
                {
                    if (job.block != null && !string.IsNullOrEmpty(inputKey) && job.block.Inputs.Contains(inputKey))
                        job.block.Inputs[inputKey].Value = selection;
                }
                catch (Exception exWrite)
                {
                    _logger.WriteLog("相机" + (cameraIndex + 1) + " 触发参数即时写入失败: " + exWrite.Message);
                }
                // ★ 2026-09-13 修复：原实现在此用 while(TryDequeue) 清空「整个」payload 队列来回滚，
                //   会连带清掉其他仍在等待回帧的触发参数 —— 那些帧此后取不到参数、或取到别人的参数。
                //   现在 payload 改为「软触发成功后才入队」，失败时本就没有本次 payload 需要回滚，
                //   因此这里不再清空队列；仅对触发来源做一次 best-effort 回滚。
                var srcQueue = (TriggerSources != null && cameraIndex >= 0 && cameraIndex < 12)
                    ? TriggerSources[cameraIndex] : null;
                if (srcQueue != null)
                {
                    CommTriggerSource junkSrc;
                    srcQueue.TryDequeue(out junkSrc);
                }
                // 回滚待处理计数：软触发失败不会有回帧，若不减则该计数只增不减，
                // 使 ShouldProcessImageCallback 的 count>0 恒真 → 通讯触发门控常开。
                if (job.commTriggerPendingCount > 0)
                    System.Threading.Interlocked.Decrement(ref job.commTriggerPendingCount);
                return;
            }
            // ★ 软触发成功，才入队 payload 快照（触发时刻的参数，随帧携带到检测阶段）
            var payloadQueue = (TriggerPayloads != null && cameraIndex >= 0 && cameraIndex < 12)
                ? TriggerPayloads[cameraIndex] : null;
            if (payloadQueue != null)
            {
                // ★ 2026-09-07：限制快照队列长度。否则「触发成功但相机未回帧」（掉线/停流）
                //   时 payload 会无限滞留，下一触发的帧 FIFO 取到的是过期参数。
                System.Collections.Generic.KeyValuePair<string, string> stale;
                while (payloadQueue.Count >= TriggerPayloadQueueDepth && payloadQueue.TryDequeue(out stale)) { }
                payloadQueue.Enqueue(new System.Collections.Generic.KeyValuePair<string, string>(inputKey, selection));
            }
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
