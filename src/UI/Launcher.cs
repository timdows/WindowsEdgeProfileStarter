using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal sealed class Launcher : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
        private readonly ConfigurationStore configuration;
        private readonly EdgeLauncher edge = new EdgeLauncher();
        private readonly RdpLauncher rdp = new RdpLauncher();
        private readonly VncLauncher vnc = new VncLauncher();
        private readonly ShareLauncher shareLauncher = new ShareLauncher();
        private readonly SshLauncher ssh = new SshLauncher();
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly FlowLayoutPanel profiles = new FlowLayoutPanel();
        private readonly FlowLayoutPanel connections = new FlowLayoutPanel();
        private readonly Button themeToggle = new QuietButton();
        private bool launching;
        private bool resourcesDisposed;

        public Launcher() : this(new ConfigurationStore(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"))) { }

        internal Launcher(ConfigurationStore configuration)
        {
            this.configuration = configuration;
            Text = "Edge Profile Starter";
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            // Load the embedded icon directly; shell icon extraction can delay startup.
            using (Stream stream = typeof(Launcher).Assembly.GetManifestResourceStream("EdgeProfileStarter.AppIcon"))
            using (Icon icon = new Icon(stream))
                Icon = (Icon)icon.Clone();
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(246, 248, 252);
            ClientSize = new Size(980, 670);
            MinimumSize = new Size(820, 480);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;

            BuildContent();
            BuildTitleBar();
            LoadProfiles();
        }

        private void BuildContent()
        {
            TableLayoutPanel layout = new GradientLayout();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(32, 12, 32, 20);
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);
            Control heading = new HeroHeader { Dock = DockStyle.Fill };
            layout.Controls.Add(heading, 0, 0);
            layout.SetColumnSpan(heading, 2);
            layout.Controls.Add(new Label { Text = "JOUW PROFIELEN", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(119, 131, 153), BackColor = Color.Transparent, Dock = DockStyle.Fill, Padding = new Padding(0, 4, 0, 0) }, 0, 1);
            layout.Controls.Add(new Label { Text = "VERBINDINGEN", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.Transparent, Dock = DockStyle.Fill, Padding = new Padding(12, 4, 0, 0) }, 1, 1);
            ConfigureList(profiles, new Padding(0, 0, 12, 0));
            ConfigureList(connections, new Padding(12, 0, 0, 0));
            layout.Controls.Add(profiles, 0, 2);
            layout.Controls.Add(connections, 1, 2);
        }

        private void ConfigureList(FlowLayoutPanel list, Padding margin)
        {
            list.Dock = DockStyle.Fill;
            list.FlowDirection = FlowDirection.TopDown;
            list.WrapContents = false;
            list.AutoScroll = true;
            list.BackColor = Color.Transparent;
            list.Margin = margin;
            list.SizeChanged += delegate { ResizeButtons(); };
        }
        private void BuildTitleBar()
        {
            Panel titleBar = new Panel { Height = 44, Dock = DockStyle.Top, BackColor = Color.FromArgb(237, 243, 255) };
            titleBar.MouseDown += delegate(object sender, MouseEventArgs e) {
                if (e.Button == MouseButtons.Left && e.Clicks == 1) {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
                }
            };
            WindowButton close = new WindowButton(true) { Dock = DockStyle.Right, AccessibleName = "Sluiten" };
            close.Click += delegate { Close(); };
            WindowButton minimize = new WindowButton(false) { Dock = DockStyle.Right, AccessibleName = "Minimaliseren" };
            minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            titleBar.Controls.Add(minimize);
            titleBar.Controls.Add(close);
            themeToggle.Width = 148;
            themeToggle.Dock = DockStyle.Left;
            themeToggle.AccessibleName = "Donkere modus wisselen";
            themeToggle.Click += delegate { ToggleTheme(); };
            titleBar.Controls.Add(themeToggle);
            Controls.Add(titleBar);
            titleBar.SendToBack();
        }

        private void ApplyTheme()
        {
            themeToggle.Text = Theme.Dark ? "Lichte modus" : "Donkere modus";
            Theme.Apply(this);
            Refresh();
        }

        private void ToggleTheme()
        {
            try
            {
                bool next = !Theme.Dark;
                configuration.SetDarkMode(next);
                Theme.Dark = next;
                ApplyTheme();
            }
            catch (Exception ex) { ShowError("De thema-instelling kon niet worden opgeslagen. " + ex.Message); }
        }

        protected override void WndProc(ref Message m)
        {
            // Block maximize, including Windows shortcuts and title-bar gestures.
            if (m.Msg == 0x112 && (m.WParam.ToInt64() & 0xFFF0) == 0xF030) return;
            base.WndProc(ref m);
        }

        private void ResizeButtons()
        {
            foreach (FlowLayoutPanel list in new[] { profiles, connections })
                foreach (Control control in list.Controls)
                    control.Width = Math.Max(180, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        }

        private void LoadProfiles()
        {
            try
            {
                Configuration config = configuration.Load();
                while (profiles.Controls.Count > 0) profiles.Controls[0].Dispose();
                Text = String.IsNullOrWhiteSpace(config.windowTitle) ? "Edge Profile Starter" : config.windowTitle;
                int index = 0;
                foreach (Profile profile in config.profiles)
                {
                    Profile selected = profile;
                    bool exists = Directory.Exists(EdgeLauncher.ProfilePath(selected.directory));
                    Button button = new ProfileCard(selected.name, selected.directory, exists, index++);
                    button.Height = 96;
                    button.Margin = new Padding(0, 0, 0, 12);
                    button.Click += delegate { Launch(selected); };
                    profiles.Controls.Add(button);
                }
                while (connections.Controls.Count > 0) connections.Controls[0].Dispose();
                index = 0;
                if (config.rdpSessions != null)
                    foreach (RdpSession session in config.rdpSessions)
                    {
                        RdpSession selected = session;
                        var button = new ConnectionCard(selected.name, "RDP  ·  " + selected.host, index++, "Open deze RDP-verbinding", delegate(DisplayMode mode) {
                            try { rdp.Launch(selected, mode); }
                            catch (Exception error) { ShowError("RDP kon niet worden gestart. " + error.Message); }
                        });
                        connections.Controls.Add(button);
                    }
                if (config.vncSessions != null)
                    foreach (VncSession session in config.vncSessions)
                    {
                        VncSession selected = session;
                        var button = new ConnectionCard(selected.name, "UltraVNC  ·  " + selected.host, index++, "Open deze UltraVNC-verbinding", delegate(DisplayMode mode) {
                            try { vnc.Launch(selected, config.ultraVncViewerPath, mode); }
                            catch (Exception error) { ShowError("UltraVNC kon niet worden gestart. " + error.Message); }
                        });
                        connections.Controls.Add(button);
                    }
                if (config.sshSessions != null)
                    foreach (SshSession session in config.sshSessions)
                    {
                        SshSession selected = session;
                        string target = (String.IsNullOrWhiteSpace(selected.username) ? "" : selected.username + "@") + selected.host;
                        if (selected.port.HasValue) target += "  ·  poort " + selected.port.Value;
                        var button = new ProfileCard(selected.name, "SSH  ·  " + target, index++, false, false, true) {
                            Height = 96, Margin = new Padding(0, 0, 0, 14), AccessibleDescription = "Open deze SSH-verbinding"
                        };
                        button.Click += delegate {
                            try { ssh.Launch(selected); }
                            catch (Exception error) { ShowError("SSH kon niet worden gestart. " + error.Message); }
                        };
                        connections.Controls.Add(button);
                    }
                if (config.shares != null)
                    foreach (NetworkShare share in config.shares)
                    {
                        NetworkShare selected = share;
                        var button = new ProfileCard(selected.name, "Share  ·  " + selected.path, index++, false, true) {
                            Height = 96, Margin = new Padding(0, 0, 0, 14), AccessibleDescription = "Open deze share in Windows Verkenner"
                        };
                        button.Click += delegate {
                            try { shareLauncher.Launch(selected); }
                            catch (Exception error) { ShowError("De share kon niet worden geopend. " + error.Message); }
                        };
                        connections.Controls.Add(button);
                    }
                if (connections.Controls.Count == 0)
                    connections.Controls.Add(new Label { Text = "Voeg verbindingen of shares toe in config.json.", Height = 64, Padding = new Padding(4, 16, 0, 0), BackColor = Color.Transparent });
                ResizeButtons();
                Theme.Dark = config.darkMode;
                ApplyTheme();
            }
            catch (Exception ex)
            {
                ShowError("Controleer config.json naast de app." + Environment.NewLine + Environment.NewLine + ex.Message);
            }
        }

        private async void Launch(Profile profile)
        {
            if (launching) return;
            launching = true;
            profiles.Enabled = false;
            try
            {
                if (!Directory.Exists(EdgeLauncher.ProfilePath(profile.directory)))
                    throw new Exception("Dit Edge-profiel bestaat niet: " + profile.directory + ". Controleer de directory in config.json.");
                await edge.LaunchAsync(EdgeLauncher.FindExecutable(), profile.directory, shutdown.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed) ShowError("Edge kon niet gemaximaliseerd worden geopend. " + ex.Message); }
            finally { launching = false; if (!IsDisposed) profiles.Enabled = true; }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            shutdown.Cancel();
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !resourcesDisposed)
            {
                resourcesDisposed = true;
                shutdown.Cancel();
                shutdown.Dispose();
                if (Icon != null) Icon.Dispose();
            }
            base.Dispose(disposing);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "Edge Profile Starter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
