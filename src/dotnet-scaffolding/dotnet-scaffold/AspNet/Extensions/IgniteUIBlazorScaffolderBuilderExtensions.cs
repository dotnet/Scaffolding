// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Ignite UI for Blazor scaffolding steps.
/// The steps cover the project passed via '--project' and, for Blazor Web Apps with a WebAssembly client
/// project (detected by <see cref="DetectBlazorWasmStep"/>), the client project as well.
/// </summary>
internal static class IgniteUIBlazorScaffolderBuilderExtensions
{
    /// <summary>Code modification config for ASP.NET Core hosted Blazor projects (Blazor Web App / Blazor Server).</summary>
    internal const string CodeModificationConfigFileName = "igniteUIBlazorChanges.json";
    /// <summary>Code modification config for Blazor WebAssembly projects (standalone or the Web App client project).</summary>
    internal const string WasmCodeModificationConfigFileName = "igniteUIBlazorWasmChanges.json";

    /// <summary>
    /// Adds a step that detects whether the project references a Blazor WebAssembly client project.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorDetectBlazorWasmStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<DetectBlazorWasmStep>(config =>
        {
            var model = GetModel(config.Context);
            config.Step.ProjectPath = model.ProjectPath;
            // A failure to enumerate project references must not abort the scaffolder.
            config.Step.ContinueOnError = true;
        });
    }

    /// <summary>
    /// Adds a step that installs the selected Ignite UI NuGet package(s) into the project.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            var settings = GetSettings(config.Context);
            step.ProjectPath = model.ProjectPath;
            step.Prerelease = settings.Prerelease;
            step.Packages = GetPackages(model);
        });
    }

    /// <summary>
    /// Adds a step that installs the selected Ignite UI NuGet package(s) into the Blazor WebAssembly client
    /// project of a Blazor Web App. Skipped when no client project was detected.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorWasmAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            if (!TryGetClientProjectPath(config.Context, out var clientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            var model = GetModel(config.Context);
            var settings = GetSettings(config.Context);
            step.ProjectPath = clientProjectPath;
            step.Prerelease = settings.Prerelease;
            step.Packages = GetPackages(model);
        });
    }

    /// <summary>
    /// Adds a step that registers 'builder.Services.AddIgniteUIBlazor()' in the project's Program.cs.
    /// Skipped when only IgniteUI.Blazor.GridLite is added, since GridLite needs no service registration.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (!model.RequiresServiceRegistration)
            {
                step.SkipStep = true;
                return;
            }

            var configFileName = model.IsWebAssemblyProject ? WasmCodeModificationConfigFileName : CodeModificationConfigFileName;
            if (!TryConfigureCodeModificationStep(step, config.Context, model.ProjectPath, configFileName, model.ProjectInfo.CodeChangeOptions))
            {
                step.SkipStep = true;
            }
        });
    }

    /// <summary>
    /// Adds a step that registers 'builder.Services.AddIgniteUIBlazor()' in the Program.cs of the Blazor
    /// WebAssembly client project of a Blazor Web App. Skipped when no client project was detected or when
    /// only IgniteUI.Blazor.GridLite is added.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorWasmCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (!model.RequiresServiceRegistration || !TryGetClientProjectPath(config.Context, out var clientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            if (!TryConfigureCodeModificationStep(step, config.Context, clientProjectPath, WasmCodeModificationConfigFileName, model.ProjectInfo.CodeChangeOptions))
            {
                step.SkipStep = true;
            }
        });
    }

    /// <summary>
    /// Adds a step that imports the 'IgniteUI.Blazor.Controls' namespace in the project's _Imports.razor.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorImportsStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddRazorImportsStep>(config =>
        {
            var model = GetModel(config.Context);
            config.Step.ImportsFilePath = model.ImportsFilePath;
            config.Step.Namespaces = [IgniteUIBlazorHelper.ControlsNamespace];
        });
    }

    /// <summary>
    /// Adds a step that imports the 'IgniteUI.Blazor.Controls' namespace in the _Imports.razor of the Blazor
    /// WebAssembly client project of a Blazor Web App. Skipped when no client project was detected.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorWasmImportsStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddRazorImportsStep>(config =>
        {
            var step = config.Step;
            if (!TryGetClientProjectPath(config.Context, out var clientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            var clientProjectDirectory = Path.GetDirectoryName(clientProjectPath);
            if (string.IsNullOrEmpty(clientProjectDirectory))
            {
                step.SkipStep = true;
                return;
            }

            step.ImportsFilePath = GetClientImportsFilePath(clientProjectDirectory);
            step.Namespaces = [IgniteUIBlazorHelper.ControlsNamespace];
        });
    }

    /// <summary>
    /// Adds a step that links the selected Ignite UI theme stylesheet in the project's host page.
    /// Skipped when no host page could be resolved (guidance is logged by <see cref="ValidateIgniteUIBlazorStep"/>).
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorThemeStylesheetStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddIgniteUIThemeStylesheetStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (string.IsNullOrEmpty(model.HostPagePath))
            {
                step.SkipStep = true;
                return;
            }

            step.HostPagePath = model.HostPagePath;
            step.StylesheetPath = model.StylesheetPath;
        });
    }

    /// <summary>
    /// Returns the NuGet packages to install for the given model.
    /// </summary>
    internal static List<Package> GetPackages(IgniteUIBlazorModel model)
    {
        List<Package> packages = [];
        if (model.IncludeLite)
        {
            packages.Add(PackageConstants.IgniteUIPackages.IgniteUIBlazorLitePackage);
        }

        if (model.IncludeGridLite)
        {
            packages.Add(PackageConstants.IgniteUIPackages.IgniteUIBlazorGridLitePackage);
        }

        return packages;
    }

    /// <summary>
    /// Resolves the _Imports.razor of a Blazor WebAssembly client project (root '_Imports.razor' by convention,
    /// 'Components/_Imports.razor' when that is what the project uses).
    /// </summary>
    internal static string GetClientImportsFilePath(string clientProjectDirectory)
    {
        var rootImports = Path.Combine(clientProjectDirectory, "_Imports.razor");
        var componentsImports = Path.Combine(clientProjectDirectory, "Components", "_Imports.razor");
        return !File.Exists(rootImports) && File.Exists(componentsImports) ? componentsImports : rootImports;
    }

    private static bool TryConfigureCodeModificationStep(WrappedCodeModificationStep step, ScaffolderContext context, string projectPath, string configFileName, IList<string>? codeChangeOptions)
    {
        string targetFrameworkFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);
        string? codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(configFileName, System.Reflection.Assembly.GetExecutingAssembly(), targetFrameworkFolder);
        if (string.IsNullOrEmpty(codeModificationFilePath))
        {
            return false;
        }

        step.CodeModifierConfigPath = codeModificationFilePath;
        step.ProjectPath = projectPath;
        step.CodeChangeOptions = codeChangeOptions ?? [];
        if (context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj) &&
            codeModifierPropertiesObj is Dictionary<string, string> codeModifierProperties)
        {
            foreach (var kvp in codeModifierProperties)
            {
                step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
            }
        }

        return true;
    }

    private static bool TryGetClientProjectPath(ScaffolderContext context, out string clientProjectPath)
    {
        clientProjectPath = string.Empty;
        if (context.Properties.TryGetValue("IsBlazorWasmProject", out var isBlazorWasm) && isBlazorWasm is true &&
            context.Properties.TryGetValue("BlazorWasmClientProjectPath", out var clientProjectPathObj) &&
            clientProjectPathObj is string path && !string.IsNullOrEmpty(path))
        {
            clientProjectPath = path;
            return true;
        }

        return false;
    }

    private static IgniteUIBlazorModel GetModel(ScaffolderContext context)
    {
        context.Properties.TryGetValue(nameof(IgniteUIBlazorModel), out var modelObj);
        return modelObj as IgniteUIBlazorModel ??
            throw new InvalidOperationException("missing 'IgniteUIBlazorModel' in 'ScaffolderContext.Properties'");
    }

    private static IgniteUIBlazorSettings GetSettings(ScaffolderContext context)
    {
        context.Properties.TryGetValue(nameof(IgniteUIBlazorSettings), out var settingsObj);
        return settingsObj as IgniteUIBlazorSettings ??
            throw new InvalidOperationException("missing 'IgniteUIBlazorSettings' in 'ScaffolderContext.Properties'");
    }
}
