// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;

internal class AddConnectionStringTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "AddConnectionStringStep";
    public AddConnectionStringTelemetryEvent(string scaffolderName, string status, string? failureReason = null) : base(TelemetryEventName)
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("Result", status);
        if (!string.IsNullOrEmpty(failureReason))
        {
            SetProperty("FailureReason", failureReason);
        }
    }
}
