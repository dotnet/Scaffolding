// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;

/// <summary>
/// Telemetry event for tracking the result of the ValidateOptionsStep.
/// </summary>
internal class ValidateOptionsStepTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "ValidationOptionsStep";

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateOptionsStepTelemetryEvent"/> class.
    /// </summary>
    /// <param name="scaffolderName">The name of the scaffolder.</param>
    /// <param name="methodName">The name of the validation method.</param>
    /// <param name="result">The result of the validation (true for success, false for failure).</param>
    public ValidateOptionsStepTelemetryEvent(string scaffolderName, string methodName, bool result) : base(TelemetryEventName)
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("MethodName", methodName);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
