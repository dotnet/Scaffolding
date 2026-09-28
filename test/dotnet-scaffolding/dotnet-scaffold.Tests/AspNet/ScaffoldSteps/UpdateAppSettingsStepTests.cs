// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppSettingsStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _projectDirectory;
    private readonly string _projectPath;
    private readonly string _devSettingsPath;
    private readonly string _appSettingsPath;

    public UpdateAppSettingsStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockScaffolder = new Mock<IScaffolder>();
        _projectDirectory = Path.Combine("test", "project");
        _projectPath = Path.Combine(_projectDirectory, "TestProject.csproj");
        _devSettingsPath = Path.Combine(_projectDirectory, "appsettings.Development.json");
        _appSettingsPath = Path.Combine(_projectDirectory, "appsettings.json");

        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesDevelopmentSettingsOnly_PreservingUnrelatedSettings()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              },
              "AzureAd": {
                "Instance": "https://old-instance/",
                "TenantId": "old-tenant",
                "Domain": "old-domain",
                "ClientId": "old-client",
                "CallbackPath": "/old-callback"
              }
            }
            """);

        string? writtenPath = null;
        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) =>
            {
                writtenPath = path;
                writtenContent = content;
            });

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "new-tenant-id",
            ClientId = "new-client-id",
            ClientSecret = "do-not-write-this",
            Overwrite = true
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(_devSettingsPath, writtenPath);
        Assert.NotNull(writtenContent);

        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("Information", json["Logging"]?["LogLevel"]?["Default"]?.ToString());
        Assert.Equal("https://login.microsoftonline.com/", json["AzureAd"]?["Instance"]?.ToString());
        Assert.Equal("new-tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("devuser.onmicrosoft.com", json["AzureAd"]?["Domain"]?.ToString());
        Assert.Equal("new-client-id", json["AzureAd"]?["ClientId"]?.ToString());
        Assert.Equal("/signin-oidc", json["AzureAd"]?["CallbackPath"]?.ToString());
        Assert.Null(json["AzureAd"]?["ClientSecret"]);

        _mockFileSystem.Verify(
            fs => fs.EnumerateFiles(It.IsAny<string>(), "appsettings.json", SearchOption.AllDirectories),
            Times.Never);
        _mockFileSystem.Verify(fs => fs.WriteAllText(_appSettingsPath, It.IsAny<string>()), Times.Never);
        _mockFileSystem.Verify(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenAzureAdConflictsAndOverwriteIsDisabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "existing-tenant-id",
                "Domain": "existing-domain",
                "ClientId": "existing-client-id",
                "CallbackPath": "/signin-oidc"
              }
            }
            """);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "new-tenant-id",
            ClientId = "new-client-id",
            Overwrite = false
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OverwritesConflictingAzureAd_WhenOverwriteIsEnabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "existing-tenant-id",
                "Domain": "existing-domain",
                "ClientId": "existing-client-id",
                "CallbackPath": "/signin-oidc"
              }
            }
            """);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "new-tenant-id",
            ClientId = "new-client-id",
            Overwrite = true
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("new-tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("new-client-id", json["AzureAd"]?["ClientId"]?.ToString());
        Assert.Equal("devuser.onmicrosoft.com", json["AzureAd"]?["Domain"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_SucceedsWithoutWrite_WhenAzureAdAlreadyMatchesAndOverwriteIsDisabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "tenant-id",
                "Domain": "devuser.onmicrosoft.com",
                "ClientId": "client-id",
                "CallbackPath": "/signin-oidc"
              }
            }
            """);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = false
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenAzureAdSectionIsNotObjectAndOverwriteIsDisabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": "invalid-shape"
            }
            """);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = false
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReplacesAzureAdSection_WhenItIsNotObjectAndOverwriteIsEnabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": "invalid-shape"
            }
            """);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = true
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("client-id", json["AzureAd"]?["ClientId"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDevelopmentSettingsFile_WhenMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(false);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("client-id", json["AzureAd"]?["ClientId"]?.ToString());
        _mockFileSystem.Verify(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDevelopmentSettingsIsInvalidJson()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns("{ invalid json");

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenClientIdIsMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = null
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTenantIdIsMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = null,
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDomainAndUsernameAreMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(false);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = null,
            Domain = null,
            TenantId = "tenant-id",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
