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
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Files;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class IdentityHelperTests
{
    [Fact]
    public void GetTextTemplatingProperties_WithEmptyFilePaths_ReturnsEmpty()
    {
        // Arrange
        List<string> filePaths = [];
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        IEnumerable<TextTemplatingProperty> result = IdentityHelper.GetTextTemplatingProperties(filePaths, identityModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNullProjectPath_ReturnsEmpty()
    {
        // Arrange
        List<string> filePaths = [Path.Combine("Identity", "Test.tt")];
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
        IEnumerable<TextTemplatingProperty> result = IdentityHelper.GetTextTemplatingProperties(filePaths, identityModel);

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
        TextTemplatingProperty? result = IdentityHelper.GetApplicationUserTextTemplatingProperty(templatePath, identityModel);

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
        TextTemplatingProperty? result = IdentityHelper.GetApplicationUserTextTemplatingProperty(null, identityModel);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_WithEmptyTemplatePath_ReturnsNull()
    {
        // Arrange
        IdentityModel identityModel = CreateTestIdentityModel();

        // Act
        TextTemplatingProperty? result = IdentityHelper.GetApplicationUserTextTemplatingProperty(string.Empty, identityModel);

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
        TextTemplatingProperty? result = IdentityHelper.GetApplicationUserTextTemplatingProperty(templatePath, identityModel);

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
        TextTemplatingProperty? result = IdentityHelper.GetApplicationUserTextTemplatingProperty(templatePath, identityModel);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("CustomUser", result.OutputPath);
    }

    private IdentityModel CreateTestIdentityModel()
    {
        return new IdentityModel
        {
            ProjectInfo = new ProjectInfo(Path.Combine("test", "project", "TestProject.csproj")),
            IdentityNamespace = "TestNamespace",
            BaseOutputPath = "Areas\\Identity",
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestNamespace.Data",
            DbContextInfo = new DbContextInfo()
        };
    }
}
