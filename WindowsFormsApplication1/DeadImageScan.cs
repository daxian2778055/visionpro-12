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
        /// <summary>下钻深度上限。★第51.8轮由 10 提到 15——**这次是实测拍的，不是推理**：
        /// 2026-09-29 现场真跑手动诊断（bin\Release\Log\2026-09-29.txt），连点 4 次，
        /// **每一次的结论都是"扫描被截断（下钻深度超过 10 层）"**，一次都没走到底：
        /// 既不能确认"没有死引用"，也不能排除，实用价值约等于 0。
        /// 也就是说 51.2 那次 6→10 的调整**没解决问题**（当时只有推理，没有实测）。
        /// <para>
        /// 提到 15 而不是更高：撞深度上限的很可能是**无关的深分支**（工具对象的 COM 包装链），
        /// 真正要走的 PMAlign 路径只有 7~8 层。深度开太大，节点数会被这些无关分支撑爆。
        /// 15 是折中，靠节点/时间两个预算兜底。
        /// </para>
        /// <para>
        /// 配套改动：Form1 侧两处深度截断留痕现在会**带上路径**——原来只说"超过 10 层"，
        /// 根本分不清是 PMAlign 那条太深还是无关分支太深，两者要往相反方向调。现在能分清了。
        /// 下次实测若仍报深度截断，日志里会直接给出是哪条路径、多深。
        /// </para>
        /// </summary>
        public const int MaxDepth = 15;

        /// <summary>单次扫描的节点预算：防反射遍历在超大工具树上把保存失败后的 UI 线程拖死。
        /// ★第51.8轮由 20000 提到 50000：实测在**深度只有 10** 时就已经走了 12570 节点
        /// （且是被深度截断、没走完的状态），深度放到 15 后节点数必然成倍涨，
        /// 20000 会先于深度上限撞上，结论变成"节点预算用尽"——那还是没走到底，等于白改。
        /// 50000 在实测热趟速度下约 1.75 秒，仍在时间预算内。
        /// 读日志口径提醒：<see cref="CountNode"/> 是先 ++Nodes 再与本值比较，所以"访问 N 节点"
        /// 最坏会比预算多 1——这是 51.1 单测明确钉住的行为，不是漏统计。</summary>
        public const int MaxNodes = 50000;

        /// <summary>报告条数上限：日志只用来定位，几十条足够，封顶防止刷爆日志文件。</summary>
        public const int MaxReport = 60;

        /// <summary>时间预算（毫秒）。诊断在 UI 线程、MessageBox 之前跑，必须封顶。
        /// <para>
        /// ★第51.8轮由 1500 提到 3000，**依据是 2026-09-29 的现场实测**（Release，4 次手动诊断）：
        ///   · 热趟 424ms / 12570 节点、471ms / 12570、523ms / 12576 ⇒ **约 0.035ms/节点**；
        ///   · **冷启动首趟 1400ms / 7608 节点 ⇒ 0.184ms/节点，比热趟慢 5 倍**（JIT + 反射首次调用）。
        /// </para>
        /// <para>
        /// 顺带更正一条旧账：原注释按"外部实测 0.23ms/节点"推算"1.5 秒 ≈ 6500 节点"，
        /// **这个数错了 6.6 倍**——实测 0.035ms/节点下 1.5 秒能走约 44000 节点。
        /// 错因是拿 Debug/冷启动的量当常态；真常态是 Release 热趟。
        /// </para>
        /// <para>
        /// 取 3000 的理由：深度 10→15 后节点数会成倍涨，1500ms 会在热趟就撞上限，等于白改。
        /// 3000 + 节点预算 50000 ≈ 1.75 秒的热趟上限，两者互相兜底。
        /// **冷启动首趟仍可能撞时间预算**（实测 0.184ms/节点下 3000ms 只够 16000 节点）——
        /// 这是已知且可接受的：结论会如实报"耗时上限 + 可能不完整"，**再点一次就是热趟**。
        /// UI 冻结最长 3 秒：手动诊断是低频操作，且点之前日志已预告会卡一下。
        /// </para>
        /// 可用单测注入更小的值。</summary>
        public const int MaxElapsedMs = 3000;

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

        // ★第51.7轮 P1：节点级沉默必须记账。51.6 只把"整路异常"这一层收口（failedTrees），
        //   下面一层的结构性 catch{ } 仍旧一条留痕都没有——属性环/子工具集合/停用集合/集合枚举
        //   任一抛异常，那一支就没了，而状态仍是"未截断 + 预算未用尽 + 覆盖走到最后一棵"，
        //   Conclusion() 于是照样输出最硬的那句"未在工具树里找到失效图像引用（说明死引用不在…）"。
        //   这与 51.2（静默 return）、51.6（静默吞单路）是同一类病的第三层。
        /// <summary>结构性下钻中断的次数：一整支子树/属性环因异常没走完（硬口径，否掉"未找到"断言）。</summary>
        public int SkippedBranches { get; private set; }

        /// <summary>逐属性/逐元素读值失败的个数（软口径，只附注不否结论，理由见 <see cref="SkippedBranches"/> 下方的 IsConclusive）。</summary>
        public int SkippedReads { get; private set; }

        /// <summary>走进去但整路遍历异常、被跳过的 job 路数（由调用方报进来；硬口径）。</summary>
        public int SkippedTrees { get; private set; }

        /// <summary>本次根本没有可扫描的工具树（分母为 0，典型场景：方案尚未加载就点了诊断；硬口径）。</summary>
        public bool NothingToScan { get; private set; }

        /// <summary>标记一次结构性中断。同时按既有口径留首个痕迹（首因为准，后续同类不覆盖原因）。</summary>
        public void NoteSkippedBranch(string where)
        {
            SkippedBranches++;
            MarkTruncated("反射下钻中途异常：" + where);
        }

        /// <summary>标记一次"某个属性/集合元素读值失败被跳过"。只影响一条边，故计软口径、不占首因。</summary>
        public void NoteSkippedRead()
        {
            SkippedReads++;
        }

        /// <summary>标记一整路 job 走进去却没走完（异常被调用方捕获，或压根没取到根工具）。
        /// 原因整句由调用方给——"遍历中途异常"和"未取到根工具"是两回事，本方法不能替它选措辞
        ///（★第51.7轮 P3）。首因仍按既有口径只记第一次。</summary>
        public void NoteSkippedTree(string reason)
        {
            SkippedTrees++;
            MarkTruncated(reason);
        }

        /// <summary>标记本次没有任何已挂工具树可扫。此时"没找到"毫无意义——压根没看。</summary>
        public void NoteNothingToScan()
        {
            NothingToScan = true;
            MarkTruncated("没有任何已挂工具树可扫描");
        }

        /// <summary>
        /// 结论能否成立：必须"看过了全部挂树的工具树、且没有任何硬口径的没查全"。
        /// <para>
        /// **软口径 SkippedReads 不在此列，这是明示的取舍不是遗漏**：Cognex 工具上"读某个属性就抛"
        /// 是常态（未运行时的 Result/CurrentImage 一类），把它算成不结论会让这道闸恒为假、
        /// 从此没人再看结论——那样比不设闸更糟。SkippedReads 只在结论后附一行计数，
        /// 让"有 N 条边没读到"这件事仍然可见。
        /// </para>
        /// </summary>
        public bool IsConclusive
        {
            get { return !Truncated && !NothingToScan && SkippedBranches == 0 && SkippedTrees == 0; }
        }

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
            // ★第51.7轮 P3：分母与分子来自对 Myjobs 的**两次**枚举，中间若有并发重绑，
            // 会出现"走到第 5 路 / 挂树 3 路"这种自相矛盾的分数——覆盖口径自己就不可信了，
            // 必须当场说明，不能让读者拿它当进度。
            if (reachedTrees > totalTrees)
                return "挂树 " + totalTrees + " 路中走到第 " + reachedTrees
                    + " 路（枚举两次之间挂树数发生变化，本次覆盖口径不可信）";
            string s = "挂树 " + totalTrees + " 路中走到第 " + reachedTrees + " 路";
            if (reachedTrees <= 0)
                return BudgetExhausted ? s + "（预算已尽，未进入任何一棵）" : s;
            s += "（myjob" + lastMyjob;
            if (BudgetExhausted) s += "，未完";
            return s + "）";
        }

        /// <summary>
        /// 手动诊断入口（★第51.6轮，菜单"已释放图像诊断"）是否该拒绝本次运行。
        /// 返回 null = 放行；否则返回给操作者的拒绝理由（日志与弹窗共用同一句话，避免两处说法不一）。
        /// <para>
        /// 拒绝的理由是**可信度**，不是安全：检测运行中，工具正被 Cognex 执行到一半，此刻反射读到的
        /// 是瞬时状态——扫出来的"没死引用/已死引用"都可能是假的。诊断一旦说谎，代价比没诊断更大
        ///（第47/48轮的老路）。方案切换同理：整棵工具树正在被替换。
        /// </para>
        /// </summary>
        public static string ManualEntryRefusal(bool detectionRunning, bool schemeSwitching,
            bool nothingLoaded, bool formDisposing)
        {
            if (formDisposing) return "窗体正在关闭，本次不执行诊断";
            if (detectionRunning) return "检测正在运行，此刻读到的是瞬时状态、结论不可信：请先停止检测再诊断";
            if (schemeSwitching) return "方案切换进行中，工具树正在被替换：请稍候再试";
            // ★第51.7轮 P2/P3：方案未加载（manager1 为空或 JobCount 0）与启动/切型握手未完成
            //（qiehuanzhong 初值就是 1）都属"此刻没有稳定工具树可扫"。这句与"方案切换进行中"
            // 必须分开：后者是等一会儿就好，前者是**必须先加载方案**，说成一个会把人引向等待。
            if (nothingLoaded) return "还没有已加载的稳定工具树（方案未加载或启动/切型握手未完成）：请先加载方案再诊断";
            return null;
        }

        /// <summary>
        /// 扫描后的状态复查（★第51.7轮 P2-3）。守卫是在**点击那一刻**读的，而诊断在 UI 线程
        /// 最多冻结 1.5 秒；这期间托管在后台线程的启动流程可以把 <c>_jobs.yunxing</c> 翻真
        /// （button1_Click 里的赋值在 Task.Run 内，不经封送，冻结挡不住它），扫完却没人标注。
        /// 返回 null 表示状态没变；否则返回要打进日志的复查说明——不推翻结论，但把它的前提标掉。
        /// </summary>
        public static string MidScanStateChangeNote(bool detectionStartedDuringScan, bool schemeSwitchingStartedDuringScan)
        {
            if (!detectionStartedDuringScan && !schemeSwitchingStartedDuringScan) return null;
            string what = detectionStartedDuringScan && schemeSwitchingStartedDuringScan
                ? "检测启动且方案切换开始"
                : (detectionStartedDuringScan ? "检测已启动" : "方案切换已开始");
            return "扫描期间" + what + "——本次结论按扫描开始时的状态得出，仅供参照";
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

            // ★第51.7轮 P2：分母为 0 时压根没看任何一棵树，"没找到"是空话——单独成一支，
            //   措辞里不得出现"未在工具树里找到"（那是断言，且现有单测正是按这句判"断言支"的）。
            if (NothingToScan)
                return "本次没有可扫描的工具树（挂树数为 0，通常是方案尚未加载或已被释放）"
                    + "——什么都没看到，**不能据此判断有没有死引用**";

            if (Dead.Count > 0)
            {
                return "共 " + Dead.Count + " 处"
                    + (Truncated ? "（截断原因：" + why + "，可能存在更多）" : "")
                    + "，位置如下" + SkipAccount();
            }
            if (!IsConclusive)
            {
                return "未找到失效图像引用，但扫描被截断（" + why + "），结果可能不完整（不等于没有）" + SkipAccount();
            }
            // 走到这里才是真断言：全部挂树走完、无结构性中断、无整路跳过、无截断留痕。
            return "未在工具树里找到失效图像引用（说明死引用不在本程序持有的工具树里，或发生在序列化过程中）"
                + SkipAccount();
        }

        /// <summary>跳过账目（★第51.7轮 P1）：把"哪里没查全"的数量摆在结论末尾，
        /// 让读日志的人不必回头找旁证行就能判断这条结论值多少。全为 0 时输出空串，
        /// 不给干净结论添噪音。</summary>
        private string SkipAccount()
        {
            if (SkippedBranches == 0 && SkippedTrees == 0 && SkippedReads == 0) return "";
            string s = "（跳过账目：";
            s += "结构性中断 " + SkippedBranches + " 处";
            s += "、整路跳过 " + SkippedTrees + " 路";
            s += "、属性/元素读值失败 " + SkippedReads + " 处";
            return s + "）";
        }
    }
}
