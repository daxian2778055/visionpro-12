using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 轮询线程写表格：合并单元格更新后异步刷新 UI，不阻塞通讯读循环。
    /// </summary>
    public sealed class CommGridUiSink
    {
        private readonly DataGridView _dgv;
        private readonly Dictionary<long, object> _pending = new Dictionary<long, object>();
        private int _invokeScheduled;

        public CommGridUiSink(DataGridView dgv)
        {
            _dgv = dgv ?? throw new ArgumentNullException(nameof(dgv));
        }

        public void Queue(int col, int row, object value)
        {
            if (_dgv.IsDisposed) return;
            long key = ((long)row << 32) | (uint)col;
            lock (_pending)
            {
                _pending[key] = value;
            }
            ScheduleFlush();
        }

        private void ScheduleFlush()
        {
            if (Interlocked.CompareExchange(ref _invokeScheduled, 1, 0) != 0)
                return;
            try
            {
                if (_dgv.IsDisposed)
                {
                    Interlocked.Exchange(ref _invokeScheduled, 0);
                    return;
                }
                if (_dgv.InvokeRequired)
                {
                    if (_dgv.IsHandleCreated)
                        _dgv.BeginInvoke(new Action(FlushPending));
                    else
                        Interlocked.Exchange(ref _invokeScheduled, 0);
                }
                else
                    FlushPending();
            }
            catch
            {
                Interlocked.Exchange(ref _invokeScheduled, 0);
            }
        }

        private void FlushPending()
        {
            Interlocked.Exchange(ref _invokeScheduled, 0);
            if (_dgv.IsDisposed) return;

            Dictionary<long, object> batch;
            lock (_pending)
            {
                if (_pending.Count == 0) return;
                batch = new Dictionary<long, object>(_pending);
                _pending.Clear();
            }

            _dgv.SuspendLayout();
            try
            {
                foreach (var kv in batch)
                {
                    int row = (int)(kv.Key >> 32);
                    int col = (int)(kv.Key & 0xFFFFFFFF);
                    if (row >= 0 && row < _dgv.RowCount && col >= 0 && col < _dgv.ColumnCount)
                        _dgv[col, row].Value = kv.Value;
                }
            }
            finally
            {
                _dgv.ResumeLayout(false);
            }

            lock (_pending)
            {
                if (_pending.Count > 0)
                    ScheduleFlush();
            }
        }
    }
}
