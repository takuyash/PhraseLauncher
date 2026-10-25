using System;
using System.Drawing;
using System.Windows.Forms;

namespace PhraseLauncher
{
    public class SettingForm : Form
    {
        private const int Margin_ = 16;    // フォーム外周の余白
        private const int Pad = 16;        // カード内の左右余白
        private const int CardWidth = 388;
        private const int RowHeight = 52;

        private ComboBox langCombo;
        private Label lblLang;
        private Button btnSave;
        private CheckBox chkEnableHotKey;
        private Label lblEnableHotKey;
        private CheckBox chkEncrypt;
        private Label lblEncrypt;

        private ComboBox cmbCtrlCount;
        private Label lblCtrlCount;

        private ComboBox cmbTriggerKey;
        private Label lblTriggerKey;

        public SettingForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Icon = Program.AppIcon;
            this.BackColor = UiTheme.Surface;
            this.Font = UiTheme.Font;

            langCombo = CreateCombo(180);
            langCombo.Items.Add("日本語 (Japanese)");
            langCombo.Items.Add("English");
            langCombo.SelectedIndex = LanguageManager.CurrentLanguage == "en" ? 1 : 0;

            chkEnableHotKey = new ToggleSwitch { Checked = LanguageManager.EnableHotKey };
            chkEncrypt = new ToggleSwitch { Checked = LanguageManager.EnableTemplateEncryption };

            cmbTriggerKey = CreateCombo(120);
            cmbTriggerKey.Items.AddRange(new object[] { "Ctrl", "Shift", "Alt", "Space" });
            cmbTriggerKey.SelectedItem = LanguageManager.TriggerKey;

            cmbCtrlCount = CreateCombo(80);
            cmbCtrlCount.Items.AddRange(new object[] { "2", "3", "4", "5" });
            cmbCtrlCount.SelectedItem = LanguageManager.CtrlPressCount.ToString();

            lblLang = CreateLabel();
            lblEnableHotKey = CreateLabel(chkEnableHotKey);
            lblTriggerKey = CreateLabel();
            lblCtrlCount = CreateLabel();
            lblEncrypt = CreateLabel(chkEncrypt);

            // 設定項目を1枚のカードに行として並べる
            var rows = new (Label label, Control control)[]
            {
                (lblLang, langCombo),
                (lblEnableHotKey, chkEnableHotKey),
                (lblTriggerKey, cmbTriggerKey),
                (lblCtrlCount, cmbCtrlCount),
                (lblEncrypt, chkEncrypt)
            };

            var card = new CardPanel
            {
                Bounds = new Rectangle(Margin_, Margin_, CardWidth, RowHeight * rows.Length)
            };

            for (int i = 0; i < rows.Length; i++)
            {
                var (label, control) = rows[i];
                int top = i * RowHeight;

                control.Location = new Point(CardWidth - Pad - control.Width, top + (RowHeight - control.Height) / 2);
                label.Bounds = new Rectangle(Pad, top, control.Left - Pad - 12, RowHeight);
                card.Controls.Add(label);
                card.Controls.Add(control);

                if (i > 0)
                    card.Controls.Add(new Panel { Bounds = new Rectangle(Pad, top, CardWidth - Pad * 2, 1), BackColor = UiTheme.Divider });
            }

            btnSave = new ModernButton { Variant = ButtonVariant.Primary };
            btnSave.Click += (s, e) =>
            {
                bool oldEnc = LanguageManager.EnableTemplateEncryption;

                LanguageManager.SaveLanguage(langCombo.SelectedIndex == 1 ? "en" : "ja");
                LanguageManager.SaveEnableHotKey(chkEnableHotKey.Checked);
                LanguageManager.SaveTriggerKey(cmbTriggerKey.SelectedItem.ToString());
                LanguageManager.SaveCtrlPressCount(int.Parse(cmbCtrlCount.SelectedItem.ToString()));
                LanguageManager.SaveTemplateEncryption(chkEncrypt.Checked);

                if (oldEnc != chkEncrypt.Checked)
                    TemplateRepository.ApplyEncryptionSetting(chkEncrypt.Checked);

                this.Close();
            };

            ClientSize = new Size(CardWidth + Margin_ * 2, card.Bottom + 16 + btnSave.Height + Margin_);

            Controls.AddRange(new Control[] { card, btnSave });

            UpdateText();
        }

        private static ComboBox CreateCombo(int width) => new()
        {
            Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UiTheme.LargeFont
        };

        // toggle を渡すと、ラベルのクリックでも切り替えられる
        private static Label CreateLabel(CheckBox? toggle = null)
        {
            var label = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = UiTheme.Font,
                ForeColor = UiTheme.Text,
                AutoEllipsis = true
            };
            if (toggle != null)
            {
                label.Cursor = Cursors.Hand;
                label.Click += (s, e) => toggle.Checked = !toggle.Checked;
            }
            return label;
        }

        // 行レイアウトでは末尾のコロンは不要
        private static string RowText(string key) =>
            LanguageManager.GetString(key).TrimEnd(':', '：', ' ');

        private void UpdateText()
        {
            this.Text = LanguageManager.GetString("SettingTitle");
            lblLang.Text = RowText("SettingLang");
            lblEnableHotKey.Text = RowText("SettingEnableHotKey");
            lblEncrypt.Text = RowText("SettingEncryptTemplate");
            lblTriggerKey.Text = RowText("SettingTriggerKey");
            lblCtrlCount.Text = RowText("SettingLaunchKeyCount");
            btnSave.Text = LanguageManager.GetString("BtnSave");

            // 文字数で幅が変わるので右下に合わせ直す
            btnSave.Width = Math.Max(btnSave.Width, 96);
            btnSave.Location = new Point(ClientSize.Width - Margin_ - btnSave.Width, ClientSize.Height - Margin_ - btnSave.Height);
        }
    }
}
