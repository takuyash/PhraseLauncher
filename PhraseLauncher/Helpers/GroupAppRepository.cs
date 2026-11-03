using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace PhraseLauncher
{
    /// <summary>
    /// グループ（タブ）ごとの対象アプリ（プロセス名）を管理する。
    /// 定型文一覧を開いたとき、使用中のアプリに対応するタブを最初に表示するために使う。
    /// </summary>
    static class GroupAppRepository
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // *.json を走査してグループ一覧を作る処理が多いため、拡張子は .json 以外にする
        private static string FilePath =>
            Path.Combine(TemplateRepository.JsonFolder, "group_apps.cfg");

        /// <summary>
        /// 全グループの対象アプリを読み込む（キー：グループ名、大文字小文字は区別しない）
        /// </summary>
        public static Dictionary<string, List<string>> LoadAll()
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(FilePath)) return result;

            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(FilePath));
                if (data != null)
                {
                    foreach (var kv in data)
                    {
                        var apps = (kv.Value ?? new List<string>())
                            .Select(Normalize)
                            .Where(a => a.Length > 0)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        if (apps.Count > 0) result[kv.Key] = apps;
                    }
                }
            }
            catch
            {
                // 壊れている場合は未設定扱い
            }
            return result;
        }

        private static void SaveAll(Dictionary<string, List<string>> data)
        {
            Directory.CreateDirectory(TemplateRepository.JsonFolder);
            var sorted = data
                .Where(kv => kv.Value.Count > 0)
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(sorted, new JsonSerializerOptions
            {
                WriteIndented = true,
                // グループ名（日本語）を \uXXXX ではなくそのまま書き出す
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            }));
        }

        public static List<string> Get(string group) =>
            LoadAll().TryGetValue(group, out var apps) ? apps : new List<string>();

        public static void Set(string group, IEnumerable<string> apps)
        {
            var data = LoadAll();
            var list = apps
                .Select(Normalize)
                .Where(a => a.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count > 0) data[group] = list;
            else data.Remove(group);
            SaveAll(data);
        }

        public static void Rename(string oldName, string newName)
        {
            var data = LoadAll();
            if (!data.TryGetValue(oldName, out var apps)) return;
            data.Remove(oldName);
            data[newName] = apps;
            SaveAll(data);
        }

        public static void Remove(string group)
        {
            var data = LoadAll();
            if (data.Remove(group)) SaveAll(data);
        }

        /// <summary>
        /// 入力をプロセス名の形にそろえる（パスと .exe を取り除く）
        /// 例："C:\Program Files\...\EXCEL.EXE" → "EXCEL"
        /// </summary>
        public static string Normalize(string? app)
        {
            if (string.IsNullOrWhiteSpace(app)) return "";
            string name = app.Trim().Trim('"');
            try { name = Path.GetFileName(name); } catch { }
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                name = name[..^4];
            return name.Trim();
        }

        /// <summary>
        /// 現在前面にあるウィンドウのプロセス名。取得できない場合は null
        /// </summary>
        public static string? GetForegroundProcessName()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0 || pid == (uint)Environment.ProcessId) return null;
                using var p = Process.GetProcessById((int)pid);
                return p.ProcessName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 指定したアプリが登録されているグループのうち、並び順で最初のものを返す
        /// </summary>
        public static int FindGroupIndex(string? processName, IList<string> orderedGroups)
        {
            if (string.IsNullOrEmpty(processName)) return -1;
            var data = LoadAll();
            for (int i = 0; i < orderedGroups.Count; i++)
            {
                if (data.TryGetValue(orderedGroups[i], out var apps) &&
                    apps.Any(a => string.Equals(a, processName, StringComparison.OrdinalIgnoreCase)))
                    return i;
            }
            return -1;
        }
    }
}
