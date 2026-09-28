using System;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 文件原子写：先写同目录临时文件，全部成功才替换目标文件。
    /// ★ 第47轮（方案保存失败破坏原文件根修）：现场事故链为
    ///   CogSerializer.SaveObjectToFile 直写目标 .vpp → 序列化中途抛
    ///   ObjectDisposedException("无法访问已释放的对象 对象名:CogImage8Grey") →
    ///   目标文件已被截断成残文件 → 下次启动 LoadObjectFromFile 报
    ///   "在数据读取之前就结束了流尾" → 好方案彻底打不开。
    /// <para>
    /// 本类保证：
    /// ① 写临时文件阶段任何异常（含进程被杀之外的一切失败）→ 目标文件保持原字节不变，
    ///    半成品临时文件被清理，绝不产生"半写方案"；
    /// ② 替换成功时旧文件滚动备份为 .bak（与替换同一原子操作），作恢复手段——
    ///    打开对话框过滤串为 "*.vpp*"，可手动选中 .bak 恢复；
    /// ③ 上次异常退出残留的 .saving 在下次保存开始时先清理。
    /// </para>
    /// </summary>
    internal static class AtomicFileSave
    {
        /// <param name="targetPath">最终目标文件路径</param>
        /// <param name="writeToTemp">接收临时路径并把内容完整写入；中途抛异常即视为保存失败</param>
        public static void Write(string targetPath, Action<string> writeToTemp)
        {
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentNullException("targetPath");
            if (writeToTemp == null) throw new ArgumentNullException("writeToTemp");

            string full = Path.GetFullPath(targetPath);
            string dir = Path.GetDirectoryName(full);
            string temp = full + ".saving";
            string backup = full + ".bak";

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);   // 另存为/模板保存可能选择尚不存在的文件夹

            if (File.Exists(temp))
            {
                try { File.Delete(temp); } catch { }   // 清上次进程被杀可能残留的半成品
            }

            try
            {
                writeToTemp(temp);

                if (!File.Exists(temp))
                    throw new IOException("保存过程未产生目标文件: " + temp);

                if (File.Exists(full))
                    ReplaceWithBackup(temp, full, backup);
                else
                    File.Move(temp, full);   // 首次保存（目标不存在，无旧文件可破坏）
            }
            finally
            {
                // 成功路径 temp 已被 Move/Replace 消耗；失败路径删除半成品，绝不留下误导性文件
                if (File.Exists(temp))
                {
                    try { File.Delete(temp); } catch { }
                }
            }
        }

        private static void ReplaceWithBackup(string temp, string full, string backup)
        {
            try
            {
                // 同卷原子替换：新文件落位与旧文件滚动 .bak 是同一个操作，不存在"两头落空"的窗口
                File.Replace(temp, full, backup, true);
            }
            catch (NotSupportedException)   // 含 PlatformNotSupportedException：个别文件系统不支持 Replace
            {
                ReplaceFallback(temp, full, backup);
            }
            catch (IOException)
            {
                ReplaceFallback(temp, full, backup);
            }
        }

        private static void ReplaceFallback(string temp, string full, string backup)
        {
            // File.Replace 不可用时的兜底：先保旧文件，再落新文件。
            // 中途任何一步失败，旧内容至少还在 full 或 backup 其中之一。
            if (File.Exists(backup))
            {
                try { File.Delete(backup); } catch { }
            }
            File.Copy(full, backup, true);
            File.Delete(full);
            File.Move(temp, full);
        }
    }
}
