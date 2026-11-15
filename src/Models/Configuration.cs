using System.Collections.Generic;

namespace EdgeProfileStarter
{
    public sealed class Configuration
    {
        public bool darkMode { get; set; }
        public string windowTitle { get; set; }
        public List<Profile> profiles { get; set; }
        public List<RdpSession> rdpSessions { get; set; }
        public List<VncSession> vncSessions { get; set; }
        public string ultraVncViewerPath { get; set; }
    }
}
