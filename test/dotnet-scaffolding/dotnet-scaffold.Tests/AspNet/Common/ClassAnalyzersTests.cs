// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        MetadataReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
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
