// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Internal.Telemetry;

internal static class FirstTimeTelemetryInitializer
{
    //Log the first time telemetry notice and create the sentinel file if the conditions are met
    //For use by the first party tools ('dotnet-scaffold-aspnet' and 'dotnet-scaffold-aspire')
    internal static void ConfigureTelemetry(IServiceProvider? serviceProvider)
    {
        if (serviceProvider is not null)
        {
            var environmentService = serviceProvider.GetRequiredService<IEnvironmentService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var firstTimeTelemetryLogger = loggerFactory.CreateLogger("firstTimeTelemetryLogger");
            var firstTimeUseNoticeSentinel = serviceProvider.GetRequiredService<IFirstTimeUseNoticeSentinel>();
            //no need to display the first time telemetry manner if any of the conditions below are met
            if (environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.TELEMETRY_OPTOUT) ||
                firstTimeUseNoticeSentinel.Exists())
            {
                return;
            }

            var dotnetScaffoldTelemetryState = environmentService.GetEnvironmentVariable(TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE);
            //if not called by 'dotnet-scaffold', and it's a first time use (earlier check would have caught the existing sentinel file), display the notice (if no SkipFirstTimeExperience) and create the sentinel file.
            if (dotnetScaffoldTelemetryState is null)
            {
                if (!firstTimeUseNoticeSentinel.SkipFirstTimeExperience)
                {
                    firstTimeTelemetryLogger.Log(LogLevel.Information, firstTimeUseNoticeSentinel.DisclosureText);
                }

                firstTimeUseNoticeSentinel.CreateIfNotExists();
            }

            return;
        }
    }
}
