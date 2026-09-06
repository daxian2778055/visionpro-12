using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 通讯连接统一抽象（多实例改造 阶段2）。
    ///
    /// 目的：目前每种协议的连接都“焊死”在自己的窗体里（一个窗体 = 一个连接），
    ///       无法再开第二个。定义本接口后，所有协议（Modbus TCP / Modbus RTU / FINS /
    ///       无协议串口·TCP）都实现它，由 CommLinkManager 统一登记与调度，
    ///       上层只面向 ICommLink 编程，就能同时管理多个连接实例。
    ///
    /// 说明：各协议的建链参数差异较大（TCP 要 IP/端口，串口要 COM/波特率），
    ///       因此本接口只收敛“与协议无关”的生命周期与身份信息；
    ///       建链参数由各实现类自己以强类型属性提供。
    /// </summary>
    public interface ICommLink
    {
        /// <summary>连接标识，全局唯一；后续触发路由与反馈回写按此查找目标连接。</summary>
        int LinkId { get; set; }

        /// <summary>连接名称，界面上用于区分多个连接（如 "1号线PLC"、"2号线PLC"）。</summary>
        string Name { get; set; }

        /// <summary>协议类型标识，如 "modbustcp" / "modbusrtu" / "fins" / "raw"。</summary>
        string Protocol { get; }

        /// <summary>当前是否已连接。</summary>
        bool IsConnected { get; }

        /// <summary>断开连接。</summary>
        void Close();

        /// <summary>断线重连，返回是否成功。</summary>
        bool Reconnect();
    }
}
