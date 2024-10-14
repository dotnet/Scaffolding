// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class ServiceCollectionExtensions
{
    public static void AddTelemetry(this IServiceCollection services, string productName)
    {
        services.AddSingleton<IFirstTimeUseNoticeSentinel, FirstTimeUseNoticeSentinel>((serviceProvider) =>
        {
            return new FirstTimeUseNoticeSentinel(serviceProvider.GetRequiredService<IFileSystem>(),
                                                  serviceProvider.GetRequiredService<IEnvironmentService>(),
                                                  productName);
        });
        services.AddSingleton<ITelemetryService, TelemetryService>();
    }
}
