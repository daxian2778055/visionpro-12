using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Ports;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// COM 口互斥防呆（阶段 7 补充）：Modbus-RTU 与无协议串口“全连接统一纳入”一套进程内占用表。
    ///
    /// 背景：连接 1 主窗、连接 2~4 整窗（Modbus-RTU / 无协议）各自在 Load/自动重连/手动连接时会去打开
    /// 各自的串口；若两台设备被误配到同一 COM（例如管理器添加时都默认 COM3），第二个打开方会拿到
    /// “端口被占用”的 OS 异常并反复失败。本表在每个连接真正 Open 前登记（TryAcquire），
    /// 冲突时给出“被谁占用”的明确提示而不去打开，避免重复失败重试。
    ///
    /// 本类同时提供 ini 层面的配置冲突扫描与空闲 COM 推荐，供“连接设备”管理器在添加/保存时做配置期防呆。
    /// </summary>
    public static class SerialPortGuard
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, string> _ownerByPort =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>是否 COM 串口名（形如 COM3 / com10）。非串口（如空串/TCP 地址）不参与互斥。</summary>
        public static bool IsComPort(string port)
        {
            string p = (port ?? "").Trim();
            if (p.Length < 4 || !p.StartsWith("COM", StringComparison.OrdinalIgnoreCase)) return false;
            int n;
            return int.TryParse(p.Substring(3), out n) && n >= 0;
        }

        /// <summary>登记占用。占用方已占用返回 false 并给出当前占用方；同占用方重复登记视为幂等成功。</summary>
        public static bool TryAcquire(string port, string owner, out string occupiedBy)
        {
            string p = (port ?? "").Trim();
            occupiedBy = null;
            if (!IsComPort(p) || string.IsNullOrEmpty(owner)) return true;   // 非串口/无属主不纳入互斥
            lock (_lock)
            {
                if (_ownerByPort.TryGetValue(p, out occupiedBy))
                    return occupiedBy == owner;                              // 自己已占用视为成功
                _ownerByPort[p] = owner;
                return true;
            }
        }

        /// <summary>释放占用（仅当属主一致，避免误删他人登记）。</summary>
        public static void Release(string port, string owner)
        {
            string p = (port ?? "").Trim();
            if (!IsComPort(p) || string.IsNullOrEmpty(owner)) return;
            lock (_lock)
            {
                string cur;
                if (_ownerByPort.TryGetValue(p, out cur) && cur == owner)
                    _ownerByPort.Remove(p);
            }
        }

        public static string GetOwner(string port)
        {
            string p = (port ?? "").Trim();
            lock (_lock)
            {
                string cur;
                return _ownerByPort.TryGetValue(p, out cur) ? cur : "";
            }
        }

        /// <summary>占用冲突时的明确提示文案。</summary>
        public static string OccupiedMessage(string port, string occupiedBy)
        {
            return "串口 " + (port ?? "").Trim() + " 已被[" + occupiedBy + "]占用（COM 互斥防呆），请先关闭占用方或改用其它串口。";
        }

        /// <summary>
        /// 配置期扫描：把 test.ini 中所有“已启用串口连接”的占用关系汇总为 COM→属主描述。
        /// 覆盖：Modbus-RTU 连接 1..MaxLinks（[modbusrtu][/N]，仅当段存在且 modbusrtu_en=true 的串口项）；
        ///       无协议连接 2..MaxLinks（[noprotoN]，kind=Serial 且 en=true）。
        /// 注意：Form3 原窗（无协议连接 1）串口由界面 checkbox 驱动，配置不落 [noprotoN]，运行时以实际
        ///       TryAcquire 为准，此处不扫描。
        /// </summary>
        public static Dictionary<string, string> ScanIniOwners(demo.ClassIni ini)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (ini == null || string.IsNullOrEmpty(ini.FileName)) return map;

            // Modbus-RTU：段存在才纳入（Load 对空段也会吐默认值，不能盲扫）
            for (int link = 1; link <= ModbusRtuIniStore.MaxLinks; link++)
            {
                var keys = new StringCollection();
                ini.ReadSection(ModbusRtuIniStore.ConnSection(link), keys);
                if (keys.Count == 0) continue;
                var cfg = ModbusRtuIniStore.Load(ini, link);
                if (cfg == null || !cfg.ModbusEn || !IsComPort(cfg.PortName)) continue;
                map[cfg.PortName.Trim()] = "Modbus-RTU 连接" + link;
            }

            // 无协议额外链路（LoadAll 只返回已启用且 kind 有效的段）
            foreach (var cfg in NoProtoIniStore.LoadAll(ini))
            {
                if (cfg.Kind != NoProtoKind.Serial || !IsComPort(cfg.PortName)) continue;
                map[cfg.PortName.Trim()] = "无协议连接" + cfg.LinkId;
            }
            return map;
        }

        /// <summary>在既有占用集合里找一个能落脚的默认串口：优先系统实际枚举出的空闲口，其次数值递增避开已占用。</summary>
        public static string PickFreeComPort(IEnumerable<string> usedPorts)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (usedPorts != null)
                foreach (var p in usedPorts)
                    if (!string.IsNullOrEmpty(p)) used.Add(p.Trim());

            try
            {
                foreach (var sys in SerialPort.GetPortNames())
                    if (!used.Contains(sys.Trim()))
                        return sys.Trim();
            }
            catch { }

            for (int i = 3; i < 256; i++)
            {
                string c = "COM" + i;
                if (!used.Contains(c)) return c;
            }
            return "COM3";
        }

        /// <summary>在 ini 扫描结果中做排除自身的冲突检测，供“保存/添加”时给出明确提示。</summary>
        public static string FindConfigConflict(demo.ClassIni ini, string selfOwner, string port, out string occupiedBy)
        {
            occupiedBy = null;
            string p = (port ?? "").Trim();
            if (!IsComPort(p)) return null;
            var map = ScanIniOwners(ini);
            string cur;
            if (map.TryGetValue(p, out cur) && cur != selfOwner)
            {
                occupiedBy = cur;
                return OccupiedMessage(p, cur);
            }
            return null;
        }
    }
}
