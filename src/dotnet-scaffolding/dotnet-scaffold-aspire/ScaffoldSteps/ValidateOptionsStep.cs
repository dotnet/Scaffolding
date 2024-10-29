// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

//TODO: move to Microsoft.DotNet.Scaffolding.Core and register it there.
internal class ValidateOptionsStep : ScaffoldStep
{
    public required Func<ScaffolderContext, ILogger, bool> ValidateMethod { get; set; }
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    public ValidateOptionsStep(ILogger<ValidateOptionsStep> logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

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
