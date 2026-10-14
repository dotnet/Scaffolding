// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// Logs what the Syncfusion Blazor Toolkit setup changed. Every statement is
/// derived from facts the earlier steps recorded for each project they touched
/// (the server and, in an Auto layout, the WebAssembly client), so a run that
/// modified any project is never described as a no-op.
/// </summary>
internal class SummarizeSyncfusionBlazorToolkitStep : ScaffoldStep
{
    private const string StylesNote =
        "  - Styles: Syncfusion.Blazor.Toolkit 2.0.0+ ships styles with the assembly. No host stylesheet link was added.";

    private readonly ILogger _logger;

    public SummarizeSyncfusionBlazorToolkitStep(ILogger<SummarizeSyncfusionBlazorToolkitStep> logger)
    {
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) ||
            settingsObj is not SyncfusionBlazorToolkitSettings settings)
        {
            return Task.FromResult(true);
        }

        bool hasClient = !string.IsNullOrEmpty(settings.ClientProjectPath);
        bool importsMissing = !settings.ImportsApplied && settings.ImportsFileSkipped;

        // A no-op means NOTHING changed in ANY targeted project - package, registration or imports.
        bool anyChange = settings.Server.AnyChange || (hasClient && settings.Client.AnyChange);

        if (!anyChange && settings.ServicesApplied && settings.Server.PackageReferenced && !importsMissing)
        {
            _logger.LogInformation(
                "Syncfusion Blazor Toolkit was already configured in {ProjectCount}. The scaffolder made no changes.",
                hasClient ? "the server and client projects" : "this project");
        }
        else
        {
            _logger.LogInformation("Syncfusion Blazor Toolkit setup summary:");
        }

        LogPackage("Package reference", settings.Server);
        LogServices(settings);
        LogImports(settings, importsMissing);

        if (hasClient)
        {
            string client = Path.GetFileName(settings.ClientProjectPath!);
            _logger.LogInformation("  - Client project '{Client}':", client);
            LogPackage("    Package reference", settings.Client);
            _logger.LogInformation(
                settings.Client.ImportsInserted
                    ? "    _Imports.razor: added @using Syncfusion.Blazor.Toolkit."
                    : "    _Imports.razor: @using Syncfusion.Blazor.Toolkit already present (no change).");
            if (settings.Client.ServicesInserted)
            {
                _logger.LogInformation("    Entry point: added AddSyncfusionBlazorToolkit().");
            }
        }

        _logger.LogInformation("  - Hosting: {Guidance}", DescribeHostingGuidance(settings));
        _logger.LogInformation(StylesNote);

        return Task.FromResult(true);
    }

    private void LogPackage(string label, ProjectChanges changes)
    {
        if (changes.PackageVersionBefore is null && changes.PackageVersionAfter is not null)
        {
            _logger.LogInformation("  - {Label}: added Syncfusion.Blazor.Toolkit {Version}.", label, changes.PackageVersionAfter);
        }
        else if (changes.PackageChanged && changes.PackageVersionAfter is not null)
        {
            _logger.LogInformation(
                "  - {Label}: updated Syncfusion.Blazor.Toolkit from {Before} to {After}.",
                label,
                changes.PackageVersionBefore,
                changes.PackageVersionAfter);
        }
        else if (changes.PackageReferenced)
        {
            _logger.LogInformation("  - {Label}: Syncfusion.Blazor.Toolkit {Version} (unchanged).", label, changes.PackageVersionAfter);
        }
        else
        {
            _logger.LogWarning("  - {Label}: Syncfusion.Blazor.Toolkit is NOT referenced.", label);
        }
    }

    private void LogServices(SyncfusionBlazorToolkitSettings settings)
    {
        if (settings.Server.ServicesInserted)
        {
            _logger.LogInformation(
                "  - Updated {EntryPoint}: AddSyncfusionBlazorToolkit() + using Syncfusion.Blazor.Toolkit;",
                settings.ProgramCsFile ?? "the entry point");
        }
        else if (settings.ServicesApplied)
        {
            _logger.LogInformation("  - {EntryPoint}: AddSyncfusionBlazorToolkit() already registered (no change).", settings.ProgramCsFile ?? "Entry point");
        }
        else
        {
            _logger.LogWarning(
                "  - Entry point: AddSyncfusionBlazorToolkit() was NOT registered. Add " +
                "'builder.Services.AddSyncfusionBlazorToolkit();' before 'Build()' and 'using Syncfusion.Blazor.Toolkit;'.");
        }
    }

    private void LogImports(SyncfusionBlazorToolkitSettings settings, bool importsMissing)
    {
        if (importsMissing)
        {
            _logger.LogWarning(
                "  - _Imports.razor: SKIPPED (no suitable file could be found or created). " +
                "Add '@using Syncfusion.Blazor.Toolkit' to your _Imports file manually if your project uses one.");
        }
        else if (settings.Server.ImportsInserted)
        {
            _logger.LogInformation("  - Updated '{ImportsFile}': added @using Syncfusion.Blazor.Toolkit.", settings.ImportsFile);
        }
        else if (!string.IsNullOrEmpty(settings.ImportsFile))
        {
            _logger.LogInformation("  - Imports: '{ImportsFile}' already contains @using Syncfusion.Blazor.Toolkit (no change).", settings.ImportsFile);
        }
    }

    /// <summary>
    /// Hosting guidance derived from what the entry point actually registered. The global render mode is
    /// reported only as a fact: a page-level render mode is a normal way to opt in, so "no global render mode"
    /// is never described as "no interactivity". Nothing here changes any render mode.
    /// </summary>
    private static string DescribeHostingGuidance(SyncfusionBlazorToolkitSettings settings)
    {
        // Standalone WebAssembly is always interactive on the client; there is no server hosting to register.
        if (settings.AppKind == BlazorAppKind.StandaloneWasm)
        {
            return "Standalone Blazor WebAssembly: components run interactively in the browser; no render-mode registration is needed.";
        }

        BlazorHostingModes supported = settings.Hosting.Supported;
        bool server = supported.HasFlag(BlazorHostingModes.Server);
        bool wasm = supported.HasFlag(BlazorHostingModes.WebAssembly);
        string global = settings.Hosting.GlobalRenderMode;

        string registered = (server, wasm) switch
        {
            (true, true) => "Interactive Server and WebAssembly hosting are both registered, so InteractiveServer, InteractiveWebAssembly and InteractiveAuto are available.",
            (true, false) => "Interactive Server hosting is registered, so InteractiveServer is available. InteractiveAuto also needs AddInteractiveWebAssemblyComponents()/AddInteractiveWebAssemblyRenderMode().",
            (false, true) => "Interactive WebAssembly hosting is registered, so InteractiveWebAssembly is available. InteractiveAuto also needs AddInteractiveServerComponents()/AddInteractiveServerRenderMode().",
            _ => "No interactive hosting is registered (static server rendering). Add AddInteractiveServerComponents()/AddInteractiveServerRenderMode() and/or the WebAssembly equivalents before using interactive Toolkit components.",
        };

        string globalNote = global == "None"
            ? "No global render mode is set; pages choose their own with @rendermode."
            : $"The app-wide render mode is {global}.";

        return $"{registered} {globalNote}";
    }
}
