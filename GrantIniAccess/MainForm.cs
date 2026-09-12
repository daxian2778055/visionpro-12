using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrantIniAccess
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateModeUi();
            RefreshAdminStatus();
        }

        #region 状态 / UI 切换

        private void RefreshAdminStatus()
        {
            bool isAdmin = PermissionHelper.IsAdministrator();
            lblAdminStatus.ForeColor = isAdmin ? Color.DarkGreen : Color.DarkRed;
            lblAdminStatus.Text = isAdmin
                ? "当前状态：管理员权限（可开通）"
                : "当前状态：普通用户（需管理员权限才能开通，请点击右上角按钮重启）";
            btnRestartAdmin.Visible = !isAdmin;
            SetButtonsEnabled(true);
        }

        private void UpdateModeUi()
        {
            bool isFileMode = rbFile.Checked;
            bool isDirMode = rbDir.Checked;
            bool isSpecificMode = rbSpecific.Checked;

            // 文件开通：路径+浏览
            lblFilePath.Enabled = isFileMode;
            txtFilePath.Enabled = isFileMode;
            btnBrowseFile.Enabled = isFileMode;
            // 目录开通
            lblDirPath.Enabled = isDirMode;
            txtDirPath.Enabled = isDirMode;
            btnBrowseDir.Enabled = isDirMode;
            // 自定义文件
            lblSpecific.Enabled = isSpecificMode;
            txtSpecific.Enabled = isSpecificMode;
            btnBrowseSpecific.Enabled = isSpecificMode;

            // 更新提示
            // 目标是什么文件名并不重要：本工具不识别、也不关心文件内容，
            // 只做一件事——把选中的文件/目录的读写权限打开。
            if (isFileMode)
                lblHint.Text = "选一个文件：开通它以及它所在目录的读写权限。";
            else if (isDirMode)
                lblHint.Text = "开通所选目录及全部子项。想给整个安装目录/工程目录开放时选这个。";
            else
                lblHint.Text = "直接指定任意一个文件（可浏览选择）：开通它以及它所在目录。";
        }

        #endregion

        #region 模式切换 / 浏览 / 拖放

        private void rbMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateModeUi();
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择要开通权限的 ini 文件";
                dlg.Filter = "ini 文件 (*.ini)|*.ini|所有文件 (*.*)|*.*";
                if (!string.IsNullOrWhiteSpace(txtFilePath.Text) && File.Exists(txtFilePath.Text))
                    dlg.FileName = txtFilePath.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtFilePath.Text = dlg.FileName;
            }
        }

        private void btnBrowseDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择要开通权限的目录";
                dlg.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(txtDirPath.Text) && Directory.Exists(txtDirPath.Text))
                    dlg.SelectedPath = txtDirPath.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtDirPath.Text = dlg.SelectedPath;
            }
        }

        private void btnBrowseSpecific_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "选择具体的 ini 文件（将连同所在目录一起开通）";
                dlg.Filter = "ini 文件 (*.ini)|*.ini|所有文件 (*.*)|*.*";
                if (!string.IsNullOrWhiteSpace(txtSpecific.Text) && File.Exists(txtSpecific.Text))
                    dlg.FileName = txtSpecific.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtSpecific.Text = dlg.FileName;
            }
        }

        /// <summary>★ 2026-09-07 拖放支持：把文件夹或文件拖入窗体即可。</summary>
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null) return;
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            string first = files[0];
            if (Directory.Exists(first))
            {
                rbDir.Checked = true;
                txtDirPath.Text = first;
            }
            else if (File.Exists(first))
            {
                // 是默认 test.ini 则走文件快捷；否则一律作为任意文件处理（不再有"应用程序目录"）
                if (string.Equals(first, @"C:\Program Files\test.ini", StringComparison.OrdinalIgnoreCase))
                {
                    rbFile.Checked = true;
                    txtFilePath.Text = first;
                }
                else
                {
                    rbSpecific.Checked = true;
                    txtSpecific.Text = first;
                }
            }

            UpdateModeUi();
        }

        #endregion

        #region 按钮：管理员自提升 / 诊断 / 写测试 / 开通

        private void btnRestartAdmin_Click(object sender, EventArgs e)
        {
            try
            {
                PermissionHelper.RestartAsAdministrator();
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "无法以管理员身份启动:\r\n" + ex.Message, "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private PermissionHelper.AccessResult DiagnoseCurrentTarget()
        {
            string path = GetCurrentTargetPath();
            if (string.IsNullOrEmpty(path))
                return new PermissionHelper.AccessResult
                {
                    Issue = PermissionHelper.AccessIssue.PathEmpty,
                    Message = "未选择任何目标路径。"
                };
            return PermissionHelper.Diagnose(path);
        }

        private void btnDiagnose_Click(object sender, EventArgs e)
        {
            var result = DiagnoseCurrentTarget();
            ShowResult(result.Message, result.Ok, "诊断结果");
        }

        private void btnTestWrite_Click(object sender, EventArgs e)
        {
            string path = GetCurrentTargetPath();
            if (string.IsNullOrEmpty(path))
            {
                ShowResult("未选择任何目标路径。", false, "写测试");
                return;
            }
            PermissionHelper.AccessResult result;
            if (Directory.Exists(path))
                result = PermissionHelper.TestDirectoryWriteAccess(path);
            else
                result = PermissionHelper.TestWriteAccess(path);

            ShowResult(result.Message, result.Ok, "写测试");
        }

        /// <summary>
        /// ★ 2026-09-07：深度诊断 —— 把 13 项拦截逐项探测（不改文件/ACL）。
        /// ACL 改对了仍写不进时，用它查真正的原因。
        /// </summary>
        private async void btnDeepDiag_Click(object sender, EventArgs e)
        {
            string path = GetCurrentTargetPath();
            if (string.IsNullOrEmpty(path))
            {
                ShowResult("请先选择目标路径，再执行深度诊断。", false, "深度诊断");
                return;
            }

            // ★ P2：报告要枚举全部进程 + 注册表 + Restart Manager，放后台线程避免卡界面
            lblResult.Text = "深度诊断报告：";
            txtResult.ForeColor = Color.Black;
            txtResult.Text = "正在生成诊断报告，请稍候...";
            SetButtonsEnabled(false);
            try
            {
                string report = await Task.Run(() => EnvironmentProbe.BuildReport(path));
                txtResult.Text = report;
                MessageBox.Show(this, "诊断完成。详细报告已写入下方文本框（13 项逐项探测，可滚动查看复制）。\n\n"
                    + "★ 若报告标注了某项拦截（如文件被占用 / Defender 受控文件夹 / EFS 加密），\n"
                    + "请先处理该项再开通权限，否则改 ACL 无效。", "深度诊断", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        /// <summary>
        /// ★ 2026-09-07：把主程序 exe 加入 Defender「受控文件夹访问」允许列表。
        /// ACL 全对仍写不进、且诊断报告显示目标受 CFA 保护时使用。
        /// </summary>
        private void btnDefender_Click(object sender, EventArgs e)
        {
            string hint = "选择要加入允许列表的主程序 EXE（即写 ini 失败的那个软件）";
            if (!string.IsNullOrEmpty(GetCurrentTargetPath()))
            {
                try
                {
                    string dir = Directory.Exists(GetCurrentTargetPath())
                        ? GetCurrentTargetPath()
                        : Path.GetDirectoryName(GetCurrentTargetPath());
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        string[] exes = Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly);
                        if (exes.Length > 0)
                            hint += "\r\n\r检测到该目录下的程序: " + string.Join("\r\n", exes);
                    }
                }
                catch { }
            }

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = hint;
                dlg.Filter = "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string msg;
                    bool ok = EnvironmentProbe.TryAddToDefenderAllowed(dlg.FileName, out msg);
                    ShowResult(msg, ok, ok ? "完成" : "失败");
                }
            }
        }

        private async void btnGrant_Click(object sender, EventArgs e)
        {
            if (!PermissionHelper.IsAdministrator())
            {
                MessageBox.Show(this, "请先点击「以管理员身份运行」后再开通权限。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ★ P2：先在 UI 线程取出目标参数，避免后台线程访问控件
            string target;
            bool dirMode;
            if (rbFile.Checked)
            {
                // 文件模式：路径框允许为空 → 默认 test.ini
                target = string.IsNullOrWhiteSpace(txtFilePath.Text)
                    ? @"C:\Program Files\test.ini"
                    : txtFilePath.Text.Trim();
                dirMode = false;
            }
            else if (rbDir.Checked)
            {
                target = txtDirPath.Text.Trim();
                dirMode = true;
            }
            else // rbSpecific
            {
                target = txtSpecific.Text.Trim();
                dirMode = false;
            }

            SetBusy(true, "正在开通权限，请稍候...");

            // 后台进度 → 回 UI 线程更新进度条/状态文本
            IProgress<Tuple<int, int, string>> progress = new Progress<Tuple<int, int, string>>(
                p => UpdateGrantProgress(p.Item1, p.Item2, p.Item3));
            Action<int, int, string> listener = (cur, total, msg) =>
                progress.Report(Tuple.Create(cur, total, msg));
            PermissionHelper.ProgressChanged += listener;

            try
            {
                // ★ 2026-09-07：先启用 SeTakeOwnership 等特权。
                // 否则遇到 Owner 是 TrustedInstaller 的文件，管理员也改不动 ACL。
                await Task.Run(() => PermissionHelper.EnsurePrivileges());

                string result = await Task.Run(() =>
                {
                    try
                    {
                        return dirMode
                            ? PermissionHelper.GrantDirectoryAccess(target)
                            : PermissionHelper.GrantSpecificFileAccess(target);
                    }
                    catch (Exception ex)
                    {
                        return "未捕获异常: " + ex.Message;
                    }
                });

                bool ok = result != null && result.StartsWith("开通成功");
                ShowResult(result, ok, ok ? "完成" : "失败");
            }
            finally
            {
                PermissionHelper.ProgressChanged -= listener;
                SetBusy(false, null);
            }
        }

        private void UpdateGrantProgress(int current, int total, string message)
        {
            if (!string.IsNullOrEmpty(message))
                lblResult.Text = "状态：" + message;

            // 底层统一以 (百分比, 100) 上报，这里换算并夹取到 0~100
            int percent = total > 0 ? (int)((long)current * 100 / total) : 0;
            percent = Math.Max(0, Math.Min(100, percent));

            if (pbProgress.Minimum != 0 || pbProgress.Maximum != 100)
            {
                pbProgress.Minimum = 0;
                pbProgress.Maximum = 100;
            }
            pbProgress.Value = percent;
            lblPercent.Text = percent + "%";
        }

        #endregion

        #region 辅助

        private string GetCurrentTargetPath()
        {
            if (rbFile.Checked)
                return string.IsNullOrWhiteSpace(txtFilePath.Text) ? @"C:\Program Files\test.ini" : txtFilePath.Text.Trim();
            if (rbDir.Checked)
                return txtDirPath.Text.Trim();
            return txtSpecific.Text.Trim();
        }

        private void ShowResult(string message, bool ok, string title)
        {
            txtResult.Text = message ?? "(空)";
            txtResult.ForeColor = ok ? Color.DarkGreen : Color.DarkRed;
            lblResult.Text = "执行结果：";
            MessageBox.Show(this, message ?? "(空)", title,
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        /// <summary>统一控制按钮可用性（需管理员的功能同时受管理员状态约束）。</summary>
        private void SetButtonsEnabled(bool enabled)
        {
            bool admin = PermissionHelper.IsAdministrator();
            btnDiagnose.Enabled = enabled && admin;
            btnTestWrite.Enabled = enabled && admin;
            btnGrant.Enabled = enabled && admin;
            btnDeepDiag.Enabled = enabled && admin;
            btnDefender.Enabled = enabled && admin;
            btnBrowseFile.Enabled = enabled;
            btnBrowseDir.Enabled = enabled && rbDir.Checked;
            btnBrowseSpecific.Enabled = enabled && rbSpecific.Checked;
        }

        private void SetBusy(bool busy, string hint)
        {
            SetButtonsEnabled(!busy);

            if (busy)
            {
                txtResult.ForeColor = Color.DodgerBlue;
                txtResult.Text = hint ?? "正在处理...";
                lblResult.Text = "状态：";
                // 进度条固定为真实百分比模式
                pbProgress.Style = ProgressBarStyle.Blocks;
                pbProgress.Minimum = 0;
                pbProgress.Maximum = 100;
                pbProgress.Value = 0;
                pbProgress.Visible = true;
                lblPercent.Visible = true;
                lblPercent.Text = "0%";
            }
            else
            {
                pbProgress.Visible = false;
                lblPercent.Visible = false;
            }
        }

        #endregion
    }
}