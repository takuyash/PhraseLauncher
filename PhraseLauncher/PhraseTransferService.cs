using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

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
    }
}
