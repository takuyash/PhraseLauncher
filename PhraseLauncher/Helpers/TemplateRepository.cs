using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Linq;

namespace PhraseLauncher
{
    static class TemplateRepository
    {
        public static string JsonFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json");

        /* ================= DPAPI ================= */

        private static byte[] Encrypt(string text)
        {
            return ProtectedData.Protect(
                Encoding.UTF8.GetBytes(text),
                null,
                DataProtectionScope.CurrentUser
            );
        }

        private static string Decrypt(byte[] data)
        {
            var bytes = ProtectedData.Unprotect(
                data,
                null,
                DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(bytes);
        }

        /* ================= Load / Save ================= */

        public static List<TemplateItem> Load(string file)
        {
            // groups.json は今まで通り平文
            if (Path.GetFileNameWithoutExtension(file) == "groups")
                return new List<TemplateItem>();

            if (!File.Exists(file))
                return new List<TemplateItem>();

            try
            {
                if (LanguageManager.EnableTemplateEncryption)
                {
                    var encrypted = File.ReadAllBytes(file);
                    var json = Decrypt(encrypted).Replace("\r\n", "\n");
                    return JsonSerializer.Deserialize<List<TemplateItem>>(json)
                           ?? new List<TemplateItem>();
                }
                else
                {
                    var json = File.ReadAllText(file).Replace("\r\n", "\n");
                    return JsonSerializer.Deserialize<List<TemplateItem>>(json)
                           ?? new List<TemplateItem>();
                }
            }
            catch
            {
                // 旧：平文json救済（初回起動用）
                try
                {
                    var json = File.ReadAllText(file).Replace("\r\n", "\n");
                    var list = JsonSerializer.Deserialize<List<TemplateItem>>(json)
                               ?? new List<TemplateItem>();

                    // 次回から暗号化されるよう保存し直す
                    Save(file, list);
                    return list;
                }
                catch
                {
                    return new List<TemplateItem>();
                }
            }
        }

        /* ================= Save ================= */

        public static void Save(string file, List<TemplateItem> list)
        {
            var json = JsonSerializer.Serialize(
                list,
                new JsonSerializerOptions { WriteIndented = true }
            );

            if (LanguageManager.EnableTemplateEncryption)
            {
                File.WriteAllBytes(file, Encrypt(json));
            }
            else
            {
                File.WriteAllText(file, json);
            }
        }

        /* ================= Encryption Toggle ================= */

        public static void ApplyEncryptionSetting(bool enable)
        {
            if (!Directory.Exists(JsonFolder)) return;

            foreach (var file in Directory.GetFiles(JsonFolder, "*.json"))
            {
            	// groups.json は除外
                if (Path.GetFileNameWithoutExtension(file) == "groups")
                    continue;

                try
                {
                    var list = LoadAuto(file);

                    var json = JsonSerializer.Serialize(
                        list,
                        new JsonSerializerOptions { WriteIndented = true }
                    );

                    if (enable)
                        File.WriteAllBytes(file, Encrypt(json));
                    else
                        File.WriteAllText(file, json);
                }
                catch
                {
                    // 変換失敗時は何もしない
                }
            }
        }
        private static List<TemplateItem> LoadAuto(string file)
        {
            // まず暗号化として試す
            try
            {
                var encrypted = File.ReadAllBytes(file);
                var json = Decrypt(encrypted).Replace("\r\n", "\n");

                return JsonSerializer.Deserialize<List<TemplateItem>>(json)
                       ?? new List<TemplateItem>();
            }
            catch
            {
                // だめなら平文
                try
                {
                    var json = File.ReadAllText(file).Replace("\r\n", "\n");
                    return JsonSerializer.Deserialize<List<TemplateItem>>(json)
                           ?? new List<TemplateItem>();
                }
                catch
                {
                    return new List<TemplateItem>();
                }
            }
        }

        public static string[] GetGroupNames()
        {
            if (!Directory.Exists(JsonFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(JsonFolder, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => n != "groups")
                .ToArray();
        }
    }
}
