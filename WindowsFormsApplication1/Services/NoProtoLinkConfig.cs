using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 无协议链路类型（阶段 5 激活）：与 Form3 窗体的三通道一一对应。
    /// </summary>
    public enum NoProtoKind
    {
        Serial = 0,
        TcpClient = 1,
        TcpServer = 2
    }

    /// <summary>
    /// 一条无协议链路（[noprotoN] 段）的连接配置。
    /// 与窗体 UI 的连接 1 无关：连接 1 仍由界面 checkbox + serial/tcpclient/tcpserver 段驱动，行为不变。
    /// 额外链路 N(=1..) 独立一条数据通路，与窗体共享同一套切型与 getData 管线。
    /// </summary>
    public class NoProtoLinkConfig
    {
        public int LinkId = 1;            // ini [noprotoN] 的 N
        public NoProtoKind Kind = NoProtoKind.Serial;   // 段键 kind: Serial / Tcp_client / Tcp_server
        public string Name = "";          // 链路描述（用于日志）
        public bool Enabled = true;       // 段键 en

        // 串口参数（kind=Serial 用）
        public string PortName = "";
        public int BaudRate = 9600;
        public int DataBits = 8;
        public int StopBits = 1;          // 0=None 1=One 2=Two（与 Form3 strStopBits 一致）
        public int Jiaoyan = 0;           // 0=None 1=Odd 2=Even

        // TCP 参数（kind=Tcp_client / Tcp_server 用）
        public string Ip = "";
        public int Port = 0;
    }
}
