using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MVCGrid.Utility
{
    /// <summary>
    /// Access to the client assets (MVCGrid.js + sort/loader icons) embedded in this
    /// core assembly, so every host adapter serves byte-identical resources. Lookups
    /// match by filename substring against the manifest resource names
    /// (e.g. "MVCGrid.js" matches "MVCGrid.Scripts.MVCGrid.js").
    ///
    /// Internal — reached by the host adapters via InternalsVisibleTo.
    /// </summary>
    internal static class EmbeddedResources
    {
        private static readonly Assembly Asm = typeof(EmbeddedResources).Assembly;

        public static string GetText(string fileName)
        {
            using (var reader = new StreamReader(OpenStream(fileName)))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] GetBinary(string fileName)
        {
            using (var stream = OpenStream(fileName))
            {
                var buffer = new byte[stream.Length];
                int offset = 0;
                int read;
                while (offset < buffer.Length &&
                       (read = stream.Read(buffer, offset, buffer.Length - offset)) > 0)
                {
                    offset += read;
                }
                return buffer;
            }
        }

        private static Stream OpenStream(string fileName)
        {
            string resourceName = Asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.Contains(fileName));

            if (resourceName == null)
            {
                throw new InvalidOperationException("Embedded MVCGrid resource not found: " + fileName);
            }

            return Asm.GetManifestResourceStream(resourceName);
        }
    }
}
