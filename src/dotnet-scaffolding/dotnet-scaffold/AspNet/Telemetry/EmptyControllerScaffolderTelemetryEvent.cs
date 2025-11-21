// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;

/// <summary>
/// Telemetry event for tracking the Empty Controller scaffolder step.
/// </summary>
internal class EmptyControllerScaffolderTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "EmptyControllerScaffolderStep";
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyControllerScaffolderTelemetryEvent"/> class.
    /// </summary>
    /// <param name="scaffolderName">The name of the scaffolder.</param>
    /// <param name="actions">Whether actions are included in the controller.</param>
    /// <param name="settingsValidationResult">Whether the settings validation succeeded.</param>
    /// <param name="result">The result status (Success/Failure).</param>
    public EmptyControllerScaffolderTelemetryEvent(string scaffolderName, bool actions, bool settingsValidationResult, bool result) : base(TelemetryEventName)
    {
        SetProperty("ScaffolderName", scaffolderName);
        SetProperty("ActionsController", actions ? TelemetryConstants.Yes : TelemetryConstants.No);
        SetProperty("SettingsValidationResult", settingsValidationResult ? TelemetryConstants.Success : TelemetryConstants.Failure);
        SetProperty("Result", result ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}

