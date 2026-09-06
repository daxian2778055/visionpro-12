using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 通讯服务。集中持有 4 类 PLC 通讯客户端（Modbus-TCP / Modbus-RTU / Omron / 三菱 FX 串口），
    /// 由组合根以单例创建并注入 Form1，收口通讯控件的所有权。
    /// <para>
    /// 说明：Form1 直接调用本服务暴露的客户端极少（仅 Show / xie / StopPolling 等），
    /// 真正的 PLC 读写逻辑经作业系统（myjob.block.Outputs["modbustcp"] 等）按名称路由到对应客户端，
    /// 属于更大的 JobService 职责范围，留待后续增量抽取。
    /// </para>
    /// </summary>
    public sealed class CommunicationService
    {
        public FormModbus Modbustcp { get; }
        public FormModbusRtu ModbusRtu { get; }
        public FormOmron Omron { get; }
        public FormMelsecSerial Melsec { get; }

        /// <summary>
        /// 通讯连接注册表（多实例改造 阶段2）。
        /// 后续所有连接实例都登记在这里，触发路由与反馈回写按 LinkId 查找目标连接。
        /// </summary>
        public CommLinkManager Links { get; }

        public CommunicationService()
        {
            Modbustcp = new FormModbus();
            ModbusRtu = new FormModbusRtu();
            Omron = new FormOmron();
            Melsec = new FormMelsecSerial();

            Links = new CommLinkManager();
            // 阶段2：把现有唯一的 Modbus TCP 连接登记进管理器。
            // 只是“纳入统一管理”，不创建第二个连接，因此行为与改造前完全一致。
            Links.Register(Modbustcp.Link);
        }
    }
}
