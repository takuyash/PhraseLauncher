using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PhraseLauncher
{
    // アプリ共通の配色・フォント
    static class UiTheme
    {
        public static readonly Color Surface = Color.FromArgb(247, 248, 250);
        public static readonly Color Border = Color.FromArgb(214, 218, 225);
        public static readonly Color Divider = Color.FromArgb(235, 237, 241);
        public static readonly Color Hover = Color.FromArgb(234, 236, 240);
        public static readonly Color Pressed = Color.FromArgb(225, 228, 233);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color AccentHover = Color.FromArgb(29, 78, 216);
        public static readonly Color AccentPressed = Color.FromArgb(30, 64, 175);
        public static readonly Color AccentSoft = Color.FromArgb(232, 240, 254);
        public static readonly Color Selection = Color.FromArgb(219, 234, 254);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);
        public static readonly Color DangerSoft = Color.FromArgb(254, 242, 242);
        public static readonly Color DangerBorder = Color.FromArgb(248, 180, 180);
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color TextStrong = Color.FromArgb(17, 24, 39);
        public static readonly Color TextMuted = Color.FromArgb(95, 102, 115);
        public static readonly Color Disabled = Color.FromArgb(203, 206, 212);

        public static readonly Font Font = CreateFont(9.5f, FontStyle.Regular);
        public static readonly Font BoldFont = CreateFont(9.5f, FontStyle.Bold);
        public static readonly Font LargeFont = CreateFont(10f, FontStyle.Regular);
        public static readonly Font TitleFont = CreateFont(15f, FontStyle.Bold);

        private static Font CreateFont(float size, FontStyle style)
        {
            var font = new Font("Yu Gothic UI", size, style);
            if (font.Name == "Yu Gothic UI") return font;
            font.Dispose();
            return new Font(SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif, size, style);
        }

        public static GraphicsPath RoundedPath(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
