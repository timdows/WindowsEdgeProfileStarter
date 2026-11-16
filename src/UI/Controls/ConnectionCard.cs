using System;
using System.Drawing;
using System.Windows.Forms;

namespace EdgeProfileStarter
{
    internal sealed class ConnectionCard : UserControl
    {
        private readonly ComboBox display = new ComboBox();
        internal DisplayMode SelectedDisplay { get { return (DisplayMode)(display.SelectedIndex + 1); } }

        internal ConnectionCard(string name, string detail, int index, string description, Action<DisplayMode> launch)
        {
            BackColor = Color.Transparent;
            Height = 138;
            Margin = new Padding(0, 0, 0, 14);
            var button = new ProfileCard(name, detail, index, true) {
                Dock = DockStyle.Top, Height = 96, Margin = new Padding(0), AccessibleDescription = description
            };
            button.Click += delegate { launch(SelectedDisplay); };
            Controls.Add(button);
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.Transparent, Padding = new Padding(4, 0, 4, 0) };
            var label = new Label { Text = "Weergave", Dock = DockStyle.Left, Width = 90, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent };
            display.Dock = DockStyle.Fill;
            display.DropDownStyle = ComboBoxStyle.DropDownList;
            display.FlatStyle = FlatStyle.Flat;
            display.DrawMode = DrawMode.OwnerDrawFixed;
            display.ItemHeight = Font.Height + 6;
            display.DrawItem += DrawDisplayItem;
            display.AccessibleName = "Weergave voor " + name;
            display.Items.AddRange(new object[] { "Volledig scherm", "Alle schermen", "1280 × 1024" });
            display.SelectedIndex = 0;
            footer.Controls.Add(display);
            footer.Controls.Add(label);
            Controls.Add(footer);
        }
        private void DrawDisplayItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool highlighted = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            Color background = highlighted ? SystemColors.Highlight : display.BackColor;
            using (var brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, display.Items[e.Index].ToString(), display.Font, e.Bounds,
                highlighted ? SystemColors.HighlightText : display.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        }    }
}
