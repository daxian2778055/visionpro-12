using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus RTU 数据块配置（对应 ini [Nmodbusrtu] / [NmodbusrtuN]）。
    /// 一个数据块 = 一组连续保持寄存器地址（起始 + 长度 + 高低位 + 格式）。
    /// </summary>
    public class ModbusRtuDataBlockConfig
    {
        public int Index;
        public string Name = "";
        public decimal Qishi;
        public decimal Changdu;
        public string Gaodiwei = "";
        public string Geshi = "";
    }

    /// <summary>
    /// Modbus RTU 相机绑定配置（对应 ini [cNmodbusrtu] / [cNmodbusrtuN]）。
    /// 把一台相机绑定到某个触发字符 / 反馈值 / 出库方式，由所属连接统一触发与回写。
    /// </summary>
    public class ModbusRtuCameraBindingConfig
    {
        public int CameraNo;
        public string Chufa = "";
        public string Fanhuizhi = "";
        public bool Fanhuien;
        public string Fankui = "";
        public string Chukufangshi = "";
        public string Chufazhi1 = "";
        public string Chufazhi2 = "";
    }

    /// <summary>
    /// Modbus RTU 单条连接的完整配置（阶段 4-A 纯数据模型，不含运行逻辑）。
    ///
    /// 命名规则（与总纲第 4 节一致）：
    ///   LinkId=1  → 既有 [modbusrtu] 段（零迁移，不破坏现有 test.ini：
    ///                数据块 [Nmodbusrtu]、相机 [cNmodbusrtu]、参数键含 PortName/baudRate/dataBits/stopBits/Parity/station）
    ///   LinkId=N  → [modbusrtuN] 及其 [NmodbusrtuN] / [cNmodbusrtuN] / [change_modbusrtuN] / [path_modbusrtuN]
    ///
    /// 与 ModbusTcpLinkConfig 的差异仅在“连接参数”：TCP 用 Ip/Port，RTU 用串口参数集合。
    /// </summary>
    public class ModbusRtuLinkConfig
    {
        public int LinkId = 1;
        public string Name = "";

        // ===== 连接参数（[modbusrtu] 段，串口 + 帧格式） =====
        public string PortName = "COM3";
        public string BaudRate = "9600";
        public string DataBits = "8";
        public string StopBits = "1";
        public string Parity = "None";
        public string Station = "1";
        public string Abcd = "CDAB";
        public decimal Qishi;
        public decimal Zongchang = 1;
        public decimal LunxunTime = 20;
        public bool ModbusEn;
        public bool ModbusLunxunen;
        public int Geshu;

        // ===== 数据块（[Nmodbusrtu]） =====
        public List<ModbusRtuDataBlockConfig> DataBlocks = new List<ModbusRtuDataBlockConfig>();

        // ===== 相机绑定（[cNmodbusrtu]） =====
        public List<ModbusRtuCameraBindingConfig> CameraBindings = new List<ModbusRtuCameraBindingConfig>();

        // ===== 切型字符 / 方案路径（[change_modbusrtuN] / [path_modbusrtuN]，键 1..12） =====
        public Dictionary<int, string> ChangeTypes = new Dictionary<int, string>();
        public Dictionary<int, string> SchemePaths = new Dictionary<int, string>();
    }
}
