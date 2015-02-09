﻿using MVCGrid.Interfaces;
using MVCGrid.Models;
using MVCGrid.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MVCGrid.Rendering
{
    public class HtmlRenderingEngine : IMVCGridRenderingEngine
    {
        private readonly string CssTable;
        private readonly string HtmlImageSortAsc;
        private readonly string HtmlImageSortDsc;
        private readonly string HtmlImageSort;

        public HtmlRenderingEngine()
        {
            CssTable = "table table-striped table-bordered";

            HtmlImageSortAsc = "<img src='/content/icon_up_sort_arrow.png' class='pull-right' />";
            HtmlImageSortDsc = "<img src='/content/icon_down_sort_arrow.png' class='pull-right' />";
            HtmlImageSort = "<img src='/content/icon_sort_arrow.png' class='pull-right' />";
        }

        public bool AllowsPaging
        {
            get { return true; }
        }

        public void Render(GridData data, GridContext gridContext, HttpResponse httpResponse)
        {
            StringBuilder sbHtml = new StringBuilder();

            sbHtml.AppendFormat("<table id='{0}'", HtmlUtility.GetTableHtmlId(gridContext.GridName));
            sbHtml.Append(HtmlUtility.MakeCssClassAttributeStirng(CssTable));
            sbHtml.Append(">");


            RenderHeader(gridContext, sbHtml);
            RenderBody(data, gridContext, sbHtml);
            sbHtml.AppendLine("</table>");

            MakePaging(data, gridContext, sbHtml);

            httpResponse.Write(sbHtml.ToString());
        }

        private void RenderBody(GridData data, GridContext gridContext, StringBuilder sbHtml)
        {
            sbHtml.AppendLine("<tbody>");

            foreach (var item in data.Rows)
            {
                sbHtml.AppendLine("  <tr>");
                foreach (var col in gridContext.GridDefinition.GetColumns())
                {
                    string val = "";

                    if (item.Values.ContainsKey(col.ColumnName))
                    {
                        val = item.Values[col.ColumnName];
                    }

                    if (col.HtmlEncode)
                    {
                        sbHtml.AppendLine(String.Format("    <td>{0}</td>", HttpUtility.HtmlEncode(val)));
                    }
                    else
                    {
                        sbHtml.AppendLine(String.Format("    <td>{0}</td>", val));
                    }
                }
                sbHtml.AppendLine("  </tr>");
            }

            sbHtml.AppendLine("</tbody>");
        }

        private void RenderHeader(GridContext gridContext, StringBuilder sbHtml)
        {
            sbHtml.AppendLine("<thead>");
            sbHtml.AppendLine("  <tr>");
            foreach (var col in gridContext.GridDefinition.GetColumns())
            {
                sbHtml.Append("<th");

                if (col.EnableSorting)
                {
                    SortDirection direction = SortDirection.Asc;
                    if (gridContext.QueryOptions.SortColumn == col.ColumnName && gridContext.QueryOptions.SortDirection == SortDirection.Asc)
                    {
                        direction = SortDirection.Dsc;
                    }

                    sbHtml.Append(" style='cursor: pointer;'");
                    sbHtml.AppendFormat(" onclick='{0}'", HtmlUtility.MakeSortLink(gridContext.GridName, col.ColumnName, direction));
                }
                sbHtml.Append(">");

                sbHtml.Append(HttpUtility.HtmlEncode(col.HeaderText));

                if (gridContext.QueryOptions.SortColumn == col.ColumnName && gridContext.QueryOptions.SortDirection == SortDirection.Asc)
                {
                    sbHtml.Append(" ");
                    sbHtml.Append(HtmlImageSortAsc);
                }
                else if (gridContext.QueryOptions.SortColumn == col.ColumnName && gridContext.QueryOptions.SortDirection == SortDirection.Dsc)
                {
                    sbHtml.Append(" ");
                    sbHtml.Append(HtmlImageSortDsc);
                }
                else
                {
                    if (col.EnableSorting)
                    {
                        sbHtml.Append(" ");
                        sbHtml.Append(HtmlImageSort);
                    }
                }


                sbHtml.Append("</th>");
            }
            sbHtml.AppendLine("  </tr>");
            sbHtml.AppendLine("</thead>");
        }

        private static void MakePaging(GridData data, GridContext gridContext, StringBuilder sbHtml)
        {
            if (!gridContext.QueryOptions.ItemsPerPage.HasValue)
            {
                return;
            }

            var numberOfPagesD = (data.TotalRecords + 0.0) / (gridContext.QueryOptions.ItemsPerPage.Value + 0.0);
            int numberOfPages = (int)Math.Ceiling(numberOfPagesD);
            int currentPageIndex = gridContext.QueryOptions.PageIndex.Value;

            int firstRecord = (currentPageIndex * gridContext.QueryOptions.ItemsPerPage.Value) + 1;
            int lastRecord = (firstRecord + gridContext.QueryOptions.ItemsPerPage.Value) - 1;
            if (lastRecord > data.TotalRecords)
            {
                lastRecord = data.TotalRecords;
            }

            string recordText = String.Format("Showing {0} to {1} of {2} entries",
                firstRecord, lastRecord, data.TotalRecords
                );

            sbHtml.Append("<div class=\"row\">");
            sbHtml.Append("<div class=\"col-xs-6\">");
            sbHtml.AppendLine(recordText);
            sbHtml.Append("</div>");


            sbHtml.Append("<div class=\"col-xs-6\">");
            int currentPage = currentPageIndex + 1;
            int pageToStart = currentPage - 2;
            while (pageToStart < 1)
            {
                pageToStart++;
            }
            int pageToEnd = pageToStart + 4;
            while (pageToEnd > numberOfPages)
            {
                pageToStart--;
                pageToEnd = pageToStart + 4;
            }
            while (pageToStart < 1)
            {
                pageToStart++;
            }

            sbHtml.Append("<ul class='pagination pull-right' style='margin-top: 0;'>");

            sbHtml.Append("<li");
            if (pageToStart == currentPage)
            {
                sbHtml.Append(" class='disabled'");
            }
            sbHtml.Append(">");

            sbHtml.Append("<a href='#' aria-label='Previous' ");
            if (pageToStart < currentPage)
            {
                sbHtml.AppendFormat("onclick='{0}'", HtmlUtility.MakeGotoPageLink(gridContext.GridName, currentPage - 1));
            }
            else
            {
                sbHtml.AppendFormat("onclick='{0}'", "return false;");
            }
            sbHtml.Append(">");
            sbHtml.Append("<span aria-hidden='true'>&laquo; Previous</span></a></li>");

            for (int i = pageToStart; i <= pageToEnd; i++)
            {
                sbHtml.Append("<li");
                if (i == currentPage)
                {
                    sbHtml.Append(" class='active'");
                }
                sbHtml.Append(">");
                sbHtml.AppendFormat("<a href='#' onclick='{0}'>{1}</a></li>", HtmlUtility.MakeGotoPageLink(gridContext.GridName, i), i);
            }


            sbHtml.Append("<li");
            if (pageToEnd == currentPage)
            {
                sbHtml.Append(" class='disabled'");
            }
            sbHtml.Append(">");

            sbHtml.Append("<a href='#' aria-label='Next' ");
            if (pageToEnd > currentPage)
            {
                sbHtml.AppendFormat("onclick='{0}'", HtmlUtility.MakeGotoPageLink(gridContext.GridName, currentPage + 1));
            }
            else
            {
                sbHtml.AppendFormat("onclick='{0}'", "return false;");
            }
            sbHtml.Append(">");
            sbHtml.Append("<span aria-hidden='true'>Next &raquo;</span></a></li>");

            sbHtml.Append("</ul>");
            sbHtml.Append("</div>");
            sbHtml.Append("</div>");
        }
    }
}
