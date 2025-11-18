// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.DotNet.Scaffolding.Core.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step that wraps AddPackagesStep and adds telemetry tracking for package installation.
/// </summary>
internal class WrappedAddPackagesStep : AddPackagesStep
{
    private readonly IScaffolderLogger _logger;
    private readonly ITelemetryService _telemetryService;
    /// <summary>
    /// Constructor for WrappedAddPackagesStep.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="telemetryService">Telemetry service instance.</param>
    public WrappedAddPackagesStep(
        IScaffolderLogger logger,
        ITelemetryService telemetryService) : base(logger)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to add packages and track telemetry.
    /// </summary>
    /// <param name="context">Scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var result = await base.ExecuteAsync(context, cancellationToken);
        _telemetryService.TrackEvent(new WrappedStepTelemetryEvent(nameof(AddPackagesStep), context.Scaffolder.DisplayName, result));
        return result;
    }
}
