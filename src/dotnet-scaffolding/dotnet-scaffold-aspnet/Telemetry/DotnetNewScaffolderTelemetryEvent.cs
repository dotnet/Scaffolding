// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;

internal class DotnetNewScaffolderTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "DotnetNewScaffolderStep";
    public DotnetNewScaffolderTelemetryEvent(string scaffolderName, bool settingsValidationResult, bool result) : base(TelemetryEventName)
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("SettingsValidationResult", settingsValidationResult ? TelemetryConstants.Success : TelemetryConstants.Failure);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}

