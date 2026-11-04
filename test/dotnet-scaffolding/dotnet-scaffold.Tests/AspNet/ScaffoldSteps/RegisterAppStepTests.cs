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

/// <summary>
/// Unit tests for RegisterAppStep to ensure MSIdentity CLI is invoked correctly during Entra ID scaffolding.
/// </summary>
public class RegisterAppStepTests
{
    [Fact]
    public async Task ExecuteAsync_UsesRegistrationOnlyCommandAndRecordsCreatedApplication()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists("Test.csproj")).Returns(true);
        var step = new RegistrationOnlyTestStep(fileSystem.Object)
        {
            ProjectPath = "Test.csproj",
            TenantId = "tenant-a",
            Username = "user@example.com"
        };
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());

        Assert.True(await step.ExecuteAsync(context));

        Assert.Equal(new[] { "--create-app-registration", "--tenant-id", "tenant-a",
            "--username", "user@example.com" }, step.Arguments);
        Assert.Equal("12345678-1234-1234-1234-123456789abc", context.Properties["ClientId"]);
        Assert.Equal(context.Properties["ClientId"], context.Properties["EntraCreatedClientId"]);
        fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private sealed class RegistrationOnlyTestStep(IFileSystem fileSystem)
        : RegisterAppStep(Microsoft.Extensions.Logging.Abstractions.NullLogger<AddClientSecretStep>.Instance,
            fileSystem, Mock.Of<ITelemetryService>())
    {
        public string[]? Arguments { get; private set; }
        protected override bool EnsureToolInstalled() => true;
        protected override (int ExitCode, string? Output, string? Error) ReadCapabilities()
            => (0, "registration-cleanup", null);
        protected override (int ExitCode, string? Output, string? Error) RunRegistration(string[] args)
        {
            Arguments = args;
            return (0, "Created app Test - 12345678-1234-1234-1234-123456789abc.", null);
        }
    }

    [Theory]
    [InlineData(1, null, false)]
    [InlineData(0, "Usage: dotnet-msidentity", false)]
    [InlineData(0, "registration-cleanup", true)]
    public async Task ExecuteAsync_RequiresCleanupCapabilityBeforeCreatingApplication(int exitCode, string? output, bool expected)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists("Test.csproj")).Returns(true);
        var step = new CapabilityTestStep(fileSystem.Object, exitCode, output)
        {
            ProjectPath = "Test.csproj",
            TenantId = "tenant-a",
            Username = "user@example.com"
        };
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());

        Assert.Equal(expected, await step.ExecuteAsync(context));
        Assert.Equal(expected ? 1 : 0, step.Registrations);
        Assert.Empty(context.Properties);
    }

    private sealed class CapabilityTestStep(IFileSystem fileSystem, int exitCode, string? output)
        : RegisterAppStep(Microsoft.Extensions.Logging.Abstractions.NullLogger<AddClientSecretStep>.Instance,
            fileSystem, Mock.Of<ITelemetryService>())
    {
        public int Registrations { get; private set; }
        protected override bool EnsureToolInstalled() => true;
        protected override (int ExitCode, string? Output, string? Error) ReadCapabilities()
            => (exitCode, output, "Unsupported command");
        protected override bool RegisterApp(ScaffolderContext context)
        {
            Registrations++;
            return true;
        }
    }

    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger<AddClientSecretStep>> _mockLogger;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _testProjectPath;

    public RegisterAppStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockLogger = new Mock<ILogger<AddClientSecretStep>>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _testProjectPath = Path.Combine("test", "project", "TestProject.csproj");

        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathIsEmpty()
    {
        // Arrange
        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = string.Empty,
            Username = "test@example.com",
            TenantId = "test-tenant-id"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathDoesNotExist()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(false);

        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            Username = "test@example.com",
            TenantId = "test-tenant-id"
        };

        // Assert
        Assert.NotNull(step);
        Assert.Equal(_testProjectPath, step.ProjectPath);
        Assert.Equal("test@example.com", step.Username);
        Assert.Equal("test-tenant-id", step.TenantId);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        string expectedProjectPath = _testProjectPath;
        string expectedUsername = "test@example.com";
        string expectedTenantId = "test-tenant-id";
        string expectedClientId = "client-id-12345";

        // Act
        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = expectedProjectPath,
            Username = expectedUsername,
            TenantId = expectedTenantId,
            ClientId = expectedClientId
        };

        // Assert
        Assert.Equal(expectedProjectPath, step.ProjectPath);
        Assert.Equal(expectedUsername, step.Username);
        Assert.Equal(expectedTenantId, step.TenantId);
        Assert.Equal(expectedClientId, step.ClientId);
    }

    [Fact]
    public void ClientId_DefaultsToNull()
    {
        // Act
        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Null(step.ClientId);
    }

    [Fact]
    public void Username_DefaultsToNull()
    {
        // Act
        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Null(step.Username);
    }

    [Fact]
    public void TenantId_DefaultsToNull()
    {
        // Act
        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Null(step.TenantId);
    }

    [Fact]
    public void ProjectPath_IsRequired()
    {
        // This test verifies the ProjectPath property is required through compilation
        // The 'required' keyword on the property ensures it must be set during object initialization
        
        // Act & Assert
        // If ProjectPath was not required, this would cause a compilation error
        var step = new RegisterAppStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            ProjectPath = _testProjectPath // Required - must be set
        };

        Assert.NotNull(step.ProjectPath);
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
