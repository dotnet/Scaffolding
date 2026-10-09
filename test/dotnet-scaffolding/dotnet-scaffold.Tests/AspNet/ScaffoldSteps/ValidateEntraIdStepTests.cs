// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class ValidateEntraIdStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger<ValidateEntraIdStep>> _mockLogger;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _testProjectPath;

    public ValidateEntraIdStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockLogger = new Mock<ILogger<ValidateEntraIdStep>>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _testProjectPath = Path.Combine("test", "project", "TestProject.csproj");

        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectIsEmpty()
    {
        // Arrange
        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = string.Empty,
            Username = "test@example.com",
            TenantId = "test-tenant-id",
            UseExistingApplication = false
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectDoesNotExist()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(false);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id",
            UseExistingApplication = false
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenUsernameIsEmpty()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = string.Empty,
            TenantId = "test-tenant-id",
            UseExistingApplication = false
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTenantIdIsEmpty()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = string.Empty,
            UseExistingApplication = false
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenUseExistingApplicationIsTrueButApplicationIdIsEmpty()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id",
            UseExistingApplication = true,
            Application = string.Empty
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenUseExistingApplicationIsTrueButApplicationIdIsNull()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id",
            UseExistingApplication = true,
            Application = null
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenUseExistingApplicationIsFalseAndApplicationIdIsProvided()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id",
            UseExistingApplication = false,
            Application = "some-app-id-12345"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenCreatingNewAppAndDevelopmentAzureAdClientIdExistsWithoutOverwrite()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine("test", "project", "appsettings.Development.json"))).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(Path.Combine("test", "project", "appsettings.Development.json"))).Returns(
            """
            {
              "AzureAd": {
                "ClientId": "existing-client-id"
              }
            }
            """);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id",
            UseExistingApplication = false,
            Overwrite = false
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenCreatingNewAppAndAzureAdSectionIsNotObjectWithoutOverwrite()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine("test", "project", "appsettings.Development.json"))).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(Path.Combine("test", "project", "appsettings.Development.json"))).Returns(
            """
            {
              "AzureAd": "invalid-shape"
            }
            """);

        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id",
            UseExistingApplication = false,
            Overwrite = false
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Theory]
    [InlineData("""{"AzureAd":{"ClientId":"existing-client-id"}}""", false, false, "ClientId", false)]
    [InlineData("""{"AzureAd":{"ClientId":"different-client-id"}}""", true, false, "ClientId", false)]
    [InlineData("""{"AzureAd":{"Instance":"https://other-instance/"}}""", false, false, "Instance", false)]
    [InlineData("""{"AzureAd":{"TenantId":"different-tenant"}}""", false, false, "TenantId", false)]
    [InlineData("""{"AzureAd":{"Domain":"different-domain"}}""", false, false, "Domain", false)]
    [InlineData("""{"AzureAd":{"CallbackPath":"/different-callback"}}""", false, false, "CallbackPath", false)]
    [InlineData("""{"AzureAd":{"TenantId":"different-tenant"}}""", true, false, "TenantId", false)]
    [InlineData("""{"AzureAd":"invalid-shape"}""", true, false, "AzureAd", false)]
    [InlineData("{ invalid json", false, false, null, false)]
    [InlineData("{ invalid json", true, true, null, false)]
    [InlineData("[]", false, true, null, false)]
    [InlineData("""{"AzureAd":{"ClientId":"existing-client-id"}}""", false, false, "ClientId", true)]
    [InlineData("""{"AzureAd":{"ClientId":"different-client-id"}}""", true, false, "ClientId", true)]
    [InlineData("""{"AzureAd":{"TenantId":"different-tenant"}}""", false, false, "TenantId", true)]
    [InlineData("{ invalid json", false, true, null, true)]
    [InlineData("""{"azuread":{"ClientId":"existing-client-id"}}""", false, false, "ClientId", false)]
    [InlineData("""{"AzureAd":{"clientid":"existing-client-id"}}""", false, false, "ClientId", false)]
    [InlineData("""{"AZUREAD":{"CLIENTID":"different-client-id"}}""", true, false, "ClientId", false)]
    [InlineData("""{"azuread":{"instance":"https://other-instance/"}}""", false, false, "Instance", false)]
    [InlineData("""{"azuread":{"tenantid":"different-tenant"}}""", false, false, "TenantId", false)]
    [InlineData("""{"azuread":{"domain":"different-domain"}}""", false, false, "Domain", false)]
    [InlineData("""{"azuread":{"callbackpath":"/different-callback"}}""", false, false, "CallbackPath", false)]
    [InlineData("""{"azuread":"invalid-shape"}""", false, false, "AzureAd", false)]
    [InlineData("""{ /* development settings */ "azuread": { "clientid": "existing-client-id" } }""", false, false, "ClientId", false)]
    [InlineData("""{ "AzureAd": { "ClientId": "existing-client-id", }, }""", false, false, "ClientId", false)]
    public async Task ExecuteAsync_PreflightFailure_StopsRegistrationAndClientSecretSteps(
        string settings, bool useExistingApplication, bool overwrite, string? conflictingKey, bool bareProjectFilename)
    {
        string projectPath = bareProjectFilename ? "TestProject.csproj" : _testProjectPath;
        string projectDirectory = bareProjectFilename ? Directory.GetCurrentDirectory() : Path.Combine("test", "project");
        string devSettingsPath = Path.Combine(projectDirectory, "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.FileExists(projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(devSettingsPath)).Returns(settings);

        var logger = new TestLogger();
        var step = new ValidateEntraIdStep(_mockFileSystem.Object, logger, _testTelemetryService)
        {
            Project = projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            UseExistingApplication = useExistingApplication,
            Application = useExistingApplication ? "Existing application client-id" : null,
            Overwrite = overwrite
        };
        var registration = new Mock<RegisterAppStep>(
            Mock.Of<ILogger<AddClientSecretStep>>(), _mockFileSystem.Object, _testTelemetryService);
        var secret = new Mock<AddClientSecretStep>(
            Mock.Of<ILogger<AddClientSecretStep>>(), _mockFileSystem.Object, Mock.Of<IEnvironmentService>());
        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(ValidateEntraIdStep))).Returns(step);
        services.Setup(s => s.GetService(typeof(RegisterAppStep))).Returns(registration.Object);
        services.Setup(s => s.GetService(typeof(AddClientSecretStep))).Returns(secret.Object);

        IScaffolder scaffolder = new ScaffoldBuilder("entra-preflight-test")
            .WithStep<ValidateEntraIdStep>()
            .WithRegisterAppStep()
            .WithAddClientSecretStep()
            .Build(services.Object);
        var context = new ScaffolderContext(scaffolder);

        await scaffolder.ExecuteAsync(context);

        Assert.Empty(context.Properties);
        Assert.Single(_testTelemetryService.TrackedEvents);
        _mockFileSystem.Verify(fs => fs.ReadAllText(devSettingsPath), Times.Once);
        registration.Verify(s => s.ExecuteAsync(It.IsAny<ScaffolderContext>(), It.IsAny<CancellationToken>()), Times.Never);
        secret.Verify(s => s.ExecuteAsync(It.IsAny<ScaffolderContext>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        if (conflictingKey is not null)
        {
            Assert.Contains(logger.Errors, message => message.Contains($"key '{conflictingKey}'"));
            Assert.DoesNotContain(logger.Errors, message => message.Contains("existing-client-id") || message.Contains("different-"));
        }
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id"
        };

        // Assert
        Assert.NotNull(step);
        Assert.Equal(_testProjectPath, step.Project);
        Assert.Equal("test@example.com", step.Username);
        Assert.Equal("test-tenant-id", step.TenantId);
    }

    [Theory]
    [InlineData(null, false, false, false)]
    [InlineData("{}", false, false, false)]
    [InlineData("""{"AzureAd":{"ClientId":""}}""", false, false, false)]
    [InlineData("""{"AzureAd":{"ClientId":null,"SignedOutCallbackPath":"/custom-signout"}}""", false, false, false)]
    [InlineData("""{"AzureAd":{"ClientId":"old-client","TenantId":"old-tenant"}}""", false, true, false)]
    [InlineData("""{"AzureAd":"invalid-shape"}""", false, true, false)]
    [InlineData("""{"AzureAd":{"Instance":"https://login.microsoftonline.com/","TenantId":"tenant-id","Domain":"devuser.onmicrosoft.com","ClientId":"client-id","CallbackPath":"/signin-oidc","SignedOutCallbackPath":"/custom-signout"}}""", true, false, false)]
    [InlineData("""{"AzureAd":{"ClientId":"old-client"}}""", true, true, false)]
    [InlineData(null, false, false, true)]
    [InlineData("{}", false, false, true)]
    [InlineData("""{"AzureAd":{"ClientId":"old-client"}}""", false, true, true)]
    [InlineData("""{"AzureAd":{"ClientId":"client-id"}}""", true, false, true)]
    [InlineData("""{"azuread":{"clientid":"client-id","tenantid":"tenant-id"}}""", true, false, false)]
    [InlineData("""{"azuread":{"clientid":"old-client"}}""", false, true, false)]
    [InlineData("""{ /* development settings */ "Logging": {} }""", false, false, false)]
    [InlineData("""{ "Logging": {}, }""", false, false, false)]
    [InlineData("""{ /* development settings */ "azuread": { "clientid": "client-id", }, }""", true, false, false)]
    public void DevelopmentSettingsPreflight_AllowsCompatibleSettingsOrExplicitOverwrite(
        string? settings, bool useExistingApplication, bool overwrite, bool bareProjectFilename)
    {
        string projectPath = bareProjectFilename ? "TestProject.csproj" : _testProjectPath;
        string projectDirectory = bareProjectFilename ? Directory.GetCurrentDirectory() : Path.Combine("test", "project");
        string devSettingsPath = Path.Combine(projectDirectory, "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.FileExists(devSettingsPath)).Returns(settings is not null);
        if (settings is not null)
        {
            _mockFileSystem.Setup(fs => fs.ReadAllText(devSettingsPath)).Returns(settings);
        }

        var logger = new TestLogger();
        var step = new ValidateEntraIdStep(_mockFileSystem.Object, logger, _testTelemetryService)
        {
            Username = "devuser",
            TenantId = "tenant-id",
            Application = useExistingApplication ? "Existing application client-id" : null
        };

        Assert.True(step.ValidateDevelopmentSettingsPreflight(projectPath, useExistingApplication, overwrite));
        Assert.Empty(logger.Errors);
        _mockFileSystem.Verify(fs => fs.FileExists(devSettingsPath), Times.Once);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        string expectedProject = _testProjectPath;
        string expectedUsername = "test@example.com";
        string expectedTenantId = "test-tenant-id";
        string expectedApplication = "app-id-12345";
        bool expectedUseExistingApplication = true;

        // Act
        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = expectedProject,
            Username = expectedUsername,
            TenantId = expectedTenantId,
            Application = expectedApplication,
            UseExistingApplication = expectedUseExistingApplication
        };

        // Assert
        Assert.Equal(expectedProject, step.Project);
        Assert.Equal(expectedUsername, step.Username);
        Assert.Equal(expectedTenantId, step.TenantId);
        Assert.Equal(expectedApplication, step.Application);
        Assert.Equal(expectedUseExistingApplication, step.UseExistingApplication);
    }

    [Fact]
    public void UseExistingApplication_DefaultsToFalse()
    {
        // Act
        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService);

        // Assert
        Assert.False(step.UseExistingApplication);
    }

    private sealed class TestLogger : ILogger<ValidateEntraIdStep>
    {
        public List<string> Errors { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                Errors.Add(formatter(state, exception));
            }
        }
    }

    private class TestTelemetryService : ITelemetryService
    {
        public List<(string EventName, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measurements)> TrackedEvents { get; } = new();

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> measurements)
        {
            TrackedEvents.Add((eventName, properties, measurements));
        }

        public void Flush()
        {
        }
    }
}
