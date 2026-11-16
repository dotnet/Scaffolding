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
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ScaffoldTemplates = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Common;

public class ClassAnalyzersTests
{
    private readonly Mock<ILogger> _mockLogger;

    public ClassAnalyzersTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void GetDbContextInfo_WithExistingDbContext_ReturnsCorrectInfo()
    {
        // Arrange
        string code = @"
using Microsoft.EntityFrameworkCore;
namespace TestNamespace
{
    public class TestDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "TestDbContext");
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = "Product",
            ModelFullName = "TestNamespace.Product"
        };

        // Act
        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj",
            dbContextSymbol,
            "TestDbContext",
            "SqlServer",
            modelInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestDbContext", result.DbContextClassName);
        Assert.Equal("TestNamespace", result.DbContextNamespace);
        Assert.True(result.EfScenario);
        Assert.Equal("SqlServer", result.DatabaseProvider);
        Assert.NotNull(result.DbContextClassPath);
    }

    [Fact]
    public void GetDbContextInfo_WithNewDbContext_ReturnsCorrectInfo()
    {
        // Arrange
        string projectPath = "TestProject.csproj";
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = "Product",
            ModelFullName = "TestNamespace.Product"
        };

        // Act
        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            projectPath,
            null,
            "NewDbContext",
            "SqlServer",
            modelInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewDbContext", result.DbContextClassName);
        Assert.Equal("SqlServer", result.DatabaseProvider);
        Assert.True(result.EfScenario);
        Assert.Equal("Product", result.EntitySetVariableName);
        Assert.Contains("public DbSet<TestNamespace.Product> Product", result.NewDbSetStatement);
    }

    [Fact]
    public void GetDbContextInfo_WithGlobalNamespace_SetsNamespaceToEmpty()
    {
        // Arrange
        string code = @"
using Microsoft.EntityFrameworkCore;
public class TestDbContext : DbContext
{
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "TestDbContext");

        // Act
        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj",
            dbContextSymbol,
            "TestDbContext",
            "SqlServer");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.DbContextNamespace);
    }

    [Fact]
    public void GetIdentityDbContextInfo_WithExistingDbContext_ReturnsCorrectInfo()
    {
        // Arrange
        string code = @"
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace TestNamespace
{
    public class ApplicationDbContext : IdentityDbContext
    {
    }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "ApplicationDbContext");

        // Act
        DbContextInfo result = ClassAnalyzers.GetIdentityDbContextInfo(
            "TestProject.csproj",
            dbContextSymbol,
            "ApplicationDbContext",
            "SqlServer");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ApplicationDbContext", result.DbContextClassName);
        Assert.Equal("TestNamespace", result.DbContextNamespace);
        Assert.True(result.EfScenario);
        Assert.Equal("SqlServer", result.DatabaseProvider);
        Assert.Equal(string.Empty, result.EntitySetVariableName);
        Assert.Null(result.NewDbSetStatement);
    }

    [Fact]
    public void GetIdentityDbContextInfo_WithNewDbContext_ReturnsCorrectInfo()
    {
        // Arrange
        string projectPath = Path.Combine("path", "to", "TestProject.csproj");

        // Act
        DbContextInfo result = ClassAnalyzers.GetIdentityDbContextInfo(
            projectPath,
            null,
            "NewIdentityDbContext",
            "SqlServer");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewIdentityDbContext", result.DbContextClassName);
        Assert.Equal("TestProject.Data", result.DbContextNamespace);
        Assert.Equal("SqlServer", result.DatabaseProvider);
        Assert.True(result.EfScenario);
        Assert.Equal(string.Empty, result.EntitySetVariableName);
        Assert.Equal(string.Empty, result.NewDbSetStatement);
    }

    [Fact]
    public void GetModelClassInfo_WithValidModel_ReturnsCorrectInfo()
    {
        // Arrange
        string code = @"
namespace TestNamespace
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol modelSymbol = GetTypeSymbol(compilation, "Product");

        // Act
        ModelInfo result = ClassAnalyzers.GetModelClassInfo(modelSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Product", result.ModelTypeName);
        Assert.Equal("TestNamespace", result.ModelNamespace);
        Assert.Equal("TestNamespace.Product", result.ModelFullName);
    }

    [Fact]
    public void GetModelClassInfo_WithGlobalNamespace_ReturnsCorrectInfo()
    {
        // Arrange
        string code = @"
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol modelSymbol = GetTypeSymbol(compilation, "Product");

        // Act
        ModelInfo result = ClassAnalyzers.GetModelClassInfo(modelSymbol);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Product", result.ModelTypeName);
        Assert.Equal(string.Empty, result.ModelNamespace);
        Assert.Equal("Product", result.ModelFullName);
    }

    [Fact]
    public void ValidateModelForCrudScaffolders_WithValidModel_ReturnsTrue()
    {
        // Arrange
        string code = @"
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol modelSymbol = GetTypeSymbol(compilation, "Product");
        List<IPropertySymbol> properties = modelSymbol.GetMembers().OfType<IPropertySymbol>().ToList();
        
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = "Product",
            PrimaryKeyName = "Id",
            ModelProperties = properties
        };

        // Act
        bool result = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _mockLogger.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateModelForCrudScaffolders_WithNoProperties_ReturnsFalseAndLogsError()
    {
        // Arrange
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = "Product",
            PrimaryKeyName = "Id",
            ModelProperties = new List<IPropertySymbol>()
        };

        // Act
        bool result = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _mockLogger.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateModelForCrudScaffolders_WithNoPrimaryKey_ReturnsFalseAndLogsError()
    {
        // Arrange
        string code = @"
public class Product
{
    public string Name { get; set; }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol modelSymbol = GetTypeSymbol(compilation, "Product");
        List<IPropertySymbol> properties = modelSymbol.GetMembers().OfType<IPropertySymbol>().ToList();
        
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = "Product",
            PrimaryKeyName = string.Empty,
            ModelProperties = properties
        };

        // Act
        bool result = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _mockLogger.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateModelForCrudScaffolders_WithEmptyModelTypeName_ReturnsFalse()
    {
        // Arrange
        string code = @"
public class Product
{
    public int Id { get; set; }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol modelSymbol = GetTypeSymbol(compilation, "Product");
        List<IPropertySymbol> properties = modelSymbol.GetMembers().OfType<IPropertySymbol>().ToList();
        
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = string.Empty,
            PrimaryKeyName = "Id",
            ModelProperties = properties
        };

        // Act
        bool result = ClassAnalyzers.ValidateModelForCrudScaffolders(modelInfo, _mockLogger.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetDbContextInfo_WithExistingDbContext_PopulatesNewDbSetStatement_WhenDbSetIsMissing()
    {
        // Arrange — context exists but has NO DbSet for Movie
        string code = @"
using Microsoft.EntityFrameworkCore;
namespace MyApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
    }

    public class Customer { public int Id { get; set; } }
    public class Movie { public int Id { get; set; } }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "ApplicationDbContext");
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = "Movie",
            ModelFullName = "MyApp.Data.Movie"
        };

        // Act
        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj",
            dbContextSymbol,
            "ApplicationDbContext",
            "SqlServer",
            modelInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ApplicationDbContext", result.DbContextClassName);
        // EntitySetVariableName should fall back to model name when DbSet is missing.
        Assert.Equal("Movie", result.EntitySetVariableName);
        // NewDbSetStatement must be set so AddDbSetToExistingContextStep can insert it
        Assert.NotNull(result.NewDbSetStatement);
        Assert.Contains("DbSet<MyApp.Data.Movie>", result.NewDbSetStatement);
        Assert.Contains("Movie", result.NewDbSetStatement);
        Assert.Contains("default!", result.NewDbSetStatement);
    }

    [Fact]
    public void GetDbContextInfo_WithExistingDbContext_DoesNotPopulateNewDbSetStatement_WhenDbSetAlreadyPresent()
    {
        // Arrange — context already has a DbSet<Movie>
        string code = @"
using Microsoft.EntityFrameworkCore;
namespace MyApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Movie> Movie { get; set; }
    }

    public class Movie { public int Id { get; set; } }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "ApplicationDbContext");
        ModelInfo modelInfo = new ModelInfo
        {
            ModelTypeName = "Movie",
            ModelFullName = "MyApp.Data.Movie"
        };

        // Act
        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj",
            dbContextSymbol,
            "ApplicationDbContext",
            "SqlServer",
            modelInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Movie", result.EntitySetVariableName);
        // DbSet exists → no statement to inject
        Assert.Null(result.NewDbSetStatement);
    }

    [Fact]
    public void GetDbContextInfo_WithExistingPluralDbSet_UsesExistingProperty()
    {
        string code = """
            using Microsoft.EntityFrameworkCore;
            namespace MyApp.Models
            {
                public class BloggingContext : DbContext
                {
                    public DbSet<Blog> Blogs { get; set; } = null!;
                }

                public class Blog { public int BlogId { get; set; } }
            }
            """;
        CSharpCompilation compilation = CreateCompilation(code);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "BloggingContext");
        ModelInfo modelInfo = new()
        {
            ModelTypeName = "Blog",
            ModelFullName = "MyApp.Models.Blog"
        };

        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj", dbContextSymbol, "BloggingContext", "SqlServer", modelInfo);

        Assert.Equal("Blogs", result.EntitySetVariableName);
        Assert.Null(result.NewDbSetStatement);
    }

    [Fact]
    public void GetDbContextInfo_WithDbSetForDifferentBlogType_DoesNotReuseProperty()
    {
        string code = """
            using Microsoft.EntityFrameworkCore;
            namespace MyApp.Models
            {
                public class BloggingContext : DbContext
                {
                    public DbSet<Blog> Blogs { get; set; } = null!;
                }

                public class Blog { public int BlogId { get; set; } }
            }

            namespace Other.Models
            {
                public class Blog { public int BlogId { get; set; } }
            }
            """;
        CSharpCompilation compilation = CreateCompilation(code);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "BloggingContext");
        ModelInfo modelInfo = new()
        {
            ModelTypeName = "Blog",
            ModelFullName = "Other.Models.Blog"
        };

        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj", dbContextSymbol, "BloggingContext", "SqlServer", modelInfo);

        Assert.Equal("Blog", result.EntitySetVariableName);
        Assert.Contains("DbSet<Other.Models.Blog> Blog", result.NewDbSetStatement);
    }

    [Theory]
    [InlineData(9, true)]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(10, false)]
    [InlineData(11, true)]
    [InlineData(11, false)]
    public void GetDbContextInfo_OtherCrudTemplates_UseDiscoveredOrNewDbSet(int version, bool existingDbSet)
    {
        string property = existingDbSet ? "public DbSet<Blog> Blogs { get; set; } = null!;" : "";
        CSharpCompilation compilation = CreateCompilation($$"""
            using Microsoft.EntityFrameworkCore;
            namespace Models
            {
                public class BloggingContext : DbContext { {{property}} }
                public class Blog
                {
                    public int BlogId { get; set; }
                    public string Url { get; set; } = "";
                }
            }
            """);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        ModelInfo model = ClassAnalyzers.GetModelClassInfo(GetTypeSymbol(compilation, "Blog"));
        DbContextInfo context = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj", GetTypeSymbol(compilation, "BloggingContext"),
            "BloggingContext", "sqlite-efcore", model);
        string expectedSet = existingDbSet ? "Blogs" : "Blog";
        Assert.Equal(expectedSet, context.EntitySetVariableName);
        if (existingDbSet)
        {
            Assert.Null(context.NewDbSetStatement);
        }
        else
        {
            Assert.Contains("DbSet<Models.Blog> Blog", context.NewDbSetStatement);
        }

        (ITextTransformation mvc, ITextTransformation api, ITextTransformation minimal, ITextTransformation blazor) templates = version switch
        {
            9 => (new ScaffoldTemplates.net9.EfController.MvcEfController(),
                new ScaffoldTemplates.net9.EfController.ApiEfController(),
                new ScaffoldTemplates.net9.MinimalApi.MinimalApiEf(),
                new ScaffoldTemplates.net9.BlazorCrud.Index()),
            10 => (new ScaffoldTemplates.net10.EfController.MvcEfController(),
                new ScaffoldTemplates.net10.EfController.ApiEfController(),
                new ScaffoldTemplates.net10.MinimalApi.MinimalApiEf(),
                new ScaffoldTemplates.net10.BlazorCrud.Index()),
            11 => (new ScaffoldTemplates.net11.EfController.MvcEfController(),
                new ScaffoldTemplates.net11.EfController.ApiEfController(),
                new ScaffoldTemplates.net11.MinimalApi.MinimalApiEf(),
                new ScaffoldTemplates.net11.BlazorCrud.Index()),
            _ => throw new ArgumentOutOfRangeException(nameof(version))
        };
        var project = new Microsoft.DotNet.Tools.Scaffold.AspNet.Common.ProjectInfo(null);
        var controller = new EfControllerModel
        {
            ControllerType = "MvcController",
            ControllerName = "BlogsController",
            ControllerOutputPath = "BlogsController.cs",
            DbContextInfo = context,
            ModelInfo = model,
            ProjectInfo = project
        };
        Assert.Contains($"_context.{expectedSet}", Transform(templates.mvc, controller));
        controller.ControllerType = "ApiController";
        Assert.Contains($"_context.{expectedSet}", Transform(templates.api, controller));
        Assert.Contains($"db.{expectedSet}", Transform(templates.minimal, new MinimalApiModel
        {
            EndpointsClassName = "BlogEndpoints",
            EndpointsMethodName = "MapBlogEndpoints",
            DbContextInfo = context,
            ModelInfo = model,
            ProjectInfo = project
        }));
        Assert.Contains($"context.{expectedSet}", Transform(templates.blazor, new BlazorCrudModel
        {
            PageType = "Index",
            DbContextInfo = context,
            ModelInfo = model,
            ProjectInfo = project
        }));
    }

    private static string Transform(ITextTransformation template, object model)
    {
        template.Session = new Dictionary<string, object> { ["Model"] = model };
        template.Initialize();
        string output = template.TransformText();
        Assert.False(template.Errors.HasErrors);
        return output;
    }

    [Fact]
    public void GetDbContextInfo_WithExistingDbContext_NullModelInfo_DoesNotPopulateNewDbSetStatement()
    {
        // Arrange
        string code = @"
using Microsoft.EntityFrameworkCore;
namespace MyApp.Data
{
    public class ApplicationDbContext : DbContext { }
}";
        CSharpCompilation compilation = CreateCompilation(code);
        INamedTypeSymbol dbContextSymbol = GetTypeSymbol(compilation, "ApplicationDbContext");

        // Act — no modelInfo provided
        DbContextInfo result = ClassAnalyzers.GetDbContextInfo(
            "TestProject.csproj",
            dbContextSymbol,
            "ApplicationDbContext",
            "SqlServer",
            modelInfo: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.EntitySetVariableName);
        Assert.Null(result.NewDbSetStatement);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        string baseDir = System.AppContext.BaseDirectory;

        // Start with all currently-loaded assemblies so framework + DI/logging transitive deps are present
        System.Collections.Generic.HashSet<string> addedFileNames =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        System.Collections.Generic.List<MetadataReference> references = new System.Collections.Generic.List<MetadataReference>();

        foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location) &&
                addedFileNames.Add(System.IO.Path.GetFileName(asm.Location)))
            {
                references.Add(MetadataReference.CreateFromFile(asm.Location));
            }
        }

        // Explicitly add EF Core assemblies from the test output directory if not already loaded
        string[] efCoreNames = { "Microsoft.EntityFrameworkCore.dll", "Microsoft.EntityFrameworkCore.Abstractions.dll" };
        foreach (string asmName in efCoreNames)
        {
            if (addedFileNames.Add(asmName))
            {
                string path = System.IO.Path.Combine(baseDir, asmName);
                if (System.IO.File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references.ToArray(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static INamedTypeSymbol GetTypeSymbol(Compilation compilation, string typeName)
    {
        INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(typeName);
        if (symbol == null)
        {
            // If not found in root, search in all types
            IEnumerable<INamedTypeSymbol> allTypes = compilation.GetSymbolsWithName(typeName, SymbolFilter.Type).OfType<INamedTypeSymbol>();
            symbol = allTypes.FirstOrDefault();
        }
        return symbol ?? throw new System.Exception($"Type '{typeName}' not found in compilation");
    }
}
