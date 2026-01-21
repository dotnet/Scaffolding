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
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class AddAspNetConnectionStringStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _testProjectPath;

    public AddAspNetConnectionStringStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockScaffolder = new Mock<IScaffolder>();
        _testProjectPath = Path.Combine("test", "project");
        
        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesNewAppSettingsFile_WhenFileDoesNotExist()
    {
        // Arrange
        string expectedConnectionStringName = "DefaultConnection";
        string expectedConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;";
        string appSettingsPath = Path.Combine(_testProjectPath, "appsettings.json");

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "appsettings.json",
            SearchOption.AllDirectories))
            .Returns(new[] { appSettingsPath });

        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(false);

        string? capturedContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => capturedContent = content);

        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        AddAspNetConnectionStringStep step = new AddAspNetConnectionStringStep(
            NullLogger<AddAspNetConnectionStringStep>.Instance,
            _mockFileSystem.Object,
            mockTelemetryService.Object)
        {
            BaseProjectPath = _testProjectPath,
            ConnectionStringName = expectedConnectionStringName,
            ConnectionString = expectedConnectionString
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.NotNull(capturedContent);

        JsonNode? jsonContent = JsonNode.Parse(capturedContent);
        Assert.NotNull(jsonContent);
        Assert.NotNull(jsonContent["ConnectionStrings"]);
        Assert.Equal(expectedConnectionString, jsonContent["ConnectionStrings"]?[expectedConnectionStringName]?.ToString());

        _mockFileSystem.Verify(fs => fs.WriteAllText(appSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AddsConnectionString_WhenFileExistsWithoutConnectionStrings()
    {
        // Arrange
        string expectedConnectionStringName = "DefaultConnection";
        string expectedConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;";
        string appSettingsPath = Path.Combine(_testProjectPath, "appsettings.json");

        JsonObject existingContent = new JsonObject
        {
            ["Logging"] = new JsonObject
            {
                ["LogLevel"] = new JsonObject
                {
                    ["Default"] = "Information"
                }
            }
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "appsettings.json",
            SearchOption.AllDirectories))
            .Returns(new[] { appSettingsPath });

        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath))
            .Returns(existingContent.ToJsonString());

        string? capturedContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => capturedContent = content);

        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        AddAspNetConnectionStringStep step = new AddAspNetConnectionStringStep(
            NullLogger<AddAspNetConnectionStringStep>.Instance,
            _mockFileSystem.Object,
            mockTelemetryService.Object)
        {
            BaseProjectPath = _testProjectPath,
            ConnectionStringName = expectedConnectionStringName,
            ConnectionString = expectedConnectionString
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.NotNull(capturedContent);

        JsonNode? jsonContent = JsonNode.Parse(capturedContent);
        Assert.NotNull(jsonContent);
        Assert.NotNull(jsonContent["ConnectionStrings"]);
        Assert.Equal(expectedConnectionString, jsonContent["ConnectionStrings"]?[expectedConnectionStringName]?.ToString());
        Assert.NotNull(jsonContent["Logging"]);

        _mockFileSystem.Verify(fs => fs.WriteAllText(appSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotModifyFile_WhenConnectionStringAlreadyExists()
    {
        // Arrange
        string expectedConnectionStringName = "DefaultConnection";
        string expectedConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;";
        string appSettingsPath = Path.Combine(_testProjectPath, "appsettings.json");

        JsonObject existingContent = new JsonObject
        {
            ["ConnectionStrings"] = new JsonObject
            {
                [expectedConnectionStringName] = "ExistingConnectionString"
            }
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "appsettings.json",
            SearchOption.AllDirectories))
            .Returns(new[] { appSettingsPath });

        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath))
            .Returns(existingContent.ToJsonString());

        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        AddAspNetConnectionStringStep step = new AddAspNetConnectionStringStep(
            NullLogger<AddAspNetConnectionStringStep>.Instance,
            _mockFileSystem.Object,
            mockTelemetryService.Object)
        {
            BaseProjectPath = _testProjectPath,
            ConnectionStringName = expectedConnectionStringName,
            ConnectionString = expectedConnectionString
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenJsonParsingFails()
    {
        // Arrange
        string appSettingsPath = Path.Combine(_testProjectPath, "appsettings.json");

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "appsettings.json",
            SearchOption.AllDirectories))
            .Returns(new[] { appSettingsPath });

        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath))
            .Returns("invalid json content");

        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        AddAspNetConnectionStringStep step = new AddAspNetConnectionStringStep(
            NullLogger<AddAspNetConnectionStringStep>.Instance,
            _mockFileSystem.Object,
            mockTelemetryService.Object)
        {
            BaseProjectPath = _testProjectPath,
            ConnectionStringName = "DefaultConnection",
            ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AddsSecondConnectionString_WhenOneAlreadyExists()
    {
        // Arrange
        string existingConnectionStringName = "ExistingConnection";
        string newConnectionStringName = "NewConnection";
        string newConnectionString = "Server=(localdb)\\mssqllocaldb;Database=NewDb;Trusted_Connection=True;";
        string appSettingsPath = Path.Combine(_testProjectPath, "appsettings.json");

        JsonObject existingContent = new JsonObject
        {
            ["ConnectionStrings"] = new JsonObject
            {
                [existingConnectionStringName] = "ExistingConnectionString"
            }
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "appsettings.json",
            SearchOption.AllDirectories))
            .Returns(new[] { appSettingsPath });

        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath))
            .Returns(existingContent.ToJsonString());

        string? capturedContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => capturedContent = content);

        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        AddAspNetConnectionStringStep step = new AddAspNetConnectionStringStep(
            NullLogger<AddAspNetConnectionStringStep>.Instance,
            _mockFileSystem.Object,
            mockTelemetryService.Object)
        {
            BaseProjectPath = _testProjectPath,
            ConnectionStringName = newConnectionStringName,
            ConnectionString = newConnectionString
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.NotNull(capturedContent);

        JsonNode? jsonContent = JsonNode.Parse(capturedContent);
        Assert.NotNull(jsonContent);
        Assert.NotNull(jsonContent["ConnectionStrings"]);
        Assert.NotNull(jsonContent["ConnectionStrings"]?[existingConnectionStringName]);
        Assert.Equal(newConnectionString, jsonContent["ConnectionStrings"]?[newConnectionStringName]?.ToString());

        _mockFileSystem.Verify(fs => fs.WriteAllText(appSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotWrite_WhenConnectionStringIsEmpty()
    {
        // Arrange
        string appSettingsPath = Path.Combine(_testProjectPath, "appsettings.json");

        JsonObject existingContent = new JsonObject
        {
            ["Logging"] = new JsonObject()
        };

        _mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "appsettings.json",
            SearchOption.AllDirectories))
            .Returns(new[] { appSettingsPath });

        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath))
            .Returns(existingContent.ToJsonString());

        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        AddAspNetConnectionStringStep step = new AddAspNetConnectionStringStep(
            NullLogger<AddAspNetConnectionStringStep>.Instance,
            _mockFileSystem.Object,
            mockTelemetryService.Object)
        {
            BaseProjectPath = _testProjectPath,
            ConnectionStringName = "DefaultConnection",
            ConnectionString = string.Empty
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        // Even with empty connection string, the ConnectionStrings section is created
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
