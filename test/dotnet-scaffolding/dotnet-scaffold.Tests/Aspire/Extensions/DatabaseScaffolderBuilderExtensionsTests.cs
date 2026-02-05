// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Xunit;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Extensions;

public class DatabaseScaffolderBuilderExtensionsTests
{
    [Fact]
    public void GetAppHostProperties_WithValidNpgsqlSettings_ReturnsExpectedProperties()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "npgsql-efcore",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetAppHostProperties(settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("$(DbName)"));
        Assert.True(result.ContainsKey("$(AddDbMethod)"));
        Assert.True(result.ContainsKey("$(DbType)"));
        Assert.Equal("postgresqldb", result["$(DbName)"]);
        Assert.Equal("AddPostgres", result["$(AddDbMethod)"]);
        Assert.Equal("postgresql", result["$(DbType)"]);
    }

    [Fact]
    public void GetAppHostProperties_WithValidSqlServerSettings_ReturnsExpectedProperties()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "sqlserver-efcore",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetAppHostProperties(settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("$(DbName)"));
        Assert.True(result.ContainsKey("$(AddDbMethod)"));
        Assert.True(result.ContainsKey("$(DbType)"));
        Assert.Equal("sqldb", result["$(DbName)"]);
        Assert.Equal("AddSqlServer", result["$(AddDbMethod)"]);
        Assert.Equal("sqlserver", result["$(DbType)"]);
    }

    [Fact]
    public void GetAppHostProperties_WithInvalidType_ReturnsEmptyDictionary()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "invalid-type",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetAppHostProperties(settings);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetApiProjectProperties_WithValidNpgsqlSettings_ReturnsExpectedProperties()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "npgsql-efcore",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetApiProjectProperties(settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("$(DbName)"));
        Assert.True(result.ContainsKey("$(AddDbContextMethod)"));
        Assert.True(result.ContainsKey("$(DbContextName)"));
        Assert.Equal("postgresqldb", result["$(DbName)"]);
        Assert.Equal("AddNpgsqlDbContext", result["$(AddDbContextMethod)"]);
    }

    [Fact]
    public void GetApiProjectProperties_WithValidSqlServerSettings_ReturnsExpectedProperties()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "sqlserver-efcore",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetApiProjectProperties(settings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("$(DbName)"));
        Assert.True(result.ContainsKey("$(AddDbContextMethod)"));
        Assert.True(result.ContainsKey("$(DbContextName)"));
        Assert.Equal("sqldb", result["$(DbName)"]);
        Assert.Equal("AddSqlServerDbContext", result["$(AddDbContextMethod)"]);
    }

    [Fact]
    public void GetApiProjectProperties_WithInvalidType_ReturnsEmptyDictionary()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "invalid-type",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetApiProjectProperties(settings);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAppHostProperties_AllPropertiesHaveValidPlaceholderFormat()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "npgsql-efcore",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetAppHostProperties(settings);

        // Assert
        foreach (string key in result.Keys)
        {
            Assert.StartsWith("$(", key);
            Assert.EndsWith(")", key);
        }
    }

    [Fact]
    public void GetApiProjectProperties_AllPropertiesHaveValidPlaceholderFormat()
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = "sqlserver-efcore",
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetApiProjectProperties(settings);

        // Assert
        foreach (string key in result.Keys)
        {
            Assert.StartsWith("$(", key);
            Assert.EndsWith(")", key);
        }
    }

    [Theory]
    [InlineData("npgsql-efcore")]
    [InlineData("sqlserver-efcore")]
    public void GetAppHostProperties_AllValuesAreNotNullOrEmpty(string dbType)
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = dbType,
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetAppHostProperties(settings);

        // Assert
        Assert.NotEmpty(result);
        foreach (string value in result.Values)
        {
            Assert.NotNull(value);
            Assert.NotEmpty(value);
        }
    }

    [Theory]
    [InlineData("npgsql-efcore")]
    [InlineData("sqlserver-efcore")]
    public void GetApiProjectProperties_AllValuesAreNotNullOrEmpty(string dbType)
    {
        // Arrange
        CommandSettings settings = new()
        {
            Type = dbType,
            AppHostProject = "/path/to/apphost.csproj",
            Project = "/path/to/project.csproj"
        };

        // Act
        Dictionary<string, string> result = DatabaseScaffolderBuilderExtensions.GetApiProjectProperties(settings);

        // Assert
        Assert.NotEmpty(result);
        foreach (string value in result.Values)
        {
            Assert.NotNull(value);
            Assert.NotEmpty(value);
        }
    }
}
