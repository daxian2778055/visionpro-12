using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace WindowsFormsApplication1.Core.Logging
{
    /// <summary>
    /// 异步文件日志：调用线程只负责入队，后台线程批量落盘。
    /// <para>
    /// 相比旧的 <c>ErrorLog</c>（每条日志 lock + CreateDirectory + 打开文件 + 4 次 WriteLine + Flush），
    /// 本实现消除了检测线程上的同步磁盘 IO，12 路相机并发时不再因全局锁互相等待。
    /// </para>
    /// </summary>
    /// <remarks>
    /// 设计要点：
    /// 1) 有界队列 + 丢弃计数：磁盘抖动或日志风暴时宁可丢日志，也绝不拖慢检测线程或撑爆内存；
    /// 2) 文件句柄保持打开，按 flushInterval 批量 Flush，避免每条日志一次打开/关闭；
    /// 3) 按天分文件，超过 maxFileSizeBytes 时滚动归档（.1.txt ~ .9.txt），归档规则与旧实现一致；
    /// 4) WriteCritical 同步直写，供崩溃/退出路径使用；
    /// 5) 输出文本格式与旧 ErrorLog 完全一致，ReadErrorLog 窗体无需改动。
    /// </remarks>
    public sealed class AsyncFileLogger : ILogger, IDisposable
    {
        private sealed class LogEntry
        {
            public string Category;
            public string Message;
            public DateTime Time;

            /// <summary>非空表示为 Flush 哨兵：处理到它时先落盘，再置位该事件。</summary>
            public ManualResetEventSlim FlushSignal;
        }

        private const int MaxArchivePerDay = 9;
        private const int HistoryRetentionDays = 30;   // ★日志按天分文件但从不删除历史——长期运行目录无限增长，保留最近 30 天

        private readonly BlockingCollection<LogEntry> _queue;
        private readonly object _fileLock = new object();
        private readonly string _directory;
        private readonly long _maxFileSizeBytes;
        private readonly TimeSpan _flushInterval;
        private readonly Thread _worker;

        private StreamWriter _writer;
        private string _currentFilePath;
        private long _estimatedBytes;
        private bool _needRotate;
        private DateTime _lastFlushUtc;
        private volatile bool _stopping;
        private int _dropped;
        private int _reportedDropped;
        private int _disposed;

        /// <summary>使用默认参数：目录按天分文件、单日 10MB 轮转、队列 10000 条、500ms 批量刷盘。</summary>
        public AsyncFileLogger(string directory)
            : this(directory, 10L * 1024 * 1024, 10000, TimeSpan.FromMilliseconds(500))
        {
        }

        public AsyncFileLogger(string directory, long maxFileSizeBytes, int maxQueueSize, TimeSpan flushInterval)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException("directory");
            if (maxQueueSize <= 0) throw new ArgumentOutOfRangeException("maxQueueSize");

            _directory = directory;
            _maxFileSizeBytes = maxFileSizeBytes > 0 ? maxFileSizeBytes : long.MaxValue;
            _flushInterval = flushInterval > TimeSpan.Zero ? flushInterval : TimeSpan.FromMilliseconds(500);
            _queue = new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>(), maxQueueSize);

            _worker = new Thread(WriteLoop);
            _worker.IsBackground = true;
            _worker.Name = "AsyncFileLogger";
            _worker.Start();
        }

        public int DroppedCount
        {
            get { return Thread.VolatileRead(ref _dropped); }
        }

        public void Write(string message)
        {
            Write(null, message);
        }

        public void Write(string category, string message)
        {
            if (_stopping) return;

            LogEntry entry = new LogEntry();
            entry.Category = category;
            entry.Message = message ?? string.Empty;
            entry.Time = DateTime.Now;

            // 有界队列：满了直接丢弃并计数，绝不让日志成为检测线程的瓶颈
            if (!_queue.TryAdd(entry))
                Interlocked.Increment(ref _dropped);
        }

        /// <summary>
        /// 同步直写。用于崩溃回调等场景：不依赖后台线程，尽量少依赖托管堆。
        /// </summary>
        public void WriteCritical(string message)
        {
            try
            {
                lock (_fileLock)
                {
                    EnsureWriter(DateTime.Now);
                    if (_writer == null) return;
                    WriteEntry(_writer, null, message ?? string.Empty, DateTime.Now);
                    _writer.Flush();
                }
            }
            catch
            {
                // 崩溃路径上的写日志失败不能再抛异常
            }
        }

        /// <summary>入队一个刷新哨兵并等待其被处理，保证此前的日志均已落盘。</summary>
        public void Flush(TimeSpan timeout)
        {
            if (_stopping) return;

            using (ManualResetEventSlim signal = new ManualResetEventSlim(false))
            {
                LogEntry entry = new LogEntry();
                entry.Time = DateTime.Now;
                entry.FlushSignal = signal;

                if (!_queue.TryAdd(entry)) return;
                signal.Wait(timeout);
            }
        }

        public void Shutdown(TimeSpan timeout)
        {
            _stopping = true;
            try { _queue.CompleteAdding(); }
            catch { }

            try
            {
                if (_worker.IsAlive && !_worker.Join(timeout))
                {
                    // 超时不强杀线程：它只可能在写磁盘上卡住，继续等待由调用方决定
                }
            }
            catch { }

            try
            {
                lock (_fileLock)
                {
                    ReportDroppedIfAny(DateTime.Now);   // 退出前把最终丢弃数落盘
                    CloseWriter();
                }
            }
            catch { }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Shutdown(TimeSpan.FromSeconds(3));
        }

        // ==================== 后台写线程 ====================

        private void WriteLoop()
        {
            try
            {
                foreach (LogEntry entry in _queue.GetConsumingEnumerable())
                {
                    if (entry == null) continue;
                    try
                    {
                        ProcessEntry(entry);
                    }
                    catch
                    {
                        // 单条日志失败不能终止写线程
                    }
                }
            }
            catch
            {
            }
            finally
            {
                try
                {
                    lock (_fileLock) { CloseWriter(); }
                }
                catch { }
            }
        }

        private void ProcessEntry(LogEntry entry)
        {
            ManualResetEventSlim signal = entry.FlushSignal;
            if (signal != null)
            {
                lock (_fileLock)
                {
                    FlushInternal();
                    ReportDroppedIfAny(DateTime.Now);
                }
                signal.Set();
                return;
            }

            lock (_fileLock)
            {
                EnsureWriter(entry.Time);
                if (_writer != null)
                {
                    WriteEntry(_writer, entry.Category, entry.Message, entry.Time);
                    _estimatedBytes += ((long)entry.Message.Length + 64L) * 2L;
                }
                else
                {
                    // ★EnsureWriter 失败（磁盘不可写等）时条目并未落盘——计入丢弃数，
                    //   否则日志"静默消失"且 ReportDroppedIfAny 也无法反映真实损失
                    Interlocked.Increment(ref _dropped);
                }

                if (DateTime.UtcNow - _lastFlushUtc >= _flushInterval)
                    FlushInternal();

                // ★ N6：把因队列满被丢弃的条数落到日志文件里，避免"日志静默消失"无人知晓
                ReportDroppedIfAny(entry.Time);

                // 达到阈值则标记轮转并关闭当前文件，下次 EnsureWriter 时归档。
                // 注意：轮转依据累计写入量而非文件真实大小——_estimatedBytes 是按 UTF-16 放大估算的，
                // 与 UTF-8 落盘的真实字节数不同步，若在此处用真实大小判断会导致反复关闭重开却不归档。
                if (_estimatedBytes >= _maxFileSizeBytes)
                {
                    _needRotate = true;
                    CloseWriter();
                }
            }
        }

        /// <summary>
        /// ★ N6：把"因队列满被丢弃的日志条数"写入日志文件。
        /// 只在后台写线程上调用（调用方持 <see cref="_fileLock"/>），不给业务线程增加磁盘 IO。
        /// 写不进去（writer 为 null，例如磁盘故障）时保留计数，等 writer 恢复后再报，不丢信息。
        /// </summary>
        private void ReportDroppedIfAny(DateTime now)
        {
            int dropped = Thread.VolatileRead(ref _dropped);
            if (dropped <= _reportedDropped) return;
            if (_writer == null) return;

            int newly = dropped - _reportedDropped;
            _reportedDropped = dropped;

            WriteEntry(_writer, "日志丢弃",
                "日志队列已满，丢弃 " + newly + " 条（累计 " + dropped +
                " 条）。请检查磁盘空间与日志目录是否可写。", now);
            FlushInternal();
        }

        /// <summary>文本格式与旧 ErrorLog 保持一致，保证 ReadErrorLog 兼容性。</summary>
        private static void WriteEntry(TextWriter writer, string category, string message, DateTime time)
        {
            if (string.IsNullOrEmpty(category))
                writer.WriteLine(message + "/");
            else
                writer.WriteLine("[" + category + "] " + message + "/");

            writer.WriteLine("时间" + time.ToString("yyyy-MM-dd HH:mm:ss/", CultureInfo.InvariantCulture));
            writer.WriteLine("-------------------------------------------------/");
            writer.WriteLine();
        }

        // ==================== 文件管理（调用方需持有 _fileLock） ====================

        private void EnsureWriter(DateTime now)
        {
            string path = BuildLogPath(now);
            if (_writer != null && string.Equals(_currentFilePath, path, StringComparison.OrdinalIgnoreCase))
                return;

            CloseWriter();

            try { Directory.CreateDirectory(_directory); }
            catch { }

            if (_needRotate)
            {
                // 由累计写入量触发：无条件归档
                _needRotate = false;
                Rotate(path);
            }
            else if (File.Exists(path))
            {
                // 首次打开当日已有文件（进程重启/跨天）：按实际大小判断是否已超限
                try
                {
                    if (new FileInfo(path).Length >= _maxFileSizeBytes)
                        Rotate(path);
                }
                catch { }
            }

            try
            {
                // 共享模式必须同时允许"读"与"删除"：
                // 1) 读：ReadErrorLog 窗体需要在写线程持有句柄的同时读取当日日志；
                // 2) 删除：日志轮转时要对当前文件执行 File.Move，若句柄未声明 Delete 共享会被占用而失败。
                //    （Windows 文件共享是双向校验，读取方也必须以 FileShare.ReadWrite 打开。）
                FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                _writer = new StreamWriter(fs, Encoding.UTF8);
                _currentFilePath = path;

                FileInfo fi = new FileInfo(path);
                _estimatedBytes = fi.Exists ? fi.Length : 0;

                CleanupOldLogs(now);
            }
            catch
            {
                _writer = null;
                _currentFilePath = null;
            }
        }

        private string BuildLogPath(DateTime time)
        {
            return Path.Combine(_directory, time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".txt");
        }

        private DateTime _lastCleanupUtc;

        /// <summary>
        /// ★ 删除超过保留期的历史日志文件（主文件与 .1~.9 归档）。文件名即日期(yyyy-MM-dd[.N].txt)，
        /// 解析失败的回退按最后写入时间判断。每小时最多执行一次，调用方需持有 _fileLock。
        /// </summary>
        private void CleanupOldLogs(DateTime now)
        {
            if (now.ToUniversalTime() - _lastCleanupUtc < TimeSpan.FromHours(1)) return;
            _lastCleanupUtc = now.ToUniversalTime();

            try
            {
                DateTime cutoff = now.Date.AddDays(-HistoryRetentionDays);
                string[] files = Directory.GetFiles(_directory, "*.txt");
                for (int i = 0; i < files.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    int dot = name.IndexOf('.');
                    string datePart = dot > 0 ? name.Substring(0, dot) : name;

                    DateTime fileDate;
                    bool parsed = DateTime.TryParseExact(datePart, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out fileDate);
                    if (!parsed)
                    {
                        try { fileDate = File.GetLastWriteTime(files[i]); }
                        catch { continue; }
                    }
                    if (fileDate < cutoff)
                    {
                        try { File.Delete(files[i]); } catch { }
                    }
                }
            }
            catch
            {
                // 清理失败不影响写入
            }
        }

        /// <summary>把当日日志文件归档为 .1.txt ~ .9.txt（滚动复用）。调用方需持有 _fileLock。</summary>
        private void Rotate(string path)
        {
            try
            {
                if (!File.Exists(path)) return;

                string baseName = Path.GetFileNameWithoutExtension(path);

                for (int i = 1; i <= MaxArchivePerDay; i++)
                {
                    string archive = Path.Combine(_directory, baseName + "." + i.ToString(CultureInfo.InvariantCulture) + ".txt");
                    if (!File.Exists(archive))
                    {
                        File.Move(path, archive);
                        return;
                    }
                }

                // 归档位全部占用：删除最旧的一个，其余前移，再归档当前
                string oldest = Path.Combine(_directory, baseName + ".1.txt");
                try { File.Delete(oldest); }
                catch { }

                for (int i = 2; i <= MaxArchivePerDay; i++)
                {
                    string src = Path.Combine(_directory, baseName + "." + i.ToString(CultureInfo.InvariantCulture) + ".txt");
                    string dst = Path.Combine(_directory, baseName + "." + (i - 1).ToString(CultureInfo.InvariantCulture) + ".txt");
                    if (File.Exists(src))
                    {
                        try { File.Move(src, dst); }
                        catch { }
                    }
                }

                File.Move(path, Path.Combine(_directory, baseName + "." + MaxArchivePerDay.ToString(CultureInfo.InvariantCulture) + ".txt"));
            }
            catch
            {
                // 轮转失败不影响后续写入
            }
        }

        private void FlushInternal()
        {
            if (_writer != null)
            {
                try { _writer.Flush(); }
                catch { }
            }
            _lastFlushUtc = DateTime.UtcNow;
        }

        private void CloseWriter()
        {
            if (_writer != null)
            {
                try { _writer.Flush(); }
                catch { }
                try { _writer.Dispose(); }
                catch { }
                _writer = null;
            }
            _currentFilePath = null;
        }
    }
}
