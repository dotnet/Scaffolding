// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

public class ValidationHelperTests
{
    private readonly TestLogger _testLogger;
    private readonly Mock<IScaffolder> _mockScaffolder;

    public ValidationHelperTests()
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

    #region ValidateCachingSettings Tests

    [Fact]
    public void ValidateCachingSettings_WithValidRedisSettings_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "redis" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        Assert.True(context.Properties.ContainsKey(nameof(CommandSettings)));
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("redis", settings.Type);
        Assert.Equal("/path/to/apphost.csproj", settings.AppHostProject);
        Assert.Equal("/path/to/worker.csproj", settings.Project);
        Assert.False(settings.Prerelease);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateCachingSettings_WithValidRedisWithOutputCachingSettings_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "redis-with-output-caching" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, true }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("redis-with-output-caching", settings.Type);
        Assert.True(settings.Prerelease);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateCachingSettings_WithInvalidType_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "invalid" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Valid options"));
    }

    [Fact]
    public void ValidateCachingSettings_WithNullType_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, null },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
    }

    [Fact]
    public void ValidateCachingSettings_WithEmptyType_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
    }

    [Fact]
    public void ValidateCachingSettings_WithMissingAppHostProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "redis" },
            { AspireOptions.AppHostProject, null },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --apphost-project option"));
    }

    [Fact]
    public void ValidateCachingSettings_WithEmptyAppHostProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "redis" },
            { AspireOptions.AppHostProject, "" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --apphost-project option"));
    }

    [Fact]
    public void ValidateCachingSettings_WithMissingWorkerProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "redis" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, null },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --project option"));
    }

    [Fact]
    public void ValidateCachingSettings_WithEmptyWorkerProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, "redis" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --project option"));
    }

    [Theory]
    [InlineData("redis")]
    [InlineData("REDIS")]
    [InlineData("Redis")]
    [InlineData("redis-with-output-caching")]
    [InlineData("REDIS-WITH-OUTPUT-CACHING")]
    public void ValidateCachingSettings_IsCaseInsensitive(string cacheType)
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.CachingType, cacheType },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateCachingSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    #endregion

    #region ValidateDatabaseSettings Tests

    [Fact]
    public void ValidateDatabaseSettings_WithValidNpgsqlSettings_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.DatabaseType, "npgsql-efcore" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateDatabaseSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        Assert.True(context.Properties.ContainsKey(nameof(CommandSettings)));
        Assert.True(context.Properties.ContainsKey("BaseProjectPath")); // Constants.StepConstants.BaseProjectPath
        Assert.Empty(_testLogger.ErrorMessages);

        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("npgsql-efcore", settings.Type);
    }

    [Fact]
    public void ValidateDatabaseSettings_WithValidSqlServerSettings_ReturnsTrue()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.DatabaseType, "sqlserver-efcore" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/Project/worker.csproj" },
            { AspireOptions.Prerelease, true }
        });

        // Act
        bool result = ValidationHelper.ValidateDatabaseSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("sqlserver-efcore", settings.Type);
        Assert.True(settings.Prerelease);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateDatabaseSettings_AddsBaseProjectPathToContext()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.DatabaseType, "npgsql-efcore" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/project/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateDatabaseSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        Assert.True(context.Properties.ContainsKey("BaseProjectPath")); // Constants.StepConstants.BaseProjectPath
        string? basePath = context.Properties["BaseProjectPath"] as string;
        Assert.NotNull(basePath);
        Assert.Contains("project", basePath);
    }

    [Fact]
    public void ValidateDatabaseSettings_WithInvalidType_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.DatabaseType, "invalid-db" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateDatabaseSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --type option"));
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Valid options"));
    }

    [Fact]
    public void ValidateDatabaseSettings_WithMissingAppHostProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.DatabaseType, "npgsql-efcore" },
            { AspireOptions.AppHostProject, null },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateDatabaseSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --apphost-project option"));
    }

    [Fact]
    public void ValidateDatabaseSettings_WithMissingWorkerProject_ReturnsFalse()
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.DatabaseType, "npgsql-efcore" },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, null },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateDatabaseSettings(context, _testLogger);

        // Assert
        Assert.False(result);
        Assert.Contains(_testLogger.ErrorMessages, e => e.Contains("Missing/Invalid --project option"));
    }

    [Theory]
    [InlineData("npgsql-efcore")]
    [InlineData("NPGSQL-EFCORE")]
    [InlineData("NpgSql-EfCore")]
    [InlineData("sqlserver-efcore")]
    [InlineData("SQLSERVER-EFCORE")]
    public void ValidateDatabaseSettings_IsCaseInsensitive(string dbType)
    {
        // Arrange
        ScaffolderContext context = CreateContext(new Dictionary<ScaffolderOption, object?>
        {
            { AspireOptions.DatabaseType, dbType },
            { AspireOptions.AppHostProject, "/path/to/apphost.csproj" },
            { AspireOptions.Project, "/path/to/worker.csproj" },
            { AspireOptions.Prerelease, false }
        });

        // Act
        bool result = ValidationHelper.ValidateDatabaseSettings(context, _testLogger);

        // Assert
        Assert.True(result);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    #endregion

    #region ValidateStorageSettings Tests

    [Fact]
    public void ValidateStorageSettings_WithValidQueuesSettings_ReturnsTrue()
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
        CommandSettings? settings = context.Properties[nameof(CommandSettings)] as CommandSettings;
        Assert.NotNull(settings);
        Assert.Equal("azure-storage-queues", settings.Type);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateStorageSettings_WithValidBlobsSettings_ReturnsTrue()
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
        Assert.Equal("azure-storage-blobs", settings.Type);
        Assert.True(settings.Prerelease);
        Assert.Empty(_testLogger.ErrorMessages);
    }

    [Fact]
    public void ValidateStorageSettings_WithValidTablesSettings_ReturnsTrue()
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
    public void ValidateStorageSettings_WithMissingWorkerProject_ReturnsFalse()
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

    [Theory]
    [InlineData("azure-storage-queues")]
    [InlineData("AZURE-STORAGE-QUEUES")]
    [InlineData("Azure-Storage-Queues")]
    [InlineData("azure-storage-blobs")]
    [InlineData("AZURE-STORAGE-BLOBS")]
    [InlineData("azure-data-tables")]
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
        Assert.Empty(_testLogger.ErrorMessages);
    }

    #endregion
}

