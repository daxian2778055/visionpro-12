using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ★第60轮：NG 图窗体内预览。
    ///
    /// 替换原 chatu_fangfa 的「Process.Start(图) + 立刻 Kill」组合 —— 那套的毛病：
    ///   · 用 ShellExecute 拉起系统默认看图程序；单实例看图器（照片查看器等）会复用已开窗口，
    ///     Kill 直接误伤用户正开着的图，甚至关掉整个看图程序；
    ///   · 启动失败时 Process.Start 返回 null，myProc.Kill() 抛 NRE 被空 catch 吞掉；
    ///   · 打开又立刻杀，图片实际来不及渲染 —— 这个功能此前等于没有。
    ///
    /// 现在改为在本软件窗体里显示：不启动任何外部进程；同一时刻只保留一个预览窗，
    /// 再次预览时复用它换图；图片按字节读入后从内存解码，不长期占用文件句柄。
    /// 线程约定：ShowPreview 可从任意线程调用（内部 Invoke 到 UI 线程）。
    /// </summary>
    internal sealed class NgImagePreviewForm : Form
    {
        private static NgImagePreviewForm _current;

        private readonly PictureBox _pic;
        private readonly Label _info;
        private Image _image;

        private NgImagePreviewForm()
        {
            Text = "NG 图预览";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(960, 720);
            MinimumSize = new Size(360, 240);
            BackColor = Color.FromArgb(28, 28, 28);
            KeyPreview = true;

            _pic = new PictureBox();
            _pic.Dock = DockStyle.Fill;
            _pic.SizeMode = PictureBoxSizeMode.Zoom;
            _pic.BackColor = Color.FromArgb(18, 18, 18);

            _info = new Label();
            _info.Dock = DockStyle.Bottom;
            _info.Height = 26;
            _info.ForeColor = Color.Gainsboro;
            _info.TextAlign = ContentAlignment.MiddleLeft;
            _info.Padding = new Padding(8, 0, 8, 0);
            _info.AutoEllipsis = true;

            Controls.Add(_pic);
            Controls.Add(_info);

            KeyDown += NgImagePreviewForm_KeyDown;
            FormClosed += NgImagePreviewForm_FormClosed;
        }

        private void NgImagePreviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        private void NgImagePreviewForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try { DropImage(); } catch { }
        }

        private void DropImage()
        {
            Image old = _image;
            _image = null;
            _pic.Image = null;
            if (old != null) { try { old.Dispose(); } catch { } }
        }

        /// <summary>
        /// 从后台线程或 UI 线程发起的预览请求入口。
        /// owner 传主窗体（它是 Control，可拿 Invoke/InvokeRequired）。
        /// </summary>
        public static void ShowPreview(IWin32Window owner, string path, string caption)
        {
            Control ctl = owner as Control;
            if (ctl != null && ctl.InvokeRequired)
            {
                try { ctl.BeginInvoke(new Action<IWin32Window, string, string>(ShowPreview), owner, path, caption); }
                catch { }
                return;
            }
            ShowPreviewCore(owner, path, caption);
        }

        private static void ShowPreviewCore(IWin32Window owner, string path, string caption)
        {
            try
            {
                NgImagePreviewForm f = _current;
                if (f == null || f.IsDisposed)
                {
                    f = new NgImagePreviewForm();
                    _current = f;
                }
                f.LoadFrom(path, caption);
                if (!f.Visible) f.Show(owner);
                else f.Activate();
            }
            catch { }
        }

        /// <summary>只跑在 UI 线程。文件不存在/读失败时不弹框，在信息条写明原因。</summary>
        private void LoadFrom(string path, string caption)
        {
            DropImage();

            string head = string.IsNullOrEmpty(caption) ? "NG 图预览" : caption;

            if (string.IsNullOrEmpty(path))
            {
                Text = head;
                _info.Text = "预览失败：未给出图片路径";
                return;
            }

            try
            {
                if (!File.Exists(path))
                {
                    Text = head;
                    _info.Text = "预览失败：文件不存在 —— " + path;
                    return;
                }

                // 先整读进内存再解码：读完即释放文件句柄，现场删除/覆盖该图不被预览窗占住
                byte[] bytes = File.ReadAllBytes(path);
                Bitmap bmp;
                using (MemoryStream ms = new MemoryStream(bytes, false))
                {
                    bmp = new Bitmap(ms);
                }
                _image = bmp;
                _pic.Image = _image;

                Text = head + "  -  " + Path.GetFileName(path);
                _info.Text = head + "   |   " + _image.Width + " x " + _image.Height + "   |   " + path;
            }
            catch (Exception ex)
            {
                Text = head;
                _info.Text = "预览失败：" + ex.Message + " —— " + path;
            }
        }
    }
}
