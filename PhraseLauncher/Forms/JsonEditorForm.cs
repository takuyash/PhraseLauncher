using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Text.Json;

namespace PhraseLauncher
{
    class JsonEditorForm : Form
    {
        ComboBox fileCombo = new();
        DataGridView dgv = new();

        string GroupOrderPath =>
            Path.Combine(TemplateRepository.JsonFolder, "groups.json");

        public JsonEditorForm()
        {
            Width = 780;
            Height = 540;
            MinimumSize = new Size(700, 400);
            Text = LanguageManager.GetString("EditorTitle");
            StartPosition = FormStartPosition.CenterScreen;
            this.Icon = Program.AppIcon;
            BackColor = UiTheme.Surface;
            Font = UiTheme.Font;

            fileCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            fileCombo.Font = UiTheme.LargeFont;
            fileCombo.Width = 240;
            fileCombo.Margin = new Padding(0, (32 - fileCombo.Height) / 2, 10, 0);

            Directory.CreateDirectory(TemplateRepository.JsonFolder);

            ModernButton newBtn = new() { Text = LanguageManager.GetString("EditorNew"), Margin = new Padding(0, 0, 6, 0) };
            ModernButton renameBtn = new() { Text = LanguageManager.GetString("EditorRename"), Margin = new Padding(0, 0, 6, 0) };
            ModernButton reorderBtn = new() { Text = LanguageManager.GetString("EditorGroupOrder"), Margin = new Padding(0, 0, 6, 0) };
            ModernButton deleteGroupBtn = new() { Text = LanguageManager.GetString("EditorDeleteGroup"), Variant = ButtonVariant.Danger, Margin = new Padding(0) };

            dgv.Dock = DockStyle.Fill;
            dgv.AllowUserToAddRows = true;
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = LanguageManager.GetString("EditorColPhrase");
            dgv.Columns[1].Name = LanguageManager.GetString("EditorColNote");
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.Columns[0].FillWeight = 55;
            dgv.Columns[1].FillWeight = 45;
            StyleGrid();

            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            dgv.MultiSelect = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;


            dgv.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb)
                    tb.Multiline = true;
            };

            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && e.Shift &&
                    dgv.EditingControl is TextBox tb)
                {
                    tb.SelectedText = Environment.NewLine;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            ModernButton saveBtn = new() { Text = LanguageManager.GetString("EditorSave"), Variant = ButtonVariant.Primary, Margin = new Padding(0) };
            ModernButton delBtn = new() { Text = LanguageManager.GetString("EditorDelete"), Variant = ButtonVariant.Danger, Margin = new Padding(0, 0, 8, 0) };
            saveBtn.Width = Math.Max(saveBtn.Width, 96);

            ModernButton upBtn = new() { Text = "↑", Margin = new Padding(0, 0, 6, 0) };
            ModernButton downBtn = new() { Text = "↓", Margin = new Padding(0) };

            upBtn.TabStop = false;
            downBtn.TabStop = false;

            RefreshFileList();
            fileCombo.SelectedIndexChanged += (s, e) => LoadSelectedFile();

            saveBtn.Click += (s, e) => SaveData();
            delBtn.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow) return;
                if (MessageBox.Show(LanguageManager.GetString("EditorMsgConfirm"), LanguageManager.GetString("EditorMsgConfirmTitle"),
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                    dgv.Rows.Remove(dgv.CurrentRow);
            };



            reorderBtn.Click += (s, e) =>
            {
                using var f = new GroupReorderForm(
                    fileCombo.Items.Cast<string>().ToList()
                );

                if (f.ShowDialog() == DialogResult.OK)
                {
                    fileCombo.Items.Clear();
                    foreach (var name in f.ResultOrder)
                        fileCombo.Items.Add(name);

                    if (fileCombo.Items.Count > 0)
                        fileCombo.SelectedIndex = 0;

                    SaveGroupOrder();
                }
            };

            newBtn.Click += (s, e) => CreateNewFile();
            renameBtn.Click += (s, e) => RenameFile();
            deleteGroupBtn.Click += (s, e) => DeleteGroup();

            upBtn.Click += (s, e) => MoveRow(-1);
            downBtn.Click += (s, e) => MoveRow(1);


            if (fileCombo.Items.Count > 0)
                fileCombo.SelectedIndex = 0;

            // 上部ツールバー
            FlowLayoutPanel toolbar = new()
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(14, 14, 14, 0),
                WrapContents = false
            };
            toolbar.Controls.AddRange(new Control[] { fileCombo, newBtn, renameBtn, reorderBtn, deleteGroupBtn });

            // 中央：表（1pxの枠付きカード）
            Panel card = new() { Dock = DockStyle.Fill, Padding = new Padding(1), BackColor = UiTheme.Border };
            card.Controls.Add(dgv);
            Panel body = new() { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 0) };
            body.Controls.Add(card);

            // 下部バー：左に並べ替え、右に削除・保存
            FlowLayoutPanel moveButtons = new()
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            moveButtons.Controls.AddRange(new Control[] { upBtn, downBtn });

            FlowLayoutPanel actionButtons = new()
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            actionButtons.Controls.AddRange(new Control[] { saveBtn, delBtn });

            Panel bottomBar = new() { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(14, 14, 14, 14) };
            bottomBar.Controls.AddRange(new Control[] { moveButtons, actionButtons });

            Controls.AddRange(new Control[] { body, bottomBar, toolbar });
        }

        private void StyleGrid()
        {
            dgv.BorderStyle = BorderStyle.None;
            dgv.BackgroundColor = Color.White;
            dgv.GridColor = UiTheme.Divider;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.RowHeadersVisible = false;

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 38;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = UiTheme.Surface,
                ForeColor = UiTheme.TextMuted,
                SelectionBackColor = UiTheme.Surface,
                SelectionForeColor = UiTheme.TextMuted,
                Font = UiTheme.BoldFont,
                Padding = new Padding(8, 0, 8, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            dgv.DefaultCellStyle.Font = UiTheme.LargeFont;
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = UiTheme.Text;
            dgv.DefaultCellStyle.SelectionBackColor = UiTheme.Selection;
            dgv.DefaultCellStyle.SelectionForeColor = UiTheme.TextStrong;
            dgv.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
            dgv.RowTemplate.MinimumHeight = 34;
        }

        /* ================= グループ管理 ================= */

        private void RefreshFileList()
        {
            fileCombo.Items.Clear();

            List<string> order = LoadGroupOrder();

            var files = Directory.GetFiles(TemplateRepository.JsonFolder, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(name => name != "groups")
                .ToList();

            foreach (var name in order.ToList())
            {
                if (files.Remove(name))
                    fileCombo.Items.Add(name);
            }

            foreach (var name in files)
                fileCombo.Items.Add(name);

            SaveGroupOrder();
        }

        private List<string> LoadGroupOrder()
        {
            if (!File.Exists(GroupOrderPath))
                return new List<string>();

            return JsonSerializer.Deserialize<List<string>>(
                File.ReadAllText(GroupOrderPath)
            ) ?? new List<string>();
        }

        private void SaveGroupOrder()
        {
            var list = fileCombo.Items.Cast<string>().ToList();
            File.WriteAllText(
                GroupOrderPath,
                JsonSerializer.Serialize(list, new JsonSerializerOptions
                {
                    WriteIndented = true
                })
            );
        }

        private void MoveGroup(int dir)
        {
            int i = fileCombo.SelectedIndex;
            if (i < 0) return;

            int ni = i + dir;
            if (ni < 0 || ni >= fileCombo.Items.Count) return;

            var item = fileCombo.Items[i];
            fileCombo.Items.RemoveAt(i);
            fileCombo.Items.Insert(ni, item);
            fileCombo.SelectedIndex = ni;

            SaveGroupOrder();
        }

        private void DeleteGroup()
        {
            if (string.IsNullOrEmpty(fileCombo.Text)) return;

            string name = fileCombo.Text;

            if (MessageBox.Show(
                string.Format(LanguageManager.GetString("EditorMsgConfirmGroup"), name),
                LanguageManager.GetString("EditorMsgConfirmTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            string path = Path.Combine(
                TemplateRepository.JsonFolder, name + ".json");

            if (File.Exists(path))
                File.Delete(path);

            int index = fileCombo.SelectedIndex;

            fileCombo.Items.Remove(name);
            SaveGroupOrder();

            dgv.Rows.Clear();

            if (fileCombo.Items.Count > 0)
                fileCombo.SelectedIndex = Math.Min(index, fileCombo.Items.Count - 1);
        }

        /* ================= 定型文 ================= */

        private void LoadSelectedFile()
        {
            if (string.IsNullOrEmpty(fileCombo.Text)) return;

            dgv.Rows.Clear();
            var path = Path.Combine(
                TemplateRepository.JsonFolder,
                fileCombo.Text + ".json"
            );

            var list = TemplateRepository.Load(path);

            foreach (var t in list)
            {
                dgv.Rows.Add(
                    t.text?.Replace("\n", Environment.NewLine),
                    t.note?.Replace("\n", Environment.NewLine)
                );
            }
        }

        private void SaveData()
        {
            if (string.IsNullOrEmpty(fileCombo.Text)) return;

            List<TemplateItem> list = new();

            foreach (DataGridViewRow r in dgv.Rows)
            {
                if (r.IsNewRow) continue;

                list.Add(new TemplateItem
                {
                    text = (r.Cells[0].Value ?? "").ToString().Replace("\r\n", "\n"),
                    note = (r.Cells[1].Value ?? "").ToString().Replace("\r\n", "\n")
                });
            }

            var path = Path.Combine(
                TemplateRepository.JsonFolder,
                fileCombo.Text + ".json"
            );

            TemplateRepository.Save(path, list);
            MessageBox.Show(LanguageManager.GetString("EditorMsgSaved"));
        }

        private void CreateNewFile()
        {
            string newName =
                Microsoft.VisualBasic.Interaction.InputBox(
                    LanguageManager.GetString("EditorInputNewMsg"),
                    LanguageManager.GetString("EditorInputNewTitle"), "new");

            if (string.IsNullOrWhiteSpace(newName)) return;

            string path = Path.Combine(
                TemplateRepository.JsonFolder, newName + ".json");

            if (File.Exists(path))
            {
                MessageBox.Show(LanguageManager.GetString("EditorMsgExists"));
                return;
            }

            File.WriteAllText(path, "[]");

            RefreshFileList();
            fileCombo.SelectedItem = newName;
            SaveGroupOrder();
        }

        private void RenameFile()
        {
            if (string.IsNullOrWhiteSpace(fileCombo.Text)) return;

            string oldName = fileCombo.Text;
            string newName =
                Microsoft.VisualBasic.Interaction.InputBox(
                    LanguageManager.GetString("EditorInputRenameMsg"),
                    LanguageManager.GetString("EditorInputRenameTitle"), oldName);

            if (string.IsNullOrWhiteSpace(newName) || newName == oldName)
                return;

            string oldPath = Path.Combine(
                TemplateRepository.JsonFolder, oldName + ".json");

            string newPath = Path.Combine(
                TemplateRepository.JsonFolder, newName + ".json");

            if (File.Exists(newPath))
            {
                MessageBox.Show(LanguageManager.GetString("EditorMsgExists"));
                return;
            }

            File.Move(oldPath, newPath);

            RefreshFileList();
            fileCombo.SelectedItem = newName;
            SaveGroupOrder();
        }

        /// <summary>
        /// 定型文の行を移動（複数選択にも対応）
        /// </summary>
        /// <param name="dir"></param>
        private void MoveRow(int dir)
        {
            if (dgv.SelectedRows.Count == 0) return;

            var selectedIndexes = dgv.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => r.Index)
                .OrderBy(i => i)
                .ToList();

            if (selectedIndexes.Count == 0) return;

            // 境界チェック
            if (dir < 0 && selectedIndexes.First() == 0) return;
            if (dir > 0 && selectedIndexes.Last() >= dgv.Rows.Count - 2) return;

            // 上：昇順 / 下：降順
            var workOrder = dir < 0
                ? selectedIndexes
                : selectedIndexes.OrderByDescending(i => i).ToList();

            foreach (var i in workOrder)
            {
                var row = dgv.Rows[i];
                dgv.Rows.RemoveAt(i);
                dgv.Rows.Insert(i + dir, row);
            }

            // 選択状態を復元
            dgv.ClearSelection();
            foreach (var i in selectedIndexes)
                dgv.Rows[i + dir].Selected = true;

            // フォーカスを戻す
            dgv.Focus();
        }


    }
}