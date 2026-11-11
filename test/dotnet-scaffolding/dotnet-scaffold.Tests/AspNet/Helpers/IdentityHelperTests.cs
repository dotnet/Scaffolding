// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Files;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class IdentityHelperTests : IDisposable
{
    private readonly string _testDirectory = Directory.CreateTempSubdirectory(nameof(IdentityHelperTests)).FullName;

    [Theory]
    [InlineData("net8.0", true)]
    [InlineData("net8.0", false)]
    [InlineData("net9.0", true)]
    [InlineData("net9.0", false)]
    [InlineData("net10.0", true)]
    [InlineData("net10.0", false)]
    [InlineData("net11.0", true)]
    [InlineData("net11.0", false)]
    public async Task GetTextTemplatingProperties_LoginPartial_GeneratesSharedFileWithoutOverwrite(string targetFramework, bool isRazorPages)
    {
        IdentityModel model = CreateLoginPartialModel(targetFramework, isRazorPages);
        string templatePath = Path.Combine("templates", targetFramework, "Identity", "_LoginPartial.tt");
        string outputPath = Path.Combine(model.BaseOutputPath, isRazorPages ? "Pages" : "Views", "Shared", "_LoginPartial.cshtml");

        TextTemplatingProperty property = Assert.Single(IdentityHelper.GetTextTemplatingProperties([templatePath], model));

        Assert.Equal(outputPath, property.OutputPath);
        Assert.Equal(templatePath, property.TemplatePath);
        Assert.Same(model, property.TemplateModel);

        var step = new TextTemplatingStep(NullLogger<TextTemplatingStep>.Instance)
        {
            TextTemplatingProperties = [property],
            Overwrite = model.Overwrite
        };
        Assert.True(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        Assert.True(File.Exists(outputPath));
        string content = File.ReadAllText(outputPath);
        Assert.Contains("@using Microsoft.AspNetCore.Identity", content);
        Assert.Contains("@using TestProject.Data", content);
        Assert.Contains("@inject SignInManager<CustomUser> SignInManager", content);
        Assert.Contains("@inject UserManager<CustomUser> UserManager", content);
        Assert.Contains("SignInManager.IsSignedIn(User)", content);
        Assert.Contains("asp-page=\"/Account/Manage/Index\"", content);
        Assert.Contains("asp-page=\"/Account/Logout\"", content);
        Assert.Contains("asp-page=\"/Account/Register\"", content);
        Assert.Contains("asp-page=\"/Account/Login\"", content);
        Assert.Contains(isRazorPages
            ? "@Url.Page(\"/Index\", new { area = \"\" })"
            : "@Url.Action(\"Index\", \"Home\", new { area = \"\" })", content);
        Assert.DoesNotContain(isRazorPages ? "@Url.Action(" : "@Url.Page(", content);
        Assert.False(File.Exists(Path.Combine(model.BaseOutputPath, "Areas", "Identity", "_LoginPartial.cshtml")));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void GetTextTemplatingProperties_LoginPartial_PreservesExistingFile(bool isRazorPages, bool overwrite)
    {
        IdentityModel model = CreateLoginPartialModel("net11.0", isRazorPages);
        model.Overwrite = overwrite;
        string outputDirectory = Path.Combine(model.BaseOutputPath, isRazorPages ? "Pages" : "Views", "Shared");
        string outputPath = Path.Combine(outputDirectory, "_LoginPartial.cshtml");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(outputPath, "custom login partial");

        var properties = IdentityHelper.GetTextTemplatingProperties([Path.Combine("Identity", "_LoginPartial.tt")], model);

        Assert.Empty(properties);
        Assert.Equal("custom login partial", File.ReadAllText(outputPath));
    }

    [Fact]
    public void GetTextTemplatingProperties_AccountPage_RemainsUnderIdentityArea()
    {
        IdentityModel model = CreateLoginPartialModel("net11.0", true);
        var property = Assert.Single(IdentityHelper.GetTextTemplatingProperties(
            [Path.Combine("Identity", "Pages", "Account", "Login.tt")], model));

        Assert.Equal(Path.Combine(model.BaseOutputPath, "Areas", "Identity", "Pages", "Account", "Login.cshtml"), property.OutputPath);
    }

    [Theory]
    [InlineData("net8.0", true, false)]
    [InlineData("net8.0", true, true)]
    [InlineData("net8.0", false, false)]
    [InlineData("net8.0", false, true)]
    [InlineData("net9.0", true, false)]
    [InlineData("net9.0", true, true)]
    [InlineData("net9.0", false, false)]
    [InlineData("net9.0", false, true)]
    [InlineData("net10.0", true, false)]
    [InlineData("net10.0", true, true)]
    [InlineData("net10.0", false, false)]
    [InlineData("net10.0", false, true)]
    [InlineData("net11.0", true, false)]
    [InlineData("net11.0", true, true)]
    [InlineData("net11.0", false, false)]
    [InlineData("net11.0", false, true)]
    public async Task GetTextTemplatingProperties_LoginPartial_PreservesRootFileWithoutCreatingSharedDuplicate(
        string targetFramework, bool isRazorPages, bool overwrite)
    {
        IdentityModel model = CreateLoginPartialModel(targetFramework, isRazorPages);
        model.Overwrite = overwrite;
        string rootDirectory = Path.Combine(model.BaseOutputPath, isRazorPages ? "Pages" : "Views");
        Directory.CreateDirectory(rootDirectory);
        string existingPath = Path.Combine(rootDirectory, "_LoginPartial.cshtml");
        File.WriteAllText(existingPath, "custom root login partial");

        var properties = IdentityHelper.GetTextTemplatingProperties(
            [Path.Combine("Identity", "_LoginPartial.tt"), Path.Combine("Identity", "Pages", "Account", "Login.tt")], model);

        var property = Assert.Single(properties);
        Assert.Equal(Path.Combine(model.BaseOutputPath, "Areas", "Identity", "Pages", "Account", "Login.cshtml"), property.OutputPath);
        var step = new TextTemplatingStep(NullLogger<TextTemplatingStep>.Instance)
        {
            TextTemplatingProperties = [property],
            Overwrite = model.Overwrite
        };
        Assert.True(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        Assert.True(File.Exists(property.OutputPath));
        Assert.Equal("custom root login partial", File.ReadAllText(existingPath));
        Assert.False(File.Exists(Path.Combine(rootDirectory, "Shared", "_LoginPartial.cshtml")));
    }

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
            BaseOutputPath = Path.Combine("Areas", "Identity"),
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestNamespace.Data",
            DbContextInfo = new DbContextInfo()
        };
    }

    private IdentityModel CreateLoginPartialModel(string targetFramework, bool isRazorPages)
    {
        string projectDirectory = Path.Combine(_testDirectory, "TestProject");
        Directory.CreateDirectory(projectDirectory);
        string projectPath = Path.Combine(projectDirectory, "TestProject.csproj");
        File.WriteAllText(projectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        return new IdentityModel
        {
            ProjectInfo = new ProjectInfo(projectPath),
            IdentityNamespace = "TestProject.Areas.Identity",
            BaseOutputPath = projectDirectory,
            UserClassName = "CustomUser",
            UserClassNamespace = "TestProject.Data",
            DbContextInfo = new DbContextInfo(),
            IsRazorPages = isRazorPages,
            Overwrite = false
        };
    }

    public void Dispose()
    {
        Directory.Delete(_testDirectory, recursive: true);
    }
}
