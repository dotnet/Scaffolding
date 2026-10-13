// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

    [Fact]
    public void DevelopmentSettingsPreflight_RejectsExistingClientIdBeforeNewRegistration()
    {
        string developmentSettingsPath = Path.Combine(
            Path.GetDirectoryName(_testProjectPath)!,
            "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "ClientId": "existing-client-id"
              }
            }
            """);
        var step = CreateValidStep();

        bool result = step.ValidateDevelopmentSettingsPreflight(
            _testProjectPath,
            useExistingApplication: false,
            overwrite: false);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnDevelopmentSettingsConflictBeforeModelInitialization()
    {
        string developmentSettingsPath = Path.Combine(
            Path.GetDirectoryName(_testProjectPath)!,
            "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "ClientId": "existing-client-id"
              }
            }
            """);
        ValidateEntraIdStep step = CreateValidStep();

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.False(_context.Properties.ContainsKey("EntraIdModel"));
        Assert.False(_context.Properties.ContainsKey("EntraIdSettings"));
    }

    [Fact]
    public void DevelopmentSettingsPreflight_AllowsConflictsWithOverwrite()
    {
        string developmentSettingsPath = Path.Combine(
            Path.GetDirectoryName(_testProjectPath)!,
            "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.us/",
                "CallbackPath": "/custom-signin",
                "ClientId": "existing-client-id"
              }
            }
            """);
        var step = CreateValidStep();

        bool result = step.ValidateDevelopmentSettingsPreflight(
            _testProjectPath,
            useExistingApplication: false,
            overwrite: true);

        Assert.True(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void DevelopmentSettingsPreflight_AllowsMatchingExistingRegistrationValues()
    {
        const string applicationId = "existing-client-id";
        string developmentSettingsPath = Path.Combine(
            Path.GetDirectoryName(_testProjectPath)!,
            "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns(
            $$"""
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "test-tenant-id",
                "Domain": "example.com",
                "ClientId": "{{applicationId}}",
                "CallbackPath": "/signin-oidc"
              }
            }
            """);
        ValidateEntraIdStep step = CreateValidStep();
        step.Application = applicationId;
        step.UseExistingApplication = true;

        bool result = step.ValidateDevelopmentSettingsPreflight(
            _testProjectPath,
            useExistingApplication: true,
            overwrite: false);

        Assert.True(result);
    }

    [Fact]
    public void DevelopmentSettingsPreflight_RejectsMalformedJsonEvenWithOverwrite()
    {
        string developmentSettingsPath = Path.Combine(
            Path.GetDirectoryName(_testProjectPath)!,
            "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns("{ malformed json");
        var step = CreateValidStep();

        bool result = step.ValidateDevelopmentSettingsPreflight(
            _testProjectPath,
            useExistingApplication: false,
            overwrite: true);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Overwrite_DefaultsToFalse()
    {
        var step = new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService);

        Assert.False(step.Overwrite);
    }

    private ValidateEntraIdStep CreateValidStep()
    {
        return new ValidateEntraIdStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id"
        };
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
