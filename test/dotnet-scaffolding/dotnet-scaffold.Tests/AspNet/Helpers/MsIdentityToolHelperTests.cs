// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
