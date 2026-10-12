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
using ScaffoldIdentityModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Models.IdentityModel;
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
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();

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
        ScaffoldIdentityModel identityModel = new ScaffoldIdentityModel
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
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();

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
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();

        // Act
        TextTemplatingProperty? result = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(null, identityModel);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_WithEmptyTemplatePath_ReturnsNull()
    {
        // Arrange
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();

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
        ScaffoldIdentityModel identityModel = new ScaffoldIdentityModel
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
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();
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
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();

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
        List<string> templatePaths = [Path.Combine("BlazorIdentity", "Shared", "ManageNavMenu.tt")];
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel);

        // Assert
        TextTemplatingProperty property = Assert.Single(result);
        Assert.Equal(Path.Combine(identityModel.BaseOutputPath, "Components", "Account", "Shared", "ManageNavMenu.razor"), property.OutputPath);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNonPagesOrSharedPath_UsesCsExtension()
    {
        // Arrange
        List<string> templatePaths = [Path.Combine("BlazorIdentity", "IdentityRedirectManager.tt")];
        ScaffoldIdentityModel identityModel = CreateTestIdentityModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = BlazorIdentityHelper.GetTextTemplatingProperties(templatePaths, identityModel);

        // Assert
        TextTemplatingProperty property = Assert.Single(result);
        Assert.Equal(Path.Combine(identityModel.BaseOutputPath, "Components", "Account", "IdentityRedirectManager.cs"), property.OutputPath);
    }

    private ScaffoldIdentityModel CreateTestIdentityModel()
    {
        return new ScaffoldIdentityModel
        {
            ProjectInfo = new ProjectInfo(Path.Combine("test", "project", "TestProject.csproj")),
            IdentityNamespace = "TestNamespace",
            BaseOutputPath = Path.Combine("Components", "Account"),
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestNamespace.Data",
            DbContextInfo = new DbContextInfo()
        };
    }
}
