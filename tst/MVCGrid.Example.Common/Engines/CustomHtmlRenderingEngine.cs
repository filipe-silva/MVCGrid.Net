using System;
using System.IO;
using System.Text;
using MVCGrid.Abstractions;
using MVCGrid.Interfaces;
using MVCGrid.Models;

namespace MVCGrid.Example.Common.Engines
{
    /// <summary>Demonstrates a fully custom HTML rendering engine (emits its own table markup).</summary>
    public class CustomHtmlRenderingEngine : IMVCGridRenderingEngine
    {
        public bool AllowsPaging { get { return true; } }

        public void PrepareResponse(IGridResponse response)
        {
        }

        public void Render(RenderingModel model, GridContext gridContext, TextWriter outputStream)
        {
            var sb = new StringBuilder();

            sb.Append("<table class='customStyleTable'><thead><tr>");
            foreach (var col in model.Columns)
            {
                sb.Append("<th");
                if (!String.IsNullOrWhiteSpace(col.Onclick))
                {
                    sb.AppendFormat(" onclick='{0}'", col.Onclick);
                }
                sb.Append(">");
                sb.Append(col.HeaderText);
                if (col.SortIconDirection.HasValue)
                {
                    switch (col.SortIconDirection.Value)
                    {
                        case SortDirection.Asc: sb.Append(" (Ascending)"); break;
                        case SortDirection.Dsc: sb.Append(" (Descending)"); break;
                        case SortDirection.Unspecified: sb.Append(" (Sort)"); break;
                    }
                }
                sb.Append("</th>");
            }
            sb.Append("</tr></thead><tbody>");

            foreach (var row in model.Rows)
            {
                sb.Append("<tr");
                if (!String.IsNullOrWhiteSpace(row.CalculatedCssClass))
                {
                    sb.AppendFormat(" class='{0}'", row.CalculatedCssClass);
                }
                sb.Append(">");

                foreach (var col in model.Columns)
                {
                    var cell = row.Cells[col.Name];
                    sb.Append("<td");
                    if (!String.IsNullOrWhiteSpace(cell.CalculatedCssClass))
                    {
                        sb.AppendFormat(" class='{0}'", cell.CalculatedCssClass);
                    }
                    sb.Append(">");
                    sb.Append(cell.HtmlText);
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");

            if (model.PagingModel != null)
            {
                sb.Append("<div><ul>");
                foreach (var pl in model.PagingModel.PageLinks)
                {
                    sb.Append("<li class='pageItem'>");
                    sb.AppendFormat("<a href='#' onclick='{0}'>{1}</a>", pl.Value, pl.Key);
                    sb.Append("</li>");
                }
                sb.Append("</ul></div>");
            }

            outputStream.Write(sb.ToString());
            outputStream.Write(model.ClientDataTransferHtmlBlock);
        }

        public void RenderContainer(ContainerRenderingModel model, TextWriter outputStream)
        {
            outputStream.Write(model.InnerHtmlBlock);
        }
    }
}
