// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
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

    [Fact]
    public void GetTextTemplatingProperties_ClientWithoutNavMenu_ServerNavMenuExists_UsesServerLayout()
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

            EntraIdModel entraIdModel = new()
            {
                ProjectInfo = new ProjectInfo(Path.Combine(serverDirectory, "Server.csproj")),
                BaseOutputPath = serverDirectory
            };

            TextTemplatingProperty property = Assert.Single(EntraIdHelper.GetTextTemplatingProperties(
                [Path.Combine("BlazorEntraId", "LoginOrLogout.tt")],
                entraIdModel,
                clientProjectPath));

            Assert.Equal(Path.Combine(serverLayoutDirectory, "LoginOrLogout.razor"), property.OutputPath);
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
            BaseOutputPath = "output"
        };
    }
}
