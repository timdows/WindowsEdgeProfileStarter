using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal static class Artwork
    {
        internal static GraphicsPath Rounded(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static void Text(Graphics g, string text, float size, FontStyle style, Color color, Rectangle bounds)
        {
            using (Font font = new Font("Segoe UI", size, style))
                TextRenderer.DrawText(g, text, font, bounds, color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }
}
