// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;

/// <summary>
/// Telemetry event for tracking wrapped step execution in scaffolders.
/// </summary>
internal class WrappedStepTelemetryEvent : TelemetryEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WrappedStepTelemetryEvent"/> class.
    /// </summary>
    /// <param name="stepName">The name of the step.</param>
    /// <param name="scaffolderName">The name of the scaffolder.</param>
    /// <param name="result">The result status (Success/Failure).</param>
    public WrappedStepTelemetryEvent(string stepName, string scaffolderName, bool result) : base($"{stepName}Event")
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
