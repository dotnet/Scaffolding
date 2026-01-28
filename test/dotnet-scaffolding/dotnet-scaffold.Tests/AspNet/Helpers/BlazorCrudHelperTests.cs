// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class BlazorCrudHelperTests
{
    [Fact]
    public void GetTemplateType_WithCreateTemplate_ReturnsCreateType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", BlazorCrudHelper.CreateBlazorTemplate);

        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Create), result);
    }

    [Fact]
    public void GetTemplateType_WithIndexTemplate_ReturnsIndexType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", BlazorCrudHelper.IndexBlazorTemplate);

        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Index), result);
    }

    [Fact]
    public void GetTemplateType_WithDeleteTemplate_ReturnsDeleteType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", BlazorCrudHelper.DeleteBlazorTemplate);

        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Delete), result);
    }

    [Fact]
    public void GetTemplateType_WithEditTemplate_ReturnsEditType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", BlazorCrudHelper.EditBlazorTemplate);

        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Edit), result);
    }

    [Fact]
    public void GetTemplateType_WithDetailsTemplate_ReturnsDetailsType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", BlazorCrudHelper.DetailsBlazorTemplate);

        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.Details), result);
    }

    [Fact]
    public void GetTemplateType_WithNotFoundTemplate_ReturnsNotFoundType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", BlazorCrudHelper.NotFoundBlazorTemplate);

        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorCrud.NotFound), result);
    }

    [Fact]
    public void GetTemplateType_WithNullPath_ReturnsNull()
    {
        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithEmptyPath_ReturnsNull()
    {
        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithUnknownTemplate_ReturnsNull()
    {
        // Arrange
        string templatePath = Path.Combine("templates", "Unknown.tt");

        // Act
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsValidTemplate_WithCRUDType_ReturnsTrue()
    {
        // Act
        bool result = BlazorCrudHelper.IsValidTemplate("CRUD", "Create");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidTemplate_WithMatchingTemplateType_ReturnsTrue()
    {
        // Act
        bool result = BlazorCrudHelper.IsValidTemplate("Create", "Create");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidTemplate_WithNonMatchingTemplateType_ReturnsFalse()
    {
        // Act
        bool result = BlazorCrudHelper.IsValidTemplate("Create", "Edit");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidTemplate_CaseInsensitiveComparison_ReturnsTrue()
    {
        // Act
        bool result = BlazorCrudHelper.IsValidTemplate("create", "CREATE");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetBaseOutputPath_WithValidInputs_ReturnsExpectedPath()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string modelName = "Product";

        // Act
        string result = BlazorCrudHelper.GetBaseOutputPath(modelName, projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Components", result);
        Assert.Contains("Pages", result);
        Assert.Contains("ProductPages", result);
    }

    [Fact]
    public void GetBaseOutputPath_WithNullProjectPath_ReturnsPathWithCurrentDirectory()
    {
        // Arrange
        string modelName = "Product";

        // Act
        string result = BlazorCrudHelper.GetBaseOutputPath(modelName, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("ProductPages", result);
    }

    [Fact]
    public void CreateBlazorTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Create.tt", BlazorCrudHelper.CreateBlazorTemplate);
    }

    [Fact]
    public void DeleteBlazorTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Delete.tt", BlazorCrudHelper.DeleteBlazorTemplate);
    }

    [Fact]
    public void DetailsBlazorTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Details.tt", BlazorCrudHelper.DetailsBlazorTemplate);
    }

    [Fact]
    public void EditBlazorTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Edit.tt", BlazorCrudHelper.EditBlazorTemplate);
    }

    [Fact]
    public void IndexBlazorTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Index.tt", BlazorCrudHelper.IndexBlazorTemplate);
    }

    [Fact]
    public void NotFoundBlazorTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("NotFound.tt", BlazorCrudHelper.NotFoundBlazorTemplate);
    }

    [Fact]
    public void CRUDPages_Contains_AllExpectedPageTypes()
    {
        // Assert
        Assert.Contains("CRUD", BlazorCrudHelper.CRUDPages);
        Assert.Contains("Create", BlazorCrudHelper.CRUDPages);
        Assert.Contains("Delete", BlazorCrudHelper.CRUDPages);
        Assert.Contains("Details", BlazorCrudHelper.CRUDPages);
        Assert.Contains("Edit", BlazorCrudHelper.CRUDPages);
        Assert.Contains("Index", BlazorCrudHelper.CRUDPages);
        Assert.Contains("NotFound", BlazorCrudHelper.CRUDPages);
    }
}
