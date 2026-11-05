using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal static class CaptureScreenshot
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var window = new Launcher(new ConfigurationStore(args[0])))
            {
                window.ShowInTaskbar = false;
                window.Show();
                Application.DoEvents();
                Theme.Dark = true;
                Theme.Apply(window);
                foreach (Control control in window.Controls)
                    foreach (Control child in control.Controls)
                        if (child.AccessibleName == "Donkere modus wisselen") child.Text = "Lichte modus";
                window.Refresh();
                using (var image = new Bitmap(window.Width, window.Height))
                {
                    window.DrawToBitmap(image, window.ClientRectangle);
                    image.Save(args[1], ImageFormat.Png);
                }
                window.Close();
            }
        }
    }
}
