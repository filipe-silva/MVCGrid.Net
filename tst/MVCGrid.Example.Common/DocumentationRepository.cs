using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MVCGrid.Example.Common
{
    /// <summary>One row of the API-reference tables shown on the Documentation page.</summary>
    public class MethodDocItem
    {
        public string Class;
        public int Order;
        public string Return;
        public string Name;
        public string Description;
    }

    /// <summary>
    /// Reads the API-reference data (documentation.csv, embedded in this assembly) that feeds
    /// the four doc grids (GridDefinition/GridColumn/QueryOptions/ClientSide) registered in
    /// <see cref="SampleGrids"/> — portable, so every host shows the same Documentation page.
    /// </summary>
    public static class DocumentationRepository
    {
        private static readonly Lazy<List<MethodDocItem>> _documentation =
            new Lazy<List<MethodDocItem>>(Load);

        public static List<MethodDocItem> GetData(string className)
        {
            return _documentation.Value.Where(p => p.Class == className).OrderBy(p => p.Order).ToList();
        }

        private static List<MethodDocItem> Load()
        {
            var items = new List<MethodDocItem>();
            var asm = typeof(DocumentationRepository).Assembly;
            string name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("documentation.csv", StringComparison.Ordinal));
            if (name == null) return items;

            using (var stream = asm.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var f = ParseCsvLine(line);
                    if (f.Count < 5) continue;
                    items.Add(new MethodDocItem
                    {
                        Class = f[0],
                        Order = int.TryParse(f[1], out int order) ? order : 0,
                        Return = f[2],
                        Name = f[3],
                        Description = f[4]
                    });
                }
            }
            return items;
        }

        // Minimal CSV field splitter: comma-delimited, fields optionally quoted with ",
        // "" inside a quoted field is an escaped quote. No multiline fields.
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"' && sb.Length == 0) inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields;
        }
    }
}
