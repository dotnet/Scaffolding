// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;
using Microsoft.DotNet.Scaffolding.Core.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

/// <summary>
/// A scaffold step that wraps the AddPackagesStep to add telemetry tracking for package installation.
/// </summary>
internal class WrappedAddPackagesStep : AddPackagesStep
{
    private readonly IScaffolderLogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WrappedAddPackagesStep"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="telemetryService">The telemetry service instance.</param>
    public WrappedAddPackagesStep(
        IScaffolderLogger logger,
        ITelemetryService telemetryService) : base(logger)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the package installation step and tracks the result in telemetry.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the package installation succeeded; otherwise, false.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var result = await base.ExecuteAsync(context, cancellationToken);
        _telemetryService.TrackEvent(new WrappedStepTelemetryEvent(nameof(AddPackagesStep), context.Scaffolder.DisplayName, result));
        return result;
    }
}
