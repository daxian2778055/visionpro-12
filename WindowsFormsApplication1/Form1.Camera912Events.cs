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
    // Form1 partial class: camera 9-12 tab page event handlers.
    // Moved verbatim from Form1.cs - organizational split only, no logic change.
    public partial class Form1 : Form
    {
        #region Camera 9-12 Tab Page Event Handlers

        // NumericUpDown ValueChanged handlers
        private void numericUpDown17_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown17.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[8])
            {
                for (int i = 0; i < camera_name.Count(); i++)
                {
                    if (camera_name[i] == name_temp)
                    {
                        chongming_temp = 1;
                    }

                }
                if (chongming_temp == 0)
                {
                    camera_name[8] = name_temp;
                    _config.WriteString("camera", "name9", name_temp);
                }
                else
                {
                    numericUpDown17.Value = int.Parse(camera_name[8]);
                }
            }
        }

        private void numericUpDown18_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown18.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[9])
            {
                for (int i = 0; i < camera_name.Count(); i++)
                {
                    if (camera_name[i] == name_temp)
                    {
                        chongming_temp = 1;
                    }

                }
                if (chongming_temp == 0)
                {
                    camera_name[9] = name_temp;
                    _config.WriteString("camera", "name10", name_temp);
                }
                else
                {
                    numericUpDown18.Value = int.Parse(camera_name[9]);
                }
            }
        }

        private void numericUpDown19_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown19.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[10])
            {
                for (int i = 0; i < camera_name.Count(); i++)
                {
                    if (camera_name[i] == name_temp)
                    {
                        chongming_temp = 1;
                    }

                }
                if (chongming_temp == 0)
                {
                    camera_name[10] = name_temp;
                    _config.WriteString("camera", "name11", name_temp);
                }
                else
                {
                    numericUpDown19.Value = int.Parse(camera_name[10]);
                }
            }
        }

        private void numericUpDown20_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown20.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[11])
            {
                for (int i = 0; i < camera_name.Count(); i++)
                {
                    if (camera_name[i] == name_temp)
                    {
                        chongming_temp = 1;
                    }

                }
                if (chongming_temp == 0)
                {
                    camera_name[11] = name_temp;
                    _config.WriteString("camera", "name12", name_temp);
                }
                else
                {
                    numericUpDown20.Value = int.Parse(camera_name[11]);
                }
            }
        }

        // Button click handlers (cam9 tab page)
        private void button85_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox43, listBox19);
        }

        private void button88_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 8)
                {
                    _jobs.myjob9.block.Inputs[comboBox41.Text].Value = textBox41.Text;
                }
                MessageBox.Show("参数:" + comboBox41.Text + "写入数值" + textBox41.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button89_Click(object sender, EventArgs e)
        {
            if (button89.Text == "顶" && listBox19.Visible == true)
            {
                if (_jobs.yunxing == false && listBox19.Items.Count > 0)
                {
                    listBox19.SelectedIndex = 0;
                    button89.Text = "底";
                }
            }
            else if (button89.Text == "底" && listBox19.Visible == true)
            {
                if (_jobs.yunxing == false && listBox19.Items.Count > 0)
                {
                    listBox19.SelectedIndex = listBox19.Items.Count - 1;
                    button89.Text = "顶";
                }
            }
        }

        private void button90_Click(object sender, EventArgs e)
        {
            trriger9_temp = 0;
            timer9.Enabled = false;
        }

        private void button91_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox19.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    _jobs.myjob9.img = new Bitmap(item.filePath);
                    if (_jobs.myjob9.trriger == 0)
                    {
                        _jobs.myjob9.trriger = 1;
                        getrecord(_jobs.myjob9, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger9_temp = 1;
                    timer9.Interval = int.Parse(textBox42.Text);
                    timer9.Enabled = true;
                }
            }
        }

        private void button92_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox46, listBox20);
        }

        private void button95_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 9)
                {
                    _jobs.myjob10.block.Inputs[comboBox45.Text].Value = textBox44.Text;
                }
                MessageBox.Show("参数:" + comboBox45.Text + "写入数值" + textBox44.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button96_Click(object sender, EventArgs e)
        {
            if (button96.Text == "顶" && listBox20.Visible == true)
            {
                if (_jobs.yunxing == false && listBox20.Items.Count > 0)
                {
                    listBox20.SelectedIndex = 0;
                    button96.Text = "底";
                }
            }
            else if (button96.Text == "底" && listBox20.Visible == true)
            {
                if (_jobs.yunxing == false && listBox20.Items.Count > 0)
                {
                    listBox20.SelectedIndex = listBox20.Items.Count - 1;
                    button96.Text = "顶";
                }
            }
        }

        private void button97_Click(object sender, EventArgs e)
        {
            trriger10_temp = 0;
            timer18.Enabled = false;
        }

        private void button98_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox20.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    _jobs.myjob10.img = new Bitmap(item.filePath);
                    if (_jobs.myjob10.trriger == 0)
                    {
                        _jobs.myjob10.trriger = 1;
                        getrecord(_jobs.myjob10, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger10_temp = 1;
                    timer18.Interval = int.Parse(textBox45.Text);
                    timer18.Enabled = true;
                }
            }
        }

        private void button99_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox49, listBox21);
        }

        private void button102_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 10)
                {
                    _jobs.myjob11.block.Inputs[comboBox49.Text].Value = textBox47.Text;
                }
                MessageBox.Show("参数:" + comboBox49.Text + "写入数值" + textBox47.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button103_Click(object sender, EventArgs e)
        {
            if (button103.Text == "顶" && listBox21.Visible == true)
            {
                if (_jobs.yunxing == false && listBox21.Items.Count > 0)
                {
                    listBox21.SelectedIndex = 0;
                    button103.Text = "底";
                }
            }
            else if (button103.Text == "底" && listBox21.Visible == true)
            {
                if (_jobs.yunxing == false && listBox21.Items.Count > 0)
                {
                    listBox21.SelectedIndex = listBox21.Items.Count - 1;
                    button103.Text = "顶";
                }
            }
        }

        private void button104_Click(object sender, EventArgs e)
        {
            trriger11_temp = 0;
            timer19.Enabled = false;
        }

        private void button105_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox21.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    _jobs.myjob11.img = new Bitmap(item.filePath);
                    if (_jobs.myjob11.trriger == 0)
                    {
                        _jobs.myjob11.trriger = 1;
                        getrecord(_jobs.myjob11, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger11_temp = 1;
                    timer19.Interval = int.Parse(textBox48.Text);
                    timer19.Enabled = true;
                }
            }
        }

        private void button106_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox52, listBox22);
        }

        private void button109_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 11)
                {
                    _jobs.myjob12.block.Inputs[comboBox53.Text].Value = textBox50.Text;
                }
                MessageBox.Show("参数:" + comboBox53.Text + "写入数值" + textBox50.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button110_Click(object sender, EventArgs e)
        {
            if (button110.Text == "顶" && listBox22.Visible == true)
            {
                if (_jobs.yunxing == false && listBox22.Items.Count > 0)
                {
                    listBox22.SelectedIndex = 0;
                    button110.Text = "底";
                }
            }
            else if (button110.Text == "底" && listBox22.Visible == true)
            {
                if (_jobs.yunxing == false && listBox22.Items.Count > 0)
                {
                    listBox22.SelectedIndex = listBox22.Items.Count - 1;
                    button110.Text = "顶";
                }
            }
        }

        private void button111_Click(object sender, EventArgs e)
        {
            trriger12_temp = 0;
            timer20.Enabled = false;
        }

        private void button112_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox22.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    _jobs.myjob12.img = new Bitmap(item.filePath);
                    if (_jobs.myjob12.trriger == 0)
                    {
                        _jobs.myjob12.trriger = 1;
                        getrecord(_jobs.myjob12, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger12_temp = 1;
                    timer20.Interval = int.Parse(textBox51.Text);
                    timer20.Enabled = true;
                }
            }
        }

        // Button click handlers (cam10-12 tab pages)
        private void button86_Click(object sender, EventArgs e)
        {
        }

        private void button87_Click(object sender, EventArgs e)
        {
        }

        private void button93_Click(object sender, EventArgs e)
        {
        }

        private void button94_Click(object sender, EventArgs e)
        {
        }

        private void button100_Click(object sender, EventArgs e)
        {
        }

        private void button101_Click(object sender, EventArgs e)
        {
        }

        private void button107_Click(object sender, EventArgs e)
        {
        }

        private void button108_Click(object sender, EventArgs e)
        {
        }

        // CheckBox CheckedChanged handlers
        private void checkBox72_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob9.roi = checkBox72.Checked;
            ICogTool selTool;
            if (tools9.TryGetValue(comboBox40.Text, out selTool))
                roiset2(_jobs.myjob9.block, selTool, checkBox72.Checked, cogRecordDisplay9);
        }

        private void checkBox73_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox74_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox74.CheckState == CheckState.Checked)
                {
                    _jobs.myjob9.serial = true;
                }
                else
                {

                    _jobs.myjob9.serial = false;
                }
            }
            catch { }
        }

        private void checkBox75_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox75.CheckState == CheckState.Checked)
                {
                    _jobs.myjob9.tcp = true;
                }
                else
                {

                    _jobs.myjob9.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox76_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(8, cbLineMode9, checkBox76);
        }

        private void checkBox77_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox77.CheckState == CheckState.Checked)
                _jobs.myjob9.shijianEn = true;
            else
                _jobs.myjob9.shijianEn = false;
        }

        private void checkBox78_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob10.roi = checkBox78.Checked;
            ICogTool selTool;
            if (tools10.TryGetValue(comboBox44.Text, out selTool))
                roiset2(_jobs.myjob10.block, selTool, checkBox78.Checked, cogRecordDisplay10);
        }

        private void checkBox79_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox80_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox80.CheckState == CheckState.Checked)
                {
                    _jobs.myjob10.serial = true;
                }
                else
                {

                    _jobs.myjob10.serial = false;
                }
            }
            catch { }
        }

        private void checkBox81_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox81.CheckState == CheckState.Checked)
                {
                    _jobs.myjob10.tcp = true;
                }
                else
                {

                    _jobs.myjob10.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox82_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(9, cbLineMode10, checkBox82);
        }

        private void checkBox83_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox83.CheckState == CheckState.Checked)
                _jobs.myjob10.shijianEn = true;
            else
                _jobs.myjob10.shijianEn = false;
        }

        private void checkBox84_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob11.roi = checkBox84.Checked;
            ICogTool selTool;
            if (tools11.TryGetValue(comboBox48.Text, out selTool))
                roiset2(_jobs.myjob11.block, selTool, checkBox84.Checked, cogRecordDisplay11);
        }

        private void checkBox85_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox86_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox86.CheckState == CheckState.Checked)
                {
                    _jobs.myjob11.serial = true;
                }
                else
                {

                    _jobs.myjob11.serial = false;
                }
            }
            catch { }
        }

        private void checkBox87_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox87.CheckState == CheckState.Checked)
                {
                    _jobs.myjob11.tcp = true;
                }
                else
                {

                    _jobs.myjob11.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox88_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(10, cbLineMode11, checkBox88);
        }

        private void checkBox89_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox89.CheckState == CheckState.Checked)
                _jobs.myjob11.shijianEn = true;
            else
                _jobs.myjob11.shijianEn = false;
        }

        private void checkBox90_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob12.roi = checkBox90.Checked;
            ICogTool selTool;
            if (tools12.TryGetValue(comboBox52.Text, out selTool))
                roiset2(_jobs.myjob12.block, selTool, checkBox90.Checked, cogRecordDisplay12);
        }

        private void checkBox91_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox92_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox92.CheckState == CheckState.Checked)
                {
                    _jobs.myjob12.serial = true;
                }
                else
                {

                    _jobs.myjob12.serial = false;
                }
            }
            catch { }
        }

        private void checkBox93_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox93.CheckState == CheckState.Checked)
                {
                    _jobs.myjob12.tcp = true;
                }
                else
                {

                    _jobs.myjob12.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox94_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(11, cbLineMode12, checkBox94);
        }

        private void checkBox95_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox95.CheckState == CheckState.Checked)
                _jobs.myjob12.shijianEn = true;
            else
                _jobs.myjob12.shijianEn = false;
        }

        // ComboBox DropDown handlers
        private void comboBox40_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox40, _jobs.myjob9.block, tools9);
        }

        private void comboBox41_DropDown(object sender, EventArgs e)
        {
            // 参数下拉：列 block.Inputs 键（工具名.输出名），与 Inputs[键] 读写匹配（原错用工具名导致键不匹配）
            comboBox41.Items.Clear();
            if (manager1 != null && _jobs.myjob9.block != null)
            {
                for (int i = 0; i < _jobs.myjob9.block.Inputs.Count; i++)
                    comboBox41.Items.Add(_jobs.myjob9.block.Inputs[i].Name);
            }
        }

        private void comboBox42_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox42, 9);
        }

        private void comboBox43_DropDown(object sender, EventArgs e)
        {
        }

        private void comboBox44_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox44, _jobs.myjob10.block, tools10);
        }

        private void comboBox45_DropDown(object sender, EventArgs e)
        {
            // 参数下拉：列 block.Inputs 键（工具名.输出名），与 Inputs[键] 读写匹配
            comboBox45.Items.Clear();
            if (manager1 != null && _jobs.myjob10.block != null)
            {
                for (int i = 0; i < _jobs.myjob10.block.Inputs.Count; i++)
                    comboBox45.Items.Add(_jobs.myjob10.block.Inputs[i].Name);
            }
        }

        private void comboBox46_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox46, 10);
        }

        private void comboBox47_DropDown(object sender, EventArgs e)
        {
        }

        private void comboBox48_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox48, _jobs.myjob11.block, tools11);
        }

        private void comboBox49_DropDown(object sender, EventArgs e)
        {
            // 参数下拉：列 block.Inputs 键（工具名.输出名），与 Inputs[键] 读写匹配
            comboBox49.Items.Clear();
            if (manager1 != null && _jobs.myjob11.block != null)
            {
                for (int i = 0; i < _jobs.myjob11.block.Inputs.Count; i++)
                    comboBox49.Items.Add(_jobs.myjob11.block.Inputs[i].Name);
            }
        }

        private void comboBox50_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox50, 11);
        }

        private void comboBox51_DropDown(object sender, EventArgs e)
        {
        }

        private void comboBox52_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox52, _jobs.myjob12.block, tools12);
        }

        private void comboBox53_DropDown(object sender, EventArgs e)
        {
            // 参数下拉：列 block.Inputs 键（工具名.输出名），与 Inputs[键] 读写匹配
            comboBox53.Items.Clear();
            if (manager1 != null && _jobs.myjob12.block != null)
            {
                for (int i = 0; i < _jobs.myjob12.block.Inputs.Count; i++)
                    comboBox53.Items.Add(_jobs.myjob12.block.Inputs[i].Name);
            }
        }

        private void comboBox54_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox54, 12);
        }

        private void comboBox55_DropDown(object sender, EventArgs e)
        {
        }

        // ComboBox SelectedIndexChanged handlers (comboBox40 already exists)
        private void comboBox41_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 8)
                {
                    textBox41.Text = _jobs.myjob9.block.Inputs[comboBox41.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox42_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    SwitchJobBlock(8, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\9\\" + comboBox42.SelectedItem.ToString()));
                    _config.WriteString("camera9", "fen", comboBox42.SelectedItem.ToString());
                    MessageBox.Show("切换流程9:" + comboBox42.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程9:" + comboBox42.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox43_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob9.state.Contains("相"))
            {
                try
                {
                    if (comboBox43.Text == "连续运行")
                    {
                        _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger9.Enabled = false;
                        bnTriggerExec9.Enabled = false;
                    }
                    else if (comboBox43.Text == "触发拍照" || comboBox43.Text == "通讯触发")
                    {
                        _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                        if (comboBox43.Text == "触发拍照")
                        {
                            _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                            cbSoftTrigger9.Enabled = false;
                            bnTriggerExec9.Enabled = false;
                        }
                        else
                        {
                            _cameraCtrl.Cameras[8].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                            if (m_bGrabbing9)
                            {
                                cbSoftTrigger9.Enabled = true;
                                bnTriggerExec9.Enabled = true;
                            }
                        }
                    }
                }
                catch { }
                _jobs.myjob9.triggerMode = comboBox43.Text;
            }
        }

        private void comboBox44_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void comboBox45_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 9)
                {
                    textBox44.Text = _jobs.myjob10.block.Inputs[comboBox45.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox46_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    SwitchJobBlock(9, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\10\\" + comboBox46.SelectedItem.ToString()));
                    _config.WriteString("camera10", "fen", comboBox46.SelectedItem.ToString());
                    MessageBox.Show("切换流程10:" + comboBox46.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程10:" + comboBox46.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox47_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob10.state.Contains("相"))
            {
                try
                {
                    if (comboBox47.Text == "连续运行")
                    {
                        _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger10.Enabled = false;
                        bnTriggerExec10.Enabled = false;
                    }
                    else if (comboBox47.Text == "触发拍照" || comboBox47.Text == "通讯触发")
                    {
                        _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                        if (comboBox47.Text == "触发拍照")
                        {
                            _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                            cbSoftTrigger10.Enabled = false;
                            bnTriggerExec10.Enabled = false;
                        }
                        else
                        {
                            _cameraCtrl.Cameras[9].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                            if (m_bGrabbing10)
                            {
                                cbSoftTrigger10.Enabled = true;
                                bnTriggerExec10.Enabled = true;
                            }
                        }
                    }
                }
                catch { }
                _jobs.myjob10.triggerMode = comboBox47.Text;
            }
        }

        private void comboBox48_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void comboBox49_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 10)
                {
                    textBox47.Text = _jobs.myjob11.block.Inputs[comboBox49.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox50_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    SwitchJobBlock(10, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\11\\" + comboBox50.SelectedItem.ToString()));
                    _config.WriteString("camera11", "fen", comboBox50.SelectedItem.ToString());
                    MessageBox.Show("切换流程11:" + comboBox50.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程11:" + comboBox50.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox51_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob11.state.Contains("相"))
            {
                try
                {
                    if (comboBox51.Text == "连续运行")
                    {
                        _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger11.Enabled = false;
                        bnTriggerExec11.Enabled = false;
                    }
                    else if (comboBox51.Text == "触发拍照" || comboBox51.Text == "通讯触发")
                    {
                        _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                        if (comboBox51.Text == "触发拍照")
                        {
                            _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                            cbSoftTrigger11.Enabled = false;
                            bnTriggerExec11.Enabled = false;
                        }
                        else
                        {
                            _cameraCtrl.Cameras[10].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                            if (m_bGrabbing11)
                            {
                                cbSoftTrigger11.Enabled = true;
                                bnTriggerExec11.Enabled = true;
                            }
                        }
                    }
                }
                catch { }
                _jobs.myjob11.triggerMode = comboBox51.Text;
            }
        }

        private void comboBox52_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void comboBox53_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 11)
                {
                    textBox50.Text = _jobs.myjob12.block.Inputs[comboBox53.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox54_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    SwitchJobBlock(11, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\12\\" + comboBox54.SelectedItem.ToString()));
                    _config.WriteString("camera12", "fen", comboBox54.SelectedItem.ToString());
                    MessageBox.Show("切换流程12:" + comboBox54.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程12:" + comboBox54.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox55_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob12.state.Contains("相"))
            {
                try
                {
                    if (comboBox55.Text == "连续运行")
                    {
                        _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger12.Enabled = false;
                        bnTriggerExec12.Enabled = false;
                    }
                    else if (comboBox55.Text == "触发拍照" || comboBox55.Text == "通讯触发")
                    {
                        _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                        if (comboBox55.Text == "触发拍照")
                        {
                            _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                            cbSoftTrigger12.Enabled = false;
                            bnTriggerExec12.Enabled = false;
                        }
                        else
                        {
                            _cameraCtrl.Cameras[11].MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                            if (m_bGrabbing12)
                            {
                                cbSoftTrigger12.Enabled = true;
                                bnTriggerExec12.Enabled = true;
                            }
                        }
                    }
                }
                catch { }
                _jobs.myjob12.triggerMode = comboBox55.Text;
            }
        }

        // ListBox SelectedIndexChanged handlers
        private void listBox19_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox19.SelectedItem;
                if (item == null) return;
                _jobs.myjob9.img = new Bitmap(item.filePath);
                if (_jobs.yunxing == false)
                {
                    if (_jobs.myjob9.trriger == 0)
                    {
                        _jobs.myjob9.trriger = 1;
                        getrecord(_jobs.myjob9, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void listBox20_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox20.SelectedItem;
                if (item == null) return;
                _jobs.myjob10.img = new Bitmap(item.filePath);
                if (_jobs.yunxing == false)
                {
                    if (_jobs.myjob10.trriger == 0)
                    {
                        _jobs.myjob10.trriger = 1;
                        getrecord(_jobs.myjob10, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void listBox21_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox21.SelectedItem;
                if (item == null) return;
                _jobs.myjob11.img = new Bitmap(item.filePath);
                if (_jobs.yunxing == false)
                {
                    if (_jobs.myjob11.trriger == 0)
                    {
                        _jobs.myjob11.trriger = 1;
                        getrecord(_jobs.myjob11, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void listBox22_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox22.SelectedItem;
                if (item == null) return;
                _jobs.myjob12.img = new Bitmap(item.filePath);
                if (_jobs.yunxing == false)
                {
                    if (_jobs.myjob12.trriger == 0)
                    {
                        _jobs.myjob12.trriger = 1;
                        getrecord(_jobs.myjob12, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        #endregion
    }
}
