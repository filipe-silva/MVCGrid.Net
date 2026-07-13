using System.Collections.Generic;
using System.Text;

namespace MVCGrid.Example.Common
{
    /// <summary>Metadata for one showcase demo (drives the WASM app's nav + toolbar + code panel).</summary>
    public sealed class DemoInfo
    {
        public string Key;          // route key, e.g. "sorting"
        public string Title;        // nav label
        public string Category;     // nav group
        public string GridName;     // MVCGrid definition name to mount
        public string Blurb;        // one-line description shown above the grid
        public bool Search;         // render a global-search box (additional query option "Search"/"search")
        public string SearchOption; // the additional-query-option name the grid reads
        public bool PageSize;       // render a page-size selector
        public bool Export;         // render an "Export CSV" button
        public bool ColumnVisibility; // render column show/hide checkboxes (grid must have toggleable cols)
    }

    /// <summary>The curated list of demos shown in the WebAssembly showcase, grouped by category.</summary>
    public static class DemoCatalog
    {
        public static readonly IReadOnlyList<DemoInfo> All = new List<DemoInfo>
        {
            new DemoInfo { Key = "overview", Title = "Overview", Category = "Overview", GridName = "TestGrid",
                Blurb = "A grid exercising many features at once: sorting, paging, page-size, global search, column visibility, value templates and cell styling.",
                Search = true, SearchOption = "search", PageSize = true, Export = true, ColumnVisibility = true },

            new DemoInfo { Key = "sorting", Title = "Sorting", Category = "Basics", GridName = "SortableGrid",
                Blurb = "Click a column header to sort. Sorting is enabled per-column and at the grid level (with a default sort column)." },
            new DemoInfo { Key = "paging", Title = "Paging", Category = "Basics", GridName = "PagingGrid",
                Blurb = "Server-style paging with a total record count driving the page links." },
            new DemoInfo { Key = "filtering", Title = "Filtering", Category = "Basics", GridName = "Filtering",
                Blurb = "Per-column filters. Type in the filter row to narrow the results (case-insensitive)." },
            new DemoInfo { Key = "pagesize", Title = "Page size", Category = "Basics", GridName = "PageSizeGrid",
                Blurb = "Let the user change how many rows are shown per page.", PageSize = true },

            new DemoInfo { Key = "formatting", Title = "Formatting", Category = "Columns", GridName = "FormattingGrid",
                Blurb = "Format cell values and emit raw HTML (e.g. a link column) with HtmlEncoding turned off." },
            new DemoInfo { Key = "value-templates", Title = "Value templates", Category = "Columns", GridName = "ValueTemplate",
                Blurb = "Build cell HTML from a template referencing {Model.X} and the current {Value}." },
            new DemoInfo { Key = "styling", Title = "Cell & row styling", Category = "Columns", GridName = "StyledGrid",
                Blurb = "Apply CSS classes to rows and cells based on the data." },
            new DemoInfo { Key = "column-visibility", Title = "Column visibility", Category = "Columns", GridName = "ColumnVisibilityGrid",
                Blurb = "Let the user show and hide columns.", ColumnVisibility = true },

            new DemoInfo { Key = "global-search", Title = "Global search", Category = "Data", GridName = "GlobalSearchGrid",
                Blurb = "A single search box mapped to an additional query option that the data method reads.",
                Search = true, SearchOption = "Search" },
            new DemoInfo { Key = "nested-object", Title = "Nested objects", Category = "Data", GridName = "NestedObjectTest",
                Blurb = "Bind to nested properties (e.g. row.Contact.FullName) in value expressions and templates." },
            new DemoInfo { Key = "employees", Title = "Minimal grid", Category = "Data", GridName = "EmployeeGrid",
                Blurb = "The smallest possible grid: three columns and a data method, no paging or sorting." },

            new DemoInfo { Key = "export", Title = "CSV export", Category = "Export", GridName = "ExportGrid",
                Blurb = "The built-in CSV export engine. Click Export to download the current result set.", Export = true },
            new DemoInfo { Key = "custom-style", Title = "Custom render engine", Category = "Export", GridName = "CustomStyle",
                Blurb = "A fully custom IMVCGridRenderingEngine emitting its own table markup." },

            new DemoInfo { Key = "preloading", Title = "Preloading", Category = "Advanced", GridName = "Preloading",
                Blurb = "Defer the first data load until the client requests it (PreloadData=false)." },
            new DemoInfo { Key = "no-qpl", Title = "No query on page load", Category = "Advanced", GridName = "QPLGrid",
                Blurb = "The grid renders empty until an action (sort/page/filter) triggers the first query." },
            new DemoInfo { Key = "loading-message", Title = "Client loading hooks", Category = "Advanced", GridName = "CustomLoading",
                Blurb = "Call your own JS functions while the grid loads and when it completes." },
            new DemoInfo { Key = "additional-options", Title = "Additional query options", Category = "Advanced", GridName = "AQOGrid",
                Blurb = "Pass extra named parameters from the page to the data method." },
            new DemoInfo { Key = "localization", Title = "Localization", Category = "Advanced", GridName = "LocalizationGrid",
                Blurb = "Localize the processing message, paging captions and summary text." },
            new DemoInfo { Key = "error-message", Title = "Custom error message", Category = "Advanced", GridName = "CustomErrorMessage",
                Blurb = "Show custom HTML when the data method throws. (Sort by First Name to trigger it.)" },
        };

        public static string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            bool first = true;
            foreach (var d in All)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('{');
                sb.Append("\"key\":").Append(Str(d.Key)).Append(',');
                sb.Append("\"title\":").Append(Str(d.Title)).Append(',');
                sb.Append("\"category\":").Append(Str(d.Category)).Append(',');
                sb.Append("\"gridName\":").Append(Str(d.GridName)).Append(',');
                sb.Append("\"blurb\":").Append(Str(d.Blurb)).Append(',');
                sb.Append("\"search\":").Append(d.Search ? "true" : "false").Append(',');
                sb.Append("\"searchOption\":").Append(Str(d.SearchOption)).Append(',');
                sb.Append("\"pageSize\":").Append(d.PageSize ? "true" : "false").Append(',');
                sb.Append("\"export\":").Append(d.Export ? "true" : "false").Append(',');
                sb.Append("\"columnVisibility\":").Append(d.ColumnVisibility ? "true" : "false");
                sb.Append('}');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string Str(string s)
        {
            if (s == null) return "null";
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
                    default: sb.Append(c < ' ' ? "\\u" + ((int)c).ToString("x4") : c.ToString()); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
