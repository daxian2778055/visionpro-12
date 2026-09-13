using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public sealed class CameraTriggerRecord
    {
        public readonly KeyValuePair<string, string> Payload;
        public readonly CommTriggerSource Source;
        /// <summary>★ 2026-09-13：触发登记时刻(UTC)，用于「命令成功却不回帧」的超时监测。</summary>
        public readonly DateTime TriggeredAtUtc;
        public CameraTriggerRecord(KeyValuePair<string, string> payload, CommTriggerSource source)
        {
            Payload = payload;
            Source = source;
            TriggeredAtUtc = DateTime.UtcNow;
        }
    }

    // Serialize command submission, but never hold the frame-consumer lock inside the SDK.
    // A synchronous/early callback can consume the record before the command returns.
    public sealed class PendingCameraTriggers
    {
        private readonly object _sendLock = new object();
        private readonly object _sync = new object();
        private readonly LinkedList<CameraTriggerRecord> _pending = new LinkedList<CameraTriggerRecord>();
        private readonly int _capacity;
        public PendingCameraTriggers(int capacity = 3) { _capacity = capacity; }
        public bool HasPending { get { lock (_sync) return _pending.Count > 0; } }

        /// <summary>★ 2026-09-13：队首（最早一笔在途触发）已等待的毫秒数；无在途记录返回 -1。
        /// FIFO 下队首对应最早那笔触发，其超时可判定"命令成功但一直不回帧"。</summary>
        public double OldestAgeMs()
        {
            lock (_sync)
            {
                if (_pending.First == null) return -1;
                return (DateTime.UtcNow - _pending.First.Value.TriggeredAtUtc).TotalMilliseconds;
            }
        }

        public int Execute(CameraTriggerRecord record, Func<int> send)
        {
            lock (_sendLock)
            {
                LinkedListNode<CameraTriggerRecord> node;
                lock (_sync)
                {
                    // Never evict a record whose frame is still in flight.
                    if (_pending.Count >= _capacity) return -1;
                    node = _pending.AddLast(record);
                }
                bool succeeded = false;
                try
                {
                    int result = send();
                    succeeded = result == 0;
                    return result;
                }
                finally
                {
                    if (!succeeded)
                        lock (_sync)
                            if (node.List != null) _pending.Remove(node);
                }
            }
        }

        public CameraTriggerRecord Take()
        {
            lock (_sync)
            {
                if (_pending.First == null) return null;
                var record = _pending.First.Value;
                _pending.RemoveFirst();
                return record;
            }
        }

        public void Clear()
        {
            lock (_sendLock)
                lock (_sync) _pending.Clear();
        }
    }
}
