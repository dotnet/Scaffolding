// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Xunit;

using AspNetProjectInfo = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.ProjectInfo;

using Net9MvcEfController = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.EfController.MvcEfController;
using Net10MvcEfController = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.EfController.MvcEfController;
using Net11MvcEfController = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.EfController.MvcEfController;
using Net9RazorCreateModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.RazorPages.CreateModel;
using Net10RazorCreateModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.RazorPages.CreateModel;
using Net11RazorCreateModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.RazorPages.CreateModel;
using Net9CreateView = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.Views.Create;
using Net10CreateView = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.Views.Create;
using Net11CreateView = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Views.Create;
using Net9EditView = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net9.Views.Edit;
using Net10EditView = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.Views.Edit;
using Net11EditView = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Views.Edit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

/// <summary>
/// Tests for the MvcEfController template to verify that the [Bind] attribute and
/// action parameter names are generated from the actual model properties, not
/// hardcoded values (regression for "movie" bug).
/// </summary>
public class MvcEfControllerTemplateTests
{
    #region Net9 — [Bind] uses actual model properties

    [Fact]
    public void Net9_Create_BindAttribute_ContainsActualModelProperties()
    {
        // Arrange – Blog model: BlogId (PK), Url
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet9Template(model);

        // Act
        string result = template.TransformText();

        // Assert – must contain the real property names, not "Title,ReleaseDate,Genre,Price"
        Assert.Contains("[Bind(\"BlogId,Url\")]", result);
    }

    [Fact]
    public void Net9_Create_ParameterName_UsesModelNameLowerCase()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet9Template(model);

        // Act
        string result = template.TransformText();

        // Assert – parameter must be named 'blog', not 'movie'
        Assert.Contains("Create([Bind(", result);
        Assert.Contains("] Blog blog)", result);
        Assert.DoesNotContain("] Blog movie)", result);
    }

    [Fact]
    public void Net9_WhenModelAndDbContextShareNamespace_EmitsSingleUsing()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet9Template(model);

        string result = template.TransformText();

        Assert.Equal(1, CountOccurrences(result, "using Models;"));
    }

    [Fact]
    public void Net9_Edit_BindAttribute_ContainsActualModelProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet9Template(model);

        // Act
        string result = template.TransformText();

        // Assert – Edit POST [Bind] must use real properties, not "Id,Title,ReleaseDate,Genre,Price"
        // There are two [Bind] occurrences; both should reference the real property names.
        int bindOccurrences = CountOccurrences(result, "[Bind(\"BlogId,Url\")]");
        Assert.Equal(2, bindOccurrences);
    }

    [Fact]
    public void Net9_Edit_ParameterName_UsesModelNameLowerCase()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet9Template(model);

        // Act
        string result = template.TransformText();

        // Assert – Edit POST parameter must be named 'blog', not 'movie'
        Assert.DoesNotContain("] Blog movie)", result);
        Assert.Contains("] Blog blog)", result);
    }

    [Fact]
    public void Net9_DoesNotContainHardcodedMovieProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet9Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.DoesNotContain("Title,ReleaseDate,Genre,Price", result);
        Assert.DoesNotContain("Id,Title,ReleaseDate,Genre,Price", result);
        Assert.DoesNotContain(" movie)", result);
    }

    [Fact]
    public void Net9_WhenEntitySetVariableNameEmpty_FallsBackToModelName()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        model.DbContextInfo.EntitySetVariableName = string.Empty;
        var template = CreateNet9Template(model);

        string result = template.TransformText();

        Assert.Contains("_context.Blog.ToListAsync()", result);
        Assert.DoesNotContain("_context..ToListAsync()", result);
    }

    [Fact]
    public void Net9_WhenEntitySetVariableNameWhitespace_FallsBackToModelName()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        model.DbContextInfo.EntitySetVariableName = "   ";
        var template = CreateNet9Template(model);

        string result = template.TransformText();

        Assert.Contains("_context.Blog.ToListAsync()", result);
        Assert.DoesNotContain("_context.   .ToListAsync()", result);
    }

    [Fact]
    public void Net9_Create_BindAttribute_UsesProductProperties()
    {
        // Arrange – Product model: ProductId (PK), Name, Price
        EfControllerModel model = CreateProductEfControllerModel();
        var template = CreateNet9Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.Contains("[Bind(\"ProductId,Name,Price\")]", result);
        Assert.Contains("] Product product)", result);
    }

    #endregion

    #region Net10 — [Bind] uses actual model properties

    [Fact]
    public void Net10_Create_BindAttribute_ContainsActualModelProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet10Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.Contains("[Bind(\"BlogId,Url\")]", result);
    }

    [Fact]
    public void Net10_Create_ParameterName_UsesModelNameLowerCase()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet10Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.Contains("] Blog blog)", result);
        Assert.DoesNotContain("] Blog movie)", result);
    }

    [Fact]
    public void Net10_WhenModelAndDbContextShareNamespace_EmitsSingleUsing()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet10Template(model);

        string result = template.TransformText();

        Assert.Equal(1, CountOccurrences(result, "using Models;"));
    }

    [Fact]
    public void Net10_Edit_BindAttribute_ContainsActualModelProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet10Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        int bindOccurrences = CountOccurrences(result, "[Bind(\"BlogId,Url\")]");
        Assert.Equal(2, bindOccurrences);
    }

    [Fact]
    public void Net10_DoesNotContainHardcodedMovieProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet10Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.DoesNotContain("Title,ReleaseDate,Genre,Price", result);
        Assert.DoesNotContain("Id,Title,ReleaseDate,Genre,Price", result);
        Assert.DoesNotContain(" movie)", result);
    }

    [Fact]
    public void Net10_WhenEntitySetVariableNameEmpty_FallsBackToModelName()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        model.DbContextInfo.EntitySetVariableName = string.Empty;
        var template = CreateNet10Template(model);

        string result = template.TransformText();

        Assert.Contains("_context.Blog.ToListAsync()", result);
        Assert.DoesNotContain("_context..ToListAsync()", result);
    }

    [Fact]
    public void Net10_WhenEntitySetVariableNameWhitespace_FallsBackToModelName()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        model.DbContextInfo.EntitySetVariableName = "   ";
        var template = CreateNet10Template(model);

        string result = template.TransformText();

        Assert.Contains("_context.Blog.ToListAsync()", result);
        Assert.DoesNotContain("_context.   .ToListAsync()", result);
    }

    #endregion

    #region Net11 — [Bind] uses actual model properties

    [Fact]
    public void Net11_Create_BindAttribute_ContainsActualModelProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet11Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.Contains("[Bind(\"BlogId,Url\")]", result);
    }

    [Fact]
    public void Net11_Create_ParameterName_UsesModelNameLowerCase()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet11Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.Contains("] Blog blog)", result);
        Assert.DoesNotContain("] Blog movie)", result);
    }

    [Fact]
    public void Net11_WhenModelAndDbContextShareNamespace_EmitsSingleUsing()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet11Template(model);

        string result = template.TransformText();

        Assert.Equal(1, CountOccurrences(result, "using Models;"));
    }

    [Fact]
    public void Net11_Edit_BindAttribute_ContainsActualModelProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet11Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        int bindOccurrences = CountOccurrences(result, "[Bind(\"BlogId,Url\")]");
        Assert.Equal(2, bindOccurrences);
    }

    [Fact]
    public void Net11_DoesNotContainHardcodedMovieProperties()
    {
        // Arrange
        EfControllerModel model = CreateBlogEfControllerModel();
        var template = CreateNet11Template(model);

        // Act
        string result = template.TransformText();

        // Assert
        Assert.DoesNotContain("Title,ReleaseDate,Genre,Price", result);
        Assert.DoesNotContain("Id,Title,ReleaseDate,Genre,Price", result);
        Assert.DoesNotContain(" movie)", result);
    }

    [Fact]
    public void Net11_WhenEntitySetVariableNameEmpty_FallsBackToModelName()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        model.DbContextInfo.EntitySetVariableName = string.Empty;
        var template = CreateNet11Template(model);

        string result = template.TransformText();

        Assert.Contains("_context.Blog.ToListAsync()", result);
        Assert.DoesNotContain("_context..ToListAsync()", result);
    }

    [Fact]
    public void Net11_WhenEntitySetVariableNameWhitespace_FallsBackToModelName()
    {
        EfControllerModel model = CreateBlogEfControllerModel();
        model.DbContextInfo.EntitySetVariableName = "   ";
        var template = CreateNet11Template(model);

        string result = template.TransformText();

        Assert.Contains("_context.Blog.ToListAsync()", result);
        Assert.DoesNotContain("_context.   .ToListAsync()", result);
    }

    #endregion

    #region Razor Pages — single namespace import regression

    [Fact]
    public void Net9_RazorCreateModel_WhenModelAndDbContextShareNamespace_EmitsSingleUsing()
    {
        RazorPageModel model = CreateBlogRazorPageModel();
        var template = CreateNet9RazorTemplate(model);

        string result = template.TransformText();

        Assert.Equal(1, CountOccurrences(result, "using Models;"));
    }

    [Fact]
    public void Net10_RazorCreateModel_WhenModelAndDbContextShareNamespace_EmitsSingleUsing()
    {
        RazorPageModel model = CreateBlogRazorPageModel();
        var template = CreateNet10RazorTemplate(model);

        string result = template.TransformText();

        Assert.Equal(1, CountOccurrences(result, "using Models;"));
    }

    [Fact]
    public void Net11_RazorCreateModel_WhenModelAndDbContextShareNamespace_EmitsSingleUsing()
    {
        RazorPageModel model = CreateBlogRazorPageModel();
        var template = CreateNet11RazorTemplate(model);

        string result = template.TransformText();

        Assert.Equal(1, CountOccurrences(result, "using Models;"));
    }

    #endregion

    #region Foreign keys — SelectList and <select> parity with legacy scaffolder

    [Theory]
    [InlineData("net9")]
    [InlineData("net10")]
    [InlineData("net11")]
    public void ModelWithForeignKey_RendersSelectListAndSkipsNavigation(string targetFramework)
    {
        ModelInfo modelInfo = CreateCountryCityModelInfo("City");

        var (controller, createView, editView) = TransformCountryCityTemplates(targetFramework, modelInfo);

        Assert.Contains("using Microsoft.AspNetCore.Mvc.Rendering;", controller);
        Assert.Equal(1, CountOccurrences(controller, "ViewData[\"CountryId\"] = new SelectList(_context.Countries, \"Id\", \"Name\");"));
        Assert.Equal(3, CountOccurrences(controller, "ViewData[\"CountryId\"] = new SelectList(_context.Countries, \"Id\", \"Name\", city.CountryId);"));
        foreach (string view in new[] { createView, editView })
        {
            Assert.Contains("<label asp-for=\"CountryId\" class=\"control-label\"></label>", view);
            Assert.Contains("<select asp-for=\"CountryId\" class=\"form-control\" asp-items=\"ViewBag.CountryId\"></select>", view);
            Assert.Contains("<span asp-validation-for=\"CountryId\" class=\"text-danger\"></span>", view);
            Assert.DoesNotContain("<input asp-for=\"CountryId\"", view);
            Assert.DoesNotContain("asp-for=\"Country\"", view);
        }
    }

    [Theory]
    [InlineData("net9")]
    [InlineData("net10")]
    [InlineData("net11")]
    public void ModelWithoutForeignKey_RendersNoSelectList(string targetFramework)
    {
        // Country has a collection navigation but no foreign key; output must stay as before.
        ModelInfo modelInfo = CreateCountryCityModelInfo("Country");

        var (controller, createView, editView) = TransformCountryCityTemplates(targetFramework, modelInfo);

        Assert.Empty(modelInfo.ForeignKeyNavigations);
        Assert.DoesNotContain("SelectList", controller);
        Assert.DoesNotContain("Microsoft.AspNetCore.Mvc.Rendering", controller);
        Assert.StartsWith(
            "\r\nusing Microsoft.AspNetCore.Mvc;\r\nusing Microsoft.EntityFrameworkCore;\r\nusing Models;\r\n\r\npublic class CountrysController",
            controller,
            System.StringComparison.Ordinal);
        foreach (string view in new[] { createView, editView })
        {
            Assert.DoesNotContain("<select", view);
            Assert.Contains("<input asp-for=\"Name\" class=\"form-control\" />", view);
            Assert.Contains("<input asp-for=\"Cities\" class=\"form-control\" />", view);
        }
    }

    [Fact]
    public void NotMappedNavigation_IsNotTreatedAsForeignKey()
    {
        ModelInfo modelInfo = CreateCountryCityModelInfo("City", "[System.ComponentModel.DataAnnotations.Schema.NotMapped]");

        var (controller, createView, editView) = TransformCountryCityTemplates("net11", modelInfo);

        Assert.Empty(modelInfo.ForeignKeyNavigations);
        Assert.DoesNotContain("SelectList", controller);
        Assert.DoesNotContain("<select", createView);
        Assert.DoesNotContain("<select", editView);
    }

    [Fact]
    public void NullableForeignKey_RendersEmptyOption()
    {
        ModelInfo modelInfo = CreateCountryCityModelInfo("City", countryIdDeclaration: "public int? CountryId { get; set; }");

        var (_, createView, editView) = TransformCountryCityTemplates("net11", modelInfo);

        Assert.True(Assert.Single(modelInfo.ForeignKeyNavigations).IsNullable);
        foreach (string view in new[] { createView, editView })
        {
            Assert.Contains("<select asp-for=\"CountryId\" class=\"form-control\" asp-items=\"ViewBag.CountryId\">\r\n                    <option value=\"\">-- select --</option>\r\n                </select>", view);
        }
    }

    [Fact]
    public void NotMappedForeignKeyProperty_IsNotTreatedAsForeignKey()
    {
        ModelInfo modelInfo = CreateCountryCityModelInfo("City", countryIdDeclaration: "[System.ComponentModel.DataAnnotations.Schema.NotMapped] public int CountryId { get; set; }");

        Assert.Empty(modelInfo.ForeignKeyNavigations);
    }

    [Fact]
    public void SingleKeyAttribute_DeclaredAfterId_IsUsedAsPrincipalKey()
    {
        ModelInfo modelInfo = CreateModelInfoFromSource(@"
namespace Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
        [System.ComponentModel.DataAnnotations.Key]
        public int Code { get; set; }
    }

    public class City
    {
        public int Id { get; set; }
        public int CountryCode { get; set; }
        public Country? Country { get; set; }
    }

    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<Country> Countries { get; set; } = null!;
    }
}", "City");

        var (controller, _, _) = TransformCountryCityTemplates("net11", modelInfo);

        Assert.Equal("CountryCode", Assert.Single(modelInfo.ForeignKeyNavigations).ForeignKeyPropertyName);
        Assert.Contains("ViewData[\"CountryCode\"] = new SelectList(_context.Countries, \"Code\", \"Name\");", controller);
    }

    [Fact]
    public void HasPrincipalKeyInOnModelCreating_RendersNoSelectList()
    {
        ModelInfo modelInfo = CreateModelInfoFromSource(@"
namespace Models
{
    public class Country
    {
        public int Id { get; set; }
        public int Code { get; set; }
        public string Name { get; set; } = """";
        public ICollection<City> Cities { get; set; } = new List<City>();
    }

    public class City
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public Country? Country { get; set; }
    }

    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<Country> Countries { get; set; } = null!;

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<City>().HasOne(c => c.Country).WithMany(c => c.Cities).HasForeignKey(c => c.CountryId).HasPrincipalKey(c => c.Code);
        }
    }
}", "City");

        var (controller, createView, editView) = TransformCountryCityTemplates("net11", modelInfo);

        Assert.Empty(modelInfo.ForeignKeyNavigations);
        Assert.DoesNotContain("SelectList", controller);
        Assert.DoesNotContain("<select", createView);
        Assert.DoesNotContain("<select", editView);
    }

    [Fact]
    public void NestedPrincipalWithoutDbSet_UsesFullyQualifiedSetFallback()
    {
        ModelInfo modelInfo = CreateModelInfoFromSource(@"
namespace Models
{
    public class Outer
    {
        public class Country
        {
            public int Id { get; set; }
            public string Name { get; set; } = """";
        }
    }

    public class City
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public Outer.Country? Country { get; set; }
    }

    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<City> Cities { get; set; } = null!;
    }
}", "City");

        var (controller, _, _) = TransformCountryCityTemplates("net11", modelInfo);

        Assert.Equal("Set<global::Models.Outer.Country>()", Assert.Single(modelInfo.ForeignKeyNavigations).EntitySetName);
        Assert.Contains("ViewData[\"CountryId\"] = new SelectList(_context.Set<global::Models.Outer.Country>(), \"Id\", \"Name\");", controller);
    }

    #endregion

    #region Helpers

    private static ModelInfo CreateCountryCityModelInfo(string modelTypeName, string countryNavigationAttribute = "", string countryIdDeclaration = "public int CountryId { get; set; }")
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@"
#nullable enable
using System.Collections.Generic;
namespace System.ComponentModel.DataAnnotations.Schema
{
    public class NotMappedAttribute : System.Attribute { }
}
namespace Microsoft.EntityFrameworkCore
{
    public class DbContext { }
    public class DbSet<T> { }
}
namespace Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
        public ICollection<City> Cities { get; set; } = new List<City>();
    }

    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";
        " + countryIdDeclaration + @"
        " + countryNavigationAttribute + @"
        public virtual Country? Country { get; set; }
    }

    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public Microsoft.EntityFrameworkCore.DbSet<City> Cities { get; set; } = null!;
        public Microsoft.EntityFrameworkCore.DbSet<Country> Countries { get; set; } = null!;
    }
}");
        return CreateModelInfoFromSyntaxTree(syntaxTree, modelTypeName);
    }

    private static ModelInfo CreateModelInfoFromSource(string modelsSource, string modelTypeName)
    {
        return CreateModelInfoFromSyntaxTree(CSharpSyntaxTree.ParseText(@"
#nullable enable
using System.Collections.Generic;
namespace System.ComponentModel.DataAnnotations
{
    public class KeyAttribute : System.Attribute { }
}
namespace Microsoft.EntityFrameworkCore
{
    public class ModelBuilder { }
    public class DbContext { protected virtual void OnModelCreating(ModelBuilder modelBuilder) { } }
    public class DbSet<T> { }
}
" + modelsSource), modelTypeName);
    }

    private static ModelInfo CreateModelInfoFromSyntaxTree(SyntaxTree syntaxTree, string modelTypeName)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        INamedTypeSymbol modelSymbol = compilation.GetTypeByMetadataName($"Models.{modelTypeName}")!;
        INamedTypeSymbol dbContextSymbol = compilation.GetTypeByMetadataName("Models.AppDbContext")!;
        ModelInfo modelInfo = ClassAnalyzers.GetModelClassInfo(modelSymbol);
        modelInfo.ForeignKeyNavigations = ClassAnalyzers.GetForeignKeyNavigations(modelInfo, dbContextSymbol);
        return modelInfo;
    }

    private static EfControllerModel CreateCountryCityEfControllerModel(ModelInfo modelInfo)
    {
        return new EfControllerModel
        {
            ControllerType = "MVC",
            ControllerName = $"{modelInfo.ModelTypeName}sController",
            ControllerOutputPath = Path.Combine("Controllers", $"{modelInfo.ModelTypeName}sController.cs"),
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "AppDbContext",
                DbContextNamespace = "Models",
                EfScenario = true,
                EntitySetVariableName = modelInfo.ModelTypeName == "City" ? "Cities" : "Countries"
            },
            ModelInfo = modelInfo,
            ProjectInfo = new AspNetProjectInfo(null)
        };
    }

    private static ViewModel CreateCountryCityViewModel(ModelInfo modelInfo)
    {
        return new ViewModel
        {
            PageType = "CRUD",
            DbContextInfo = new DbContextInfo(),
            ModelInfo = modelInfo,
            ProjectInfo = new AspNetProjectInfo(null)
        };
    }

    private static (string Controller, string CreateView, string EditView) TransformCountryCityTemplates(string targetFramework, ModelInfo modelInfo)
    {
        EfControllerModel controllerModel = CreateCountryCityEfControllerModel(modelInfo);
        var viewSession = new Dictionary<string, object> { { "Model", CreateCountryCityViewModel(modelInfo) } };
        switch (targetFramework)
        {
            case "net9":
                var net9Create = new Net9CreateView { Session = viewSession };
                var net9Edit = new Net9EditView { Session = viewSession };
                net9Create.Initialize();
                net9Edit.Initialize();
                return (CreateNet9Template(controllerModel).TransformText(), net9Create.TransformText(), net9Edit.TransformText());
            case "net10":
                var net10Create = new Net10CreateView { Session = viewSession };
                var net10Edit = new Net10EditView { Session = viewSession };
                net10Create.Initialize();
                net10Edit.Initialize();
                return (CreateNet10Template(controllerModel).TransformText(), net10Create.TransformText(), net10Edit.TransformText());
            default:
                var net11Create = new Net11CreateView { Session = viewSession };
                var net11Edit = new Net11EditView { Session = viewSession };
                net11Create.Initialize();
                net11Edit.Initialize();
                return (CreateNet11Template(controllerModel).TransformText(), net11Create.TransformText(), net11Edit.TransformText());
        }
    }

    private static EfControllerModel CreateBlogEfControllerModel()
    {
        List<IPropertySymbol> properties = GetPropertiesFromCode(@"
namespace Models
{
    public class Blog
    {
        public int BlogId { get; set; }
        public string? Url { get; set; }
    }
}", "Blog");

        return new EfControllerModel
        {
            ControllerType = "MVC",
            ControllerName = "BlogsController",
            ControllerOutputPath = Path.Combine("Controllers", "BlogsController.cs"),
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "BloggingContext",
                DbContextNamespace = "Models",
                EfScenario = true,
                EntitySetVariableName = "Blogs"
            },
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Blog",
                ModelNamespace = "Models",
                ModelFullName = "Models.Blog",
                PrimaryKeyName = "BlogId",
                PrimaryKeyTypeName = "int",
                PrimaryKeyShortTypeName = "int",
                ModelProperties = properties
            },
            ProjectInfo = new AspNetProjectInfo(null)
        };
    }

    private static EfControllerModel CreateProductEfControllerModel()
    {
        List<IPropertySymbol> properties = GetPropertiesFromCode(@"
namespace Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}", "Product");

        return new EfControllerModel
        {
            ControllerType = "MVC",
            ControllerName = "ProductsController",
            ControllerOutputPath = Path.Combine("Controllers", "ProductsController.cs"),
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "AppDbContext",
                DbContextNamespace = "Models",
                EfScenario = true,
                EntitySetVariableName = "Products"
            },
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Product",
                ModelNamespace = "Models",
                ModelFullName = "Models.Product",
                PrimaryKeyName = "ProductId",
                PrimaryKeyTypeName = "int",
                PrimaryKeyShortTypeName = "int",
                ModelProperties = properties
            },
            ProjectInfo = new AspNetProjectInfo(Path.Combine("test", "TestProject.csproj"))
        };
    }

    private static RazorPageModel CreateBlogRazorPageModel()
    {
        List<IPropertySymbol> properties = GetPropertiesFromCode(@"
namespace Models
{
    public class Blog
    {
        public int BlogId { get; set; }
        public string? Url { get; set; }
    }
}", "Blog");

        return new RazorPageModel
        {
            PageType = "Create",
            DbContextInfo = new DbContextInfo
            {
                DbContextClassName = "BloggingContext",
                DbContextNamespace = "Models",
                EfScenario = true,
                EntitySetVariableName = "Blogs"
            },
            ModelInfo = new ModelInfo
            {
                ModelTypeName = "Blog",
                ModelNamespace = "Models",
                ModelFullName = "Models.Blog",
                PrimaryKeyName = "BlogId",
                PrimaryKeyTypeName = "int",
                PrimaryKeyShortTypeName = "int",
                ModelProperties = properties
            },
            RazorPageNamespace = "Pages.Blogs",
            ProjectInfo = new AspNetProjectInfo(null)
        };
    }

    private static List<IPropertySymbol> GetPropertiesFromCode(string source, string typeName)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        ];
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName($"Models.{typeName}")
            ?? compilation.GetSymbolsWithName(typeName, SymbolFilter.Type).OfType<INamedTypeSymbol>().FirstOrDefault()
            ?? throw new System.Exception($"Type '{typeName}' not found in compilation");

        return symbol.GetMembers().OfType<IPropertySymbol>().ToList();
    }

    private static Net9MvcEfController CreateNet9Template(EfControllerModel model)
    {
        var template = new Net9MvcEfController();
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template;
    }

    private static Net10MvcEfController CreateNet10Template(EfControllerModel model)
    {
        var template = new Net10MvcEfController();
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template;
    }

    private static Net11MvcEfController CreateNet11Template(EfControllerModel model)
    {
        var template = new Net11MvcEfController();
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template;
    }

    private static Net9RazorCreateModel CreateNet9RazorTemplate(RazorPageModel model)
    {
        var template = new Net9RazorCreateModel();
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template;
    }

    private static Net10RazorCreateModel CreateNet10RazorTemplate(RazorPageModel model)
    {
        var template = new Net10RazorCreateModel();
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template;
    }

    private static Net11RazorCreateModel CreateNet11RazorTemplate(RazorPageModel model)
    {
        var template = new Net11RazorCreateModel();
        template.Session = new Dictionary<string, object> { { "Model", model } };
        template.Initialize();
        return template;
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    #endregion
}
