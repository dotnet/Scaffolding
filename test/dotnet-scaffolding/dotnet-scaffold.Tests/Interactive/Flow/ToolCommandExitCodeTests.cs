// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.Interactive.Command;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

public class ToolCommandExitCodeTests
{
    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void Install_ReturnsToolManagerResult(bool succeeds, int expectedExitCode)
    {
        var manager = new Mock<IToolManager>();
        manager.Setup(m => m.AddTool("test-tool", It.IsAny<string[]>(), null, false, null, false))
            .Returns(succeeds);

        var exitCode = new ToolInstallCommand(manager.Object).Execute(
            null!, new ToolInstallSettings { PackageName = "test-tool" });

        Assert.Equal(expectedExitCode, exitCode);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void Uninstall_ReturnsToolManagerResult(bool succeeds, int expectedExitCode)
    {
        var manager = new Mock<IToolManager>();
        manager.Setup(m => m.RemoveTool("test-tool", false)).Returns(succeeds);

        var exitCode = new ToolUninstallCommand(manager.Object).Execute(
            null!, new ToolUninstallSettings { PackageName = "test-tool" });

        Assert.Equal(expectedExitCode, exitCode);
    }
}
