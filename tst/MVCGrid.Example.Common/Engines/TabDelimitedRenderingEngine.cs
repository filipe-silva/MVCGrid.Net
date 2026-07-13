using System;
using System.IO;
using System.Text;
using MVCGrid.Abstractions;
using MVCGrid.Interfaces;
using MVCGrid.Models;

namespace MVCGrid.Example.Common.Engines
{
    /// <summary>Demonstrates an additional export engine (tab-separated values).</summary>
    public class TabDelimitedRenderingEngine : IMVCGridRenderingEngine
    {
        public bool AllowsPaging { get { return false; } }

        public void PrepareResponse(IGridResponse httpResponse)
        {
            httpResponse.ContentType = "text/tab-separated-values";
            httpResponse.AddHeader("content-disposition", "attachment; filename=\"export.tsv\"");
        }

        public void Render(RenderingModel model, GridContext gridContext, TextWriter outputStream)
        {
            var sbHeaderRow = new StringBuilder();
            foreach (var col in model.Columns)
            {
                if (sbHeaderRow.Length != 0) sbHeaderRow.Append("\t");
                sbHeaderRow.Append(Encode(col.Name));
            }
            sbHeaderRow.AppendLine();
            outputStream.Write(sbHeaderRow.ToString());

            foreach (var item in model.Rows)
            {
                var sbRow = new StringBuilder();
                foreach (var col in model.Columns)
                {
                    var cell = item.Cells[col.Name];
                    if (sbRow.Length != 0) sbRow.Append("\t");
                    sbRow.Append(Encode(cell.PlainText));
                }
                sbRow.AppendLine();
                outputStream.Write(sbRow.ToString());
            }
        }

        private string Encode(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return "";
            if (s.Contains("\t")) s = s.Replace("\t", " ");
            return s;
        }

        public void RenderContainer(ContainerRenderingModel model, TextWriter outputStream)
        {
        }
    }
}
