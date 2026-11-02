using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhraseLauncher
{
    // 角丸枠の中に枠なしTextBoxを配置した入力欄（検索アイコンは任意）
    class ModernTextBox : Panel
    {
        private static readonly Color IconColor = Color.FromArgb(140, 146, 158);

        private const int IconArea = 30;
        private const int SidePad = 10;

        private bool showSearchIcon;

        public TextBox TextBox { get; } = new() { BorderStyle = BorderStyle.None, BackColor = Color.White, Font = UiTheme.Font };

        public bool ShowSearchIcon
        {
            get => showSearchIcon;
            set { showSearchIcon = value; PerformLayout(); Invalidate(); }
        }

        public ModernTextBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 32;

            TextBox.GotFocus += (s, e) => Invalidate();
            TextBox.LostFocus += (s, e) => Invalidate();
            TextBox.EnabledChanged += (s, e) =>
            {
                TextBox.BackColor = TextBox.Enabled ? Color.White : UiTheme.Surface;
                Invalidate();
            };
            Controls.Add(TextBox);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int left = showSearchIcon ? IconArea : SidePad;
            TextBox.SetBounds(
                left,
                (ClientSize.Height - TextBox.Height) / 2,
                Math.Max(0, ClientSize.Width - left - SidePad),
                TextBox.Height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            TextBox.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? SystemColors.Control);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool enabled = TextBox.Enabled;
            bool focused = TextBox.Focused;
            var r = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1.5f, ClientSize.Height - 1.5f);
            using (var path = UiTheme.RoundedPath(r, 6))
            {
                using var brush = new SolidBrush(enabled ? Color.White : UiTheme.Surface);
                g.FillPath(brush, path);
                using var pen = new Pen(focused ? UiTheme.Accent : enabled ? UiTheme.Border : UiTheme.Divider, focused ? 1.5f : 1f);
                g.DrawPath(pen, path);
            }

            if (!showSearchIcon) return;

            // 虫眼鏡
            float cx = 14, cy = ClientSize.Height / 2f - 1;
            const float radius = 5f;
            using var iconPen = new Pen(focused ? UiTheme.Accent : IconColor, 1.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawEllipse(iconPen, cx - radius, cy - radius, radius * 2, radius * 2);
            float d = radius * 0.7071f;
            g.DrawLine(iconPen, cx + d, cy + d, cx + d + 4, cy + d + 4);
        }
    }
}
