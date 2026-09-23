using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Cognex.VisionPro;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ResultsAnalysis;

namespace WindowsFormsApplication1
{
    public partial class SubSet : Form
    {
        public int temp;
        public delegate void GetSeletionData(object Sender, SelectionChangedEventArgs e);
        public event GetSeletionData getData;
        public delegate void GetSeletionData2(object Sender, SelectionChangedEventArgs2 e);
        public event GetSeletionData2 getData2;
        public string path;
        public CogJobManager Myjob;
        public CogToolBlock block_1;
        public CogToolBlock block_2;
        public CogToolBlock block_3;
        public CogToolBlock block_4;
        public CogToolBlock block_5;
        public CogToolBlock block_6;
        public CogToolBlock block_7;
        public CogToolBlock block_8;
        public CogToolBlock block_9;
        public CogToolBlock block_10;
        public CogToolBlock block_11;
        public CogToolBlock block_12;
        public CogToolBlock block_13;
        public CogToolBlock block_14;
        public CogToolBlock block_21;
        public CogToolBlock block_22;
        public CogToolBlock block_23;
        public CogToolBlock block_24;
        public CogResultsAnalysisTool block_t;
        public ICogTool tempTool=null;
        public int jobsum=0;
        Dictionary<string, string> toolName = new Dictionary<string, string>();
        Dictionary<string, ICogTool> tools = new Dictionary<string, ICogTool>();
        ErrorLog MsgErroeLog = new ErrorLog();
        public SubSet()
        {
            InitializeComponent();


        }
        public class SelectionChangedEventArgs : EventArgs
        {

            private string m_selection;
            private bool m_check;
            private CogToolBlock m_toolblock;

            //本属性用于传递事件数据

            public string Selection
            {

                get { return m_selection; }

            }
            public bool Check
            {
                get { return m_check; }
            }
            public CogToolBlock Toolblock
            {
                get { return m_toolblock; }
            }
            public SelectionChangedEventArgs(string selection, bool check, CogToolBlock toolblock)
            {

                m_selection = selection;
                m_check = check;
                m_toolblock = toolblock;
            }
        }
        public class SelectionChangedEventArgs2 : EventArgs
        {

            private string m_selection;
            private bool m_check;
            private CogToolBlock m_toolblock;

            //本属性用于传递事件数据

            public string Selection
            {

                get { return m_selection; }

            }
            public bool Check
            {
                get { return m_check; }
            }
            public CogToolBlock Toolblock
            {
                get { return m_toolblock; }
            }
            public SelectionChangedEventArgs2(string selection, bool check, CogToolBlock toolblock)
            {

                m_selection = selection;
                m_check = check;
                m_toolblock = toolblock;
            }
        }
        private void SubSet_Load(object sender, EventArgs e)
        {


        }




        private void textBox5_KeyUp(object sender, KeyEventArgs e)
        {
 
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox6_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {

            ShowToolInTree(comboBox5);   // ★G4
        }

        private void comboBox5_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox5, block_1);   // ★G4：原 25 行复制体现在守护/收口到公共方法
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowToolInTree(comboBox6);   // ★G4

        }

        private void comboBox6_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox6, block_2);   // ★G4
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }


        private void SubSet_FormClosing(object sender, FormClosingEventArgs e)
        {

            //  if (MessageBox.Show("将要关闭参数设置页面，是否继续？", "询问", MessageBoxButtons.YesNo) == DialogResult.Yes)
            // {
            // Application.Exit();
                cogResultsAnalysisEdit1.Subject = null;
            if (tempTool != null)
            {
                cogToolTreeView1.RemoveToolNode(tempTool);
                tempTool = null;
            }
            if (e.CloseReason != CloseReason.UserClosing)
                return;
            e.Cancel = true;
            this.Visible = false;
          //  }
          //  else
          //  {

          //      e.Cancel = true;

           // }
        }

        private void SubSet_VisibleChanged(object sender, EventArgs e)
        {
            if (Myjob != null)
            {
                if (Myjob.JobCount == 1)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = false;
                    groupBox3.Enabled = false;
                    groupBox4.Enabled = false;
                    groupBox5.Enabled = false;
                    groupBox6.Enabled = false;
                    groupBox7.Enabled = false;
                    groupBox8.Enabled = false;
                }
                if (Myjob.JobCount == 2)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = false;
                    groupBox4.Enabled = false;
                    groupBox5.Enabled = false;
                    groupBox6.Enabled = false;
                    groupBox7.Enabled = false;
                    groupBox8.Enabled = false;
                }
                if (Myjob.JobCount == 3)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    groupBox4.Enabled = false;
                    groupBox5.Enabled = false;
                    groupBox6.Enabled = false;
                    groupBox7.Enabled = false;
                    groupBox8.Enabled = false;
                }
                if (Myjob.JobCount == 4)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    groupBox4.Enabled = true;
                    groupBox5.Enabled = false;
                    groupBox6.Enabled = false;
                    groupBox7.Enabled = false;
                    groupBox8.Enabled = false;
                }
                if (Myjob.JobCount == 5)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    groupBox4.Enabled = true;
                    groupBox5.Enabled = false;
                    groupBox6.Enabled = false;
                    groupBox7.Enabled = false;
                    groupBox8.Enabled = true;
                }
                if (Myjob.JobCount == 6)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    groupBox4.Enabled = true;
                    groupBox5.Enabled = false;
                    groupBox6.Enabled = false;
                    groupBox7.Enabled = true;
                    groupBox8.Enabled = true;
                }
                if (Myjob.JobCount == 7)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    groupBox4.Enabled = true;
                    groupBox5.Enabled = false;
                    groupBox6.Enabled = true;
                    groupBox7.Enabled = true;
                    groupBox8.Enabled = true;
                }
                if (Myjob.JobCount == 8)
                {
                    groupBox1.Enabled = true;
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = true;
                    groupBox4.Enabled = true;
                    groupBox5.Enabled = true;
                    groupBox6.Enabled = true;
                    groupBox7.Enabled = true;
                    groupBox8.Enabled = true;
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_1.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch {
                try
                {
                    block_t = null;
                    block_t = block_1.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
            
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_1.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch {
                try
                {
                    block_t = null;
                    block_t = block_1.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_2.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_2.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_2.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch {
                try
                {
                    block_t = null;
                    block_t = block_2.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_3.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch {
                try
                {
                    block_t = null;
                    block_t = block_3.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_3.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch {
                try
                {
                    block_t = null;
                    block_t = block_3.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_4.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_4.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_4.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_4.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_5.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_5.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button27_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_5.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_5.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_6.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_6.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_6.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_6.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_7.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_7.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_7.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_7.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_8.Tools["逻辑0"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_8.Tools["CogResultsAnalysisTool0"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                block_t = null;
                block_t = block_8.Tools["逻辑1"] as CogResultsAnalysisTool;
                // InitializeComponent();
                cogResultsAnalysisEdit1.Subject = block_t;
            }
            catch
            {
                try
                {
                    block_t = null;
                    block_t = block_8.Tools["CogResultsAnalysisTool1"] as CogResultsAnalysisTool;
                    // InitializeComponent();
                    cogResultsAnalysisEdit1.Subject = block_t;
                }
                catch { }
            }
        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowToolInTree(comboBox7);   // ★G4
        }

        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowToolInTree(comboBox8);   // ★G4
        }

        private void comboBox15_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowToolInTree(comboBox15);   // ★G4
        }

        private void comboBox13_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowToolInTree(comboBox13);   // ★G4
        }

        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowToolInTree(comboBox11);   // ★G4
        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowToolInTree(comboBox9);   // ★G4
        }

        private void comboBox7_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox7, block_3);   // ★G4
        }

        private void comboBox8_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox8, block_4);   // ★G4
        }

        private void comboBox15_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox15, block_5);   // ★G4
        }

        private void comboBox13_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox13, block_6);   // ★G4
        }

        private void comboBox11_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox11, block_7);   // ★G4
        }

        private void comboBox9_DropDown(object sender, EventArgs e)
        {
            PopulateToolCombo(comboBox9, block_8);   // ★G4
        }
        // ★G4 修复（2026-09-23）：原 8 组 DropDown/SelectedIndexChanged 全部裸写且共用一个 tools 字典——
        //   ① 方案流程数不足时 block_N 为 null → foreach 直接 NRE；
        //   ② 方案内工具重名（或外层与子块同名）→ tools.Add 抛 ArgumentException，下拉即崩；
        //   ③ 任一组合框展开会 tools.Clear() 重灌 → 其他组合框当前文本失效，SelectedIndexChanged 里
        //      tools[Text] 抛 KeyNotFoundException——三者都在事件处理器且无任何 try，Debug/Release 均直接崩窗。
        //   收口为两个带守护的公共方法，8 组事件只作转调。
        private void PopulateToolCombo(ComboBox cb, CogToolBlock block)
        {
            cb.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            if (block == null) return;   // ★G4：该组在方案中不存在（流程数不足/未绑定）→ 空列表而非 NRE
            int i = 0;
            foreach (ICogTool tool in block.Tools)
            {
                string name = (tool.Name ?? "").ToString();
                if (!(name.Contains("CogToolBlock") || name.Contains("工具块")))
                    AddToolEntry(cb, name, tool);
                if (name.Contains("CogToolBlock" + i) || name.Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    if (block_11 != null)   // ★G4：名为工具块但实际不是 CogToolBlock 时原实现 NRE
                        foreach (ICogTool tool1 in block_11.Tools)
                            AddToolEntry(cb, "工具块" + i + "-" + (tool1.Name ?? "").ToString(), tool1);
                }
            }
        }
        private void AddToolEntry(ComboBox cb, string key, ICogTool tool)
        {
            // ★G4：重名键去冲突（原 tools.Add 重复键直接抛）
            if (tools.ContainsKey(key)) key = key + "#" + tools.Count;
            tools[key] = tool;
            cb.Items.Add(key);
        }
        private void ShowToolInTree(ComboBox cb)
        {
            try
            {
                ICogTool t;
                if (!tools.TryGetValue(cb.Text, out t) || t == null)
                {
                    // ★G4：文本已失效（其他组下拉清空了共用字典）→ 撤下旧节点即可，不崩
                    if (tempTool != null)
                    {
                        cogToolTreeView1.RemoveToolNode(tempTool);
                        tempTool = null;
                    }
                    return;
                }
                if (tempTool != null)
                    cogToolTreeView1.RemoveToolNode(tempTool);
                cogToolTreeView1.AddToolNode(t);
                tempTool = t;
            }
            catch { }
        }
        private bool front = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Visible == true && front == false)
            {
                front = true;
                this.BringToFront();
        //        this.TopMost = true;
            }
            if (this.Visible == false)
            {
                front = false;
            }
        }
    }
}
