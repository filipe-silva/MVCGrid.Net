using System.Net;
using System.Text;

namespace MVCGrid.Example.Common
{
    /// <summary>
    /// Builds the demo toolbar markup (page size / column visibility / export / global search)
    /// shared by the hosts: the classic MVC example renders it from the _MVCGridToolbar partial
    /// and the WASM showcase injects it client-side, so the markup is defined once here.
    /// </summary>
    public static class DemoToolbar
    {
        public static string BuildHtml(string gridName, bool pageSize, bool columnVisibility,
            bool export, bool globalSearch, string searchOption = "search")
        {
            string g = WebUtility.HtmlEncode(gridName);
            var sb = new StringBuilder();

            sb.Append("<div class=\"row\">");

            sb.Append("<div class=\"col-md-3\"><div class=\"form-inline\">");
            if (pageSize)
            {
                sb.Append("<div class=\"form-group\"><label>Page Size: ")
                  .Append("<select class=\"form-control\" data-mvcgrid-type=\"pageSize\" data-mvcgrid-name=\"").Append(g).Append("\">")
                  .Append("<option value=\"10\">10</option>")
                  .Append("<option value=\"25\">25</option>")
                  .Append("<option value=\"50\">50</option>")
                  .Append("<option value=\"100\">100</option>")
                  .Append("</select></label></div>");
            }
            sb.Append("</div></div>");

            sb.Append("<div class=\"col-md-9\"><div class=\"form-inline pull-right\">");
            sb.Append("<div class=\"btn-group\">");
            if (columnVisibility)
            {
                sb.Append("<div class=\"btn-group\">")
                  .Append("<button type=\"button\" class=\"btn btn-default dropdown-toggle\" data-toggle=\"dropdown\" aria-expanded=\"false\">")
                  .Append("<span class=\"glyphicon glyphicon-list\" aria-hidden=\"true\"></span> Fields</button>")
                  .Append("<ul class=\"dropdown-menu\" role=\"menu\" data-mvcgrid-type=\"columnVisibilityList\" data-mvcgrid-name=\"").Append(g).Append("\"></ul>")
                  .Append("</div>");
            }
            if (export)
            {
                sb.Append("<button class=\"btn btn-default\" data-mvcgrid-type=\"export\" data-mvcgrid-name=\"").Append(g).Append("\">")
                  .Append("<span class=\"glyphicon glyphicon-download-alt\" aria-hidden=\"true\"></span> Export</button>");
            }
            sb.Append("</div>");
            if (globalSearch)
            {
                sb.Append("<span>&nbsp;</span><div class=\"form-group\"><label>Search: ")
                  .Append("<input type=\"search\" placeholder=\"Search\" class=\"form-control\" ")
                  .Append("data-mvcgrid-type=\"additionalQueryOption\" data-mvcgrid-option=\"").Append(WebUtility.HtmlEncode(searchOption ?? "search"))
                  .Append("\" data-mvcgrid-name=\"").Append(g).Append("\" data-mvcgrid-apply-additional=\"keyup\"></label></div>");
            }
            sb.Append("</div></div>");

            sb.Append("</div>");
            return sb.ToString();
        }
    }
}
