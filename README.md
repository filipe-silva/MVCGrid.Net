# MVCGrid.Net

A server-side data grid for ASP.NET with Bootstrap styling: AJAX paging, sorting, filtering, column visibility, CSV export, back-button/state support, and graceful degradation. Easily configurable — you tell it how to fetch your data and it handles all the AJAX.

This is a maintained fork of the original [MVCGrid.Net](https://github.com/bigjoe714/MVCGrid.Net) by bigjoe714, restructured into a portable core plus per-host adapters so the same grid works on **classic ASP.NET MVC**, **ASP.NET Core** — and even **in the browser via .NET WebAssembly**.

**Live demo:** https://filipe-silva.github.io/MVCGrid.Net — the docs site *is* the demo: every sample grid runs the real engine entirely client-side (a WebAssembly host feeds the unchanged `MVCGrid.js`), no server involved.

## Packages

| Package | Target | Use it when |
|---|---|---|
| **[MVCGrid](https://www.nuget.org/packages/MVCGrid)** | netstandard2.0 | The portable, `System.Web`-free core. Pulled in automatically by an adapter; reference directly only to build your own host adapter. |
| **[MVCGrid.MvcWeb](https://www.nuget.org/packages/MVCGrid.MvcWeb)** | net48 | Hosting in **classic ASP.NET MVC 3/4/5**. |
| **[MVCGrid.AspNetCore](https://www.nuget.org/packages/MVCGrid.AspNetCore)** | net8.0 | Hosting in **ASP.NET Core** MVC (RenderingEngine mode). |
| **[MVCGrid.Wasm](https://www.nuget.org/packages/MVCGrid.Wasm)** | netstandard2.0 | Running the grid **in the browser via .NET WebAssembly** — no `System.Web`, no ASP.NET Core, no server at all. `WasmGridRenderer` renders the grid, answers the client script's AJAX requests and exports in-process. Powers the live demo site. |

## Features

* Uses your existing model objects
* Server-side sorting, paging and filtering over AJAX
* Updates the query string so grid state survives back-button navigation
* Column visibility and per-grid query-string prefixes (multiple grids per page)
* Built-in CSV/export via pluggable rendering engines
* Graceful degradation on older browsers

## Quick start

### Classic ASP.NET MVC (`MVCGrid.MvcWeb`)

1. Register the HTTP handler in `Web.config`:
   ```xml
   <add name="MVCGridHandler" verb="*" path="MVCGridHandler.axd"
        type="MVCGrid.Web.MVCGridHandler, MVCGrid.MvcWeb" />
   ```
2. Include the script after jQuery: `<script src="~/MVCGridHandler.axd/script.js"></script>`
3. Register your grids at `Application_Start`.
4. Render: `@Html.MVCGrid("GridName")`.

### ASP.NET Core (`MVCGrid.AspNetCore`)

```csharp
builder.Services.AddMVCGrid(o => { o.HandlerPath = "/mvcgrid"; });
// ... register grids into MVCGridDefinitionTable at startup ...
app.MapMVCGrid();
```

In `_Layout`, after jQuery: `<script src="/mvcgrid/script.js"></script>`, then `@Html.MVCGrid("GridName")` (`@using MVCGrid.AspNetCore`).

### Browser / WebAssembly (`MVCGrid.Wasm`)

In a .NET WebAssembly browser app — no server at all:

```csharp
// ... register grids into MVCGridDefinitionTable at startup ...
var renderer = new WasmGridRenderer(new WasmGridOptions { HandlerPath = "mvcgrid" });

string pageHtml = renderer.GetBasePageHtml("GridName", currentUrl); // the @Html.MVCGrid equivalent
string fragment = renderer.RenderData(requestUrl);   // answers MVCGrid.js's AJAX request in-process
string script   = renderer.GetClientScript();        // the unchanged MVCGrid.js, ready to inject
```

Answer the client script's one AJAX call from a jQuery `ajaxTransport` shim and turn exports into Blob downloads — the live demo site (`tst/MVCGrid.Example.Wasm`) is the reference implementation.

## Documentation

Docs and live samples: https://filipe-silva.github.io/MVCGrid.Net — built from the `MVCGrid.Example.Wasm` app in this repo and deployed to GitHub Pages; the interactive demos there run in-browser via WebAssembly. The same demo catalog also ships as runnable classic MVC and ASP.NET Core example apps under `tst/`.

## License

MIT — see [LICENSE](LICENSE). Original work © 2015 bigjoe714; fork © filipe-silva.
