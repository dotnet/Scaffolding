// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Shared base class for Entra ID integration tests across .NET versions (net10+).
/// </summary>
[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "entra-id")]
public abstract class EntraIdIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _testProjectDir;
    protected readonly string _testProjectPath;
    protected readonly Mock<IFileSystem> _mockFileSystem;
    protected readonly TestTelemetryService _testTelemetryService;
    protected readonly Mock<IScaffolder> _mockScaffolder;
    protected readonly ScaffolderContext _context;

    protected EntraIdIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);
        File.WriteAllText(Path.Combine(_testDirectory, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_testDirectory, "Directory.Build.targets"), "<Project />");
        File.WriteAllText(Path.Combine(_testDirectory, "Directory.Packages.props"), "<Project />");
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.GetTestNuGetConfig(TargetFramework));

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.EntraId.DisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.EntraId.Name);
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    protected string ProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";

    #region ValidateEntraIdStep — Validation Logic

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenProjectMissing()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenUsernameMissing()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = null;
        step.TenantId = "tenant-123";

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenTenantIdMissing()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = "user@contoso.com";
        step.TenantId = null;

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenUseExistingTrueButApplicationMissing()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";
        step.UseExistingApplication = true;
        step.Application = null;

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenProjectFileDoesNotExist()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateEntraIdStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public void ValidateEntraIdStep_IsScaffoldStep()
    {
        var step = CreateValidateEntraIdStep();
        Assert.IsAssignableFrom<Scaffolding.Core.Steps.ScaffoldStep>(step);
    }

    [Fact]
    public void ValidateEntraIdStep_HasUsernameProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.Username = "test@example.com";
        Assert.NotNull(step.Username);
    }

    [Fact]
    public void ValidateEntraIdStep_HasProjectProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        Assert.NotNull(step.Project);
    }

    [Fact]
    public void ValidateEntraIdStep_HasTenantIdProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.TenantId = "tenant-123";
        Assert.NotNull(step.TenantId);
    }

    [Fact]
    public void ValidateEntraIdStep_HasApplicationProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.Application = "app-456";
        Assert.NotNull(step.Application);
    }

    [Fact]
    public void ValidateEntraIdStep_HasUseExistingApplicationProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.UseExistingApplication = true;
        Assert.True(step.UseExistingApplication);
    }

    #endregion

    #region Telemetry

    [Fact]
    public async Task ValidateEntraIdStep_TracksTelemetry_OnProjectMissingFailure()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEntraIdStep_TracksTelemetry_OnUsernameMissingFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = null;
        step.TenantId = "tenant-123";

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEntraIdStep_SingleEventPerValidation()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region Code Modification Configs

    [Fact]
    public void BlazorEntraChangesConfig_ExistsForTargetFramework()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorEntraChanges.json");
        Assert.True(File.Exists(configPath),
            $"blazorEntraChanges.json should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorWasmEntraChangesConfig_ExistsForTargetFramework()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorWasmEntraChanges.json");
        Assert.True(File.Exists(configPath),
            $"blazorWasmEntraChanges.json should exist for {TargetFramework}");
    }

    [Fact]
    public async Task BlazorEntraChangesConfig_AddsLoginOrLogoutToServerNavMenu()
    {
        await AssertNavMenuModificationAsync("blazorEntraChanges.json", Path.Combine("Components", "Layout", "NavMenu.razor"));
    }

    [Fact]
    public void BlazorEntraChangesConfig_AddsAntiforgeryMiddlewareOnlyForNet11()
    {
        var configPath = Path.Combine(GetActualTemplatesBasePath(), TargetFramework, "CodeModificationConfigs", "blazorEntraChanges.json");
        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var programConfig = Assert.Single(
            config.RootElement.GetProperty("Files").EnumerateArray(),
            file => file.GetProperty("FileName").GetString() == "Program.cs");
        var codeChanges = programConfig
            .GetProperty("Methods")
            .GetProperty("Global")
            .GetProperty("CodeChanges")
            .EnumerateArray()
            .ToArray();
        var antiforgeryChanges = codeChanges.Where(
            change => change.TryGetProperty("CheckBlock", out var checkBlock) &&
                checkBlock.GetString() == "app.UseAntiforgery()").ToArray();

        if (TargetFramework == "net10.0")
        {
            Assert.Empty(antiforgeryChanges);
            return;
        }

        var antiforgeryChange = Assert.Single(
            antiforgeryChanges);

        Assert.Equal(
            [
                "app.MapStaticAssets()",
                "app.MapRazorComponents<App>()"
            ],
            antiforgeryChange.GetProperty("InsertBefore").EnumerateArray().Select(line => line.GetString()));
        Assert.Equal(
            "app.UseAntiforgery()",
            antiforgeryChange.GetProperty("Block").GetString());

        var antiforgeryIndex = Array.IndexOf(codeChanges, antiforgeryChange);
        var logoutEndpointIndex = Array.FindIndex(
            codeChanges,
            change => change.TryGetProperty("Block", out var block) &&
                block.GetString() == "app.MapGroup(\"/authentication\").MapLoginAndLogout()");

        Assert.True(antiforgeryIndex < logoutEndpointIndex);
    }

    [Fact]
    public void LoginOrLogoutTemplate_EmitsAntiforgeryToken()
    {
        var templatePath = Path.Combine(GetActualTemplatesBasePath(), TargetFramework, "BlazorEntraId", "LoginOrLogout.tt");

        var template = File.ReadAllText(templatePath);
        Assert.Contains("""<form action="authentication/logout" method="post">""", template);
        Assert.Contains("<AntiforgeryToken />", template);
        if (TargetFramework == "net11.0")
        {
            Assert.Contains("https://github.com/AzureAD/microsoft-identity-web/issues/4057", template);
        }
    }

    [Fact]
    public async Task BlazorEntraChangesConfig_GeneratesAntiforgeryMiddlewareOnce()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        var antiforgeryMiddleware = TargetFramework == "net10.0" ? "app.UseAntiforgery();" : string.Empty;
        File.WriteAllText(programPath, $$"""
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            {{antiforgeryMiddleware}}
            app.MapStaticAssets();
            app.MapRazorComponents<App>();

            app.Run();
            """);
        await RestoreTestProjectAsync();
        var step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            _testTelemetryService)
        {
            CodeModifierConfigPath = Path.Combine(
                GetActualTemplatesBasePath(),
                TargetFramework,
                "CodeModificationConfigs",
                "blazorEntraChanges.json"),
            CodeChangeOptions = [],
            ProjectPath = _testProjectPath
        };

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));

        var program = File.ReadAllText(programPath);
        Assert.Equal(1, CountOccurrences(program, "app.UseAntiforgery();"));
        Assert.True(
            program.IndexOf("app.UseAntiforgery();", StringComparison.Ordinal) <
            program.IndexOf("app.MapStaticAssets();", StringComparison.Ordinal));

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(programPath), "app.UseAntiforgery();"));
    }

    [Fact]
    public async Task BlazorEntraChangesConfig_UsesRazorComponentsAsAntiforgeryFallback()
    {
        if (TargetFramework == "net10.0")
        {
            return;
        }

        File.WriteAllText(_testProjectPath, ProjectContent);
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(programPath, """
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapRazorComponents<App>();

            app.Run();
            """);
        await RestoreTestProjectAsync();
        var step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            _testTelemetryService)
        {
            CodeModifierConfigPath = Path.Combine(
                GetActualTemplatesBasePath(),
                TargetFramework,
                "CodeModificationConfigs",
                "blazorEntraChanges.json"),
            CodeChangeOptions = [],
            ProjectPath = _testProjectPath
        };

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));

        var program = File.ReadAllText(programPath);
        Assert.Equal(1, CountOccurrences(program, "app.UseAntiforgery();"));
        Assert.True(
            program.IndexOf("app.UseAntiforgery();", StringComparison.Ordinal) <
            program.IndexOf("app.MapRazorComponents<App>();", StringComparison.Ordinal));

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(programPath), "app.UseAntiforgery();"));
    }

    [Fact]
    public async Task BlazorWasmEntraChangesConfig_AddsLoginOrLogoutToClientNavMenu()
    {
        await AssertNavMenuModificationAsync("blazorWasmEntraChanges.json", Path.Combine("Layout", "NavMenu.razor"));
    }

    [Fact]
    public async Task ServerAndClientNavMenus_GenerateAndReferenceTheirOwnLoginOrLogoutComponents()
    {
        string serverLayoutDirectory = Path.Combine(_testProjectDir, "Components", "Layout");
        string serverNavMenuPath = Path.Combine(serverLayoutDirectory, "NavMenu.razor");
        string clientDirectory = Path.Combine(_testDirectory, "Client");
        string clientLayoutDirectory = Path.Combine(clientDirectory, "Layout");
        string clientNavMenuPath = Path.Combine(clientLayoutDirectory, "NavMenu.razor");
        string clientProjectPath = Path.Combine(clientDirectory, "Client.csproj");
        Directory.CreateDirectory(serverLayoutDirectory);
        Directory.CreateDirectory(clientLayoutDirectory);
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(clientProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), "var builder = WebApplication.CreateBuilder(args);");
        File.WriteAllText(Path.Combine(clientDirectory, "Program.cs"), "var builder = WebAssemblyHostBuilder.CreateDefault(args);");
        File.WriteAllText(serverNavMenuPath, GetTestNavMenuContent());
        File.WriteAllText(clientNavMenuPath, GetTestNavMenuContent());
        EntraIdModel entraIdModel = new()
        {
            ProjectInfo = new ProjectInfo(_testProjectPath),
            BaseOutputPath = _testProjectDir
        };

        var templateProperties = EntraIdHelper.GetTextTemplatingProperties(
            [Path.Combine("BlazorEntraId", "LoginOrLogout.tt")],
            entraIdModel,
            clientProjectPath).ToArray();
        Assert.Equal(2, templateProperties.Length);

        string generatedComponent = TargetFramework == "net10.0"
            ? new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.BlazorEntraId.LoginOrLogout().TransformText()
            : new Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.BlazorEntraId.LoginOrLogout().TransformText();
        foreach (var templateProperty in templateProperties)
        {
            File.WriteAllText(templateProperty.OutputPath, generatedComponent);
        }

        var serverStep = CreateCodeModificationStep("blazorEntraChanges.json", _testProjectPath);
        var clientStep = CreateCodeModificationStep("blazorWasmEntraChanges.json", clientProjectPath);
        Assert.True(await serverStep.ExecuteAsync(_context, CancellationToken.None));
        Assert.True(await clientStep.ExecuteAsync(_context, CancellationToken.None));

        Assert.True(File.Exists(Path.Combine(serverLayoutDirectory, "LoginOrLogout.razor")));
        Assert.True(File.Exists(Path.Combine(clientLayoutDirectory, "LoginOrLogout.razor")));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(serverNavMenuPath), "<LoginOrLogout />"));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(clientNavMenuPath), "<LoginOrLogout />"));

        Assert.True(await serverStep.ExecuteAsync(_context, CancellationToken.None));
        Assert.True(await clientStep.ExecuteAsync(_context, CancellationToken.None));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(serverNavMenuPath), "<LoginOrLogout />"));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(clientNavMenuPath), "<LoginOrLogout />"));
    }

    [Theory]
    [InlineData("Console.WriteLine(\"AddMicrosoftIdentityWebApp\");")]
    [InlineData("// AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"));")]
    [InlineData("Console.WriteLine(nameof(AddMicrosoftIdentityWebApp));")]
    [InlineData("builder.Services.AddMicrosoftIdentityWebApplication();")]
    [InlineData("Console.WriteLine(\"builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\\\"AzureAd\\\"))\");")]
    public async Task BlazorEntraChangesConfig_UnrelatedRegistrationReferenceDoesNotSuppressRegistration(string reference)
    {
        var programContent = BlazorServerProgramContent.Replace(
            "var app = builder.Build();", reference + Environment.NewLine + "var app = builder.Build();", StringComparison.Ordinal);

        var firstRunContent = await ApplyBlazorEntraChangesConfigAsync(programContent);
        var secondRunContent = await ApplyBlazorEntraChangesConfigAsync(firstRunContent);
        var invocations = CSharpSyntaxTree.ParseText(secondRunContent).GetRoot()
            .DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.ValueText == "AddMicrosoftIdentityWebApp");

        Assert.Single(invocations);
        Assert.Contains(reference, secondRunContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("builder.Services.AddRazorComponents()\n    .AddInteractiveWebAssemblyComponents();", "builder.Services.AddRazorComponents()\n    .AddInteractiveWebAssemblyComponents()\n    .AddAuthenticationStateSerialization();")]
    [InlineData("builder.Services.AddRazorComponents()\n    .AddInteractiveServerComponents()\n    .AddInteractiveWebAssemblyComponents();", "builder.Services.AddRazorComponents()\n    .AddInteractiveServerComponents()\n    .AddInteractiveWebAssemblyComponents()\n    .AddAuthenticationStateSerialization();")]
    [InlineData("builder.Services.AddRazorComponents()\n    .AddInteractiveServerComponents();", "builder.Services.AddRazorComponents()\n    .AddInteractiveServerComponents();")]
    public async Task BlazorEntraChangesConfig_AddsAuthenticationStateSerializationForWasmProjects(string programContent, string expectedRegistration)
    {
        var isServerOnly = !programContent.Contains("AddInteractiveWebAssemblyComponents", StringComparison.Ordinal);
        programContent = programContent.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
        expectedRegistration = expectedRegistration.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
        var updatedProgramContent = await ApplyBlazorEntraChangesConfigAsync(programContent);

        Assert.Contains(expectedRegistration, updatedProgramContent, StringComparison.Ordinal);
        if (isServerOnly)
        {
            Assert.DoesNotContain("AddAuthenticationStateSerialization", updatedProgramContent, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task BlazorEntraChangesConfig_DoesNotDuplicateAuthenticationStateSerialization()
    {
        var programContent = "builder.Services.AddRazorComponents()\n    .AddInteractiveWebAssemblyComponents()\n    .AddAuthenticationStateSerialization();"
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);

        var updatedProgramContent = await ApplyBlazorEntraChangesConfigAsync(programContent);

        Assert.Equal(1, updatedProgramContent.Split("AddAuthenticationStateSerialization").Length - 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".AddInteractiveServerComponents()")]
    [InlineData(".AddInteractiveWebAssemblyComponents()")]
    [InlineData(".AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents()")]
    public async Task BlazorEntraChangesConfig_ConfiguresAuthorizationCodeFlow(string renderModeServices)
    {
        var programContent = BlazorServerProgramContent.Replace(
            ".AddInteractiveServerComponents()", renderModeServices, StringComparison.Ordinal);
        var updatedProgramContent = await ApplyBlazorEntraChangesConfigAsync(programContent);

        var normalizedProgramContent = RemoveWhitespace(updatedProgramContent);
        Assert.Contains("usingMicrosoft.IdentityModel.Protocols.OpenIdConnect;", normalizedProgramContent, StringComparison.Ordinal);
        Assert.Contains(
            "builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"));",
            normalizedProgramContent,
            StringComparison.Ordinal);
        Assert.Contains(AuthorizationCodeFlowConfiguration, normalizedProgramContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".AddInteractiveServerComponents()")]
    [InlineData(".AddInteractiveWebAssemblyComponents()")]
    [InlineData(".AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents()")]
    public async Task BlazorEntraChangesConfig_ConfiguresEachRegisteredSchemeAcrossRenderModes(string renderModeServices)
    {
        const string registrations = """
            const string scheme = "CustomOidc";
            builder.Services.AddAuthentication(scheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"), openIdConnectScheme: scheme);
            builder.Services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("OtherAzureAd"), openIdConnectScheme: "OtherOidc", cookieScheme: "OtherCookies");
            """;
        var programContent = BlazorServerProgramContent
            .Replace(".AddInteractiveServerComponents()", renderModeServices, StringComparison.Ordinal)
            .Replace("var app = builder.Build();", registrations + Environment.NewLine + "var app = builder.Build();", StringComparison.Ordinal);

        var firstRunContent = await ApplyBlazorEntraChangesConfigAsync(programContent);
        var secondRunContent = await ApplyBlazorEntraChangesConfigAsync(firstRunContent);
        var normalized = RemoveWhitespace(secondRunContent);

        foreach (var registration in CSharpSyntaxTree.ParseText(registrations).GetRoot()
            .DescendantNodes().OfType<ExpressionStatementSyntax>())
        {
            Assert.Contains(RemoveWhitespace(registration.ToString()), normalized, StringComparison.Ordinal);
        }
        Assert.Equal(2, secondRunContent.Split("AddMicrosoftIdentityWebApp").Length - 1);
        Assert.Equal(2, secondRunContent.Split("PostConfigure<OpenIdConnectOptions>").Length - 1);
        Assert.Contains(AuthorizationCodeFlowConfiguration.Replace("OpenIdConnectDefaults.AuthenticationScheme", "\"CustomOidc\"", StringComparison.Ordinal), normalized, StringComparison.Ordinal);
        Assert.Contains(AuthorizationCodeFlowConfiguration.Replace("OpenIdConnectDefaults.AuthenticationScheme", "\"OtherOidc\"", StringComparison.Ordinal), normalized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlazorEntraChangesConfig_DoesNotDuplicateAuthorizationCodeFlowOnRerun()
    {
        var firstRunContent = await ApplyBlazorEntraChangesConfigAsync(BlazorServerProgramContent);
        var secondRunContent = await ApplyBlazorEntraChangesConfigAsync(firstRunContent);

        Assert.Equal(1, secondRunContent.Split("AddMicrosoftIdentityWebApp").Length - 1);
        Assert.Equal(1, secondRunContent.Split("OpenIdConnectResponseType.Code").Length - 1);
        Assert.Equal(1, secondRunContent.Split("using Microsoft.IdentityModel.Protocols.OpenIdConnect;").Length - 1);
    }

    [Theory]
    [InlineData(".AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"));")]
    [InlineData(".AddMicrosoftIdentityWebApp(\n        builder.Configuration.GetSection(\"AzureAd\"));")]
    [InlineData(".AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"))\n    .EnableTokenAcquisitionToCallDownstreamApi()\n    .AddInMemoryTokenCaches();")]
    [InlineData(".AddMicrosoftIdentityWebApp(options =>\n    {\n        builder.Configuration.Bind(\"AzureAd\", options);\n        options.ResponseType = OpenIdConnectResponseType.IdToken;\n    });")]
    public async Task BlazorEntraChangesConfig_UpgradesExistingRegistrationWithoutRewritingIt(string registration)
    {
        var implicitFlowRegistration = string.Join(Environment.NewLine,
            "builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)",
            "    " + registration,
            string.Empty,
            "var app = builder.Build();");
        var programContent = BlazorServerProgramContent.Replace("var app = builder.Build();", implicitFlowRegistration, StringComparison.Ordinal);

        var updatedProgramContent = await ApplyBlazorEntraChangesConfigAsync(programContent);
        var secondRunContent = await ApplyBlazorEntraChangesConfigAsync(updatedProgramContent);

        Assert.Contains(RemoveWhitespace(registration), RemoveWhitespace(secondRunContent), StringComparison.Ordinal);
        Assert.Equal(1, secondRunContent.Split("AddMicrosoftIdentityWebApp").Length - 1);
        Assert.Equal(1, secondRunContent.Split("PostConfigure<OpenIdConnectOptions>").Length - 1);
        Assert.Contains(AuthorizationCodeFlowConfiguration, RemoveWhitespace(secondRunContent), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("// Reference: OpenIdConnectResponseType.Code is the desired migration.")]
    [InlineData("Console.WriteLine(OpenIdConnectResponseType.Code);")]
    public async Task BlazorEntraChangesConfig_UnrelatedCodeFlowReferenceDoesNotPreventConfiguration(string reference)
    {
        var programContent = string.Join(Environment.NewLine,
            "var builder = WebApplication.CreateBuilder(args);",
            "builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)",
            "    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"));",
            reference,
            "var app = builder.Build();");

        var updatedProgramContent = await ApplyBlazorEntraChangesConfigAsync(programContent);
        var secondRunContent = await ApplyBlazorEntraChangesConfigAsync(updatedProgramContent);

        Assert.Contains(reference, secondRunContent, StringComparison.Ordinal);
        Assert.Equal(1, secondRunContent.Split("PostConfigure<OpenIdConnectOptions>").Length - 1);
        Assert.Contains(AuthorizationCodeFlowConfiguration, RemoveWhitespace(secondRunContent), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("""""
        const string reference = """
            builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.ResponseType = OpenIdConnectResponseType.Code;
            })
            """;
        """"")]
    [InlineData("builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)\n    .AddMicrosoftIdentityWebApp(\n        builder.Configuration.GetSection(\"AzureAd\"));")]
    [InlineData("builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)\n    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"))\n    .EnableTokenAcquisitionToCallDownstreamApi()\n    .AddInMemoryTokenCaches();")]
    [InlineData("builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)\n    .AddMicrosoftIdentityWebApp(options =>\n    {\n        builder.Configuration.Bind(\"AzureAd\", options);\n        options.ResponseType = OpenIdConnectResponseType.IdToken;\n    });")]
    public async Task BlazorEntraChangesConfig_GeneratedAppChallengesWithCodeAndPkce(string registration, string scheme = "OpenIdConnect", string additionalScheme = "")
    {
        var programContent = $$"""
            using Microsoft.AspNetCore.Authentication;
            using Microsoft.AspNetCore.DataProtection;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.WebUtilities;
            using Microsoft.Extensions.Options;
            using Microsoft.IdentityModel.Protocols;
            using Microsoft.IdentityModel.Protocols.OpenIdConnect;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "11111111-2222-3333-4444-555555555555",
                ["AzureAd:ClientId"] = "22222222-2222-3333-4444-555555555555",
                ["AzureAd:ResponseType"] = "id_token",
                ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
                ["AzureAd:ClientCredentials:0:ClientSecret"] = "test-only-credential"
            });
            {{registration}}
            // An unrelated reference must not suppress the authorization code configuration.
            Console.WriteLine(OpenIdConnectResponseType.Code);
            builder.Services.PostConfigureAll<OpenIdConnectOptions>(options =>
            {
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(new OpenIdConnectConfiguration
                {
                    AuthorizationEndpoint = "https://identity.example.test/authorize",
                    TokenEndpoint = "https://identity.example.test/token",
                    Issuer = "https://identity.example.test/"
                });
            });
            builder.Services.AddAuthentication().AddOpenIdConnect("Unrelated", options =>
            {
                options.ClientId = "unrelated-client";
                options.ResponseType = OpenIdConnectResponseType.IdToken;
                options.Configuration = new OpenIdConnectConfiguration
                {
                    AuthorizationEndpoint = "https://identity.example.test/unrelated",
                    Issuer = "https://identity.example.test/"
                };
            });
            var app = builder.Build();
            await using var disposableApp = app;
            using var scope = app.Services.CreateScope();
            var optionsMonitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
            var unrelated = optionsMonitor.Get("Unrelated");
            foreach (var scheme in new[] { "{{scheme}}", "{{additionalScheme}}" }.Where(name => !string.IsNullOrEmpty(name)))
            {
                var options = optionsMonitor.Get(scheme);
                var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
                http.Request.Scheme = "https";
                http.Request.Host = new HostString("localhost", 5001);
                await http.ChallengeAsync(scheme);
                var query = QueryHelpers.ParseQuery(new Uri(http.Response.Headers.Location.ToString()).Query);
                if (http.Response.StatusCode != 302 ||
                    query["response_type"] != "code" ||
                    !query.TryGetValue("code_challenge_method", out var method) || method != "S256" ||
                    !query.TryGetValue("code_challenge", out var challenge) || string.IsNullOrEmpty(challenge) ||
                    !options.UsePkce ||
                    unrelated.ResponseType != OpenIdConnectResponseType.IdToken)
                {
                    Console.Error.WriteLine($"Expected authorization code flow with PKCE/S256 for {scheme} without changing unrelated schemes.");
                    return 1;
                }
            }
            Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            Console.WriteLine("Authorization code flow with PKCE/S256 verified.");
            return 0;
            """;

        var firstRunContent = await ApplyBlazorEntraChangesConfigAsync(programContent);
        var secondRunContent = await ApplyBlazorEntraChangesConfigAsync(firstRunContent);
        Assert.Equal(string.IsNullOrEmpty(additionalScheme) ? 1 : 2,
            CSharpSyntaxTree.ParseText(secondRunContent).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Count(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "PostConfigure"));
        File.WriteAllText(_testProjectPath, $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{{TargetFramework}}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Identity.Web" Version="4.16.1" />
              </ItemGroup>
            </Project>
            """);

        var (exitCode, output, error) = await ScaffoldCliHelper.RunDotNetAsync(
            _testProjectDir, "run", "--project", _testProjectPath, "--framework", TargetFramework);

        Assert.True(exitCode == 0, $"Generated authentication must build and use code flow with PKCE.\nOutput: {output}\nError: {error}");
        Assert.Contains("Authorization code flow with PKCE/S256 verified.", output, StringComparison.Ordinal);
        Assert.Contains(TargetFramework == "net11.0" ? ".NET 11.0." : ".NET 10.0.", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("builder.Services.AddAuthentication(\"CustomOidc\")\n    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"), openIdConnectScheme: \"CustomOidc\");")]
    [InlineData("builder.Services.AddAuthentication(\"CustomOidc\")\n    .AddMicrosoftIdentityWebApp(options =>\n    {\n        builder.Configuration.Bind(\"AzureAd\", options);\n        options.ResponseType = OpenIdConnectResponseType.IdToken;\n    }, openIdConnectScheme: \"CustomOidc\");")]
    [InlineData("const string customScheme = \"CustomOidc\";\nbuilder.Services.AddAuthentication(customScheme)\n    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"), openIdConnectScheme: customScheme);\nbuilder.Services.AddAuthentication()\n    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"), openIdConnectScheme: \"OtherOidc\", cookieScheme: \"OtherCookies\");", "OtherOidc")]
    [InlineData("builder.Services.AddAuthentication(\"CustomOidc\").AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"), openIdConnectScheme: \"CustomOidc\");\nbuilder.Services.AddAuthentication().AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(\"AzureAd\"), openIdConnectScheme: \"Custom Oidc\", cookieScheme: \"OtherCookies\");", "Custom Oidc")]
    public Task BlazorEntraChangesConfig_CustomSchemeChallengesWithCodeAndPkce(string registration, string additionalScheme = "")
        => BlazorEntraChangesConfig_GeneratedAppChallengesWithCodeAndPkce(registration, "CustomOidc", additionalScheme);

    [Fact]
    public Task BlazorEntraChangesConfig_RuntimeSchemeChallengesWithCodeAndPkce()
        => BlazorEntraChangesConfig_GeneratedAppChallengesWithCodeAndPkce("""
            string customScheme = "CustomOidc";
            builder.Services.AddAuthentication(customScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"), openIdConnectScheme: customScheme);
            """, "CustomOidc");

    #endregion

    #region Validation Combination Tests

    [Fact]
    public async Task ValidateEntraIdStep_AllNullInputs_DoNotThrow()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;
        step.Username = null;
        step.TenantId = null;
        step.Application = null;

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    #endregion

    #region Template Root — Expected Scaffolder Folders

    [Theory]
    [InlineData("BlazorCrud")]
    [InlineData("BlazorIdentity")]
    [InlineData("CodeModificationConfigs")]
    [InlineData("EfController")]
    [InlineData("Files")]
    [InlineData("Identity")]
    [InlineData("MinimalApi")]
    [InlineData("RazorPages")]
    [InlineData("Views")]
    public void Templates_HasExpectedScaffolderFolder(string folderName)
    {
        var basePath = GetActualTemplatesBasePath();
        var folderPath = Path.Combine(basePath, TargetFramework, folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for {TargetFramework}");
    }

    #endregion

    #region Helper Methods

    private ValidateEntraIdStep CreateValidateEntraIdStep()
    {
        return new ValidateEntraIdStep(
            _mockFileSystem.Object,
            NullLogger<ValidateEntraIdStep>.Instance,
            _testTelemetryService);
    }

    protected static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    private async Task AssertNavMenuModificationAsync(string configFileName, string navMenuPath)
    {
        var configPath = Path.Combine(GetActualTemplatesBasePath(), TargetFramework, "CodeModificationConfigs", configFileName);
        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var normalizedNavMenuPath = NormalizePathSeparators(navMenuPath);
        var navMenuConfig = Assert.Single(
            config.RootElement.GetProperty("Files").EnumerateArray(),
            file =>
            {
                string? configuredFileName = file.GetProperty("FileName").GetString();
                return configuredFileName is not null &&
                    NormalizePathSeparators(configuredFileName) == normalizedNavMenuPath;
            });
        var replacement = Assert.Single(navMenuConfig.GetProperty("Replacements").EnumerateArray());

        Assert.Equal("<LoginOrLogout />", replacement.GetProperty("CheckBlock").GetString());
        Assert.Equal(["</nav>"], replacement.GetProperty("ReplaceSnippet").EnumerateArray().Select(line => line.GetString()));
        Assert.Equal(["    <LoginOrLogout />", "    </nav>"], replacement.GetProperty("MultiLineBlock").EnumerateArray().Select(line => line.GetString()));

        File.WriteAllText(_testProjectPath, ProjectContent);
        var navMenuOutputPath = Path.Combine(_testProjectDir, normalizedNavMenuPath);
        Directory.CreateDirectory(Path.GetDirectoryName(navMenuOutputPath)!);
        File.WriteAllText(navMenuOutputPath, GetTestNavMenuContent());
        var step = CreateCodeModificationStep(configFileName, _testProjectPath);

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(navMenuOutputPath), "<LoginOrLogout />"));

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(navMenuOutputPath), "<LoginOrLogout />"));
    }

    private WrappedCodeModificationStep CreateCodeModificationStep(string configFileName, string projectPath)
        => new(
            NullLogger<WrappedCodeModificationStep>.Instance,
            _testTelemetryService)
        {
            CodeModifierConfigPath = Path.Combine(
                GetActualTemplatesBasePath(),
                TargetFramework,
                "CodeModificationConfigs",
                configFileName),
            CodeChangeOptions = [],
            ProjectPath = projectPath
        };

    private static string GetTestNavMenuContent() => """
            <div class="top-row ps-3 navbar navbar-dark">
                <a class="navbar-brand" href="">TestProject</a>
            </div>

            <input type="checkbox" title="Navigation menu" class="navbar-toggler" />

            <div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
                <nav class="nav flex-column">
                    <div class="nav-item px-3">
                        <NavLink class="nav-link" href="" Match="NavLinkMatch.All">Home</NavLink>
                    </div>
                </nav>
            </div>
            """;

    private static string NormalizePathSeparators(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    private static int CountOccurrences(string value, string searchValue)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(searchValue, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += searchValue.Length;
        }

        return count;
    }

    private async Task<string> ApplyBlazorEntraChangesConfigAsync(string programContent)
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(programPath, programContent);
        await RestoreTestProjectAsync();
        var configPath = Path.Combine(GetActualTemplatesBasePath(), TargetFramework, "CodeModificationConfigs", "blazorEntraChanges.json");
        var step = new CodeModificationStep(NullLogger<CodeModificationStep>.Instance)
        {
            CodeModifierConfigPath = configPath,
            CodeChangeOptions = [],
            ProjectPath = _testProjectPath
        };

        var result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        return File.ReadAllText(programPath);
    }

    private async Task RestoreTestProjectAsync()
    {
        if (!File.Exists(Path.Combine(_testProjectDir, "obj", "project.assets.json")))
        {
            // MSBuildWorkspace needs restored framework references to bind schemes and service receivers.
            var (exitCode, output, error) = await ScaffoldCliHelper.RunDotNetAsync(
                _testProjectDir, "restore", _testProjectPath);
            Assert.True(exitCode == 0, $"Project restore must succeed before code modification.\nOutput: {output}\nError: {error}");
        }
    }

    private static readonly string BlazorServerProgramContent = string.Join(Environment.NewLine,
        "var builder = WebApplication.CreateBuilder(args);",
        string.Empty,
        "builder.Services.AddRazorComponents()",
        "    .AddInteractiveServerComponents();",
        string.Empty,
        "var app = builder.Build();",
        string.Empty,
        "app.UseAntiforgery();",
        string.Empty,
        "app.MapRazorComponents<App>()",
        "    .AddInteractiveServerRenderMode();",
        string.Empty,
        "app.Run();",
        string.Empty);

    private static string RemoveWhitespace(string value)
        => new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());

    private const string AuthorizationCodeFlowConfiguration =
        "builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme,options=>{options.ResponseType=OpenIdConnectResponseType.Code;});";

    protected Task<(int ExitCode, string Output, string Error)> RunBuildAsync(string workingDirectory)
        => ScaffoldCliHelper.RunBuildForFrameworkAsync(workingDirectory, TargetFramework);

    protected class TestTelemetryService : ITelemetryService
    {
        public List<(string EventName, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measures)> TrackedEvents { get; } = new();
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, double>? measures = null)
        {
            TrackedEvents.Add((eventName, properties ?? new Dictionary<string, string>(), measures ?? new Dictionary<string, double>()));
        }

        public void Flush() { }
    }

    #endregion
}
