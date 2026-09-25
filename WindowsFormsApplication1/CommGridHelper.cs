using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 三通讯页数据配置表格：斑马纹、清除、INI 同步。
    /// </summary>
    public static class CommGridHelper
    {
        public static bool ConfirmDeleteBlock(string name)
        {
            return MessageBox.Show(
                "确定删除数据配置「" + name + "」？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static bool ConfirmClearAll()
        {
            return MessageBox.Show(
                "确定清除全部数据配置及相机绑定？此操作不可撤销。",
                "确认清除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        /// <summary>
        /// 清除按钮两步流程：名称栏有内容时仅清空名称；名称为空时再确认全部清除。
        /// </summary>
        /// <returns>true 表示名称已为空且用户确认，可执行全部清除。</returns>
        public static bool TryPrepareFullClear(string activeName, Action clearNameField)
        {
            if (!string.IsNullOrEmpty(activeName))
            {
                clearNameField?.Invoke();
                MessageBox.Show(
                    "名称已清空。\r\n再次点击「清除」将清空全部数据配置及相机绑定。",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }
            return ConfirmClearAll();
        }

        public static void ClearGridZebra(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.SuspendLayout();
            try
            {
                int rows = Math.Min(dgv.RowCount, 10);
                int cols = Math.Min(dgv.ColumnCount, 10);
                for (int i = 0; i < rows; i++)
                {
                    Color rowColor = ((i + 1) % 2 == 0) ? Color.LightBlue : Color.White;
                    for (int j = 0; j < cols; j++)
                    {
                        dgv[j, i].Style.BackColor = rowColor;
                        dgv[j, i].Value = "";
                    }
                }
            }
            finally
            {
                dgv.ResumeLayout(true);
            }
        }

        /// <summary>
        /// 斑马纹行绘制：不覆盖已占用（绿色）单元格。
        /// </summary>
        public static void RowPrePaintZebra(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (!(sender is DataGridView dgv)) return;
            if ((e.RowIndex + 1) % 2 != 0) return;
            bool hasGreen = false;
            foreach (DataGridViewCell cell in dgv.Rows[e.RowIndex].Cells)
            {
                if (cell.Style.BackColor == Color.Green)
                {
                    hasGreen = true;
                    break;
                }
            }
            if (!hasGreen)
                dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightBlue;
        }

        public static void EraseDataSections(ClassIni wdini, string sectionSuffix, int maxCount = 10)
        {
            for (int i = 1; i <= maxCount; i++)
            {
                string section = i.ToString() + sectionSuffix;
                try { wdini.EraseSection(section); }
                catch { /* 节不存在时忽略 */ }
            }
        }

        public static void UnpaintBlock(
            DataGridView dgv,
            Dictionary<int, int[]> finsName,
            Dictionary<int, int[]> finsData,
            decimal addressQishi,
            decimal qishi,
            decimal changdu)
        {
            for (int i = 0; i < changdu; i++)
            {
                decimal idx = qishi - addressQishi + i;
                int key = int.Parse(idx.ToString());
                if (!finsName.ContainsKey(key) || !finsData.ContainsKey(key)) continue;

                int colN = finsName[key][0];
                int rowN = finsName[key][1];
                int colD = finsData[key][0];
                int rowD = finsData[key][1];
                Color colorN = ((rowN + 1) % 2 == 0) ? Color.LightBlue : Color.White;
                Color colorD = ((rowD + 1) % 2 == 0) ? Color.LightBlue : Color.White;

                dgv[colN, rowN].Style.BackColor = colorN;
                dgv[colN, rowN].Value = "";
                dgv[colD, rowD].Style.BackColor = colorD;
                dgv[colD, rowD].Value = "";
            }
        }

        public static void RewriteDataIni(
            ClassIni wdini,
            string iniMainSection,
            string sectionSuffix,
            Dictionary<string, string[]> finsDic)
        {
            EraseDataSections(wdini, sectionSuffix);
            int i = 1;
            foreach (var kv in finsDic)
            {
                string section = i.ToString() + sectionSuffix;
                string[] v = kv.Value;
                wdini.WriteString(section, "name", v[0]);
                wdini.WriteString(section, "qishi", v[1]);
                wdini.WriteString(section, "changdu", v[2]);
                wdini.WriteString(section, "gaodiwei", v[3]);
                wdini.WriteString(section, "geshi", v[4]);
                i++;
            }
            wdini.WriteString(iniMainSection, "geshu", finsDic.Count.ToString());
        }

        public static void PerformFullDataClear(
            ClassIni wdini,
            string iniMainSection,
            string sectionSuffix,
            ref int geshu,
            Dictionary<string, string[]> finsDic,
            System.Collections.Concurrent.ConcurrentDictionary<int, bool> triggerLatch)
        {
            EraseDataSections(wdini, sectionSuffix);
            wdini.WriteString(iniMainSection, "geshu", "0");
            geshu = 0;
            finsDic.Clear();
            triggerLatch?.Clear();
        }

        /// <summary>
        /// ★P1：轮询线程遍历用的数据块快照。
        /// UI 线程会原地增删/清空 <paramref name="finsDic"/>（例如"清除"会调用 Clear()），
        /// 轮询直接 foreach 会抛 "Collection was modified" 并被外层 catch 吞掉（丢一圈 + 日志刷屏）。
        /// 拷贝失败（正在被修改）返回 null，调用方应静默跳过本圈。
        /// </summary>
        public static Dictionary<string, string[]> SnapshotFinsDic(Dictionary<string, string[]> finsDic)
        {
            try { return finsDic == null ? null : new Dictionary<string, string[]>(finsDic); }
            catch { return null; }
        }

        /// <summary>★P1：相机绑定表快照（与 <see cref="SnapshotFinsDic"/> 同理）。</summary>
        public static Dictionary<int, string[]> SnapshotCameraDic(Dictionary<int, string[]> cameraDic)
        {
            try { return cameraDic == null ? null : new Dictionary<int, string[]>(cameraDic); }
            catch { return null; }
        }

        private static readonly HashSet<string> _dirtyBlocksLogged = new HashSet<string>();

        /// <summary>
        /// ★P5：非法数据块（起始地址/长度解析不了）只允许记一次日志，避免轮询每 20ms 刷屏。
        /// 返回 true 表示这是首次出现，调用方应记一条日志。
        /// </summary>
        public static bool ShouldLogDirtyBlockOnce(string blockKey)
        {
            if (string.IsNullOrEmpty(blockKey)) blockKey = "(未命名)";
            lock (_dirtyBlocksLogged) { return _dirtyBlocksLogged.Add(blockKey); }
        }

        /// <summary>
        /// ★第32轮 S2：改"台账起始地址"（address_qishi / numericUpDown1）前的越界预检。
        /// 数据块在台账里的格位 = 块起始绝对地址 - 起始地址 + i，格位只有 0..ledgerCells-1；
        /// 第32轮 A5 只是把越界的后果从"启动崩溃/静默不轮询"降级为"跳过回绿+日志"，
        /// 根因是这里允许把起始地址改成让已有块算出负数或超 49 的值并持久化。
        /// 返回 null 表示新基准可用；否则返回首个越界块的文字说明，由调用方拒绝本次改动。
        /// </summary>
        public static string FindBlockOutOfRange(Dictionary<string, string[]> finsDic, decimal newBase, int ledgerCells)
        {
            var snap = SnapshotFinsDic(finsDic);
            if (snap == null || ledgerCells <= 0) return null;
            foreach (var kv in snap)
            {
                string[] v = kv.Value;
                if (v == null || v.Length < 3) continue;
                long start, len;
                if (!long.TryParse((v[1] ?? "").Trim(), out start)) continue;   // 脏块由轮询侧 P5 单独报
                if (!long.TryParse((v[2] ?? "").Trim(), out len) || len <= 0) continue;
                long first = start - (long)newBase;
                long last = first + len - 1;
                if (first < 0 || last >= ledgerCells)
                {
                    return "数据块「" + (string.IsNullOrEmpty(kv.Key) ? "(未命名)" : kv.Key) + "」起始 "
                        + start + "、长度 " + len + " 在起始地址 " + newBase + " 下落在台账 0.."
                        + (ledgerCells - 1) + " 之外";
                }
            }
            return null;
        }

        private static readonly HashSet<string> _badIniNumberLogged = new HashSet<string>();

        /// <summary>★第33轮：脏 ini 值只按"位置"记一次日志（where 需自带连接号）。</summary>
        private static void LogBadIniOnce(string where, string raw, string fallbackText, Action<string> log)
        {
            LogOnce(where, "配置项「" + where + "」的值 \"" + raw + "\" 无法解析，已按默认 " + fallbackText + " 处理（手改 ini 所致，请在界面上重新设置后保存）", log);
        }

        /// <summary>★第33轮复审：同一位置只记一次的通用日志（解析失败与超范围共用去重表）。</summary>
        private static void LogOnce(string key, string message, Action<string> log)
        {
            bool first;
            lock (_badIniNumberLogged) { first = _badIniNumberLogged.Add(key ?? ""); }
            if (!first || log == null) return;
            try { log(message); }
            catch { }
        }

        /// <summary>
        /// ★第33轮：ini → 数值 的安全读取（三协议窗体 InitializeForm 的参数读回段）。
        /// 这些位置原先是裸 int.Parse / decimal.Parse：界面上写不出非数字（值恒来自 NumericUpDown），
        /// 但手改 code.ini 成一个非数字串就会在启动巨型 try 内抛 FormatException，后果与第32轮 A5
        /// 修复前同型——连接 1 走全局"程序崩溃"弹窗，连接 2~4 被管理器 try{EnsureHandleCreated()}catch{}
        /// 吞掉且 _formLoaded 已置真 = 该连接永久不轮询。现降级为"取该键本来的默认值 + 记一条日志"。
        /// </summary>
        public static decimal ReadIniDecimal(string raw, decimal fallback, string where, Action<string> log)
        {
            decimal v;
            string s = (raw ?? "").Replace("\0", "").Trim();
            if (decimal.TryParse(s, out v)) return v;
            LogBadIniOnce(where, s, fallback.ToString(), log);
            return fallback;
        }

        /// <summary>
        /// ★第33轮复审③：解析后立刻按 decimal 钳到 [min,max]。
        /// 用于"值是合法数字但量级离谱"的手改 ini（如块长度 changdu=1000000000）：这类值不会抛异常，
        /// 只会让启动回绿循环空转约 10^9 圈（≥2^31 时 int 计数回绕 = 永久死循环），界面写不出、仅手改可触发。
        /// </summary>
        public static decimal ReadIniDecimalBounded(string raw, decimal fallback, decimal min, decimal max, string where, Action<string> log)
        {
            decimal v = ReadIniDecimal(raw, fallback, where, log);
            if (v < min || v > max)
            {
                decimal clamped = v < min ? min : max;
                LogOnce(where + "(范围)", "配置项「" + where + "」的值 " + v + " 超出可用范围 [" + min + ", " + max + "]，已按 " + clamped + " 处理（手改 ini 所致，请在界面上重新设置后保存）", log);
                v = clamped;
            }
            return v;
        }

        /// <summary>
        /// ★第33轮复审②：ini → int 的安全读取。原调用点写法 `int x = (int)ReadIniDecimal(...)` 是受检转换：
        /// 手改 geshu=3000000000 能解析成合法 decimal，但强转 int 抛 OverflowException，
        /// 而写在强转之后的 if (x &gt; 上限) 钳位根本拦不到。这里先钳成 decimal 再强转，强转必然安全。
        /// </summary>
        public static int ReadIniInt(string raw, int fallback, int min, int max, string where, Action<string> log)
        {
            return (int)ReadIniDecimalBounded(raw, fallback, min, max, where, log);
        }

        /// <summary>★第33轮：同上口径的 bool 读回（fins_lunxunen / fins_en 的裸 bool.Parse 同型）。</summary>
        public static bool ReadIniBool(string raw, bool fallback, string where, Action<string> log)
        {
            bool v;
            string s = (raw ?? "").Replace("\0", "").Trim();
            if (bool.TryParse(s, out v)) return v;
            LogBadIniOnce(where, s, fallback.ToString(), log);
            return fallback;
        }

        /// <summary>
        /// ★第33轮：ini→NumericUpDown 赋值前钳位（Form1.cs 的 R7 私有 ClampToNud 同口径）。
        /// "值是数字但超出控件量程"与"值不是数字"是同一崩溃面的两种形态——都发生在启动读回段，
        /// 越界赋值抛 ArgumentOutOfRangeException 同样打断 InitializeForm。
        /// </summary>
        public static decimal ClampToNud(NumericUpDown nud, decimal v)
        {
            if (nud == null) return v;
            if (v < nud.Minimum) return nud.Minimum;
            if (v > nud.Maximum) return nud.Maximum;
            return v;
        }

        /// <summary>轮询线程安全写单元格（合并刷新，不阻塞读循环）。</summary>
        public static void SetPollCell(CommGridUiSink sink, Dictionary<int, int[]> finsData, int idx, object value)
        {
            if (sink == null) return;
            // ★ 2026-09-07：合并 ContainsKey + 索引取值两步为一次 TryGetValue，
            //   消除 TOCTOU 窗口（两步之间他线程若移除该键，索引取值会抛 KeyNotFoundException）。
            int[] cell;
            if (!finsData.TryGetValue(idx, out cell) || cell == null || cell.Length < 2) return;
            sink.Queue(cell[0], cell[1], value);
        }

        public static void ApplyDataTabChrome(TabPage tab, DataGridView dgv, Button clearBtn, Button confirmBtn)
        {
            if (tab == null || dgv == null) return;
            if (clearBtn != null) StyleClearButton(clearBtn);
            if (confirmBtn != null) StyleConfirmButton(confirmBtn);

            foreach (Control c in tab.Controls)
            {
                if (c is Label && c.Text != null && c.Text.Contains("绿色"))
                    return;
            }

            var legend = new Label
            {
                AutoSize = true,
                Text = "■ 绿色 = 已占用",
                ForeColor = Color.DarkGreen,
                Location = new Point(dgv.Left, Math.Max(4, dgv.Top - 18))
            };
            tab.Controls.Add(legend);
            legend.BringToFront();
        }

        public static void StyleClearButton(Button btn)
        {
            if (btn == null) return;
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Color.MistyRose;
        }

        public static void StyleConfirmButton(Button btn)
        {
            if (btn == null) return;
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Color.Honeydew;
        }

        /// <summary>删除数据块后，清除引用该名称的相机绑定（内存 + INI + 下拉框）。</summary>
        public static void RemoveNameFromBindings(
            string name,
            Dictionary<int, string[]> cameraDic,
            ClassIni wdini,
            string camSectionSuffix,
            ComboBox[] bindingCombos)
        {
            if (string.IsNullOrEmpty(name) || cameraDic == null) return;

            foreach (var kv in cameraDic)
            {
                if (kv.Value[0] == name)
                {
                    kv.Value[0] = " ";
                    wdini.WriteString("c" + kv.Key + camSectionSuffix, "chufa", " ");
                }
                if (kv.Value[3] == name)
                {
                    kv.Value[3] = "0";
                    wdini.WriteString("c" + kv.Key + camSectionSuffix, "fankui", "0");
                }
            }

            if (bindingCombos == null) return;
            foreach (var cb in bindingCombos)
            {
                if (cb != null && cb.Text == name)
                    cb.Text = " ";
            }
        }

        public static void RebuildFinsZuhe(
            Dictionary<int, string[]> cameraDic,
            Dictionary<string, string> finsZuhe,
            Label lbl40,
            Label lbl41,
            Label lbl42,
            Label lbl43)
        {
            if (finsZuhe == null || cameraDic == null) return;
            finsZuhe.Clear();
            for (int i = 1; i <= 13; i++)
            {
                if (!cameraDic.ContainsKey(i)) continue;
                string t = (cameraDic[i][0] ?? "").Replace("\0", "").Trim();
                if (t.Length <= 1 || t == "0") continue;
                if (!finsZuhe.ContainsKey(t))
                    finsZuhe.Add(t, i.ToString());
                else
                    finsZuhe[t] += i.ToString();
            }

            Label[] labels = { lbl43, lbl42, lbl41, lbl40 };
            int c = 0;
            foreach (var f in finsZuhe.Keys)
            {
                if (c < labels.Length && labels[c] != null)
                    labels[c].Text = finsZuhe[f] + "_" + f;
                c++;
            }
            for (; c < labels.Length; c++)
            {
                if (labels[c] != null)
                    labels[c].Text = "";
            }
        }
    }
}


