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
/// project (resolved by <see cref="ValidateIgniteUIBlazorStep"/>), the client project as well.
/// </summary>
internal static class IgniteUIBlazorScaffolderBuilderExtensions
{
    /// <summary>Code modification config for ASP.NET Core hosted Blazor projects (Blazor Web App / Blazor Server).</summary>
    internal const string CodeModificationConfigFileName = "igniteUIBlazorChanges.json";
    /// <summary>Code modification config for Blazor WebAssembly projects (standalone or the Web App client project).</summary>
    internal const string WasmCodeModificationConfigFileName = "igniteUIBlazorWasmChanges.json";

    /// <summary>
    /// Adds a step that installs the IgniteUI.Blazor.Lite and IgniteUI.Blazor.GridLite NuGet packages into the project.
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
            step.Packages = GetPackages();
        });
    }

    /// <summary>
    /// Adds a step that installs the IgniteUI.Blazor.Lite and IgniteUI.Blazor.GridLite NuGet packages into the Blazor
    /// WebAssembly client project of a Blazor Web App. Skipped when the project has no client project.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorWasmAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (string.IsNullOrEmpty(model.ClientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            var settings = GetSettings(config.Context);
            step.ProjectPath = model.ClientProjectPath;
            step.Prerelease = settings.Prerelease;
            step.Packages = GetPackages();
        });
    }

    /// <summary>
    /// Adds a step that registers 'builder.Services.AddIgniteUIBlazor()' in the project's Program.cs.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            var configFileName = model.IsWebAssemblyProject ? WasmCodeModificationConfigFileName : CodeModificationConfigFileName;
            if (!TryConfigureCodeModificationStep(step, config.Context, model.ProjectPath, configFileName, model.ProjectInfo.CodeChangeOptions))
            {
                step.SkipStep = true;
            }
        });
    }

    /// <summary>
    /// Adds a step that registers 'builder.Services.AddIgniteUIBlazor()' in the Program.cs of the Blazor
    /// WebAssembly client project of a Blazor Web App. Skipped when the project has no client project.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorWasmCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (string.IsNullOrEmpty(model.ClientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            if (!TryConfigureCodeModificationStep(step, config.Context, model.ClientProjectPath, WasmCodeModificationConfigFileName, model.ProjectInfo.CodeChangeOptions))
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
    /// WebAssembly client project of a Blazor Web App. Skipped when the project has no client project.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorWasmImportsStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddRazorImportsStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (string.IsNullOrEmpty(model.ClientImportsFilePath))
            {
                step.SkipStep = true;
                return;
            }

            step.ImportsFilePath = model.ClientImportsFilePath;
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
    /// Returns the NuGet packages to install: IgniteUI.Blazor.Lite (core components) and IgniteUI.Blazor.GridLite (grid).
    /// </summary>
    internal static List<Package> GetPackages()
        => [PackageConstants.IgniteUIPackages.IgniteUIBlazorLitePackage, PackageConstants.IgniteUIPackages.IgniteUIBlazorGridLitePackage];

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
