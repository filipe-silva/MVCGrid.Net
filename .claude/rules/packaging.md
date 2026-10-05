---
paths:
  - ".github/workflows/**"
  - "src/**/*.csproj"
  - "Directory.Build.props"
---

# NuGet packaging and publishing

The four libraries ship via **SDK-style `dotnet pack`** (no hand-written `.nuspec`). Shared
metadata (version, authors, MIT license expression, repo URL, README, tags) lives in the root
`Directory.Build.props`; per-package `PackageId`/`Title`/`Description` and the README pack-include
live in each `.csproj`.

| Project | Package | Notes |
|---|---|---|
| `src/MVCGrid.Core` | `MVCGrid` | netstandard2.0, zero deps; embeds JS/images |
| `src/MVCGrid.MvcWeb` | `MVCGrid.MvcWeb` | net48; deps on `MVCGrid` + `Microsoft.AspNet.Mvc 3.0.20105.1`, auto-emitted from the Project/PackageReference |
| `src/MVCGrid.AspNetCore` | `MVCGrid.AspNetCore` | net8; dep on `MVCGrid`, framework-ref `Microsoft.AspNetCore.App` |
| `src/MVCGrid.Wasm` | `MVCGrid.Wasm` | netstandard2.0; dep on `MVCGrid` only |

Everything under `tst/` sets `<IsPackable>false</IsPackable>`. Pack one library with e.g.
`dotnet pack src/MVCGrid.Core/MVCGrid.Core.csproj -c Release -o <out>` (`.nupkg` + `.snupkg`).
The old content-file install model (`NuGetFiles/`, `.pp` transforms, `Install.ps1`,
`MVCGrid.nuspec`) and the legacy `.nuget/` folder are gone — consumers do the manual host setup in
CLAUDE.md.

## Publishing (`.github/workflows/publish.yml`)

- Runs on `windows-latest` because net48 needs the .NET Framework targeting pack.
- The package version is **always `<Version>` in `Directory.Build.props`** — bump it there before
  releasing. The release tag is only a marker; it does not set the version.
- Trigger: **publish a GitHub Release** (packs all four and pushes `.nupkg`+`.snupkg` to
  nuget.org), or run it manually (`workflow_dispatch`) with the `dry_run` toggle to pack without
  pushing.
- **Trusted Publishing (OIDC)**, no stored API key: the job has `id-token: write`, and
  `NuGet/login@v1` exchanges a short-lived GitHub OIDC token for a temporary nuget.org key just
  before push. One-time setup: (1) a Trusted Publishing policy on nuget.org for this repo; (2) repo
  secret **`NUGET_USER`** = the nuget.org username (profile name, not email).

`.github/workflows/pages.yml` (the GitHub Pages site) is described in `examples.md`.
