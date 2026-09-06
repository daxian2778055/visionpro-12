using System;

using System.Drawing;

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



        private void RefreshAdminStatus()

        {

            bool isAdmin = PermissionHelper.IsAdministrator();

            lblAdminStatus.ForeColor = isAdmin ? Color.DarkGreen : Color.DarkRed;

            lblAdminStatus.Text = isAdmin

                ? "当前状态：管理员权限（可开通）"

                : "当前状态：普通用户（需管理员权限才能开通）";

            btnRestartAdmin.Visible = !isAdmin;

            btnGrant.Enabled = isAdmin;

        }



        private void UpdateModeUi()

        {

            bool dirMode = rbDir.Checked;

            txtDirPath.Enabled = dirMode;

            btnBrowseDir.Enabled = dirMode;

            lblDirHint.Enabled = dirMode;

        }



        private void rbMode_CheckedChanged(object sender, EventArgs e)

        {

            UpdateModeUi();

        }



        private void btnBrowseDir_Click(object sender, EventArgs e)

        {

            using (var dlg = new FolderBrowserDialog())

            {

                dlg.Description = "选择要开通权限的目录";

                dlg.ShowNewFolderButton = true;

                if (!string.IsNullOrWhiteSpace(txtDirPath.Text) && System.IO.Directory.Exists(txtDirPath.Text))

                    dlg.SelectedPath = txtDirPath.Text;



                if (dlg.ShowDialog(this) == DialogResult.OK)

                    txtDirPath.Text = dlg.SelectedPath;

            }

        }



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



        private void btnGrant_Click(object sender, EventArgs e)

        {

            if (!PermissionHelper.IsAdministrator())

            {

                MessageBox.Show(this, "请先点击「以管理员身份运行」后再开通权限。", "提示",

                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return;

            }



            string result = rbFile.Checked

                ? PermissionHelper.GrantDefaultFileAccess()

                : PermissionHelper.GrantDirectoryAccess(txtDirPath.Text);



            txtResult.Text = result;

            bool ok = result.StartsWith("开通成功");

            MessageBox.Show(this, result, ok ? "完成" : "失败",

                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        }

    }

}

