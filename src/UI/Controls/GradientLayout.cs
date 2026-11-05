using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal sealed class GradientLayout : TableLayoutPanel
    {
        internal GradientLayout() { DoubleBuffered = true; }
        internal void PaintSurface(Graphics graphics, Rectangle bounds, Point offset)
        {
            if (Width < 1 || Height < 1) return;
            Rectangle gradient = new Rectangle(-offset.X, -offset.Y, Width, Height);
            using (LinearGradientBrush brush = new LinearGradientBrush(gradient, Theme.Top, Theme.Bottom, 55f))
                graphics.FillRectangle(brush, bounds);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            PaintSurface(e.Graphics, ClientRectangle, Point.Empty);
        }
    }
}
