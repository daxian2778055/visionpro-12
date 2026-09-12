using System;
using System.Windows.Forms;

namespace GrantIniAccess
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ★ 全局异常兜底：避免未处理异常直接导致进程崩溃且无任何提示
            Application.ThreadException += (sender, e) =>
                MessageBox.Show("程序发生未处理错误：\r\n" + e.Exception.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show("程序发生严重错误：\r\n" + (ex != null ? ex.Message : Convert.ToString(e.ExceptionObject)),
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }
    }
}
