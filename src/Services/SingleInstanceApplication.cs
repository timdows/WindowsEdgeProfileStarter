using System;
using System.Threading;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal static class SingleInstanceApplication
    {
        internal static void Run()
        {
            string scope = "Local\\EdgeProfileStarter." + System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            bool first;
            using (EventWaitHandle activate = new EventWaitHandle(false, EventResetMode.AutoReset, scope + ".Activate"))
            using (Mutex mutex = new Mutex(true, scope, out first))
            {
                if (!first) { activate.Set(); return; }
                try
                {
                    using (Launcher window = new Launcher())
                    using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 150 })
                    {
                        timer.Tick += delegate {
                            if (activate.WaitOne(0)) {
                                if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
                                window.Show();
                                window.Activate();
                            }
                        };
                        timer.Start();
                        Application.Run(window);
                    }
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
