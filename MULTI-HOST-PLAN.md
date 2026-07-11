# Multi-host support plan — ASP.NET Core *and* classic MVC 3/4/5

Goal: one library usable from **ASP.NET Core MVC** and **classic ASP.NET MVC 3/4/5**, sharing a single portable core. This is a planning/checklist doc — not yet implemented (the ASP.NET Core adapter is deferred).

## Where we are now (done)

- [x] Core extracted to **`MVCGrid`** (netstandard2.0), zero `System.Web` dependency, zero NuGet deps. Consumable by net48 *and* net6/8.
- [x] Framework-neutral abstractions in `MVCGrid.Abstractions`: `IMvcGridUrlBuilder`, `IGridResponse`, `IQueryStringReader`; auth via BCL `IPrincipal`; `GridContext.HandlerPath`.
- [x] Classic adapter **`MVCGrid.MvcWeb`** (net48) implements those abstractions and hosts the core in ASP.NET MVC.
- [x] SDK-style projects + Central Package Management (`Directory.Packages.props`).
- [x] Classic adapter compiles against **MVC 3.0** (`VersionOverride` in `MVCGrid.MvcWeb.csproj`) so one DLL serves MVC 3/4/5; hosts bind up via `bindingRedirect`. Central CPM version stays 5.2.2 for `MVCGridExample`.

## Target package layout

```
MVCGrid              (netstandard2.0)   core — shared by both adapters
 ├─ MVCGrid.MvcWeb       (net48)         classic ASP.NET MVC 3/4/5 adapter  [exists]
 └─ MVCGrid.AspNetCore   (net8)          ASP.NET Core MVC adapter           [to build]
```

## Decisions

Recorded:
- Classic adapter targets **MVC 3.0** (widest 3/4/5 compatibility).
- Multi-host = additional thin adapters over the unchanged core (no core rewrite).

Resolved:
- **D1 — `IGridResponse` shape.** RESOLVED via option (a): `ContentType` + `AddHeader` only. See Phase 0.
- **D3 — Where embedded `MVCGrid.js` + images live.** RESOLVED: embedded in the core assembly. See Phase 0.

Open (resolve before/while building the Core adapter):
- **D2 — Core resource serving.** Endpoint routing (`app.MapMVCGrid()`) vs. middleware (`app.UseMVCGrid()`). *Recommend endpoint routing.*
- **D4 — View helper surface on Core.** `@Html.MVCGrid(...)` for parity *(recommended first)*, and/or a `<mvc-grid>` TagHelper (idiomatic, optional).
- **D5 — Include `RenderingMode.Controller` (view-to-string) in the first Core release?** *Recommend deferring* to a follow-up; ship `RenderingEngine` mode first.

## Phase 0 — Shared prep (benefits both adapters, low risk) — DONE

- [x] **D1 (option a):** slimmed `IGridResponse` to `ContentType` + `AddHeader` (dropped `Clear`/`BufferOutput`). Updated `CsvRenderingEngine` and the example's `TabDelimitedRenderingEngine` to drop those calls; `MvcGridResponse` no longer implements them. The classic-only `Clear()`/`BufferOutput=false` now happen once in `MVCGridHandler.HandleTable` around the engine call, so export streaming behavior is preserved without leaking classic-isms into the portable contract.
- [x] **D3:** moved `Scripts/MVCGrid.js` + `Images/*` into `MVCGrid.Core`, embedded there (shared by all adapters). `MVCGridHandler` now loads them from the core assembly (`typeof(GridEngine).Assembly`) and keeps doing the `%%HANDLERPATH%%`/`%%CONTROLLERPATH%%`/`%%ERRORDETAILS%%` substitution. Also removed empty leftover folders in `MVCGrid.MvcWeb`.
- [x] Verify: full `MSBuild MVCGrid.sln -t:Rebuild -restore` → exit 0; resource manifest names confirmed embedded in `MVCGrid.dll` and absent from the adapter DLL. (Runtime render/export smoke-test in VS/IIS Express still recommended.)

## Phase 1 — `MVCGrid.AspNetCore` adapter, RenderingEngine mode (the MVP)

- [ ] New SDK-style project `MVCGrid.AspNetCore` (net8), `ProjectReference` to core, added to the solution + CPM.
- [ ] Implement abstractions:
  - [ ] `IMvcGridUrlBuilder` over `IUrlHelper` (`IUrlHelperFactory` from the current `ActionContext`).
  - [ ] `IQueryStringReader` over `HttpRequest.Query`.
  - [ ] `IGridResponse` over Core `HttpResponse` (per D1).
  - [ ] Populate `GridContext.User` from `HttpContext.User` (`ClaimsPrincipal`), and `HandlerPath` from the configured route base.
- [ ] Data endpoint + resources (D2/D3): `app.MapMVCGrid()` serving the grid AJAX (equivalent of `MVCGridHandler`/`MVCGridController`) and `script.js` + images from the core's embedded resources.
- [ ] `@Html.MVCGrid("name")` `IHtmlHelper` extension → base-page HTML (Core equivalent of `MvcGridHtmlRenderer.GetBasePageHtml`, RenderingEngine path only).
- [ ] Startup: `builder.Services.AddMVCGrid()` + `app.MapMVCGrid()`; grid definitions still register into the static `MVCGridDefinitionTable`.
- [ ] Verify: a minimal net8 MVC sample renders, sorts, pages, filters, and CSV-exports.

## Phase 2 — Core adapter, Controller mode (deferred, D5)

- [ ] Razor view-to-string renderer (`IRazorViewEngine` + `ICompositeViewEngine`, `ViewContext`/`ViewData`/`TempData`) — Core equivalent of the classic view-render helpers.
- [ ] Wire `RenderingMode.Controller` + `ViewPath`/`ContainerViewPath`.

## Phase 3 — Polish (optional)

- [ ] `<mvc-grid>` TagHelper (D4).
- [ ] DI conveniences (options binding for error-detail, etc. — Core reads from `IConfiguration` instead of `ConfigUtility`/appSettings).
- [ ] Packaging: `.nuspec`/package metadata for `MVCGrid`, `MVCGrid.MvcWeb`, `MVCGrid.AspNetCore` (the existing `NuGetFiles/` + `.nuget/` assets are stale and predate the split).

## Verification notes

- `dotnet build` handles `MVCGrid.Core`, `MVCGrid.MvcWeb`, and (once created) `MVCGrid.AspNetCore`.
- The classic **web app** (`MVCGridExample`) still requires **VS2022 MSBuild** (`Microsoft.WebApplication.targets`); `dotnet` can't build it.
- No automated tests exist; sample apps are the verification surface.
