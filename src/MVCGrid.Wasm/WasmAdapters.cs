using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using MVCGrid.Abstractions;

namespace MVCGrid.Wasm
{
    /// <summary>
    /// Implements the core's IMvcGridUrlBuilder for a browser SPA. There is no MVC
    /// routing, so Action(...) maps to a client hash route + query string,
    /// e.g. Action("detail","demo", new { id = 5 }) => "#/detail?id=5". The single-page
    /// app's hash router handles it, so links never trigger a WASM runtime reboot.
    /// </summary>
    public sealed class WasmUrlBuilder : IMvcGridUrlBuilder
    {
        public string Action(string actionName)
        {
            return Build(actionName, null);
        }

        public string Action(string actionName, object routeValues)
        {
            return Build(actionName, routeValues);
        }

        public string Action(string actionName, string controllerName)
        {
            return Build(actionName, null);
        }

        public string Action(string actionName, string controllerName, object routeValues)
        {
            return Build(actionName, routeValues);
        }

        private static string Build(string actionName, object routeValues)
        {
            string page = "#/" + (actionName ?? "").ToLowerInvariant();
            if (routeValues == null)
            {
                return page;
            }

            var sb = new StringBuilder();
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(routeValues))
            {
                object value = prop.GetValue(routeValues);
                if (value == null)
                {
                    continue;
                }
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.Append(WebUtility.UrlEncode(prop.Name));
                sb.Append("=");
                sb.Append(WebUtility.UrlEncode(value.ToString()));
            }
            return page + sb.ToString();
        }
    }

    /// <summary>
    /// Implements the core's IGridResponse with in-memory state (there is no HTTP
    /// response in the browser). Captures the content type and headers so the export
    /// path can recover the file name from the Content-Disposition header.
    /// </summary>
    public sealed class WasmGridResponse : IGridResponse
    {
        private readonly Dictionary<string, string> _headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string ContentType { get; set; }

        public void AddHeader(string name, string value)
        {
            _headers[name] = value;
        }

        /// <summary>Parses the file name out of a captured Content-Disposition header, if any.</summary>
        public string GetFileName()
        {
            if (_headers.TryGetValue("content-disposition", out string cd) && !string.IsNullOrEmpty(cd))
            {
                const string marker = "filename=";
                int i = cd.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (i >= 0)
                {
                    return cd.Substring(i + marker.Length).Trim().Trim('"');
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Implements the core's IQueryStringReader by parsing a query string out of a URL
    /// (or a bare query string) supplied by the browser. Case-insensitive and returns
    /// null for absent keys, matching the classic HttpRequest.QueryString semantics.
    /// </summary>
    public sealed class WasmQueryStringReader : IQueryStringReader
    {
        private readonly Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public WasmQueryStringReader(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            int q = url.IndexOf('?');
            string qs = q >= 0 ? url.Substring(q + 1) : url;

            int hash = qs.IndexOf('#');
            if (hash >= 0)
            {
                qs = qs.Substring(0, hash);
            }

            foreach (string pair in qs.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                string key = eq >= 0 ? pair.Substring(0, eq) : pair;
                string val = eq >= 0 ? pair.Substring(eq + 1) : "";
                key = WebUtility.UrlDecode(key);
                val = WebUtility.UrlDecode(val);
                if (!_values.ContainsKey(key))
                {
                    _values[key] = val;
                }
            }
        }

        public string this[string key]
        {
            get { return _values.TryGetValue(key, out string v) ? v : null; }
        }
    }

    /// <summary>Result of a client-side export render: what the browser needs to trigger a download.</summary>
    public sealed class ExportResult
    {
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
    }
}
