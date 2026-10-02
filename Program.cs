using System;
using System.Windows.Forms;

namespace FocusLock
{
    /// <summary>
    /// 程序入口
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // 启用视觉样式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 运行主界面
            Application.Run(new MainForm());
        }
    }
}
