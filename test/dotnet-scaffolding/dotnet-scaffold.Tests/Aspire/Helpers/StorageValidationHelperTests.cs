// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Helpers;

/// <summary>
/// Integration tests for storage validation helper based on the
/// aspire storage command options and validation requirements.
/// </summary>
public class StorageValidationHelperTests
{
    private readonly TestLogger _testLogger;
    private readonly Mock<IScaffolder> _mockScaffolder;

    public StorageValidationHelperTests()
    {
        _testLogger = new TestLogger();
        _mockScaffolder = new Mock<IScaffolder>();
    }

    private ScaffolderContext CreateContext(Dictionary<ScaffolderOption, object?> optionResults)
    {
        // Use reflection to call the internal constructor
        ConstructorInfo? constructor = typeof(ScaffolderContext).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [typeof(IScaffolder)],
            null);

        ScaffolderContext context = (ScaffolderContext)constructor!.Invoke(new object[] { _mockScaffolder.Object });
        
        foreach (KeyValuePair<ScaffolderOption, object?> kvp in optionResults)
        {
            context.OptionResults[kvp.Key] = kvp.Value;
        }
        return context;
    }

    private class TestLogger : ILogger
    {
        public List<string> ErrorMessages { get; } = new();
        public List<string> InfoMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            if (logLevel == LogLevel.Error)
            {
                ErrorMessages.Add(message);
            }
            else if (logLevel == LogLevel.Information)
            {
                InfoMessages.Add(message);
            }
        }
    }

    #region ValidateStorageSettings - All Storage Types Tests

    [Theory]
    [InlineData("azure-storage-queues")]
    [InlineData("azure-storage-blobs")]
    [InlineData("azure-data-tables")]
    public void ValidateStorageSettings_WithValidType_ReturnsTrue(string storageType)
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, storageType },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        Assert.True(context.Properties.ContainsKey(nameof(CommandSettings)));
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal(storageType, settings.Type);
        Assert.Equal("/path/to/apphost.csproj", settings.AppHostProject);
        Assert.Equal("/path/to/worker.csproj", settings.Project);
        Assert.False(settings.Prerelease);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateStorageSettings_WithQueues_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-queues" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("azure-storage-queues", settings.Type);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateStorageSettings_WithBlobs_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-blobs" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("azure-storage-blobs", settings.Type);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateStorageSettings_WithTables_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-data-tables" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("azure-data-tables", settings.Type);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateStorageSettings_WithPrerelease_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-blobs" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, true }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.True(settings.Prerelease);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    #endregion

    #region ValidateStorageSettings - Invalid Inputs Tests

    [Fact]
    public void ValidateStorageSettings_WithInvalidType_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "invalid-storage" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Valid options"));
    }

    [Fact]
    public void ValidateStorageSettings_WithNullType_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, null },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
    }

    [Fact]
    public void ValidateStorageSettings_WithEmptyType_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
    }

    [Fact]
    public void ValidateStorageSettings_WithMissingAppHostProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-blobs" },
            { AspireOptions.AppHostProject, null },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --apphost-project option"));
    }

    [Fact]
    public void ValidateStorageSettings_WithEmptyAppHostProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-blobs" },
            { AspireOptions.AppHostProject, "" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --apphost-project option"));
    }

    [Fact]
    public void ValidateStorageSettings_WithMissingProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-blobs" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, null },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --project option"));
    }

    [Fact]
    public void ValidateStorageSettings_WithEmptyProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-blobs" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --project option"));
    }

    [Theory]
    [InlineData("AZURE-STORAGE-QUEUES")]
    [InlineData("Azure-Storage-Blobs")]
    [InlineData("AZURE-DATA-TABLES")]
    public void ValidateStorageSettings_IsCaseInsensitive(string storageType)
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, storageType },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        // The type is stored as provided (case-sensitive), not lowercased
        Assert.Equal(storageType, settings.Type);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateStorageSettings_WithMultipleErrors_ReportsAll()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "invalid-type" },
            { AspireOptions.AppHostProject, "" },
            { AspireOptions.Project, "" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        // Should report error for invalid type and missing projects
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
        Assert.True(_testLogger.ErrorMessages.Count > 1);
    }

    #endregion

    #region CommandSettings Creation Tests

    [Theory]
    [InlineData("azure-storage-queues", true)]
    [InlineData("azure-storage-blobs", false)]
    [InlineData("azure-data-tables", true)]
    public void ValidateStorageSettings_CreatesCommandSettings_WithCorrectProperties(string storageType, bool prerelease)
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, storageType },
            { AspireOptions.AppHostProject, "/custom/path/apphost.csproj" },
            { AspireOptions.Project, "/custom/path/worker.csproj" },
            { AspireOptions.Prerelease, prerelease }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal(storageType, settings.Type);
        Assert.Equal("/custom/path/apphost.csproj", settings.AppHostProject);
        Assert.Equal("/custom/path/worker.csproj", settings.Project);
        Assert.Equal(prerelease, settings.Prerelease);
    }

    [Fact]
    public void ValidateStorageSettings_AddsCommandSettings_ToContextProperties()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.StorageType, "azure-storage-queues" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateStorageSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        Assert.True(context.Properties.ContainsKey(nameof(CommandSettings)));
        Assert.IsType<CommandSettings>(context.Properties[nameof(CommandSettings)]);
    }

    #endregion
}
