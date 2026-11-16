using System;
using System.Drawing;
using System.Globalization;

namespace EdgeProfileStarter
{
    internal enum DisplayMode { Default, FullScreen, AllMonitors, Window1280 }

    internal static class DisplayOptions
    {
        internal static string ForRdp(DisplayMode mode)
        {
            switch (mode)
            {
                case DisplayMode.Default: return "";
                case DisplayMode.FullScreen: return " /f";
                case DisplayMode.AllMonitors: return " /f /multimon";
                case DisplayMode.Window1280: return " /w:1280 /h:1024";
                default: throw new ArgumentOutOfRangeException("mode");
            }
        }

        internal static string ForVnc(DisplayMode mode, Rectangle desktop)
        {
            switch (mode)
            {
                case DisplayMode.Default: return "";
                case DisplayMode.FullScreen: return " -fullscreen -autoscaling";
                case DisplayMode.AllMonitors:
                    return String.Format(CultureInfo.InvariantCulture, " -noborder -notoolbar -autoscaling -position {0} {1} {2} {3}", desktop.X, desktop.Y, desktop.Width, desktop.Height);
                case DisplayMode.Window1280: return " -autoscaling -position 0 0 1280 1024";
                default: throw new ArgumentOutOfRangeException("mode");
            }
        }
    }
}
