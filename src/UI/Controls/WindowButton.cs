using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal sealed class WindowButton : Button
    {
        private readonly bool close;
        internal WindowButton(bool isClose)
        {
            close = isClose;
            Width = 46;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = isClose ? Color.FromArgb(255, 224, 230) : Color.FromArgb(220, 230, 248);
            FlatAppearance.MouseDownBackColor = isClose ? Color.FromArgb(250, 198, 208) : Color.FromArgb(205, 220, 244);
            BackColor = Color.FromArgb(237, 243, 255);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float s = DeviceDpi / 96f, x = Width/2f, y = Height/2f;
            using (Pen pen = new Pen(Theme.Muted, 1.6f*s))
            {
                if (close) {
                    e.Graphics.DrawLine(pen, x-4*s, y-4*s, x+4*s, y+4*s);
                    e.Graphics.DrawLine(pen, x+4*s, y-4*s, x-4*s, y+4*s);
                } else e.Graphics.DrawLine(pen, x-5*s, y+2*s, x+5*s, y+2*s);
            }
            if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(5, 5, Width-10, Height-10));
        }
    }
}
