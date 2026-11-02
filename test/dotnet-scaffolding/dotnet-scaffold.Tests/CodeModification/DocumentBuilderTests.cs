// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class DocumentBuilderTests
{
    [Theory]
    [InlineData("", "Default")]
    [InlineData(", openIdConnectScheme: \"CustomOidc\"", "CustomOidc")]
    public async Task InvocationArguments_GlobalGuardReusesSiblingHelperRegistration(string argument, string scheme)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateRegistrationDocument(workspace, $$"""
            class Program
            {
                static void Main()
                {
                    var builder = new Builder();
                    ConfigureAuthentication(builder.Services);
                    var app = builder.Build();
                }
                static void ConfigureAuthentication(Services services)
                {
                    services.AddAuthentication().AddMicrosoftIdentityWebApp(new object(){{argument}});
                }
            }
            class Builder
            {
                public Services Services { get; } = new Services();
                public object Build() => new object();
            }
            """);
        var compilation = (await document.Project.GetCompilationAsync())!;
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        for (int run = 0; run < 2; run++)
        {
            var file = new CodeFile
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
                                CheckInvocation = "AddMicrosoftIdentityWebApp",
                                InsertBefore = ["var app = builder.Build();"],
                                Block = "builder.Services.AddAuthentication().AddMicrosoftIdentityWebApp(new object());"
                            },
                            new CodeSnippet
                            {
                                Parent = "AddMicrosoftIdentityWebApp",
                                InvocationArguments = new() { ["openIdConnectScheme"] = "\"Default\"" },
                                InsertBefore = ["var app = builder.Build();"],
                                Block = "builder.Services.PostConfigure<OpenIdConnectOptions>($openIdConnectScheme$, options => { options.ResponseType = \"code\"; });"
                            }
                        ]
                    }
                }
            };
            document = await new DocumentBuilder(document, file, [], NullLogger.Instance).RunAsync();
            document = document.WithText(SourceText.From((await document.GetTextAsync()).ToString()));
            compilation = (await document.Project.GetCompilationAsync())!;
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var root = (await document.GetSyntaxRootAsync())!;
            var registration = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "AddMicrosoftIdentityWebApp");
            Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "AddAuthentication");
            Assert.Equal(argument.Length == 0 ? 1 : 2, registration.ArgumentList.Arguments.Count);
            if (argument.Length > 0)
            {
                Assert.Equal(scheme, Assert.IsType<LiteralExpressionSyntax>(registration.ArgumentList.Arguments[1].Expression).Token.ValueText);
                Assert.Equal("openIdConnectScheme", registration.ArgumentList.Arguments[1].NameColon!.Name.Identifier.ValueText);
            }
            var configuration = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure");
            Assert.Equal(scheme, Assert.IsType<LiteralExpressionSyntax>(configuration.ArgumentList.Arguments[0].Expression).Token.ValueText);
            Assert.Equal("services", ((MemberAccessExpressionSyntax)configuration.Expression).Expression.ToString());
            var assignment = Assert.Single(configuration.DescendantNodes().OfType<AssignmentExpressionSyntax>());
            Assert.Equal("options.ResponseType", assignment.Left.ToString());
            Assert.Equal("code", Assert.IsType<LiteralExpressionSyntax>(assignment.Right).Token.ValueText);
            var helper = configuration.Ancestors().OfType<MethodDeclarationSyntax>().First();
            Assert.Equal("ConfigureAuthentication", helper.Identifier.ValueText);
            Assert.Same(helper, registration.Ancestors().OfType<MethodDeclarationSyntax>().First());
        }
    }

    [Fact]
    public void CheckInvocation_MethodGuardDoesNotSearchSiblingMethods()
    {
        var root = CSharpSyntaxTree.ParseText("""
            class Registration
            {
                void Configure() { }
                void Other() { services.AddMicrosoftIdentityWebApp(configuration); }
            }
            """).GetCompilationUnitRoot();
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var updated = DocumentBuilder.ApplyChangesToMethod(method.Body,
            [new CodeSnippet { CheckInvocation = "AddMicrosoftIdentityWebApp", Block = "services.AddMicrosoftIdentityWebApp(configuration);" }]);

        Assert.Single(updated!.DescendantNodes().OfType<InvocationExpressionSyntax>());
    }

    [Fact]
    public async Task InvocationArguments_FallbackWorkspace_ReusesDefaultWebApplicationRegistration()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, $"FallbackWorkspace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(directory, "Directory.Build.targets"), "<Project />");
            File.WriteAllText(Path.Combine(directory, "global.json"),
                "{\"sdk\":{\"version\":\"" + Environment.Version.Major + ".0.100-0\",\"rollForward\":\"latestMajor\",\"allowPrerelease\":true}}");
            var projectPath = Path.Combine(directory, "WebApp.csproj");
            File.WriteAllText(projectPath, $$"""
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>net{{Environment.Version.Major}}.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <LangVersion>12</LangVersion>
                    <DefineConstants>$(DefineConstants);FALLBACK_REGISTRATION</DefineConstants>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(directory, "Program.cs"), """
                var webHost = WebApplication.CreateBuilder(args);
                webHost.Services.AddAuthentication("OpenIdConnect")
                    .AddMicrosoftIdentityWebApp(webHost.Configuration);
                var app = webHost.Build();
                app.Run();

                #if !FALLBACK_REGISTRATION
                #error The project's compilation constants must be preserved.
                #endif

                class OpenIdConnectOptions { public string ResponseType { get; set; } = ""; }
                static class IdentityExtensions
                {
                    public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddMicrosoftIdentityWebApp(
                        this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder,
                        Microsoft.Extensions.Configuration.IConfiguration configuration,
                        string openIdConnectScheme = "OpenIdConnect") => builder;
                }
                """);
            // Restore only the synthetic fixture's SDK framework assets, never a customer project.
            var restore = DotnetCliRunner.CreateDotNet("restore", [projectPath, "--ignore-failed-sources"]);
            restore._psi.WorkingDirectory = directory;
            Assert.True(restore.ExecuteAndCaptureOutput(out var output, out var error) == 0, output + error);
            using var service = new CodeService(NullLogger.Instance, projectPath);
            // Invoke the exact production fallback independently of whether MSBuildWorkspace can
            // load this SDK on the machine running the test.
            var buildFallback = typeof(CodeService).GetMethod("BuildFallbackWorkspaceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            using var workspace = (await (Task<AdhocWorkspace?>)buildFallback.Invoke(service, null)!)!;
            var project = Assert.Single(workspace.CurrentSolution.Projects);
            var document = Assert.Single(project.Documents, document => document.Name == "Program.cs");

            for (var run = 0; run < 2; run++)
            {
                var snippet = new CodeSnippet
                {
                    Parent = "AddMicrosoftIdentityWebApp",
                    InvocationArguments = new() { ["openIdConnectScheme"] = "\"OpenIdConnect\"" },
                    InsertBefore = ["var app = webHost.Build();"],
                    Block = "builder.Services.PostConfigure<OpenIdConnectOptions>($openIdConnectScheme$, options => { options.ResponseType = \"code\"; });"
                };
                var file = new CodeFile
                {
                    FileName = "Program.cs",
                    Methods = new() { ["Global"] = new Method { CodeChanges = [snippet] } }
                };
                document = await new DocumentBuilder(document, file, [Constants.UseTopLevelStatements], NullLogger.Instance).RunAsync();
                document = document.WithText(await document.GetTextAsync());
                var root = (await document.GetSyntaxRootAsync())!;
                var configuration = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                    invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure");
                Assert.Equal("webHost.Services", ((MemberAccessExpressionSyntax)configuration.Expression).Expression.ToString());
                Assert.Equal("\"OpenIdConnect\"", configuration.ArgumentList.Arguments[0].Expression.ToString());
                var compilation = (await document.Project.GetCompilationAsync())!;
                Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            }

            Assert.Equal(LanguageVersion.CSharp12, ((CSharpParseOptions)project.ParseOptions!).LanguageVersion);
            Assert.Equal(NullableContextOptions.Enable, ((CSharpCompilationOptions)project.CompilationOptions!).NullableContextOptions);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(true, "method")]
    [InlineData(true, "localFunction")]
    [InlineData(true, "lambda")]
    [InlineData(true, "authenticationBuilder")]
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
                var literal = Assert.IsType<LiteralExpressionSyntax>(configuration.ArgumentList.Arguments[0].Expression);
                Assert.Equal("Custom\"\\Oidc", literal.Token.ValueText);
            }
            else
            {
                Assert.Equal("scheme", configuration.ArgumentList.Arguments[0].Expression.ToString());
            }
            var block = Assert.IsType<BlockSyntax>(configuration.Parent!.Parent);
            Assert.Contains(block.Statements.OfType<LocalDeclarationStatementSyntax>(),
                statement => statement.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "scheme"));
            Assert.Equal(scope == "authenticationBuilder" ? "authentication.Services" : "services",
                ((MemberAccessExpressionSyntax)configuration.Expression).Expression.ToString());
            Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(),
                invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "KeepChain");
        }
    }

    [Theory]
    [InlineData(true, "", true)]
    [InlineData(false, "", true)]
    [InlineData(true, ", openIdConnectScheme: \"CustomOidc\"", true)]
    [InlineData(false, ", openIdConnectScheme: \"CustomOidc\"", true)]
    [InlineData(true, "", false)]
    [InlineData(false, "", false)]
    [InlineData(true, ", openIdConnectScheme: \"CustomOidc\"", false)]
    [InlineData(false, ", openIdConnectScheme: \"CustomOidc\"", false)]
    public async Task InvocationArguments_ConstantAndDefaultSchemesKeepEachReceiverAndBranch(bool topLevel, string argument, bool branching)
    {
        using var workspace = new AdhocWorkspace();
        var registration = $"AddAuthentication().AddMicrosoftIdentityWebApp(new object(){argument});";
        var statements = branching ? $$"""
            if (enabled)
            {
                first.{{registration}}
            }
            else
            {
                second.{{registration}}
            }
            """ : $"first.{registration} second.{registration}";
        var calls = branching ? "Registration.Configure(first, second, true); Registration.Configure(first, second, false);" :
            "Registration.Configure(first, second, true);";
        var source = $$"""
            var first = new Services();
            var second = new Services();
            var builder = new AuthenticationBuilder(new Services());
            {{(topLevel ? (branching ? $"foreach (bool enabled in new[] {{ true, false }}) {{ {statements} }}" : statements) : calls)}}
            if (first.Configurations != 1 || second.Configurations != 1 || builder.Services.Configurations != 0)
                throw new System.Exception("Configuration used the wrong receiver or branch.");
            {{(topLevel ? "" : $"static class Registration {{ public static void Configure(Services first, Services second, bool enabled) {{ {statements} }} }}")}}
            """;
        var document = CreateRegistrationDocument(workspace, source);
        for (int run = 0; run < 2; run++)
        {
            document = await ScaffoldAsync(document);
            document = document.WithText(SourceText.From((await document.GetTextAsync()).ToString()));
            var root = (await document.GetSyntaxRootAsync())!;
            var registrations = root.DescendantNodes().OfType<ExpressionStatementSyntax>()
                .Where(statement => statement.Expression is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "AddMicrosoftIdentityWebApp").ToArray();
            Assert.Equal(2, registrations.Length);
            Assert.Equal(2, root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Count(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure"));
            foreach (var registrationStatement in registrations)
            {
                SyntaxNode anchor = registrationStatement.Parent is GlobalStatementSyntax global ? global : registrationStatement;
                var next = anchor.Parent!.ChildNodes().SkipWhile(node => node != anchor).Skip(1).First();
                Assert.Contains("PostConfigure", next.ToString(), StringComparison.Ordinal);
            }
            await CompileAndExecuteAsync(document);
        }
    }

    [Theory]
    [InlineData("if (true) services.AddAuthentication().AddMicrosoftIdentityWebApp(new object());")]
    [InlineData("System.Func<AuthenticationBuilder> register = () => services.AddAuthentication().AddMicrosoftIdentityWebApp(new object());")]
    [InlineData("var registration = true ? services.AddAuthentication().AddMicrosoftIdentityWebApp(new object()) : null;")]
    [InlineData("Consume(services.AddAuthentication().AddMicrosoftIdentityWebApp(new object())); void Consume(object value) { }")]
    [InlineData("services?.AddAuthentication().AddMicrosoftIdentityWebApp(new object());")]
    public async Task InvocationArguments_RejectUnsupportedExecutionPositions(string registration)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateRegistrationDocument(workspace, "var services = new Services(); " + registration);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ScaffoldAsync(document));
        Assert.Contains("registration statement inside a block", error.Message);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task InvocationArguments_FallbackMatchesExecutableStatementsOnly(bool topLevel, bool existingConfiguration)
    {
        using var workspace = new AdhocWorkspace();
        const string configuration = "builder.Services.PostConfigure<OpenIdConnectOptions>(\"Default\", options => { options.ResponseType = \"code\"; });";
        var statements = $$""""
            var builder = new AuthenticationBuilder(new Services());
            // {{configuration}}
            const string reference = """{{configuration}}""";
            {{(existingConfiguration ? configuration.Replace("\"Default\"", "\"Def ault\"", StringComparison.Ordinal) : "")}}
            if (builder.Services.Configurations != {{(existingConfiguration ? 2 : 1)}})
                throw new System.Exception(reference);
            """";
        var source = topLevel ? statements : $"static class Program {{ public static void Main(string[] args) {{ {statements} }} }}";
        var document = CreateRegistrationDocument(workspace, source);
        for (int run = 0; run < 2; run++)
        {
            var snippet = new CodeSnippet
            {
                Parent = "AddMicrosoftIdentityWebApp",
                InvocationArguments = new() { ["openIdConnectScheme"] = "\"Default\"" },
                Block = configuration.Replace("\"Default\"", "$openIdConnectScheme$", StringComparison.Ordinal),
                CheckBlock = configuration,
                InsertBefore = ["if (builder.Services.Configurations"]
            };
            var file = new CodeFile
            {
                FileName = "Program.cs",
                Methods = new() { ["Global"] = new Method { CodeChanges = [snippet] } }
            };
            document = await new DocumentBuilder(document, file, topLevel ? [Constants.UseTopLevelStatements] : [], NullLogger.Instance).RunAsync();
            document = document.WithText(SourceText.From((await document.GetTextAsync()).ToString()));
            await CompileAndExecuteAsync(document);
        }
    }

    [Fact]
    public async Task InvocationArguments_WhitespaceInSchemeLiteralsIsSignificantOnRerun()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateRegistrationDocument(workspace, """
            var services = new Services();
            services.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: "CustomOidc");
            services.AddAuthentication().AddMicrosoftIdentityWebApp(new object(), openIdConnectScheme: "Custom Oidc");
            if (services.Configurations != 2) throw new System.Exception("Missing distinct scheme configuration.");
            """);
        for (int run = 0; run < 2; run++)
        {
            document = await ScaffoldAsync(document);
            document = document.WithText(SourceText.From((await document.GetTextAsync()).ToString()));
            await CompileAndExecuteAsync(document);
            var configurations = (await document.GetSyntaxRootAsync())!.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure").ToArray();
            Assert.Equal(["CustomOidc", "Custom Oidc"], configurations.Select(invocation =>
                ((LiteralExpressionSyntax)invocation.ArgumentList.Arguments[0].Expression).Token.ValueText).ToArray());
        }
    }

    private static async Task CompileAndExecuteAsync(Document document)
    {
        var compilation = await document.Project.GetCompilationAsync();
        using var assembly = new MemoryStream();
        var emitted = compilation!.Emit(assembly);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        System.Reflection.Assembly.Load(assembly.ToArray()).EntryPoint!.Invoke(null, [Array.Empty<string>()]);
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
    [InlineData("string scheme = \"First\";", "scheme", ".KeepChain(((scheme, _) = (NextScheme(), \"\")).Item1)")]
    [InlineData("", "scheme", ".KeepChain(scheme = \"Changed\")", true)]
    [InlineData("", "scheme", ".KeepChain(((scheme, _) = (NextScheme(), \"\")).Item1)", true)]
    public async Task InvocationArguments_RejectSchemeValuesThatCannotBeSafelyReused(
        string declarations, string scheme, string suffix, bool parameter = false)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateRegistrationDocument(workspace, $$"""
            Registration.Configure(new Services(){{(parameter ? ", \"First\"" : "")}});
            static class Registration
            {
                static string NextScheme() => System.Guid.NewGuid().ToString();
                static string Scheme => NextScheme();
                static void Change(ref string value) => value = NextScheme();
                static void Observe(in string value) { }
                public static void Configure(Services services{{(parameter ? ", string scheme" : "")}})
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
    [InlineData("Microsoft.AspNetCore", "builder.Services", false, ".KeepChain(((builder, _) = (CreateBuilder(), 0)).Item1)")]
    [InlineData("Microsoft.AspNetCore", "builder.Services", true, "", "builder.Configuration.TenantId = \"tenant\";")]
    [InlineData("Microsoft.AspNetCore", "builder.Services", true, "", "builder.Configuration[\"AzureAd:TenantId\"] = \"tenant\";")]
    public async Task InvocationArguments_ReuseOnlyFrameworkServicesOnStableBuilder(
        string assemblyName, string receiver, bool safe, string suffix = "", string mutation = "")
    {
        using var workspace = new AdhocWorkspace();
        var frameworkSource = """
            namespace Microsoft.AspNetCore.Builder
            {
                public class WebApplicationBuilder
                {
                    public Services Services { get; } = new Services();
                    public Configuration Configuration { get; } = new Configuration();
                }
            }
            public class Configuration
            {
                public string TenantId { get; set; }
                public string this[string key] { get => TenantId; set => TenantId = value; }
                public void Bind(object options) { }
            }
            public class Services
            {
                public AuthenticationBuilder AddAuthentication() => new AuthenticationBuilder();
                public void PostConfigure<T>(string scheme, System.Action<T> configure) { }
            }
            public class AuthenticationBuilder
            {
                public AuthenticationBuilder AddMicrosoftIdentityWebApp(object configuration, string openIdConnectScheme = "Default") => this;
                public AuthenticationBuilder AddMicrosoftIdentityWebApp(System.Action<object> configure, string openIdConnectScheme = "Default") => this;
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
            {{mutation}}
            {{receiver}}.AddAuthentication().AddMicrosoftIdentityWebApp({{(mutation.Length > 0 ? "options => builder.Configuration.Bind(options)" : "new object()")}}, openIdConnectScheme: scheme){{suffix}};
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
                public int Configurations;
                public AuthenticationBuilder AddAuthentication() => new AuthenticationBuilder(this);
                public void PostConfigure<T>(string scheme, System.Action<T> configure) { Configurations++; }
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
    public void InvocationArguments_BindNamedAndPositionalOverloadsPerRegistration()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            class Registration
            {
                public Registration Services => this;
                Registration AddIdentity(object configuration, string scheme = "Default") => this;
                Registration AddIdentity(object configuration, object cookies, string scheme = "Default") => this;
                static void Configure()
                {
                    var authentication = new Registration();
                    authentication.AddIdentity(new object());
                    authentication.AddIdentity(new object(), "Positional");
                    authentication.AddIdentity(new object(), new object(), "OtherOverload");
                    authentication.AddIdentity(new object(), scheme: "Named");
                    authentication.AddIdentity(new object(), scheme: "Named");
                }
            }
            """);
        var compilation = CSharpCompilation.Create("Test", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var snippet = CreateSnippet();
        snippet.Parent = "AddIdentity";
        snippet.InvocationArguments = new() { ["scheme"] = "\"Default\"" };
        snippet.Block = "builder.Services.Configure($scheme$);";

        var scoped = new System.Collections.Generic.Dictionary<SyntaxNode, System.Collections.Generic.List<CodeSnippet>>();
        var expanded = DocumentBuilder.ExpandInvocationArguments([snippet], tree.GetRoot(), compilation.GetSemanticModel(tree), scoped);

        Assert.Empty(expanded);
        var changes = scoped.Values.Select(changes => Assert.Single(changes)).ToArray();
        Assert.Equal(["Default", "Positional", "OtherOverload", "Named", "Named"],
            changes.Select(change => ((LiteralExpressionSyntax)((InvocationExpressionSyntax)((ExpressionStatementSyntax)
                SyntaxFactory.ParseStatement(change.Block)).Expression).ArgumentList.Arguments[0].Expression).Token.ValueText).ToArray());
        Assert.All(changes, change => Assert.StartsWith("authentication.Services.Configure(", change.Block, StringComparison.Ordinal));
        changes[0].LeadingTrivia.NumberOfSpaces = 8;
        Assert.Equal(0, changes[1].LeadingTrivia.NumberOfSpaces);
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
