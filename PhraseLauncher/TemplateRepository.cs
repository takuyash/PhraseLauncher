using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

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
                var encrypted = File.ReadAllBytes(file);
                var json = Decrypt(encrypted).Replace("\r\n", "\n");

                return JsonSerializer.Deserialize<List<TemplateItem>>(json)
                       ?? new List<TemplateItem>();
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

        public static void Save(string file, List<TemplateItem> list)
        {
            var json = JsonSerializer.Serialize(
                list,
                new JsonSerializerOptions { WriteIndented = true }
            );

            var encrypted = Encrypt(json);
            File.WriteAllBytes(file, encrypted);
        }
    }
}
