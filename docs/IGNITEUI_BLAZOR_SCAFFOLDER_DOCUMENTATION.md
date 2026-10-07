# Ignite UI for Blazor Scaffolder Documentation

## Table of Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Command Usage](#command-usage)
- [Options Reference](#options-reference)
- [What the Scaffolder Does](#what-the-scaffolder-does)
- [Supported Project Types](#supported-project-types)
- [Render Mode Requirements](#render-mode-requirements)
- [Failures and Incomplete Setup](#failures-and-incomplete-setup)
- [Re-running the Scaffolder](#re-running-the-scaffolder)
- [Troubleshooting](#troubleshooting)
- [Implementation Notes](#implementation-notes)

---

## Overview

The **Ignite UI for Blazor Scaffolder** (`dotnet scaffold aspnet blazor-igniteui`) adds the open-source (MIT licensed) Ignite UI for Blazor components from Infragistics to an **existing** Blazor project. It always installs and configures both component sets:

| Package | Contents |
|---------|----------|
| `IgniteUI.Blazor.Lite` | Core UI components (inputs, buttons, dialogs, combo, ...) |
| `IgniteUI.Blazor.GridLite` | Lightweight data grid (`<IgbGridLite>`) |

If you don't need one of them, remove its package reference after scaffolding (see [Removing a component set](#removing-a-component-set)).

The scaffolder performs the manual setup steps described in the Ignite UI documentation:

1. Installs both NuGet packages.
2. Registers `builder.Services.AddIgniteUIBlazor()` in `Program.cs` (`MauiProgram.cs` for a .NET MAUI Blazor Hybrid app).
3. Adds `@using IgniteUI.Blazor.Controls` to `_Imports.razor`.
4. Links a theme stylesheet in the host page (`Components/App.razor`, `Pages/_Host.cshtml`, `Pages/_Layout.cshtml` or `wwwroot/index.html`).
5. For a Blazor Web App with a WebAssembly client project, repeats steps 1–3 in the client project.
6. For a Blazor Web App without any interactivity, enables Interactive Server support in `Program.cs` (see [Render Mode Requirements](#render-mode-requirements)).

---

## Prerequisites

- .NET SDK 8.0 or later (the packages target `net8.0`, `net9.0` and `net10.0`; `net11.0` projects consume the `net10.0` assets).
- The `dotnet scaffold` tool.
- An existing Blazor project (Blazor Web App, Blazor Server, standalone Blazor WebAssembly or .NET MAUI Blazor Hybrid; see [Supported Project Types](#supported-project-types)).
- For a .NET MAUI Blazor Hybrid app, the .NET MAUI workloads for its target platforms (the packages are installed with a restore).
- No reference to the commercial `IgniteUI.Blazor` package (see [Commercial Ignite UI for Blazor package](#commercial-ignite-ui-for-blazor-package)).
- Network access to your NuGet feeds (the packages are installed with `dotnet add package`).

---

## Command Usage

```bash
# Interactive
dotnet scaffold aspnet blazor-igniteui

# Add Ignite UI with the default light bootstrap theme
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp.csproj

# Add Ignite UI with the dark material theme
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp.csproj --theme material --theme-variant dark

# Add Ignite UI using prerelease package versions
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp.csproj --prerelease

# Blazor Web App with a WebAssembly client project: target the SERVER project.
# The referenced MyBlazorApp.Client project is discovered and updated automatically.
dotnet scaffold aspnet blazor-igniteui --project C:/MyBlazorApp/MyBlazorApp/MyBlazorApp.csproj
```

---

## Options Reference

| Option | Required | Values | Default | Description |
|--------|----------|--------|---------|-------------|
| `--project` | Yes | path to `.csproj` | | The Blazor project to modify. For a Blazor Web App with a `.Client` project, pass the **server** project; the client is updated automatically (see [section 5](#5-blazor-web-app-with-a-webassembly-client-project)). |
| `--theme` | No | `bootstrap`, `material`, `fluent`, `indigo` | the linked theme, otherwise `bootstrap` | Theme stylesheet to link. |
| `--theme-variant` | No | `light`, `dark` | the linked variant, otherwise `light` | Variant of the theme stylesheet. |
| `--prerelease` | No | flag | `false` | Install prerelease package versions. |

When `--theme` or `--theme-variant` is omitted, the theme or variant of the Ignite UI stylesheet already linked in the host page is kept, so a re-run does not change the app's appearance; for an initial setup (no Ignite UI theme linked yet) the defaults `bootstrap` and `light` apply. Each option that is given changes only its own part: `--theme fluent` on an app with the dark material theme links dark fluent. A value that is not supported is rejected with an error that lists the supported values, before any file is changed.

---

## What the Scaffolder Does

### 1. NuGet packages

`IgniteUI.Blazor.Lite` and `IgniteUI.Blazor.GridLite` are added with `dotnet add package`. The packages are not versioned in lockstep with .NET, so the latest stable version is installed (or the latest prerelease with `--prerelease`).

### 2. Service registration (`Program.cs` / `MauiProgram.cs`)

The scaffolder registers the Ignite UI services required by the `IgniteUI.Blazor.Lite` components (the GridLite grid needs no registration of its own):

```csharp
using IgniteUI.Blazor.Controls;
// ...
builder.Services.AddIgniteUIBlazor();
```

- ASP.NET Core hosted projects (Blazor Web App / Blazor Server): inserted before `var app = builder.Build();` using `CodeModificationConfigs/igniteUIBlazorChanges.json`.
- Blazor WebAssembly projects (standalone, or the `.Client` project of a Blazor Web App): inserted before `await builder.Build().RunAsync();` using `CodeModificationConfigs/igniteUIBlazorWasmChanges.json`.
- .NET MAUI Blazor Hybrid apps: inserted in `MauiProgram.CreateMauiApp()` right after `builder.Services.AddMauiBlazorWebView();` using `CodeModificationConfigs/igniteUIBlazorMauiChanges.json`. Anchoring on `AddMauiBlazorWebView()` keeps the registration out of the template's `#if DEBUG` block, which ends just before `return builder.Build();`.

The change is skipped when `AddIgniteUIBlazor` is already present. To trim the initial payload you can later pass module types explicitly, e.g. `builder.Services.AddIgniteUIBlazor(typeof(IgbInputModule), typeof(IgbComboModule));`.

### 3. `_Imports.razor`

`@using IgniteUI.Blazor.Controls` is appended to `Components/_Imports.razor` (Blazor Web App) or the project root `_Imports.razor` (Blazor WebAssembly / Blazor Server). The file is created when it does not exist. The directive is not duplicated when it is already present.

### 4. Theme stylesheet

Exactly one theme stylesheet is linked in the `<head>` of the host page: `_content/IgniteUI.Blazor/themes/{variant}/{theme}.css`. This theme styles both the core components and the GridLite grid.

The link is added with the shared code modification recipe `igniteUIBlazorThemeChanges.json`, which covers `Components/App.razor`, `Pages/_Host.cshtml`, `Pages/_Layout.cshtml` and `wwwroot/index.html`. The scaffolder analyzes the host page and passes the values into the recipe. The `<link>` is inserted right before `</head>`, indented one level deeper than `</head>` and using the page's line endings. When the host page is a `.razor` file that already uses the fingerprinted asset collection (`@Assets["..."]`, .NET 9+), the new link uses the same syntax:

```razor
<link rel="stylesheet" href="@Assets["_content/IgniteUI.Blazor/themes/light/bootstrap.css"]" />
```

Otherwise:

```html
<link href="_content/IgniteUI.Blazor/themes/light/bootstrap.css" rel="stylesheet" />
```

If an Ignite UI theme is already linked (including a GridLite-only one), only its path is swapped for the requested theme, keeping the existing link syntax, so the application never loads two themes at once. A theme that is already linked is left unchanged.

### 5. Blazor Web App with a WebAssembly client project

**Always target the server project**, i.e. the project that references `MyApp.Client`. While validating the options, before any file is changed, the scaffolder evaluates the server project's `ProjectReference` items with MSBuild and evaluates each referenced project. A referenced project that uses the `Microsoft.NET.Sdk.BlazorWebAssembly` SDK is the Blazor WebAssembly client. The packages, the service registration and the `@using` directive are applied to it as well (using `igniteUIBlazorWasmChanges.json` for its `Program.cs`). The host page only exists in the server project, so the stylesheet is linked once, in the server host page. The tool logs `Found Blazor WebAssembly client project '...'; it will be configured as well.` when a client was found.

Discovery reads the project files only, so neither project has to be restored or built first. A server project that references no WebAssembly project (Blazor Server, or a Blazor Web App without a `.Client` project) is configured on its own.

The scaffolder stops with an error, without changing any file, when:

- a referenced project does not exist (`Referenced project '...' was not found`): fix the `ProjectReference` path;
- the server project or a referenced project cannot be evaluated, for example because of a missing SDK or import: the error includes the MSBuild message;
- more than one referenced project uses the Blazor WebAssembly SDK (`Multiple referenced projects use the Microsoft.NET.Sdk.BlazorWebAssembly SDK: ...`): the server project must reference exactly one client, which matches the `dotnet new blazor -int WebAssembly` / `-int Auto` layout.

Passing the `.Client` project instead treats it as a standalone WebAssembly app: only the client is updated, and no host page is found because a Web App client project has no `wwwroot/index.html`.

---

## Supported Project Types

| Project type | Host page | `_Imports.razor` | Program.cs config |
|--------------|-----------|------------------|-------------------|
| Blazor Web App (server project, .NET 8+) | `Components/App.razor` | `Components/_Imports.razor` | `igniteUIBlazorChanges.json` |
| Blazor Web App `.Client` project (updated automatically when the **server** project is targeted) | n/a | `_Imports.razor` | `igniteUIBlazorWasmChanges.json` |
| Standalone Blazor WebAssembly | `wwwroot/index.html` | `_Imports.razor` | `igniteUIBlazorWasmChanges.json` |
| Blazor Server (.NET 7 and earlier layouts) | `Pages/_Layout.cshtml` or `Pages/_Host.cshtml` | `_Imports.razor` | `igniteUIBlazorChanges.json` |
| .NET MAUI Blazor Hybrid (`MauiProgram.cs` instead of `Program.cs`) | `wwwroot/index.html` | `Components/_Imports.razor` | `igniteUIBlazorMauiChanges.json` |

Before changing anything, the scaffolder evaluates the project with MSBuild (SDKs, imports such as `Directory.Build.props`, conditions and globs; no restore is needed) and accepts it when it is:

- a standalone Blazor WebAssembly app: the project uses the `Microsoft.NET.Sdk.BlazorWebAssembly` SDK; or
- a Blazor Web App or Blazor Server app: the project uses the `Microsoft.NET.Sdk.Web` SDK and either includes at least one `.razor` component or references a Blazor WebAssembly client project; or
- a .NET MAUI Blazor Hybrid app: the project sets `UseMaui` to `true`, uses the Razor SDK (`Microsoft.NET.Sdk.Razor`) and includes at least one `.razor` component.

A multi-targeted project, such as a MAUI app targeting `net10.0-android;net10.0-ios;...`, is also evaluated for each of its target frameworks, because MSBuild adds default items such as `.razor` files only per target framework. These evaluations run out of process with `dotnet msbuild`, so evaluating a MAUI app does not need its workloads installed; installing the packages afterwards does.

The check does not depend on the template layout: components can live in any folder, including folders outside the project that are added with a `<Content Include="...razor" />` item. The layout only determines which host page and `_Imports.razor` are updated (the table above); when no known host page exists, the theme stylesheet cannot be linked automatically and the run ends as incomplete (see [Failures and Incomplete Setup](#failures-and-incomplete-setup)).

Other projects are rejected with an error that says why, for example a Razor Pages or MVC app (no Razor components), a .NET MAUI app without Razor components, a Razor class library or a console app. Blazor Hybrid apps hosted in WPF or Windows Forms (`BlazorWebView` without .NET MAUI) are not supported; follow the manual steps in the Ignite UI documentation for those.

### Commercial Ignite UI for Blazor package

The commercial `IgniteUI.Blazor` package (and its `IgniteUI.Blazor.Trial` edition) already contains every `IgniteUI.Blazor.Lite` component, in the same `IgniteUI.Blazor.Controls` namespace. Adding `IgniteUI.Blazor.Lite` next to it would introduce duplicate components, so the scaffolder stops with an error, without changing any file, when the target project or its Blazor WebAssembly client project references either package.

The check uses the evaluated `PackageReference` items of each project, so references declared in imported files (for example `Directory.Build.props`) or behind conditions are found too; the error names the file that declares the reference. Conditions are evaluated with the default configuration (`Debug`). References that only reach the project transitively, through another project or package, are not checked.

To keep the commercial package, set it up by following the Ignite UI for Blazor documentation instead of running this scaffolder. To switch to the MIT-licensed packages, remove the commercial `PackageReference` and re-run the scaffolder.

---

## Render Mode Requirements

Ignite UI components need an **interactive** render mode; static server-side rendering renders nothing usable. The scaffolder configures the hosting support a Blazor Web App needs, based on the registrations in its `Program.cs` (analyzed with the project's semantic model):

- **Server, WebAssembly or Auto already configured** (`.AddInteractiveServerComponents()` and/or `.AddInteractiveWebAssemblyComponents()`): the configuration is kept as is, and the Ignite UI setup is completed for it (including the `.Client` project for WebAssembly and Auto).
- **No interactivity configured**: Interactive Server support is enabled with the shared Blazor code modification recipes, by chaining `.AddInteractiveServerComponents()` onto `builder.Services.AddRazorComponents()` and `.AddInteractiveServerRenderMode()` onto `app.MapRazorComponents<App>()`. Neither call is added twice. If the app does not call `app.MapRazorComponents<App>()`, the scaffolder stops before making changes and explains what to add.

Blazor Server apps (`AddServerSideBlazor()`), standalone Blazor WebAssembly apps and .NET MAUI Blazor Hybrid apps are always interactive and keep their hosting model.

Enabling interactive hosting support does not make any page interactive: the scaffolder never changes the render modes on `<Routes>` or `<HeadOutlet>` or on existing pages, so static SSR pages stay static. Because the scaffolder does not generate pages, it ends with guidance for the pages that use Ignite UI components, suggesting only the render modes the app is configured for: for example `@rendermode InteractiveServer` when Server support was added or is configured, `@rendermode InteractiveWebAssembly` for WebAssembly (such pages belong in the client project), and `@rendermode InteractiveAuto` only when both Server and WebAssembly are configured. No guidance is shown when `App.razor` or `Routes.razor` already declares a global render mode.

---

## Failures and Incomplete Setup

Every setup step is required, so the scaffolder reports success (exit code `0`) only when Ignite UI is fully set up. Otherwise it logs an error that says what is left to do and exits with a non-zero code:

- **Validation** (before any file is changed): an invalid option, an unsupported project, a client discovery problem, a commercial Ignite UI package, a `Program.cs` whose Blazor registrations cannot be analyzed, a Blazor Web App without interactivity that does not call `app.MapRazorComponents<App>()`, or a missing code modification configuration (`Unable to find the code modification configuration ...`; reinstall the tool) stops the scaffolder without changes.
- **Package installation**: when `dotnet add package` fails, the scaffolder stops at that step (`Failed to add package ...`) and makes no further changes.
- **Interactive Server support**: when `.AddInteractiveServerComponents()` or `.AddInteractiveServerRenderMode()` cannot be added, the scaffolder stops with `Ignite UI for Blazor setup is incomplete: Interactive Server support could not be added ...` and the calls to chain manually.
- **Service registration**: when `builder.Services.AddIgniteUIBlazor()` cannot be added because `Program.cs` (or `MauiProgram.cs`) has a shape the scaffolder does not recognize, the scaffolder stops with `Ignite UI for Blazor setup is incomplete: 'builder.Services.AddIgniteUIBlazor()' could not be added ...`. Add `using IgniteUI.Blazor.Controls;` and `builder.Services.AddIgniteUIBlazor();` where the app's services are registered, then re-run the scaffolder to complete `_Imports.razor` and the theme stylesheet.
- **`_Imports.razor`**: when the file cannot be written, the scaffolder stops and prints the `@using` line to add.
- **Theme stylesheet** (the last step): when no known host page with a `</head>` element exists, or the link cannot be added, every other change has been applied and the scaffolder ends with `Ignite UI for Blazor setup is incomplete: ...` and the `<link>` line to add to the `<head>` of the page that hosts your Blazor app.

Changes made before a failing step are kept. Because the scaffolder is idempotent, fixing the cause and re-running it completes the remaining steps without duplicating the earlier ones.

---

## Re-running the Scaffolder

The scaffolder is idempotent:

- Packages already referenced are left as they are (`dotnet add package` updates the version at most).
- `AddIgniteUIBlazor` and `@using IgniteUI.Blazor.Controls` are never duplicated.
- Re-running without `--theme` / `--theme-variant` keeps the linked theme and variant (the scaffolder logs `Keeping the Ignite UI theme ... already linked`); passing either option swaps only that part of the linked theme.
- A GridLite-only theme link (`_content/IgniteUI.Blazor.GridLite/themes/...`), for example one added by an earlier version of the scaffolder, is replaced with the `IgniteUI.Blazor` theme.

### Removing a component set

To keep only one component set, remove the other package after scaffolding:

- Without the grid: `dotnet remove package IgniteUI.Blazor.GridLite`.
- Without the core components: `dotnet remove package IgniteUI.Blazor.Lite`, remove `builder.Services.AddIgniteUIBlazor();` from `Program.cs` (or `MauiProgram.cs`), and change the theme link to the GridLite-only theme, `_content/IgniteUI.Blazor.GridLite/themes/{variant}/{theme}.css`.

For a Blazor Web App with a WebAssembly client project, apply the same changes to the client project, except the theme link, which is only in the server host page. Re-running the scaffolder installs both component sets again.

---

## Troubleshooting

| Symptom | Cause / fix |
|---------|-------------|
| `Ignite UI for Blazor setup is incomplete: no host page ... was found` (or `... has no </head> element`), followed by a `<link>` line | None of the known host pages (`Components/App.razor`, `Pages/_Host.cshtml`, `Pages/_Layout.cshtml`, `wwwroot/index.html`) contains a `</head>` element. All other changes were applied; add the printed `<link>` to the `<head>` of the page that hosts your Blazor app. |
| Components render but are unstyled | The theme stylesheet is missing or the path is wrong. Check the `<link>` in the host page and that the package restore succeeded. |
| Components render nothing in a Blazor Web App | The page that uses them has no interactive render mode. Add the render mode suggested at the end of the scaffolder output, e.g. `@rendermode InteractiveServer` (see [Render Mode Requirements](#render-mode-requirements)). |
| `... configures none and does not map its components with 'app.MapRazorComponents<App>()'` | The Blazor Web App registers Razor components without interactivity and without `app.MapRazorComponents<App>()`, so Interactive Server support cannot be added automatically. Nothing was changed; add the calls named in the message and re-run the scaffolder. |
| `Unable to analyze Blazor registrations in '...'` or `Unable to resolve Blazor registration '...'` | The scaffolder could not determine the app's interactivity from `Program.cs`, so it made no changes rather than risk a duplicate or wrong registration. Follow the message (check the SDK, references and imports, run `dotnet build` for diagnostics) and re-run the scaffolder. |
| `Failed to add package 'IgniteUI.Blazor.Lite' ...` (or `GridLite`) | `dotnet add package` failed. The scaffolder stopped at this step without making further changes. Verify network access and that your `NuGet.config` can reach nuget.org (or a mirror), then re-run the scaffolder. Use `--prerelease` to allow prerelease versions. |
| `error: There are no versions available for the package 'IgniteUI.Blazor.Lite'` (or `GridLite`) followed by `Failed.` | The project's `NuGet.config` only lists feeds that do not carry third-party packages (for example curated Microsoft mirror feeds). The scaffolder stops at the package step without making further changes; add nuget.org (or your organization's mirror of it) as a package source and re-run the scaffolder. |
| `Ignite UI for Blazor setup is incomplete: 'builder.Services.AddIgniteUIBlazor()' could not be added to Program.cs` (or `MauiProgram.cs`) | The registration is inserted before `var app = builder.Build();` (hosted) or `await builder.Build().RunAsync();` (WebAssembly), or after `builder.Services.AddMauiBlazorWebView();` in `MauiProgram.CreateMauiApp()` (MAUI Blazor Hybrid). For other bootstrapping shapes, add `using IgniteUI.Blazor.Controls;` and `builder.Services.AddIgniteUIBlazor();` manually and re-run the scaffolder to complete the remaining steps. |
| `Unable to find the code modification configuration '...'` | The tool installation is missing a file the scaffolder needs. Nothing was changed; reinstall the `dotnet scaffold` tool and re-run it. |
| The `.Client` project of a Blazor Web App was not updated | `--project` pointed at the client instead of the server project, or the server project has no `ProjectReference` to the client. Target the server project and re-run the scaffolder (it is idempotent). The log should then contain `Found Blazor WebAssembly client project`. |
| `... is not a supported Blazor app` | The project is not a Blazor Web App, Blazor Server, standalone Blazor WebAssembly or .NET MAUI Blazor Hybrid app (see [Supported Project Types](#supported-project-types)). Pass the Blazor app project to `--project`; for a Blazor Web App with a `.Client` project, pass the server project. |
| `... references the commercial IgniteUI.Blazor package` (or `IgniteUI.Blazor.Trial`) | See [Commercial Ignite UI for Blazor package](#commercial-ignite-ui-for-blazor-package). |
| `Argument '...' not recognized. Must be one of: ...` for `--theme` or `--theme-variant` | Use one of the listed values, or omit the option to use the default. |
| `Unable to resolve the Blazor WebAssembly client project ...` or `Unable to evaluate ...` | Client discovery failed before any change was made: a referenced project is missing, a project could not be evaluated, or the server references more than one Blazor WebAssembly project. Follow the guidance in the message (see [section 5](#5-blazor-web-app-with-a-webassembly-client-project)) and re-run the scaffolder. |

---

## Implementation Notes

Source locations (see [CONTRIBUTING.md](../CONTRIBUTING.md) for the general layout):

- Registration: `src/dotnet-scaffolding/dotnet-scaffold/AspNet/AspNetCommandService.cs` (`blazor-igniteui`)
- Options / strings: `AspNet/Commands/AspNetOptions.cs`, `AspNet/Commands/AspnetStrings.cs`, `AspNet/Common/Constants.cs`
- Packages: `AspNet/Common/PackageConstants.cs` (`IgniteUIPackages`)
- Steps: `AspNet/ScaffoldSteps/ValidateIgniteUIBlazorStep.cs`, `IgniteUICodeModificationStep.cs` (the shared `WrappedCodeModificationStep` plus a check that the required calls were added; used for the Interactive Server support and the service registration), `AddRazorImportsStep.cs`, `IgniteUIBlazorGuidanceStep.cs` (plus the shared `WrappedAddPackagesStep`)
- Interactivity: `AspNet/Helpers/BlazorInteractivityAnalyzer.cs` (shared with the Identity scaffolder) and the shared recipes in `AspNet/Helpers/BlazorCrudHelper.cs` (`AddInteractiveServerComponentsSnippet`, `AddInteractiveServerRenderModeSnippet`)
- WebAssembly client discovery: `AspNet/Helpers/BlazorWebAssemblyClientProjectResolver.cs` (shared with the Identity scaffolder), called from `ValidateIgniteUIBlazorStep`
- Supported-project and commercial-package checks: `AspNet/Helpers/IgniteUIBlazorProjectInspector.cs`, called from `ValidateIgniteUIBlazorStep`, using `MSBuildProjectService.TryGetEvaluatedProperties` / `TryGetEvaluatedItems`
- Builder extensions: `AspNet/Extensions/IgniteUIBlazorScaffolderBuilderExtensions.cs`
- Helper / model / settings: `AspNet/Helpers/IgniteUIBlazorHelper.cs`, `AspNet/Models/IgniteUIBlazorModel.cs`, `AspNet/ScaffoldSteps/Settings/IgniteUIBlazorSettings.cs`
- Code modification configs: `AspNet/Templates/{tfm}/CodeModificationConfigs/igniteUIBlazorChanges.json`, `igniteUIBlazorWasmChanges.json`, `igniteUIBlazorMauiChanges.json` and the theme recipe `igniteUIBlazorThemeChanges.json` (its values come from `IgniteUIBlazorHelper.GetThemeRecipeInputs`) for `net8.0`, `net9.0`, `net10.0` and `net11.0`
- Tests: `test/dotnet-scaffolding/dotnet-scaffold.Tests/AspNet/**/*IgniteUI*`, `AspNet/ScaffoldSteps/AddRazorImportsStepTests.cs` and `AspNet/Integration/Blazor/BlazorIgniteUI*IntegrationTests.cs`
