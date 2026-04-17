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

    #endregion

    #region Helpers

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
