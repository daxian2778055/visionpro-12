using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApplication1.Core.Infrastructure;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1
{
    static class Program
    {
        /// <summary>
        /// 1代表中文，2代表英文
        /// </summary>
        public static int Language = 1;

        /// <summary>
        /// 是否显示相关的信息
        /// </summary>
        public static bool ShowAuthorInfomation = true;

        private static ErrorLog _errorLog;
        private static readonly object ErrorLogLock = new object();

        private static ErrorLog GetErrorLog()
        {
            if (_errorLog == null)
            {
                lock (ErrorLogLock)
                {
                    if (_errorLog == null)
                        _errorLog = new ErrorLog();
                }
            }
            return _errorLog;
        }

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool newMutexCreated = true;
            using (new Mutex(true, Assembly.GetExecutingAssembly().FullName, out newMutexCreated))
            {
                if (!newMutexCreated)
                {
                    MessageBox.Show("程序已启动！请不要启动多个程序");
                    Environment.Exit(0);
                    return;
                }

                // 注册全局异常处理
                try
                {
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandleException;
                    Application.ThreadException += Application_ThreadException;
                    TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                }
                catch (Exception ex)
                {
                    GetErrorLog().WriteLog("入口点异常" + ex.Message);
                    MessageBox.Show("入口点异常" + ex.Message);
                }

                // 崩溃监控：原生崩溃minidump + 上次非正常退出检测
                // 说明：原生过滤器/启动标记/异常现场均为"崩溃或启停瞬间"才工作，正常运行零开销。
                // FirstChance(被吞异常日志)默认关闭：它会在每次抛异常(含被正常catch的)时触发，
                // 若需排查"被空catch吞掉的错误"可临时改为 true。
                try
                {
                    CrashMonitor.Initialize(false);
                    CrashMonitor.CleanupDumps();   // 2026-09-06：启动清理历史崩溃转储，仅保留最近 3 个
                    CrashMonitor.CheckPreviousCrash();
                    // ★修复（2026-09-20）：原实现"只要历史上生成过 最近崩溃.txt 就每次启动都弹窗"，
                    //   且"客户直接关电脑/断电/任务管理器强杀"残留标记也会弹——两类都是误报（现场明确反馈）。
                    //   现统一由 HadPreviousCrash() 判定：标记残留 且 崩溃记录时间不早于上次启动时间
                    //   （即上次运行期间确实发生过崩溃）。无崩溃记录的残留标记只记日志，不打扰用户。
                    try
                    {
                        if (CrashMonitor.HadPreviousCrash())
                        {
                            string reportPath = CrashMonitor.GetCrashReportPath();
                            bool haveReport = !string.IsNullOrEmpty(reportPath) && System.IO.File.Exists(reportPath);
                            string msg = haveReport
                                ? "检测到上次程序异常退出（闪退）。\n上次崩溃原因已记录到：\n" + reportPath +
                                  "\n\n请把该文件（及程序目录下的 Log 文件夹）发给开发者即可。"
                                : "检测到上次程序异常退出（闪退）。\n未生成详细报告，但程序目录下的 Log 文件夹里仍有线索，\n请把 Log 文件夹发给开发者。";
                            MessageBox.Show(msg, "上次崩溃提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else if (CrashMonitor.HasStaleMarker())
                        {
                            GetErrorLog().WriteLog("上次程序未正常关闭（断电/直接关机/强杀，无崩溃记录），本次不弹崩溃提醒。");
                        }
                    }
                    catch { }
                    // ★2026-09-20：Windows 正常关机/注销（SessionEnding）时提前清标记——
                    //   否则"客户直接关电脑"会残留标记；有了崩溃证据判定兜底不会误报，但正常关机本不应残留。
                    try
                    {
                        Microsoft.Win32.SystemEvents.SessionEnding += (s, se) =>
                        {
                            try { CrashMonitor.MarkExitedCleanly(); } catch { }
                        };
                    }
                    catch (Exception exSe) { GetErrorLog().WriteLog("注册关机清标记异常" + exSe.Message); }
                    CrashMonitor.MarkStarted();
                }
                catch (Exception ex)
                {
                    GetErrorLog().WriteLog("崩溃监控初始化异常" + ex.Message);
                }

                // ★ 关键：日志收尾兜底。
                // Form1 关闭时走 Environment.Exit(0)，而 Environment.Exit 不会执行本方法中的 finally，
                // 因此必须挂 ProcessExit —— 它能覆盖 Environment.Exit、Application.Exit 等所有退出路径，
                // 否则退出瞬间队列中尚未落盘的日志会全部丢失。
                try
                {
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                }
                catch (Exception ex)
                {
                    GetErrorLog().WriteLog("注册退出事件异常" + ex.Message);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 组合根：在创建主窗体前注册并初始化全部应用服务（配置、日志等）。
                // ★ 修复【启动即崩】：Build 失败若继续往下走，Form1 构造时字段初始化器 Resolve 必然二次抛错，
                //   导致"启动即退出"且原因被上一层吞成"严重错误"。这里失败要明确告知并终止，绝不带病启动。
                try
                {
                    AppHost.Build();
                }
                catch (Exception ex)
                {
                    try { GetErrorLog().WriteLogSync("服务组合根初始化异常" + ex.ToString()); } catch { }
                    MessageBox.Show("软件初始化失败，即将退出。\n\n" + ex.Message +
                                    "\n\n请把程序目录下 Log 文件夹发给开发者。", "启动失败",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    try { CrashMonitor.MarkExitedCleanly(); } catch { }
                    return;   // 不创建主窗体；using(Mutex) 与 ProcessExit 兜底日志收尾
                }

                try
                {
                    Application.Run(new Form1());
                }
                catch (Exception ex)
                {
                    GetErrorLog().WriteLogSync("主窗体运行异常" + ex.ToString());
                    MessageBox.Show("程序遇到严重错误，即将退出。\n" + ex.Message, "严重错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // 覆盖 Application.Exit 等正常退出路径（Environment.Exit 由 ProcessExit 兜底）
                    try { CrashMonitor.MarkExitedCleanly(); } catch { }
                    // ★ N4：先释放服务（ConfigService → ClassIni 刷新），再关日志，保证收尾顺序
                    try { AppHost.Shutdown(); } catch { }
                    try { LogManager.Shutdown(); }
                    catch { }
                }
            }
        }

        private static string GetExceptionTitle(object exObj)
        {
            if (exObj is Exception ex)
                return ex.GetType().Name;
            return exObj?.GetType().Name ?? "未知";
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var log = GetErrorLog();
                log.WriteLog("未处理Task异常: " + e.Exception?.ToString());
                log.WriteLog("异常现场: " + CrashMonitor.GetCrashContext());
                e.SetObserved();
            }
            catch
            {
                // 异常处理中的异常不做处理，防止递归
            }
        }

        private static void CurrentDomain_UnhandleException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var log = GetErrorLog();
                string errorInfo = e.ExceptionObject?.ToString() ?? "未知异常对象";
                // ★ 2026-09-11：AppDomain 未处理异常是进程终止路径（IsTerminating=true），
                //   处理完本回调进程即销毁，必须用同步 WriteLogSync，否则异步日志来不及落盘就丢失崩溃原因。
                log.WriteLogSync("未处理AppDomain异常: " + errorInfo);
                log.WriteLogSync("崩溃现场: " + CrashMonitor.GetCrashContext());
                string dumpPath = CrashMonitor.WriteMinidump();
                log.WriteLogSync("已生成崩溃转储: " + dumpPath);
                // 生成面向用户的大白话崩溃报告
                string report = CrashMonitor.WriteCrashReport(GetExceptionTitle(e.ExceptionObject), errorInfo);
                string tip = string.IsNullOrEmpty(report)
                    ? "程序遇到未处理的异常，已崩溃。详细日志在程序目录下的 Log 文件夹。"
                    : "程序遇到未处理的异常，已崩溃。\n\n原因已记录到文件：\n" + report +
                      "\n\n请把该文件（及程序目录下的 Log 文件夹）发给开发者即可。";
                MessageBox.Show(tip, "程序崩溃", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // 异常处理中的异常不做处理
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                var log = GetErrorLog();
                string errorInfo = e.Exception?.ToString() ?? "未知UI线程异常";
                log.WriteLog("未处理UI异常: " + errorInfo);
                log.WriteLog("崩溃现场: " + CrashMonitor.GetCrashContext());
                string dumpPath = CrashMonitor.WriteMinidump();
                log.WriteLog("已生成崩溃转储: " + dumpPath);
                string report = CrashMonitor.WriteCrashReport(GetExceptionTitle(e.Exception), errorInfo);
                string tip = string.IsNullOrEmpty(report)
                    ? "程序遇到界面异常，已崩溃。详细日志在程序目录下的 Log 文件夹。"
                    : "程序遇到界面异常，已崩溃。\n\n原因已记录到文件：\n" + report +
                      "\n\n请把该文件（及程序目录下的 Log 文件夹）发给开发者即可。";
                MessageBox.Show(tip, "程序崩溃", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch
            {
                // 异常处理中的异常不做处理
            }
        }

        /// <summary>
        /// 进程退出兜底：Environment.Exit 不会执行 Main 中的 finally，
        /// 而本事件能覆盖 Environment.Exit / Application.Exit / 主窗体关闭 等全部退出路径。
        /// 注意 ProcessExit 的处理有执行时间限制，故收尾超时取较短值。
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            try { CrashMonitor.MarkExitedCleanly(); } catch { }
            // ★ N4：主窗体关闭走的是 FormClosed → Environment.Exit(0)，Main 的 finally 不会执行，
            //   因此在这里释放组合根服务（ConfigService → ClassIni 刷新；相机为兜底释放）。
            try { AppHost.Shutdown(); } catch { }
            try { LogManager.Shutdown(TimeSpan.FromSeconds(2)); }
            catch { }
        }
    }
}
