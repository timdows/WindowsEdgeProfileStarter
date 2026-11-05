using System;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SingleInstanceApplication.Run();
        }
    }
}
