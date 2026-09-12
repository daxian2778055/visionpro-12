using System;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Profinet.Omron;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// FINS (Omron) 连接封装（多实例改造 阶段 3-B：所有权收口）。
    ///
    /// 改造前：OmronFinsNet 客户端是 FormOmron 的一个裸字段，与窗体生命周期绑死，
    ///         一个窗体只能有一个连接，无法再连第二个设备。
    /// 改造后：把“连接参数 + 建链 / 断链 / 重连”收口到本类。
    ///         一个 FinsLink 实例 = 一个 FINS 连接，可被 new 出多份。
    ///
    /// 本阶段只搬移代码，行为与原 FormOmron.button1_Click 的建链逻辑完全一致。
    /// </summary>
    public class FinsLink : ICommLink
    {
        public FinsLink()
        {
            LinkId = 1;
            Name = "FINS-1";
        }

        /// <summary>协议类型标识（ICommLink）。</summary>
        public string Protocol { get { return "fins"; } }

        /// <summary>连接标识，多实例路由时使用。</summary>
        public int LinkId { get; set; }

        /// <summary>连接名称，便于界面区分多个连接。</summary>
        public string Name { get; set; }

        // ===== 连接参数（对应原窗体 textBox1/ip、textBox2/port、textBox15/local(SA1)、textBox16/cell(DA2)） =====
        public string Ip { get; set; }
        public int Port { get; set; }
        public byte SA1 { get; set; }   // PC Net Num（原 textBox15）
        public byte DA2 { get; set; }   // Unit Num（原 textBox16）
        public DataFormat? DataFormat { get; set; }

        /// <summary>
        /// 底层客户端。阶段 1 暂时直接暴露，使原 Fins_duxie / xie 等数十处读写零改动；
        /// 原窗体字段 omronFinsNet 可赋值给本属性，运行时即复用同一连接对象。
        /// </summary>
        public OmronFinsNet Client { get; set; }

        /// <summary>建链超时（毫秒）。默认 3000，防止不可达/半开网关时同步阻塞卡死调用线程。</summary>
        public int ConnectTimeOutMs { get; set; } = 3000;

        // ★A1 修复：以 ConnectServer 的真实结果为准（旧实现 "Client != null" 会把建链失败误判为已连接）。
        private volatile bool _connected;

        public bool IsConnected { get { return _connected; } }

        /// <summary>
        /// 用当前参数创建客户端并连接。等价于原 button1_Click 中的建链逻辑。
        /// dataFormat 传 null 表示不设置（保持底层默认值）。
        /// </summary>
        public OperateResult Connect(DataFormat? dataFormat = null)
        {
            try
            {
                Close();
                ReleaseClient();          // ★B2：丢弃旧客户端，避免反复建链泄漏
                Client = new OmronFinsNet();
                Client.IpAddress = Ip;
                Client.Port = Port;
                Client.SA1 = SA1;
                Client.DA2 = DA2;
                Client.ConnectTimeOut = ConnectTimeOutMs;   // ★ 2026-09-11：设置建链超时，避免网关卡顿时长时间挂起
                if (dataFormat.HasValue)
                    Client.ByteTransform.DataFormat = dataFormat.Value;
                OperateResult result = Client.ConnectServer();
                _connected = result.IsSuccess;
                return result;
            }
            catch (Exception ex)
            {
                _connected = false;
                return new OperateResult(ex.Message);
            }
        }

        /// <summary>异步建链（后台线程执行，不阻塞调用线程）。返回 Task 结果，成功与否见 IsSuccess。</summary>
        public System.Threading.Tasks.Task<OperateResult> ConnectAsync(DataFormat? dataFormat = null)
        {
            return System.Threading.Tasks.Task.Run(() => Connect(dataFormat));
        }

        /// <summary>断开连接。</summary>
        public void Close()
        {
            _connected = false;
            if (Client == null) return;
            try { Client.ConnectClose(); }
            catch { }
        }

        /// <summary>★B2：释放并丢弃旧客户端（避免 FormOmron 构造时预建 / 反复建链造成的实例泄漏）。</summary>
        private void ReleaseClient()
        {
            OmronFinsNet old = Client;
            Client = null;
            if (old == null) return;
            try { old.ConnectClose(); } catch { }
            var disposable = old as IDisposable;
            if (disposable != null) { try { disposable.Dispose(); } catch { } }
        }

        /// <summary>重连：先关再连。</summary>
        public bool Reconnect()
        {
            if (Client == null) { _connected = false; return false; }
            try { Client.ConnectClose(); }
            catch { }
            try
            {
                bool ok = Client.ConnectServer().IsSuccess;
                _connected = ok;
                return ok;
            }
            catch { _connected = false; return false; }
        }
    }
}
