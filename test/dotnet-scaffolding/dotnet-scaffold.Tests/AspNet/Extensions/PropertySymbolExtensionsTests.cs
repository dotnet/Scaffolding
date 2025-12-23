// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class PropertySymbolExtensionsTests
{
    private static Compilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        MetadataReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IPropertySymbol? GetPropertySymbol(Compilation compilation, string className, string propertyName)
    {
        INamedTypeSymbol? classSymbol = compilation.GetTypeByMetadataName(className);
        return classSymbol?.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == propertyName);
    }

    [Fact]
    public void HasRequiredAttribute_WithRequiredAttribute_ReturnsTrue()
    {
        // Arrange
        string source = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    [Required]
    public string Name { get; set; }
}";
        Compilation compilation = CreateCompilation(source);
        IPropertySymbol? propertySymbol = GetPropertySymbol(compilation, "TestClass", "Name");

        // Act
        bool result = propertySymbol!.HasRequiredAttribute();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRequiredAttribute_WithoutRequiredAttribute_ReturnsFalse()
    {
        // Arrange
        string source = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    [Display(Name = ""Display Name"")]
    public string Name { get; set; }
}";
        Compilation compilation = CreateCompilation(source);
        IPropertySymbol? propertySymbol = GetPropertySymbol(compilation, "TestClass", "Name");

        // Act
        bool result = propertySymbol!.HasRequiredAttribute();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRequiredAttribute_WithNoAttributes_ReturnsFalse()
    {
        // Arrange
        string source = @"
public class TestClass
{
    public string Name { get; set; }
}";
        Compilation compilation = CreateCompilation(source);
        IPropertySymbol? propertySymbol = GetPropertySymbol(compilation, "TestClass", "Name");

        // Act
        bool result = propertySymbol!.HasRequiredAttribute();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRequiredAttribute_WithMultipleAttributes_ContainingRequired_ReturnsTrue()
    {
        // Arrange
        string source = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    [Display(Name = ""Display Name"")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
}";
        Compilation compilation = CreateCompilation(source);
        IPropertySymbol? propertySymbol = GetPropertySymbol(compilation, "TestClass", "Name");

        // Act
        bool result = propertySymbol!.HasRequiredAttribute();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRequiredAttribute_WithMultipleAttributes_NotContainingRequired_ReturnsFalse()
    {
        // Arrange
        string source = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    [Display(Name = ""Display Name"")]
    [MaxLength(50)]
    [StringLength(100)]
    public string Name { get; set; }
}";
        Compilation compilation = CreateCompilation(source);
        IPropertySymbol? propertySymbol = GetPropertySymbol(compilation, "TestClass", "Name");

        // Act
        bool result = propertySymbol!.HasRequiredAttribute();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRequiredAttribute_CaseInsensitiveMatch_ReturnsTrue()
    {
        // Arrange
        // The attribute class name ends with "Attribute" but we use it without the suffix
        string source = @"
using System.ComponentModel.DataAnnotations;

public class TestClass
{
    [Required]
    public string RequiredProperty { get; set; }
}";
        Compilation compilation = CreateCompilation(source);
        IPropertySymbol? propertySymbol = GetPropertySymbol(compilation, "TestClass", "RequiredProperty");

        // Act
        bool result = propertySymbol!.HasRequiredAttribute();

        // Assert
        Assert.True(result);
        // Verify the attribute class name
        AttributeData attribute = propertySymbol!.GetAttributes().First();
        Assert.NotNull(attribute.AttributeClass);
        Assert.Equal("RequiredAttribute", attribute.AttributeClass!.Name);
    }
}
