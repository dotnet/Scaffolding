// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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

    public CodeServiceTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void GetAllClassSymbolsAsync_PreservesParameterlessSignatures()
    {
        Assert.NotNull(typeof(ICodeService).GetMethod(nameof(ICodeService.GetAllClassSymbolsAsync), Type.EmptyTypes));
        Assert.NotNull(typeof(CodeService).GetMethod(nameof(CodeService.GetAllClassSymbolsAsync), Type.EmptyTypes));
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_SupportsExistingImplementation()
    {
        var implementation = new ExistingCodeService();
        ICodeService service = implementation;

        Assert.Empty(await service.GetAllClassSymbolsAsync());
        Assert.Empty(await service.GetAllClassSymbolsAsync(includeReferencedProjects: true));
        Assert.Empty(await service.GetAllClassSymbolsAsync(includeReferencedProjects: false));
        Assert.Equal(3, implementation.DiscoveryCalls);
    }

    private sealed class ExistingCodeService : ICodeService
    {
        public int DiscoveryCalls { get; private set; }

        public Task<List<ISymbol>> GetAllClassSymbolsAsync()
        {
            DiscoveryCalls++;
            return Task.FromResult(new List<ISymbol>());
        }

        public Task<Workspace> GetWorkspaceAsync() => throw new NotSupportedException();
        public bool TryApplyChanges(Solution solution) => throw new NotSupportedException();
        public Task ReloadWorkspaceAsync() => throw new NotSupportedException();
        public Task<List<Document>> GetAllDocumentsAsync() => throw new NotSupportedException();
        public Task<Document> GetDocumentAsync(string documentName) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAllClassSymbolsAsync_IncludesReferencedProjects(bool directModelReference)
    {
        var modelProject = WriteProject("Models", """
            namespace Models;
            public class Movie { public int Id { get; set; } }
            """);
        WriteProject("Repository", "namespace Repository; public class MyDbContext { }",
            """<ProjectReference Include="..\Models\Models.csproj" />""");
        var modelReference = directModelReference
            ? """<ProjectReference Include="..\Models\Models.csproj" />"""
            : string.Empty;
        var webProject = WriteProject("Web", "namespace Web; public class Movie { }",
            $"""<ProjectReference Include="..\Repository\Repository.csproj" />{modelReference}""");
        using var service = new CodeService(NullLogger.Instance, webProject);
        Assert.IsNotType<AdhocWorkspace>(await service.GetWorkspaceAsync());

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var symbols = await service.GetAllClassSymbolsAsync();

            Assert.Equal(3, symbols.Count);
            Assert.Equal("Web.Movie", symbols[0].ToDisplayString());
            var model = Assert.Single(symbols, symbol => symbol.ToDisplayString() == "Models.Movie");
            Assert.Equal(Path.Combine(Path.GetDirectoryName(modelProject), "Classes.cs"),
                Assert.Single(model.Locations).SourceTree.FilePath);
            Assert.Contains(symbols, symbol => symbol.ToDisplayString() == "Repository.MyDbContext");
        }

        Assert.Equal("Web.Movie", Assert.Single(
            await service.GetAllClassSymbolsAsync(includeReferencedProjects: false)).ToDisplayString());
        await service.ReloadWorkspaceAsync();
        Assert.Equal(3, (await service.GetAllClassSymbolsAsync()).Count);
    }

    [Fact]
    public async Task GetAllClassSymbolsAsync_FallbackCompilation_PreservesLocalClasses()
    {
        File.WriteAllText(Path.Combine(_directory, "Movie.cs"), "public class Movie { public int Id { get; set; } }");
        using var service = new CodeService(NullLogger.Instance, Path.Combine(_directory, "Missing.csproj"));

        var symbol = Assert.Single(await service.GetAllClassSymbolsAsync());

        Assert.Equal("Movie", symbol.Name);
    }

    private string WriteProject(string name, string source, string references = "")
    {
        var directory = Path.Combine(_directory, name);
        Directory.CreateDirectory(directory);
        var projectPath = Path.Combine(directory, name + ".csproj");
        File.WriteAllText(projectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup>{references}</ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(directory, "Classes.cs"), source);
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
        Directory.Delete(_directory, recursive: true);
    }
}
