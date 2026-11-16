using System;
using System.Diagnostics;
using System.IO;

namespace EdgeProfileStarter
{
    internal sealed class RdpLauncher
    {
        private readonly Action<ProcessStartInfo> start;

        internal RdpLauncher() : this(StartProcess) { }
        internal RdpLauncher(Action<ProcessStartInfo> start) { this.start = start; }

        internal static void Validate(RdpSession session)
        {
            if (session == null || String.IsNullOrWhiteSpace(session.name) || String.IsNullOrWhiteSpace(session.host))
                throw new InvalidDataException("Elke RDP-sessie moet een naam en host hebben.");
            foreach (char c in session.host)
                if (Char.IsWhiteSpace(c) || Char.IsControl(c) || c == '"' || c == '/' || c == '\\')
                    throw new InvalidDataException("Ongeldige RDP-host: gebruik een hostnaam of IP-adres, eventueel met poort.");
        }

        internal void Launch(RdpSession session, DisplayMode mode = DisplayMode.Default)
        {
            Validate(session);
            string executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "mstsc.exe");
            start(new ProcessStartInfo(executable, "/v:" + session.host + DisplayOptions.ForRdp(mode)) { UseShellExecute = true });
        }

        private static void StartProcess(ProcessStartInfo info)
        {
            using (Process process = Process.Start(info)) { }
        }
    }
}
