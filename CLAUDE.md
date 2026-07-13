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
- **MVCGrid.AspNetCore/** — the **net8** ASP.NET Core MVC adapter (assembly `MVCGrid.AspNetCore`, `FrameworkReference Microsoft.AspNetCore.App`), references the core. Implements the core abstractions over ASP.NET Core (`IUrlHelper`, `HttpResponse`, `HttpRequest.Query`, `HttpContext.User`) and provides `services.AddMVCGrid()`, `app.MapMVCGrid()` (endpoint routing for the AJAX/export data endpoint + `script.js` + images), and the `@Html.MVCGrid(...)` helper. **RenderingEngine mode only** so far — Controller-mode view rendering and a TagHelper are deferred (see `MULTI-HOST-PLAN.md`). Compile-verified; runtime smoke-test still pending.
- **MVCGrid.Wasm/** — a **netstandard2.0** browser/WebAssembly host adapter (assembly `MVCGrid.Wasm`, `IsPackable=false`), references the core. The in-browser analogue of the ASP.NET Core adapter: `WasmUrlBuilder`/`WasmQueryStringReader`/`WasmGridResponse` implement the three abstractions with no server (URLs → client hash routes like `#/detail?id=5`, query string parsed from the browser URL, response state held in memory), and `WasmGridRenderer` mirrors `MvcGridRenderer` (`GetBasePageHtml`/`RenderData`/`RenderExport`/`GetClientScript`) by running the core `GridEngine` in-process. Added to the core's `InternalsVisibleTo`. Proves the core runs anywhere — no `System.Web`, no ASP.NET Core, no server at all.
- **MVCGrid.Example.Common/** (under `tst/`) — a **netstandard2.0** shared library (`IsPackable=false`) referenced by **all three example hosts**, so the demo domain and grids are defined once. Holds the `Person` model (8-prop superset), the in-memory `PeopleData`/`PeopleRepository` (200 deterministic rows, no DB), `JobRepo`/`TestItemRepository` (static `Lazy` caches — no `System.Web`), the two custom engines (`CustomHtmlRenderingEngine`, `TabDelimitedRenderingEngine`), **`SampleGrids.RegisterAll()`** (registers ~28 portable RenderingEngine-mode grids — the same grid *names* the classic views use), and **`DemoCatalog`** (nav/toolbar metadata + JSON, drives the WASM showcase).
- **MVCGrid.Example.MvcWeb/** (was `MVCGridExample`) — the **net48** classic MVC host example (assembly `MVCGrid.Example.MvcWeb`; note internal C# namespaces are still the legacy `MVCGrid.Web.*`/`MVCGridExample` — folder/assembly renamed, namespaces left to avoid a large ripple). `MVCGridConfig` calls `SampleGrids.RegisterAll()` **plus** the host-only `RenderingMode.Controller` Razor grids (`CustomRazorView`, `CustomRazorView2`, `ContainerGrid`) and the API-doc grids (`DocumentationRepository`). Builds only with **VS MSBuild**. The `Content/MVCGridConfig.txt` code-snippet source is post-build-copied from the shared `SampleGrids.cs`.
- **MVCGrid.Example.AspNetCore/** (was `MVCGrid.AspNetCoreExample`) — a minimal **net8** ASP.NET Core host example; `Program.cs` calls `SampleGrids.RegisterAll()` and renders a shared grid. Builds/runs under the `dotnet` CLI: `dotnet run --project tst/MVCGrid.Example.AspNetCore`.
- **MVCGrid.Example.Wasm/** (was `MVCGrid.WasmExample`) — the **.NET WebAssembly SPA** (`Microsoft.NET.Sdk.WebAssembly`, net10, `[JSExport]` interop; the runtime is left resident — do **not** call `dotnet.run()`, which would exit it) that is **both the demo and the docs, deployed to GitHub Pages as the whole site**. References the shared lib; `Interop` exposes `GetBasePageHtml`/`RenderData`/`RenderExport`/`GetClientScript`/`GetCatalogJson`/`GetCodeSnippet`. `wwwroot/main.js` boots .NET once, builds a sidebar nav + hash router from `DemoCatalog`, and per route injects a grid via the shared engine and runs the **unchanged** `MVCGrid.js` (a jQuery `ajaxTransport` answers its one AJAX call from WASM; a capture-phase handler turns exports into Blob downloads). **`PublishTrimmed=false` is required** (engines are instantiated by string type name via `Activator`). Needs the `wasm-tools` workload (`dotnet workload install wasm-tools`).
- **No `docs/` site.** The old hand-maintained Jekyll site was retired — the WASM app above **is** the site (docs pages + interactive demos), published to `https://filipe-silva.github.io/MVCGrid.Net/` by **`.github/workflows/pages.yml`** (setup .NET 10 + `wasm-tools`, `dotnet publish` the WASM app, deploy its `wwwroot` as the Pages artifact). Requires repo Pages source = "GitHub Actions".

## Build & run

SDK-style projects with **Central Package Management**: `Directory.Packages.props` at the repo root holds every package version (`<PackageVersion>`), and projects use version-less `<PackageReference>`. Exception: `MVCGrid.MvcWeb` pins `Microsoft.AspNet.Mvc` via `VersionOverride="3.0.20105.1"` — it compiles against **MVC 3.0** so one adapter DLL works on ASP.NET MVC 3/4/5 (hosts bind up to their own version via `bindingRedirect`; the example runs it on 5.2.2). There is no CLI test runner or lint step.

```
# The core (netstandard2.0) and the adapter (net48) build/restore with the dotnet CLI:
dotnet build MVCGrid.Core/MVCGrid.Core.csproj
dotnet build MVCGrid.MvcWeb/MVCGrid.MvcWeb.csproj

# The FULL solution — including the MVCGrid.Example.MvcWeb web app — needs Visual Studio 2022's MSBuild.
# The web project imports Microsoft.WebApplication.targets, which the dotnet CLI does not have,
# so `dotnet build tst/MVCGrid.Example.MvcWeb` fails with MSB4019; use VS MSBuild instead:
#   "…/Microsoft Visual Studio/2022/<edition>/MSBuild/Current/Bin/amd64/MSBuild.exe"
msbuild MVCGrid.sln -t:Build -restore

# The WASM app needs the wasm-tools workload once: dotnet workload install wasm-tools
dotnet publish tst/MVCGrid.Example.Wasm/MVCGrid.Example.Wasm.csproj -c Release
```

To verify a change: the demo grids are defined once in **`MVCGrid.Example.Common/SampleGrids.cs`** and shown by all hosts. Quickest loop is the WASM app — `dotnet publish tst/MVCGrid.Example.Wasm`, serve the published `wwwroot` statically, and drive it in a browser (or headless Chrome via puppeteer-core — that's how the SPA is regression-tested). For the classic host, run **MVCGrid.Example.MvcWeb** in IIS Express from Visual Studio. For the Core host, `dotnet run --project tst/MVCGrid.Example.AspNetCore` and hit `/`, `/mvcgrid?Name=Filtering`, `/mvcgrid/script.js`, `/mvcgrid?Name=Filtering&engine=export`. CSV/tab export exercises the `IGridResponse` path and column URL expressions exercise `IMvcGridUrlBuilder`.

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

Four things a host app must do — mirrored in MVCGrid.Example.MvcWeb:
1. Register the HTTP handler in `Web.config`: `<add name="MVCGridHandler" verb="*" path="MVCGridHandler.axd" type="MVCGrid.Web.MVCGridHandler, MVCGrid.MvcWeb"/>` (note the assembly is `MVCGrid.MvcWeb`; the handler *type* namespace is still `MVCGrid.Web`). This serves the embedded `MVCGrid.js` + images and is the AJAX endpoint.
2. Include the script after jQuery: `<script src="~/MVCGridHandler.axd/script.js"></script>`.
3. Register grids at `Application_Start` (see `Global.asax.cs`): call your own `MVCGridConfig.RegisterGrids()` **and** `GridRegistration.RegisterAllGrids()`.
4. Render in a view: `@Html.MVCGrid("GridName")`.

### ASP.NET Core host

Reference `MVCGrid.AspNetCore` and in `Program.cs`: `builder.Services.AddMVCGrid(o => { o.HandlerPath = "/mvcgrid"; });`, then `app.MapMVCGrid();`. Register grids into `MVCGridDefinitionTable` at startup (same as classic). In `_Layout`, include the script after jQuery: `<script src="/mvcgrid/script.js"></script>`. Render with `@Html.MVCGrid("GridName")` (`@using MVCGrid.AspNetCore`). RenderingEngine mode only for now.

## NuGet packaging

The three libraries ship as packages via **SDK-style `dotnet pack`** (no hand-written `.nuspec`). Shared metadata (version, authors, MIT license expression, repo URL, README, tags) lives in the repo-root **`Directory.Build.props`**; per-package `PackageId`/`Title`/`Description` and the README pack-include live in each `.csproj`. Current version is **3.0.0**.

- `MVCGrid.Core` → package **`MVCGrid`** (netstandard2.0, zero deps; embeds JS/images).
- `MVCGrid.MvcWeb` → **`MVCGrid.MvcWeb`** (net48; deps on `MVCGrid` + `Microsoft.AspNet.Mvc 3.0.20105.1`, both auto-emitted from the ProjectReference/PackageReference).
- `MVCGrid.AspNetCore` → **`MVCGrid.AspNetCore`** (net8; dep on `MVCGrid`, framework-ref `Microsoft.AspNetCore.App`).

The example apps and the shared `MVCGrid.Example.Common` lib under `tst/` set `<IsPackable>false</IsPackable>`. Pack each shippable library with the `dotnet` CLI, e.g. `dotnet pack src/MVCGrid.Core/MVCGrid.Core.csproj -c Release -o <out>` (produces `.nupkg` + `.snupkg`). The old content-file install model (`NuGetFiles/`, the `.pp` transforms, `Install.ps1`, `MVCGrid.MvcWeb/MVCGrid.nuspec`) and the legacy `.nuget/` folder have been removed — consumers do the manual host setup documented above.

**Publishing** is automated by `.github/workflows/publish.yml` (runs on `windows-latest` since net48 needs the .NET Framework targeting pack). Push a `v*.*.*` tag (the tag drives the package version, overriding `Directory.Build.props`) to pack all three and push `.nupkg`+`.snupkg` to nuget.org, or run it manually (`workflow_dispatch`) with an optional version override and a `dry_run` toggle to pack without pushing. It uses **Trusted Publishing (OIDC)** — no stored API key: the job has `id-token: write` and `NuGet/login@v1` exchanges a short-lived GitHub OIDC token for a temporary nuget.org key just before push. One-time setup: (1) add a Trusted Publishing policy on nuget.org for this repo (2) set the repo secret **`NUGET_USER`** to your nuget.org username (profile name, not email).

## Editing the shared grid catalog

Demo grids live in **`tst/MVCGrid.Example.Common/SampleGrids.cs`** (`SampleGrids.RegisterAll()`), registered by all three hosts. The WASM showcase and the classic host both display each grid's registration **source** as a live code snippet by slicing the `MVCGridDefinitionTable.Add("<name>"...)` block out of `SampleGrids.cs` — so keep each registration well-formed with balanced parentheses (the WASM `CodeSnippets` slicer and the classic `CodeSnippetHelper`, which reads a post-build copy of `SampleGrids.cs`, both depend on it). Grid *names* are also referenced by the classic `Views/Demo/*.cshtml`, so don't rename a grid without updating its view. New demos should also be added to `DemoCatalog` to appear in the WASM app's nav.
