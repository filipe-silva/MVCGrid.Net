using System;
using System.IO;
using System.Reflection;

namespace MVCGrid.Example.Wasm
{
    /// <summary>
    /// Slices a single grid's registration block out of the embedded SampleGrids.cs source,
    /// so the showcase can show "here's the code that defines this grid".
    /// </summary>
    internal static class CodeSnippets
    {
        private static readonly string Source = Load();

        public static string For(string gridName)
        {
            if (string.IsNullOrEmpty(Source)) return "";

            string marker = "MVCGridDefinitionTable.Add(\"" + gridName + "\"";
            int start = Source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";

            int i = Source.IndexOf('(', start);
            int depth = 0;
            for (; i < Source.Length; i++)
            {
                if (Source[i] == '(') depth++;
                else if (Source[i] == ')') { depth--; if (depth == 0) { i++; break; } }
            }
            while (i < Source.Length && Source[i] != ';') i++;
            if (i < Source.Length) i++; // include the ';'

            return Dedent(Source.Substring(start, i - start));
        }

        // Remove the common leading indentation so the snippet reads cleanly in a <pre>.
        private static string Dedent(string s)
        {
            var lines = s.Replace("\r\n", "\n").Split('\n');
            for (int n = 1; n < lines.Length; n++)
            {
                int strip = 0;
                while (strip < 16 && strip < lines[n].Length && lines[n][strip] == ' ') strip++;
                lines[n] = lines[n].Substring(strip);
            }
            return string.Join("\n", lines);
        }

        private static string Load()
        {
            var asm = typeof(CodeSnippets).Assembly;
            string name = null;
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (n.EndsWith("SampleGrids.cs.txt", StringComparison.Ordinal)) { name = n; break; }
            }
            if (name == null) return "";
            using (var stream = asm.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
