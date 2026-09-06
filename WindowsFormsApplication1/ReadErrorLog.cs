using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class ReadErrorLog : Form
    {
        ErrorLog ReadError = new ErrorLog();
        private string[] _pendingLogArr;   // 待加载日志行数组
        private int _pendingLogIdx;        // 当前已加载到的索引
        private int _pendingLogTotal;      // 总行数
        private const int LoadChunkSize = 300; // 每批加载行数
        public ReadErrorLog()
        {
            InitializeComponent();
        }

        private void ReadErrorLog_Load(object sender, EventArgs e)
        {
            this.BeginInvoke(new EventHandler(delegate {
                if (comboBox1.Items.Count == 0)
                {
                    for (int i = 2020; 2020 <= i && i < 2050; i++)
                    {
                        comboBox1.Items.Add(i.ToString());
                    }
                    for (int i = 1; i < 13; i++)
                    {
                        if (i < 10)
                            comboBox2.Items.Add("0" + i.ToString());
                        else
                            comboBox2.Items.Add(i.ToString());
                    }
                    for (int i = 1; i < 32; i++)
                    {
                        if (i < 10)
                            comboBox3.Items.Add("0" + i.ToString());
                        else
                            comboBox3.Items.Add(i.ToString());
                    }
                }
                comboBox1.SelectedItem = DateTime.Now.Year.ToString();
                if (DateTime.Now.Month.ToString().Length == 1)
                    comboBox2.SelectedItem = "0" + DateTime.Now.Month.ToString();
                else
                    comboBox2.SelectedItem = DateTime.Now.Month.ToString();
                if (DateTime.Now.Day.ToString().Length == 1)
                    comboBox3.SelectedItem = "0" + DateTime.Now.Day.ToString();
                else
                    comboBox3.SelectedItem = DateTime.Now.Day.ToString();
            }));
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 禁用按钮避免重复点击
            button1.Enabled = false;
            button1.Text = "加载中…";

            string filePath = AppDomain.CurrentDomain.BaseDirectory + "Log\\"
                + comboBox1.SelectedItem + "-" + comboBox2.SelectedItem + "-" + comboBox3.SelectedItem + ".txt";

            Task.Run(() =>
            {
                string content;
                try
                {
                    content = ReadError.ReadLog(filePath);
                }
                catch
                {
                    content = null;
                }

                if (content == null)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        button1.Enabled = true;
                        button1.Text = "打开";
                        listBox1.Items.Clear();
                        listBox1.Items.Add("无法读取日志文件");
                    }));
                    return;
                }

                string[] arr = content.Split('/');

                this.BeginInvoke(new Action(() =>
                {
                    // 在UI线程启动定时分批加载
                    listBox1.Items.Clear();
                    _pendingLogArr = arr;
                    _pendingLogIdx = 0;
                    _pendingLogTotal = arr.Length;
                    listBox1.BeginUpdate();
                    timerLoad.Enabled = true;
                }));
            });
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

        /// <summary>
        /// 定时分片加载日志行，避免长时间阻塞UI线程（包括父窗体Form1）
        /// </summary>
        private void timerLoad_Tick(object sender, EventArgs e)
        {
            if (_pendingLogArr == null || _pendingLogIdx >= _pendingLogTotal)
            {
                // 加载完成
                timerLoad.Enabled = false;
                if (listBox1.Items.Count > 0)
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                listBox1.EndUpdate();
                button1.Enabled = true;
                button1.Text = "打开";
                _pendingLogArr = null;
                return;
            }

            int end = Math.Min(_pendingLogIdx + LoadChunkSize, _pendingLogTotal);
            int count = end - _pendingLogIdx;
            string[] chunk = new string[count];
            Array.Copy(_pendingLogArr, _pendingLogIdx, chunk, 0, count);
            listBox1.Items.AddRange(chunk);
            _pendingLogIdx = end;

            // 更新按钮文字显示进度
            button1.Text = "加载中 " + (_pendingLogIdx * 100 / _pendingLogTotal) + "%";
        }
    }
}
