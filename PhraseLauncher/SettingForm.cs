using System;
using System.Drawing;
using System.Windows.Forms;

namespace PhraseLauncher
{
    public class SettingForm : Form
    {
        private ComboBox langCombo;
        private Label lblLang;
        private Button btnSave;
        private CheckBox chkEnableHotKey;

        public SettingForm()
        {
            this.Size = new Size(300, 190);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Icon = Program.AppIcon;

            lblLang = new Label { Location = new Point(20, 20), AutoSize = true };
            langCombo = new ComboBox { Location = new Point(20, 45), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            langCombo.Items.Add("日本語 (Japanese)");
            langCombo.Items.Add("English");
            langCombo.SelectedIndex = LanguageManager.CurrentLanguage == "en" ? 1 : 0;

            chkEnableHotKey = new CheckBox
            {
                Location = new Point(20, 80),
                Width = 240,
                Checked = LanguageManager.EnableHotKey
            };

            btnSave = new Button { Location = new Point(185, 120), Width = 75 };
            btnSave.Click += (s, e) =>
            {
                LanguageManager.SaveLanguage(langCombo.SelectedIndex == 1 ? "en" : "ja");
                LanguageManager.SaveEnableHotKey(chkEnableHotKey.Checked);
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblLang, langCombo, chkEnableHotKey, btnSave });
            UpdateText();
        }

        private void UpdateText()
        {
            this.Text = LanguageManager.GetString("SettingTitle");
            lblLang.Text = LanguageManager.GetString("SettingLang");
            chkEnableHotKey.Text = "Enable HotKey";
            btnSave.Text = LanguageManager.GetString("BtnSave");
        }
    }
}
