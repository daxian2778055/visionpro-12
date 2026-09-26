using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WindowsFormsApplication1.Tests
{
    /// <summary>
    /// PendingCameraTriggers 通讯触发队列（★第43轮"集成层确定性子集"之一）。
    /// 覆盖现场最难台架复现的语义：在途帧不许被驱逐、失败即撤、FIFO 取用、
    /// 发送锁串行（Clear 不能在 send 中途抢先清队）。全纯逻辑——无硬件、无网络、无 wall-clock 断言。
    /// </summary>
    [TestClass]
    public class PendingCameraTriggersTests
    {
        private static CameraTriggerRecord Rec(string name)
        {
            return new CameraTriggerRecord(
                new KeyValuePair<string, string>("jieshou", name),
                new CommTriggerSource(1, 0, name));
        }

        [TestMethod]
        public void Execute_发送成功_记录留队且按FIFO取出()
        {
            var q = new PendingCameraTriggers(3);
            var a = Rec("A");
            var b = Rec("B");
            Assert.AreEqual(0, q.Execute(a, () => 0));
            Assert.AreEqual(0, q.Execute(b, () => 0));
            Assert.IsTrue(q.HasPending);
            Assert.AreSame(a, q.Take(), "FIFO：队首必须是最早一笔");
            Assert.AreSame(b, q.Take());
            Assert.IsNull(q.Take());
            Assert.IsFalse(q.HasPending);
        }

        [TestMethod]
        public void Execute_发送失败_记录即撤_返回码原样透传()
        {
            var q = new PendingCameraTriggers(3);
            int ret = q.Execute(Rec("X"), () => -7);
            Assert.AreEqual(-7, ret, "send 返回码必须原样透传给调用方");
            Assert.IsFalse(q.HasPending, "发送失败的记录不得留队（否则回执会对应一个从未上总线的帧）");
            Assert.IsNull(q.Take());
        }

        [TestMethod]
        public void Execute_队满_在调用send之前拒绝_返回负一()
        {
            var q = new PendingCameraTriggers(2);
            int calls = 0;
            Func<int> ok = () => { calls++; return 0; };
            Assert.AreEqual(0, q.Execute(Rec("1"), ok));
            Assert.AreEqual(0, q.Execute(Rec("2"), ok));
            int ret = q.Execute(Rec("3"), ok);
            Assert.AreEqual(-1, ret, "队满返回 -1");
            Assert.AreEqual(2, calls, "队满必须在调用 send 之前拒绝（在途帧不驱逐）");
            Assert.IsTrue(q.HasPending, "队满拒绝不得影响已在队的两笔");
        }

        [TestMethod]
        public void Execute_失败撤队后_不占用容量()
        {
            var q = new PendingCameraTriggers(1);
            Assert.AreEqual(-3, q.Execute(Rec("f"), () => -3));
            Assert.IsFalse(q.HasPending);
            // 容量 1，上一笔失败已撤——下一笔必须能进，不得被"幽灵容量"挡住
            Assert.AreEqual(0, q.Execute(Rec("s"), () => 0));
            Assert.IsTrue(q.HasPending);
        }

        [TestMethod]
        public void Take_空队返回null()
        {
            var q = new PendingCameraTriggers(3);
            Assert.IsNull(q.Take());
            Assert.IsFalse(q.HasPending);
        }

        [TestMethod]
        public void Clear_清空全部在队记录()
        {
            var q = new PendingCameraTriggers(5);
            q.Execute(Rec("A"), () => 0);
            q.Execute(Rec("B"), () => 0);
            Assert.IsTrue(q.HasPending);
            q.Clear();
            Assert.IsFalse(q.HasPending);
            Assert.IsNull(q.Take());
        }

        [TestMethod]
        public void OldestAgeMs_空队负一_有队非负()
        {
            var q = new PendingCameraTriggers(3);
            Assert.AreEqual(-1d, q.OldestAgeMs());
            q.Execute(Rec("A"), () => 0);
            double age = q.OldestAgeMs();
            Assert.IsTrue(age >= 0, "在途记录年龄必须非负，实际 " + age);
            q.Take();
            Assert.AreEqual(-1d, q.OldestAgeMs());
        }

        [TestMethod]
        public void TriggeredAtUtc_构造即落时间戳_位于构造时间窗内()
        {
            DateTime t0 = DateTime.UtcNow;
            var r = Rec("T");
            DateTime t1 = DateTime.UtcNow;
            Assert.IsTrue(r.TriggeredAtUtc >= t0 && r.TriggeredAtUtc <= t1,
                "触发时间戳 " + r.TriggeredAtUtc.ToString("O") + " 应落在 [" + t0.ToString("O") + ", " + t1.ToString("O") + "] 内");
        }

        [TestMethod]
        public void Execute_并发发送被_sendLock_串行()
        {
            var q = new PendingCameraTriggers(8);
            int active = 0;
            int max = 0;
            Func<int> send = () =>
            {
                int cur = Interlocked.Increment(ref active);
                int prev;
                while (cur > (prev = max))
                {
                    if (Interlocked.CompareExchange(ref max, cur, prev) == prev) break;
                }
                Thread.Sleep(60);
                Interlocked.Decrement(ref active);
                return 0;
            };

            int r1 = -99, r2 = -99;
            var t1 = new Thread(() => { r1 = q.Execute(Rec("A"), send); });
            var t2 = new Thread(() => { r2 = q.Execute(Rec("B"), send); });
            t1.Start();
            t2.Start();
            Assert.IsTrue(t1.Join(5000), "线程1 超时未退出");
            Assert.IsTrue(t2.Join(5000), "线程2 超时未退出");
            Assert.AreEqual(0, r1);
            Assert.AreEqual(0, r2);
            Assert.AreEqual(1, max, "send 必须被 _sendLock 串行（并发度峰值应为 1），实际 " + max);
            Assert.IsTrue(q.HasPending, "两笔成功发送都应留队");
        }

        [TestMethod]
        public void Clear_不与在途send并发完成_锁序保证()
        {
            // 设计约束回归：Clear 与 Execute 共用 _sendLock——
            // 清队不得把一帧"上总线到一半"的记录从队里抽走（否则回执错位）。
            var q = new PendingCameraTriggers(3);
            var sendEntered = new ManualResetEventSlim(false);
            var releaseSend = new ManualResetEventSlim(false);
            var clearFinished = new ManualResetEventSlim(false);

            var sender = new Thread(() =>
            {
                q.Execute(Rec("S"), () =>
                {
                    sendEntered.Set();
                    releaseSend.Wait(5000);
                    return 0;
                });
            });
            sender.Start();
            Assert.IsTrue(sendEntered.Wait(3000), "send 未在 3 秒内进入");

            var clearer = new Thread(() =>
            {
                q.Clear();
                clearFinished.Set();
            });
            clearer.Start();

            Thread.Sleep(150);
            Assert.IsFalse(clearFinished.IsSet,
                "Clear 不得在在途 send 期间完成（_sendLock 必须把它挡在外面）");

            releaseSend.Set();
            Assert.IsTrue(sender.Join(3000), "发送线程超时未退出");
            Assert.IsTrue(clearer.Join(3000), "清队线程超时未退出");
            Assert.IsTrue(clearFinished.IsSet);
            Assert.IsFalse(q.HasPending, "Clear 完成后队列应为空");
        }
    }
}
