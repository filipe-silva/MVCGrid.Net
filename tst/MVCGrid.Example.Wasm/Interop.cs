using System.Runtime.InteropServices.JavaScript;
using System.Text;
using MVCGrid.Example.Common;
using MVCGrid.Wasm;

namespace MVCGrid.Example.Wasm
{
    /// <summary>
    /// The JS-callable surface of the demo. Each method runs the MVCGrid core engine
    /// in-process (in the browser) and returns exactly what the server would, so the
    /// unchanged MVCGrid.js drives the grid with no server round-trip.
    /// </summary>
    public static partial class Interop
    {
        private static readonly WasmGridRenderer Renderer =
            new WasmGridRenderer(new WasmGridOptions { HandlerPath = "mvcgrid", ShowErrorDetails = true });

        private static bool _initialized;

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }
            SampleGrids.RegisterAll();
            _initialized = true;
        }

        /// <summary>Returns MVCGrid.js with its placeholder tokens substituted.</summary>
        [JSExport]
        internal static string GetClientScript()
        {
            EnsureInitialized();
            return Renderer.GetClientScript();
        }

        /// <summary>Initial grid shell + preloaded first page (the @Html.MVCGrid equivalent).</summary>
        [JSExport]
        internal static string GetBasePageHtml(string gridName, string url)
        {
            EnsureInitialized();
            return Renderer.GetBasePageHtml(gridName, url);
        }

        /// <summary>The HTML fragment for the current query string (answers MVCGrid.js's AJAX GET).</summary>
        [JSExport]
        internal static string RenderData(string url)
        {
            EnsureInitialized();
            return Renderer.RenderData(url);
        }

        /// <summary>Export render as JSON {contentType, fileName, content} for a client-side download.</summary>
        [JSExport]
        internal static string RenderExportJson(string url)
        {
            EnsureInitialized();
            var r = Renderer.RenderExport(url);
            return "{\"contentType\":" + Json(r.ContentType)
                 + ",\"fileName\":" + Json(r.FileName)
                 + ",\"content\":" + Json(r.Content) + "}";
        }

        /// <summary>The demo catalog (nav + toolbar metadata) as JSON, from the shared library.</summary>
        [JSExport]
        internal static string GetCatalogJson()
        {
            return DemoCatalog.ToJson();
        }

        /// <summary>The registration source for a grid, sliced out of the embedded SampleGrids.cs.</summary>
        [JSExport]
        internal static string GetCodeSnippet(string gridName)
        {
            return CodeSnippets.For(gridName);
        }

        private static string Json(string s)
        {
            if (s == null)
            {
                return "null";
            }
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
