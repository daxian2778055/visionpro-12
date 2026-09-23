using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HslCommunication.Profinet;
using System.Threading;
using HslCommunication.Profinet.Melsec;
using HslCommunication;

namespace WindowsFormsApplication1
{
    public partial class FormMelsecSerial : Form
    {
        public FormMelsecSerial( )
        {
            InitializeComponent( );
            melsecSerial = new MelsecFxSerial( );
        }


        private MelsecFxSerial melsecSerial = null;
        public delegate void GetSeletionData(object Sender, SelectionChangedEventArgs e);
        public event GetSeletionData getData;
        public class SelectionChangedEventArgs : EventArgs
        {

            private string m_selection;



            //本属性用于传递事件数据

            public string Selection
            {

                get { return m_selection; }

            }
            public SelectionChangedEventArgs(string selection)
            {

                m_selection = selection;

            }
        }

        private void FormSiemens_Load( object sender, EventArgs e )
        {
            panel2.Enabled = false;
            comboBox1.SelectedIndex = 2;
            Program.Language = Settings1.Default.language;
            Language( Program.Language );
        }


        private void Language( int language )
        {
            if (language == 2)
            {
                Text = "Melsec Read PLC Demo";

                label1.Text = "parity:";
                label3.Text = "Stop bits";
                label27.Text = "Com:";
                label26.Text = "BaudRate";
                label25.Text = "Data bits";
                button1.Text = "Connect";
                button2.Text = "Disconnect";
                label21.Text = "Address:";
                label6.Text = "address:";
                label7.Text = "result:";

                button_read_bool.Text = "Read Bit";
                label23.Text = "X,Y,M,L,V,B";
                button_read_short.Text = "r-short";
                button_read_ushort.Text = "r-ushort";
                button_read_int.Text = "r-int";
                button_read_uint.Text = "r-uint";
                button_read_long.Text = "r-long";
                button_read_ulong.Text = "r-ulong";
                button_read_float.Text = "r-float";
                button_read_double.Text = "r-double";
                button_read_string.Text = "r-string";
                label8.Text = "length:";
                label11.Text = "Address:";
                label12.Text = "length:";
                button25.Text = "Bulk Read";
                label13.Text = "Results:";
                label16.Text = "Message:";
                label14.Text = "Results:";
                button26.Text = "Read";

                label10.Text = "Address:";
                label9.Text = "Value:";
                label19.Text = "Note: The value of the string needs to be converted";
                button24.Text = "Write Bit";
                button22.Text = "w-short";
                button21.Text = "w-ushort";
                button20.Text = "w-int";
                button19.Text = "w-uint";
                button18.Text = "w-long";
                button17.Text = "w-ulong";
                button16.Text = "w-float";
                button15.Text = "w-double";
                button14.Text = "w-string";

                groupBox1.Text = "Single Data Read test";
                groupBox2.Text = "Single Data Write test";
                groupBox3.Text = "Bulk Read test";
                groupBox4.Text = "Message reading test, hex string needs to be filled in";

                button3.Text = "Pressure test, r/w 3,000s";
                label24.Text = "X,Y,M,L,V,B";
                comboBox1.DataSource = new string[] { "None", "Odd", "Even" };
            }
        }

        private void FormSiemens_FormClosing( object sender, FormClosingEventArgs e )
        {
            if (e.CloseReason != CloseReason.UserClosing)
                return;
            e.Cancel = true;
            this.Visible = false;
        }
        
        #region Connect And Close

        
        private void button1_Click( object sender, EventArgs e )
        {
            int baudRate;
            if (!int.TryParse( textBox2.Text, out baudRate ))
            {
                MessageBox.Show( DemoUtils.BaudRateInputWrong );
                return;
            }
            int dataBits;
            if (!int.TryParse( textBox16.Text, out dataBits ))
            {
                MessageBox.Show( DemoUtils.DataBitsInputWrong );
                return;
            }
            int stopBits;
            if (!int.TryParse( textBox17.Text, out stopBits ))
            {
                MessageBox.Show( DemoUtils.StopBitInputWrong );
                return;
            }
            
            if (melsecSerial != null) melsecSerial.Close( );
            melsecSerial = new MelsecFxSerial( );
            
            try
            {
                melsecSerial.SerialPortInni( sp =>
                {
                    sp.PortName = textBox1.Text;
                    sp.BaudRate = baudRate;
                    sp.DataBits = dataBits;
                    sp.StopBits = stopBits == 0 ? System.IO.Ports.StopBits.None : (stopBits == 1 ? System.IO.Ports.StopBits.One : System.IO.Ports.StopBits.Two);
                    sp.Parity = comboBox1.SelectedIndex == 0 ? System.IO.Ports.Parity.None : (comboBox1.SelectedIndex == 1 ? System.IO.Ports.Parity.Odd : System.IO.Ports.Parity.Even);
                } );
                melsecSerial.Open( );

                button2.Enabled = true;
                button1.Enabled = false;
                panel2.Enabled = true;

                userControlCurve1.ReadWriteNet = melsecSerial;
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message );
            }
        }

        private void button2_Click( object sender, EventArgs e )
        {
            // 断开连接
            melsecSerial.Close( );
            button2.Enabled = false;
            button1.Enabled = true;
            panel2.Enabled = false;
        }

        

        #endregion

        #region 单数据读取测试


        private void button_read_bool_Click( object sender, EventArgs e )
        {
            // 读取bool变量
            DemoUtils.ReadResultRender( melsecSerial.ReadBool( textBox3.Text ), textBox3.Text, textBox4 );
        }

        private void button_read_short_Click( object sender, EventArgs e )
        {
            // 读取short变量
            DemoUtils.ReadResultRender( melsecSerial.ReadInt16( textBox3.Text ), textBox3.Text, textBox4 );
        }

        private void button_read_ushort_Click( object sender, EventArgs e )
        {
            // 读取ushort变量
            DemoUtils.ReadResultRender( melsecSerial.ReadUInt16( textBox3.Text ), textBox3.Text, textBox4 );
        }

        private void button_read_int_Click( object sender, EventArgs e )
        {
            // 读取int变量
            DemoUtils.ReadResultRender( melsecSerial.ReadInt32( textBox3.Text ), textBox3.Text, textBox4 );
        }
        private void button_read_uint_Click( object sender, EventArgs e )
        {
            // 读取uint变量
            DemoUtils.ReadResultRender( melsecSerial.ReadUInt32( textBox3.Text ), textBox3.Text, textBox4 );
        }
        private void button_read_long_Click( object sender, EventArgs e )
        {
            // 读取long变量
            DemoUtils.ReadResultRender( melsecSerial.ReadInt64( textBox3.Text ), textBox3.Text, textBox4 );
        }

        private void button_read_ulong_Click( object sender, EventArgs e )
        {
            // 读取ulong变量
            DemoUtils.ReadResultRender( melsecSerial.ReadUInt64( textBox3.Text ), textBox3.Text, textBox4 );
        }

        private void button_read_float_Click( object sender, EventArgs e )
        {
            // 读取float变量
            DemoUtils.ReadResultRender( melsecSerial.ReadFloat( textBox3.Text ), textBox3.Text, textBox4 );
        }

        private void button_read_double_Click( object sender, EventArgs e )
        {
            // 读取double变量
            DemoUtils.ReadResultRender( melsecSerial.ReadDouble( textBox3.Text ), textBox3.Text, textBox4 );
        }

        private void button_read_string_Click( object sender, EventArgs e )
        {
            // 读取字符串
            DemoUtils.ReadResultRender( melsecSerial.ReadString( textBox3.Text, ushort.Parse( textBox5.Text ) ), textBox3.Text, textBox4 );
        }


        #endregion

        #region 单数据写入测试


        private void button24_Click( object sender, EventArgs e )
        {
            // bool写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text,bool.Parse( textBox7.Text )), textBox8.Text );
        }

        private void button22_Click( object sender, EventArgs e )
        {
            // short写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, short.Parse( textBox7.Text ) ), textBox8.Text );
        }

        private void button21_Click( object sender, EventArgs e )
        {
            // ushort写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, ushort.Parse( textBox7.Text ) ), textBox8.Text );
        }


        private void button20_Click( object sender, EventArgs e )
        {
            // int写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, int.Parse( textBox7.Text ) ), textBox8.Text );
        }

        private void button19_Click( object sender, EventArgs e )
        {
            // uint写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, uint.Parse( textBox7.Text ) ), textBox8.Text );
        }

        private void button18_Click( object sender, EventArgs e )
        {
            // long写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, long.Parse( textBox7.Text ) ), textBox8.Text );
        }

        private void button17_Click( object sender, EventArgs e )
        {
            // ulong写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, ulong.Parse( textBox7.Text ) ), textBox8.Text );
        }

        private void button16_Click( object sender, EventArgs e )
        {
            // float写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, float.Parse( textBox7.Text ) ), textBox8.Text );
        }

        private void button15_Click( object sender, EventArgs e )
        {
            // double写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, double.Parse( textBox7.Text ) ), textBox8.Text );
        }


        private void button14_Click( object sender, EventArgs e )
        {
            // string写入
            DemoUtils.WriteResultRender( () => melsecSerial.Write( textBox8.Text, textBox7.Text ), textBox8.Text );
        }




        #endregion

        #region 批量读取测试

        private void button25_Click( object sender, EventArgs e )
        {
            DemoUtils.BulkReadRenderResult( melsecSerial, textBox6, textBox9, textBox10 );
        }


        #endregion

        #region 报文读取测试


        private void button26_Click( object sender, EventArgs e )
        {
            OperateResult<byte[]> read = melsecSerial.ReadBase( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) );
            if (read.IsSuccess)
            {
                textBox11.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content );
            }
            else
            {
                MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
            }
        }


        #endregion

        #region Use Exmaple

        private void test1()
        {
            // 如果我们想要读取M100-M109，我们可以按照如下的代码进行操作

            // if we want read M100-M109, so we can do like this
            OperateResult<bool[]> read = melsecSerial.ReadBool( "M100", 10 );
            if (read.IsSuccess)
            {
                bool m100 = read.Content[0];
                // and so on
                // ...
                // then
                bool m109 = read.Content[9];
            }
            else
            {
                // failed, the follow operation is output the wrong msg
                Console.WriteLine( "Read failed: " + read.ToMessageShowString( ) );
            }
        }


        private void test3()
        {
            // These are the underlying operations that ignore validation of success
            short d100_short = melsecSerial.ReadInt16( "D100" ).Content;
            ushort d100_ushort = melsecSerial.ReadUInt16( "D100" ).Content;
            int d100_int = melsecSerial.ReadInt32( "D100" ).Content;
            uint d100_uint = melsecSerial.ReadUInt32( "D100" ).Content;
            long d100_long = melsecSerial.ReadInt64( "D100" ).Content;
            ulong d100_ulong = melsecSerial.ReadUInt64( "D100" ).Content;
            float d100_float = melsecSerial.ReadFloat( "D100" ).Content;
            double d100_double = melsecSerial.ReadDouble( "D100" ).Content;
            // need to specify the text length
            string d100_string = melsecSerial.ReadString( "D100", 10 ).Content;
        }
        private void test4()
        {
            // These are the underlying operations that ignore validation of success
            melsecSerial.Write( "D100", (short)5 );
            melsecSerial.Write( "D100", (ushort)5 );
            melsecSerial.Write( "D100", 5 );
            melsecSerial.Write( "D100", (uint)5 );
            melsecSerial.Write( "D100", (long)5 );
            melsecSerial.Write( "D100", (ulong)5 );
            melsecSerial.Write( "D100", 5f );
            melsecSerial.Write( "D100", 5d );
            // length should Multiples of 2 
            melsecSerial.Write( "D100", "12345678" );
        }


        private void test5()
        {
            // The complex situation is that you need to parse the byte array yourself.
            // Here's just one example.
            OperateResult<byte[]> read = melsecSerial.Read( "D100", 10 );
            if (read.IsSuccess)
            {
                int count = melsecSerial.ByteTransform.TransInt32( read.Content, 0 );
                float temp = melsecSerial.ByteTransform.TransSingle( read.Content, 4 );
                short name1 = melsecSerial.ByteTransform.TransInt16( read.Content, 8 );
                string barcode = Encoding.ASCII.GetString( read.Content, 10, 10 );
            }
        }
        public class UserType : HslCommunication.IDataTransfer
        {
            #region IDataTransfer

            private HslCommunication.Core.IByteTransform ByteTransform = new HslCommunication.Core.RegularByteTransform();


            public ushort ReadCount { get { return 10; } }

            public void ParseSource(byte[] Content)
            {
                int count = ByteTransform.TransInt32(Content, 0);
                float temp = ByteTransform.TransSingle(Content, 4);
                short name1 = ByteTransform.TransInt16(Content, 8);
                string barcode = Encoding.ASCII.GetString(Content, 10, 10);
            }

            public byte[] ToSource()
            {
                byte[] buffer = new byte[20];
                ByteTransform.TransByte(count).CopyTo(buffer, 0);
                ByteTransform.TransByte(temp).CopyTo(buffer, 4);
                ByteTransform.TransByte(name1).CopyTo(buffer, 8);
                Encoding.ASCII.GetBytes(barcode).CopyTo(buffer, 10);
                return buffer;
            }


            #endregion


            #region Public Data

            public int count { get; set; }

            public float temp { get; set; }

            public short name1 { get; set; }

            public string barcode { get; set; }

            #endregion
        }
        private void test6()
        {
            // Custom types of Read and write situations in which type usertype need to be implemented in advance.
            // 自定义类型的读写的示例，前提是需要提前实现UserType类，做好相应的序列化，反序列化的操作

            OperateResult<UserType> read = melsecSerial.ReadCustomer<UserType>( "D100" );
            if (read.IsSuccess)
            {
                UserType value = read.Content;
            }
            // write value
            melsecSerial.WriteCustomer( "D100", new UserType( ) );

            // Sets an instance operation for the log.
            melsecSerial.LogNet = new HslCommunication.LogNet.LogNetSingle( Application.StartupPath + "\\Logs.txt" );
        }

        #endregion

        #region 压力测试

        private int thread_status = 0;
        private int failed = 0;
        private DateTime thread_time_start = DateTime.Now;
        // 压力测试，开3个线程，每个线程进行读写操作，看使用时间
        private void button3_Click( object sender, EventArgs e )
        {
            // ★第26轮#31（与 FormModbus 压力测试同构站点）：
            //   原实现三线程 500 轮直打串口，无任何门控/异常保护——
            //   ①端口未开或已被"断开"按钮关掉时，后台线程内 Hsl 抛异常 = 未处理线程异常直接杀进程；
            //   ②压测期间关窗，thread_end 的裸 Invoke 抛 InvalidOperationException 同样杀进程；
            //   ③failed 由 3 个线程共享，普通 ++ 互相覆盖，报告的失败数不可信。
            //   本窗体没有与 FormModbus 等价的 _ioSync（曲线控件 userControlCurve1 也在同一串口上定时读写），
            //   故此处只做"知情确认 + 让出 + 不崩"三件事，不做假承诺的互斥。
            if (!button2.Enabled || melsecSerial == null)
            {
                MessageBox.Show( "串口未打开，无法开始压力测试。", "提示" );
                return;
            }
            if (MessageBox.Show(
                    "压力测试会向地址【D100】连续写入固定值 1234 共 1500 次（3 线程 × 500 轮），"
                    + "将覆盖该地址在生产 PLC 中的真实数据，且期间串口被大量占用。\n\n确认该地址当前可被随意覆盖吗？",
                    "压力测试确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning ) != DialogResult.Yes )
                return;
            thread_status = 3;
            failed = 0;
            thread_time_start = DateTime.Now;
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            new Thread( new ThreadStart( thread_test2 ) ) { IsBackground = true, }.Start( );
            button3.Enabled = false;
        }

        private void thread_test2( )
        {
            int count = 500;
            try
            {
                while (count > 0)
                {
                    bool okW = melsecSerial.Write( "D100", (short)1234 ).IsSuccess;
                    bool okR = melsecSerial.ReadInt16( "D100" ).IsSuccess;
                    // ★#31：口径同原版（写/读失败各计一次），但改为 Interlocked 防三线程互相覆盖
                    if (!okW) Interlocked.Increment( ref failed );
                    if (!okR) Interlocked.Increment( ref failed );
                    Thread.Sleep( 1 );   // ★#31：让出串口，避免 1500 次连打把曲线控件/手动读写饿死
                    count--;
                }
            }
            catch { Interlocked.Increment( ref failed ); }   // ★#31：单轮异常只记失败，绝不让后台线程带未处理异常杀进程
            thread_end( );
        }

        private void thread_end( )
        {
            if (Interlocked.Decrement( ref thread_status ) == 0)
            {
                // ★#31：压测期间窗体可能已关闭/释放——原裸 Invoke 会抛 InvalidOperationException（后台线程未处理=杀进程）
                try
                {
                    if (IsHandleCreated && !IsDisposed)
                        Invoke( new Action( ( ) =>
                        {
                            button3.Enabled = true;
                            MessageBox.Show( "Spend：" + (DateTime.Now - thread_time_start).TotalSeconds + Environment.NewLine + " Failed Count：" + failed );
                        } ) );
                }
                catch { }
            }
        }




        #endregion
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
    }
}
