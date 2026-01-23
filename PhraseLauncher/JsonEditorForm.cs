using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace PhraseLauncher
{
    class JsonEditorForm : Form
    {
        ComboBox fileCombo = new();
        DataGridView dgv = new();

        public JsonEditorForm()
        {
            Width = 700; // 少し広げました
            Height = 500;
            Text = "定型文編集・登録";
            StartPosition = FormStartPosition.CenterScreen;
            fileCombo.DropDownStyle = ComboBoxStyle.DropDownList;

            Directory.CreateDirectory(TemplateRepository.JsonFolder);

            fileCombo.SetBounds(10, 10, 300, 25);
            Button newBtn = new() { Text = "新規作成", Left = 320, Top = 10, Width = 80 };
            Button renameBtn = new() { Text = "名変更", Left = 410, Top = 10, Width = 80 };

            dgv.SetBounds(10, 40, 660, 380);
            dgv.AllowUserToAddRows = true;
            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "定型文";
            dgv.Columns[1].Name = "メモ";
            dgv.Columns[0].Width = 300;
            dgv.Columns[1].Width = 300;

            // --- 改行と表示のための重要設定 ---
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True; // 文字の折り返しを有効化
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; // 行の高さを自動調整

            // 1. 入力用テキストボックスを「複数行入力」モードにする
            dgv.EditingControlShowing += (s, e) =>
            {
                if (e.Control is TextBox tb)
                {
                    tb.Multiline = true;
                }
            };

            // 2. キー入力制御 (Shift + Enter で改行を挿入)
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && e.Shift)
                {
                    if (dgv.EditingControl is TextBox tb)
                    {
                        // 選択箇所に改行を挿入
                        int pos = tb.SelectionStart;
                        tb.SelectedText = Environment.NewLine;

                        // DataGridView側でEnterを処理させない（下のセルに移動させない）
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
            };

            // 保存ボタンなどの配置
            Button saveBtn = new() { Text = "保存", Left = 580, Top = 430 };
            Button delBtn = new() { Text = "削除", Left = 490, Top = 430 };
            Button upBtn = new() { Text = "↑", Left = 220, Top = 430, Width = 40 };
            Button downBtn = new() { Text = "↓", Left = 270, Top = 430, Width = 40 };

            // ファイル読み込み処理
            RefreshFileList();
            fileCombo.SelectedIndexChanged += (s, e) => LoadSelectedFile();

            saveBtn.Click += (s, e) => SaveData();
            delBtn.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow) return;
                if (MessageBox.Show("削除しますか？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    dgv.Rows.Remove(dgv.CurrentRow);
            };

            newBtn.Click += (s, e) => CreateNewFile();
            renameBtn.Click += (s, e) => RenameFile();
            upBtn.Click += (s, e) => MoveRow(-1);
            downBtn.Click += (s, e) => MoveRow(1);

            if (fileCombo.Items.Count > 0) fileCombo.SelectedIndex = 0;

            Controls.AddRange(new Control[] { fileCombo, newBtn, dgv, saveBtn, delBtn, upBtn, downBtn, renameBtn });
        }

        private void RefreshFileList()
        {
            fileCombo.Items.Clear();
            foreach (var f in Directory.GetFiles(TemplateRepository.JsonFolder, "*.json"))
                fileCombo.Items.Add(Path.GetFileNameWithoutExtension(f));
        }

        private void LoadSelectedFile()
        {
            if (string.IsNullOrEmpty(fileCombo.Text)) return;
            dgv.Rows.Clear();
            var path = Path.Combine(TemplateRepository.JsonFolder, fileCombo.Text + ".json");
            var list = TemplateRepository.Load(path);
            foreach (var t in list)
            {
                // JSONから読み込む際に \n をシステム改行に変換
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
                    // 保存時にシステム改行を \n に戻す
                    text = (r.Cells[0].Value ?? "").ToString().Replace(Environment.NewLine, "\n"),
                    note = (r.Cells[1].Value ?? "").ToString().Replace(Environment.NewLine, "\n")
                });
            }
            var path = Path.Combine(TemplateRepository.JsonFolder, fileCombo.Text + ".json");
            TemplateRepository.Save(path, list);
            MessageBox.Show("保存しました。");
        }

        private void CreateNewFile()
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox("新規グループ名", "作成", "new");
            if (string.IsNullOrWhiteSpace(newName)) return;
            string path = Path.Combine(TemplateRepository.JsonFolder, newName + ".json");
            if (File.Exists(path)) { MessageBox.Show("既に存在します。"); return; }
            File.WriteAllText(path, "[]");
            RefreshFileList();
            fileCombo.SelectedItem = newName;
        }

        private void RenameFile()
        {
            if (string.IsNullOrWhiteSpace(fileCombo.Text)) return;
            string oldName = fileCombo.Text;
            string newName = Microsoft.VisualBasic.Interaction.InputBox("新しい名前", "変更", oldName);
            if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;
            string oldPath = Path.Combine(TemplateRepository.JsonFolder, oldName + ".json");
            string newPath = Path.Combine(TemplateRepository.JsonFolder, newName + ".json");
            if (File.Exists(newPath)) { MessageBox.Show("既に存在します。"); return; }
            File.Move(oldPath, newPath);
            RefreshFileList();
            fileCombo.SelectedItem = newName;
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