using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace WindowsFormsApplication1.Core.Logging
{
    /// <summary>
    /// 限流日志器（P3-2）：专门治理"高频循环里的 catch 疯狂刷日志"。
    ///
    /// 背景：轮询/收帧等热路径每 20~500ms 跑一圈，一旦 PLC/串口断线，外层 catch 每圈触发一次，
    /// 数秒内产生上百条几乎相同的日志，既污染日志文件、又掩盖真正的第一条。
    ///
    /// 设计（冷却窗口 + 计数，业界常见 leaky bucket）：
    ///   · 同一 <paramref name="tag"/> 在 <paramref name="windowMs"/> 冷却期内<b>只写第一条</b>（带完整异常），
    ///     其后窗口内的重复全部静默计数；窗口到期时<b>补报一条累计次数</b>。
    ///   · 这样"高频重复异常"在日志里是<b>每窗口一行 + 累计计数</b>，辨识度高、绝不刷屏。
    ///
    /// 辨识 <paramref name="tag"/>：调用方传"协议+连接号+方法名"，例如
    /// <c>"[ModbusTCP-连接1-Fins_duxie] ReadInt16"</c>，日志一眼看出出处。
    ///
    /// 落点解耦：本类不直接持有日志器，通过 <paramref name="write"/> 委托把拼好的文本交给
    /// 调用方决定写到哪里（ErrorLog / LoggingService / LogManager.Default），避免耦合两套日志体系。
    /// 仅记录到诊断日志级别，不影响业务；线程安全（每 tag 一把锁 + 全局 ConcurrentDictionary）。
    /// </summary>
    public static class RateLimitedLog
    {
        private sealed class State
        {
            /// <summary>上一次真正落日志的时间戳（Stopwatch.GetTimestamp，单调无回绕，.NET 4.6 可用）。</summary>
            public long Last;
            /// <summary>冷却窗口内被静默计数的次数（不含已报首条）。</summary>
            public int Suppressed;
        }

        /// <summary>Stopwatch 频率（用于把 timestamp 换算成毫秒）。</summary>
        private static readonly long TicksPerMs = Math.Max(1L, Stopwatch.Frequency / 1000);

        private static readonly ConcurrentDictionary<string, State> _states =
            new ConcurrentDictionary<string, State>(StringComparer.Ordinal);

        /// <summary>
        /// 约束：调用方传入的 <c>tag</c> 必须是<b>静态/有限集合</b>（如"协议+连接号+方法名"），
        /// 不得把高频变化的值（帧号、时间戳、随机 ID）拼进 tag，否则本字典随调用无界增长、内存泄漏。
        /// 现有调用方（三窗体轮询 catch、CameraWorkQueue）的 tag 均随连接数/相机数固定，符合此约束。
        /// </summary>
        internal static int StateCount => _states.Count;   // 供诊断/测试观测，避免误用

        /// <summary>
        /// 高频 catch 里调用。冷却窗口内：首条完整写出、后续静默计数；窗口到期补报累计。
        /// </summary>
        /// <param name="tag">辨识标签（协议+连接号+方法名）。</param>
        /// <param name="write">真正落日志的委托（决定写到哪个日志文件/级别）。拼好的文本作为唯一参数。</param>
        /// <param name="ex">当前异常（可空；空则按"未捕获异常消息"记）。</param>
        /// <param name="windowMs">冷却窗口（毫秒）。默认 5000ms = 每 5 秒最多一行。</param>
        /// <returns>true=本次实际写出了日志（首条或窗口到期补报）；false=窗口内被静默限流。</returns>
        public static bool Throttled(string tag, Action<string> write, Exception ex = null, int windowMs = 5000)
        {
            if (string.IsNullOrEmpty(tag)) tag = "(未标注)";
            if (write == null) return false;
            var st = _states.GetOrAdd(tag, _ => new State());
            lock (st)
            {
                st.Suppressed++;
                long now = Stopwatch.GetTimestamp();
                // 换算成毫秒：与初始 Last=0（即 Stopwatch 起点）同基准
                if ((now - st.Last) / TicksPerMs >= windowMs)
                {
                    st.Last = now;
                    int n = st.Suppressed;
                    st.Suppressed = 0;
                    try
                    {
                        // 窗口到期补报：既报本次异常，又报窗口内被静默的次数
                        write(string.Format("[限流日志] {0}：{1}（窗口 {2}ms 内累计 {3} 次，仅报其一）",
                            tag, Describe(ex), windowMs, n));
                    }
                    catch
                    {
                        // 写日志本身失败不能再抛，否则污染热路径
                    }
                    return true;
                }
            }
            return false; // 冷却窗口内，静默计数，不刷屏
        }

        /// <summary>
        /// 非异常的限流记录（如"队列满丢弃""重试"等正常路径告警）：文案不带"异常"字样，避免现场误判。
        /// 冷却语义与 <see cref="Throttled"/> 一致；窗口到期补报累计次数。
        /// </summary>
        public static bool ThrottledMessage(string tag, Action<string> write, string message, int windowMs = 5000)
        {
            if (string.IsNullOrEmpty(tag)) tag = "(未标注)";
            if (write == null) return false;
            if (string.IsNullOrEmpty(message)) message = "(无描述)";
            var st = _states.GetOrAdd(tag, _ => new State());
            lock (st)
            {
                st.Suppressed++;
                long now = Stopwatch.GetTimestamp();
                if ((now - st.Last) / TicksPerMs >= windowMs)
                {
                    st.Last = now;
                    int n = st.Suppressed;
                    st.Suppressed = 0;
                    try
                    {
                        write(string.Format("[限流日志] {0}：{1}（窗口 {2}ms 内累计 {3} 次，仅报其一）",
                            tag, message, windowMs, n));
                    }
                    catch
                    {
                        // 写日志本身失败不能再抛，否则污染热路径
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>生成辨识性描述（异常类型 + 消息；异常为空时用占位说明）。</summary>
        private static string Describe(Exception ex)
        {
            if (ex == null) return "异常（无堆栈/消息）";
            string msg = ex.Message;
            if (string.IsNullOrEmpty(msg)) msg = "(空消息)";
            if (msg.Length > 200) msg = msg.Substring(0, 200) + "…";
            return ex.GetType().Name + ": " + msg;
        }

        /// <summary>清空某标签的限流状态（如切换连接/重启轮询时），使下一条不被旧窗口抑制。</summary>
        public static void Reset(string tag)
        {
            State st;
            if (_states.TryRemove(tag, out st))
                lock (st) { st.Suppressed = 0; st.Last = 0; }
        }

        /// <summary>清空全部限流状态（程序级重置）。</summary>
        public static void ResetAll() { _states.Clear(); }
    }
}
