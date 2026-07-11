using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MVCGrid.Engine;
using MVCGrid.Interfaces;
using MVCGrid.Models;
using MVCGrid.Utility;
using MVCGrid.Web;

namespace MVCGrid.AspNetCore
{
    /// <summary>
    /// The ASP.NET Core host-render flow: builds the initial @Html.MVCGrid HTML and
    /// handles the AJAX/export data requests, delegating model generation to the core
    /// GridEngine. RenderingEngine mode only (Controller mode is deferred — see
    /// MULTI-HOST-PLAN.md).
    /// </summary>
    internal static class MvcGridRenderer
    {
        // ---- @Html.MVCGrid : initial page HTML ----

        internal static string GetBasePageHtml(HttpContext http, IUrlHelper url, string gridName,
            IMVCGridDefinition grid, object pageParameters, MvcGridOptions opts)
        {
            string preload = "";
            if (grid.QueryOnPageLoad && grid.PreloadData)
            {
                try
                {
                    preload = RenderPreloadedGridHtml(http, url, gridName, grid, pageParameters, opts);
                }
                catch (Exception ex)
                {
                    preload = opts.ShowErrorDetails
                        ? "<div class='alert alert-danger'>" +
                          WebUtility.HtmlEncode(ex.ToString()).Replace("\r\n", "<br />") + "</div>"
                        : grid.ErrorMessageHtml;
                }
            }

            string baseGridHtml = MVCGridHtmlGenerator.GenerateBasePageHtml(gridName, grid, pageParameters, opts.HandlerPath);
            baseGridHtml = baseGridHtml.Replace("%%PRELOAD%%", preload);

            // RenderingEngine mode only: let the rendering engine wrap the container.
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

        private static string RenderPreloadedGridHtml(HttpContext http, IUrlHelper url, string gridName,
            IMVCGridDefinition grid, object pageParameters, MvcGridOptions opts)
        {
            var options = QueryStringParser.ParseOptions(grid, new AspNetCoreQueryStringReader(http.Request));

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
                options.PageParameters[name] = pageParams.TryGetValue(name, out var v) ? v : "";
            }

            var gridContext = CreateGridContext(http, url, gridName, grid, options, opts);

            var engine = new GridEngine();
            var renderingEngine = GridEngine.GetRenderingEngine(gridContext);
            using (var sw = new StringWriter())
            {
                engine.Run(renderingEngine, gridContext, sw);
                return sw.ToString();
            }
        }

        // ---- AJAX / export data endpoint ----

        internal static async Task HandleDataRequest(HttpContext http, MvcGridOptions opts)
        {
            string gridName = http.Request.Query["Name"];
            var grid = MVCGridDefinitionTable.GetDefinitionInterface(gridName);

            var options = QueryStringParser.ParseOptions(grid, new AspNetCoreQueryStringReader(http.Request));
            var url = GetUrlHelper(http);
            var gridContext = CreateGridContext(http, url, gridName, grid, options, opts);

            var engine = new GridEngine();
            if (!engine.CheckAuthorization(gridContext))
            {
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            var renderingEngine = GridEngine.GetRenderingEngine(gridContext);

            // Default to HTML (classic behaviour); an export engine overrides via PrepareResponse.
            http.Response.ContentType = "text/html; charset=utf-8";
            renderingEngine.PrepareResponse(new AspNetCoreGridResponse(http.Response));

            string output;
            using (var sw = new StringWriter())
            {
                engine.Run(renderingEngine, gridContext, sw);
                output = sw.ToString();
            }

            await http.Response.WriteAsync(output);
        }

        // ---- client script (placeholder substitution) ----

        internal static string GetClientScript(MvcGridOptions opts, string basePath)
        {
            string js = EmbeddedResources.GetText("MVCGrid.js");
            js = js.Replace("%%HANDLERPATH%%", basePath);
            // Controller rendering mode is deferred; the script only uses controllerPath for it.
            js = js.Replace("%%CONTROLLERPATH%%", basePath);
            js = js.Replace("%%ERRORDETAILS%%", opts.ShowErrorDetails ? "true" : "false");
            return js;
        }

        internal static byte[] GetImage(string fileName)
        {
            return EmbeddedResources.GetBinary(fileName);
        }

        // ---- helpers ----

        internal static GridContext CreateGridContext(HttpContext http, IUrlHelper url, string gridName,
            IMVCGridDefinition grid, QueryOptions options, MvcGridOptions opts)
        {
            return new GridContext
            {
                GridName = gridName,
                User = http.User,
                HandlerPath = opts.HandlerPath,
                GridDefinition = grid,
                QueryOptions = options,
                UrlHelper = new AspNetCoreUrlBuilder(url)
            };
        }

        private static IUrlHelper GetUrlHelper(HttpContext http)
        {
            var factory = http.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var actionContext = new ActionContext(http, http.GetRouteData(), new ActionDescriptor());
            return factory.GetUrlHelper(actionContext);
        }
    }
}
