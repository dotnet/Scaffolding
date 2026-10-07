// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Registers the Ignite UI for Blazor services ('builder.Services.AddIgniteUIBlazor()') with the shared code
/// modification step, then verifies that the registration is present. The code modifier leaves the file unchanged
/// when it does not recognize the bootstrapping code (for example a Program.cs without 'var app = builder.Build();'),
/// so without this check an app that cannot use the Ignite UI components would be reported as set up.
/// </summary>
internal class AddIgniteUIServicesStep : WrappedCodeModificationStep
{
    /// <summary>The method call that registers the Ignite UI for Blazor services.</summary>
    internal const string RegistrationCall = "AddIgniteUIBlazor(";

    /// <summary>
    /// Gets or sets the name of the file that registers the app's services: 'Program.cs', or 'MauiProgram.cs' for a
    /// .NET MAUI Blazor Hybrid app.
    /// </summary>
    public required string RegistrationFileName { get; set; }

    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddIgniteUIServicesStep"/> class.
    /// </summary>
    public AddIgniteUIServicesStep(ILogger<WrappedCodeModificationStep> logger, ITelemetryService telemetryService, IFileSystem fileSystem)
        : base(logger, telemetryService)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (await base.ExecuteAsync(context, cancellationToken) && IsRegistered())
        {
            return true;
        }

        _logger.LogError(
            $"Ignite UI for Blazor setup is incomplete: 'builder.Services.AddIgniteUIBlazor()' could not be added to {RegistrationFileName} in '{ProjectPath}'. " +
            $"Add 'using {IgniteUIBlazorHelper.ControlsNamespace};' and 'builder.Services.AddIgniteUIBlazor();' where the app's services are registered (before the app is built), " +
            "then re-run the scaffolder to complete the remaining steps.");
        return false;
    }

    /// <summary>
    /// Returns true when a <see cref="RegistrationFileName"/> file in the project (outside bin/ and obj/) calls
    /// <see cref="RegistrationCall"/>.
    /// </summary>
    internal bool IsRegistered()
    {
        var projectDirectory = Path.GetDirectoryName(ProjectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !_fileSystem.DirectoryExists(projectDirectory))
        {
            return false;
        }

        return _fileSystem.EnumerateFiles(projectDirectory, RegistrationFileName, SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(projectDirectory, file)
                .Split(Path.DirectorySeparatorChar)
                .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Any(file => _fileSystem.ReadAllText(file).Contains(RegistrationCall, StringComparison.Ordinal));
    }
}
