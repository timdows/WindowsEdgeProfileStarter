using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EdgeProfileStarter
{
    internal static class BrowserWindows
    {
        private delegate bool EnumWindowProc(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int count);
        [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr window);

        internal static HashSet<IntPtr> Snapshot()
        {
            HashSet<IntPtr> windows = new HashSet<IntPtr>();
            EnumWindows(delegate(IntPtr window, IntPtr parameter) {
                if (!IsWindowVisible(window)) return true;
                StringBuilder name = new StringBuilder(128);
                GetClassName(window, name, name.Capacity);
                if (name.ToString() != "Chrome_WidgetWin_1") return true;
                uint pid;
                GetWindowThreadProcessId(window, out pid);
                try {
                    using (Process process = Process.GetProcessById((int)pid))
                        if (String.Equals(process.ProcessName, "msedge", StringComparison.OrdinalIgnoreCase)) windows.Add(window);
                }
                catch (ArgumentException) { }
                catch (System.ComponentModel.Win32Exception) { }
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        internal static bool Maximize(IntPtr window)
        {
            ShowWindowAsync(window, 3); // SW_MAXIMIZE, also works with an existing Edge process.
            return IsZoomed(window);
        }
    }
}
