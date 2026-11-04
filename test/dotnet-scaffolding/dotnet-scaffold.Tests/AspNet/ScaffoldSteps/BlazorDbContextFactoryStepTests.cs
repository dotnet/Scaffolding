// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class BlazorDbContextFactoryStepTests
{
    private const string FrameworkApis = """
        namespace Microsoft.Extensions.DependencyInjection
        {
            public enum ServiceLifetime { Singleton, Scoped }
            public class ServiceCollection { }
            public static class EntityFrameworkServiceCollectionExtensions
            {
                public static ServiceCollection AddDbContext<T>(this ServiceCollection services,
                    System.Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> configure = null,
                    ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) => services;
                public static ServiceCollection AddDbContextFactory<T>(this ServiceCollection services,
                    System.Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> configure = null,
                    ServiceLifetime lifetime = ServiceLifetime.Singleton) => services;
                public static ServiceCollection AddDbContextFactory<T, TFactory>(this ServiceCollection services) => services;
                public static ServiceCollection AddPooledDbContextFactory<T>(this ServiceCollection services) => services;
            }
        }
        namespace Microsoft.EntityFrameworkCore
        {
            public class DbContextOptionsBuilder
            {
                public DbContextOptionsBuilder UseSqlServer(string connection) => this;
            }
        }
        namespace Microsoft.AspNetCore.Builder
        {
            public class Configuration
            {
                public string GetConnectionString(string name) => name;
            }
            public class WebApplicationBuilder
            {
                public Microsoft.Extensions.DependencyInjection.ServiceCollection Services { get; } = new();
                public Configuration Configuration { get; } = new();
                public WebApplication Build() => new();
            }
            public class WebApplication
            {
                public static WebApplicationBuilder CreateBuilder(string[] args) => new();
            }
        }
        namespace App
        {
            public class MovieContext { }
            public class CustomMovieFactory { }
        }
        namespace Other
        {
            public class MovieContext { }
        }
        """;

    [Theory]
    [InlineData("")]
    [InlineData("builder.Services.AddDbContext< MovieContext >();")]
    [InlineData("builder.Services.AddDbContext<global::App.MovieContext>();")]
    [InlineData("builder.Services.AddDbContext<MovieAlias>(optionsLifetime: ServiceLifetime.Scoped);")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext, CustomMovieFactory>();")]
    [InlineData("builder.Services.AddPooledDbContextFactory<MovieContext>();")]
    [InlineData("""var text = "builder.Services.AddDbContextFactory<MovieContext>";""")]
    [InlineData("// builder.Services.AddDbContextFactory<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<Other.MovieContext>();")]
    [InlineData("builder.Services.AddDbContext<Other.MovieContext>().AddDbContext<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<Other.MovieContext>().AddDbContextFactory<MovieContext, CustomMovieFactory>();")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.AddDbContext<MovieContext>(builder.Services);")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.AddDbContext<MovieContext>(configure: null, services: builder.Services);")]
    public async Task RegisterFactoryAsync_PreservesExistingRegistrations_AndIsIdempotent(string registration)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, registration);

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer(builder.Configuration.GetConnectionString("Movies"))""");
        var text = (await updated.GetTextAsync()).ToString();
        Assert.Contains(registration, text);
        updated = updated.WithText(SourceText.From(text));
        var compilation = await updated.Project.GetCompilationAsync();
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var root = await updated.GetSyntaxRootAsync();
        var semanticModel = await updated.GetSemanticModelAsync();
        var factory = Assert.Single(root!.DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
            semanticModel!.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
            method.Name is "AddDbContextFactory" or "AddPooledDbContextFactory" &&
            method.TypeArguments[0].ToDisplayString() == "App.MovieContext");
        if (registration.Contains(".AddDbContext<", StringComparison.Ordinal))
        {
            Assert.Contains("ServiceLifetime.Scoped", factory.ToString());
            Assert.DoesNotContain("UseSqlServer", factory.ToString());
        }

        var repeated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            updated, "App.MovieContext", """UseSqlServer(builder.Configuration.GetConnectionString("Movies"))""");
        Assert.Equal(text, (await repeated.GetTextAsync()).ToString());
    }

    [Fact]
    public async Task RegisterFactoryAsync_UsesTheActualBuilderIdentifier()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, "", builderName: "webBuilder");

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer(builder.Configuration.GetConnectionString("Movies"))""");

        var text = (await updated.GetTextAsync()).ToString();
        Assert.Contains("webBuilder.Services.AddDbContextFactory<global::App.MovieContext>", text);
        Assert.Contains("""webBuilder.Configuration.GetConnectionString("Movies")""", text);
        Assert.DoesNotContain("builder.", text);
    }

    [Theory]
    [InlineData("if (true) { builder.Services.AddDbContext<MovieContext>(); }")]
    [InlineData("Action configure = () => builder.Services.AddDbContext<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext>(unknownOption: true);")]
    public async Task RegisterFactoryAsync_RejectsUnverifiableRegistrations(string registration)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, registration);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BlazorDbContextFactoryStep.RegisterFactoryAsync(document, "App.MovieContext", """UseSqlServer("Movies")"""));
    }

    [Fact]
    public async Task ExecuteAsync_MissingSettingsFailsWithADiagnostic()
    {
        var logger = new Mock<ILogger<BlazorDbContextFactoryStep>>();
        var step = new BlazorDbContextFactoryStep(logger.Object);

        Assert.False(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        Assert.Contains(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) && Equals(invocation.Arguments[0], LogLevel.Error) &&
            invocation.Arguments[2].ToString()!.Contains("settings or model are missing"));
    }

    private static Document CreateDocument(AdhocWorkspace workspace, string registration, string builderName = "builder")
    {
        var project = workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(),
            "FactoryTests", "FactoryTests", LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
            metadataReferences: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]));
        var types = workspace.AddDocument(project.Id, "Framework.cs", SourceText.From(FrameworkApis));
        return workspace.AddDocument(types.Project.Id, "Program.cs", SourceText.From($$"""
            using System;
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;
            using App;
            using MovieAlias = App.MovieContext;

            var {{builderName}} = WebApplication.CreateBuilder(args);
            {{registration}}
            var app = {{builderName}}.Build();
            """));
    }
}
