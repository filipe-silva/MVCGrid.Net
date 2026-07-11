# MVCGrid.Net

A server-side data grid for ASP.NET with Bootstrap styling: AJAX paging, sorting, filtering, column visibility, CSV export, back-button/state support, and graceful degradation. Easily configurable — you tell it how to fetch your data and it handles all the AJAX.

This is a maintained fork of the original [MVCGrid.Net](https://github.com/bigjoe714/MVCGrid.Net) by bigjoe714, restructured into a portable core plus per-host adapters so the same grid works on **classic ASP.NET MVC** *and* **ASP.NET Core**.

## Packages

| Package | Target | Use it when |
|---|---|---|
| **[MVCGrid](https://www.nuget.org/packages/MVCGrid)** | netstandard2.0 | The portable, `System.Web`-free core. Pulled in automatically by an adapter; reference directly only to build your own host adapter. |
| **[MVCGrid.MvcWeb](https://www.nuget.org/packages/MVCGrid.MvcWeb)** | net48 | Hosting in **classic ASP.NET MVC 3/4/5**. |
| **[MVCGrid.AspNetCore](https://www.nuget.org/packages/MVCGrid.AspNetCore)** | net8.0 | Hosting in **ASP.NET Core** MVC (RenderingEngine mode). |

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

## Documentation

Docs and live samples: https://filipe-silva.github.io/MVCGrid.Net

## License

MIT — see [LICENSE](LICENSE). Original work © 2015 bigjoe714; fork © filipe-silva.
