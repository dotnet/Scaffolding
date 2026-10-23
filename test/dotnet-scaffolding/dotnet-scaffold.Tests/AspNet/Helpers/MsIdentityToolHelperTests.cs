// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

/// <summary>
/// Tests for <see cref="MsIdentityToolHelper"/>, which is responsible for surfacing clear,
/// actionable guidance when the 'Microsoft.dotnet-msidentity' tool is not installed.
/// </summary>
public class MsIdentityToolHelperTests
{
    [Fact]
    public void MsIdentityToolName_HasExpectedValue()
    {
        Assert.Equal("Microsoft.dotnet-msidentity", MsIdentityToolHelper.MsIdentityToolName);
    }

    [Fact]
    public void MsIdentityCommandName_HasExpectedValue()
    {
        Assert.Equal("msidentity", MsIdentityToolHelper.MsIdentityCommandName);
    }

    [Fact]
    public void NotInstalledMessage_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(MsIdentityToolHelper.NotInstalledMessage));
    }

    [Fact]
    public void NotInstalledMessage_MentionsToolName()
    {
        Assert.Contains(MsIdentityToolHelper.MsIdentityToolName, MsIdentityToolHelper.NotInstalledMessage);
    }

    [Fact]
    public void NotInstalledMessage_IncludesInstallCommand()
    {
        // The message must tell the user exactly how to install the missing tool.
        Assert.Contains(
            $"dotnet tool install --global {MsIdentityToolHelper.MsIdentityToolName}",
            MsIdentityToolHelper.NotInstalledMessage);
    }

    [Fact]
    public void NotInstalledMessage_MentionsPrereleaseGuidance()
    {
        Assert.Contains("--prerelease", MsIdentityToolHelper.NotInstalledMessage);
    }

    [Fact]
    public void NotInstalledMessage_ExplainsEntraIdDependency()
    {
        Assert.Contains("Entra ID", MsIdentityToolHelper.NotInstalledMessage);
    }

    [Theory]
    [InlineData(true, "dotnet-msidentity.exe")]
    [InlineData(false, "dotnet-msidentity")]
    public void GetGlobalToolCommandPath_ReturnsUserProfileToolShim(
        bool isWindows,
        string expectedCommandName)
    {
        string userProfile = Path.Combine("users", "test-user");

        string commandPath = MsIdentityToolHelper.GetGlobalToolCommandPath(userProfile, isWindows);

        Assert.Equal(
            Path.Combine(userProfile, ".dotnet", "tools", expectedCommandName),
            commandPath);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CreateMsIdentityRunner_AppliesTelemetryEnvironmentWithoutChangingParent(
        bool optOut,
        bool inherited)
    {
        string variableName = optOut
            ? TelemetryConstants.TELEMETRY_OPTOUT
            : TelemetryConstants.DOTNET_SCAFFOLD_TELEMETRY_STATE;
        string? originalValue = Environment.GetEnvironmentVariable(variableName);
        string? parentValue = inherited ? "inherited-value" : null;
        try
        {
            Environment.SetEnvironmentVariable(variableName, parentValue);

            var runner = MsIdentityToolHelper.CreateMsIdentityRunner(
                ["--create-client-secret", "--json"],
                new Dictionary<string, string> { [variableName] = "1" });

            Assert.Equal("1", runner._psi.Environment[variableName]);
            Assert.Equal(Environment.GetEnvironmentVariable("PATH"), runner._psi.Environment["PATH"]);
            Assert.Equal(parentValue, Environment.GetEnvironmentVariable(variableName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
}
