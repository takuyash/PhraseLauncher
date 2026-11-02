using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhraseLauncher
{
    enum ButtonVariant { Secondary, Primary, Danger }

    // 角丸のフラットボタン
    class ModernButton : Button
    {
        private bool hovered, pressed;
        private ButtonVariant variant;

        public ButtonVariant Variant
        {
            get => variant;
            set { variant = value; Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = UiTheme.Font;
            Height = 32;
            Cursor = Cursors.Hand;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            // 文字数に合わせて幅を調整
            Width = Math.Max(40, TextRenderer.MeasureText(Text, Font).Width + 28);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); pressed = false; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? UiTheme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill, border, text;
            switch (variant)
            {
                case ButtonVariant.Primary:
                    fill = pressed ? UiTheme.AccentPressed : hovered ? UiTheme.AccentHover : UiTheme.Accent;
                    border = fill;
                    text = Color.White;
                    break;
                case ButtonVariant.Danger:
                    fill = pressed ? Color.FromArgb(254, 226, 226) : hovered ? UiTheme.DangerSoft : Color.White;
                    border = hovered ? UiTheme.Danger : UiTheme.DangerBorder;
                    text = UiTheme.Danger;
                    break;
                default:
                    fill = pressed ? UiTheme.Pressed : hovered ? UiTheme.Hover : Color.White;
                    border = UiTheme.Border;
                    text = UiTheme.Text;
                    break;
            }
            if (!Enabled)
            {
                fill = UiTheme.Surface;
                border = UiTheme.Divider;
                text = UiTheme.Disabled;
            }

            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var path = UiTheme.RoundedPath(r, 6))
            {
                using var brush = new SolidBrush(fill);
                g.FillPath(brush, path);
                using var pen = new Pen(border);
                g.DrawPath(pen, path);
            }

            // キーボード操作時のフォーカスリング
            if (Focused && ShowFocusCues)
            {
                using var path = UiTheme.RoundedPath(RectangleF.Inflate(r, -2, -2), 4);
                using var pen = new Pen(variant == ButtonVariant.Primary ? Color.White : UiTheme.Accent, 1.5f);
                g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
