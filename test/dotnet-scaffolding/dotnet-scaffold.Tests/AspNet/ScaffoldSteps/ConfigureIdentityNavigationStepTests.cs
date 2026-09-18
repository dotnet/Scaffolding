// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class ConfigureIdentityNavigationStepTests
{
    [Theory]
    [InlineData(false, "Views")]
    [InlineData(true, "Pages")]
    public async Task ExecuteAsync_AddsLoginPartialAndLayoutReference(bool isRazorPages, string hostFolder)
    {
        var projectDirectory = Path.Combine("test", "project");
        var projectPath = Path.Combine(projectDirectory, "TestProject.csproj");
        var sharedDirectory = Path.Combine(projectDirectory, hostFolder, "Shared");
        var layoutPath = Path.Combine(sharedDirectory, "_Layout.cshtml");
        var loginPartialPath = Path.Combine(sharedDirectory, "_LoginPartial.cshtml");
        var layoutContent = "<nav>\n    <ul class=\"navbar-nav flex-grow-1\">\n    </ul>\n</nav>";
        var writtenFiles = new Dictionary<string, string>();
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(layoutPath)).Returns(true);
        fileSystem.Setup(fs => fs.FileExists(loginPartialPath)).Returns(false);
        fileSystem.Setup(fs => fs.ReadAllText(layoutPath)).Returns(layoutContent);
        fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => writtenFiles[path] = content);

        var step = new ConfigureIdentityNavigationStep(
            NullLogger<ConfigureIdentityNavigationStep>.Instance,
            fileSystem.Object)
        {
            ProjectPath = projectPath,
            IsRazorPages = isRazorPages,
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestProject.Data"
        };

        var result = await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>()));

        Assert.True(result);
        Assert.Contains("@inject SignInManager<ApplicationUser>", writtenFiles[loginPartialPath]);
        Assert.Contains("<partial name=\"_LoginPartial\" />", writtenFiles[layoutPath]);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotOverwriteExistingNavigation()
    {
        var projectDirectory = Path.Combine("test", "project");
        var projectPath = Path.Combine(projectDirectory, "TestProject.csproj");
        var sharedDirectory = Path.Combine(projectDirectory, "Views", "Shared");
        var layoutPath = Path.Combine(sharedDirectory, "_Layout.cshtml");
        var loginPartialPath = Path.Combine(sharedDirectory, "_LoginPartial.cshtml");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(layoutPath)).Returns(true);
        fileSystem.Setup(fs => fs.FileExists(loginPartialPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(layoutPath)).Returns("<partial name=\"_LoginPartial\" />");

        var step = new ConfigureIdentityNavigationStep(
            NullLogger<ConfigureIdentityNavigationStep>.Instance,
            fileSystem.Object)
        {
            ProjectPath = projectPath,
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestProject.Data"
        };

        var result = await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>()));

        Assert.True(result);
        fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AddsLoginPartialToNavbarList()
    {
        var projectDirectory = Path.Combine("test", "project");
        var projectPath = Path.Combine(projectDirectory, "TestProject.csproj");
        var sharedDirectory = Path.Combine(projectDirectory, "Views", "Shared");
        var layoutPath = Path.Combine(sharedDirectory, "_Layout.cshtml");
        var loginPartialPath = Path.Combine(sharedDirectory, "_LoginPartial.cshtml");
        var layoutContent = "<ul class=\"footer-links\"></ul>\n<nav>\n    <ul class=\"navbar-nav flex-grow-1\">\n    </ul>\n</nav>";
        var writtenFiles = new Dictionary<string, string>();
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(layoutPath)).Returns(true);
        fileSystem.Setup(fs => fs.FileExists(loginPartialPath)).Returns(false);
        fileSystem.Setup(fs => fs.ReadAllText(layoutPath)).Returns(layoutContent);
        fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => writtenFiles[path] = content);

        var step = new ConfigureIdentityNavigationStep(
            NullLogger<ConfigureIdentityNavigationStep>.Instance,
            fileSystem.Object)
        {
            ProjectPath = projectPath,
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestProject.Data"
        };

        var result = await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>()));

        Assert.True(result);
        Assert.DoesNotContain("<ul class=\"footer-links\"></ul>\n<partial", writtenFiles[layoutPath]);
        Assert.Contains("</ul>\n    <partial name=\"_LoginPartial\" />\n</nav>", writtenFiles[layoutPath]);
    }

    [Fact]
    public async Task ExecuteAsync_AddsLoginPartialAfterNavbarWithNestedList()
    {
        var projectDirectory = Path.Combine("test", "project");
        var projectPath = Path.Combine(projectDirectory, "TestProject.csproj");
        var sharedDirectory = Path.Combine(projectDirectory, "Views", "Shared");
        var layoutPath = Path.Combine(sharedDirectory, "_Layout.cshtml");
        var loginPartialPath = Path.Combine(sharedDirectory, "_LoginPartial.cshtml");
        var layoutContent = """
<nav>
    <ul class="navbar-nav flex-grow-1">
        <li>
            <ul class="dropdown-menu">
            </ul>
        </li>
    </ul>
</nav>
""";
        var writtenFiles = new Dictionary<string, string>();
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(layoutPath)).Returns(true);
        fileSystem.Setup(fs => fs.FileExists(loginPartialPath)).Returns(false);
        fileSystem.Setup(fs => fs.ReadAllText(layoutPath)).Returns(layoutContent);
        fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => writtenFiles[path] = content);

        var step = new ConfigureIdentityNavigationStep(
            NullLogger<ConfigureIdentityNavigationStep>.Instance,
            fileSystem.Object)
        {
            ProjectPath = projectPath,
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestProject.Data"
        };

        var result = await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>()));

        Assert.True(result);
        var updatedLayout = writtenFiles[layoutPath].Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.DoesNotContain("</ul>\n            <partial name=\"_LoginPartial\" />\n        </li>", updatedLayout);
        Assert.Contains("</li>\n    </ul>\n    <partial name=\"_LoginPartial\" />\n</nav>", updatedLayout);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreatePartialWhenNavbarListIsMissing()
    {
        var projectDirectory = Path.Combine("test", "project");
        var projectPath = Path.Combine(projectDirectory, "TestProject.csproj");
        var sharedDirectory = Path.Combine(projectDirectory, "Views", "Shared");
        var layoutPath = Path.Combine(sharedDirectory, "_Layout.cshtml");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(layoutPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(layoutPath)).Returns("<main>@RenderBody()</main>");

        var step = new ConfigureIdentityNavigationStep(
            NullLogger<ConfigureIdentityNavigationStep>.Instance,
            fileSystem.Object)
        {
            ProjectPath = projectPath,
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestProject.Data"
        };

        var result = await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>()));

        Assert.True(result);
        fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
