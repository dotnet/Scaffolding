# Runnable scaffolding baselines

## Input baselines

`Inputs\<framework>\<template>` holds checked-in snapshots of basic ASP.NET Core and Blazor project templates targeting each represented major .NET version. Scaffolders share these starting projects. Tests copy an input before modifying it; never scaffold into the checked-in project. Register each input in `Inputs\input-baselines.json` and list its template, arguments, and generating SDK version here.

From this directory, update all existing inputs with the currently active SDK (or pass `-SdkVersion` to select another installed SDK):

```powershell
.\UpdateInputBaselines.ps1
```

The script uses the registered template and arguments, skips inputs already generated with that SDK unless `-Force` is specified, and updates the SDK versions in the README. It does not update output baselines: review the complete input diff and reevaluate corresponding outputs whenever an input changes.

To add an input instead, run `.\New-InputBaseline.ps1 -Framework net11.0 -Name EmptyWebApp -Template web` (optionally with `-SdkVersion` or `-TemplateArguments`). It generates the checked-in input, registers it, and prints the README row to add. Use `New-InputCandidate.ps1` to inspect a template without changing any checked-in inputs.

## Output baselines

`<scaffolder>\<framework>\<template>` contains runnable expected output for a representative default path. Baseline tests build and compare the generated app with this expected project; focused integration tests cover configuration variations.

To add coverage, reuse or add a shared input, check in a runnable expected project, and call `ScaffolderBaselineRunner.RunAsync` with the scaffolder directory, framework, input template, scaffolder-specific refresh variable, and callback that runs the scaffolder on the copied input. The runner handles setup, builds, comparison, and cleanup.

Baseline tests live under `test\dotnet-scaffolding\dotnet-scaffold.Tests\AspNet\Integration`; their shared runner is `dotnet-scaffold.Tests\Helpers\ScaffolderBaselineRunner.cs`. A scaffolder that needs additional input files can supply the runner's `prepareInput` callback without changing the checked-in template.

Edit and run the expected project before updating a scaffolder, then run its baseline test. If a generated-output refresh is deliberately needed, set that test's update variable to `1`, run only the intended test case, and review the entire output diff before accepting it. Never refresh just to make a failing comparison pass; clear the variable and rerun the test normally.

## Build isolation

`Directory.Build.props`, `Directory.Build.targets`, and `Directory.Packages.props` apply to both input and output projects, keeping repository build targets and central package settings out of these standalone apps.
