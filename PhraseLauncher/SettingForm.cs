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

        public SettingForm()
        {
            this.Size = new Size(300, 150);
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

            btnSave = new Button { Location = new Point(185, 80), Width = 75 };
            btnSave.Click += (s, e) =>
            {
                LanguageManager.SaveLanguage(langCombo.SelectedIndex == 1 ? "en" : "ja");
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblLang, langCombo, btnSave });
            UpdateText();
        }

        private void UpdateText()
        {
            this.Text = LanguageManager.GetString("SettingTitle");
            lblLang.Text = LanguageManager.GetString("SettingLang");
            btnSave.Text = LanguageManager.GetString("BtnSave");
        }
    }
}