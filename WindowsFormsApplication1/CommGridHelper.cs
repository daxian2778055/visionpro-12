using System;
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
            Dictionary<int, bool> triggerLatch)
        {
            EraseDataSections(wdini, sectionSuffix);
            wdini.WriteString(iniMainSection, "geshu", "0");
            geshu = 0;
            finsDic.Clear();
            triggerLatch?.Clear();
        }

        /// <summary>轮询线程安全写单元格（合并刷新，不阻塞读循环）。</summary>
        public static void SetPollCell(CommGridUiSink sink, Dictionary<int, int[]> finsData, int idx, object value)
        {
            if (sink == null || !finsData.ContainsKey(idx)) return;
            int[] cell = finsData[idx];
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
