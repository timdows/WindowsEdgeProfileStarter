using System;
using System.Diagnostics;
using System.IO;

namespace EdgeProfileStarter
{
    internal sealed class VncLauncher
    {
        private readonly Action<ProcessStartInfo> start;

        internal VncLauncher() : this(StartProcess) { }
        internal VncLauncher(Action<ProcessStartInfo> start) { this.start = start; }

        internal static void Validate(VncSession session)
        {
            if (session == null || String.IsNullOrWhiteSpace(session.name) || String.IsNullOrWhiteSpace(session.host))
                throw new InvalidDataException("Elke UltraVNC-sessie moet een naam en host hebben.");
            if (session.host[0] == '-') throw new InvalidDataException("Een UltraVNC-host mag niet met een optie beginnen.");
            foreach (char c in session.host)
                if (Char.IsWhiteSpace(c) || Char.IsControl(c) || c == '"' || c == '/' || c == '\\')
                    throw new InvalidDataException("Ongeldige UltraVNC-host: gebruik een hostnaam of IP-adres, eventueel met ::poort.");
            if (session.username != null)
                foreach (char c in session.username)
                    if (Char.IsControl(c) || c == '"')
                        throw new InvalidDataException("De UltraVNC-gebruikersnaam mag geen besturingstekens of dubbele aanhalingstekens bevatten.");
        }

        internal void Launch(VncSession session, string viewerPath)
        {
            Validate(session);
            string executable = FindViewer(viewerPath);
            string arguments = "-connect " + session.host;
            if (!String.IsNullOrWhiteSpace(session.username))
            {
                // Quote as a single Windows argument; double trailing backslashes
                // so they cannot escape the closing quote.
                string value = session.username;
                int trailing = value.Length - value.TrimEnd('\\').Length;
                arguments += " -user \"" + value + new string('\\', trailing) + "\"";
            }
            start(new ProcessStartInfo(executable, arguments) {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executable)
            });
        }

        internal static string FindViewer(string configuredPath)
        {
            if (!String.IsNullOrWhiteSpace(configuredPath))
            {
                string expanded = Environment.ExpandEnvironmentVariables(configuredPath);
                string resolved = Path.GetFullPath(Path.IsPathRooted(expanded) ? expanded : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, expanded));
                if (File.Exists(resolved)) return resolved;
                throw new FileNotFoundException("UltraVNC Viewer is niet gevonden. Controleer ultraVncViewerPath in config.json.", resolved);
            }
            string[] roots = {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            foreach (string root in roots)
                foreach (string folder in new[] { @"uvnc bvba\UltraVNC", @"uvnc\UltraVNC", "UltraVNC" })
                {
                    string candidate = Path.Combine(root, folder, "vncviewer.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vncviewer.exe");
            if (File.Exists(local)) return local;
            foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                if (String.IsNullOrWhiteSpace(folder)) continue;
                string candidate = Path.Combine(Environment.ExpandEnvironmentVariables(folder.Trim().Trim('"')), "vncviewer.exe");
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }
            throw new FileNotFoundException("UltraVNC Viewer is niet gevonden. Installeer de viewer of stel ultraVncViewerPath in config.json in.");
        }

        private static void StartProcess(ProcessStartInfo info)
        {
            using (Process process = Process.Start(info)) { }
        }
    }
}
