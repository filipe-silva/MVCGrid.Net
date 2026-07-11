# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

MVCGrid.Net is a server-side data grid for ASP.NET MVC + Bootstrap with AJAX paging, sorting, filtering, column visibility, and CSV export. This is a fork of the original mvcgrid.net, maintained by filipe-silva.

The library has been split into a **portable core + a framework adapter**:
- **`MVCGrid`** — the core, targets **netstandard2.0**, free of any `System.Web` dependency. All the grid logic lives here.
- **`MVCGrid.MvcWeb`** — the **.NET Framework 4.8** adapter that hosts the core inside classic ASP.NET MVC (`System.Web` / `System.Web.Mvc`, *not* ASP.NET Core).

Both ship as NuGet packages. A consumer using classic MVC references `MVCGrid.MvcWeb` (which pulls in `MVCGrid`); the netstandard core exists so a different host can be built against it. A planned `MVCGrid.AspNetCore` adapter (ASP.NET Core MVC) is scoped in `MULTI-HOST-PLAN.md` at the repo root — not yet implemented.

## Projects (`MVCGrid.sln`)

- **MVCGrid.Core/** — the portable library, **netstandard2.0**, ships as assembly/package **`MVCGrid`** (note: the folder is `MVCGrid.Core` but `AssemblyName` is `MVCGrid`). Contains all System.Web-free logic: models, `GridEngine` (model generation/auth/engine resolution), Bootstrap + CSV rendering engines, `SimpleTemplatingEngine`, the definition table and query-string parser — plus the framework-neutral abstractions in `MVCGrid.Abstractions`. Also holds the **embedded client assets** (`Scripts/MVCGrid.js`, `Images/*`) so every adapter serves identical assets. Zero NuGet dependencies. No test project exists.
- **MVCGrid.MvcWeb/** — the **net48** ASP.NET MVC adapter (assembly `MVCGrid.MvcWeb`), references the core. Holds all the `System.Web`/`System.Web.Mvc` glue: `@Html.MVCGrid` (`HtmlExtensions`), `MVCGridController`, the `MVCGridHandler.axd` HTTP handler (which serves the core's embedded `MVCGrid.js` + images and does the `%%HANDLERPATH%%`/`%%CONTROLLERPATH%%`/`%%ERRORDETAILS%%` substitution), `GridRegistration`, `GridContextUtility`, `ConfigUtility`, the host-side render flow (`MvcGridHtmlRenderer`), and the adapter implementations of the core abstractions (`MvcGridAdapters.cs`).
- **MVCGridExample/** — a net48 MVC web app that is both the demo site and the de-facto integration test surface (assembly name `MVCGrid.Web`). Every feature has a demo grid registered in `App_Start/MVCGridConfig.cs` and a matching view under `Views/Demo/`.
- **docs/** — a Jekyll site published to GitHub Pages (`https://filipe-silva.github.io/MVCGrid.Net`, `baseurl: /MVCGrid.Net`). Hand-written HTML, not generated from code. The `pages` branch is where docs/layout work happens. Links must be baseurl-aware (`{{ 'x.html' | relative_url }}`); grid sample images live in `docs/images/`.

## Build & run

SDK-style projects with **Central Package Management**: `Directory.Packages.props` at the repo root holds every package version (`<PackageVersion>`), and projects use version-less `<PackageReference>`. Exception: `MVCGrid.MvcWeb` pins `Microsoft.AspNet.Mvc` via `VersionOverride="3.0.20105.1"` — it compiles against **MVC 3.0** so one adapter DLL works on ASP.NET MVC 3/4/5 (hosts bind up to their own version via `bindingRedirect`; the example runs it on 5.2.2). There is no CLI test runner or lint step.

```
# The core (netstandard2.0) and the adapter (net48) build/restore with the dotnet CLI:
dotnet build MVCGrid.Core/MVCGrid.Core.csproj
dotnet build MVCGrid.MvcWeb/MVCGrid.MvcWeb.csproj

# The FULL solution — including the MVCGridExample web app — needs Visual Studio 2022's MSBuild.
# The web project imports Microsoft.WebApplication.targets, which the dotnet CLI does not have,
# so `dotnet build MVCGridExample` fails with MSB4019; use VS MSBuild instead:
#   "…/Microsoft Visual Studio/2022/<edition>/MSBuild/Current/Bin/amd64/MSBuild.exe"
msbuild MVCGrid.sln -t:Build -restore
```

To verify a change, run **MVCGridExample** in IIS Express from Visual Studio and exercise the relevant demo grid (each feature = one grid in `MVCGridConfig.cs` + one page under `/Demo`). There are no automated tests; the example app is how behavior is checked. CSV/tab export exercises the `IGridResponse` path and column URL expressions exercise `IMvcGridUrlBuilder` — good things to smoke-test.

## The core/adapter split and its abstractions

netstandard2.0 cannot reference `System.Web.Mvc`, so every place the core needed a `System.Web` type it takes a framework-neutral abstraction instead; `MVCGrid.MvcWeb` supplies the real implementations. When editing the core, never reach for `System.Web` — add/extend an abstraction and implement it in the adapter.

- **`IMvcGridUrlBuilder`** (in `MVCGrid.Abstractions`) replaces `System.Web.Mvc.UrlHelper`. It is exposed as `GridContext.UrlHelper` and `TemplateModel.Url` (property names kept), so existing grid definitions like `c.UrlHelper.Action("detail","demo", new { id })` compile unchanged. Adapter impl: `MvcUrlBuilder`.
- **`IGridResponse`** replaces `System.Web.HttpResponse` in `IMVCGridRenderingEngine.PrepareResponse` — deliberately just `ContentType` + `AddHeader` (portable across hosts). Classic-only `Clear()`/`BufferOutput=false` are applied by `MVCGridHandler.HandleTable` around the engine call, not exposed on the interface. Adapter impl: `MvcGridResponse`. Custom rendering engines implement `PrepareResponse(IGridResponse)`.
- **`IQueryStringReader`** replaces `System.Web.HttpRequest` in `QueryStringParser.ParseOptions`. Adapter impl: `MvcQueryStringReader` (wraps `HttpRequest.QueryString`).
- **Auth**: `GridContext.User` is a BCL `System.Security.Principal.IPrincipal` (set by the adapter from `HttpContext.User`); the core checks `User.Identity.IsAuthenticated`. There is no `HttpContext` in the core.
- **`GridContext.HandlerPath`** is set by the adapter (from `HttpContext.Current`) since the `MVCGridHandler.axd` base path is a host concern.
- The core is kept dependency-free: rendering-engine registrations use `RenderingEngineCollection`/`RenderingEngineSetting` (not `System.Configuration.ProviderSettings`); HTML/JS encoding uses `System.Net.WebUtility` + a hand-rolled `HtmlUtility.JavaScriptStringEncode`. `MVCGrid.Core` marks internals visible to `MVCGrid.MvcWeb` (`InternalsVisibleTo`) so the adapter can reach `QueryStringParser`, `MVCGridHtmlGenerator`, and `GridContext`'s internal setter without widening the public API.

## Architecture — request flow

Grids are **defined once at startup, keyed by string name** in a static table, then looked up per request. There is no per-grid controller; all data requests hit one shared endpoint.

**Definition (startup, core):** `MVCGridBuilder<T>` builds a `GridDefinition<T>`, stored in the static `MVCGridDefinitionTable` under its name. `GridDefinition<T>` implements the non-generic `IMVCGridDefinition` (used wherever the type param isn't known) and derives from `GridDefinitionBase` (whose internal `GetData` runs the column value expressions). Column value logic lives in `Func<T, GridContext, string>` on each `GridColumn<T>`.

**Page load (adapter → core):** `@Html.MVCGrid(name)` → `HtmlExtensions.MVCGrid` → `MvcGridHtmlRenderer.GetBasePageHtml` (adapter) → core `MVCGridHtmlGenerator.GenerateBasePageHtml`. Emits the outer HTML shell + a JS-populated placeholder. If `PreloadData` + `QueryOnPageLoad` are set, the adapter also renders the first page of data inline (via core `GridEngine.GenerateModel`) so the initial AJAX round-trip is skipped.

**Data requests (AJAX):** the browser (`MVCGrid.js`) calls one of two adapter entry points that do the same thing:
- `MVCGridHandler.axd` (`Web/MVCGridHandler.cs`, an `IHttpHandler`) — the default path; also serves the embedded JS/PNG/GIF resources.
- `MVCGridController` (`/mvcgrid/grid`) — used when `RenderingMode.Controller` renders through a Razor view.

Both: read the grid name → `MVCGridDefinitionTable.GetDefinitionInterface(name)` (core) → `QueryStringParser.ParseOptions` (core, fed an `MvcQueryStringReader`) → `GridContextUtility.Create` (adapter; sets `UrlHelper`, `User`, `HandlerPath`) → `GridEngine.CheckAuthorization` (core) → `GridEngine` runs the definition's `RetrieveData`, wraps rows into a `RenderingModel`, and hands it to a rendering engine.

**Two rendering modes** (`RenderingMode` enum, via `WithRenderingMode`):
- `RenderingEngine` (default) — an `IMVCGridRenderingEngine` writes HTML directly. Default impl `BootstrapRenderingEngine`; `CsvRenderingEngine` handles export. Engines are registered by name (`AddRenderingEngine`) and chosen per-request via the `engine` query-string param, enabling alternate exports.
- `Controller` — renders through a Razor partial view at `ViewPath` (see the `CustomRazorView*` demos), driven by `MvcGridHtmlRenderer` in the adapter.

**Templating:** columns can use `WithValueTemplate("...{Model.Prop}...{Value}...")` instead of a value expression. Templates are processed per-cell by an `IMVCGridTemplatingEngine` (default `SimpleTemplatingEngine`).

## Key conventions

- **`RetrieveData` owns all data access.** The library never queries anything itself — the callback (EF, an IoC-resolved repo, raw SQL, whatever) must honor `context.QueryOptions` for sorting/paging/filtering and, **when paging is enabled, must set `QueryResult.TotalRecords`** or `GetData` throws. Helpers: `options.GetLimitOffset()`, `GetLimitRowcount()`, `GetSortColumnData<T>()`, `GetFilterString(col)`, `GetAdditionalQueryOptionString(name)`, `GetPageParameterString(name)`.
- **Enabling a feature is two-level:** sorting/filtering must be on at the grid level (`WithSorting`/`WithFiltering`) *and* per column. Grid-level sorting also requires a default sort column or `Add` throws.
- **Query-string params are prefixed per grid** (`WithQueryStringPrefix`) so multiple grids can coexist on one page. Reserved suffixes (see `QueryStringParser`): `page`, `sort`, `dir`, `engine`, `pagesize`, `cols`; page parameters use the `_pp_` prefix. Additional query-option names may not collide with these or with column names — validated in `MVCGridDefinitionTable.Add`.
- **Fluent column API** (`.Add("Name").WithHeaderText(...).WithValueExpression(...)`) is the modern path; `GridDefaults`/`ColumnDefaults` supply reusable defaults passed to the builder ctor. A legacy non-fluent path (construct `GridColumn<T>` directly) is still supported — see `NonFluentUsageExample`.
- **`GridRegistration`** (adapter) is an alternative to central config: subclass it, implement `RegisterGrids()`, and `RegisterAllGrids()` discovers all subclasses across referenced assemblies via reflection at startup.
- **Error detail** is controlled by the `MVCGridShowErrorDetail` appSetting (default false → shows `ErrorMessageHtml`; true → dumps the exception). Read via `ConfigUtility` (adapter); the core receives the resolved behavior rather than reading config itself.
- **`WithRenderingEngine(Type)` is obsolete** — use `AddRenderingEngine(name, type)` + `WithDefaultRenderingEngineName`.

## Consumer setup (classic MVC host)

Four things a host app must do — mirrored in MVCGridExample:
1. Register the HTTP handler in `Web.config`: `<add name="MVCGridHandler" verb="*" path="MVCGridHandler.axd" type="MVCGrid.Web.MVCGridHandler, MVCGrid.MvcWeb"/>` (note the assembly is `MVCGrid.MvcWeb`; the handler *type* namespace is still `MVCGrid.Web`). This serves the embedded `MVCGrid.js` + images and is the AJAX endpoint.
2. Include the script after jQuery: `<script src="~/MVCGridHandler.axd/script.js"></script>`.
3. Register grids at `Application_Start` (see `Global.asax.cs`): call your own `MVCGridConfig.RegisterGrids()` **and** `GridRegistration.RegisterAllGrids()`.
4. Render in a view: `@Html.MVCGrid("GridName")`.

Note: the NuGet packaging assets (`NuGetFiles/`, `MVCGrid.MvcWeb/MVCGrid.nuspec`, the old `.nuget/` folder) have **not** yet been updated for the two-package split — treat them as stale until reworked.

## Editing the example config

`MVCGridConfig.cs` contains a comment `//MVCGridDefinitionTable.Add DO NOT DELETE - Needed for demo code parsing` — the docs/demo pages parse this file's source to display live code snippets, so grid registrations above that marker are shown to users. Keep registrations well-formed and don't remove that marker.
