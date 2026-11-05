using System.Drawing;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal sealed class QuietButton : Button
    {
        internal QuietButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.FromArgb(228, 235, 249);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(215, 226, 245);
            BackColor = Color.FromArgb(237, 242, 250);
            ForeColor = Color.FromArgb(82, 104, 142);
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 0, 10, 0);
        }
    }
}
