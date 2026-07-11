# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

MVCGrid.Net is a server-side data grid library for **ASP.NET MVC 5 on .NET Framework 4.8** (classic `System.Web` / `System.Web.Mvc`, not ASP.NET Core). It renders Bootstrap-styled HTML tables with AJAX paging, sorting, filtering, column visibility, and CSV export. The distributable is the `MVCGrid` class library, published as a NuGet package. This is a fork of the original mvcgrid.net, maintained by filipe-silva.

## Projects (`MVCGrid.sln`)

- **MVCGrid/** — the library itself. This is what ships. No test project exists.
- **MVCGridExample/** — an MVC web app that is both the demo site and the de-facto integration test surface. Every feature has a demo grid registered in `App_Start/MVCGridConfig.cs` and a matching view under `Views/Demo/`.
- **MVCGrid.RazorTemplates/** — optional separate NuGet package providing a Razor-based templating/rendering engine (`RazorTemplatingEngine`, `RazorRenderingEngine`). Depends on RazorEngine.
- **docs/** — a Jekyll site published to GitHub Pages (`https://filipe-silva.github.io/MVCGrid.Net`). The `pages` branch is where docs/layout work happens; hand-written HTML, not generated from code.

## Build & run

There is no CLI test runner or lint step — this is a Visual Studio / MSBuild solution.

```
# Restore + build (use VS 2013+ or MSBuild; project uses old-style .csproj + packages.config)
msbuild MVCGrid.sln /p:Configuration=Debug
nuget restore MVCGrid.sln    # packages restore from .nuget/ (RestorePackages=true)
```

To verify a change, run **MVCGridExample** in IIS Express from Visual Studio and exercise the relevant demo grid (each feature = one grid in `MVCGridConfig.cs` + one page under `/Demo`). There are no automated tests; the example app is how behavior is checked.

## Consumer setup (how the library is wired into a host app)

Three things a host app must do — mirrored in MVCGridExample:
1. Register the HTTP handler in `Web.config`: `<add name="MVCGridHandler" verb="*" path="MVCGridHandler.axd" type="MVCGrid.Web.MVCGridHandler, MVCGrid"/>`. This serves the embedded `MVCGrid.js` and image resources, and is the AJAX endpoint.
2. Include the script after jQuery: `<script src="~/MVCGridHandler.axd/script.js"></script>`.
3. Register grids at `Application_Start` (see `Global.asax.cs`): call your own `MVCGridConfig.RegisterGrids()` **and** `GridRegistration.RegisterAllGrids()`.
4. Render in a view: `@Html.MVCGrid("GridName")`.

The NuGet package automates 1–2 via `NuGetFiles/web.config.transform` + `Install.ps1`, and drops a starter `MVCGridConfig.cs.pp`.

## Architecture — request flow

Grids are **defined once at startup, keyed by string name** in a static table, then looked up per request. There is no per-grid controller; all data requests hit one shared endpoint.

**Definition (startup):**
`MVCGridBuilder<T>` (fluent API in `Models/MVCGridBuilder.cs`) builds a `GridDefinition<T>` (`Models/GridDefinition.cs`), which is stored in the static `MVCGridDefinitionTable` (`Web/MVCGridDefinitionTable.cs`) under its name. `GridDefinition<T>` implements the non-generic `IMVCGridDefinition` interface (used everywhere the type param isn't known) and derives from `GridDefinitionBase` (which exposes the internal `GetData` that actually runs column value expressions). Column value logic lives in `Func<T, GridContext, string>` expressions on each `GridColumn<T>`.

**Rendering the container (page load):** `@Html.MVCGrid(name)` → `HtmlExtensions.MVCGrid` → `GridEngine.GetBasePageHtml`. Emits the outer HTML shell + a JS-populated placeholder. If `PreloadData` + `QueryOnPageLoad` are set, it also renders the first page of data inline so the initial AJAX round-trip is skipped.

**Data requests (AJAX):** the browser (`MVCGrid.js`) calls back into one of two entry points that do the same thing:
- `Web/MVCGridHandler.cs` (`MVCGridHandler.axd`) — the default `IHttpHandler` path; also serves static JS/PNG/GIF resources embedded in the assembly.
- `Web/MVCGridController.cs` (`/mvcgrid/grid`) — used when `RenderingMode.Controller` renders through a Razor view.

Both: read the grid name → `MVCGridDefinitionTable.GetDefinitionInterface(name)` → `QueryStringParser.ParseOptions` builds `QueryOptions` → `GridContextUtility.Create` builds `GridContext` → `GridEngine.CheckAuthorization` → `GridEngine` runs the definition's `RetrieveData` func, wraps rows into a `RenderingModel`, and hands it to a rendering engine.

**Two rendering modes** (`RenderingMode` enum, selected via `WithRenderingMode`):
- `RenderingEngine` (default) — an `IMVCGridRenderingEngine` writes HTML directly. Default impl is `Rendering/BootstrapRenderingEngine.cs`; `Rendering/CsvRenderingEngine.cs` handles export. Engines are registered by name (`AddRenderingEngine`) and chosen per-request via the `engine` query-string param, enabling alternate exports.
- `Controller` — renders through a Razor partial view at `ViewPath` (see the `CustomRazorView*` demos).

**Templating:** columns can use `WithValueTemplate("...{Model.Prop}...{Value}...")` instead of a value expression. Templates are processed per-cell by an `IMVCGridTemplatingEngine` (default `Templating/SimpleTemplatingEngine.cs`; RazorTemplates project supplies a Razor one).

## Key conventions

- **`RetrieveData` owns all data access.** The library never queries anything itself — the callback (using EF, an IoC-resolved repo, raw SQL, whatever) must honor `context.QueryOptions` for sorting/paging/filtering and, **when paging is enabled, must set `QueryResult.TotalRecords`** or `GetData` throws. Helpers: `options.GetLimitOffset()`, `GetLimitRowcount()`, `GetSortColumnData<T>()`, `GetFilterString(col)`, `GetAdditionalQueryOptionString(name)`, `GetPageParameterString(name)`.
- **Enabling a feature is two-level:** e.g. sorting/filtering must be turned on at the grid level (`WithSorting`/`WithFiltering`) *and* per column. Grid-level sorting also requires a default sort column or `Add` throws.
- **Query-string params are prefixed per grid** (`WithQueryStringPrefix`) so multiple grids can coexist on one page. Reserved suffixes (see `QueryStringParser`): `page`, `sort`, `dir`, `engine`, `pagesize`, `cols`; page parameters use the `_pp_` prefix. Additional query-option names may not collide with these or with column names — validated in `MVCGridDefinitionTable.Add`.
- **`GridColumnListBuilder` fluent column API** (`.Add("Name").WithHeaderText(...).WithValueExpression(...)`) is the modern path; `GridDefaults`/`ColumnDefaults` supply reusable defaults passed to the builder ctor. There is also a legacy non-fluent path (construct `GridColumn<T>` directly) still supported — see `NonFluentUsageExample`.
- **`GridRegistration`** is an alternative to a central config: subclass it, implement `RegisterGrids()`, and `RegisterAllGrids()` discovers all subclasses across referenced assemblies via reflection at startup.
- **Error detail** is controlled by the `MVCGridShowErrorDetail` appSetting (default false → shows `ErrorMessageHtml`; true → dumps the exception). Read via `ConfigUtility`.
- **`WithRenderingEngine(Type)` is obsolete** — use `AddRenderingEngine(name, type)` + `WithDefaultRenderingEngineName`.

## Editing the example config

`MVCGridConfig.cs` contains a comment `//MVCGridDefinitionTable.Add DO NOT DELETE - Needed for demo code parsing` — the docs/demo pages parse this file's source to display live code snippets, so grid registrations above that marker are shown to users. Keep registrations well-formed and don't remove that marker.
