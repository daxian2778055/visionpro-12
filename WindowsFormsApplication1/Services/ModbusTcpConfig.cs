using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus TCP 数据块配置（对应 ini [N_modbustcp] / [N_modbustcpN]）。
    /// 一个数据块 = 一组连续保持寄存器地址（起始 + 长度 + 高低位 + 格式）。
    /// </summary>
    public class ModbusTcpDataBlockConfig
    {
        public int Index;
        public string Name = "";
        public decimal Qishi;
        public decimal Changdu;
        public string Gaodiwei = "";
        public string Geshi = "";
    }

    /// <summary>
    /// Modbus TCP 相机绑定配置（对应 ini [cN_modbustcp] / [cN_modbustcpN]）。
    /// 把一台相机绑定到某个触发字符 / 反馈值 / 出库方式，由所属连接统一触发与回写。
    /// </summary>
    public class ModbusTcpCameraBindingConfig
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
    /// Modbus TCP 单条连接的完整配置（阶段 2-A 纯数据模型，不含运行逻辑）。
    ///
    /// 命名规则（与总纲第 4 节一致）：
    ///   LinkId=1  → 既有 [modbustcp] 段（零迁移，不破坏现有 test.ini）
    ///   LinkId=N  → [modbustcpN] 及其 [N_modbustcpN] / [cN_modbustcpN] / [change_modbustcpN] / [path_modbustcpN]
    ///
    /// 该模型只描述"配置长什么样"，读写由 ModbusTcpIniStore 负责。
    /// </summary>
    public class ModbusTcpLinkConfig
    {
        public int LinkId = 1;
        public string Name = "";

        // ===== 连接参数（[modbustcp] 段） =====
        public string Ip = "127.0.0.1";
        public string Port = "9600";
        public string Cell = "0";
        public string Abcd = "CDAB";
        public decimal Qishi;
        public decimal Zongchang = 1;
        public decimal LunxunTime = 20;
        public bool ModbusEn;
        public bool ModbusLunxunen;
        public int Geshu;

        // ===== 数据块（[N_modbustcp]） =====
        public List<ModbusTcpDataBlockConfig> DataBlocks = new List<ModbusTcpDataBlockConfig>();

        // ===== 相机绑定（[cN_modbustcp]） =====
        public List<ModbusTcpCameraBindingConfig> CameraBindings = new List<ModbusTcpCameraBindingConfig>();

        // ===== 切型字符 / 方案路径（[change_modbustcp] / [path_modbustcp]，键 1..12） =====
        public Dictionary<int, string> ChangeTypes = new Dictionary<int, string>();
        public Dictionary<int, string> SchemePaths = new Dictionary<int, string>();
    }
}
