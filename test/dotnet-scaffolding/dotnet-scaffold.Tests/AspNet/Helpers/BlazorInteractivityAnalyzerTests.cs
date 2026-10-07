// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class BlazorInteractivityAnalyzerTests
{
    // The framework's Razor component registrations, as the semantic model sees them before the app's packages are
    // restored: 'AddInteractiveWebAssemblyComponents()' comes from the Microsoft.AspNetCore.Components.WebAssembly.Server
    // package and does not resolve yet.
    private const string FrameworkApis = """
        namespace Microsoft.Extensions.DependencyInjection
        {
            public interface IServiceCollection { }
            public interface IRazorComponentsBuilder { }
            public static class RazorComponentsServiceCollectionExtensions
            {
                public static IRazorComponentsBuilder AddRazorComponents(this IServiceCollection services) => null!;
                public static IRazorComponentsBuilder AddInteractiveServerComponents(this IRazorComponentsBuilder builder) => builder;
            }
        }
        """;

    private const string WebAssemblyProgram = """
        using Microsoft.Extensions.DependencyInjection;

        static class Program
        {
            static void Configure(IServiceCollection services, dynamic app)
            {
                services.AddRazorComponents()
                    .AddInteractiveWebAssemblyComponents();
                app.MapRazorComponents();
            }
        }
        """;

    [Fact]
    public async Task AnalyzeAsync_WithSyntaxFallback_CountsUnresolvedPackageRegistrationAsPresent()
    {
        var (interactivity, error) = await BlazorInteractivityAnalyzer.AnalyzeAsync(
            CreateProgramDocument(WebAssemblyProgram, FrameworkApis), "Program.cs", "TestProject.csproj", allowSyntaxFallback: true);

        Assert.Null(error);
        Assert.NotNull(interactivity);
        Assert.True(interactivity.UsesRazorComponents);
        Assert.True(interactivity.UsesInteractiveWebAssembly);
        Assert.False(interactivity.UsesInteractiveServer);
        Assert.True(interactivity.MapsRazorComponents);
    }

    [Fact]
    public async Task AnalyzeAsync_WithoutSyntaxFallback_ReportsUnresolvedRegistration()
    {
        var (interactivity, error) = await BlazorInteractivityAnalyzer.AnalyzeAsync(
            CreateProgramDocument(WebAssemblyProgram, FrameworkApis), "Program.cs", "TestProject.csproj");

        Assert.Null(interactivity);
        Assert.Contains("Unable to resolve Blazor registration 'AddInteractiveWebAssemblyComponents'", error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnalyzeAsync_WithoutBlazorApis_FallsBackOnlyWhenAllowed(bool allowSyntaxFallback)
    {
        // No framework reference pack: none of the Blazor types resolve.
        var (interactivity, error) = await BlazorInteractivityAnalyzer.AnalyzeAsync(
            CreateProgramDocument(WebAssemblyProgram), "Program.cs", "TestProject.csproj", allowSyntaxFallback);

        if (allowSyntaxFallback)
        {
            Assert.Null(error);
            Assert.True(interactivity!.UsesInteractiveWebAssembly);
        }
        else
        {
            Assert.Null(interactivity);
            Assert.Contains("Unable to analyze Blazor registrations", error);
        }
    }

    private static Document CreateProgramDocument(string program, params string[] otherSources)
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]));
        foreach (var (source, index) in otherSources.Select((source, index) => (source, index)))
        {
            project = project.AddDocument($"Framework{index}.cs", SourceText.From(source)).Project;
        }

        return project.AddDocument("Program.cs", SourceText.From(program));
    }
}
