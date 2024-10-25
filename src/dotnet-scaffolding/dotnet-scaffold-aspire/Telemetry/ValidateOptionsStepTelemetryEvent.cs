// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;

internal class ValidateOptionsStepTelemetryEvent : TelemetryEventBase
{

    private const string TelemetryEventName = "ValidationOptionsStep";
    public ValidateOptionsStepTelemetryEvent(string scaffolderName, string methodName, bool result) : base(TelemetryEventName)
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("MethodName", methodName);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
