using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal sealed class ProfileCard : Button
    {
        private readonly string title;
        private readonly string subtitle;
        private readonly Color accent;
        private readonly int symbol;
        private bool hovered;
        private bool pressed;
        internal ProfileCard(string name, string directory, bool exists, int index)
        {
            title = name;
            subtitle = directory + (exists ? "" : "  ·  Niet gevonden");
            Text = name;
            AccessibleName = name + ", " + subtitle;
            AccessibleDescription = "Open Microsoft Edge met dit profiel";
            Color[] colors = { Color.FromArgb(85, 120, 225), Color.FromArgb(29, 157, 143), Color.FromArgb(160, 104, 211) };
            accent = colors[index % colors.Length];
            symbol = index % 3;
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Color.FromArgb(246, 248, 252);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // Paint every pixel ourselves. Transparent themed Button backgrounds can
            // otherwise leave black corners on desktop/DPI configurations.
            Control ancestor = Parent;
            Point offset = Location;
            while (ancestor != null && !(ancestor is GradientLayout))
            {
                offset.Offset(ancestor.Left, ancestor.Top);
                ancestor = ancestor.Parent;
            }
            GradientLayout surface = ancestor as GradientLayout;
            if (surface != null) surface.PaintSurface(g, ClientRectangle, offset);
            else g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = DeviceDpi / 96f;
            RectangleF card = new RectangleF(1, 1, Width - 4, Height - 6);
            using (GraphicsPath shadow = Artwork.Rounded(new RectangleF(2, 4, Width - 5, Height - 6), 17*s))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(16, 48, 67, 112))) g.FillPath(brush, shadow);
            using (GraphicsPath path = Artwork.Rounded(card, 17*s))
            {
                Color start = Theme.Dark ? Color.FromArgb(36, 44, 63) : Color.White;
                Color end = Theme.Dark ? (pressed ? Color.FromArgb(54, 64, 91) : hovered ? Color.FromArgb(44, 54, 79) : Color.FromArgb(29, 36, 53)) : (pressed ? Color.FromArgb(226, 234, 251) : hovered ? Color.FromArgb(239, 244, 255) : Color.FromArgb(250, 252, 255));
                using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, start, end, 20f)) g.FillPath(brush, path);
                using (Pen pen = new Pen(Focused || hovered ? Color.FromArgb(145, accent) : Theme.Dark ? Color.FromArgb(60, 72, 98) : Color.FromArgb(226, 232, 243), Focused ? 2*s : s)) g.DrawPath(pen, path);
            }
            g.TranslateTransform(20*s, (Height - 52*s)/2 - 2*s);
            g.ScaleTransform(s, s);
            using (GraphicsPath tile = Artwork.Rounded(new RectangleF(0, 0, 52, 52), 15))
            using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, 52, 52), Color.FromArgb(30, accent), Color.FromArgb(12, accent), 50f)) g.FillPath(brush, tile);
            using (Pen pen = new Pen(accent, 1.9f))
            {
                pen.StartCap = pen.EndCap = LineCap.Round;
                if (symbol == 0) {
                    g.DrawEllipse(pen, 21, 13, 10, 10);
                    g.DrawArc(pen, 15, 27, 22, 17, 180, 180);
                    g.DrawLine(pen, 15, 35, 37, 35);
                } else if (symbol == 1) {
                    g.DrawRectangle(pen, 14, 21, 24, 17);
                    g.DrawRectangle(pen, 21, 15, 10, 6);
                    g.DrawLine(pen, 14, 28, 38, 28);
                    g.DrawLine(pen, 26, 26, 26, 31);
                } else {
                    g.DrawLine(pen, 14, 19, 24, 19);
                    g.DrawLine(pen, 24, 19, 28, 23);
                    g.DrawLine(pen, 28, 23, 38, 23);
                    g.DrawLine(pen, 38, 23, 38, 37);
                    g.DrawLine(pen, 38, 37, 14, 37);
                    g.DrawLine(pen, 14, 37, 14, 19);
                }
            }
            g.ResetTransform();
            int left = (int)(90*s);
            Artwork.Text(g, title, 13, FontStyle.Bold, Theme.Text, new Rectangle(left, (int)(19*s), Width-left-(int)(42*s), (int)(28*s)));
            Artwork.Text(g, subtitle, 9, FontStyle.Regular, Theme.Muted, new Rectangle(left, (int)(49*s), Width-left-(int)(42*s), (int)(22*s)));
            using (Pen pen = new Pen(hovered ? accent : Color.FromArgb(163, 175, 196), 1.7f*s))
            {
                float x = Width - 27*s, y = Height/2f - 2*s;
                g.DrawLine(pen, x-5*s, y-5*s, x, y);
                g.DrawLine(pen, x, y, x-5*s, y+5*s);
            }
        }
    }
}
