// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;
using Microsoft.DotNet.Scaffolding.Core.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

//TODO: move to Microsoft.DotNet.Scaffolding.Core and register it there.
/// <summary>
/// A scaffold step that validates options using a provided validation method and tracks telemetry.
/// </summary>
internal class ValidateOptionsStep : ScaffoldStep
{
    /// <summary>
    /// The validation method to execute for this step.
    /// </summary>
    public required Func<ScaffolderContext, IScaffolderLogger, bool> ValidateMethod { get; set; }
    private readonly IScaffolderLogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateOptionsStep"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="telemetryService">The telemetry service instance.</param>
    public ValidateOptionsStep(IScaffolderLogger logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the validation step and tracks the result in telemetry.
    /// </summary>
    /// <param name="context">The scaffolder context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if validation succeeded; otherwise, false.</returns>
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateMethod(context, _logger);
        if (!validationResult)
        {
            _logger.LogError("Validation failed.");
        }
        else
        {
            _logger.LogInformation("Validation succeeded.");
        }

        _telemetryService.TrackEvent(new ValidateOptionsStepTelemetryEvent(context.Scaffolder.DisplayName, ValidateMethod.Method.Name, validationResult));
        return Task.FromResult(validationResult);
    }
}
