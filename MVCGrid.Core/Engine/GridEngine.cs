using MVCGrid.Interfaces;
using MVCGrid.Models;
using MVCGrid.Rendering;
using MVCGrid.Utility;
using MVCGrid.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MVCGrid.Engine
{
    public class GridEngine
    {
        public static IMVCGridRenderingEngine GetRenderingEngine(GridContext gridContext)
        {
            IMVCGridRenderingEngine renderingEngine = null;

            if (!String.IsNullOrWhiteSpace(gridContext.QueryOptions.RenderingEngineName))
            {
                foreach (RenderingEngineSetting configuredEngine in gridContext.GridDefinition.RenderingEngines)
                {
                    if (String.Compare(gridContext.QueryOptions.RenderingEngineName, configuredEngine.Name, true) == 0)
                    {
                        string engineName = gridContext.QueryOptions.RenderingEngineName;

                        string typeString = gridContext.GridDefinition.RenderingEngines[engineName].Type;
                        Type engineType = Type.GetType(typeString, true);

                        renderingEngine = (IMVCGridRenderingEngine)Activator.CreateInstance(engineType, true);
                    }
                }
            }

            if (renderingEngine == null)
            {
                renderingEngine = GetRenderingEngineInternal(gridContext.GridDefinition);
            }

            return renderingEngine;
        }

        internal static IMVCGridRenderingEngine GetRenderingEngineInternal(IMVCGridDefinition gridDefinition)
        {
            string engineName = gridDefinition.DefaultRenderingEngineName;

            if (gridDefinition.RenderingEngines[engineName] == null)
            {
                throw new InvalidOperationException(String.Format("The requested default rendering engine '{0}' was not found.", engineName));
            }

            string typeString = gridDefinition.RenderingEngines[engineName].Type;
            Type engineType = Type.GetType(typeString, true);

            IMVCGridRenderingEngine renderingEngine = (IMVCGridRenderingEngine)Activator.CreateInstance(engineType, true);

            return renderingEngine;
        }

        public void Run(IMVCGridRenderingEngine renderingEngine, GridContext gridContext, TextWriter outputStream)
        {
            if (!renderingEngine.AllowsPaging)
            {
                gridContext.QueryOptions.ItemsPerPage = null;
            }

            var model = GenerateModel(gridContext);

            renderingEngine.Render(model, gridContext, outputStream);
        }

        public RenderingModel GenerateModel(GridContext gridContext)
        {
            int? totalRecords;
            var rows = ((GridDefinitionBase)gridContext.GridDefinition).GetData(gridContext, out totalRecords);

            // if a page was requested higher than available pages, requery for first page
            if (rows.Count == 0 && totalRecords.HasValue && totalRecords.Value > 0)
            {
                gridContext.QueryOptions.PageIndex = 0;
                rows = ((GridDefinitionBase)gridContext.GridDefinition).GetData(gridContext, out totalRecords);
            }

            var model = PrepModel(totalRecords, rows, gridContext);
            return model;
        }

        private RenderingModel PrepModel(int? totalRecords, List<Row> rows, Models.GridContext gridContext)
        {
            RenderingModel model = new RenderingModel();

            model.HandlerPath = gridContext.HandlerPath;
            model.TableHtmlId = HtmlUtility.GetTableHtmlId(gridContext.GridName);

            PrepColumns(gridContext, model);
            model.Rows = rows;

            if (model.Rows.Count == 0)
            {
                model.NoResultsMessage = gridContext.GridDefinition.NoResultsMessage;
            }

            model.NextButtonCaption = gridContext.GridDefinition.NextButtonCaption;
            model.PreviousButtonCaption = gridContext.GridDefinition.PreviousButtonCaption;
            model.SummaryMessage = gridContext.GridDefinition.SummaryMessage;
            model.ProcessingMessage = gridContext.GridDefinition.ProcessingMessage;

            model.PagingModel = null;
            if (gridContext.QueryOptions.ItemsPerPage.HasValue)
            {
                model.PagingModel = new PagingModel();

                int currentPageIndex = gridContext.QueryOptions.PageIndex.Value;

                model.PagingModel.TotalRecords = totalRecords.Value;

                model.PagingModel.FirstRecord = (currentPageIndex * gridContext.QueryOptions.ItemsPerPage.Value) + 1;
                if(model.PagingModel.FirstRecord > model.PagingModel.TotalRecords)
                {
                    model.PagingModel.FirstRecord = model.PagingModel.TotalRecords;
                }
                model.PagingModel.LastRecord = (model.PagingModel.FirstRecord + gridContext.QueryOptions.ItemsPerPage.Value) - 1;
                if (model.PagingModel.LastRecord > model.PagingModel.TotalRecords)
                {
                    model.PagingModel.LastRecord = model.PagingModel.TotalRecords;
                }
                model.PagingModel.CurrentPage = currentPageIndex + 1;

                var numberOfPagesD = (model.PagingModel.TotalRecords + 0.0) / (gridContext.QueryOptions.ItemsPerPage.Value + 0.0);
                model.PagingModel.NumberOfPages = (int)Math.Ceiling(numberOfPagesD);

                for (int i = 1; i <= model.PagingModel.NumberOfPages; i++)
                {
                    model.PagingModel.PageLinks.Add(i, HtmlUtility.MakeGotoPageLink(gridContext.GridName, i));
                }
            }

            model.ClientDataTransferHtmlBlock = MVCGrid.Web.MVCGridHtmlGenerator.GenerateClientDataTransferHtml(gridContext);

            return model;
        }

        private void PrepColumns(Models.GridContext gridContext, RenderingModel model)
        {
            foreach (var col in gridContext.GetVisibleColumns())
            {
                Column renderingColumn = new Column();
                model.Columns.Add(renderingColumn);
                renderingColumn.Name = col.ColumnName;
                renderingColumn.HeaderText = col.HeaderText;

                if (gridContext.GridDefinition.Sorting && col.EnableSorting)
                {
                    SortDirection linkDirection = SortDirection.Asc;
                    SortDirection iconDirection = SortDirection.Unspecified;

                    if (gridContext.QueryOptions.SortColumnName == col.ColumnName && gridContext.QueryOptions.SortDirection == SortDirection.Asc)
                    {
                        iconDirection = SortDirection.Asc;
                        linkDirection = SortDirection.Dsc;
                    }
                    else if (gridContext.QueryOptions.SortColumnName == col.ColumnName && gridContext.QueryOptions.SortDirection == SortDirection.Dsc)
                    {
                        iconDirection = SortDirection.Dsc;
                        linkDirection = SortDirection.Asc;
                    }
                    else
                    {
                        iconDirection = SortDirection.Unspecified;
                        linkDirection = SortDirection.Asc;
                    }

                    renderingColumn.Onclick = HtmlUtility.MakeSortLink(gridContext.GridName, col.ColumnName, linkDirection);
                    renderingColumn.SortIconDirection = iconDirection;
                }
            }
        }

        public bool CheckAuthorization(GridContext gridContext)
        {
            bool allowAccess = false;

            switch (gridContext.GridDefinition.AuthorizationType)
            {
                case AuthorizationType.AllowAnonymous:
                    allowAccess = true;
                    break;
                case AuthorizationType.Authorized:
                    allowAccess = gridContext.User != null
                        && gridContext.User.Identity != null
                        && gridContext.User.Identity.IsAuthenticated;
                    break;
                default:
                    throw new Exception("Unsupported AuthorizationType");
            }

            return allowAccess;
        }
    }
}
