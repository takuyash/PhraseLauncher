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

        private const int Margin_ = 16;
        private const int CardWidth = 368;

        private LinkRow lnkRepo, lnkHelp, lnkLicense;
        private LinkRow? lnkUpdate;
        private CardPanel card;

        public HelpForm()
        {
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Icon = Program.AppIcon;
            this.BackColor = UiTheme.Surface;
            this.Font = UiTheme.Font;

            /* ===== ヘッダー（アイコン・アプリ名・バージョン） ===== */
            var picture = new PictureBox
            {
                Bounds = new Rectangle(Margin_ + 4, Margin_ + 4, 48, 48),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            if (Program.AppIcon != null)
                picture.Image = new Icon(Program.AppIcon, 48, 48).ToBitmap();

            var lblName = new Label
            {
                Text = "PhraseLauncher",
                Font = UiTheme.TitleFont,
                ForeColor = UiTheme.TextStrong,
                AutoSize = true,
                Location = new Point(picture.Right + 14, picture.Top - 2)
            };

            var lblVersion = new Label
            {
                Text = $"Version {GetVersion()}",
                Font = UiTheme.Font,
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Location = new Point(lblName.Left + 2, picture.Top + 30)
            };

            /* ===== リンク ===== */
            lnkRepo = CreateLink(GitHubRepoUrl);
            lnkHelp = CreateLink(HelpUrl);
            lnkLicense = CreateLink(LicenseUrl);

            var links = new[] { lnkRepo, lnkHelp, lnkLicense };
            card = new CardPanel
            {
                Bounds = new Rectangle(Margin_, picture.Bottom + 20, CardWidth, links.Length * 48)
            };
            for (int i = 0; i < links.Length; i++)
            {
                links[i].Bounds = new Rectangle(1, i * 48, CardWidth - 2, 48);
                links[i].ShowDivider = i > 0;
                card.Controls.Add(links[i]);
            }

            ClientSize = new Size(CardWidth + Margin_ * 2, card.Bottom + Margin_);
            this.Controls.AddRange(new Control[] { picture, lblName, lblVersion, card });

            LanguageManager.LanguageChanged += UpdateText;
            UpdateText();

            // 非同期でチェック
            this.Load += async (s, e) => await CheckForUpdateAsync();
            this.FormClosed += (s, e) =>
            {
                LanguageManager.LanguageChanged -= UpdateText;
                LanguageManager.LanguageChanged -= UpdateUpdateLinkText;
            };
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

        private static LinkRow CreateLink(string url)
        {
            var link = new LinkRow();
            link.Click += (s, e) => OpenUrl(url);
            return link;
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private async Task CheckForUpdateAsync()
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

                if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(url) || IsDisposed) return;

                var latest = new Version(tag.TrimStart('v', 'V'));
                var current = Assembly.GetExecutingAssembly().GetName().Version;

                if (current != null && latest > current)
                {
                    // カードの下にお知らせバナーを追加し、その分ウィンドウを伸ばす
                    lnkUpdate = new LinkRow
                    {
                        Highlight = true,
                        Bounds = new Rectangle(Margin_, card.Bottom + 12, CardWidth, 44),
                        Tag = latest // バージョン値を保持
                    };
                    lnkUpdate.Click += (s, e) => OpenUrl(url);
                    UpdateUpdateLinkText();

                    Controls.Add(lnkUpdate);
                    ClientSize = new Size(ClientSize.Width, lnkUpdate.Bottom + Margin_);

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
