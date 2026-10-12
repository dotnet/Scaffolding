// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// Verifies the actual post-scaffolding state of the project so the
/// summary step can report outcomes (changes applied / already present /
/// incomplete setup) instead of relying on pre-scaffolding flags.
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

        // Re-inspect Program.cs and the .csproj after the code-modification
        // step has run. A pre-scaffolding "already registered" flag is not
        // the same as "registration is now present" if the scaffolder
        // silently skipped the code-modification step.
        try
        {
            string projectDirectory = Path.GetDirectoryName(settings.Project) ?? string.Empty;
            string programPath = Path.Combine(projectDirectory, "Program.cs");
            if (File.Exists(programPath))
            {
                string programText = File.ReadAllText(programPath);
                settings.ServicesApplied = programText.Contains(
                    "AddSyncfusionBlazorToolkit",
                    StringComparison.OrdinalIgnoreCase);
            }

            if (File.Exists(settings.Project))
            {
                string csprojText = File.ReadAllText(settings.Project);
                settings.PackageApplied = csprojText.Contains(
                    "Syncfusion.Blazor.Toolkit",
                    StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Syncfusion Blazor Toolkit verification step could not re-inspect the project state.");
        }

        return Task.FromResult(true);
    }
}