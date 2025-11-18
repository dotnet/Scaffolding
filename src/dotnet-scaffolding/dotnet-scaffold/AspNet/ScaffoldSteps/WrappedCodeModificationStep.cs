// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.DotNet.Scaffolding.Core.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step that wraps CodeModificationStep and adds telemetry tracking for code modification.
/// </summary>
internal class WrappedCodeModificationStep : CodeModificationStep
{
    private readonly IScaffolderLogger _logger;
    private readonly ITelemetryService _telemetryService;
    /// <summary>
    /// Constructor for WrappedCodeModificationStep.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="telemetryService">Telemetry service instance.</param>
    public WrappedCodeModificationStep(
        IScaffolderLogger logger,
        ITelemetryService telemetryService) : base(logger)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to modify code and track telemetry.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, containing a boolean result.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var result = await base.ExecuteAsync(context, cancellationToken);
        _telemetryService.TrackEvent(new WrappedStepTelemetryEvent(nameof(CodeModificationStep), context.Scaffolder.DisplayName, result));
        return result;
    }
}
