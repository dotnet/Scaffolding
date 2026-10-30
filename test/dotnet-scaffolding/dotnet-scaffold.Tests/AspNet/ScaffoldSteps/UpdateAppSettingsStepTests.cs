// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings.Tests;

public class UpdateAppSettingsStepTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_UpdatesConfigurationKeysCaseInsensitively(bool primarySettings)
    {
        var fileSystem = CreateFileSystem();
        var path = Path.Combine("project", primarySettings ? "appsettings.json" : "appsettings.Development.json");
        fileSystem.Setup(fs => fs.ReadAllText(path))
            .Returns("""{"azuread":{"clientid":"old-app","TENANTID":"old-tenant","clientcredentials":[{"SourceType":"StoreWithDistinguishedName","CertificateDistinguishedName":"CN=Dev"}]}}""");
        string? written = null;
        fileSystem.Setup(fs => fs.WriteAllText(path, It.IsAny<string>()))
            .Callback((string _, string json) => written = json);
        var step = CreateStep(fileSystem);

        Assert.True(await step.ExecuteAsync(CreateContext(existingApp: true)));

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Assert.IsType<string>(written)));
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonStream(stream).Build();
        Assert.Equal("app-id", configuration["AzureAd:ClientId"]);
        Assert.Equal("tenant-a", configuration["AzureAd:TenantId"]);
        Assert.Equal("CN=Dev", configuration["AzureAd:ClientCredentials:0:CertificateDistinguishedName"]);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesDevelopmentOnlyCredentialsAndUnrelatedProperties()
    {
        var fileSystem = CreateFileSystem();
        var developmentPath = Path.Combine("project", "appsettings.Development.json");
        const string original = """
            {"AzureAd":{"ClientId":"old-app","Domain":"development.contoso.com",
            "ClientCredentials":[{"SourceType":"StoreWithDistinguishedName","CertificateDistinguishedName":"CN=Dev"}],
            "SignUpSignInPolicyId":"B2C_1_dev"},"Keep":"development"}
            """;
        fileSystem.Setup(fs => fs.ReadAllText(developmentPath)).Returns(original);
        fileSystem.Setup(fs => fs.ReadAllText(Path.Combine("project", "appsettings.json")))
            .Returns("""{"AzureAd":{"ClientCredentials":[{"SourceType":"ClientSecret","ClientSecret":"primary-only"}],"SignUpSignInPolicyId":"B2C_1_primary"}}""");
        string? written = null;
        fileSystem.Setup(fs => fs.WriteAllText(developmentPath, It.IsAny<string>()))
            .Callback((string _, string json) => written = json);
        var step = CreateStep(fileSystem);
        var context = CreateContext(existingApp: true);

        Assert.True(await step.ExecuteAsync(context));

        var result = JsonNode.Parse(Assert.IsType<string>(written))!;
        var originalNode = JsonNode.Parse(original)!;
        Assert.True(JsonNode.DeepEquals(originalNode["AzureAd"]!["ClientCredentials"], result["AzureAd"]!["ClientCredentials"]));
        Assert.Equal("development.contoso.com", result["AzureAd"]!["Domain"]!.GetValue<string>());
        Assert.Equal("B2C_1_dev", result["AzureAd"]!["SignUpSignInPolicyId"]!.GetValue<string>());
        Assert.Equal("development", result["Keep"]!.GetValue<string>());
        Assert.Equal("app-id", result["AzureAd"]!["ClientId"]!.GetValue<string>());
        Assert.Equal("tenant-a", result["AzureAd"]!["TenantId"]!.GetValue<string>());
        Assert.Empty(step.CleanupCommands);
    }

    [Theory]
    [InlineData(false, "{/* supported comment */\"AzureAd\":{}}")]
    [InlineData(false, "{\"AzureAd\":{\"ClientId\":\"old-app\",},}")]
    [InlineData(true, "{/* supported comment */\"AzureAd\":{}}")]
    [InlineData(true, "{\"AzureAd\":{\"ClientId\":\"old-app\",},}")]
    public async Task ExecuteAsync_AcceptsConfigurationJsonFormats(bool primarySettings, string json)
    {
        var fileSystem = CreateFileSystem();
        var path = Path.Combine("project", primarySettings ? "appsettings.json" : "appsettings.Development.json");
        fileSystem.Setup(fs => fs.ReadAllText(path)).Returns(json);
        var step = CreateStep(fileSystem);

        Assert.True(await step.ExecuteAsync(CreateContext(existingApp: false)));

        Assert.Empty(step.CleanupCommands);
        fileSystem.Verify(fs => fs.WriteAllText(path,
            It.Is<string>(content => content.Contains("app-id") && content.Contains("tenant-a"))), Times.Once);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ExecuteAsync_OnWriteFailure_CleansUpOnlyCurrentRunApplication(bool existingApp, bool developmentSettings)
    {
        var fileSystem = CreateFileSystem();
        var failingPath = Path.Combine("project", developmentSettings ? "appsettings.Development.json" : "appsettings.json");
        fileSystem.Setup(fs => fs.WriteAllText(failingPath, It.IsAny<string>()))
            .Throws(new IOException("Write failed."));
        var step = CreateStep(fileSystem);
        var context = CreateContext(existingApp);

        Assert.False(await step.ExecuteAsync(context));

        if (existingApp)
        {
            Assert.Empty(step.CleanupCommands);
            Assert.Equal("app-id", context.Properties["ClientId"]);
        }
        else
        {
            Assert.Equal(new[] { "--unregister-app", "--client-id", "app-id", "--tenant-id", "tenant-a",
            "--username", "user@example.com" }, Assert.Single(step.CleanupCommands));
            Assert.Empty(context.Properties);
        }
    }

    [Fact]
    public async Task ExecuteAsync_InvalidDevelopmentSettings_FailsWithoutOverwritingThem()
    {
        var fileSystem = CreateFileSystem();
        var developmentPath = Path.Combine("project", "appsettings.Development.json");
        fileSystem.Setup(fs => fs.ReadAllText(developmentPath)).Returns("invalid JSON");
        var step = CreateStep(fileSystem);

        Assert.False(await step.ExecuteAsync(CreateContext(false)));

        Assert.Single(step.CleanupCommands);
        fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_PartialDevelopmentWriteFailure_RestoresOriginalSettings(bool missingPrimary)
    {
        var primary = Path.Combine("project", "appsettings.json");
        var development = Path.Combine("project", "appsettings.Development.json");
        var originalPrimary = """{"Keep":"primary","AzureAd":{"ClientId":"previous-app"}}""";
        var originalDevelopment = """{"Keep":"development"}""";
        var files = new Dictionary<string, string> { [development] = originalDevelopment };
        if (!missingPrimary)
        {
            files[primary] = originalPrimary;
        }
        var fileSystem = CreateFileSystem();
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns((string path) => files.ContainsKey(path));
        fileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
            .Returns((string path) => files[path]);
        fileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
            .Callback((string path) => files.Remove(path));
        var failed = false;
        fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string path, string content) =>
            {
                if (path == development && !failed)
                {
                    failed = true;
                    files[path] = "partially written";
                    throw new IOException("Development write failed after truncation.");
                }
                files[path] = content;
            });
        var step = CreateStep(fileSystem);

        Assert.False(await step.ExecuteAsync(CreateContext(false)));

        Assert.True(failed);
        Assert.Equal(originalDevelopment, files[development]);
        if (missingPrimary)
        {
            Assert.False(files.ContainsKey(primary));
        }
        else
        {
            Assert.Equal(originalPrimary, files[primary]);
        }
        Assert.Single(step.CleanupCommands);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessPersistsBothSettingsFilesWithoutCleanup()
    {
        var fileSystem = CreateFileSystem();
        var step = CreateStep(fileSystem);
        var context = CreateContext(false);

        Assert.True(await step.ExecuteAsync(context));

        Assert.Empty(step.CleanupCommands);
        Assert.Equal("app-id", context.Properties["ClientId"]);
        foreach (var name in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            fileSystem.Verify(fs => fs.WriteAllText(Path.Combine("project", name),
                It.Is<string>(json => json.Contains("app-id") && json.Contains("tenant-a"))), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_MissingSettings_CreatesFileInProjectDirectory()
    {
        var fileSystem = CreateFileSystem();
        fileSystem.Setup(fs => fs.EnumerateFiles("project", "appsettings.json", SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());
        var step = CreateStep(fileSystem);

        Assert.True(await step.ExecuteAsync(CreateContext(false)));

        fileSystem.Verify(fs => fs.WriteAllText(Path.Combine("project", "appsettings.json"), It.IsAny<string>()), Times.Once);
    }

    private static Mock<IFileSystem> CreateFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.DirectoryExists("project")).Returns(true);
        fileSystem.Setup(fs => fs.EnumerateFiles("project", "appsettings.json", SearchOption.AllDirectories))
            .Returns(new[] { Path.Combine("project", "appsettings.json") });
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("{}");
        return fileSystem;
    }

    private static TestStep CreateStep(Mock<IFileSystem> fileSystem) => new(fileSystem.Object)
    {
        ProjectPath = Path.Combine("project", "Test.csproj"),
        ClientId = "app-id",
        TenantId = "tenant-a",
        Username = "user@example.com"
    };

    private static ScaffolderContext CreateContext(bool existingApp)
    {
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties["ClientId"] = "app-id";
        if (!existingApp)
        {
            context.Properties[EntraRegistrationCleanup.CreatedClientIdKey] = "app-id";
            context.Properties["ClientSecret"] = "development-secret";
        }
        return context;
    }

    private sealed class TestStep(IFileSystem fileSystem) : UpdateAppSettingsStep(
        NullLogger<UpdateAppSettingsStep>.Instance, fileSystem, Mock.Of<ITelemetryService>())
    {
        public List<string[]> CleanupCommands { get; } = [];

        protected override void CleanupRegistration(ScaffolderContext context)
            => EntraRegistrationCleanup.TryCleanup(context, TenantId, Username, NullLogger.Instance, args =>
            {
                CleanupCommands.Add(args);
                return (0, null);
            });
    }
    private readonly Mock<ILogger<UpdateAppSettingsStep>> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly ScaffolderContext _context;
    private readonly UpdateAppSettingsStep _step;

    /// <summary>
    /// Helper method to create a mock ScaffolderContext for testing.
    /// </summary>
    private ScaffolderContext CreateMockContext()
    {
        var mockScaffolder = new Mock<IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("Test");
        mockScaffolder.Setup(s => s.Name).Returns("test");
        return new ScaffolderContext(mockScaffolder.Object);
    }

    public UpdateAppSettingsStepTests()
    {
        _mockLogger = new Mock<ILogger<UpdateAppSettingsStep>>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories))
            .Returns(new List<string>());

        Mock<IScaffolder> scaffolder = new Mock<IScaffolder>();
        scaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        scaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(scaffolder.Object);

        _step = new UpdateAppSettingsStep(_mockLogger.Object, _mockFileSystem.Object, _mockTelemetryService.Object)
        {
            ProjectPath = Path.Combine(Path.GetTempPath(), "UpdateAppSettingsStepTests", "TestApp.csproj"),
            ClientId = "test-client-id",
            Instance = "https://login.microsoftonline.com/",
            CallbackPath = "/signin-oidc"
        };
    }

    [Fact]
    public async Task ExecuteAsync_WritesAppSettingsWithTrailingNewLine()
    {
        string baseProjectPath = Path.Combine("test", "project");
        string projectPath = Path.Combine(baseProjectPath, "TestProject.csproj");
        string generatedAppSettingsPath = Path.Combine(baseProjectPath, "appsettings.json");

        _mockFileSystem.Setup(fs => fs.DirectoryExists(baseProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string>());

        string? appSettingsContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(generatedAppSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => appSettingsContent = content);

        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(baseProjectPath, "appsettings.Development.json"))).Returns(false);

        UpdateAppSettingsStep step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            _mockTelemetryService.Object)
        {
            ProjectPath = projectPath,
            Username = "testuser",
            ClientId = "client-id",
            TenantId = "tenant-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(appSettingsContent);
        Assert.EndsWith(Environment.NewLine, appSettingsContent);

        using JsonDocument document = JsonDocument.Parse(appSettingsContent);
        Assert.True(document.RootElement.TryGetProperty("AzureAd", out JsonElement azureAd));
        Assert.Equal("client-id", azureAd.GetProperty("ClientId").GetString());

        _mockFileSystem.Verify(fs => fs.WriteAllText(generatedAppSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesMissingTrailingNewLine()
    {
        string baseProjectPath = Path.Combine("test", "project");
        string projectPath = Path.Combine(baseProjectPath, "TestProject.csproj");
        string appSettingsPath = Path.Combine(baseProjectPath, "appsettings.json");
        string developmentSettingsPath = Path.Combine(baseProjectPath, "appsettings.Development.json");

        _mockFileSystem.Setup(fs => fs.DirectoryExists(baseProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { appSettingsPath });
        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath)).Returns("{}");

        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns("{}");

        string? developmentContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(developmentSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => developmentContent = content);

        UpdateAppSettingsStep step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            _mockTelemetryService.Object)
        {
            ProjectPath = projectPath,
            Username = "testuser",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(developmentContent);
        Assert.False(developmentContent.EndsWith(Environment.NewLine, StringComparison.Ordinal));

        using JsonDocument document = JsonDocument.Parse(developmentContent);
        Assert.True(document.RootElement.TryGetProperty("AzureAd", out _));

        _mockFileSystem.Verify(fs => fs.WriteAllText(developmentSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789012")]
    [InlineData("contoso.onmicrosoft.com")]
    [InlineData("contoso.com")]
    public async Task ExecuteAsync_DoesNotGenerateDomain(string tenantId)
    {
        _step.TenantId = tenantId;
        _step.Username = "user@contoso.onmicrosoft.com";

        JsonObject azureAd = await ExecuteAndReadAzureAdAsync();

        Assert.False(azureAd.ContainsKey("Domain"));
        Assert.Equal(tenantId, azureAd["TenantId"]?.GetValue<string>());
    }

    [Fact]
    public async Task ExecuteAsync_Issue3849_Reproduction_OmitsDomain()
    {
        _step.Username = "user@contoso.onmicrosoft.com";
        _step.TenantId = "contoso.onmicrosoft.com";

        JsonObject azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject());
        Assert.False(azureAd.ContainsKey("Domain"));
        Assert.Equal("contoso.onmicrosoft.com", azureAd["TenantId"]?.GetValue<string>());
    }

    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789012", "contoso.onmicrosoft.com")]
    [InlineData("contoso.onmicrosoft.com", "fabrikam.onmicrosoft.com")]
    [InlineData("12345678-1234-1234-1234-123456789012", "login.contoso.com")]
    [InlineData("contoso.onmicrosoft.com", "login.contoso.com")]
    [InlineData(null, "contoso.onmicrosoft.com")]
    [InlineData("12345678-1234-1234-1234-123456789012", "another@contoso.onmicrosoft.com.onmicrosoft.com")]
    public async Task ExecuteAsync_PreservesExistingDomain(string? tenantId, string domain)
    {
        _step.Username = "user@contoso.onmicrosoft.com";
        _step.TenantId = tenantId;

        JsonObject azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
        {
            ["Domain"] = domain,
            ["SignUpSignInPolicyId"] = "B2C_1_signupsignin"
        });

        Assert.Equal(domain, azureAd["Domain"]?.GetValue<string>());
        Assert.Equal("B2C_1_signupsignin", azureAd["SignUpSignInPolicyId"]?.GetValue<string>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("contoso")]
    public async Task ExecuteAsync_PreservesExistingDomainWithoutEmailUsername(string? username)
    {
        _step.Username = username;
        _step.TenantId = "12345678-1234-1234-1234-123456789012";

        JsonObject azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
        {
            ["Domain"] = "contoso.onmicrosoft.com"
        });

        Assert.Equal("contoso.onmicrosoft.com", azureAd["Domain"]?.GetValue<string>());
    }

    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789012", null)]
    [InlineData("contoso.onmicrosoft.com", null)]
    [InlineData("contoso.onmicrosoft.com", "")]
    [InlineData(null, null)]
    public async Task ExecuteAsync_RemovesLegacyUsernameDerivedDomain(string? tenantId, string? explicitDomain)
    {
        _step.Username = "user@contoso.onmicrosoft.com";
        _step.TenantId = tenantId;
        _step.Domain = explicitDomain;

        JsonObject azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
        {
            ["Domain"] = "user@contoso.onmicrosoft.com.onmicrosoft.com"
        });

        Assert.False(azureAd.ContainsKey("Domain"));
    }

    [Theory]
    [InlineData(null, "contoso.onmicrosoft.com")]
    [InlineData("contoso.onmicrosoft.com", "contoso.onmicrosoft.com")]
    [InlineData("fabrikam.onmicrosoft.com", "contoso.onmicrosoft.com")]
    [InlineData("user@contoso.onmicrosoft.com.onmicrosoft.com", "contoso.onmicrosoft.com")]
    [InlineData("user@contoso.onmicrosoft.com.onmicrosoft.com", "user@contoso.onmicrosoft.com.onmicrosoft.com")]
    public async Task ExecuteAsync_HonorsExplicitDomain(string? existingDomain, string explicitDomain)
    {
        _step.Username = "user@contoso.onmicrosoft.com";
        _step.TenantId = "12345678-1234-1234-1234-123456789012";
        _step.Domain = explicitDomain;

        JsonObject azureAd = await ExecuteAndReadAzureAdAsync(new JsonObject
        {
            ["Domain"] = existingDomain
        });

        Assert.Equal(explicitDomain, azureAd["Domain"]?.GetValue<string>());
    }

    private async Task<JsonObject> ExecuteAndReadAzureAdAsync(JsonObject? existingAzureAd = null)
    {
        if (existingAzureAd is not null)
        {
            string appSettingsPath = Path.Combine(Path.GetTempPath(), "UpdateAppSettingsStepTests", "appsettings.json");
            JsonObject initialContent = new JsonObject { ["AzureAd"] = existingAzureAd };
            _mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>(), "appsettings.json", SearchOption.AllDirectories))
                .Returns(new[] { appSettingsPath });
            _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath)).Returns(initialContent.ToJsonString());
        }

        string? capturedContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, content) => capturedContent = content);

        bool result = await _step.ExecuteAsync(_context);
        Assert.True(result);
        Assert.NotNull(capturedContent);
        JsonObject azureAd = Assert.IsType<JsonObject>(JsonNode.Parse(capturedContent)?["AzureAd"]);
        Assert.Equal(_step.ClientId, azureAd["ClientId"]?.GetValue<string>());
        Assert.Equal(_step.Instance, azureAd["Instance"]?.GetValue<string>());
        Assert.Equal(_step.CallbackPath, azureAd["CallbackPath"]?.GetValue<string>());
        return azureAd;
    }

    [Theory]
    [InlineData("{\n  // Preserve this comment\n  \"FeatureEnabled\": true\n}")]
    [InlineData("{\n  \"FeatureEnabled\": true,\n}")]
    public async Task ExecuteAsync_DoesNotOverwriteDevelopmentSettingsWhenItContainsUnsupportedJson(string developmentSettingsContent)
    {
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string baseProjectPath = Path.GetDirectoryName(projectPath)!;
        string appSettingsPath = Path.Combine(baseProjectPath, "appsettings.json");
        string developmentSettingsPath = Path.Combine(baseProjectPath, "appsettings.Development.json");

        _mockFileSystem.Setup(fs => fs.DirectoryExists(baseProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { appSettingsPath });
        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath)).Returns("{}");
        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns(developmentSettingsContent);

        string? writtenContent = null;

        _mockFileSystem
            .Setup(fs => fs.WriteAllText(
                developmentSettingsPath,
                It.IsAny<string>()))
            .Callback<string, string>((_, content) =>
            {
                writtenContent = content;
            });
        UpdateAppSettingsStep step = new UpdateAppSettingsStep(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockTelemetryService.Object)
        {
            ProjectPath = projectPath,
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        if (writtenContent is not null)
        {
            Assert.Equal(developmentSettingsContent, writtenContent);
        }
        _mockFileSystem.Verify(fs => fs.WriteAllText(developmentSettingsPath, It.IsAny<string>()), Times.Never);

        Assert.Contains(_mockLogger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments[0] is LogLevel.Warning &&
            invocation.Arguments[2]?.ToString()?.Contains(
                "Failed to parse appsettings.Development.json") == true);
    }
    [Fact]
    public async Task ExecuteAsync_LogsWarningWhenDevelopmentJsonInvalid()
    {
        string baseProjectPath = Path.Combine("test", "project");
        string projectPath = Path.Combine(baseProjectPath, "TestProject.csproj");
        string appSettingsPath = Path.Combine(baseProjectPath, "appsettings.json");
        string developmentSettingsPath = Path.Combine(baseProjectPath, "appsettings.Development.json");

        _mockFileSystem.Setup(fs => fs.DirectoryExists(baseProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { appSettingsPath });
        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath)).Returns("{}");
        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath))
            .Returns("{ invalid json }");

        var mockLogger = new Mock<ILogger<UpdateAppSettingsStep>>();

        UpdateAppSettingsStep step = new UpdateAppSettingsStep(
            mockLogger.Object,
            _mockFileSystem.Object,
            _mockTelemetryService.Object)
        {
            ProjectPath = projectPath,
            Username = "testuser",
            ClientId = "client-id"
        };
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);

        mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<object>(o => o.ToString()!.Contains("Failed to parse")),
            It.IsAny<Exception>(),
            It.IsAny<Func<object, Exception?, string>>()), Times.Once);
    }
}
