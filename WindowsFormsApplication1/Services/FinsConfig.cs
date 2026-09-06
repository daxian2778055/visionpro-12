using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// FINS 数据块配置（对应 ini [N] / [N_finsN]）。
    /// 一个数据块 = 一组连续 D 区地址（起始 + 长度 + 高低位 + 格式）。
    /// </summary>
    public class FinsDataBlockConfig
    {
        public int Index;
        public string Name = "";
        public decimal Qishi;
        public decimal Changdu;
        public string Gaodiwei = "";
        public string Geshi = "";
    }

    /// <summary>
    /// FINS 相机绑定配置（对应 ini [cN] / [cN_finsN]）。
    /// 把一台相机绑定到某个触发字符 / 反馈值 / 出库方式，由所属连接统一触发与回写。
    /// </summary>
    public class FinsCameraBindingConfig
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
    /// FINS 单条连接完整配置（阶段 3-A 纯数据模型，不含运行逻辑）。
    ///
    /// 命名规则（与总纲第 4 节一致，连接 1 空后缀零迁移）：
    ///   LinkId=1  → 既有 [fins] 段
    ///   LinkId=N  → [finsN] 及其 [N_finsN] / [cN_finsN] / [change_finsN] / [path_finsN]
    ///
    /// 实测差异（2026-09-04 核对 FormOmron.cs FormSiemens_Load）：
    ///   连接 1 的数据块段是纯数字 [N]（无后缀）；切型/路径段是 [change]/[path]（无 fins 后缀，与“无协议”共用同名段）。
    ///   连接 2+ 才追加 _finsN 后缀，避免多实例互相覆盖。
    /// </summary>
    public class FinsLinkConfig
    {
        public int LinkId = 1;
        public string Name = "";

        // ===== 连接参数（[fins] 段） =====
        public string Ip = "127.0.0.1";
        public string Port = "9600";
        public string Cell = "0";          // Unit Num
        public string Local = "192";       // PC Net Num
        public string Abcd = "CDAB";
        public decimal Qishi;
        public decimal Zongchang = 1;
        public decimal LunxunTime = 20;
        public bool FinsEn;
        public bool FinsLunxunen;
        public int Geshu;

        // ===== 数据块（[N] / [N_finsN]） =====
        public List<FinsDataBlockConfig> DataBlocks = new List<FinsDataBlockConfig>();

        // ===== 相机绑定（[cN] / [cN_finsN]） =====
        public List<FinsCameraBindingConfig> CameraBindings = new List<FinsCameraBindingConfig>();

        // ===== 切型字符 / 方案路径（[change]/[change_finsN]、[path]/[path_finsN]，键 1..12） =====
        public Dictionary<int, string> ChangeTypes = new Dictionary<int, string>();
        public Dictionary<int, string> SchemePaths = new Dictionary<int, string>();
    }
}
