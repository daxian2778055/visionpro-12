using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
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
                    // ★ 2026-09-13 修复：所有自定义请求头必须在 GetRequestStream() 之前设置。
                    //   HttpWebRequest 一旦开始发送请求(GetRequestStream/GetResponse)，之后再设的 Headers
                    //   可能不生效（Authorization 等会丢失）。原实现先写请求体、后设请求头，导致自定义头收不到。
                    foreach (var h in headers)
                    {
                        // Content-Type 等受限头由属性设置，跳过避免抛异常
                        if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                        req.Headers[h.Key] = h.Value;
                    }
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
                            using (var resp = wex.Response)
                            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
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

        // 配置行结构：名称 \t 方法 \t URL \t 头键\u0002头值 \t … \t Body
        // ★第32轮 W7：字段内混入 \t 或 \u0002（粘贴令牌时常带）会让 Split 错位——头被截断、
        //   Body 被当成头，请求内容静默出错。落盘前统一把结构字符替换为空格（HTTP 头值本就不允许这些字符）。
        private static string CleanField(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace('\t', ' ').Replace('\u0002', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }

        // ★第32轮 W6：凭据类请求头（Authorization / 各类 token、key、secret）不落明文。
        // ★第32轮 S3：键名比对放宽到词根（去掉 -/_ 后 contains），覆盖 X-Auth-Token、
        //   Set-Cookie、Bearer、Session-Id、accessKey、client_secret 之类写法。
        //   代价：PublicKey 这类真"非机密"键也会被加密存储——只是文件里看不见，功能不受影响。
        private static bool IsSecretName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string k = name.Replace("-", "").Replace("_", "").Replace(" ", "").ToLowerInvariant();
            return k.Contains("auth") || k.Contains("cookie") || k.Contains("token")
                || k.Contains("secret") || k.Contains("password") || k.Contains("passwd")
                || k.Contains("key") || k.Contains("bearer") || k.Contains("signature")
                || k.Contains("credential") || k.Contains("session");
        }

        private static bool IsSecretHeader(string key)
        {
            return IsSecretName(key);
        }

        /// <summary>★第32轮 S3 + 第33轮：URL 把凭据写进串里的三种形态都要整条加密存放——
        /// ① 查询串参数名命中关键词（?api_key=xxx、&token=yyy）；
        /// ② userinfo 段（http://user:pass@host/...，'@' 落在协议头之后、路径之前）；
        /// ③ fragment 段（#access_token=xxx&token_type=bearer，前端/回调地址常见）。
        /// 只按"参数名/有 userinfo"判定，宁可多加密也不漏；路径里出现 '@'（…/a@b.com/）不算凭据形态。</summary>
        private static bool UrlHasSecret(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            // ② userinfo：'@' 必须出现在 authority 段内（第一个 '/' 之前）
            int scheme = url.IndexOf("://");
            int at = url.IndexOf('@');
            int firstSlash = url.IndexOf('/', scheme < 0 ? 0 : scheme + 3);
            if (at > 0 && (firstSlash < 0 || at < firstSlash)) return true;
            // ① 查询串（到 fragment 为止）
            int q = url.IndexOf('?');
            if (HasSecretParam(q >= 0 ? url.Substring(q + 1) : null, '#')) return true;
            // ③ fragment
            int h = url.IndexOf('#');
            if (HasSecretParam(h >= 0 ? url.Substring(h + 1) : null, '\0')) return true;
            return false;
        }

        /// <summary>按 &amp; 切分一段"k=v&k=v"，逐个键名比对凭据关键词；stop 为段的终止字符（'\0'=不终止）。</summary>
        private static bool HasSecretParam(string segment, char stop)
        {
            if (string.IsNullOrEmpty(segment)) return false;
            if (stop != '\0')
            {
                int cut = segment.IndexOf(stop);
                if (cut >= 0) segment = segment.Substring(0, cut);
            }
            foreach (string pair in segment.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0) continue;
                if (IsSecretName(pair.Substring(0, eq))) return true;
            }
            return false;
        }

        private const string EncPrefix = "enc:";

        // 口令加密（DPAPI，当前用户作用域，依赖本机 Windows 凭据，异地/换用户不可解）
        private static string Protect(string plain)
        {
            byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain ?? ""), null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(data);
        }
        private static string Unprotect(string enc)
        {
            byte[] data = ProtectedData.Unprotect(Convert.FromBase64String(enc), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>★第32轮 S4：只有"形状像 DPAPI base64"的内容才当密文解，
        /// 避免合法值恰好以 enc: 开头（如 enc:hello-world）被误判、解不开又清空。</summary>
        private static bool LooksLikeDpapiBlob(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length % 4 != 0 || s.Length < 32) return false;
            foreach (char c in s)
            {
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')
                    || c == '+' || c == '/' || c == '=')) return false;
            }
            return true;
        }

        /// <summary>★第32轮 S4：载入时解码一个配置字段——带 enc: 且形状像密文才解；
        /// 解不开（换机/换用户拷贝）置 unreadable 并留空，普通明文与偶然以 enc: 开头的原样返回。</summary>
        private static string UnprotectField(string value, ref bool unreadable)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith(EncPrefix)) return value;
            string body = value.Substring(EncPrefix.Length);
            if (!LooksLikeDpapiBlob(body)) return value;   // 不是我们的密文，原样保留
            try { return Unprotect(body); }
            catch { unreadable = true; return ""; }
        }

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
            string name = CleanField(txtCfgName.Text.Trim());
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
            bool secretFailed = false;
            // ★第32轮 S3 + 第33轮：凭据写在查询串（?api_key=…）、userinfo（user:pass@host）
            //   或 fragment（#access_token=…）里同样是明文泄露——整条 URL 加密存放
            string urlField = CleanField(txtUrl.Text.Trim());
            if (UrlHasSecret(urlField))
            {
                try { urlField = EncPrefix + Protect(urlField); }
                catch { secretFailed = true; }
            }
            sb.Append(name).Append('\t').Append(method).Append('\t').Append(urlField);
            foreach (var h in CollectHeaders())
            {
                // ★第32轮 W6：凭据类头的值改为 DPAPI 加密落盘（enc: 前缀），不再明文躺在 exe 目录
                string hv = CleanField(h.Value);
                if (IsSecretHeader(h.Key))
                {
                    try { hv = EncPrefix + Protect(hv); }
                    catch { secretFailed = true; hv = ""; }
                }
                sb.Append('\t').Append(CleanField(h.Key)).Append('\u0002').Append(hv);
            }
            if (secretFailed)
            {
                // 加密不可用时既不落明文、也不静默丢掉凭据——直接拒绝保存
                SetStatus("保存失败：本机凭据加密(DPAPI)不可用，请检查 Windows 用户配置文件");
                return;
            }
            // Body 放最后一项（内部换行/制表符替换为空格，避免破坏分隔）
            // ★第33轮核实口径：Body 明文落盘（DPAPI 加密后现场无法直接查看/复制报文，代价大于收益），
            //   凭据请放请求头或 URL——两者已按 enc: 加密。若日后确需带口令的报文模板，再单独处理。
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
                        bool secretUnreadable = false;
                        cmbMethod.SelectedItem = p[1];
                        txtUrl.Text = UnprotectField(p[2], ref secretUnreadable);   // ★第32轮 S3：URL 也可能整条加密
                        dgvHeaders.Rows.Clear();
                        for (int i = 3; i < p.Length - 1; i++)
                        {
                            string[] kv = p[i].Split('\u0002');
                            // 旧文件里可能还有未消毒的值（含 \u0002）：按键后全部段还原，不截断
                            string hv = kv.Length > 1 ? string.Join("\u0002", kv, 1, kv.Length - 1) : "";
                            // ★第32轮 W6/S4：enc: 且形状像密文的头值才解 DPAPI；
                            //   换机/换用户拷贝时解不开——留空并提示，不能把密文当令牌发出去。
                            hv = UnprotectField(hv, ref secretUnreadable);
                            dgvHeaders.Rows.Add(kv[0], hv);
                        }
                        txtBody.Text = p[p.Length - 1];
                        txtCfgName.Text = name;
                        SetStatus("已载入接口：" + name
                            + (secretUnreadable ? "（含本机无法解密的凭据头，已留空，请重新填写）" : ""));
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
