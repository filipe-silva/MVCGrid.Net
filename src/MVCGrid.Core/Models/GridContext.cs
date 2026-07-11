using MVCGrid.Abstractions;
using MVCGrid.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace MVCGrid.Models
{
    public class GridContext
    {
        public GridContext()
        {
            Items = new Dictionary<string, object>();
        }
        internal IMVCGridDefinition GridDefinition { get; set; }

        /// <summary>
        /// The current user, used for the Authorized authorization type. Supplied by
        /// the web adapter (from HttpContext.User). Replaces the former
        /// CurrentHttpContext property, which leaked System.Web into the core.
        /// </summary>
        public IPrincipal User { get; set; }

        /// <summary>
        /// Base path to the MVCGrid resource handler, used to build AJAX/image URLs.
        /// Supplied by the web adapter when the context is created.
        /// </summary>
        public string HandlerPath { get; set; }

        public QueryOptions QueryOptions { get; set; }

        /// <summary>
        /// Builds URLs for column value expressions and templates. Backed by the host
        /// framework's URL generation (System.Web.Mvc.UrlHelper in the MVC adapter).
        /// </summary>
        public IMvcGridUrlBuilder UrlHelper { get; set; }
        public string GridName { get; set; }

        public IEnumerable<IMVCGridColumn> GetVisibleColumns()
        {
            List<IMVCGridColumn> visibleColumns = new List<IMVCGridColumn>();

            var gridColumns = this.GridDefinition.GetColumns();

            if (QueryOptions.ColumnVisibility == null || QueryOptions.ColumnVisibility.Count == 0)
            {
                foreach (var col in gridColumns)
                {
                    if (col.Visible)
                    {
                        visibleColumns.Add(col);
                    }
                }
            }
            else
            {
                foreach (var colVis in QueryOptions.ColumnVisibility)
                {
                    var gridColumn = gridColumns.SingleOrDefault(p => p.ColumnName == colVis.ColumnName);

                    if (colVis.Visible)
                    {
                        visibleColumns.Add(gridColumn);
                    }
                }
            }

            if (visibleColumns.Count == 0)
            {
                visibleColumns.Add(this.GridDefinition.GetColumns().ElementAt(0));
            }

            return visibleColumns;
        }

        /// <summary>
        /// Arbitrary settings for this context
        /// </summary>
        public Dictionary<string, object> Items { get; set; }
    }
}
