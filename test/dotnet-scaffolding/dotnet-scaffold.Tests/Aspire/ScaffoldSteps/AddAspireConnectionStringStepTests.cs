// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.ScaffoldSteps;

public class AddAspireConnectionStringStepTests : IDisposable
{
    private readonly string _testProjectDirectory;
    private readonly string _appSettingsPath;
    private readonly Mock<ILogger<AddAspireConnectionStringStep>> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly TestTelemetryService _testTelemetryService;

    public AddAspireConnectionStringStepTests()
    {
        _testProjectDirectory = Path.Combine(Path.GetTempPath(), "AddAspireConnectionStringStepTests", Guid.NewGuid().ToString());
        _appSettingsPath = Path.Combine(_testProjectDirectory, "appsettings.json");

        _mockLogger = new Mock<ILogger<AddAspireConnectionStringStep>>();
        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testProjectDirectory))
        {
            try
            {
                Directory.Delete(_testProjectDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_CreatesNewAppSettingsFile_WhenFileDoesNotExist()
    {
        // Arrange
        string capturedContent = string.Empty;
        _mockFileSystem.Setup(x => x.EnumerateFiles(_testProjectDirectory, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string>());
        _mockFileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileSystem.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => capturedContent = content);

        AddAspireConnectionStringStep step = new AddAspireConnectionStringStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            BaseProjectPath = _testProjectDirectory,
            ConnectionStringName = "DefaultConnection",
            ConnectionString = "Server=localhost;Database=MyDb"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        
        JsonDocument doc = JsonDocument.Parse(capturedContent);
        Assert.True(doc.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connectionStrings));
        Assert.True(connectionStrings.TryGetProperty("DefaultConnection", out JsonElement connection));
        Assert.Equal("Server=localhost;Database=MyDb", connection.GetString());
    }

    [Fact]
    public async Task ExecuteAsync_AddsConnectionString_ToExistingAppSettings()
    {
        // Arrange
        string existingJson = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information""
    }
  }
}";
        string capturedContent = string.Empty;

        _mockFileSystem.Setup(x => x.EnumerateFiles(_testProjectDirectory, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { _appSettingsPath });
        _mockFileSystem.Setup(x => x.FileExists(_appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllText(_appSettingsPath)).Returns(existingJson);
        _mockFileSystem.Setup(x => x.WriteAllText(_appSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((path, content) => capturedContent = content);

        AddAspireConnectionStringStep step = new AddAspireConnectionStringStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            BaseProjectPath = _testProjectDirectory,
            ConnectionStringName = "DefaultConnection",
            ConnectionString = "Server=localhost;Database=MyDb"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockFileSystem.Verify(x => x.WriteAllText(_appSettingsPath, It.IsAny<string>()), Times.Once);

        JsonDocument doc = JsonDocument.Parse(capturedContent);
        Assert.True(doc.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connectionStrings));
        Assert.True(connectionStrings.TryGetProperty("DefaultConnection", out JsonElement connection));
        Assert.Equal("Server=localhost;Database=MyDb", connection.GetString());
        
        // Verify existing content is preserved
        Assert.True(doc.RootElement.TryGetProperty("Logging", out JsonElement logging));
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotOverwriteExistingConnectionString()
    {
        // Arrange
        string existingJson = @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=existing;Database=Existing""
  }
}";

        _mockFileSystem.Setup(x => x.EnumerateFiles(_testProjectDirectory, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { _appSettingsPath });
        _mockFileSystem.Setup(x => x.FileExists(_appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllText(_appSettingsPath)).Returns(existingJson);

        AddAspireConnectionStringStep step = new AddAspireConnectionStringStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            BaseProjectPath = _testProjectDirectory,
            ConnectionStringName = "DefaultConnection",
            ConnectionString = "Server=localhost;Database=MyDb"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AddsNewConnectionString_ToExistingConnectionStringsSection()
    {
        // Arrange
        string existingJson = @"{
  ""ConnectionStrings"": {
    ""ExistingConnection"": ""Server=existing;Database=Existing""
  }
}";
        string capturedContent = string.Empty;

        _mockFileSystem.Setup(x => x.EnumerateFiles(_testProjectDirectory, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { _appSettingsPath });
        _mockFileSystem.Setup(x => x.FileExists(_appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllText(_appSettingsPath)).Returns(existingJson);
        _mockFileSystem.Setup(x => x.WriteAllText(_appSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((path, content) => capturedContent = content);

        AddAspireConnectionStringStep step = new AddAspireConnectionStringStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            BaseProjectPath = _testProjectDirectory,
            ConnectionStringName = "NewConnection",
            ConnectionString = "Server=localhost;Database=MyDb"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockFileSystem.Verify(x => x.WriteAllText(_appSettingsPath, It.IsAny<string>()), Times.Once);

        JsonDocument doc = JsonDocument.Parse(capturedContent);
        Assert.True(doc.RootElement.TryGetProperty("ConnectionStrings", out JsonElement connectionStrings));
        Assert.True(connectionStrings.TryGetProperty("ExistingConnection", out JsonElement existingConnection));
        Assert.Equal("Server=existing;Database=Existing", existingConnection.GetString());
        Assert.True(connectionStrings.TryGetProperty("NewConnection", out JsonElement newConnection));
        Assert.Equal("Server=localhost;Database=MyDb", newConnection.GetString());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenJsonParsingFails()
    {
        // Arrange
        string invalidJson = "{ invalid json";

        _mockFileSystem.Setup(x => x.EnumerateFiles(_testProjectDirectory, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { _appSettingsPath });
        _mockFileSystem.Setup(x => x.FileExists(_appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllText(_appSettingsPath)).Returns(invalidJson);

        AddAspireConnectionStringStep step = new AddAspireConnectionStringStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            BaseProjectPath = _testProjectDirectory,
            ConnectionStringName = "DefaultConnection",
            ConnectionString = "Server=localhost;Database=MyDb"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_TracksNoChangeTelemetry_WhenConnectionStringExists()
    {
        // Arrange
        string existingJson = @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=existing;Database=Existing""
  }
}";

        _mockFileSystem.Setup(x => x.EnumerateFiles(_testProjectDirectory, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { _appSettingsPath });
        _mockFileSystem.Setup(x => x.FileExists(_appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(x => x.ReadAllText(_appSettingsPath)).Returns(existingJson);

        AddAspireConnectionStringStep step = new AddAspireConnectionStringStep(_mockLogger.Object, _mockFileSystem.Object, _testTelemetryService)
        {
            BaseProjectPath = _testProjectDirectory,
            ConnectionStringName = "DefaultConnection",
            ConnectionString = "Server=localhost;Database=MyDb"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    private class TestScaffolder : IScaffolder
    {
        public TestScaffolder(string name)
        {
            Name = name;
            DisplayName = name;
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string? Description => "Test Scaffolder";
        public IEnumerable<string> Categories => new[] { "Test" };
        public IEnumerable<ScaffolderOption> Options => Enumerable.Empty<ScaffolderOption>();
        public IEnumerable<(string Example, string? Description)> Examples => Enumerable.Empty<(string, string?)>();

        public Task ExecuteAsync(ScaffolderContext context)
        {
            return Task.CompletedTask;
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
