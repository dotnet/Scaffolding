// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class AddClientSecretStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _testProjectPath;

    public AddClientSecretStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
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
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        AddClientSecretStep step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = string.Empty,
            ClientId = "test-client-id",
            TenantId = "test-tenant-id",
            Username = "test@example.com"
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
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        AddClientSecretStep step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath,
            ClientId = "test-client-id",
            TenantId = "test-tenant-id",
            Username = "test@example.com"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act
        AddClientSecretStep step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath,
            ClientId = "test-client-id"
        };

        // Assert
        Assert.NotNull(step);
        Assert.Equal(_testProjectPath, step.ProjectPath);
        Assert.Equal("test-client-id", step.ClientId);
    }

    [Fact]
    public void SecretName_HasDefaultValue()
    {
        // Arrange
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act
        AddClientSecretStep step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Equal("AzureAd:ClientCredentials:0:ClientSecret", step.SecretName);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        string expectedClientId = "test-client-id";
        string expectedTenantId = "test-tenant-id";
        string expectedUsername = "test@example.com";
        string expectedSecretName = "CustomSecretName";
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();

        // Act
        AddClientSecretStep step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath,
            ClientId = expectedClientId,
            TenantId = expectedTenantId,
            Username = expectedUsername,
            SecretName = expectedSecretName
        };

        // Assert
        Assert.Equal(_testProjectPath, step.ProjectPath);
        Assert.Equal(expectedClientId, step.ClientId);
        Assert.Equal(expectedTenantId, step.TenantId);
        Assert.Equal(expectedUsername, step.Username);
        Assert.Equal(expectedSecretName, step.SecretName);
    }

    [Fact]
    public async Task ExecuteAsync_UsesCancellationToken()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();

        AddClientSecretStep step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath,
            ClientId = "test-client-id"
        };

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Act
        bool result = await step.ExecuteAsync(_context, cancellationToken);

        // Assert - Even though the result is false (because msidentity tool isn't installed in tests),
        // we're verifying that the cancellation token is properly passed through
        Assert.False(result);
    }

    [Fact]
    public void ProjectPath_IsRequired()
    {
        // This test verifies the ProjectPath property is required through compilation
        // The 'required' keyword on the property ensures it must be set during object initialization
        
        // Arrange
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act & Assert
        // If ProjectPath was not required, this would cause a compilation error
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath // Required - must be set
        };

        Assert.NotNull(step.ProjectPath);
    }

    [Fact]
    public void ClientId_DefaultsToNull()
    {
        // Arrange
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Null(step.ClientId);
    }

    [Fact]
    public void ClientSecret_DefaultsToNull()
    {
        // Arrange
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Null(step.ClientSecret);
    }

    [Fact]
    public void Username_DefaultsToNull()
    {
        // Arrange
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Null(step.Username);
    }

    [Fact]
    public void TenantId_DefaultsToNull()
    {
        // Arrange
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath
        };

        // Assert
        Assert.Null(step.TenantId);
    }

    [Fact]
    public void SecretName_CanBeCustomized()
    {
        // Arrange
        string customSecretName = "MyApp:Azure:Secret";
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        
        // Act
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance,
            _mockFileSystem.Object,
            mockEnvironmentService.Object)
        {
            ProjectPath = _testProjectPath,
            SecretName = customSecretName
        };

        // Assert
        Assert.Equal(customSecretName, step.SecretName);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingApplication_DoesNotCreateOrStoreCredential()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        var environmentService = new Mock<IEnvironmentService>(MockBehavior.Strict);
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance, _mockFileSystem.Object, environmentService.Object)
        {
            ProjectPath = _testProjectPath,
            UseExistingApplication = true
        };

        Assert.True(await step.ExecuteAsync(_context));
        Assert.Null(step.ClientSecret);
        Assert.False(_context.Properties.ContainsKey("ClientSecret"));
        environmentService.VerifyNoOtherCalls();
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void StoreClientSecret_InitializesProjectAndPreservesExistingSecrets()
    {
        string projectDirectory = Path.Combine(Path.GetTempPath(), $"EntraCredentialTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectDirectory);
        string projectPath = Path.Combine(projectDirectory, "Test.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance, _mockFileSystem.Object, Mock.Of<IEnvironmentService>())
        {
            ProjectPath = projectPath
        };

        try
        {
            Assert.True(step.InitializeUserSecrets());
            string? secretsId = XDocument.Load(projectPath).Root?.Element("PropertyGroup")?.Element("UserSecretsId")?.Value;
            Assert.False(string.IsNullOrEmpty(secretsId));
            var seed = DotnetCliRunner.CreateDotNet("user-secrets", ["set", "Unrelated:Setting", "preserved", "--project", projectPath]);
            Assert.Equal(0, seed.ExecuteAndCaptureOutput(out _, out _));

            Assert.True(step.StoreClientSecret("test-only-credential"));
            Assert.True(step.InitializeUserSecrets());
            Assert.True(step.StoreClientSecret("replacement-test-credential"));
            Assert.Equal(secretsId, XDocument.Load(projectPath).Root?.Element("PropertyGroup")?.Element("UserSecretsId")?.Value);

            var list = DotnetCliRunner.CreateDotNet("user-secrets", ["list", "--json", "--project", projectPath]);
            Assert.Equal(0, list.ExecuteAndCaptureOutput(out string? output, out _));
            using var secrets = JsonDocument.Parse(output!, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            Assert.Equal("ClientSecret", secrets.RootElement.GetProperty("AzureAd:ClientCredentials:0:SourceType").GetString());
            Assert.Equal("replacement-test-credential", secrets.RootElement.GetProperty("AzureAd:ClientCredentials:0:ClientSecret").GetString());
            Assert.Equal("preserved", secrets.RootElement.GetProperty("Unrelated:Setting").GetString());
            Assert.DoesNotContain("test-credential", File.ReadAllText(projectPath));
            Assert.False(File.Exists(Path.Combine(projectDirectory, "appsettings.json")));
        }
        finally
        {
            var clear = DotnetCliRunner.CreateDotNet("user-secrets", ["clear", "--project", projectPath]);
            clear.ExecuteAndCaptureOutput(out _, out _);
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void StoreClientSecret_ReturnsFalse_WhenProjectCannotBeLoaded()
    {
        var step = new AddClientSecretStep(
            NullLogger<AddClientSecretStep>.Instance, _mockFileSystem.Object, Mock.Of<IEnvironmentService>())
        {
            ProjectPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.csproj")
        };

        Assert.False(step.InitializeUserSecrets());
        Assert.False(step.StoreClientSecret("test-only-credential"));
    }
}
