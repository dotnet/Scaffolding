// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    public async Task ExecuteAsync_UpdatesHostLayout(bool isRazorPages, string hostFolder)
    {
        var projectPath = Path.Combine("test", "project", "TestProject.csproj");
        var layoutPath = Path.Combine("test", "project", hostFolder, "Shared", "_Layout.cshtml");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(layoutPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(layoutPath)).Returns("<ul class=\"navbar-nav\"></ul>");
        var step = new ConfigureIdentityNavigationStep(NullLogger<ConfigureIdentityNavigationStep>.Instance, fileSystem.Object)
        {
            ProjectPath = projectPath,
            IsRazorPages = isRazorPages
        };

        Assert.True(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        fileSystem.Verify(fs => fs.WriteAllText(layoutPath, "<ul class=\"navbar-nav\"></ul>\n<partial name=\"_LoginPartial\" />"), Times.Once);
    }

    [Theory]
    [InlineData("<partial name=\"_LoginPartial\" />")]
    [InlineData("<partial name='_LoginPartial' />")]
    [InlineData("@await Html.PartialAsync(\"_LoginPartial\")")]
    [InlineData("@{ await Html.RenderPartialAsync(\"_LoginPartial\"); }")]
    public void AddLoginPartialReference_PreservesExistingReference(string content)
        => Assert.Equal(content, ConfigureIdentityNavigationStep.AddLoginPartialReference(content));

    [Theory]
    [InlineData("")]
    [InlineData("<ul class=\"footer-links\"></ul>\n")]
    [InlineData("<!-- <ul class=\"navbar-nav\"></ul><partial name=\"_LoginPartial\" /> -->\n")]
    [InlineData("@* <ul class=\"navbar-nav\"></ul><partial name=\"_LoginPartial\" /> *@\n")]
    [InlineData("<style>.navbar-nav { color: red; }</style>\n")]
    public void AddLoginPartialReference_TargetsNavbarAndIsIdempotent(string prefix)
    {
        var content = prefix + "<nav>\n    <ul class='navbar-nav flex-grow-1'>\n        <li><ul class='dropdown-menu'></ul></li>\n    </ul>\n</nav>";
        var expected = content.Replace("    </ul>\n</nav>", "    </ul>\n    <partial name=\"_LoginPartial\" />\n</nav>");

        var actual = ConfigureIdentityNavigationStep.AddLoginPartialReference(content);

        Assert.Equal(expected, actual);
        Assert.Equal(actual, ConfigureIdentityNavigationStep.AddLoginPartialReference(actual!));
    }

    [Theory]
    [InlineData("<main>@RenderBody()</main>")]
    [InlineData("<ul class=\"navbar-nav-custom\"></ul>")]
    [InlineData("<ul class=\"navbar-nav\"><li>Unclosed</li>")]
    public void AddLoginPartialReference_LeavesUnsupportedLayoutAlone(string content)
        => Assert.Null(ConfigureIdentityNavigationStep.AddLoginPartialReference(content));

    [Fact]
    public void AddLoginPartialReference_PreservesLineEndings()
    {
        const string content = "<ul class=\"navbar-nav\">\r\n</ul>\r\n";
        Assert.Equal("<ul class=\"navbar-nav\">\r\n</ul>\r\n<partial name=\"_LoginPartial\" />\r\n",
            ConfigureIdentityNavigationStep.AddLoginPartialReference(content));
    }
}
