# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

MVCGrid.Net is a server-side data grid for ASP.NET MVC + Bootstrap with AJAX paging, sorting,
filtering, column visibility, and CSV export — a fork of the original mvcgrid.net, maintained by
filipe-silva. The library is a **portable netstandard2.0 core** plus **host adapters**, each
shipped as a NuGet package. A classic-MVC consumer references `MVCGrid.MvcWeb` (which pulls in
`MVCGrid`). `MULTI-HOST-PLAN.md` is the multi-host checklist (the ASP.NET Core adapter's Phase 1 is
done; Controller mode and a TagHelper are deferred there).

## Projects (`MVCGrid.sln`)

- **`src/MVCGrid.Core`** — netstandard2.0, assembly/package **`MVCGrid`** (folder name ≠
  `AssemblyName`). All `System.Web`-free logic: models, `GridEngine`, Bootstrap + CSV engines,
  `SimpleTemplatingEngine`, the definition table, the query-string parser, the abstractions in
  `MVCGrid.Abstractions`, and the **embedded client assets** (`Scripts/MVCGrid.js`, `Images/*`) so
  every adapter serves identical assets. Zero NuGet dependencies.
- **`src/MVCGrid.MvcWeb`** — net48 classic ASP.NET MVC adapter (`System.Web`/`System.Web.Mvc`, not
  Core): `@Html.MVCGrid` (`HtmlExtensions`), `MVCGridController`, the `MVCGridHandler.axd` handler
  (serves the embedded assets, substitutes `%%HANDLERPATH%%`/`%%CONTROLLERPATH%%`/`%%ERRORDETAILS%%`),
  `GridRegistration`, `GridContextUtility`, `ConfigUtility`, `MvcGridHtmlRenderer`, and the
  abstraction impls in `MvcGridAdapters.cs`.
- **`src/MVCGrid.AspNetCore`** — net8 ASP.NET Core MVC adapter (`FrameworkReference
  Microsoft.AspNetCore.App`): abstractions over `IUrlHelper`/`HttpResponse`/`HttpRequest.Query`/
  `HttpContext.User`, `services.AddMVCGrid()`, `app.MapMVCGrid()` (data/export endpoint +
  `script.js` + images), `@Html.MVCGrid(...)`. **RenderingEngine mode only**; compile-verified,
  runtime smoke-test still pending.
- **`src/MVCGrid.Wasm`** — netstandard2.0 browser/WebAssembly adapter with no server:
  `WasmUrlBuilder` (client hash routes like `#/detail?id=5`), `WasmQueryStringReader` (browser
  URL), `WasmGridResponse` (in-memory), and `WasmGridRenderer` mirroring `MvcGridRenderer`
  (`GetBasePageHtml`/`RenderData`/`RenderExport`/`GetClientScript`) by running `GridEngine` in-process.
- **`tst/MVCGrid.Example.Common`** — netstandard2.0 shared demo lib (models, `SampleGrids`,
  `DemoCatalog`, `DemoToolbar`, `CodeSnippets`, `StaticPages`) used by all three example hosts.
- **`tst/MVCGrid.Example.MvcWeb`** — net48 classic MVC host; builds only with VS MSBuild.
- **`tst/MVCGrid.Example.AspNetCore`** — net8 Core host, same-looking site.
- **`tst/MVCGrid.Example.Wasm`** — net10 WebAssembly SPA; **it is the GitHub Pages site** (docs +
  live demos, published by `.github/workflows/pages.yml`). There is no `docs/` site.

No test project exists, and there is no CLI test runner or lint step.

## Build & run

CPM is on (see the workspace CLAUDE.md). Repo-specific: `MVCGrid.MvcWeb` pins
`Microsoft.AspNet.Mvc` with `VersionOverride="3.0.20105.1"` — it compiles against **MVC 3.0** so
one adapter DLL works on MVC 3/4/5; hosts bind up to their own version via `bindingRedirect` (the
example runs 5.3.0, the central version).

```
# Libraries build with the dotnet CLI:
dotnet build src/MVCGrid.Core/MVCGrid.Core.csproj
dotnet build src/MVCGrid.MvcWeb/MVCGrid.MvcWeb.csproj

# The FULL solution needs Visual Studio's MSBuild: the classic example imports
# Microsoft.WebApplication.targets, which the dotnet CLI lacks (MSB4019). Locate it with
#   vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\amd64\MSBuild.exe
msbuild MVCGrid.sln -t:Build -restore

# WASM app (once: dotnet workload install wasm-tools)
dotnet publish tst/MVCGrid.Example.Wasm/MVCGrid.Example.Wasm.csproj -c Release
```

To verify a change, demo grids are defined once in `tst/MVCGrid.Example.Common/SampleGrids.cs` and
shown by every host; the quickest loop is the WASM app. CSV/tab export exercises the
`IGridResponse` path and column URL expressions exercise `IMvcGridUrlBuilder`. Per-host run
instructions are in `.claude/rules/examples.md`.

## The core/adapter split and its abstractions

netstandard2.0 cannot reference `System.Web.Mvc`. **When editing the core, never reach for
`System.Web`** — add/extend an abstraction and implement it in each adapter.

- **`IMvcGridUrlBuilder`** replaces `UrlHelper`; exposed as `GridContext.UrlHelper` and
  `TemplateModel.Url` (names kept, so `c.UrlHelper.Action("detail","demo", new { id })` compiles
  unchanged). Classic impl `MvcUrlBuilder`.
- **`IGridResponse`** replaces `HttpResponse` in `IMVCGridRenderingEngine.PrepareResponse` —
  deliberately just `ContentType` + `AddHeader`. Classic-only `Clear()`/`BufferOutput=false` are
  applied by `MVCGridHandler.HandleTable` around the engine call. Classic impl `MvcGridResponse`;
  custom engines implement `PrepareResponse(IGridResponse)`.
- **`IQueryStringReader`** replaces `HttpRequest` in `QueryStringParser.ParseOptions`. Classic impl
  `MvcQueryStringReader`.
- **Auth:** `GridContext.User` is a BCL `IPrincipal` set by the adapter; the core checks
  `User.Identity.IsAuthenticated`. No `HttpContext` in the core.
- **`GridContext.HandlerPath`** is set by the adapter — the handler base path is a host concern.
- Keep the core dependency-free: engine registrations use `RenderingEngineCollection`/
  `RenderingEngineSetting` (not `System.Configuration.ProviderSettings`); encoding uses
  `System.Net.WebUtility` + hand-rolled `HtmlUtility.JavaScriptStringEncode`. Internals are visible
  to the three adapters (`Properties/InternalsVisibleTo.cs`), so they reach `QueryStringParser`,
  `MVCGridHtmlGenerator` and `GridContext`'s internal setter without widening the public API.

## Request flow

Grids are **defined once at startup, keyed by string name** in a static table, then looked up per
request; all data requests hit one shared endpoint (no per-grid controller).

- **Definition:** `MVCGridBuilder<T>` builds a `GridDefinition<T>` stored in `MVCGridDefinitionTable`.
  It implements non-generic `IMVCGridDefinition` and derives from `GridDefinitionBase` (internal
  `GetData` runs the column value expressions, `Func<T, GridContext, string>` per `GridColumn<T>`).
- **Page load:** `@Html.MVCGrid(name)` → `HtmlExtensions.MVCGrid` → `MvcGridHtmlRenderer.GetBasePageHtml`
  → core `MVCGridHtmlGenerator.GenerateBasePageHtml` (HTML shell + JS-populated placeholder). With
  `PreloadData` + `QueryOnPageLoad`, the first page is rendered inline via `GridEngine.GenerateModel`,
  skipping the first AJAX round-trip.
- **Data requests:** `MVCGrid.js` calls `MVCGridHandler.axd` (`Web/MVCGridHandler.cs`, default; also
  serves JS/PNG/GIF) or `MVCGridController` (`/mvcgrid/grid`, for `RenderingMode.Controller`). Both:
  `GetDefinitionInterface(name)` → `QueryStringParser.ParseOptions` → `GridContextUtility.Create`
  (sets `UrlHelper`, `User`, `HandlerPath`) → `GridEngine.CheckAuthorization` → `RetrieveData` →
  `RenderingModel` → rendering engine.
- **Rendering modes** (`WithRenderingMode`): `RenderingEngine` (default; `BootstrapRenderingEngine`,
  `CsvRenderingEngine` for export; engines registered by name with `AddRenderingEngine` and picked
  per request via the `engine` query param) or `Controller` (Razor partial at `ViewPath`, see the
  `CustomRazorView*` demos).
- **Templating:** `WithValueTemplate("...{Model.Prop}...{Value}...")`, processed per cell by an
  `IMVCGridTemplatingEngine` (default `SimpleTemplatingEngine`).

## Key conventions

- **`RetrieveData` owns all data access** and must honor `context.QueryOptions`; **when paging is
  enabled it must set `QueryResult.TotalRecords`** or `GetData` throws. Helpers:
  `GetLimitOffset()`, `GetLimitRowcount()`, `GetSortColumnData<T>()`, `GetFilterString(col)`,
  `GetAdditionalQueryOptionString(name)`, `GetPageParameterString(name)`.
- **Features are two-level:** sorting/filtering on at grid level (`WithSorting`/`WithFiltering`)
  *and* per column. Grid-level sorting needs a default sort column or `Add` throws.
- **Query-string params are prefixed per grid** (`WithQueryStringPrefix`). Reserved suffixes:
  `page`, `sort`, `dir`, `engine`, `pagesize`, `cols`; page parameters use `_pp_`. Additional
  query-option names may not collide with these or with column names (validated in
  `MVCGridDefinitionTable.Add`).
- **Fluent column API** is the modern path; `GridDefaults`/`ColumnDefaults` feed the builder ctor.
  The legacy non-fluent `GridColumn<T>` path still works (`NonFluentUsageExample`).
- **`GridRegistration`** (adapter): subclass, implement `RegisterGrids()`; `RegisterAllGrids()`
  discovers subclasses across referenced assemblies by reflection.
- **Error detail:** `MVCGridShowErrorDetail` appSetting (false → `ErrorMessageHtml`; true → the
  exception), read by `ConfigUtility`; the core gets the resolved behavior, never reads config.
- **`WithRenderingEngine(Type)` is obsolete** — use `AddRenderingEngine(name, type)` +
  `WithDefaultRenderingEngineName`.

## Consumer setup

**Classic MVC** (mirrored in `tst/MVCGrid.Example.MvcWeb`):
1. `Web.config` handler: `<add name="MVCGridHandler" verb="*" path="MVCGridHandler.axd" type="MVCGrid.Web.MVCGridHandler, MVCGrid.MvcWeb"/>`
   — assembly `MVCGrid.MvcWeb`, but the type namespace is still `MVCGrid.Web`.
2. After jQuery: `<script src="~/MVCGridHandler.axd/script.js"></script>`.
3. At `Application_Start`: your `MVCGridConfig.RegisterGrids()` **and** `GridRegistration.RegisterAllGrids()`.
4. In a view: `@Html.MVCGrid("GridName")`.

**ASP.NET Core:** `builder.Services.AddMVCGrid(o => { o.HandlerPath = "/mvcgrid"; });` then
`app.MapMVCGrid();`; register grids into `MVCGridDefinitionTable` at startup; in `_Layout`, after
jQuery, `<script src="/mvcgrid/script.js"></script>`; render with `@Html.MVCGrid("GridName")`
(`@using MVCGrid.AspNetCore`).

## Where the rest lives

- `.claude/rules/examples.md` — the example hosts, the shared demo lib, editing `SampleGrids.cs`,
  LibMan client libraries, the netstandard-in-Razor quirk, bindingRedirect sync, IIS Express.
- `.claude/rules/packaging.md` — NuGet packages, versioning, the publish workflow.
