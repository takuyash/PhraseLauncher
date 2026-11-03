using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PhraseLauncher
{
    /// <summary>
    /// グループ（タブ）の対象アプリを登録するダイアログ
    /// </summary>
    class GroupAppsForm : Form
    {
        private const int Pad = 20;
        private const int FormWidth = 460;

        private readonly ListBox appList = new();
        private readonly ModernTextBox appInput = new();

        public List<string> ResultApps { get; private set; } = new();

        public GroupAppsForm(string groupName, IEnumerable<string> apps)
        {
            Text = string.Format(LanguageManager.GetString("AppsTitle"), groupName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = UiTheme.Font;

            int inner = FormWidth - Pad * 2;
            int y = Pad;

            // 説明
            string desc = LanguageManager.GetString("AppsDescription");
            var flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;
            int descHeight = TextRenderer.MeasureText(desc, UiTheme.Font, new Size(inner, 0), flags).Height + 4;
            Controls.Add(new Label
            {
                Text = desc,
                ForeColor = UiTheme.TextMuted,
                UseMnemonic = false,
                Bounds = new Rectangle(Pad, y, inner, descHeight)
            });
            y += descHeight + 10;

            // 登録済みアプリ一覧（1pxの枠付き）
            Panel listCard = new() { Bounds = new Rectangle(Pad, y, inner, 150), Padding = new Padding(1), BackColor = UiTheme.Border };
            appList.Dock = DockStyle.Fill;
            appList.BorderStyle = BorderStyle.None;
            appList.Font = UiTheme.LargeFont;
            appList.IntegralHeight = false;
            appList.SelectionMode = SelectionMode.MultiExtended;
            foreach (var a in apps) appList.Items.Add(a);
            Panel listInner = new() { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(8, 6, 0, 0) };
            listInner.Controls.Add(appList);
            listCard.Controls.Add(listInner);
            Controls.Add(listCard);
            y += listCard.Height + 8;

            ModernButton removeBtn = new() { Text = LanguageManager.GetString("AppsRemove"), Variant = ButtonVariant.Danger };
            removeBtn.Location = new Point(FormWidth - Pad - removeBtn.Width, y);
            Controls.Add(removeBtn);
            y += removeBtn.Height + 16;

            // 追加欄：アプリ名（例: notepad, EXCEL.EXE）を直接入力するか、exe ファイルを参照して追加
            Controls.Add(new Label
            {
                Text = LanguageManager.GetString("AppsAddLabel"),
                ForeColor = UiTheme.TextStrong,
                AutoSize = true,
                Location = new Point(Pad, y)
            });
            y += 22;

            ModernButton addBtn = new() { Text = LanguageManager.GetString("AppsAdd"), Variant = ButtonVariant.Primary };
            ModernButton browseBtn = new() { Text = LanguageManager.GetString("AppsBrowse") };
            addBtn.Location = new Point(FormWidth - Pad - addBtn.Width, y);
            browseBtn.Location = new Point(addBtn.Left - 6 - browseBtn.Width, y);
            appInput.Bounds = new Rectangle(Pad, y, browseBtn.Left - 8 - Pad, 32);
            appInput.TextBox.Font = UiTheme.LargeFont;
            Controls.Add(appInput);
            Controls.Add(browseBtn);
            Controls.Add(addBtn);
            y += 32 + Pad;

            // フッター
            const int footerHeight = 60;
            Panel footer = new() { Bounds = new Rectangle(0, y, FormWidth, footerHeight), BackColor = UiTheme.Surface };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(UiTheme.Divider);
                e.Graphics.DrawLine(pen, 0, 0, FormWidth, 0);
            };
            ModernButton okBtn = new() { Text = LanguageManager.GetString("DialogOK"), Variant = ButtonVariant.Primary, DialogResult = DialogResult.OK };
            ModernButton cancelBtn = new() { Text = LanguageManager.GetString("DialogCancel"), DialogResult = DialogResult.Cancel };
            okBtn.Width = Math.Max(okBtn.Width, 88);
            cancelBtn.Width = Math.Max(cancelBtn.Width, 88);
            cancelBtn.Location = new Point(FormWidth - 16 - cancelBtn.Width, (footerHeight - cancelBtn.Height) / 2);
            okBtn.Location = new Point(cancelBtn.Left - 8 - okBtn.Width, cancelBtn.Top);
            footer.Controls.Add(okBtn);
            footer.Controls.Add(cancelBtn);
            Controls.Add(footer);

            ClientSize = new Size(FormWidth, y + footerHeight);
            CancelButton = cancelBtn;

            void updateButtons()
            {
                removeBtn.Enabled = appList.SelectedIndices.Count > 0;
                addBtn.Enabled = !string.IsNullOrWhiteSpace(appInput.TextBox.Text);
            }
            appList.SelectedIndexChanged += (s, e) => updateButtons();
            appInput.TextBox.TextChanged += (s, e) => updateButtons();
            updateButtons();

            addBtn.Click += (s, e) => { AddApp(); updateButtons(); };
            removeBtn.Click += (s, e) => { RemoveSelected(); updateButtons(); };
            browseBtn.Click += (s, e) => { BrowseExe(); updateButtons(); };

            // 入力欄で Enter → 追加（ダイアログは閉じない）
            appInput.TextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    AddApp();
                    updateButtons();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            appList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete) { RemoveSelected(); updateButtons(); }
            };

            okBtn.Click += (s, e) =>
            {
                // 入力途中のものがあれば登録してから閉じる
                if (!string.IsNullOrWhiteSpace(appInput.TextBox.Text)) AddApp();
                ResultApps = appList.Items.Cast<string>().ToList();
            };

            Shown += (s, e) => appInput.TextBox.Focus();
        }

        /// <summary>
        /// exe ファイルを選んで追加する
        /// </summary>
        private void BrowseExe()
        {
            using var dlg = new OpenFileDialog
            {
                Filter = LanguageManager.GetString("AppsBrowseFilter") + " (*.exe)|*.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            appInput.TextBox.Text = dlg.FileName;
            AddApp();
        }

        private void AddApp()
        {
            string name = GroupAppRepository.Normalize(appInput.TextBox.Text);
            if (name.Length == 0) return;

            bool exists = appList.Items.Cast<string>()
                .Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
            if (!exists) appList.Items.Add(name);

            appInput.TextBox.Text = "";
        }

        private void RemoveSelected()
        {
            var indices = appList.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
            foreach (var i in indices) appList.Items.RemoveAt(i);
        }
    }
}
