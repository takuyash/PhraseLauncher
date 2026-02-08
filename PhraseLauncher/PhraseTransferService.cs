using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace PhraseLauncher
{
    static class PhraseTransferService
    {
        public static void ExportAllGroups(
            string outputFolder,
            PhraseTransferFormat format)
        {
            Directory.CreateDirectory(outputFolder);

            foreach (var file in Directory.GetFiles(
                TemplateRepository.JsonFolder, "*.json"))
            {
                var groupName = Path.GetFileNameWithoutExtension(file);

                // groups.json は対象外
                if (groupName == "groups")
                    continue;

                var items = TemplateRepository.Load(file); // ← 復号

                if (items.Count == 0)
                    continue;

                var outPath = Path.Combine(
                    outputFolder,
                    groupName + (format == PhraseTransferFormat.Json ? ".json" : ".csv")
                );

                if (format == PhraseTransferFormat.Json)
                    ExportJson(outPath, items);
                else
                    ExportCsv(outPath, items);
            }
        }

        private static void ExportJson(string path, List<TemplateItem> list)
        {
            var json = JsonSerializer.Serialize(
                list,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static void ExportCsv(string path, List<TemplateItem> list)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Phrase,Note");

            foreach (var t in list)
            {
                sb.Append('"').Append(Escape(t.text)).Append("\",");
                sb.Append('"').Append(Escape(t.note)).Append('"');
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string Escape(string s)
            => (s ?? "").Replace("\"", "\"\"");


        public static bool Import(
    string filePath,
    string groupName,
    PhraseTransferFormat format,
    out string error)
        {
            error = "";

            try
            {
                List<TemplateItem> items;

                if (format == PhraseTransferFormat.Json)
                    items = ImportJson(filePath);
                else
                    items = ImportCsv(filePath);

                if (items.Count == 0)
                {
                    error = "No valid data.";
                    return false;
                }

                var path = Path.Combine(
                    TemplateRepository.JsonFolder,
                    groupName + ".json"
                );

                TemplateRepository.Save(path, items); // ← 暗号化保存
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static List<TemplateItem> ImportJson(string path)
        {
            var json = File.ReadAllText(path).Replace("\r\n", "\n");

            var list = JsonSerializer.Deserialize<List<TemplateItem>>(json);
            if (list == null)
                throw new Exception("Invalid JSON format.");

            return list;
        }

        private static List<TemplateItem> ImportCsv(string path)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length < 2 || !lines[0].StartsWith("Phrase"))
                throw new Exception("Invalid CSV header.");

            var list = new List<TemplateItem>();

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = ParseCsvLine(lines[i]);
                if (cols.Length < 2)
                    continue;

                list.Add(new TemplateItem
                {
                    text = cols[0],
                    note = cols[1]
                });
            }

            return list;
        }
        private static string[] ParseCsvLine(string line)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    quoted = !quoted;
                    continue;
                }

                if (c == ',' && !quoted)
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            list.Add(sb.ToString());
            return list.ToArray();
        }
        public static string[] GetGroupNames()
        {
            if (!Directory.Exists(TemplateRepository.JsonFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(TemplateRepository.JsonFolder, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => n != "groups")
                .ToArray();
        }


    }
}
