// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
