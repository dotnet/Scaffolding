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
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Views;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class ViewHelperTests
{
    [Fact]
    public void GetTextTemplatingProperties_WithValidCreateTemplate_ReturnsExpectedProperty()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string templatePath = Path.Combine("templates", ViewHelper.CreateTemplate);
        List<string> templatePaths = [templatePath];
        
        ViewModel viewModel = new ViewModel
        {
            PageType = "Create",
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product"
            },
            ProjectInfo = new ProjectInfo(projectPath),
            DbContextInfo = new DbContextInfo()
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = ViewHelper.GetTextTemplatingProperties(templatePaths, viewModel);

        // Assert
        Assert.NotNull(result);
        TextTemplatingProperty? property = result.FirstOrDefault();
        Assert.NotNull(property);
        Assert.Equal(typeof(Create), property.TemplateType);
        Assert.Equal(templatePath, property.TemplatePath);
        Assert.Contains("Product", property.OutputPath);
        Assert.EndsWith(".cshtml", property.OutputPath);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithValidIndexTemplate_ReturnsExpectedProperty()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string templatePath = Path.Combine("templates", ViewHelper.IndexTemplate);
        List<string> templatePaths = [templatePath];
        
        ViewModel viewModel = new ViewModel
        {
            PageType = "Index",
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product"
            },
            ProjectInfo = new ProjectInfo(projectPath),
            DbContextInfo = new DbContextInfo()
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = ViewHelper.GetTextTemplatingProperties(templatePaths, viewModel);

        // Assert
        Assert.NotNull(result);
        TextTemplatingProperty? property = result.FirstOrDefault();
        Assert.NotNull(property);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Views.Index), property.TemplateType);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithCRUDPageType_ReturnsPropertiesForAllTemplates()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        List<string> templatePaths =
        [
            Path.Combine("templates", ViewHelper.CreateTemplate),
            Path.Combine("templates", ViewHelper.EditTemplate),
            Path.Combine("templates", ViewHelper.DeleteTemplate),
            Path.Combine("templates", ViewHelper.DetailsTemplate),
            Path.Combine("templates", ViewHelper.IndexTemplate)
        ];
        
        ViewModel viewModel = new ViewModel
        {
            PageType = "CRUD",
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product"
            },
            ProjectInfo = new ProjectInfo(projectPath),
            DbContextInfo = new DbContextInfo()
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = ViewHelper.GetTextTemplatingProperties(templatePaths, viewModel);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count());
        Assert.All(result, property =>
        {
            Assert.NotNull(property.TemplateType);
            Assert.Contains("Product", property.OutputPath);
            Assert.EndsWith(".cshtml", property.OutputPath);
        });
    }

    [Fact]
    public void GetTextTemplatingProperties_WithEmptyTemplatePaths_ReturnsEmpty()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        List<string> templatePaths = new List<string>();
        
        ViewModel viewModel = new ViewModel
        {
            PageType = "Create",
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product"
            },
            ProjectInfo = new ProjectInfo(projectPath),
            DbContextInfo = new DbContextInfo()
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = ViewHelper.GetTextTemplatingProperties(templatePaths, viewModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNullTemplatePaths_ReturnsEmpty()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        IEnumerable<string>? templatePaths = null;
        
        ViewModel viewModel = new ViewModel
        {
            PageType = "Create",
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product"
            },
            ProjectInfo = new ProjectInfo(projectPath),
            DbContextInfo = new DbContextInfo()
        };

        // Act
        IEnumerable<TextTemplatingProperty> result = ViewHelper.GetTextTemplatingProperties(templatePaths!, viewModel);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTemplateType_WithCreateTemplate_ReturnsCreateType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", ViewHelper.CreateTemplate);

        // Act
        Type? result = ViewHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Create), result);
    }

    [Fact]
    public void GetTemplateType_WithIndexTemplate_ReturnsIndexType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", ViewHelper.IndexTemplate);

        // Act
        Type? result = ViewHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Views.Index), result);
    }

    [Fact]
    public void GetTemplateType_WithDeleteTemplate_ReturnsDeleteType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", ViewHelper.DeleteTemplate);

        // Act
        Type? result = ViewHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Delete), result);
    }

    [Fact]
    public void GetTemplateType_WithEditTemplate_ReturnsEditType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", ViewHelper.EditTemplate);

        // Act
        Type? result = ViewHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Edit), result);
    }

    [Fact]
    public void GetTemplateType_WithDetailsTemplate_ReturnsDetailsType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", ViewHelper.DetailsTemplate);

        // Act
        Type? result = ViewHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Details), result);
    }

    [Fact]
    public void GetTemplateType_WithNullPath_ReturnsNull()
    {
        // Act
        Type? result = ViewHelper.GetTemplateType(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithEmptyPath_ReturnsNull()
    {
        // Act
        Type? result = ViewHelper.GetTemplateType(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithUnknownTemplate_ReturnsNull()
    {
        // Arrange
        string templatePath = Path.Combine("templates", "Unknown.tt");

        // Act
        Type? result = ViewHelper.GetTemplateType(templatePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Create.tt", ViewHelper.CreateTemplate);
    }

    [Fact]
    public void DeleteTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Delete.tt", ViewHelper.DeleteTemplate);
    }

    [Fact]
    public void DetailsTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Details.tt", ViewHelper.DetailsTemplate);
    }

    [Fact]
    public void EditTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Edit.tt", ViewHelper.EditTemplate);
    }

    [Fact]
    public void IndexTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Index.tt", ViewHelper.IndexTemplate);
    }
}
