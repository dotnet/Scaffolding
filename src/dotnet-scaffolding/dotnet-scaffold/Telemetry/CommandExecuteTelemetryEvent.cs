// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Tools.Scaffold.Telemetry;

internal class CommandExecuteTelemetryEvent : TelemetryEventBase
{
    private const string TelemetryEventName = "CommandExecute";
    public CommandExecuteTelemetryEvent(DotNetToolInfo dotnetToolInfo, int? exitCode) : base(TelemetryEventName)
    {
        SetProperty("PackageName", dotnetToolInfo.PackageName);
        SetProperty("Version", dotnetToolInfo.Version);
        SetProperty("Command", dotnetToolInfo.Command);
        SetProperty("ToolLevel", dotnetToolInfo.IsGlobalTool ? TelemetryConstants.GlobalTool : TelemetryConstants.LocalTool);
        SetProperty("Result", exitCode is null ? TelemetryConstants.Unknown : exitCode == 0 ? TelemetryConstants.Success : TelemetryConstants.Failure);
    }
}
