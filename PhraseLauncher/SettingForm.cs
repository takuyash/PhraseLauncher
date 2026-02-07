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

        private ComboBox cmbCtrlCount;
        private Label lblCtrlCount;

        public SettingForm()
        {
            this.Size = new Size(300, 240);
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

            lblCtrlCount = new Label
            {
                Location = new Point(20, 110),
                AutoSize = true
            };

            cmbCtrlCount = new ComboBox
            {
                Location = new Point(20, 135),
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCtrlCount.Items.AddRange(new object[] { "2", "3", "4", "5" });
            cmbCtrlCount.SelectedItem = LanguageManager.CtrlPressCount.ToString();

            btnSave = new Button { Location = new Point(185, 170), Width = 75 };
            btnSave.Click += (s, e) =>
            {
                LanguageManager.SaveLanguage(langCombo.SelectedIndex == 1 ? "en" : "ja");
                LanguageManager.SaveEnableHotKey(chkEnableHotKey.Checked);
                LanguageManager.SaveCtrlPressCount(int.Parse(cmbCtrlCount.SelectedItem.ToString()));
                this.Close();
            };

            this.Controls.AddRange(new Control[]
            {
                lblLang,
                langCombo,
                chkEnableHotKey,
                lblCtrlCount,
                cmbCtrlCount,
                btnSave
            });

            UpdateText();
        }

        private void UpdateText()
        {
            this.Text = LanguageManager.GetString("SettingTitle");
            lblLang.Text = LanguageManager.GetString("SettingLang");
            chkEnableHotKey.Text = LanguageManager.GetString("SettingEnableHotKey");
            lblCtrlCount.Text = LanguageManager.GetString("SettingLaunchKeyCount");
            btnSave.Text = LanguageManager.GetString("BtnSave");
        }
    }
}
