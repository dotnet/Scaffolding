// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Azure.Identity.Telemetry;

internal class WrappedStepTelemetryEvent : TelemetryEventBase
{
    public WrappedStepTelemetryEvent(string stepName, string scaffolderName, bool result) : base($"{stepName}Event")
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
