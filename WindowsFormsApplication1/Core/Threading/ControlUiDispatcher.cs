using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1.Core.Threading
{
    /// <summary>
    /// 基于 <see cref="Control"/> 的 UI 派发实现。
    /// </summary>
    public sealed class ControlUiDispatcher : IUiDispatcher
    {
        private readonly Control _control;

        public ControlUiDispatcher(Control control)
        {
            if (control == null) throw new ArgumentNullException("control");
            _control = control;
        }

        public bool InvokeRequired
        {
            get
            {
                try { return _control.InvokeRequired; }
                catch { return false; }
            }
        }

        public void Invoke(Action action)
        {
            if (action == null) return;
            _control.Invoke(action);
        }

        public void BeginInvoke(Action action)
        {
            if (action == null) return;
            _control.BeginInvoke(action);
        }

        public bool TryBeginInvoke(Action action)
        {
            if (action == null) return false;

            try
            {
                if (_control.IsDisposed) return false;
                if (!_control.IsHandleCreated) return false;

                _control.BeginInvoke(action);
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                // 句柄尚未创建完成或正在销毁
                return false;
            }
        }
    }
}
