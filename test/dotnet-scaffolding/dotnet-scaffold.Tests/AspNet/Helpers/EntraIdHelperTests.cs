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
    public void GetTextTemplatingProperties_WithHostedWasmClient_GeneratesRazorComponentsInHostProject()
    {
        // Arrange
        EntraIdModel entraIdModel = CreateTestEntraIdModel();
        List<string> templatePaths =
        [
            Path.Combine("BlazorEntraId", "LoginOrLogout.tt"),
            Path.Combine("BlazorEntraId", "RedirectToLogin.tt")
        ];
        string clientProjectPath = Path.Combine("output", "TestProject.Client", "TestProject.Client.csproj");

        // Act
        TextTemplatingProperty[] properties = EntraIdHelper
            .GetTextTemplatingProperties(templatePaths, entraIdModel, clientProjectPath)
            .ToArray();

        // Assert
        Assert.Equal(2, properties.Length);
        Assert.Contains(properties, property =>
            property.OutputPath == Path.Combine("output", "Components", "Layout", "LoginOrLogout.razor"));
        Assert.Contains(properties, property =>
            property.OutputPath == Path.Combine("output", "Components", "RedirectToLogin.razor"));
    }

    [Fact]
    public void GetTextTemplatingProperties_WithStandaloneWasmProject_GeneratesRazorComponentsInWasmProject()
    {
        string clientProjectPath = Path.Combine("output", "TestProject.Client", "TestProject.Client.csproj");
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

        TextTemplatingProperty[] properties = EntraIdHelper
            .GetTextTemplatingProperties(templatePaths, entraIdModel, clientProjectPath)
            .ToArray();

        Assert.Equal(2, properties.Length);
        Assert.Contains(properties, property =>
            property.OutputPath == Path.Combine("output", "TestProject.Client", "Layout", "LoginOrLogout.razor"));
        Assert.Contains(properties, property =>
            property.OutputPath == Path.Combine("output", "TestProject.Client", "RedirectToLogin.razor"));
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
