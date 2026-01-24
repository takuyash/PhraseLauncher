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
                    "定型文の登録がありません。\nタスクトレイのアプリを右クリックして登録してください。",
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
                Text = "定型文一覧",
                KeyPreview = true,
                StartPosition = FormStartPosition.CenterScreen
            };

            Program.JsonForm = form;

            form.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    form.Close();
                    Program.JsonForm = null;
                }
            };

            TabControl tab = new() { Dock = DockStyle.Fill };

            // ===============================
            // タブ生成
            // ===============================
            foreach (var file in files)
            {
                List<TemplateItem> list;

                try
                {
                    list = TemplateRepository.Load(file) ?? new List<TemplateItem>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"{Path.GetFileName(file)} の読み込みに失敗しました。\n\n{ex.Message}",
                        "JSONエラー",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    continue;
                }

                AllTemplates.Add(list);

                ListBox lb = new()
                {
                    Dock = DockStyle.Fill,
                    IntegralHeight = false
                };

                ToolTip tip = new();

                lb.MouseMove += (_, e) =>
                {
                    int i = lb.IndexFromPoint(e.Location);
                    if (i >= 0 && i < list.Count)
                        tip.SetToolTip(lb, list[i].note ?? "");
                };

                for (int i = 0; i < list.Count; i++)
                {
                    string text = list[i].text ?? "";
                    string label = GetShortcutLabel(i);
                    string prefix = string.IsNullOrEmpty(label) ? "" : $"{label}: ";
                    lb.Items.Add($"{prefix}{text.Replace("\n", " ")}");
                }

                // ▲▼ + Enter
                lb.KeyDown += (_, e) =>
                {
                    // 1. Enterキーの処理
                    if (e.KeyCode == Keys.Enter)
                    {
                        ExecuteSelected(lb, list, form);
                        e.Handled = true;
                        return;
                    }

                    // 2. 上矢印キーでタブへフォーカス移動
                    if (e.KeyCode == Keys.Up && lb.SelectedIndex == 0)
                    {
                        tab.Focus();
                        e.Handled = true;
                        return;
                    }

                    // 3. ショートカットキー(1-9, A-Z)の判定
                    int targetIndex = -1;
                    if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
                        targetIndex = e.KeyCode - Keys.D1;
                    else if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9)
                        targetIndex = e.KeyCode - Keys.NumPad1;
                    else if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
                        targetIndex = (e.KeyCode - Keys.A) + 9;

                    // 範囲内かつラベルが存在するインデックスなら実行
                    if (targetIndex >= 0 && targetIndex < list.Count && targetIndex <= 34)
                    {
                        lb.SelectedIndex = targetIndex;
                        ExecuteSelected(lb, list, form);
                        e.Handled = true;
                        e.SuppressKeyPress = true; // ビープ音防止
                    }
                };

                // ダブルクリック
                lb.DoubleClick += (_, _) =>
                {
                    ExecuteSelected(lb, list, form);
                };

                var page = new TabPage(Path.GetFileNameWithoutExtension(file));
                page.Controls.Add(lb);
                tab.TabPages.Add(page);
            }

            if (tab.TabPages.Count == 0) return;

            tab.KeyDown += (_, e) =>
            {
                if (!tab.Focused) return;

                if (tab.SelectedTab.Controls[0] is ListBox targetLb && targetLb.Items.Count > 0)
                {
                    // 下矢印でフォーカス移動
                    if (e.KeyCode == Keys.Down)
                    {
                        targetLb.Focus();
                        targetLb.SelectedIndex = 0;
                        e.Handled = true;
                        return;
                    }

                    // タブにフォーカスがある状態でもショートカット(1-9, A-Z)を処理
                    int targetIndex = -1;
                    if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
                        targetIndex = e.KeyCode - Keys.D1;
                    else if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9)
                        targetIndex = e.KeyCode - Keys.NumPad1;
                    else if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
                        targetIndex = (e.KeyCode - Keys.A) + 9;

                    if (targetIndex >= 0 && targetIndex < AllTemplates[tab.SelectedIndex].Count && targetIndex <= 34)
                    {
                        targetLb.SelectedIndex = targetIndex;
                        ExecuteSelected(targetLb, AllTemplates[tab.SelectedIndex], form);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
            };

            form.Controls.Add(tab);

            // ===============================
            // 確実にフォーカスを当てる処理
            // ===============================
            form.Show();

            // OSにウィンドウを前面に出すよう強制
            SetForegroundWindow(form.Handle);
            form.Activate();

            // 初期フォーカス設定
            if (tab.TabPages.Count > 0 && tab.TabPages[0].Controls[0] is ListBox first && first.Items.Count > 0)
            {
                first.SelectedIndex = 0;
                first.Focus();
            }
        }

        private static void ExecuteSelected(ListBox lb, List<TemplateItem> list, Form form)
        {
            if (lb.SelectedIndex < 0 || lb.SelectedIndex >= list.Count) return;

            string text = list[lb.SelectedIndex].text ?? "";

            Clipboard.SetText(text);

            form.Close();
            Program.JsonForm = null;

            Timer t = new() { Interval = 50 };
            t.Tick += (_, _) =>
            {
                t.Stop();
                t.Dispose();
                SendKeys.SendWait("^v");
            };
            t.Start();
        }

        static string GetShortcutLabel(int index)
        {
            // 1-9 (index 0-8)
            if (index < 9) return (index + 1).ToString();

            // a-z (index 9-34)
            if (index <= 34)
            {
                return ((char)('a' + (index - 9))).ToString();
            }

            // それ以降は割り振らない
            return "";
        }
    }
}