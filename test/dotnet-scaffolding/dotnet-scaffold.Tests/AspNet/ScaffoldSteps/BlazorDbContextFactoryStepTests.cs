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
                public static void Consume(object value) { }
                public static ServiceCollection AddDbContext<T>(this ServiceCollection services,
                    System.Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> configure = null,
                    ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) => services;
                public static ServiceCollection AddDbContextFactory<T>(this ServiceCollection services,
                    System.Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> configure = null,
                    ServiceLifetime lifetime = ServiceLifetime.Singleton) => services;
                public static ServiceCollection AddDbContextFactory<T, TFactory>(this ServiceCollection services) => services;
                public static ServiceCollection AddPooledDbContextFactory<T>(this ServiceCollection services) => services;
                public static ServiceCollection AddQuickGridEntityFrameworkAdapter(this ServiceCollection services) => services;
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
            public class WebApplicationOptions
            {
                public string[] Args { get; set; }
            }
            public class WebApplication
            {
                public static WebApplicationBuilder CreateBuilder(string[] args) => new();
                public static WebApplicationBuilder CreateSlimBuilder(string[] args) => new();
                public static WebApplicationBuilder CreateEmptyBuilder(WebApplicationOptions options) => new();
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
        var adapter = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
            invocation.Expression.ToString() == "builder.Services.AddQuickGridEntityFrameworkAdapter");
        var build = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
            invocation.Expression.ToString() == "builder.Build");
        Assert.True(adapter.SpanStart < build.SpanStart);
        if (registration.Contains(".AddDbContext<", StringComparison.Ordinal))
        {
            Assert.Contains("ServiceLifetime.Scoped", factory.ToString());
            Assert.DoesNotContain("UseSqlServer", factory.ToString());
        }

        var repeated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            updated, "App.MovieContext", """UseSqlServer(builder.Configuration.GetConnectionString("Movies"))""");
        Assert.Equal(text, (await repeated.GetTextAsync()).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("builder.Services.AddDbContext<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext, CustomMovieFactory>();")]
    [InlineData("builder.Services.AddPooledDbContextFactory<MovieContext>();")]
    public async Task RegisterFactoryAsync_DoesNotDuplicateAnExistingAdapter(string registration)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, registration + "\nbuilder.Services.AddQuickGridEntityFrameworkAdapter();");

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer("Movies")""");
        var text = (await updated.GetTextAsync()).ToString();
        var root = (await updated.GetSyntaxRootAsync())!;
        var semanticModel = (await updated.GetSemanticModelAsync())!;
        Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
            invocation.Expression.ToString() == "builder.Services.AddQuickGridEntityFrameworkAdapter");
        Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
            semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
            method.Name is "AddDbContextFactory" or "AddPooledDbContextFactory" &&
            method.TypeArguments[0].ToDisplayString() == "App.MovieContext");
        var compilation = await updated.WithText(SourceText.From(text)).Project.GetCompilationAsync();
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var repeated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            updated, "App.MovieContext", """UseSqlServer("Movies")""");
        Assert.Equal(text, (await repeated.GetTextAsync()).ToString());
    }

    [Theory]
    [InlineData("void NotCalled() { builder.Services.AddQuickGridEntityFrameworkAdapter(); }")]
    [InlineData("if (args.Length > 0) { builder.Services.AddQuickGridEntityFrameworkAdapter(); }")]
    [InlineData("Action notCalled = () => builder.Services.AddQuickGridEntityFrameworkAdapter();")]
    [InlineData("new ServiceCollection().AddQuickGridEntityFrameworkAdapter();")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.Consume(args.Length > 0 ? builder.Services.AddQuickGridEntityFrameworkAdapter() : builder.Services);")]
    public async Task RegisterFactoryAsync_AddsAdapterWhenExistingCallIsNotOnStartupServices(string registration)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace,
            "builder.Services.AddDbContextFactory<MovieContext>();\n" + registration);

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer("Movies")""");
        var text = (await updated.GetTextAsync()).ToString();
        var root = Assert.IsType<CompilationUnitSyntax>(await updated.GetSyntaxRootAsync());
        Assert.Contains(registration, text);
        Assert.Equal(2, root.DescendantNodes().OfType<InvocationExpressionSyntax>().Count(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddQuickGridEntityFrameworkAdapter" }));
        var adapter = Assert.Single(root.Members.OfType<GlobalStatementSyntax>(), statement =>
            statement.Statement is ExpressionStatementSyntax
            {
                Expression: InvocationExpressionSyntax invocation
            } && invocation.Expression.ToString() == "builder.Services.AddQuickGridEntityFrameworkAdapter");
        var build = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
            invocation.Expression.ToString() == "builder.Build");
        Assert.True(adapter.SpanStart < build.SpanStart);
        Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(), invocation =>
            invocation.Expression.ToString() == "builder.Services.AddDbContextFactory<MovieContext>");
        var compilation = await updated.WithText(SourceText.From(text)).Project.GetCompilationAsync();
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var repeated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            updated, "App.MovieContext", """UseSqlServer("Movies")""");
        Assert.Equal(text, (await repeated.GetTextAsync()).ToString());
    }

    [Theory]
    [InlineData("webBuilder")]
    [InlineData("@event")]
    public async Task RegisterFactoryAsync_UsesTheActualBuilderIdentifier(string builderName)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, "", builderName: builderName);

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer(builder.Configuration.GetConnectionString("Movies"))""");

        var text = (await updated.GetTextAsync()).ToString();
        Assert.Contains($"{builderName}.Services.AddDbContextFactory<global::App.MovieContext>", text);
        Assert.Contains($"{builderName}.Services.AddQuickGridEntityFrameworkAdapter()", text);
        Assert.Contains($"""{builderName}.Configuration.GetConnectionString("Movies")""", text);
        Assert.DoesNotContain("builder.", text);
        var compilation = await updated.WithText(SourceText.From(text)).Project.GetCompilationAsync();
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    [Theory]
    [InlineData("if (true) { builder.Services.AddDbContext<MovieContext>(); }")]
    [InlineData("Action configure = () => builder.Services.AddDbContext<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext>(unknownOption: true);")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.Consume(false ? builder.Services.AddDbContextFactory<MovieContext>() : builder.Services);")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.Consume(false && builder.Services.AddDbContextFactory<MovieContext>() != null);")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.Consume(true || builder.Services.AddDbContextFactory<MovieContext>() != null);")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.Consume(builder.Services ?? builder.Services.AddDbContextFactory<MovieContext>());")]
    [InlineData("EntityFrameworkServiceCollectionExtensions.Consume(false switch { true => builder.Services.AddDbContextFactory<MovieContext>(), _ => builder.Services });")]
    [InlineData("var services = builder.Services; services.AddDbContextFactory<MovieContext>();")]
    [InlineData("void Register() { builder.Services.AddDbContextFactory<MovieContext>(); } Register();")]
    [InlineData("class Registration { public static void Register(Microsoft.Extensions.DependencyInjection.ServiceCollection services) { services.AddDbContextFactory<MovieContext>(); } }")]
    [InlineData("var anotherBuilder = WebApplication.CreateBuilder(args);")]
    public async Task RegisterFactoryAsync_WarnsWithoutChangingUnverifiableRegistrations(string registration)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, registration);
        var logger = new Mock<ILogger<BlazorDbContextFactoryStep>>();
        var before = (await document.GetTextAsync()).ToString();

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer("Movies")""", logger: logger.Object);

        Assert.Equal(before, (await updated.GetTextAsync()).ToString());
        AssertManualWarning(logger, """builder.Services.AddDbContextFactory<App.MovieContext>(options => options.UseSqlServer("Movies"));""");
    }

    [Theory]
    [InlineData("CreateBuilder")]
    [InlineData("CreateSlimBuilder")]
    [InlineData("CreateEmptyBuilder")]
    public async Task RegisterFactoryAsync_AcceptsWebApplicationBuilderMethods(string method)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, "", builderMethod: method);
        var logger = new Mock<ILogger<BlazorDbContextFactoryStep>>();

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer("Movies")""", logger: logger.Object);
        var repeated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            updated, "App.MovieContext", """UseSqlServer("Movies")""", logger: logger.Object);

        Assert.Contains("builder.Services.AddDbContextFactory<global::App.MovieContext>", (await updated.GetTextAsync()).ToString());
        Assert.Equal((await updated.GetTextAsync()).ToString(), (await repeated.GetTextAsync()).ToString());
        Assert.DoesNotContain(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) && Equals(invocation.Arguments[0], LogLevel.Warning));
    }

    [Theory]
    [InlineData("no-builder")]
    [InlineData("no-build")]
    [InlineData("multiple-builds")]
    [InlineData("unresolved-context")]
    [InlineData("missing-provider")]
    [InlineData("invalid-provider")]
    [InlineData("conditional-startup")]
    [InlineData("local-function-startup")]
    [InlineData("helper-method-startup")]
    public async Task RegisterFactoryAsync_UnsafeLayoutPreservesOriginalDocument(string scenario)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, "");
        var source = (await document.GetTextAsync()).ToString().Replace("using Microsoft.EntityFrameworkCore;", "");
        source = scenario switch
        {
            "no-builder" => source.Replace("var builder = WebApplication.CreateBuilder(args);", ""),
            "no-build" => source.Replace("var app = builder.Build();", ""),
            "multiple-builds" => source + "\nvar anotherApp = builder.Build();",
            "conditional-startup" => source.Replace("var builder =", "if (args.Length != 0) {\nvar builder =") + "\n}",
            "local-function-startup" => source.Replace("var builder =", "void Configure() {\nvar builder =") + "\n}\nConfigure();",
            "helper-method-startup" => source.Replace("var builder =", "class Registration { public static void Configure(string[] args) {\nvar builder =") + "\n}}",
            _ => source
        };
        document = document.WithText(SourceText.From(source));
        var logger = new Mock<ILogger<BlazorDbContextFactoryStep>>();
        var provider = scenario switch
        {
            "missing-provider" => null,
            "invalid-provider" => "not an invocation",
            _ => """UseSqlServer("Movies")"""
        };
        var context = scenario == "unresolved-context" ? "App.UnknownContext" : "App.MovieContext";

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, context, provider, logger: logger.Object);

        Assert.Equal(source, (await updated.GetTextAsync()).ToString());
        AssertManualWarning(logger, $"builder.Services.AddDbContextFactory<{context}>(");
    }

    [Theory]
    [InlineData("builder.Services.AddDbContext<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext>();")]
    [InlineData("builder.Services.AddDbContextFactory<MovieContext, CustomMovieFactory>();")]
    public async Task RegisterFactoryAsync_ExistingOptionsDoNotRequireProvider(string registration)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, registration);
        var logger = new Mock<ILogger<BlazorDbContextFactoryStep>>();

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", null, logger: logger.Object);
        var repeated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            updated, "App.MovieContext", null, logger: logger.Object);

        var text = (await updated.GetTextAsync()).ToString();
        Assert.Contains(registration, text);
        Assert.Equal(text, (await repeated.GetTextAsync()).ToString());
        Assert.DoesNotContain(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) && Equals(invocation.Arguments[0], LogLevel.Warning));
    }

    [Theory]
    [InlineData("webBuilder")]
    [InlineData("@event")]
    public async Task RegisterFactoryAsync_WarningUsesTheActualBuilderIdentifier(string builderName)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, $"if (true) {{ {builderName}.Services.AddDbContext<MovieContext>(); }}", builderName: builderName);
        var logger = new Mock<ILogger<BlazorDbContextFactoryStep>>();
        var before = (await document.GetTextAsync()).ToString();

        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer(builder.Configuration.GetConnectionString("Movies"))""", logger: logger.Object);

        Assert.Equal(before, (await updated.GetTextAsync()).ToString());
        var warning = Assert.Single(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) && Equals(invocation.Arguments[0], LogLevel.Warning));
        Assert.Contains($"""Add before {builderName}.Build(): {builderName}.Services.AddDbContextFactory<App.MovieContext>(options => options.UseSqlServer({builderName}.Configuration.GetConnectionString("Movies"))); {builderName}.Services.AddQuickGridEntityFrameworkAdapter();""",
            warning.Arguments[2].ToString());
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

    [Fact]
    public async Task RegisterFactoryAsync_EvaluatesConnectionStringBeforeBuild()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, "");
        var updated = await BlazorDbContextFactoryStep.RegisterFactoryAsync(
            document, "App.MovieContext", """UseSqlServer(builder.Configuration.GetConnectionString("Movies") ?? throw new InvalidOperationException("Missing Movies"))""");
        var root = (await updated.GetSyntaxRootAsync())!;
        var declaration = Assert.Single(root.DescendantNodes().OfType<VariableDeclaratorSyntax>(),
            variable => variable.Initializer?.Value is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.CoalesceExpression));
        Assert.Empty(declaration.Ancestors().OfType<AnonymousFunctionExpressionSyntax>());
        var build = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
            invocation => invocation.Expression.ToString() == "builder.Build");
        Assert.True(declaration.SpanStart < build.SpanStart);
        Assert.Contains($"options.UseSqlServer({declaration.Identifier.ValueText})", root.ToFullString());
    }

    private static void AssertManualWarning(Mock<ILogger<BlazorDbContextFactoryStep>> logger, string manualLine)
    {
        var warning = Assert.Single(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) && Equals(invocation.Arguments[0], LogLevel.Warning));
        Assert.Contains("Could not safely update Program.cs. Add before builder.Build():", warning.Arguments[2].ToString());
        Assert.Contains(manualLine, warning.Arguments[2].ToString());
    }

    private static Document CreateDocument(
        AdhocWorkspace workspace, string registration, string builderName = "builder", string builderMethod = "CreateBuilder")
    {
        var builderArguments = builderMethod == "CreateEmptyBuilder" ? "new WebApplicationOptions { Args = args }" : "args";
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

            var {{builderName}} = WebApplication.{{builderMethod}}({{builderArguments}});
            {{registration}}
            var app = {{builderName}}.Build();
            """));
    }
}
