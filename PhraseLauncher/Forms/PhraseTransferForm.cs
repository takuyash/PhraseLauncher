using System;
using System.Drawing;
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

            const int margin = 16;     // フォーム外周・カード間の余白
            const int pad = 16;        // カード内の余白
            const int cardWidth = 428;
            const int rowTop = CardPanel.TitleHeight + 4;

            ClientSize = new Size(cardWidth + margin * 2, 464);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Surface;
            Font = UiTheme.Font;

            /* ===== フォーマット ===== */
            var cardFormat = new CardPanel
            {
                Title = LanguageManager.GetString("TransferFormat"),
                Bounds = new Rectangle(margin, margin, cardWidth, 92)
            };

            rbJson = new ChipRadioButton
            {
                Text = LanguageManager.GetString("TransferFormatJson"),
                Checked = true
            };
            rbJson.Location = new Point(pad, rowTop);

            rbCsv = new ChipRadioButton { Text = LanguageManager.GetString("TransferFormatCsv") };
            rbCsv.Location = new Point(rbJson.Right + 8, rowTop);

            cardFormat.Controls.AddRange(new Control[] { rbJson, rbCsv });

            /* ===== Export ===== */
            var cardExport = new CardPanel
            {
                Title = LanguageManager.GetString("TransferExportTitle"),
                Bounds = new Rectangle(margin, cardFormat.Bottom + 12, cardWidth, 136)
            };

            btnBrowse = new ModernButton { Text = LanguageManager.GetString("TransferBrowse") };
            btnBrowse.Location = new Point(cardWidth - pad - btnBrowse.Width, rowTop);

            var folderBox = new ModernTextBox
            {
                Bounds = new Rectangle(pad, rowTop, btnBrowse.Left - 8 - pad, 32)
            };
            txtFolder = folderBox.TextBox;

            btnBrowse.Click += (_, _) =>
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtFolder.Text = dlg.SelectedPath;
            };

            btnExport = new ModernButton
            {
                Text = LanguageManager.GetString("TransferExport"),
                Variant = ButtonVariant.Primary
            };
            btnExport.Width = Math.Max(btnExport.Width, 110);
            btnExport.Location = new Point(cardWidth - pad - btnExport.Width, rowTop + 44);
            btnExport.Click += ExecuteExport;

            cardExport.Controls.AddRange(new Control[]
            {
                folderBox, btnBrowse, btnExport
            });

            /* ===== Import ===== */
            var cardImport = new CardPanel
            {
                Title = LanguageManager.GetString("TransferImportTitle"),
                Bounds = new Rectangle(margin, cardExport.Bottom + 12, cardWidth, 180)
            };

            rbImportNew = new RadioButton
            {
                Text = LanguageManager.GetString("TransferImportNew"),
                Checked = true,
                AutoSize = false,
                Height = 32,
                Font = UiTheme.Font, // 幅の計測前にフォントを確定させる
                ForeColor = UiTheme.Text
            };

            rbImportUpdate = new RadioButton
            {
                Text = LanguageManager.GetString("TransferImportUpdate"),
                AutoSize = false,
                Height = 32,
                Font = UiTheme.Font,
                ForeColor = UiTheme.Text
            };

            // 入力欄はラジオボタンの文字幅に合わせて揃える
            int radioWidth = Math.Max(rbImportNew.PreferredSize.Width, rbImportUpdate.PreferredSize.Width) + 8;
            int inputLeft = Math.Max(170, pad + radioWidth + 12);
            int inputWidth = cardWidth - pad - inputLeft;

            rbImportNew.Bounds = new Rectangle(pad, rowTop, radioWidth, 32);
            rbImportUpdate.Bounds = new Rectangle(pad, rowTop + 40, radioWidth, 32);

            var newGroupBox = new ModernTextBox
            {
                Bounds = new Rectangle(inputLeft, rowTop, inputWidth, 32)
            };
            txtNewGroup = newGroupBox.TextBox;

            cmbGroup = new ComboBox
            {
                Width = inputWidth,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UiTheme.LargeFont,
                Enabled = false
            };
            cmbGroup.Location = new Point(inputLeft, rowTop + 40 + (32 - cmbGroup.Height) / 2);
            cmbGroup.Items.AddRange(TemplateRepository.GetGroupNames());

            rbImportNew.CheckedChanged += (_, _) => UpdateImportMode();
            rbImportUpdate.CheckedChanged += (_, _) => UpdateImportMode();

            btnImport = new ModernButton
            {
                Text = LanguageManager.GetString("TransferImport"),
                Variant = ButtonVariant.Primary
            };
            btnImport.Width = Math.Max(btnImport.Width, 110);
            btnImport.Location = new Point(cardWidth - pad - btnImport.Width, rowTop + 88);
            btnImport.Click += ExecuteImport;

            cardImport.Controls.AddRange(new Control[]
            {
                rbImportNew, rbImportUpdate,
                newGroupBox, cmbGroup,
                btnImport
            });

            Controls.AddRange(new Control[]
            {
                cardFormat,
                cardExport,
                cardImport
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
                ModernDialog.Show(LanguageManager.GetString("TransferSelectFolder"), icon: DialogIcon.Warning);
                return;
            }

            var format = rbJson.Checked
                ? PhraseTransferFormat.Json
                : PhraseTransferFormat.Csv;

            PhraseTransferService.ExportAllGroups(txtFolder.Text, format);

            ModernDialog.Show(LanguageManager.GetString("TransferDone"), icon: DialogIcon.Success);
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
                ModernDialog.Show(LanguageManager.GetString("TransferSelectGroup"), icon: DialogIcon.Warning);
                return;
            }

            var confirm = ModernDialog.Show(
                string.Format(
                    LanguageManager.GetString("TransferConfirmMessage"),
                    Path.GetFileName(dlg.FileName),
                    modeText,
                    groupName),
                LanguageManager.GetString("TransferConfirmTitle"),
                MessageBoxButtons.YesNo,
                DialogIcon.Warning
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
                ModernDialog.Show(error, icon: DialogIcon.Error);
                return;
            }

            ModernDialog.Show(LanguageManager.GetString("TransferDone"), icon: DialogIcon.Success);
            Close();
        }
    }
}
