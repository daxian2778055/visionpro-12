using System.Collections.Generic;
using System.Diagnostics;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 死引用诊断的一次扫描状态（★第51.1轮自 Form1.cs 抽出；51.2 补截断留痕；51.3 拆开"留痕"与"停"）。
    /// 演进里踩过的三个坑，都是"诊断说谎"的变体，留档防复发：
    /// 51.1 —— 节点计数放 static 且从不复位 ⇒ 第二次诊断一进函数就被预算挡住，照样打"未找到"。
    ///         改为每次新建实例，计数天然归零。
    /// 51.2 —— 只有"节点预算"会标记截断，深度到顶/集合超限是静默 return/break ⇒ 被挡在门外的部分
    ///         照样输出"未找到"。改为所有截断点统一 MarkTruncated(reason) 并把原因带进结论。
    /// 51.3 —— 但 51.2 把两件事塞进了同一个开关：CountNode() 开头的 if (Truncated) return false
    ///         让 MarkTruncated（**只该留痕**）变成了"整趟停止"。任一子树撞深度上限，其余 job
    ///         全部不再遍历——覆盖率反而比 51.1 还差，而日志还诚实地说"被截断（深度）"，
    ///         看起来正常。现在分成两个状态：
    ///           · Truncated/TruncatedReason —— 只是"结果不完整"的记号，**不挡任何遍历**；
    ///           · BudgetExhausted —— 节点预算/报告上限/耗时上限用尽，才让 CountNode 返回 false 停止。
    /// 另注：诊断跑在保存失败的 catch 里、UI 线程、弹窗之前，所以必须有**时间**预算，
    ///       否则 2 万节点 × 实测 0.23ms/节点 ≈ 4.6 秒的界面冻结。
    /// 与 Form1.cs 的分工：本类只管"扫描到哪了、报什么结论"，不碰任何 Cognex 类型。
    /// </summary>
    internal sealed class DeadImageScan
    {
        /// <summary>下钻深度上限。★第51.2轮由 6 提到 10：
        /// 现场加载路径是 group → 外层 CogToolBlock → 内层 CogToolBlock → 工具，光两层块嵌套就吃掉 2 层，
        /// 再加 Operator/Items[]/Pattern 才够到 TrainImage——6 层正卡在边沿。既然截断会如实报，
        /// 就必须真能走到底，否则日志永远是"可能不完整"。撞到它的代价只是留痕，不再停整趟（见类注释 51.3）。</summary>
        public const int MaxDepth = 10;

        /// <summary>单次扫描的节点预算：防反射遍历在超大工具树上把保存失败后的 UI 线程拖死。
        /// 读日志口径提醒：<see cref="CountNode"/> 是先 ++Nodes 再与本值比较，所以"访问 N 节点"
        /// 最坏会比预算多 1（走满即 20001）——这是 51.1 单测明确钉住的行为，不是漏统计。</summary>
        public const int MaxNodes = 20000;

        /// <summary>报告条数上限：日志只用来定位，几十条足够，封顶防止刷爆日志文件。</summary>
        public const int MaxReport = 60;

        /// <summary>时间预算（毫秒）。诊断在 UI 线程、MessageBox 之前跑，必须封顶。
        /// 上限依据：外部实测 Cognex 工具对象"一次 GetProperties + 全部可读属性 GetValue" ≈ 0.23ms/节点，
        /// 2 万节点走完 ≈ 4.6 秒 ⇒ 取 1.5 秒把最坏冻结压到约三分之一。
        /// **"1.5 秒 ≈ 6500 节点"是按该外部实测量出来的，不是承诺**：现场跑 Debug 时反射更慢，
        /// 实际可达节点数只会更少——以日志里的"耗时/节点"实测为准，别把这个数当保证。
        /// 可用单测注入更小的值。</summary>
        public const int MaxElapsedMs = 1500;

        /// <summary>已确认失效的图像引用路径。</summary>
        public readonly List<string> Dead = new List<string>();

        /// <summary>已访问实例的参考标识（防环 + 防同一实例多处引用重复下钻）。</summary>
        public readonly HashSet<int> Seen = new HashSet<int>();

        private readonly int _maxElapsedMs;
        private readonly long _startedAt = Stopwatch.GetTimestamp();
        private readonly double _msPerTick = 1000.0 / Stopwatch.Frequency;

        /// <summary>默认按 <see cref="MaxElapsedMs"/> 计时。</summary>
        public DeadImageScan() : this(MaxElapsedMs) { }

        /// <summary>可注入时间预算——单测用（主工程仍走无参构造）。</summary>
        public DeadImageScan(int maxElapsedMs) { _maxElapsedMs = maxElapsedMs; }

        /// <summary>本次扫描已访问的节点数。每次新建实例即从 0 开始。</summary>
        public int Nodes { get; private set; }

        /// <summary>结果是否不完整（节点/报告/耗时/深度/集合任一原因）。**只记账，不阻止遍历。**</summary>
        public bool Truncated { get; private set; }

        /// <summary>截断原因（首个原因为准），随结论一起打进日志。</summary>
        public string TruncatedReason { get; private set; }

        /// <summary>预算是否用尽（节点/报告/耗时三者之一）。只有它才让 <see cref="CountNode"/> 返回 false。</summary>
        public bool BudgetExhausted { get; private set; }

        /// <summary>整趟**真正**停下的原因（★第51.4轮 P2）。它与 <see cref="TruncatedReason"/> 可能不同：
        /// TruncatedReason 是"第一个留痕原因"（先撞到哪个算哪个，而且留痕不再停遍历），
        /// 这个才是"最后让 CountNode 返回 false 的那个预算"。只印前者会让人以为
        /// "树基本走完、只有一支深了"，实情可能是时间预算到点、后面几路 job 一个节点都没走。</summary>
        public string BudgetStopReason { get; private set; }

        /// <summary>已耗时毫秒——诊断在 catch 里，异常分支也要能报出耗时口径。</summary>
        public int ElapsedMs { get { return (int)((Stopwatch.GetTimestamp() - _startedAt) * _msPerTick); } }

        /// <summary>标记"结果不完整"及原因。只记第一次的原因——先撞到的才是把扫描挡住的那个。
        /// **它不设置 BudgetExhausted**：深度/集合这类截断只影响当前这一支，其余分支必须继续走。</summary>
        public void MarkTruncated(string reason)
        {
            if (Truncated) return;
            Truncated = true;
            TruncatedReason = string.IsNullOrEmpty(reason) ? "扫描上限" : reason;
        }

        /// <summary>
        /// 计一个节点。**只有预算用尽**才返回 false（调用方据此停止本支）；单纯的"结果不完整"记号
        /// 不影响放行——否则一处深度截断会连带废掉整个扫描（51.2 的回归，见类注释）。
        /// </summary>
        public bool CountNode()
        {
            if (BudgetExhausted) return false;
            if (ElapsedMs > _maxElapsedMs) return Stop("耗时上限 " + _maxElapsedMs + "ms");
            if (++Nodes > MaxNodes) return Stop("节点预算 " + MaxNodes + " 已用尽");
            if (Dead.Count >= MaxReport) return Stop("报告条数已达 " + MaxReport);
            return true;
        }

        /// <summary>预算用尽：记下"整趟真正停下的原因"，同时按既有口径留首个痕迹，然后停止。</summary>
        private bool Stop(string reason)
        {
            BudgetExhausted = true;
            BudgetStopReason = reason;
            MarkTruncated(reason);   // TruncatedReason 只在"还没留过痕"时才写（见 MarkTruncated）
            return false;
        }

        /// <summary>记录一条死引用路径。达报告上限后丢弃。截断标记由**两处**负责：CountNode 判定
        /// 节点/报告/耗时上限时会标；集合环里的调用方在自己 break 时也会标（见 Form1 集合遍历处）。</summary>
        public void Add(string path)
        {
            if (Dead.Count < MaxReport) Dead.Add(path);
        }

        /// <summary>按参考标识登记访问。返回 false 表示该实例已访问过（调用方应跳过）。</summary>
        public bool TryVisit(int referenceHash)
        {
            return Seen.Add(referenceHash);
        }

        /// <summary>
        /// 覆盖口径文案（★第51.5轮按外部实测修正两点，故放进本类以便单测钉住）：
        /// ① 分母不能是 Myjobs 的定长 12 —— 它恒为 12 且字段初始化全非空，未挂树的 job 在 walker
        ///    一进门 tool==null 就 return、零成本，既不该进分母也不该进分子；调用方按
        ///    "mj.job != null || mj.block != null"计数后传入。
        /// ② 分子是**走进去**的那一路，但"走到第 N 路" ≠ "第 N 路走完了"——预算若在这一路中途到点，
        ///    原写法会打"走到第 12/12 路"却伴随 BudgetExhausted，读者以为全走完了（外部模拟实测：
        ///    Nodes=20001、BudgetExhausted=True 时正是这个组合）。故预算耗尽时把最后一路标成"未完"。
        /// myjob 序号恒带（含预算耗尽外的正常情形）：少一个分支就少一个会骗人的状态。
        /// </summary>
        public string CoverageText(int totalTrees, int reachedTrees, int lastMyjob)
        {
            string s = "挂树 " + totalTrees + " 路中走到第 " + reachedTrees + " 路";
            if (reachedTrees <= 0)
                return BudgetExhausted ? s + "（预算已尽，未进入任何一棵）" : s;
            s += "（myjob" + lastMyjob;
            if (BudgetExhausted) s += "，未完";
            return s + "）";
        }

        /// <summary>
        /// 把扫描结果翻译成给日志的结论。**必须区分"没查完"与"真没有"**：
        /// 截断时如实说原因与可能漏报，否则"预算耗尽/深度到顶"会被读成"树里没有死引用"。
        /// </summary>
        public string Conclusion()
        {
            string why = string.IsNullOrEmpty(TruncatedReason) ? "扫描上限" : TruncatedReason;
            // ★第51.4轮 P2：留痕原因与"整趟停止原因"在 51.3 之后分叉了（留痕不再停遍历），
            //   只印前者是**归因骗人**——会让读者以为树基本走完、只有一支被挡，而实情可能是
            //   时间预算到点、后面几路 job 一个节点都没走。两者不同时并列输出。
            //   （"首个留痕原因为准"的既有口径不动，见 MarkTruncated。）
            if (BudgetExhausted && !string.IsNullOrEmpty(BudgetStopReason) && BudgetStopReason != why)
                why = why + "；整趟停止原因：" + BudgetStopReason;
            if (Dead.Count > 0)
            {
                return "共 " + Dead.Count + " 处"
                    + (Truncated ? "（截断原因：" + why + "，可能存在更多）" : "")
                    + "，位置如下";
            }
            if (Truncated)
            {
                return "未找到失效图像引用，但扫描被截断（" + why + "），结果可能不完整（不等于没有）";
            }
            return "未在工具树里找到失效图像引用（说明死引用不在本程序持有的工具树里，或发生在序列化过程中）";
        }
    }
}
