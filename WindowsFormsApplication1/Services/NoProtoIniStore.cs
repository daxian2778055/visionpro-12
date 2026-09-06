using System;
using System.Collections.Generic;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 无协议额外链路配置存储（阶段 5 激活）。
    /// 从 test.ini 的 [noprotoN] 段读取（N=1..32，可支持上限 4 及更多），每段描述一条独立数据链路：
    ///     [noproto2]
    ///     kind=Serial | TcpClient | TcpServer（保存按枚举名写盘；手工按旧格式 Tcp_client/Tcp_server 写的段因解析失败会被安全跳过）
    ///     en=true|false
    ///     ; kind=Serial 时：
    ///     portName=COM3
    ///     baudRate=9600
    ///     dataBits=8
    ///     stopBits=1
    ///     jiaoyan=0
    ///     ; kind=Tcp_client 时：
    ///     ip=192.168.0.10
    ///     port=5020
    ///     ; kind=Tcp_server 时：
    ///     port=9001
    ///
    /// 段不存在 / kind 无效 / en=false 的段一律跳过（当前 test.ini 无此段故休眠，行为与改造前完全一致）。
    /// 窗体 UI 的连接 1 不经过这里（仍由 serial/tcpclient/tcpserver 既有段 + checkbox 驱动）。
    /// </summary>
    public static class NoProtoIniStore
    {
        /// <summary>“连接设备”管理器可管理的连接数上限（连接 1=Form3 原窗 UI，连接 2..MaxLinks 走 ini 段）。</summary>
        public const int MaxLinks = 4;

        /// <summary>ini 扫描上限：LoadAll 仍扫描到该号，保留手工写入 5..32 段的能力（历史行为）。</summary>
        private const int MaxLink = 32;

        public static string ConnSection(int link) => "noproto" + link;

        /// <summary>加载全部已启用（en=true 且 kind 有效）的额外链路。</summary>
        public static List<NoProtoLinkConfig> LoadAll(ClassIni wdini)
        {
            var list = new List<NoProtoLinkConfig>();
            if (wdini == null || string.IsNullOrEmpty(wdini.FileName))
                return list;

            for (int i = 1; i <= MaxLink; i++)
            {
                var cfg = Load(wdini, i);
                if (cfg == null || !cfg.Enabled) continue;
                list.Add(cfg);
            }
            return list;
        }

        /// <summary>加载单条链路配置。段不存在或无有效 kind 返回 null；en=false 时返回配置但 Enabled=false（供管理器停用该连接）。</summary>
        public static NoProtoLinkConfig Load(ClassIni ini, int linkId)
        {
            if (ini == null || string.IsNullOrEmpty(ini.FileName) || linkId < 1) return null;

            string sec = ConnSection(linkId);
            string kindText = ini.ReadString(sec, "kind", "");
            NoProtoKind kind;
            if (!Enum.TryParse(kindText, true, out kind))
                return null;                                      // 段不存在或无有效 kind

            var cfg = new NoProtoLinkConfig
            {
                LinkId = linkId,
                Kind = kind,
                Enabled = ini.ReadString(sec, "en", "true").Equals("true", StringComparison.OrdinalIgnoreCase),
                PortName = ini.ReadString(sec, "portName", ""),
                BaudRate = ini.ReadInteger(sec, "baudRate", 9600),
                DataBits = ini.ReadInteger(sec, "dataBits", 8),
                StopBits = ini.ReadInteger(sec, "stopBits", 1),
                Jiaoyan = ini.ReadInteger(sec, "jiaoyan", 0),
                Ip = ini.ReadString(sec, "ip", ""),
                Port = ini.ReadInteger(sec, "port", 0)
            };

            switch (cfg.Kind)
            {
                case NoProtoKind.Serial:
                    cfg.Name = "串口" + cfg.PortName;
                    break;
                case NoProtoKind.TcpClient:
                    cfg.Name = "TCP客户机 " + cfg.Ip + ":" + cfg.Port;
                    break;
                case NoProtoKind.TcpServer:
                    cfg.Name = "TCP监听端口" + cfg.Port;
                    break;
            }
            return cfg;
        }

        /// <summary>保存单条链路配置（写 [noprotoN] 段；kind 按枚举名写 Serial/TcpClient/TcpServer，en 写 true/false）。</summary>
        public static void Save(ClassIni ini, NoProtoLinkConfig cfg)
        {
            if (ini == null || cfg == null || cfg.LinkId < 1) return;
            string sec = ConnSection(cfg.LinkId);

            ini.WriteString(sec, "kind", cfg.Kind.ToString());
            ini.WriteString(sec, "en", cfg.Enabled ? "true" : "false");
            ini.WriteString(sec, "portName", cfg.PortName ?? "");
            ini.WriteString(sec, "baudRate", cfg.BaudRate.ToString());
            ini.WriteString(sec, "dataBits", cfg.DataBits.ToString());
            ini.WriteString(sec, "stopBits", cfg.StopBits.ToString());
            ini.WriteString(sec, "jiaoyan", cfg.Jiaoyan.ToString());
            ini.WriteString(sec, "ip", cfg.Ip ?? "");
            ini.WriteString(sec, "port", cfg.Port.ToString());
        }

        /// <summary>删除一条链路配置段（连接 1 为 Form3 原窗 UI 通道，不允许删除）。</summary>
        public static void Delete(ClassIni ini, int linkId)
        {
            if (ini == null || linkId < 2) return;
            string sec = ConnSection(linkId);
            var keys = new System.Collections.Specialized.StringCollection();
            ini.ReadSection(sec, keys);
            if (keys.Count > 0)
                ini.EraseSection(sec);
        }
    }
}
