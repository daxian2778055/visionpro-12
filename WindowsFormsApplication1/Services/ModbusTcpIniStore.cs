using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus TCP 多连接配置读写（阶段 2-A：只读写 ini，不接管任何运行逻辑）。
    ///
    /// 把 test.ini 中 N 条 Modbus TCP 连接的全部段加载/保存成 ModbusTcpLinkConfig 列表。
    /// 命名规则（与总纲第 4 节一致）：连接 1 空后缀（[modbustcp]），连接 N 追加数字。
    ///
    ///   [modbustcp]        / [modbustcpN]        连接参数（ip/port/cell/abcd/轮询...）
    ///   [N_modbustcp]      / [N_modbustcpN]      数据块（name/qishi/changdu/gaodiwei/geshi）
    ///   [cN_modbustcp]     / [cN_modbustcpN]     相机绑定（chufa/fanhuizhi/fanhuien/fankui/chukufangshi/chufazhi1/2）
    ///   [change_modbustcp] / [change_modbustcpN] 切型字符（键 1..12）
    ///   [path_modbustcp]   / [path_modbustcpN]   方案路径（键 1..12）
    /// </summary>
    public static class ModbusTcpIniStore
    {
        public const int MaxLinks = 4;          // 每协议最多连接数（与总纲决策一致）
        public const int MaxDataBlocks = 10;    // 数据块上限（与既有逻辑一致）
        public const int MaxCameras = 13;       // 相机绑定上限（1..13）
        public const int MaxChangeTypes = 12;   // 切型/方案条目上限（1..12）
        // ★第33轮复审建议①：地址/长度/轮询间隔量程——与三协议窗体的界面控件一致（超出即无意义配置）
        internal const decimal MaxAddress = 60000;        // 连接起始 numericUpDown1.Maximum / 块起始 numericUpDown5.Maximum
        internal const decimal MaxTotalLength = 50;       // 总长度 numericUpDown2.Maximum
        internal const decimal MaxBlockLength = 50;       // 块长度 numericUpDown4.Maximum
        internal const decimal MaxPollInterval = 2000;    // 轮询间隔 numericUpDown3.Maximum

        internal static string ConnSection(int link) => link == 1 ? "modbustcp" : "modbustcp" + link;
        internal static string BlockSection(int link, int n) => n + "_modbustcp" + (link == 1 ? "" : link.ToString());
        internal static string CameraSection(int link, int n) => "c" + n + "_modbustcp" + (link == 1 ? "" : link.ToString());
        internal static string ChangeSection(int link) => "change_modbustcp" + (link == 1 ? "" : link.ToString());
        internal static string PathSection(int link) => "path_modbustcp" + (link == 1 ? "" : link.ToString());

        /// <summary>加载全部连接（连接 1 始终存在；连接 2..MaxLinks 仅当对应段非空时才纳入）。</summary>
        public static List<ModbusTcpLinkConfig> LoadAll(ClassIni ini)
        {
            if (ini == null || string.IsNullOrEmpty(ini.FileName))
                return new List<ModbusTcpLinkConfig>();

            var list = new List<ModbusTcpLinkConfig>();
            list.Add(Load(ini, 1));
            for (int link = 2; link <= MaxLinks; link++)
            {
                var keys = new StringCollection();
                ini.ReadSection(ConnSection(link), keys);
                if (keys.Count > 0)
                    list.Add(Load(ini, link));
            }
            return list;
        }

        /// <summary>加载单条连接的全部配置。</summary>
        public static ModbusTcpLinkConfig Load(ClassIni ini, int link)
        {
            string sec = ConnSection(link);
            var cfg = new ModbusTcpLinkConfig
            {
                LinkId = link,
                Name = ini.ReadString(sec, "name", "ModbusTCP-" + link),
                Ip = ini.ReadString(sec, "ip", "127.0.0.1").Replace("\0", ""),
                Port = ini.ReadString(sec, "port", "9600").Replace("\0", ""),
                Cell = ini.ReadString(sec, "cell", "0").Replace("\0", ""),
                Abcd = ini.ReadString(sec, "abcd", "CDAB").Replace("\0", ""),
                Qishi = ReadDecimalBounded(ini, sec, "qishi", 0, 0, MaxAddress),
                Zongchang = ReadDecimalBounded(ini, sec, "zongchang", 1, 0, MaxTotalLength),
                LunxunTime = ReadDecimalBounded(ini, sec, "lunxun_time", 20, 0, MaxPollInterval),
                ModbusEn = ini.ReadBool(sec, "modbus_en", false),
                ModbusLunxunen = ini.ReadBool(sec, "modbus_lunxunen", false),
                Geshu = ReadGeshuBounded(ini, sec),
            };

            // 数据块：1..Geshu
            for (int n = 1; n <= cfg.Geshu; n++)
            {
                string bsec = BlockSection(link, n);
                cfg.DataBlocks.Add(new ModbusTcpDataBlockConfig
                {
                    Index = n,
                    Name = ini.ReadString(bsec, "name", "").Replace("\0", ""),
                    Qishi = ReadDecimalBounded(ini, bsec, "qishi", 0, 0, MaxAddress),
                    Changdu = ReadDecimalBounded(ini, bsec, "changdu", 0, 0, MaxBlockLength),
                    Gaodiwei = ini.ReadString(bsec, "gaodiwei", "触发").Replace("\0", ""),
                    Geshi = ini.ReadString(bsec, "geshi", "int").Replace("\0", ""),
                });
            }

            // 相机绑定：1..MaxCameras，仅纳入段内有内容的
            for (int n = 1; n <= MaxCameras; n++)
            {
                string csec = CameraSection(link, n);
                var keys = new StringCollection();
                ini.ReadSection(csec, keys);
                if (keys.Count == 0) continue;
                cfg.CameraBindings.Add(new ModbusTcpCameraBindingConfig
                {
                    CameraNo = n,
                    Chufa = ini.ReadString(csec, "chufa", " ").Replace("\0", ""),
                    Fanhuizhi = ini.ReadString(csec, "fanhuizhi", "0").Replace("\0", ""),
                    Fanhuien = ini.ReadBool(csec, "fanhuien", false),
                    Fankui = ini.ReadString(csec, "fankui", "0").Replace("\0", ""),
                    Chukufangshi = ini.ReadString(csec, "chukufangshi", "相等").Replace("\0", ""),
                    Chufazhi1 = ini.ReadString(csec, "chufazhi1", "").Replace("\0", ""),
                    Chufazhi2 = ini.ReadString(csec, "chufazhi2", "").Replace("\0", ""),
                });
            }

            // 切型字符 + 方案路径：1..MaxChangeTypes
            for (int n = 1; n <= MaxChangeTypes; n++)
            {
                cfg.ChangeTypes[n] = ini.ReadString(ChangeSection(link), n.ToString(), "").Replace("\0", "");
                cfg.SchemePaths[n] = ini.ReadString(PathSection(link), n.ToString(), "").Replace("\0", "");
            }

            return cfg;
        }

        /// <summary>保存全部连接。</summary>
        public static void SaveAll(ClassIni ini, List<ModbusTcpLinkConfig> configs)
        {
            if (ini == null || configs == null) return;
            foreach (var cfg in configs)
                Save(ini, cfg);
        }

        /// <summary>保存单条连接（先清本连接旧的数据块/相机段，再写当前；段名含 link 后缀，不影响其它连接）。</summary>
        public static void Save(ClassIni ini, ModbusTcpLinkConfig cfg)
        {
            if (ini == null || cfg == null) return;
            string sec = ConnSection(cfg.LinkId);

            ini.WriteString(sec, "name", cfg.Name);
            ini.WriteString(sec, "ip", cfg.Ip);
            ini.WriteString(sec, "port", cfg.Port);
            ini.WriteString(sec, "cell", cfg.Cell);
            ini.WriteString(sec, "abcd", cfg.Abcd);
            ini.WriteString(sec, "qishi", cfg.Qishi.ToString());
            ini.WriteString(sec, "zongchang", cfg.Zongchang.ToString());
            ini.WriteString(sec, "lunxun_time", cfg.LunxunTime.ToString());
            ini.WriteBool(sec, "modbus_en", cfg.ModbusEn);
            ini.WriteBool(sec, "modbus_lunxunen", cfg.ModbusLunxunen);
            ini.WriteString(sec, "geshu", cfg.Geshu.ToString());

            // 数据块：清旧的再写新的
            for (int n = 1; n <= MaxDataBlocks; n++)
            {
                string bsec = BlockSection(cfg.LinkId, n);
                var keys = new StringCollection();
                ini.ReadSection(bsec, keys);
                if (keys.Count > 0) ini.EraseSection(bsec);
            }
            for (int i = 0; i < cfg.DataBlocks.Count && i < MaxDataBlocks; i++)
            {
                var blk = cfg.DataBlocks[i];
                int n = i + 1;
                string bsec = BlockSection(cfg.LinkId, n);
                ini.WriteString(bsec, "name", blk.Name);
                ini.WriteString(bsec, "qishi", blk.Qishi.ToString());
                ini.WriteString(bsec, "changdu", blk.Changdu.ToString());
                ini.WriteString(bsec, "gaodiwei", blk.Gaodiwei);
                ini.WriteString(bsec, "geshi", blk.Geshi);
            }

            // 相机绑定：1..MaxCameras，有则写，无则清
            for (int n = 1; n <= MaxCameras; n++)
            {
                string csec = CameraSection(cfg.LinkId, n);
                var cam = cfg.CameraBindings.Find(c => c.CameraNo == n);
                if (cam == null)
                {
                    var keys = new StringCollection();
                    ini.ReadSection(csec, keys);
                    if (keys.Count > 0) ini.EraseSection(csec);
                    continue;
                }
                ini.WriteString(csec, "chufa", cam.Chufa);
                ini.WriteString(csec, "fanhuizhi", cam.Fanhuizhi);
                ini.WriteBool(csec, "fanhuien", cam.Fanhuien);
                ini.WriteString(csec, "fankui", cam.Fankui);
                ini.WriteString(csec, "chukufangshi", cam.Chukufangshi);
                ini.WriteString(csec, "chufazhi1", cam.Chufazhi1);
                ini.WriteString(csec, "chufazhi2", cam.Chufazhi2);
            }

            // 切型字符 + 方案路径：1..MaxChangeTypes 全覆盖（保证 round-trip 稳定）
            for (int n = 1; n <= MaxChangeTypes; n++)
            {
                ini.WriteString(ChangeSection(cfg.LinkId), n.ToString(),
                    cfg.ChangeTypes.ContainsKey(n) ? (cfg.ChangeTypes[n] ?? "") : "");
                ini.WriteString(PathSection(cfg.LinkId), n.ToString(),
                    cfg.SchemePaths.ContainsKey(n) ? (cfg.SchemePaths[n] ?? "") : "");
            }
        }

        /// <summary>删除一个连接的全部段（连接 1 不允许删除）。仅当该连接段存在时才动手。</summary>
        public static void Delete(ClassIni ini, int linkId)
        {
            if (ini == null || linkId < 2) return;
            var keys = new StringCollection();
            ini.ReadSection(ConnSection(linkId), keys);
            if (keys.Count == 0) return;

            ini.EraseSection(ConnSection(linkId));
            for (int n = 1; n <= MaxDataBlocks; n++) ini.EraseSection(BlockSection(linkId, n));
            for (int n = 1; n <= MaxCameras; n++) ini.EraseSection(CameraSection(linkId, n));
            ini.EraseSection(ChangeSection(linkId));
            ini.EraseSection(PathSection(linkId));
        }

        /// <summary>
        /// ★第33轮复审建议①：本 Store 的读取直连 LinkContext / 轮询线程（连接 2~4 不经三协议窗体的启动读回段），
        /// 钳位必须落在这里——手改 ini 的 changdu=10⁹ 会让该连接轮询线程每圈做 10⁹ 次网络读
        /// （≥2³¹ 时连 int.Parse 都抛 OverflowException = 异常风暴），比启动阶段假死更隐蔽；qishi≥2³¹ 同型。
        /// 与 CommGridHelper.ReadIniDecimalBounded 同口径；本处无日志句柄，log 传 null = 静默钳位
        /// （连接 1 由窗体读回段记日志）。
        /// </summary>
        private static decimal ReadDecimalBounded(ClassIni ini, string sec, string key, decimal def, decimal min, decimal max)
        {
            decimal v = CommGridHelper.ReadIniDecimalBounded(ini.ReadString(sec, key, def.ToString()), def, min, max, sec + "/" + key, null);
            // 地址/长度/间隔本就只该是整数，且下游轮询用 int.Parse(本值的字符串)——留小数（手改 2.5）会每圈抛 FormatException
            return Math.Truncate(v);
        }

        /// <summary>★第33轮复审建议①：块个数同样钳到 MaxDataBlocks——手改 geshu=2000000000 原会让
        /// Load 的 1..Geshu 循环构造 20 亿个块配置对象（加载即假死/OOM）。</summary>
        private static int ReadGeshuBounded(ClassIni ini, string sec)
        {
            return CommGridHelper.ReadIniInt(ini.ReadString(sec, "geshu", "0"), 0, 0, MaxDataBlocks, sec + "/geshu", null);
        }
    }
}
