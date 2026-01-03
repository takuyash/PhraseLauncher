using System.Runtime.InteropServices;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

class Program
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(Keys vKey);

    private const int HOTKEY_ID = 9001;
    private const uint MOD_CONTROL_SHIFT = 0x0002 | 0x0004; // Ctrl+Shift
    private const uint VK_SPACE = 0x20;

    public static HiddenForm hiddenForm;
    public static Form jsonForm;
    private static List<List<TemplateItem>> allTemplates = new List<List<TemplateItem>>();

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        NotifyIcon tray = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "PhraseLauncher"
        };

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("一覧表示", null, (s, e) => ShowJsonList()));
        menu.Items.Add(new ToolStripMenuItem("編集/登録", null, (s, e) => ShowJsonEditor()));
        menu.Items.Add(new ToolStripMenuItem("終了", null, (s, e) => Application.Exit()));
        tray.ContextMenuStrip = menu;

        hiddenForm = new HiddenForm();
        hiddenForm.ShowInTaskbar = false;
        hiddenForm.Opacity = 0;
        hiddenForm.Show();

        Application.Run();
    }

    class TemplateItem
    {
        public string text { get; set; }
        public string note { get; set; }
    }

    // -------------------
    // JSON一覧表示
    // -------------------
    static void ShowJsonList()
    {
        if (jsonForm != null && !jsonForm.IsDisposed)
        {
            jsonForm.BringToFront();
            return;
        }

        string jsonFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json");
        if (!Directory.Exists(jsonFolder)) Directory.CreateDirectory(jsonFolder);

        string[] jsonFiles = Directory.GetFiles(jsonFolder, "*.json");
        if (jsonFiles.Length == 0)
        {
            MessageBox.Show("json フォルダに JSON ファイルが見つかりません。");
            return;
        }

        allTemplates.Clear();

        jsonForm = new Form
        {
            Width = 500,
            Height = 400,
            StartPosition = FormStartPosition.CenterScreen,
            TopMost = true,
            Text = "定型文一覧"
        };

        TabControl tabControl = new TabControl { Dock = DockStyle.Fill };

        foreach (var file in jsonFiles)
        {
            List<TemplateItem> templates;
            try
            {
                string json = File.ReadAllText(file);
                templates = JsonSerializer.Deserialize<List<TemplateItem>>(json.Replace("\r\n", "\n"));
            }
            catch
            {
                templates = new List<TemplateItem> { new TemplateItem { text = "読み込みエラー", note = "" } };
            }

            allTemplates.Add(templates);

            ListBox listBox = new ListBox { Dock = DockStyle.Fill };
            ToolTip toolTip = new ToolTip();

            List<string> displayList = new List<string>();
            for (int i = 0; i < templates.Count; i++)
                displayList.Add(GetShortcutKeyLabel(i) + ": " + templates[i].text.Replace("\n", " ")); // 表示はスペース
            listBox.Items.AddRange(displayList.ToArray());

            listBox.MouseMove += (s, e) =>
            {
                int index = listBox.IndexFromPoint(e.Location);
                if (index >= 0 && index < templates.Count)
                    toolTip.SetToolTip(listBox, templates[index].note);
            };

            // ダブルクリックで貼り付け対応
            listBox.DoubleClick += (s, e) =>
            {
                int idx = listBox.SelectedIndex;
                if (idx >= 0 && idx < templates.Count)
                    HiddenForm.TriggerTemplateStatic(templates[idx].text);
            };

            TabPage tabPage = new TabPage(Path.GetFileName(file));
            tabPage.Controls.Add(listBox);
            tabControl.TabPages.Add(tabPage);
        }

        jsonForm.Controls.Add(tabControl);
        jsonForm.Show();
    }

    static string GetShortcutKeyLabel(int index)
    {
        if (index < 9) return (index + 1).ToString();
        index -= 9;
        string label = "";
        do
        {
            label = (char)('a' + (index % 26)) + label;
            index = index / 26 - 1;
        } while (index >= 0);
        return label;
    }

    static void PasteText(string text)
    {
        Clipboard.SetText(text);
        var timer = new System.Windows.Forms.Timer { Interval = 100 };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            timer.Dispose();
            SendKeys.SendWait("^v");
        };
        timer.Start();
    }

    // -------------------
    // JSON編集・登録フォーム
    // -------------------
    static void ShowJsonEditor()
    {
        string jsonFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json");
        if (!Directory.Exists(jsonFolder)) Directory.CreateDirectory(jsonFolder);

        Form editorForm = new Form
        {
            Width = 600,
            Height = 500,
            StartPosition = FormStartPosition.CenterScreen,
            Text = "定型文編集・登録"
        };

        ComboBox fileCombo = new ComboBox { Left = 10, Top = 10, Width = 400 };
        Button newFileBtn = new Button { Text = "新規作成", Left = 420, Top = 10, Width = 80 };
        DataGridView dgv = new DataGridView { Left = 10, Top = 40, Width = 560, Height = 380 };
        Button saveBtn = new Button { Text = "保存", Left = 480, Top = 430, Width = 80 };
        Button deleteBtn = new Button { Text = "削除", Left = 390, Top = 430, Width = 80 };
        Button upBtn = new Button { Text = "↑", Left = 300, Top = 430, Width = 40 };
        Button downBtn = new Button { Text = "↓", Left = 340, Top = 430, Width = 40 };


        dgv.AllowUserToAddRows = true;
        dgv.AllowUserToDeleteRows = true;
        dgv.ColumnCount = 2;
        dgv.Columns[0].Name = "定型文";
        dgv.Columns[1].Name = "メモ";
        dgv.Columns[0].Width = 250;
        dgv.Columns[1].Width = 280;
        dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        dgv.MultiSelect = false;
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;

        dgv.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && dgv.CurrentCell != null && !dgv.CurrentCell.ReadOnly)
            {
                e.Handled = true;
                dgv.BeginEdit(true);
                var tb = dgv.EditingControl as TextBox;
                if (tb != null)
                {
                    int pos = tb.SelectionStart;
                    tb.Text = tb.Text.Insert(pos, Environment.NewLine);
                    tb.SelectionStart = pos + Environment.NewLine.Length;
                }
            }
        };

        string[] jsonFiles = Directory.GetFiles(jsonFolder, "*.json");
        foreach (var f in jsonFiles) fileCombo.Items.Add(Path.GetFileName(f));

        editorForm.Controls.Add(fileCombo);
        editorForm.Controls.Add(newFileBtn);
        editorForm.Controls.Add(dgv);
        editorForm.Controls.Add(saveBtn);
        editorForm.Controls.Add(deleteBtn);
        editorForm.Controls.Add(upBtn);
        editorForm.Controls.Add(downBtn);


        fileCombo.SelectedIndexChanged += (s, e) =>
        {
            string filePath = Path.Combine(jsonFolder, fileCombo.SelectedItem.ToString());
            dgv.Rows.Clear();
            try
            {
                var templates = JsonSerializer.Deserialize<List<TemplateItem>>(File.ReadAllText(filePath).Replace("\r\n", "\n"));
                foreach (var t in templates)
                    dgv.Rows.Add(t.text.Replace("\n", Environment.NewLine), t.note.Replace("\n", Environment.NewLine));
            }
            catch { }
        };

        newFileBtn.Click += (s, e) =>
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox("新しい JSON ファイル名を入力（例: test.json）", "新規作成", "new.json");
            if (!string.IsNullOrEmpty(newName))
            {
                dgv.Rows.Clear();
                if (!newName.EndsWith(".json")) newName += ".json";
                fileCombo.Items.Add(newName);
                fileCombo.SelectedItem = newName;
            }
        };

        saveBtn.Click += (s, e) =>
        {
            if (fileCombo.SelectedItem == null)
            {
                MessageBox.Show("ファイルを選択してください。");
                return;
            }

            string filePath = Path.Combine(jsonFolder, fileCombo.SelectedItem.ToString());
            List<TemplateItem> templates = new List<TemplateItem>();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                string text = row.Cells[0].Value?.ToString() ?? "";
                string note = row.Cells[1].Value?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(text))
                    templates.Add(new TemplateItem { text = text.Replace(Environment.NewLine, "\n"), note = note.Replace(Environment.NewLine, "\n") });
            }

            File.WriteAllText(filePath, JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true }));
            MessageBox.Show("保存しました。");
        };

        deleteBtn.Click += (s, e) =>
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow)
            {
                MessageBox.Show("削除する行を選択してください。");
                return;
            }

            var result = MessageBox.Show(
                "選択中の定型文を削除しますか？",
                "確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                dgv.Rows.Remove(dgv.CurrentRow);
            }
        };

        upBtn.Click += (s, e) =>
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow) return;

            int index = dgv.CurrentRow.Index;
            if (index <= 0) return;

            DataGridViewRow row = dgv.Rows[index];
            dgv.Rows.RemoveAt(index);
            dgv.Rows.Insert(index - 1, row);

            dgv.CurrentCell = row.Cells[0];
        };

        downBtn.Click += (s, e) =>
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.IsNewRow) return;

            int index = dgv.CurrentRow.Index;
            if (index >= dgv.Rows.Count - 2) return; // NewRow分を考慮

            DataGridViewRow row = dgv.Rows[index];
            dgv.Rows.RemoveAt(index);
            dgv.Rows.Insert(index + 1, row);

            dgv.CurrentCell = row.Cells[0];
        };




        editorForm.Show();
    }

    // -------------------
    // HiddenForm
    // -------------------
    public class HiddenForm : Form
    {
        System.Windows.Forms.Timer keyTimer = new System.Windows.Forms.Timer();
        HashSet<Keys> pressedKeys = new HashSet<Keys>();

        public HiddenForm()
        {
            this.Load += (s, e) => RegisterShortcutKeys();
            this.FormClosing += (s, e) => UnregisterShortcutKeys();

            keyTimer.Interval = 50;
            keyTimer.Tick += KeyTimer_Tick;
            keyTimer.Start();
        }

        private void KeyTimer_Tick(object sender, EventArgs e)
        {
            if (Program.jsonForm == null || Program.jsonForm.IsDisposed || !Program.jsonForm.Visible) return;
            if (Program.jsonForm.Controls.Count == 0) return;

            var tabControl = Program.jsonForm.Controls[0] as TabControl;
            if (tabControl == null) return;

            var selectedTab = tabControl.SelectedTab;
            if (selectedTab == null || selectedTab.Controls.Count == 0) return;

            var listBox = selectedTab.Controls[0] as ListBox;
            if (listBox == null) return;

            int tabIndex = tabControl.SelectedIndex;
            var templates = allTemplates[tabIndex];

            for (int i = 0; i < 9; i++)
            {
                Keys k = (Keys)(0x31 + i); // VK_1 ～ VK_9（IME非依存）
                if ((GetAsyncKeyState(k) & 0x8000) != 0)
                {
                    if (!pressedKeys.Contains(k))
                    {
                        pressedKeys.Add(k);
                        TriggerTemplate(templates, i);
                    }
                }
                else pressedKeys.Remove(k);
            }

            for (int i = 0; i < 26; i++)
            {
                Keys k = Keys.A + i;
                if ((GetAsyncKeyState(k) & 0x8000) != 0)
                {
                    if (!pressedKeys.Contains(k))
                    {
                        pressedKeys.Add(k);
                        TriggerTemplate(templates, 9 + i);
                    }
                }
                else pressedKeys.Remove(k);
            }
        }

        void TriggerTemplate(List<TemplateItem> templates, int index)
        {
            if (index >= templates.Count) return;
            TriggerTemplateStatic(templates[index].text);
        }

        // 数字キー入力と別に貼り付け処理
        public static void TriggerTemplateStatic(string text)
        {
            Form activeForm = Form.ActiveForm;
            if (activeForm != null)
                activeForm.ActiveControl = null; // 入力抑制

            PasteText(text);

            if (Program.jsonForm != null && !Program.jsonForm.IsDisposed)
            {
                Program.jsonForm.Close();
                Program.jsonForm = null;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                Program.ShowJsonList();
            }
            base.WndProc(ref m);
        }

        public void RegisterShortcutKeys()
        {
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL_SHIFT, VK_SPACE);
        }

        public void UnregisterShortcutKeys()
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }
    }
}