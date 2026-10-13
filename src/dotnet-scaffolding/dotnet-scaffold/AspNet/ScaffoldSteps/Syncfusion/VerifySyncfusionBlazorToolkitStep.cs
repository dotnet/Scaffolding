// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// Verifies the actual post-scaffolding state of the project so the
/// summary step can report outcomes (changes applied / already present /
/// incomplete setup) instead of relying on pre-scaffolding flags.
/// <para>The step also returns <c>false</c> when the configuration
/// is incomplete: missing or commented-out service registration,
/// missing package, or — when a Blazor WebAssembly client was
/// resolved — a client project that is itself unconfigured. Each
/// failure is logged with an actionable error message so the user
/// can fix the project by hand.</para>
/// </summary>
internal class VerifySyncfusionBlazorToolkitStep : ScaffoldStep
{
    private readonly ILogger _logger;

    public VerifySyncfusionBlazorToolkitStep(ILogger<VerifySyncfusionBlazorToolkitStep> logger)
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

        bool allOk = true;

        // Re-inspect the project after the code-modification step has
        // run. A pre-scaffolding "already registered" flag is not the
        // same as "registration is now present" if the scaffolder
        // silently skipped the code-modification step.
        try
        {
            string projectDirectory = Path.GetDirectoryName(settings.Project) ?? string.Empty;
            string programPath = Path.Combine(projectDirectory, "Program.cs");
            string programText = File.Exists(programPath) ? File.ReadAllText(programPath) : string.Empty;

            // `IsActiveServiceRegistrationPresent` rejects commented-out
            // calls and chained forms, so a project that ends up with
            //   builder.Services.AddRazorComponents().AddSyncfusionBlazorToolkit();
            // is reported as missing.
            bool serverHasRegistration = SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(programText);
            settings.ServicesApplied = serverHasRegistration;
            if (!serverHasRegistration && !settings.ServicesAlreadyRegistered)
            {
                _logger.LogError(
                    "Syncfusion Blazor Toolkit: 'AddSyncfusionBlazorToolkit()' is not registered on the server project '{Project}'. " +
                    "Add 'builder.Services.AddSyncfusionBlazorToolkit();' (or 'wasmBuilder.Services.AddSyncfusionBlazorToolkit();' for standalone Blazor WebAssembly) to Program.cs and re-run the scaffolder.",
                    settings.Project);
                allOk = false;
            }

            // Package reference check — accept the package whether it was
            // added by the scaffolder or already existed.
            if (File.Exists(settings.Project))
            {
                string csprojText = File.ReadAllText(settings.Project);
                settings.PackageApplied = csprojText.Contains(
                    "Syncfusion.Blazor.Toolkit",
                    StringComparison.OrdinalIgnoreCase);
                if (!settings.PackageApplied)
                {
                    _logger.LogError(
                        "Syncfusion Blazor Toolkit: NuGet package reference 'Syncfusion.Blazor.Toolkit' is missing from '{Project}'. " +
                        "Add a PackageReference to Syncfusion.Blazor.Toolkit and re-run the scaffolder.",
                        settings.Project);
                    allOk = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Syncfusion Blazor Toolkit verification step could not re-inspect the project state.");
        }

        // When a Blazor WebAssembly client was resolved by the validate
        // step, the client project must also be configured: package
        // reference on the client, and an @using directive in its
        // _Imports.razor. A server that is fully set up but a client
        // that is not would leave WASM components broken.
        if (!string.IsNullOrEmpty(settings.ClientProjectPath))
        {
            try
            {
                if (File.Exists(settings.ClientProjectPath))
                {
                    string clientText = File.ReadAllText(settings.ClientProjectPath);
                    if (!clientText.Contains("Syncfusion.Blazor.Toolkit", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError(
                            "Syncfusion Blazor Toolkit: NuGet package reference 'Syncfusion.Blazor.Toolkit' is missing from the resolved client project '{Client}'. " +
                            "Add a PackageReference to Syncfusion.Blazor.Toolkit and re-run the scaffolder.",
                            settings.ClientProjectPath);
                        allOk = false;
                    }
                }
                else
                {
                    _logger.LogError(
                        "Syncfusion Blazor Toolkit: resolved client project '{Client}' was not found on disk. " +
                        "Restore the file or fix the project reference and re-run the scaffolder.",
                        settings.ClientProjectPath);
                    allOk = false;
                }

                string clientDirectory = Path.GetDirectoryName(settings.ClientProjectPath) ?? string.Empty;
                string? clientImports = SyncfusionBlazorToolkitHelper.DiscoverImportsFile(clientDirectory);
                if (string.IsNullOrEmpty(clientImports))
                {
                    _logger.LogError(
                        "Syncfusion Blazor Toolkit: the resolved client project '{Client}' does not have a conventional _Imports.razor. " +
                        "Create 'Components/_Imports.razor' (or '_Imports.razor' at the project root) containing '@using Syncfusion.Blazor.Toolkit' and re-run the scaffolder.",
                        settings.ClientProjectPath);
                    allOk = false;
                }
                else
                {
                    string importsText = File.ReadAllText(clientImports);
                    if (!SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(importsText))
                    {
                        _logger.LogError(
                            "Syncfusion Blazor Toolkit: the resolved client project's _Imports file '{ImportsFile}' is missing '@using Syncfusion.Blazor.Toolkit'. " +
                            "Add the directive and re-run the scaffolder.",
                            clientImports);
                        allOk = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Syncfusion Blazor Toolkit verification step could not re-inspect the resolved client project.");
            }
        }

        return Task.FromResult(allOk);
    }
}