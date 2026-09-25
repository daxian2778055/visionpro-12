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
            catch (Exception ex)
            {
                tishi = "训练新模式失败:" + ex.Message;
            }

            ShowTrainRecord();
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
            catch (Exception ex)
            {
                tishi = "训练失败:" + ex.Message;
            }

            ShowTrainRecord();
        }

        /// <summary>★第32轮 W8：训练后回显当前记录。原两处调用点把 CreateCurrentRecord() 写在 try 之外——
        /// 未选中定位工具（Inspect1 为 null）或该工具尚无记录/子项不足 3 个时，NRE/越界直接打到全局崩溃
        /// 处理器：每点一次弹一次"程序已崩溃"并落一份 minidump。改为独立 try + 空值/子项数守卫，
        /// 回显失败只并入提示，不影响训练结果本身。</summary>
        private void ShowTrainRecord()
        {
            try
            {
                if (Inspect1 == null)
                {
                    tishi += "（未选择定位工具，无记录可显示）";
                    return;
                }
                var rec = Inspect1.CreateCurrentRecord();
                if (rec == null || rec.SubRecords == null || rec.SubRecords.Count <= 2)
                {
                    tishi += "（当前记录子项不足，未回显）";
                    return;
                }
                cogRecordDisplay1.Record = rec.SubRecords[2];
                if (max1 == 0)
                {
                    cogRecordDisplay1.Fit(true);
                    max1 = 1;
                }
            }
            catch (Exception ex)
            {
                tishi += "（回显失败:" + ex.Message + "）";
            }
        }
        Dictionary<string, ICogTool> tools1 = new Dictionary<string, ICogTool>();
        Dictionary<string, string> toolName1 = new Dictionary<string, string>();
        // ★清单① 修复（2026-09-20）：下拉项 → "父工具块 + 原工具实例"映射——
        //   原"加载模板"(button3) 只把新对象放进 tools1，未写回 blk.Tools：之后训练/阈值/保存作用于
        //   新对象，而运行检测仍用 blk 里的旧工具（调参白调）。写回时用这两张表定位并替换。
        Dictionary<string, CogToolBlock> toolParent1 = new Dictionary<string, CogToolBlock>();
        Dictionary<string, ICogTool> toolOld1 = new Dictionary<string, ICogTool>();
        public CogToolBlock block_11;
        private void comboBox27_DropDown(object sender, EventArgs e)
        {
            combdrop(comboBox27, block4, tools1);
        }
        private void combdrop(ComboBox combox, CogToolBlock blk, Dictionary<string, ICogTool> dic)
        {
            combox.Items.Clear();
            toolName1.Clear();
            toolParent1.Clear();   // ★N9：清空“父块+原工具”映射（原只增不清，切方案/多次枚举会残留旧块引用）
            toolOld1.Clear();
            // ★ 防崩溃（2026-09-20）：流程块未加载时（blk 为 null）原实现直接 blk.Tools → NullReferenceException
            //   （崩溃日志定位：Form9.cs:106 / comboBox27_DropDown）。此处给出提示项并返回，不再枚举。
            if (blk == null)
            {
                combox.Items.Add("（该相机流程未加载，无法枚举工具）");
                return;
            }
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
                        // ★清单①：记录父块+原工具实例（供"加载模板"写回 blk.Tools）
                        toolParent1[tool.Name] = blk;
                        toolOld1[tool.Name] = tool;
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
                            string _k1 = "工具块" + i + "-" + tool1.Name;
                            dic.Add(_k1, tool1);
                            combox.Items.Add(_k1);
                            // ★清单①：记录父块+原工具实例（供"加载模板"写回 blk.Tools）
                            toolParent1[_k1] = tool as CogToolBlock;
                            toolOld1[_k1] = tool1;
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
                                    string _k2 = "工具块" + i + "-" + "工具块" + j + "-" + tool2.Name;
                                    dic.Add(_k2, tool2);
                                    combox.Items.Add(_k2);
                                    // ★清单①：记录父块+原工具实例（供"加载模板"写回 blk.Tools）
                                    toolParent1[_k2] = tool1 as CogToolBlock;
                                    toolOld1[_k2] = tool2;
                                }
                                if (tool2 is CogToolBlock)
                                {
                                    k++;
                                    block_11 = tool2 as CogToolBlock;
                                    foreach (ICogTool tool12 in block_11.Tools)
                                    {
                                        if (tool12 is CogPatInspectTool)
                                        {
                                            string _k3 = "工具块" + i + "-" + "工具块" + j + "-" + "工具块" + k + "-" + tool12.Name;
                                            dic.Add(_k3, tool12);
                                            combox.Items.Add(_k3);
                                            // ★清单①：记录父块+原工具实例（供"加载模板"写回 blk.Tools）
                                            toolParent1[_k3] = tool2 as CogToolBlock;
                                            toolOld1[_k3] = tool12;
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
            catch (Exception ex)
            {
                tishi = "保存模板失败:" + ex.Message;
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
                // ★清单① 修复（2026-09-20）：把加载的工具写回 blk.Tools（替换原工具实例）——
                //   原实现只更新 tools1 字典，blk 里仍是旧工具 → 后续训练/阈值/保存都作用于新对象，
                //   而运行检测用旧工具（调参白调）。用 combdrop 记录的"父块 + 原工具名"做替换。
                try
                {
                    CogToolBlock _parentBlk;
                    ICogTool _oldTool;
                    if (toolParent1.TryGetValue(comboBox27.Text, out _parentBlk) && _parentBlk != null
                        && toolOld1.TryGetValue(comboBox27.Text, out _oldTool) && _oldTool != null
                        && !string.IsNullOrEmpty(_oldTool.Name))
                    {
                        // ★N9 修复（2026-09-20）：新加载工具改名为原工具名——原实现只按 _oldTool.Name 替换字典项，
                        //   而 Inspect1.Name 仍是模板内旧名；第二次写回时 toolOld1 已存新实例、按其 Name 查不到 → 落"未找到"。
                        Inspect1.Name = _oldTool.Name;
                        _parentBlk.Tools[_oldTool.Name] = Inspect1;   // 替换流程内的原工具
                        toolOld1[comboBox27.Text] = Inspect1;         // 之后再次加载时以新实例为准
                        tishi = "加载模板成功（已写回流程工具）";
                    }
                    else
                    {
                        tishi = "加载模板成功（未找到流程内原工具，未写回：运行仍用旧工具！）";
                    }
                }
                catch (Exception exWb)
                {
                    tishi = "加载模板成功，但写回流程失败: " + exWb.Message;
                }
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
