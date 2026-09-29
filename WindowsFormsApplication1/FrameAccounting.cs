using System.Text;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ★第53轮：帧账本——把「相机触发过多少次」和「界面检测数为什么少了」逐笔对上。
    ///
    /// 现场长期混淆的三个口径：
    ///   m_nFrames[路] = 我们**收到**的帧（ProcessImageCallback 里自增）
    ///   myjob.sum     = 我们**跑完一遍检测**的帧（GetRecordCore 的「总数计算」唯一自增点）
    ///   界面「漏帧」   = m_nFrames - sum，只说差多少，不说差在哪。
    ///
    /// 丢弃按原因分类记账，并严格分三组（见 Groups）：
    ///   PreReceive 「接收前」——不产生 m_nFrames，因此不进漏帧；机台触发数与我们的接收数对不上时，缺口多半在这组。
    ///   AfterReceive 「接收后」——必须与漏帧一一对得上；对不上就在 Reconcile 里明说未归因多少，
    ///                绝不写成「没丢帧」。同口径见 DeadImageScan 的硬/软两级记账。
    ///   Note 「备注」——既不是丢帧也不参与对账（取图失败仍判 NG 照样计数、手动触发次数）。
    ///
    /// 线程约定：Note 跑在相机 SDK 回调线程与检测线程上，必须 O(1)、永不抛、永不阻塞。
    /// </summary>
    public sealed class FrameAccounting
    {
        public const int Slots = 12;

        /// <summary>丢弃/备注原因。分组见 Groups，新增项必须同步补 Display 与 Groups 两张表且保持在 Count 之前。</summary>
        public enum Reason
        {
            // ---- PreReceive 接收前（不计入「漏帧」）----
            CallbackPaused,         // ImageCallBack 拿不到检测生命周期（保存/切方案/停采暂停中）
            CallbackStopping,       // 回调入口判到停止/切方案/关窗
            CallbackSlotInvalid,    // 回调槽位越界或该路 job 不存在
            ModeGate,               // ShouldProcessImageCallback 判该帧不该处理（含模式串为空/不认识）
            CommNoTrigger,          // 通讯触发模式但本帧没有对应的在途触发记录

            // ---- AfterReceive 接收后（与「漏帧」严格对账）----
            QueueEvicted,           // 待检队列满，丢最旧帧
            EnqueueStopped,         // 入队入口判到停止/切方案/槽位非法
            WorkerStopping,         // 检测线程取出后判到切方案/关窗
            WorkerBusy,             // 该路 recordBusy 被占（回图/上一次检测未结束）或该路 job 为空
            Drained,                // 停线时排空队列里未检的帧
            RecordPaused,           // getrecord 拿不到检测生命周期
            RecordStopping,         // getrecord 内判到切方案/关窗（含方案切换丢弃旧帧）
            MasterGate,             // GetRecordCore 总门不通过（未运行 / 该路未启动 / 该路不在方案流程数内 / block 未加载）
            JobNotStopped,          // VisionPro 作业状态不是 Stopped，整段检测被跳过
            CoreException,          // GetRecordCore 外层 catch（本帧未产生检测数）
            WorkerException,        // 检测线程 catch（本帧未产生检测数）

            // ---- Note 备注（不参与对账）----
            AcquireFailed,          // 取图/转换失败：仍以空帧入队按失败交易处理，**照样计入检测数（判 NG）**
            ManualTrigger,          // 界面「软触发一次」发起的触发次数
            ManualTriggerStopped,   // 其中停止态发起的次数（按设计只采图、不计数）
            Count
        }

        /// <summary>每项原因属于哪一组（三值，因为「备注」既不算接收前也不算接收后）。</summary>
        public enum ReasonGroup { PreReceive, AfterReceive, Note }

        private static readonly string[] Display =
        {
            "暂停丢(回调)", "停止中丢(回调)", "槽位无效丢(回调)", "模式门丢(回调)", "无在途触发丢(回调)",
            "队列满丢", "入队被拒丢", "取出后停线丢", "该路占用中丢", "停线排空丢",
            "暂停丢(检测)", "检测中停线丢", "总门不通过", "作业非Stopped", "检测流程异常", "线程异常",
            "取图失败(仍判NG)", "手动触发", "手动触发(停止态)",
        };

        private static readonly ReasonGroup[] Groups =
        {
            ReasonGroup.PreReceive, ReasonGroup.PreReceive, ReasonGroup.PreReceive, ReasonGroup.PreReceive, ReasonGroup.PreReceive,
            ReasonGroup.AfterReceive, ReasonGroup.AfterReceive, ReasonGroup.AfterReceive, ReasonGroup.AfterReceive,
            ReasonGroup.AfterReceive, ReasonGroup.AfterReceive, ReasonGroup.AfterReceive, ReasonGroup.AfterReceive,
            ReasonGroup.AfterReceive, ReasonGroup.AfterReceive, ReasonGroup.AfterReceive,
            ReasonGroup.Note, ReasonGroup.Note, ReasonGroup.Note,
        };

        /// <summary>表长度一致性兜底：新增 Reason 忘了补表时，宁可全判「未归因」也不能静默漏账。</summary>
        private static readonly bool TablesAligned =
            Display.Length == (int)Reason.Count && Groups.Length == (int)Reason.Count;

        private readonly int[,] _counts = new int[Slots, (int)Reason.Count];

        /// <summary>记一笔。槽位/原因非法一律静默忽略——本方法跑在相机回调线程上，绝不允许抛。</summary>
        public void Note(int slot, Reason reason)
        {
            int r = (int)reason;
            if (slot < 0 || slot >= Slots || r < 0 || r >= (int)Reason.Count) return;
            Interlocked.Increment(ref _counts[slot, r]);
        }

        public int Get(int slot, Reason reason)
        {
            int r = (int)reason;
            if (slot < 0 || slot >= Slots || r < 0 || r >= (int)Reason.Count) return 0;
            return Volatile.Read(ref _counts[slot, r]);
        }

        /// <summary>该原因所属分组（越界原因按 Note 处理，即不参与任何对账）。</summary>
        public static ReasonGroup GroupOf(Reason reason)
        {
            int r = (int)reason;
            return r >= 0 && r < Groups.Length ? Groups[r] : ReasonGroup.Note;
        }

        /// <summary>该原因是否属于「接收后」组（对账组）。</summary>
        public static bool IsAfterReceive(Reason reason) => GroupOf(reason) == ReasonGroup.AfterReceive;

        /// <summary>
        /// 触发模式串归一化：模式串来自 vpp 输入或历史 ini，可能带残留 '\0' 与空白，
        /// 不先去干净会判不出"连续运行"。Form1.NormalizeTriggerMode 即本方法的转发，全项目一处实现。
        /// </summary>
        public static string NormalizeMode(string mode) => (mode ?? "").Replace("\0", "").Trim();

        /// <summary>
        /// 该路是否「连续运行」（相机自由跑帧）。判定收口到这里，是为了让这条边界**可被单测钉住**：
        /// ★第55轮恢复的正是第53轮误删的"连续运行的相机不参与计件"——当时它只是界面刷新处
        /// 一个内联条件，被当成"界面停在空种子文本"的显示缺陷顺手删掉了，边界就此消失。
        /// </summary>
        public static bool IsContinuousMode(string mode) => NormalizeMode(mode) == "连续运行";

        /// <summary>接收后已归因丢弃总数（对账用的分子）。</summary>
        public int TotalAfterReceive(int slot) => TotalWhere(slot, ReasonGroup.AfterReceive);

        /// <summary>接收前丢弃总数（机台触发数与漏帧对不上时，缺口多半在这里）。</summary>
        public int TotalBeforeReceive(int slot) => TotalWhere(slot, ReasonGroup.PreReceive);

        private int TotalWhere(int slot, ReasonGroup group)
        {
            if (slot < 0 || slot >= Slots || !TablesAligned) return 0;
            int total = 0;
            for (int i = 0; i < (int)Reason.Count; i++)
                if (Groups[i] == group)
                    total += Volatile.Read(ref _counts[slot, i]);
            return total;
        }

        /// <summary>分类明细（含备注类），0 的类别省略；全为 0 返回「无」。</summary>
        public string Breakdown(int slot) => BreakdownWhere(slot, null);

        /// <summary>
        /// 按分组取明细。备注类不会混进「接收前」，免得把"取图失败仍判 NG"念给现场听成丢帧。
        /// </summary>
        public string Breakdown(int slot, ReasonGroup group) => BreakdownWhere(slot, group);

        private string BreakdownWhere(int slot, ReasonGroup? group)
        {
            if (slot < 0 || slot >= Slots || !TablesAligned) return "无";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < (int)Reason.Count; i++)
            {
                if (group.HasValue && Groups[i] != group.Value) continue;
                int v = Volatile.Read(ref _counts[slot, i]);
                if (v == 0) continue;
                if (sb.Length > 0) sb.Append('+');
                sb.Append(Display[i]).Append(v);
            }
            return sb.Length > 0 ? sb.ToString() : "无";
        }

        /// <summary>
        /// 对账一行：接收 - 检测完成 = 差额，再看差额是否被「接收后」分类全部吃掉。
        /// 差额为负是合法的：回图/手动检测不来自相机帧，不入接收数。
        /// </summary>
        public string Reconcile(int slot, int received, int detected)
        {
            int gap = received - detected;
            int accounted = TotalAfterReceive(slot);
            StringBuilder sb = new StringBuilder();
            sb.Append("接收").Append(received).Append("，检测完成").Append(detected)
              .Append("，差额").Append(gap)
              .Append("（接收后已归因").Append(accounted).Append("）");

            if (!TablesAligned)
                return sb.Append("｜⚠ 账本分类表与原因枚举不同步，本轮归因不可用").ToString();

            string afterDetail = Breakdown(slot, ReasonGroup.AfterReceive);
            if (gap < 0)
                sb.Append("｜检测完成数大于接收数：回图/手动检测不来自相机帧，属正常，该差额不参与对账");
            else if (accounted == gap)
                sb.Append(gap == 0 ? "｜已查全（本路接收帧全部完成检测）"
                                   : "｜已查全：差额与归因分类逐项相符 " + afterDetail);
            else if (accounted < gap)
                sb.Append("｜⚠ 未查全：还有 ").Append(gap - accounted)
                  .Append(" 帧既没完成检测也没记账，存在未接入账本的丢弃路径，不能断定『没丢帧』。已归因：")
                  .Append(afterDetail);
            else
                sb.Append("｜⚠ 归因超出差额 ").Append(accounted - gap)
                  .Append("：同一帧被重复记账，或有丢弃其实发生在接收计数之前。已归因：")
                  .Append(afterDetail);
            return sb.ToString();
        }

        /// <summary>该路是否有任何账（只读诊断据此只列活跃路，避免 12 行空账刷屏）。</summary>
        public bool HasActivity(int slot)
        {
            if (slot < 0 || slot >= Slots) return false;
            for (int i = 0; i < (int)Reason.Count; i++)
                if (Volatile.Read(ref _counts[slot, i]) != 0) return true;
            return false;
        }

        public void ResetAll()
        {
            for (int s = 0; s < Slots; s++)
                for (int i = 0; i < (int)Reason.Count; i++)
                    Interlocked.Exchange(ref _counts[s, i], 0);
        }
    }
}
