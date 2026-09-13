using System;
using System.Data;
using System.Windows.Forms;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// FINS 单条连接（2~4）的配置窗体（阶段 5 收尾：让“连接设备”管理器能真正编辑多实例）。
    ///
    /// 全部控件在 FormFinsLinkConfig.Designer.cs 声明，可直接在 VS 设计器编辑。
    /// 数据用 DataTable 绑定三个 DataGridView（数据块 / 相机绑定 / 切型方案），
    /// 保存时经 <see cref="FinsIniStore"/> 写回本连接的 [finsN] 段族，
    /// 并触发 <see cref="Saved"/> 让主窗体运行时（FormOmron.ReloadFinsLinks）即时生效。
    ///
    /// 连接 1 仍走原 FormOmron 单例窗口，不在此窗体编辑（避免重复配置源）。
    /// </summary>
    public partial class FormFinsLinkConfig : Form
    {
        private ClassIni _ini;
        private int _linkId = 2;

        private readonly DataTable _dtBlocks = new DataTable();
        private readonly DataTable _dtCameras = new DataTable();
        private readonly DataTable _dtScheme = new DataTable();

        /// <summary>保存并应用成功后触发（参数为本连接号），供管理器刷新运行时。</summary>
        public event Action<int> Saved;

        public FormFinsLinkConfig()
        {
            InitializeComponent();
            SetupGrids();
        }

        /// <summary>注入 ini 与要编辑的连接号，并加载当前配置填充界面。</summary>
        public void Attach(ClassIni ini, int linkId)
        {
            _ini = ini;
            _linkId = linkId;
            this.Text = "FINS 连接 " + linkId + " 配置";
            LoadFromIni();
            lblInfo.Text = "";
        }

        // ===================== 网格与数据表绑定 =====================
        // 列已在 FormFinsLinkConfig.Designer.cs 中定义（可直接在 VS 设计器编辑）。
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
            var cfg = FinsIniStore.Load(_ini, _linkId);

            txtName.Text = cfg.Name;
            txtIp.Text = cfg.Ip;
            txtPort.Text = cfg.Port;
            txtCell.Text = cfg.Cell;
            txtLocal.Text = cfg.Local;
            cboAbcd.Text = cfg.Abcd;
            numQishi.Value = Clamp(cfg.Qishi, numQishi);
            numZongchang.Value = Clamp(cfg.Zongchang, numZongchang);
            numLunxunTime.Value = Clamp(cfg.LunxunTime, numLunxunTime);
            chkEn.Checked = cfg.FinsEn;
            chkLunxun.Checked = cfg.FinsLunxunen;

            _dtBlocks.Rows.Clear();
            foreach (var blk in cfg.DataBlocks)
                _dtBlocks.Rows.Add(blk.Index, blk.Name, blk.Qishi.ToString(), blk.Changdu.ToString(), blk.Gaodiwei, blk.Geshi);

            _dtCameras.Rows.Clear();
            foreach (var cam in cfg.CameraBindings)
                _dtCameras.Rows.Add(cam.CameraNo, cam.Chufa, cam.Fanhuizhi, cam.Fanhuien, cam.Fankui, cam.Chukufangshi, cam.Chufazhi1, cam.Chufazhi2);

            _dtScheme.Rows.Clear();
            for (int n = 1; n <= FinsIniStore.MaxChangeTypes; n++)
                _dtScheme.Rows.Add(n,
                    cfg.ChangeTypes.ContainsKey(n) ? cfg.ChangeTypes[n] : "",
                    cfg.SchemePaths.ContainsKey(n) ? cfg.SchemePaths[n] : "");
        }

        // ===================== 保存 =====================
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var cfg = new FinsLinkConfig { LinkId = _linkId };
                cfg.Name = txtName.Text.Trim();
                cfg.Ip = txtIp.Text.Trim();
                cfg.Port = txtPort.Text.Trim();
                cfg.Cell = txtCell.Text.Trim();
                cfg.Local = txtLocal.Text.Trim();
                cfg.Abcd = cboAbcd.Text;
                cfg.Qishi = numQishi.Value;
                cfg.Zongchang = numZongchang.Value;
                cfg.LunxunTime = numLunxunTime.Value;
                cfg.FinsEn = chkEn.Checked;
                cfg.FinsLunxunen = chkLunxun.Checked;

                int idx = 1;
                foreach (DataRow r in _dtBlocks.Rows)
                {
                    if (string.IsNullOrWhiteSpace(Str(r, "Name")) && string.IsNullOrWhiteSpace(Str(r, "Qishi")))
                        continue;
                    cfg.DataBlocks.Add(new FinsDataBlockConfig
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
                    if (camNo < 1 || camNo > FinsIniStore.MaxCameras) continue;
                    cfg.CameraBindings.Add(new FinsCameraBindingConfig
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

                for (int n = 1; n <= FinsIniStore.MaxChangeTypes; n++)
                {
                    cfg.ChangeTypes[n] = "";
                    cfg.SchemePaths[n] = "";
                }
                foreach (DataRow r in _dtScheme.Rows)
                {
                    int no = ToInt(r, "No", 0);
                    if (no < 1 || no > FinsIniStore.MaxChangeTypes) continue;
                    cfg.ChangeTypes[no] = Str(r, "Change");
                    cfg.SchemePaths[no] = Str(r, "Path");
                }

                // 跨连接相机占用防呆：同协议其它连接已占用的相机/功能槽不允许本连接再绑定
                // （相机 13 触发槽=方案切换整协议唯一；13 反馈槽=心跳，各连接独立，不参与占用）
                try { _ini.ReadINIFile(_ini.FileName); } catch { }
                string conflict = CommCameraGuard.CheckFins(_ini, _linkId, cfg.CameraBindings);
                if (conflict != null)
                {
                    lblInfo.Text = "未保存：相机占用冲突。";
                    MessageBox.Show(this, conflict, "相机占用冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ★ 跨协议相机绑定软提示（不硬拦）：本连接要绑的物理相机若已被另一协议绑定，警告但仍保存
                try { _ini.ReadINIFile(_ini.FileName); } catch { }
                foreach (var cb in cfg.CameraBindings)
                {
                    if (cb.CameraNo < 1 || cb.CameraNo > 12) continue;
                    if (!CommCameraGuard.IsBoundValue(cb.Chufa) && !CommCameraGuard.IsBoundValue(cb.Fankui)) continue;
                    string xwarn = CommCameraGuard.CrossProtoWarning(_ini, CommCameraGuard.CommProto.Fins, _linkId, cb.CameraNo);
                    if (!string.IsNullOrEmpty(xwarn))
                    {
                        MessageBox.Show(this, xwarn, "跨协议相机绑定提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }
                }

                FinsIniStore.Save(_ini, cfg);
                lblInfo.Text = "已保存并应用：FINS 连接 " + _linkId;
                Saved?.Invoke(_linkId);
            }
            catch (Exception ex)
            {
                lblInfo.Text = "保存失败：" + ex.Message;
                MessageBox.Show(this, "保存失败：" + ex.Message, "FINS 连接配置", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
