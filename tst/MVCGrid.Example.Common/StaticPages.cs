using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;

namespace MVCGrid.Example.Common
{
    /// <summary>
    /// Shared HTML bodies for the site pages (Home, Getting Started, Download, Support,
    /// Documentation) so the three example hosts render identical content: the classic and
    /// Core hosts emit them from Razor views via Html.Raw, the WASM showcase via Interop.
    ///
    /// The markup lives in Pages/*.html, embedded into this assembly and cached on first
    /// use. Dynamic parts are {{TOKEN}} placeholders substituted with string.Replace —
    /// deliberately NOT string.Format, whose {0} syntax would require doubling every
    /// literal '{'/'}' in the embedded C# snippets and CSS.
    /// Pages assume the MvcWeb-look layout (Bootstrap 3 + SyntaxHighlighter + .nuget-badge).
    /// </summary>
    public static class StaticPages
    {
        private static readonly ConcurrentDictionary<string, string> _templates = new();

        /// <param name="hostNoteHtml">Optional host-specific paragraph (raw HTML) shown under the intro.</param>
        /// <param name="renderGrid">Renders a registered grid by name into HTML (see <see cref="DocumentationHtml"/>).</param>
        public static string HomeHtml(string hostNoteHtml, Func<string, string> renderGrid)
        {
            return Load("Home")
                .Replace("{{HOST_NOTE}}", hostNoteHtml ?? "")
                .Replace("{{TOOLBAR}}", DemoToolbar.BuildHtml("TestGrid", true, true, true, true))
                .Replace("{{GRID}}", renderGrid("TestGrid"))
                .Replace("{{CONFIG_SNIPPET}}", WebUtility.HtmlEncode(CodeSnippets.For("TestGrid")));
        }

        /// <param name="demosHref">Host-specific link to the demos page ("/demo" or "#/demos").</param>
        public static string GettingStartedHtml(string demosHref)
        {
            return Load("GettingStarted")
                .Replace("{{USAGE_SNIPPET}}", WebUtility.HtmlEncode(CodeSnippets.For("UsageExample")))
                .Replace("{{DEMOS_HREF}}", WebUtility.HtmlEncode(demosHref ?? "/demo"));
        }

        /// <param name="renderGrid">Renders a registered grid by name into HTML — the classic host
        /// passes Html.MVCGrid(...).ToHtmlString(), the Core host Html.MVCGrid(...).ToString(),
        /// the WASM host the WasmGridRenderer's base page HTML.</param>
        public static string DocumentationHtml(Func<string, string> renderGrid)
        {
            return Load("Documentation")
                .Replace("{{GRID_DEFINITION}}", renderGrid("GridDefinition"))
                .Replace("{{GRID_COLUMN}}", renderGrid("GridColumn"))
                .Replace("{{GRID_QUERYOPTIONS}}", renderGrid("QueryOptions"))
                .Replace("{{GRID_CLIENTSIDE}}", renderGrid("ClientSide"));
        }

        /// <summary>The generic catalog demo page: sidebar + grid + toolbar + code panels.
        /// Used by the Core and WASM hosts; the classic host keeps its hand-written demo
        /// views because they carry host-specific interactive content.</summary>
        /// <param name="demo">The catalog entry to show.</param>
        /// <param name="demoHrefPrefix">Host-specific demo link prefix ("/demo/" or "#/demo/").</param>
        /// <param name="renderGrid">Renders a registered grid by name into HTML (see <see cref="DocumentationHtml"/>).</param>
        public static string DemoHtml(DemoInfo demo, string demoHrefPrefix, Func<string, string> renderGrid)
        {
            var sidebar = new System.Text.StringBuilder();
            foreach (var d in DemoCatalog.All)
            {
                sidebar.Append("            <li><a href=\"")
                    .Append(WebUtility.HtmlEncode(demoHrefPrefix + d.Key))
                    .Append("\">")
                    .Append(WebUtility.HtmlEncode(d.Title))
                    .Append("</a></li>\n");
            }

            bool hasToolbar = demo.PageSize || demo.ColumnVisibility || demo.Export || demo.Search;
            string toolbar = hasToolbar
                ? DemoToolbar.BuildHtml(demo.GridName, demo.PageSize, demo.ColumnVisibility, demo.Export, demo.Search, demo.SearchOption ?? "search")
                : "";

            return Load("Demo")
                .Replace("{{SIDEBAR}}", sidebar.ToString())
                .Replace("{{TITLE}}", WebUtility.HtmlEncode(demo.Title))
                .Replace("{{BLURB}}", WebUtility.HtmlEncode(demo.Blurb))
                .Replace("{{TOOLBAR}}", toolbar)
                .Replace("{{GRID}}", renderGrid(demo.GridName))
                .Replace("{{CONFIG_SNIPPET}}", WebUtility.HtmlEncode(CodeSnippets.For(demo.GridName)))
                .Replace("{{GRID_NAME}}", WebUtility.HtmlEncode(demo.GridName));
        }

        /// <param name="id">The person id from the grid's URL column (HTML-encoded here).</param>
        public static string DetailHtml(string id)
        {
            return Load("Detail")
                .Replace("{{PERSON_ID}}", WebUtility.HtmlEncode(id ?? ""));
        }

        public static string DownloadHtml()
        {
            return Load("Download");
        }

        public static string SupportHtml()
        {
            return Load("Support");
        }

        private static string Load(string page)
        {
            return _templates.GetOrAdd(page, p =>
            {
                var asm = typeof(StaticPages).Assembly;
                using (var stream = asm.GetManifestResourceStream("MVCGrid.Example.Common.Pages." + p + ".html"))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException("Embedded page not found: Pages/" + p + ".html");
                    }
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            });
        }
    }
}
