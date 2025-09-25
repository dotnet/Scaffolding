// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Telemetry;

/// <summary>
/// Telemetry event for tracking the result of adding a connection string step.
/// </summary>
internal class AddConnectionStringTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "AddConnectionStringStep";

    /// <summary>
    /// Initializes a new instance of the <see cref="AddConnectionStringTelemetryEvent"/> class.
    /// </summary>
    /// <param name="scaffolderName">The name of the scaffolder.</param>
    /// <param name="status">The status of the operation (e.g., Success, Failure).</param>
    /// <param name="failureReason">The reason for failure, if any.</param>
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
