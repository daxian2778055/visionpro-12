using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 显示渲染预算的**按需求解分摊**调度器（纯 BCL，不依赖 Cognex，源码链接进测试工程）。
    /// 只做一件事：决定"这一帧显不显示"。**绝不参与检测本身**——被拒的帧照常完成 block.Run、
    /// 判定、计数、存图、PLC 反馈，一路计数不受本类影响。
    ///
    /// 要解决的问题（第54轮，现场反馈 + 模拟复现）：旧口径是"两次渲染启动之间不足
    /// RenderMinIntervalMs 就拒绝，被拒路累计满 40 次放行一次"。两处硬伤：
    ///   1) 判据用的是一个**全局共享**的"上次启动时刻"⇒ 先到先赢会锁相，与赢家同相的路**每一帧都被拒**；
    ///   2) 逃生阀计的是**次数**不是**时间**⇒ 3fps 的路要等满 40 个自己的帧（13 秒）才轮得到一次。
    /// 模拟实测（10 秒、上限 62.5 次/秒）旧法：12 路×30fps ⇒ 一路 99%、其余 11 路 2%；
    /// 12 路×3fps（总需求 36/s，**预算根本没用完**）⇒ 一路 96%、**11 路 0%**（彻底不刷新）。
    /// 操作员看到的就是"总是盯着某一个相机刷新，别的相机一卡一卡"。
    ///
    /// 为什么不是简单均分（现场反馈的关键修正）：各路本来就快慢不同（连续采集 30fps、
    /// PLC/软触发 1~3fps）。均分给每路 1/路数 的份额，会让快路被压到 5fps 而慢路根本用不完
    /// 自己的份额——预算闲置、快路反而变卡。实测：2 路 30fps + 10 路 3fps 时
    /// 均分只用了 41.6/s 且快路显示率 17%；按需求解用到 62.4/s 且快路 51%，慢路两版都是 100%。
    ///
    /// 本类的口径（max-min fair share，"低频路全显、余量给高频路"）：
    ///   · 每路实测帧周期（到达间隔的指数滑动平均）⇒ 各路需求 d_i（毫帧/秒）；
    ///   · 解一个份额 x，使 Σ min(d_i, x) = 全局上限（二分，无分配、无浮点）；
    ///     需求低于 x 的路 = "喂饱"，应得间隔取自己的周期（**一帧不丢**）；
    ///     需求高于 x 的路 = "贪心"，应得间隔 = 1000/x，即闲置份额全部分给它们；
    ///   · 每路一个"下次应渲染时刻"（DRR 到期表）按应得间隔顺延，同路并发用 CAS 只有一个赢家。
    /// 到期时刻从**应得时刻**起算而不是从当前时刻起算：被挤掉的那一帧不白丢，下一帧仍落在自己
    /// 时隙里，长期速率才严格等于应得速率（按当前时刻起算会把整段间隔白送对方，正是旧法偏心的根源）。
    ///
    /// 上限不变：各路放行间隔 ≥ 应得间隔，Σ 应得速率 ≡ 1000/RenderMinIntervalMs，
    /// 即总渲染量与旧口径同一个封顶，**一点没多给**（实测各场景总量 61.8~62.4/s ≤ 62.5/s）。
    /// 需求估计收敛期内（约 8 帧）可能有轻微过冲，之后回到封顶；code.ini 的
    /// [Display]RenderMinIntervalMs 仍是唯一的 CPU 旋钮，调大它就等比例降总量。
    ///
    /// 线程约定：检测线程并发调用，全 Volatile/Interlocked，无锁、无分配、不阻塞。
    /// now 一律是 Environment.TickCount（int 毫秒），差值全部走 unchecked 以自洽回绕；
    /// "首帧必放行"由每路的到达过/渲染过标志保证，不依赖任何哨兵时刻值（哨兵在环形坐标上
    /// 必有半周期落在"未来"，机器连续开机 18.7~43.5 天这段启动会让所有路永不刷新）。
    ///
    /// 自身开销实测（本机 csc 优化编译，8 路 30fps + 4 路 3fps 跑满 60 秒、15276 次裁决）：
    /// 累计 36.4ms ⇒ 单次约 2.4 微秒、占单核 0.06%。相对一次 VisionPro record 深拷贝可忽略，
    /// 且它跑在检测线程上也不阻塞任何锁。
    /// </summary>
    internal sealed class RenderBudget
    {
        public const int Slots = 12;

        /// <summary>最近这么久之内请求过渲染的路才算"活跃"（参与需求普查）；停流的路 2 秒后自动让出份额。</summary>
        public const int ActivityWindowMs = 2000;

        /// <summary>速率单位：毫帧/秒（1000 = 1fps），全程整数运算避免浮点与非原子双字写。</summary>
        private const int MilliPerFps = 1000;

        /// <summary>尚无周期样本的路按此需求记账（视作贪心路）。高估贪心路不影响他人：份额只由喂饱路的需求与贪心路条数决定。</summary>
        private const int UnknownDemandMilliFps = 100 * MilliPerFps;

        private readonly int[] _dueTick = new int[Slots];      // 每路下次应渲染时刻（仅 _granted=1 时有意义）
        private readonly int[] _granted = new int[Slots];      // 该路渲染过一次没有（首帧必放行的依据）
        private readonly int[] _requestTick = new int[Slots];  // 每路最近一次请求时刻（活跃普查，仅 _arrived=1 时有意义）
        private readonly int[] _arrived = new int[Slots];      // 该路到达过一次没有
        private readonly int[] _lastArriveTick = new int[Slots]; // 每路上一次到达时刻（测周期用）
        private readonly int[] _periodMs = new int[Slots];     // 每路实测帧周期（0=尚无样本）

        // 为什么用"到达过/渲染过"标志而不是某个哨兵时刻值：Environment.TickCount 是回绕的 int，
        // 任何固定常量在半个周期里都位于"未来"——机器连续开机约 18.7~43.5 天这段启动的话，
        // 用 MinValue/4 当"很久以前"会让每路首帧都被判成"还没到期"且永不解锁。
        // 标志位不参与环形比较，回绕前后一律自洽；_dueTick/_requestTick 只在标志置位后才被读取。

        /// <summary>活跃路数（普查窗口内请求过的路）——只作诊断展示，分摊分母由需求解决定。</summary>
        public int ActivePaths(int now)
        {
            int n = 0;
            for (int i = 0; i < Slots; i++)
            {
                if (Volatile.Read(ref _arrived[i]) == 0) continue;
                if (unchecked(now - Volatile.Read(ref _requestTick[i])) <= ActivityWindowMs) n++;
            }
            return n;
        }

        /// <summary>本路当前需求（毫帧/秒）；从未到达或停流超过普查窗口的路为 0（不占份额）；尚无周期样本按贪心记账。</summary>
        private int DemandMilliFps(int slot, int now)
        {
            if (Volatile.Read(ref _arrived[slot]) == 0) return 0;
            if (unchecked(now - Volatile.Read(ref _requestTick[slot])) > ActivityWindowMs) return 0;
            int p = Volatile.Read(ref _periodMs[slot]);
            return p <= 0 ? UnknownDemandMilliFps : MilliPerFps * 1000 / p;
        }

        /// <summary>全体需求按 cap 截断后求和（单调递增于 cap，用于二分求份额）。</summary>
        private int CappedDemandMilliFps(int capMilliFps, int now)
        {
            int sum = 0;
            for (int i = 0; i < Slots; i++)
            {
                int d = DemandMilliFps(i, now);
                sum += d < capMilliFps ? d : capMilliFps;
            }
            return sum;
        }

        /// <summary>
        /// 解出贪心路的公共份额（毫帧/秒）：最小的 x 使 Σ min(d_i, x) ≥ 全局上限。
        /// 需求总额本就低于上限时返回上限本身（此时所有路都被喂饱，谁也不限）。
        /// </summary>
        public int ShareMilliFps(int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return int.MaxValue;
            int cap = MilliPerFps * 1000 / minIntervalMs;
            int lo = 0, hi = cap;
            while (hi - lo > 1)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (CappedDemandMilliFps(mid, now) >= cap) hi = mid;
                else lo = mid;
            }
            return hi;
        }

        /// <summary>
        /// 本路应得的渲染间隔（ms）。喂饱路取自己周期打 9 折（留抖动余量，早到的帧不该被拒），
        /// 贪心路取 1000/份额；两者都不小于全局最小间隔。minIntervalMs&lt;=0 时返回 0（不限速）。
        /// </summary>
        public int EntitledMs(int slot, int now, int minIntervalMs)
        {
            if (minIntervalMs <= 0) return 0;
            if (slot < 0 || slot >= Slots) return minIntervalMs;
            int share = ShareMilliFps(now, minIntervalMs);
            int own = DemandMilliFps(slot, now);
            if (own > 0 && own <= share)
            {
                int p = Volatile.Read(ref _periodMs[slot]);
                int fair = p <= 0 ? minIntervalMs : p - p / 10;
                return fair < minIntervalMs ? minIntervalMs : fair;
            }
            int greedy = (MilliPerFps * 1000 + share - 1) / share;
            return greedy < minIntervalMs ? minIntervalMs : greedy;
        }

        /// <summary>登记一次帧到达（测周期）。该路首次到达不取样本（没有可比的前次时刻）；间隔非正/超普查窗口也不取。</summary>
        private void NoteArrival(int slot, int now)
        {
            int prevArrive = Volatile.Read(ref _lastArriveTick[slot]);
            bool firstEver = Volatile.Read(ref _arrived[slot]) == 0;
            Volatile.Write(ref _lastArriveTick[slot], now);
            Volatile.Write(ref _requestTick[slot], now);
            Volatile.Write(ref _arrived[slot], 1);
            if (firstEver) return;
            int gap = unchecked(now - prevArrive);
            if (gap <= 0 || gap > ActivityWindowMs) return;
            int p = Volatile.Read(ref _periodMs[slot]);
            Volatile.Write(ref _periodMs[slot], p <= 0 ? gap : (p * 3 + gap) / 4);
        }

        /// <summary>
        /// 申请渲染本帧。true=可以渲染（调用方负责后续投递）；false=本帧不显示（**不影响检测与计数**）。
        /// 同一路并发申请时只有一个赢家（CAS 到期表），输者按"未到期"处理，下一帧仍在自己时隙内。
        /// </summary>
        public bool TryClaim(int slot, int now, int minIntervalMs)
        {
            if (slot < 0 || slot >= Slots) return false;   // 槽位非法：不记账、不放行
            NoteArrival(slot, now);
            if (minIntervalMs <= 0) return true;           // 不限速（交互期间或配置为 0）

            int fair = EntitledMs(slot, now, minIntervalMs);
            int due = Volatile.Read(ref _dueTick[slot]);
            bool armed = Volatile.Read(ref _granted[slot]) != 0;
            if (armed && unchecked(now - due) < 0) return false;   // 还没轮到自己的时隙

            // 已渲染过的路从"应得时刻"顺延，而不是从"当前时刻"起算：被挤掉的那一帧不白丢，
            // 下一帧仍落在自己时隙里，长期速率才严格等于应得速率（按当前时刻起算会把整段间隔
            // 白送对方，正是旧法偏心的根源）。首帧没有时隙可言，从当前时刻起算。
            // 再加 slot 毫秒的固定错峰，避免各路到期时刻随时间收敛到同一毫秒（同瞬齐发会一次打满 UI 线程）。
            int next = unchecked((armed ? due : now) + fair + slot);
            if (unchecked(next - now) <= 0) next = unchecked(now + fair + slot);
            if (Interlocked.CompareExchange(ref _dueTick[slot], next, due) != due) return false;
            Volatile.Write(ref _granted[slot], 1);
            return true;
        }
    }
}
