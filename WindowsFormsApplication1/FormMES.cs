using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// MES 通讯工具窗体（类似 Apifox 的 HTTP 接口调试，另预留 WebService/TCP 等通讯方式）
    /// 独立窗体，所有网络操作在后台线程执行，不影响主 CCD 程序性能。
    /// </summary>
    public partial class FormMES : Form
    {
        private volatile bool _busy;

        private string CfgPath
        {
            get { return Path.Combine(Application.StartupPath, "MESConfig.txt"); }
        }

        public FormMES()
        {
            InitializeComponent();
            cmbMethod.Items.AddRange(new object[] { "GET", "POST", "PUT", "DELETE" });
            cmbMethod.SelectedIndex = 0;
            RefreshSavedList();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }

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

        // 收集请求头（名称/值两列，自动忽略空行）
        private List<KeyValuePair<string, string>> CollectHeaders()
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (DataGridViewRow r in dgvHeaders.Rows)
            {
                string k = r.Cells[0].Value == null ? "" : r.Cells[0].Value.ToString().Trim();
                string v = r.Cells[1].Value == null ? "" : r.Cells[1].Value.ToString().Trim();
                if (k.Length > 0)
                    list.Add(new KeyValuePair<string, string>(k, v));
            }
            return list;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            string url = txtUrl.Text.Trim();
            if (url.Length == 0)
            {
                SetStatus("请输入请求 URL");
                return;
            }
            string method = cmbMethod.SelectedItem == null ? "GET" : cmbMethod.SelectedItem.ToString();
            string body = txtBody.Text;
            var headers = CollectHeaders();

            _busy = true;
            btnSend.Enabled = false;
            SetStatus("发送中...");
            txtResponse.Text = "";

            Task.Run(() =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(url);
                    req.Method = method;
                    req.Timeout = 15000;
                    req.ReadWriteTimeout = 30000;
                    req.UserAgent = "MESClient/1.0";
                    if (method == "POST" || method == "PUT")
                    {
                        byte[] data = Encoding.UTF8.GetBytes(body);
                        // Headers 表格里显式填写的 Content-Type 优先，否则默认 JSON
                        string ct = "application/json; charset=utf-8";
                        foreach (var h in headers)
                            if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                                ct = h.Value;
                        req.ContentType = ct;
                        req.ContentLength = data.Length;
                        using (Stream s = req.GetRequestStream())
                            s.Write(data, 0, data.Length);
                    }
                    foreach (var h in headers)
                    {
                        // Content-Type 等受限头由属性设置，跳过避免抛异常
                        if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                        req.Headers[h.Key] = h.Value;
                    }

                    string respText;
                    string statusLine;
                    using (var resp = (HttpWebResponse)req.GetResponse())
                    {
                        statusLine = string.Format("{0} {1}", (int)resp.StatusCode, resp.StatusDescription);
                        using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                            respText = reader.ReadToEnd();
                    }
                    sw.Stop();
                    string final = FormatJsonIfNeeded(respText);
                    string line = statusLine;
                    SafeUi(() =>
                    {
                        txtResponse.Text = final;
                        SetStatus("状态：" + line + "   耗时：" + sw.ElapsedMilliseconds + " ms");
                    });
                }
                catch (WebException wex)
                {
                    string errBody = "";
                    if (wex.Response != null)
                    {
                        try
                        {
                            using (var reader = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8))
                                errBody = reader.ReadToEnd();
                        }
                        catch { }
                    }
                    sw.Stop();
                    string final = FormatJsonIfNeeded(errBody);
                    string msg = wex.Message;
                    SafeUi(() =>
                    {
                        txtResponse.Text = final;
                        SetStatus("错误：" + msg + "   耗时：" + sw.ElapsedMilliseconds + " ms");
                    });
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    string msg = ex.Message;
                    SafeUi(() => SetStatus("错误：" + msg));
                }
                finally
                {
                    SafeUi(() => { _busy = false; btnSend.Enabled = true; });
                }
            });
        }

        // 响应内容若为 JSON 则自动格式化缩进显示（非 JSON 原样显示）
        private static string FormatJsonIfNeeded(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            string t = s.TrimStart();
            if (t.Length == 0 || (t[0] != '{' && t[0] != '[')) return s;
            try
            {
                var sb = new StringBuilder(s.Length + 256);
                int indent = 0;
                bool inStr = false;
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (inStr)
                    {
                        sb.Append(c);
                        if (c == '\\' && i + 1 < s.Length) { sb.Append(s[++i]); continue; }
                        if (c == '"') inStr = false;
                        continue;
                    }
                    switch (c)
                    {
                        case '"': inStr = true; sb.Append(c); break;
                        case '{':
                        case '[':
                            sb.Append(c).Append("\r\n").Append(new string(' ', ++indent * 2));
                            break;
                        case '}':
                        case ']':
                            sb.Append("\r\n").Append(new string(' ', --indent * 2)).Append(c);
                            break;
                        case ',':
                            sb.Append(c).Append("\r\n").Append(new string(' ', indent * 2));
                            break;
                        case ':':
                            sb.Append(": ");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }
                return sb.ToString();
            }
            catch
            {
                return s;   // 解析失败保持原文
            }
        }

        // ============ 接口配置保存（存 exe 目录 MESConfig.txt，方便现场快速复用） ============

        private void RefreshSavedList()
        {
            cmbSaved.Items.Clear();
            if (!File.Exists(CfgPath)) return;
            try
            {
                foreach (string l in File.ReadAllLines(CfgPath, Encoding.UTF8))
                {
                    if (l.Contains("\t"))
                        cmbSaved.Items.Add(l.Split('\t')[0]);
                }
            }
            catch { }
        }

        private void btnSaveCfg_Click(object sender, EventArgs e)
        {
            string name = txtCfgName.Text.Trim();
            if (name.Length == 0)
            {
                SetStatus("请先填写接口名称");
                return;
            }
            string method = cmbMethod.SelectedItem == null ? "GET" : cmbMethod.SelectedItem.ToString();
            var lines = new List<string>();
            if (File.Exists(CfgPath))
            {
                try { lines.AddRange(File.ReadAllLines(CfgPath, Encoding.UTF8)); } catch { }
            }
            lines.RemoveAll(l => l.StartsWith(name + "\t"));

            var sb = new StringBuilder();
            sb.Append(name).Append('\t').Append(method).Append('\t').Append(txtUrl.Text.Trim());
            foreach (var h in CollectHeaders())
                sb.Append('\t').Append(h.Key).Append('\u0002').Append(h.Value);
            // Body 放最后一项（内部换行/制表符替换为空格，避免破坏分隔）
            sb.Append('\t').Append(txtBody.Text.Replace('\t', ' ').Replace("\r\n", " ").Replace('\n', ' '));
            lines.Add(sb.ToString());

            try
            {
                File.WriteAllLines(CfgPath, lines, Encoding.UTF8);
                RefreshSavedList();
                cmbSaved.SelectedItem = name;
                SetStatus("已保存接口：" + name);
            }
            catch (Exception ex)
            {
                SetStatus("保存失败：" + ex.Message);
            }
        }

        private void btnLoadCfg_Click(object sender, EventArgs e)
        {
            string name = cmbSaved.SelectedItem == null ? "" : cmbSaved.SelectedItem.ToString();
            if (name.Length == 0)
            {
                SetStatus("请先在列表选择要载入的接口");
                return;
            }
            if (!File.Exists(CfgPath)) return;
            try
            {
                foreach (string l in File.ReadAllLines(CfgPath, Encoding.UTF8))
                {
                    string[] p = l.Split('\t');
                    if (p.Length >= 3 && p[0] == name)
                    {
                        cmbMethod.SelectedItem = p[1];
                        txtUrl.Text = p[2];
                        dgvHeaders.Rows.Clear();
                        for (int i = 3; i < p.Length - 1; i++)
                        {
                            string[] kv = p[i].Split('\u0002');
                            dgvHeaders.Rows.Add(kv[0], kv.Length > 1 ? kv[1] : "");
                        }
                        txtBody.Text = p[p.Length - 1];
                        txtCfgName.Text = name;
                        SetStatus("已载入接口：" + name);
                        return;
                    }
                }
                SetStatus("未找到接口：" + name);
            }
            catch (Exception ex)
            {
                SetStatus("载入失败：" + ex.Message);
            }
        }

        private void btnDeleteCfg_Click(object sender, EventArgs e)
        {
            string name = cmbSaved.SelectedItem == null ? "" : cmbSaved.SelectedItem.ToString();
            if (name.Length == 0) { SetStatus("请先选择要删除的接口"); return; }
            if (!File.Exists(CfgPath)) return;
            try
            {
                var lines = new List<string>(File.ReadAllLines(CfgPath, Encoding.UTF8));
                lines.RemoveAll(l => l.StartsWith(name + "\t"));
                File.WriteAllLines(CfgPath, lines, Encoding.UTF8);
                RefreshSavedList();
                SetStatus("已删除接口：" + name);
            }
            catch (Exception ex)
            {
                SetStatus("删除失败：" + ex.Message);
            }
        }
    }
}
