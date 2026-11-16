using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal static class SmokeTests
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                ConfigurationRoundTrip();
                RdpStartup();
                VncStartup();
                DisplayModes();
                ShareStartup();
                SshStartup();
                SynchronizationContext.SetSynchronizationContext(null);
                BrowserStartup().GetAwaiter().GetResult();
                Console.WriteLine("OK: configuration, validation, theme persistence, form lifecycle, browser window selection, cancellation and RDP/UltraVNC/share/SSH connections.");
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void ConfigurationRoundTrip()
        {
            string path = Path.Combine(Path.GetTempPath(), "EdgeProfileStarter-test-" + Guid.NewGuid().ToString("N") + ".json");
            var serializer = new JavaScriptSerializer();
            var config = new Configuration {
                windowTitle = "Testvenster", darkMode = false,
                profiles = new List<Profile> { new Profile { directory = "Default", name = "Werk \"A\" \\ thuis" } },
                rdpSessions = new List<RdpSession> { new RdpSession { name = "Server", host = "server.example:3390" } },
                vncSessions = new List<VncSession> { new VncSession { name = "VNC server", host = "vnc.example::5901", username = @"DOMEIN\tim" } },
                ultraVncViewerPath = @"C:\Tools\UltraVNC\vncviewer.exe",
                shares = new List<NetworkShare> { new NetworkShare { name = "FTP", path = @"\\192.168.1.253\ftp" } },
                sshSessions = new List<SshSession> { new SshSession { name = "Linux", host = "linux.example", username = "admin", port = 2222 } }
            };
            var values = serializer.Deserialize<Dictionary<string, object>>(serializer.Serialize(config));
            values["extra"] = new object[] { "quotes: \" \\ {} []", new Dictionary<string, object>() };
            try
            {
                File.WriteAllText(path, serializer.Serialize(values));
                var store = new ConfigurationStore(path);
                store.SetDarkMode(true);
                Configuration loaded = store.Load();
                Assert(loaded.darkMode && loaded.profiles[0].name == config.profiles[0].name, "Theme save changed profiles.");
                Assert(loaded.vncSessions.Count == 1 && loaded.vncSessions[0].host == "vnc.example::5901" && loaded.ultraVncViewerPath == config.ultraVncViewerPath && loaded.vncSessions[0].username == @"DOMEIN\tim", "Theme save changed VNC configuration.");
                Assert(loaded.shares.Count == 1 && loaded.shares[0].path == @"\\192.168.1.253\ftp", "Theme save changed shares.");
                Assert(loaded.sshSessions.Count == 1 && loaded.sshSessions[0].username == "admin" && loaded.sshSessions[0].port == 2222, "Theme save changed SSH configuration.");
                Assert(serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path)).ContainsKey("extra"), "Extra fields lost.");
                using (var form = new Launcher(store))
                {
                    form.Show();
                    Application.DoEvents();
                    Assert(form.Text == "Testvenster" && Theme.Dark, "Form failed to load configuration.");
                    Assert(form.FormBorderStyle == FormBorderStyle.None && !form.MaximizeBox, "Window chrome changed.");
                    form.Close();
                } // Closing and disposing again must be safe.
                foreach (string invalid in new[] { "null", "{}", "{\"profiles\":[]}",
                    "{\"profiles\":[{\"name\":\"X\",\"directory\":\"../bad\"}]}",
                    "{\"profiles\":[{\"name\":\"X\",\"directory\":\"Default\"},{\"name\":\"Y\",\"directory\":\"Default\"}]}" })
                {
                    File.WriteAllText(path, invalid);
                    bool rejected = false;
                    try { store.SetDarkMode(false); }
                    catch (InvalidDataException) { rejected = true; }
                    Assert(rejected && File.ReadAllText(path) == invalid, "Invalid configuration was overwritten.");
                }
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        private static void RdpStartup()
        {
            ProcessStartInfo command = null;
            int calls = 0;
            var launcher = new RdpLauncher(delegate(ProcessStartInfo info) { command = info; calls++; });
            foreach (string host in new[] { "server.example", "192.168.1.20:3390", "[2001:db8::1]:3389" })
            {
                launcher.Launch(new RdpSession { name = "Server", host = host });
                Assert(command != null && Path.GetFileName(command.FileName) == "mstsc.exe", "RDP must start mstsc.");
                Assert(command.Arguments == "/v:" + host && command.UseShellExecute, "RDP must connect directly to the configured host.");
            }
            int previousCalls = calls;
            foreach (string host in new[] { "bad\r\n/v:other", "", "server /admin", "server\"name", "server\\path", "server/name" })
            {
                bool rejected = false;
                try { launcher.Launch(new RdpSession { name = "Bad", host = host }); }
                catch (InvalidDataException) { rejected = true; }
                Assert(rejected, "Unsafe RDP host accepted.");
            }
            Assert(calls == previousCalls, "Invalid host started a process.");
            // An old username field must not prevent existing configurations from loading.
            var session = new JavaScriptSerializer().Deserialize<RdpSession>("{\"name\":\"Old\",\"host\":\"server\",\"username\":\"ignored\"}");
            launcher.Launch(session);
            Assert(command.Arguments == "/v:server", "Legacy username affected direct startup.");
        }
        private static void VncStartup()
        {
            string file = Path.Combine(Path.GetTempPath(), "UltraVNC test " + Guid.NewGuid().ToString("N") + ".exe");
            File.WriteAllText(file, "test placeholder; never executed");
            ProcessStartInfo command = null;
            int calls = 0;
            var launcher = new VncLauncher(delegate(ProcessStartInfo info) { command = info; calls++; });
            try
            {
                foreach (string host in new[] { "server.example", "192.168.1.20::5901", "server:1" })
                {
                    launcher.Launch(new VncSession { name = "Server", host = host }, file);
                    Assert(command.FileName == file && command.Arguments == "-connect " + host, "UltraVNC command changed.");
                    Assert(command.UseShellExecute && command.WorkingDirectory == Path.GetDirectoryName(file), "Viewer launch directory lost.");
                }
                foreach (string username in new[] { @"DOMEIN\tim", "tim@example.com", "Tim Example", "account\\" })
                {
                    launcher.Launch(new VncSession { name = "Server", host = "server", username = username }, file);
                    string quoted = "\"" + username + (username.EndsWith("\\") ? "\\" : "") + "\"";
                    Assert(command.Arguments == "-connect server -user " + quoted, "VNC username was not passed as one argument.");
                }
                foreach (string username in new[] { "", "   ", null })
                {
                    launcher.Launch(new VncSession { name = "Server", host = "server", username = username }, file);
                    Assert(command.Arguments == "-connect server", "Empty VNC username should be omitted.");
                }
                foreach (string username in new[] { "bad\r\n-user other", "bad\" -listen", "bad\0name" })
                {
                    int before = calls;
                    bool rejected = false;
                    try { launcher.Launch(new VncSession { name = "Server", host = "server", username = username }, file); }
                    catch (InvalidDataException) { rejected = true; }
                    Assert(rejected && calls == before, "Invalid VNC username started a process.");
                }
                int previous = calls;
                foreach (string host in new[] { "", "bad\n-connect other", "server -listen", "-listen", "server\"name", "server/path" })
                {
                    bool rejected = false;
                    try { launcher.Launch(new VncSession { name = "Bad", host = host }, file); }
                    catch (InvalidDataException) { rejected = true; }
                    Assert(rejected, "Unsafe VNC host accepted.");
                }
                bool missing = false;
                try { launcher.Launch(new VncSession { name = "Server", host = "server" }, file + ".missing"); }
                catch (FileNotFoundException) { missing = true; }
                Assert(missing && calls == previous, "Missing viewer or invalid host started a process.");
            }
            finally { File.Delete(file); }
        }
        private static void DisplayModes()
        {
            ProcessStartInfo command = null;
            var rdp = new RdpLauncher(delegate(ProcessStartInfo info) { command = info; });
            var session = new RdpSession { name = "RDP", host = "server:3390" };
            var modes = new[] { DisplayMode.FullScreen, DisplayMode.AllMonitors, DisplayMode.Window1280 };
            var expected = new[] { " /f", " /f /multimon", " /w:1280 /h:1024" };
            for (int i = 0; i < modes.Length; i++)
            {
                rdp.Launch(session, modes[i]);
                Assert(command.Arguments == "/v:server:3390" + expected[i], "RDP display mode not passed.");
            }
            Assert(DisplayOptions.ForVnc(DisplayMode.AllMonitors, new System.Drawing.Rectangle(-1920, -1080, 3840, 2160)) == " -noborder -notoolbar -autoscaling -position -1920 -1080 3840 2160", "Multi-screen coordinates lost.");
            string file = Path.Combine(Path.GetTempPath(), "VNC-display-test-" + Guid.NewGuid().ToString("N") + ".exe");
            File.WriteAllText(file, "test placeholder; never executed");
            try
            {
                var vnc = new VncLauncher(delegate(ProcessStartInfo info) { command = info; });
                foreach (DisplayMode mode in modes)
                {
                    vnc.Launch(new VncSession { name = "VNC", host = "server", username = @"DOMAIN\tim" }, file, mode);
                    Assert(command.Arguments == "-connect server -user \"DOMAIN\\tim\"" + DisplayOptions.ForVnc(mode, SystemInformation.VirtualScreen), "VNC display mode or username lost.");
                }
            }
            finally { File.Delete(file); }
            DisplayMode selected = DisplayMode.Default;
            using (var card = new ConnectionCard("Test", "RDP", 0, "Test", delegate(DisplayMode mode) { selected = mode; }))
            {
                card.CreateControl();
                ComboBox combo = FindControl<ComboBox>(card);
                Button button = FindControl<Button>(card);
                for (int i = 0; i < modes.Length; i++)
                {
                    combo.SelectedIndex = i;
                    button.PerformClick();
                    Assert(selected == modes[i], "Frontend selection not used at launch.");
                }
            }
        }

        private static T FindControl<T>(Control parent) where T : Control
        {
            foreach (Control child in parent.Controls)
            {
                T result = child as T;
                if (result != null) return result;
                result = FindControl<T>(child);
                if (result != null) return result;
            }
            return null;
        }
        private static void ShareStartup()
        {
            ProcessStartInfo command = null;
            int calls = 0;
            var launcher = new ShareLauncher(delegate(ProcessStartInfo info) { command = info; calls++; });
            foreach (string path in new[] { @"\\192.168.1.253\ftp", @"\\server\Team Files\Sub Folder", @"\\server\share\", @"\\server\C$" })
            {
                launcher.Launch(new NetworkShare { name = "Share", path = path });
                Assert(Path.GetFileName(command.FileName) == "explorer.exe" && command.UseShellExecute, "Share must open Explorer.");
                Assert(command.Arguments == "\"" + path.TrimEnd('\\') + "\"", "UNC path must be one correctly quoted argument.");
            }
            int previous = calls;
            foreach (string path in new[] { "", @"C:\folder", @"\server\share", @"\\\server\share", @"\\server", @"\\.\C$", @"\\?\C:\folder", "\\\\server\\share\r\n", "\\\\server\\share\" /select", @"\\server\share\..\other", "https://example.com" })
            {
                bool rejected = false;
                try { launcher.Launch(new NetworkShare { name = "Bad", path = path }); }
                catch (InvalidDataException) { rejected = true; }
                Assert(rejected, "Invalid share accepted.");
            }
            Assert(calls == previous, "Invalid share started Explorer.");
        }
        private static void SshStartup()
        {
            ProcessStartInfo command = null;
            int calls = 0;
            var launcher = new SshLauncher(delegate(ProcessStartInfo info) { command = info; calls++; }, delegate { return @"C:\Windows\System32\OpenSSH\ssh.exe"; });
            launcher.Launch(new SshSession { name = "Linux", host = "linux.example", username = "admin", port = 2222 });
            Assert(command.Arguments == "-p 2222 -l \"admin\" -- \"linux.example\"", "SSH arguments changed.");
            Assert(command.FileName == @"C:\Windows\System32\OpenSSH\ssh.exe" && command.UseShellExecute && !command.CreateNoWindow, "SSH must open an interactive Windows SSH console.");
            launcher.Launch(new SshSession { name = "Default", host = "my-ssh-alias" });
            Assert(command.Arguments == "-- \"my-ssh-alias\"", "Optional SSH fields must be omitted for SSH config defaults.");
            launcher.Launch(new SshSession { name = "IPv6", host = "2001:db8::1", username = @"DOMAIN\tim" });
            Assert(command.Arguments == "-l \"DOMAIN\\tim\" -- \"2001:db8::1\"", "IPv6 or domain username lost.");
            int before = calls;
            foreach (var invalid in new[] {
                new SshSession { name = "Bad", host = "-oProxyCommand=bad" },
                new SshSession { name = "Bad", host = "server command" },
                new SshSession { name = "Bad", host = "server\"" },
                new SshSession { name = "Bad", host = "user@server" },
                new SshSession { name = "Bad", host = "server", port = 0 },
                new SshSession { name = "Bad", host = "server", port = 65536 },
                new SshSession { name = "Bad", host = "server", username = "user\r\nother" }
            })
            {
                bool rejected = false;
                try { launcher.Launch(invalid); }
                catch (InvalidDataException) { rejected = true; }
                Assert(rejected, "Invalid SSH session accepted.");
            }
            Assert(calls == before, "Invalid SSH input started a process.");
            bool missing = false;
            launcher = new SshLauncher(delegate { throw new Exception("Must not start."); }, delegate { throw new FileNotFoundException(); });
            try { launcher.Launch(new SshSession { name = "Missing client", host = "server" }); }
            catch (FileNotFoundException) { missing = true; }
            Assert(missing, "Missing Windows SSH client error lost.");
        }
        private static async Task BrowserStartup()
        {
            IntPtr oldWindow = new IntPtr(1), newWindow = new IntPtr(2);
            bool started = false;
            int attempts = 0;
            var launcher = new EdgeLauncher(
                delegate { return started ? new HashSet<IntPtr> { oldWindow, newWindow } : new HashSet<IntPtr> { oldWindow }; },
                delegate(IntPtr window) { Assert(window == newWindow, "Existing browser window changed."); return ++attempts == 2; },
                delegate(ProcessStartInfo info) {
                    Assert(info.Arguments == "--profile-directory=\"Profile 3\" --new-window --start-maximized", "Browser arguments changed.");
                    started = true;
                });
            await launcher.LaunchAsync("test-edge.exe", "Profile 3", CancellationToken.None);
            Assert(started && attempts == 2, "Did not wait for confirmed maximization.");
            using (var cancellation = new CancellationTokenSource())
            {
                launcher = new EdgeLauncher(delegate { return new HashSet<IntPtr>(); }, delegate { throw new Exception("Unexpected window."); }, delegate { cancellation.Cancel(); });
                bool cancelled = false;
                try { await launcher.LaunchAsync("test-edge.exe", "Default", cancellation.Token); }
                catch (OperationCanceledException) { cancelled = true; }
                Assert(cancelled, "Closing the launcher did not cancel the wait.");
            }
            launcher = new EdgeLauncher(delegate { return new HashSet<IntPtr>(); }, delegate { return false; }, delegate { throw new FileNotFoundException(); });
            bool failed = false;
            try { await launcher.LaunchAsync("missing-edge.exe", "Default", CancellationToken.None); }
            catch (FileNotFoundException) { failed = true; }
            Assert(failed, "Process startup errors were swallowed.");
        }
    }
}
