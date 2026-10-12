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

        namespace Microsoft.AspNetCore.Builder
        {
            public sealed class RazorComponentsEndpointConventionBuilder { }
            public static class ServerRazorComponentsEndpointConventionBuilderExtensions
            {
                public static RazorComponentsEndpointConventionBuilder AddInteractiveServerRenderMode(this RazorComponentsEndpointConventionBuilder builder) => builder;
            }
        }
        """;

    // A Blazor Web App's Program.cs whose registrations all resolve. The mapped endpoints are passed in so that the chained
    // render mode binds to the framework API; '{0}' is replaced with the endpoint render mode chain.
    private const string ServerProgramFormat = """
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.DependencyInjection;

        static class Program
        {{
            static void Configure(IServiceCollection services, dynamic app, RazorComponentsEndpointConventionBuilder endpoints)
            {{
                services.AddRazorComponents()
                    .AddInteractiveServerComponents();
                app.MapRazorComponents();
                endpoints{0};
            }}
        }}
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
        Assert.False(interactivity.MapsInteractiveWebAssemblyRenderMode);
        Assert.True(interactivity.HasPartialInteractiveWebAssembly);
        Assert.False(interactivity.HasCompleteInteractiveWebAssembly);
    }

    [Theory]
    [InlineData(".AddInteractiveServerRenderMode()", true)]
    [InlineData(".ToString()", false)]
    public async Task AnalyzeAsync_TracksTheInteractiveServerEndpointRenderModeSeparately(string endpointChain, bool mapsRenderMode)
    {
        var (interactivity, error) = await BlazorInteractivityAnalyzer.AnalyzeAsync(
            CreateProgramDocument(string.Format(ServerProgramFormat, endpointChain), FrameworkApis), "Program.cs", "TestProject.csproj");

        Assert.Null(error);
        Assert.NotNull(interactivity);
        Assert.True(interactivity.UsesInteractiveServer);
        Assert.Equal(mapsRenderMode, interactivity.MapsInteractiveServerRenderMode);
        Assert.Equal(mapsRenderMode, interactivity.HasCompleteInteractiveServer);
        Assert.Equal(!mapsRenderMode, interactivity.HasPartialInteractiveServer);
    }

    [Fact]
    public async Task AnalyzeAsync_WithoutSyntaxFallback_ReportsUnresolvedEndpointRenderMode()
    {
        // 'AddInteractiveWebAssemblyRenderMode()' comes from a package that is not restored yet.
        var (interactivity, error) = await BlazorInteractivityAnalyzer.AnalyzeAsync(
            CreateProgramDocument(string.Format(ServerProgramFormat, ".AddInteractiveWebAssemblyRenderMode()"), FrameworkApis), "Program.cs", "TestProject.csproj");

        Assert.Null(interactivity);
        Assert.Contains("Unable to resolve Blazor registration 'AddInteractiveWebAssemblyRenderMode'", error);
    }

    [Fact]
    public async Task AnalyzeAsync_WithSyntaxFallback_MatchesEndpointRenderModesByName()
    {
        const string program = """
            static class Program
            {
                static void Configure(dynamic services, dynamic app)
                {
                    services.AddRazorComponents()
                        .AddInteractiveWebAssemblyComponents();
                    app.MapRazorComponents()
                        .AddInteractiveServerRenderMode()
                        .AddInteractiveWebAssemblyRenderMode();
                }
            }
            """;

        var (interactivity, error) = await BlazorInteractivityAnalyzer.AnalyzeAsync(
            CreateProgramDocument(program), "Program.cs", "TestProject.csproj", allowSyntaxFallback: true);

        Assert.Null(error);
        Assert.NotNull(interactivity);
        Assert.True(interactivity.HasCompleteInteractiveWebAssembly);
        Assert.True(interactivity.MapsInteractiveServerRenderMode);
        Assert.False(interactivity.UsesInteractiveServer);
        Assert.True(interactivity.HasPartialInteractiveServer);
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
