using MVCGrid.Engine;
using MVCGrid.Interfaces;
using MVCGrid.Models;
using MVCGrid.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MVCGrid.Web
{
    /// <summary>
    /// The System.Web/MVC-dependent rendering flow that used to live in GridEngine.
    /// Produces the initial page HTML (@Html.MVCGrid) and, for preloaded grids, the
    /// first page of data — delegating the portable model generation to the core
    /// GridEngine. Kept in the adapter because it needs HtmlHelper, ViewEngines,
    /// ViewContext, and HttpContext.
    /// </summary>
    internal static class MvcGridHtmlRenderer
    {
        private static readonly Encoding LocalEncoding = Encoding.UTF8;

        /// <summary>
        /// Base path to the MVCGrid resource handler (script/images/ajax endpoint).
        /// Moved here from the (now portable) HtmlUtility because it needs HttpContext.
        /// </summary>
        public static string GetHandlerPath()
        {
            string appPath = HttpContext.Current.Request.ApplicationPath.TrimEnd('/');
            return appPath + "/MVCGridHandler.axd";
        }

        public static string GetBasePageHtml(HtmlHelper helper, string gridName, IMVCGridDefinition grid, object pageParameters)
        {
            string preload = "";
            if (grid.QueryOnPageLoad && grid.PreloadData)
            {
                try
                {
                    preload = RenderPreloadedGridHtml(helper, grid, gridName, pageParameters);
                }
                catch (Exception ex)
                {
                    bool showDetails = ConfigUtility.GetShowErrorDetailsSetting();

                    if (showDetails)
                    {
                        string detail = "<div class='alert alert-danger'>";
                        detail += HttpUtility.HtmlEncode(ex.ToString()).Replace("\r\n", "<br />");
                        detail += "</div>";

                        preload = detail;
                    }
                    else
                    {
                        preload = grid.ErrorMessageHtml;
                    }
                }
            }

            string baseGridHtml = MVCGridHtmlGenerator.GenerateBasePageHtml(gridName, grid, pageParameters, GetHandlerPath());
            baseGridHtml = baseGridHtml.Replace("%%PRELOAD%%", preload);

            ContainerRenderingModel containerRenderingModel = new ContainerRenderingModel() { InnerHtmlBlock = baseGridHtml };

            string html = RenderContainerHtml(helper, grid, gridName, containerRenderingModel);

            return html;
        }

        private static string RenderContainerHtml(HtmlHelper helper, IMVCGridDefinition grid, string gridName, ContainerRenderingModel containerRenderingModel)
        {
            string container = containerRenderingModel.InnerHtmlBlock;
            switch (grid.RenderingMode)
            {
                case Models.RenderingMode.RenderingEngine:
                    container = RenderContainerUsingRenderingEngine(grid, containerRenderingModel);
                    break;
                case Models.RenderingMode.Controller:
                    if (!String.IsNullOrWhiteSpace(grid.ContainerViewPath))
                    {
                        container = RenderContainerUsingController(grid, helper, containerRenderingModel);
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }

            if (!container.Contains(containerRenderingModel.InnerHtmlBlock))
            {
                throw new Exception("When rendering a container, you must output Model.InnerHtmlBlock inside the container (Raw).");
            }

            return container;
        }

        private static string RenderPreloadedGridHtml(HtmlHelper helper, IMVCGridDefinition grid, string gridName, object pageParameters)
        {
            string preload = "";

            var options = QueryStringParser.ParseOptions(grid, new MvcQueryStringReader(HttpContext.Current.Request));

            // set the page parameters for the preloaded grid
            Dictionary<string, string> pageParamsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pageParameters != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(pageParameters))
                {
                    object obj2 = descriptor.GetValue(pageParameters);
                    pageParamsDict.Add(descriptor.Name, obj2.ToString());
                }
            }
            if (grid.PageParameterNames.Count > 0)
            {
                foreach (var aqon in grid.PageParameterNames)
                {
                    string val = "";

                    if (pageParamsDict.ContainsKey(aqon))
                    {
                        val = pageParamsDict[aqon];
                    }

                    options.PageParameters[aqon] = val;
                }
            }

            var gridContext = GridContextUtility.Create(HttpContext.Current, gridName, grid, options);

            GridEngine engine = new GridEngine();

            switch (grid.RenderingMode)
            {
                case Models.RenderingMode.RenderingEngine:
                    preload = RenderUsingRenderingEngine(engine, gridContext);
                    break;
                case Models.RenderingMode.Controller:
                    preload = RenderUsingController(engine, gridContext, helper);
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return preload;
        }

        private static string RenderUsingController(GridEngine engine, Models.GridContext gridContext, HtmlHelper helper)
        {
            var model = engine.GenerateModel(gridContext);

            var controllerContext = helper.ViewContext.Controller.ControllerContext;
            ViewDataDictionary vdd = new ViewDataDictionary(model);
            TempDataDictionary tdd = new TempDataDictionary();
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controllerContext,
                                                                         gridContext.GridDefinition.ViewPath);
                var viewContext = new ViewContext(controllerContext, viewResult.View, vdd, tdd, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(controllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        private static string RenderUsingRenderingEngine(GridEngine engine, Models.GridContext gridContext)
        {
            IMVCGridRenderingEngine renderingEngine = GridEngine.GetRenderingEngine(gridContext);

            using (MemoryStream ms = new MemoryStream())
            {
                using (TextWriter tw = new StreamWriter(ms))
                {
                    engine.Run(renderingEngine, gridContext, tw);
                }

                string result = LocalEncoding.GetString(ms.ToArray());
                return result;
            }
        }

        private static string RenderContainerUsingController(IMVCGridDefinition gridDefinition, HtmlHelper helper, ContainerRenderingModel model)
        {
            var controllerContext = helper.ViewContext.Controller.ControllerContext;
            ViewDataDictionary vdd = new ViewDataDictionary(model);
            TempDataDictionary tdd = new TempDataDictionary();
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controllerContext,
                                                                         gridDefinition.ContainerViewPath);
                var viewContext = new ViewContext(controllerContext, viewResult.View, vdd, tdd, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(controllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        private static string RenderContainerUsingRenderingEngine(IMVCGridDefinition gridDefinition, ContainerRenderingModel model)
        {
            IMVCGridRenderingEngine renderingEngine = GridEngine.GetRenderingEngineInternal(gridDefinition);

            using (MemoryStream ms = new MemoryStream())
            {
                using (TextWriter tw = new StreamWriter(ms))
                {
                    renderingEngine.RenderContainer(model, tw);
                }

                return LocalEncoding.GetString(ms.ToArray());
            }
        }
    }
}
