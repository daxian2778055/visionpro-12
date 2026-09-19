using HslCommunication;
using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DEMO程序的一些静态变量信息
    /// </summary>
    public class DemoUtils
    {
        /// <summary>
        /// 统一的读取结果的数据解析，显示
        /// </summary>
        /// <typeparam name="T">类型对象</typeparam>
        /// <param name="result">读取的结果值</param>
        /// <param name="address">地址信息</param>
        /// <param name="textBox">输入的控件</param>
        public static void ReadResultRender<T>( OperateResult<T> result, string address, TextBox textBox )
        {
            if (result.IsSuccess)
            {
                textBox.AppendText( DateTime.Now.ToString( "[HH:mm:ss] " ) + "[" + address + "] " + result.Content + Environment.NewLine );
            }
            else
            {
                MessageBox.Show( DateTime.Now.ToString( "[HH:mm:ss] " ) + "[" + address + "] Read Failed " + Environment.NewLine + "Reason：" + result.ToMessageShowString( ) );
            }
        }
        public static void ReadResultRender1<T>(OperateResult<T> result, string address,out string textBox)
        {
            if (result.IsSuccess)
            {
                textBox="[" + address + "] " + result.Content + Environment.NewLine;
            }
            else
            {
                textBox =  "[" + address + "] Read Failed " + Environment.NewLine + "Reason：" + result.ToMessageShowString();
            }
        }

        /// <summary>
        /// 统一的数据写入的结果显示
        /// </summary>
        /// <param name="result">写入的结果信息</param>
        /// <param name="address">地址信息</param>
        public static void WriteResultRender( OperateResult result, string address )
        {
            if (result.IsSuccess)
            {
                MessageBox.Show( DateTime.Now.ToString( "[HH:mm:ss] " ) + "[" + address + "] Write Success" );
            }
            else
            {
                MessageBox.Show( DateTime.Now.ToString( "[HH:mm:ss] " ) + "[" + address + "] Write Failed " + Environment.NewLine + " Reason：" + result.ToMessageShowString( ) );
            }
        }

        /// <summary>
        /// 统一的数据写入的结果显示
        /// </summary>
        /// <param name="result">写入的结果信息</param>
        /// <param name="address">地址信息</param>
        public static void WriteResultRender( Func<OperateResult> write, string address )
        {
            try
            {
                OperateResult result = write( );
                if (result.IsSuccess)
                {
                    MessageBox.Show( DateTime.Now.ToString( "[HH:mm:ss] " ) + "[" + address + "] Write Success" );
                }
                else
                {
                    MessageBox.Show( DateTime.Now.ToString( "[HH:mm:ss] " ) + "[" + address + "] Write Failed " + Environment.NewLine + " Reason：" + result.ToMessageShowString( ) );
                }
            }
            catch(Exception ex)
            {
                // 主要是为了捕获写入的值不正确的情况
                MessageBox.Show( "Data for writting is not corrent: " + ex.Message );
            }
        }
        /// <summary>★H3：改为返回是否成功（原 void 只设 err 字符串、调用方无法得知成败，
        /// 导致 xie() 写失败也清槽、结果与完成字一次丢光）。现有调用点作为语句调用不受影响。</summary>
        public static bool WriteResultRender1(Func<OperateResult> write, string address,out string err)
        {
            try
            {
                OperateResult result = write();
                if (result.IsSuccess)
                {
                    err=DateTime.Now.ToString("[HH:mm:ss] ") + "[" + address + "] Write Success";
                    return true;
                }
                else
                {
                    err = DateTime.Now.ToString("[HH:mm:ss] ") + "[" + address + "] Write Failed " + Environment.NewLine + " Reason：" + result.ToMessageShowString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                // 主要是为了捕获写入的值不正确的情况
                err="Data for writting is not corrent: " + ex.Message;
                return false;
            }
        }
        public static void BulkReadRenderResult( HslCommunication.Core.IReadWriteNet readWrite, TextBox addTextBox, TextBox lengthTextBox, TextBox resultTextBox )
        {
            try
            {
                OperateResult<byte[]> read = readWrite.Read( addTextBox.Text, ushort.Parse( lengthTextBox.Text ) );
                if (read.IsSuccess)
                {
                    resultTextBox.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content );
                }
                else
                {
                    MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show( "Read Failed：" + ex.Message );
            }
        }
        public static readonly string IpAddressInputWrong = "IpAddress input wrong";
        public static readonly string PortInputWrong = "Port input wrong";
        public static readonly string SlotInputWrong = "Slot input wrong";
        public static readonly string BaudRateInputWrong = "Baud rate input wrong";
        public static readonly string DataBitsInputWrong = "Data bit input wrong";
        public static readonly string StopBitInputWrong = "Stop bit input wrong";
    }
}
