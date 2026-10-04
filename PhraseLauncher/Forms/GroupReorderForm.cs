using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PhraseLauncher
{
    class GroupReorderForm : Form
    {
        DataGridView dgv = new();
        Button upBtn = new() { Text = "▲", Width = 60 };
        Button downBtn = new() { Text = "▼", Width = 60 };
        Button saveBtn = new() { Text = LanguageManager.GetString("GroupReorderSave"), Width = 100 };

        public List<string> ResultOrder { get; private set; }

        public GroupReorderForm(List<string> groups)
        {
            Text = LanguageManager.GetString("GroupReorderTitle");
            Width = 400;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            dgv.SetBounds(10, 10, 360, 380);
            dgv.ColumnCount = 1;
            dgv.Columns[0].Name = LanguageManager.GetString("GroupReorderGroup");
            dgv.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = true;
            dgv.AllowUserToAddRows = false;

            foreach (var g in groups)
                dgv.Rows.Add(g);

            upBtn.SetBounds(10, 400, 60, 30);
            downBtn.SetBounds(80, 400, 60, 30);
            saveBtn.SetBounds(270, 400, 100, 30);

            upBtn.Click += (s, e) => Move(-1);
            downBtn.Click += (s, e) => Move(1);

            saveBtn.Click += (s, e) =>
            {
                ResultOrder = dgv.Rows
                    .Cast<DataGridViewRow>()
                    .Select(r => r.Cells[0].Value?.ToString())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                dgv, upBtn, downBtn, saveBtn
            });
        }

        private void Move(int dir)
        {
            if (dgv.SelectedRows.Count == 0) return;

            var idx = dgv.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => r.Index)
                .OrderBy(i => i)
                .ToList();

            if (dir < 0 && idx.First() == 0) return;
            if (dir > 0 && idx.Last() == dgv.Rows.Count - 1) return;

            var work = dir < 0
                       ? idx
                       : idx.OrderByDescending(i => i).ToList();

            foreach (var i in work)
            {
                var row = dgv.Rows[i];
                dgv.Rows.RemoveAt(i);
                dgv.Rows.Insert(i + dir, row);
            }

            dgv.ClearSelection();
            foreach (var i in idx)
                dgv.Rows[i + dir].Selected = true;
        }
    }
}
