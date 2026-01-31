using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PhraseLauncher
{
    static class Program
    {
        public static KeyboardManager KbdManager;
        public static Form JsonForm;
        public static NotifyIcon TrayIcon;

        // どこからでも参照できるアイコンオブジェクト
        public static Icon AppIcon;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // アイコンを一度だけ読み込む
            AppIcon = LoadIcon("icon.ico");

            // タスクトレイの設定
            TrayIcon = new NotifyIcon
            {
                Icon = AppIcon,
                Visible = true,
                Text = "PhraseLauncher"
            };

            // 言語変更時にメニューを更新
            LanguageManager.LanguageChanged += UpdateTrayMenu;
            UpdateTrayMenu();

            // キーボード監視開始
            KbdManager = new KeyboardManager();

            Application.Run();
        }

        private static void UpdateTrayMenu()
        {
            // メニュー作成
            ContextMenuStrip menu = new();
            menu.Items.Add(new ToolStripMenuItem(LanguageManager.GetString("MenuShow"), null, (s, e) => JsonListForm.Show()));
            menu.Items.Add(new ToolStripMenuItem(LanguageManager.GetString("MenuEdit"), null, (s, e) => new JsonEditorForm().Show()));
            menu.Items.Add(new ToolStripMenuItem(LanguageManager.GetString("MenuSetting"), null, (s, e) => new SettingForm().Show()));
            menu.Items.Add(new ToolStripMenuItem(LanguageManager.GetString("MenuHelp"), null, (s, e) => new HelpForm().Show()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(LanguageManager.GetString("MenuExit"), null, (s, e) => ExitApplication()));
            TrayIcon.ContextMenuStrip = menu;
        }

        private static Icon LoadIcon(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(path))
            {
                try { return new Icon(path); } catch { }
            }
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        public static void ExitApplication()
        {
            KbdManager?.Dispose();
            if (TrayIcon != null)
            {
                TrayIcon.Visible = false;
                TrayIcon.Dispose();
            }
            Application.Exit();
        }
    }
}