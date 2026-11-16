using System;
using System.Diagnostics;
using System.IO;

namespace EdgeProfileStarter
{
    internal sealed class ShareLauncher
    {
        private readonly Action<ProcessStartInfo> start;
        internal ShareLauncher() : this(StartProcess) { }
        internal ShareLauncher(Action<ProcessStartInfo> start) { this.start = start; }

        internal static void Validate(NetworkShare share)
        {
            if (share == null || String.IsNullOrWhiteSpace(share.name) || String.IsNullOrWhiteSpace(share.path))
                throw new InvalidDataException("Elke share moet een naam en UNC-pad hebben.");
            string path = share.path;
            if (!path.StartsWith(@"\\", StringComparison.Ordinal) || path.Length < 5 || path[2] == '\\')
                throw new InvalidDataException(@"Gebruik een UNC-pad zoals \\server\share.");
            foreach (char c in path)
                if (Char.IsControl(c) || "<>:\"|?*/".IndexOf(c) >= 0)
                    throw new InvalidDataException("Ongeldig teken in het share-pad.");
            string[] parts = path.Substring(2).TrimEnd('\\').Split('\\');
            if (parts.Length < 2) throw new InvalidDataException("Geef zowel de server als de sharenaam op.");
            foreach (string part in parts)
                if (String.IsNullOrWhiteSpace(part) || part == "." || part == "..")
                    throw new InvalidDataException("Ongeldig onderdeel in het share-pad.");
        }

        internal void Launch(NetworkShare share)
        {
            Validate(share);
            // Do not probe the network here: Explorer handles unavailable hosts and sign-in.
            string path = share.path.TrimEnd('\\');
            string executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            start(new ProcessStartInfo(executable, "\"" + path + "\"") { UseShellExecute = true });
        }

        private static void StartProcess(ProcessStartInfo info)
        {
            using (Process process = Process.Start(info)) { }
        }
    }
}
