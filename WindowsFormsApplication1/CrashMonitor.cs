using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 崩溃监控与关键信息记录：
    /// 1. 原生访问冲突(Access Violation)等不可捕获异常发生时，通过 UnhandledExceptionFilter
    ///    生成 minidump 并记录崩溃代码，避免"闪退"后无任何现场信息；
    /// 2. 通过启动标记文件检测上次是否非正常退出(闪退/被强杀)；
    /// 3. 记录崩溃时的系统上下文(内存、句柄、线程、运行时长)，便于后续定位。
    /// </summary>
    public static class CrashMonitor
    {
        private static readonly DateTime StartTime = DateTime.Now;
        private static readonly ErrorLog CrashErrorLog = new ErrorLog();
        private static TopLevelExceptionFilter _nativeFilter;   // 保持引用，防止被GC回收
        private static IntPtr _previousFilter = IntPtr.Zero;
        private static bool _firstChanceEnabled;
        private static DateTime _lastFirstChanceLog = DateTime.MinValue;
        private static int _insideFirstChance;

        // ==================== 启动/退出标记 ====================

        private static string MarkerPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_running.marker"); }
        }

        /// <summary>
        /// 安装原生崩溃过滤器。必须在 Main 最早期调用一次。
        /// </summary>
        public static void Initialize(bool enableFirstChanceLog = false)
        {
            _firstChanceEnabled = enableFirstChanceLog;
            try
            {
                _nativeFilter = OnNativeCrash;
                _previousFilter = SetUnhandledExceptionFilter(_nativeFilter);
            }
            catch
            {
                // 安装失败不阻塞程序启动
            }
            if (_firstChanceEnabled)
            {
                try
                {
                    // 记录所有被捕获但"看似忽略"的异常，便于发现被空catch吞掉的错误
                    AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
                }
                catch
                {
                }
            }
        }

        /// <summary>检查上次是否非正常退出，如有则写日志。</summary>
        public static void CheckPreviousCrash()
        {
            try
            {
                if (File.Exists(MarkerPath))
                {
                    string lastInfo;
                    try { lastInfo = File.ReadAllText(MarkerPath); }
                    catch { lastInfo = "(无法读取)"; }
                    // 启动早期：后台写线程刚创建，用同步直写确保这条关键诊断信息一定落盘
                    CrashErrorLog.WriteLogSync("★ 检测到上次程序未正常退出(可能闪退/被强制结束)。上次启动信息=" + lastInfo +
                        "。请查看Log目录当日日志，以及 CrashDumps 目录下的崩溃转储(*.dmp)。");
                }
            }
            catch
            {
            }
        }

        /// <summary>启动时写入标记，正常退出时删除。</summary>
        public static void MarkStarted()
        {
            try
            {
                File.WriteAllText(MarkerPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "|PID=" +
                    System.Diagnostics.Process.GetCurrentProcess().Id);
            }
            catch
            {
            }
        }

        /// <summary>确认正常退出时调用，清除启动标记。</summary>
        public static void MarkExitedCleanly()
        {
            try
            {
                if (File.Exists(MarkerPath))
                    File.Delete(MarkerPath);
            }
            catch
            {
            }
        }

        // ==================== 崩溃现场信息 ====================

        /// <summary>收集崩溃时的系统上下文(内存/句柄/线程/运行时长)。</summary>
        public static string GetCrashContext()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (System.Diagnostics.Process p = System.Diagnostics.Process.GetCurrentProcess())
                {
                    sb.Append("OS=").Append(Environment.OSVersion.VersionString)
                      .Append(" 64位=").Append(Environment.Is64BitProcess)
                      .Append(" .NET=").Append(Environment.Version)
                      .Append(" 启动=").Append(StartTime.ToString("yyyy-MM-dd HH:mm:ss"))
                      .Append(" 已运行=").Append(DateTime.Now.Subtract(StartTime).ToString(@"hh\:mm\:ss"))
                      .Append(" | 工作集=").Append(p.WorkingSet64 / 1024 / 1024).Append("MB")
                      .Append(" 虚拟内存=").Append(p.VirtualMemorySize64 / 1024 / 1024).Append("MB")
                      .Append(" 线程数=").Append(p.Threads.Count)
                      .Append(" GDI句柄=").Append(GetGuiResources(p.Handle, 0))
                      .Append(" USER句柄=").Append(GetGuiResources(p.Handle, 1));
                }
            }
            catch
            {
            }
            try
            {
                sb.Append(" | 线程ID=").Append(GetCurrentThreadId())
                  .Append(" 名称=").Append(Thread.CurrentThread.Name ?? "(无)")
                  .Append(" 托管线程=").Append(Thread.CurrentThread.ManagedThreadId);
            }
            catch
            {
            }
            return sb.ToString();
        }

        /// <summary>最近崩溃报告的固定路径（供启动时提示用户）。</summary>
        public static string GetCrashReportPath()
        {
            try { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashDumps", "最近崩溃.txt"); }
            catch { return ""; }
        }

        /// <summary>是否存在上次未正常退出的标记（用于启动提醒）。</summary>
        public static bool HadPreviousCrash()
        {
            try { return File.Exists(MarkerPath); }
            catch { return false; }
        }

        /// <summary>
        /// 生成一份面向普通用户(非开发者)的纯文本崩溃报告，便于"不用 VS 也能看懂原因"。
        /// 落地到 CrashDumps\最近崩溃.txt，同时保留一份带时间戳的历史副本避免互相覆盖。
        /// </summary>
        public static string WriteCrashReport(string title, string detail)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashDumps");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "最近崩溃.txt");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("================ 程序崩溃原因报告 ================");
                sb.AppendLine("生成时间 : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("崩溃类型 : " + (string.IsNullOrEmpty(title) ? "(未知)" : title));
                sb.AppendLine();
                sb.AppendLine("【发生了什么】");
                sb.AppendLine(string.IsNullOrEmpty(detail) ? "(无详细信息)" : detail);
                sb.AppendLine();
                sb.AppendLine("【崩溃时电脑状态】");
                sb.AppendLine(GetCrashContext());
                sb.AppendLine();
                sb.AppendLine("【你只需要做一件事】");
                sb.AppendLine("把本文件，以及程序目录下的 \"Log\" 文件夹，一起发给开发者即可。");
                sb.AppendLine("（进阶：CrashDumps 目录下还有 .dmp 文件，可一并发送，便于精确定位。）");
                string content = sb.ToString();
                File.WriteAllText(path, content, Encoding.UTF8);
                string hist = Path.Combine(dir, "崩溃_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                File.WriteAllText(hist, content, Encoding.UTF8);
                return path;
            }
            catch
            {
                return "";
            }
        }

        // ==================== minidump ====================

        /// <summary>生成崩溃转储文件，返回文件路径；失败返回空串。</summary>
        public static string WriteMinidump()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashDumps");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" +
                    System.Diagnostics.Process.GetCurrentProcess().Id + ".dmp");
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    MINIDUMP_EXCEPTION_INFORMATION dummy = default(MINIDUMP_EXCEPTION_INFORMATION);
                    bool ok = MiniDumpWriteDump(
                        System.Diagnostics.Process.GetCurrentProcess().Handle,
                        (uint)System.Diagnostics.Process.GetCurrentProcess().Id,
                        fs.SafeFileHandle,
                        MiniDumpWithDataSegs | MiniDumpWithHandleData | MiniDumpWithIndirectlyReferencedMemory |
                        MiniDumpWithThreadInfo | MiniDumpWithProcessThreadData,
                        ref dummy, IntPtr.Zero, IntPtr.Zero);
                    if (!ok)
                        return "";
                }
                return path;
            }
            catch
            {
                return "";
            }
        }

        // ==================== 崩溃转储清理 ====================

        /// <summary>
        /// 2026-09-06：清理历史崩溃转储，仅保留最近 keepCount 个 .dmp 文件，避免长期运行无限堆积
        /// （现场曾堆积 8GB）。仅删除 .dmp，文本报告(很小)不受影响。
        /// 须在 Initialize 之后、本次崩溃写入之前调用（启动早期执行即可清掉历史堆积）。
        /// </summary>
        public static void CleanupDumps(int keepCount = 3)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashDumps");
                if (!Directory.Exists(dir)) return;
                var dumps = new DirectoryInfo(dir)
                    .GetFiles("*.dmp")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();
                for (int i = keepCount; i < dumps.Count; i++)
                {
                    try { dumps[i].Delete(); }
                    catch { }
                }
            }
            catch
            {
            }
        }

        // ==================== 原生崩溃过滤器 ====================

        /// <summary>
        /// 原生未处理异常(如访问冲突)回调。此回调运行在崩溃线程上、CLR可能处于不稳定状态，
        /// 仅做最少的 P/Invoke 操作(转储+追加一行原生日志)，尽量不依赖托管堆。
        /// 随后把控制权交还前一个过滤器，保持系统原有处理链。
        /// </summary>
        private static int OnNativeCrash(IntPtr exceptionInfo)
        {
            uint code = 0;
            try
            {
                if (exceptionInfo != IntPtr.Zero)
                {
                    EXCEPTION_POINTERS pointers = (EXCEPTION_POINTERS)Marshal.PtrToStructure(exceptionInfo, typeof(EXCEPTION_POINTERS));
                    if (pointers.ExceptionRecord != IntPtr.Zero)
                    {
                        EXCEPTION_RECORD record = (EXCEPTION_RECORD)Marshal.PtrToStructure(pointers.ExceptionRecord, typeof(EXCEPTION_RECORD));
                        code = record.ExceptionCode;
                    }
                }
            }
            catch
            {
            }

            // 1. 生成带异常上下文的 minidump
            string dumpPath = "";
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashDumps");
                Directory.CreateDirectory(dir);
                dumpPath = Path.Combine(dir, "native_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" +
                    System.Diagnostics.Process.GetCurrentProcess().Id + ".dmp");
                using (FileStream fs = new FileStream(dumpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    MINIDUMP_EXCEPTION_INFORMATION mei = new MINIDUMP_EXCEPTION_INFORMATION();
                    mei.ThreadId = GetCurrentThreadId();
                    mei.ExceptionPointers = exceptionInfo;
                    mei.ClientPointers = true;
                    MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), fs.SafeFileHandle,
                        MiniDumpWithDataSegs | MiniDumpWithHandleData | MiniDumpWithIndirectlyReferencedMemory |
                        MiniDumpWithThreadInfo | MiniDumpWithProcessThreadData,
                        ref mei, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch
            {
                dumpPath = "";
            }

            // 2. 追加一行原生崩溃日志(纯原生API，尽量不依赖托管堆)
            string line = "NATIVE_CRASH code=0x" + code.ToString("X8") + " time=" +
                          DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " dump=" + dumpPath + "\r\n";
            AppendNativeLogLine(line);

            // 3. 尽力再写一条托管日志(若CLR已损坏则静默失败，不影响主流程)
            try
            {
                // 原生崩溃回调：后台写线程此刻可能已不可用，必须同步直写
                CrashErrorLog.WriteLogSync("原生崩溃: code=0x" + code.ToString("X8") + " 现场=" + GetCrashContext() +
                                       " 转储=" + dumpPath);
                // 尽力生成面向用户的崩溃报告（CLR 若已损坏会静默失败，不影响主流程）
                WriteCrashReport("原生层崩溃 code=0x" + code.ToString("X8"),
                    "底层发生内存/访问冲突错误，错误代码 0x" + code.ToString("X8") + "。\n崩溃转储: " + dumpPath);
            }
            catch
            {
            }

            // 4. 交还前一个过滤器(通常是CLR自身)，保持系统原有处理行为
            if (_previousFilter != IntPtr.Zero)
            {
                try
                {
                    TopLevelExceptionFilter prev =
                        (TopLevelExceptionFilter)Marshal.GetDelegateForFunctionPointer(_previousFilter, typeof(TopLevelExceptionFilter));
                    return prev(exceptionInfo);
                }
                catch
                {
                }
            }
            return 0; // EXCEPTION_CONTINUE_SEARCH：由系统默认处理(进程终止)
        }

        private static void AppendNativeLogLine(string line)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashDumps");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "native_crash_log.txt");
                IntPtr hFile = CreateFileW(path, 0x4 /*FILE_APPEND_DATA*/, 0x3 /*READ|WRITE share*/,
                    IntPtr.Zero, 4 /*OPEN_ALWAYS*/, 0x80 /*NORMAL*/, IntPtr.Zero);
                if (hFile == new IntPtr(-1) || hFile == IntPtr.Zero)
                    return;
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(line);
                    if (bytes.Length > 1024)
                    {
                        byte[] truncated = new byte[1024];
                        Array.Copy(bytes, truncated, 1024);
                        bytes = truncated;
                    }
                    uint written;
                    WriteFile(hFile, bytes, (uint)bytes.Length, out written, IntPtr.Zero);
                }
                finally
                {
                    CloseHandle(hFile);
                }
            }
            catch
            {
            }
        }

        // ==================== First-Chance 异常日志 ====================

        /// <summary>
        /// 记录"被捕获但看似忽略"的异常(节流，默认每5秒至多一条)。
        /// 用于发现空catch吞掉的配置/工具错误。
        /// </summary>
        private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (Interlocked.Exchange(ref _insideFirstChance, 1) == 1)
                return;
            try
            {
                Exception ex = e.Exception;
                if (ex == null)
                    return;
                // 节流：避免高频异常刷爆日志
                DateTime now = DateTime.Now;
                if ((now - _lastFirstChanceLog).TotalSeconds < 5)
                    return;
                _lastFirstChanceLog = now;
                string topStack = "";
                try
                {
                    if (ex.StackTrace != null)
                    {
                        string[] lines = ex.StackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                            topStack = lines[0];
                    }
                }
                catch
                {
                }
                CrashErrorLog.WriteLog("[FirstChance] " + ex.GetType().Name + ": " + ex.Message + " @" + topStack);
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref _insideFirstChance, 0);
            }
        }

        // ==================== P/Invoke 声明 ====================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int TopLevelExceptionFilter(IntPtr exceptionInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct EXCEPTION_POINTERS
        {
            public IntPtr ExceptionRecord;
            public IntPtr ContextRecord;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EXCEPTION_RECORD
        {
            public uint ExceptionCode;
            public uint ExceptionFlags;
            public IntPtr ExceptionRecord;
            public IntPtr ExceptionAddress;
            public uint NumberParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public IntPtr[] ExceptionInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINIDUMP_EXCEPTION_INFORMATION
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            [MarshalAs(UnmanagedType.Bool)]
            public bool ClientPointers;
        }

        private const uint MiniDumpWithDataSegs = 0x00000002;
        private const uint MiniDumpWithHandleData = 0x00000004;
        private const uint MiniDumpWithIndirectlyReferencedMemory = 0x00000008;
        private const uint MiniDumpWithThreadInfo = 0x00000010;
        private const uint MiniDumpWithProcessThreadData = 0x00000100;

        [DllImport("dbghelp.dll", SetLastError = true)]
        private static extern bool MiniDumpWriteDump(
            IntPtr hProcess, uint ProcessId, Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
            uint DumpType, ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
            IntPtr UserStreamParam, IntPtr CallbackParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr SetUnhandledExceptionFilter(TopLevelExceptionFilter lpTopLevelExceptionFilter);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
