using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace PhraseLauncher
{


    class JsonEditorForm : Form
    {
        ComboBox fileCombo = new();

        DataGridView dgv = new();

        public JsonEditorForm()
        {
            Width = 600;
            Height = 500;
            Text = "定型文編集・登録";
            StartPosition = FormStartPosition.CenterScreen;
            fileCombo.DropDownStyle = ComboBoxStyle.DropDownList; // 手入力禁止

            Directory.CreateDirectory(TemplateRepository.JsonFolder);

            fileCombo.SetBounds(10, 10, 400, 25);
            Button newBtn = new() { Text = "新規作成", Left = 410, Top = 10 };

            // グループ名変更ボタン
            Button renameBtn = new() { Text = "グループ名変更", Left = 490, Top = 10 };

            dgv.SetBounds(10, 40, 560, 380);
            Button saveBtn = new() { Text = "保存", Left = 480, Top = 430 };
            Button delBtn = new() { Text = "削除", Left = 390, Top = 430 };
            Button upBtn = new() { Text = "↑", Left = 220, Top = 430 };
            Button downBtn = new() { Text = "↓", Left = 310, Top = 430 };

            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "定型文";
            dgv.Columns[1].Name = "メモ";
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Enterキーで改行できるようにする
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && dgv.CurrentCell != null && !dgv.CurrentCell.ReadOnly)
                {
                    e.Handled = true;
                    dgv.BeginEdit(true);

                    if (dgv.EditingControl is TextBox tb)
                    {
                        int pos = tb.SelectionStart;
                        tb.Text = tb.Text.Insert(pos, Environment.NewLine);
                        tb.SelectionStart = pos + Environment.NewLine.Length;
                    }
                }
            };

            foreach (var f in Directory.GetFiles(TemplateRepository.JsonFolder, "*.json"))
                fileCombo.Items.Add(Path.GetFileNameWithoutExtension(f));

            fileCombo.SelectedIndexChanged += (s, e) =>
            {
                dgv.Rows.Clear();
                var list = TemplateRepository.Load(
                    Path.Combine(TemplateRepository.JsonFolder, fileCombo.Text + ".json"));
                foreach (var t in list)
                    dgv.Rows.Add(t.text.Replace("\n", Environment.NewLine),
                                 t.note.Replace("\n", Environment.NewLine));
            };

            saveBtn.Click += (s, e) =>
            {
                List<TemplateItem> list = new();
                foreach (DataGridViewRow r in dgv.Rows)
                {
                    if (r.IsNewRow) continue;
                    list.Add(new TemplateItem
                    {
                        text = (r.Cells[0].Value ?? "").ToString().Replace(Environment.NewLine, "\n"),
                        note = (r.Cells[1].Value ?? "").ToString().Replace(Environment.NewLine, "\n")
                    });
                }
                TemplateRepository.Save(
                    Path.Combine(TemplateRepository.JsonFolder, fileCombo.Text + ".json"), list);
                MessageBox.Show("保存しました。");
            };

            delBtn.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow) return;
                if (MessageBox.Show("削除しますか？", "確認",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    dgv.Rows.Remove(dgv.CurrentRow);
            };

            // 新規作成ボタンのクリックイベント
            newBtn.Click += (s, e) =>
            {
                // ユーザーには拡張子なしの名前だけ入力してもらう
                string newName = Microsoft.VisualBasic.Interaction.InputBox(
                    "新しいグループ名を入力（例: test）", "新規作成", "new");

                if (string.IsNullOrWhiteSpace(newName)) return;

                // 拡張子を自動で付与
                string fileName = newName + ".json";
                string path = Path.Combine(TemplateRepository.JsonFolder, fileName);

                // ファイルが存在する場合は警告
                if (File.Exists(path))
                {
                    MessageBox.Show("同名のグループが既に存在します。");
                    return;
                }

                // 空の JSON ファイルを作成
                File.WriteAllText(path, "[]");

                // ComboBox に追加して選択
                if (!fileCombo.Items.Contains(newName))
                    fileCombo.Items.Add(newName);
                fileCombo.SelectedItem = newName;

                // DataGridView を空にして編集可能に
                dgv.Rows.Clear();
                dgv.Rows.Add("", ""); // 最初の空行を追加してすぐ編集可能に
                dgv.CurrentCell = dgv.Rows[0].Cells[0];
            };

            // ファイル名変更ボタン
            renameBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(fileCombo.Text)) return;

                string oldName = fileCombo.Text;
                string newName = Microsoft.VisualBasic.Interaction.InputBox(
                    "新しいグループ名を入力", "名前変更", oldName);

                if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;

                string oldPath = Path.Combine(TemplateRepository.JsonFolder, oldName + ".json");
                string newPath = Path.Combine(TemplateRepository.JsonFolder, newName + ".json");

                if (File.Exists(newPath))
                {
                    MessageBox.Show("同名のグループが既に存在します。");
                    return;
                }

                try
                {
                    File.Move(oldPath, newPath);
                    int idx = fileCombo.SelectedIndex;
                    fileCombo.Items[idx] = newName;
                    fileCombo.SelectedItem = newName;
                    MessageBox.Show("名前を変更しました。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("名前変更に失敗しました。\n" + ex.Message);
                }
            };

            upBtn.Click += (s, e) => MoveRow(-1);
            downBtn.Click += (s, e) => MoveRow(1);

            // 最初のグループを選択して内容を表示
            if (fileCombo.Items.Count > 0)
                fileCombo.SelectedIndex = 0;

            Controls.AddRange(new Control[]
            { fileCombo, newBtn, dgv, saveBtn, delBtn, upBtn, downBtn,renameBtn });
        }

        void MoveRow(int dir)
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
