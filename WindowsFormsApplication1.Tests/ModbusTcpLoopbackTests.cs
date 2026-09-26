using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// ★第43轮"集成层确定性子集"之二：Modbus TCP 本地回环。
    /// 127.0.0.1 自建极简 FC3 从站（0x10 内核请求循环，不做任何 wall-clock 断言），
    /// 跑通 ModbusTcpLink.Connect → ModbusTcpRuntime.PollLoop → Hsl 解析 → DemoUtils 渲染 →
    /// ICommLinkContext.UpdatePollCell 的完整链——这条链以前只有台架才能验到。
    /// 断言口径：寄存器原值必须以 "[addr] 值" 形态出现在对应行单元；健康回环零日志、零触发回调、
    /// Stop 后更新计数冻结。
    /// </summary>
    [TestClass]
    public class ModbusTcpLoopbackTests
    {
        private MiniModbusServer _server;
        private ModbusTcpLink _link;
        private ModbusTcpRuntime _runtime;
        private FakeLinkContext _ctx;

        [TestCleanup]
        public void Cleanup()
        {
            try { if (_runtime != null) _runtime.Stop(); } catch { }
            try { if (_link != null) _link.Close(); } catch { }
            try { if (_server != null) _server.Dispose(); } catch { }
        }

        [TestMethod]
        public void 回环轮询_寄存器原值经渲染进入UpdatePollCell_停止后冻结()
        {
            // ---- 布置从站与寄存器池（含负值，验证符号位解析） ----
            _server = new MiniModbusServer();
            _server.Start();
            _server.WriteRegister(0, 111);
            _server.WriteRegister(1, -22);
            _server.WriteRegister(2, 30000);
            _server.WriteRegister(3, -30000);

            // ---- 建链 ----
            _link = new ModbusTcpLink
            {
                Ip = "127.0.0.1",
                Port = _server.Port,
                Station = 1,
                AddressStartWithZero = true,
                // Hsl 7.0.1：SetLoginAccount(null,…) 会在 NetworkDoubleBase 内 NRE（探针实证），
                // 生产链同理依赖 ini 填充值非 null——空串是安全下界
                UserName = "",
                Password = "",
            };
            var conn = _link.Connect(null);
            Assert.IsTrue(conn.IsSuccess, "回环建链失败：" + conn.Message);

            // ---- 上下文：一块 4 寄存器的 "int" 数据块（非"触发"块 → 不进相机分支） ----
            _ctx = new FakeLinkContext();
            _ctx.FinsBlocks["blk0"] = new[] { "0", "0", "4", "数据", "int" };

            _runtime = new ModbusTcpRuntime(_link, new FakeRuntimeHost());
            _runtime.Context = _ctx;
            _runtime.Start();

            // ---- 观察：行值必须出现（轮询 50ms，给足 8 秒宽限；纯"等到出现"无时序断言） ----
            string[] expect = { "111", "-22", "30000", "-30000" };
            DateTime deadline = DateTime.UtcNow.AddSeconds(8);
            bool ok = false;
            while (DateTime.UtcNow < deadline)
            {
                ok = true;
                for (int row = 0; row < expect.Length; row++)
                {
                    string cell;
                    if (!_ctx.LastCell.TryGetValue(row, out cell) || !cell.Contains(expect[row]))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok) break;
                Thread.Sleep(50);
            }
            Assert.IsTrue(ok, "8 秒内未观察到全部回环轮询值；实测：" + _ctx.Dump());
            Assert.IsTrue(_ctx.Updates > 0, "应已有轮询更新");

            // ---- 健康回环的四类副作用必须全部为零 ----
            Assert.AreEqual(0, _ctx.Logs.Count, "健康回环不应产生日志：" + string.Join(" | ", _ctx.Logs));
            Assert.AreEqual(0, _ctx.SelectionChanges, "数据块非「触发」，不得触发选择变更事件");
            Assert.AreEqual(0, _ctx.Reconnects, "读取健康，不得发起重连");
            Assert.AreEqual(0, _ctx.FeedbackWrites, "数据块非「触发」，不得回写反馈");
            foreach (var kv in _ctx.LastCell)
            {
                Assert.IsFalse(kv.Value.Contains("Read Failed"),
                    "行 " + kv.Key + " 出现读失败文案：" + kv.Value);
            }

            // ---- Stop 后轮询更新必须冻结（Stop 内部 Join 线程，本地回环不可能阻塞超时） ----
            _runtime.Stop();
            int snap = _ctx.Updates;
            Thread.Sleep(400);
            Assert.AreEqual(snap, _ctx.Updates, "Stop 之后仍在产生新更新（轮询线程未真正退出）");
        }

        #region ---- 微型 Modbus TCP 从站（仅 FC3，足够承载本测试的读路径） ----

        private sealed class MiniModbusServer : IDisposable
        {
            private TcpListener _listener;
            private Thread _acceptThread;
            private volatile bool _stop;
            private readonly List<Socket> _clients = new List<Socket>();
            private readonly ushort[] _regs = new ushort[128];

            public int Port { get; private set; }

            public void Start()
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
                _acceptThread.Start();
            }

            /// <summary>测试侧播寄存器（高位在前，Modbus 标准字节序）。读发生在测试主线程播种之后，无并发写。</summary>
            public void WriteRegister(int addr, short value)
            {
                _regs[addr] = unchecked((ushort)value);
            }

            private void AcceptLoop()
            {
                while (!_stop)
                {
                    Socket c;
                    try { c = _listener.AcceptSocket(); }
                    catch { break; }
                    lock (_clients) _clients.Add(c);
                    var t = new Thread(() => Serve(c)) { IsBackground = true };
                    t.Start();
                }
            }

            private void Serve(Socket c)
            {
                var buf = new byte[1024];
                try
                {
                    while (!_stop)
                    {
                        if (!ReadExact(c, buf, 0, 7)) break;           // MBAP 头（7 字节已含 unit）
                        int bodyLen = (buf[4] << 8) | buf[5];          // Length 计数含 unit —— 头里已消费 1 字节
                        if (bodyLen < 2 || bodyLen > 1000) break;
                        if (!ReadExact(c, buf, 7, bodyLen - 1)) break; // 余下 PDU（func + data）
                        Handle(c, buf);
                    }
                }
                catch { }
                try { c.Close(); } catch { }
            }

            private void Handle(Socket c, byte[] buf)
            {
                ushort tid = (ushort)((buf[0] << 8) | buf[1]);
                byte uid = buf[6];
                byte func = buf[7];

                byte[] resp;                        // unit + PDU
                if (func == 3)
                {
                    int start = (buf[8] << 8) | buf[9];
                    int qty = (buf[10] << 8) | buf[11];
                    if (qty < 1 || qty > 125)
                    {
                        resp = new byte[] { uid, 0x83, 3 };
                    }
                    else
                    {
                        resp = new byte[3 + qty * 2];
                        resp[0] = uid;
                        resp[1] = func;
                        resp[2] = (byte)(qty * 2);
                        for (int i = 0; i < qty; i++)
                        {
                            ushort v = (start + i >= 0 && start + i < _regs.Length) ? _regs[start + i] : (ushort)0;
                            resp[3 + i * 2] = (byte)(v >> 8);
                            resp[4 + i * 2] = (byte)(v & 0xFF);
                        }
                    }
                }
                else
                {
                    resp = new byte[] { uid, (byte)(func | 0x80), 1 };  // illegal function
                }

                var pkt = new byte[6 + resp.Length];
                pkt[0] = (byte)(tid >> 8);
                pkt[1] = (byte)(tid & 0xFF);
                pkt[2] = 0;
                pkt[3] = 0;
                pkt[4] = (byte)(resp.Length >> 8);
                pkt[5] = (byte)(resp.Length & 0xFF);
                Array.Copy(resp, 0, pkt, 6, resp.Length);
                c.Send(pkt);
            }

            private static bool ReadExact(Socket s, byte[] buf, int off, int need)
            {
                int got = 0;
                while (got < need)
                {
                    int n;
                    try { n = s.Receive(buf, off + got, need - got, SocketFlags.None); }
                    catch { return false; }
                    if (n <= 0) return false;
                    got += n;
                }
                return true;
            }

            public void Dispose()
            {
                _stop = true;
                try { _listener.Stop(); } catch { }
                lock (_clients)
                {
                    foreach (var c in _clients) { try { c.Close(); } catch { } }
                    _clients.Clear();
                }
            }
        }

        #endregion

        #region ---- 假上下文/宿主（ICommLinkContext 全成员可观测） ----

        private sealed class FakeRuntimeHost : ICommRuntimeHost
        {
            public bool StopRequested { get; set; }
            public Thread PollThread { get; set; }
            public void RunPollLoop() { }
        }

        private sealed class FakeLinkContext : ICommLinkContext
        {
            public readonly ConcurrentDictionary<int, string> LastCell = new ConcurrentDictionary<int, string>();
            public readonly ConcurrentQueue<string> Logs = new ConcurrentQueue<string>();
            public int Updates;
            public int SelectionChanges;
            public int Reconnects;
            public int FeedbackWrites;

            public bool IsInitialized { get { return true; } }
            public int PollInterval { get { return 50; } }
            public bool IsPollEnabled { get { return true; } }
            public bool IsCommEnabled { get { return true; } }
            public bool IsReconnecting { get { return false; } }
            public int AddressBase { get { return 0; } }
            public Dictionary<string, string[]> FinsBlocks { get; } = new Dictionary<string, string[]>();
            public Dictionary<int, string[]> CameraBindings { get; } = new Dictionary<int, string[]>();
            public string SchemePath { get { return null; } }

            public void Log(string message) { Logs.Enqueue(message); }

            public void UpdatePollCell(int row, string value)
            {
                LastCell[row] = value;
                Interlocked.Increment(ref Updates);
            }

            public void RaiseSelectionChanged(string dataVal, string camKey, string third, int linkId)
            {
                Interlocked.Increment(ref SelectionChanges);
            }

            public void WriteTriggerFeedback(string[] block, string value, ref int xuanzhong, ref string fins)
            {
                Interlocked.Increment(ref FeedbackWrites);
            }

            public bool TrySchemeSwitch(string dataVal) { return false; }

            public string MiddleValue(string str, string sta, string end)
            {
                if (str == null) return null;
                int i = str.IndexOf(sta, StringComparison.Ordinal);
                if (i < 0) return str;
                int s = i + sta.Length;
                int j = str.IndexOf(end, s, StringComparison.Ordinal);
                if (j < 0) return str.Substring(s);
                return str.Substring(s, j - s);
            }

            public void OnReconnect() { Interlocked.Increment(ref Reconnects); }

            public string Dump()
            {
                var parts = new List<string>();
                foreach (var kv in LastCell) parts.Add(kv.Key + "=>" + kv.Value.Trim().Replace("\r", "").Replace("\n", ""));
                return parts.Count == 0 ? "(无)" : string.Join(", ", parts);
            }
        }

        #endregion
    }
}
