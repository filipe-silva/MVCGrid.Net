using System;
using System.Collections.Generic;
using System.Linq;
using MVCGrid.Example.Common.Engines;
using MVCGrid.Models;
using MVCGrid.Web;

namespace MVCGrid.Example.Common
{
    /// <summary>
    /// Registers every portable (RenderingEngine-mode) demo grid into the shared
    /// <see cref="MVCGridDefinitionTable"/>. Used by all three example hosts, so the grids
    /// use the in-memory <see cref="PeopleRepository"/>/<see cref="PeopleData"/> directly
    /// (no DI container, no System.Web). Grid names match the classic example's views.
    /// Controller-mode (Razor) grids stay host-specific and are NOT registered here.
    /// </summary>
    public static class SampleGrids
    {
        private static readonly IPeopleRepository repo = new PeopleRepository();
        private static bool _registered;

        public static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;

            var colDefauls = new ColumnDefaults { EnableSorting = true };

            MVCGridDefinitionTable.Add("TestGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .WithSorting(sorting: true, defaultSortColumn: "Id", defaultSortDirection: SortDirection.Dsc)
                .WithPaging(paging: true, itemsPerPage: 10, allowChangePageSize: true, maxItemsPerPage: 100)
                .WithAdditionalQueryOptionNames("search")
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}'>{Model.Id}</a>", false)
                        .WithPlainTextValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithVisibility(true, true).WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithVisibility(true, true).WithValueExpression(p => p.LastName);
                    cols.Add("FullName").WithHeaderText("Full Name")
                        .WithValueTemplate("{Model.FirstName} {Model.LastName}")
                        .WithVisibility(visible: false, allowChangeVisibility: true).WithSorting(false);
                    cols.Add("StartDate").WithHeaderText("Start Date").WithVisibility(true, true)
                        .WithValueExpression(p => p.StartDate.HasValue ? p.StartDate.Value.ToShortDateString() : "");
                    cols.Add("Status").WithSortColumnData("Active").WithVisibility(true, true).WithHeaderText("Status")
                        .WithValueExpression(p => p.Active ? "Active" : "Inactive")
                        .WithCellCssClassExpression(p => p.Active ? "success" : "danger");
                    cols.Add("Gender").WithValueExpression((p, c) => p.Gender).WithAllowChangeVisibility(true);
                    cols.Add("Email").WithVisibility(false, true).WithValueExpression(p => p.Email);
                    cols.Add("Url").WithVisibility(false).WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }));
                })
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    int total;
                    var items = repo.GetData(out total, options.GetAdditionalQueryOptionString("search"),
                        options.GetLimitOffset(), options.GetLimitRowcount(),
                        options.GetSortColumnData<string>(), options.SortDirection == SortDirection.Dsc);
                    return new QueryResult<Person> { Items = items, TotalRecords = total };
                })
            );

            MVCGridDefinitionTable.Add("EmployeeGrid", new MVCGridBuilder<Person>()
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                })
                .WithRetrieveDataMethod(context => new QueryResult<Person>
                {
                    Items = PeopleData.Query().Where(p => p.Employee).ToList()
                })
            );

            MVCGridDefinitionTable.Add("SortableGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                })
                .WithSorting(true, "LastName")
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    var query = PeopleData.Query().Where(p => p.Employee);
                    if (!String.IsNullOrWhiteSpace(options.SortColumnName))
                    {
                        switch (options.SortColumnName.ToLower())
                        {
                            case "firstname": query = query.OrderBy(p => p.FirstName, options.SortDirection); break;
                            case "lastname": query = query.OrderBy(p => p.LastName, options.SortDirection); break;
                        }
                    }
                    return new QueryResult<Person> { Items = query.ToList() };
                })
            );

            MVCGridDefinitionTable.Add("PagingGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                })
                .WithSorting(true, "LastName")
                .WithPaging(true, 10)
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    var result = new QueryResult<Person>();
                    var query = PeopleData.Query();
                    result.TotalRecords = query.Count();
                    if (!String.IsNullOrWhiteSpace(options.SortColumnName))
                    {
                        switch (options.SortColumnName.ToLower())
                        {
                            case "firstname": query = query.OrderBy(p => p.FirstName, options.SortDirection); break;
                            case "lastname": query = query.OrderBy(p => p.LastName, options.SortDirection); break;
                        }
                    }
                    if (options.GetLimitOffset().HasValue)
                        query = query.Skip(options.GetLimitOffset().Value).Take(options.GetLimitRowcount().Value);
                    result.Items = query.ToList();
                    return result;
                })
            );

            MVCGridDefinitionTable.Add("DIGrid", PersonPagedGrid(colDefauls));
            MVCGridDefinitionTable.Add("PageSizeGrid", PersonPagedGrid(colDefauls, allowChangePageSize: true));
            MVCGridDefinitionTable.Add("QPLGrid", PersonPagedGrid(colDefauls, queryOnPageLoad: false, preload: false));

            MVCGridDefinitionTable.Add("FormattingGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("StartDate").WithHeaderText("Start Date")
                        .WithValueExpression(p => p.StartDate.HasValue ? p.StartDate.Value.ToShortDateString() : "");
                    cols.Add("ViewLink").WithSorting(false).WithHeaderText("").WithHtmlEncoding(false)
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}'>View</a>");
                })
                .WithSorting(true, "LastName").WithPaging(true, 10)
                .WithRetrieveDataMethod(SortColumnNamePage)
            );

            MVCGridDefinitionTable.Add("StyledGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("StartDate").WithHeaderText("Start Date")
                        .WithValueExpression(p => p.StartDate.HasValue ? p.StartDate.Value.ToShortDateString() : "");
                    cols.Add("Status").WithSortColumnData("Active").WithHeaderText("Status")
                        .WithValueExpression(p => p.Active ? "Active" : "Inactive");
                    cols.Add("Gender").WithValueExpression(p => p.Gender)
                        .WithCellCssClassExpression(p => p.Gender == "Female" ? "danger" : "warning");
                    cols.Add().WithColumnName("ViewLink").WithSorting(false).WithHeaderText("").WithHtmlEncoding(false)
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}'>View</a>");
                })
                .WithRowCssClassExpression(p => p.Active ? "success" : "")
                .WithSorting(true, "LastName").WithPaging(true, 10)
                .WithRetrieveDataMethod(SortColumnDataPage)
            );

            MVCGridDefinitionTable.Add("Preloading", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(PersonThreeCols)
                .WithPreloadData(false).WithSorting(true, "LastName").WithPaging(true, 10)
                .WithRetrieveDataMethod(SortColumnDataPage)
            );

            MVCGridDefinitionTable.Add("CustomLoading", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(PersonThreeCols)
                .WithSorting(true, "LastName").WithPaging(true, 10)
                .WithClientSideLoadingMessageFunctionName("showLoading")
                .WithClientSideLoadingCompleteFunctionName("hideLoading")
                .WithRetrieveDataMethod(SortColumnDataPage)  // (classic demo slept 1s here; omitted — unsafe on the WASM UI thread)
            );

            MVCGridDefinitionTable.Add("Filtering", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithValueExpression(p => p.Id.ToString());
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName).WithFiltering(true);
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName).WithFiltering(true);
                    cols.Add("Status").WithSortColumnData("Active").WithHeaderText("Status")
                        .WithValueExpression(p => p.Active ? "Active" : "Inactive").WithFiltering(true);
                })
                .WithSorting(true, "LastName").WithPaging(true, 10, true, 100).WithFiltering(true)
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    int total;
                    bool? active = null;
                    string fa = options.GetFilterString("Status");
                    if (!String.IsNullOrWhiteSpace(fa)) active = (String.Compare(fa, "active", true) == 0);
                    var items = repo.GetData(out total,
                        options.GetFilterString("FirstName"), options.GetFilterString("LastName"), active,
                        options.GetLimitOffset(), options.GetLimitRowcount(),
                        options.GetSortColumnData<string>(), options.SortDirection == SortDirection.Dsc);
                    return new QueryResult<Person> { Items = items, TotalRecords = total };
                })
            );

            MVCGridDefinitionTable.Add("ExportGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add().WithColumnName("Id").WithSorting(false).WithHtmlEncoding(false)
                        .WithValueExpression((p, c) => String.Format("<a href='{0}'>{1}</a>", c.UrlHelper.Action("detail", "demo", new { id = p.Id }), p.Id))
                        .WithPlainTextValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("Status").WithSortColumnData("Active").WithHeaderText("Status").WithValueExpression(p => p.Active ? "Active" : "Inactive");
                })
                .WithSorting(true, "LastName").WithPaging(true, 10)
                .WithClientSideLoadingMessageFunctionName("showLoading")
                .WithClientSideLoadingCompleteFunctionName("hideLoading")
                .WithRetrieveDataMethod(SortColumnDataPage)
            );

            MVCGridDefinitionTable.Add("Multiple1", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithHtmlEncoding(false)
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}'>{Model.Id}</a>")
                        .WithPlainTextValueExpression((p, c) => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("Status").WithSortColumnData("Active").WithHeaderText("Status").WithValueExpression(p => p.Active ? "Active" : "Inactive");
                })
                .WithSorting(true, "LastName").WithPaging(true, 10).WithQueryStringPrefix("grid1")
                .WithRetrieveDataMethod(SortColumnDataPage)
            );

            MVCGridDefinitionTable.Add("Multiple2", new MVCGridBuilder<TestItem>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Col1").WithValueExpression(p => p.Col1);
                    cols.Add("Col2").WithValueExpression(p => p.Col2);
                    cols.Add("Col3").WithValueExpression(p => p.Col3);
                })
                .WithSorting(true, "Col1").WithPaging(true, 10).WithQueryStringPrefix("grid2")
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    var r = new TestItemRepository();
                    int total;
                    var items = r.GetData(out total, options.GetLimitOffset(), options.GetLimitRowcount(),
                        options.GetSortColumnData<string>(), options.SortDirection == SortDirection.Dsc);
                    return new QueryResult<TestItem> { Items = items, TotalRecords = total };
                })
            );

            MVCGridDefinitionTable.Add("CustomStyle", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
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
                .AddRenderingEngine("custom", typeof(CustomHtmlRenderingEngine))
                .WithDefaultRenderingEngineName("custom")
                .WithSorting(true, "LastName").WithPaging(true, 20)
                .WithRetrieveDataMethod(SortColumnDataPage)
            );

            MVCGridDefinitionTable.Add("ValueTemplate", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false)
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}'>{Model.Id}</a>", false)
                        .WithPlainTextValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("Edit").WithHtmlEncoding(false).WithSorting(false).WithHeaderText(" ")
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}' class='btn btn-primary' role='button'>Edit</a>");
                    cols.Add("Delete").WithHtmlEncoding(false).WithSorting(false).WithHeaderText(" ")
                        .WithValueExpression((p, c) => c.UrlHelper.Action("detail", "demo", new { id = p.Id }))
                        .WithValueTemplate("<a href='{Value}' class='btn btn-danger' role='button'>Delete</a>");
                    cols.Add("Example").WithHtmlEncoding(false).WithSorting(false).WithHeaderText("Example")
                        .WithValueExpression((p, c) => p.Active ? "label-success" : "label-danger")
                        .WithValueTemplate("You can access any of the item's properties: <strong>{Model.FirstName}</strong> <br />or the current column value: <span class='label {Value}'>{Model.Active}</span>");
                })
                .WithSorting(true, "LastName").WithPaging(true, 20)
                .WithRetrieveDataMethod(SortColumnDataPage)
            );

            MVCGridDefinitionTable.Add("CustomErrorMessage", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(PersonThreeCols)
                .WithErrorMessageHtml(@"<div class=""alert alert-danger"" role=""alert"">OH NO!!!</div>")
                .WithSorting(true, "LastName").WithPaging(true)
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    var result = new QueryResult<Person>();
                    var query = PeopleData.Query();
                    result.TotalRecords = query.Count();
                    if (!String.IsNullOrWhiteSpace(options.SortColumnName))
                    {
                        switch (options.SortColumnName.ToLower())
                        {
                            case "firstname": throw new Exception("Test exception");
                            case "lastname": query = query.OrderBy(p => p.LastName, options.SortDirection); break;
                        }
                    }
                    if (options.GetLimitOffset().HasValue)
                        query = query.Skip(options.GetLimitOffset().Value).Take(options.GetLimitRowcount().Value);
                    result.Items = query.ToList();
                    return result;
                })
            );

            MVCGridDefinitionTable.Add("GlobalSearchGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(PersonThreeCols)
                .WithAdditionalQueryOptionNames("Search")
                .WithAdditionalSetting("RenderLoadingDiv", false)
                .WithSorting(true, "LastName").WithPaging(true, 10, true, 100)
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    int total;
                    var items = repo.GetData(out total, options.GetAdditionalQueryOptionString("Search"),
                        options.GetLimitOffset(), options.GetLimitRowcount(),
                        options.SortColumnName, options.SortDirection == SortDirection.Dsc);
                    return new QueryResult<Person> { Items = items, TotalRecords = total };
                })
            );

            MVCGridDefinitionTable.Add("ColumnVisibilityGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("Id").WithSorting(false).WithValueExpression(p => p.Id.ToString());
                    cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
                    cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
                    cols.Add("StartDate").WithHeaderText("Start Date").WithVisibility(false, true)
                        .WithValueExpression(p => p.StartDate.HasValue ? p.StartDate.Value.ToShortDateString() : "");
                    cols.Add("Status").WithSortColumnData("Active").WithHeaderText("Status").WithVisibility(false, true)
                        .WithValueExpression(p => p.Active ? "Active" : "Inactive")
                        .WithCellCssClassExpression((p, c) => p.Active ? "success" : "danger");
                    cols.Add("Gender").WithVisibility(false, true).WithValueExpression(p => p.Gender);
                })
                .WithSorting(true, "LastName").WithPaging(true, 10)
                .WithRetrieveDataMethod(SortColumnNamePage)
            );

            MVCGridDefinitionTable.Add("NestedObjectTest", new MVCGridBuilder<Job>()
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .WithPaging(true)
                .AddColumns(cols =>
                {
                    cols.Add("Id", "Id", row => row.JobId.ToString());
                    cols.Add("Name", "Name", row => row.Name);
                    cols.Add("Contact").WithHeaderText("Contact").WithHtmlEncoding(false).WithSorting(true)
                        .WithValueExpression((p, c) => p.Contact != null ? c.UrlHelper.Action("Edit", "Contact", new { id = p.Contact.Id }) : "")
                        .WithValueTemplate("<a href='{Value}'>{Model.Contact.FullName}</a>")
                        .WithPlainTextValueExpression((p, c) => p.Contact != null ? p.Contact.FullName : "");
                })
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    var r = new JobRepo();
                    int total;
                    var data = r.GetData(out total, null, options.GetLimitOffset(), options.GetLimitRowcount(), null, false);
                    return new QueryResult<Job> { Items = data, TotalRecords = total };
                })
            );

            MVCGridDefinitionTable.Add("PPGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .WithPageParameterNames("Active")
                .AddColumns(PersonThreeCols)
                .WithPreloadData(true).WithSorting(true, "LastName").WithPaging(true, 10)
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    int total;
                    string ppactive = options.GetPageParameterString("active");
                    bool filterActive = bool.Parse(ppactive);
                    var items = repo.GetData(out total, null, null, filterActive, options.GetLimitOffset(), options.GetLimitRowcount(),
                        options.GetSortColumnData<string>(), options.SortDirection == SortDirection.Dsc);
                    return new QueryResult<Person> { Items = items, TotalRecords = total };
                })
            );

            MVCGridDefinitionTable.Add("CustomExport", new MVCGridBuilder<Person>(colDefauls)
                .AddRenderingEngine("tabs", typeof(TabDelimitedRenderingEngine))
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
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
                .WithRetrieveDataMethod(SortColumnDataPage)
            );

            MVCGridDefinitionTable.Add("AQOGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(PersonThreeCols)
                .WithAdditionalQueryOptionNames("param1", "param2", "param3")
                .WithAdditionalSetting("RenderLoadingDiv", false)
                .WithSorting(true, "LastName").WithPaging(true, 10, true, 100)
                .WithRetrieveDataMethod(SortColumnNamePage)
            );

            MVCGridDefinitionTable.Add("LocalizationGrid", new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(PersonThreeCols)
                .WithSorting(true, "LastName").WithPaging(true, 10)
                .WithProcessingMessage("Cargando")
                .WithNextButtonCaption("Siguiente")
                .WithPreviousButtonCaption("Anterior")
                .WithSummaryMessage("Mostrando {0} a {1} de {2} entradas")
                .WithRetrieveDataMethod(context =>
                {
                    var options = context.QueryOptions;
                    var result = new QueryResult<Person>();
                    var query = PeopleData.Query();
                    result.TotalRecords = query.Count();
                    if (!String.IsNullOrWhiteSpace(options.SortColumnName))
                    {
                        switch (options.SortColumnName.ToLower())
                        {
                            case "firstname": query = query.OrderBy(p => p.FirstName, options.SortDirection); break;
                            case "lastname": query = query.OrderBy(p => p.LastName, options.SortDirection); break;
                        }
                    }
                    if (options.GetLimitOffset().HasValue)
                        query = query.Skip(options.GetLimitOffset().Value).Take(options.GetLimitRowcount().Value);
                    result.Items = query.ToList();
                    return result;
                })
            );

            MVCGridDefinitionTable.Add("UsageExample", new MVCGridBuilder<YourModelItem>()
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(cols =>
                {
                    cols.Add("UniqueColumnName").WithHeaderText("Any Header").WithValueExpression(i => i.YourProperty);
                    cols.Add().WithColumnName("UrlExample").WithHeaderText("Edit")
                        .WithValueExpression((i, c) => c.UrlHelper.Action("detail", "demo", new { id = i.YourProperty }));
                })
                .WithRetrieveDataMethod(context => new QueryResult<YourModelItem> { Items = new List<YourModelItem>(), TotalRecords = 0 })
            );

            var nonFluent = new GridDefinition<YourModelItem>();
            var col = new GridColumn<YourModelItem> { ColumnName = "UniqueColumnName", HeaderText = "Any Header", ValueExpression = (i, c) => i.YourProperty };
            nonFluent.AddColumn(col);
            nonFluent.RetrieveData = options => new QueryResult<YourModelItem> { Items = new List<YourModelItem>(), TotalRecords = 0 };
            MVCGridDefinitionTable.Add("NonFluentUsageExample", nonFluent);

            var defaultSet1 = new GridDefaults { Paging = true, ItemsPerPage = 20, Sorting = true, NoResultsMessage = "Sorry, no results were found" };
            MVCGridDefinitionTable.Add("DefaultsExample", new MVCGridBuilder<YourModelItem>(defaultSet1)
                .AddColumns(cols => { })
                .WithDefaultSortColumn("Test")
                .WithRetrieveDataMethod(context => new QueryResult<YourModelItem>())
            );
        }

        // ---- shared column sets / retrieve helpers ----

        private static void PersonThreeCols(MVCGrid.Models.GridColumnListBuilder<Person> cols)
        {
            cols.Add("Id").WithSorting(false).WithValueExpression(p => p.Id.ToString());
            cols.Add("FirstName").WithHeaderText("First Name").WithValueExpression(p => p.FirstName);
            cols.Add("LastName").WithHeaderText("Last Name").WithValueExpression(p => p.LastName);
        }

        private static MVCGridBuilder<Person> PersonPagedGrid(ColumnDefaults colDefauls,
            bool allowChangePageSize = false, bool queryOnPageLoad = true, bool preload = true)
        {
            var b = new MVCGridBuilder<Person>(colDefauls)
                .WithAuthorizationType(AuthorizationType.AllowAnonymous)
                .AddColumns(PersonThreeCols)
                .WithSorting(true, "LastName")
                .WithPaging(true, 10, allowChangePageSize, 100)
                .WithQueryOnPageLoad(queryOnPageLoad)
                .WithPreloadData(preload)
                .WithRetrieveDataMethod(SortColumnNamePage);
            return b;
        }

        // Sort by QueryOptions.SortColumnName (used by grids without WithSortColumnData columns).
        private static QueryResult<Person> SortColumnNamePage(GridContext context)
        {
            var options = context.QueryOptions;
            int total;
            var items = repo.GetData(out total, options.GetLimitOffset(), options.GetLimitRowcount(),
                options.SortColumnName, options.SortDirection == SortDirection.Dsc);
            return new QueryResult<Person> { Items = items, TotalRecords = total };
        }

        // Sort by the column's SortColumnData (used by grids with a Status→Active mapping etc.).
        private static QueryResult<Person> SortColumnDataPage(GridContext context)
        {
            var options = context.QueryOptions;
            int total;
            var items = repo.GetData(out total, options.GetLimitOffset(), options.GetLimitRowcount(),
                options.GetSortColumnData<string>(), options.SortDirection == SortDirection.Dsc);
            return new QueryResult<Person> { Items = items, TotalRecords = total };
        }
    }
}
