namespace WindowsFormsApplication1
{
    partial class FormMES
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.tcMain = new System.Windows.Forms.TabControl();
            this.tabHttp = new System.Windows.Forms.TabPage();
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtResponse = new System.Windows.Forms.TextBox();
            this.lblResp = new System.Windows.Forms.Label();
            this.txtBody = new System.Windows.Forms.TextBox();
            this.lblReqBody = new System.Windows.Forms.Label();
            this.dgvHeaders = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblReqHead = new System.Windows.Forms.Label();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.cmbMethod = new System.Windows.Forms.ComboBox();
            this.tabWs = new System.Windows.Forms.TabPage();
            this.lblWsInfo = new System.Windows.Forms.Label();
            this.tabTcp = new System.Windows.Forms.TabPage();
            this.lblTcpInfo = new System.Windows.Forms.Label();
            this.tabConfig = new System.Windows.Forms.TabPage();
            this.btnDeleteCfg = new System.Windows.Forms.Button();
            this.btnLoadCfg = new System.Windows.Forms.Button();
            this.cmbSaved = new System.Windows.Forms.ComboBox();
            this.btnSaveCfg = new System.Windows.Forms.Button();
            this.txtCfgName = new System.Windows.Forms.TextBox();
            this.lblCfgName = new System.Windows.Forms.Label();
            this.tcMain.SuspendLayout();
            this.tabHttp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeaders)).BeginInit();
            this.tabWs.SuspendLayout();
            this.tabTcp.SuspendLayout();
            this.tabConfig.SuspendLayout();
            this.SuspendLayout();
            // 
            // tcMain
            // 
            this.tcMain.Controls.Add(this.tabHttp);
            this.tcMain.Controls.Add(this.tabWs);
            this.tcMain.Controls.Add(this.tabTcp);
            this.tcMain.Controls.Add(this.tabConfig);
            this.tcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcMain.Location = new System.Drawing.Point(0, 0);
            this.tcMain.Name = "tcMain";
            this.tcMain.SelectedIndex = 0;
            this.tcMain.Size = new System.Drawing.Size(860, 640);
            this.tcMain.TabIndex = 0;
            // 
            // tabHttp
            // 
            this.tabHttp.Controls.Add(this.lblStatus);
            this.tabHttp.Controls.Add(this.txtResponse);
            this.tabHttp.Controls.Add(this.lblResp);
            this.tabHttp.Controls.Add(this.txtBody);
            this.tabHttp.Controls.Add(this.lblReqBody);
            this.tabHttp.Controls.Add(this.dgvHeaders);
            this.tabHttp.Controls.Add(this.lblReqHead);
            this.tabHttp.Controls.Add(this.btnSend);
            this.tabHttp.Controls.Add(this.txtUrl);
            this.tabHttp.Controls.Add(this.cmbMethod);
            this.tabHttp.Location = new System.Drawing.Point(4, 22);
            this.tabHttp.Name = "tabHttp";
            this.tabHttp.Padding = new System.Windows.Forms.Padding(3);
            this.tabHttp.Size = new System.Drawing.Size(852, 614);
            this.tabHttp.TabIndex = 0;
            this.tabHttp.Text = "HTTP接口";
            this.tabHttp.UseVisualStyleBackColor = true;
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Location = new System.Drawing.Point(12, 578);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(824, 20);
            this.lblStatus.TabIndex = 9;
            this.lblStatus.Text = "就绪";
            // 
            // txtResponse
            // 
            this.txtResponse.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtResponse.Location = new System.Drawing.Point(12, 300);
            this.txtResponse.Multiline = true;
            this.txtResponse.Name = "txtResponse";
            this.txtResponse.ReadOnly = true;
            this.txtResponse.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtResponse.Size = new System.Drawing.Size(824, 272);
            this.txtResponse.TabIndex = 8;
            this.txtResponse.WordWrap = false;
            // 
            // lblResp
            // 
            this.lblResp.AutoSize = true;
            this.lblResp.Location = new System.Drawing.Point(12, 282);
            this.lblResp.Name = "lblResp";
            this.lblResp.Size = new System.Drawing.Size(89, 12);
            this.lblResp.TabIndex = 7;
            this.lblResp.Text = "响应 Response：";
            // 
            // txtBody
            // 
            this.txtBody.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBody.Location = new System.Drawing.Point(12, 194);
            this.txtBody.Multiline = true;
            this.txtBody.Name = "txtBody";
            this.txtBody.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtBody.Size = new System.Drawing.Size(824, 82);
            this.txtBody.TabIndex = 6;
            this.txtBody.WordWrap = false;
            // 
            // lblReqBody
            // 
            this.lblReqBody.AutoSize = true;
            this.lblReqBody.Location = new System.Drawing.Point(12, 176);
            this.lblReqBody.Name = "lblReqBody";
            this.lblReqBody.Size = new System.Drawing.Size(89, 12);
            this.lblReqBody.TabIndex = 5;
            this.lblReqBody.Text = "请求体 Body：";
            // 
            // dgvHeaders
            // 
            this.dgvHeaders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvHeaders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHeaders.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colName,
            this.colValue});
            this.dgvHeaders.Location = new System.Drawing.Point(12, 78);
            this.dgvHeaders.Name = "dgvHeaders";
            this.dgvHeaders.Size = new System.Drawing.Size(824, 92);
            this.dgvHeaders.TabIndex = 4;
            // 
            // colName
            // 
            this.colName.HeaderText = "名称";
            this.colName.Name = "colName";
            this.colName.Width = 200;
            // 
            // colValue
            // 
            this.colValue.HeaderText = "值";
            this.colValue.Name = "colValue";
            this.colValue.Width = 580;
            // 
            // lblReqHead
            // 
            this.lblReqHead.AutoSize = true;
            this.lblReqHead.Location = new System.Drawing.Point(12, 60);
            this.lblReqHead.Name = "lblReqHead";
            this.lblReqHead.Size = new System.Drawing.Size(113, 12);
            this.lblReqHead.TabIndex = 3;
            this.lblReqHead.Text = "请求头 Headers：";
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(724, 12);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(112, 30);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtUrl
            // 
            this.txtUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUrl.Location = new System.Drawing.Point(88, 16);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(630, 21);
            this.txtUrl.TabIndex = 1;
            // 
            // cmbMethod
            // 
            this.cmbMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMethod.FormattingEnabled = true;
            this.cmbMethod.Location = new System.Drawing.Point(12, 15);
            this.cmbMethod.Name = "cmbMethod";
            this.cmbMethod.Size = new System.Drawing.Size(70, 20);
            this.cmbMethod.TabIndex = 0;
            // 
            // tabWs
            // 
            this.tabWs.Controls.Add(this.lblWsInfo);
            this.tabWs.Location = new System.Drawing.Point(4, 22);
            this.tabWs.Name = "tabWs";
            this.tabWs.Padding = new System.Windows.Forms.Padding(3);
            this.tabWs.Size = new System.Drawing.Size(852, 614);
            this.tabWs.TabIndex = 1;
            this.tabWs.Text = "WebService(SOAP)";
            this.tabWs.UseVisualStyleBackColor = true;
            // 
            // lblWsInfo
            // 
            this.lblWsInfo.Location = new System.Drawing.Point(20, 20);
            this.lblWsInfo.Name = "lblWsInfo";
            this.lblWsInfo.Size = new System.Drawing.Size(800, 60);
            this.lblWsInfo.TabIndex = 0;
            this.lblWsInfo.Text = "预留：WebService(SOAP) 调用。\r\n接入方式：填写 WSDL 地址与 SOAP 报文后调用。\r\n（后续按客户现场需求在此扩展）";
            // 
            // tabTcp
            // 
            this.tabTcp.Controls.Add(this.lblTcpInfo);
            this.tabTcp.Location = new System.Drawing.Point(4, 22);
            this.tabTcp.Name = "tabTcp";
            this.tabTcp.Padding = new System.Windows.Forms.Padding(3);
            this.tabTcp.Size = new System.Drawing.Size(852, 614);
            this.tabTcp.TabIndex = 2;
            this.tabTcp.Text = "TCP/Socket";
            this.tabTcp.UseVisualStyleBackColor = true;
            // 
            // lblTcpInfo
            // 
            this.lblTcpInfo.Location = new System.Drawing.Point(20, 20);
            this.lblTcpInfo.Name = "lblTcpInfo";
            this.lblTcpInfo.Size = new System.Drawing.Size(800, 60);
            this.lblTcpInfo.TabIndex = 0;
            this.lblTcpInfo.Text = "预留：TCP/Socket 自定义报文通讯。\r\n接入方式：填写 IP、端口与报文模板后连接收发。\r\n（后续按客户现场需求在此扩展）";
            // 
            // tabConfig
            // 
            this.tabConfig.Controls.Add(this.btnDeleteCfg);
            this.tabConfig.Controls.Add(this.btnLoadCfg);
            this.tabConfig.Controls.Add(this.cmbSaved);
            this.tabConfig.Controls.Add(this.btnSaveCfg);
            this.tabConfig.Controls.Add(this.txtCfgName);
            this.tabConfig.Controls.Add(this.lblCfgName);
            this.tabConfig.Location = new System.Drawing.Point(4, 22);
            this.tabConfig.Name = "tabConfig";
            this.tabConfig.Padding = new System.Windows.Forms.Padding(3);
            this.tabConfig.Size = new System.Drawing.Size(852, 614);
            this.tabConfig.TabIndex = 3;
            this.tabConfig.Text = "接口保存";
            this.tabConfig.UseVisualStyleBackColor = true;
            // 
            // btnDeleteCfg
            // 
            this.btnDeleteCfg.Location = new System.Drawing.Point(418, 48);
            this.btnDeleteCfg.Name = "btnDeleteCfg";
            this.btnDeleteCfg.Size = new System.Drawing.Size(90, 28);
            this.btnDeleteCfg.TabIndex = 5;
            this.btnDeleteCfg.Text = "删除";
            this.btnDeleteCfg.UseVisualStyleBackColor = true;
            this.btnDeleteCfg.Click += new System.EventHandler(this.btnDeleteCfg_Click);
            // 
            // btnLoadCfg
            // 
            this.btnLoadCfg.Location = new System.Drawing.Point(322, 48);
            this.btnLoadCfg.Name = "btnLoadCfg";
            this.btnLoadCfg.Size = new System.Drawing.Size(90, 28);
            this.btnLoadCfg.TabIndex = 4;
            this.btnLoadCfg.Text = "载入";
            this.btnLoadCfg.UseVisualStyleBackColor = true;
            this.btnLoadCfg.Click += new System.EventHandler(this.btnLoadCfg_Click);
            // 
            // cmbSaved
            // 
            this.cmbSaved.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSaved.FormattingEnabled = true;
            this.cmbSaved.Location = new System.Drawing.Point(16, 52);
            this.cmbSaved.Name = "cmbSaved";
            this.cmbSaved.Size = new System.Drawing.Size(300, 20);
            this.cmbSaved.TabIndex = 3;
            // 
            // btnSaveCfg
            // 
            this.btnSaveCfg.Location = new System.Drawing.Point(322, 10);
            this.btnSaveCfg.Name = "btnSaveCfg";
            this.btnSaveCfg.Size = new System.Drawing.Size(90, 28);
            this.btnSaveCfg.TabIndex = 2;
            this.btnSaveCfg.Text = "保存";
            this.btnSaveCfg.UseVisualStyleBackColor = true;
            this.btnSaveCfg.Click += new System.EventHandler(this.btnSaveCfg_Click);
            // 
            // txtCfgName
            // 
            this.txtCfgName.Location = new System.Drawing.Point(60, 13);
            this.txtCfgName.Name = "txtCfgName";
            this.txtCfgName.Size = new System.Drawing.Size(256, 21);
            this.txtCfgName.TabIndex = 1;
            // 
            // lblCfgName
            // 
            this.lblCfgName.AutoSize = true;
            this.lblCfgName.Location = new System.Drawing.Point(14, 17);
            this.lblCfgName.Name = "lblCfgName";
            this.lblCfgName.Size = new System.Drawing.Size(41, 12);
            this.lblCfgName.TabIndex = 0;
            this.lblCfgName.Text = "名称：";
            // 
            // FormMES
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(860, 640);
            this.Controls.Add(this.tcMain);
            this.Name = "FormMES";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MES 通讯工具";
            this.tcMain.ResumeLayout(false);
            this.tabHttp.ResumeLayout(false);
            this.tabHttp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHeaders)).EndInit();
            this.tabWs.ResumeLayout(false);
            this.tabTcp.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            this.tabConfig.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tcMain;
        private System.Windows.Forms.TabPage tabHttp;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtResponse;
        private System.Windows.Forms.Label lblResp;
        private System.Windows.Forms.TextBox txtBody;
        private System.Windows.Forms.Label lblReqBody;
        private System.Windows.Forms.DataGridView dgvHeaders;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.Label lblReqHead;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.ComboBox cmbMethod;
        private System.Windows.Forms.TabPage tabWs;
        private System.Windows.Forms.Label lblWsInfo;
        private System.Windows.Forms.TabPage tabTcp;
        private System.Windows.Forms.Label lblTcpInfo;
        private System.Windows.Forms.TabPage tabConfig;
        private System.Windows.Forms.Button btnDeleteCfg;
        private System.Windows.Forms.Button btnLoadCfg;
        private System.Windows.Forms.ComboBox cmbSaved;
        private System.Windows.Forms.Button btnSaveCfg;
        private System.Windows.Forms.TextBox txtCfgName;
        private System.Windows.Forms.Label lblCfgName;
    }
}
