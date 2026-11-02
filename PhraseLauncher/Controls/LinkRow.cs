using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhraseLauncher
{
    // クリックできるリンク行（右端に外部リンクアイコン）
    class LinkRow : Control
    {
        private bool hovered;
        private bool showDivider;
        private bool highlight;

        // 上側に区切り線を引く（カード内の2行目以降用）
        public bool ShowDivider
        {
            get => showDivider;
            set { showDivider = value; Invalidate(); }
        }

        // お知らせ用の強調表示（青背景＋太字）
        public bool Highlight
        {
            get => highlight;
            set { highlight = value; Invalidate(); }
        }

        public LinkRow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Font = UiTheme.Font;
            Height = 48;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bg = highlight
                ? new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f)
                : new RectangleF(6, 4, Width - 12, Height - 8);

            if (highlight)
            {
                using var path = UiTheme.RoundedPath(bg, 8);
                using var brush = new SolidBrush(hovered ? UiTheme.Selection : UiTheme.AccentSoft);
                g.FillPath(brush, path);
                using var pen = new Pen(UiTheme.Accent);
                g.DrawPath(pen, path);
            }
            else if (hovered)
            {
                using var path = UiTheme.RoundedPath(bg, 6);
                using var brush = new SolidBrush(UiTheme.Hover);
                g.FillPath(brush, path);
            }
            else if (showDivider)
            {
                using var pen = new Pen(UiTheme.Divider);
                g.DrawLine(pen, 16, 0, Width - 16, 0);
            }

            if (Focused && ShowFocusCues)
            {
                using var path = UiTheme.RoundedPath(RectangleF.Inflate(bg, -1.5f, -1.5f), 5);
                using var pen = new Pen(UiTheme.Accent, 1.5f);
                g.DrawPath(pen, path);
            }

            Color fore = highlight ? UiTheme.Accent : UiTheme.TextStrong;
            TextRenderer.DrawText(g, Text, highlight ? UiTheme.BoldFont : Font,
                new Rectangle(16, 0, Width - 56, Height), fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            DrawExternalIcon(g, Width - 28, Height / 2f, highlight ? UiTheme.Accent : UiTheme.TextMuted);
        }

        // 右上向きの矢印（外部リンク）
        private static void DrawExternalIcon(Graphics g, float cx, float cy, Color color)
        {
            using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.DrawLine(pen, cx - 4, cy + 4, cx + 4, cy - 4);
            g.DrawLines(pen, new[] { new PointF(cx - 2, cy - 4), new PointF(cx + 4, cy - 4), new PointF(cx + 4, cy + 2) });
        }
    }
}
