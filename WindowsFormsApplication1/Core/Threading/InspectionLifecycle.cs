using System;
using System.Diagnostics;
using System.Threading;

namespace WindowsFormsApplication1.Core.Threading
{
    // Covers the entire transaction: preparing inputs, running tools and snapshotting outputs.
    public sealed class InspectionLifecycle
    {
        private readonly object _sync = new object();
        private bool _paused;
        private int _active;
        public bool TryEnter()
        {
            lock (_sync)
            {
                if (_paused) return false;
                _active++;
                return true;
            }
        }
        public void Exit()
        {
            lock (_sync)
            {
                _active--;
                if (_active == 0) Monitor.PulseAll(_sync);
            }
        }
        public void StopAccepting() { lock (_sync) _paused = true; }
        public void Resume() { lock (_sync) _paused = false; }
        public bool WaitForIdle(int timeoutMs)
        {
            var clock = Stopwatch.StartNew();
            lock (_sync)
            {
                while (_active != 0)
                {
                    int remaining = timeoutMs - (int)clock.ElapsedMilliseconds;
                    if (remaining <= 0) return false;
                    Monitor.Wait(_sync, remaining);
                }
                return true;
            }
        }
    }
}
