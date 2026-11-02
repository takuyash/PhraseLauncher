using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{
    // ON/OFF のトグルスイッチ（CheckBox として扱える）
    class ToggleSwitch : CheckBox
    {
        private static readonly Color OffTrack = Color.FromArgb(209, 213, 219);
        private static readonly Color OffTrackHover = Color.FromArgb(190, 195, 203);

        private readonly Timer animTimer = new() { Interval = 15 };
        private float knobPos; // 0:OFF ～ 1:ON
        private bool hovered;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = false;
            Size = new Size(44, 24);
            Cursor = Cursors.Hand;

            animTimer.Tick += (s, e) =>
            {
                float target = Checked ? 1 : 0;
                knobPos += (target - knobPos) * 0.35f;
                if (Math.Abs(target - knobPos) < 0.02f)
                {
                    knobPos = target;
                    animTimer.Stop();
                }
                Invalidate();
            };
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            knobPos = Checked ? 1 : 0; // 初期表示はアニメーションなし
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            if (IsHandleCreated) animTimer.Start();
            else knobPos = Checked ? 1 : 0;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var track = new RectangleF(1, 1, Width - 2, Height - 2);
            Color off = hovered ? OffTrackHover : OffTrack;
            Color on = hovered ? UiTheme.AccentHover : UiTheme.Accent;
            using (var path = UiTheme.RoundedPath(track, track.Height / 2))
            using (var brush = new SolidBrush(Blend(off, on, knobPos)))
                g.FillPath(brush, path);

            if (Focused && ShowFocusCues)
            {
                using var path = UiTheme.RoundedPath(RectangleF.Inflate(track, 0.5f, 0.5f), track.Height / 2);
                using var pen = new Pen(UiTheme.AccentPressed, 1.5f);
                g.DrawPath(pen, path);
            }

            float knob = track.Height - 6;
            float x = track.Left + 3 + (track.Width - knob - 6) * knobPos;
            using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                g.FillEllipse(shadow, x, track.Top + 3.8f, knob, knob);
            g.FillEllipse(Brushes.White, x, track.Top + 3, knob, knob);
        }

        private static Color Blend(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));

        protected override void Dispose(bool disposing)
        {
            if (disposing) animTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
