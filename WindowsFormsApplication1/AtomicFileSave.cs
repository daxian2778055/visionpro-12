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
    /// ③ 上次异常退出残留的 .saving 在下次保存开始时先清理；
    /// ④ 临时文件名刻意剥离 ".vpp"（形如 xxx.saving 而非 xxx.vpp.saving）——
    ///    过滤串 "*.vpp*" 不会放行半成品临时文件（放行 .bak 是刻意的恢复手段，
    ///    放行 .saving 是事故：选中即拿半截文件去加载，正是上面根因链里的"流尾"崩溃）；
    /// ⑤ 兜底替换第三步落位失败时先把 .bak 还原回原路径再抛，连还原都失败才抛
    ///    IntegrityLostException 由调用方改口——"原方案文件未被改动"这句绝不骗人。
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
            string temp = TempPathFor(full);
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

        /// <summary>
        /// 临时文件路径：同目录、文件名剥离全部 ".vpp"（含大小写）。
        /// ★第48轮复审P3②：打开框过滤串为 "*.vpp*"（放行 .bak 是刻意的恢复手段），若临时名
        /// 形如 xxx.vpp.saving 同样会被放行——操作员选中即拿半截文件去加载，
        /// 正是第47轮根因链里的"流尾"崩溃。仅硬杀进程时临时文件才会残留，且下次保存开始即清理。
        /// </summary>
        internal static string TempPathFor(string targetPath)
        {
            string full = Path.GetFullPath(targetPath);
            string dir = Path.GetDirectoryName(full);
            string stem = Path.GetFileName(full);
            for (int i = stem.IndexOf(".vpp", StringComparison.OrdinalIgnoreCase); i >= 0;
                i = stem.IndexOf(".vpp", StringComparison.OrdinalIgnoreCase))
                stem = stem.Remove(i, 4);
            if (stem.Length == 0) stem = "save";
            return Path.Combine(dir ?? "", stem + ".saving");
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
            // ★第48轮复审P2-1（三步失败点的文案诚实性）：
            //  · Copy 失败 → full 原封不动，调用方"原方案文件未被改动"为真；
            //  · Delete 失败 → full 仍在，为真；
            //  · Move（第三步）失败 → 此刻 full 是空的、旧内容只在 backup——必须先把
            //    backup 还原回 full 再抛，否则那句"未被改动"就是假话；
            //    连还原都失败（二次失败，极罕见）才抛 IntegrityLostException，
            //    由调用方改口按 .bak 指引恢复，任何出口都不许骗人。
            if (File.Exists(backup))
            {
                try { File.Delete(backup); } catch { }
            }
            File.Copy(full, backup, true);
            File.Delete(full);
            try
            {
                File.Move(temp, full);
            }
            catch (Exception moveEx)
            {
                bool restored = false;
                if (!File.Exists(full) && File.Exists(backup))
                {
                    try { File.Move(backup, full); restored = true; } catch { }
                }
                if (restored)
                {
                    // 原路径已还原为旧内容——"原方案文件未被改动"仍成立，抛原始失败原因
                    throw;
                }
                throw new IntegrityLostException(
                    "保存落位失败: " + moveEx.Message
                    + "；原方案旧内容保留在备份文件 " + backup
                    + "（如原方案内容缺失/异常，请把该备份改名为 " + Path.GetFileName(full) + " 恢复）",
                    backup, moveEx);
            }
        }

        /// <summary>
        /// ★第48轮复审P2-1：兜底替换连"把 .bak 还原回原路径"都失败时才抛——原路径此刻无内容
        /// 保证，调用方文案不得再声称"原方案文件未被改动"，应据 Message 里的 .bak 路径指引手动
        /// 恢复（Message 必带备份路径，BackupPath 供程序化判断）。
        /// </summary>
        public class IntegrityLostException : IOException
        {
            public string BackupPath { get; private set; }

            public IntegrityLostException(string message, string backupPath, Exception innerException)
                : base(message, innerException)
            {
                BackupPath = backupPath;
            }
        }

        /// <summary>
        /// 失败原因是否为"死图像引用"（供调用方弹特化指引）。
        /// ★第48轮复审P3③：原判断只看最外层 ex is ObjectDisposedException || Message 含"已释放"——
        /// 异常被 COM/包装层裹一层、或走英文消息时会漏判成通用文案。顺 InnerException 找三层，
        /// 中英文口径都认（现场日志"无法访问已释放的对象"，Cognex/COM 英文侧 ObjectDisposed/disposed）。
        /// </summary>
        internal static bool IsDeadImageException(Exception ex)
        {
            for (int depth = 0; ex != null && depth < 3; depth++, ex = ex.InnerException)
            {
                if (ex is ObjectDisposedException) return true;
                string msg = ex.Message;
                if (string.IsNullOrEmpty(msg)) continue;
                if (msg.Contains("已释放")) return true;
                if (msg.IndexOf("ObjectDisposed", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (msg.IndexOf("disposed", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
