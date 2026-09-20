// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Services;

public class DotNetToolServiceTests
{
    private readonly DotNetToolService _service = new(
        NullLogger<DotNetToolService>.Instance,
        Mock.Of<IEnvironmentService>(),
        Mock.Of<IFileSystem>());

    [Theory]
    [InlineData("Microsoft.dotnet-scaffold", false)]
    [InlineData("microsoft.dotnet-scaffold", true)]
    public void GetCommands_BuiltInTool_UsesRunningAssembly(string packageName, bool isGlobalTool)
    {
        var tool = CreateUnavailableTool(packageName, isGlobalTool);
        Dictionary<string, string>? envVars = Environment.GetEnvironmentVariable(TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE) is null
            ? new() { [TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE] = TelemetryConstants.TELEMETRY_STATE_DISABLED }
            : null;

        var commands = _service.GetCommands(tool, envVars);

        Assert.Contains(commands, command => command.Name == "blazor-empty");
        Assert.Contains(commands, command => command.Name == "caching");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetCommands_UnavailableThirdPartyTool_DoesNotReturnBuiltInCommands(bool isGlobalTool)
    {
        var tool = CreateUnavailableTool("Microsoft.dotnet-scaffold-custom", isGlobalTool);

        var commands = _service.GetCommands(tool);

        Assert.Empty(commands);
    }

    private static DotNetToolInfo CreateUnavailableTool(string packageName, bool isGlobalTool) => new()
    {
        PackageName = packageName,
        Version = "0.0.0",
        Command = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "dotnet-scaffold.dll"),
        IsGlobalTool = isGlobalTool
    };
}
