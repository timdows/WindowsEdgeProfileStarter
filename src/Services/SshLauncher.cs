using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace EdgeProfileStarter
{
    internal sealed class SshLauncher
    {
        private readonly Action<ProcessStartInfo> start;
        private readonly Func<string> findExecutable;
        internal SshLauncher() : this(StartProcess, FindExecutable) { }
        internal SshLauncher(Action<ProcessStartInfo> start, Func<string> findExecutable)
        {
            this.start = start;
            this.findExecutable = findExecutable;
        }

        internal static void Validate(SshSession session)
        {
            if (session == null || String.IsNullOrWhiteSpace(session.name) || String.IsNullOrWhiteSpace(session.host))
                throw new InvalidDataException("Elke SSH-sessie moet een naam en host hebben.");
            if (session.host[0] == '-') throw new InvalidDataException("Een SSH-host mag niet met een optie beginnen.");
            foreach (char c in session.host)
                if (Char.IsWhiteSpace(c) || Char.IsControl(c) || "\"\\/@".IndexOf(c) >= 0)
                    throw new InvalidDataException("Ongeldige SSH-host: geef alleen de hostnaam of het IP-adres op.");
            if (session.port.HasValue && (session.port < 1 || session.port > 65535))
                throw new InvalidDataException("Een SSH-poort moet tussen 1 en 65535 liggen.");
            if (session.username != null)
                foreach (char c in session.username)
                    if (Char.IsControl(c) || c == '"')
                        throw new InvalidDataException("De SSH-gebruikersnaam mag geen besturingstekens of dubbele aanhalingstekens bevatten.");
        }

        internal void Launch(SshSession session)
        {
            Validate(session);
            string arguments = "";
            if (session.port.HasValue) arguments += "-p " + session.port.Value.ToString(CultureInfo.InvariantCulture) + " ";
            if (!String.IsNullOrWhiteSpace(session.username)) arguments += "-l " + Quote(session.username) + " ";
            arguments += "-- " + Quote(session.host);
            // Shell execution of this console executable creates an interactive terminal.
            start(new ProcessStartInfo(findExecutable(), arguments) { UseShellExecute = true, CreateNoWindow = false });
        }

        private static string Quote(string value)
        {
            int trailing = value.Length - value.TrimEnd('\\').Length;
            return "\"" + value + new string('\\', trailing) + "\"";
        }

        internal static string FindExecutable()
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                string native = Path.Combine(windows, "Sysnative", "OpenSSH", "ssh.exe");
                if (File.Exists(native)) return native;
            }
            string executable = Path.Combine(windows, "System32", "OpenSSH", "ssh.exe");
            if (File.Exists(executable)) return executable;
            throw new FileNotFoundException("Windows OpenSSH Client is niet gevonden. Installeer OpenSSH Client via de optionele Windows-onderdelen.");
        }

        private static void StartProcess(ProcessStartInfo info)
        {
            using (Process process = Process.Start(info)) { }
        }
    }
}
