using MVCGrid.AspNetCoreExample.Data;
using MVCGrid.Models;
using MVCGrid.Web;

namespace MVCGrid.AspNetCoreExample.Grids
{
    public static class GridConfig
    {
        public static void RegisterGrids()
        {
            var columnDefaults = new ColumnDefaults { EnableSorting = true };

            MVCGridDefinitionTable.Add("peopleGrid", new MVCGridBuilder<Person>(columnDefaults)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .WithSorting(sorting: true, defaultSortColumn: "Id", defaultSortDirection: SortDirection.Dsc)
                .WithPaging(paging: true, itemsPerPage: 10, allowChangePageSize: true, maxItemsPerPage: 50)
                .WithFiltering(true)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name")
                        .WithValueExpression(p => p.FirstName)
                        .WithFiltering(true);
                    cols.Add("LastName").WithHeaderText("Last Name")
                        .WithValueExpression(p => p.LastName)
                        .WithFiltering(true);
                    cols.Add("StartDate").WithHeaderText("Start Date")
                        .WithValueExpression(p => p.StartDate.HasValue ? p.StartDate.Value.ToShortDateString() : "");
                    cols.Add("Status").WithSortColumnData("Active")
                        .WithHeaderText("Status")
                        .WithValueExpression(p => p.Active ? "Active" : "Inactive")
                        .WithCellCssClassExpression(p => p.Active ? "success" : "danger");
                    cols.Add("View").WithSorting(false)
                        .WithHeaderText("")
                        .WithHtmlEncoding(false)
                        .WithValueExpression((p, c) => c.UrlHelper.Action("Detail", "Home", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}' class='btn btn-xs btn-primary'>View</a>");
                })
                .WithRetrieveDataMethod(context =>
                {
                    var o = context.QueryOptions;
                    string sortColumn = o.GetSortColumnData<string>() ?? o.SortColumnName;

                    var (items, total) = PeopleRepository.GetData(
                        o.GetFilterString("FirstName"),
                        o.GetFilterString("LastName"),
                        sortColumn,
                        o.SortDirection == SortDirection.Dsc,
                        o.GetLimitOffset(),
                        o.GetLimitRowcount());

                    return new QueryResult<Person>
                    {
                        Items = items,
                        TotalRecords = total
                    };
                })
            );
        }
    }
}
