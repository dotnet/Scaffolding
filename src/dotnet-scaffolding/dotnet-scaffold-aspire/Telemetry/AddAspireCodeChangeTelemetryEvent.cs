// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;

internal class AddAspireCodeChangeTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "AddAspireCodeChangeStep";
    public AddAspireCodeChangeTelemetryEvent(string scaffolderName, bool result) : base(TelemetryEventName)
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
