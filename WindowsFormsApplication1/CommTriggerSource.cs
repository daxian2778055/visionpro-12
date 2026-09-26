namespace WindowsFormsApplication1
{
    /// <summary>
    /// BUG A 修复：通讯触发来源快照（不可变）。一次检测只对应一个来源，
    /// 引用赋值是原子的，保证 Proto + LinkId 永远成对一致（原两个独立 volatile int 存在跨协议交错风险）。
    /// ★第43轮：自 Myjob.cs 抽出为独立文件——PendingCameraTriggers（通讯触发队列）引用本类型，
    ///   而 Myjob.cs 依赖 Cognex VisionPro（测试工程在 CI 上无法源码链接），抽出后测试可独立编译。
    ///   纯代码搬移：命名空间 / 可见性 / 字段 / 构造完全不变，全量重编译验证，行为零变化。
    /// </summary>
    public sealed class CommTriggerSource
    {
        public readonly int LinkId;
        public readonly int Proto;
        public readonly string Received;
        public CommTriggerSource(int linkId, int proto, string received)
        {
            LinkId = linkId;
            Proto = proto;
            Received = received;
        }
    }
}
