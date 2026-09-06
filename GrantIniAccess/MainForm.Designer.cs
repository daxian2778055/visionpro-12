namespace GrantIniAccess

{

    partial class MainForm

    {

        private System.ComponentModel.IContainer components = null;



        protected override void Dispose(bool disposing)

        {

            if (disposing && (components != null))

                components.Dispose();

            base.Dispose(disposing);

        }



        private void InitializeComponent()

        {

            this.lblTitle = new System.Windows.Forms.Label();

            this.lblAdminStatus = new System.Windows.Forms.Label();

            this.btnRestartAdmin = new System.Windows.Forms.Button();

            this.grpMode = new System.Windows.Forms.GroupBox();

            this.btnBrowseDir = new System.Windows.Forms.Button();

            this.txtDirPath = new System.Windows.Forms.TextBox();

            this.lblDirHint = new System.Windows.Forms.Label();

            this.rbDir = new System.Windows.Forms.RadioButton();

            this.rbFile = new System.Windows.Forms.RadioButton();

            this.btnGrant = new System.Windows.Forms.Button();

            this.lblResult = new System.Windows.Forms.Label();

            this.txtResult = new System.Windows.Forms.TextBox();

            this.grpMode.SuspendLayout();

            this.SuspendLayout();

            // 

            // lblTitle

            // 

            this.lblTitle.AutoSize = true;

            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold);

            this.lblTitle.Location = new System.Drawing.Point(20, 16);

            this.lblTitle.Name = "lblTitle";

            this.lblTitle.Size = new System.Drawing.Size(90, 22);

            this.lblTitle.TabIndex = 0;

            this.lblTitle.Text = "权限开通";

            // 

            // lblAdminStatus

            // 

            this.lblAdminStatus.AutoSize = true;

            this.lblAdminStatus.Location = new System.Drawing.Point(22, 48);

            this.lblAdminStatus.Name = "lblAdminStatus";

            this.lblAdminStatus.Size = new System.Drawing.Size(56, 17);

            this.lblAdminStatus.TabIndex = 1;

            this.lblAdminStatus.Text = "状态...";

            // 

            // btnRestartAdmin

            // 

            this.btnRestartAdmin.Location = new System.Drawing.Point(420, 42);

            this.btnRestartAdmin.Name = "btnRestartAdmin";

            this.btnRestartAdmin.Size = new System.Drawing.Size(152, 28);

            this.btnRestartAdmin.TabIndex = 2;

            this.btnRestartAdmin.Text = "以管理员身份运行";

            this.btnRestartAdmin.UseVisualStyleBackColor = true;

            this.btnRestartAdmin.Click += new System.EventHandler(this.btnRestartAdmin_Click);

            // 

            // grpMode

            // 

            this.grpMode.Controls.Add(this.btnBrowseDir);

            this.grpMode.Controls.Add(this.txtDirPath);

            this.grpMode.Controls.Add(this.lblDirHint);

            this.grpMode.Controls.Add(this.rbDir);

            this.grpMode.Controls.Add(this.rbFile);

            this.grpMode.Location = new System.Drawing.Point(20, 82);

            this.grpMode.Name = "grpMode";

            this.grpMode.Size = new System.Drawing.Size(552, 118);

            this.grpMode.TabIndex = 3;

            this.grpMode.TabStop = false;

            this.grpMode.Text = "开通方式";

            // 

            // rbFile

            // 

            this.rbFile.AutoSize = true;

            this.rbFile.Checked = true;

            this.rbFile.Location = new System.Drawing.Point(16, 28);

            this.rbFile.Name = "rbFile";

            this.rbFile.Size = new System.Drawing.Size(75, 21);

            this.rbFile.TabIndex = 0;

            this.rbFile.TabStop = true;

            this.rbFile.Text = "文件开通";

            this.rbFile.UseVisualStyleBackColor = true;

            this.rbFile.CheckedChanged += new System.EventHandler(this.rbMode_CheckedChanged);

            // 

            // rbDir

            // 

            this.rbDir.AutoSize = true;

            this.rbDir.Location = new System.Drawing.Point(16, 62);

            this.rbDir.Name = "rbDir";

            this.rbDir.Size = new System.Drawing.Size(99, 21);

            this.rbDir.TabIndex = 1;

            this.rbDir.Text = "指定目录开通";

            this.rbDir.UseVisualStyleBackColor = true;

            this.rbDir.CheckedChanged += new System.EventHandler(this.rbMode_CheckedChanged);

            // 

            // lblDirHint

            // 

            this.lblDirHint.AutoSize = true;

            this.lblDirHint.Enabled = false;

            this.lblDirHint.Location = new System.Drawing.Point(130, 64);

            this.lblDirHint.Name = "lblDirHint";

            this.lblDirHint.Size = new System.Drawing.Size(68, 17);

            this.lblDirHint.TabIndex = 2;

            this.lblDirHint.Text = "目录路径：";

            // 

            // txtDirPath

            // 

            this.txtDirPath.Enabled = false;

            this.txtDirPath.Location = new System.Drawing.Point(204, 61);

            this.txtDirPath.Name = "txtDirPath";

            this.txtDirPath.Size = new System.Drawing.Size(252, 23);

            this.txtDirPath.TabIndex = 3;

            this.txtDirPath.Text = "C:\\Program Files";

            // 

            // btnBrowseDir

            // 

            this.btnBrowseDir.Enabled = false;

            this.btnBrowseDir.Location = new System.Drawing.Point(462, 59);

            this.btnBrowseDir.Name = "btnBrowseDir";

            this.btnBrowseDir.Size = new System.Drawing.Size(74, 27);

            this.btnBrowseDir.TabIndex = 4;

            this.btnBrowseDir.Text = "浏览...";

            this.btnBrowseDir.UseVisualStyleBackColor = true;

            this.btnBrowseDir.Click += new System.EventHandler(this.btnBrowseDir_Click);

            // 

            // btnGrant

            // 

            this.btnGrant.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);

            this.btnGrant.Location = new System.Drawing.Point(20, 212);

            this.btnGrant.Name = "btnGrant";

            this.btnGrant.Size = new System.Drawing.Size(552, 36);

            this.btnGrant.TabIndex = 4;

            this.btnGrant.Text = "开通权限";

            this.btnGrant.UseVisualStyleBackColor = true;

            this.btnGrant.Click += new System.EventHandler(this.btnGrant_Click);

            // 

            // lblResult

            // 

            this.lblResult.AutoSize = true;

            this.lblResult.Location = new System.Drawing.Point(20, 262);

            this.lblResult.Name = "lblResult";

            this.lblResult.Size = new System.Drawing.Size(68, 17);

            this.lblResult.TabIndex = 5;

            this.lblResult.Text = "执行结果：";

            // 

            // txtResult

            // 

            this.txtResult.Location = new System.Drawing.Point(20, 284);

            this.txtResult.Multiline = true;

            this.txtResult.Name = "txtResult";

            this.txtResult.ReadOnly = true;

            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

            this.txtResult.Size = new System.Drawing.Size(552, 88);

            this.txtResult.TabIndex = 6;

            // 

            // MainForm

            // 

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.ClientSize = new System.Drawing.Size(594, 391);

            this.Controls.Add(this.txtResult);

            this.Controls.Add(this.lblResult);

            this.Controls.Add(this.btnGrant);

            this.Controls.Add(this.grpMode);

            this.Controls.Add(this.btnRestartAdmin);

            this.Controls.Add(this.lblAdminStatus);

            this.Controls.Add(this.lblTitle);

            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;

            this.MaximizeBox = false;

            this.Name = "MainForm";

            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.Text = "权限开通工具";

            this.Load += new System.EventHandler(this.MainForm_Load);

            this.grpMode.ResumeLayout(false);

            this.grpMode.PerformLayout();

            this.ResumeLayout(false);

            this.PerformLayout();

        }



        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.Label lblAdminStatus;

        private System.Windows.Forms.Button btnRestartAdmin;

        private System.Windows.Forms.GroupBox grpMode;

        private System.Windows.Forms.RadioButton rbFile;

        private System.Windows.Forms.RadioButton rbDir;

        private System.Windows.Forms.Label lblDirHint;

        private System.Windows.Forms.TextBox txtDirPath;

        private System.Windows.Forms.Button btnBrowseDir;

        private System.Windows.Forms.Button btnGrant;

        private System.Windows.Forms.Label lblResult;

        private System.Windows.Forms.TextBox txtResult;

    }

}

