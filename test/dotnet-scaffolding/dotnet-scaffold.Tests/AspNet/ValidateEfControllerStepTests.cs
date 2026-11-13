// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet;

public class ValidateEfControllerStepTests
{
    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("123Orders")]
    [InlineData("Orders-Controller")]
    [InlineData("@@OrdersController")]
    [InlineData("Orders Controller")]
    [InlineData("Controller")]
    [InlineData("CONTROLLER")]
    [InlineData("@Controller")]
    public async Task InvalidController_FailsBeforeProjectChanges(string controllerName)
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(fs => fs.FileExists("Test.csproj")).Returns(true);
        var step = CreateStep(fileSystem.Object, controllerName);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.OptionResults[new ScaffolderOption<bool>
        {
            DisplayName = "Views", CliOption = Constants.CliOptions.ViewsOption
        }] = true;

        Assert.False(await step.ExecuteAsync(context));
        Assert.Empty(context.Properties);
        fileSystem.Verify(fs => fs.FileExists("Test.csproj"), Times.Once);
        fileSystem.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("NULController")]
    [InlineData("@NULController")]
    [InlineData("nulCONTROLLER")]
    [InlineData("CONController")]
    [InlineData("COM1Controller")]
    [InlineData("LPT9Controller")]
    public async Task ReservedFolder_IsRejectedBeforeProjectChangesOnWindows(string controllerName)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.True(ViewHelper.IsValidControllerViewFolder(controllerName));
            return;
        }

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(fs => fs.FileExists("Test.csproj")).Returns(true);
        var step = CreateStep(fileSystem.Object, controllerName);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.OptionResults[new ScaffolderOption<bool>
        {
            DisplayName = "Views", CliOption = Constants.CliOptions.ViewsOption
        }] = true;

        Assert.False(await step.ExecuteAsync(context));
        Assert.Empty(context.Properties);
        fileSystem.Verify(fs => fs.FileExists("Test.csproj"), Times.Once);
        fileSystem.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("NULController")]
    [InlineData("Controller")]
    public void WithoutViews_DoesNotApplyViewFolderRestrictions(string controllerName)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists("Test.csproj")).Returns(true);
        var step = CreateStep(fileSystem.Object, controllerName);
        var validate = typeof(ValidateEfControllerStep).GetMethod(
            "ValidateEfControllerSettings", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(validate);
        Assert.NotNull(validate.Invoke(step, new object[] { false }));
    }

    [Theory]
    [InlineData("@OrdersController")]
    [InlineData("@OrdersCONTROLLER")]
    public void EscapedController_IsAcceptedAndPreserved(string controllerName)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists("Test.csproj")).Returns(true);
        var step = CreateStep(fileSystem.Object, controllerName);
        var validate = typeof(ValidateEfControllerStep).GetMethod(
            "ValidateEfControllerSettings", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(validate);
        Assert.NotNull(validate.Invoke(step, new object[] { true }));
        Assert.Equal(controllerName, step.ControllerName);
    }

    private static ValidateEfControllerStep CreateStep(IFileSystem fileSystem, string controllerName) =>
        new(fileSystem, NullLogger<ValidateEfControllerStep>.Instance, Mock.Of<ITelemetryService>())
        {
            Project = "Test.csproj",
            Model = "Order",
            ControllerName = controllerName,
            ControllerType = "MVC",
            DataContext = "OrdersDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SQLite
        };
}
