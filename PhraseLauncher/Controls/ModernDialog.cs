using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PhraseLauncher
{
    enum DialogIcon { None, Info, Success, Warning, Error, Question }

    // MessageBox / InputBox の代わりに使うダイアログ
    static class ModernDialog
    {
        private const int Pad = 24;
        private const int IconSize = 40;
        private const int FooterHeight = 60;

        /// <summary>
        /// メッセージを表示する。danger = true で「はい/OK」を削除用の赤いボタンにする
        /// </summary>
        public static DialogResult Show(
            string text,
            string? title = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            DialogIcon icon = DialogIcon.None,
            bool danger = false)
        {
            using var form = CreateForm(title);

            int textLeft = icon == DialogIcon.None ? Pad : Pad + IconSize + 16;
            var flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix;
            var measured = TextRenderer.MeasureText(text, UiTheme.LargeFont, new Size(380, 0), flags);
            int textWidth = Math.Max(220, measured.Width + 4);
            int bodyHeight = Math.Max(IconSize, measured.Height) + Pad * 2;

            if (icon != DialogIcon.None)
                form.Controls.Add(new IconBadge(icon) { Bounds = new Rectangle(Pad, Pad, IconSize, IconSize) });

            // 1行なら中央揃え、複数行なら上揃えでアイコンと並べる
            int textTop = measured.Height < IconSize ? Pad + (IconSize - measured.Height) / 2 : Pad;
            form.Controls.Add(new Label
            {
                Text = text,
                Font = UiTheme.LargeFont,
                ForeColor = UiTheme.TextStrong,
                UseMnemonic = false,
                Bounds = new Rectangle(textLeft, textTop, textWidth, measured.Height + 4)
            });

            var defs = buttons switch
            {
                MessageBoxButtons.OKCancel => new[] { (DialogResult.OK, "DialogOK"), (DialogResult.Cancel, "DialogCancel") },
                MessageBoxButtons.YesNo => new[] { (DialogResult.Yes, "DialogYes"), (DialogResult.No, "DialogNo") },
                MessageBoxButtons.YesNoCancel => new[] { (DialogResult.Yes, "DialogYes"), (DialogResult.No, "DialogNo"), (DialogResult.Cancel, "DialogCancel") },
                _ => new[] { (DialogResult.OK, "DialogOK") }
            };

            int width = Math.Max(340, textLeft + textWidth + Pad);
            form.ClientSize = new Size(width, bodyHeight + FooterHeight);

            var created = AddFooter(form, width, bodyHeight, defs, danger);
            form.AcceptButton = created[0];
            // Esc・×ボタンは最後のボタン（いいえ／キャンセル）扱い。OKのみならOK
            form.CancelButton = created[^1];

            return ShowModal(form, created[^1].DialogResult);
        }

        /// <summary>
        /// 文字列を入力させる。キャンセル時は空文字を返す（InputBox と同じ）
        /// </summary>
        public static string Input(string prompt, string title, string defaultText = "")
        {
            using var form = CreateForm(title);
            const int width = 380;

            var label = new Label
            {
                Text = prompt,
                Font = UiTheme.Font,
                ForeColor = UiTheme.TextStrong,
                UseMnemonic = false,
                AutoEllipsis = true,
                Bounds = new Rectangle(Pad, 20, width - Pad * 2, 22)
            };

            var input = new ModernTextBox { Bounds = new Rectangle(Pad, 48, width - Pad * 2, 34) };
            input.TextBox.Font = UiTheme.LargeFont;
            input.TextBox.Text = defaultText;

            form.Controls.Add(label);
            form.Controls.Add(input);

            const int bodyHeight = 104;
            form.ClientSize = new Size(width, bodyHeight + FooterHeight);

            var created = AddFooter(form, width, bodyHeight,
                new[] { (DialogResult.OK, "DialogOK"), (DialogResult.Cancel, "DialogCancel") }, false);
            var ok = created[0];
            form.AcceptButton = ok;
            form.CancelButton = created[1];

            // 空欄のときはOKを押せないようにする
            void updateOk() => ok.Enabled = !string.IsNullOrWhiteSpace(input.TextBox.Text);
            input.TextBox.TextChanged += (s, e) => updateOk();
            updateOk();

            form.Shown += (s, e) =>
            {
                input.TextBox.Focus();
                input.TextBox.SelectAll();
            };

            return ShowModal(form, DialogResult.Cancel) == DialogResult.OK ? input.TextBox.Text : "";
        }

        /* ================= 共通 ================= */

        private static Form CreateForm(string? title) => new()
        {
            Text = string.IsNullOrEmpty(title) ? "PhraseLauncher" : title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ShowIcon = false,
            BackColor = Color.White,
            Font = UiTheme.Font
        };

        private static List<Button> AddFooter(Form form, int width, int top,
            (DialogResult result, string key)[] defs, bool danger)
        {
            var footer = new Panel
            {
                Bounds = new Rectangle(0, top, width, FooterHeight),
                BackColor = UiTheme.Surface
            };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(UiTheme.Divider);
                e.Graphics.DrawLine(pen, 0, 0, width, 0);
            };

            var created = new List<Button>();
            int right = width - 16;
            // 右から「いいえ／キャンセル」→「はい／OK」の順に並べる
            for (int i = defs.Length - 1; i >= 0; i--)
            {
                bool primary = i == 0;
                var btn = new ModernButton
                {
                    Text = LanguageManager.GetString(defs[i].key),
                    DialogResult = defs[i].result,
                    Variant = primary ? (danger ? ButtonVariant.Danger : ButtonVariant.Primary) : ButtonVariant.Secondary
                };
                btn.Width = Math.Max(btn.Width, 88);
                btn.Location = new Point(right - btn.Width, (FooterHeight - btn.Height) / 2);
                right = btn.Left - 8;
                footer.Controls.Add(btn);
                created.Insert(0, btn);
            }

            form.Controls.Add(footer);
            return created;
        }

        private static DialogResult ShowModal(Form form, DialogResult closeResult)
        {
            var owner = Form.ActiveForm;
            form.StartPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
            // タスクトレイから呼ばれた場合も前面に出す
            form.TopMost = owner?.TopMost ?? true;

            var result = owner != null ? form.ShowDialog(owner) : form.ShowDialog();
            // ×ボタンで閉じた場合
            return result == DialogResult.Cancel ? closeResult : result;
        }

        // 丸背景のアイコン
        private class IconBadge : Control
        {
            private readonly DialogIcon icon;

            public IconBadge(DialogIcon icon)
            {
                this.icon = icon;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var (fore, back) = icon switch
                {
                    DialogIcon.Success => (Color.FromArgb(22, 163, 74), Color.FromArgb(220, 252, 231)),
                    DialogIcon.Warning => (Color.FromArgb(217, 119, 6), Color.FromArgb(254, 243, 199)),
                    DialogIcon.Error => (UiTheme.Danger, Color.FromArgb(254, 226, 226)),
                    _ => (UiTheme.Accent, UiTheme.AccentSoft)
                };

                using (var brush = new SolidBrush(back))
                    g.FillEllipse(brush, 0, 0, Width - 1, Height - 1);

                float c = Width / 2f;
                using var pen = new Pen(fore, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                using var dot = new SolidBrush(fore);

                switch (icon)
                {
                    case DialogIcon.Success:
                        g.DrawLines(pen, new[] { new PointF(c - 7, c), new PointF(c - 2, c + 5), new PointF(c + 7, c - 5) });
                        break;
                    case DialogIcon.Error:
                        g.DrawLine(pen, c - 6, c - 6, c + 6, c + 6);
                        g.DrawLine(pen, c + 6, c - 6, c - 6, c + 6);
                        break;
                    case DialogIcon.Warning:
                        g.DrawLine(pen, c, c - 8, c, c + 2);
                        g.FillEllipse(dot, c - 1.6f, c + 5.5f, 3.2f, 3.2f);
                        break;
                    case DialogIcon.Question:
                        g.DrawArc(pen, c - 5, c - 9, 10, 9, 180, 250);
                        g.DrawLine(pen, c + 0.5f, c - 0.5f, c, c + 2);
                        g.FillEllipse(dot, c - 1.6f, c + 5.5f, 3.2f, 3.2f);
                        break;
                    default: // Info
                        g.FillEllipse(dot, c - 1.6f, c - 9, 3.2f, 3.2f);
                        g.DrawLine(pen, c, c - 2, c, c + 8);
                        break;
                }
            }
        }
    }
}
