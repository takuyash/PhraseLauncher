using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace PhraseLauncher
{
    static class JsonListForm
    {
        public static List<List<TemplateItem>> AllTemplates = new();

        public static void Show()
        {
            if (Program.JsonForm != null && !Program.JsonForm.IsDisposed)
            {
                Program.JsonForm.BringToFront();
                return;
            }

            Directory.CreateDirectory(TemplateRepository.JsonFolder);
            var files = Directory.GetFiles(TemplateRepository.JsonFolder, "*.json");
            if (files.Length == 0)
            {
                MessageBox.Show("json がありません");
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

            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    form.Close();
                    Program.JsonForm = null;
                }
            };

            TabControl tab = new() { Dock = DockStyle.Fill };

            foreach (var file in files)
            {
                var list = TemplateRepository.Load(file);
                AllTemplates.Add(list);

                ListBox lb = new()
                {
                    Dock = DockStyle.Fill,
                    IntegralHeight = false
                };

                ToolTip tip = new();

                lb.MouseMove += (s, e) =>
                {
                    int i = lb.IndexFromPoint(e.Location);
                    if (i >= 0 && i < list.Count)
                        tip.SetToolTip(lb, list[i].note);
                };

                for (int i = 0; i < list.Count; i++)
                    lb.Items.Add($"{GetShortcutLabel(i)}: {list[i].text.Replace("\n", " ")}");

                // ▲▼キー + Enter対応
                lb.KeyDown += (s, e) =>
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

                // ダブルクリック対応
                lb.DoubleClick += (s, e) =>
                {
                    ExecuteSelected(lb, list, form);
                };

                TabPage page = new(Path.GetFileNameWithoutExtension(file));
                page.Controls.Add(lb);
                tab.TabPages.Add(page);
            }

            tab.KeyDown += (s, e) =>
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

            if (tab.TabPages.Count > 0 &&
                tab.TabPages[0].Controls[0] is ListBox first &&
                first.Items.Count > 0)
            {
                first.SelectedIndex = 0;
                first.Focus();
            }
        }

        private static void ExecuteSelected(ListBox lb, List<TemplateItem> list, Form form)
        {
            if (lb.SelectedIndex < 0) return;

            string text = list[lb.SelectedIndex].text;

            Clipboard.SetText(text);

            form.Close();
            Program.JsonForm = null;

            Timer t = new() { Interval = 50 };
            t.Tick += (s, e) =>
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
