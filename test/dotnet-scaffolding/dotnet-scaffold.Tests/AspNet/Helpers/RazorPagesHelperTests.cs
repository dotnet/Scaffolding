// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class RazorPagesHelperTests
{
    [Fact]
    public void GetTemplateType_WithCreateTemplate_ReturnsCreateType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.CreateTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.Create), result);
    }

    [Fact]
    public void GetTemplateType_WithCreateModelTemplate_ReturnsCreateModelType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.CreateModelTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.CreateModel), result);
    }

    [Fact]
    public void GetTemplateType_WithIndexTemplate_ReturnsIndexType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.IndexTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.Index), result);
    }

    [Fact]
    public void GetTemplateType_WithIndexModelTemplate_ReturnsIndexModelType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.IndexModelTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.IndexModel), result);
    }

    [Fact]
    public void GetTemplateType_WithDeleteTemplate_ReturnsDeleteType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.DeleteTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.Delete), result);
    }

    [Fact]
    public void GetTemplateType_WithDeleteModelTemplate_ReturnsDeleteModelType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.DeleteModelTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.DeleteModel), result);
    }

    [Fact]
    public void GetTemplateType_WithEditTemplate_ReturnsEditType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.EditTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.Edit), result);
    }

    [Fact]
    public void GetTemplateType_WithEditModelTemplate_ReturnsEditModelType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.EditModelTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.EditModel), result);
    }

    [Fact]
    public void GetTemplateType_WithDetailsTemplate_ReturnsDetailsType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.DetailsTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.Details), result);
    }

    [Fact]
    public void GetTemplateType_WithDetailsModelTemplate_ReturnsDetailsModelType()
    {
        // Arrange
        string templatePath = Path.Combine("templates", RazorPagesHelper.DetailsModelTemplate);

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.DetailsModel), result);
    }

    [Fact]
    public void GetTemplateType_WithNullPath_ReturnsNull()
    {
        // Act
        Type? result = RazorPagesHelper.GetTemplateType(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithEmptyPath_ReturnsNull()
    {
        // Act
        Type? result = RazorPagesHelper.GetTemplateType(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithUnknownTemplate_ReturnsNull()
    {
        // Arrange
        string templatePath = Path.Combine("templates", "Unknown.tt");

        // Act
        Type? result = RazorPagesHelper.GetTemplateType(templatePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsValidTemplate_WithCRUDType_ReturnsTrue()
    {
        // Act
        bool result = RazorPagesHelper.IsValidTemplate("CRUD", "Create");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidTemplate_WithMatchingTemplateType_ReturnsTrue()
    {
        // Act
        bool result = RazorPagesHelper.IsValidTemplate("Create", "Create");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidTemplate_WithMatchingModelTemplateType_ReturnsTrue()
    {
        // Act
        bool result = RazorPagesHelper.IsValidTemplate("Create", "CreateModel");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidTemplate_WithNonMatchingTemplateType_ReturnsFalse()
    {
        // Act
        bool result = RazorPagesHelper.IsValidTemplate("Create", "Edit");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidTemplate_CaseInsensitiveComparison_ReturnsTrue()
    {
        // Act
        bool result = RazorPagesHelper.IsValidTemplate("create", "CREATE");

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
        string result = RazorPagesHelper.GetBaseOutputPath(modelName, projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Pages", result);
        Assert.Contains("Product", result);
    }

    [Fact]
    public void GetBaseOutputPath_WithNullProjectPath_ReturnsPathWithCurrentDirectory()
    {
        // Arrange
        string modelName = "Product";

        // Act
        string result = RazorPagesHelper.GetBaseOutputPath(modelName, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Product", result);
    }

    [Fact]
    public void CreateTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Create.tt", RazorPagesHelper.CreateTemplate);
    }

    [Fact]
    public void CreateModelTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("CreateModel.tt", RazorPagesHelper.CreateModelTemplate);
    }

    [Fact]
    public void DeleteTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Delete.tt", RazorPagesHelper.DeleteTemplate);
    }

    [Fact]
    public void DeleteModelTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("DeleteModel.tt", RazorPagesHelper.DeleteModelTemplate);
    }

    [Fact]
    public void DetailsTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Details.tt", RazorPagesHelper.DetailsTemplate);
    }

    [Fact]
    public void DetailsModelTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("DetailsModel.tt", RazorPagesHelper.DetailsModelTemplate);
    }

    [Fact]
    public void EditTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Edit.tt", RazorPagesHelper.EditTemplate);
    }

    [Fact]
    public void EditModelTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("EditModel.tt", RazorPagesHelper.EditModelTemplate);
    }

    [Fact]
    public void IndexTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Index.tt", RazorPagesHelper.IndexTemplate);
    }

    [Fact]
    public void IndexModelTemplate_Constant_HasExpectedValue()
    {
        // Assert
        Assert.Equal("IndexModel.tt", RazorPagesHelper.IndexModelTemplate);
    }
}
