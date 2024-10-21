// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Telemetry;

internal class CommandExecuteTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "CommandExecute";
    public CommandExecuteTelemetryEvent(DotNetToolInfo dotnetToolInfo, CommandInfo commandInfo, int? exitCode, string? chosenCategory = null) : base(TelemetryEventName)
    {
        SetProperty("PackageName", dotnetToolInfo.PackageName);
        SetProperty("Version", dotnetToolInfo.Version);
        SetProperty("ToolName", dotnetToolInfo.Command);
        SetProperty("ToolLevel", dotnetToolInfo.IsGlobalTool ? TelemetryConstants.GlobalTool : TelemetryConstants.LocalTool);
        SetProperty("CommandName", commandInfo.Name);
        SetProperty("AllCommandCategories", string.Join(",", commandInfo.DisplayCategories));
        if (!string.IsNullOrEmpty(chosenCategory))
        {
            SetProperty("ChosenCategory", chosenCategory);
        }

        SetProperty("Result", exitCode is null ? TelemetryConstants.Unknown : exitCode == 0 ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
