using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus RTU 多连接配置读写（阶段 4-A：只读写 ini，不接管任何运行逻辑）。
    ///
    /// 把 test.ini 中 N 条 Modbus RTU 连接的全部段加载/保存成 ModbusRtuLinkConfig 列表。
    /// 命名规则（与总纲第 4 节一致）：连接 1 空后缀（[modbusrtu]），连接 N 追加数字。
    ///
    ///   [modbusrtu]             / [modbusrtuN]         连接参数（PortName/baudRate/dataBits/stopBits/Parity/station/abcd/轮询...）
    ///   [Nmodbusrtu]            / [NmodbusrtuN]        数据块（name/qishi/changdu/gaodiwei/geshi）
    ///   [cNmodbusrtu]           / [cNmodbusrtuN]       相机绑定（chufa/fanhuizhi/fanhuien/fankui/chukufangshi/chufazhi1/2）
    ///   [changemodbusrtuN]      / [pathmodbusrtuN]     切型字符 / 方案路径（键 1..12；段名与 FormModbusRtu 窗体一致，连接1 无后缀）
    /// </summary>
    public static class ModbusRtuIniStore
    {
        public const int MaxLinks = 4;          // 每协议最多连接数（与总纲决策一致）
        public const int MaxDataBlocks = 10;    // 数据块上限（与既有逻辑一致）
        public const int MaxCameras = 13;       // 相机绑定上限（1..13）
        public const int MaxChangeTypes = 12;   // 切型/方案条目上限（1..12）

        // 连接 1 沿用既有段名（无下划线，与 FormModbusRtu 原始 Load 一致）；连接 N 追加数字。
        internal static string ConnSection(int link) => link == 1 ? "modbusrtu" : "modbusrtu" + link;
        internal static string BlockSection(int link, int n) => n + "modbusrtu" + (link == 1 ? "" : link.ToString());
        internal static string CameraSection(int link, int n) => "c" + n + "modbusrtu" + (link == 1 ? "" : link.ToString());
        internal static string ChangeSection(int link) => "change" + "modbusrtu" + (link == 1 ? "" : link.ToString());
        internal static string PathSection(int link) => "path" + "modbusrtu" + (link == 1 ? "" : link.ToString());

        /// <summary>加载全部连接（连接 1 始终存在；连接 2..MaxLinks 仅当对应段非空时才纳入）。</summary>
        public static List<ModbusRtuLinkConfig> LoadAll(ClassIni ini)
        {
            if (ini == null || string.IsNullOrEmpty(ini.FileName))
                return new List<ModbusRtuLinkConfig>();

            var list = new List<ModbusRtuLinkConfig>();
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
        public static ModbusRtuLinkConfig Load(ClassIni ini, int link)
        {
            string sec = ConnSection(link);
            var cfg = new ModbusRtuLinkConfig
            {
                LinkId = link,
                Name = ini.ReadString(sec, "name", "ModbusRTU-" + link),
                PortName = ini.ReadString(sec, "PortName", "COM3").Replace("\0", ""),
                BaudRate = ini.ReadString(sec, "baudRate", "9600").Replace("\0", ""),
                DataBits = ini.ReadString(sec, "dataBits", "8").Replace("\0", ""),
                StopBits = ini.ReadString(sec, "stopBits", "1").Replace("\0", ""),
                Parity = ini.ReadString(sec, "Parity", "None").Replace("\0", ""),
                Station = ini.ReadString(sec, "station", "1").Replace("\0", ""),
                Abcd = ini.ReadString(sec, "abcd", "CDAB").Replace("\0", ""),
                Qishi = ReadDecimal(ini, sec, "qishi", 0),
                Zongchang = ReadDecimal(ini, sec, "zongchang", 1),
                LunxunTime = ReadDecimal(ini, sec, "lunxun_time", 20),
                ModbusEn = ini.ReadBool(sec, "modbusrtu_en", false),
                ModbusLunxunen = ini.ReadBool(sec, "modbusrtu_lunxunen", false),
                Geshu = ini.ReadInteger(sec, "geshu", 0),
            };

            // 数据块：1..Geshu
            for (int n = 1; n <= cfg.Geshu; n++)
            {
                string bsec = BlockSection(link, n);
                cfg.DataBlocks.Add(new ModbusRtuDataBlockConfig
                {
                    Index = n,
                    Name = ini.ReadString(bsec, "name", "").Replace("\0", ""),
                    Qishi = ReadDecimal(ini, bsec, "qishi", 0),
                    Changdu = ReadDecimal(ini, bsec, "changdu", 0),
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
                cfg.CameraBindings.Add(new ModbusRtuCameraBindingConfig
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
        public static void SaveAll(ClassIni ini, List<ModbusRtuLinkConfig> configs)
        {
            if (ini == null || configs == null) return;
            foreach (var cfg in configs)
                Save(ini, cfg);
        }

        /// <summary>保存单条连接（先清本连接旧的数据块/相机段，再写当前；段名含 link 后缀，不影响其它连接）。</summary>
        public static void Save(ClassIni ini, ModbusRtuLinkConfig cfg)
        {
            if (ini == null || cfg == null) return;
            string sec = ConnSection(cfg.LinkId);

            ini.WriteString(sec, "name", cfg.Name);
            ini.WriteString(sec, "PortName", cfg.PortName);
            ini.WriteString(sec, "baudRate", cfg.BaudRate);
            ini.WriteString(sec, "dataBits", cfg.DataBits);
            ini.WriteString(sec, "stopBits", cfg.StopBits);
            ini.WriteString(sec, "Parity", cfg.Parity);
            ini.WriteString(sec, "station", cfg.Station);
            ini.WriteString(sec, "abcd", cfg.Abcd);
            ini.WriteString(sec, "qishi", cfg.Qishi.ToString());
            ini.WriteString(sec, "zongchang", cfg.Zongchang.ToString());
            ini.WriteString(sec, "lunxun_time", cfg.LunxunTime.ToString());
            ini.WriteBool(sec, "modbusrtu_en", cfg.ModbusEn);
            ini.WriteBool(sec, "modbusrtu_lunxunen", cfg.ModbusLunxunen);
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

        private static decimal ReadDecimal(ClassIni ini, string sec, string key, decimal def)
        {
            string s = ini.ReadString(sec, key, def.ToString()).Replace("\0", "");
            decimal v;
            return decimal.TryParse(s, out v) ? v : def;
        }
    }
}
