// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Telemetry;

/// <summary>
/// Represents a telemetry event for the execution of a scaffold command.
/// Captures tool information, command details, categories, and the result of the execution.
/// </summary>
internal class CommandExecuteTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "CommandExecute";

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecuteTelemetryEvent"/> class.
    /// </summary>
    /// <param name="dotnetToolInfo">Information about the .NET tool being executed.</param>
    /// <param name="commandInfo">Information about the command being executed.</param>
    /// <param name="exitCode">The exit code of the command execution.</param>
    /// <param name="chosenCategory">The category chosen for the command, if any.</param>
    public CommandExecuteTelemetryEvent(
        DotNetToolInfo dotnetToolInfo,
        CommandInfo commandInfo,
        int? exitCode,
        string? chosenCategory = null)
        : base(TelemetryEventName)
    {
        // Set tool and command properties for telemetry
        SetProperty("PackageName", dotnetToolInfo.PackageName, isPII: true);
        SetProperty("Version", dotnetToolInfo.Version);
        SetProperty("ToolName", dotnetToolInfo.Command, isPII: true);
        SetProperty("ToolLevel", dotnetToolInfo.IsGlobalTool ? TelemetryConstants.GlobalTool : TelemetryConstants.LocalTool);
        SetProperty("CommandName", commandInfo.Name, isPII: true);
        SetProperty("AllCommandCategories", commandInfo.DisplayCategories, isPII: true);
        if (!string.IsNullOrEmpty(chosenCategory))
        {
            SetProperty("ChosenCategory", chosenCategory, isPII: true);
        }

        // Set the result property based on the exit code
        SetProperty("Result", exitCode is null ? TelemetryConstants.Unknown : exitCode == 0 ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
