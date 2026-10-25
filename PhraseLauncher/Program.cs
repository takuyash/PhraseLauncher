using System;
using System.Drawing;
using System.IO;


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
            // ==========================================
            // 二重起動防止
            // ==========================================
            bool createdNew;

            using Mutex mutex = new Mutex(
                true,
                "PhraseLauncher_SingleInstance",
                out createdNew);

            // すでに起動している場合は何もせず終了
            if (!createdNew)
            {
                return;
            }
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
            ModernContextMenu menu = new();
            menu.AddHeader("PhraseLauncher");
            menu.AddItem(LanguageManager.GetString("MenuShow"), MenuIcon.List, (s, e) => JsonListForm.Show());
            menu.AddItem(LanguageManager.GetString("MenuEdit"), MenuIcon.Edit, (s, e) => new JsonEditorForm().Show());
            menu.AddItem(LanguageManager.GetString("MenuTransfer"), MenuIcon.Transfer, (s, e) => new PhraseTransferForm().Show());
            menu.AddItem(LanguageManager.GetString("MenuSetting"), MenuIcon.Settings, (s, e) => new SettingForm().Show());
            menu.AddItem(LanguageManager.GetString("MenuHelp"), MenuIcon.Help, (s, e) => new HelpForm().Show());
            menu.Items.Add(new ToolStripSeparator());
            menu.AddItem(LanguageManager.GetString("MenuExit"), MenuIcon.Exit, (s, e) => ExitApplication(), danger: true);
            var old = TrayIcon.ContextMenuStrip;
            TrayIcon.ContextMenuStrip = menu;
            old?.Dispose();
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