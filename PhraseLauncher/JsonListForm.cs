using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{
    static class JsonListForm
    {
        // ウィンドウを強制的に最前面に持ってくるためのWin32 API
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static List<List<TemplateItem>> AllTemplates = new();

        private static string GroupOrderPath =>
          Path.Combine(TemplateRepository.JsonFolder, "groups.json");


        public static void Show()
        {
            if (Program.JsonForm != null && !Program.JsonForm.IsDisposed)
            {
                Program.JsonForm.BringToFront();
                Program.JsonForm.Activate(); // 既存ウィンドウがある場合もアクティブ化
                return;
            }

            Directory.CreateDirectory(TemplateRepository.JsonFolder);

            // ===============================
            // groups.json 読み込み
            // ===============================
            List<string> groupOrder = new();
            if (File.Exists(GroupOrderPath))
            {
                try
                {
                    groupOrder = JsonSerializer.Deserialize<List<string>>(
                      File.ReadAllText(GroupOrderPath)
                    ) ?? new List<string>();
                }
                catch
                {
                    groupOrder = new List<string>();
                }
            }

            // ===============================
            // ファイル一覧（groups.json除外）
            // ＋ groups.json の順で並び替え
            // ===============================
            var files = Directory.GetFiles(TemplateRepository.JsonFolder, "*.json")
        .Where(f => Path.GetFileName(f) != "groups.json")
        .OrderBy(f =>
        {
            var name = Path.GetFileNameWithoutExtension(f);
            int index = groupOrder.IndexOf(name);
            return index == -1 ? int.MaxValue : index;
        })
        .ThenBy(f => Path.GetFileNameWithoutExtension(f))
        .ToArray();

            if (files.Length == 0)
            {
                MessageBox.Show(
                  LanguageManager.GetString("ListEmpty"),
                  "PhraseLauncher",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information
                );
                return;
            }

            AllTemplates.Clear();

            Form form = new()
            {
                Width = 500,
                Height = 400,
                TopMost = true,
                Text = LanguageManager.GetString("ListTitle"),
                KeyPreview = true,
                StartPosition = FormStartPosition.CenterScreen,
                Icon = Program.AppIcon
            };

            Program.JsonForm = form;

            // 言語変更時にタイトルを更新
            Action updateTitle = () => form.Text = LanguageManager.GetString("ListTitle");
            LanguageManager.LanguageChanged += updateTitle;
            form.FormClosed += (s, e) => LanguageManager.LanguageChanged -= updateTitle;

            // 検索パネル
            Panel searchPanel = new() { Dock = DockStyle.Top, Height = 30, Padding = new Padding(5) };
            TextBox searchBox = new() { Dock = DockStyle.Fill };

            // メモ検索切り替え
            CheckBox includeNoteCheck = new()
            {
                Text = "メモも含める",
                Dock = DockStyle.Right,
                Width = 110,
                Checked = true
            };

            searchPanel.Controls.Add(searchBox);
            searchPanel.Controls.Add(includeNoteCheck);

            TabControl tab = new() { Dock = DockStyle.Fill };

            // データのロード
            List<List<TemplateItem>> originalData = new();
            // ===============================
            // タブ生成
            // ===============================
            foreach (var file in files)
            {
                try { originalData.Add(TemplateRepository.Load(file) ?? new List<TemplateItem>()); }
                catch { originalData.Add(new List<TemplateItem>()); }
            }
            AllTemplates = originalData;

            // リスト更新用アクション
            Action updateLists = () =>
            {
                string query = searchBox.Text.ToLower();
                bool includeNote = includeNoteCheck.Checked;

                for (int i = 0; i < tab.TabPages.Count; i++)
                {
                    var lb = tab.TabPages[i].Controls[0] as ListBox;
                    var filtered = originalData[i]
                        .Where(x =>
                            (x.text ?? "").ToLower().Contains(query) ||
                            (includeNote && (x.note ?? "").ToLower().Contains(query))
                        )
                        .ToList();

                    lb.Tag = filtered; // 現在の表示データを保持
                    lb.Items.Clear();
                    for (int j = 0; j < filtered.Count; j++)
                    {
                        string label = GetShortcutLabel(j);
                        string prefix = string.IsNullOrEmpty(label) ? "" : $"{label}: ";
                        lb.Items.Add($"{prefix}{filtered[j].text?.Replace("\n", " ")}");
                    }
                }
            };

            searchBox.TextChanged += (s, e) => updateLists();
            includeNoteCheck.CheckedChanged += (s, e) => updateLists();

            // タブとリストの生成
            for (int i = 0; i < files.Length; i++)
            {
                ListBox lb = new() { Dock = DockStyle.Fill, IntegralHeight = false };
                ToolTip tip = new();
                lb.MouseMove += (s, e) =>
                {
                    var items = lb.Tag as List<TemplateItem>;
                    int idx = lb.IndexFromPoint(e.Location);
                    if (idx >= 0 && items != null && idx < items.Count)
                        tip.SetToolTip(lb, items[idx].note ?? "");
                };
                // ▲▼ + Enter
                lb.KeyDown += (s, e) =>
                {
                    var items = lb.Tag as List<TemplateItem>;
                    // 1. Enterキーの処理
                    if (e.KeyCode == Keys.Enter && items != null)
                    {
                        ExecuteSelected(lb, items, form);
                        e.Handled = true;
                    }
                    // 2. 上矢印キーでタブへフォーカス移動
                    else if (e.KeyCode == Keys.Up && lb.SelectedIndex <= 0)
                    {
                        tab.Focus();
                        e.Handled = true;
                    }
                    else
                    {
                        // ショートカット
                        int targetIndex = GetIndexFromKey(e.KeyCode);
                        if (targetIndex >= 0 && items != null && targetIndex < items.Count)
                        {
                            lb.SelectedIndex = targetIndex;
                            ExecuteSelected(lb, items, form);
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                        }
                    }
                };
                // ダブルクリック
                lb.DoubleClick += (s, e) => { if (lb.Tag is List<TemplateItem> items) ExecuteSelected(lb, items, form); };

                var page = new TabPage(Path.GetFileNameWithoutExtension(files[i]));
                page.Controls.Add(lb);
                tab.TabPages.Add(page);
            }

            // 検索ボックスのキー制御
            searchBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter)
                {
                    tab.Focus();
                    e.Handled = true;
                }
            };

            // タブのキー制御
            tab.KeyDown += (s, e) =>
            {
                if (!tab.Focused) return;
                var lb = tab.SelectedTab.Controls[0] as ListBox;

                if (e.KeyCode == Keys.Down)
                {
                    lb.Focus();
                    if (lb.Items.Count > 0) lb.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Up)
                {
                    searchBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                {
                    // 標準のタブ切り替えを維持
                }
                else
                {
                    // ショートカット
                    int targetIndex = GetIndexFromKey(e.KeyCode);
                    if (targetIndex >= 0 && lb.Tag is List<TemplateItem> items && targetIndex < items.Count)
                    {
                        lb.SelectedIndex = targetIndex;
                        ExecuteSelected(lb, items, form);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
            };

            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { form.Close(); Program.JsonForm = null; }
            };

            form.Controls.Add(tab);
            form.Controls.Add(searchPanel);

            updateLists();
            // ===============================
            // 確実にフォーカスを当てる処理
            // ===============================
            form.Show();

            // OSにウィンドウを前面に出すよう強制
            SetForegroundWindow(form.Handle);
            form.Activate();

            // 初期フォーカス：リスト部分
            if (tab.TabPages.Count > 0)
            {
                var firstLb = tab.TabPages[0].Controls[0] as ListBox;
                firstLb.Focus();
                if (firstLb.Items.Count > 0) firstLb.SelectedIndex = 0;
            }
        }

        private static int GetIndexFromKey(Keys key)
        {
            if (key >= Keys.D1 && key <= Keys.D9) return key - Keys.D1;
            if (key >= Keys.NumPad1 && key <= Keys.NumPad9) return key - Keys.NumPad1;
            if (key >= Keys.A && key <= Keys.Z) return (key - Keys.A) + 9;
            return -1;
        }

        private static void ExecuteSelected(ListBox lb, List<TemplateItem> list, Form form)
        {
            if (lb.SelectedIndex < 0 || lb.SelectedIndex >= list.Count) return;
            string text = list[lb.SelectedIndex].text ?? "";
            Clipboard.SetText(text);
            form.Close();
            Program.JsonForm = null;

            Timer t = new() { Interval = 50 };
            t.Tick += (s, e) => { t.Stop(); t.Dispose(); SendKeys.SendWait("^v"); };
            t.Start();
        }

        static string GetShortcutLabel(int index)
        {
            // 1-9 (index 0-8)
            if (index < 9) return (index + 1).ToString();
            // a-z (index 9-34)
            if (index <= 34) return ((char)('a' + (index - 9))).ToString();

            // それ以降は割り振らない
            return "";
        }
    }
}