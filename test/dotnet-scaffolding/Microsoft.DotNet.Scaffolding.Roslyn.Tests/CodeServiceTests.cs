// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Tests;

public class CodeServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(AppContext.BaseDirectory, nameof(CodeServiceTests), Guid.NewGuid().ToString());

    public CodeServiceTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task TryApplyChanges_FallbackWorkspace_PersistsLazySyntaxChanges()
    {
        var filePath = Path.Combine(_directory, "ApplicationDbContext.cs");
        File.WriteAllText(filePath, "public class ApplicationDbContext { }");
        using var service = await CreateFallbackServiceAsync();
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
        using var service = await CreateFallbackServiceAsync();
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

    [Fact]
    public async Task GetWorkspaceAsync_FallbackWorkspace_ReportsProjectResolutionFailure()
    {
        using var service = new CodeService(NullLogger.Instance, Path.Combine(_directory, "Missing.csproj"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetWorkspaceAsync());

        Assert.Contains("Missing.csproj", exception.Message);
        Assert.Contains("MSB1009", exception.Message);
    }

    private async Task<CodeService> CreateFallbackServiceAsync()
    {
        // Persistence needs compiler inputs, not an SDK restore or a second framework-resolution
        // test. Supply a minimal MSBuild compiler-input project and invoke the production factory.
        var projectPath = Path.Combine(_directory, "Test.csproj");
        File.WriteAllText(projectPath, """
            <Project>
              <ItemGroup>
                <Compile Include="*.cs" />
              </ItemGroup>
              <Target Name="Compile">
                <ItemGroup>
                  <CscCommandLineArgs Include="/target:library;@(Compile)" />
                </ItemGroup>
              </Target>
            </Project>
            """);
        var service = new CodeService(NullLogger.Instance, projectPath);
        var factory = typeof(CodeService).GetMethod("BuildFallbackWorkspaceAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var workspace = await (Task<AdhocWorkspace>)factory.Invoke(service, null);
        typeof(CodeService).GetField("_fallbackWorkspace", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, workspace);
        return service;
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
