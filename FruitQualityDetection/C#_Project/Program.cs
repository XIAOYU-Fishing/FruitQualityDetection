using System;
using System.Windows.Forms;
using FruitQualityDetection.UI;

namespace FruitQualityDetection
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // 捕获所有未处理异常
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = (Exception)e.ExceptionObject;
                MessageBox.Show($"未处理异常:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                    "致命错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show($"线程异常:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", 
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"程序启动失败:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                    "致命错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}