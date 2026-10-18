using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
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
    ///
    /// 线程约定：<see cref="ShowPreview"/> 可从任意线程调用（内部投递回 UI 线程）；
    /// ★第63轮 D-1 起，**读盘与解码不在 UI 线程**——本机实测一张 1080p 24bpp（6 MB）要 36~63 ms，
    /// 原先全压在 UI 线程，而存图根目录改成可配之后还可能指向网络盘（那种路径下连 File.Exists 都会阻塞）。
    /// 后台解码完成后按序号回投：序号已过期（期间又点了别的图）就丢弃并释放本次位图，防"旧图盖新图"。
    /// </summary>
    internal sealed class NgImagePreviewForm : Form
    {
        private static NgImagePreviewForm _current;

        private readonly PictureBox _pic;
        private readonly Label _info;
        private Image _image;

        // ★第63轮 D-1：预览改到后台线程解码后，必须有这枚序号——多路连点时两次解码的完成顺序
        // 不保证等于点击顺序，回投时对不上号就直接丢弃刚解出来的位图，否则旧图会盖掉新图。
        private int _loadSeq;

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
                // 先显示再发起解码：解码在后台线程，回投用 BeginInvoke，要求句柄已建
                if (!f.Visible) f.Show(owner);
                else f.Activate();
                f.LoadFrom(path, caption);
            }
            catch { }
        }

        /// <summary>★第63轮 D-1：只跑 UI 线程，且**不碰 IO**——本机实测一张 1080p 24bpp（6 MB）
        /// 读盘+解码+拷贝要 36~63 ms，原先全压在 UI 线程；根目录改成可配之后还可能指向网络盘
        /// （那种路径下的 File.ReadAllBytes / File.Exists 能阻塞数秒，且没有取消入口）。
        /// 这里只登记"正在读取"，实际读盘解码交给后台线程，回投时再比对序号。</summary>
        private void LoadFrom(string path, string caption)
        {
            DropImage();

            string head = string.IsNullOrEmpty(caption) ? "NG 图预览" : caption;
            Text = head;

            if (string.IsNullOrEmpty(path))
            {
                _info.Text = "预览失败：未给出图片路径";
                return;
            }

            _info.Text = "正在读取…　" + path;
            int mine = ++_loadSeq;
            try
            {
                Task.Run(() => DecodeInBackground(mine, path, head));
            }
            catch (Exception ex)
            {
                _info.Text = "预览失败：无法发起读取（" + ex.GetType().Name + "）—— " + path;
            }
        }

        /// <summary>后台线程：存在性检查 + 整读 + 解码，做完回投 UI 线程贴图。
        /// 任何失败都只写信息条、不弹框（与改前一致）。</summary>
        private void DecodeInBackground(int seq, string path, string head)
        {
            Bitmap bmp = null;
            string error = null;
            try
            {
                if (!File.Exists(path)) error = "预览失败：文件不存在 —— " + path;
                else
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    using (MemoryStream ms = new MemoryStream(bytes, false))
                    {
                        using (Image fromStream = Image.FromStream(ms))
                        {
                            // GDI+ 要求 Image 生存期内流保持打开（流 Dispose 后 Clone/Save 实测抛
                            // OutOfMemoryException / ExternalException）——趁流还活着做一次完整拷贝，
                            // 之后这张位图与流再无关系，DropImage 只释放拷贝。
                            bmp = new Bitmap(fromStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = "预览失败：" + ex.Message + " —— " + path;
                if (bmp != null) { try { bmp.Dispose(); } catch { } bmp = null; }
            }

            try
            {
                BeginInvoke((Action)(() => ApplyDecoded(seq, path, head, bmp, error)));
            }
            catch
            {
                // 窗已释放/句柄已毁：这次结果没人要了，位图必须在这里释放，否则整张图漏在非托管内存里
                if (bmp != null) { try { bmp.Dispose(); } catch { } }
            }
        }

        /// <summary>UI 线程贴图。序号不是最新（期间又点了别的图）就丢弃这次解码结果，防"旧图盖新图"。</summary>
        private void ApplyDecoded(int seq, string path, string head, Bitmap bmp, string error)
        {
            if (IsDisposed)
            {
                if (bmp != null) { try { bmp.Dispose(); } catch { } }
                return;
            }
            if (seq != _loadSeq)
            {
                if (bmp != null) { try { bmp.Dispose(); } catch { } }
                return;   // 已有更新的预览请求在途/已落地，本次作废
            }

            if (bmp == null)
            {
                _info.Text = error ?? "预览失败：未取到图像 —— " + path;
                return;
            }

            _image = bmp;
            _pic.Image = _image;
            Text = head + "  -  " + Path.GetFileName(path);
            _info.Text = head + "   |   " + _image.Width + " x " + _image.Height + "   |   " + path;
        }
    }
}
