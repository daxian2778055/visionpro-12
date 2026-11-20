using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Form3（无协议通讯配置窗）视觉主题 —— 自包含的浅色工业风样式。
    /// 本文件只设置纯外观属性（颜色 / 字体 / 边框 / 扁平样式），
    /// 不改动任何控件的 Location / Size / Name / Text / TabIndex / Anchor / Dock，
    /// 不改动事件绑定，也不触碰任何业务逻辑。
    /// 所有配色常量集中在 ThemePalette 区域，便于后续统一微调。
    /// </summary>
    public partial class Form3 : Form
    {
        #region ThemePalette —— 配色常量（集中定义，便于调整/回退）

        // 窗体 / 容器背景
        private static readonly Color ThemeFormBg = Color.FromArgb(0xF5, 0xF6, 0xF8);   // #F5F6F8
        private static readonly Color ThemePanelBg = Color.FromArgb(0xFA, 0xFA, 0xFC);   // #FAFAFC

        // 文字
        private static readonly Color ThemeText = Color.FromArgb(0x34, 0x49, 0x5E);      // #34495E 深灰
        private static readonly Color ThemeGroupText = Color.FromArgb(0x2C, 0x3E, 0x50); // #2C3E50 蓝灰
        private static readonly Color ThemeBtnText = Color.FromArgb(0x1F, 0x4E, 0x79);   // #1F4E79 深蓝
        private static readonly Color ThemeAccent = Color.FromArgb(0x1F, 0x5C, 0x99);    // #1F5C99 小标题蓝

        // 输入框
        private static readonly Color ThemeInputBg = Color.FromArgb(0xFF, 0xFF, 0xFF);   // 纯白
        private static readonly Color ThemeLogBg = Color.FromArgb(0xFB, 0xFB, 0xFD);     // #FBFBFD 极浅灰白
        private static readonly Color ThemeBorder = Color.FromArgb(0xC8, 0xD4, 0xE0);    // #C8D4E0 淡蓝灰边框

        // 按钮
        private static readonly Color ThemeBtnBg = Color.FromArgb(0xDC, 0xE6, 0xF1);         // #DCE6F1 主按钮浅蓝底
        private static readonly Color ThemeBtnBgHover = Color.FromArgb(0xC5, 0xD8, 0xEC);    // 悬停
        private static readonly Color ThemeBtnBgDown = Color.FromArgb(0xAA, 0xC3, 0xDF);     // 按下
        private static readonly Color ThemeBtnBgSecondary = Color.FromArgb(0xEE, 0xF2, 0xF7); // 次要按钮更淡

        #endregion

        #region ThemeFonts —— 字体（Microsoft YaHei 9pt，含回退）

        // ★文字显示不全修复：原 9pt 加粗 Microsoft YaHei 比控件设计时的默认字体更大，
        //   label(高12)/按钮(高23) 装不下 → 文字被裁。统一降到 8pt，与原生控件尺寸匹配。
        private static readonly Font ThemeFontRegular = ResolveThemeFont(8F, FontStyle.Regular);
        private static readonly Font ThemeFontBold = ResolveThemeFont(8F, FontStyle.Bold);

        /// <summary>按“微软雅黑 → Segoe UI → 系统默认”顺序挑选可用字体，找不到时安全回退。</summary>
        private static Font ResolveThemeFont(float size, FontStyle style)
        {
            string[] preferred = { "Microsoft YaHei", "微软雅黑", "Segoe UI", "Microsoft Sans Serif" };
            try
            {
                FontFamily[] families = FontFamily.Families;
                foreach (string name in preferred)
                {
                    foreach (FontFamily family in families)
                    {
                        if (string.Equals(family.Name, name, StringComparison.OrdinalIgnoreCase))
                            return new Font(family, size, style, GraphicsUnit.Point);
                    }
                }
            }
            catch { /* 字体枚举失败时走下面的回退 */ }

            try { return new Font("Microsoft YaHei", size, style, GraphicsUnit.Point); } catch { }
            try { return new Font("Segoe UI", size, style, GraphicsUnit.Point); } catch { }
            try { return new Font(SystemFonts.DefaultFont.FontFamily, size, style, GraphicsUnit.Point); } catch { }
            return SystemFonts.DefaultFont;
        }

        #endregion

        #region 安全应用helper —— 单个控件失败不影响整体

        private static void SafeSkin(Action action)
        {
            if (action == null) return;
            try { action(); } catch { /* 主题是纯装饰，任何控件异常都忽略 */ }
        }

        private void StyleButton(Button button, bool primary)
        {
            SafeSkin(() =>
            {
                if (button == null || button.IsDisposed) return;
                button.FlatStyle = FlatStyle.Flat;
                button.UseVisualStyleBackColor = false;
                button.BackColor = primary ? ThemeBtnBg : ThemeBtnBgSecondary;
                button.ForeColor = ThemeBtnText;
                // 按钮统一用 Regular（不用 Bold）——加粗会撑宽字形，窄按钮(发送/保存/接收数据)的文字易被裁
                button.Font = ThemeFontRegular;
                button.FlatAppearance.BorderColor = ThemeBorder;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.MouseOverBackColor = ThemeBtnBgHover;
                button.FlatAppearance.MouseDownBackColor = ThemeBtnBgDown;
                button.Cursor = Cursors.Hand;
            });
        }

        private void StyleTextBox(TextBox box, bool logLike)
        {
            SafeSkin(() =>
            {
                if (box == null || box.IsDisposed) return;
                box.BorderStyle = BorderStyle.FixedSingle;
                box.BackColor = logLike ? ThemeLogBg : ThemeInputBg;
                box.ForeColor = ThemeText;
                box.Font = ThemeFontRegular;
                // 注意：不设置 ReadOnly（含发送框，ReadOnly 会改变业务行为）；
                // Padding 若当前环境不支持则跳过（try 已包裹）。
                try { box.Padding = new Padding(3, 2, 3, 2); } catch { }
            });
        }

        private void StyleLabel(Label label, bool title)
        {
            SafeSkin(() =>
            {
                if (label == null || label.IsDisposed) return;
                label.BackColor = Color.Transparent;
                label.ForeColor = ThemeText;
                label.Font = title ? ThemeFontBold : ThemeFontRegular;
            });
        }

        private void StyleCameraLabel(Label label)
        {
            SafeSkin(() =>
            {
                if (label == null || label.IsDisposed) return;
                label.BackColor = Color.Transparent;
                label.ForeColor = ThemeAccent;
                label.Font = ThemeFontBold;
            });
        }

        private void StyleGroupBox(GroupBox group)
        {
            SafeSkin(() =>
            {
                if (group == null || group.IsDisposed) return;
                group.BackColor = ThemeFormBg;
                group.ForeColor = ThemeGroupText;
                group.Font = ThemeFontBold;
            });
        }

        private void StyleComboBox(ComboBox combo)
        {
            SafeSkin(() =>
            {
                if (combo == null || combo.IsDisposed) return;
                combo.BackColor = ThemeInputBg;
                combo.ForeColor = ThemeText;
                combo.Font = ThemeFontRegular;
            });
        }

        private void StyleCheckBox(CheckBox check)
        {
            SafeSkin(() =>
            {
                if (check == null || check.IsDisposed) return;
                check.BackColor = Color.Transparent;
                check.ForeColor = ThemeText;
                check.Font = ThemeFontRegular;
            });
        }

        #endregion

        /// <summary>
        /// 应用统一浅色主题。由 Form3_Load 在初始化前调用一次。
        /// 全程只改外观属性；单个控件设置失败由 SafeSkin / 外层 try 吞掉，不影响其它控件。
        /// 特别注意：不设置 this.Font —— 窗体现为 AutoScaleMode=None，坐标 1:1 渲染；
        /// 字体仍逐控件设置，保持可调且与 Designer 坐标解耦。
        /// </summary>
        internal void ApplyTheme()
        {
            if (IsDisposed) return;
            try
            {
                // 窗体本身：仅背景 / 前景色，不碰 Font、不碰 AutoScale
                SafeSkin(() =>
                {
                    this.BackColor = ThemeFormBg;
                    this.ForeColor = ThemeText;
                });

                // GroupBox（SEND / RECEIVE / 相机触发字符表）
                foreach (var group in new[] { groupBox1, groupBox2, groupBox3 })
                    StyleGroupBox(group);

                // 主要按钮（扁平化 + 浅蓝底）
                foreach (var button in new[] { button1, button2, btnSetSp, button6, btnSendData, btnReceive, button5 })
                    StyleButton(button, true);

                // 次要按钮（颜色更淡）
                foreach (var button in new[] { button3, button4, button7, btn_Ok, btnSendFile })
                    StyleButton(button, false);

                // 日志 / 收发文本框（浅底、细边框；含发送框，仅改背景不改 ReadOnly）
                foreach (var box in new[] { textBox1, textBox2, textBox7, textBox8, texSend, txtReceive })
                    StyleTextBox(box, true);

                // 普通输入框
                foreach (var box in new[]
                {
                    textBox3, textBox4, textBox5, textBox6, textBox9, txtFileName,
                    textBox10, textBox11, textBox12, textBox13, textBox14, textBox15, textBox16, textBox17,
                    textBox18, textBox19, textBox20, textBox21, textBox22, textBox23, textBox24, textBox25, textBox26
                })
                    StyleTextBox(box, false);

                // 相机触发字符表内 12 组输入框（浅底）
                foreach (var box in new[]
                {
                    textBoxP1, textBoxP2, textBoxP3, textBoxP4, textBoxP5, textBoxP6,
                    textBoxP7, textBoxP8, textBoxP9, textBoxP10, textBoxP11, textBoxP12
                })
                    StyleTextBox(box, true);

                // 普通标签
                foreach (var label in new[] { label1, label2, label3, label4, label5, label6, label7, label8, label9 })
                    StyleLabel(label, false);

                // 小标题标签（深灰加粗）
                foreach (var label in new[] { label14, label15, label16, label17 })
                    StyleLabel(label, true);

                // 相机小标题（蓝色加粗）
                foreach (var label in new[]
                {
                    labelP1, labelP2, labelP3, labelP4, labelP5, labelP6,
                    labelP7, labelP8, labelP9, labelP10, labelP11, labelP12
                })
                    StyleCameraLabel(label);

                // 下拉框 / 复选框
                foreach (var combo in new[] { comboBox1, comboBox2 })
                    StyleComboBox(combo);
                foreach (var check in new[] { checkBox1, checkBox2, checkBox3, checkBox4 })
                    StyleCheckBox(check);
            }
            catch { /* 兜底：主题失败也不得影响窗体正常加载 */ }
        }
    }
}
