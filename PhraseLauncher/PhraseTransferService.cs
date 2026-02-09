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
                sb.Append(CsvField(t.text))
                  .Append(',')
                  .Append(CsvField(t.note))
                  .AppendLine();
            }

            // BOM付きUTF-8
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        // CSV用フィールド生成（改行・カンマ・ダブルクォート完全対応）
        private static string CsvField(string value)
        {
            if (value == null)
                return "\"\"";

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

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
            var list = new List<TemplateItem>();

            using var reader = new StreamReader(path, Encoding.UTF8);

            // ヘッダ
            var header = reader.ReadLine();
            if (header == null)
                throw new Exception("Invalid CSV header.");

            // BOM除去
            header = header.TrimStart('\uFEFF');

            if (!header.StartsWith("Phrase"))
                throw new Exception("Invalid CSV header.");

            var record = new StringBuilder();
            bool inQuotes = false;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (record.Length > 0)
                    record.Append('\n');

                record.Append(line);

                inQuotes = IsOpenQuote(record.ToString());
                if (inQuotes)
                    continue;

                var cols = ParseCsvRecord(record.ToString());
                record.Clear();

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

        private static bool IsOpenQuote(string s)
        {
            bool inQuotes = false;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '"')
                {
                    // "" はエスケープ
                    if (i + 1 < s.Length && s[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }
                    inQuotes = !inQuotes;
                }
            }
            return inQuotes;
        }

        private static string[] ParseCsvRecord(string record)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < record.Length; i++)
            {
                char c = record[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < record.Length && record[i + 1] == '"')
                    {
                        sb.Append('"'); // "" → "
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            result.Add(sb.ToString());
            return result.ToArray();
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
