using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhraseLauncher
{
    public class HelpForm : Form
    {

        private const string GitHubRepoUrl = "https://github.com/takuyash/PhraseLauncher";
        private const string HelpUrl = "https://takuyash.github.io/PhraseLauncherSite/docs.html";
        private const string LicenseUrl = "https://github.com/takuyash/PhraseLauncher/blob/main/LICENSE";

        private LinkLabel lnkRepo, lnkHelp, lnkLicense, lnkUpdate;

        public HelpForm()
        {
            this.Size = new Size(420, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Icon = Program.AppIcon;

            var panel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(15),
                AutoScroll = true
            };
            this.Controls.Add(panel);

            // アプリ名
            panel.Controls.Add(new Label()
            {
                Text = "PhraseLauncher",
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                AutoSize = true
            });

            // バージョン
            panel.Controls.Add(new Label()
            {
                Text = $"Version: {GetVersion()}",
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 15)
            });

            lnkRepo = CreateLink("", GitHubRepoUrl) as LinkLabel;
            lnkHelp = CreateLink("", HelpUrl) as LinkLabel;
            lnkLicense = CreateLink("", LicenseUrl) as LinkLabel;

            panel.Controls.Add(lnkRepo);
            panel.Controls.Add(lnkHelp);
            panel.Controls.Add(lnkLicense);

            LanguageManager.LanguageChanged += UpdateText;
            UpdateText();

            // 非同期でチェック
            this.Load += async (s, e) => await CheckForUpdateAsync(panel);
            this.FormClosed += (s, e) => LanguageManager.LanguageChanged -= UpdateText;
        }

        private void UpdateText()
        {
            this.Text = LanguageManager.GetString("HelpTitle");
            lnkRepo.Text = LanguageManager.GetString("HelpRepo");
            lnkHelp.Text = LanguageManager.GetString("HelpUsage");
            lnkLicense.Text = LanguageManager.GetString("HelpLicense");
        }

        private string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }

        private Control CreateLink(string text, string url)
        {
            var link = new LinkLabel()
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };

            link.LinkClicked += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            };

            return link;
        }

        private async Task CheckForUpdateAsync(Control parent)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PhraseLauncher");

                var json = await client.GetStringAsync(
                  "https://api.github.com/repos/takuyash/PhraseLauncher/releases/latest");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tag = root.GetProperty("tag_name").GetString();
                var url = root.GetProperty("html_url").GetString();

                if (string.IsNullOrEmpty(tag)) return;

                var latest = new Version(tag.TrimStart('v', 'V'));
                var current = Assembly.GetExecutingAssembly().GetName().Version;

                if (current != null && latest > current)
                {
                    lnkUpdate = new LinkLabel()
                    {
                        Text = string.Format(LanguageManager.GetString("HelpUpdate"), latest),
                        AutoSize = true,
                        LinkColor = Color.DarkRed,
                        Margin = new Padding(0, 15, 0, 0),
                        Tag = latest // バージョン値を保持
                    };

                    lnkUpdate.LinkClicked += (s, e) =>
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    };

                    parent.Controls.Add(lnkUpdate);

                    // アップデートリンク用の動的更新イベント登録
                    LanguageManager.LanguageChanged += UpdateUpdateLinkText;
                }
            }
            catch
            {
                // 失敗しても何もしない
            }
        }

        private void UpdateUpdateLinkText()
        {
            if (lnkUpdate != null)
                lnkUpdate.Text = string.Format(LanguageManager.GetString("HelpUpdate"), lnkUpdate.Tag);
        }
    }
}