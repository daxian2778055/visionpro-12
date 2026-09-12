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

            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox5.Text]);
            tempTool = tools[comboBox5.Text];
        }

        private void comboBox5_DropDown(object sender, EventArgs e)
        {
            comboBox5.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_1.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") ||tool.Name.ToString().Contains("工具块")))
                {
                    tools.Add(tool.Name, tool);
                    comboBox5.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock"+i) || tool.Name.ToString().Contains("工具块"+i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                            tools.Add("工具块"+i+"-"+tool1.Name, tool1);
                            comboBox5.Items.Add(tools.Keys.Last());
                    }
                }
            }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox6.Text]);
            tempTool = tools[comboBox6.Text];

        }

        private void comboBox6_DropDown(object sender, EventArgs e)
        {
            comboBox6.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_2.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") || tool.Name.ToString().Contains("工具块")))
                {
                    // toolName.Add("初定位0", tool.Name.ToString());
                    tools.Add(tool.Name, tool);
                    comboBox6.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock" + i) || tool.Name.ToString().Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        tools.Add("工具块" + i + "-" + tool1.Name, tool1);
                        // toolName.Add("斑点工具0-0", tool1.Name.ToString());
                        comboBox6.Items.Add(tools.Keys.Last());
                    }
                }
            }
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
            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox7.Text]);
            tempTool = tools[comboBox7.Text];
        }

        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox8.Text]);
            tempTool = tools[comboBox8.Text];
        }

        private void comboBox15_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox15.Text]);
            tempTool = tools[comboBox15.Text];
        }

        private void comboBox13_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox13.Text]);
            tempTool = tools[comboBox13.Text];
        }

        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox11.Text]);
            tempTool = tools[comboBox11.Text];
        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tempTool != null)
                cogToolTreeView1.RemoveToolNode(tempTool);
            cogToolTreeView1.AddToolNode(tools[comboBox9.Text]);
            tempTool = tools[comboBox9.Text];
        }

        private void comboBox7_DropDown(object sender, EventArgs e)
        {
            comboBox7.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_3.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") || tool.Name.ToString().Contains("工具块")))
                {
                    // toolName.Add("初定位0", tool.Name.ToString());
                    tools.Add(tool.Name, tool);
                    comboBox7.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock" + i) || tool.Name.ToString().Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        tools.Add("工具块" + i + "-" + tool1.Name, tool1);
                        // toolName.Add("斑点工具0-0", tool1.Name.ToString());
                        comboBox7.Items.Add(tools.Keys.Last());
                    }
                }
            }
        }

        private void comboBox8_DropDown(object sender, EventArgs e)
        {
            comboBox8.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_4.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") || tool.Name.ToString().Contains("工具块")))
                {
                    // toolName.Add("初定位0", tool.Name.ToString());
                    tools.Add(tool.Name, tool);
                    comboBox8.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock" + i) || tool.Name.ToString().Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        tools.Add("工具块" + i + "-" + tool1.Name, tool1);
                        // toolName.Add("斑点工具0-0", tool1.Name.ToString());
                        comboBox8.Items.Add(tools.Keys.Last());
                    }
                }
            }
        }

        private void comboBox15_DropDown(object sender, EventArgs e)
        {
            comboBox15.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_5.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") || tool.Name.ToString().Contains("工具块")))
                {
                    // toolName.Add("初定位0", tool.Name.ToString());
                    tools.Add(tool.Name, tool);
                    comboBox15.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock" + i) || tool.Name.ToString().Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        tools.Add("工具块" + i + "-" + tool1.Name, tool1);
                        // toolName.Add("斑点工具0-0", tool1.Name.ToString());
                        comboBox15.Items.Add(tools.Keys.Last());
                    }
                }
            }
        }

        private void comboBox13_DropDown(object sender, EventArgs e)
        {
            comboBox13.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_6.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") || tool.Name.ToString().Contains("工具块")))
                {
                    // toolName.Add("初定位0", tool.Name.ToString());
                    tools.Add(tool.Name, tool);
                    comboBox13.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock" + i) || tool.Name.ToString().Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        tools.Add("工具块" + i + "-" + tool1.Name, tool1);
                        // toolName.Add("斑点工具0-0", tool1.Name.ToString());
                        comboBox13.Items.Add(tools.Keys.Last());
                    }
                }
            }
        }

        private void comboBox11_DropDown(object sender, EventArgs e)
        {
            comboBox11.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_7.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") || tool.Name.ToString().Contains("工具块")))
                {
                    // toolName.Add("初定位0", tool.Name.ToString());
                    tools.Add(tool.Name, tool);
                    comboBox11.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock" + i) || tool.Name.ToString().Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        tools.Add("工具块" + i + "-" + tool1.Name, tool1);
                        // toolName.Add("斑点工具0-0", tool1.Name.ToString());
                        comboBox11.Items.Add(tools.Keys.Last());
                    }
                }
            }
        }

        private void comboBox9_DropDown(object sender, EventArgs e)
        {
            comboBox9.Items.Clear();
            toolName.Clear();
            tools.Clear();
            block_11 = null;
            int i = 0;
            foreach (ICogTool tool in block_8.Tools)
            {
                if (!(tool.Name.ToString().Contains("CogToolBlock") || tool.Name.ToString().Contains("工具块")))
                {
                    // toolName.Add("初定位0", tool.Name.ToString());
                    tools.Add(tool.Name, tool);
                    comboBox9.Items.Add(tools.Keys.Last());
                }
                if (tool.Name.ToString().Contains("CogToolBlock" + i) || tool.Name.ToString().Contains("工具块" + i))
                {
                    i++;
                    if (block_11 == null)
                        block_11 = tool as CogToolBlock;
                    foreach (ICogTool tool1 in block_11.Tools)
                    {
                        tools.Add("工具块" + i + "-" + tool1.Name, tool1);
                        // toolName.Add("斑点工具0-0", tool1.Name.ToString());
                        comboBox9.Items.Add(tools.Keys.Last());
                    }
                }
            }
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
