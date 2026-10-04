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
        private CheckBox chkEncrypt;

        private ComboBox cmbCtrlCount;
        private Label lblCtrlCount;

        private ComboBox cmbTriggerKey;
        private Label lblTriggerKey;

        public SettingForm()
        {
            this.Size = new Size(320, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Icon = Program.AppIcon;

            lblLang = new Label { Location = new Point(20, 20), AutoSize = true };
            langCombo = new ComboBox { Location = new Point(20, 45), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            langCombo.Items.Add("日本語 (Japanese)");
            langCombo.Items.Add("English");
            langCombo.SelectedIndex = LanguageManager.CurrentLanguage == "en" ? 1 : 0;

            chkEnableHotKey = new CheckBox
            {
                Location = new Point(20, 80),
                Width = 260,
                Checked = LanguageManager.EnableHotKey
            };

            chkEncrypt = new CheckBox
            {
                Location = new Point(20, 110),
                Width = 260,
                Checked = LanguageManager.EnableTemplateEncryption
            };

            lblTriggerKey = new Label { Location = new Point(20, 145), AutoSize = true };
            cmbTriggerKey = new ComboBox { Location = new Point(20, 170), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTriggerKey.Items.AddRange(new object[] { "Ctrl", "Shift", "Alt", "Space" });
            cmbTriggerKey.SelectedItem = LanguageManager.TriggerKey;

            lblCtrlCount = new Label { Location = new Point(160, 145), AutoSize = true };
            cmbCtrlCount = new ComboBox { Location = new Point(160, 170), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCtrlCount.Items.AddRange(new object[] { "2", "3", "4", "5" });
            cmbCtrlCount.SelectedItem = LanguageManager.CtrlPressCount.ToString();

            btnSave = new Button { Location = new Point(205, 240), Width = 75 };
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

            Controls.AddRange(new Control[]
            {
                lblLang, langCombo,
                chkEnableHotKey,
                chkEncrypt,
                lblTriggerKey, cmbTriggerKey,
                lblCtrlCount, cmbCtrlCount,
                btnSave
            });

            UpdateText();
        }

        private void UpdateText()
        {
            this.Text = LanguageManager.GetString("SettingTitle");
            lblLang.Text = LanguageManager.GetString("SettingLang");
            chkEnableHotKey.Text = LanguageManager.GetString("SettingEnableHotKey");
            chkEncrypt.Text = LanguageManager.GetString("SettingEncryptTemplate");
            lblTriggerKey.Text = LanguageManager.GetString("SettingTriggerKey");
            lblCtrlCount.Text = LanguageManager.GetString("SettingLaunchKeyCount");
            btnSave.Text = LanguageManager.GetString("BtnSave");
        }
    }
}
