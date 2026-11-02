using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{
    // ヘッダー部分を非表示にしたTabControl（見た目は ModernTabStrip が担当）
    class HeaderlessTabControl : TabControl
    {
        private const int TCM_ADJUSTRECT = 0x1328;

        public HeaderlessTabControl()
        {
            // 複数行にしておくと標準のスクロールボタン(◀▶)が生成されない
            Multiline = true;
        }

        protected override void WndProc(ref Message m)
        {
            // ページ領域をクライアント全体にしてヘッダーを隠す
            if (m.Msg == TCM_ADJUSTRECT && !DesignMode)
            {
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }
    }

    // モダンなタブヘッダー（下線インジケーター＋スクロールボタン）
    class ModernTabStrip : Control
    {
        private static readonly Color StripColor = UiTheme.Surface;
        private static readonly Color BorderColor = Color.FromArgb(227, 230, 235);
        private static readonly Color HoverColor = UiTheme.Hover;
        private static readonly Color FocusColor = UiTheme.AccentSoft;
        private static readonly Color AccentColor = UiTheme.Accent;
        private static readonly Color TextColor = UiTheme.TextMuted;
        private static readonly Color SelectedTextColor = UiTheme.TextStrong;
        private static readonly Color ArrowColor = Color.FromArgb(75, 85, 99);
        private static readonly Color DisabledColor = UiTheme.Disabled;

        private const int SidePad = 8;
        private const int TabGap = 2;
        private const int TabPadX = 14;
        private const int MaxTabWidth = 180;
        private const int ArrowSize = 26;
        private const int PillMargin = 5;
        private const int HitNone = -1, HitLeft = -2, HitRight = -3;

        private readonly TabControl target;
        private readonly Font normalFont = UiTheme.Font;
        private readonly Font boldFont = UiTheme.BoldFont;
        private readonly Timer scrollTimer = new() { Interval = 15 };
        private readonly List<(int X, int Width)> tabs = new(); // コンテンツ座標
        private int contentWidth;
        private float offset, targetOffset;
        private int hover = HitNone;

        public ModernTabStrip(TabControl target)
        {
            this.target = target;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);

            Font = normalFont;

            Dock = DockStyle.Top;
            Height = normalFont.Height + 20;

            target.ControlAdded += (s, e) => RecalcLayout();
            target.ControlRemoved += (s, e) => RecalcLayout();
            target.SelectedIndexChanged += (s, e) => { EnsureVisible(target.SelectedIndex); Invalidate(); };
            target.GotFocus += (s, e) => Invalidate();
            target.LostFocus += (s, e) => Invalidate();

            scrollTimer.Tick += (s, e) =>
            {
                offset += (targetOffset - offset) * 0.3f;
                if (Math.Abs(targetOffset - offset) < 0.5f)
                {
                    offset = targetOffset;
                    scrollTimer.Stop();
                }
                UpdateHover(PointToClient(Cursor.Position));
                Invalidate();
            };
        }

        /* ================= レイアウト ================= */

        private bool Overflow => contentWidth > ClientSize.Width - SidePad * 2;

        private Rectangle Viewport => Overflow
            ? new Rectangle(SidePad, 0, Math.Max(0, ClientSize.Width - SidePad - ArrowSize * 2 - 12), ClientSize.Height)
            : new Rectangle(SidePad, 0, Math.Max(0, ClientSize.Width - SidePad * 2), ClientSize.Height);

        private float MaxOffset => Math.Max(0, contentWidth - Viewport.Width);

        private int ArrowTop => PillMargin + (ClientSize.Height - PillMargin * 2 - ArrowSize) / 2;

        private Rectangle LeftArrowRect =>
            new(ClientSize.Width - SidePad / 2 - ArrowSize * 2 - 2, ArrowTop, ArrowSize, ArrowSize);

        private Rectangle RightArrowRect =>
            new(ClientSize.Width - SidePad / 2 - ArrowSize, ArrowTop, ArrowSize, ArrowSize);

        private bool CanScrollLeft => targetOffset > 0.5f;
        private bool CanScrollRight => targetOffset < MaxOffset - 0.5f;

        private void RecalcLayout()
        {
            tabs.Clear();
            int x = 0;
            foreach (TabPage page in target.TabPages)
            {
                int w = Math.Min(MaxTabWidth, TextRenderer.MeasureText(page.Text, boldFont).Width + TabPadX * 2);
                tabs.Add((x, w));
                x += w + TabGap;
            }
            contentWidth = Math.Max(0, x - TabGap);
            ClampOffset();
            Invalidate();
        }

        private Rectangle TabRect(int index)
        {
            var (x, w) = tabs[index];
            return new Rectangle(Viewport.X + x - (int)Math.Round(offset), 0, w, ClientSize.Height);
        }

        private void ClampOffset()
        {
            targetOffset = Math.Clamp(targetOffset, 0, MaxOffset);
            offset = Math.Clamp(offset, 0, MaxOffset);
        }

        private void ScrollTo(float value)
        {
            targetOffset = Math.Clamp(value, 0, MaxOffset);
            scrollTimer.Start();
        }

        private void EnsureVisible(int index)
        {
            if (index < 0 || index >= tabs.Count) return;
            var (x, w) = tabs[index];
            int viewWidth = Viewport.Width;
            if (x < targetOffset) ScrollTo(x);
            else if (x + w > targetOffset + viewWidth) ScrollTo(x + w - viewWidth);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClampOffset();
            EnsureVisible(target.SelectedIndex);
        }

        /* ================= マウス操作 ================= */

        private int HitTest(Point p)
        {
            if (Overflow)
            {
                if (LeftArrowRect.Contains(p)) return HitLeft;
                if (RightArrowRect.Contains(p)) return HitRight;
            }
            if (!Viewport.Contains(p)) return HitNone;
            for (int i = 0; i < tabs.Count; i++)
                if (TabRect(i).Contains(p)) return i;
            return HitNone;
        }

        private void UpdateHover(Point p)
        {
            int hit = ClientRectangle.Contains(p) ? HitTest(p) : HitNone;
            if (hit == hover) return;
            hover = hit;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateHover(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = HitNone;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int hit = HitTest(e.Location);
            if (hit == HitLeft && CanScrollLeft) ScrollTo(targetOffset - Viewport.Width * 0.6f);
            else if (hit == HitRight && CanScrollRight) ScrollTo(targetOffset + Viewport.Width * 0.6f);
            else if (hit >= 0)
            {
                target.SelectedIndex = hit;
                target.Focus();
            }
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollTo(targetOffset - e.Delta / 120f * 80);
        }

        /* ================= 描画 ================= */

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(StripColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var pen = new Pen(BorderColor))
                g.DrawLine(pen, 0, ClientSize.Height - 1, ClientSize.Width, ClientSize.Height - 1);

            var vp = Viewport;
            var state = g.Save();
            g.SetClip(vp);

            bool focused = target.Focused;
            for (int i = 0; i < tabs.Count; i++)
            {
                var r = TabRect(i);
                if (r.Right < vp.Left || r.Left > vp.Right) continue;

                bool selected = i == target.SelectedIndex;
                var pill = Rectangle.FromLTRB(r.Left, PillMargin, r.Right, r.Bottom - PillMargin);

                if (selected && focused) FillRounded(g, pill, 6, FocusColor);
                else if (i == hover) FillRounded(g, pill, 6, HoverColor);

                if (selected)
                    FillRounded(g, new Rectangle(r.Left + 8, r.Bottom - 3, r.Width - 16, 3), 1.5f, AccentColor);

                TextRenderer.DrawText(
                    g,
                    target.TabPages[i].Text,
                    selected ? boldFont : normalFont,
                    Rectangle.FromLTRB(r.Left + TabPadX / 2, r.Top, r.Right - TabPadX / 2, r.Bottom),
                    selected ? (focused ? AccentColor : SelectedTextColor) : TextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
                );
            }

            // 端のフェード（続きがあることを示す）
            const int fade = 24;
            if (offset > 0.5f)
            {
                using var brush = new LinearGradientBrush(new Rectangle(vp.Left - 1, 0, fade + 1, vp.Height),
                    StripColor, Color.FromArgb(0, StripColor), LinearGradientMode.Horizontal);
                g.FillRectangle(brush, vp.Left, 0, fade, vp.Height - 1);
            }
            if (offset < MaxOffset - 0.5f)
            {
                using var brush = new LinearGradientBrush(new Rectangle(vp.Right - fade - 1, 0, fade + 1, vp.Height),
                    Color.FromArgb(0, StripColor), StripColor, LinearGradientMode.Horizontal);
                g.FillRectangle(brush, vp.Right - fade, 0, fade, vp.Height - 1);
            }

            g.Restore(state);

            if (Overflow)
            {
                DrawArrow(g, LeftArrowRect, true, CanScrollLeft, hover == HitLeft);
                DrawArrow(g, RightArrowRect, false, CanScrollRight, hover == HitRight);
            }
        }

        private static void DrawArrow(Graphics g, Rectangle r, bool left, bool enabled, bool hovered)
        {
            if (enabled && hovered) FillRounded(g, r, 6, HoverColor);

            float cx = r.Left + r.Width / 2f, cy = r.Top + r.Height / 2f;
            float tip = left ? cx - 3 : cx + 3;
            float tail = left ? cx + 3 : cx - 3;
            using var pen = new Pen(enabled ? ArrowColor : DisabledColor, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawLines(pen, new[]
            {
                new PointF(tail, cy - 5),
                new PointF(tip, cy),
                new PointF(tail, cy + 5)
            });
        }

        private static void FillRounded(Graphics g, RectangleF r, float radius, Color color)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            using var path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) scrollTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
