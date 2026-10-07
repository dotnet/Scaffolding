// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
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
    /// <summary>Code modification config for .NET MAUI Blazor Hybrid apps (MauiProgram.cs).</summary>
    internal const string MauiCodeModificationConfigFileName = "igniteUIBlazorMauiChanges.json";
    /// <summary>The file that registers the app's services in ASP.NET Core and Blazor WebAssembly projects.</summary>
    internal const string RegistrationFileName = "Program.cs";
    /// <summary>The file that registers the app's services in a .NET MAUI Blazor Hybrid app.</summary>
    internal const string MauiRegistrationFileName = "MauiProgram.cs";

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
    /// Adds a step that enables Interactive Server support in the Program.cs of a Blazor Web App that configures no
    /// interactivity, using the shared Blazor code modification recipes: 'AddInteractiveServerComponents()' on the
    /// 'AddRazorComponents()' chain and 'AddInteractiveServerRenderMode()' on the 'MapRazorComponents&lt;App&gt;()' chain.
    /// Render modes on &lt;Routes&gt;, &lt;HeadOutlet&gt; and existing pages are not changed. Skipped when the app already
    /// configures Server, WebAssembly or Auto interactivity, or is not a Blazor Web App.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorInteractivityStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<IgniteUICodeModificationStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (!model.AddInteractiveServerSupport)
            {
                step.SkipStep = true;
                return;
            }

            step.CodeModifierConfigJsonText = GetInteractiveServerCodeModificationJson();
            step.ProjectPath = model.ProjectPath;
            step.CodeChangeOptions = [];
            step.TargetFileName = RegistrationFileName;
            step.RequiredCalls = [$"{BlazorCrudHelper.AddInteractiveServerComponentsMethod}(", $"{BlazorCrudHelper.AddInteractiveServerRenderModeMethod}("];
            step.ChangeDescription = "Interactive Server support";
            step.ManualInstructions = "Chain '.AddInteractiveServerComponents()' after 'builder.Services.AddRazorComponents()' and '.AddInteractiveServerRenderMode()' after 'app.MapRazorComponents<App>()'.";
        });
    }

    /// <summary>
    /// Adds a step that registers 'builder.Services.AddIgniteUIBlazor()' in the project's Program.cs
    /// (MauiProgram.cs for a .NET MAUI Blazor Hybrid app). The step fails when the registration cannot be added.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<IgniteUICodeModificationStep>(config =>
        {
            var model = GetModel(config.Context);
            ConfigureServicesStep(config.Step, config.Context, model.ProjectPath, model.CodeModificationConfigPath,
                model.IsMauiBlazorHybridProject ? MauiRegistrationFileName : RegistrationFileName, model.ProjectInfo.CodeChangeOptions);
        });
    }

    /// <summary>
    /// Adds a step that registers 'builder.Services.AddIgniteUIBlazor()' in the Program.cs of the Blazor
    /// WebAssembly client project of a Blazor Web App. Skipped when the project has no client project;
    /// otherwise the step fails when the registration cannot be added.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorWasmCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<IgniteUICodeModificationStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            if (string.IsNullOrEmpty(model.ClientProjectPath) || string.IsNullOrEmpty(model.ClientCodeModificationConfigPath))
            {
                step.SkipStep = true;
                return;
            }

            ConfigureServicesStep(step, config.Context, model.ClientProjectPath, model.ClientCodeModificationConfigPath, RegistrationFileName, model.ProjectInfo.CodeChangeOptions);
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
    /// Adds a step that links the selected Ignite UI theme stylesheet in the project's host page. When no host page
    /// could be resolved, the step fails and reports the &lt;link&gt; to add manually.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorThemeStylesheetStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddIgniteUIThemeStylesheetStep>(config =>
        {
            var step = config.Step;
            var model = GetModel(config.Context);
            step.HostPagePath = model.HostPagePath;
            step.ProjectDirectory = model.BaseOutputPath;
            step.StylesheetPath = model.StylesheetPath;
        });
    }

    /// <summary>
    /// Adds a final step that logs what the developer still has to do once the setup has completed, such as adding an
    /// interactive render mode to the pages that use Ignite UI components. Skipped when there is nothing to report.
    /// </summary>
    public static IScaffoldBuilder WithIgniteUIBlazorGuidanceStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<IgniteUIBlazorGuidanceStep>(config =>
        {
            var model = GetModel(config.Context);
            if (string.IsNullOrEmpty(model.RenderModeGuidance))
            {
                config.Step.SkipStep = true;
                return;
            }

            config.Step.Messages = [model.RenderModeGuidance];
        });
    }

    /// <summary>
    /// Returns the code modification config that enables Interactive Server support, built from the shared Blazor
    /// recipes (<see cref="BlazorCrudHelper.AddInteractiveServerComponentsSnippet"/> and
    /// <see cref="BlazorCrudHelper.AddInteractiveServerRenderModeSnippet"/>). The code modifier skips a change whose
    /// block is already present, so the registrations are never duplicated.
    /// </summary>
    internal static string GetInteractiveServerCodeModificationJson()
        => BlazorCrudHelper.AdditionalCodeModificationJson.Replace(
            "$(CodeChanges)",
            string.Join(",", BlazorCrudHelper.AddInteractiveServerComponentsSnippet, BlazorCrudHelper.AddInteractiveServerRenderModeSnippet));

    /// <summary>
    /// Returns the code modification config that registers the Ignite UI services for the given project type.
    /// </summary>
    internal static string GetCodeModificationConfigFileName(bool isWebAssemblyProject, bool isMauiBlazorHybridProject)
        => isMauiBlazorHybridProject ? MauiCodeModificationConfigFileName
            : isWebAssemblyProject ? WasmCodeModificationConfigFileName
            : CodeModificationConfigFileName;

    /// <summary>
    /// Returns the NuGet packages to install: IgniteUI.Blazor.Lite (core components) and IgniteUI.Blazor.GridLite (grid).
    /// </summary>
    internal static List<Package> GetPackages()
        => [PackageConstants.IgniteUIPackages.IgniteUIBlazorLitePackage, PackageConstants.IgniteUIPackages.IgniteUIBlazorGridLitePackage];

    private static void ConfigureServicesStep(IgniteUICodeModificationStep step, ScaffolderContext context, string projectPath, string codeModificationConfigPath, string registrationFileName, IList<string>? codeChangeOptions)
    {
        step.CodeModifierConfigPath = codeModificationConfigPath;
        step.TargetFileName = registrationFileName;
        step.RequiredCalls = ["AddIgniteUIBlazor("];
        step.ChangeDescription = "'builder.Services.AddIgniteUIBlazor()'";
        step.ManualInstructions = $"Add 'using {IgniteUIBlazorHelper.ControlsNamespace};' and 'builder.Services.AddIgniteUIBlazor();' where the app's services are registered (before the app is built).";
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
