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
    // Form1 partial class: camera 5-8 events and misc UI event handlers.
    // Moved verbatim from Form1.cs - organizational split only, no logic change.
    public partial class Form1 : Form
    {
        #region 相机5-8 事件与杂项
        private void 相机1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob1);
        }

        private void 相机2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob2);
        }

        private void 相机3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob3);
        }

        private void 相机4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob4);
        }

        private void label74_Click(object sender, EventArgs e)
        {

        }

        private void 关闭监控ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Control item in this.panel1.Controls)
            {
                if (item is Form)
                {
                    ((Form)item).Close();
                }
            }
            this.Invoke(new Action(() =>
            {
                groupBox1.Visible = true;
                groupBox5.Visible = true;
                groupBox7.Visible = true;
                groupBox8.Visible = true;
                groupBox18.Visible = true;
                groupBox19.Visible = true;
                groupBox20.Visible = true;
                groupBox21.Visible = true;
                groupBox22.Visible = true;
                groupBox27.Visible = true;
                groupBox28.Visible = true;
                groupBox29.Visible = true;
            }));
        }

        private void listBox1_MouseDown_1(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(1, _jobs.myjob1, listBox1.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox3_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(2, _jobs.myjob2, listBox3.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox7_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(3, _jobs.myjob3, listBox7.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox6_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(4, _jobs.myjob4, listBox6.SelectedItem.ToString());
            }
            catch { }
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            zhangshu = numericUpDown4.Value;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.CheckState == CheckState.Checked)
                _jobs.myjob1.shijianEn = true;
            else
                _jobs.myjob1.shijianEn = false;
        }

        private void comboBox22_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox22.SelectedIndex == 0)
            {
                tongji = 0;
            }
            else
            {
                tongji = 1;
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            if (button20.Text == "顶" && listBox10.Visible == true)
            {
                if (_jobs.yunxing == false && listBox10.Items.Count > 0)
                {
                    listBox10.SelectedIndex = 0;
                    button20.Text = "底";
                }
            }
            else if (button20.Text == "底" && listBox10.Visible == true)
            {
                if (_jobs.yunxing == false && listBox10.Items.Count > 0)
                {
                    listBox10.SelectedIndex = listBox10.Items.Count - 1;
                    button20.Text = "顶";
                }
            }
        }

        private void button44_Click(object sender, EventArgs e)
        {
            if (button44.Text == "顶" && listBox11.Visible == true)
            {
                if (_jobs.yunxing == false && listBox11.Items.Count > 0)
                {
                    listBox11.SelectedIndex = 0;
                    button44.Text = "底";
                }
            }
            else if (button44.Text == "底" && listBox11.Visible == true)
            {
                if (_jobs.yunxing == false && listBox11.Items.Count > 0)
                {
                    listBox11.SelectedIndex = listBox11.Items.Count - 1;
                    button44.Text = "顶";
                }
            }
        }

        private void button57_Click(object sender, EventArgs e)
        {
            if (button57.Text == "顶" && listBox12.Visible == true)
            {
                if (_jobs.yunxing == false && listBox12.Items.Count > 0)
                {
                    listBox12.SelectedIndex = 0;
                    button57.Text = "底";
                }
            }
            else if (button57.Text == "底" && listBox12.Visible == true)
            {
                if (_jobs.yunxing == false && listBox12.Items.Count > 0)
                {
                    listBox12.SelectedIndex = listBox12.Items.Count - 1;
                    button57.Text = "顶";
                }
            }
        }

        private void button70_Click(object sender, EventArgs e)
        {
            if (button70.Text == "顶" && listBox13.Visible == true)
            {
                if (_jobs.yunxing == false && listBox13.Items.Count > 0)
                {
                    listBox13.SelectedIndex = 0;
                    button70.Text = "底";
                }
            }
            else if (button70.Text == "底" && listBox13.Visible == true)
            {
                if (_jobs.yunxing == false && listBox13.Items.Count > 0)
                {
                    listBox13.SelectedIndex = listBox13.Items.Count - 1;
                    button70.Text = "顶";
                }
            }
        }

        private void button40_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox22, listBox10);
        }

        private void button53_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox28, listBox11);
        }

        private void button66_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox34, listBox12);
        }

        private void button79_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox40, listBox13);
        }

        private void timer13_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob5.trriger == 0)
            {
                if (trriger5_temp == 1)
                {
                    int count = listBox10.Items.Count;
                    int select = listBox10.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox10.SelectedIndex = select + 1;
                        }
                        else
                            listBox10.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void timer14_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob6.trriger == 0)
            {
                if (trriger6_temp == 1)
                {
                    int count = listBox11.Items.Count;
                    int select = listBox11.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox11.SelectedIndex = select + 1;
                        }
                        else
                            listBox11.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void timer15_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob7.trriger == 0)
            {
                if (trriger7_temp == 1)
                {
                    int count = listBox12.Items.Count;
                    int select = listBox12.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox12.SelectedIndex = select + 1;
                        }
                        else
                            listBox12.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void timer16_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob8.trriger == 0)
            {
                if (trriger8_temp == 1)
                {
                    int count = listBox13.Items.Count;
                    int select = listBox13.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox13.SelectedIndex = select + 1;
                        }
                        else
                            listBox13.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void timer18_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob10.trriger == 0)
            {
                if (trriger10_temp == 1)
                {
                    int count = listBox20.Items.Count;
                    int select = listBox20.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox20.SelectedIndex = select + 1;
                        }
                        else
                            listBox20.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void timer19_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob11.trriger == 0)
            {
                if (trriger11_temp == 1)
                {
                    int count = listBox21.Items.Count;
                    int select = listBox21.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox21.SelectedIndex = select + 1;
                        }
                        else
                            listBox21.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void timer20_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob12.trriger == 0)
            {
                if (trriger12_temp == 1)
                {
                    int count = listBox22.Items.Count;
                    int select = listBox22.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox22.SelectedIndex = select + 1;
                        }
                        else
                            listBox22.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void button41_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox10.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    SetReplayImg(_jobs.myjob5, item.filePath);
                    if (_jobs.myjob5.trriger == 0)
                    {
                        _jobs.myjob5.trriger = 1;
                        getrecord(_jobs.myjob5, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger5_temp = 1;
                    ApplyReplayInterval(timer13, textBox21);
                    timer13.Enabled = true;
                }
            }
        }

        private void button54_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox11.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    SetReplayImg(_jobs.myjob6, item.filePath);
                    if (_jobs.myjob6.trriger == 0)
                    {
                        _jobs.myjob6.trriger = 1;
                        getrecord(_jobs.myjob6, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger6_temp = 1;
                    ApplyReplayInterval(timer14, textBox27);
                    timer14.Enabled = true;
                }
            }
        }

        private void button67_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox12.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    SetReplayImg(_jobs.myjob7, item.filePath);
                    if (_jobs.myjob7.trriger == 0)
                    {
                        _jobs.myjob7.trriger = 1;
                        getrecord(_jobs.myjob7, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger7_temp = 1;
                    ApplyReplayInterval(timer15, textBox33);
                    timer15.Enabled = true;
                }
            }
        }

        private void button80_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox13.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    SetReplayImg(_jobs.myjob8, item.filePath);
                    if (_jobs.myjob8.trriger == 0)
                    {
                        _jobs.myjob8.trriger = 1;
                        getrecord(_jobs.myjob8, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger8_temp = 1;
                    ApplyReplayInterval(timer16, textBox39);
                    timer16.Enabled = true;
                }
            }
        }

        private void button36_Click(object sender, EventArgs e)
        {
            trriger5_temp = 0;
            timer13.Enabled = false;
        }

        private void button52_Click(object sender, EventArgs e)
        {
            trriger6_temp = 0;
            timer14.Enabled = false;
        }

        private void button65_Click(object sender, EventArgs e)
        {
            trriger7_temp = 0;
            timer15.Enabled = false;
        }

        private void button78_Click(object sender, EventArgs e)
        {
            trriger8_temp = 0;
            timer16.Enabled = false;
        }

        private void comboBox25_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob5.state.Contains("相"))
            {
                try
                {
                    if (comboBox25.Text.Contains("连续运行"))
                    {
                        _cameraCtrl.Cameras[4].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger5.Enabled = false;
                        bnTriggerExec5.Enabled = false;
                    }
                    else if (comboBox25.Text.Contains("触发拍照") || comboBox25.Text.Contains("通讯触发"))
                    {

                        _cameraCtrl.Cameras[4].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger5.Checked || comboBox25.Text.Contains("通讯触发"))
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
                    _logger.WriteLog(ex.Message + "触发切换5");
                }
                _jobs.myjob5.triggerMode = comboBox25.Text;
            }
        }

        private void comboBox28_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob6.state.Contains("相"))
            {
                try
                {
                    if (comboBox28.Text.Contains("连续运行"))
                    {
                        _cameraCtrl.Cameras[5].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger6.Enabled = false;
                        bnTriggerExec6.Enabled = false;
                    }
                    else if (comboBox28.Text.Contains("触发拍照") || comboBox28.Text.Contains("通讯触发"))
                    {

                        _cameraCtrl.Cameras[5].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger6.Checked || comboBox28.Text.Contains("通讯触发"))
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
                    _logger.WriteLog(ex.Message + "触发切换6");
                }
                _jobs.myjob6.triggerMode = comboBox28.Text;
            }
        }

        private void comboBox31_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob7.state.Contains("相"))
            {
                try
                {
                    if (comboBox31.Text.Contains("连续运行"))
                    {
                        _cameraCtrl.Cameras[6].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger7.Enabled = false;
                        bnTriggerExec7.Enabled = false;
                    }
                    else if (comboBox31.Text.Contains("触发拍照") || comboBox31.Text.Contains("通讯触发"))
                    {

                        _cameraCtrl.Cameras[6].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger7.Checked || comboBox31.Text.Contains("通讯触发"))
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
                    _logger.WriteLog(ex.Message + "触发切换7");
                }
                _jobs.myjob7.triggerMode = comboBox31.Text;
            }
        }

        private void comboBox34_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob8.state.Contains("相"))
            {
                try
                {
                    if (comboBox34.Text.Contains("连续运行"))
                    {
                        _cameraCtrl.Cameras[7].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger8.Enabled = false;
                        bnTriggerExec8.Enabled = false;
                    }
                    else if (comboBox34.Text.Contains("触发拍照") || comboBox34.Text.Contains("通讯触发"))
                    {

                        _cameraCtrl.Cameras[7].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger8.Checked || comboBox34.Text.Contains("通讯触发"))
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
                    _logger.WriteLog(ex.Message + "触发切换8");
                }
                _jobs.myjob8.triggerMode = comboBox34.Text;
            }
        }

        private void listBox10_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox10.SelectedItem;
                if (item == null) return;
                if (_jobs.yunxing == false)
                {
                    // ★运行中不覆盖回图（未消费的 img 仍被 timer 重试使用）
                    SetReplayImg(_jobs.myjob5, item.filePath);
                    if (_jobs.myjob5.trriger == 0)
                    {
                        _jobs.myjob5.trriger = 1;
                        getrecord(_jobs.myjob5, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void listBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox11.SelectedItem;
                if (item == null) return;
                if (_jobs.yunxing == false)
                {
                    // ★运行中不覆盖回图（同上）
                    SetReplayImg(_jobs.myjob6, item.filePath);
                    if (_jobs.myjob6.trriger == 0)
                    {
                        _jobs.myjob6.trriger = 1;
                        getrecord(_jobs.myjob6, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void listBox12_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox12.SelectedItem;
                if (item == null) return;
                if (_jobs.yunxing == false)
                {
                    // ★运行中不覆盖回图（同上）
                    SetReplayImg(_jobs.myjob7, item.filePath);
                    if (_jobs.myjob7.trriger == 0)
                    {
                        _jobs.myjob7.trriger = 1;
                        getrecord(_jobs.myjob7, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void listBox13_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox13.SelectedItem;
                if (item == null) return;
                if (_jobs.yunxing == false)
                {
                    // ★运行中不覆盖回图（同上）
                    SetReplayImg(_jobs.myjob8, item.filePath);
                    if (_jobs.myjob8.trriger == 0)
                    {
                        _jobs.myjob8.trriger = 1;
                        getrecord(_jobs.myjob8, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void checkBox32_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void checkBox38_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void checkBox44_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox50_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void bnGetLineSel5_Click(object sender, EventArgs e)
        {
            get_Selector(4, cbLineSel5);
        }

        private void bnGetLineSel6_Click(object sender, EventArgs e)
        {
            get_Selector(5, cbLineSel6);
        }

        private void bnGetLineSel7_Click(object sender, EventArgs e)
        {
            get_Selector(6, cbLineSel7);
        }

        private void bnGetLineSel8_Click(object sender, EventArgs e)
        {
            get_Selector(7, cbLineSel8);
        }

        private void bnSetLineSel5_Click(object sender, EventArgs e)
        {
            set_Selector(4, cbLineSel5);
        }

        private void bnSetLineSel6_Click(object sender, EventArgs e)
        {
            set_Selector(5, cbLineSel6);
        }

        private void bnSetLineSel7_Click(object sender, EventArgs e)
        {
            set_Selector(6, cbLineSel7);
        }

        private void bnSetLineSel8_Click(object sender, EventArgs e)
        {
            set_Selector(7, cbLineSel8);
        }

        private void bnGetLineMode5_Click(object sender, EventArgs e)
        {
            get_Mode(4, cbLineMode5);
        }

        private void bnGetLineMode6_Click(object sender, EventArgs e)
        {
            get_Mode(5, cbLineMode6);
        }

        private void bnGetLineMode7_Click(object sender, EventArgs e)
        {
            get_Mode(6, cbLineMode7);
        }

        private void bnGetLineMode8_Click(object sender, EventArgs e)
        {
            get_Mode(7, cbLineMode8);
        }

        private void bnSetLineMode8_Click(object sender, EventArgs e)
        {
            set_Mode(7, cbLineMode8);
        }

        private void bnSetLineMode5_Click(object sender, EventArgs e)
        {
            set_Mode(4, cbLineMode5);
        }

        private void bnSetLineMode6_Click(object sender, EventArgs e)
        {
            set_Mode(5, cbLineMode6);
        }

        private void bnSetLineMode7_Click(object sender, EventArgs e)
        {
            set_Mode(6, cbLineMode7);
        }

        private void bnSetLineMode9_Click(object sender, EventArgs e)
        {
            set_Mode(8, cbLineMode9);
        }
        private void bnSetLineMode10_Click(object sender, EventArgs e)
        {
            set_Mode(9, cbLineMode10);
        }
        private void bnSetLineMode11_Click(object sender, EventArgs e)
        {
            set_Mode(10, cbLineMode11);
        }
        private void bnSetLineMode12_Click(object sender, EventArgs e)
        {
            set_Mode(11, cbLineMode12);
        }
        private void bnGetLineMode9_Click(object sender, EventArgs e)
        {
            get_Mode(8, cbLineMode9);
        }
        private void bnGetLineMode10_Click(object sender, EventArgs e)
        {
            get_Mode(9, cbLineMode10);
        }
        private void bnGetLineMode11_Click(object sender, EventArgs e)
        {
            get_Mode(10, cbLineMode11);
        }
        private void bnGetLineMode12_Click(object sender, EventArgs e)
        {
            get_Mode(11, cbLineMode12);
        }
        private void bnSetLineSel9_Click(object sender, EventArgs e)
        {
            set_Selector(8, cbLineSel9);
        }
        private void bnSetLineSel10_Click(object sender, EventArgs e)
        {
            set_Selector(9, cbLineSel10);
        }
        private void bnSetLineSel11_Click(object sender, EventArgs e)
        {
            set_Selector(10, cbLineSel11);
        }
        private void bnSetLineSel12_Click(object sender, EventArgs e)
        {
            set_Selector(11, cbLineSel12);
        }
        private void bnGetLineSel9_Click(object sender, EventArgs e)
        {
            get_Selector(8, cbLineSel9);
        }
        private void bnGetLineSel10_Click(object sender, EventArgs e)
        {
            get_Selector(9, cbLineSel10);
        }
        private void bnGetLineSel11_Click(object sender, EventArgs e)
        {
            get_Selector(10, cbLineSel11);
        }
        private void bnGetLineSel12_Click(object sender, EventArgs e)
        {
            get_Selector(11, cbLineSel12);
        }
        private void checkBox34_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(4, cbLineMode5, checkBox34);
        }

        private void checkBox40_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(5, cbLineMode6, checkBox40);
        }

        private void checkBox46_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(6, cbLineMode7, checkBox46);
        }

        private void checkBox52_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(7, cbLineMode8, checkBox52);
        }

        private void bnGetParam5_Click(object sender, EventArgs e)
        {
            GetParamFor(4);

        }

        private void bnGetParam6_Click(object sender, EventArgs e)
        {
            GetParamFor(5);

        }

        private void bnGetParam7_Click(object sender, EventArgs e)
        {
            GetParamFor(6);

        }

        private void bnGetParam8_Click(object sender, EventArgs e)
        {
            GetParamFor(7);

        }

        private void bnGetParam9_Click(object sender, EventArgs e)
        {
            GetParamFor(8);

        }

        private void bnGetParam10_Click(object sender, EventArgs e)
        {
            GetParamFor(9);

        }

        private void bnGetParam11_Click(object sender, EventArgs e)
        {
            GetParamFor(10);

        }

        private void bnGetParam12_Click(object sender, EventArgs e)
        {
            GetParamFor(11);

        }

        private void bnSetParam5_Click(object sender, EventArgs e)
        {
            SetParamFor(4);

        }

        private void bnSetParam6_Click(object sender, EventArgs e)
        {
            SetParamFor(5);

        }

        private void bnSetParam7_Click(object sender, EventArgs e)
        {
            SetParamFor(6);

        }

        private void bnSetParam8_Click(object sender, EventArgs e)
        {
            SetParamFor(7);

        }

        private void bnSetParam9_Click(object sender, EventArgs e)
        {
            SetParamFor(8);

        }

        private void bnSetParam10_Click(object sender, EventArgs e)
        {
            SetParamFor(9);

        }

        private void bnSetParam11_Click(object sender, EventArgs e)
        {
            SetParamFor(10);

        }

        private void bnSetParam12_Click(object sender, EventArgs e)
        {
            SetParamFor(11);

        }

        private void bnStartGrab5_Click(object sender, EventArgs e)
        {
            StartGrabFor(4);

        }

        private void bnStartGrab6_Click(object sender, EventArgs e)
        {
            StartGrabFor(5);

        }

        private void bnStartGrab7_Click(object sender, EventArgs e)
        {
            StartGrabFor(6);

        }

        private void bnStartGrab8_Click(object sender, EventArgs e)
        {
            StartGrabFor(7);

        }

        private void bnStopGrab5_Click(object sender, EventArgs e)
        {
            StopGrabFor(4);

        }

        private void bnStopGrab6_Click(object sender, EventArgs e)
        {
            StopGrabFor(5);

        }

        private void bnStopGrab7_Click(object sender, EventArgs e)
        {
            StopGrabFor(6);

        }

        private void bnStopGrab8_Click(object sender, EventArgs e)
        {
            StopGrabFor(7);

        }

        private void bnStartGrab9_Click(object sender, EventArgs e)
        {
            StartGrabFor(8);

        }

        private void bnStopGrab9_Click(object sender, EventArgs e)
        {
            StopGrabFor(8);

        }

        private void bnStartGrab10_Click(object sender, EventArgs e)
        {
            StartGrabFor(9);

        }

        private void bnStopGrab10_Click(object sender, EventArgs e)
        {
            StopGrabFor(9);

        }

        private void bnStartGrab11_Click(object sender, EventArgs e)
        {
            StartGrabFor(10);

        }

        private void bnStopGrab11_Click(object sender, EventArgs e)
        {
            StopGrabFor(10);

        }

        private void bnStartGrab12_Click(object sender, EventArgs e)
        {
            StartGrabFor(11);

        }

        private void bnStopGrab12_Click(object sender, EventArgs e)
        {
            StopGrabFor(11);

        }

        private void cbSoftTrigger5_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(4);

        }

        private void cbSoftTrigger6_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(5);

        }

        private void cbSoftTrigger7_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(6);

        }

        private void cbSoftTrigger8_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(7);

        }

        private void bnTriggerExec5_Click(object sender, EventArgs e)
        {
            TriggerExecFor(4);

        }

        private void bnTriggerExec6_Click(object sender, EventArgs e)
        {
            TriggerExecFor(5);

        }

        private void bnTriggerExec7_Click(object sender, EventArgs e)
        {
            TriggerExecFor(6);

        }

        private void bnTriggerExec8_Click(object sender, EventArgs e)
        {
            TriggerExecFor(7);

        }


        private void cbSoftTrigger9_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(8);

        }

        private void bnTriggerExec9_Click(object sender, EventArgs e)
        {
            TriggerExecFor(8);

        }

        private void cbSoftTrigger10_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(9);

        }

        private void bnTriggerExec10_Click(object sender, EventArgs e)
        {
            TriggerExecFor(9);

        }

        private void cbSoftTrigger11_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(10);

        }

        private void bnTriggerExec11_Click(object sender, EventArgs e)
        {
            TriggerExecFor(10);

        }

        private void cbSoftTrigger12_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(11);

        }

        private void bnTriggerExec12_Click(object sender, EventArgs e)
        {
            TriggerExecFor(11);

        }

        private void 相机5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob5);
        }

        private void 相机6ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob6);
        }

        private void 相机7ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob7);
        }

        private void 相机8ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob8);
        }

        private void 相机9ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob9);
        }

        private void 相机10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob10);
        }

        private void 相机11ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob11);
        }

        private void 相机12ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jiankong_Huamian(_jobs.myjob12);
        }

        private void cogRecordDisplay9_Enter(object sender, EventArgs e)
        {

        }

        private void listBox14_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(5, _jobs.myjob5, listBox14.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox15_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(6, _jobs.myjob6, listBox15.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox18_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(7, _jobs.myjob7, listBox18.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox17_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(8, _jobs.myjob8, listBox17.SelectedItem.ToString());
            }
            catch
            { }
        }

        private void listBox16_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(9, _jobs.myjob9, listBox16.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox23_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(10, _jobs.myjob10, listBox23.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox24_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(11, _jobs.myjob11, listBox24.SelectedItem.ToString());
            }
            catch { }
        }

        private void listBox25_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                chatu_fangfa(12, _jobs.myjob12, listBox25.SelectedItem.ToString());
            }
            catch { }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            if (button31.Text == "IO未反转")
            {
                button31.Text = "IO已反转";
                shuchuqufan = "Reject";
            }
            else
            {
                button31.Text = "IO未反转";
                shuchuqufan = "Accept";
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            int temp_time = 0;
            temp_time = int.Parse(textBox3.Text.ToString().Trim());
            _jobs.myjob1.timespace = temp_time;
            _jobs.myjob2.timespace = temp_time;
            _jobs.myjob3.timespace = temp_time;
            _jobs.myjob4.timespace = temp_time;
            _jobs.myjob5.timespace = temp_time;
            _jobs.myjob6.timespace = temp_time;
            _jobs.myjob7.timespace = temp_time;
            _jobs.myjob8.timespace = temp_time;
            _jobs.myjob9.timespace = temp_time;
            _jobs.myjob10.timespace = temp_time;
            _jobs.myjob11.timespace = temp_time;
            _jobs.myjob12.timespace = temp_time;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.CheckState == CheckState.Checked)
            {
                _jobs.myjob1.cunok = true;
                _jobs.myjob2.cunok = true;
                _jobs.myjob3.cunok = true;
                _jobs.myjob4.cunok = true;
                _jobs.myjob5.cunok = true;
                _jobs.myjob6.cunok = true;
                _jobs.myjob7.cunok = true;
                _jobs.myjob8.cunok = true;
                _jobs.myjob9.cunok = true;
                _jobs.myjob10.cunok = true;
                _jobs.myjob11.cunok = true;
                _jobs.myjob12.cunok = true;
            }
            else
            {
                _jobs.myjob1.cunok = false;
                _jobs.myjob2.cunok = false;
                _jobs.myjob3.cunok = false;
                _jobs.myjob4.cunok = false;
                _jobs.myjob5.cunok = false;
                _jobs.myjob6.cunok = false;
                _jobs.myjob7.cunok = false;
                _jobs.myjob8.cunok = false;
                _jobs.myjob9.cunok = false;
                _jobs.myjob10.cunok = false;
                _jobs.myjob11.cunok = false;
                _jobs.myjob12.cunok = false;

            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.CheckState == CheckState.Checked)
            {
                _jobs.myjob1.cunng = true;
                _jobs.myjob2.cunng = true;
                _jobs.myjob3.cunng = true;
                _jobs.myjob4.cunng = true;
                _jobs.myjob5.cunng = true;
                _jobs.myjob6.cunng = true;
                _jobs.myjob7.cunng = true;
                _jobs.myjob8.cunng = true;
                _jobs.myjob9.cunng = true;
                _jobs.myjob10.cunng = true;
                _jobs.myjob11.cunng = true;
                _jobs.myjob12.cunng = true;
            }
            else
            {
                _jobs.myjob1.cunng = false;
                _jobs.myjob2.cunng = false;
                _jobs.myjob3.cunng = false;
                _jobs.myjob4.cunng = false;
                _jobs.myjob5.cunng = false;
                _jobs.myjob6.cunng = false;
                _jobs.myjob7.cunng = false;
                _jobs.myjob8.cunng = false;
                _jobs.myjob9.cunng = false;
                _jobs.myjob10.cunng = false;
                _jobs.myjob11.cunng = false;
                _jobs.myjob12.cunng = false;

            }
        }

        private void checkBox31_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox31.CheckState == CheckState.Checked)
                {
                    _jobs.myjob5.serial = true;
                }
                else
                {

                    _jobs.myjob5.serial = false;
                }
            }
            catch { }
        }

        private void checkBox27_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox27.CheckState == CheckState.Checked)
                {
                    _jobs.myjob1.serial = true;
                }
                else
                {

                    _jobs.myjob1.serial = false;
                }
            }
            catch { }
        }

        private void checkBox28_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox28.CheckState == CheckState.Checked)
                {
                    _jobs.myjob2.serial = true;
                }
                else
                {

                    _jobs.myjob2.serial = false;
                }
            }
            catch { }
        }

        private void checkBox29_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox29.CheckState == CheckState.Checked)
                {
                    _jobs.myjob3.serial = true;
                }
                else
                {

                    _jobs.myjob3.serial = false;
                }
            }
            catch { }
        }

        private void checkBox30_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox30.CheckState == CheckState.Checked)
                {
                    _jobs.myjob4.serial = true;
                }
                else
                {

                    _jobs.myjob4.serial = false;
                }
            }
            catch { }
        }

        private void checkBox37_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox37.CheckState == CheckState.Checked)
                {
                    _jobs.myjob6.serial = true;
                }
                else
                {

                    _jobs.myjob6.serial = false;
                }
            }
            catch { }
        }

        private void checkBox43_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox43.CheckState == CheckState.Checked)
                {
                    _jobs.myjob7.serial = true;
                }
                else
                {

                    _jobs.myjob7.serial = false;
                }
            }
            catch { }
        }

        private void checkBox49_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox49.CheckState == CheckState.Checked)
                {
                    _jobs.myjob8.serial = true;
                }
                else
                {

                    _jobs.myjob8.serial = false;
                }
            }
            catch { }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox5.CheckState == CheckState.Checked)
                {
                    _jobs.myjob1.tcp = true;
                }
                else
                {

                    _jobs.myjob1.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox9.CheckState == CheckState.Checked)
                {
                    _jobs.myjob2.tcp = true;
                }
                else
                {

                    _jobs.myjob2.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox15.CheckState == CheckState.Checked)
                {
                    _jobs.myjob3.tcp = true;
                }
                else
                {

                    _jobs.myjob3.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox19_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox19.CheckState == CheckState.Checked)
                {
                    _jobs.myjob4.tcp = true;
                }
                else
                {

                    _jobs.myjob4.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox33_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox33.CheckState == CheckState.Checked)
                {
                    _jobs.myjob5.tcp = true;
                }
                else
                {

                    _jobs.myjob5.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox39_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox39.CheckState == CheckState.Checked)
                {
                    _jobs.myjob6.tcp = true;
                }
                else
                {

                    _jobs.myjob6.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox45_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox45.CheckState == CheckState.Checked)
                {
                    _jobs.myjob7.tcp = true;
                }
                else
                {

                    _jobs.myjob7.tcp = false;
                }
            }
            catch { }
        }

        private void checkBox51_CheckedChanged(object sender, EventArgs e)
        {
            try
            {

                if (checkBox51.CheckState == CheckState.Checked)
                {
                    _jobs.myjob8.tcp = true;
                }
                else
                {

                    _jobs.myjob8.tcp = false;
                }
            }
            catch { }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
     
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button32_Click(object sender, EventArgs e)
        {
            try
            {
                string path = (textBox19.Text ?? "").Trim();
                if (string.IsNullOrEmpty(path))
                {
                    MessageBox.Show("请先选择要启动的程序。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // 2026-09-06：仅允许 .exe / .application，且文件必须真实存在，杜绝任意命令执行
                if (!File.Exists(path))
                {
                    MessageBox.Show("文件不存在：" + path, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".exe" && ext != ".application")
                {
                    MessageBox.Show("只允许启动 .exe / .application 程序。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox19_DoubleClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "应用 File|*.application;*.exe*";
            DialogResult openFileRes = openFileDialog.ShowDialog();
            if (DialogResult.OK == openFileRes)
            {
                textBox19.Text = openFileDialog.FileName;
            }
        }

        private void textBox19_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox25_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox25.CheckState == CheckState.Checked)
            {
                datajilu = 1;

            }
            else
            {
                datajilu = 0;

            }
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            _jobs.myjob1.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob2.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob3.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob4.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob5.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob6.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob7.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob8.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob9.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob10.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob11.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
            _jobs.myjob12.IOyanshi = decimal.ToInt32(numericUpDown5.Value);
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox13.CheckState == CheckState.Checked)
                _jobs.myjob2.shijianEn = true;
            else
                _jobs.myjob2.shijianEn = false;
        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox18.CheckState == CheckState.Checked)
                _jobs.myjob3.shijianEn = true;
            else
                _jobs.myjob3.shijianEn = false;
        }

        private void checkBox22_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox22.CheckState == CheckState.Checked)
                _jobs.myjob4.shijianEn = true;
            else
                _jobs.myjob4.shijianEn = false;
        }

        private void checkBox36_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox36.CheckState == CheckState.Checked)
                _jobs.myjob5.shijianEn = true;
            else
                _jobs.myjob5.shijianEn = false;
        }

        private void checkBox42_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox42.CheckState == CheckState.Checked)
                _jobs.myjob6.shijianEn = true;
            else
                _jobs.myjob6.shijianEn = false;
        }

        private void checkBox48_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox48.CheckState == CheckState.Checked)
                _jobs.myjob7.shijianEn = true;
            else
                _jobs.myjob7.shijianEn = false;
        }

        private void checkBox54_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox54.CheckState == CheckState.Checked)
                _jobs.myjob8.shijianEn = true;
            else
                _jobs.myjob8.shijianEn = false;
        }

        private void mesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 2026-09-06：MES/MySQL 等敏感功能加厂家登录门控（保留万能密码 "A"）
            if (!Form5.Authorize(this))
                return;

            // MES 通讯窗体（Apifox 式 HTTP 接口调试、WebService/TCP 预留）
            if (frmMES == null)
                frmMES = new FormMES();
            if (frmMES.Visible == false)
            {
                frmMES.Show();
                frmMES.Activate();
            }
            else
                frmMES.Hide();
        }

        private void mysqlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 2026-09-06：MES/MySQL 等敏感功能加厂家登录门控（保留万能密码 "A"）
            if (!Form5.Authorize(this))
                return;

            // MySQL 数据库查询窗体
            if (frmMySQL == null)
                frmMySQL = new FormMySQL();
            if (frmMySQL.Visible == false)
            {
                frmMySQL.Show();
                frmMySQL.Activate();
            }
            else
                frmMySQL.Hide();
        }

        // “连接设备”总管理器：四协议连接 2~4 的添加/删除/编辑与整窗嵌入查看；
        // 各协议“连接 1”入口亦收归于此（打开管理器并定位对应连接，嵌入其常驻原窗查看）。
        private FormCommManager frmCommManager;

        private void 连接设备ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frmCommManager == null || frmCommManager.IsDisposed)
                frmCommManager = new FormCommManager(_comm, frm3);   // 无协议连接 1 宿主：Form3 主窗单例（阶段 6）
            if (frmCommManager.Visible == false)
            {
                frmCommManager.Show();
                frmCommManager.Activate();
            }
            else
                frmCommManager.Hide();
        }

        private void 三菱Fx编程口ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_comm.Melsec.Visible == false)
                _comm.Melsec.Visible = true;
            else
                _comm.Melsec.Visible = false;
        }

        /// <summary>
        /// ★ 2026-09-13：单路流程切换的统一入口，补齐检测事务保护。
        /// 原实现只检查 _jobs.yunxing == false 就直接替换 myjob.block —— 但 yunxing=false 只表示
        /// "停止按钮已按下"，并不保证在途检测已结束；长耗时检测未完成时替换，会出现
        /// "在旧 block 上 Run、却从新 block 读结果"的错配。这里在替换前禁止进入检测并等待事务排空。
        /// 注意：只恢复"本方法此次造成的暂停"，不影响切型流程自身的暂停状态。
        /// </summary>
        private bool SwitchJobBlock(int slot, CogToolBlock newBlock)
        {
            if (slot < 0 || slot >= 12 || newBlock == null) return false;
            bool wasPaused = _inspectionLifecycle.IsPaused;
            if (!wasPaused) _inspectionLifecycle.StopAccepting();
            try
            {
                if (!_inspectionLifecycle.WaitForIdle(10000))
                {
                    // ★ 2026-09-13：超时则取消本次切换，绝不"带病替换"——
                    //   否则在途检测仍可能在旧 block 上 Run、却从新 block 读结果。
                    _logger.WriteLog("切换流程：等待检测事务结束超时（相机" + (slot + 1) + "），已取消切换");
                    return false;
                }
                _jobs.Myjobs[slot].block = newBlock;
                return true;
            }
            finally
            {
                if (!wasPaused) _inspectionLifecycle.Resume();
            }
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(0, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\1\\" + comboBox7.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 1);
                    _config.WriteString("camera1", "fen", comboBox7.SelectedItem.ToString());
                    MessageBox.Show("切换流程1:" + comboBox7.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程1:" + comboBox7.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox7_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox7, 1);
        }

        private void comboBox9_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox9, 2);
        }

        private void comboBox10_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox10, 3);
        }

        private void comboBox11_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox11, 4);
        }

        private void comboBox12_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox12, 5);
        }

        private void comboBox13_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox13, 6);
        }

        private void comboBox14_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox14, 7);
        }

        private void comboBox15_DropDown(object sender, EventArgs e)
        {
            Showjob(comboBox15, 8);
        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(1, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\2\\" + comboBox9.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 2);
                    _config.WriteString("camera2", "fen", comboBox9.SelectedItem.ToString());
                    MessageBox.Show("切换流程2:" + comboBox9.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程2:" + comboBox9.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox10_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(2, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\3\\" + comboBox10.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 3);
                    _config.WriteString("camera3", "fen", comboBox10.SelectedItem.ToString());
                    MessageBox.Show("切换流程3:" + comboBox10.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程3:" + comboBox10.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(3, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\4\\" + comboBox11.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 4);
                    _config.WriteString("camera4", "fen", comboBox11.SelectedItem.ToString());
                    MessageBox.Show("切换流程4:" + comboBox11.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程4:" + comboBox11.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox12_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(4, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\5\\" + comboBox12.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 5);
                    _config.WriteString("camera5", "fen", comboBox12.SelectedItem.ToString());
                    MessageBox.Show("切换流程5:" + comboBox12.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程5:" + comboBox12.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox13_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(5, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\6\\" + comboBox13.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 6);
                    _config.WriteString("camera6", "fen", comboBox13.SelectedItem.ToString());
                    MessageBox.Show("切换流程6:" + comboBox13.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程6:" + comboBox13.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox14_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(6, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\7\\" + comboBox14.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 7);
                    _config.WriteString("camera7", "fen", comboBox14.SelectedItem.ToString());
                    MessageBox.Show("切换流程7:" + comboBox14.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程7:" + comboBox14.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void comboBox15_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_jobs.yunxing && Frm2.start == 1)
            {
                try
                {
                    if (!SwitchJobBlock(7, (CogToolBlock)CogSerializer.LoadObjectFromFile(wenjianjia + "\\8\\" + comboBox15.SelectedItem.ToString()))) throw new Exception("检测事务未结束，已取消切换流程" + 8);
                    _config.WriteString("camera8", "fen", comboBox15.SelectedItem.ToString());
                    MessageBox.Show("切换流程8:" + comboBox15.SelectedItem.ToString() + "成功");
                }
                catch (Exception ex)
                {
                    _logger.WriteLog(ex.Message);
                    MessageBox.Show("切换流程8:" + comboBox15.SelectedItem.ToString() + "失败");
                }
            }
        }

        private void tabPage10_Click(object sender, EventArgs e)
        {

        }

        private void tabPage11_Click(object sender, EventArgs e)
        {

        }

        private void tabPage12_Click(object sender, EventArgs e)
        {

        }

        private void tabPage13_Click(object sender, EventArgs e)
        {

        }

        private void tabPage14_Click(object sender, EventArgs e)
        {

        }

        private void comboBox16_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 0)
                {
                    textBox13.Text = _jobs.myjob1.block.Inputs[comboBox16.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox16_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 0)
                {
                    comboBox16.Items.Clear();
                    for (int i = 0; i < _jobs.myjob1.block.Inputs.Count; i++)
                    {
                        comboBox16.Items.Add(_jobs.myjob1.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button35_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 0)
                {
                    _jobs.myjob1.block.Inputs[comboBox16.Text].Value = textBox13.Text;
                }
                MessageBox.Show("参数:" + comboBox16.Text + "写入数值" + textBox13.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox26_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 7)
                {
                    textBox35.Text = _jobs.myjob8.block.Inputs[comboBox26.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox26_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 7)
                {
                    comboBox26.Items.Clear();
                    for (int i = 0; i < _jobs.myjob8.block.Inputs.Count; i++)
                    {
                        comboBox26.Items.Add(_jobs.myjob8.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button49_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 7)
                {
                    _jobs.myjob8.block.Inputs[comboBox26.Text].Value = textBox35.Text;
                }
                MessageBox.Show("参数:" + comboBox26.Text + "写入数值" + textBox35.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox17_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 1)
                {
                    textBox23.Text = _jobs.myjob2.block.Inputs[comboBox17.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button42_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 1)
                {
                    _jobs.myjob2.block.Inputs[comboBox17.Text].Value = textBox23.Text;
                }
                MessageBox.Show("参数:" + comboBox17.Text + "写入数值" + textBox23.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox17_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 1)
                {
                    comboBox17.Items.Clear();
                    for (int i = 0; i < _jobs.myjob2.block.Inputs.Count; i++)
                    {
                        comboBox17.Items.Add(_jobs.myjob2.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox18_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 2)
                {
                    comboBox18.Items.Clear();
                    for (int i = 0; i < _jobs.myjob3.block.Inputs.Count; i++)
                    {
                        comboBox18.Items.Add(_jobs.myjob3.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox19_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 3)
                {
                    comboBox19.Items.Clear();
                    for (int i = 0; i < _jobs.myjob4.block.Inputs.Count; i++)
                    {
                        comboBox19.Items.Add(_jobs.myjob4.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox20_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 4)
                {
                    comboBox20.Items.Clear();
                    for (int i = 0; i < _jobs.myjob5.block.Inputs.Count; i++)
                    {
                        comboBox20.Items.Add(_jobs.myjob5.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox23_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 5)
                {
                    comboBox23.Items.Clear();
                    for (int i = 0; i < _jobs.myjob6.block.Inputs.Count; i++)
                    {
                        comboBox23.Items.Add(_jobs.myjob6.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox24_DropDown(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 6)
                {
                    comboBox24.Items.Clear();
                    for (int i = 0; i < _jobs.myjob7.block.Inputs.Count; i++)
                    {
                        comboBox24.Items.Add(_jobs.myjob7.block.Inputs[i].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button48_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 6)
                {
                    _jobs.myjob7.block.Inputs[comboBox24.Text].Value = textBox31.Text;
                }
                MessageBox.Show("参数:" + comboBox24.Text + "写入数值" + textBox31.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button47_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 5)
                {
                    _jobs.myjob6.block.Inputs[comboBox23.Text].Value = textBox30.Text;
                }
                MessageBox.Show("参数:" + comboBox23.Text + "写入数值" + textBox30.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button46_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 4)
                {
                    _jobs.myjob5.block.Inputs[comboBox20.Text].Value = textBox29.Text;
                }
                MessageBox.Show("参数:" + comboBox20.Text + "写入数值" + textBox29.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button45_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 3)
                {
                    _jobs.myjob4.block.Inputs[comboBox19.Text].Value = textBox25.Text;
                }
                MessageBox.Show("参数:" + comboBox19.Text + "写入数值" + textBox25.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button43_Click(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 2)
                {
                    _jobs.myjob3.block.Inputs[comboBox18.Text].Value = textBox24.Text;
                }
                MessageBox.Show("参数:" + comboBox18.Text + "写入数值" + textBox24.Text + "成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox18_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 2)
                {
                    textBox24.Text = _jobs.myjob3.block.Inputs[comboBox18.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox19_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 3)
                {
                    textBox25.Text = _jobs.myjob4.block.Inputs[comboBox19.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox20_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 4)
                {
                    textBox29.Text = _jobs.myjob5.block.Inputs[comboBox20.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox23_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 5)
                {
                    textBox30.Text = _jobs.myjob6.block.Inputs[comboBox23.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox24_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (manager1.JobCount > 6)
                {
                    textBox31.Text = _jobs.myjob7.block.Inputs[comboBox24.Text].Value.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void comboBox27_DropDownClosed(object sender, EventArgs e)
        {

        }
        Dictionary<string, string> toolName1 = new Dictionary<string, string>();
        Dictionary<string, ICogTool> tools1 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools2 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools3 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools4 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools5 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools6 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools7 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools8 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools9 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools10 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools11 = new Dictionary<string, ICogTool>();
        Dictionary<string, ICogTool> tools12 = new Dictionary<string, ICogTool>();
        void gongjukuai(string camere, string jieguo)
        {
            CogToolBlock block_temp;
            int youwu = 0;
            this.Invoke(new Action(() =>
            {
                switch (camere)
                {
                    case "1":
                        if (jieguo == "Accept")
                        {
                            c11.Text = "ok";
                            c11.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            c11.Text = "ng";
                            c11.BackColor = Color.Red;
                        }

                        _jobs.myjob1.list_block.Clear();
                        c12.Visible = false;
                        c13.Visible = false;
                        c14.Visible = false;
                        c15.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob1.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob1.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob1.list_block.Count)
                                            {
                                                case 0:
                                                    c12.Visible = true;
                                                    break;
                                                case 1:
                                                    c13.Visible = true;
                                                    break;
                                                case 2:
                                                    c14.Visible = true;
                                                    break;
                                                case 3:
                                                    c15.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob1.list_block.Add(_jobs.myjob1.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            // for (int j= 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob1.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob1.list_block.Count)
                                                        {
                                                            case 0:
                                                                c12.Visible = true;
                                                                break;
                                                            case 1:
                                                                c13.Visible = true;
                                                                break;
                                                            case 2:
                                                                c14.Visible = true;
                                                                break;
                                                            case 3:
                                                                c15.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob1.list_block.Add(_jobs.myjob1.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        //{
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob1.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob1.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            c12.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            c13.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            c14.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            c15.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob1.list_block.Add(_jobs.myjob1.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //  }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                //}
                            }
                        }
                        break;
                    case "2":
                        if (jieguo == "Accept")
                        {
                            c21.Text = "ok";
                            c21.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            c21.Text = "ng";
                            c21.BackColor = Color.Red;
                        }
                        _jobs.myjob2.list_block.Clear();
                        c22.Visible = false;
                        c23.Visible = false;
                        c24.Visible = false;
                        c25.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob2.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                //for (int i = 0; i < block_temp.Outputs.Count; i++)
                                //  {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob2.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob2.list_block.Count)
                                            {
                                                case 0:
                                                    c22.Visible = true;
                                                    break;
                                                case 1:
                                                    c23.Visible = true;
                                                    break;
                                                case 2:
                                                    c24.Visible = true;
                                                    break;
                                                case 3:
                                                    c25.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob2.list_block.Add(_jobs.myjob2.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            // for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob2.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob2.list_block.Count)
                                                        {
                                                            case 0:
                                                                c22.Visible = true;
                                                                break;
                                                            case 1:
                                                                c23.Visible = true;
                                                                break;
                                                            case 2:
                                                                c24.Visible = true;
                                                                break;
                                                            case 3:
                                                                c25.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob2.list_block.Add(_jobs.myjob2.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        //for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int j = 0; j < block_temp.Outputs.Count; j++)
                                                        {
                                                            if (block_temp.Outputs[j].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob2.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob2.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            c22.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            c23.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            c24.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            c25.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob2.list_block.Add(_jobs.myjob2.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //   }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }
                                //  }
                            }
                        }
                        break;
                    case "3":

                        _jobs.myjob3.list_block.Clear();
                        if (tableLayoutPanel5.Visible == true)
                        {
                            if (jieguo == "Accept")
                            {
                                c31.Text = "ok";
                                c31.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                c31.Text = "ng";
                                c31.BackColor = Color.Red;
                            }
                            c32.Visible = false;
                            c33.Visible = false;
                            c34.Visible = false;
                            c35.Visible = false;
                        }
                        else
                        {
                            if (jieguo == "Accept")
                            {
                                c41.Text = "ok";
                                c41.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                c41.Text = "ng";
                                c41.BackColor = Color.Red;
                            }
                            c42.Visible = false;
                            c43.Visible = false;
                            c44.Visible = false;
                            c45.Visible = false;
                        }
                        foreach (ICogTool tool in _jobs.myjob3.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                //for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob3.list_block.Count < 4)
                                        {
                                            if (tableLayoutPanel5.Visible == true)
                                            {
                                                switch (_jobs.myjob3.list_block.Count)
                                                {
                                                    case 0:
                                                        c32.Visible = true;
                                                        break;
                                                    case 1:
                                                        c33.Visible = true;
                                                        break;
                                                    case 2:
                                                        c34.Visible = true;
                                                        break;
                                                    case 3:
                                                        c35.Visible = true;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                switch (_jobs.myjob3.list_block.Count)
                                                {
                                                    case 0:
                                                        c42.Visible = true;
                                                        break;
                                                    case 1:
                                                        c43.Visible = true;
                                                        break;
                                                    case 2:
                                                        c44.Visible = true;
                                                        break;
                                                    case 3:
                                                        c45.Visible = true;
                                                        break;
                                                }
                                            }
                                            _jobs.myjob3.list_block.Add(_jobs.myjob3.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            // for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            //  {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob3.list_block.Count < 4)
                                                    {
                                                        if (tableLayoutPanel5.Visible == true)
                                                        {
                                                            switch (_jobs.myjob3.list_block.Count)
                                                            {
                                                                case 0:
                                                                    c32.Visible = true;
                                                                    break;
                                                                case 1:
                                                                    c33.Visible = true;
                                                                    break;
                                                                case 2:
                                                                    c34.Visible = true;
                                                                    break;
                                                                case 3:
                                                                    c35.Visible = true;
                                                                    break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            switch (_jobs.myjob3.list_block.Count)
                                                            {
                                                                case 0:
                                                                    c42.Visible = true;
                                                                    break;
                                                                case 1:
                                                                    c43.Visible = true;
                                                                    break;
                                                                case 2:
                                                                    c44.Visible = true;
                                                                    break;
                                                                case 3:
                                                                    c45.Visible = true;
                                                                    break;
                                                            }
                                                        }
                                                        _jobs.myjob3.list_block.Add(_jobs.myjob3.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        //    for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        //  {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob3.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob3.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            c32.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            c33.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            c34.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            c35.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob3.list_block.Add(_jobs.myjob3.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //  }
                                                    }
                                                }

                                            }

                                            // }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                    case "4":

                        _jobs.myjob4.list_block.Clear();
                        if (tableLayoutPanel5.Visible == true)
                        {
                            if (jieguo == "Accept")
                            {
                                c41.Text = "ok";
                                c41.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                c41.Text = "ng";
                                c41.BackColor = Color.Red;
                            }
                            c42.Visible = false;
                            c43.Visible = false;
                            c44.Visible = false;
                            c45.Visible = false;
                        }
                        else
                        {
                            if (jieguo == "Accept")
                            {
                                c51.Text = "ok";
                                c51.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                c51.Text = "ng";
                                c51.BackColor = Color.Red;
                            }
                            c52.Visible = false;
                            c53.Visible = false;
                            c54.Visible = false;
                            c55.Visible = false;
                        }
                        foreach (ICogTool tool in _jobs.myjob4.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob4.list_block.Count < 4)
                                        {
                                            if (tableLayoutPanel5.Visible == true)
                                            {
                                                switch (_jobs.myjob4.list_block.Count)
                                                {
                                                    case 0:
                                                        c42.Visible = true;
                                                        break;
                                                    case 1:
                                                        c43.Visible = true;
                                                        break;
                                                    case 2:
                                                        c44.Visible = true;
                                                        break;
                                                    case 3:
                                                        c45.Visible = true;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                switch (_jobs.myjob4.list_block.Count)
                                                {
                                                    case 0:
                                                        c52.Visible = true;
                                                        break;
                                                    case 1:
                                                        c53.Visible = true;
                                                        break;
                                                    case 2:
                                                        c54.Visible = true;
                                                        break;
                                                    case 3:
                                                        c55.Visible = true;
                                                        break;
                                                }
                                            }
                                            _jobs.myjob4.list_block.Add(_jobs.myjob4.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            //  for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob4.list_block.Count < 4)
                                                    {
                                                        if (tableLayoutPanel5.Visible == true)
                                                        {
                                                            switch (_jobs.myjob4.list_block.Count)
                                                            {
                                                                case 0:
                                                                    c42.Visible = true;
                                                                    break;
                                                                case 1:
                                                                    c43.Visible = true;
                                                                    break;
                                                                case 2:
                                                                    c44.Visible = true;
                                                                    break;
                                                                case 3:
                                                                    c45.Visible = true;
                                                                    break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            switch (_jobs.myjob4.list_block.Count)
                                                            {
                                                                case 0:
                                                                    c52.Visible = true;
                                                                    break;
                                                                case 1:
                                                                    c53.Visible = true;
                                                                    break;
                                                                case 2:
                                                                    c54.Visible = true;
                                                                    break;
                                                                case 3:
                                                                    c55.Visible = true;
                                                                    break;
                                                            }
                                                        }
                                                        _jobs.myjob4.list_block.Add(_jobs.myjob4.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        //  for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        //  {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob4.list_block.Count < 4)
                                                                {
                                                                    if (tableLayoutPanel5.Visible == true)
                                                                    {
                                                                        switch (_jobs.myjob4.list_block.Count)
                                                                        {
                                                                            case 0:
                                                                                c42.Visible = true;
                                                                                break;
                                                                            case 1:
                                                                                c43.Visible = true;
                                                                                break;
                                                                            case 2:
                                                                                c44.Visible = true;
                                                                                break;
                                                                            case 3:
                                                                                c45.Visible = true;
                                                                                break;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        switch (_jobs.myjob4.list_block.Count)
                                                                        {
                                                                            case 0:
                                                                                c52.Visible = true;
                                                                                break;
                                                                            case 1:
                                                                                c53.Visible = true;
                                                                                break;
                                                                            case 2:
                                                                                c54.Visible = true;
                                                                                break;
                                                                            case 3:
                                                                                c55.Visible = true;
                                                                                break;
                                                                        }
                                                                    }
                                                                    _jobs.myjob4.list_block.Add(_jobs.myjob4.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //  }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                //  }
                            }
                        }
                        break;
                    case "5":
                        if (jieguo == "Accept")
                        {
                            c51.Text = "ok";
                            c51.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            c51.Text = "ng";
                            c51.BackColor = Color.Red;
                        }
                        _jobs.myjob5.list_block.Clear();
                        c52.Visible = false;
                        c53.Visible = false;
                        c54.Visible = false;
                        c55.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob5.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob5.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob5.list_block.Count)
                                            {
                                                case 0:
                                                    c52.Visible = true;
                                                    break;
                                                case 1:
                                                    c53.Visible = true;
                                                    break;
                                                case 2:
                                                    c54.Visible = true;
                                                    break;
                                                case 3:
                                                    c55.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob5.list_block.Add(_jobs.myjob5.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            // for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            //  {
                                            //  for (int k = 0; k < block_temp.Outputs.Count; k++)
                                            //  {
                                            youwu = 0;
                                            for (int k = 0; k < block_temp.Outputs.Count; k++)
                                            {
                                                if (block_temp.Outputs[k].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob5.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob5.list_block.Count)
                                                        {
                                                            case 0:
                                                                c52.Visible = true;
                                                                break;
                                                            case 1:
                                                                c53.Visible = true;
                                                                break;
                                                            case 2:
                                                                c54.Visible = true;
                                                                break;
                                                            case 3:
                                                                c55.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob5.list_block.Add(_jobs.myjob5.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob5.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob5.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            c52.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            c53.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            c54.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            c55.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob5.list_block.Add(_jobs.myjob5.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //  }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                    case "6":
                        if (jieguo == "Accept")
                        {
                            c61.Text = "ok";
                            c61.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            c61.Text = "ng";
                            c61.BackColor = Color.Red;
                        }
                        _jobs.myjob6.list_block.Clear();
                        c62.Visible = false;
                        c63.Visible = false;
                        c64.Visible = false;
                        c65.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob6.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                //  for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob6.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob6.list_block.Count)
                                            {
                                                case 0:
                                                    c62.Visible = true;
                                                    break;
                                                case 1:
                                                    c63.Visible = true;
                                                    break;
                                                case 2:
                                                    c64.Visible = true;
                                                    break;
                                                case 3:
                                                    c65.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob6.list_block.Add(_jobs.myjob6.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            //  for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            //  {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob6.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob6.list_block.Count)
                                                        {
                                                            case 0:
                                                                c62.Visible = true;
                                                                break;
                                                            case 1:
                                                                c63.Visible = true;
                                                                break;
                                                            case 2:
                                                                c64.Visible = true;
                                                                break;
                                                            case 3:
                                                                c65.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob6.list_block.Add(_jobs.myjob6.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob6.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob6.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            c62.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            c63.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            c64.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            c65.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob6.list_block.Add(_jobs.myjob6.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        // }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                //  }
                            }
                        }
                        break;
                    case "7":
                        if (jieguo == "Accept")
                        {
                            c71.Text = "ok";
                            c71.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            c71.Text = "ng";
                            c71.BackColor = Color.Red;
                        }
                        _jobs.myjob7.list_block.Clear();
                        c72.Visible = false;
                        c73.Visible = false;
                        c74.Visible = false;
                        c75.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob7.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob7.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob7.list_block.Count)
                                            {
                                                case 0:
                                                    c72.Visible = true;
                                                    break;
                                                case 1:
                                                    c73.Visible = true;
                                                    break;
                                                case 2:
                                                    c74.Visible = true;
                                                    break;
                                                case 3:
                                                    c75.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob7.list_block.Add(_jobs.myjob7.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            // for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob7.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob7.list_block.Count)
                                                        {
                                                            case 0:
                                                                c72.Visible = true;
                                                                break;
                                                            case 1:
                                                                c73.Visible = true;
                                                                break;
                                                            case 2:
                                                                c74.Visible = true;
                                                                break;
                                                            case 3:
                                                                c75.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob7.list_block.Add(_jobs.myjob7.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob7.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob7.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            c72.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            c73.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            c74.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            c75.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob7.list_block.Add(_jobs.myjob7.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //  }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                    case "8":
                        if (jieguo == "Accept")
                        {
                            c81.Text = "ok";
                            c81.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            c81.Text = "ng";
                            c81.BackColor = Color.Red;
                        }
                        _jobs.myjob8.list_block.Clear();
                        c82.Visible = false;
                        c83.Visible = false;
                        c84.Visible = false;
                        c85.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob8.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob8.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob8.list_block.Count)
                                            {
                                                case 0:
                                                    c82.Visible = true;
                                                    break;
                                                case 1:
                                                    c83.Visible = true;
                                                    break;
                                                case 2:
                                                    c84.Visible = true;
                                                    break;
                                                case 3:
                                                    c85.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob8.list_block.Add(_jobs.myjob8.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            //for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob8.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob8.list_block.Count)
                                                        {
                                                            case 0:
                                                                c82.Visible = true;
                                                                break;
                                                            case 1:
                                                                c83.Visible = true;
                                                                break;
                                                            case 2:
                                                                c84.Visible = true;
                                                                break;
                                                            case 3:
                                                                c85.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob8.list_block.Add(_jobs.myjob8.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob8.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob8.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            c82.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            c83.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            c84.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            c85.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob8.list_block.Add(_jobs.myjob8.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //   }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                        case "9":
                        if (jieguo == "Accept")
                        {
                            button118.Text = "ok";
                            button118.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            button118.Text = "ng";
                            button118.BackColor = Color.Red;
                        }
                        _jobs.myjob9.list_block.Clear();
                        button117.Visible = false;
                        button116.Visible = false;
                        button115.Visible = false;
                        button114.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob9.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob9.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob9.list_block.Count)
                                            {
                                                case 0:
                                                    button117.Visible = true;
                                                    break;
                                                case 1:
                                                    button116.Visible = true;
                                                    break;
                                                case 2:
                                                    button115.Visible = true;
                                                    break;
                                                case 3:
                                                    button114.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob9.list_block.Add(_jobs.myjob9.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            //for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob9.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob9.list_block.Count)
                                                        {
                                                            case 0:
                                                                button117.Visible = true;
                                                                break;
                                                            case 1:
                                                                button116.Visible = true;
                                                                break;
                                                            case 2:
                                                                button115.Visible = true;
                                                                break;
                                                            case 3:
                                                                button114.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob9.list_block.Add(_jobs.myjob9.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob9.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob9.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            button117.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            button116.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            button115.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            button114.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob9.list_block.Add(_jobs.myjob9.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //   }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                        case "10":
                        if (jieguo == "Accept")
                        {
                            button123.Text = "ok";
                            button123.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            button123.Text = "ng";
                            button123.BackColor = Color.Red;
                        }
                        _jobs.myjob10.list_block.Clear();
                        button122.Visible = false;
                        button121.Visible = false;
                        button120.Visible = false;
                        button119.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob10.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob10.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob10.list_block.Count)
                                            {
                                                case 0:
                                                    button122.Visible = true;
                                                    break;
                                                case 1:
                                                    button121.Visible = true;
                                                    break;
                                                case 2:
                                                    button120.Visible = true;
                                                    break;
                                                case 3:
                                                    button119.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob10.list_block.Add(_jobs.myjob10.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            //for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob10.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob10.list_block.Count)
                                                        {
                                                            case 0:
                                                                button122.Visible = true;
                                                                break;
                                                            case 1:
                                                                button121.Visible = true;
                                                                break;
                                                            case 2:
                                                                button120.Visible = true;
                                                                break;
                                                            case 3:
                                                                button119.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob10.list_block.Add(_jobs.myjob10.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob10.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob10.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            button122.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            button121.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            button120.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            button119.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob10.list_block.Add(_jobs.myjob10.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //   }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                        case "11":
                        if (jieguo == "Accept")
                        {
                            button128.Text = "ok";
                            button128.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            button128.Text = "ng";
                            button128.BackColor = Color.Red;
                        }
                        _jobs.myjob11.list_block.Clear();
                        button127.Visible = false;
                        button126.Visible = false;
                        button125.Visible = false;
                        button124.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob11.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob11.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob11.list_block.Count)
                                            {
                                                case 0:
                                                    button127.Visible = true;
                                                    break;
                                                case 1:
                                                    button126.Visible = true;
                                                    break;
                                                case 2:
                                                    button125.Visible = true;
                                                    break;
                                                case 3:
                                                    button124.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob11.list_block.Add(_jobs.myjob11.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            //for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob11.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob11.list_block.Count)
                                                        {
                                                            case 0:
                                                                button127.Visible = true;
                                                                break;
                                                            case 1:
                                                                button126.Visible = true;
                                                                break;
                                                            case 2:
                                                                button125.Visible = true;
                                                                break;
                                                            case 3:
                                                                button124.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob11.list_block.Add(_jobs.myjob11.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob11.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob11.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            button127.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            button126.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            button125.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            button124.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob11.list_block.Add(_jobs.myjob11.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //   }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                        case "12":
                        if (jieguo == "Accept")
                        {
                            button133.Text = "ok";
                            button133.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            button133.Text = "ng";
                            button133.BackColor = Color.Red;
                        }
                        _jobs.myjob12.list_block.Clear();
                        button132.Visible = false;
                        button131.Visible = false;
                        button130.Visible = false;
                        button129.Visible = false;
                        foreach (ICogTool tool in _jobs.myjob12.block.Tools)
                        {
                            if (tool is CogToolBlock)
                            {
                                block_temp = tool as CogToolBlock;
                                // for (int i = 0; i < block_temp.Outputs.Count; i++)
                                // {
                                youwu = 0;
                                for (int i = 0; i < block_temp.Outputs.Count; i++)
                                {
                                    if (block_temp.Outputs[i].Name == "RunStatus")
                                    {
                                        youwu = 1;
                                    }
                                }
                                if (youwu == 1)
                                {
                                    if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                    {
                                        if (_jobs.myjob12.list_block.Count < 4)
                                        {
                                            switch (_jobs.myjob12.list_block.Count)
                                            {
                                                case 0:
                                                    button132.Visible = true;
                                                    break;
                                                case 1:
                                                    button131.Visible = true;
                                                    break;
                                                case 2:
                                                    button130.Visible = true;
                                                    break;
                                                case 3:
                                                    button129.Visible = true;
                                                    break;
                                            }
                                            _jobs.myjob12.list_block.Add(_jobs.myjob12.list_block.Count + 1, block_temp);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (ICogTool tool1 in block_temp.Tools)
                                    {
                                        if (tool1 is CogToolBlock)
                                        {
                                            block_temp = tool1 as CogToolBlock;
                                            //for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            // {
                                            youwu = 0;
                                            for (int j = 0; j < block_temp.Outputs.Count; j++)
                                            {
                                                if (block_temp.Outputs[j].Name == "RunStatus")
                                                {
                                                    youwu = 1;
                                                }
                                            }
                                            if (youwu == 1)
                                            {
                                                if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                {
                                                    if (_jobs.myjob12.list_block.Count < 4)
                                                    {
                                                        switch (_jobs.myjob12.list_block.Count)
                                                        {
                                                            case 0:
                                                                button132.Visible = true;
                                                                break;
                                                            case 1:
                                                                button131.Visible = true;
                                                                break;
                                                            case 2:
                                                                button130.Visible = true;
                                                                break;
                                                            case 3:
                                                                button129.Visible = true;
                                                                break;
                                                        }
                                                        _jobs.myjob12.list_block.Add(_jobs.myjob12.list_block.Count + 1, block_temp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                foreach (ICogTool tool2 in block_temp.Tools)
                                                {
                                                    if (tool2 is CogToolBlock)
                                                    {
                                                        block_temp = tool2 as CogToolBlock;
                                                        // for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        // {
                                                        youwu = 0;
                                                        for (int k = 0; k < block_temp.Outputs.Count; k++)
                                                        {
                                                            if (block_temp.Outputs[k].Name == "RunStatus")
                                                            {
                                                                youwu = 1;
                                                            }
                                                        }
                                                        if (youwu == 1)
                                                        {
                                                            if (block_temp.Outputs["RunStatus"].Value.ToString() == "0")
                                                            {
                                                                if (_jobs.myjob12.list_block.Count < 4)
                                                                {
                                                                    switch (_jobs.myjob12.list_block.Count)
                                                                    {
                                                                        case 0:
                                                                            button132.Visible = true;
                                                                            break;
                                                                        case 1:
                                                                            button131.Visible = true;
                                                                            break;
                                                                        case 2:
                                                                            button130.Visible = true;
                                                                            break;
                                                                        case 3:
                                                                            button129.Visible = true;
                                                                            break;
                                                                    }
                                                                    _jobs.myjob12.list_block.Add(_jobs.myjob12.list_block.Count + 1, block_temp);
                                                                }
                                                            }
                                                        }
                                                        //   }
                                                    }
                                                }

                                            }

                                            //  }
                                        }
                                    }

                                }

                                // }
                            }
                        }
                        break;
                
                }
            }));
        }
        private void combdrop(ComboBox combox, CogToolBlock blk, Dictionary<string, ICogTool> dic)
        {
            // 每次下拉重建：清空字典与下拉项，防止重复添加相同键（原实现第二次下拉即抛“已添加相同键”）
            dic.Clear();
            combox.Items.Clear();
            AddRoiToolsRecursive(combox, dic, blk, "");
        }

        // 递归收集 PMAlign/Blob/PMAlignMulti 工具（嵌套 ToolBlock 用“块名.工具名”前缀），避免多层嵌套写死导致前缀错乱
        private static void AddRoiToolsRecursive(ComboBox combox, Dictionary<string, ICogTool> dic, CogToolBlock blk, string prefix)
        {
            foreach (ICogTool tool in blk.Tools)
            {
                string fullName = prefix + tool.Name;
                if (tool is CogToolBlock)
                {
                    AddRoiToolsRecursive(combox, dic, tool as CogToolBlock, fullName + ".");
                }
                else if (tool is CogPMAlignTool || tool is CogBlobTool || tool is CogPMAlignMultiTool)
                {
                    if (!dic.ContainsKey(fullName))
                    {
                        dic.Add(fullName, tool);
                        combox.Items.Add(fullName);
                    }
                }
            }
        }


        private void comboBox27_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox27, _jobs.myjob1.block, tools1);
        }

        private void checkBox26_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob1.roi = checkBox26.Checked;
            ICogTool selTool;
            if (tools1.TryGetValue(comboBox27.Text, out selTool))
                roiset2(_jobs.myjob1.block, selTool, _jobs.myjob1.roi, cogRecordDisplay1);
        }

        private void comboBox29_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox29, _jobs.myjob2.block, tools2);
        }

        private void comboBox30_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox30, _jobs.myjob3.block, tools3);
        }

        private void comboBox32_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox32, _jobs.myjob4.block, tools4);
        }

        private void comboBox33_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox33, _jobs.myjob5.block, tools5);
        }

        private void comboBox35_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox35, _jobs.myjob6.block, tools6);
        }

        private void comboBox36_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox36, _jobs.myjob7.block, tools7);
        }

        private void comboBox37_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox37, _jobs.myjob8.block, tools8);
        }

        private void comboBox37_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkBox68_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob8.roi = checkBox68.Checked;
            ICogTool selTool;
            if (tools8.TryGetValue(comboBox37.Text, out selTool))
                roiset2(_jobs.myjob8.block, selTool, checkBox68.Checked, cogRecordDisplay8);
        }

        private void checkBox67_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob7.roi = checkBox67.Checked;
            ICogTool selTool;
            if (tools7.TryGetValue(comboBox36.Text, out selTool))
                roiset2(_jobs.myjob7.block, selTool, checkBox67.Checked, cogRecordDisplay7);
        }

        private void checkBox66_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob6.roi = checkBox66.Checked;
            ICogTool selTool;
            if (tools6.TryGetValue(comboBox35.Text, out selTool))
                roiset2(_jobs.myjob6.block, selTool, checkBox66.Checked, cogRecordDisplay6);
        }

        private void checkBox65_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob5.roi = checkBox65.Checked;
            ICogTool selTool;
            if (tools5.TryGetValue(comboBox33.Text, out selTool))
                roiset2(_jobs.myjob5.block, selTool, checkBox65.Checked, cogRecordDisplay5);
        }

        private void checkBox64_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob4.roi = checkBox64.Checked;
            ICogTool selTool;
            if (tools4.TryGetValue(comboBox32.Text, out selTool))
                roiset2(_jobs.myjob4.block, selTool, checkBox64.Checked, cogRecordDisplay4);
        }

        private void checkBox63_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob3.roi = checkBox63.Checked;
            ICogTool selTool;
            if (tools3.TryGetValue(comboBox30.Text, out selTool))
                roiset2(_jobs.myjob3.block, selTool, checkBox63.Checked, cogRecordDisplay3);
        }

        private void checkBox62_CheckedChanged(object sender, EventArgs e)
        {
            _jobs.myjob2.roi = checkBox62.Checked;
            ICogTool selTool;
            if (tools2.TryGetValue(comboBox29.Text, out selTool))
                roiset2(_jobs.myjob2.block, selTool, checkBox62.Checked, cogRecordDisplay2);
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox11.Checked)
            {
                dataGridView1.ReadOnly = true;
                dataGridView1.DataSource = _jobs.myjob1.myTable;
            }
            else
            {
                dataGridView1.ReadOnly = false;
                dataGridView1.DataSource = _jobs.myjob1.myTable1;
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked)
            {
              
                dataGridView2.ReadOnly = true;
               
                dataGridView2.DataSource = _jobs.myjob2.myTable;
            }
            else
            {
             
                dataGridView2.ReadOnly = false;
                dataGridView2.DataSource = _jobs.myjob2.myTable1;
            }
        }

        private void comboBox27_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        // ★修复(重开遗留窗口)：原实现直接 new 覆盖 f9[slot]，旧 Form9 不关闭——每点一次多一个
        //   找不回、关不掉的孤儿工具块窗口(且仍挂在任务栏)。现重开前先关旧窗。
        private void ShowCameraToolWindow(int slot, Cognex.VisionPro.ToolBlock.CogToolBlock block, string title)
        {
            if (f9 == null || slot < 0 || slot >= f9.Length) return;
            try
            {
                Form9 old = f9[slot];
                if (old != null && !old.IsDisposed) old.Close();
            }
            catch { }
            f9[slot] = new Form9(block);
            f9[slot].Show();
            f9[slot].label2.Text = title;
        }

        private void 相机1ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(0, _jobs.myjob1.block, "相机一");
        }

        private void 相机2ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(1, _jobs.myjob2.block, "相机二");
        }

        private void 相机3ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(2, _jobs.myjob3.block, "相机三");
        }

        private void 相机4ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(3, _jobs.myjob4.block, "相机四");
        }

        private void 相机5ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(4, _jobs.myjob5.block, "相机五");
        }

        private void 相机6ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(5, _jobs.myjob6.block, "相机六");
        }

        private void 相机7ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(6, _jobs.myjob7.block, "相机七");
        }

        private void 相机8ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(7, _jobs.myjob8.block, "相机八");
        }

        private void 相机9ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(8, _jobs.myjob9.block, "相机九");
        }

        private void 相机10ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(9, _jobs.myjob10.block, "相机十");
        }

        private void 相机11ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(10, _jobs.myjob11.block, "相机十一");
        }

        private void 相机12ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowCameraToolWindow(11, _jobs.myjob12.block, "相机十二");
        }

        private void cogRecordDisplay1_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel2);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob1.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob1.records == 0)
                _jobs.myjob1.records = 1;
            else
                _jobs.myjob1.records = 0;
        }

        private void record_bian(int b1, int b2, float c2, float c3, float c4, int c5, float c6, float c7, float c8, float c9)
        {
            if (_layoutMode == "Row")
            {
                if (c5 == 0)
                {
                    // 行布局占屏：当前相机所在行列放大为 99%，其余缩为 0.5% 边条（单列整行全宽）
                    for (int c = 0; c < tableLayoutPanel1.ColumnCount; c++)
                        tableLayoutPanel1.ColumnStyles[c] = new ColumnStyle(SizeType.Percent, c == b1 ? 99f : 0.5f);
                    for (int r = 0; r < tableLayoutPanel1.RowCount; r++)
                        tableLayoutPanel1.RowStyles[r] = new RowStyle(SizeType.Percent, r == b2 ? 99f : 0.5f);
                }
                else
                {
                    // 行布局恢复：按当前布局模式重建（恢复默认比例）
                    display();
                }
                return;
            }
            if (c5 == 0)
            {
                if (b1 == 0 && b2 == 0)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 1 && b2 == 0)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 2 && b2 == 0)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 0 && b2 == 1)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 1 && b2 == 1)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 2 && b2 == 1)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 0 && b2 == 2)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 1 && b2 == 2)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 2 && b2 == 2)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 0.5f));
                }
                else if (b1 == 0 && b2 == 3)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 99f));
                }
                else if (b1 == 1 && b2 == 3)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 99f));
                }
                else if (b1 == 2 && b2 == 3)
                {
                    this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, 99f));
                    this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, 0.5f));
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, 99f));
                }

            }
            else
            {

                this.tableLayoutPanel1.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, c2));
                this.tableLayoutPanel1.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, c3));
                this.tableLayoutPanel1.ColumnStyles[2] = (new ColumnStyle(SizeType.Percent, c4));
                this.tableLayoutPanel1.RowStyles[0] = (new RowStyle(SizeType.Percent, c6));
                this.tableLayoutPanel1.RowStyles[1] = (new RowStyle(SizeType.Percent, c7));
                this.tableLayoutPanel1.RowStyles[2] = (new RowStyle(SizeType.Percent, c8));
                if (c9 > 0f)
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Percent, c9));
                else
                    this.tableLayoutPanel1.RowStyles[3] = (new RowStyle(SizeType.Absolute, 0F));

            }
        }

        private void cogRecordDisplay2_DoubleClick(object sender, EventArgs e)
        {

            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel3);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob2.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob2.records == 0)
                _jobs.myjob2.records = 1;
            else
                _jobs.myjob2.records = 0;
        }

        private void tableLayoutPanel1_DoubleClick(object sender, EventArgs e)
        {

        }

        private void cogRecordDisplay3_Enter(object sender, EventArgs e)
        {

        }

        private void cogRecordDisplay3_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel5);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob3.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob3.records == 0)
                _jobs.myjob3.records = 1;
            else
                _jobs.myjob3.records = 0;
        }

        private void cogRecordDisplay4_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel7);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob4.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob4.records == 0)
                _jobs.myjob4.records = 1;
            else
                _jobs.myjob4.records = 0;
        }

        private void cogRecordDisplay5_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel9);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob5.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob5.records == 0)
                _jobs.myjob5.records = 1;
            else
                _jobs.myjob5.records = 0;
        }

        private void cogRecordDisplay6_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel12);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob6.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob6.records == 0)
                _jobs.myjob6.records = 1;
            else
                _jobs.myjob6.records = 0;
        }

        private void cogRecordDisplay7_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel14);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob7.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob7.records == 0)
                _jobs.myjob7.records = 1;
            else
                _jobs.myjob7.records = 0;
        }

        private void cogRecordDisplay8_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel16);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob8.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob8.records == 0)
                _jobs.myjob8.records = 1;
            else
                _jobs.myjob8.records = 0;
        }

        private void cogRecordDisplay9_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel18);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob9.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob9.records == 0)
                _jobs.myjob9.records = 1;
            else
                _jobs.myjob9.records = 0;
        }

        private void cogRecordDisplay10_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel20);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob10.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob10.records == 0)
                _jobs.myjob10.records = 1;
            else
                _jobs.myjob10.records = 0;
        }

        private void cogRecordDisplay11_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel22);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob11.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob11.records == 0)
                _jobs.myjob11.records = 1;
            else
                _jobs.myjob11.records = 0;
        }

        private void cogRecordDisplay12_DoubleClick(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition p = new TableLayoutPanelCellPosition();
            p = tableLayoutPanel1.GetCellPosition(tableLayoutPanel24);
            record_bian(p.Column, p.Row, _jobs.myjob1.record[0], _jobs.myjob1.record[1], _jobs.myjob1.record[2], _jobs.myjob12.records, _jobs.myjob2.record[0], _jobs.myjob2.record[1], _jobs.myjob2.record[2], _jobs.myjob2.record[3]);
            if (_jobs.myjob12.records == 0)
                _jobs.myjob12.records = 1;
            else
                _jobs.myjob12.records = 0;
        }

        private void label153_Click(object sender, EventArgs e)
        {

        }

        // ★R8（第25轮）：原实现在 timer17_Tick（WinForms Timer=UI 线程）里 Thread.Sleep(job.timespace)，
        //   勾选 checkBox69 后每个周期把界面连同消息驱动的检测线程一起卡最长数秒。
        //   改为两段式：写低电平后把 Interval 临时缩短为 timespace，下一拍写高电平。
        //   ★第25轮朋友复核修正节奏论证：原版（Interval 固定 1000，tick 内 low→sleep→high）实际波形为
        //   周期≈1000ms、低=ts、高=1000−ts（handler 睡 ts 后，排队 WM_TIMER 恰好在本应触发的 t=1000 处返回，
        //   ts>1000 时高电平被立即顶掉≈0）。我第一版高拍恢复整 1000ms 会把周期拉长为 ts+1000。
        //   现高电平拍设 Max(1, 1000−ts)：周期/低/高三个量与原版一致（ts≥1000 时高=1ms≈原版被顶掉的≈0），且全程不阻塞任何线程。
        private int _timer17Phase; // 0=写低电平 1=待写高电平 2=待写低电平
        private int _timer17LowMs; // 本次低电平持续时长（高电平拍据此把周期补回 1000ms）
        private const int Timer17BaseIntervalMs = 1000; // 与 Designer timer17.Interval 一致

        private void timer17_Tick(object sender, EventArgs e)
        {
            if (_jobs.yunxing)
            {
                // 与原版一致：运行中整段跳过（不写输出），并复位相位/间隔
                _timer17Phase = 0;
                timer17.Interval = Timer17BaseIntervalMs;
                return;
            }
            var job = _jobs.myjob1;
            if (_timer17Phase == 1)
            {
                // 高电平拍：写回 1/1 对；间隔补回"原周期剩余"，使 低=ts、高=1000−ts、周期≈1000ms 与原版一致
                lock (job.locker_ok)
                {
                    job.outputok2 = 1;
                    job.outputok = 1;
                }
                lock (job.locker_ng)
                {
                    job.outputng2 = 1;
                    job.outputng = 1;
                }
                _timer17Phase = 2;
                timer17.Interval = Math.Max(1, Timer17BaseIntervalMs - _timer17LowMs);
            }
            else
            {
                // 低电平拍（首拍或高电平后的下一拍）：写 -1/0 对，下一拍 timespace 后写高
                lock (job.locker_ok)
                {
                    job.outputok2 = -1;
                    job.outputok = 0;
                }
                lock (job.locker_ng)
                {
                    job.outputng2 = -1;
                    job.outputng = 0;
                }
                _timer17Phase = 1;
                int ts = job.timespace;
                if (ts < 1) ts = 1;
                if (ts > 65535) ts = 65535; // WinForms Timer 上限保护
                _timer17LowMs = ts;
                timer17.Interval = ts;
            }
        }

        private void checkBox69_CheckedChanged(object sender, EventArgs e)
        {
            // ★R8：重新勾选从"低电平拍"干净起步；解除勾选时同步恢复设计间隔（防上次停在缩短态）
            _timer17Phase = 0;
            try { timer17.Interval = Timer17BaseIntervalMs; } catch { }
            if (checkBox69.CheckState == CheckState.Checked)
            {
                timer17.Enabled = true;
            }
            else
            {
                timer17.Enabled = false;
                // ★同 timer17_Tick：成对加锁，避免与检测线程输出写竞争
                var job = _jobs.myjob1;
                lock (job.locker_ok)
                {
                    job.outputok2 = -1;
                    job.outputok = 0;
                }
                lock (job.locker_ng)
                {
                    job.outputng2 = -1;
                    job.outputng = 0;
                }
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
            var panel = sender as TableLayoutPanel;
            if (panel == null) return;
            using (Pen pen = new Pen(UiBorder, 1f))
            {
                foreach (Control child in panel.Controls)
                {
                    if (!child.Visible) continue;
                    Rectangle r = child.Bounds;
                    r.Width -= 1;
                    r.Height -= 1;
                    e.Graphics.DrawRectangle(pen, r);
                }
            }
        }

        private void c12_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob1.list_block[1]);
        }
        private void ccdshow(CogToolBlock block_temp)
        {
            Form10 frm10 = new Form10(block_temp);
            frm10.Show();
        }

        private void c13_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob1.list_block[2]);
        }

        private void c14_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob1.list_block[3]);
        }

        private void c15_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob1.list_block[4]);
        }

        private void checkBox70_CheckedChanged(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
            if (checkBox70.CheckState == CheckState.Checked)
            {
                gongjujilu = 1;
                this.tableLayoutPanel11.Visible = true;
                this.tableLayoutPanel4.Visible = true;
                this.tableLayoutPanel6.Visible = true;
                this.tableLayoutPanel8.Visible = true;
                this.tableLayoutPanel10.Visible = true;
                this.tableLayoutPanel13.Visible = true;
                this.tableLayoutPanel15.Visible = true;
                this.tableLayoutPanel17.Visible = true;
                this.tableLayoutPanel19.Visible = true;
                this.tableLayoutPanel21.Visible = true;
                this.tableLayoutPanel23.Visible = true;
                this.tableLayoutPanel25.Visible = true;
                this.tableLayoutPanel2.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel2.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel3.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel3.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel5.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel5.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel7.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel7.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel9.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel9.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel12.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel12.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel14.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel14.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel16.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel16.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel18.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel18.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel20.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel20.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel22.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel22.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
                this.tableLayoutPanel24.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 90f));
                this.tableLayoutPanel24.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 10f));
            }
            else
            {
                gongjujilu = 0;
                this.tableLayoutPanel11.Visible = false;
                this.tableLayoutPanel4.Visible = false;
                this.tableLayoutPanel6.Visible = false;
                this.tableLayoutPanel8.Visible = false;
                this.tableLayoutPanel10.Visible = false;
                this.tableLayoutPanel13.Visible = false;
                this.tableLayoutPanel15.Visible = false;
                this.tableLayoutPanel17.Visible = false;
                this.tableLayoutPanel19.Visible = false;
                this.tableLayoutPanel21.Visible = false;
                this.tableLayoutPanel23.Visible = false;
                this.tableLayoutPanel25.Visible = false;
                this.tableLayoutPanel2.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel2.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel3.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel3.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel5.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel5.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel7.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel7.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel9.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel9.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel12.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel12.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel14.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel14.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel16.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel16.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel18.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel18.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel20.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel20.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel22.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel22.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
                this.tableLayoutPanel24.ColumnStyles[0] = (new ColumnStyle(SizeType.Percent, 99f));
                this.tableLayoutPanel24.ColumnStyles[1] = (new ColumnStyle(SizeType.Percent, 1f));
            }
            }));
        }

        private void c22_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob2.list_block[1]);
        }

        private void c23_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob2.list_block[2]);
        }

        private void c24_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob2.list_block[3]);
        }

        private void c25_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob2.list_block[4]);
        }

        private void c32_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob3.list_block[1]);
        }

        private void c33_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob3.list_block[2]);
        }

        private void c34_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob3.list_block[3]);
        }

        private void c35_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob3.list_block[4]);
        }

        private void c42_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob3.list_block[1]);
            }
            else
            {
                ccdshow(_jobs.myjob4.list_block[1]);
            }
        }

        private void c43_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob3.list_block[2]);
            }
            else
            {
                ccdshow(_jobs.myjob4.list_block[2]);
            }
        }

        private void c44_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob3.list_block[3]);
            }
            else
            {
                ccdshow(_jobs.myjob4.list_block[3]);
            }
        }

        private void c45_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob3.list_block[4]);
            }
            else
            {
                ccdshow(_jobs.myjob4.list_block[4]);
            }
        }

        private void c52_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob4.list_block[1]);
            }
            else
            {
                ccdshow(_jobs.myjob5.list_block[1]);
            }
        }

        private void c53_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob4.list_block[2]);
            }
            else
            {
                ccdshow(_jobs.myjob5.list_block[2]);
            }
        }

        private void c54_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob4.list_block[3]);
            }
            else
            {
                ccdshow(_jobs.myjob5.list_block[3]);
            }
        }

        private void c55_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ccdshow(_jobs.myjob4.list_block[4]);
            }
            else
            {
                ccdshow(_jobs.myjob5.list_block[4]);
            }
        }

        private void c62_Click(object sender, EventArgs e)
        {

            ccdshow(_jobs.myjob6.list_block[1]);
        }

        private void c63_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob6.list_block[2]);
        }

        private void c64_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob6.list_block[3]);
        }

        private void c65_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob6.list_block[4]);
        }

        private void c72_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob7.list_block[1]);
        }

        private void c73_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob7.list_block[2]);
        }

        private void c74_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob7.list_block[3]);
        }

        private void c75_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob7.list_block[4]);
        }

        private void c82_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob8.list_block[1]);
        }

        private void c83_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob8.list_block[2]);
        }

        private void c84_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob8.list_block[3]);
        }

        private void c85_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob8.list_block[4]);
        }

        // Camera 9 side panel button handlers
        private void button118_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob9);
            frm6[frm6.Count - 1].Show();
        }

        private void button117_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob9.list_block[1]);
        }

        private void button116_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob9.list_block[2]);
        }

        private void button115_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob9.list_block[3]);
        }

        private void button114_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob9.list_block[4]);
        }

        // Camera 10 side panel button handlers
        private void button123_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob10);
            frm6[frm6.Count - 1].Show();
        }

        private void button122_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob10.list_block[1]);
        }

        private void button121_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob10.list_block[2]);
        }

        private void button120_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob10.list_block[3]);
        }

        private void button119_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob10.list_block[4]);
        }

        // Camera 11 side panel button handlers
        private void button128_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob11);
            frm6[frm6.Count - 1].Show();
        }

        private void button127_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob11.list_block[1]);
        }

        private void button126_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob11.list_block[2]);
        }

        private void button125_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob11.list_block[3]);
        }

        private void button124_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob11.list_block[4]);
        }

        // Camera 12 side panel button handlers
        private void button133_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob12);
            frm6[frm6.Count - 1].Show();
        }

        private void button132_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob12.list_block[1]);
        }

        private void button131_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob12.list_block[2]);
        }

        private void button130_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob12.list_block[3]);
        }

        private void button129_Click(object sender, EventArgs e)
        {
            ccdshow(_jobs.myjob12.list_block[4]);
        }

        List<Form6> frm6 = new List<Form6>();

        // ★修复(frm6 只增不查重)：原实现每次点击都向列表 Add 一个新 Form6——同一相机重复点开
        //   会层层叠窗、旧窗在列表中失联，已关闭窗口也残留引用。现以 Tag 记属主 job：
        //   重开同一相机先关旧窗，并顺带清掉已 Dispose 的条目。
        //   (调用点保留的 frm6[Count-1].Show() 作用于刚建的新窗，幂等无害。)
        private void ShowForm6For(Myjob job)
        {
            if (job == null) return;
            for (int i = frm6.Count - 1; i >= 0; i--)
            {
                Form6 f = frm6[i];
                if (f == null || f.IsDisposed) { frm6.RemoveAt(i); continue; }
                if (ReferenceEquals(f.Tag, job))
                {
                    try { f.Close(); } catch { }
                    frm6.RemoveAt(i);
                }
            }
            Form6 nf = new Form6(job.block);
            nf.Tag = job;
            frm6.Add(nf);
            nf.Show();
        }
        private void c11_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob1);
            frm6[frm6.Count - 1].Show();
        }

        private void c21_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob2);
            frm6[frm6.Count - 1].Show();
        }

        private void c31_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob3);
            frm6[frm6.Count - 1].Show();
        }

        private void c41_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ShowForm6For(_jobs.myjob3);
                frm6[frm6.Count - 1].Show();
            }
            else
            {
                ShowForm6For(_jobs.myjob4);
                frm6[frm6.Count - 1].Show();
            }
        }

        private void c51_Click(object sender, EventArgs e)
        {
            if (tableLayoutPanel5.Visible == false)
            {
                ShowForm6For(_jobs.myjob4);
                frm6[frm6.Count - 1].Show();
            }
            else
            {
                ShowForm6For(_jobs.myjob5);
                frm6[frm6.Count - 1].Show();
            }
        }

        private void c61_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob6);
            frm6[frm6.Count - 1].Show();
        }

        private void c71_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob7);
            frm6[frm6.Count - 1].Show();
        }

        private void c81_Click(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob8);
            frm6[frm6.Count - 1].Show();
        }

        private void button76_Click(object sender, EventArgs e)
        {
            string canshu = textBox5.Text.ToString().Trim();
            int cc = 0;
            if (canshu.Length > 0)
            {
                if (comboBox38.Items.Count == 0)
                {
                    comboBox38.Items.Add(canshu);
                    MessageBox.Show("参数添加成功");
                }
                else
                {
                    for (int i = 0; i < comboBox38.Items.Count; i++)
                    {
                        if (comboBox38.Items[i].ToString() == canshu)
                        {
                            MessageBox.Show("参数名重复");
                            cc = 1;
                            return;
                        }
                    }
                    if (cc == 0)
                    {
                        comboBox38.Items.Add(canshu);
                        MessageBox.Show("参数添加成功");
                    }
                }


            }
            else
            {
                MessageBox.Show("请输入参数名");
            }

        }

        private void comboBox38_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox38_TextChanged(object sender, EventArgs e)
        {
            string canshu = comboBox38.Text.Trim();
            if (canshu.Length > 0)
            {
                for (int i = 0; i < manager1.JobCount; i++)
                {
                    switch (i)
                    {
                        case 0:
                            for (int j = 0; j < _jobs.myjob1.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob1.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob1.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 1:
                            for (int j = 0; j < _jobs.myjob2.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob2.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob2.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 2:
                            for (int j = 0; j < _jobs.myjob3.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob3.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob3.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 3:
                            for (int j = 0; j < _jobs.myjob4.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob4.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob4.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 4:
                            for (int j = 0; j < _jobs.myjob5.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob5.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob5.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 5:
                            for (int j = 0; j < _jobs.myjob6.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob6.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob6.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 6:
                            for (int j = 0; j < _jobs.myjob7.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob7.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob7.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 7:
                            for (int j = 0; j < _jobs.myjob8.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob8.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob8.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 8:
                            for (int j = 0; j < _jobs.myjob9.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob9.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob9.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 9:
                            for (int j = 0; j < _jobs.myjob10.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob10.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob10.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 10:
                            for (int j = 0; j < _jobs.myjob11.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob11.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob11.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                        case 11:
                            for (int j = 0; j < _jobs.myjob12.block.Inputs.Count; j++)
                            {
                                if (_jobs.myjob12.block.Inputs[j].Name.Contains("canshu"))
                                {
                                    _jobs.myjob12.block.Inputs["canshu"].Value = canshu;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void button77_Click(object sender, EventArgs e)
        {
            comboBox38.Items.Clear();
            comboBox38.Text = "";
        }

        private void checkBox71_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox71.CheckState == CheckState.Checked)
                NG = true;
            else
                NG = false;
        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void comboBox39_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox40_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown9.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[0])
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
                    camera_name[0] = name_temp;
                    _config.WriteString("camera", "name1", name_temp);
                }
                else
                {
                    numericUpDown9.Value = int.Parse(camera_name[0]);
                }
            }
        }

        private void numericUpDown10_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown10.Value.ToString();
            int chongming_temp = 0;
            if (name_temp!= camera_name[1])
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
                    camera_name[1] = name_temp;
                    _config.WriteString("camera", "name2", name_temp);
                }
                else
                {
                    numericUpDown10.Value = int.Parse(camera_name[1]);
                }
            }
        }

        private void numericUpDown11_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown11.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[2])
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
                    camera_name[2] = name_temp;
                    _config.WriteString("camera", "name3", name_temp);
                }
                else
                {
                    numericUpDown11.Value = int.Parse(camera_name[2]);
                }
            }
        }

        private void numericUpDown12_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown12.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[3])
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
                    camera_name[3] = name_temp;
                    _config.WriteString("camera", "name4", name_temp);
                }
                else
                {
                    numericUpDown12.Value = int.Parse(camera_name[3]);
                }
            }
        }

        private void numericUpDown13_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown13.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[4])
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
                    camera_name[4] = name_temp;
                    _config.WriteString("camera", "name5", name_temp);
                }
                else
                {
                    numericUpDown13.Value = int.Parse(camera_name[4]);
                }
            }
        }

        private void numericUpDown14_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown14.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[5])
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
                    camera_name[5] = name_temp;
                    _config.WriteString("camera", "name6", name_temp);
                }
                else
                {
                    numericUpDown14.Value = int.Parse(camera_name[5]);
                }
            }
        }

        private void numericUpDown15_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown15.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[6])
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
                    camera_name[6] = name_temp;
                    _config.WriteString("camera", "name7", name_temp);
                }
                else
                {
                    numericUpDown15.Value = int.Parse(camera_name[6]);
                }
            }
        }

        private void numericUpDown16_ValueChanged(object sender, EventArgs e)
        {
            string name_temp = numericUpDown16.Value.ToString();
            int chongming_temp = 0;
            if (name_temp != camera_name[7])
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
                    camera_name[7] = name_temp;
                    _config.WriteString("camera", "name8", name_temp);
                }
                else
                {
                    numericUpDown16.Value = int.Parse(camera_name[7]);
                }
            }
        }

        private void 显示备注ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if(textBox7.Visible==false)
            {
                textBox7.Visible = true;
                label172.Visible = true;
            }
            else
            {
                textBox7.Visible = false;
                label172.Visible = false;
            }
        }

        /// <summary>
        /// 【查找】→【版本信息】：显示程序版本号与编译时间，便于现场记录/追溯。
        /// 菜单项为设计器原生项（Form1.Designer.cs 中"版本信息ToolStripMenuItem"），可直接在 VS 设计器中编辑。
        /// </summary>
        private void 版本信息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                // ★版本规则（现场定，2026-09-20）：版本号 = 构建日期 + 推送号（git 短哈希），
                //   由构建时自动写入 BuildInfo（tools/gen-buildinfo.ps1，csproj GenerateBuildInfo 目标）。
                //   正常构建后形如 "2026.09.20+18f0607"；BuildInfo 缺失（非常规构建）时回退程序集版本。
                string ver = BuildInfo.Version;
                if (string.IsNullOrEmpty(ver))
                {
                    ver = Application.ProductVersion;
                    if (string.IsNullOrEmpty(ver) && asm.GetName().Version != null) ver = asm.GetName().Version.ToString();
                }
                string bt = BuildInfo.BuildTime;
                if (string.IsNullOrEmpty(bt))
                {
                    try { bt = System.IO.File.GetLastWriteTime(asm.Location).ToString("yyyy-MM-dd HH:mm:ss"); }
                    catch { bt = "未知"; }
                }
                string info = string.Format(
                    "光眼视觉检测系统{0}版本：{1}{0}编译时间：{2}{0}{0}（菜单【查找】→【版本信息】）",
                    Environment.NewLine, ver ?? "未知", bt);
                MessageBox.Show(info, "版本信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                try { MessageBox.Show("读取版本信息失败：" + ex.Message, "版本信息", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch { }
            }
        }

        private void cogRecordDisplay7_Enter(object sender, EventArgs e)
        {

        }
        bool mingchen_ = false;
        private void label75_Click(object sender, EventArgs e)
        {
            if(label75.Text.Contains("N/A")||label75.Text.Contains("损坏"))
            {

            }
            else
            {
                if (mingchen_)
                {
                    mingchen_ = false;
                    label75.Text = path_1.ToString().Split('\\').Last();
                }
                else
                {
                    mingchen_ = true;
                    label75.Text = path_1.ToString();
                }
            }
        }



        #endregion
    }
}
