// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Files;
using Xunit;
using ProjectInfo = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.ProjectInfo;

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

    [Theory]
    [InlineData("DifferentFolder")]
    [InlineData("Folder.With.Dots")]
    public void GetTextTemplatingProperties_UsesActualProjectDirectory(string directoryName)
    {
        var model = CreateTestIdentityModel();
        model.BaseOutputPath = Path.Combine(Path.GetTempPath(), directoryName);
        var templatePath = Path.Combine("Identity", "Templates", "Identity", "Pages", "Account", "Login.tt");

        var property = Assert.Single(IdentityHelper.GetTextTemplatingProperties([templatePath], model));

        Assert.Equal(Path.Combine(model.BaseOutputPath, "Areas", "Identity", "Pages", "Account", "Login.cshtml"), property.OutputPath);
    }

    [Theory]
    [InlineData("ApplicationDbContext", "App", true)]
    [InlineData("OldApplicationDbContext", "App", false)]
    [InlineData("ApplicationDbContext", "Other", false)]
    public void HasMigration_MatchesExactContext(string name, string ns, bool expected)
    {
        var compilation = CSharpCompilation.Create("Snapshots",
            [CSharpSyntaxTree.ParseText($$"""
using System;
public class DbContextAttribute(Type context) : Attribute {}
public class ModelSnapshot {}
namespace {{ns}}
{
    public class {{name}} {}
    // ApplicationDbContext may also appear in unrelated snapshots.
    [DbContext(typeof({{name}}))]
    public class Snapshot : ModelSnapshot {}
}
""")], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var snapshot = compilation.GetTypeByMetadataName($"{ns}.Snapshot")!;
        Assert.Equal(expected, IdentityHelper.HasMigration([snapshot], new DbContextInfo
        {
            DbContextClassName = "ApplicationDbContext",
            DbContextNamespace = "App"
        }));
    }

    [Fact]
    public void GetApplicationUserTextTemplatingProperty_PreservesExistingUser()
    {
        var model = CreateTestIdentityModel();
        model.HasExistingUser = true;
        Assert.Null(IdentityHelper.GetApplicationUserTextTemplatingProperty("ApplicationUser.tt", model));
    }

    [Theory]
    [InlineData(false, "@Url.Action")]
    [InlineData(true, "@Url.Page")]
    public void LoginPartial_RendersHostNavigation(bool isRazorPages, string returnUrl)
    {
        var model = CreateTestIdentityModel();
        model.IsRazorPages = isRazorPages;
        ITextTransformation template = new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.Files._LoginPartial
        {
            Session = new Dictionary<string, object> { ["Model"] = model }
        };
        template.Initialize();
        var content = template.TransformText();
        Assert.Contains("SignInManager<ApplicationUser>", content);
        Assert.Contains(returnUrl, content);
        Assert.Contains("asp-page=\"/Account/Logout\"", content);
        Assert.Contains("asp-page=\"/Account/Register\"", content);
    }

    [Fact]
    public void GetLoginPartialTextTemplatingProperty_DoesNotOverwriteCustomPartial()
    {
        var directory = Path.Combine(Path.GetTempPath(), nameof(IdentityHelperTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "Views", "Shared"));
        try
        {
            var model = CreateTestIdentityModel(Path.Combine(directory, "TestProject.csproj"));
            var property = IdentityHelper.GetLoginPartialTextTemplatingProperty("_LoginPartial.tt", model);
            Assert.NotNull(property);
            File.WriteAllText(property.OutputPath, "Custom navigation");
            Assert.Null(IdentityHelper.GetLoginPartialTextTemplatingProperty("_LoginPartial.tt", model));
            Assert.Equal("Custom navigation", File.ReadAllText(property.OutputPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private IdentityModel CreateTestIdentityModel(string? projectPath = null)
    {
        return new IdentityModel
        {
            ProjectInfo = new ProjectInfo(projectPath ?? Path.Combine("test", "project", "TestProject.csproj")),
            IdentityNamespace = "TestNamespace",
            BaseOutputPath = Path.Combine("Areas", "Identity"),
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestNamespace.Data",
            DbContextInfo = new DbContextInfo()
        };
    }
}
