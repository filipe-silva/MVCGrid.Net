using MVCGrid.Example.Common;
using MVCGrid.Models;
using MVCGrid.Web;
using MVCGrid.Web.Models;
using System;
using System.Web;

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

            //MVCGridDefinitionTable.Add DO NOT DELETE - Needed for demo code parsing

            // ---- Host-only API-reference grids (render the DocumentationRepository tables) ----

            var docsReturnTypeColumn = new GridColumn<MethodDocItem>
            {
                ColumnName = "ReturnType",
                HeaderText = "Return Type",
                HtmlEncode = false,
                ValueExpression = (p, c) => String.Format("<code>{0}</code>", HttpUtility.HtmlEncode(p.Return))
            };
            var docsNameColumn = new GridColumn<MethodDocItem>
            {
                ColumnName = "Name",
                HtmlEncode = false,
                ValueExpression = (p, c) => String.Format("<code>{0}</code>", HttpUtility.HtmlEncode(p.Name))
            };
            var docsDescriptionColumn = new GridColumn<MethodDocItem>
            {
                ColumnName = "Description",
                ValueExpression = (p, c) => p.Description
            };

            Func<GridContext, QueryResult<MethodDocItem>> docsLoadData = context =>
            {
                var result = new QueryResult<MethodDocItem>();
                var docRepo = new DocumentationRepository();
                result.Items = docRepo.GetData(context.GridName);
                return result;
            };

            MVCGridDefinitionTable.Add("GridDefinition", new MVCGridBuilder<MethodDocItem>()
                .AddColumn(docsNameColumn).AddColumn(docsReturnTypeColumn).AddColumn(docsDescriptionColumn)
                .WithRetrieveDataMethod(docsLoadData));

            MVCGridDefinitionTable.Add("GridColumn", new MVCGridBuilder<MethodDocItem>()
                .AddColumn(docsNameColumn).AddColumn(docsReturnTypeColumn).AddColumn(docsDescriptionColumn)
                .WithRetrieveDataMethod(docsLoadData));

            MVCGridDefinitionTable.Add("QueryOptions", new MVCGridBuilder<MethodDocItem>()
                .AddColumn(docsNameColumn).AddColumn(docsReturnTypeColumn).AddColumn(docsDescriptionColumn)
                .WithRetrieveDataMethod(docsLoadData));

            MVCGridDefinitionTable.Add("ClientSide", new MVCGridBuilder<MethodDocItem>()
                .AddColumn(docsNameColumn).AddColumn(docsDescriptionColumn)
                .WithRetrieveDataMethod(docsLoadData));
        }
    }
}
