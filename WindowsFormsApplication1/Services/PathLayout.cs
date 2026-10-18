using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ★第60轮：图像存图 / 统计 CSV 的根目录解析。
    ///
    /// 现场长期硬编码 E:\ —— 24 路存图路径（Form1.InitializeJobManager 里逐个赋值）
    /// 与 6 处统计 CSV 路径（RunLog 内部拼接）全部钉死在 E 盘，现场没有 E 盘
    /// （或盘符要换）时只有改代码重编一条路。
    ///
    /// 现在改为读取 ini [camera] 段两个键：
    ///   pathhead_root   —— 图像存图根，默认 @"E:\"
    ///   statistics_root —— 统计 CSV 根，默认跟随 pathhead_root（不设时单独回退 @"E:\"）
    /// 不配置时与改动前逐字一致：子目录名（fu1ok、生产统计 / 每日统计 ……）保持原样，只换前缀。
    ///
    /// 线程约定：纯函数、无状态、永不抛 —— 供启动期一次性调用。
    /// </summary>
    internal static class PathLayout
    {
        /// <summary>图像存图根目录的默认值（现场约定，改动前就是这个值）。</summary>
        public const string DefaultImageRoot = @"E:\";

        /// <summary>统计 CSV 根目录的默认值。</summary>
        public const string DefaultStatisticsRoot = @"E:\";

        /// <summary>
        /// 规范化配置的根目录：去 NUL、首尾空白、成对引号；
        /// 空/全空白/NUL 清理后为空 → 返回 fallback；末尾统一补一个反斜杠，
        /// 使调用方可以无条件写 root + 子目录名。
        /// </summary>
        public static string NormalizeRoot(string raw, string fallback)
        {
            bool ignored;
            return NormalizeRoot(raw, fallback, out ignored);
        }

        /// <summary>
        /// 同上，另外回报<b>本次是否取了 fallback</b>（<paramref name="usedFallback"/>）。
        /// 调用方要靠它把"配置值不可识别、已按回退值走"点名到日志里——光比对字符串判不出来：
        /// 例如值是单个引号（不成对，不剥）会原样带出一个怪路径，而"配了却等于没配"正是现场最难查的那种。
        /// 注意：true 只代表"取了 fallback"，不区分键根本没配 / 配了但是脏值，那是调用方的措辞问题。
        /// </summary>
        public static string NormalizeRoot(string raw, string fallback, out bool usedFallback)
        {
            string dflt = string.IsNullOrEmpty(fallback) ? DefaultStatisticsRoot : fallback;
            if (raw == null) { usedFallback = true; return dflt; }
            string s = raw.Replace("\0", "").Trim();
            if (s.Length == 0) { usedFallback = true; return dflt; }
            // 现场常见手工编辑 ini 时把值连带引号一起写进来（"E:\data"），成对时剥掉
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                s = s.Substring(1, s.Length - 2).Trim();
                if (s.Length == 0) { usedFallback = true; return dflt; }
            }
            usedFallback = false;
            if (s[s.Length - 1] == '\\' || s[s.Length - 1] == '/') return s;
            return s + "\\";
        }

        /// <summary>
        /// 把根目录与子目录名（如 "fu1ok"、"图像统计"）拼成一个以反斜杠收尾的前缀。
        /// 子目录名两端的反斜杠会被去掉，避免拼出三连斜杠。
        /// </summary>
        public static string SubDirectory(string root, string name)
        {
            string r = NormalizeRoot(root, DefaultStatisticsRoot);
            string n = (name ?? "").Trim().Trim('\\', '/');
            return n.Length == 0 ? r : r + n + "\\";
        }
    }
}
