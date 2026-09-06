using System;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.ModBus;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus TCP 连接封装（多实例改造 阶段1：所有权收口）。
    ///
    /// 改造前：ModbusTcpNet 客户端是 FormModbus 的一个裸字段，与窗体生命周期绑死，
    ///         一个窗体只能有一个连接，无法再连第二个服务器。
    /// 改造后：把“连接参数 + 建链 / 断链 / 重连”收口到本类。
    ///         一个 ModbusTcpLink 实例 = 一个 Modbus TCP 连接，可被 new 出多份。
    ///
    /// 本阶段只搬移代码，行为与原实现完全一致，不改变任何读写逻辑。
    /// 后续阶段再把读写与轮询也收口进来，并由 CommLinkManager 统一调度多个实例。
    /// </summary>
    public class ModbusTcpLink : ICommLink
    {
        public ModbusTcpLink()
        {
            LinkId = 1;
            Name = "ModbusTCP-1";
        }

        /// <summary>协议类型标识（ICommLink）。</summary>
        public string Protocol { get { return "modbustcp"; } }

        /// <summary>连接标识，多实例路由时使用（阶段3启用）。</summary>
        public int LinkId { get; set; }

        /// <summary>连接名称，便于界面区分多个连接。</summary>
        public string Name { get; set; }

        // ===== 连接参数（后续由配置按实例加载） =====
        public string Ip { get; set; }
        public int Port { get; set; }
        public byte Station { get; set; }
        public bool AddressStartWithZero { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IsStringReverse { get; set; }

        /// <summary>
        /// 底层客户端。阶段1暂时直接暴露，使原有数十处读写代码零改动即可继续工作；
        /// 后续阶段会收口为方法，届时本属性改为 private。
        /// </summary>
        public ModbusTcpNet Client { get; set; }

        public bool IsConnected { get { return Client != null; } }

        /// <summary>
        /// 用当前参数创建客户端并连接。等价于原 button1_Click 中的建链逻辑。
        /// dataFormat 传 null 表示不设置（保持底层默认值），
        /// 与原先“下拉框未选中 0~3 时保持原值不变”的行为一致。
        /// </summary>
        public OperateResult Connect(DataFormat? dataFormat)
        {
            try
            {
                Close();
                Client = new ModbusTcpNet(Ip, Port, Station);
                Client.AddressStartWithZero = AddressStartWithZero;
                Client.SetLoginAccount(UserName, Password);
                if (dataFormat.HasValue)
                    Client.DataFormat = dataFormat.Value;
                Client.IsStringReverse = IsStringReverse;
                Client.ConnectTimeOut = 3000;   // 2026-09-06：3 秒超时，防止同步建链阻塞 UI
                return Client.ConnectServer();
            }
            catch (Exception ex)
            {
                return new OperateResult(ex.Message);
            }
        }

        /// <summary>2026-09-06：将同步建链移到线程池执行，避免阻塞 UI 线程。</summary>
        public async Task<OperateResult> ConnectAsync(DataFormat? dataFormat)
        {
            return await Task.Run(() => Connect(dataFormat));
        }

        /// <summary>断开连接。原实现直接调 ConnectClose，此处补了空引用保护，其余等价。</summary>
        public void Close()
        {
            if (Client == null) return;
            try { Client.ConnectClose(); }
            catch { }
        }

        /// <summary>重连：先关再连。等价于原 PerformReconnectCore。</summary>
        public bool Reconnect()
        {
            if (Client == null) return false;
            try { Client.ConnectClose(); }
            catch { }
            Client.ConnectTimeOut = 3000;   // 2026-09-06：统一 3 秒超时
            try { return Client.ConnectServer().IsSuccess; }
            catch { return false; }
        }
    }
}
