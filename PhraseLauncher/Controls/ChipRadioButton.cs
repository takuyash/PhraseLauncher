using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhraseLauncher
{
    // チップ型のラジオボタン（選択中は青く強調）
    class ChipRadioButton : RadioButton
    {
        private bool hovered;

        public ChipRadioButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Appearance = Appearance.Button;
            FlatStyle = FlatStyle.Flat;
            AutoSize = false;
            Font = UiTheme.Font;
            Height = 32;
            Cursor = Cursors.Hand;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Width = Math.Max(88, TextRenderer.MeasureText(Text, UiTheme.BoldFont).Width + 36);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; Invalidate(); }
        protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = Checked ? UiTheme.AccentSoft : hovered ? UiTheme.Hover : Color.White;
            Color border = Checked ? UiTheme.Accent : UiTheme.Border;
            Color text = Checked ? UiTheme.Accent : UiTheme.Text;

            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = UiTheme.RoundedPath(r, Height / 2f))
            {
                using var brush = new SolidBrush(fill);
                g.FillPath(brush, path);
                using var pen = new Pen(border, Checked ? 1.5f : 1f);
                g.DrawPath(pen, path);
            }

            if (Focused && ShowFocusCues)
            {
                using var path = UiTheme.RoundedPath(RectangleF.Inflate(r, -2.5f, -2.5f), Height / 2f - 2.5f);
                using var pen = new Pen(UiTheme.Accent) { DashStyle = DashStyle.Dot };
                g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(g, Text, Checked ? UiTheme.BoldFont : UiTheme.Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
