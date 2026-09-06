using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    public partial class Frm2 : Form
    {
        // 线程安全进度字段 - 由其他线程写入，UI线程定时读取
        public volatile int _progressValue;
        public volatile string _statusText = "正在初始化...";

        public int start;
        public int end;

        private System.Windows.Forms.Label lblStatus;

        public Frm2()
        {
            InitializeComponent();

            // 程序化添加状态文本标签（避免修改resx）
            lblStatus = new System.Windows.Forms.Label();
            lblStatus.AutoSize = false;
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblStatus.ForeColor = System.Drawing.Color.White;
            lblStatus.BackColor = System.Drawing.Color.Transparent;
            lblStatus.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            lblStatus.Location = new System.Drawing.Point(50, this.ClientSize.Height - 50);
            lblStatus.Size = new System.Drawing.Size(this.ClientSize.Width - 100, 25);
            lblStatus.Text = "正在初始化...";
            this.Controls.Add(lblStatus);
            lblStatus.BringToFront();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            progressBar1.Style = ProgressBarStyle.Continuous;
            timer1.Interval = 200;
            timer1.Start();
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            end = 1;
            this.DesktopLocation = new Point(145, 145);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // 从共享字段读取进度值并在UI线程更新控件
            int val = _progressValue;
            if (val > 0 && val <= 100)
            {
                progressBar1.Value = val;
            }
            lblStatus.Text = _statusText;

            if (start == 1)
            {
                progressBar1.Value = 100;
                lblStatus.Text = "加载完成";
                this.Refresh();
                System.Threading.Thread.Sleep(200);
                this.Hide();
                timer1.Stop();
                this.Close();
            }
        }
    }
}
