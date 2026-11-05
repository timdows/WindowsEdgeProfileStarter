using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeProfileStarter
{
    internal sealed class EdgeLauncher
    {
        private readonly Func<HashSet<IntPtr>> snapshot;
        private readonly Func<IntPtr, bool> maximize;
        private readonly Action<ProcessStartInfo> start;

        internal EdgeLauncher() : this(BrowserWindows.Snapshot, BrowserWindows.Maximize, StartProcess) { }

        internal EdgeLauncher(Func<HashSet<IntPtr>> snapshot, Func<IntPtr, bool> maximize, Action<ProcessStartInfo> start)
        {
            this.snapshot = snapshot;
            this.maximize = maximize;
            this.start = start;
        }

        internal static string ProfilePath(string directory)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", directory);
        }

        internal async Task LaunchAsync(string executable, string directory, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            HashSet<IntPtr> existing = snapshot();
            string arguments = "--profile-directory=\"" + directory + "\" --new-window --start-maximized";
            start(new ProcessStartInfo(executable, arguments) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Maximized });
            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.Elapsed < TimeSpan.FromSeconds(15))
            {
                await Task.Delay(150, cancellation);
                foreach (IntPtr window in snapshot())
                    if (!existing.Contains(window) && maximize(window)) return;
            }
            throw new TimeoutException("Er is binnen 15 seconden geen nieuw gemaximaliseerd Edge-venster gevonden. Probeer het opnieuw.");
        }

        internal static string FindExecutable()
        {
            string[] roots = {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };
            foreach (string root in roots)
            {
                string candidate = Path.Combine(root, "Microsoft", "Edge", "Application", "msedge.exe");
                if (File.Exists(candidate)) return candidate;
            }
            return "msedge.exe";
        }

        private static void StartProcess(ProcessStartInfo info)
        {
            // Edge can forward the request to another process; window detection handles that.
            using (Process process = Process.Start(info)) { }
        }
    }
}
