using MVCGrid.Example.Common;
using MVCGrid.Models;
using MVCGrid.Web;

namespace MVCGridExample
{
    public class MVCGridConfig
    {
        public static void RegisterGrids()
        {
            // The portable feature grids live in the shared library and are used by every host
            // (classic MVC, ASP.NET Core, WebAssembly). See MVCGrid.Example.Common.SampleGrids.
            SampleGrids.RegisterAll();

            var colDefauls = new ColumnDefaults { EnableSorting = true };
            var repo = new PeopleRepository();

            // ---- Host-only grids: RenderingMode.Controller renders through a Razor partial,
            //      so these are classic-MVC-only (not reproducible on the client-side hosts). ----

            MVCGridDefinitionTable.Add("CustomRazorView", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .WithRenderingMode(RenderingMode.Controller)
                .WithViewPath("~/Views/MVCGrid/_Custom.cshtml")
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithHtmlEncoding(false)
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}'>{Model.Id}</a>")
                        .WithPlainTextValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("Status").WithSortColumnData("Active").WithHeaderText("Status").WithValueExpression(p => p.Active ? "Active" : "Inactive");
                })
                .WithSorting(true, "LastName").WithPaging(true, 20)
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    int total;
                    var items = repo.GetData(out total, options.GetLimitOffset(), options.GetLimitRowcount(),
                        options.GetSortColumnData<string>(), options.SortDirection == SortDirection.Dsc);
                    return new QueryResult<Person> { Items = items, TotalRecords = total };
                })
            );

            MVCGridDefinitionTable.Add("CustomRazorView2", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .WithRenderingMode(RenderingMode.Controller)
                .WithViewPath("~/Views/MVCGrid/_Grid.cshtml")
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithHtmlEncoding(false)
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}'>{Model.Id}</a>")
                        .WithPlainTextValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("Status").WithSortColumnData("Active").WithHeaderText("Status").WithValueExpression(p => p.Active ? "Active" : "Inactive");
                })
                .WithSorting(true, "LastName").WithPaging(true, 20)
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    int total;
                    var items = repo.GetData(out total, options.GetLimitOffset(), options.GetLimitRowcount(),
                        options.GetSortColumnData<string>(), options.SortDirection == SortDirection.Dsc);
                    return new QueryResult<Person> { Items = items, TotalRecords = total };
                })
            );

            // The API-reference grids on the Documentation page (GridDefinition/GridColumn/
            // QueryOptions/ClientSide) are portable and registered by SampleGrids.RegisterAll().
        }
    }
}
