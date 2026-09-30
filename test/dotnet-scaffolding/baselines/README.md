# Runnable scaffolding baselines

## Input baselines

`Inputs\<framework>\<template>` holds checked-in snapshots of basic ASP.NET Core and Blazor project templates targeting each represented major .NET version. Scaffolders share these starting projects. Tests copy an input before modifying it; never scaffold into the checked-in project. Register each input in `Inputs\input-baselines.json` and list its template, arguments, and generating SDK version here.

From this directory, update all existing inputs with the currently active SDK (or pass `-SdkVersion` to select another installed SDK):

```powershell
.\UpdateInputBaselines.ps1
```

The script uses the registered template and arguments, skips inputs already generated with that SDK unless `-Force` is specified, and updates the SDK versions in the README. It does not update output baselines: review the complete input diff and reevaluate corresponding outputs whenever an input changes.

To add an input instead, run `.\New-InputBaseline.ps1 -Framework net11.0 -Name EmptyWebApp -Template web` (optionally with `-SdkVersion` or `-TemplateArguments`). It generates the checked-in input, registers it, and prints the README row to add. Use `New-InputCandidate.ps1` to inspect a template without changing any checked-in inputs.

### Input registry

`Inputs\input-baselines.json` contains an array of entries:

```json
[
  {
    "path": "net11.0/BlazorWebApp",
    "template": "blazor",
    "arguments": ["--name", "BlazorWebApp", "--framework", "net11.0"],
    "sdkVersion": "11.0.100-rc.2.26475.136"
  }
]
```

`path` is the unique project directory relative to `Inputs`, using `/` separators. `template` is the `dotnet new` template name. `arguments` contains individual template argument tokens; omit `--output` and `--no-restore`, which the scripts supply. `sdkVersion` is the exact SDK version used to generate the input. Normally use the generation scripts rather than edit this registry manually.

## Output baselines

`<scaffolder>\<framework>\<template>` contains runnable expected output for a representative default path. Baseline tests build and compare the generated app with this expected project; focused integration tests cover configuration variations.

Comparison includes every file except files under `bin` and `obj` directories. Common source and text formats use line-ending normalization, and `.csproj` comparisons ignore `PackageReference` version values. Other files are compared byte-for-byte; database files and `.vs` files are not excluded.

To add coverage, reuse or add a shared input, check in a runnable expected project, and call `ScaffolderBaselineRunner.RunAsync` with named arguments for the scaffolder directory, framework, input template, and callback that runs the scaffolder on the copied input. The runner handles setup, builds, comparison, and cleanup.

Baseline tests live under `test\dotnet-scaffolding\dotnet-scaffold.Tests\AspNet\Integration`; their shared runner is `dotnet-scaffold.Tests\Helpers\ScaffolderBaselineRunner.cs`. A scaffolder that needs additional input files can supply the runner's `prepareInput` callback without changing the checked-in template.

Update the expected project first and run it to verify the desired behavior, then update the scaffolder to match and run its baseline test. Tests never rewrite checked-in baselines. On failure, the runner reports the retained artifact directory; inspect or selectively copy generated files when useful, and review any baseline changes rather than replacing expected output just to make a failing comparison pass.

## Build isolation

`Directory.Build.props`, `Directory.Build.targets`, and `Directory.Packages.props` apply to both input and output projects, keeping repository build targets and central package settings out of these standalone apps.
