using Cognex.VisionPro;
using Cognex.VisionPro.PatInspect;
using Cognex.VisionPro.ToolBlock;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form9 : Form
    {
        CogToolBlock block4;
        public CogPatInspectTool Inspect1;
        public ICogRecord input_tu;
        public ICogRecord trian_tu;
        string tishi;
        private int max1;
        private int max2;
        public Form9(CogToolBlock block_44)
        {
            block4 = block_44;
            InitializeComponent();
   
            //绑定ToolBlock事件
           // cogToolBlockEdit1.Subject.Ran += new EventHandler(GetResult1_VisionPro);
        }
        //cogToolBlockEdit1绑定事件
 
        private void Form9_Load(object sender, EventArgs e)
        {
            tishi = "";
            max1 = 0;
            max2 = 0;
        }

        private void Form9_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                Inspect1.Pattern.Train();
                tishi = "训练新模式成功";
            }
            catch
            {
                tishi = "训练新模式失败";
            }


            cogRecordDisplay1.Record = Inspect1.CreateCurrentRecord().SubRecords[2];
            if (max1 == 0)
            {
                cogRecordDisplay1.Fit(true);
                max1 = 1;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                CogImage8Grey image8 = Inspect1.InputImage;
                CogTransform2DLinear pose8 = Inspect1.Pose;
                Inspect1.Pattern.StatisticalTrain(image8, pose8);
                tishi = "训练成功";
            }
            catch
            {
                tishi = "训练失败";
            }


            cogRecordDisplay1.Record = Inspect1.CreateCurrentRecord().SubRecords[2];
            if (max1 == 0)
            {
                cogRecordDisplay1.Fit(true);
                max1 = 1;
            }
        }
        Dictionary<string, ICogTool> tools1 = new Dictionary<string, ICogTool>();
        Dictionary<string, string> toolName1 = new Dictionary<string, string>();
        public CogToolBlock block_11;
        private void comboBox27_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox27, block4, tools1);
        }
        private void combdrop(ComboBox combox, CogToolBlock blk, Dictionary<string, ICogTool> dic)
        {
            combox.Items.Clear();
            toolName1.Clear();
            dic.Clear();
            block_11 = null;
            int i = 0;
            int j = 0;
            int k = 0;
            foreach (ICogTool tool in blk.Tools)
            {
                if (!(tool is CogToolBlock) && (tool is CogPatInspectTool))
                {
                    if (!dic.ContainsKey(tool.Name))
                    {
                        dic.Add(tool.Name, tool);
                        combox.Items.Add(tool.Name);
                    }
                }
                if (tool is CogToolBlock)
                {
                    i++;
                    block_11 = tool as CogToolBlock;
                    //  j = 0;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        if (!(tool1 is CogToolBlock) && tool1 is CogPatInspectTool)
                        {
                            dic.Add("工具块" + i + "-" + tool1.Name, tool1);
                            combox.Items.Add(dic.Keys.Last());
                        }
                        if (tool1 is CogToolBlock)
                        {
                            j++;
                            block_11 = tool1 as CogToolBlock;
                            // k = 0;
                            foreach (ICogTool tool2 in block_11.Tools)
                            {
                                if (!(tool2 is CogToolBlock) && tool2 is CogPatInspectTool)
                                {
                                    dic.Add("工具块" + i + "-" + "工具块" + j + "-" + tool2.Name, tool2);
                                    combox.Items.Add(dic.Keys.Last());
                                }
                                if (tool2 is CogToolBlock)
                                {
                                    k++;
                                    block_11 = tool2 as CogToolBlock;
                                    foreach (ICogTool tool12 in block_11.Tools)
                                    {
                                        if (tool12 is CogPatInspectTool)
                                        {
                                            dic.Add("工具块" + i + "-" + "工具块" + j + "-" + "工具块" + k + "-" + tool12.Name, tool12);
                                            combox.Items.Add(dic.Keys.Last());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void comboBox27_SelectedIndexChanged(object sender, EventArgs e)
        {
            bian = 1;
            ICogTool selTool9;
            if (!tools1.TryGetValue(comboBox27.Text, out selTool9)) return;   // 未选择有效工具时跳过，防止 KeyNotFound
            Inspect1 = selTool9 as CogPatInspectTool;
            numericUpDown1.Value = (decimal)Inspect1.Pattern.ThresholdScale;
            numericUpDown2.Value = (decimal)Inspect1.Pattern.ThresholdOffset;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                CogSerializer.SaveObjectToFile(Inspect1, Application.StartupPath + "//模板//" + textBox2.Text + ".vpp");
                tishi = "保存模板成功";
            }
            catch
            {
                tishi = "保存模板成功";
            }
        }
        int bian = 0;
        int bian_1 = 0;
        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                bian = 1;
                Inspect1 = CogSerializer.LoadObjectFromFile(Application.StartupPath + "//模板//" + textBox2.Text + ".vpp") as CogPatInspectTool;
                numericUpDown1.Value = (decimal)Inspect1.Pattern.ThresholdScale;
                numericUpDown2.Value = (decimal)Inspect1.Pattern.ThresholdOffset;
                tools1[comboBox27.Text] = Inspect1;
                tishi = "加载模板成功";
            }
            catch
            {
                tishi = "加载模板失败";
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if(bian==0)
            Inspect1.Pattern.ThresholdScale = (double)numericUpDown1.Value;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (bian == 0)
                Inspect1.Pattern.ThresholdOffset = (double)numericUpDown2.Value;
        }
        private bool front = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if(bian==1&&bian_1==0)
            {
                bian_1 = 1;
            }
            else if(bian == 1 && bian_1 == 1)
            {
                bian_1 = 0;
                bian = 0;
            }
            if (this.Visible == true && front == false)
            {
                front = true;
                this.BringToFront();
                this.TopMost = true;
            }
            if (this.Visible == false)
            {
                front = false;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            label3.Text = tishi;
            if (Inspect1 != null)
            {
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
                label8.Text = Inspect1.Pattern.TrainedCount.ToString();
            }
            else
            {
                numericUpDown1.Enabled = false;
                numericUpDown2.Enabled = false;
            }
            if (input_tu != null)
            {

                cogRecordDisplay1.Record = input_tu;
                if (max1 == 0)
                {
                    cogRecordDisplay1.Fit(true);
                    max1 = 1;
                }
                input_tu = null;
            }
            if (trian_tu != null)
            {

                cogRecordDisplay2.Record = trian_tu;
                if (max2 == 0)
                {
                    cogRecordDisplay2.Fit(true);
                    max2 = 1;
                }
                trian_tu = null;
            }
        }
    }
}
