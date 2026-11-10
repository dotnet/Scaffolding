// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Tests;

public class CodeServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), nameof(CodeServiceTests), Guid.NewGuid().ToString());
    private static string TargetFramework => $"net{Environment.Version.Major}.0";

    public CodeServiceTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_PreservesLegacyImplementationsAndCallers()
    {
        ICodeService service = new LegacyCodeService();

        Assert.Empty(await service.GetAllClassSymbolsAsync());
        Assert.Empty(await service.GetAllClassSymbolsAsync(includeReferencedProjects: false));
        await Assert.ThrowsAsync<NotSupportedException>(
            () => service.GetAllClassSymbolsAsync(includeReferencedProjects: true));
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_OnlyIncludesCurrentProjectByDefault()
    {
        var modelsProject = CreateProject("Models", "public class Book { } public class Startup { }");
        var project = CreateProject("WebApp", "public class LocalBook { }", modelsProject);
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync();

        Assert.Equal("LocalBook", Assert.Single(symbols).Name);
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_IncludesDirectAndTransitiveReferencesOnce()
    {
        var modelsProject = CreateProject("Models", """
            namespace BookStore.Models;
            public partial class Book
            {
                public int Id { get; set; }
            }
            """);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(modelsProject), "Book.Properties.cs"), """
            namespace BookStore.Models;
            public partial class Book
            {
                public string Title { get; set; } = string.Empty;
            }
            """);
        var dataProject = CreateProject("Data", "public class BookStoreContext { }", modelsProject);
        var project = CreateProject("WebApp", "namespace WebApp; public class Book { }", dataProject, modelsProject);
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync(includeReferencedProjects: true);

        Assert.Equal("WebApp.Book", symbols[0].ToDisplayString());
        var model = Assert.IsAssignableFrom<INamedTypeSymbol>(
            Assert.Single(symbols, symbol => symbol.ToDisplayString() == "BookStore.Models.Book"));
        Assert.Contains(model.GetMembers(), member => member.Name == "Id");
        Assert.Contains(model.GetMembers(), member => member.Name == "Title");
        Assert.Single(symbols, symbol => symbol.Name == "BookStoreContext");
        Assert.DoesNotContain(symbols, symbol => symbol.Name == "String");
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_FollowsTransitiveReferences()
    {
        var modelsProject = CreateProject("Models", "public class Book { }");
        var dataProject = CreateProject("Data", "public class BookStoreContext { }", modelsProject);
        var project = CreateProject("WebApp", "public class LocalBook { }", dataProject);
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync(includeReferencedProjects: true);

        Assert.Single(symbols, symbol => symbol.Name == "Book");
        Assert.Single(symbols, symbol => symbol.Name == "BookStoreContext");
        Assert.Single(symbols, symbol => symbol.Name == "LocalBook");
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_EvaluatesImportedReferenceConditions()
    {
        var modelsProject = CreateProject("Models", "public class Book { }");
        var excludedProject = CreateProject("Excluded", "public class ExcludedBook { }");
        var project = CreateProject("WebApp", "public class LocalBook { }");
        var projectDirectory = Path.GetDirectoryName(project);
        File.WriteAllText(Path.Combine(projectDirectory, "References.props"), $"""
            <Project>
              <ItemGroup>
                <ProjectReference Include="$(ModelsProject)" Condition="'$(IncludeModels)' == 'true'" />
                <ProjectReference Include="{Path.GetRelativePath(projectDirectory, excludedProject)}" Condition="'$(IncludeModels)' != 'true'" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(project, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ModelsProject>{Path.GetRelativePath(projectDirectory, modelsProject)}</ModelsProject>
                <IncludeModels>true</IncludeModels>
              </PropertyGroup>
              <Import Project="References.props" />
            </Project>
            """);
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync(includeReferencedProjects: true);

        Assert.Single(symbols, symbol => symbol.Name == "Book");
        Assert.DoesNotContain(symbols, symbol => symbol.Name == "ExcludedBook");
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_ReportsReferenceEvaluationFailures()
    {
        var modelsProject = CreateProject("Models", "public class Book { }");
        File.WriteAllText(modelsProject, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
              </PropertyGroup>
              <Import Project="Missing.props" />
            </Project>
            """);
        var project = CreateProject("WebApp", "public class LocalBook { }", modelsProject);
        using var service = new CodeService(NullLogger.Instance, project);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetAllClassSymbolsAsync(includeReferencedProjects: true));

        Assert.Contains("Missing.props", exception.Message);
        Assert.Contains(project, exception.Message);
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_RespectsTargetFrameworkConditionalReferences()
    {
        var modelsProject = CreateProject("Models", "public class Book { }");
        var excludedProject = CreateProject("Excluded", "public class ExcludedBook { }");
        File.WriteAllText(excludedProject, File.ReadAllText(excludedProject).Replace(TargetFramework, "net8.0"));
        var dataProject = CreateProject("Data", "public class BookStoreContext { }");
        var dataDirectory = Path.GetDirectoryName(dataProject);
        File.WriteAllText(dataProject, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net8.0;{TargetFramework}</TargetFrameworks>
              </PropertyGroup>
              <ItemGroup Condition="'$(TargetFramework)' == '{TargetFramework}'">
                <ProjectReference Include="{Path.GetRelativePath(dataDirectory, modelsProject)}" />
              </ItemGroup>
              <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                <ProjectReference Include="{Path.GetRelativePath(dataDirectory, excludedProject)}" />
              </ItemGroup>
            </Project>
            """);
        var project = CreateProject("WebApp", "public class LocalBook { }", dataProject);
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync(includeReferencedProjects: true);

        Assert.Single(symbols, symbol => symbol.Name == "Book");
        Assert.DoesNotContain(symbols, symbol => symbol.Name == "ExcludedBook");
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_ExcludesBuildOnlyReferences()
    {
        var modelsProject = CreateProject("Models", "public class Book { }");
        var project = CreateProject("WebApp", "public class LocalBook { }", modelsProject);
        File.WriteAllText(project, File.ReadAllText(project).Replace(
            "<ProjectReference ", "<ProjectReference ReferenceOutputAssembly=\"false\" "));
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync(includeReferencedProjects: true);

        Assert.Equal("LocalBook", Assert.Single(symbols).Name);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("OtherWebApp", false)]
    [InlineData("WebApp", true)]
    public async Task GetAllClassSymbolsAsync_RespectsAccessibilityFromTargetAssembly(string friendAssembly, bool hasInternalAccess)
    {
        var friendAttribute = friendAssembly is null
            ? string.Empty
            : $"""[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("{friendAssembly}")]""";
        var modelsProject = CreateProject("Models", friendAttribute + """

            namespace Library.Models;
            public class PublicBook { }
            internal class Book { }
            file class FileBook { }
            public class Container
            {
                public class PublicNested { }
                private class PrivateNested { }
                protected class ProtectedNested { }
                internal class InternalNested { }
                protected internal class ProtectedInternalNested { }
                private protected class PrivateProtectedNested { }
            }
            internal class InternalContainer
            {
                public class NestedBook { }
            }
            """);
        var project = CreateProject("WebApp", "internal class LocalBook { } file class LocalFileBook { }", modelsProject);
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync(includeReferencedProjects: true);

        Assert.Contains(symbols, symbol => symbol.Name == "LocalBook");
        Assert.Contains(symbols, symbol => symbol.Name == "PublicBook");
        Assert.Contains(symbols, symbol => symbol.Name == "PublicNested");
        Assert.Equal(hasInternalAccess, symbols.Any(symbol => symbol.Name == "Book"));
        Assert.Equal(hasInternalAccess, symbols.Any(symbol => symbol.Name == "NestedBook"));
        Assert.Equal(hasInternalAccess, symbols.Any(symbol => symbol.Name == "InternalNested"));
        Assert.Equal(hasInternalAccess, symbols.Any(symbol => symbol.Name == "ProtectedInternalNested"));
        Assert.DoesNotContain(symbols, symbol => symbol.Name is "PrivateNested" or "ProtectedNested" or "PrivateProtectedNested");
        Assert.DoesNotContain(symbols, symbol => symbol.Name is "FileBook" or "LocalFileBook");
    }

    [Theory]
    [InlineData("Data", false)]
    [InlineData("WebApp", true)]
    public async Task GetAllClassSymbolsAsync_ChecksTransitiveAccessibilityAgainstTargetAssembly(string friendAssembly, bool expected)
    {
        var modelsProject = CreateProject("Models", $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("{{friendAssembly}}")]
            namespace Library.Models;
            internal class Book { }
            """);
        var dataProject = CreateProject("Data", "public class Store { }", modelsProject);
        var project = CreateProject("WebApp", "public class LocalBook { }", dataProject);
        using var service = new CodeService(NullLogger.Instance, project);

        var symbols = await service.GetAllClassSymbolsAsync(includeReferencedProjects: true);

        Assert.Equal(expected, symbols.Any(symbol => symbol.Name == "Book"));
    }

    private string CreateProject(string name, string source, params string[] references)
    {
        var projectDirectory = Path.Combine(_directory, name);
        Directory.CreateDirectory(projectDirectory);
        var projectPath = Path.Combine(projectDirectory, $"{name}.csproj");
        var referenceItems = string.Join(Environment.NewLine, references.Select(reference =>
            $"""<ProjectReference Include="{Path.GetRelativePath(projectDirectory, reference)}" />"""));
        File.WriteAllText(projectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                {referenceItems}
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(projectDirectory, "Models.cs"), source);
        return projectPath;
    }

    [Fact]
    public async Task TryApplyChanges_FallbackWorkspace_PersistsLazySyntaxChanges()
    {
        var filePath = Path.Combine(_directory, "ApplicationDbContext.cs");
        File.WriteAllText(filePath, "public class ApplicationDbContext { }");
        // A missing project forces the source-file-backed fallback workspace.
        using var service = new CodeService(NullLogger.Instance, Path.Combine(_directory, "Missing.csproj"));
        var workspace = await service.GetWorkspaceAsync();
        Assert.IsType<AdhocWorkspace>(workspace);
        var document = Assert.Single(Assert.Single(workspace.CurrentSolution.Projects).Documents);
        const string changedSource = "public class ApplicationDbContext { public object Movie { get; set; } }";
        var updatedDocument = document.WithSyntaxRoot(SyntaxFactory.ParseCompilationUnit(changedSource));
        Assert.False(updatedDocument.TryGetText(out _));

        var result = service.TryApplyChanges(updatedDocument.Project.Solution);

        Assert.True(result);
        Assert.Equal(changedSource, File.ReadAllText(filePath));
        Assert.Equal(changedSource, (await workspace.CurrentSolution.GetDocument(document.Id).GetTextAsync()).ToString());
    }

    [Fact]
    public async Task TryApplyChanges_FallbackWorkspace_PropagatesException_WhenWriteFails()
    {
        var filePath = Path.Combine(_directory, "ApplicationDbContext.cs");
        const string originalSource = "public class ApplicationDbContext { }";
        File.WriteAllText(filePath, originalSource);
        using var service = new CodeService(NullLogger.Instance, Path.Combine(_directory, "Missing.csproj"));
        var workspace = await service.GetWorkspaceAsync();
        Assert.IsType<AdhocWorkspace>(workspace);
        var document = Assert.Single(Assert.Single(workspace.CurrentSolution.Projects).Documents);
        var updatedDocument = document.WithSyntaxRoot(SyntaxFactory.ParseCompilationUnit(
            "public class ApplicationDbContext { public object Movie { get; set; } }"));
        Assert.False(updatedDocument.TryGetText(out _));

        File.Delete(filePath);
        Directory.CreateDirectory(filePath);

        var exception = Assert.ThrowsAny<Exception>(() => service.TryApplyChanges(updatedDocument.Project.Solution));

        Assert.True(exception is IOException or UnauthorizedAccessException, exception.ToString());
        Assert.Equal(originalSource, (await workspace.CurrentSolution.GetDocument(document.Id).GetTextAsync()).ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class LegacyCodeService : ICodeService
    {
        public Task<List<ISymbol>> GetAllClassSymbolsAsync() => Task.FromResult(new List<ISymbol>());
        public Task<Workspace> GetWorkspaceAsync() => throw new NotSupportedException();
        public bool TryApplyChanges(Solution solution) => throw new NotSupportedException();
        public Task ReloadWorkspaceAsync() => throw new NotSupportedException();
        public Task<List<Document>> GetAllDocumentsAsync() => throw new NotSupportedException();
        public Task<Document> GetDocumentAsync(string documentName) => throw new NotSupportedException();
    }
}
