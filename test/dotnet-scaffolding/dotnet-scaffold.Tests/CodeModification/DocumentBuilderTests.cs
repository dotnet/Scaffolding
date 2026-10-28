// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
            var configuration = Assert.Single(root!.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure"));
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
            Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "KeepChain"));
        }
    }

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
