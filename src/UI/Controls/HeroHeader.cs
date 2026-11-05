using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal sealed class HeroHeader : Control
    {
        internal HeroHeader() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = DeviceDpi / 96f;
            g.ScaleTransform(s, s);
            using (GraphicsPath shape = Artwork.Rounded(new RectangleF(0, 4, 38, 38), 12))
            using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 4, 38, 38), Color.FromArgb(77, 153, 245), Color.FromArgb(102, 102, 224), 45f))
                g.FillPath(brush, shape);
            using (Pen pen = new Pen(Color.White, 2))
            {
                g.DrawEllipse(pen, 9, 13, 20, 20);
                g.DrawArc(pen, 14, 13, 10, 20, 0, 360);
                g.DrawLine(pen, 9, 23, 29, 23);
            }
            // TextRenderer uses device coordinates; restore before drawing text.
            g.ResetTransform();
            int w = Width;
            Artwork.Text(g, "EDGE PROFILE STARTER", 9, FontStyle.Bold, Theme.Muted, new Rectangle((int)(50*s), (int)(7*s), w-(int)(50*s), (int)(30*s)));
        }
    }
}
