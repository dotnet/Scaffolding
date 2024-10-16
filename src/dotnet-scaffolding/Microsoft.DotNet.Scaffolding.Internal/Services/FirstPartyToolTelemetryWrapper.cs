// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;

internal class FirstPartyToolTelemetryWrapper : IFirstPartyToolTelemetryWrapper
{
    private readonly IEnvironmentService _environmentService;
    private readonly ITelemetryService _telemetryService;
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
    private readonly ILogger _logger;

    public FirstPartyToolTelemetryWrapper(
        IEnvironmentService environmentService,
        ITelemetryService telemetryService,
        IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel,
        ILogger<FirstPartyToolTelemetryWrapper> logger)
    {
        _environmentService = environmentService;
        _telemetryService = telemetryService;
        _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel;
        _logger = logger;
    }

    public void ConfigureFirstTimeTelemetry()
    {
        //no need to display the first time telemetry manner if any of the conditions below are met
        if (_environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.TELEMETRY_OPTOUT) ||
            _firstTimeUseNoticeSentinel.Exists())
        {
            return;
        }

        var dotnetScaffoldTelemetryState = _environmentService.GetEnvironmentVariable(TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE);
        //if not called by 'dotnet-scaffold', and it's a first time use (earlier check would have caught the existing sentinel file), display the notice (if no SkipFirstTimeExperience) and create the sentinel file.
        if (dotnetScaffoldTelemetryState is null)
        {
            if (!_firstTimeUseNoticeSentinel.SkipFirstTimeExperience)
            {
                _logger.Log(LogLevel.Information, _firstTimeUseNoticeSentinel.DisclosureText);
            }

            _firstTimeUseNoticeSentinel.CreateIfNotExists();
        }
    }

    public void Flush()
    {
        _telemetryService.Flush();
    }
}
