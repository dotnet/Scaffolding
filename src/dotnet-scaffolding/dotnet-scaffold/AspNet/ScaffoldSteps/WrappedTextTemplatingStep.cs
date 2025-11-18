// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.DotNet.Scaffolding.Core.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step that wraps TextTemplatingStep and adds telemetry tracking for text templating.
/// </summary>
internal class WrappedTextTemplatingStep : TextTemplatingStep
{
    private readonly IScaffolderLogger _logger;
    private readonly ITelemetryService _telemetryService;
    /// <summary>
    /// Constructor for WrappedTextTemplatingStep.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="telemetryService">The telemetry service instance.</param>
    public WrappedTextTemplatingStep(
        IScaffolderLogger logger,
        ITelemetryService telemetryService) : base(logger)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to perform text templating and track telemetry.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, with a boolean result indicating success or failure.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var result = await base.ExecuteAsync(context, cancellationToken);
        _telemetryService.TrackEvent(new WrappedStepTelemetryEvent(nameof(TextTemplatingStep), context.Scaffolder.DisplayName, result));
        return result;
    }
}
