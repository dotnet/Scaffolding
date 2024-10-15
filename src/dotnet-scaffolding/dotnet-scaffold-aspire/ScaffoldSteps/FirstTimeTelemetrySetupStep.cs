// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

internal class FirstTimeTelemetrySetupStep : ScaffoldStep
{
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger _logger;
    public FirstTimeTelemetrySetupStep(
        IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel,
        IEnvironmentService environmentService,
        ILogger<FirstTimeTelemetrySetupStep> logger)
    {
        _environmentService = environmentService;
        _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel;
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        //no need to display the first time telemetry manner if any of the conditions below are met
        if (_environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.TELEMETRY_OPTOUT) ||
            _firstTimeUseNoticeSentinel.Exists() ||
            _environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.LAUNCHED_BY_DOTNET_SCAFFOLD))
        {
            return Task.FromResult(true);
        }

        if (!_firstTimeUseNoticeSentinel.SkipFirstTimeExperience)
        {
            _logger.Log(LogLevel.Information, _firstTimeUseNoticeSentinel.DisclosureText);
        }

        _firstTimeUseNoticeSentinel.CreateIfNotExists();
        return Task.FromResult(true);
    }
}
