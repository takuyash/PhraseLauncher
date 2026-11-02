using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhraseLauncher
{
    // 見出し付きの白い角丸カード（GroupBox の代わり）
    class CardPanel : Panel
    {
        public const int TitleHeight = 40;

        private string title = "";

        public string Title
        {
            get => title;
            set { title = value; Invalidate(); }
        }

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? UiTheme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1.5f, ClientSize.Height - 1.5f);
            using (var path = UiTheme.RoundedPath(r, 8))
            {
                using var brush = new SolidBrush(Color.White);
                g.FillPath(brush, path);
                using var pen = new Pen(UiTheme.Border);
                g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(g, title, UiTheme.BoldFont,
                new Rectangle(16, 0, ClientSize.Width - 32, TitleHeight), UiTheme.TextStrong,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
