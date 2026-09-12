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

        #region Windows 窗体设计器生成的代码

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblAdminStatus = new System.Windows.Forms.Label();
            this.btnRestartAdmin = new System.Windows.Forms.Button();
            this.grpMode = new System.Windows.Forms.GroupBox();
            this.btnBrowseFile = new System.Windows.Forms.Button();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.lblFilePath = new System.Windows.Forms.Label();
            this.rbFile = new System.Windows.Forms.RadioButton();
            this.btnBrowseDir = new System.Windows.Forms.Button();
            this.txtDirPath = new System.Windows.Forms.TextBox();
            this.lblDirPath = new System.Windows.Forms.Label();
            this.rbDir = new System.Windows.Forms.RadioButton();
            this.btnBrowseSpecific = new System.Windows.Forms.Button();
            this.txtSpecific = new System.Windows.Forms.TextBox();
            this.lblSpecific = new System.Windows.Forms.Label();
            this.rbSpecific = new System.Windows.Forms.RadioButton();
            this.lblHint = new System.Windows.Forms.Label();
            this.btnDiagnose = new System.Windows.Forms.Button();
            this.btnTestWrite = new System.Windows.Forms.Button();
            this.btnGrant = new System.Windows.Forms.Button();
            this.btnDeepDiag = new System.Windows.Forms.Button();
            this.btnDefender = new System.Windows.Forms.Button();
            this.lblResult = new System.Windows.Forms.Label();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.pbProgress = new System.Windows.Forms.ProgressBar();
            this.lblPercent = new System.Windows.Forms.Label();
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
            this.btnRestartAdmin.Size = new System.Drawing.Size(170, 28);
            this.btnRestartAdmin.TabIndex = 2;
            this.btnRestartAdmin.Text = "以管理员身份运行";
            this.btnRestartAdmin.UseVisualStyleBackColor = true;
            this.btnRestartAdmin.Click += new System.EventHandler(this.btnRestartAdmin_Click);

            //
            // grpMode
            //
            this.grpMode.Controls.Add(this.btnBrowseFile);
            this.grpMode.Controls.Add(this.txtFilePath);
            this.grpMode.Controls.Add(this.lblFilePath);
            this.grpMode.Controls.Add(this.rbFile);
            this.grpMode.Controls.Add(this.btnBrowseDir);
            this.grpMode.Controls.Add(this.txtDirPath);
            this.grpMode.Controls.Add(this.lblDirPath);
            this.grpMode.Controls.Add(this.rbDir);
            this.grpMode.Controls.Add(this.btnBrowseSpecific);
            this.grpMode.Controls.Add(this.txtSpecific);
            this.grpMode.Controls.Add(this.lblSpecific);
            this.grpMode.Controls.Add(this.rbSpecific);
            this.grpMode.Controls.Add(this.lblHint);
            this.grpMode.Location = new System.Drawing.Point(20, 82);
            this.grpMode.Name = "grpMode";
            this.grpMode.Size = new System.Drawing.Size(570, 158);
            this.grpMode.TabIndex = 3;
            this.grpMode.TabStop = false;
            this.grpMode.Text = "开通方式";

            //
            // rbFile
            //
            this.rbFile.AutoSize = true;
            this.rbFile.Checked = true;
            this.rbFile.Location = new System.Drawing.Point(16, 26);
            this.rbFile.Name = "rbFile";
            this.rbFile.Size = new System.Drawing.Size(75, 21);
            this.rbFile.TabIndex = 0;
            this.rbFile.TabStop = true;
            this.rbFile.Text = "test.ini";
            this.rbFile.UseVisualStyleBackColor = true;
            this.rbFile.CheckedChanged += new System.EventHandler(this.rbMode_CheckedChanged);

            //
            // lblFilePath
            //
            this.lblFilePath.AutoSize = true;
            this.lblFilePath.Location = new System.Drawing.Point(100, 30);
            this.lblFilePath.Name = "lblFilePath";
            this.lblFilePath.Size = new System.Drawing.Size(56, 17);
            this.lblFilePath.TabIndex = 1;
            this.lblFilePath.Text = "路径：";

            //
            // txtFilePath
            //
            this.txtFilePath.Location = new System.Drawing.Point(160, 27);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(310, 23);
            this.txtFilePath.TabIndex = 2;
            this.txtFilePath.Text = @"C:\Program Files\test.ini";

            //
            // btnBrowseFile
            //
            this.btnBrowseFile.Location = new System.Drawing.Point(478, 25);
            this.btnBrowseFile.Name = "btnBrowseFile";
            this.btnBrowseFile.Size = new System.Drawing.Size(74, 27);
            this.btnBrowseFile.TabIndex = 3;
            this.btnBrowseFile.Text = "浏览...";
            this.btnBrowseFile.UseVisualStyleBackColor = true;
            this.btnBrowseFile.Click += new System.EventHandler(this.btnBrowseFile_Click);

            //
            // rbDir
            //
            this.rbDir.AutoSize = true;
            this.rbDir.Location = new System.Drawing.Point(16, 60);
            this.rbDir.Name = "rbDir";
            this.rbDir.Size = new System.Drawing.Size(99, 21);
            this.rbDir.TabIndex = 4;
            this.rbDir.Text = "指定目录";
            this.rbDir.UseVisualStyleBackColor = true;
            this.rbDir.CheckedChanged += new System.EventHandler(this.rbMode_CheckedChanged);

            //
            // lblDirPath
            //
            this.lblDirPath.AutoSize = true;
            this.lblDirPath.Enabled = false;
            this.lblDirPath.Location = new System.Drawing.Point(100, 64);
            this.lblDirPath.Name = "lblDirPath";
            this.lblDirPath.Size = new System.Drawing.Size(56, 17);
            this.lblDirPath.TabIndex = 5;
            this.lblDirPath.Text = "目录：";

            //
            // txtDirPath
            //
            this.txtDirPath.Enabled = false;
            this.txtDirPath.Location = new System.Drawing.Point(160, 61);
            this.txtDirPath.Name = "txtDirPath";
            this.txtDirPath.Size = new System.Drawing.Size(310, 23);
            this.txtDirPath.TabIndex = 6;
            this.txtDirPath.Text = @"C:\Program Files";

            //
            // btnBrowseDir
            //
            this.btnBrowseDir.Enabled = false;
            this.btnBrowseDir.Location = new System.Drawing.Point(478, 59);
            this.btnBrowseDir.Name = "btnBrowseDir";
            this.btnBrowseDir.Size = new System.Drawing.Size(74, 27);
            this.btnBrowseDir.TabIndex = 7;
            this.btnBrowseDir.Text = "浏览...";
            this.btnBrowseDir.UseVisualStyleBackColor = true;
            this.btnBrowseDir.Click += new System.EventHandler(this.btnBrowseDir_Click);

            //
            // rbSpecific
            //
            this.rbSpecific.AutoSize = true;
            this.rbSpecific.Location = new System.Drawing.Point(16, 94);
            this.rbSpecific.Name = "rbSpecific";
            this.rbSpecific.Size = new System.Drawing.Size(99, 21);
            this.rbSpecific.TabIndex = 8;
            this.rbSpecific.Text = "自定义文件";
            this.rbSpecific.UseVisualStyleBackColor = true;
            this.rbSpecific.CheckedChanged += new System.EventHandler(this.rbMode_CheckedChanged);

            //
            // lblSpecific
            //
            this.lblSpecific.AutoSize = true;
            this.lblSpecific.Enabled = false;
            this.lblSpecific.Location = new System.Drawing.Point(100, 98);
            this.lblSpecific.Name = "lblSpecific";
            this.lblSpecific.Size = new System.Drawing.Size(68, 17);
            this.lblSpecific.TabIndex = 9;
            this.lblSpecific.Text = "ini 路径：";

            //
            // txtSpecific
            //
            this.txtSpecific.Enabled = false;
            this.txtSpecific.Location = new System.Drawing.Point(160, 95);
            this.txtSpecific.Name = "txtSpecific";
            this.txtSpecific.Size = new System.Drawing.Size(310, 23);
            this.txtSpecific.TabIndex = 10;

            //
            // btnBrowseSpecific
            //
            this.btnBrowseSpecific.Enabled = false;
            this.btnBrowseSpecific.Location = new System.Drawing.Point(478, 93);
            this.btnBrowseSpecific.Name = "btnBrowseSpecific";
            this.btnBrowseSpecific.Size = new System.Drawing.Size(74, 27);
            this.btnBrowseSpecific.TabIndex = 11;
            this.btnBrowseSpecific.Text = "浏览...";
            this.btnBrowseSpecific.UseVisualStyleBackColor = true;
            this.btnBrowseSpecific.Click += new System.EventHandler(this.btnBrowseSpecific_Click);

            //
            // lblHint
            //
            this.lblHint.AutoSize = true;
            this.lblHint.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Location = new System.Drawing.Point(16, 126);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(0, 17);
            this.lblHint.TabIndex = 12;


            //
            // btnDiagnose
            //
            this.btnDiagnose.Location = new System.Drawing.Point(20, 253);
            this.btnDiagnose.Name = "btnDiagnose";
            this.btnDiagnose.Size = new System.Drawing.Size(110, 32);
            this.btnDiagnose.TabIndex = 4;
            this.btnDiagnose.Text = "诊断";
            this.btnDiagnose.UseVisualStyleBackColor = true;
            this.btnDiagnose.Click += new System.EventHandler(this.btnDiagnose_Click);

            //
            // btnTestWrite
            //
            this.btnTestWrite.Location = new System.Drawing.Point(140, 253);
            this.btnTestWrite.Name = "btnTestWrite";
            this.btnTestWrite.Size = new System.Drawing.Size(110, 32);
            this.btnTestWrite.TabIndex = 5;
            this.btnTestWrite.Text = "写测试";
            this.btnTestWrite.UseVisualStyleBackColor = true;
            this.btnTestWrite.Click += new System.EventHandler(this.btnTestWrite_Click);

            //
            // btnGrant
            //
            this.btnGrant.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnGrant.Location = new System.Drawing.Point(260, 253);
            this.btnGrant.Name = "btnGrant";
            this.btnGrant.Size = new System.Drawing.Size(330, 32);
            this.btnGrant.TabIndex = 6;
            this.btnGrant.Text = "开通权限";
            this.btnGrant.UseVisualStyleBackColor = true;
            this.btnGrant.Click += new System.EventHandler(this.btnGrant_Click);

            //
            // btnDeepDiag
            //
            this.btnDeepDiag.Location = new System.Drawing.Point(20, 293);
            this.btnDeepDiag.Name = "btnDeepDiag";
            this.btnDeepDiag.Size = new System.Drawing.Size(265, 28);
            this.btnDeepDiag.TabIndex = 7;
            this.btnDeepDiag.Text = "深度诊断（13 项探测）";
            this.btnDeepDiag.UseVisualStyleBackColor = true;
            this.btnDeepDiag.Click += new System.EventHandler(this.btnDeepDiag_Click);

            //
            // btnDefender
            //
            this.btnDefender.Location = new System.Drawing.Point(295, 293);
            this.btnDefender.Name = "btnDefender";
            this.btnDefender.Size = new System.Drawing.Size(295, 28);
            this.btnDefender.TabIndex = 8;
            this.btnDefender.Text = "加入 Defender 受控文件夹白名单...";
            this.btnDefender.UseVisualStyleBackColor = true;
            this.btnDefender.Click += new System.EventHandler(this.btnDefender_Click);

            //
            // pbProgress
            //
            this.pbProgress.Location = new System.Drawing.Point(20, 328);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(500, 18);
            this.pbProgress.TabIndex = 11;
            this.pbProgress.Visible = false;

            //
            // lblPercent
            //
            this.lblPercent.AutoSize = true;
            this.lblPercent.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPercent.ForeColor = System.Drawing.Color.DodgerBlue;
            this.lblPercent.Location = new System.Drawing.Point(528, 328);
            this.lblPercent.Name = "lblPercent";
            this.lblPercent.Size = new System.Drawing.Size(28, 17);
            this.lblPercent.TabIndex = 12;
            this.lblPercent.Text = "0%";
            this.lblPercent.Visible = false;

            //
            // lblResult
            //
            this.lblResult.AutoSize = true;
            this.lblResult.Location = new System.Drawing.Point(20, 350);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(68, 17);
            this.lblResult.TabIndex = 9;
            this.lblResult.Text = "执行结果：";

            //
            // txtResult
            //
            this.txtResult.Location = new System.Drawing.Point(20, 372);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResult.Size = new System.Drawing.Size(570, 106);
            this.txtResult.TabIndex = 10;

            //
            // MainForm
            //
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(610, 493);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.lblResult);
            this.Controls.Add(this.lblPercent);
            this.Controls.Add(this.pbProgress);
            this.Controls.Add(this.btnDefender);
            this.Controls.Add(this.btnDeepDiag);
            this.Controls.Add(this.btnGrant);
            this.Controls.Add(this.btnTestWrite);
            this.Controls.Add(this.btnDiagnose);
            this.Controls.Add(this.grpMode);
            this.Controls.Add(this.btnRestartAdmin);
            this.Controls.Add(this.lblAdminStatus);
            this.Controls.Add(this.lblTitle);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
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

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblAdminStatus;
        private System.Windows.Forms.Button btnRestartAdmin;
        private System.Windows.Forms.GroupBox grpMode;
        private System.Windows.Forms.RadioButton rbFile;
        private System.Windows.Forms.RadioButton rbDir;
        private System.Windows.Forms.RadioButton rbSpecific;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Button btnBrowseFile;
        private System.Windows.Forms.Label lblDirPath;
        private System.Windows.Forms.TextBox txtDirPath;
        private System.Windows.Forms.Button btnBrowseDir;
        private System.Windows.Forms.Label lblSpecific;
        private System.Windows.Forms.TextBox txtSpecific;
        private System.Windows.Forms.Button btnBrowseSpecific;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnDiagnose;
        private System.Windows.Forms.Button btnTestWrite;
        private System.Windows.Forms.Button btnGrant;
        private System.Windows.Forms.Button btnDeepDiag;
        private System.Windows.Forms.Button btnDefender;
        private System.Windows.Forms.Label lblResult;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.ProgressBar pbProgress;
        private System.Windows.Forms.Label lblPercent;
    }
}
