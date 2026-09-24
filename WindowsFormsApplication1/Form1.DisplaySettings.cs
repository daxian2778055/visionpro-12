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
    // Form1 partial class: display and parameter settings UI handlers.
    // Moved verbatim from Form1.cs - organizational split only, no logic change.
    public partial class Form1 : Form
    {
        #region 界面显示与参数设置
        private void display()
        {
            this.Invoke(new Action(() =>
            {
                int jobCount = (manager1 != null) ? manager1.JobCount : 0;
                TableLayoutPanel[] panels = { null, tableLayoutPanel2, tableLayoutPanel3, tableLayoutPanel5, tableLayoutPanel7, tableLayoutPanel9, tableLayoutPanel12, tableLayoutPanel14, tableLayoutPanel16, tableLayoutPanel18, tableLayoutPanel20, tableLayoutPanel22, tableLayoutPanel24 };
                CogRecordDisplay[] displays = { null, cogRecordDisplay1, cogRecordDisplay2, cogRecordDisplay3, cogRecordDisplay4, cogRecordDisplay5, cogRecordDisplay6, cogRecordDisplay7, cogRecordDisplay8, cogRecordDisplay9, cogRecordDisplay10, cogRecordDisplay11, cogRecordDisplay12 };
                for (int i = 1; i <= 12; i++)
                {
                    bool enabled = (i <= jobCount);
                    _jobs.Myjobs[i - 1].en = enabled ? 1 : 0;
                    _jobs.Myjobs[i - 1].records = 0;  // 切换布局时清除占屏态，避免上一种布局的放大样式残留
                    if (panels[i] != null)
                        panels[i].Visible = enabled;
                    if (displays[i] != null)
                        displays[i].Visible = enabled;
                }
                UpdateTabNavVisibility();
                if (_layoutMode == "Row")
                {
                    ApplyRowLayout(jobCount, panels, displays);
                    this.tableLayoutPanel1.PerformLayout();
                    return;
                }
                // 从行布局切回方格布局时，先恢复设计时的默认行列结构与样式数量
                tableLayoutPanel1.ColumnCount = 3;
                tableLayoutPanel1.RowCount = 4;
                tableLayoutPanel1.ColumnStyles.Clear();
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
                tableLayoutPanel1.RowStyles.Clear();
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 24.5F));
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 24.5F));
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 24.5F));
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 24.5F));
                // 切换布局时，先统一把所有相机窗复位到方格默认位置(3列×4行 row-major)，避免从行布局切回时错位
                for (int i = 1; i < panels.Length; i++)
                {
                    if (panels[i] != null)
                    {
                        tableLayoutPanel1.SetColumnSpan(panels[i], 1);
                        tableLayoutPanel1.SetRowSpan(panels[i], 1);
                        tableLayoutPanel1.SetCellPosition(panels[i], new TableLayoutPanelCellPosition((i - 1) % 3, (i - 1) / 3));
                    }
                }
                // 先重置到默认位置/跨行列，避免切换方案时残留
                tableLayoutPanel1.SetRowSpan(tableLayoutPanel2, 1);
                tableLayoutPanel1.SetColumnSpan(tableLayoutPanel2, 1);
                tableLayoutPanel1.SetCellPosition(tableLayoutPanel5, new TableLayoutPanelCellPosition(2, 0));
                tableLayoutPanel1.SetCellPosition(tableLayoutPanel7, new TableLayoutPanelCellPosition(0, 1));
                if (jobCount < 2)
                {
                    tableLayoutPanel1.SetRowSpan(tableLayoutPanel2, 4);
                    tableLayoutPanel1.SetColumnSpan(tableLayoutPanel2, 3);
                    _jobs.myjob1.record[0] = 1;
                    _jobs.myjob1.record[1] = 1;
                    _jobs.myjob1.record[2] = 1;
                    _jobs.myjob2.record[3] = 0f;
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Absolute, 0F));
                }
                else if (jobCount < 3)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 1F));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 99F));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5F));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5F));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Absolute, 0F));
                    _jobs.myjob1.record[0] = 49.5f;
                    _jobs.myjob1.record[1] = 49.5f;
                    _jobs.myjob1.record[2] = 1f;
                    _jobs.myjob2.record[0] = 99f;
                    _jobs.myjob2.record[1] = 0.5f;
                    _jobs.myjob2.record[2] = 0.5f;
                    _jobs.myjob2.record[3] = 0f;
                }
                else if (jobCount < 5)
                {
                    // 2×2布局：panel5(Cam3)移到(0,1), panel7(Cam4)移到(1,1)
                    tableLayoutPanel1.SetCellPosition(tableLayoutPanel5, new TableLayoutPanelCellPosition(0, 1));
                    tableLayoutPanel1.SetCellPosition(tableLayoutPanel7, new TableLayoutPanelCellPosition(1, 1));
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 1F));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 1F));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Absolute, 0F));
                    _jobs.myjob1.record[0] = 49.5f;
                    _jobs.myjob1.record[1] = 49.5f;
                    _jobs.myjob1.record[2] = 1f;
                    _jobs.myjob2.record[0] = 49.5f;
                    _jobs.myjob2.record[1] = 49.5f;
                    _jobs.myjob2.record[2] = 1f;
                    _jobs.myjob2.record[3] = 0f;

                }
                else if (jobCount < 7)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 34F));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 49.5F));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 1F));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Absolute, 0F));
                    _jobs.myjob1.record[0] = 33f;
                    _jobs.myjob1.record[1] = 33f;
                    _jobs.myjob1.record[2] = 34f;
                    _jobs.myjob2.record[0] = 49.5f;
                    _jobs.myjob2.record[1] = 49.5f;
                    _jobs.myjob2.record[2] = 1f;
                    _jobs.myjob2.record[3] = 0f;
                }
                else if (jobCount < 10)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 34F));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 34F));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Absolute, 0F));
                    _jobs.myjob1.record[0] = 33f;
                    _jobs.myjob1.record[1] = 33f;
                    _jobs.myjob1.record[2] = 34f;
                    _jobs.myjob2.record[0] = 33f;
                    _jobs.myjob2.record[1] = 33f;
                    _jobs.myjob2.record[2] = 34f;
                    _jobs.myjob2.record[3] = 0f;
                }
                else
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 33F));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 34F));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 24.5F));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 24.5F));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 24.5F));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 24.5F));
                    _jobs.myjob1.record[0] = 33f;
                    _jobs.myjob1.record[1] = 33f;
                    _jobs.myjob1.record[2] = 34f;
                    _jobs.myjob2.record[0] = 33f;
                    _jobs.myjob2.record[1] = 33f;
                    _jobs.myjob2.record[2] = 34f;
                    _jobs.myjob2.record[3] = 24.5f;
                }
                this.tableLayoutPanel1.PerformLayout();
            }));
        }
        private void UpdateTabNavVisibility()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateTabNavVisibility));
                return;
            }
            int jobCount = (manager1 != null) ? manager1.JobCount : 0;
            for (int i = 0; i < 12; i++)
            {
                tabNavButtons[i].Visible = (i < jobCount);
            }
        }
        private bool WaitForManagerLoaded()
        {
            _logger.WriteLog("等待方案加载... 当前状态=" + managerState);
            int waitCount = 0;
            while (managerState == ManagerState.Loading && waitCount < 100)
            {
                Thread.Sleep(100);
                waitCount++;
            }
            if (managerState == ManagerState.Loaded)
            {
                _logger.WriteLog("方案加载成功，等待了" + (waitCount * 100) + "ms，JobCount=" + manager1.JobCount);
                return true;
            }
            else if (managerState == ManagerState.Failed)
            {
                _logger.WriteLog("方案已确定加载失败，等待了" + (waitCount * 100) + "ms");
                return false;
            }
            else
            {
                _logger.WriteLog("方案加载超时，等待了" + (waitCount * 100) + "ms后仍在加载中");
                return false;
            }
        }

        private void ApplyRowLayout(int jobCount, TableLayoutPanel[] panels, CogRecordDisplay[] displays)
        {
            // 行布局：每个相机独占一行（单列），相机数 = 行数
            const int cols = 1;
            int rows = jobCount;
            if (rows < 1) rows = 1;

            tableLayoutPanel1.ColumnCount = cols;
            tableLayoutPanel1.RowCount = rows;

            tableLayoutPanel1.ColumnStyles.Clear();
            for (int c = 0; c < cols; c++)
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));

            tableLayoutPanel1.RowStyles.Clear();
            for (int r = 0; r < rows; r++)
                tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

            for (int i = 1; i < panels.Length; i++)
            {
                bool enabled = (i <= jobCount);
                if (panels[i] != null)
                {
                    panels[i].Visible = enabled;
                    if (enabled)
                    {
                        int row = (i - 1) / cols;
                        int col = (i - 1) % cols;
                        tableLayoutPanel1.SetCellPosition(panels[i], new TableLayoutPanelCellPosition(col, row));
                    }
                }
                if (displays[i] != null)
                    displays[i].Visible = enabled;
            }

            _jobs.myjob1.record[0] = 100f / cols;
            _jobs.myjob1.record[1] = 100f / cols;
            _jobs.myjob1.record[2] = 100f / cols;
            _jobs.myjob2.record[0] = 100f / rows;
            _jobs.myjob2.record[1] = 100f / rows;
            _jobs.myjob2.record[2] = 100f / rows;
            _jobs.myjob2.record[3] = 0f;
        }

        private void comboBoxLayoutMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBoxLayoutMode.SelectedItem as string;
            _layoutMode = (selected == "行布局") ? "Row" : "Grid";
            _config.WriteString("Display", "LayoutMode", _layoutMode);
            display();
        }

        private void baoguang_set()
        {
            if (!WaitForManagerLoaded())
            {
                _logger.WriteLog("baoguang_set: managerState=" + managerState + "，跳过曝光设置");
                return;
            }
            this.Invoke(new Action(() =>
            {
                for (int i = 0; i < 12 && i < manager1.JobCount; i++)
                {
                    string camSection = "camera" + (i + 1);
                    bool fromVpp = false;
                    // ★ 优先从VPP block读取曝光值
                    try
                    {
                        if (_jobs.Myjobs[i].block != null && _jobs.Myjobs[i].block.Inputs.Contains("baoguang"))
                        {
                            _jobs.Myjobs[i].baoguang = float.Parse(_jobs.Myjobs[i].block.Inputs["baoguang"].Value.ToString());
                            tbExposure[i].Text = _jobs.Myjobs[i].baoguang.ToString();
                            fromVpp = true;
                            _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 从VPP block读取曝光=" + _jobs.Myjobs[i].baoguang);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 读取VPP曝光失败: " + ex.Message);
                    }
                    // ★ VPP block无baoguang输入时，从code.ini读取曝光值
                    if (!fromVpp)
                    {
                        try
                        {
                            string iniExp = _config.ReadString(camSection, "exposure", "1000");
                            float expVal = float.Parse(iniExp);
                            _jobs.Myjobs[i].baoguang = expVal;
                            tbExposure[i].Text = expVal.ToString();
                            _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 从code.ini读取曝光=" + expVal);
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 从code.ini读取曝光失败: " + ex.Message);
                        }
                    }
                    // ★ 从VPP block读取增益值（如果block有此输入）
                    bool gainFromVpp = false;
                    try
                    {
                        if (_jobs.Myjobs[i].block != null && _jobs.Myjobs[i].block.Inputs.Contains("zengyi"))
                        {
                            float gainVal = float.Parse(_jobs.Myjobs[i].block.Inputs["zengyi"].Value.ToString());
                            switch (i)
                            {
                                case 0: tbGain1.Text = gainVal.ToString(); break;
                                case 1: tbGain2.Text = gainVal.ToString(); break;
                                case 2: tbGain3.Text = gainVal.ToString(); break;
                                case 3: tbGain4.Text = gainVal.ToString(); break;
                                case 4: tbGain5.Text = gainVal.ToString(); break;
                                case 5: tbGain6.Text = gainVal.ToString(); break;
                                case 6: tbGain7.Text = gainVal.ToString(); break;
                                case 7: tbGain8.Text = gainVal.ToString(); break;
                                case 8: tbGain9.Text = gainVal.ToString(); break;
                                case 9: tbGain10.Text = gainVal.ToString(); break;
                                case 10: tbGain11.Text = gainVal.ToString(); break;
                                case 11: tbGain12.Text = gainVal.ToString(); break;
                            }
                            gainFromVpp = true;
                            _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 从VPP block读取增益=" + gainVal);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 读取VPP增益失败: " + ex.Message);
                    }
                    // ★ VPP block无增益输入时，从code.ini读取增益值
                    if (!gainFromVpp)
                    {
                        try
                        {
                            string iniGain = _config.ReadString(camSection, "gain", "1");
                            float gainVal = float.Parse(iniGain);
                            switch (i)
                            {
                                case 0: tbGain1.Text = gainVal.ToString(); break;
                                case 1: tbGain2.Text = gainVal.ToString(); break;
                                case 2: tbGain3.Text = gainVal.ToString(); break;
                                case 3: tbGain4.Text = gainVal.ToString(); break;
                                case 4: tbGain5.Text = gainVal.ToString(); break;
                                case 5: tbGain6.Text = gainVal.ToString(); break;
                                case 6: tbGain7.Text = gainVal.ToString(); break;
                                case 7: tbGain8.Text = gainVal.ToString(); break;
                                case 8: tbGain9.Text = gainVal.ToString(); break;
                                case 9: tbGain10.Text = gainVal.ToString(); break;
                                case 10: tbGain11.Text = gainVal.ToString(); break;
                                case 11: tbGain12.Text = gainVal.ToString(); break;
                            }
                            _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 从code.ini读取增益=" + gainVal);
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog("baoguang_set: 相机" + (i + 1) + " 从code.ini读取增益失败: " + ex.Message);
                        }
                    }
                    // ★ 从code.ini读取帧率（确保bnSetParam_Click不会因空值而跳过）
                    try
                    {
                        string iniRate = _config.ReadString(camSection, "rate", "500");
                        switch (i)
                        {
                            case 0: tbFrameRate1.Text = iniRate; break;
                            case 1: tbFrameRate2.Text = iniRate; break;
                            case 2: tbFrameRate3.Text = iniRate; break;
                            case 3: tbFrameRate4.Text = iniRate; break;
                            case 4: tbFrameRate5.Text = iniRate; break;
                            case 5: tbFrameRate6.Text = iniRate; break;
                            case 6: tbFrameRate7.Text = iniRate; break;
                            case 7: tbFrameRate8.Text = iniRate; break;
                            case 8: tbFrameRate9.Text = iniRate; break;
                            case 9: tbFrameRate10.Text = iniRate; break;
                            case 10: tbFrameRate11.Text = iniRate; break;
                            case 11: tbFrameRate12.Text = iniRate; break;
                        }
                    }
                    catch { }
                }

                try
                {
                    decimal datatemp = 1;
                    decimal.TryParse(_jobs.myjob1.block.Inputs["jiajuhao"].Value.ToString(), out datatemp);
                    numericUpDown3.Value = datatemp;
                    zhuanpanEn = true;
                    numericUpDown3.Visible = true;
                    label73.Visible = true;
                }
                catch
                {
                    zhuanpanEn = false;
                    numericUpDown3.Visible = false;
                    label73.Visible = false;
                }
            }));
        }
        private void trriger_set()
        {
            if (!WaitForManagerLoaded())
            {
                return;
            }
            this.Invoke(new Action(() =>
            {
                if (!dahua)
                {
                    try
                    {
                        if (_jobs.myjob1.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob1.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob1.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                        {
                            comboBox1.Text = _jobs.myjob1.block.Inputs["triggermode"].Value.ToString();
                            _jobs.myjob1.triggerMode = _jobs.myjob1.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                        }
                        else
                        {
                            comboBox1.Text = _config.ReadString("camera1", "triggermode", "连续运行").Replace("\0", "");
                            _jobs.myjob1.triggerMode = _config.ReadString("camera1", "triggermode", "连续运行").Replace("\0", "");
                        }
                    }
                    catch
                    {
                        comboBox1.Text = _config.ReadString("camera1", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob1.triggerMode = _config.ReadString("camera1", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 1)
                        {
                            if (_jobs.myjob2.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob2.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob2.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox4.Text = _jobs.myjob2.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                                _jobs.myjob2.triggerMode = _jobs.myjob2.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox4.Text = _config.ReadString("camera2", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob2.triggerMode = _config.ReadString("camera2", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox4.Text = _config.ReadString("camera2", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob2.triggerMode = _config.ReadString("camera2", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 2)
                        {
                            if (_jobs.myjob3.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob3.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob3.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox5.Text = _jobs.myjob3.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                                _jobs.myjob3.triggerMode = _jobs.myjob3.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox5.Text = _config.ReadString("camera3", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob3.triggerMode = _config.ReadString("camera3", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox5.Text = _config.ReadString("camera3", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob3.triggerMode = _config.ReadString("camera3", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 3)
                        {
                            if (_jobs.myjob4.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob4.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob4.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox8.Text = _jobs.myjob4.block.Inputs["triggermode"].Value.ToString();
                                _jobs.myjob4.triggerMode = _jobs.myjob4.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox8.Text = _config.ReadString("camera4", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob4.triggerMode = _config.ReadString("camera4", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox8.Text = _config.ReadString("camera4", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob4.triggerMode = _config.ReadString("camera4", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 4)
                        {
                            if (_jobs.myjob5.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob5.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob5.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox25.Text = _jobs.myjob5.block.Inputs["triggermode"].Value.ToString();
                                _jobs.myjob5.triggerMode = _jobs.myjob5.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox25.Text = _config.ReadString("camera5", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob5.triggerMode = _config.ReadString("camera5", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox25.Text = _config.ReadString("camera5", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob5.triggerMode = _config.ReadString("camera5", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 5)
                        {
                            if (_jobs.myjob6.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob6.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob6.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox28.Text = _jobs.myjob6.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                                _jobs.myjob6.triggerMode = _jobs.myjob6.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox28.Text = _config.ReadString("camera6", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob6.triggerMode = _config.ReadString("camera6", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox28.Text = _config.ReadString("camera6", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob6.triggerMode = _config.ReadString("camera6", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 6)
                        {
                            if (_jobs.myjob7.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob7.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob7.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox31.Text = _jobs.myjob7.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                                _jobs.myjob7.triggerMode = _jobs.myjob7.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox31.Text = _config.ReadString("camera7", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob7.triggerMode = _config.ReadString("camera7", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox31.Text = _config.ReadString("camera7", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob7.triggerMode = _config.ReadString("camera7", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 7)
                        {
                            if (_jobs.myjob8.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob8.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob8.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox34.Text = _jobs.myjob8.block.Inputs["triggermode"].Value.ToString();
                                _jobs.myjob8.triggerMode = _jobs.myjob8.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox34.Text = _config.ReadString("camera8", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob8.triggerMode = _config.ReadString("camera8", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox34.Text = _config.ReadString("camera8", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob8.triggerMode = _config.ReadString("camera8", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 8)
                        {
                            if (_jobs.myjob9.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob9.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob9.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox43.Text = _jobs.myjob9.block.Inputs["triggermode"].Value.ToString();
                                _jobs.myjob9.triggerMode = _jobs.myjob9.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox43.Text = _config.ReadString("camera9", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob9.triggerMode = _config.ReadString("camera9", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox43.Text = _config.ReadString("camera9", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob9.triggerMode = _config.ReadString("camera9", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 9)
                        {
                            if (_jobs.myjob10.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob10.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob10.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox47.Text = _jobs.myjob10.block.Inputs["triggermode"].Value.ToString();
                                _jobs.myjob10.triggerMode = _jobs.myjob10.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox47.Text = _config.ReadString("camera10", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob10.triggerMode = _config.ReadString("camera10", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox47.Text = _config.ReadString("camera10", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob10.triggerMode = _config.ReadString("camera10", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 10)
                        {
                            if (_jobs.myjob11.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob11.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob11.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox51.Text = _jobs.myjob11.block.Inputs["triggermode"].Value.ToString();
                                _jobs.myjob11.triggerMode = _jobs.myjob11.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox51.Text = _config.ReadString("camera11", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob11.triggerMode = _config.ReadString("camera11", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox51.Text = _config.ReadString("camera11", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob11.triggerMode = _config.ReadString("camera11", "triggermode", "连续运行").Replace("\0", "");
                    }
                    try
                    {
                        if (manager1.JobCount > 11)
                        {
                            if (_jobs.myjob12.block.Inputs["triggermode"].Value.ToString() == "触发拍照" || _jobs.myjob12.block.Inputs["triggermode"].Value.ToString() == "连续运行" || _jobs.myjob12.block.Inputs["triggermode"].Value.ToString() == "通讯触发")
                            {
                                comboBox55.Text = _jobs.myjob12.block.Inputs["triggermode"].Value.ToString();
                                _jobs.myjob12.triggerMode = _jobs.myjob12.block.Inputs["triggermode"].Value.ToString().Replace("\0", "");
                            }
                            else
                            {
                                comboBox55.Text = _config.ReadString("camera12", "triggermode", "连续运行").Replace("\0", "");
                                _jobs.myjob12.triggerMode = _config.ReadString("camera12", "triggermode", "连续运行").Replace("\0", "");
                            }
                        }
                    }
                    catch
                    {
                        comboBox55.Text = _config.ReadString("camera12", "triggermode", "连续运行").Replace("\0", "");
                        _jobs.myjob12.triggerMode = _config.ReadString("camera12", "triggermode", "连续运行").Replace("\0", "");
                    }
                }
            }));
            // ★G4 修复（2026-09-23）：trriger_set 由启动后台线程调用（Form1.cs 启动线程 try{trriger_set();}），
            //   下面 12 路"SDK 触发参数写 + cbSoftTriggerN/bnTriggerExecN 控件"原是裸跨线程代码——
            //   Debug 版一触控件即抛并被逐路 catch 吞：TriggerMode 已写 ON、TriggerSource/按钮态却没执行，
            //   相机静默半瘫（等触发永不采图）。整段收口到 UI 线程执行（三个调用点语义不变，
            //   启动/切方案/bnOpen 均无与本线程并发的开关相机动作，SDK 写不加 _cameraLock 维持原样）。
            this.Invoke(new Action(() =>
            {
            if (!dahua)
            {
                if (manager1.JobCount > 0)
                {
                    try
                    {
                        if (_jobs.myjob1.triggerMode == "连续运行")
                        {
                            _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                            cbSoftTrigger1.Enabled = false;
                            bnTriggerExec1.Enabled = false;
                        }
                        else if (_jobs.myjob1.triggerMode == "触发拍照" || _jobs.myjob1.triggerMode == "通讯触发")
                        {
                            _jobs.myjob1.trrigerEn = true;
                            _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                            // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                            //           1 - Line1;
                            //           2 - Line2;
                            //           3 - Line3;
                            //           4 - Counter;
                            //           7 - Software;
                            if (cbSoftTrigger1.Checked || _jobs.myjob1.triggerMode == "通讯触发")
                            {
                                _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                if (m_bGrabbing1)
                                {
                                    bnTriggerExec1.Enabled = true;
                                }
                            }
                            else
                            {
                                _cameraCtrl.Cameras[0].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                            }
                            cbSoftTrigger1.Enabled = true;

                        }
                    }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换1" + _jobs.myjob1.index);
                    }
                }
         
                    if (manager1.JobCount > 1)
                    {
                        try
                        {
                            if (_jobs.myjob2.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[1].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger2.Enabled = false;
                                bnTriggerExec2.Enabled = false;
                            }
                            else if (_jobs.myjob2.triggerMode == "触发拍照" || _jobs.myjob2.triggerMode == "通讯触发")
                            {
                                _jobs.myjob2.trrigerEn = true;
                                _cameraCtrl.Cameras[1].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger2.Checked || _jobs.myjob2.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[1].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing2)
                                    {
                                        bnTriggerExec2.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[1].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger2.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换2" + _jobs.myjob2.index);
                        }
                    }
                    if (manager1.JobCount > 2)
                    {
                        try
                        {
                            if (_jobs.myjob3.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[2].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger3.Enabled = false;
                                bnTriggerExec3.Enabled = false;
                            }
                            else if (_jobs.myjob3.triggerMode == "触发拍照" || _jobs.myjob3.triggerMode == "通讯触发")
                            {
                                _jobs.myjob3.trrigerEn = true;
                                _cameraCtrl.Cameras[2].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger3.Checked || _jobs.myjob3.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[2].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing3)
                                    {
                                        bnTriggerExec3.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[2].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger3.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            string mcam3 = "null";
                            if (_cameraCtrl.Cameras != null) mcam3 = (_cameraCtrl.Cameras[2] == null ? "null" : "ok");
                            _logger.WriteLog(ex.Message + "触发切换3, 状态: mgr=" + (manager1 == null ? "null" : "ok") + " _jobs.myjob3=" + (_jobs.myjob3 == null ? "null" : "ok") + " m_MyCam[2]=" + mcam3 + " idx=" + _jobs.myjob3?.index);
                        }
                    }
                    if (manager1.JobCount > 3)
                    {
                        try
                        {
                            if (_jobs.myjob4.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[3].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger4.Enabled = false;
                                bnTriggerExec4.Enabled = false;
                            }
                            else if (_jobs.myjob4.triggerMode == "触发拍照" || _jobs.myjob4.triggerMode == "通讯触发")
                            {
                                _jobs.myjob4.trrigerEn = true;
                                _cameraCtrl.Cameras[3].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger4.Checked || _jobs.myjob4.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[3].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing4)
                                    {
                                        bnTriggerExec4.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[3].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger4.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换4" + _jobs.myjob4.index);
                        }
                    }
                    if (manager1.JobCount > 4)
                    {
                        try
                        {
                            if (_jobs.myjob5.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[4].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger5.Enabled = false;
                                bnTriggerExec5.Enabled = false;
                            }
                            else if (_jobs.myjob5.triggerMode == "触发拍照" || _jobs.myjob5.triggerMode == "通讯触发")
                            {
                                _jobs.myjob5.trrigerEn = true;
                                _cameraCtrl.Cameras[4].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger5.Checked || _jobs.myjob5.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[4].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing5)
                                    {
                                        bnTriggerExec5.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[4].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger5.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换5" + _jobs.myjob5.index);
                        }
                    }
                    if (manager1.JobCount > 5)
                    {
                        try
                        {
                            if (_jobs.myjob6.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[5].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger6.Enabled = false;
                                bnTriggerExec6.Enabled = false;
                            }
                            else if (_jobs.myjob6.triggerMode == "触发拍照" || _jobs.myjob6.triggerMode == "通讯触发")
                            {
                                _jobs.myjob6.trrigerEn = true;
                                _cameraCtrl.Cameras[5].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger6.Checked || _jobs.myjob6.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[5].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing6)
                                    {
                                        bnTriggerExec6.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[5].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger6.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换6" + _jobs.myjob6.index);
                        }
                    }
                    if (manager1.JobCount > 6)
                    {
                        try
                        {
                            if (_jobs.myjob7.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[6].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger7.Enabled = false;
                                bnTriggerExec7.Enabled = false;
                            }
                            else if (_jobs.myjob7.triggerMode == "触发拍照" || _jobs.myjob7.triggerMode == "通讯触发")
                            {
                                _jobs.myjob7.trrigerEn = true;
                                _cameraCtrl.Cameras[6].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger7.Checked || _jobs.myjob7.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[6].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing7)
                                    {
                                        bnTriggerExec7.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[6].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger7.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换7" + _jobs.myjob7.index);
                        }
                    }
                    if (manager1.JobCount > 7)
                    {
                        try
                        {
                            if (_jobs.myjob8.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[7].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger8.Enabled = false;
                                bnTriggerExec8.Enabled = false;
                            }
                            else if (_jobs.myjob8.triggerMode == "触发拍照" || _jobs.myjob8.triggerMode == "通讯触发")
                            {
                                _jobs.myjob8.trrigerEn = true;
                                _cameraCtrl.Cameras[7].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger8.Checked || _jobs.myjob8.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[7].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing8)
                                    {
                                        bnTriggerExec8.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[7].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger8.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换8" + _jobs.myjob8.index);
                        }
                    }
                    if (manager1.JobCount > 8)
                    {
                        try
                        {
                            if (_jobs.myjob9.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger9.Enabled = false;
                                bnTriggerExec9.Enabled = false;
                            }
                            else if (_jobs.myjob9.triggerMode == "触发拍照" || _jobs.myjob9.triggerMode == "通讯触发")
                            {
                                _jobs.myjob9.trrigerEn = true;
                                _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger9.Checked || _jobs.myjob9.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing9)
                                    {
                                        bnTriggerExec9.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger9.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换9" + _jobs.myjob9.index);
                        }
                    }
                    if (manager1.JobCount > 9)
                    {
                        try
                        {
                            if (_jobs.myjob10.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger10.Enabled = false;
                                bnTriggerExec10.Enabled = false;
                            }
                            else if (_jobs.myjob10.triggerMode == "触发拍照" || _jobs.myjob10.triggerMode == "通讯触发")
                            {
                                _jobs.myjob10.trrigerEn = true;
                                _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger10.Checked || _jobs.myjob10.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing10)
                                    {
                                        bnTriggerExec10.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger10.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换10" + _jobs.myjob10.index);
                        }
                    }
                    if (manager1.JobCount > 10)
                    {
                        try
                        {
                            if (_jobs.myjob11.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger11.Enabled = false;
                                bnTriggerExec11.Enabled = false;
                            }
                            else if (_jobs.myjob11.triggerMode == "触发拍照" || _jobs.myjob11.triggerMode == "通讯触发")
                            {
                                _jobs.myjob11.trrigerEn = true;
                                _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger11.Checked || _jobs.myjob11.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing11)
                                    {
                                        bnTriggerExec11.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger11.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换11" + _jobs.myjob11.index);
                        }
                    }
                    if (manager1.JobCount > 11)
                    {
                        try
                        {
                            if (_jobs.myjob12.triggerMode == "连续运行")
                            {
                                _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                                cbSoftTrigger12.Enabled = false;
                                bnTriggerExec12.Enabled = false;
                            }
                            else if (_jobs.myjob12.triggerMode == "触发拍照" || _jobs.myjob12.triggerMode == "通讯触发")
                            {
                                _jobs.myjob12.trrigerEn = true;
                                _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                                // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                                //           1 - Line1;
                                //           2 - Line2;
                                //           3 - Line3;
                                //           4 - Counter;
                                //           7 - Software;
                                if (cbSoftTrigger12.Checked || _jobs.myjob12.triggerMode == "通讯触发")
                                {
                                    _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                                    if (m_bGrabbing12)
                                    {
                                        bnTriggerExec12.Enabled = true;
                                    }
                                }
                                else
                                {
                                    _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                                }
                                cbSoftTrigger12.Enabled = true;

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteLog(ex.Message + "触发切换12" + _jobs.myjob12.index);
                        }
                    }
            }
            }));   // ★G4：UI 线程收口 lambda 结尾（对应上面 this.Invoke(new Action(() => {）
            // ★C8 修复：原实现 12 路共用一个 try + 空 catch——任一路（如某路 block 缺 triggerZifu
            //   输入口）抛异常会让其后所有路都不再执行、静默沿用旧触发字符，通讯触发整链无声失配。
            //   改为逐路独立 try + 日志，单路异常不影响其它路；并先用 Inputs.Contains 做存在性检查。
            {
                var _mjArrTz = new Myjob[] { _jobs.myjob1, _jobs.myjob2, _jobs.myjob3, _jobs.myjob4, _jobs.myjob5, _jobs.myjob6, _jobs.myjob7, _jobs.myjob8, _jobs.myjob9, _jobs.myjob10, _jobs.myjob11, _jobs.myjob12 };
                for (int _ti = 0; _ti < _mjArrTz.Length; _ti++)
                {
                    var _mjTz = _mjArrTz[_ti];
                    if (_mjTz == null) continue;
                    try
                    {
                        if (_mjTz.triggerMode == "通讯触发" && _mjTz.block != null && _mjTz.block.Inputs.Contains("triggerZifu"))
                            _mjTz.triggerZifu = _mjTz.block.Inputs["triggerZifu"].Value.ToString();
                    }
                    catch (Exception exTz)
                    {
                        _logger.WriteLog("相机" + (_ti + 1) + " triggerZifu 同步失败(不影响其它路): " + exTz.Message);
                    }
                }
            }


        }
        // ★第26轮#24：存图文件名消毒 + 同秒防覆盖。
        //   原名 = cuowuma + hhmmss + "#" + JobNumber，cuowuma 直接取 VisionPro
        //   block.Outputs["tishi"] 自由文本（Form1.cs:4714）。含 ':' '/' 换行等非法字符时
        //   CogImageFileBMP.Open 必抛，而整个方法只有一个大 try→写一行日志，NG 图从此永不落盘
        //   且界面毫无提示（质量追溯断链）。另外连续运行模式下 JobNumber(numberng) 不递增，
        //   同一秒内多帧文件名完全相同→后帧覆盖前帧，同样丢 NG 图。
        //   NG 原因统计表仍用原始 tishi 文本（Form1.cs:5014），此处只改文件名不改统计口径。
        private static readonly char[] _invalidNameChars = BuildInvalidNameChars();

        private static char[] BuildInvalidNameChars()
        {
            var cs = new List<char>(Path.GetInvalidFileNameChars());
            if (!cs.Contains('/')) cs.Add('/');
            return cs.ToArray();
        }

        // 写方与回构方（chatu_fangfa、相机2 列表打开）必须用同一规则，否则按名找不到图。
        private static string SafeNamePart(string raw)
        {
            string s = (raw ?? "").Trim();
            if (s.Length == 0) return "NG";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(c < 32 || Array.IndexOf(_invalidNameChars, c) >= 0 ? '_' : c);
            // Windows 会静默剥离文件名尾部的点和空格，不剥会让"写入名"与"回构名"不一致
            string r = sb.ToString().TrimEnd('.', ' ');
            if (r.Length == 0) return "NG";
            return r.Length > 50 ? r.Substring(0, 50) : r;
        }

        // ★第26轮#25：存图失败不再静默——累计计数 + 30 秒限流的日志与界面提示。
        //   原实现仅 _logger.WriteLog("存图方法"+msg)，磁盘满/共享路径断开时可连续数小时
        //   一张图都不落盘，操作员完全不知情。
        private long _cuntuFailTotal;
        private int _cuntuLastAlertTick;

        private void NoteSaveImageFailure(Exception ex, string Jobpath, string cuowuma)
        {
            long total = Interlocked.Increment(ref _cuntuFailTotal);
            int now = Environment.TickCount;
            int last = Volatile.Read(ref _cuntuLastAlertTick);
            if (last != 0 && unchecked(now - last) < 30000) return;
            // 并发多路只允许一路抢到本次告警窗口
            if (Interlocked.CompareExchange(ref _cuntuLastAlertTick, now, last) != last) return;
            string hint = SafeNamePart(cuowuma);
            _logger.WriteLog("存图失败(累计 " + total + " 张，请检查磁盘空间/路径可写性/文件名合法性) 目录="
                + Jobpath + " 名称前缀=" + hint + " 错误=" + ex.Message);
            string txt = "存图失败：图像未落盘(累计" + total + "张)，请检查磁盘空间与存图路径";
            try { SafeBeginInvoke(new Action(() => { try { label133.Text = txt; } catch { } })); } catch { }
        }

        private void cuntu_fangfa(CogImageFileBMP cogbmp, int FileLength, int JobNumber, string Jobpath, string cuowuma, string temptime, ICogImage cogimage)
        {
            // 2026-09-06：cogbmp 是 myjob.Cogbmp 共享实例，多帧 Task 并发会 Open/Append/Close 冲突。
            // 改为每次调用新建本地 writer，避免写方竞争。保留参数以兼容旧调用点。
            try
            {
                string[] time111 = temptime.Split(':');
                int ttt1 = int.Parse(time111[0] + time111[1] + time111[2]);
                string dir = Jobpath + day1;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);   // 原目录缺失时 GetFileSystemEntries/Open 直接抛→整段被吞
                string stem = SafeNamePart(cuowuma) + ttt1 + "#" + JobNumber;
                string fullName = dir + "\\" + stem + ".bmp";
                for (int seq = 1; File.Exists(fullName) && seq <= 999; seq++)
                    fullName = dir + "\\" + stem + "+" + seq + ".bmp";
                using (var writer = new CogImageFileBMP())
                {
                    if (FileLength < zhangshu || cuntu == 1)
                    {
                        // _jobs.myjob3.number = 1;
                        writer.Open(fullName, CogImageFileModeConstants.Write);
                        writer.Append(cogimage);
                    }
                    else
                    {

                        double dt2 = 0;
                        double dt3 = 0;
                        string ffff = "f";
                        DateTime dt1;
                        foreach (string f in Directory.GetFileSystemEntries(dir))
                        {
                            if (File.Exists(f))
                            {
                                // ★ 2026-09-12：用"最后写入时间"替代"创建时间"定位最旧图。
                                //   CreationTime 在部分文件系统/首次写入后才生成，容易被复制或临时创建时间误导而删错；
                                //   LastWriteTime 反映真实存图时刻，作为"最旧图应被淘汰"判据更准。纯读操作，低风险。
                                dt1 = Directory.GetLastWriteTime(f);
                                dt2 = DateTime.Now.Subtract(dt1).TotalMinutes;
                                if (dt2 > dt3)
                                {
                                    dt3 = dt2;
                                    ffff = f;
                                }
                            }
                        }
                        //如果有子文件删除文件
                        if (ffff != "f")
                            File.Delete(ffff);
                        writer.Open(fullName, CogImageFileModeConstants.Write);
                        writer.Append(cogimage);
                    }
                }
            }
            catch (Exception ex)
            {
                NoteSaveImageFailure(ex, Jobpath, cuowuma);
            }
        }
        private void chatu_fangfa(int jobhao, Myjob myjob, string list_time)
        {
            Task.Run(() =>
            {
                try
                {
                    string[] time111 = list_time.Split(':');
                    int ttt1 = int.Parse(time111[0] + time111[1] + time111[2]);
                    string ttt2 = time111[3];
                    int ttt3 = int.Parse(time111[4]);
                    Process myProc = null;
                    myProc = Process.Start(myjob.pathhead_ng + day1 + "\\" + SafeNamePart(ttt2) + ttt1 + "#" + ttt3 + ".bmp");//开启一个进程
                    try
                    {
                        myProc.Kill();//关闭一个进程
                    }
                    catch { }
                }
                catch (Exception ex)
                { _logger.WriteLog(ex.Message + "图片显示" + jobhao); };
            });
        }
        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (_jobs.yunxing == false)
                    _jobs.myjob1.block.Inputs["jiajuhao"].Value = numericUpDown3.Value;
            }
            catch { }
        }
        private void jiankong_Huamian(Myjob myjob)
        {
            // ★G4 修复（2026-09-23）：ControlCollection 枚举器不做版本校验——循环内 Close() 会把窗体从
            //   panel1.Controls 移除，原 foreach 会静默跳过后一个元素（≥2 个嵌入式子窗时永远关不干净、
            //   旧 Form8 逐次堆积）。先快照收集再逐个关闭，并各自 try 防单窗关闭异常中断整轮。
            var formsToClose = new System.Collections.Generic.List<Form>();
            for (int ci = 0; ci < panel1.Controls.Count; ci++)
                if (panel1.Controls[ci] is Form) formsToClose.Add(panel1.Controls[ci] as Form);
            foreach (Form f in formsToClose)
            {
                try { f.Close(); } catch { }
            }
            Form8 frm8 = new Form8(myjob.block);
            frm8.TopLevel = false;
            frm8.FormBorderStyle = FormBorderStyle.None;
            frm8.Parent = this.panel1;
            frm8.Dock = DockStyle.Fill;
            frm8.Show();
            this.Invoke(new Action(() =>
            {
                groupBox1.Visible = false;
                groupBox5.Visible = false;
                groupBox7.Visible = false;
                groupBox8.Visible = false;
                groupBox18.Visible = false;
                groupBox19.Visible = false;
                groupBox20.Visible = false;
                groupBox21.Visible = false;
                groupBox22.Visible = false;
                groupBox27.Visible = false;
                groupBox28.Visible = false;
                groupBox29.Visible = false;
            }));
        }
        private void daoqi_jiankong()
        {
            int dayt = 0;
            int dayz = 0;
            int authV1 = 0;   // ★ 2026-09-07：授权到期日期（与 Form1_Load 同一门控口径）
            string zhongjian = "22";

            string code = _config.ReadString("code1", "code2", "");
            if (code == "")
            {
                Thread.Sleep(20);
                code = _config.ReadString("code1", "code2", "");
                if (code == "")
                {
                    _logger.WriteLog("监控解码: 未读到code2");
                    return; // ★ 无code则直接返回，不再弹MessageBox（后台线程不能弹窗）
                }
            }

            // ★ 带重试读取test.ini
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    duini.ReadINIFile("C:\\Program Files\\test.ini");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("监控解码: 读test.ini失败(第" + (attempt + 1) + "次): " + ex.Message);
                    if (attempt < 2)
                    {
                        try { string d = Path.GetDirectoryName("C:\\Program Files\\test.ini"); if (!Directory.Exists(d)) Directory.CreateDirectory(d); } catch { }
                        Thread.Sleep(100);
                    }
                }
            }

            string code1 = duini.ReadString("1", "2", "");
            string code2_val = duini.ReadString("1", "3", "");
            string code3 = duini.ReadString("1", "4", "");
            string beizhu = duini.ReadString("1", "1", "");
            try { this.Invoke(new Action(() => { textBox7.Text = beizhu; })); } catch { }

            // ★ 过期警告，用 today 作为基准（c3 是上次运行日期，尚未更新）
            Thread.Sleep(5);
            try
            {
                int c1 = 0;
                int today = (int)DateTime.Now.ToOADate();
                int.TryParse(code1, out c1);
                if (c1 > 0 && (c1 - today) <= 1)
                {
                    int remain = Math.Max(0, c1 - today);
                    daoqi = "软件剩余时间" + remain + "天，请联系厂家!";
                    _logger.WriteLog("监控解码: 过期警告触发, 剩余" + remain + "天");
                }
                else
                    daoqi = "";
            }
            catch (Exception ex)
            {
                _logger.WriteLog("监控解码: 过期警告异常: " + ex.Message);
            }

            // ★ 解码核心逻辑，全部用 TryParse + 防御性检查
            if (code != "" && code.Length >= 7)
            {
                try
                {
                    int offset = 0;
                    string last2 = code.Substring(code.Length - 2);
                    if (int.TryParse(last2, out offset) && offset >= 0 && offset + 5 <= code.Length)
                    {
                        string dateStr = code.Substring(offset, 5);
                        int parsedDate = 0;
                        if (int.TryParse(dateStr, out parsedDate))
                            dayt = parsedDate - ((int)DateTime.Now.ToOADate());
                        else
                            _logger.WriteLog("监控解码: code日期段解析失败, offset=" + offset);
                    }
                    else
                    {
                        _logger.WriteLog("监控解码: code偏移量无效, codeLen=" + code.Length);
                        // 重试
                        code = _config.ReadString("code1", "code2", "");
                        if (code != "" && code.Length >= 7)
                        {
                            last2 = code.Substring(code.Length - 2);
                            if (int.TryParse(last2, out offset) && offset >= 0 && offset + 5 <= code.Length)
                            {
                                int parsedDate2 = 0;
                                int.TryParse(code.Substring(offset, 5), out parsedDate2);
                                dayt = parsedDate2 - ((int)DateTime.Now.ToOADate());
                            }
                        }
                    }

                    if (dayt >= 0 && code.Length >= 7)
                    {
                        int off = 0;
                        if (int.TryParse(code.Substring(code.Length - 2), out off) && off >= 0 && off + 5 <= code.Length)
                            zhongjian = code.Substring(off, 5);
                    }
                    else
                        zhongjian = DateTime.Now.ToOADate().ToString();

                    // ★ 解锁判断
                    int v1 = 0, v2 = 0, v3 = 0, now = (int)DateTime.Now.ToOADate();
                    int.TryParse(code1, out v1);
                    int.TryParse(code2_val, out v2);
                    int.TryParse(code3, out v3);
                    authV1 = v1;
                    if (v1 > now && v2 <= now && v3 <= now)
                        dayz = 1;
                    else
                        dayz = 0;

                    // ★ 更新运行日期
                    if (v3 > 0 && v3 <= now)
                    {
                        try { duini.WriteString("1", "4", now.ToString()); }
                        catch (Exception ex) { _logger.WriteLog("监控解码: 更新运行日期失败: " + ex.Message); }
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLog("监控解码异常: " + ex.Message);
                }
            }
            try
            {
                day2 = GetCPUSerialnumber(zhongjian);
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex.Message + ":对比");
            };
            // ★ 2026-09-06 ④：同步授权过期门控，供 initialize_FormSet/bnOpen_Click 自动开相机前判断
            // ★ 2026-09-07：与 Form1_Load 同一口径 —— 授权数据读不到（authV1<=0）时不判为过期，
            //   避免把"读不到 test.ini"误判成"授权过期"而禁用功能。
            _authExpired = (authV1 > 0 && dayz == 0);
            if (dayz == 0)
            {
                // ★ 监控线程在后台运行，所有 UI 操作必须 Invoke 到 UI 线程。
                //   窗体正在关闭时 Invoke 会抛 ObjectDisposedException，必须捕获，避免后台任务异常。
                try
                {
                    this.Invoke(new Action(() =>
                    {
                    checkedListBox1.SetItemChecked(0, false);
                    checkedListBox1.SetItemChecked(1, false);
                    checkedListBox1.SetItemChecked(2, false);
                    checkedListBox1.SetItemChecked(3, false);
                    button2.Enabled = false;
                    button1.Enabled = false;
                    label12.Text = "加密中";
                    button5.Visible = true;
                    label172.Visible = true;
                    textBox7.Visible = true;
                    textBox4.Visible = true;
                    label172.Visible = true;
                    textBox7.Visible = true;
                    pictureBox1.Visible = true;
                    tableLayoutPanel1.Visible = false;
                    getCode();
                    }));
                }
                catch (Exception exUi)
                {
                    _logger.WriteLog("监控解码: 刷新加密界面失败(窗体可能正在关闭): " + exUi.Message);
                }
            }
        }
        #endregion
    }
}
