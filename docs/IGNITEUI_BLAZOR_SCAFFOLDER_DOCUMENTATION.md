# Ignite UI for Blazor Scaffolder Documentation

## Table of Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Command Usage](#command-usage)
- [Options Reference](#options-reference)
- [What the Scaffolder Does](#what-the-scaffolder-does)
- [Supported Project Types](#supported-project-types)
- [Render Mode Requirements](#render-mode-requirements)
- [Re-running the Scaffolder](#re-running-the-scaffolder)
- [Troubleshooting](#troubleshooting)
- [Implementation Notes](#implementation-notes)

---

## Overview

The **Ignite UI for Blazor Scaffolder** (`dotnet scaffold aspnet blazor-igniteui`) adds the open-source
(MIT licensed) Ignite UI for Blazor packages from Infragistics to an **existing** Blazor project:

| Package | Contents | Service registration |
|---------|----------|----------------------|
| `IgniteUI.Blazor.Lite` | Core UI components (inputs, buttons, dialogs, combo, ...) | `builder.Services.AddIgniteUIBlazor()` |
| `IgniteUI.Blazor.GridLite` | Lightweight data grid (`<IgbGridLite>`) | None required |

The scaffolder performs the manual setup steps described in the Ignite UI documentation:

1. Installs the selected NuGet package(s).
2. Registers `builder.Services.AddIgniteUIBlazor()` in `Program.cs` (only when `IgniteUI.Blazor.Lite` is added).
3. Adds `@using IgniteUI.Blazor.Controls` to `_Imports.razor`.
4. Links a theme stylesheet in the host page (`Components/App.razor`, `Pages/_Host.cshtml`,
   `Pages/_Layout.cshtml` or `wwwroot/index.html`).
5. For a Blazor Web App with a WebAssembly client project, repeats steps 1–3 in the client project.

---

## Prerequisites

- .NET SDK 8.0 or later (the packages target `net8.0`, `net9.0` and `net10.0`; `net11.0` projects consume the `net10.0` assets).
- The `dotnet scaffold` tool.
- An existing Blazor project (Blazor Web App, Blazor Server or standalone Blazor WebAssembly).
- Network access to your NuGet feeds (the packages are installed with `dotnet add package`).

---

## Command Usage

```bash
# Interactive
dotnet scaffold aspnet blazor-igniteui

# Add both packages with the default light bootstrap theme
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp.csproj --package All

# Add only the grid with the dark material theme
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp.csproj --package GridLite --theme material --theme-variant dark

# Add the core components using prerelease package versions
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp.csproj --package Lite --prerelease

# Blazor Web App with a WebAssembly client project: target the SERVER project.
# The referenced MyBlazorApp.Client project is detected and updated automatically.
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp/MyBlazorApp.csproj --package All
```

---

## Options Reference

| Option | Required | Values | Default | Description |
|--------|----------|--------|---------|-------------|
| `--project` | Yes | path to `.csproj` | | The Blazor project to modify. For a Blazor Web App with a `.Client` project, pass the **server** project; the client is updated automatically (see [section 5](#5-blazor-web-app-with-a-webassembly-client-project)). |
| `--package` | Yes | `Lite`, `GridLite`, `All` | | Which Ignite UI package(s) to add. |
| `--theme` | No | `bootstrap`, `material`, `fluent`, `indigo` | `bootstrap` | Theme stylesheet to link. |
| `--theme-variant` | No | `light`, `dark` | `light` | Variant of the theme stylesheet. |
| `--prerelease` | No | flag | `false` | Install prerelease package versions. |

Unknown `--theme` / `--theme-variant` values fall back to the defaults with an informational message.

---

## What the Scaffolder Does

### 1. NuGet packages

`IgniteUI.Blazor.Lite` and/or `IgniteUI.Blazor.GridLite` are added with `dotnet add package`. The packages are not
versioned in lockstep with .NET, so the latest stable version is installed (or the latest prerelease with `--prerelease`).

### 2. Service registration (`Program.cs`)

Only `IgniteUI.Blazor.Lite` needs service registration. When it is selected, the scaffolder adds:

```csharp
using IgniteUI.Blazor.Controls;
// ...
builder.Services.AddIgniteUIBlazor();
```

- ASP.NET Core hosted projects (Blazor Web App / Blazor Server): inserted before `var app = builder.Build();`
  using `CodeModificationConfigs/igniteUIBlazorChanges.json`.
- Blazor WebAssembly projects (standalone, or the `.Client` project of a Blazor Web App): inserted before
  `await builder.Build().RunAsync();` using `CodeModificationConfigs/igniteUIBlazorWasmChanges.json`.

The change is skipped when `AddIgniteUIBlazor` is already present. To trim the initial payload you can later pass
module types explicitly, e.g. `builder.Services.AddIgniteUIBlazor(typeof(IgbInputModule), typeof(IgbComboModule));`.

### 3. `_Imports.razor`

`@using IgniteUI.Blazor.Controls` is appended to `Components/_Imports.razor` (Blazor Web App) or the project root
`_Imports.razor` (Blazor WebAssembly / Blazor Server). The file is created when it does not exist. The directive is
not duplicated when it is already present.

### 4. Theme stylesheet

Exactly one theme stylesheet is linked in the `<head>` of the host page:

| Scenario | Stylesheet |
|----------|------------|
| `IgniteUI.Blazor.Lite` is added or already referenced | `_content/IgniteUI.Blazor/themes/{variant}/{theme}.css` |
| Only `IgniteUI.Blazor.GridLite` is used | `_content/IgniteUI.Blazor.GridLite/css/themes/{variant}/{theme}.css` |

The `<link>` is inserted after the last existing `<link>` in `<head>` and matches its indentation. When the host page
is a `.razor` file that already uses the fingerprinted asset collection (`@Assets["..."]`, .NET 9+), the new link uses
the same syntax:

```razor
<link rel="stylesheet" href="@Assets["_content/IgniteUI.Blazor/themes/light/bootstrap.css"]" />
```

Otherwise:

```html
<link href="_content/IgniteUI.Blazor/themes/light/bootstrap.css" rel="stylesheet" />
```

If a different Ignite UI theme is already linked, its `href` is swapped for the requested theme so the application never
loads two themes at once.

### 5. Blazor Web App with a WebAssembly client project

**Always target the server project**, i.e. the project that references `MyApp.Client`. The scaffolder runs
`dotnet reference list` on it and `dotnet package list --format json` on every referenced project; a referenced project
whose packages include the implicit `Microsoft.NET.Sdk.WebAssembly` pack is treated as the Blazor WebAssembly client, and
the package(s), the service registration and the `@using` directive are applied to it as well (using
`igniteUIBlazorWasmChanges.json` for its `Program.cs`). The host page only exists in the server project, so the stylesheet
is linked once. The tool logs `Detected Blazor WebAssembly project via package reference: ...` when the client was found.

Requirements and limits:

- The client project must have been restored at least once (any build, or opening the solution in Visual Studio, does
  this). `dotnet package list` fails without `obj/project.assets.json`, and the client is then skipped without an error.
- Passing the `.Client` project instead treats it as a standalone WebAssembly app: only the client is updated, and no host
  page is found because a Web App client project has no `wwwroot/index.html`.
- Only the first referenced WebAssembly project is handled, which matches the `dotnet new blazor -int WebAssembly` /
  `-int Auto` layout.

---

## Supported Project Types

| Project type | Host page | `_Imports.razor` | Program.cs config |
|--------------|-----------|------------------|-------------------|
| Blazor Web App (server project, .NET 8+) | `Components/App.razor` | `Components/_Imports.razor` | `igniteUIBlazorChanges.json` |
| Blazor Web App `.Client` project (updated automatically when the **server** project is targeted) | n/a | `_Imports.razor` | `igniteUIBlazorWasmChanges.json` |
| Standalone Blazor WebAssembly | `wwwroot/index.html` | `_Imports.razor` | `igniteUIBlazorWasmChanges.json` |
| Blazor Server (.NET 7 and earlier layouts) | `Pages/_Layout.cshtml` or `Pages/_Host.cshtml` | `_Imports.razor` | `igniteUIBlazorChanges.json` |

Blazor Hybrid (MAUI) projects are not targeted by this scaffolder; follow the manual steps in the Ignite UI documentation.

---

## Render Mode Requirements

Ignite UI components need an **interactive** render mode; static server-side rendering renders nothing usable. The
scaffolder does not change render modes (that is an application architecture decision) but it does inspect Blazor Web
Apps and logs guidance:

- A **warning** when `Program.cs` registers neither `.AddInteractiveServerComponents()` nor
  `.AddInteractiveWebAssemblyComponents()`.
- An **informational message** when no `@rendermode` is declared on `<Routes />` (`App.razor`) or in `Routes.razor`,
  reminding you to add `@rendermode InteractiveServer` (or `InteractiveWebAssembly` / `InteractiveAuto`) to the pages
  that use Ignite UI, or to set `<Routes @rendermode="InteractiveAuto" />` globally.

---

## Re-running the Scaffolder

The scaffolder is idempotent:

- Packages already referenced are left as they are (`dotnet add package` updates the version at most).
- `AddIgniteUIBlazor` and `@using IgniteUI.Blazor.Controls` are never duplicated.
- Re-running with a different `--theme` / `--theme-variant` swaps the linked theme.
- Running `--package GridLite` on a project that already references `IgniteUI.Blazor.Lite` keeps the
  `IgniteUI.Blazor` theme stylesheet (the GridLite-only stylesheet must not be used alongside other Ignite UI components).

---

## Troubleshooting

| Symptom | Cause / fix |
|---------|-------------|
| `Could not find a host page ... Add the following line to the <head> of your host page manually` | None of the known host pages contains a `</head>` element. Add the printed `<link>` to your host page. |
| Components render but are unstyled | The theme stylesheet is missing or the path is wrong. Check the `<link>` in the host page and that the package restore succeeded. |
| Components render nothing in a Blazor Web App | Add an interactive render mode (see [Render Mode Requirements](#render-mode-requirements)). |
| `dotnet add package` fails | Verify network access and that your `NuGet.config` can reach nuget.org (or a mirror). Use `--prerelease` to allow prerelease versions. |
| `error: There are no versions available for the package 'IgniteUI.Blazor.Lite'` (or `GridLite`) followed by `Failed.` | The project's `NuGet.config` only lists feeds that do not carry third-party packages (for example curated Microsoft mirror feeds). The scaffolder continues with the remaining steps; add nuget.org (or your organization's mirror of it) as a package source and re-run the scaffolder, or run `dotnet add package IgniteUI.Blazor.Lite` manually. |
| `Program.cs` was not modified | The step only runs for `--package Lite` / `All`. It inserts before `var app = builder.Build();` (hosted) or `await builder.Build().RunAsync();` (WebAssembly); other bootstrapping shapes must be edited manually. |
| The `.Client` project of a Blazor Web App was not updated | Either `--project` pointed at the client instead of the server project, or the client had never been restored so `dotnet package list` could not inspect it. Target the server project, build or restore the solution once, and re-run the scaffolder (it is idempotent). The log should then contain `Detected Blazor WebAssembly project via package reference`. |

---

## Implementation Notes

Source locations (see [CONTRIBUTING.md](../CONTRIBUTING.md) for the general layout):

- Registration: `src/dotnet-scaffolding/dotnet-scaffold/AspNet/AspNetCommandService.cs` (`blazor-igniteui`)
- Options / strings: `AspNet/Commands/AspNetOptions.cs`, `AspNet/Commands/AspnetStrings.cs`, `AspNet/Common/Constants.cs`
- Packages: `AspNet/Common/PackageConstants.cs` (`IgniteUIPackages`)
- Steps: `AspNet/ScaffoldSteps/ValidateIgniteUIBlazorStep.cs`, `AddRazorImportsStep.cs`, `AddIgniteUIThemeStylesheetStep.cs`
  (plus the shared `DetectBlazorWasmStep`, `WrappedAddPackagesStep` and `WrappedCodeModificationStep`)
- Builder extensions: `AspNet/Extensions/IgniteUIBlazorScaffolderBuilderExtensions.cs`
- Helper / model / settings: `AspNet/Helpers/IgniteUIBlazorHelper.cs`, `AspNet/Models/IgniteUIBlazorModel.cs`,
  `AspNet/ScaffoldSteps/Settings/IgniteUIBlazorSettings.cs`
- Code modification configs: `AspNet/Templates/{tfm}/CodeModificationConfigs/igniteUIBlazorChanges.json` and
  `igniteUIBlazorWasmChanges.json` for `net8.0`, `net9.0`, `net10.0` and `net11.0`
- Tests: `test/dotnet-scaffolding/dotnet-scaffold.Tests/AspNet/**/*IgniteUI*`, `AspNet/ScaffoldSteps/AddRazorImportsStepTests.cs`
  and `AspNet/Integration/Blazor/BlazorIgniteUI*IntegrationTests.cs`
