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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TryApplyChanges_FallbackWorkspace_PersistsLazySyntaxChanges(bool preventWrite)
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

        if (preventWrite)
        {
            File.Delete(filePath);
            Directory.CreateDirectory(filePath);
        }

        var result = service.TryApplyChanges(updatedDocument.Project.Solution);

        Assert.Equal(!preventWrite, result);
        if (!preventWrite)
        {
            Assert.Equal(changedSource, File.ReadAllText(filePath));
        }
        else
        {
            Assert.DoesNotContain("Movie", (await workspace.CurrentSolution.GetDocument(document.Id).GetTextAsync()).ToString());
        }
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
