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
    public partial class Form6 : Form
    {
        CogToolBlock block1;
        public Form6(CogToolBlock block_11)
        {
            block1 = block_11;
            InitializeComponent();
            // ★ 防空白窗（2026-09-20）：相机流程块未加载时（myjob.block 为 null——常见于方案流程数不含该相机/
            //   分流程 vpp 未配置/方案内 CogToolBlock1 嵌套结构与软件约定不符），原实现直接 Subject=null →
            //   窗体一片空白无任何提示；现给出明确文字提示，便于现场定位是配置问题而非软件故障。
            if (block1 == null)
            {
                this.Text = "流程块未加载 - 请检查方案/分流程配置";
                Label lblTip = new Label();
                lblTip.Dock = DockStyle.Fill;
                lblTip.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                lblTip.ForeColor = System.Drawing.Color.Red;
                lblTip.Text = "该相机的流程块未加载（myjob.block 为 null），无法显示流程工具。\r\n" +
                              "请检查：① 当前方案的流程数是否包含该相机；\r\n" +
                              "② test.ini 中该相机的分流程 vpp 配置；\r\n" +
                              "③ 方案内 CogToolBlock1 嵌套结构是否符合约定。\r\n" +
                              "（主界面 Log 目录中搜索“流程N初始化失败/分流程”可确认）";
                this.Controls.Add(lblTip);
                lblTip.BringToFront();
                return;
            }
            cogToolBlockEdit1.Subject = block1;
            //绑定ToolBlock事件
           // cogToolBlockEdit1.Subject.Ran += new EventHandler(GetResult1_VisionPro);
        }
        //cogToolBlockEdit1绑定事件
        private void Form6_Load(object sender, EventArgs e)
        {

        }

        private void Form6_FormClosing(object sender, FormClosingEventArgs e)
        {
            cogToolBlockEdit1.Subject = null;
        }
        private bool front = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Visible == true && front == false)
            {
                front = true;
                this.BringToFront();
                //this.TopMost = true;
            }
            if (this.Visible == false)
            {
                front = false;
            }
        }
    }
}
