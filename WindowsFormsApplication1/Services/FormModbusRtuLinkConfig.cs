using System;
using System.Data;
using System.Windows.Forms;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Modbus-RTU 单条连接（2~4）的配置窗体（阶段 6：让“连接设备”管理器像 FINS 一样增删多连接）。
    ///
    /// 全部控件在 FormModbusRtuLinkConfig.Designer.cs 声明，可直接在 VS 设计器编辑。
    /// 数据用 DataTable 绑定三个 DataGridView（数据块 / 相机绑定 / 切型方案），
    /// 保存时经 <see cref="ModbusRtuIniStore"/> 写回本连接的 [modbusrtuN] 段族，
    /// 并触发 <see cref="Saved"/> 让管理器把运行时应用到 FormModbusRtu 单例
    /// （写 ini 并即时启停对应连接的后台运行时）。
    ///
    /// 连接 1 仍走原 FormModbusRtu 单例窗口，不在此窗体编辑（避免重复配置源）。
    /// </summary>
    public partial class FormModbusRtuLinkConfig : Form
    {
        private ClassIni _ini;
        private int _linkId = 2;

        private readonly DataTable _dtBlocks = new DataTable();
        private readonly DataTable _dtCameras = new DataTable();
        private readonly DataTable _dtScheme = new DataTable();

        /// <summary>保存并应用成功后触发（参数为本连接号），供管理器刷新运行时。</summary>
        public event Action<int> Saved;

        public FormModbusRtuLinkConfig()
        {
            InitializeComponent();
            SetupGrids();
        }

        /// <summary>注入 ini 与要编辑的连接号，并加载当前配置填充界面。</summary>
        public void Attach(ClassIni ini, int linkId)
        {
            _ini = ini;
            _linkId = linkId;
            Text = "Modbus-RTU 连接 " + linkId + " 配置";
            LoadFromIni();
            lblInfo.Text = "";
        }

        // ===================== 网格与数据表绑定 =====================
        private void SetupGrids()
        {
            // 数据块
            _dtBlocks.Columns.Add("Index", typeof(int));
            _dtBlocks.Columns.Add("Name", typeof(string));
            _dtBlocks.Columns.Add("Qishi", typeof(string));
            _dtBlocks.Columns.Add("Changdu", typeof(string));
            _dtBlocks.Columns.Add("Gaodiwei", typeof(string));
            _dtBlocks.Columns.Add("Geshi", typeof(string));
            dgvBlocks.DataSource = _dtBlocks;
            dgvBlocks.DataError += (s, e) => e.Cancel = true;

            // 相机绑定
            _dtCameras.Columns.Add("CameraNo", typeof(int));
            _dtCameras.Columns.Add("Chufa", typeof(string));
            _dtCameras.Columns.Add("Fanhuizhi", typeof(string));
            _dtCameras.Columns.Add("Fanhuien", typeof(bool));
            _dtCameras.Columns.Add("Fankui", typeof(string));
            _dtCameras.Columns.Add("Chukufangshi", typeof(string));
            _dtCameras.Columns.Add("Chufazhi1", typeof(string));
            _dtCameras.Columns.Add("Chufazhi2", typeof(string));
            dgvCameras.DataSource = _dtCameras;
            dgvCameras.DataError += (s, e) => e.Cancel = true;

            // 切型方案
            _dtScheme.Columns.Add("No", typeof(int));
            _dtScheme.Columns.Add("Change", typeof(string));
            _dtScheme.Columns.Add("Path", typeof(string));
            dgvScheme.DataSource = _dtScheme;
            dgvScheme.DataError += (s, e) => e.Cancel = true;
        }

        // ===================== 加载 =====================
        private void LoadFromIni()
        {
            if (_ini == null) return;
            var cfg = ModbusRtuIniStore.Load(_ini, _linkId);

            txtName.Text = cfg.Name;
            chkEn.Checked = cfg.ModbusEn;
            chkLunxun.Checked = cfg.ModbusLunxunen;
            txtPortName.Text = cfg.PortName;
            txtBaud.Text = cfg.BaudRate;
            txtDataBits.Text = cfg.DataBits;
            cboStopBits.Text = cfg.StopBits;
            cboParity.Text = cfg.Parity;
            txtStation.Text = cfg.Station;
            cboAbcd.Text = cfg.Abcd;
            numQishi.Value = Clamp(cfg.Qishi, numQishi);
            numZongchang.Value = Clamp(cfg.Zongchang, numZongchang);
            numLunxunTime.Value = Clamp(cfg.LunxunTime, numLunxunTime);

            _dtBlocks.Rows.Clear();
            foreach (var blk in cfg.DataBlocks)
                _dtBlocks.Rows.Add(blk.Index, blk.Name, blk.Qishi.ToString(), blk.Changdu.ToString(), blk.Gaodiwei, blk.Geshi);

            _dtCameras.Rows.Clear();
            foreach (var cam in cfg.CameraBindings)
                _dtCameras.Rows.Add(cam.CameraNo, cam.Chufa, cam.Fanhuizhi, cam.Fanhuien, cam.Fankui, cam.Chukufangshi, cam.Chufazhi1, cam.Chufazhi2);

            _dtScheme.Rows.Clear();
            for (int n = 1; n <= ModbusRtuIniStore.MaxChangeTypes; n++)
                _dtScheme.Rows.Add(n,
                    cfg.ChangeTypes.ContainsKey(n) ? cfg.ChangeTypes[n] : "",
                    cfg.SchemePaths.ContainsKey(n) ? cfg.SchemePaths[n] : "");
        }

        // ===================== 保存 =====================
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var cfg = new ModbusRtuLinkConfig { LinkId = _linkId };
                cfg.Name = txtName.Text.Trim();
                cfg.ModbusEn = chkEn.Checked;
                cfg.ModbusLunxunen = chkLunxun.Checked;
                cfg.PortName = txtPortName.Text.Trim();
                cfg.BaudRate = txtBaud.Text.Trim();
                cfg.DataBits = txtDataBits.Text.Trim();
                cfg.StopBits = cboStopBits.Text;
                cfg.Parity = cboParity.Text;
                cfg.Station = txtStation.Text.Trim();
                cfg.Abcd = cboAbcd.Text;
                cfg.Qishi = numQishi.Value;
                cfg.Zongchang = numZongchang.Value;
                cfg.LunxunTime = numLunxunTime.Value;

                int idx = 1;
                foreach (DataRow r in _dtBlocks.Rows)
                {
                    if (string.IsNullOrWhiteSpace(Str(r, "Name")) && string.IsNullOrWhiteSpace(Str(r, "Qishi")))
                        continue;
                    cfg.DataBlocks.Add(new ModbusRtuDataBlockConfig
                    {
                        Index = idx++,
                        Name = Str(r, "Name"),
                        Qishi = ToDec(r, "Qishi", 0),
                        Changdu = ToDec(r, "Changdu", 0),
                        Gaodiwei = Str(r, "Gaodiwei"),
                        Geshi = Str(r, "Geshi")
                    });
                }
                cfg.Geshu = cfg.DataBlocks.Count;

                foreach (DataRow r in _dtCameras.Rows)
                {
                    int camNo = ToInt(r, "CameraNo", 0);
                    if (camNo < 1 || camNo > ModbusRtuIniStore.MaxCameras) continue;
                    cfg.CameraBindings.Add(new ModbusRtuCameraBindingConfig
                    {
                        CameraNo = camNo,
                        Chufa = Str(r, "Chufa"),
                        Fanhuizhi = Str(r, "Fanhuizhi"),
                        Fanhuien = Bool(r, "Fanhuien"),
                        Fankui = Str(r, "Fankui"),
                        Chukufangshi = Str(r, "Chukufangshi"),
                        Chufazhi1 = Str(r, "Chufazhi1"),
                        Chufazhi2 = Str(r, "Chufazhi2")
                    });
                }

                for (int n = 1; n <= ModbusRtuIniStore.MaxChangeTypes; n++)
                {
                    cfg.ChangeTypes[n] = "";
                    cfg.SchemePaths[n] = "";
                }
                foreach (DataRow r in _dtScheme.Rows)
                {
                    int no = ToInt(r, "No", 0);
                    if (no < 1 || no > ModbusRtuIniStore.MaxChangeTypes) continue;
                    cfg.ChangeTypes[no] = Str(r, "Change");
                    cfg.SchemePaths[no] = Str(r, "Path");
                }

                ModbusRtuIniStore.Save(_ini, cfg);
                lblInfo.Text = "已保存并应用：Modbus-RTU 连接 " + _linkId;
                Saved?.Invoke(_linkId);
            }
            catch (Exception ex)
            {
                lblInfo.Text = "保存失败：" + ex.Message;
                MessageBox.Show(this, "保存失败：" + ex.Message, "Modbus-RTU 连接配置", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (this.TopLevel)
                this.Close();
            else
                this.Hide();
        }

        // ===================== 辅助 =====================
        private static string Str(DataRow r, string col)
        {
            if (r.RowState == DataRowState.Deleted) return "";
            var v = r[col];
            return v == null || v == DBNull.Value ? "" : v.ToString().Replace("\0", "").Trim();
        }

        private static int ToInt(DataRow r, string col, int def)
        {
            string s = Str(r, col);
            int v;
            return int.TryParse(s, out v) ? v : def;
        }

        private static decimal ToDec(DataRow r, string col, decimal def)
        {
            string s = Str(r, col);
            decimal v;
            return decimal.TryParse(s, out v) ? v : def;
        }

        private static bool Bool(DataRow r, string col)
        {
            var v = r[col];
            if (v is bool) return (bool)v;
            return Str(r, col) == "True";
        }

        private static decimal Clamp(decimal v, NumericUpDown num)
        {
            if (v < num.Minimum) return num.Minimum;
            if (v > num.Maximum) return num.Maximum;
            return v;
        }
    }
}
