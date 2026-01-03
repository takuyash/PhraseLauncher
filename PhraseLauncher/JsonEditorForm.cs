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

            Directory.CreateDirectory(TemplateRepository.JsonFolder);

            fileCombo.SetBounds(10, 10, 400, 25);
            Button newBtn = new() { Text = "新規作成", Left = 420, Top = 10 };
            dgv.SetBounds(10, 40, 560, 380);
            Button saveBtn = new() { Text = "保存", Left = 480, Top = 430 };
            Button delBtn = new() { Text = "削除", Left = 390, Top = 430 };
            Button upBtn = new() { Text = "↑", Left = 220, Top = 430 };
            Button downBtn = new() { Text = "↓", Left = 310, Top = 430 };

            dgv.ColumnCount = 2;
            dgv.Columns[0].Name = "定型文";
            dgv.Columns[1].Name = "メモ";
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

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

            upBtn.Click += (s, e) => MoveRow(-1);
            downBtn.Click += (s, e) => MoveRow(1);

            Controls.AddRange(new Control[]
            { fileCombo, newBtn, dgv, saveBtn, delBtn, upBtn, downBtn });
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
