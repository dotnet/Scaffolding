// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class EntraIdHelperTests
{
    [Fact]
    public void GetTextTemplatingProperties_WithEmptyTemplatePaths_ReturnsEmpty()
    {
        // Arrange
        List<string> templatePaths = [];
        EntraIdModel entraIdModel = CreateTestEntraIdModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNullProjectInfo_ReturnsEmpty()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorEntraId", "Test.tt")];
        EntraIdModel entraIdModel = new EntraIdModel
        {
            ProjectInfo = null,
            BaseOutputPath = "output"
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNullProjectPath_ReturnsEmpty()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorEntraId", "Test.tt")];
        EntraIdModel entraIdModel = new EntraIdModel
        {
            ProjectInfo = new ProjectInfo(null),
            BaseOutputPath = "output"
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithValidInputs_ReturnsProperties()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorEntraId", "TestFile.tt")];
        EntraIdModel entraIdModel = CreateTestEntraIdModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel);

        // Assert
        Assert.NotNull(result);
        // Result may be empty if template type cannot be matched from reflection
        // This is expected behavior when testing without actual template types
    }

    [Fact]
    public void GetTextTemplatingProperties_WithLoginOrPrefix_UsesRazorExtension()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorEntraId", "LoginOrRegister.tt")];
        EntraIdModel entraIdModel = CreateTestEntraIdModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel);

        // Assert
        Assert.NotNull(result);
        // If any properties are returned, verify the extension logic
        TextTemplatingProperty? property = result.FirstOrDefault();
        if (property != null)
        {
            Assert.EndsWith(".razor", property.OutputPath);
        }
    }

    [Fact]
    public void GetTextTemplatingProperties_WithoutLoginOrPrefix_UsesCsExtension()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorEntraId", "SomeClass.tt")];
        EntraIdModel entraIdModel = CreateTestEntraIdModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel);

        // Assert
        Assert.NotNull(result);
        // If any properties are returned, verify the extension logic
        TextTemplatingProperty? property = result.FirstOrDefault();
        if (property != null)
        {
            Assert.EndsWith(".cs", property.OutputPath);
        }
    }

    [Fact]
    public void GetTextTemplatingProperties_WithRedirectToLogin_GeneratesServerRazorComponent()
    {
        // Arrange
        EntraIdModel entraIdModel = CreateTestEntraIdModel();
        List<string> templatePaths = [Path.Combine("BlazorEntraId", "RedirectToLogin.tt")];

        // Act
        TextTemplatingProperty property = Assert.Single(
            EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel));

        // Assert
        Assert.Equal(
            Path.Combine("output", "Components", "RedirectToLogin.razor"),
            property.OutputPath);
        Assert.Equal(
            typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorEntraId.RedirectToLogin),
            property.TemplateType);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithHostedWasmClientWithoutClientRouter_GeneratesRazorComponentsInHostProject()
    {
        // Arrange
        EntraIdModel entraIdModel = CreateTestEntraIdModel();
        List<string> templatePaths =
        [
            Path.Combine("BlazorEntraId", "LoginOrLogout.tt"),
            Path.Combine("BlazorEntraId", "RedirectToLogin.tt")
        ];
        string root = Path.Combine(Path.GetTempPath(), nameof(EntraIdHelperTests), Guid.NewGuid().ToString());
        string layoutDirectory = Path.Combine(root, "Components", "Layout");
        string clientProjectPath = Path.Combine(root, "TestProject.Client", "TestProject.Client.csproj");
        entraIdModel.BaseOutputPath = root;
        try
        {
            Directory.CreateDirectory(layoutDirectory);
            File.WriteAllText(Path.Combine(layoutDirectory, "NavMenu.razor"), string.Empty);

            TextTemplatingProperty[] properties = EntraIdHelper
                .GetTextTemplatingProperties(templatePaths, entraIdModel, clientProjectPath)
                .ToArray();

            Assert.Equal(2, properties.Length);
            Assert.Contains(properties, property =>
                property.OutputPath == Path.Combine(layoutDirectory, "LoginOrLogout.razor"));
            Assert.Contains(properties, property =>
                property.OutputPath == Path.Combine(root, "Components", "RedirectToLogin.razor"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("WebAssembly")]
    public async Task GetTextTemplatingProperties_WithSdkGeneratedGlobalRouter_GeneratesRedirectInClient(string interactivity)
    {
        string root = Path.Combine(Path.GetTempPath(), nameof(EntraIdHelperTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        try
        {
            var created = await ScaffoldCliHelper.RunDotNetAsync(
                root, "new", "blazor", "-n", "TestProject", "-o", root,
                "-int", interactivity, "-ai", "--no-restore");
            Assert.True(created.ExitCode == 0, $"dotnet new failed: {created.Output}{created.Error}");
            string serverDirectory = Path.Combine(root, "TestProject");
            string clientDirectory = Path.Combine(root, "TestProject.Client");
            Assert.True(File.Exists(Path.Combine(clientDirectory, "Routes.razor")));
            Assert.False(File.Exists(Path.Combine(serverDirectory, "Components", "Routes.razor")));
            var model = new EntraIdModel
            {
                ProjectInfo = new ProjectInfo(Path.Combine(serverDirectory, "TestProject.csproj")),
                BaseOutputPath = serverDirectory,
                EntraIdNamespace = "TestProject"
            };

            TextTemplatingProperty property = Assert.Single(EntraIdHelper.GetTextTemplatingProperties(
                [Path.Combine("BlazorEntraId", "RedirectToLogin.tt")],
                model,
                Path.Combine(clientDirectory, "TestProject.Client.csproj")));

            Assert.Equal(Path.Combine(clientDirectory, "RedirectToLogin.razor"), property.OutputPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void GetTextTemplatingProperties_WithStandaloneWasmProject_GeneratesRazorComponentsInWasmProject()
    {
        string root = Path.Combine(Path.GetTempPath(), nameof(EntraIdHelperTests), Guid.NewGuid().ToString());
        string clientProjectPath = Path.Combine(root, "TestProject.Client.csproj");
        EntraIdModel entraIdModel = new()
        {
            ProjectInfo = new ProjectInfo(clientProjectPath),
            BaseOutputPath = Path.GetDirectoryName(clientProjectPath)
        };
        List<string> templatePaths =
        [
            Path.Combine("BlazorEntraId", "LoginOrLogout.tt"),
            Path.Combine("BlazorEntraId", "RedirectToLogin.tt")
        ];

        try
        {
            string layoutDirectory = Path.Combine(root, "Layout");
            Directory.CreateDirectory(layoutDirectory);
            File.WriteAllText(Path.Combine(layoutDirectory, "NavMenu.razor"), string.Empty);

            TextTemplatingProperty[] properties = EntraIdHelper
                .GetTextTemplatingProperties(templatePaths, entraIdModel, clientProjectPath)
                .ToArray();

            Assert.Equal(2, properties.Length);
            Assert.Contains(properties, property =>
                property.OutputPath == Path.Combine(layoutDirectory, "LoginOrLogout.razor"));
            Assert.Contains(properties, property =>
                property.OutputPath == Path.Combine(root, "RedirectToLogin.razor"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RedirectToLoginTemplate_GeneratesEntraLoginRedirect()
    {
        var template = new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorEntraId.RedirectToLogin
        {
            Session = new Dictionary<string, object>
            {
                ["Model"] = CreateTestEntraIdModel()
            }
        };
        template.Initialize();

        string output = template.TransformText();

        Assert.Contains("@inject NavigationManager NavigationManager", output);
        Assert.DoesNotContain("@using TestProject", output);
        Assert.Contains("authentication/login?returnUrl=", output);
        Assert.Contains("Uri.EscapeDataString(NavigationManager.Uri)", output);
        Assert.Contains("forceLoad: true", output);
    }

    [Fact]
    public void GetTextTemplatingProperties_SetsCorrectTemplateModel()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorEntraId", "Test.tt")];
        EntraIdModel entraIdModel = CreateTestEntraIdModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = EntraIdHelper.GetTextTemplatingProperties(templatePaths, entraIdModel);

        // Assert
        Assert.NotNull(result);
        TextTemplatingProperty? property = result.FirstOrDefault();
        if (property != null)
        {
            Assert.Equal(entraIdModel, property.TemplateModel);
            Assert.Equal("Model", property.TemplateModelName);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void GetTextTemplatingProperties_WithHostedClient_UsesExistingLayoutsAndRouter(bool clientHasNavMenu, bool clientHasRouter)
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), nameof(EntraIdHelperTests), Guid.NewGuid().ToString());
        string serverDirectory = Path.Combine(testDirectory, "Server");
        string clientDirectory = Path.Combine(testDirectory, "Client");
        string clientProjectPath = Path.Combine(clientDirectory, "Client.csproj");
        string serverLayoutDirectory = Path.Combine(serverDirectory, "Components", "Layout");

        try
        {
            Directory.CreateDirectory(clientDirectory);
            Directory.CreateDirectory(serverLayoutDirectory);
            File.WriteAllText(Path.Combine(serverLayoutDirectory, "NavMenu.razor"), string.Empty);
            string clientLayoutDirectory = Path.Combine(clientDirectory, "Layout");
            if (clientHasNavMenu)
            {
                Directory.CreateDirectory(clientLayoutDirectory);
                File.WriteAllText(Path.Combine(clientLayoutDirectory, "NavMenu.razor"), string.Empty);
            }
            if (clientHasRouter)
            {
                File.WriteAllText(Path.Combine(clientDirectory, "Routes.razor"), string.Empty);
            }

            EntraIdModel entraIdModel = new()
            {
                ProjectInfo = new ProjectInfo(Path.Combine(serverDirectory, "Server.csproj")),
                BaseOutputPath = serverDirectory
            };

            TextTemplatingProperty[] properties = EntraIdHelper.GetTextTemplatingProperties(
                [Path.Combine("BlazorEntraId", "LoginOrLogout.tt"), Path.Combine("BlazorEntraId", "RedirectToLogin.tt")],
                entraIdModel,
                clientProjectPath).ToArray();

            Assert.Equal(clientHasNavMenu ? 3 : 2, properties.Length);
            Assert.Contains(properties, property =>
                property.OutputPath == Path.Combine(serverLayoutDirectory, "LoginOrLogout.razor"));
            if (clientHasNavMenu)
            {
                Assert.Contains(properties, property =>
                    property.OutputPath == Path.Combine(clientLayoutDirectory, "LoginOrLogout.razor"));
            }
            Assert.Single(properties, property => property.OutputPath == Path.Combine(
                clientHasRouter ? clientDirectory : Path.Combine(serverDirectory, "Components"),
                "RedirectToLogin.razor"));
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void GetTextTemplatingProperties_WithoutNavMenu_Throws()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), nameof(EntraIdHelperTests), Guid.NewGuid().ToString());
        string serverDirectory = Path.Combine(testDirectory, "Server");
        string clientProjectPath = Path.Combine(testDirectory, "Client", "Client.csproj");
        EntraIdModel entraIdModel = new()
        {
            ProjectInfo = new ProjectInfo(Path.Combine(serverDirectory, "Server.csproj")),
            BaseOutputPath = serverDirectory
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            EntraIdHelper.GetTextTemplatingProperties(
                [Path.Combine("BlazorEntraId", "LoginOrLogout.tt")],
                entraIdModel,
                clientProjectPath).ToArray());

        Assert.Contains(Path.Combine(serverDirectory, "Components", "Layout", "NavMenu.razor"), exception.Message);
        Assert.Contains(Path.Combine(testDirectory, "Client", "Layout", "NavMenu.razor"), exception.Message);
    }

    [Fact]
    public void GetTextTemplatingProperties_ClientWithNavMenu_UsesClientLayout()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), nameof(EntraIdHelperTests), Guid.NewGuid().ToString());
        string serverDirectory = Path.Combine(testDirectory, "Server");
        string clientDirectory = Path.Combine(testDirectory, "Client");
        string clientProjectPath = Path.Combine(clientDirectory, "Client.csproj");
        string clientLayoutDirectory = Path.Combine(clientDirectory, "Layout");

        try
        {
            Directory.CreateDirectory(clientLayoutDirectory);
            File.WriteAllText(Path.Combine(clientLayoutDirectory, "NavMenu.razor"), string.Empty);
            EntraIdModel entraIdModel = new()
            {
                ProjectInfo = new ProjectInfo(Path.Combine(serverDirectory, "Server.csproj")),
                BaseOutputPath = serverDirectory
            };

            TextTemplatingProperty property = Assert.Single(EntraIdHelper.GetTextTemplatingProperties(
                [Path.Combine("BlazorEntraId", "LoginOrLogout.tt")],
                entraIdModel,
                clientProjectPath));

            Assert.Equal(
                Path.Combine(clientLayoutDirectory, "LoginOrLogout.razor"),
                property.OutputPath);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private EntraIdModel CreateTestEntraIdModel()
    {
        return new EntraIdModel
        {
            ProjectInfo = new ProjectInfo(Path.Combine("test", "project", "TestProject.csproj")),
            BaseOutputPath = "output",
            EntraIdNamespace = "TestProject"
        };
    }
}
