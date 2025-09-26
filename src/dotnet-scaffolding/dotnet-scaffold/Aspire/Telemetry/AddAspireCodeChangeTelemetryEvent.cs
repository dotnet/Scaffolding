// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;

/// <summary>
/// Telemetry event for tracking the result of the AddAspireCodeChangeStep.
/// </summary>
internal class AddAspireCodeChangeTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "AddAspireCodeChangeStep";

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAspireCodeChangeTelemetryEvent"/> class.
    /// </summary>
    /// <param name="scaffolderName">The name of the scaffolder.</param>
    /// <param name="result">The result of the step (true for success, false for failure).</param>
    public AddAspireCodeChangeTelemetryEvent(string scaffolderName, bool result) : base(TelemetryEventName)
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
