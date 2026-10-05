---
paths:
  - "tst/MVCGrid.Example.*/**"
  - "**/libman.json"
---

# Example hosts (`tst/`)

Three hosts show the same demo site; the demo domain, grids and page bodies are defined once in
`MVCGrid.Example.Common`.

## MVCGrid.Example.Common (netstandard2.0, `IsPackable=false`)

Referenced by all three hosts.

- `Person` model (8-prop superset); in-memory `PeopleData`/`PeopleRepository` (200 deterministic
  rows, no DB); `JobRepo`/`TestItemRepository` (static `Lazy` caches, no `System.Web`).
- Custom engines `CustomHtmlRenderingEngine`, `TabDelimitedRenderingEngine`.
- **`SampleGrids.RegisterAll()`** registers ~28 portable RenderingEngine-mode grids, with the same
  grid *names* the classic views use.
- **`DemoCatalog`** — nav/toolbar metadata + JSON; drives the WASM showcase.
- **`DemoToolbar.BuildHtml(...)`** — the demo toolbar (page size / Fields dropdown / Export /
  Search), defined once. The classic `_MVCGridToolbar.cshtml`, the Core views and the WASM app all
  render it, so the toolbars are byte-identical.
- **`CodeSnippets.For(gridName)`** — slices a grid's registration block out of `SampleGrids.cs`,
  which the csproj embeds into this assembly.
- **`StaticPages`** — Home, Getting Started, Download, Support, Documentation, Detail and the
  generic catalog Demo page bodies. Markup lives in **`Pages/*.html`** (embedded), loaded once into
  a `ConcurrentDictionary` cache, with `{{TOKEN}}` placeholders substituted via `string.Replace` —
  deliberately **not** `string.Format`, which would require doubling every `{`/`}` in the embedded
  C# snippets. Hosts render them via `Html.Raw`/Interop, parameterized by the host's demos link, an
  optional host note, and a `Func<string,string>` that renders a grid by name for pages embedding
  live grids, so the pages stay identical everywhere.
- The Documentation page's four API-reference grids (`GridDefinition`/`GridColumn`/`QueryOptions`/
  `ClientSide`) are registered by `SampleGrids` and fed by `DocumentationRepository` (parses the
  embedded `documentation.csv`).

### Editing the shared grid catalog

- Every host shows a grid's registration **source** as a live snippet by slicing the
  `MVCGridDefinitionTable.Add("<name>"...)` block out of `SampleGrids.cs`, so **keep each
  registration well-formed with balanced parentheses**. The classic `CodeSnippetHelper` is just a
  delegate to the shared `CodeSnippets`.
- Grid *names* are referenced by the classic `Views/Demo/*.cshtml` — don't rename a grid without
  updating its view.
- Add new demos to `DemoCatalog` too, or they won't appear in the WASM nav or the Core host's
  `/demo/{key}` sidebar. The `DemoInfo` flags (`Search`/`PageSize`/`Export`/`ColumnVisibility` +
  `SearchOption`) decide which controls `DemoToolbar` renders.

## MVCGrid.Example.MvcWeb (net48, was `MVCGridExample`)

- Assembly `MVCGrid.Example.MvcWeb`, but internal C# namespaces are still the legacy
  `MVCGrid.Web.*`/`MVCGridExample` — folder/assembly renamed, namespaces left to avoid a large ripple.
- `MVCGridConfig` calls `SampleGrids.RegisterAll()` **plus** the host-only `RenderingMode.Controller`
  Razor grids (`CustomRazorView`, `CustomRazorView2`); `ContainerGrid` is registered in
  `DemoController`. Host-only grids aren't in `SampleGrids.cs`, so they render an empty snippet (as
  before). The old `Content/MVCGridConfig.txt` post-build copy is gone; `CodeSnippetHelper` is kept
  as a thin delegate for view compatibility.
- Builds only with **VS MSBuild** (see CLAUDE.md).
- **netstandard-in-Razor quirk:** runtime-compiled views that use netstandard2.0 types (e.g.
  `_MVCGridToolbar.cshtml` calling `DemoToolbar`) fail with CS0012 unless `Web.config` registers the
  netstandard facade under `<compilation><assemblies>` (`<add assembly="netstandard, Version=2.0.0.0, ..."/>`
  — already done here). Any net48 consumer calling netstandard types from its views needs the same line.
- **When bumping its packages, keep `Web.config` `bindingRedirect`s in sync** (System.Web.Mvc,
  Newtonsoft.Json, WebGrease, Microsoft.Web.Infrastructure, Ninject + Ninject.Web.Common/WebHost) —
  a mismatch is a runtime 500, not a build error. Check versions with
  `[System.Reflection.AssemblyName]::GetAssemblyName('bin\<dll>').Version`.
- Run headlessly: build the solution with VS MSBuild, then
  `iisexpress.exe /path:<project dir> /port:50255 /clr:v4.0` (IIS Express lives under
  `%ProgramFiles%\IIS Express`; 50255 is the port VS uses, from the `.csproj.user`). Or run it in
  IIS Express from Visual Studio.

## MVCGrid.Example.AspNetCore (net8, was `MVCGrid.AspNetCoreExample`)

- Styled as the same MvcWeb-look site: `_Layout.cshtml` clones the classic layout (fixed
  `navbar-inverse` with Home / Getting Started / Demos / Documentation / Download / Support,
  `.container.body-content`, `nuget-badge` style); libraries from `wwwroot/lib/` via
  `app.UseStaticFiles()`.
- `Program.cs` calls `SampleGrids.RegisterAll()`. `Home/Index` has the live `TestGrid` + toolbar
  in a panel + snippets. One `DemoController.Show` action serves **every catalog demo at
  `/demo/{key}`** (body = shared `Pages/Demo.html` via `StaticPages.DemoHtml`: `_DemoLayout`-shaped
  sidebar from `DemoCatalog` + toolbar + grid + code panels); `demo/detail/{id}` is the URL-column
  target. `GettingStarted`/`Documentation`/`Download`/`Support` controllers keep URLs matching the
  classic host.
- `dotnet run --project tst/MVCGrid.Example.AspNetCore`, then hit `/`, `/demo/{key}`,
  `/mvcgrid?Name=Filtering`, `/mvcgrid/script.js`, `/mvcgrid?Name=Filtering&engine=export`.

## MVCGrid.Example.Wasm (net10, was `MVCGrid.WasmExample`) — the GitHub Pages site

- `Microsoft.NET.Sdk.WebAssembly`, `[JSExport]` interop. The runtime is left resident — **do not
  call `dotnet.run()`**, which would exit it.
- It is **both the demo and the docs** — there is no `docs/` site (the old Jekyll site was
  retired). `.github/workflows/pages.yml` (setup .NET 10 + `wasm-tools`, `dotnet publish`, deploy
  `wwwroot`) publishes it to `https://filipe-silva.github.io/MVCGrid.Net/`; repo Pages source must
  be "GitHub Actions".
- `Interop` exposes `GetBasePageHtml`/`RenderData`/`RenderExport`/`GetClientScript`/
  `GetCatalogJson`/`GetCodeSnippet`/`GetToolbarHtml`.
- `wwwroot/index.html` replicates `_Layout.cshtml` + `Site.css` of the classic host.
  `wwwroot/main.js` boots .NET once and hash-routes pages mirroring the MvcWeb views: home (live
  `TestGrid` + toolbar), `#/demo/<key>` from the shared `Pages/Demo.html` (same body as the Core
  host, with "Inside MVCGridConfig.cs"/View panels), plus gettingstarted/documentation/download/
  support. After each mount it calls `SyntaxHighlighter.highlight()` for `brush: csharp|js` panels.
- Per route it injects a grid via the shared engine and runs the **unchanged** `MVCGrid.js`: a
  jQuery `ajaxTransport` answers its one AJAX call from WASM, a capture-phase handler turns exports
  into Blob downloads, and a delegated `stopPropagation` on `#content` keeps the Fields dropdown open.
- **`PublishTrimmed=false` is required** — engines are instantiated by string type name via `Activator`.
- Needs `dotnet workload install wasm-tools`. Quickest verification loop: publish, serve the
  published `wwwroot` statically, drive it in a browser (or headless Chrome via puppeteer-core —
  that's how the SPA is regression-tested).

## Client libraries via LibMan (no CDN, nothing vendored)

- Each host has a `libman.json` (provider cdnjs) plus a `Microsoft.Web.LibraryManager.Build`
  PackageReference, restoring at build time into a uniform **`lib/<library>`** convention: `lib/`
  at the project root for the classic host, `wwwroot/lib/` for Core and WASM.
- All three restore **jquery 3.6.1**, **twitter-bootstrap 3.4.1** (css/js/fonts — glyphicons) and
  **SyntaxHighlighter 3.0.83** (cdnjs keeps `styles/`+`scripts/` subfolders →
  `lib/syntaxhighlighter/styles/...`). The classic host also restores **modernizr 2.6.2,
  respond.js 1.4.2 and jquery.blockUI**.
- The classic `BundleConfig` bundles from those paths; its CSS bundle uses `CssRewriteUrlTransform`
  so the `../fonts/` glyphicon urls survive optimized bundling. Core/WASM reference `lib/...`
  directly, so the published WASM site is fully self-contained (verified: restored files survive a
  clean `dotnet publish` into the Pages artifact).
- Only the demo-local `jquery.sortable.min.js` and `script-debug.js` stay committed (no cdnjs
  equivalent / project-specific). All `lib/` destinations are **gitignored**; the first build needs
  network access to cdnjs.
