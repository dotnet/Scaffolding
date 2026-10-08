// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void GetAllCommandsParallel_DefaultComponents_QueriesOnlyBuiltInToolWithoutRestoring(bool emptyComponents, bool isGlobalTool)
    {
        var service = new RecordingDotNetToolService(
            localToolList: string.Join(Environment.NewLine,
                "Package Id Version Commands Manifest",
                "------------------------------------",
                "contoso.local 1.0.0 contoso-local manifest.json",
                isGlobalTool ? string.Empty : "MICROSOFT.DOTNET-SCAFFOLD 1.0.0 dotnet-scaffold manifest.json"),
            globalToolList: string.Join(Environment.NewLine,
                "Package Id Version Commands",
                "---------------------------",
                "microsoft.dotnet-scaffold 1.0.0 dotnet-scaffold",
                "contoso.global 1.0.0 contoso-global"));

        var commands = service.GetAllCommandsParallel(emptyComponents ? [] : null);

        var command = Assert.Single(commands);
        Assert.Equal("dotnet-scaffold", command.Key);
        Assert.Equal("test-command", command.Value.Name);

        var invocations = service.Invocations.ToArray();
        Assert.Equal(3, invocations.Length);
        Assert.Contains(invocations, invocation => invocation.Arguments == "tool list");
        Assert.Contains(invocations, invocation => invocation.Arguments == "tool list -g");
        Assert.DoesNotContain(invocations, invocation => invocation.Arguments == "tool restore");
        var metadataInvocation = Assert.Single(invocations, invocation => invocation.Arguments.EndsWith("get-commands", StringComparison.Ordinal));
        Assert.Contains(typeof(DotNetToolService).Assembly.Location, metadataInvocation.Arguments);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetAllCommandsParallel_ExplicitComponent_PreservesThirdPartyInvocationAndRestoreBehavior(bool isGlobalTool)
    {
        var service = new RecordingDotNetToolService();
        var tool = new DotNetToolInfo
        {
            PackageName = "Contoso.Scaffolder",
            Version = "1.0.0",
            Command = "contoso-scaffolder",
            IsGlobalTool = isGlobalTool
        };

        var commands = service.GetAllCommandsParallel([tool]);

        var command = Assert.Single(commands);
        Assert.Equal(tool.Command, command.Key);
        Assert.Equal("test-command", command.Value.Name);

        var invocations = service.Invocations.ToArray();
        Assert.Equal(isGlobalTool ? 1 : 2, invocations.Length);
        if (!isGlobalTool)
        {
            Assert.Equal("tool restore", invocations[0].Arguments);
        }

        var metadataInvocation = invocations[^1];
        Assert.Equal(isGlobalTool ? tool.Command : "dotnet", Path.GetFileNameWithoutExtension(metadataInvocation.FileName));
        Assert.Equal(isGlobalTool ? "get-commands" : $"{tool.Command} get-commands", metadataInvocation.Arguments);
    }

    private static DotNetToolInfo CreateUnavailableTool(string packageName, bool isGlobalTool) => new()
    {
        PackageName = packageName,
        Version = "0.0.0",
        Command = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "dotnet-scaffold.dll"),
        IsGlobalTool = isGlobalTool
    };

    private sealed class RecordingDotNetToolService(string localToolList = "", string globalToolList = "") : DotNetToolService(
        NullLogger<DotNetToolService>.Instance,
        Mock.Of<IEnvironmentService>(),
        Mock.Of<IFileSystem>())
    {
        public ConcurrentQueue<(string FileName, string Arguments)> Invocations { get; } = new();

        protected override int ExecuteAndCaptureOutput(DotnetCliRunner runner, out string? stdOut, out string? stdErr)
        {
            var arguments = runner._psi.Arguments;
            Invocations.Enqueue((runner._psi.FileName, arguments));
            stdErr = string.Empty;
            stdOut = arguments switch
            {
                "tool list" => localToolList,
                "tool list -g" => globalToolList,
                "tool restore" => string.Empty,
                _ when arguments.EndsWith("get-commands", StringComparison.Ordinal) => """
                    [{"Name":"test-command","DisplayName":"Test command","DisplayCategories":["All"],"Parameters":[]}]
                    """,
                _ => throw new InvalidOperationException($"Unexpected tool invocation: {runner._psi.FileName} {arguments}")
            };
            return 0;
        }
    }
}
