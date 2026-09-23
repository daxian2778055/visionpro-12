using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cognex.VisionPro.PMAlign;
using demo;
using System.Security.Cryptography;
using QRCodeUtil;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.CalibFix;
using System.Globalization;
using WindowsFormsApplication1.Core.Camera;
using WindowsFormsApplication1.Core.Infrastructure;


namespace WindowsFormsApplication1
{
    // Form1 partial class: UI theme palette and theme switching.
    // Moved verbatim from Form1.cs - organizational split only, no logic change.
    public partial class Form1 : Form
    {
        #region UI Theme
        private enum UiThemeId { Default = 0, Light = 1 }

        private sealed class UiThemePalette
        {
            public Color BgMain, BgPanel, BgInput, BgNav, BgImage, Border;
            public Color BtnDefault, BtnPrimary, BtnSuccess, BtnWarning, BtnClose, BtnEnum, BtnAdd, BtnMisc, BtnTheme;
            public Color Text, TextMuted, Accent, AccentDark, Success, Danger;
            public Color GroupTitle, StatusLabel, TabNavUtility, ConfigLabel, ConfigValue;
            public Color MenuSelected, MenuBorder, MenuDropdownBg;

            public static UiThemePalette CreateDefault()
            {
                var p = new UiThemePalette();
                p.BgMain = Color.FromArgb(32, 36, 44);
                p.BgPanel = Color.FromArgb(48, 54, 66);
                p.BgInput = Color.FromArgb(34, 40, 50);
                p.BgNav = Color.FromArgb(40, 46, 58);
                p.BgImage = Color.FromArgb(24, 28, 34);
                p.Border = Color.FromArgb(88, 98, 118);
                p.BtnDefault = Color.FromArgb(108, 120, 142);
                p.BtnPrimary = Color.FromArgb(58, 128, 210);
                p.BtnSuccess = Color.FromArgb(52, 158, 118);
                p.BtnWarning = Color.FromArgb(196, 138, 58);
                p.BtnClose = Color.FromArgb(188, 88, 78);
                p.BtnEnum = Color.FromArgb(92, 112, 148);
                p.BtnAdd = Color.FromArgb(78, 138, 198);
                p.BtnMisc = Color.FromArgb(118, 130, 152);
                p.BtnTheme = Color.FromArgb(108, 92, 168);
                p.Text = Color.FromArgb(232, 236, 241);
                p.TextMuted = Color.FromArgb(154, 164, 178);
                p.Accent = Color.FromArgb(61, 132, 230);
                p.AccentDark = Color.FromArgb(45, 98, 176);
                p.Success = Color.FromArgb(46, 184, 115);
                p.Danger = Color.FromArgb(220, 80, 80);
                p.GroupTitle = Color.FromArgb(150, 195, 255);
                p.StatusLabel = Color.FromArgb(140, 190, 255);
                p.TabNavUtility = Color.FromArgb(92, 104, 124);
                p.ConfigLabel = Color.FromArgb(108, 228, 168);
                p.ConfigValue = Color.FromArgb(228, 234, 242);
                p.MenuSelected = Color.FromArgb(55, 65, 82);
                p.MenuBorder = Color.FromArgb(70, 80, 96);
                p.MenuDropdownBg = Color.FromArgb(42, 48, 58);
                return p;
            }

            public static UiThemePalette CreateLight()
            {
                var p = new UiThemePalette();
                p.BgMain = Color.FromArgb(240, 242, 245);
                p.BgPanel = Color.FromArgb(225, 228, 232);
                p.BgInput = Color.FromArgb(255, 255, 255);
                p.BgNav = Color.FromArgb(218, 222, 228);
                p.BgImage = Color.FromArgb(24, 28, 34);
                p.Border = Color.FromArgb(180, 188, 198);
                p.BtnDefault = Color.FromArgb(140, 148, 162);
                p.BtnPrimary = Color.FromArgb(52, 120, 198);
                p.BtnSuccess = Color.FromArgb(42, 148, 102);
                p.BtnWarning = Color.FromArgb(188, 128, 48);
                p.BtnClose = Color.FromArgb(198, 88, 78);
                p.BtnEnum = Color.FromArgb(108, 118, 138);
                p.BtnAdd = Color.FromArgb(68, 128, 188);
                p.BtnMisc = Color.FromArgb(128, 136, 152);
                p.BtnTheme = Color.FromArgb(108, 88, 158);
                p.Text = Color.FromArgb(32, 36, 42);
                p.TextMuted = Color.FromArgb(100, 108, 118);
                p.Accent = Color.FromArgb(45, 110, 200);
                p.AccentDark = Color.FromArgb(35, 90, 170);
                p.Success = Color.FromArgb(38, 160, 95);
                p.Danger = Color.FromArgb(210, 70, 70);
                p.GroupTitle = Color.FromArgb(28, 100, 175);
                p.StatusLabel = Color.FromArgb(30, 110, 185);
                p.TabNavUtility = Color.FromArgb(148, 156, 168);
                p.ConfigLabel = Color.FromArgb(18, 118, 68);
                p.ConfigValue = Color.FromArgb(32, 38, 48);
                p.MenuSelected = Color.FromArgb(210, 218, 228);
                p.MenuBorder = Color.FromArgb(190, 198, 208);
                p.MenuDropdownBg = Color.FromArgb(248, 250, 252);
                return p;
            }
        }

        private UiThemeId _uiThemeId = UiThemeId.Default;
        private UiThemePalette _p = UiThemePalette.CreateDefault();
        private Button _btnUiThemeSwitch;
        private Button _btnNavConfig;
        private Button _btnNavMonitor;
        private Color UiBgMain => _p.BgMain;
        private Color UiBgPanel => _p.BgPanel;
        private Color UiBgInput => _p.BgInput;
        private Color UiBgNav => _p.BgNav;
        private Color UiBorder => _p.Border;
        private Color UiBtnDefault => _p.BtnDefault;
        private Color UiBtnPrimary => _p.BtnPrimary;
        private Color UiBtnSuccess => _p.BtnSuccess;
        private Color UiBtnWarning => _p.BtnWarning;
        private Color UiText => _p.Text;
        private Color UiTextMuted => _p.TextMuted;
        private Color UiAccent => _p.Accent;
        private Color UiAccentDark => _p.AccentDark;
        private Color UiSuccess => _p.Success;
        private Color UiDanger => _p.Danger;
        private static readonly Font UiFont = new Font("微软雅黑", 9F, FontStyle.Regular);
        private static readonly Font UiFontBold = new Font("微软雅黑", 9F, FontStyle.Bold);
        private static readonly Font UiFontTitle = new Font("微软雅黑", 10.5F, FontStyle.Bold);

        private bool IsLightTheme => _uiThemeId == UiThemeId.Light;

        private void LoadUiThemeFromIni()
        {
            string theme = _config.ReadString("ui", "theme", "0").Trim().ToLowerInvariant();
            if (theme == "1" || theme == "d" || theme == "light" || theme == "浅色")
                _uiThemeId = UiThemeId.Light;
            else
                _uiThemeId = UiThemeId.Default;
            ApplyThemePalette(_uiThemeId);
        }

        private void SaveUiThemeToIni()
        {
            try
            {
                _config.WriteString("ui", "theme", _uiThemeId == UiThemeId.Light ? "1" : "0");
            }
            catch { }
        }

        private void ApplyThemePalette(UiThemeId id)
        {
            _uiThemeId = id;
            _p = id == UiThemeId.Light
                ? UiThemePalette.CreateLight()
                : UiThemePalette.CreateDefault();
        }

        private void EnsureThemeSwitchButton()
        {
            if (tabPage5 == null) return;
            if (_btnUiThemeSwitch == null)
            {
                _btnUiThemeSwitch = new Button();
                _btnUiThemeSwitch.Name = "btnUiThemeSwitch";
                _btnUiThemeSwitch.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                _btnUiThemeSwitch.Location = new Point(310, 0);
                _btnUiThemeSwitch.Size = new Size(130, 30);
                _btnUiThemeSwitch.TabIndex = 200;
                _btnUiThemeSwitch.Click += BtnUiThemeSwitch_Click;
                tabPage5.Controls.Add(_btnUiThemeSwitch);
            }
            UpdateThemeSwitchButtonText();
            StyleActionButton(_btnUiThemeSwitch);
            _btnUiThemeSwitch.BringToFront();
        }

        private void UpdateThemeSwitchButtonText()
        {
            if (_btnUiThemeSwitch == null) return;
            _btnUiThemeSwitch.Text = _uiThemeId == UiThemeId.Light
                ? "风格: 浅色工业"
                : "风格: 标准深色";
        }

        private void BtnUiThemeSwitch_Click(object sender, EventArgs e)
        {
            UiThemeId next = _uiThemeId == UiThemeId.Default ? UiThemeId.Light : UiThemeId.Default;
            ApplyThemePalette(next);
            SaveUiThemeToIni();
            ApplyModernUiTheme();
            RefreshTabNavTheme();
        }

        private void RefreshTabNavTheme()
        {
            if (flowTabNav == null) return;
            flowTabNav.BackColor = UiBgNav;
            foreach (Control c in flowTabNav.Controls)
            {
                if (c is Label sep && sep.Text == "|")
                    sep.ForeColor = UiBorder;
            }
            RefreshTabNavSelection();
        }

        private static Color BlendUiColor(Color a, Color b, float amountB)
        {
            float t = Math.Max(0f, Math.Min(1f, amountB));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private void ApplyTabNavButtonState(Button btn, bool selected, Color baseColor)
        {
            if (btn == null) return;
            Color navBg = UiBgNav;
            if (selected)
            {
                btn.BackColor = ControlPaint.Light(baseColor, 0.06f);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = IsLightTheme
                    ? Color.FromArgb(30, 30, 30)
                    : Color.FromArgb(255, 240, 120);
                btn.Font = UiFontBold;
                btn.Height = 32;
                btn.Margin = new Padding(3, 2, 3, 2);
                btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(baseColor, 0.16f);
                btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(baseColor, 0.05f);
            }
            else
            {
                Color muted = BlendUiColor(baseColor, navBg, 0.65f);
                btn.BackColor = muted;
                btn.ForeColor = IsLightTheme ? Color.FromArgb(88, 92, 100) : Color.FromArgb(168, 176, 190);
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = BlendUiColor(muted, navBg, 0.25f);
                btn.Font = UiFont;
                btn.Height = 28;
                btn.Margin = new Padding(3, 4, 3, 2);
                btn.FlatAppearance.MouseOverBackColor = BlendUiColor(baseColor, navBg, 0.32f);
                btn.FlatAppearance.MouseDownBackColor = BlendUiColor(baseColor, navBg, 0.48f);
            }
        }

        private void RefreshTabNavSelection()
        {
            if (tabControl1 == null || flowTabNav == null) return;
            TabPage selected = tabControl1.SelectedTab;
            ApplyTabNavButtonState(_btnNavConfig, selected == tabPage5, _p.TabNavUtility);
            ApplyTabNavButtonState(_btnNavMonitor, selected == tabPage6, _p.TabNavUtility);
            if (tabNavButtons == null || tabNavPages == null || _tabNavCameraBaseColors == null) return;
            for (int i = 0; i < 12; i++)
            {
                if (tabNavButtons[i] == null) continue;
                ApplyTabNavButtonState(tabNavButtons[i], selected == tabNavPages[i], _tabNavCameraBaseColors[i]);
            }
        }

        private void ApplyModernUiTheme()
        {
            this.BackColor = UiBgMain;
            this.Font = UiFont;
            StyleMenuStrip(menuStrip1);
            StyleRunControlButton(button1, UiSuccess, "运行");
            StyleRunControlButton(button11, UiDanger, "停止");
            StyleStatusBarLabels();
            if (tabControl1 != null)
            {
                tabControl1.BackColor = UiBgPanel;
                tabControl1.Font = UiFont;
                foreach (TabPage page in tabControl1.TabPages)
                {
                    page.BackColor = UiBgPanel;
                    page.ForeColor = UiText;
                }
            }
            if (tableLayoutPanel1 != null)
            {
                tableLayoutPanel1.BackColor = _p.BgImage;
                tableLayoutPanel1.ForeColor = UiText;
            }
            ApplyThemeToControls(this.Controls);
            if (flowTabNav != null)
                flowTabNav.BackColor = UiBgNav;
            EnsureThemeSwitchButton();
            StyleConfigPageLabels();
            RefreshIoLampColors();
        }

        private bool IsDescendantOf(Control ctrl, Control ancestor)
        {
            if (ctrl == null || ancestor == null) return false;
            for (Control p = ctrl.Parent; p != null; p = p.Parent)
                if (p == ancestor) return true;
            return false;
        }

        /// <summary>配置页内实时数值标签（与说明文字区分）。</summary>
        private bool IsConfigPageValueLabel(Label lbl)
        {
            if (lbl == null) return false;
            return lbl == label14 || lbl == label15 || lbl == label24
                || lbl == label31 || lbl == label39;
        }

        private void StyleConfigPageLabelsRecursive(Control parent)
        {
            if (parent == null) return;
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is Label lbl)
                {
                    lbl.BackColor = Color.Transparent;
                    if (lbl.Font.Size < 8.5f || lbl.Font.Name == "宋体")
                        lbl.Font = UiFont;
                    if (lbl == label133)
                        lbl.ForeColor = _p.StatusLabel;
                    else if (IsConfigPageValueLabel(lbl))
                        lbl.ForeColor = _p.ConfigValue;
                    else
                        lbl.ForeColor = _p.ConfigLabel;
                }
                if (ctrl.HasChildren)
                    StyleConfigPageLabelsRecursive(ctrl);
            }
        }

        private void StyleConfigPageLabels()
        {
            if (tabPage5 == null) return;
            StyleConfigPageLabelsRecursive(tabPage5);
        }

        private void StyleStatusBarLabels()
        {
            if (label173 != null)
            {
                label173.ForeColor = UiSuccess;
                label173.Font = UiFontTitle;
                label173.BackColor = Color.Transparent;
            }
            if (label133 != null)
            {
                label133.ForeColor = _p.StatusLabel;
                label133.Font = UiFont;
                label133.BackColor = Color.Transparent;
            }
            if (label2 != null)
            {
                label2.BackColor = Color.Transparent;
                label2.ForeColor = UiText;
            }
        }

        private void StyleMenuStrip(MenuStrip menu)
        {
            if (menu == null) return;
            menu.BackColor = UiBgNav;
            menu.ForeColor = UiText;
            menu.Font = UiFont;
            menu.Renderer = new UiMenuRenderer(_p);
            foreach (ToolStripItem item in menu.Items)
                StyleToolStripItem(item);
        }

        private void StyleToolStripItem(ToolStripItem item)
        {
            if (item == null) return;
            item.ForeColor = UiText;
            item.BackColor = UiBgNav;
            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem sub in menuItem.DropDownItems)
                    StyleToolStripItem(sub);
            }
        }

        private sealed class UiMenuRenderer : ToolStripProfessionalRenderer
        {
            public UiMenuRenderer(UiThemePalette palette) : base(new UiMenuColorTable(palette)) { }
        }

        private sealed class UiMenuColorTable : ProfessionalColorTable
        {
            private readonly UiThemePalette _palette;

            public UiMenuColorTable(UiThemePalette palette)
            {
                _palette = palette;
            }

            public override Color MenuItemSelected => _palette.MenuSelected;
            public override Color MenuItemSelectedGradientBegin => _palette.MenuSelected;
            public override Color MenuItemSelectedGradientEnd => _palette.MenuSelected;
            public override Color MenuItemBorder => _palette.MenuBorder;
            public override Color MenuBorder => _palette.MenuBorder;
            public override Color ToolStripDropDownBackground => _palette.MenuDropdownBg;
            public override Color ImageMarginGradientBegin => _palette.MenuDropdownBg;
            public override Color ImageMarginGradientMiddle => _palette.MenuDropdownBg;
            public override Color ImageMarginGradientEnd => _palette.MenuDropdownBg;
        }

        private void StyleRunControlButton(Button btn, Color baseColor, string text)
        {
            if (btn == null) return;
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = baseColor;
            btn.ForeColor = Color.White;
            btn.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            btn.UseVisualStyleBackColor = false;
            btn.Cursor = Cursors.Hand;
            Color hover = ControlPaint.Light(baseColor, 0.15f);
            Color pressed = ControlPaint.Dark(baseColor, 0.1f);
            btn.FlatAppearance.MouseOverBackColor = hover;
            btn.FlatAppearance.MouseDownBackColor = pressed;
            AttachDisabledOverlay(btn);
        }

        private void ApplyDarkThemeLabelColor(Label lbl, Color fc)
        {
            if (fc == Color.Lime || fc == Color.Green)
                lbl.ForeColor = Color.FromArgb(96, 232, 148);
            else if (fc == Color.Cyan || fc == Color.Teal)
                lbl.ForeColor = Color.FromArgb(120, 210, 255);
            else if (fc == Color.Red || fc == Color.DarkRed)
                lbl.ForeColor = Color.FromArgb(255, 130, 130);
            else if (fc == Color.Orange || fc == Color.DarkOrange)
                lbl.ForeColor = Color.FromArgb(255, 190, 110);
            else if (fc == Color.Yellow)
                lbl.ForeColor = Color.FromArgb(255, 230, 120);
            else if (fc.R + fc.G + fc.B < 120)
                lbl.ForeColor = Color.FromArgb(Math.Max((int)fc.R, 160), Math.Max((int)fc.G, 170), Math.Max((int)fc.B, 180));
        }

        private void ApplyLightThemeLabelColor(Label lbl, Color fc)
        {
            if (fc == Color.Lime || fc == Color.Green)
                lbl.ForeColor = Color.FromArgb(28, 138, 72);
            else if (fc == Color.Cyan || fc == Color.Teal)
                lbl.ForeColor = Color.FromArgb(0, 108, 188);
            else if (fc == Color.Red || fc == Color.DarkRed)
                lbl.ForeColor = Color.FromArgb(200, 48, 48);
            else if (fc == Color.Orange || fc == Color.DarkOrange)
                lbl.ForeColor = Color.FromArgb(180, 100, 20);
            else if (fc == Color.Yellow)
                lbl.ForeColor = Color.FromArgb(160, 120, 0);
            else if (fc.R + fc.G + fc.B > 500)
                lbl.ForeColor = Color.FromArgb(Math.Min((int)fc.R, 80), Math.Min((int)fc.G, 90), Math.Min((int)fc.B, 100));
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            if (controls == null) return;
            foreach (Control ctrl in controls)
            {
                if (ctrl == null || ctrl == flowTabNav) continue;
                string typeName = ctrl.GetType().Name;
                if (typeName.Contains("CogRecordDisplay") || ctrl is PictureBox) continue;

                if (ctrl is GroupBox groupBox)
                {
                    groupBox.BackColor = UiBgPanel;
                    groupBox.ForeColor = _p.GroupTitle;
                    groupBox.Font = UiFontBold;
                }
                else if (ctrl is FlowLayoutPanel flowPanel)
                {
                    flowPanel.BackColor = UiBgPanel;
                    flowPanel.ForeColor = UiText;
                }
                else if (ctrl is Panel || ctrl is TableLayoutPanel)
                {
                    if (ctrl != tableLayoutPanel1)
                        ctrl.BackColor = UiBgPanel;
                    ctrl.ForeColor = UiText;
                }
                else if (ctrl is TabPage tabPage)
                {
                    tabPage.BackColor = UiBgPanel;
                    tabPage.ForeColor = UiText;
                }
                else if (ctrl is Button btn && btn != button1 && btn != button11 && btn != _btnUiThemeSwitch && !IsTabNavButton(btn) && !IsIoLampButton(btn))
                {
                    StyleActionButton(btn);
                }
                else if (ctrl is Label lbl)
                {
                    if (tabPage5 != null && lbl.Parent == tabPage5 || IsDescendantOf(lbl, tabPage5))
                    {
                        // 配置页文字标签统一在 StyleConfigPageLabels 中处理
                    }
                    else
                    {
                        if (lbl.BackColor == SystemColors.GradientActiveCaption || lbl.BackColor == Color.Blue)
                            lbl.BackColor = Color.Transparent;
                        else if (lbl.BackColor != Color.Transparent && lbl.BackColor.A > 0
                            && lbl.BackColor != UiBgMain && lbl.BackColor != UiBgPanel)
                            lbl.BackColor = Color.Transparent;
                        Color fc = lbl.ForeColor;
                        if (lbl == label133)
                            lbl.ForeColor = _p.StatusLabel;
                        else if (fc == SystemColors.ControlText || fc == Color.Black)
                            lbl.ForeColor = UiText;
                        else if (IsLightTheme)
                            ApplyLightThemeLabelColor(lbl, fc);
                        else
                            ApplyDarkThemeLabelColor(lbl, fc);
                        if (lbl.Font.Size < 8.5f || lbl.Font.Name == "宋体")
                            lbl.Font = UiFont;
                    }
                }
                else if (ctrl is TextBox tb)
                {
                    tb.BackColor = UiBgInput;
                    tb.ForeColor = UiText;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.Font = UiFont;
                }
                else if (ctrl is ComboBox cb)
                {
                    cb.BackColor = UiBgInput;
                    cb.ForeColor = UiText;
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.Font = UiFont;
                }
                else if (ctrl is NumericUpDown nud)
                {
                    nud.BackColor = UiBgInput;
                    nud.ForeColor = UiText;
                    nud.Font = UiFont;
                }
                else if (ctrl is CheckBox chk)
                {
                    chk.ForeColor = UiText;
                    chk.BackColor = Color.Transparent;
                    chk.Font = UiFont;
                }
                else if (ctrl is ListBox || ctrl is CheckedListBox)
                {
                    ctrl.BackColor = UiBgInput;
                    ctrl.ForeColor = UiText;
                    ctrl.Font = UiFont;
                }
                else if (ctrl is DataGridView dgv)
                {
                    StyleDataGridView(dgv);
                }

                if (ctrl.HasChildren)
                    ApplyThemeToControls(ctrl.Controls);
            }
        }

        private bool IsTabNavButton(Button btn)
        {
            if (tabNavButtons == null) return false;
            foreach (Button navBtn in tabNavButtons)
                if (navBtn == btn) return true;
            return false;
        }

        /// <summary>
        /// IO 输出状态指示灯按钮清单（相机1~12 的 OK/NG 指示灯，Text 为 "1"/"2"）。
        /// 这些按钮的颜色由 IO_NOK/IO_NNG 运行时实时刷新，皮肤切换不得覆盖。
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> _ioLampButtonNames =
            new System.Collections.Generic.HashSet<string>(new[]
            {
                "button55","button56","button58","button59","button60","button61",
                "button62","button63","button64","button68","button69","button71",
                "button72","button73","button74","button75","button86","button87",
                "button93","button94","button100","button101","button107","button108"
            });

        private static bool IsIoLampButton(Button btn)
        {
            return btn != null && _ioLampButtonNames.Contains(btn.Name);
        }

        /// <summary>
        /// 皮肤切换后按当前 IO 状态重新刷新指示灯颜色（与 IO_NOK/IO_NNG 判定规则一致）。
        /// </summary>
        private void RefreshIoLampColors()
        {
            RefreshIoLamp(button55, _jobs.myjob1.outputok, _jobs.myjob1.outputok2);
            RefreshIoLamp(button56, _jobs.myjob1.outputng, _jobs.myjob1.outputng2);
            RefreshIoLamp(button59, _jobs.myjob2.outputok, _jobs.myjob2.outputok2);
            RefreshIoLamp(button58, _jobs.myjob2.outputng, _jobs.myjob2.outputng2);
            RefreshIoLamp(button61, _jobs.myjob3.outputok, _jobs.myjob3.outputok2);
            RefreshIoLamp(button60, _jobs.myjob3.outputng, _jobs.myjob3.outputng2);
            RefreshIoLamp(button63, _jobs.myjob4.outputok, _jobs.myjob4.outputok2);
            RefreshIoLamp(button62, _jobs.myjob4.outputng, _jobs.myjob4.outputng2);
            RefreshIoLamp(button68, _jobs.myjob5.outputok, _jobs.myjob5.outputok2);
            RefreshIoLamp(button64, _jobs.myjob5.outputng, _jobs.myjob5.outputng2);
            RefreshIoLamp(button71, _jobs.myjob6.outputok, _jobs.myjob6.outputok2);
            RefreshIoLamp(button69, _jobs.myjob6.outputng, _jobs.myjob6.outputng2);
            RefreshIoLamp(button73, _jobs.myjob7.outputok, _jobs.myjob7.outputok2);
            RefreshIoLamp(button72, _jobs.myjob7.outputng, _jobs.myjob7.outputng2);
            RefreshIoLamp(button75, _jobs.myjob8.outputok, _jobs.myjob8.outputok2);
            RefreshIoLamp(button74, _jobs.myjob8.outputng, _jobs.myjob8.outputng2);
            RefreshIoLamp(button87, _jobs.myjob9.outputok, _jobs.myjob9.outputok2);
            RefreshIoLamp(button86, _jobs.myjob9.outputng, _jobs.myjob9.outputng2);
            RefreshIoLamp(button94, _jobs.myjob10.outputok, _jobs.myjob10.outputok2);
            RefreshIoLamp(button93, _jobs.myjob10.outputng, _jobs.myjob10.outputng2);
            RefreshIoLamp(button101, _jobs.myjob11.outputok, _jobs.myjob11.outputok2);
            RefreshIoLamp(button100, _jobs.myjob11.outputng, _jobs.myjob11.outputng2);
            RefreshIoLamp(button108, _jobs.myjob12.outputok, _jobs.myjob12.outputok2);
            RefreshIoLamp(button107, _jobs.myjob12.outputng, _jobs.myjob12.outputng2);
        }

        private static void RefreshIoLamp(Button btn, int v, int v2)
        {
            if (btn == null) return;
            if (v == 0)
                btn.BackColor = Color.Red;                 // 输出断
            else if (v >= 1 && v2 == 1)
                btn.BackColor = Color.Green;               // 输出通
            // 其余中间态（帧判定未完成）保持原色，与 IO_NOK/IO_NNG 行为一致
        }

        private void StyleActionButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.UseVisualStyleBackColor = false;
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.ForeColor = Color.White;
            btn.Font = UiFont;

            string t = btn.Text ?? "";
            string n = btn.Name ?? "";
            Color bg = UiBtnDefault;
            if (t.Contains("打开") || n == "bnOpen" || t.Contains("开始") || t.Contains("启动"))
                bg = UiBtnPrimary;
            else if (t.Contains("关闭") || n == "bnClose" || t.Contains("停止"))
                bg = _p.BtnClose;
            else if (t.Contains("保存") || t.Contains("设置参数") || t.Contains("获取参数"))
                bg = UiBtnSuccess;
            else if (t.Contains("查找") || t.Contains("枚举") || n == "bnEnum")
                bg = _p.BtnEnum;
            else if (t.Contains("清零") || t.Contains("清除") || t.Contains("复位") || t.Contains("清空"))
                bg = UiBtnWarning;
            else if (t.Contains("添加"))
                bg = _p.BtnAdd;
            else if (t.Contains("风格"))
                bg = _p.BtnTheme;
            else if (t.Contains("单次") || t.Contains("使能") || n == "button2" || t.Contains("软件"))
                bg = _p.BtnMisc;

            btn.BackColor = bg;
            btn.FlatAppearance.BorderColor = ControlPaint.Dark(bg, 0.08f);
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, 0.14f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bg, 0.12f);
            AttachDisabledOverlay(btn);
        }

        private static void AttachDisabledOverlay(Button btn)
        {
            btn.Paint -= BtnDisabled_Paint;
            btn.Paint += BtnDisabled_Paint;
            // ★皮肤每切换一次本方法就跑一遍：EnabledChanged 原缺 -=，委托列表无限增长(重复回调+泄漏)
            btn.EnabledChanged -= BtnDisabled_EnabledChanged;
            btn.EnabledChanged += BtnDisabled_EnabledChanged;
        }

        private static void BtnDisabled_EnabledChanged(object sender, EventArgs e)
        {
            if (sender is Button btn)
                btn.Cursor = btn.Enabled ? Cursors.Hand : Cursors.Default;
        }

        private static void BtnDisabled_Paint(object sender, PaintEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Enabled) return;
            // 半透明灰色遮罩 + 斜线纹理
            using (SolidBrush overlay = new SolidBrush(Color.FromArgb(140, Color.LightGray)))
            {
                e.Graphics.FillRectangle(overlay, btn.ClientRectangle);
            }
            // 在禁用按钮中心画一个小禁止图标（圆+斜线）
            Rectangle r = btn.ClientRectangle;
            int cx = r.Width / 2, cy = r.Height / 2;
            int sz = Math.Min(r.Width, r.Height) / 4;
            if (sz > 4)
            {
                using (Pen pen = new Pen(Color.FromArgb(180, 100, 100, 100), 2f))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.DrawEllipse(pen, cx - sz, cy - sz, sz * 2, sz * 2);
                    e.Graphics.DrawLine(pen, cx - sz, cy + sz, cx + sz, cy - sz);
                }
            }
        }

        private void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = UiBgInput;
            dgv.GridColor = UiBorder;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = UiBgNav;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = UiText;
            dgv.ColumnHeadersDefaultCellStyle.Font = UiFontBold;
            dgv.DefaultCellStyle.BackColor = UiBgInput;
            dgv.DefaultCellStyle.ForeColor = UiText;
            dgv.DefaultCellStyle.SelectionBackColor = UiAccentDark;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.RowHeadersDefaultCellStyle.BackColor = UiBgNav;
            dgv.Font = UiFont;
        }

        private Button StyleTabNavButton(string text, Color backColor, int minWidth)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Font = UiFontBold;
            btn.Margin = new Padding(3, 4, 3, 2);
            btn.UseVisualStyleBackColor = false;
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.Padding = new Padding(4, 0, 4, 0);
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.12f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08f);
            Size textSize = TextRenderer.MeasureText(text, btn.Font);
            int w = Math.Max(minWidth, textSize.Width + 18);
            btn.Size = new Size(w, 30);
            return btn;
        }
        #endregion
    }
}
