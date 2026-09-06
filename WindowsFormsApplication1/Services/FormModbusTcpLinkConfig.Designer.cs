namespace WindowsFormsApplication1
{
    partial class FormModbusTcpLinkConfig
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
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
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tpConn = new System.Windows.Forms.TabPage();
            this.tlpConn = new System.Windows.Forms.TableLayoutPanel();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblEn = new System.Windows.Forms.Label();
            this.chkEn = new System.Windows.Forms.CheckBox();
            this.lblLunxunEn = new System.Windows.Forms.Label();
            this.chkLunxun = new System.Windows.Forms.CheckBox();
            this.lblIp = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.lblCell = new System.Windows.Forms.Label();
            this.txtCell = new System.Windows.Forms.TextBox();
            this.lblAbcd = new System.Windows.Forms.Label();
            this.cboAbcd = new System.Windows.Forms.ComboBox();
            this.lblQishi = new System.Windows.Forms.Label();
            this.numQishi = new System.Windows.Forms.NumericUpDown();
            this.lblZongchang = new System.Windows.Forms.Label();
            this.numZongchang = new System.Windows.Forms.NumericUpDown();
            this.lblLunxunTime = new System.Windows.Forms.Label();
            this.numLunxunTime = new System.Windows.Forms.NumericUpDown();
            this.tpBlocks = new System.Windows.Forms.TabPage();
            this.lblBlocksTip = new System.Windows.Forms.Label();
            this.dgvBlocks = new System.Windows.Forms.DataGridView();
            this.tpCameras = new System.Windows.Forms.TabPage();
            this.lblCamTip = new System.Windows.Forms.Label();
            this.dgvCameras = new System.Windows.Forms.DataGridView();
            this.tpScheme = new System.Windows.Forms.TabPage();
            this.lblSchemeTip = new System.Windows.Forms.Label();
            this.dgvScheme = new System.Windows.Forms.DataGridView();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.lblInfo = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.tabMain.SuspendLayout();
            this.tpConn.SuspendLayout();
            this.tlpConn.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQishi)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numZongchang)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLunxunTime)).BeginInit();
            this.tpBlocks.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBlocks)).BeginInit();
            this.tpCameras.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCameras)).BeginInit();
            this.tpScheme.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvScheme)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tpConn);
            this.tabMain.Controls.Add(this.tpBlocks);
            this.tabMain.Controls.Add(this.tpCameras);
            this.tabMain.Controls.Add(this.tpScheme);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(884, 659);
            this.tabMain.TabIndex = 0;
            // 
            // tpConn
            // 
            this.tpConn.Controls.Add(this.tlpConn);
            this.tpConn.Location = new System.Drawing.Point(4, 22);
            this.tpConn.Name = "tpConn";
            this.tpConn.Padding = new System.Windows.Forms.Padding(8);
            this.tpConn.Size = new System.Drawing.Size(876, 633);
            this.tpConn.TabIndex = 0;
            this.tpConn.Text = "连接参数";
            // 
            // tlpConn
            // 
            this.tlpConn.ColumnCount = 2;
            this.tlpConn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpConn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpConn.Controls.Add(this.lblName, 0, 0);
            this.tlpConn.Controls.Add(this.txtName, 1, 0);
            this.tlpConn.Controls.Add(this.lblEn, 0, 1);
            this.tlpConn.Controls.Add(this.chkEn, 1, 1);
            this.tlpConn.Controls.Add(this.lblLunxunEn, 0, 2);
            this.tlpConn.Controls.Add(this.chkLunxun, 1, 2);
            this.tlpConn.Controls.Add(this.lblIp, 0, 3);
            this.tlpConn.Controls.Add(this.txtIp, 1, 3);
            this.tlpConn.Controls.Add(this.lblPort, 0, 4);
            this.tlpConn.Controls.Add(this.txtPort, 1, 4);
            this.tlpConn.Controls.Add(this.lblCell, 0, 5);
            this.tlpConn.Controls.Add(this.txtCell, 1, 5);
            this.tlpConn.Controls.Add(this.lblAbcd, 0, 6);
            this.tlpConn.Controls.Add(this.cboAbcd, 1, 6);
            this.tlpConn.Controls.Add(this.lblQishi, 0, 7);
            this.tlpConn.Controls.Add(this.numQishi, 1, 7);
            this.tlpConn.Controls.Add(this.lblZongchang, 0, 8);
            this.tlpConn.Controls.Add(this.numZongchang, 1, 8);
            this.tlpConn.Controls.Add(this.lblLunxunTime, 0, 9);
            this.tlpConn.Controls.Add(this.numLunxunTime, 1, 9);
            this.tlpConn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpConn.Location = new System.Drawing.Point(8, 8);
            this.tlpConn.Name = "tlpConn";
            this.tlpConn.RowCount = 10;
            for (int i = 0; i < 10; i++)
                this.tlpConn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.tlpConn.Size = new System.Drawing.Size(860, 617);
            this.tlpConn.TabIndex = 0;
            // 
            // lblName
            // 
            this.lblName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(3, 9);
            this.lblName.Name = "lblName";
            this.lblName.Text = "连接名称";
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.Location = new System.Drawing.Point(133, 6);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(723, 21);
            this.txtName.TabIndex = 1;
            // 
            // lblEn
            // 
            this.lblEn.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblEn.AutoSize = true;
            this.lblEn.Location = new System.Drawing.Point(3, 43);
            this.lblEn.Name = "lblEn";
            this.lblEn.Text = "启用连接";
            // 
            // chkEn
            // 
            this.chkEn.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkEn.AutoSize = true;
            this.chkEn.Location = new System.Drawing.Point(133, 43);
            this.chkEn.Name = "chkEn";
            this.chkEn.TabIndex = 2;
            this.chkEn.Text = "勾选后保存并应用时会真正连接该 Modbus 服务器";
            // 
            // lblLunxunEn
            // 
            this.lblLunxunEn.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLunxunEn.AutoSize = true;
            this.lblLunxunEn.Location = new System.Drawing.Point(3, 77);
            this.lblLunxunEn.Name = "lblLunxunEn";
            this.lblLunxunEn.Text = "开启轮询";
            // 
            // chkLunxun
            // 
            this.chkLunxun.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkLunxun.AutoSize = true;
            this.chkLunxun.Location = new System.Drawing.Point(133, 77);
            this.chkLunxun.Name = "chkLunxun";
            this.chkLunxun.TabIndex = 3;
            this.chkLunxun.Text = "轮询读取数据块并触发/回写";
            // 
            // lblIp
            // 
            this.lblIp.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(3, 111);
            this.lblIp.Name = "lblIp";
            this.lblIp.Text = "IP 地址";
            // 
            // txtIp
            // 
            this.txtIp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtIp.Location = new System.Drawing.Point(133, 108);
            this.txtIp.Name = "txtIp";
            this.txtIp.Size = new System.Drawing.Size(723, 21);
            this.txtIp.TabIndex = 4;
            // 
            // lblPort
            // 
            this.lblPort.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(3, 145);
            this.lblPort.Name = "lblPort";
            this.lblPort.Text = "端口";
            // 
            // txtPort
            // 
            this.txtPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPort.Location = new System.Drawing.Point(133, 142);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(723, 21);
            this.txtPort.TabIndex = 5;
            // 
            // lblCell
            // 
            this.lblCell.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCell.AutoSize = true;
            this.lblCell.Location = new System.Drawing.Point(3, 179);
            this.lblCell.Name = "lblCell";
            this.lblCell.Text = "单元号(Station)";
            // 
            // txtCell
            // 
            this.txtCell.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCell.Location = new System.Drawing.Point(133, 176);
            this.txtCell.Name = "txtCell";
            this.txtCell.Size = new System.Drawing.Size(723, 21);
            this.txtCell.TabIndex = 6;
            // 
            // lblAbcd
            // 
            this.lblAbcd.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAbcd.AutoSize = true;
            this.lblAbcd.Location = new System.Drawing.Point(3, 213);
            this.lblAbcd.Name = "lblAbcd";
            this.lblAbcd.Text = "数据格式";
            // 
            // cboAbcd
            // 
            this.cboAbcd.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAbcd.FormattingEnabled = true;
            this.cboAbcd.Items.AddRange(new object[] { "CDAB", "ABCD", "BADC", "DCBA" });
            this.cboAbcd.Location = new System.Drawing.Point(133, 210);
            this.cboAbcd.Name = "cboAbcd";
            this.cboAbcd.Size = new System.Drawing.Size(160, 20);
            this.cboAbcd.TabIndex = 7;
            // 
            // lblQishi
            // 
            this.lblQishi.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblQishi.AutoSize = true;
            this.lblQishi.Location = new System.Drawing.Point(3, 247);
            this.lblQishi.Name = "lblQishi";
            this.lblQishi.Text = "起始地址";
            // 
            // numQishi
            // 
            this.numQishi.Location = new System.Drawing.Point(133, 244);
            this.numQishi.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numQishi.Name = "numQishi";
            this.numQishi.Size = new System.Drawing.Size(160, 21);
            this.numQishi.TabIndex = 8;
            // 
            // lblZongchang
            // 
            this.lblZongchang.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblZongchang.AutoSize = true;
            this.lblZongchang.Location = new System.Drawing.Point(3, 281);
            this.lblZongchang.Name = "lblZongchang";
            this.lblZongchang.Text = "总长度";
            // 
            // numZongchang
            // 
            this.numZongchang.Location = new System.Drawing.Point(133, 278);
            this.numZongchang.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.numZongchang.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numZongchang.Name = "numZongchang";
            this.numZongchang.Size = new System.Drawing.Size(160, 21);
            this.numZongchang.TabIndex = 9;
            this.numZongchang.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblLunxunTime
            // 
            this.lblLunxunTime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblLunxunTime.AutoSize = true;
            this.lblLunxunTime.Location = new System.Drawing.Point(3, 315);
            this.lblLunxunTime.Name = "lblLunxunTime";
            this.lblLunxunTime.Text = "轮询周期(ms)";
            // 
            // numLunxunTime
            // 
            this.numLunxunTime.Location = new System.Drawing.Point(133, 312);
            this.numLunxunTime.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numLunxunTime.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numLunxunTime.Name = "numLunxunTime";
            this.numLunxunTime.Size = new System.Drawing.Size(160, 21);
            this.numLunxunTime.TabIndex = 10;
            this.numLunxunTime.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // tpBlocks
            // 
            this.tpBlocks.Controls.Add(this.dgvBlocks);
            this.tpBlocks.Controls.Add(this.lblBlocksTip);
            this.tpBlocks.Location = new System.Drawing.Point(4, 22);
            this.tpBlocks.Name = "tpBlocks";
            this.tpBlocks.Padding = new System.Windows.Forms.Padding(8);
            this.tpBlocks.Size = new System.Drawing.Size(876, 633);
            this.tpBlocks.TabIndex = 1;
            this.tpBlocks.Text = "数据块";
            // 
            // lblBlocksTip
            // 
            this.lblBlocksTip.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblBlocksTip.Location = new System.Drawing.Point(8, 603);
            this.lblBlocksTip.Name = "lblBlocksTip";
            this.lblBlocksTip.Size = new System.Drawing.Size(860, 22);
            this.lblBlocksTip.TabIndex = 1;
            this.lblBlocksTip.Text = "一个数据块 = 一组连续保持寄存器地址。gaodiwei 可选：触发/反馈；geshi 可选：int/long/float/string。";
            // 
            // dgvBlocks
            // 
            this.dgvBlocks.AllowUserToAddRows = true;
            this.dgvBlocks.AllowUserToDeleteRows = true;
            this.dgvBlocks.AutoGenerateColumns = false;
            this.dgvBlocks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvBlocks.Location = new System.Drawing.Point(8, 8);
            this.dgvBlocks.Name = "dgvBlocks";
            this.dgvBlocks.Size = new System.Drawing.Size(860, 595);
            this.dgvBlocks.TabIndex = 0;
            var colBlkIndex = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Index", HeaderText = "序号", ReadOnly = true, Width = 60 };
            var colBlkName = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Name", HeaderText = "数据块名", Width = 130 };
            var colBlkQishi = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Qishi", HeaderText = "起始地址", Width = 90 };
            var colBlkChangdu = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Changdu", HeaderText = "长度", Width = 70 };
            var colBlkGaodiwei = new System.Windows.Forms.DataGridViewComboBoxColumn() { DataPropertyName = "Gaodiwei", HeaderText = "高低位", Width = 80, FlatStyle = System.Windows.Forms.FlatStyle.System };
            colBlkGaodiwei.Items.AddRange(new object[] { "触发", "反馈" });
            var colBlkGeshi = new System.Windows.Forms.DataGridViewComboBoxColumn() { DataPropertyName = "Geshi", HeaderText = "格式", Width = 80, FlatStyle = System.Windows.Forms.FlatStyle.System };
            colBlkGeshi.Items.AddRange(new object[] { "int", "long", "float", "string" });
            this.dgvBlocks.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colBlkIndex, colBlkName, colBlkQishi, colBlkChangdu, colBlkGaodiwei, colBlkGeshi });
            // 
            // tpCameras
            // 
            this.tpCameras.Controls.Add(this.dgvCameras);
            this.tpCameras.Controls.Add(this.lblCamTip);
            this.tpCameras.Location = new System.Drawing.Point(4, 22);
            this.tpCameras.Name = "tpCameras";
            this.tpCameras.Padding = new System.Windows.Forms.Padding(8);
            this.tpCameras.Size = new System.Drawing.Size(876, 633);
            this.tpCameras.TabIndex = 2;
            this.tpCameras.Text = "相机绑定";
            // 
            // lblCamTip
            // 
            this.lblCamTip.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCamTip.Location = new System.Drawing.Point(8, 603);
            this.lblCamTip.Name = "lblCamTip";
            this.lblCamTip.Size = new System.Drawing.Size(860, 22);
            this.lblCamTip.TabIndex = 1;
            this.lblCamTip.Text = "chufa=触发字符；fanhuien=是否回写；fankui=反馈数据块名；chukufangshi=出库方式；chufazhi1/2=触发比较值。";
            // 
            // dgvCameras
            // 
            this.dgvCameras.AllowUserToAddRows = true;
            this.dgvCameras.AllowUserToDeleteRows = true;
            this.dgvCameras.AutoGenerateColumns = false;
            this.dgvCameras.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCameras.Location = new System.Drawing.Point(8, 8);
            this.dgvCameras.Name = "dgvCameras";
            this.dgvCameras.Size = new System.Drawing.Size(860, 595);
            this.dgvCameras.TabIndex = 0;
            var colCamNo = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "CameraNo", HeaderText = "相机号(1~13)", Width = 90 };
            var colCamChufa = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Chufa", HeaderText = "触发字符", Width = 100 };
            var colCamFhz = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Fanhuizhi", HeaderText = "返回值", Width = 90 };
            var colCamFhe = new System.Windows.Forms.DataGridViewCheckBoxColumn() { DataPropertyName = "Fanhuien", HeaderText = "回写", Width = 50 };
            var colCamFk = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Fankui", HeaderText = "反馈数据块", Width = 110 };
            var colCamCk = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Chukufangshi", HeaderText = "出库方式", Width = 90 };
            var colCamCz1 = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Chufazhi1", HeaderText = "触发值1", Width = 90 };
            var colCamCz2 = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Chufazhi2", HeaderText = "触发值2", Width = 90 };
            this.dgvCameras.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colCamNo, colCamChufa, colCamFhz, colCamFhe, colCamFk, colCamCk, colCamCz1, colCamCz2 });
            // 
            // tpScheme
            // 
            this.tpScheme.Controls.Add(this.dgvScheme);
            this.tpScheme.Controls.Add(this.lblSchemeTip);
            this.tpScheme.Location = new System.Drawing.Point(4, 22);
            this.tpScheme.Name = "tpScheme";
            this.tpScheme.Padding = new System.Windows.Forms.Padding(8);
            this.tpScheme.Size = new System.Drawing.Size(876, 633);
            this.tpScheme.TabIndex = 3;
            this.tpScheme.Text = "切型方案";
            // 
            // lblSchemeTip
            // 
            this.lblSchemeTip.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblSchemeTip.Location = new System.Drawing.Point(8, 603);
            this.lblSchemeTip.Name = "lblSchemeTip";
            this.lblSchemeTip.Size = new System.Drawing.Size(860, 22);
            this.lblSchemeTip.TabIndex = 1;
            this.lblSchemeTip.Text = "收到 Change 字符即切换到对应 Path 方案（1~12 个型号）。";
            // 
            // dgvScheme
            // 
            this.dgvScheme.AllowUserToAddRows = false;
            this.dgvScheme.AllowUserToDeleteRows = false;
            this.dgvScheme.AutoGenerateColumns = false;
            this.dgvScheme.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvScheme.Location = new System.Drawing.Point(8, 8);
            this.dgvScheme.Name = "dgvScheme";
            this.dgvScheme.Size = new System.Drawing.Size(860, 595);
            this.dgvScheme.TabIndex = 0;
            var colSchNo = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "No", HeaderText = "型号号", ReadOnly = true, Width = 70 };
            var colSchChange = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Change", HeaderText = "切型字符", Width = 120 };
            var colSchPath = new System.Windows.Forms.DataGridViewTextBoxColumn() { DataPropertyName = "Path", HeaderText = "方案路径", Width = 400 };
            this.dgvScheme.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colSchNo, colSchChange, colSchPath });
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.lblInfo);
            this.pnlBottom.Controls.Add(this.btnClose);
            this.pnlBottom.Controls.Add(this.btnSave);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 659);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(884, 50);
            this.pnlBottom.TabIndex = 1;
            // 
            // lblInfo
            // 
            this.lblInfo.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(10, 16);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(0, 12);
            this.lblInfo.TabIndex = 2;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(784, 10);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(88, 30);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(686, 10);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(88, 30);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "保存并应用";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // FormModbusTcpLinkConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 709);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.pnlBottom);
            this.Name = "FormModbusTcpLinkConfig";
            this.Text = "Modbus-TCP 连接配置";
            this.tabMain.ResumeLayout(false);
            this.tpConn.ResumeLayout(false);
            this.tlpConn.ResumeLayout(false);
            this.tlpConn.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQishi)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numZongchang)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLunxunTime)).EndInit();
            this.tpBlocks.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvBlocks)).EndInit();
            this.tpCameras.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCameras)).EndInit();
            this.tpScheme.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvScheme)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tpConn;
        private System.Windows.Forms.TabPage tpBlocks;
        private System.Windows.Forms.TabPage tpCameras;
        private System.Windows.Forms.TabPage tpScheme;
        private System.Windows.Forms.TableLayoutPanel tlpConn;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblEn;
        private System.Windows.Forms.CheckBox chkEn;
        private System.Windows.Forms.Label lblLunxunEn;
        private System.Windows.Forms.CheckBox chkLunxun;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label lblCell;
        private System.Windows.Forms.TextBox txtCell;
        private System.Windows.Forms.Label lblAbcd;
        private System.Windows.Forms.ComboBox cboAbcd;
        private System.Windows.Forms.Label lblQishi;
        private System.Windows.Forms.NumericUpDown numQishi;
        private System.Windows.Forms.Label lblZongchang;
        private System.Windows.Forms.NumericUpDown numZongchang;
        private System.Windows.Forms.Label lblLunxunTime;
        private System.Windows.Forms.NumericUpDown numLunxunTime;
        private System.Windows.Forms.Label lblBlocksTip;
        private System.Windows.Forms.DataGridView dgvBlocks;
        private System.Windows.Forms.Label lblCamTip;
        private System.Windows.Forms.DataGridView dgvCameras;
        private System.Windows.Forms.Label lblSchemeTip;
        private System.Windows.Forms.DataGridView dgvScheme;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnSave;
    }
}
