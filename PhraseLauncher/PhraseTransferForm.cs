using System;
using System.IO;
using System.Windows.Forms;

namespace PhraseLauncher
{
    public class PhraseTransferForm : Form
    {
        private RadioButton rbJson;
        private RadioButton rbCsv;
        private TextBox txtFolder;

        public PhraseTransferForm()
        {
            Text = LanguageManager.GetString("TransferTitle");
            Icon = Program.AppIcon;

            Width = 420;
            Height = 200;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            rbJson = new RadioButton { Text = "JSON", Left = 20, Top = 20, Checked = true };
            rbCsv = new RadioButton { Text = "CSV", Left = 130, Top = 20 };

            txtFolder = new TextBox { Left = 20, Top = 55, Width = 280 };

            var btnBrowse = new Button { Text = "...", Left = 310, Top = 53, Width = 60 };
            btnBrowse.Click += (_, _) =>
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtFolder.Text = dlg.SelectedPath;
            };

            var btnExec = new Button
            {
                Text = LanguageManager.GetString("TransferExport"),
                Left = 150,
                Top = 100,
                Width = 100
            };
            btnExec.Click += ExecuteExport;

            Controls.AddRange(new Control[]
            {
                rbJson, rbCsv, txtFolder, btnBrowse, btnExec
            });
        }

        private void ExecuteExport(object sender, EventArgs e)
        {
            if (!Directory.Exists(txtFolder.Text))
            {
                MessageBox.Show(LanguageManager.GetString("TransferSelectFolder"));
                return;
            }

            var format = rbJson.Checked
                ? PhraseTransferFormat.Json
                : PhraseTransferFormat.Csv;

            PhraseTransferService.ExportAllGroups(txtFolder.Text, format);

            MessageBox.Show(LanguageManager.GetString("TransferDone"));
            Close();
        }
    }
}
