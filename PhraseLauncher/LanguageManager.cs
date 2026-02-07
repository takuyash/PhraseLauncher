using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PhraseLauncher
{
    public static class LanguageManager
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private static string IniPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
        public static string CurrentLanguage { get; private set; } = "ja";

        public static bool EnableHotKey { get; private set; } = true;

        public static event Action LanguageChanged;

        static LanguageManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            StringBuilder temp = new StringBuilder(255);
            GetPrivateProfileString("Settings", "Language", "ja", temp, 255, IniPath);
            CurrentLanguage = temp.ToString();

            GetPrivateProfileString("Settings", "EnableHotKey", "true", temp, 255, IniPath);
            EnableHotKey = bool.TryParse(temp.ToString(), out bool enabled) ? enabled : true;
        }

        public static void SaveLanguage(string lang)
        {
            CurrentLanguage = lang;
            WritePrivateProfileString("Settings", "Language", lang, IniPath);
            LanguageChanged?.Invoke();
        }

        public static string GetString(string key)
        {
            var dict = CurrentLanguage == "en" ? English : Japanese;
            return dict.ContainsKey(key) ? dict[key] : key;
        }

        public static void SaveEnableHotKey(bool enabled)
        {
            EnableHotKey = enabled;
            WritePrivateProfileString("Settings", "EnableHotKey", enabled.ToString(), IniPath);
        }

        private static readonly Dictionary<string, string> Japanese = new()
        {
            { "MenuShow", "一覧表示" },
            { "MenuEdit", "編集/登録" },
            { "MenuSetting", "設定" },
            { "MenuHelp", "ヘルプ" },
            { "MenuExit", "終了" },
            { "HelpTitle", "ヘルプ / バージョン情報" },
            { "HelpRepo", "GitHub リポジトリ" },
            { "HelpUsage", "ヘルプ / 使い方" },
            { "HelpLicense", "ライセンス" },
            { "HelpUpdate", "新しいバージョンがあります（v{0}）" },
            { "ListTitle", "定型文一覧" },
            { "ListEmpty", "定型文の登録がありません。\nタスクトレイのアプリを右クリックして登録してください。" },
            { "SettingTitle", "設定" },
            { "SettingLang", "言語 (Language):" },
            { "BtnSave", "保存" },
            // JsonEditorForm
            { "EditorTitle", "定型文編集・登録" },
            { "EditorNew", "新規作成" },
            { "EditorRename", "グループ名変更" },
            { "EditorDeleteGroup", "グループ削除" },
            { "EditorSave", "保存" },
            { "EditorDelete", "削除" },
            { "EditorColPhrase", "定型文" },
            { "EditorColNote", "メモ" },
            { "EditorMsgConfirm", "削除しますか？" },
            { "EditorMsgConfirmGroup", "グループ「{0}」を削除しますか？\n定型文もすべて消えます。" },
            { "EditorMsgConfirmTitle", "確認" },
            { "EditorMsgSaved", "保存しました。" },
            { "EditorMsgExists", "既に存在します。" },
            { "EditorInputNewTitle", "作成" },
            { "EditorInputNewMsg", "新規グループ名" },
            { "EditorInputRenameTitle", "変更" },
            { "EditorInputRenameMsg", "新しい名前" }
        };

        private static readonly Dictionary<string, string> English = new()
        {
            { "MenuShow", "Show List" },
            { "MenuEdit", "Edit/Register" },
            { "MenuSetting", "Settings" },
            { "MenuHelp", "Help" },
            { "MenuExit", "Exit" },
            { "HelpTitle", "Help / Version Info" },
            { "HelpRepo", "GitHub Repository" },
            { "HelpUsage", "Help / Usage" },
            { "HelpLicense", "License" },
            { "HelpUpdate", "New version available (v{0})" },
            { "ListTitle", "Phrase List" },
            { "ListEmpty", "No phrases registered.\nRight-click the tray icon to register." },
            { "SettingTitle", "Settings" },
            { "SettingLang", "Language:" },
            { "BtnSave", "Save" },
            // JsonEditorForm
            { "EditorTitle", "Edit/Register Phrases" },
            { "EditorNew", "New" },
            { "EditorRename", "Rename" },
            { "EditorDeleteGroup", "Delete Group" },
            { "EditorSave", "Save" },
            { "EditorDelete", "Delete" },
            { "EditorColPhrase", "Phrase" },
            { "EditorColNote", "Note" },
            { "EditorMsgConfirm", "Are you sure you want to delete?" },
            { "EditorMsgConfirmGroup", "Delete group '{0}'?\nAll phrases in this group will be lost." },
            { "EditorMsgConfirmTitle", "Confirm" },
            { "EditorMsgSaved", "Saved successfully." },
            { "EditorMsgExists", "Already exists." },
            { "EditorInputNewTitle", "Create" },
            { "EditorInputNewMsg", "New Group Name" },
            { "EditorInputRenameTitle", "Rename" },
            { "EditorInputRenameMsg", "New Name" }
        };
    }
}