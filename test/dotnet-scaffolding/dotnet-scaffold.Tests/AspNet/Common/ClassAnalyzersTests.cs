// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Common;

public class ClassAnalyzersTests
{
    private readonly Mock<ILogger> _mockLogger;

    public ClassAnalyzersTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Theory]
    [InlineData("public", false, true)]
    [InlineData("internal", false, false)]
    [InlineData("internal", true, true)]
    public async Task ValidateTypeAccessibilityAsync_HonorsTargetAssemblyAccess(
        string accessibility, bool friendAssembly, bool expected)
    {
        using var workspace = new AdhocWorkspace();
        var path = Path.Combine(Path.GetTempPath(), "Web.csproj");
        var library = workspace.AddProject("Library", LanguageNames.CSharp)
            .WithMetadataReferences([MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var friend = friendAssembly ? "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Web\")]" : "";
        library = library.AddDocument("Types.cs", SourceText.From($"{friend} namespace Models {{ {accessibility} class Movie {{ }} }}")).Project;
        var web = library.Solution.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
            ProjectId.CreateNewId(), VersionStamp.Create(), "Web", "Web", LanguageNames.CSharp, filePath: path));
        var webProject = web.Projects.Single(project => project.Name == "Web")
            .AddProjectReference(new ProjectReference(library.Id))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        Assert.True(workspace.TryApplyChanges(webProject.Solution));
        var compilation = await workspace.CurrentSolution.GetProject(library.Id)!.GetCompilationAsync();
        var selectedType = compilation!.GetTypeByMetadataName("Models.Movie");
        var service = new Mock<ICodeService>();
        service.Setup(code => code.GetWorkspaceAsync()).ReturnsAsync(workspace);

        Assert.Equal(expected, await ClassAnalyzers.ValidateTypeAccessibilityAsync(service.Object, path, selectedType!, _mockLogger.Object));
    }

    [Fact]
    public async Task ValidateTypeAccessibilityAsync_PreservesLocalFallbackSymbols()
    {
        var directory = Path.Combine(Path.GetTempPath(), nameof(ClassAnalyzersTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "Missing.csproj");
            File.WriteAllText(Path.Combine(directory, "Movie.cs"), "public class Movie { }");
            using var service = new CodeService(_mockLogger.Object, path);
            var selectedType = Assert.Single(await service.GetAllClassSymbolsAsync());
            Assert.IsType<AdhocWorkspace>(await service.GetWorkspaceAsync());

            Assert.True(await ClassAnalyzers.ValidateTypeAccessibilityAsync(service, path, selectedType, _mockLogger.Object));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ValidateTypeAccessibilityAsync_HandlesEscapedNamespaceMetadata(bool referenced, bool nested)
    {
        using var workspace = new AdhocWorkspace();
        var path = Path.Combine(Path.GetTempPath(), "Web.csproj");
        var project = workspace.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
            ProjectId.CreateNewId(), VersionStamp.Create(), referenced ? "Library" : "Web",
            referenced ? "Library" : "Web", LanguageNames.CSharp, filePath: referenced ? null : path));
        var declaration = nested ? "public class Container { public class Movie { } }" : "public class Movie { }";
        project = project.AddDocument("Types.cs",
            SourceText.From($"namespace MyApp.@event {{ {declaration} }}")).Project;
        var compilation = await project.GetCompilationAsync();
        var selectedType = compilation!.GetTypeByMetadataName(nested
            ? "MyApp.event.Container+Movie" : "MyApp.event.Movie");
        if (referenced)
        {
            var solution = project.Solution.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
                ProjectId.CreateNewId(), VersionStamp.Create(), "Web", "Web", LanguageNames.CSharp, filePath: path));
            project = solution.Projects.Single(candidate => candidate.Name == "Web")
                .AddProjectReference(new ProjectReference(project.Id));
        }
        Assert.True(workspace.TryApplyChanges(project.Solution));
        var service = new Mock<ICodeService>();
        service.Setup(code => code.GetWorkspaceAsync()).ReturnsAsync(workspace);

        Assert.True(await ClassAnalyzers.ValidateTypeAccessibilityAsync(service.Object, path, selectedType!, _mockLogger.Object));
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
