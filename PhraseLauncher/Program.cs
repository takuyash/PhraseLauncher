using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

class Program
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(Keys vKey);

    private const int HOTKEY_ID = 9001;
    private const uint MOD_CONTROL_SHIFT = 0x0002 | 0x0004;
    private const uint VK_SPACE = 0x20;
    private const int WM_HOTKEY = 0x0312;


    public static HiddenForm hiddenForm;
    public static Form jsonForm;
    private static List<List<TemplateItem>> allTemplates = new();

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        NotifyIcon tray = new()
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "PhraseLauncher"
        };

        ContextMenuStrip menu = new();
        menu.Items.Add(new ToolStripMenuItem("一覧表示", null, (s, e) => ShowJsonList()));
        menu.Items.Add(new ToolStripMenuItem("編集/登録", null, (s, e) => ShowJsonEditor()));
        menu.Items.Add(new ToolStripMenuItem("終了", null, (s, e) => Application.Exit()));
        tray.ContextMenuStrip = menu;

        hiddenForm = new HiddenForm { ShowInTaskbar = false, Opacity = 0 };
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
        Directory.CreateDirectory(jsonFolder);

        var files = Directory.GetFiles(jsonFolder, "*.json");
        if (files.Length == 0)
        {
            MessageBox.Show("json がありません");
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

        TabControl tabControl = new() { Dock = DockStyle.Fill };

        foreach (var file in files)
        {
            var templates = JsonSerializer.Deserialize<List<TemplateItem>>(
                File.ReadAllText(file).Replace("\r\n", "\n"));

            allTemplates.Add(templates);

            ListBox listBox = new()
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false
            };

            for (int i = 0; i < templates.Count; i++)
            {
                listBox.Items.Add(
                    GetShortcutKeyLabel(i) + ": " + templates[i].text.Replace("\n", " "));
            }

            // ★ ListBox は ↑↓ 完全に標準動作
            // 先頭で ↑ のときだけタブへ戻す
            listBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Up && listBox.SelectedIndex == 0)
                {
                    tabControl.Focus();
                    e.Handled = true;
                }
            };

            TabPage tab = new(Path.GetFileNameWithoutExtension(file));
            tab.Controls.Add(listBox);
            tabControl.TabPages.Add(tab);
        }

        // ★ タブ操作は「タブにフォーカスがあるときだけ」
        tabControl.KeyDown += (s, e) =>
        {
            if (!tabControl.Focused) return;

            if (e.KeyCode == Keys.Down)
            {
                if (tabControl.SelectedTab.Controls[0] is ListBox lb && lb.Items.Count > 0)
                {
                    lb.Focus();
                    lb.SelectedIndex = 0;
                }
                e.Handled = true;
            }
        };

        jsonForm.Controls.Add(tabControl);
        jsonForm.Show();
        tabControl.Focus();
    }

    static string GetShortcutKeyLabel(int index)
    {
        if (index < 9) return (index + 1).ToString();
        index -= 9;
        string s = "";
        do
        {
            s = (char)('a' + index % 26) + s;
            index = index / 26 - 1;
        } while (index >= 0);
        return s;
    }

    static void PasteText(string text)
    {
        Clipboard.SetText(text);
        var t = new Timer { Interval = 100 };
        t.Tick += (s, e) => { t.Stop(); SendKeys.SendWait("^v"); };
        t.Start();
    }

    // -------------------
    // JSON編集・登録フォーム
    // -------------------
    static void ShowJsonEditor()
    {
        string jsonFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json");
        Directory.CreateDirectory(jsonFolder);

        Form editorForm = new()
        {
            Width = 600,
            Height = 500,
            StartPosition = FormStartPosition.CenterScreen,
            Text = "定型文編集・登録"
        };

        ComboBox fileCombo = new() { Left = 10, Top = 10, Width = 400 };
        Button newFileBtn = new() { Text = "新規作成", Left = 420, Top = 10, Width = 80 };
        DataGridView dgv = new() { Left = 10, Top = 40, Width = 560, Height = 380 };
        Button saveBtn = new() { Text = "保存", Left = 480, Top = 430, Width = 80 };
        Button deleteBtn = new() { Text = "削除", Left = 390, Top = 430, Width = 80 };

        dgv.ColumnCount = 2;
        dgv.Columns[0].Name = "定型文";
        dgv.Columns[1].Name = "メモ";
        dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

        // ★ 拡張子なしで表示
        foreach (var f in Directory.GetFiles(jsonFolder, "*.json"))
            fileCombo.Items.Add(Path.GetFileNameWithoutExtension(f));

        fileCombo.SelectedIndexChanged += (s, e) =>
        {
            dgv.Rows.Clear();
            string filePath = Path.Combine(jsonFolder, fileCombo.Text + ".json");
            if (!File.Exists(filePath)) return;

            var templates = JsonSerializer.Deserialize<List<TemplateItem>>(
                File.ReadAllText(filePath).Replace("\r\n", "\n"));

            foreach (var t in templates)
                dgv.Rows.Add(
                    t.text.Replace("\n", Environment.NewLine),
                    t.note.Replace("\n", Environment.NewLine));
        };

        newFileBtn.Click += (s, e) =>
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "新しいグループ名を入力", "新規作成", "new");

            if (string.IsNullOrWhiteSpace(name)) return;

            name = Path.GetFileNameWithoutExtension(name);
            if (!fileCombo.Items.Contains(name))
                fileCombo.Items.Add(name);

            fileCombo.SelectedItem = name;
            dgv.Rows.Clear();
        };

        saveBtn.Click += (s, e) =>
        {
            if (fileCombo.SelectedItem == null) return;

            string filePath = Path.Combine(jsonFolder, fileCombo.Text + ".json");
            List<TemplateItem> list = new();

            foreach (DataGridViewRow r in dgv.Rows)
            {
                if (r.IsNewRow) continue;
                list.Add(new TemplateItem
                {
                    text = (r.Cells[0].Value ?? "").ToString()
                        .Replace(Environment.NewLine, "\n"),
                    note = (r.Cells[1].Value ?? "").ToString()
                        .Replace(Environment.NewLine, "\n")
                });
            }

            File.WriteAllText(filePath,
                JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
            MessageBox.Show("保存しました。");
        };

        deleteBtn.Click += (s, e) =>
        {
            if (dgv.CurrentRow != null && !dgv.CurrentRow.IsNewRow)
                dgv.Rows.Remove(dgv.CurrentRow);
        };

        editorForm.Controls.AddRange(new Control[]
        {
            fileCombo, newFileBtn, dgv, saveBtn, deleteBtn
        });

        editorForm.Show();
    }

    // -------------------
    // HiddenForm
    // -------------------
    public class HiddenForm : Form
    {
        Timer timer = new() { Interval = 50 };
        HashSet<Keys> pressed = new();

        public HiddenForm()
        {
            Load += (s, e) => RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL_SHIFT, VK_SPACE);
            FormClosing += (s, e) => UnregisterHotKey(Handle, HOTKEY_ID);
            timer.Tick += Tick;
            timer.Start();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ShowJsonList();
            }
            base.WndProc(ref m);
        }

        void Tick(object sender, EventArgs e)
        {
            if (jsonForm == null || jsonForm.IsDisposed) return;
            var tab = jsonForm.Controls[0] as TabControl;
            if (tab == null) return;

            var list = allTemplates[tab.SelectedIndex];

            for (int i = 0; i < 9; i++)
            {
                Keys k = (Keys)(Keys.D1 + i);
                if ((GetAsyncKeyState(k) & 0x8000) != 0 && !pressed.Contains(k))
                {
                    pressed.Add(k);
                    if (i < list.Count)
                        TriggerTemplateStatic(list[i].text);
                }
                if ((GetAsyncKeyState(k) & 0x8000) == 0)
                    pressed.Remove(k);
            }
        }

        public static void TriggerTemplateStatic(string text)
        {
            PasteText(text);
            jsonForm?.Close();
            jsonForm = null;
        }
    }
}
