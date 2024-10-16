// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Services;

namespace Microsoft.DotNet.Scaffolding.Internal.Telemetry;

internal static class TelemetryExtensions
{
    public static void TrackEvent(this ITelemetryService telemetryService, TelemetryEventBase telemetryEvent)
    {
        telemetryService.TrackEvent(telemetryEvent.Name, telemetryEvent.Properties, telemetryEvent.Measurements);
    }
}
