using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 死引用诊断的一次扫描状态（★第51.1轮自 Form1.cs 抽出；第51.2轮补截断留痕）。
    /// 抽出的三个目的：
    /// 1) 修 BUG：原实现在 Form1.cs 里用 **static int** 计数且从不复位——第二次诊断一进函数
    ///    就被 2 万节点预算耗尽挡住，于是照样打出"未找到失效图像引用"。诊断谎报"没问题"
    ///    比没有诊断更糟：它会让人基于错误信息继续猜（这正是第47/48轮反复修错的模式）。
    ///    改成"每次新建一个实例"，预算天然从 0 开始，两个扫描实例互不干扰。
    /// 2) ★第51.2轮（P2）：截断**必须留痕**。仅"节点预算"会标记 Truncated 是不够的——
    ///    下钻深度到顶、集合元素超限这两处直接 return/break 不标记，被挡在门外的那部分
    ///    照样会输出"未在工具树里找到失效图像引用"，与第1条是同一类谎报。现在所有截断点
    ///    统一调 MarkTruncated(reason)，结论里带上原因。
    /// 3) 让"预算/截断/去重/结论口径"这类纯逻辑可被单元测试覆盖——它不依赖 Cognex，
    ///    因此可以按本工程既有的"源码链接"方式（见 WindowsFormsApplication1.Tests.csproj）
    ///    直接链进测试程序集，测试零第三方依赖、CI 可跑。
    /// 与 Form1.cs 的分工：本类只管"扫描到哪了、报什么结论"，不碰任何 Cognex 类型。
    /// </summary>
    internal sealed class DeadImageScan
    {
        /// <summary>下钻深度上限。★第51.2轮由 6 提到 10：
        /// 现场加载路径是 group → 外层 CogToolBlock → 内层 CogToolBlock → 工具，光两层块嵌套就吃掉 2 层，
        /// 再加 Operator/Items[]/Pattern 才够到 TrainImage——6 层正卡在边沿。既然截断现在会如实报
        /// （见 MarkTruncated），就必须真能走到底，否则日志永远是"可能不完整"。代价由 MaxNodes 兜底。</summary>
        public const int MaxDepth = 10;

        /// <summary>单次扫描的节点预算：防反射遍历在超大工具树上把保存失败后的 UI 线程拖死。</summary>
        public const int MaxNodes = 20000;

        /// <summary>报告条数上限：日志只用来定位，几十条足够，封顶防止刷爆日志文件。</summary>
        public const int MaxReport = 60;

        /// <summary>已确认失效的图像引用路径。</summary>
        public readonly List<string> Dead = new List<string>();

        /// <summary>已访问实例的参考标识（防环 + 防同一实例多处引用重复下钻）。</summary>
        public readonly HashSet<int> Seen = new HashSet<int>();

        /// <summary>本次扫描已访问的节点数。每次新建实例即从 0 开始（见类注释第 1 条）。</summary>
        public int Nodes { get; private set; }

        /// <summary>扫描是否被截断。为 true 时结果**不完整**，"没找到"不能当结论用。</summary>
        public bool Truncated { get; private set; }

        /// <summary>截断原因（首个原因为准），随结论一起打进日志。</summary>
        public string TruncatedReason { get; private set; }

        /// <summary>标记"扫描被截断"及原因。只记第一次的原因——先撞到的才是把扫描挡住的那个。</summary>
        public void MarkTruncated(string reason)
        {
            if (Truncated) return;
            Truncated = true;
            TruncatedReason = string.IsNullOrEmpty(reason) ? "扫描上限" : reason;
        }

        /// <summary>
        /// 计一个节点。超出节点预算、或已达报告上限，则标记截断并返回 false——调用方据此停止本支。
        /// </summary>
        public bool CountNode()
        {
            if (Truncated) return false;
            if (++Nodes > MaxNodes) { MarkTruncated("节点预算 " + MaxNodes + " 已用尽"); return false; }
            if (Dead.Count >= MaxReport) { MarkTruncated("报告条数已达 " + MaxReport); return false; }
            return true;
        }

        /// <summary>记录一条死引用路径（达报告上限后丢弃，由调用方/CountNode 标记截断）。</summary>
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
        /// 把扫描结果翻译成给日志的结论。**必须区分"没查完"与"真没有"**：
        /// 截断时如实说原因与可能漏报，否则"预算耗尽/深度到顶"会被读成"树里没有死引用"。
        /// </summary>
        public string Conclusion()
        {
            string why = string.IsNullOrEmpty(TruncatedReason) ? "扫描上限" : TruncatedReason;
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
