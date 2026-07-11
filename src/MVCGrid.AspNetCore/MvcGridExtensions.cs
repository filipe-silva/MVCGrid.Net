using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MVCGrid.Web;

namespace MVCGrid.AspNetCore
{
    /// <summary>
    /// Registration + endpoint wiring for the ASP.NET Core MVCGrid adapter.
    /// </summary>
    public static class MvcGridServiceCollectionExtensions
    {
        /// <summary>
        /// Registers MVCGrid services. Grid definitions themselves are still added to the
        /// static <c>MVCGridDefinitionTable</c> (call your registration code at startup).
        /// </summary>
        public static IServiceCollection AddMVCGrid(this IServiceCollection services, Action<MvcGridOptions> configure = null)
        {
            var options = new MvcGridOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);
            return services;
        }
    }

    public static class MvcGridEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps the MVCGrid endpoints: the AJAX/export data endpoint at <c>{HandlerPath}</c>,
        /// the client script at <c>{HandlerPath}/script.js</c>, and the icon images.
        /// </summary>
        public static IEndpointRouteBuilder MapMVCGrid(this IEndpointRouteBuilder endpoints)
        {
            var opts = endpoints.ServiceProvider.GetService<MvcGridOptions>() ?? new MvcGridOptions();
            string basePath = "/" + (opts.HandlerPath ?? "/mvcgrid").Trim('/');

            // Data (RenderingEngine mode) + export.
            endpoints.MapMethods(basePath, new[] { "GET", "POST" },
                (HttpContext http) => MvcGridRenderer.HandleDataRequest(http, opts));

            // Client script.
            endpoints.MapGet(basePath + "/script.js", async (HttpContext http) =>
            {
                http.Response.ContentType = "application/javascript";
                await http.Response.WriteAsync(MvcGridRenderer.GetClientScript(opts, basePath));
            });

            // Icon/loader images (embedded in the core assembly).
            var images = new[] { "ajaxloader.gif", "sort.png", "sortdown.png", "sortup.png" };
            foreach (var image in images)
            {
                string file = image;
                string contentType = file.EndsWith(".gif") ? "image/gif" : "image/png";
                endpoints.MapGet(basePath + "/" + file, async (HttpContext http) =>
                {
                    http.Response.ContentType = contentType;
                    byte[] bytes = MvcGridRenderer.GetImage(file);
                    await http.Response.Body.WriteAsync(bytes.AsMemory(0, bytes.Length));
                });
            }

            return endpoints;
        }
    }

    /// <summary>The <c>@Html.MVCGrid("gridName")</c> helper.</summary>
    public static class MvcGridHtmlHelperExtensions
    {
        public static IHtmlContent MVCGrid(this IHtmlHelper helper, string name)
        {
            return helper.MVCGrid(name, null);
        }

        public static IHtmlContent MVCGrid(this IHtmlHelper helper, string name, object pageParameters)
        {
            var http = helper.ViewContext.HttpContext;
            var opts = http.RequestServices.GetService<MvcGridOptions>() ?? new MvcGridOptions();
            var grid = MVCGridDefinitionTable.GetDefinitionInterface(name);

            var factory = http.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var url = factory.GetUrlHelper(helper.ViewContext);

            string html = MvcGridRenderer.GetBasePageHtml(http, url, name, grid, pageParameters, opts);
            return new HtmlString(html);
        }
    }
}
