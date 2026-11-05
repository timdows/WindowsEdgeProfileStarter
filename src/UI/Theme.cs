using System.Drawing;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal static class Theme
    {
        internal static bool Dark;
        internal static Color Top { get { return Dark ? Color.FromArgb(26, 32, 49) : Color.FromArgb(237, 243, 255); } }
        internal static Color Bottom { get { return Dark ? Color.FromArgb(17, 22, 34) : Color.FromArgb(248, 250, 252); } }
        internal static Color Text { get { return Dark ? Color.FromArgb(231, 237, 250) : Color.FromArgb(43, 57, 82); } }
        internal static Color Muted { get { return Dark ? Color.FromArgb(158, 174, 200) : Color.FromArgb(112, 125, 148); } }
        internal static void Apply(Control control)
        {
            if (control is Form || (control is Panel && !(control is FlowLayoutPanel))) control.BackColor = Top;
            if (control is Label) control.ForeColor = Muted;
            Button button = control as Button;
            if (button != null && !(button is ProfileCard))
            {
                button.BackColor = Dark ? Color.FromArgb(37, 46, 66) : Color.FromArgb(237, 242, 250);
                button.ForeColor = Text;
                button.FlatAppearance.MouseOverBackColor = Dark ? Color.FromArgb(54, 66, 94) : Color.FromArgb(220, 230, 248);
                button.FlatAppearance.MouseDownBackColor = Dark ? Color.FromArgb(65, 79, 110) : Color.FromArgb(205, 220, 244);
            }
            foreach (Control child in control.Controls) Apply(child);
            control.Invalidate();
        }
    }
}
