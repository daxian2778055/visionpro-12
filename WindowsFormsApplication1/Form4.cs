using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Security.Cryptography;
namespace WindowsFormsApplication1
{
    public partial class Form4 : Form
    {
     
       
        public Form4()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 目标窗（调用方 Form3 实例）。非空时确认结果直接写回该窗的 Cur* 属性：
        /// 主窗（连接 1）= 写回静态字段（行为与旧版一致）；额外整窗（连接 2~4）= 写回各自实例字段，
        /// 不再经由共享静态字段中转，避免“某额外窗改串口后污染主窗/其它窗配置”的跨连接串扰。
        /// 为空（旧调用方）时保持原逻辑写 Form3 静态字段。
        /// 注：命名 Target 而非 Owner，避免隐藏继承自 Form.Owner 的属性。
        /// </summary>
        public Form3 Target { get; set; }
     
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form4_Load(object sender, EventArgs e)
        {
            String[] ports = SerialPort.GetPortNames();
            try
            {
                foreach (string port in ports)
                {
                    comboBox1.Items.Add(port);
                }
                comboBox1.SelectedIndex = 0;
            }
            catch { }
            comboBox2.Items.Add("4800");
            comboBox2.Items.Add("9600");
            comboBox2.Items.Add("19200");
            comboBox2.Items.Add("38400");
            comboBox2.Items.Add("57600");
            comboBox2.Items.Add("115200");
            comboBox2.SelectedIndex = 1;
            comboBox3.Items.Add("6");
            comboBox3.Items.Add("7");
            comboBox3.Items.Add("8");
            comboBox3.Items.Add("9");
            comboBox3.SelectedIndex = 2;
            comboBox4.Items.Add("0");
            comboBox4.Items.Add("1");
            comboBox4.Items.Add("2");
            comboBox4.SelectedIndex = 1;
            comboBox5.Items.Add("无");
            comboBox5.Items.Add("奇");
            comboBox5.Items.Add("偶");
            comboBox5.SelectedIndex = 0;
            TryPreloadOwnerConfig();
        }

        /// <summary>
        /// 打开时回显调用方当前串口配置（仅当能在列表中找到对应项时生效，
        /// 找不到则保持默认项，避免用户仅改端口时其余参数被静默重置）。
        /// </summary>
        private void TryPreloadOwnerConfig()
        {
            if (Target == null) return;
            try
            {
                if (string.IsNullOrEmpty(Target.CurPortName)) return;
                int idx;
                // 端口名忽略大小写匹配（ini 历史值可能为 "Com1"，系统枚举为 "COM1"），匹配失败则保持默认第 1 个端口
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    if (string.Equals(comboBox1.Items[i].ToString(), Target.CurPortName, StringComparison.OrdinalIgnoreCase))
                    { comboBox1.SelectedIndex = i; break; }
                }
                idx = comboBox2.FindStringExact(Target.CurBaudRate);
                if (idx >= 0) comboBox2.SelectedIndex = idx;
                idx = comboBox3.FindStringExact(Target.CurDataBits);
                if (idx >= 0) comboBox3.SelectedIndex = idx;
                idx = comboBox4.FindStringExact(Target.CurStopBits);
                if (idx >= 0) comboBox4.SelectedIndex = idx;
                // 校验位 0/1/2 = 无/奇/偶，正好等于下拉 SelectedIndex
                idx = Target.CurParity == "" ? -1 : int.Parse(Target.CurParity);
                if (idx >= 0 && idx < comboBox5.Items.Count) comboBox5.SelectedIndex = idx;
            }
            catch { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 有调用方实例时直接写回该窗（主窗→静态字段，额外整窗→实例字段），不再污染共享静态字段；
            // 无调用方（旧路径）才写静态字段保持兼容。
            if (Target != null)
            {
                Target.CurPortName = comboBox1.Text;
                Target.CurBaudRate = comboBox2.Text;
                Target.CurDataBits = comboBox3.Text;
                Target.CurStopBits = comboBox4.Text;
                // 校验位由 comboBox5（无/奇/偶，0/1/2）决定；勿误用停止位下拉 comboBox4 的 SelectedIndex
                Target.CurParity = comboBox5.SelectedIndex.ToString();
            }
            else
            {
                Form3.strportName = comboBox1.Text;
                Form3.strbaudRate = comboBox2.Text;
                Form3.strDataBits = comboBox3.Text;
                Form3.strStopBits = comboBox4.Text;
                Form3.strjiaoyan = comboBox5.SelectedIndex.ToString();
            }
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //ManagementClass mc1 = new ManagementClass("Win32_PhysicalMedia");
            ////网上有提到，用Win32_DiskDrive，但是用Win32_DiskDrive获得的硬盘信息中并不包含SerialNumber属性。   
            //ManagementObjectCollection moc1 = mc1.GetInstances();
            //string strID = null;
            //foreach (ManagementObject mo in moc1)
            //{
            //    strID = mo.Properties["SerialNumber"].Value.ToString();
            //    break;
            //}
            string cpuSerialnumber1 = string.Empty;
            using (MD5 md5Hash = MD5.Create())
            {

                byte[] data =md5Hash.ComputeHash(Encoding.UTF8.GetBytes(textBox1.Text.Trim()));
                byte[] data1 = new byte[] { 0x16,0xa2,0xa8};
                byte[] data2 = md5Hash.ComputeHash(Encoding.UTF8.GetBytes("123"));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                for (int i = 0; i < data1.Length; i++)
                {
                    sBuilder.Append(data1[i].ToString("x2"));
                }
                for (int i = 0; i < data2.Length; i++)
                {
                    sBuilder.Append(data2[i].ToString("x2"));
                }
                string id = sBuilder.ToString();
                textBox2.Text =id;
            }    
        }
        private bool front = false;
        private void timer1_Tick(object sender, EventArgs e)
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

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
