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
            Width = 700;
            Height = 500;
            Text = LanguageManager.GetString("EditorTitle");
            StartPosition = FormStartPosition.CenterScreen;
            this.Icon = Program.AppIcon;

            fileCombo.DropDownStyle = ComboBoxStyle.DropDownList;

            Directory.CreateDirectory(TemplateRepository.JsonFolder);

            fileCombo.SetBounds(10, 10, 300, 25);

            Button newBtn = new() { Text = LanguageManager.GetString("EditorNew"), Left = 320, Top = 10, Width = 80 };
            Button renameBtn = new() { Text = LanguageManager.GetString("EditorRename"), Left = 410, Top = 10, Width = 90 };
            Button deleteGroupBtn = new() { Text = LanguageManager.GetString("EditorDeleteGroup"), Left = 590, Top = 10, Width = 90 };

            Button groupUpBtn = new() { Text = "▲", Left = 500, Top = 10, Width = 40 };
            Button groupDownBtn = new() { Text = "▼", Left = 545, Top = 10, Width = 40 };

            dgv.SetBounds(10, 40, 660, 380);
            dgv.AllowUserToAddRows = true;
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = LanguageManager.GetString("EditorColPhrase");
            dgv.Columns[1].Name = LanguageManager.GetString("EditorColNote");
            dgv.Columns[0].Width = 300;
            dgv.Columns[1].Width = 300;

            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

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

            Button saveBtn = new() { Text = LanguageManager.GetString("EditorSave"), Left = 580, Top = 430 };
            Button delBtn = new() { Text = LanguageManager.GetString("EditorDelete"), Left = 490, Top = 430 };
            Button upBtn = new() { Text = "↑", Left = 220, Top = 430, Width = 40 };
            Button downBtn = new() { Text = "↓", Left = 270, Top = 430, Width = 40 };

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

            newBtn.Click += (s, e) => CreateNewFile();
            renameBtn.Click += (s, e) => RenameFile();
            deleteGroupBtn.Click += (s, e) => DeleteGroup();

            upBtn.Click += (s, e) => MoveRow(-1);
            downBtn.Click += (s, e) => MoveRow(1);

            groupUpBtn.Click += (s, e) => MoveGroup(-1);
            groupDownBtn.Click += (s, e) => MoveGroup(1);

            if (fileCombo.Items.Count > 0)
                fileCombo.SelectedIndex = 0;

            Controls.AddRange(new Control[]
            {
                fileCombo, newBtn, renameBtn, deleteGroupBtn,
                groupUpBtn, groupDownBtn,
                dgv, saveBtn, delBtn, upBtn, downBtn
            });
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

        private void MoveRow(int dir)
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow) return;

            int i = dgv.CurrentRow.Index;
            int ni = i + dir;

            if (ni < 0 || ni >= dgv.Rows.Count - 1) return;

            var row = dgv.Rows[i];
            dgv.Rows.RemoveAt(i);
            dgv.Rows.Insert(ni, row);
            dgv.CurrentCell = row.Cells[0];
        }
    }
}