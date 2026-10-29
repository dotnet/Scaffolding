// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class DocumentBuilderTests
{
    [Theory]
    [InlineData(true, "method")]
    [InlineData(false, "method")]
    [InlineData(false, "localFunction")]
    [InlineData(false, "lambda")]
    [InlineData(false, "authenticationBuilder")]
    public async Task InvocationArguments_HelperSchemesCompileInTheirConfigurationScope(bool constant, string scope)
    {
        var registration = $$"""
            {{(constant ? "const " : "")}}string scheme = "Custom\"\\Oidc";
            {{(scope == "authenticationBuilder" ? "var authentication = services.AddAuthentication(scheme);" : "")}}
            {{(scope == "authenticationBuilder" ? "authentication" : "services.AddAuthentication(scheme)")}}
                .AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: scheme).KeepChain();
            """;
        var helper = scope switch
        {
            "localFunction" => $"void Configure(Services services) {{ {registration} }}",
            "lambda" => $"static class Registration {{ public static void Configure(Services services) {{ System.Action register = () => {{ {registration} }}; register(); }} }}",
            _ => $"static class Registration {{ public static void Configure(Services services) {{ {registration} }} }}"
        };
        var source = $$"""
            var builder = new Builder();
            {{(scope == "localFunction" ? "Configure" : "Registration.Configure")}}(builder.Services);
            var app = builder.Build();
            {{helper}}
            class Builder
            {
                public Services Services { get; } = new Services();
                public object Build() => new object();
            }
            class Services
            {
                public AuthenticationBuilder AddAuthentication(string scheme) => new AuthenticationBuilder(this);
                public void PostConfigure<T>(string scheme, System.Action<T> configure) { }
            }
            class AuthenticationBuilder
            {
                public AuthenticationBuilder(Services services) { Services = services; }
                public Services Services { get; }
                public AuthenticationBuilder AddMicrosoftIdentityWebApp(object configuration, string openIdConnectScheme = "Default") => this;
                public AuthenticationBuilder KeepChain() => this;
            }
            class OpenIdConnectOptions { public string ResponseType { get; set; } = ""; }
            """;
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Test", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var document = project.AddDocument("Program.cs", SourceText.From(source));
        var originalCompilation = await document.Project.GetCompilationAsync();
        Assert.Empty(originalCompilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        for (int run = 0; run < 2; run++)
        {
            var snippet = new CodeSnippet
            {
                Parent = "AddMicrosoftIdentityWebApp",
                InvocationArguments = new() { ["openIdConnectScheme"] = "\"Default\"" },
                InsertBefore = ["var app = builder.Build();"],
                Block = "builder.Services.PostConfigure<OpenIdConnectOptions>($openIdConnectScheme$, options => { options.ResponseType = \"code\"; })"
            };
            var file = new CodeFile
            {
                FileName = "Program.cs",
                Methods = new() { ["Global"] = new Method { CodeChanges = [snippet] } }
            };
            document = await new DocumentBuilder(document, file, [Constants.UseTopLevelStatements], NullLogger.Instance).RunAsync();
            // Compile the persisted source, including the insertion machinery's trivia.
            document = document.WithSyntaxRoot(CSharpSyntaxTree.ParseText((await document.GetTextAsync()).ToString()).GetRoot());
            var compilation = await document.Project.GetCompilationAsync();
            Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var root = await document.GetSyntaxRootAsync();
            var configuration = Assert.Single(root!.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure");
            if (constant)
            {
                Assert.IsType<GlobalStatementSyntax>(configuration.Parent!.Parent);
                var literal = Assert.IsType<LiteralExpressionSyntax>(configuration.ArgumentList.Arguments[0].Expression);
                Assert.Equal("Custom\"\\Oidc", literal.Token.ValueText);
            }
            else
            {
                Assert.Equal("scheme", configuration.ArgumentList.Arguments[0].Expression.ToString());
                var block = Assert.IsType<BlockSyntax>(configuration.Parent!.Parent);
                Assert.Contains(block.Statements.OfType<LocalDeclarationStatementSyntax>(),
                    statement => statement.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "scheme"));
                Assert.Equal(scope == "authenticationBuilder" ? "authentication.Services" : "services",
                    ((MemberAccessExpressionSyntax)configuration.Expression).Expression.ToString());
            }
            Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "KeepChain");
        }
    }

    [Fact]
    public async Task InvocationArguments_RejectSideEffectingSchemeWithoutRewritingDocument()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateRegistrationDocument(workspace, """
            var services = new Services();
            services.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: NextScheme());
            string NextScheme() => System.Guid.NewGuid().ToString();
            """);
        var originalText = await document.GetTextAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ScaffoldAsync(document));

        Assert.Contains("openIdConnectScheme", error.Message);
        Assert.Contains("local variable", error.Message);
        Assert.Contains("NextScheme()", error.Message);
        Assert.Equal(originalText.ToString(), (await document.GetTextAsync()).ToString());
    }

    [Theory]
    [InlineData("", "Scheme", "")]
    [InlineData("", "NextScheme()", "")]
    [InlineData("var scheme = new SchemeValue();", "scheme", "")]
    [InlineData("var holder = new SchemeValue();", "holder.Scheme", "")]
    [InlineData("string scheme = \"First\";", "(scheme = NextScheme())", "")]
    [InlineData("string scheme = \"First\"; System.Action mutate = () => scheme = NextScheme();", "scheme", "")]
    [InlineData("string scheme = \"First\"; Change(ref scheme);", "scheme", "")]
    [InlineData("string scheme = \"First\"; Observe(scheme);", "scheme", "")]
    [InlineData("string scheme = \"First\"; ref string alias = ref scheme;", "scheme", "")]
    [InlineData("string scheme = \"First\";", "scheme", ".KeepChain(scheme = \"Changed\")")]
    public async Task InvocationArguments_RejectSchemeValuesThatCannotBeSafelyReused(string declarations, string scheme, string suffix)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateRegistrationDocument(workspace, $$"""
            Registration.Configure(new Services());
            static class Registration
            {
                static string NextScheme() => System.Guid.NewGuid().ToString();
                static string Scheme => NextScheme();
                static void Change(ref string value) => value = NextScheme();
                static void Observe(in string value) { }
                public static void Configure(Services services)
                {
                    {{declarations}}
                    services.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: {{scheme}}){{suffix}};
                }
            }
            """);
        var compilation = await document.Project.GetCompilationAsync();
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ScaffoldAsync(document));

        Assert.Contains("openIdConnectScheme", error.Message);
        Assert.Contains("local variable", error.Message);
    }

    [Theory]
    [InlineData("Services")]
    [InlineData("new Holder().Services")]
    [InlineData("holder.Services")]
    [InlineData("GetServices()")]
    public async Task InvocationArguments_RejectServiceCollectionGetter(string receiver)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateRegistrationDocument(workspace, """
            Registration.Configure();
            static class Registration
            {
                static Services Services => new Services();
                static Services GetServices() => new Services();
                public static void Configure()
                {
                    var holder = new Holder();
                    string scheme = "First";
                    RECEIVER.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: scheme);
                }
            }
            class Holder { public Services Services => new Services(); }
            """.Replace("RECEIVER", receiver, StringComparison.Ordinal));
        var compilation = await document.Project.GetCompilationAsync();
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ScaffoldAsync(document));

        Assert.Contains("service collection", error.Message);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore", "builder.Services", true)]
    [InlineData("Application", "builder.Services", false)]
    [InlineData("Microsoft.AspNetCore", "CreateBuilder().Services", false)]
    [InlineData("Microsoft.AspNetCore", "builder.Services", false, ".KeepChain(builder = CreateBuilder())")]
    public async Task InvocationArguments_ReuseOnlyFrameworkServicesOnStableBuilder(
        string assemblyName, string receiver, bool safe, string suffix = "")
    {
        using var workspace = new AdhocWorkspace();
        var frameworkSource = """
            namespace Microsoft.AspNetCore.Builder
            {
                public class WebApplicationBuilder
                {
                    public Services Services { get; } = new Services();
                }
            }
            public class Services
            {
                public AuthenticationBuilder AddAuthentication() => new AuthenticationBuilder();
                public void PostConfigure<T>(string scheme, System.Action<T> configure) { }
            }
            public class AuthenticationBuilder
            {
                public AuthenticationBuilder AddMicrosoftIdentityWebApp(object configuration, string openIdConnectScheme = "Default") => this;
                public AuthenticationBuilder KeepChain(object value) => this;
            }
            public class OpenIdConnectOptions { public string ResponseType { get; set; } = ""; }
            """;
        using var assembly = new MemoryStream();
        var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var emitted = CSharpCompilation.Create(assemblyName, [CSharpSyntaxTree.ParseText(frameworkSource)], [reference],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).Emit(assembly);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        var project = workspace.AddProject("Test", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .AddMetadataReferences([reference, MetadataReference.CreateFromImage(assembly.ToArray())]);
        var document = project.AddDocument("Program.cs", SourceText.From($$"""
            var builder = CreateBuilder();
            string scheme = "First";
            {{receiver}}.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: scheme){{suffix}};
            scheme = "Second";
            {{receiver}}.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: scheme){{suffix}};
            static Microsoft.AspNetCore.Builder.WebApplicationBuilder CreateBuilder() => new Microsoft.AspNetCore.Builder.WebApplicationBuilder();
            """));
        var compilation = await document.Project.GetCompilationAsync();
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        if (!safe)
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ScaffoldAsync(document));
            Assert.Contains("service collection", error.Message);
            return;
        }

        for (int run = 0; run < 2; run++)
        {
            document = await ScaffoldAsync(document);
            document = document.WithText(SourceText.From((await document.GetTextAsync()).ToString()));
            compilation = await document.Project.GetCompilationAsync();
            Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var root = (await document.GetSyntaxRootAsync())!;
            var registrations = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "AddMicrosoftIdentityWebApp").ToArray();
            var configurations = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure").ToArray();
            Assert.Equal(2, registrations.Length);
            Assert.Equal(2, configurations.Length);
            foreach (var registration in registrations)
            {
                var anchor = registration.Ancestors().OfType<GlobalStatementSyntax>().First();
                var next = anchor.Parent!.ChildNodes().SkipWhile(node => node != anchor).Skip(1).First();
                var configuration = Assert.Single(configurations, configuration => next.Span.Contains(configuration.Span));
                Assert.Equal("builder.Services", ((MemberAccessExpressionSyntax)configuration.Expression).Expression.ToString());
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvocationArguments_ReassignedSchemeGetsConfigurationAfterEachRegistration(bool topLevel)
    {
        using var workspace = new AdhocWorkspace();
        const string registration = "services.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: scheme);";
        var statements = $"string scheme = \"First\"; {registration}";
        var source = topLevel
            ? $"var services = new Services(); {statements}"
            : $"Registration.Configure(new Services()); static class Registration {{ public static void Configure(Services services) {{ {statements} }} }}";
        var document = await ScaffoldAsync(CreateRegistrationDocument(workspace, source));
        var persisted = (await document.GetTextAsync()).ToString();
        Assert.Single((await document.GetSyntaxRootAsync())!.DescendantNodes().OfType<InvocationExpressionSyntax>(),
            invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure");

        // Simulate editing the persisted file after scaffolding the first registration.
        var root = CSharpSyntaxTree.ParseText(persisted).GetRoot();
        var configuration = root.DescendantNodes().OfType<ExpressionStatementSyntax>()
            .Single(statement => statement.Expression is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure");
        persisted = persisted.Insert(configuration.Span.End, $"\nscheme = \"Second\";\n{registration}\n");
        document = document.WithText(SourceText.From(persisted));

        for (int run = 0; run < 2; run++)
        {
            document = await ScaffoldAsync(document);
            document = document.WithText(SourceText.From((await document.GetTextAsync()).ToString()));
            var compilation = await document.Project.GetCompilationAsync();
            Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            root = (await document.GetSyntaxRootAsync())!;
            var registrations = root.DescendantNodes().OfType<ExpressionStatementSyntax>()
                .Where(statement => statement.Expression is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "AddMicrosoftIdentityWebApp").ToArray();
            var configurations = root.DescendantNodes().OfType<ExpressionStatementSyntax>()
                .Where(statement => statement.Expression is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure").ToArray();
            Assert.Equal(2, registrations.Length);
            Assert.Equal(2, configurations.Length);
            for (int index = 0; index < registrations.Length; index++)
            {
                SyntaxNode anchor = registrations[index].Parent is GlobalStatementSyntax global ? global : registrations[index];
                var next = anchor.Parent!.ChildNodes().SkipWhile(node => node != anchor).Skip(1).First();
                Assert.Same(configurations[index], next is GlobalStatementSyntax nextGlobal ? nextGlobal.Statement : next);
            }
            Assert.True(configurations[0].Span.End < root.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Single(assignment => assignment.Left is IdentifierNameSyntax { Identifier.ValueText: "scheme" }).SpanStart);
        }
    }

    private static Document CreateRegistrationDocument(AdhocWorkspace workspace, string statements)
    {
        var source = statements + """

            class Services
            {
                public AuthenticationBuilder AddAuthentication() => new AuthenticationBuilder(this);
                public void PostConfigure<T>(string scheme, System.Action<T> configure) { }
            }
            class AuthenticationBuilder
            {
                public AuthenticationBuilder(Services services) { Services = services; }
                public Services Services { get; }
                public AuthenticationBuilder AddMicrosoftIdentityWebApp(object configuration, string openIdConnectScheme = "Default") => this;
                public AuthenticationBuilder KeepChain(string value) => this;
            }
            class OpenIdConnectOptions { public string ResponseType { get; set; } = ""; }
            class SchemeValue
            {
                public string Scheme => System.Guid.NewGuid().ToString();
                public static implicit operator string(SchemeValue value) => value.Scheme;
            }
            """;
        var project = workspace.AddProject("Test", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        return project.AddDocument("Program.cs", SourceText.From(source));
    }

    private static Task<Document> ScaffoldAsync(Document document) => new DocumentBuilder(document, new CodeFile
    {
        FileName = "Program.cs",
        Methods = new()
        {
            ["Global"] = new Method
            {
                CodeChanges =
                [
                    new CodeSnippet
                    {
                        Parent = "AddMicrosoftIdentityWebApp",
                        InvocationArguments = new() { ["openIdConnectScheme"] = "\"Default\"" },
                        Block = "builder.Services.PostConfigure<OpenIdConnectOptions>($openIdConnectScheme$, options => { options.ResponseType = \"code\"; });"
                    }
                ]
            }
        }
    }, [Constants.UseTopLevelStatements], NullLogger.Instance).RunAsync();

    [Fact]
    public void InvocationArguments_BindNamedAndPositionalOverloadsAndDeduplicateSchemes()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            class Registration
            {
                static void AddIdentity(object configuration, string scheme = "Default") { }
                static void AddIdentity(object configuration, object cookies, string scheme = "Default") { }
                static void Configure()
                {
                    AddIdentity(new object());
                    AddIdentity(new object(), "Positional");
                    AddIdentity(new object(), new object(), "OtherOverload");
                    AddIdentity(new object(), scheme: "Named");
                    AddIdentity(new object(), scheme: "Named");
                }
            }
            """);
        var compilation = CSharpCompilation.Create("Test", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var snippet = CreateSnippet();
        snippet.Parent = "AddIdentity";
        snippet.InvocationArguments = new() { ["scheme"] = "\"Default\"" };
        snippet.Block = "Configure($scheme$);";

        var expanded = DocumentBuilder.ExpandInvocationArguments([snippet], tree.GetRoot(), compilation.GetSemanticModel(tree));

        Assert.Equal(["Configure(\"Default\");", "Configure(\"Positional\");", "Configure(\"OtherOverload\");", "Configure(\"Named\");"],
            expanded.Select(change => change.Block).ToArray());
        expanded[0].LeadingTrivia.NumberOfSpaces = 8;
        Assert.Equal(0, expanded[1].LeadingTrivia.NumberOfSpaces);
        Assert.Equal(0, snippet.LeadingTrivia.NumberOfSpaces);
    }

    [Fact]
    public void InvocationArguments_UseDefaultWhenNoInvocationExists()
    {
        var expanded = DocumentBuilder.ExpandInvocationArguments(
            [CreateSnippet()], CSharpSyntaxTree.ParseText("Console.WriteLine(\"AddIdentity\");").GetRoot(), null);

        Assert.Equal("Configure(\"Default\");", Assert.Single(expanded).Block);
    }

    [Fact]
    public void InvocationArguments_DoNotSilentlyUseDefaultForUnresolvedPositionalArguments()
    {
        Assert.Throws<InvalidOperationException>(() => DocumentBuilder.ExpandInvocationArguments(
            [CreateSnippet()], CSharpSyntaxTree.ParseText("AddIdentity(null, \"Custom\");").GetRoot(), null));
    }

    private static CodeSnippet CreateSnippet() => new()
    {
        Parent = "AddIdentity",
        InvocationArguments = new() { ["scheme"] = "\"Default\"" },
        Block = "Configure($scheme$);"
    };
}
