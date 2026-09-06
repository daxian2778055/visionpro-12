using System;
using System.IO;
using System.Text;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 错误日志门面。
    /// <para>
    /// 旧实现为「每条日志 lock → 创建目录 → 打开文件 → 4 次 WriteLine → Flush」的同步写。
    /// 由于相机检测、IO 输出、通讯轮询等线程都在写日志，全局锁 + 同步磁盘 IO 会让
    /// 12 路检测线程互相等待，日志越密越慢。
    /// </para>
    /// 现改为转发到 <see cref="AsyncFileLogger"/>：调用线程只入队，后台线程批量落盘。
    /// 日志文本格式与旧实现完全一致，ReadErrorLog 窗体无需改动。
    /// </summary>
    class ErrorLog
    {
        /// <summary>异步写日志。业务代码与检测热路径统一使用此方法。</summary>
        public void WriteLog(string msg)
        {
            LogManager.Default.Write(msg);
        }

        /// <summary>
        /// 同步直写，绕过后台队列。
        /// 仅用于崩溃回调、进程退出等「后台写线程可能已停止或托管堆已损坏」的场景。
        /// </summary>
        public void WriteLogSync(string msg)
        {
            LogManager.Default.WriteCritical(msg);
        }

        public string ReadLog(string path)
        {
            // 先落盘，避免读到尚未刷出的当日日志
            try { LogManager.Default.Flush(TimeSpan.FromSeconds(1)); }
            catch { }

            try
            {
                // 必须声明 FileShare.ReadWrite：日志写入端持有常开句柄，
                // 默认的 FileShare.Read 会因 Windows 文件共享双向校验而抛 IOException。
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete))
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (IOException)
            {
                return "Error：所选日期并没有报告";
            }
        }
    }
}
