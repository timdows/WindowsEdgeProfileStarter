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
                SynchronizationContext.SetSynchronizationContext(null);
                BrowserStartup().GetAwaiter().GetResult();
                Console.WriteLine("OK: configuration, validation, theme persistence, form lifecycle, browser window selection, cancellation and RDP/UltraVNC connections.");
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
                ultraVncViewerPath = @"C:\Tools\UltraVNC\vncviewer.exe"
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
