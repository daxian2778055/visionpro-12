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
    // Form1 partial class: camera 1-8 control event handlers.
    // Moved verbatim from Form1.cs - organizational split only, no logic change.
    public partial class Form1 : Form
    {
        #region 相机1-8 控制事件
        private void button7_Click_4(object sender, EventArgs e)
        {
            if (label75.Text.Contains("N/A") || label75.Text.Contains("损坏"))
            {
                MessageBox.Show("方案未正确加载,不能执行保存参数操作!");
            }
            {
                if (!comboBox7.Text.Contains(".vpp"))
                    _config.WriteString("camera1", "fen", " ");
                if (!comboBox9.Text.Contains(".vpp"))
                    _config.WriteString("camera2", "fen", " ");
                if (!comboBox10.Text.Contains(".vpp"))
                    _config.WriteString("camera3", "fen", " ");
                if (!comboBox11.Text.Contains(".vpp"))
                    _config.WriteString("camera4", "fen", " ");
                if (!comboBox12.Text.Contains(".vpp"))
                    _config.WriteString("camera5", "fen", " ");
                if (!comboBox13.Text.Contains(".vpp"))
                    _config.WriteString("camera6", "fen", " ");
                if (!comboBox14.Text.Contains(".vpp"))
                    _config.WriteString("camera7", "fen", " ");
                if (!comboBox15.Text.Contains(".vpp"))
                    _config.WriteString("camera8", "fen", " ");
                if (!comboBox42.Text.Contains(".vpp"))
                    _config.WriteString("camera9", "fen", " ");
                if (!comboBox46.Text.Contains(".vpp"))
                    _config.WriteString("camera10", "fen", " ");
                if (!comboBox50.Text.Contains(".vpp"))
                    _config.WriteString("camera11", "fen", " ");
                if (!comboBox54.Text.Contains(".vpp"))
                    _config.WriteString("camera12", "fen", " ");
                _config.WriteString("time", "IOyanshi", numericUpDown5.Value.ToString());
                _config.WriteString("time", "feng", numericUpDown22.Value.ToString());
                if (comboBox21.SelectedIndex == 0)
                    _config.WriteString("camera", "cuntu", "存图限制");
                else
                    _config.WriteString("camera", "cuntu", "存图释放");
                if (comboBox22.SelectedIndex == 0)
                    _config.WriteString("camera", "tongji", "存图限制");
                else
                    _config.WriteString("camera", "tongji", "存图释放");
                _config.WriteString("camera", "qufan", button31.Text);
                _config.WriteString("camera", "yanshi", numericUpDown1.Value.ToString());
                _config.WriteString("cuntu", "zhangshu", numericUpDown4.Value.ToString());
                if (checkBox3.CheckState == CheckState.Checked)
                    _config.WriteString("camera1", "shijianEn", "true");
                else
                    _config.WriteString("camera1", "shijianEn", "false");
                if (checkBox13.CheckState == CheckState.Checked)
                    _config.WriteString("camera2", "shijianEn", "true");
                else
                    _config.WriteString("camera2", "shijianEn", "false");
                if (checkBox18.CheckState == CheckState.Checked)
                    _config.WriteString("camera3", "shijianEn", "true");
                else
                    _config.WriteString("camera3", "shijianEn", "false");
                if (checkBox22.CheckState == CheckState.Checked)
                    _config.WriteString("camera4", "shijianEn", "true");
                else
                    _config.WriteString("camera4", "shijianEn", "false");
                if (checkBox36.CheckState == CheckState.Checked)
                    _config.WriteString("camera5", "shijianEn", "true");
                else
                    _config.WriteString("camera5", "shijianEn", "false");
                if (checkBox42.CheckState == CheckState.Checked)
                    _config.WriteString("camera6", "shijianEn", "true");
                else
                    _config.WriteString("camera6", "shijianEn", "false");
                if (checkBox48.CheckState == CheckState.Checked)
                    _config.WriteString("camera7", "shijianEn", "true");
                else
                    _config.WriteString("camera7", "shijianEn", "false");
                if (checkBox54.CheckState == CheckState.Checked)
                    _config.WriteString("camera8", "shijianEn", "true");
                else
                    _config.WriteString("camera8", "shijianEn", "false");
                if (checkBox77.CheckState == CheckState.Checked)
                    _config.WriteString("camera9", "shijianEn", "true");
                else
                    _config.WriteString("camera9", "shijianEn", "false");
                if (checkBox83.CheckState == CheckState.Checked)
                    _config.WriteString("camera10", "shijianEn", "true");
                else
                    _config.WriteString("camera10", "shijianEn", "false");
                if (checkBox89.CheckState == CheckState.Checked)
                    _config.WriteString("camera11", "shijianEn", "true");
                else
                    _config.WriteString("camera11", "shijianEn", "false");
                if (checkBox95.CheckState == CheckState.Checked)
                    _config.WriteString("camera12", "shijianEn", "true");
                else
                    _config.WriteString("camera12", "shijianEn", "false");
                if (checkBox27.CheckState == CheckState.Checked)
                    _config.WriteString("camera1", "serial", "true");
                else
                    _config.WriteString("camera1", "serial", "false");
                if (checkBox28.CheckState == CheckState.Checked)
                    _config.WriteString("camera2", "serial", "true");
                else
                    _config.WriteString("camera2", "serial", "false");
                if (checkBox29.CheckState == CheckState.Checked)
                    _config.WriteString("camera3", "serial", "true");
                else
                    _config.WriteString("camera3", "serial", "false");
                if (checkBox30.CheckState == CheckState.Checked)
                    _config.WriteString("camera4", "serial", "true");
                else
                    _config.WriteString("camera4", "serial", "false");
                if (checkBox31.CheckState == CheckState.Checked)
                    _config.WriteString("camera5", "serial", "true");
                else
                    _config.WriteString("camera5", "serial", "false");
                if (checkBox37.CheckState == CheckState.Checked)
                    _config.WriteString("camera6", "serial", "true");
                else
                    _config.WriteString("camera6", "serial", "false");
                if (checkBox43.CheckState == CheckState.Checked)
                    _config.WriteString("camera7", "serial", "true");
                else
                    _config.WriteString("camera7", "serial", "false");
                if (checkBox49.CheckState == CheckState.Checked)
                    _config.WriteString("camera8", "serial", "true");
                else
                    _config.WriteString("camera8", "serial", "false");
                if (checkBox74.CheckState == CheckState.Checked)
                    _config.WriteString("camera9", "serial", "true");
                else
                    _config.WriteString("camera9", "serial", "false");
                if (checkBox80.CheckState == CheckState.Checked)
                    _config.WriteString("camera10", "serial", "true");
                else
                    _config.WriteString("camera10", "serial", "false");
                if (checkBox86.CheckState == CheckState.Checked)
                    _config.WriteString("camera11", "serial", "true");
                else
                    _config.WriteString("camera11", "serial", "false");
                if (checkBox92.CheckState == CheckState.Checked)
                    _config.WriteString("camera12", "serial", "true");
                else
                    _config.WriteString("camera12", "serial", "false");
                _config.WriteString("path", "path_1", path_1);
                _config.WriteString("camera1", "triggermode", comboBox1.Text);
                _config.WriteString("camera1", "exposure", tbExposure1.Text);
                _config.WriteString("camera1", "gain", tbGain1.Text);
                _config.WriteString("camera1", "rate", tbFrameRate1.Text);
                _config.WriteString("camera1", "outtime", textBox3.Text);

                if (checkBox25.CheckState == CheckState.Checked)
                    _config.WriteString("camera", "datajilu", "true");
                else
                    _config.WriteString("camera", "datajilu", "false");
                if (checkBox70.CheckState == CheckState.Checked)
                    _config.WriteString("camera", "gongjujilu", "true");
                else
                    _config.WriteString("camera", "gongjujilu", "false");
                if (checkBox5.CheckState == CheckState.Checked)
                    _config.WriteString("camera1", "triggeren", "true");
                else
                    _config.WriteString("camera1", "triggeren", "false");


                if (checkBox1.CheckState == CheckState.Checked)
                    _config.WriteString("camera1", "1", "true");
                else
                    _config.WriteString("camera1", "1", "false");
                if (checkBox2.CheckState == CheckState.Checked)
                    _config.WriteString("camera1", "2", "true");
                else
                    _config.WriteString("camera1", "2", "false");
                _config.WriteString("camera2", "triggermode", comboBox4.Text);
                _config.WriteString("camera2", "exposure", tbExposure2.Text);
                _config.WriteString("camera2", "gain", tbGain2.Text);
                _config.WriteString("camera2", "rate", tbFrameRate2.Text);

                if (checkBox9.CheckState == CheckState.Checked)
                    _config.WriteString("camera2", "triggeren", "true");
                else
                    _config.WriteString("camera2", "triggeren", "false");

                _config.WriteString("camera3", "triggermode", comboBox5.Text);
                _config.WriteString("camera3", "exposure", tbExposure3.Text);
                _config.WriteString("camera3", "gain", tbGain3.Text);
                _config.WriteString("camera3", "rate", tbFrameRate3.Text);
                if (checkBox15.CheckState == CheckState.Checked)
                    _config.WriteString("camera3", "triggeren", "true");
                else
                    _config.WriteString("camera3", "triggeren", "false");


                _config.WriteString("camera4", "triggermode", comboBox8.Text);
                _config.WriteString("camera4", "exposure", tbExposure4.Text);
                _config.WriteString("camera4", "gain", tbGain4.Text);
                _config.WriteString("camera4", "rate", tbFrameRate4.Text);
                if (checkBox19.CheckState == CheckState.Checked)
                    _config.WriteString("camera4", "triggeren", "true");
                else
                    _config.WriteString("camera4", "triggeren", "false");

                if (checkBox7.CheckState == CheckState.Checked)
                    _config.WriteString("camera1", "chatu", "true");
                else
                    _config.WriteString("camera1", "chatu", "false");
                if (checkBox71.CheckState == CheckState.Checked)
                    _config.WriteString("camera", "NG", "true");
                else
                    _config.WriteString("camera", "NG", "false");

                if (checkBox6.CheckState == CheckState.Checked)
                    _config.WriteString("camera2", "chatu", "true");
                else
                    _config.WriteString("camera2", "chatu", "false");
                if (checkBox14.CheckState == CheckState.Checked)
                    _config.WriteString("camera3", "chatu", "true");
                else
                    _config.WriteString("camera3", "chatu", "false");
                if (checkBox12.CheckState == CheckState.Checked)
                    _config.WriteString("camera4", "chatu", "true");
                else
                    _config.WriteString("camera4", "chatu", "false");
                if (checkBox35.CheckState == CheckState.Checked)
                    _config.WriteString("camera5", "chatu", "true");
                else
                    _config.WriteString("camera5", "chatu", "false");
                if (checkBox41.CheckState == CheckState.Checked)
                    _config.WriteString("camera6", "chatu", "true");
                else
                    _config.WriteString("camera6", "chatu", "false");
                if (checkBox55.CheckState == CheckState.Checked)
                    _config.WriteString("camera7", "chatu", "true");
                else
                    _config.WriteString("camera7", "chatu", "false");
                if (checkBox53.CheckState == CheckState.Checked)
                    _config.WriteString("camera8", "chatu", "true");
                else
                    _config.WriteString("camera8", "chatu", "false");
                if (checkBox73.CheckState == CheckState.Checked)
                    _config.WriteString("camera9", "chatu", "true");
                else
                    _config.WriteString("camera9", "chatu", "false");
                if (checkBox79.CheckState == CheckState.Checked)
                    _config.WriteString("camera10", "chatu", "true");
                else
                    _config.WriteString("camera10", "chatu", "false");
                if (checkBox85.CheckState == CheckState.Checked)
                    _config.WriteString("camera11", "chatu", "true");
                else
                    _config.WriteString("camera11", "chatu", "false");
                if (checkBox91.CheckState == CheckState.Checked)
                    _config.WriteString("camera12", "chatu", "true");
                else
                    _config.WriteString("camera12", "chatu", "false");

                if (checkBox11.CheckState == CheckState.Checked)
                    _config.WriteString("camera1", "biaoge", "true");
                else
                    _config.WriteString("camera1", "biaoge", "false");
                if (checkBox8.CheckState == CheckState.Checked)
                    _config.WriteString("camera2", "biaoge", "true");
                else
                    _config.WriteString("camera2", "biaoge", "false");
                if (checkBox56.CheckState == CheckState.Checked)
                    _config.WriteString("camera3", "biaoge", "true");
                else
                    _config.WriteString("camera3", "biaoge", "false");
                if (checkBox57.CheckState == CheckState.Checked)
                    _config.WriteString("camera4", "biaoge", "true");
                else
                    _config.WriteString("camera4", "biaoge", "false");
                if (checkBox58.CheckState == CheckState.Checked)
                    _config.WriteString("camera5", "biaoge", "true");
                else
                    _config.WriteString("camera5", "biaoge", "false");
                if (checkBox59.CheckState == CheckState.Checked)
                    _config.WriteString("camera6", "biaoge", "true");
                else
                    _config.WriteString("camera6", "biaoge", "false");
                if (checkBox60.CheckState == CheckState.Checked)
                    _config.WriteString("camera7", "biaoge", "true");
                else
                    _config.WriteString("camera7", "biaoge", "false");
                if (checkBox61.CheckState == CheckState.Checked)
                    _config.WriteString("camera8", "biaoge", "true");
                else
                    _config.WriteString("camera8", "biaoge", "false");
                if (checkBox73.CheckState == CheckState.Checked)
                    _config.WriteString("camera9", "biaoge", "true");
                else
                    _config.WriteString("camera9", "biaoge", "false");
                if (checkBox79.CheckState == CheckState.Checked)
                    _config.WriteString("camera10", "biaoge", "true");
                else
                    _config.WriteString("camera10", "biaoge", "false");
                if (checkBox85.CheckState == CheckState.Checked)
                    _config.WriteString("camera11", "biaoge", "true");
                else
                    _config.WriteString("camera11", "biaoge", "false");
                if (checkBox91.CheckState == CheckState.Checked)
                    _config.WriteString("camera12", "biaoge", "true");
                else
                    _config.WriteString("camera12", "biaoge", "false");

                if (checkBox47.CheckState == CheckState.Checked)
                    _config.WriteString("camera9", "jiankong", "true");
                else
                    _config.WriteString("camera9", "jiankong", "false");
                if (checkBox96.CheckState == CheckState.Checked)
                    _config.WriteString("camera10", "jiankong", "true");
                else
                    _config.WriteString("camera10", "jiankong", "false");
                if (checkBox97.CheckState == CheckState.Checked)
                    _config.WriteString("camera11", "jiankong", "true");
                else
                    _config.WriteString("camera11", "jiankong", "false");
                if (checkBox98.CheckState == CheckState.Checked)
                    _config.WriteString("camera12", "jiankong", "true");
                else
                    _config.WriteString("camera12", "jiankong", "false");

                _config.WriteString("camera5", "triggermode", comboBox25.Text);
                _config.WriteString("camera5", "exposure", tbExposure5.Text);
                _config.WriteString("camera5", "gain", tbGain5.Text);
                _config.WriteString("camera5", "rate", tbFrameRate5.Text);
                if (checkBox33.CheckState == CheckState.Checked)
                    _config.WriteString("camera5", "triggeren", "true");
                else
                    _config.WriteString("camera5", "triggeren", "false");


                _config.WriteString("camera6", "triggermode", comboBox28.Text);
                _config.WriteString("camera6", "exposure", tbExposure6.Text);
                _config.WriteString("camera6", "gain", tbGain6.Text);
                _config.WriteString("camera6", "rate", tbFrameRate6.Text);
                if (checkBox39.CheckState == CheckState.Checked)
                    _config.WriteString("camera6", "triggeren", "true");
                else
                    _config.WriteString("camera6", "triggeren", "false");



                _config.WriteString("camera7", "triggermode", comboBox31.Text);
                _config.WriteString("camera7", "exposure", tbExposure7.Text);
                _config.WriteString("camera7", "gain", tbGain7.Text);
                _config.WriteString("camera7", "rate", tbFrameRate7.Text);
                if (checkBox45.CheckState == CheckState.Checked)
                    _config.WriteString("camera7", "triggeren", "true");
                else
                    _config.WriteString("camera7", "triggeren", "false");


                _config.WriteString("camera8", "triggermode", comboBox34.Text);
                _config.WriteString("camera8", "exposure", tbExposure8.Text);
                _config.WriteString("camera8", "gain", tbGain8.Text);
                _config.WriteString("camera8", "rate", tbFrameRate8.Text);
                if (checkBox51.CheckState == CheckState.Checked)
                    _config.WriteString("camera8", "triggeren", "true");
                else
                    _config.WriteString("camera8", "triggeren", "false");

                _config.WriteString("camera9", "triggermode", comboBox43.Text);
                _config.WriteString("camera9", "exposure", tbExposure9.Text);
                _config.WriteString("camera9", "gain", tbGain9.Text);
                _config.WriteString("camera9", "rate", tbFrameRate9.Text);
                if (checkBox75.CheckState == CheckState.Checked)
                    _config.WriteString("camera9", "triggeren", "true");
                else
                    _config.WriteString("camera9", "triggeren", "false");

                _config.WriteString("camera10", "triggermode", comboBox47.Text);
                _config.WriteString("camera10", "exposure", tbExposure10.Text);
                _config.WriteString("camera10", "gain", tbGain10.Text);
                _config.WriteString("camera10", "rate", tbFrameRate10.Text);
                if (checkBox81.CheckState == CheckState.Checked)
                    _config.WriteString("camera10", "triggeren", "true");
                else
                    _config.WriteString("camera10", "triggeren", "false");

                _config.WriteString("camera11", "triggermode", comboBox51.Text);
                _config.WriteString("camera11", "exposure", tbExposure11.Text);
                _config.WriteString("camera11", "gain", tbGain11.Text);
                _config.WriteString("camera11", "rate", tbFrameRate11.Text);
                if (checkBox87.CheckState == CheckState.Checked)
                    _config.WriteString("camera11", "triggeren", "true");
                else
                    _config.WriteString("camera11", "triggeren", "false");

                _config.WriteString("camera12", "triggermode", comboBox55.Text);
                _config.WriteString("camera12", "exposure", tbExposure12.Text);
                _config.WriteString("camera12", "gain", tbGain12.Text);
                _config.WriteString("camera12", "rate", tbFrameRate12.Text);
                if (checkBox93.CheckState == CheckState.Checked)
                    _config.WriteString("camera12", "triggeren", "true");
                else
                    _config.WriteString("camera12", "triggeren", "false");

                _config.WriteString("zhendongpan", "path", textBox19.Text);
                int geshu = comboBox38.Items.Count;
                _config.WriteString("canshu", "geshu", geshu.ToString());
                if (geshu > 0)
                {
                    for (int i = 0; i < geshu; i++)
                    {
                        _config.WriteString("canshu", (i + 1).ToString(), comboBox38.Items[i].ToString());
                    }
                    _config.WriteString("canshu", "xuanze", comboBox38.Text);
                }
            }

        }

        private void bnGetLineSel_Click(object sender, EventArgs e)
        {
            get_Selector(0, cbLineSel1);
        }

        private void bnSetLineSel_Click(object sender, EventArgs e)
        {
            set_Selector(0, cbLineSel1);
        }

        private void bnGetLineMode_Click(object sender, EventArgs e)
        {
            get_Mode(0, cbLineMode1);
        }

        private void bnSetLineMode_Click(object sender, EventArgs e)
        {
            set_Mode(0, cbLineMode1);
        }

        private void checkBox4_CheckedChanged_1(object sender, EventArgs e)
        {
            output_inverse(0, cbLineMode1, checkBox4);

        }

        private void button24_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox10, listBox5);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox5.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    SetReplayImg(_jobs.myjob2, item.filePath);
                    if (_jobs.myjob2.trriger == 0)
                    {
                        _jobs.myjob2.trriger = 1;
                        getrecord(_jobs.myjob2, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger2_temp = 1;
                    timer8.Interval = int.Parse(textBox9.Text);
                    timer8.Enabled = true;
                }
            }
            //img = new Bitmap(item.filePath);
            // _jobs.myjob1.trriger = 1;
        }

        private void button23_Click(object sender, EventArgs e)
        {
            trriger2_temp = 0;
            timer8.Enabled = false;
        }

        private void comboBox4_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob2.state.Contains("相"))
            {
                try
                {
                    if (comboBox4.Text == "连续运行")
                    {
                        _cameraCtrl.Cameras[1].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger2.Enabled = false;
                        bnTriggerExec2.Enabled = false;
                    }
                    else if (comboBox4.Text == "触发拍照" || comboBox4.Text == "通讯触发")
                    {

                        _cameraCtrl.Cameras[1].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger2.Checked || comboBox4.Text == "通讯触发")
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
                    _logger.WriteLog(ex.Message + "触发切换2");
                }
                _jobs.myjob2.triggerMode = comboBox4.Text;
            }
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(1, cbLineMode2, checkBox10);
        }

        private void listBox5_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox5.SelectedItem;
                if (item == null) return;
                if (_jobs.yunxing == false)
                {
                    // ★运行中不覆盖回图：上一次 trriger==1 未消费完时 img 仍被 timer 重试使用
                    SetReplayImg(_jobs.myjob2, item.filePath);
                    if (_jobs.myjob2.trriger == 0)
                    {
                        _jobs.myjob2.trriger = 1;
                        getrecord(_jobs.myjob2, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void bnGetParam2_Click(object sender, EventArgs e)
        {
            GetParamFor(1);

        }

        private void bnSetParam2_Click(object sender, EventArgs e)
        {
            SetParamFor(1);

        }

        private void bnStartGrab2_Click(object sender, EventArgs e)
        {
            StartGrabFor(1);

        }

        private void bnStopGrab2_Click(object sender, EventArgs e)
        {
            StopGrabFor(1);

        }

        private void cbSoftTrigger2_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(1);

        }

        private void bnTriggerExec2_Click(object sender, EventArgs e)
        {
            TriggerExecFor(1);

        }

        private void bnGetLineMode2_Click(object sender, EventArgs e)
        {
            get_Mode(1, cbLineMode2);
        }

        private void bnSetLineMode2_Click(object sender, EventArgs e)
        {
            set_Mode(1, cbLineMode2);
        }

        private void bnGetLineSel2_Click(object sender, EventArgs e)
        {
            get_Selector(1, cbLineSel2);
        }

        private void bnSetLineSel2_Click(object sender, EventArgs e)
        {
            set_Selector(1, cbLineSel2);
        }

        private void button19_Click_1(object sender, EventArgs e)
        {

        }

        private void 配置相机2ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob2);
            frm6[frm6.Count - 1].Show();
        }

        private void listBox3_MouseDown_1(object sender, MouseEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    string[] time111 = listBox3.SelectedItem.ToString().Split(':');
                    int ttt1 = int.Parse(time111[0] + time111[1] + time111[2]);
                    string ttt2 = time111[3];
                    int ttt3 = int.Parse(time111[4]);
                    Process myProc = null;
                    myProc = Process.Start(_jobs.myjob2.pathhead_ng + day1 + "\\" + ttt1 + ttt2 + "#" + ttt3 + ".bmp");//开启一个进程
                    try
                    {
                        myProc.Kill();//关闭一个进程
                    }
                    catch { }
                }
                catch (Exception ex)
                { _logger.WriteLog(ex.Message + "图片显示2"); };
            });
        }

        private void button26_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox12, listBox8);
        }

        private void button38_Click(object sender, EventArgs e)
        {
            ShowPictureList(textBox17, listBox9);
        }

        private void button27_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox8.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    SetReplayImg(_jobs.myjob3, item.filePath);
                    if (_jobs.myjob3.trriger == 0)
                    {
                        _jobs.myjob3.trriger = 1;
                        getrecord(_jobs.myjob3, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger3_temp = 1;
                    timer11.Interval = int.Parse(textBox11.Text);
                    timer11.Enabled = true;
                }
            }
        }

        private void button39_Click(object sender, EventArgs e)
        {
            if (_jobs.yunxing == false)
            {
                PictureListItem item = (PictureListItem)listBox9.SelectedItem;
                if (item == null)
                {
                    MessageBox.Show("请选择图片");
                    return;
                }
                else
                {
                    SetReplayImg(_jobs.myjob4, item.filePath);
                    if (_jobs.myjob4.trriger == 0)
                    {
                        _jobs.myjob4.trriger = 1;
                        getrecord(_jobs.myjob4, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                    trriger4_temp = 1;
                    timer12.Interval = int.Parse(textBox16.Text);
                    timer12.Enabled = true;
                }
            }
        }

        private void timer12_Tick(object sender, EventArgs e)
        {
            if (_jobs.myjob4.trriger == 0)
            {
                if (trriger4_temp == 1)
                {
                    int count = listBox9.Items.Count;
                    int select = listBox9.SelectedIndex;
                    this.Invoke(new Action(() =>
                    {
                        if (select < count - 1)
                        {
                            listBox9.SelectedIndex = select + 1;
                        }
                        else
                            listBox9.SelectedIndex = 0;
                    }));
                }

            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            trriger3_temp = 0;
            timer11.Enabled = false;
        }

        private void button37_Click(object sender, EventArgs e)
        {
            trriger4_temp = 0;
            timer12.Enabled = false;
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob3.state.Contains("相"))
            {
                try
                {
                    if (comboBox5.Text == "连续运行")
                    {
                        _cameraCtrl.Cameras[2].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger3.Enabled = false;
                        bnTriggerExec3.Enabled = false;
                    }
                    else if (comboBox5.Text == "触发拍照" || comboBox5.Text == "通讯触发")
                    {

                        _cameraCtrl.Cameras[2].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger3.Checked || comboBox5.Text == "通讯触发")
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
                    _logger.WriteLog(ex.Message + "触发切换3");
                }
                _jobs.myjob3.triggerMode = comboBox5.Text;
            }
        }

        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (frm5.mark == 1 || _jobs.myjob4.state.Contains("相"))
            {
                try
                {
                    if (comboBox8.Text.Contains("连续运行"))
                    {
                        _cameraCtrl.Cameras[3].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                        cbSoftTrigger4.Enabled = false;
                        bnTriggerExec4.Enabled = false;
                    }
                    else if (comboBox8.Text.Contains("触发拍照") || comboBox8.Text.Contains("通讯触发"))
                    {

                        _cameraCtrl.Cameras[3].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                        // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                        //           1 - Line1;
                        //           2 - Line2;
                        //           3 - Line3;
                        //           4 - Counter;
                        //           7 - Software;
                        if (cbSoftTrigger4.Checked || comboBox8.Text.Contains("通讯触发"))
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
                    _logger.WriteLog(ex.Message + "触发切换4");
                }
                _jobs.myjob4.triggerMode = comboBox8.Text;
            }
        }

        private void listBox8_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox8.SelectedItem;
                if (item == null) return;
                if (_jobs.yunxing == false)
                {
                    // ★运行中不覆盖回图（同上）
                    SetReplayImg(_jobs.myjob3, item.filePath);
                    if (_jobs.myjob3.trriger == 0)
                    {
                        _jobs.myjob3.trriger = 1;
                        getrecord(_jobs.myjob3, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void listBox9_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            try
            {
                PictureListItem item = (PictureListItem)listBox9.SelectedItem;
                if (item == null) return;
                if (_jobs.yunxing == false)
                {
                    // ★运行中不覆盖回图（同上）
                    SetReplayImg(_jobs.myjob4, item.filePath);
                    if (_jobs.myjob4.trriger == 0)
                    {
                        _jobs.myjob4.trriger = 1;
                        getrecord(_jobs.myjob4, default(System.Collections.Generic.KeyValuePair<string, string>));
                    }
                }
            }
            catch { }
        }

        private void bnGetParam3_Click(object sender, EventArgs e)
        {
            GetParamFor(2);

        }

        private void bnGetParam4_Click(object sender, EventArgs e)
        {
            GetParamFor(3);

        }

        private void bnSetParam3_Click(object sender, EventArgs e)
        {
            SetParamFor(2);

        }

        private void bnSetParam4_Click(object sender, EventArgs e)
        {
            SetParamFor(3);

        }

        private void bnStartGrab3_Click(object sender, EventArgs e)
        {
            StartGrabFor(2);

        }

        private void bnStartGrab4_Click(object sender, EventArgs e)
        {
            StartGrabFor(3);

        }

        private void bnStopGrab3_Click(object sender, EventArgs e)
        {
            StopGrabFor(2);

        }

        private void bnStopGrab4_Click(object sender, EventArgs e)
        {
            StopGrabFor(3);

        }

        private void cbSoftTrigger3_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(2);

        }

        private void cbSoftTrigger4_CheckedChanged(object sender, EventArgs e)
        {
            SoftTriggerChangedFor(3);

        }

        private void bnTriggerExec3_Click(object sender, EventArgs e)
        {
            TriggerExecFor(2);

        }

        private void bnTriggerExec4_Click(object sender, EventArgs e)
        {
            TriggerExecFor(3);

        }

        private void 配置相机3ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob3);
            frm6[frm6.Count - 1].Show();
        }

        private void 配置相机4ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ShowForm6For(_jobs.myjob4);
            frm6[frm6.Count - 1].Show();
        }

        private void checkBox24_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox23_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void checkBox21_CheckedChanged(object sender, EventArgs e)
        {
           
        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button12_Click_1(object sender, EventArgs e)
        {
            if (button12.Text == "显示")
            {
                tabControl1.Visible = true;
                flowTabNav.Visible = true;
                button12.Text = "隐藏";
            }
            else if (button12.Text == "隐藏")
            {
                tabControl1.Visible = false;
                flowTabNav.Visible = false;
                button12.Text = "显示";
            }
        }

        private void checkBox6_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void 打开日志界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReadErrorLog f1 = new ReadErrorLog();
            f1.Show();
        }

        private void 打开参数调整界面ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (f1.Visible == false)
            {
                f1.block_1 = _jobs.myjob1.block;
                if (manager1.JobCount > 1)
                {
                    f1.temp = 1;
                    f1.block_2 = _jobs.myjob2.block;
                }
                if (manager1.JobCount > 2)
                    f1.block_3 = _jobs.myjob3.block;
                if (manager1.JobCount > 3)
                    f1.block_4 = _jobs.myjob4.block;
                if (manager1.JobCount > 4)
                    f1.block_5 = _jobs.myjob5.block;
                if (manager1.JobCount > 5)
                    f1.block_6 = _jobs.myjob6.block;
                if (manager1.JobCount > 6)
                    f1.block_7 = _jobs.myjob7.block;
                if (manager1.JobCount > 7)
                    f1.block_8 = _jobs.myjob8.block;
                f1.path = path_1;
                f1.Myjob = manager1;
                f1.Visible = true;
            }
            else
            {
                f1.temp = 0;
                f1.Visible = false;

            }
        }

        private void tabPage5_Click(object sender, EventArgs e)
        {

        }

        private void label57_Click(object sender, EventArgs e)
        {

        }

        private void label59_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                jiankongshijian = double.Parse(numericUpDown2.Value.ToString());
                _config.WriteString("time", "NG", numericUpDown2.Value.ToString());

            }
            catch
            {

            }
        }

        // ★ 2026-09-11：存图保留天数控件值变化 → 更新字段并写回 ini [存图] baocun_tianshu
        private void numericUpDown_saveDays_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                int days = (int)numericUpDown_saveDays.Value;
                if (days >= 1)
                {
                    _saveImageKeepDays = days;
                    _config.WriteString("存图", "baocun_tianshu", days.ToString());
                }
            }
            catch
            {
            }
        }

        private void label58_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            _jobs.myjob1.runcishu = 0;
            _jobs.myjob2.runcishu = 0;
            _jobs.myjob3.runcishu = 0;
            _jobs.myjob4.runcishu = 0;
            // ★清单②修复（2026-09-20）：原只清 1~4 路——5~12 路"运行次数"漏清
            _jobs.myjob5.runcishu = 0;
            _jobs.myjob6.runcishu = 0;
            _jobs.myjob7.runcishu = 0;
            _jobs.myjob8.runcishu = 0;
            _jobs.myjob9.runcishu = 0;
            _jobs.myjob10.runcishu = 0;
            _jobs.myjob11.runcishu = 0;
            _jobs.myjob12.runcishu = 0;
            //  label140.Text = cogRecordDisplay1.BackColor.ToString();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {

        }

        private void button4_Click_2(object sender, EventArgs e)
        {
            _jobs.myjob1.sum = 0;
            _jobs.myjob1.oksum = 0;
            _jobs.myjob1.ngsum = 0;
            _jobs.myjob1.rate = 0;
            _jobs.myjob2.sum = 0;
            _jobs.myjob2.oksum = 0;
            _jobs.myjob2.ngsum = 0;
            _jobs.myjob2.rate = 0;
            _jobs.myjob3.sum = 0;
            _jobs.myjob3.oksum = 0;
            _jobs.myjob3.ngsum = 0;
            _jobs.myjob3.rate = 0;
            _jobs.myjob4.sum = 0;
            _jobs.myjob4.oksum = 0;
            _jobs.myjob4.ngsum = 0;
            _jobs.myjob4.rate = 0;
            _jobs.myjob5.sum = 0;
            _jobs.myjob5.oksum = 0;
            _jobs.myjob5.ngsum = 0;
            _jobs.myjob5.rate = 0;
            _jobs.myjob6.sum = 0;
            _jobs.myjob6.oksum = 0;
            _jobs.myjob6.ngsum = 0;
            _jobs.myjob6.rate = 0;
            _jobs.myjob7.sum = 0;
            _jobs.myjob7.oksum = 0;
            _jobs.myjob7.ngsum = 0;
            _jobs.myjob7.rate = 0;
            _jobs.myjob8.sum = 0;
            _jobs.myjob8.oksum = 0;
            _jobs.myjob8.ngsum = 0;
            _jobs.myjob8.rate = 0;
            // ★清单②修复（2026-09-20）：原实现只清到 myjob8——9~12 路统计漏清（点"清零"后 9-12 仍显示旧值）
            _jobs.myjob9.sum = 0;
            _jobs.myjob9.oksum = 0;
            _jobs.myjob9.ngsum = 0;
            _jobs.myjob9.rate = 0;
            _jobs.myjob10.sum = 0;
            _jobs.myjob10.oksum = 0;
            _jobs.myjob10.ngsum = 0;
            _jobs.myjob10.rate = 0;
            _jobs.myjob11.sum = 0;
            _jobs.myjob11.oksum = 0;
            _jobs.myjob11.ngsum = 0;
            _jobs.myjob11.rate = 0;
            _jobs.myjob12.sum = 0;
            _jobs.myjob12.oksum = 0;
            _jobs.myjob12.ngsum = 0;
            _jobs.myjob12.rate = 0;
            for (int i = 0; i < 12; ++i)
            {
                m_nFrames[i] = 0;
            }

        }

        private void comboBox21_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox21.SelectedIndex == 0)
            {
                cuntu = 0;
            }
            else
            {
                cuntu = 1;
            }
        }

        private void bnGetLineSel3_Click(object sender, EventArgs e)
        {
            get_Selector(2, cbLineSel3);
        }

        private void bnGetLineSel4_Click(object sender, EventArgs e)
        {
            get_Selector(3, cbLineSel4);
        }

        private void bnSetLineSel3_Click(object sender, EventArgs e)
        {
            set_Selector(2, cbLineSel3);
        }

        private void bnSetLineSel4_Click(object sender, EventArgs e)
        {
            set_Selector(3, cbLineSel4);
        }



        private void bnGetLineMode4_Click(object sender, EventArgs e)
        {
            get_Mode(3, cbLineMode4);
        }

        private void bnSetLineMode3_Click(object sender, EventArgs e)
        {
            set_Mode(2, cbLineMode3);
        }
        private void get_Selector(int c, ComboBox b)
        {
            int nRet;
            MyCamera.MVCC_ENUMVALUE stSelValue = new MyCamera.MVCC_ENUMVALUE();
            nRet = _cameraCtrl.Cameras[c].MV_CC_GetEnumValue_NET("LineSelector", ref stSelValue);
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Get Fail!", nRet);
                return;
            }

            b.Items.Clear();

            for (int i = 0; i < stSelValue.nSupportedNum; i++)
            {
                b.Items.Add("LineSelector" + stSelValue.nSupportValue[i]);
                if (stSelValue.nCurValue == stSelValue.nSupportValue[i])
                {
                    b.SelectedIndex = i;
                }
            }
        }
        private void set_Selector(int c, ComboBox b)
        {
            int nRet;

            if (b.SelectedIndex == -1)
            {
                ShowErrorMsg("Please Select Output!", 0);
                return;
            }

            String strValue = b.SelectedItem.ToString().Substring(12);
            UInt32 nValue = Convert.ToUInt32(strValue);
            nRet = _cameraCtrl.Cameras[c].MV_CC_SetEnumValue_NET("LineSelector", nValue);
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Set Fail!", nRet);
                return;
            }

            ShowErrorMsg("Set Succeed!", 0);
        }
        private void get_Mode(int c, ComboBox b)
        {
            int nRet;
            MyCamera.MVCC_ENUMVALUE stModeValue = new MyCamera.MVCC_ENUMVALUE();
            nRet = _cameraCtrl.Cameras[c].MV_CC_GetEnumValue_NET("LineMode", ref stModeValue);
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Get Fail!", nRet);
                return;
            }

            b.Items.Clear();

            for (int i = 0; i < stModeValue.nSupportedNum; i++)
            {
                b.Items.Add("LineMode" + stModeValue.nSupportValue[i]);
                if (stModeValue.nCurValue == stModeValue.nSupportValue[i])
                {
                    b.SelectedIndex = i;
                }
            }
        }
        private void set_Mode(int c, ComboBox b)
        {
            int nRet;
            if (!dahua)
            {
                if (b.SelectedIndex == -1)
                {
                    ShowErrorMsg("Please Select Output!", 0);
                    return;
                }

                String strValue = b.SelectedItem.ToString().Substring(8);
                UInt32 nValue = Convert.ToUInt32(strValue);
                nRet = _cameraCtrl.Cameras[c].MV_CC_SetEnumValue_NET("LineMode", nValue);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Set Fail!", nRet);
                    return;
                }

                ShowErrorMsg("Set Succeed!", 0);
            }

        }
        private void output_inverse(int c, ComboBox b, CheckBox d)
        {
            if (d.CheckState == CheckState.Checked)
            {
                int nRet;

                if (b.SelectedIndex == -1)
                {
                    ShowErrorMsg("Please Select Output!", 0);
                    return;
                }

                //String strValue = cbLineMode.SelectedItem.ToString().Substring(8);
                UInt32 nValue = Convert.ToUInt32("1");
                //nRet = _cameraCtrl.Cameras.MV_CC_SetEnumValue_NET("LineInverter", nValue);
                nRet = _cameraCtrl.Cameras[c].MV_CC_SetBoolValue_NET("LineInverter", true);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Set Fail!", nRet);
                    return;
                }

                ShowErrorMsg("Set Succeed!", 0);

            }
            else
            {
                int nRet;

                if (b.SelectedIndex == -1)
                {
                    ShowErrorMsg("Please Select Output!", 0);
                    return;
                }

                //String strValue = cbLineMode.SelectedItem.ToString().Substring(8);
                UInt32 nValue = Convert.ToUInt32("0");
                // nRet = _cameraCtrl.Cameras.MV_CC_SetEnumValue_NET("LineInverter", nValue);
                nRet = _cameraCtrl.Cameras[c].MV_CC_SetBoolValue_NET("LineInverter", false);
                if (MyCamera.MV_OK != nRet)
                {
                    ShowErrorMsg("Set Fail!", nRet);
                    return;
                }

                ShowErrorMsg("Set Succeed!", 0);
            }
        }

        private void bnGetLineMode3_Click(object sender, EventArgs e)
        {
            get_Mode(2, cbLineMode3);
        }

        private void bnSetLineMode4_Click(object sender, EventArgs e)
        {
            set_Mode(3, cbLineMode4);
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(2, cbLineMode3, checkBox16);
        }

        private void checkBox20_CheckedChanged(object sender, EventArgs e)
        {
            output_inverse(3, cbLineMode4, checkBox20);
        }

        private void numericUpDown22_ValueChanged(object sender, EventArgs e)
        {
            feng = long.Parse(numericUpDown22.Value.ToString());
        }

        private void button6_Click_3(object sender, EventArgs e)
        {
            if (button6.Text == "顶" && listBox4.Visible == true)
            {
                if (_jobs.yunxing == false && listBox4.Items.Count > 0)
                {
                    listBox4.SelectedIndex = 0;
                    button6.Text = "底";
                }
            }
            else if (button6.Text == "底" && listBox4.Visible == true)
            {
                if (_jobs.yunxing == false && listBox4.Items.Count > 0)
                {
                    listBox4.SelectedIndex = listBox4.Items.Count - 1;
                    button6.Text = "顶";
                }
            }
        }

        private void button14_Click_1(object sender, EventArgs e)
        {
            if (button14.Text == "顶" && listBox5.Visible == true)
            {
                if (_jobs.yunxing == false && listBox5.Items.Count > 0)
                {
                    listBox5.SelectedIndex = 0;
                    button14.Text = "底";
                }
            }
            else if (button14.Text == "底" && listBox5.Visible == true)
            {
                if (_jobs.yunxing == false && listBox5.Items.Count > 0)
                {
                    listBox5.SelectedIndex = listBox5.Items.Count - 1;
                    button14.Text = "顶";
                }
            }
        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            if (button15.Text == "顶" && listBox8.Visible == true)
            {
                if (_jobs.yunxing == false && listBox8.Items.Count > 0)
                {
                    listBox8.SelectedIndex = 0;
                    button15.Text = "底";
                }
            }
            else if (button15.Text == "底" && listBox8.Visible == true)
            {
                if (_jobs.yunxing == false && listBox8.Items.Count > 0)
                {
                    listBox8.SelectedIndex = listBox8.Items.Count - 1;
                    button15.Text = "顶";
                }
            }
        }

        private void button16_Click_1(object sender, EventArgs e)
        {
            if (button16.Text == "顶" && listBox9.Visible == true)
            {
                if (_jobs.yunxing == false && listBox9.Items.Count > 0)
                {
                    listBox9.SelectedIndex = 0;
                    button16.Text = "底";
                }
            }
            else if (button16.Text == "底" && listBox9.Visible == true)
            {
                if (_jobs.yunxing == false && listBox9.Items.Count > 0)
                {
                    listBox9.SelectedIndex = listBox9.Items.Count - 1;
                    button16.Text = "顶";
                }
            }
        }



        #endregion
    }
}
