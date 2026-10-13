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

        // Re-inspect the project after the Program.cs step has run. A
        // pre-scaffolding "already registered" flag is not the same as
        // "registration is now present" if the scaffolder silently
        // skipped the Program.cs step.
        try
        {
            // Entry point and package are resolved from the MSBuild-evaluated
            // project (compiled sources / PackageReference items), not from
            // file names or raw .csproj text.
            string? programPath = SyncfusionBlazorToolkitHelper.DiscoverEntryPoint(settings.Project, out string? entryPointError);
            bool serverHasRegistration = false;
            if (programPath is null)
            {
                _logger.LogError(
                    "Syncfusion Blazor Toolkit: could not determine the application entry point of '{Project}'. {Error} " +
                    "Add 'builder.Services.AddSyncfusionBlazorToolkit();' to the entry point, before 'Build()', and re-run the scaffolder.",
                    settings.Project,
                    entryPointError);
            }
            else
            {
                // `IsActiveServiceRegistrationPresent` rejects commented-out
                // calls, chained forms, deferred code and registrations that
                // do not cover every host builder in the file.
                serverHasRegistration = SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(File.ReadAllText(programPath));
                if (!serverHasRegistration)
                {
                    _logger.LogError(
                        "Syncfusion Blazor Toolkit: 'AddSyncfusionBlazorToolkit()' is not registered on the server project '{Project}'. " +
                        "Add 'builder.Services.AddSyncfusionBlazorToolkit();' (or 'wasmBuilder.Services.AddSyncfusionBlazorToolkit();' for standalone Blazor WebAssembly) to '{ProgramCs}' and re-run the scaffolder.",
                        settings.Project,
                        programPath);
                }
            }

            settings.ServicesApplied = serverHasRegistration;
            allOk &= serverHasRegistration;

            if (!SyncfusionBlazorToolkitHelper.TryIsPackageReferenced(settings.Project, out bool packageReferenced, out string? packageError))
            {
                _logger.LogError("Syncfusion Blazor Toolkit: {Error}", packageError);
                settings.PackageApplied = false;
                allOk = false;
            }
            else
            {
                settings.PackageApplied = packageReferenced;
                if (!packageReferenced)
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
            // Fail closed: if the state cannot be re-inspected, it is not verified.
            _logger.LogError(ex,
                "Syncfusion Blazor Toolkit verification step could not re-inspect the project state; the setup is not verified.");
            allOk = false;
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
                    if (!SyncfusionBlazorToolkitHelper.TryIsPackageReferenced(settings.ClientProjectPath, out bool clientHasPackage, out string? clientPackageError))
                    {
                        _logger.LogError("Syncfusion Blazor Toolkit: {Error}", clientPackageError);
                        allOk = false;
                    }
                    else if (!clientHasPackage)
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
                _logger.LogError(ex,
                    "Syncfusion Blazor Toolkit verification step could not re-inspect the resolved client project; the setup is not verified.");
                allOk = false;
            }
        }

        return Task.FromResult(allOk);
    }
}