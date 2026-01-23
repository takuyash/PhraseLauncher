using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{
    static class JsonListForm
    {
        public static List<List<TemplateItem>> AllTemplates = new();

        private static string GroupOrderPath =>
            Path.Combine(TemplateRepository.JsonFolder, "groups.json");

        public static void Show()
        {
            if (Program.JsonForm != null && !Program.JsonForm.IsDisposed)
            {
                Program.JsonForm.BringToFront();
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
                .ThenBy(f => Path.GetFileNameWithoutExtension(f)) // 保険
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
                    lb.Items.Add($"{GetShortcutLabel(i)}: {text.Replace("\n", " ")}");
                }

                // ▲▼ + Enter
                lb.KeyDown += (_, e) =>
                {
                    if (e.KeyCode == Keys.Up && lb.SelectedIndex == 0)
                    {
                        tab.Focus();
                        e.Handled = true;
                        return;
                    }

                    if (e.KeyCode == Keys.Enter)
                    {
                        ExecuteSelected(lb, list, form);
                        e.Handled = true;
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

            if (tab.TabPages.Count == 0)
            {
                MessageBox.Show(
                    "有効な定型文がありません。",
                    "PhraseLauncher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            tab.KeyDown += (_, e) =>
            {
                if (!tab.Focused) return;

                if (e.KeyCode == Keys.Down &&
                    tab.SelectedTab.Controls[0] is ListBox lb &&
                    lb.Items.Count > 0)
                {
                    lb.Focus();
                    lb.SelectedIndex = 0;
                    e.Handled = true;
                }
            };

            form.Controls.Add(tab);
            form.Show();

            // 初期フォーカス
            if (tab.TabPages[0].Controls[0] is ListBox first &&
                first.Items.Count > 0)
            {
                first.SelectedIndex = 0;
                first.Focus();
            }
        }

        private static void ExecuteSelected(ListBox lb, List<TemplateItem> list, Form form)
        {
            if (lb.SelectedIndex < 0) return;

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
    }
}
