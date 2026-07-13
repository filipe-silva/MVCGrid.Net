using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using MVCGrid.Engine;
using MVCGrid.Interfaces;
using MVCGrid.Models;
using MVCGrid.Web;

namespace MVCGrid.Wasm
{
    /// <summary>
    /// The browser/WASM render flow — the in-process equivalent of the ASP.NET Core
    /// adapter's MvcGridRenderer. It builds the initial @Html.MVCGrid HTML and answers
    /// the (JS-intercepted) AJAX/export requests by running the core GridEngine and
    /// returning the same HTML/CSV the server would. RenderingEngine mode only.
    /// </summary>
    public sealed class WasmGridRenderer
    {
        private readonly WasmGridOptions _opts;

        public WasmGridRenderer(WasmGridOptions opts)
        {
            _opts = opts ?? new WasmGridOptions();
        }

        // ---- @Html.MVCGrid : initial page HTML (with inline preload) ----

        public string GetBasePageHtml(string gridName, string currentUrl, object pageParameters = null)
        {
            var grid = MVCGridDefinitionTable.GetDefinitionInterface(gridName);

            string preload = "";
            if (grid.QueryOnPageLoad && grid.PreloadData)
            {
                try
                {
                    preload = RenderPreloadedGridHtml(gridName, currentUrl, grid, pageParameters);
                }
                catch (Exception ex)
                {
                    preload = _opts.ShowErrorDetails
                        ? "<div class='alert alert-danger'>" +
                          WebUtility.HtmlEncode(ex.ToString()).Replace("\r\n", "<br />") + "</div>"
                        : grid.ErrorMessageHtml;
                }
            }

            string baseGridHtml = MVCGridHtmlGenerator.GenerateBasePageHtml(gridName, grid, pageParameters, _opts.HandlerPath);
            baseGridHtml = baseGridHtml.Replace("%%PRELOAD%%", preload);

            var containerModel = new ContainerRenderingModel { InnerHtmlBlock = baseGridHtml };
            var renderingEngine = GridEngine.GetRenderingEngineInternal(grid);

            string container;
            using (var sw = new StringWriter())
            {
                renderingEngine.RenderContainer(containerModel, sw);
                container = sw.ToString();
            }

            if (!container.Contains(baseGridHtml))
            {
                throw new Exception("When rendering a container, you must output Model.InnerHtmlBlock inside the container (Raw).");
            }

            return container;
        }

        private string RenderPreloadedGridHtml(string gridName, string currentUrl, IMVCGridDefinition grid, object pageParameters)
        {
            var options = QueryStringParser.ParseOptions(grid, new WasmQueryStringReader(currentUrl));
            ApplyPageParameters(grid, options, pageParameters);

            var gridContext = CreateContext(gridName, grid, options);
            var engine = new GridEngine();
            var renderingEngine = GridEngine.GetRenderingEngine(gridContext);
            using (var sw = new StringWriter())
            {
                engine.Run(renderingEngine, gridContext, sw);
                return sw.ToString();
            }
        }

        // ---- AJAX data request (intercepted from MVCGrid.js) ----

        public string RenderData(string url)
        {
            var reader = new WasmQueryStringReader(url);
            string gridName = reader["Name"];
            var grid = MVCGridDefinitionTable.GetDefinitionInterface(gridName);

            var options = QueryStringParser.ParseOptions(grid, reader);
            var gridContext = CreateContext(gridName, grid, options);

            var engine = new GridEngine();
            if (!engine.CheckAuthorization(gridContext))
            {
                return "<div class='alert alert-danger'>Not authorized.</div>";
            }

            var renderingEngine = GridEngine.GetRenderingEngine(gridContext);
            renderingEngine.PrepareResponse(new WasmGridResponse());

            using (var sw = new StringWriter())
            {
                engine.Run(renderingEngine, gridContext, sw);
                return sw.ToString();
            }
        }

        // ---- export request (intercepted; client turns this into a download) ----

        public ExportResult RenderExport(string url)
        {
            var reader = new WasmQueryStringReader(url);
            string gridName = reader["Name"];
            var grid = MVCGridDefinitionTable.GetDefinitionInterface(gridName);

            var options = QueryStringParser.ParseOptions(grid, reader);
            var gridContext = CreateContext(gridName, grid, options);

            var engine = new GridEngine();
            var renderingEngine = GridEngine.GetRenderingEngine(gridContext);
            var response = new WasmGridResponse();
            renderingEngine.PrepareResponse(response);

            string content;
            using (var sw = new StringWriter())
            {
                engine.Run(renderingEngine, gridContext, sw);
                content = sw.ToString();
            }

            return new ExportResult
            {
                ContentType = response.ContentType ?? "text/csv",
                FileName = response.GetFileName() ?? "export.csv",
                Content = content
            };
        }

        // ---- client assets (served from wwwroot; helpers to emit byte-identical copies) ----

        public string GetClientScript()
        {
            return WasmAssets.GetClientScript(_opts.HandlerPath, _opts.ShowErrorDetails);
        }

        public byte[] GetImageBytes(string fileName)
        {
            return WasmAssets.GetImage(fileName);
        }

        // ---- helpers ----

        private GridContext CreateContext(string gridName, IMVCGridDefinition grid, QueryOptions options)
        {
            return new GridContext
            {
                GridName = gridName,
                User = null, // demo grids are AllowAnonymous; no principal needed
                HandlerPath = _opts.HandlerPath,
                GridDefinition = grid,
                QueryOptions = options,
                UrlHelper = new WasmUrlBuilder()
            };
        }

        private static void ApplyPageParameters(IMVCGridDefinition grid, QueryOptions options, object pageParameters)
        {
            var pageParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pageParameters != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(pageParameters))
                {
                    object value = descriptor.GetValue(pageParameters);
                    pageParams[descriptor.Name] = value == null ? "" : value.ToString();
                }
            }
            foreach (var name in grid.PageParameterNames)
            {
                options.PageParameters[name] = pageParams.TryGetValue(name, out string v) ? v : "";
            }
        }
    }
}
