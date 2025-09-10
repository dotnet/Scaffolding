// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;

/// <summary>
/// Telemetry event for tracking the result of a wrapped scaffold step, such as AddPackagesStep.
/// </summary>
internal class WrappedStepTelemetryEvent : TelemetryEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WrappedStepTelemetryEvent"/> class.
    /// </summary>
    /// <param name="stepName">The name of the step being wrapped.</param>
    /// <param name="scaffolderName">The name of the scaffolder.</param>
    /// <param name="result">The result of the step (true for success, false for failure).</param>
    public WrappedStepTelemetryEvent(string stepName, string scaffolderName, bool result) : base($"{stepName}Event")
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
