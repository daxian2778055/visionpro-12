using System;
using System.Collections.Concurrent;
using System.Threading;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1.Core.Threading
{
    /// <summary>
    /// 每路相机的有界任务队列（P2-1 热路径 Task.Run 收敛）。
    ///
    /// 背景：getrecord 检测主循环每帧对每路相机创建多个 Task.Run（IO 脉冲 / PLC 回写），
    /// 高帧率 + 12 路同时检测时线程池任务堆积、慢 PLC 拖慢整条线程池。
    ///
    /// 设计：每路一个常驻后台线程 + 有界 BlockingCollection（默认容量 4）。
    ///   · 同一路的任务按 FIFO 顺序执行（慢 PLC 只拖自己那路，不影响其他路）。
    ///   · 满时丢最旧（FIFO 语义保留；IO/PLC 脉冲可漏一帧，但<b>会记限流日志</b>便于现场排查）。
    ///   · <b>仅适合"可丢弃"的 IO 脉冲 / PLC 回写</b>；关键计数（合格/不合格统计）不可入队，
    ///     应在检测线程同步执行，否则会因队列满被丢导致漏计（见 Form1.getrecord）。
    /// </summary>
    public sealed class CameraWorkQueue : IDisposable
    {
        private readonly BlockingCollection<Action> _queue;
        private readonly Thread _worker;
        private readonly string _name;
        private readonly Action<string> _logSink;
        private volatile bool _disposed;
        // ★D：入队失败标志与 _disposed 分离。只有 Dispose() 才置 _disposed；
        // 若 Enqueue 内部异常（理论罕见）只置 _enqueueFailed，避免绕过 Dispose 的 CompleteAdding/Join 导致 worker 线程不回收。
        private volatile bool _enqueueFailed;

        public CameraWorkQueue(string name, int capacity = 4, Action<string> logSink = null)
        {
            _name = name;
            _logSink = logSink;
            _queue = new BlockingCollection<Action>(new ConcurrentQueue<Action>(), Math.Max(1, capacity));
            _worker = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = "CamWork-" + name
            };
            _worker.Start();
        }

        /// <summary>入队。队列满时丢最旧再入队（不阻塞调用线程）；已释放则直接忽略（不抛异常）。</summary>
        public void Enqueue(Action work)
        {
            if (work == null || _disposed || _enqueueFailed) return;
            try
            {
                if (_queue.TryAdd(work)) return;
                // ★C：满 → 丢最旧。会静默丢任务（IO 脉冲可漏一帧），但必须留下痕迹供现场排查。
                Action oldest;
                if (_queue.TryTake(out oldest, 0))
                {
                    RateLimitedLog.ThrottledMessage("[CamWork-" + _name + "] 队列满，丢最旧(含本次入队前 1 条)IO/PLC 任务",
                        _logSink, "IO/PLC 任务因队列满载被丢弃", 5000);
                    _queue.TryAdd(work);
                }
            }
            catch (InvalidOperationException)
            {
                // ★fix②：CompleteAdding() 后 TryAdd 抛此异常；视为已释放。
                // ★D：仅置 _enqueueFailed（不复用 _disposed），保证 Dispose() 仍会 CompleteAdding+Join 回收 worker。
                _enqueueFailed = true;
            }
            catch
            {
                _enqueueFailed = true;
            }
        }

        private void WorkerLoop()
        {
            try
            {
                foreach (var work in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        work();
                    }
                    catch (Exception ex)
                    {
                        // 任务失败不留死角：限流日志（tag 含队列名=相机路号），5s 窗口内只报首条+累计，不刷屏。
                        RateLimitedLog.Throttled("[CamWork-" + _name + "] 任务失败", _logSink, ex, 5000);
                    }
                }
            }
            catch (Exception)
            {
                // GetConsumingEnumerable 在 CompleteAdding 后正常退出；忽略
            }
        }

        /// <summary>释放：停止入队、等 worker 排空（最多 2s）。幂等。</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _enqueueFailed = true;   // 之后 Enqueue 一律忽略
            try { _queue.CompleteAdding(); } catch { }
            try { if (_worker.IsAlive) _worker.Join(2000); } catch { }
        }
    }
}
