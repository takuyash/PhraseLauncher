using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhraseLauncher
{
    enum MenuIcon { List, Edit, Transfer, Settings, Help, Exit }

    // タスクトレイ用のモダンなコンテキストメニュー
    class ModernContextMenu : ContextMenuStrip
    {
        private readonly ModernMenuRenderer renderer = new();

        public ModernContextMenu()
        {
            Renderer = renderer;
            Font = UiTheme.Font;
            BackColor = Color.White;
            Padding = new Padding(4, 6, 4, 6);
            ImageScalingSize = new Size(16, 16);
            ShowImageMargin = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Windows 11 ではウィンドウ自体を角丸にする（OSが枠も描くので自前の枠は不要）
            try
            {
                int pref = NativeMethods.DWMWCP_ROUND;
                renderer.DrawBorder = NativeMethods.DwmSetWindowAttribute(
                    Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)) != 0;
            }
            catch
            {
                renderer.DrawBorder = true;
            }
        }

        // 見出し（アプリ名）
        public void AddHeader(string text)
        {
            Items.Add(new ToolStripLabel(text)
            {
                Font = UiTheme.BoldFont,
                ForeColor = UiTheme.TextStrong,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 4, 0, 4)
            });
            Items.Add(new ToolStripSeparator());
        }

        public ToolStripMenuItem AddItem(string text, MenuIcon icon, EventHandler onClick, bool danger = false)
        {
            var item = new ToolStripMenuItem(text, CreateIcon(icon, danger ? UiTheme.Danger : UiTheme.TextMuted), onClick)
            {
                Padding = new Padding(4, 6, 4, 6),
                Tag = danger ? ModernMenuRenderer.DangerTag : null
            };
            Items.Add(item);
            return item;
        }

        /* ================= アイコン描画 ================= */

        private static Bitmap CreateIcon(MenuIcon icon, Color color)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var brush = new SolidBrush(color);

            switch (icon)
            {
                case MenuIcon.List:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 4 + i * 4;
                        g.FillEllipse(brush, 1.5f, y - 1, 2, 2);
                        g.DrawLine(pen, 6, y, 14, y);
                    }
                    break;

                case MenuIcon.Edit:
                    g.DrawPolygon(pen, new[] { new PointF(10.5f, 2.5f), new PointF(13.5f, 5.5f), new PointF(5.5f, 13.5f), new PointF(2.5f, 13.5f), new PointF(2.5f, 10.5f) });
                    g.DrawLine(pen, 8.5f, 4.5f, 11.5f, 7.5f);
                    break;

                case MenuIcon.Transfer:
                    // 上下の矢印
                    g.DrawLine(pen, 5, 2.5f, 5, 13);
                    g.DrawLines(pen, new[] { new PointF(2, 5.5f), new PointF(5, 2.5f), new PointF(8, 5.5f) });
                    g.DrawLine(pen, 11, 3, 11, 13.5f);
                    g.DrawLines(pen, new[] { new PointF(8, 10.5f), new PointF(11, 13.5f), new PointF(14, 10.5f) });
                    break;

                case MenuIcon.Settings:
                    // 歯車（8本の歯＋輪）
                    for (int i = 0; i < 8; i++)
                    {
                        double a = Math.PI / 4 * i;
                        g.DrawLine(pen,
                            8 + (float)Math.Cos(a) * 5, 8 + (float)Math.Sin(a) * 5,
                            8 + (float)Math.Cos(a) * 6.8f, 8 + (float)Math.Sin(a) * 6.8f);
                    }
                    g.DrawEllipse(pen, 3.5f, 3.5f, 9, 9);
                    g.DrawEllipse(pen, 6.3f, 6.3f, 3.4f, 3.4f);
                    break;

                case MenuIcon.Help:
                    g.DrawEllipse(pen, 1.5f, 1.5f, 13, 13);
                    g.DrawArc(pen, 6, 4.2f, 4.2f, 4, 180, 250);
                    g.DrawLine(pen, 8.4f, 8.2f, 8, 9.2f);
                    g.FillEllipse(brush, 7.1f, 10.8f, 1.8f, 1.8f);
                    break;

                case MenuIcon.Exit:
                    // 電源マーク
                    g.DrawArc(pen, 2.5f, 3, 11, 11, -60, 300);
                    g.DrawLine(pen, 8, 1.5f, 8, 7.5f);
                    break;
            }
            return bmp;
        }
    }

    class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        public const string DangerTag = "danger";

        public bool DrawBorder { get; set; } = true;

        public ModernMenuRenderer() : base(new ModernColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(Color.White);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (!DrawBorder) return;
            using var pen = new Pen(UiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        // 画像列の背景は塗らない（白で統一）
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool danger = Equals(e.Item.Tag, DangerTag);
            var r = new RectangleF(2, 1, e.Item.Width - 4, e.Item.Height - 2);
            using var path = UiTheme.RoundedPath(r, 6);
            using var brush = new SolidBrush(danger ? UiTheme.DangerSoft : UiTheme.Hover);
            g.FillPath(brush, path);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item is ToolStripMenuItem)
                e.TextColor = Equals(e.Item.Tag, DangerTag) ? UiTheme.Danger : UiTheme.TextStrong;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var pen = new Pen(UiTheme.Divider);
            e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
        }

        private class ModernColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;
            public override Color MenuBorder => UiTheme.Border;
            public override Color MenuItemBorder => Color.Transparent;
            public override Color MenuItemSelected => UiTheme.Hover;
            public override Color SeparatorDark => UiTheme.Divider;
            public override Color SeparatorLight => Color.White;
        }
    }
}
