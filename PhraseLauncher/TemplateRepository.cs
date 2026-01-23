using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace PhraseLauncher
{

    static class TemplateRepository
    {
        public static string JsonFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json");

        public static List<TemplateItem> Load(string file)
        {
            if (Path.GetFileNameWithoutExtension(file) == "groups")
                return new List<TemplateItem>();

            var json = File.ReadAllText(file).Replace("\r\n", "\n");

            return JsonSerializer.Deserialize<List<TemplateItem>>(json)
                   ?? new List<TemplateItem>();
        }

        public static void Save(string file, List<TemplateItem> list)
        {
            File.WriteAllText(
                file,
                JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true })
            );
        }
    }

}
