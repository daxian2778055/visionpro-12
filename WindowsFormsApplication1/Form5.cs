using demo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            wdini.ReadINIFile(AppDomain.CurrentDomain.BaseDirectory + "//test.ini");
            InitializeComponent();
        }
        public int mark1;
        public int monitor;
        public int mark;
        private bool _authMode = false;   // 2026-09-06：MES/MySQL 等权限校验的模态模式
        private ClassIni wdini = new ClassIni();
        private string code1;
        private string code2;
        Stopwatch timewatch;
        private void Form5_Load(object sender, EventArgs e)
        {
            mark1 = 0;
            monitor=0;
            mark = 0;
            timewatch = new Stopwatch();
            comboBox1.Items.Add (wdini.ReadString("userName", "user1", "空"));
            comboBox1.Items.Add(wdini.ReadString("userName", "user2", "空"));
            code1 = wdini.ReadString("userCode", "code1", "空");
            code2 = wdini.ReadString("userCode", "code2", "空");
            if (_authMode)
            {
                this.Text = "权限验证（仅厂家可进入）";
                comboBox1.Text = "厂家";
                comboBox1.Enabled = false;
                textBox1.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            comboBox1.Text = "";
            textBox1.Text = "";
            this.textBox1.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(comboBox1.Text=="")
            {
                MessageBox.Show("请选择有效的用户名！", "提示",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
            }
            if(comboBox1.Text=="厂家")
            {
                if(textBox1.Text=="Abc1234"|| textBox1.Text == "A")
                {
                    textBox1.Clear();
                    mark = 1;
                    mark1 = 1;
                    timewatch.Reset();
                    timewatch.Start();
                    if (_authMode)
                    {
                        this.DialogResult = DialogResult.OK;
                        return;
                    }
                    MessageBox.Show("厂家登录成功!");
                }
                else
                {
                    mark1 = 0;
                    textBox1.Clear();
                    this.textBox1.Focus();
                    MessageBox.Show("密码不正确!");
                }
            }
            if(comboBox1.Text=="操作工")
            {

                if(textBox1.Text=="123456")
                {
                    mark1 = 0;
                    mark = 1;
                    timewatch.Reset();
                    timewatch.Start();
                    textBox1.Clear();
                    MessageBox.Show("操作工登录成功!");
                }
                else
                {
                    mark1 = 0;
                    textBox1.Clear();
                    this.textBox1.Focus();
                    MessageBox.Show("密码不正确!");
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(mark1==1&&mark==1)
            {
                label4.Text = "厂家";
            }
            if(mark1==0&&mark==1)
            {
                label4.Text = "操作工";
            }
            if(mark==0)
            {
                label4.Text = "未登录";
            }
            if (mark == 1)
            {
                if (monitor == 1)
                    timewatch.Start();
                else
                    timewatch.Stop();
            }
            try
            {
                label3.Text = (timewatch.ElapsedMilliseconds / 1000).ToString();
                if (timewatch.ElapsedMilliseconds > 30000 && mark1 == 0)
                {
                    mark = 0;
                    timewatch.Stop();
                }
           
            if(mark==0)
            {
                timewatch.Stop();
            }
            }
            catch { }
        }

        private void Form5_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_authMode) return;   // 2026-09-06：权限校验模式允许正常关闭
            if (e.CloseReason != CloseReason.UserClosing)
                return;
            e.Cancel = true;
            this.Visible = false;      
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (label4.Text == "厂家")
                {
                    if (_authMode)
                    {
                        this.DialogResult = DialogResult.OK;
                        return;
                    }
                    this.Visible = false;
                }
                else
                    button1_Click(null, null);
            }
        }
        private bool front = false;
        private void timer2_Tick(object sender, EventArgs e)
        {
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

        /// <summary>
        /// 2026-09-06：MES/MySQL 等敏感菜单的厂家权限校验。保留万能密码 "A"。
        /// </summary>
        public static bool Authorize(IWin32Window owner)
        {
            using (var dlg = new Form5())
            {
                dlg._authMode = true;
                dlg.StartPosition = FormStartPosition.CenterParent;
                if (dlg.ShowDialog(owner) == DialogResult.OK)
                    return dlg.mark1 == 1;
                return false;
            }
        }
    }
}
