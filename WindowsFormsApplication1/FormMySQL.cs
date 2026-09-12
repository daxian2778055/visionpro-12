using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// MySQL 数据库查询窗体（独立窗体，全部操作在后台线程执行，不影响主 CCD 程序性能）
    /// </summary>
    public partial class FormMySQL : Form
    {
        private MySqlConnection _conn;
        private volatile bool _busy;
        private const int MaxRows = 5000;   // 查询结果行数上限，防止大表卡界面

        private string CfgIni
        {
            get { return Path.Combine(Application.StartupPath, "MySQLConfig.ini"); }
        }

        public FormMySQL()
        {
            InitializeComponent();
            txtServer.Text = "127.0.0.1";
            txtPort.Text = "3306";
            LoadConnSettings();
        }

        // 关闭即隐藏，方便反复打开（不销毁，避免反复初始化）
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            // 程序整体关闭时释放常开连接，避免句柄残留
            try { if (_conn != null) { _conn.Close(); _conn.Dispose(); } } catch { }
            _conn = null;
            base.OnFormClosing(e);
        }

        // 安全更新 UI（跨线程）
        private void SafeUi(Action a)
        {
            if (IsDisposed || Disposing) return;
            try
            {
                if (InvokeRequired) BeginInvoke(a);
                else a();
            }
            catch { }
        }

        private void SetStatus(string s)
        {
            lblStatus.Text = s;
        }

        // 连接参数保存/加载（exe 目录 MySQLConfig.ini）
        private void SaveConnSettings()
        {
            try
            {
                var lines = new List<string>
                {
                    "server=" + txtServer.Text.Trim(),
                    "port=" + txtPort.Text.Trim(),
                    "user=" + txtUser.Text.Trim(),
                    "database=" + txtDatabase.Text.Trim(),
                    "rememberpwd=" + (chkRemember.Checked ? "1" : "0")
                };
                if (chkRemember.Checked && !string.IsNullOrEmpty(txtPwd.Text))
                    lines.Add("pwd_enc=" + Protect(txtPwd.Text));   // ★ 2026-09-11：改 DPAPI 加密写盘，不再明文保存口令
                File.WriteAllLines(CfgIni, lines, Encoding.UTF8);
            }
            catch { }
        }

        private void LoadConnSettings()
        {
            try
            {
                if (!File.Exists(CfgIni)) return;
                foreach (string l in File.ReadAllLines(CfgIni, Encoding.UTF8))
                {
                    int i = l.IndexOf('=');
                    if (i < 0) continue;
                    string k = l.Substring(0, i).Trim();
                    string v = l.Substring(i + 1).Trim();
                    switch (k)
                    {
                        case "server": txtServer.Text = v; break;
                        case "port": txtPort.Text = v; break;
                        case "user": txtUser.Text = v; break;
                        case "database": txtDatabase.Text = v; break;
                        case "pwd_enc":
                            try { txtPwd.Text = Unprotect(v); } catch { txtPwd.Text = ""; }
                            break;
                        case "pwd": txtPwd.Text = v; break;
                        case "rememberpwd": chkRemember.Checked = (v == "1"); break;
                    }
                }
                // 未勾选记住密码时不回填密码
            if (!chkRemember.Checked) txtPwd.Text = "";
            }
            catch { }
        }

        // ===== 口令加密（DPAPI，当前用户作用域，依赖本机 Windows 凭据，异地不可解）=====
        // 写盘用 Protect() 加密；读盘优先解 pwd_enc=，旧版本明文 pwd= 仍可读，兼容升级。
        private static string Protect(string plain)
        {
            byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(data);
        }
        private static string Unprotect(string enc)
        {
            byte[] data = ProtectedData.Unprotect(Convert.FromBase64String(enc), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }

        // 判断是否为只读查询语句；其余（UPDATE/DELETE/INSERT/DROP 等）视为写操作
        private static bool IsReadOnlySql(string sql)
        {
            string head = sql.TrimStart().ToUpperInvariant();
            return head.StartsWith("SELECT") || head.StartsWith("SHOW")
                || head.StartsWith("DESC") || head.StartsWith("DESCRIBE")
                || head.StartsWith("EXPLAIN") || head.StartsWith("WITH");
        }

        // 查询结果导出 CSV
        private void btnExport_Click(object sender, EventArgs e)
        {
            var dt = dgvResult.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                SetStatus("没有可导出的数据");
                return;
            }
            using (var dlg = new SaveFileDialog { Filter = "CSV 文件|*.csv", FileName = "查询结果.csv" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine(string.Join(",", dt.Columns.Cast<DataColumn>()
                        .Select(c => "\"" + c.ColumnName.Replace("\"", "\"\"") + "\"")));
                    foreach (DataRow r in dt.Rows)
                    {
                        sb.AppendLine(string.Join(",", dt.Columns.Cast<DataColumn>()
                            .Select(c => "\"" + Convert.ToString(r[c]).Replace("\"", "\"\"") + "\"")));
                    }
                    File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                    SetStatus("已导出：" + dlg.FileName);
                }
                catch (Exception ex)
                {
                    SetStatus("导出失败：" + ex.Message);
                }
            }
        }

        private string BuildConnString()
        {
            var b = new MySqlConnectionStringBuilder
            {
                Server = txtServer.Text.Trim(),
                Port = uint.TryParse(txtPort.Text.Trim(), out uint p) ? p : 3306u,
                UserID = txtUser.Text.Trim(),
                Password = txtPwd.Text,
                Database = txtDatabase.Text.Trim(),
                SslMode = MySqlSslMode.None,
                AllowUserVariables = true
            };
            return b.ConnectionString;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            if (_conn != null && _conn.State == ConnectionState.Open)
            {
                try { _conn.Close(); _conn.Dispose(); } catch { }
                _conn = null;
                SetStatus("已断开连接");
                btnConnect.Text = "连接";
                return;
            }
            _busy = true;
            btnConnect.Enabled = false;
            SetStatus("正在连接...");
            string cs = BuildConnString();
            Task.Run(() =>
            {
                try
                {
                    var c = new MySqlConnection(cs);
                    c.Open();
                    SafeUi(() =>
                    {
                        _conn = c;
                        SetStatus("连接成功：" + txtServer.Text.Trim() + " / " + txtDatabase.Text.Trim());
                        btnConnect.Text = "断开";
                        SaveConnSettings();
                    });
                }
                catch (Exception ex)
                {
                    SafeUi(() => SetStatus("连接失败：" + ex.Message));
                }
                finally
                {
                    SafeUi(() => { _busy = false; btnConnect.Enabled = true; });
                }
            });
        }

        private void btnExec_Click(object sender, EventArgs e)
        {
            if (_conn == null || _conn.State != ConnectionState.Open)
            {
                SetStatus("请先点击“连接”");
                return;
            }
            if (_busy) return;
            string sql = txtSql.Text.Trim();
            if (sql.Length == 0)
            {
                SetStatus("请输入 SQL 语句");
                return;
            }
            // 写语句（UPDATE/DELETE/INSERT/DROP 等）无法撤销，先强制二次确认，避免误触造成数据不可恢复
            bool isQuery = IsReadOnlySql(sql);
            if (!isQuery)
            {
                if (MessageBox.Show("该语句将对数据库执行写入/删除操作（可能不可恢复），是否继续？\n\n" + sql,
                    "确认执行写操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    SetStatus("已取消执行");
                    return;
                }
            }
            _busy = true;
            btnExec.Enabled = false;
            SetStatus("执行中...");
            var conn = _conn;
            Task.Run(() =>
            {
                try
                {
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        if (isQuery)
                        {
                            var dt = new DataTable();
                            using (var da = new MySqlDataAdapter(cmd))
                                da.Fill(0, MaxRows, dt);
                            bool limited = dt.Rows.Count >= MaxRows;
                            SafeUi(() =>
                            {
                                dgvResult.DataSource = dt;
                                SetStatus("查询完成：" + dt.Rows.Count + " 行" + (limited ? "（已达 " + MaxRows + " 行上限，请缩小条件）" : ""));
                            });
                        }
                        else
                        {
                            int n = cmd.ExecuteNonQuery();
                            SafeUi(() =>
                            {
                                dgvResult.DataSource = null;
                                SetStatus("执行完成，影响行数：" + n);
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeUi(() => { dgvResult.DataSource = null; SetStatus("执行失败：" + ex.Message); });
                }
                finally
                {
                    SafeUi(() => { _busy = false; btnExec.Enabled = true; });
                }
            });
        }

        private void txtSql_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter 快捷执行
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnExec_Click(sender, e);
            }
        }
    }
}
