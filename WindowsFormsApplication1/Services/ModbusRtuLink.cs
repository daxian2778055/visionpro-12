using System;
using System.IO.Ports;
using System.Threading.Tasks;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.ModBus;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus RTU 连接封装（多实例改造 阶段 4-B：所有权收口）。
    ///
    /// 改造前：ModbusRtu 客户端是 FormModbusRtu 的一个裸字段，与窗体生命周期绑死，
    ///         一个窗体只能持有一个 COM 口连接。
    /// 改造后：把“串口参数 + 开串口 / 关串口 / 重开”收口到本类。
    ///         一个 ModbusRtuLink 实例 = 一个 COM 口连接，可被 new 出多份（多 COM 并联）。
    ///
    /// 连接参数（PortName/BaudRate/DataBits/StopBits/ParityName/Station/AddressStartWithZero/IsStringReverse）
    /// 在建链前由调用方赋值；DataFormat 走 Connect 参数（与窗体 comboBox2 的 abcd 选择一致）。
    /// </summary>
    public class ModbusRtuLink : ICommLink
    {
        public ModbusRtuLink()
        {
            LinkId = 1;
            Name = "ModbusRTU-1";
        }

        /// <summary>协议类型标识（ICommLink）。</summary>
        public string Protocol { get { return "modbusrtu"; } }

        /// <summary>连接标识，多实例路由时使用。</summary>
        public int LinkId { get; set; }

        /// <summary>连接名称，便于界面区分多个连接。</summary>
        public string Name { get; set; }

        /// <summary>COM 互斥防呆登记用的属主描述（用于“被谁占用”提示）。</summary>
        public string OwnerDesc { get { return "Modbus-RTU 连接" + LinkId; } }

        // ===== 串口连接参数 =====
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        /// <summary>停止位：0=None, 1=One, 2=Two（与窗体 textBox17 约定一致）。</summary>
        public int StopBitsValue { get; set; }
        /// <summary>校验文本："None"/"Odd"/"Even"（与窗体 comboBox1 项一致）。</summary>
        public string ParityName { get; set; }
        public byte Station { get; set; }
        public bool AddressStartWithZero { get; set; }
        public bool IsStringReverse { get; set; }

        /// <summary>底层客户端（收口为属性，原窗体 busRtuClient 字段经代理访问）。</summary>
        public ModbusRtu Client { get; set; }

        // ★P6：串口无法实时探活，但至少要反映"已成功打开过且未断开"。
        // 旧实现 "Client != null" 在 Close() 之后仍返回 true（Connect 是 Open() 成功后才赋 Client，
        // 所以并不存在"建链失败被误判为已连接"的问题，只是断开后状态不更新）。
        private volatile bool _connected;

        public bool IsConnected { get { return _connected; } }

        // ★W5 配套（2026-09-23 第23轮）：本实例当前在 SerialPortGuard 实际登记占用的端口名。
        //   Close/失败回滚原先按"当前 PortName 字段"释放——手动重连换口时字段已先被覆盖成 NEW，
        //   旧口的登记永远还回去（Release 错键=空操作），导致旧口被自己钉死、别的连接也抢不到。
        //   现登记/释放统一以 _guardPort 为准。
        private string _guardPort;

        /// <summary>
        /// 用当前串口参数创建客户端并打开串口。等价于原 button1_Click 中的建链逻辑。
        /// dataFormat 传 null 表示不设置（保持底层默认值）。
        /// </summary>
        public OperateResult Connect(DataFormat? dataFormat)
        {
            try
            {
                Close();
                ReleaseClient();   // ★B2对称(同 FinsLink)：丢弃旧客户端实例，避免反复建链泄漏
                // COM 互斥防呆：先登记占用，被别的连接占用则直接给出明确提示，不再去打开串口反复失败
                if (SerialPortGuard.IsComPort(PortName))
                {
                    string busy;
                    if (!SerialPortGuard.TryAcquire(PortName, OwnerDesc, out busy))
                        return new OperateResult(SerialPortGuard.OccupiedMessage(PortName, busy));
                    _guardPort = PortName;   // ★W5：登记成功才记账，释放按此键
                }
                var rtu = new ModbusRtu(Station);
                rtu.AddressStartWithZero = AddressStartWithZero;
                rtu.IsStringReverse = IsStringReverse;
                if (dataFormat.HasValue)
                    rtu.DataFormat = dataFormat.Value;
                rtu.SerialPortInni(sp =>
                {
                    sp.PortName = PortName;
                    sp.BaudRate = BaudRate;
                    sp.DataBits = DataBits;
                    sp.StopBits = StopBitsValue == 0 ? System.IO.Ports.StopBits.None
                               : (StopBitsValue == 1 ? System.IO.Ports.StopBits.One
                                                     : System.IO.Ports.StopBits.Two);
                    sp.Parity = ParityName == "Odd" ? System.IO.Ports.Parity.Odd
                              : (ParityName == "Even" ? System.IO.Ports.Parity.Even
                                                      : System.IO.Ports.Parity.None);
                });
                rtu.Open();
                Client = rtu;
                _connected = true;
                return OperateResult.CreateSuccessResult();
            }
            catch (Exception ex)
            {
                _connected = false;
                if (SerialPortGuard.IsComPort(_guardPort))
                    SerialPortGuard.Release(_guardPort, OwnerDesc);   // ★W5：按实际登记键释放
                _guardPort = null;
                return new OperateResult(ex.Message);
            }
        }

        /// <summary>2026-09-06：将同步建链移到线程池执行，避免阻塞 UI 线程。</summary>
        public async Task<OperateResult> ConnectAsync(DataFormat? dataFormat)
        {
            return await Task.Run(() => Connect(dataFormat));
        }

        /// <summary>关闭串口。原实现直接调 Close，此处补了空引用保护并释放 COM 互斥登记，其余等价。</summary>
        public void Close()
        {
            _connected = false;   // ★P6：断开后不再显示"已连接"
            if (Client == null) return;
            try { Client.Close(); }
            catch { }
            if (SerialPortGuard.IsComPort(_guardPort))
                SerialPortGuard.Release(_guardPort, OwnerDesc);   // ★W5：按实际登记键释放，不受 PortName 被改写影响
            _guardPort = null;
        }

        /// <summary>★B2对称：释放并丢弃旧客户端（Close 只关串口，实例仍被 Client 引用到下次赋值）。</summary>
        private void ReleaseClient()
        {
            ModbusRtu old = Client;
            Client = null;
            if (old == null) return;
            try { old.Close(); } catch { }
            var disposable = old as IDisposable;
            if (disposable != null) { try { disposable.Dispose(); } catch { } }
        }

        /// <summary>重开串口：先关再开（重新确认占用登记）。等价于原 PerformReconnectCore 思路。</summary>
        public bool Reconnect()
        {
            if (Client == null) { _connected = false; return false; }
            string p = (PortName ?? "").Trim();
            if (SerialPortGuard.IsComPort(p))
            {
                string busy;
                if (!SerialPortGuard.TryAcquire(p, OwnerDesc, out busy))
                {
                    _connected = false;
                    return false;   // 串口已被其它连接占用，本次重开不执行
                }
                _guardPort = p;   // ★W5：同步账本，后续 Close/释放按登记键走
            }
            try { Client.Close(); }
            catch { }
            try
            {
                Client.Open();
                _connected = true;
                return true;
            }
            catch
            {
                _connected = false;
                if (SerialPortGuard.IsComPort(_guardPort))
                    SerialPortGuard.Release(_guardPort, OwnerDesc);   // ★W5：按登记键释放
                _guardPort = null;
                return false;
            }
        }
    }
}
