using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 死引用诊断的一次扫描状态（★第51.1轮自 Form1.cs 抽出）。
    /// 抽出有两个目的：
    /// 1) 修 BUG：原实现在 Form1.cs 里用 **static int** 计数且从不复位——第二次诊断一进函数
    ///    就被 2 万节点预算耗尽挡住，于是照样打出"未找到失效图像引用"。诊断谎报"没问题"
    ///    比没有诊断更糟：它会让人基于错误信息继续猜（这正是第47/48轮反复修错的模式）。
    ///    改成"每次新建一个实例"，预算天然从 0 开始，两个扫描实例互不干扰。
    /// 2) 让"预算耗尽/报告封顶"这类纯逻辑可被单元测试覆盖——它不依赖 Cognex，
    ///    因此可以按本工程既有的"源码链接"方式（见 WindowsFormsApplication1.Tests.csproj）
    ///    直接链进测试程序集，测试零第三方依赖、CI 可跑。
    /// 与 Form1.cs 的分工：本类只管"扫描到哪了、报什么结论"，不碰任何 Cognex 类型。
    /// </summary>
    internal sealed class DeadImageScan
    {
        /// <summary>下钻深度上限：够到 Operator/Items[]/Pattern/TrainImage 一层即可，再深是自找麻烦。</summary>
        public const int MaxDepth = 6;

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

        /// <summary>扫描是否被上限截断。为 true 时结果**不完整**，"没找到"不能当结论用。</summary>
        public bool Truncated { get; private set; }

        /// <summary>
        /// 计一个节点。超出节点预算、或已达报告上限，则标记截断并返回 false——调用方据此停止本支。
        /// </summary>
        public bool CountNode()
        {
            if (Truncated) return false;
            if (++Nodes > MaxNodes || Dead.Count >= MaxReport)
            {
                Truncated = true;
                return false;
            }
            return true;
        }

        /// <summary>记录一条死引用路径（达报告上限后丢弃，但 Truncated 已由 CountNode 标记）。</summary>
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
        /// 截断时如实说可能漏报，否则"预算耗尽"会被读成"树里没有死引用"。
        /// </summary>
        public string Conclusion()
        {
            if (Dead.Count > 0)
            {
                return "共 " + Dead.Count + " 处" + (Truncated ? "（已达扫描上限，可能存在更多）" : "") + "，位置如下";
            }
            if (Truncated)
            {
                return "未找到失效图像引用，但扫描已达节点上限、结果可能不完整（不等于没有）";
            }
            return "未在工具树里找到失效图像引用（说明死引用不在本程序持有的工具树里，或发生在序列化过程中）";
        }
    }
}
