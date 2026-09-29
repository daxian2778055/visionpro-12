using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 单次耗时实测累加器（★第56轮 P0：先量再改）。纯 BCL、无锁、无分配，可在检测线程与 UI 线程并发调用。
    ///
    /// 为什么要有它：本轮要回答的现场问题是"限频裁决本身会不会反过来加重负荷"和
    /// "一次真渲染到底有多贵——把 80Hz 降到 40Hz 省下的 CPU 是不是真金白银"。前者要量**裁决自身**
    /// 的耗时（微秒级才有资格每帧都问），后者要量**一次真渲染**的耗时
    /// （VisionPro record 光栅化 vs 原图贴 PictureBox 差一个量级）。
    /// 这两个数字之前只有离线模拟值，没有台架实测值，凭它拍参数就是拍脑袋。
    ///
    /// 口径：滑动窗口默认 1000ms，窗口内累计次数/总耗时/峰值；换窗时把窗口值搬到"上一窗"，
    /// 读侧拿到的永远是"上一个完整窗口"的统计——避免读到正在累加的半窗而低估。
    /// 换窗与计数之间的偶发竞争只会让样本数差 1，不参与任何裁决，故不加锁。
    /// </summary>
    internal sealed class RenderCostMeter
    {
        private readonly long _frequencyHz;
        private readonly int _windowMs;

        private int _windowStartTick;      // 当前窗口起点（Environment.TickCount）；0 兼作"未起始"哨兵：
                                           // now 恰好取到 0 的那一笔（开机、以及约 24.9 天回绕一次）
                                           // 会把起点顺延到下一笔 ⇒ 最多让某一窗对齐偏一个窗长。
                                           // 只影响诊断读数、不参与裁决（RenderCostMeterTests.
                                           // 零点时刻与未起始哨兵同值_窗口起点由第一笔非零时刻钉住 钉住该行为）
        private int _windowCount;          // 当前窗口样本数
        private long _windowTicks;         // 当前窗口累计耗时（Stopwatch tick）
        private long _windowPeakTicks;     // 当前窗口峰值
        private int _prevCount;            // 上一窗口样本数
        private long _prevTicks;           // 上一窗口累计耗时
        private long _prevPeakTicks;       // 上一窗口峰值
        private long _totalTicks;          // 进程启动以来累计耗时（峰值/均值都用窗口值，累计只作总账）
        private int _totalCount;

        public RenderCostMeter(int windowMs)
        {
            _windowMs = windowMs <= 0 ? 1000 : windowMs;
            _frequencyHz = System.Diagnostics.Stopwatch.Frequency;
            if (_frequencyHz <= 0) _frequencyHz = 10_000_000L;   // 理论不可能；防御除零
        }

        /// <summary>登记一次耗时（由调用方用 Stopwatch.GetTimestamp 差值给出，单位 tick）。</summary>
        public void Record(long elapsedTicks, int now)
        {
            if (elapsedTicks < 0) return;
            Interlocked.Add(ref _totalTicks, elapsedTicks);
            Interlocked.Increment(ref _totalCount);
            if (Volatile.Read(ref _windowStartTick) == 0)
            {
                Volatile.Write(ref _windowStartTick, now);
            }
            else if (unchecked(now - Volatile.Read(ref _windowStartTick)) >= _windowMs)
            {
                // 换窗：先把正在累加的窗快照成"上一窗"，再开新窗。
                // 读改写之间可能被并发插入样本，诊断量允许这 1 个样本的误差。
                Interlocked.Exchange(ref _prevCount, Volatile.Read(ref _windowCount));
                Interlocked.Exchange(ref _prevTicks, Volatile.Read(ref _windowTicks));
                Interlocked.Exchange(ref _prevPeakTicks, Volatile.Read(ref _windowPeakTicks));
                Volatile.Write(ref _windowCount, 0);
                Volatile.Write(ref _windowTicks, 0L);
                Volatile.Write(ref _windowPeakTicks, 0L);
                Volatile.Write(ref _windowStartTick, now);
            }
            Interlocked.Add(ref _windowTicks, elapsedTicks);
            Interlocked.Increment(ref _windowCount);
            long peak = Volatile.Read(ref _windowPeakTicks);
            while (elapsedTicks > peak)
            {
                long before = Interlocked.CompareExchange(ref _windowPeakTicks, elapsedTicks, peak);
                if (before == peak) break;
                peak = before;
            }
        }

        /// <summary>上一完整窗口的样本数（≈次数/秒，窗口默认 1s）。</summary>
        public int SamplesPerWindow()
        {
            return Volatile.Read(ref _prevCount);
        }

        /// <summary>上一完整窗口的平均耗时（微秒）。没有样本时返回 0（明确区分"没测到"与"很快"）。</summary>
        public int AvgMicroseconds()
        {
            int n = Volatile.Read(ref _prevCount);
            if (n <= 0) return 0;
            return TicksToMicroseconds(Volatile.Read(ref _prevTicks) / n);
        }

        /// <summary>上一完整窗口的峰值耗时（微秒）。</summary>
        public int PeakMicroseconds()
        {
            return TicksToMicroseconds(Volatile.Read(ref _prevPeakTicks));
        }

        /// <summary>进程启动以来的平均耗时（微秒）——窗口还没换过第一次时用这个兜底，避免报 0。</summary>
        public int OverallAvgMicroseconds()
        {
            int n = Volatile.Read(ref _totalCount);
            if (n <= 0) return 0;
            return TicksToMicroseconds(Volatile.Read(ref _totalTicks) / n);
        }

        /// <summary>进程启动以来的总次数（只作总账，不当速率读）。</summary>
        public int TotalSamples()
        {
            return Volatile.Read(ref _totalCount);
        }

        private int TicksToMicroseconds(long ticks)
        {
            if (ticks <= 0) return 0;
            // 1 tick = 10^7 Hz / frequency 秒；换算成微秒后可能溢出 int，饱和到 int.MaxValue。
            long micros = ticks * 1_000_000L / _frequencyHz;
            return micros > int.MaxValue ? int.MaxValue : (int)micros;
        }
    }
}
