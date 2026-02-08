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
        private Button btnBrowse;
        private Button btnExport;

        private RadioButton rbImportNew;
        private RadioButton rbImportUpdate;

        private ComboBox cmbGroup;
        private TextBox txtNewGroup;
        private Button btnImport;

        public PhraseTransferForm()
        {
            Text = LanguageManager.GetString("TransferTitle");
            Icon = Program.AppIcon;

            Width = 430;
            Height = 380;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            /* ===== フォーマット ===== */
            var gbFormat = new GroupBox
            {
                Text = LanguageManager.GetString("TransferFormat"),
                Left = 10,
                Top = 10,
                Width = 380,
                Height = 55
            };

            rbJson = new RadioButton
            {
                Text = LanguageManager.GetString("TransferFormatJson"),
                Left = 20,
                Top = 22,
                Checked = true
            };
            rbCsv = new RadioButton
            {
                Text = LanguageManager.GetString("TransferFormatCsv"),
                Left = 130,
                Top = 22
            };

            gbFormat.Controls.AddRange(new Control[] { rbJson, rbCsv });

            /* ===== Export ===== */
            var gbExport = new GroupBox
            {
                Text = LanguageManager.GetString("TransferExportTitle"),
                Left = 10,
                Top = 70,
                Width = 380,
                Height = 95
            };

            txtFolder = new TextBox { Left = 15, Top = 25, Width = 260 };

            btnBrowse = new Button
            {
                Text = LanguageManager.GetString("TransferBrowse"),
                Left = 285,
                Top = 23,
                Width = 70
            };
            btnBrowse.Click += (_, _) =>
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtFolder.Text = dlg.SelectedPath;
            };

            btnExport = new Button
            {
                Text = LanguageManager.GetString("TransferExport"),
                Left = 255,
                Top = 55,
                Width = 100
            };
            btnExport.Click += ExecuteExport;

            gbExport.Controls.AddRange(new Control[]
            {
                txtFolder, btnBrowse, btnExport
            });

            /* ===== Import ===== */
            var gbImport = new GroupBox
            {
                Text = LanguageManager.GetString("TransferImportTitle"),
                Left = 10,
                Top = 170,
                Width = 380,
                Height = 150
            };

            rbImportNew = new RadioButton
            {
                Text = LanguageManager.GetString("TransferImportNew"),
                Left = 15,
                Top = 25,
                Checked = true
            };

            rbImportUpdate = new RadioButton
            {
                Text = LanguageManager.GetString("TransferImportUpdate"),
                Left = 15,
                Top = 50
            };

            txtNewGroup = new TextBox
            {
                Left = 190,
                Top = 23,
                Width = 180
            };

            cmbGroup = new ComboBox
            {
                Left = 190,
                Top = 48,
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cmbGroup.Items.AddRange(TemplateRepository.GetGroupNames());

            rbImportNew.CheckedChanged += (_, _) => UpdateImportMode();
            rbImportUpdate.CheckedChanged += (_, _) => UpdateImportMode();

            btnImport = new Button
            {
                Text = LanguageManager.GetString("TransferImport"),
                Left = 255,
                Top = 105,
                Width = 100
            };
            btnImport.Click += ExecuteImport;

            gbImport.Controls.AddRange(new Control[]
            {
                rbImportNew, rbImportUpdate,
                txtNewGroup, cmbGroup,
                btnImport
            });

            Controls.AddRange(new Control[]
            {
                gbFormat,
                gbExport,
                gbImport
            });

            UpdateImportMode();
        }

        private void UpdateImportMode()
        {
            txtNewGroup.Enabled = rbImportNew.Checked;
            cmbGroup.Enabled = rbImportUpdate.Checked;
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

        private void ExecuteImport(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = rbJson.Checked
                    ? "JSON (*.json)|*.json"
                    : "CSV (*.csv)|*.csv"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string groupName;
            string modeText;

            if (rbImportNew.Checked)
            {
                groupName = txtNewGroup.Text.Trim();
                modeText = LanguageManager.GetString("TransferImportNew");
            }
            else
            {
                groupName = cmbGroup.SelectedItem as string;
                modeText = LanguageManager.GetString("TransferImportUpdate");
            }

            if (string.IsNullOrEmpty(groupName))
            {
                MessageBox.Show(LanguageManager.GetString("TransferSelectGroup"));
                return;
            }

            var confirm = MessageBox.Show(
                string.Format(
                    LanguageManager.GetString("TransferConfirmMessage"),
                    Path.GetFileName(dlg.FileName),
                    modeText,
                    groupName),
                LanguageManager.GetString("TransferConfirmTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
                return;

            var format = rbJson.Checked
                ? PhraseTransferFormat.Json
                : PhraseTransferFormat.Csv;

            if (!PhraseTransferService.Import(
                    dlg.FileName,
                    groupName,
                    format,
                    out var error))
            {
                MessageBox.Show(error);
                return;
            }

            MessageBox.Show(LanguageManager.GetString("TransferDone"));
            Close();
        }
    }
}
