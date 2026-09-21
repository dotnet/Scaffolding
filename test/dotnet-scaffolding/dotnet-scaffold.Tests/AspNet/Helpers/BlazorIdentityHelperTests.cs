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
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Files;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class BlazorIdentityHelperTests
{
    [Fact]
    public void GetTextTemplatingProperties_WithEmptyTemplatePaths_ReturnsEmpty()
    {
        // Arrange
        List<string> templatePaths = [];
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNullProjectPath_ReturnsEmpty()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorIdentity", "Test.tt")];
        IdentityModel identityModel = new IdentityModel
        {
            ProjectInfo = new ProjectInfo(null),
            IdentityNamespace = "TestNamespace",
            BaseOutputPath = "output",
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestNamespace.Data",
            DbContextInfo = new DbContextInfo()
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_WithValidInputs_ReturnsProperty()
    {
        // Arrange
        string templatePath = Path.Combine("templates", "ApplicationUser.tt");
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        TextTemplatingProperty? result = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(templatePath, identityModel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(templatePath, result.TemplatePath);
        Assert.Equal(typeof(ApplicationUser), result.TemplateType);
        Assert.Equal("Model", result.TemplateModelName);
        Assert.Equal(identityModel, result.TemplateModel);
        Assert.Contains("Data", result.OutputPath);
        Assert.Contains(identityModel.UserClassName, result.OutputPath);
        Assert.EndsWith(".cs", result.OutputPath);
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_WithNullTemplatePath_ReturnsNull()
    {
        // Arrange
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        TextTemplatingProperty? result = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(null, identityModel);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_WithEmptyTemplatePath_ReturnsNull()
    {
        // Arrange
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        TextTemplatingProperty? result = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(string.Empty, identityModel);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_WithNullProjectPath_ReturnsNull()
    {
        // Arrange
        string templatePath = Path.Combine("templates", "ApplicationUser.tt");
        IdentityModel identityModel = new IdentityModel
        {
            ProjectInfo = new ProjectInfo(null),
            IdentityNamespace = "TestNamespace",
            BaseOutputPath = "output",
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestNamespace.Data",
            DbContextInfo = new DbContextInfo()
        };

        // Act
        TextTemplatingProperty? result = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(templatePath, identityModel);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_WithValidUserClassName_IncludesUserClassNameInOutputPath()
    {
        // Arrange
        string templatePath = Path.Combine("templates", "ApplicationUser.tt");
        IdentityModel identityModel = CreateTestIdentityModel();
        identityModel.UserClassName = "CustomUser";

        // Act
        TextTemplatingProperty? result = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(templatePath, identityModel);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("CustomUser", result.OutputPath);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithPagesPath_UsesRazorExtension()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorIdentity", "Pages", "Login.tt")];
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel);

        // Assert
        TextTemplatingProperty property = Assert.Single(result);
        Assert.Equal(Path.Combine(identityModel.BaseOutputPath, "Components", "Account", "Pages", "Login.razor"), property.OutputPath);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithSharedPath_UsesRazorExtension()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorIdentity", "Shared", "RedirectToLogin.tt")];
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel);

        // Assert
        TextTemplatingProperty property = Assert.Single(result);
        Assert.Equal(Path.Combine(identityModel.BaseOutputPath, "Components", "Account", "Shared", "RedirectToLogin.razor"), property.OutputPath);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNonPagesOrSharedPath_UsesCsExtension()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorIdentity", "IdentityRedirectManager.tt")];
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel);

        // Assert
        TextTemplatingProperty property = Assert.Single(result);
        Assert.Equal(Path.Combine(identityModel.BaseOutputPath, "Components", "Account", "IdentityRedirectManager.cs"), property.OutputPath);
    }

    [Theory]
    [InlineData("net8.0", false, true, true, false)]
    [InlineData("net9.0", false, false, false, false)]
    [InlineData("net9.0", false, true, false, true)]
    [InlineData("net10.0", true, false, true, false)]
    [InlineData("net11.0", true, true, true, true)]
    public void GetTextTemplatingProperties_SelectsProviderAndRedirectLocation(
        string targetFramework, bool usesInteractiveServer, bool hasClient, bool expectProvider, bool expectClientRedirect)
    {
        string projectDirectory = Path.Combine(Path.GetTempPath(), "BlazorIdentityHelperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectDirectory);
        try
        {
            string projectPath = Path.Combine(projectDirectory, "TestProject.csproj");
            File.WriteAllText(projectPath, $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{targetFramework}</TargetFramework>
                  </PropertyGroup>
                </Project>
                """);
            IdentityModel identityModel = CreateTestIdentityModel(projectPath);
            Assert.NotNull(identityModel.ProjectInfo.LowestSupportedTargetFramework);
            identityModel.BaseOutputPath = projectDirectory;
            identityModel.UsesInteractiveServer = usesInteractiveServer;
            string clientDirectory = Path.Combine(projectDirectory, "Client");
            identityModel.BlazorWebAssemblyClientProjectPath = hasClient ? Path.Combine(clientDirectory, "Client.csproj") : null;
            List<string> templatePaths = [
                string.Empty,
                Path.Combine("BlazorIdentity", "Unknown.tt"),
                Path.Combine("BlazorIdentity", "IdentityRevalidatingAuthenticationStateProvider.tt"),
                Path.Combine("BlazorIdentity", "Shared", "RedirectToLogin.tt")
            ];

            var result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel).ToList();

            Assert.Equal(expectProvider ? 2 : 1, result.Count);
            Assert.Equal(expectProvider, result.Any(property => property.TemplateType.Name == "IdentityRevalidatingAuthenticationStateProvider"));
            var redirect = Assert.Single(result, property => property.TemplateType.Name == "RedirectToLogin");
            Assert.Equal(
                expectClientRedirect
                    ? Path.Combine(clientDirectory, "RedirectToLogin.razor")
                    : Path.Combine(projectDirectory, "Components", "Account", "Shared", "RedirectToLogin.razor"),
                redirect.OutputPath);
            Assert.All(result, property =>
            {
                Assert.Same(identityModel, property.TemplateModel);
                Assert.Equal("Model", property.TemplateModelName);
                Assert.Contains(property.TemplatePath, templatePaths);
            });
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    private IdentityModel CreateTestIdentityModel(string? projectPath = null)
    {
        return new IdentityModel
        {
            ProjectInfo = new ProjectInfo(projectPath ?? Path.Combine("test", "project", "TestProject.csproj")),
            IdentityNamespace = "TestNamespace",
            BaseOutputPath = Path.Combine("Components", "Account"),
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestNamespace.Data",
            DbContextInfo = new DbContextInfo()
        };
    }
}
