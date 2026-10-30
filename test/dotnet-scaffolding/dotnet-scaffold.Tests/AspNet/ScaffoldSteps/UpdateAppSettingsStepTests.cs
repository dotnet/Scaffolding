// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings.Tests;

public class UpdateAppSettingsStepTests
{
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
