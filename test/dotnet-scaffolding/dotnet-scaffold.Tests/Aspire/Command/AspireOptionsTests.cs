// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Command;

public class AspireOptionsTests
{
    [Fact]
    public void CachingType_CustomPickerValues_ContainsRedis()
    {
        // Act
        var option = AspireOptions.CachingType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("redis", option.CustomPickerValues);
    }

    [Fact]
    public void CachingType_CustomPickerValues_ContainsRedisWithOutputCaching()
    {
        // Act
        var option = AspireOptions.CachingType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("redis-with-output-caching", option.CustomPickerValues);
    }

    [Fact]
    public void CachingType_CustomPickerValues_HasExactlyTwoValues()
    {
        // Act
        var option = AspireOptions.CachingType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(2, option.CustomPickerValues.Count());
    }

    [Fact]
    public void CachingType_CustomPickerValues_HasExpectedValues()
    {
        // Arrange
        var expectedValues = new List<string> { "redis", "redis-with-output-caching" };

        // Act
        var option = AspireOptions.CachingType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(expectedValues, option.CustomPickerValues);
    }

    [Fact]
    public void CachingType_CustomPickerValues_MatchesCliStrings()
    {
        // Act
        var option = AspireOptions.CachingType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(AspireCliStrings.CachingTypeCustomValues, option.CustomPickerValues);
    }

    [Fact]
    public void DatabaseType_CustomPickerValues_ContainsNpgsqlEfCore()
    {
        // Act
        var option = AspireOptions.DatabaseType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("npgsql-efcore", option.CustomPickerValues);
    }

    [Fact]
    public void DatabaseType_CustomPickerValues_ContainsSqlServerEfCore()
    {
        // Act
        var option = AspireOptions.DatabaseType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("sqlserver-efcore", option.CustomPickerValues);
    }

    [Fact]
    public void DatabaseType_CustomPickerValues_HasExactlyTwoValues()
    {
        // Act
        var option = AspireOptions.DatabaseType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(2, option.CustomPickerValues.Count());
    }

    [Fact]
    public void DatabaseType_CustomPickerValues_HasExpectedValues()
    {
        // Arrange
        var expectedValues = new List<string> { "npgsql-efcore", "sqlserver-efcore" };

        // Act
        var option = AspireOptions.DatabaseType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(expectedValues.Count, option.CustomPickerValues.Count());
        foreach (var value in expectedValues)
        {
            Assert.Contains(value, option.CustomPickerValues);
        }
    }

    [Fact]
    public void DatabaseType_CustomPickerValues_MatchesCliStrings()
    {
        // Act
        var option = AspireOptions.DatabaseType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(AspireCliStrings.Database.DatabaseTypeCustomValues, option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_CustomPickerValues_ContainsAzureStorageQueues()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("azure-storage-queues", option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_CustomPickerValues_ContainsAzureStorageBlobs()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("azure-storage-blobs", option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_CustomPickerValues_ContainsAzureDataTables()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("azure-data-tables", option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_CustomPickerValues_HasExactlyThreeValues()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(3, option.CustomPickerValues.Count());
    }

    [Fact]
    public void StorageType_CustomPickerValues_HasExpectedValues()
    {
        // Arrange
        var expectedValues = new List<string>
        {
            "azure-storage-queues",
            "azure-storage-blobs",
            "azure-data-tables"
        };

        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(expectedValues, option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_CustomPickerValues_MatchesCliStrings()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(AspireCliStrings.StorageTypeCustomValues, option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_CustomPickerValues_AllValuesStartWithAzure()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.All(option.CustomPickerValues, value => Assert.StartsWith("azure-", value));
    }

    [Fact]
    public void AllCustomPickerValues_AreNotNullOrEmpty()
    {
        // Arrange
        var options = new[]
        {
            AspireOptions.CachingType,
            AspireOptions.DatabaseType,
            AspireOptions.StorageType
        };

        // Act & Assert
        foreach (var option in options)
        {
            Assert.NotNull(option.CustomPickerValues);
            Assert.NotEmpty(option.CustomPickerValues);
            Assert.All(option.CustomPickerValues, value =>
            {
                Assert.NotNull(value);
                Assert.NotEmpty(value);
            });
        }
    }

    [Fact]
    public void AllCustomPickerValues_DoNotContainDuplicates()
    {
        // Arrange
        var options = new[]
        {
            AspireOptions.CachingType,
            AspireOptions.DatabaseType,
            AspireOptions.StorageType
        };

        // Act & Assert
        foreach (var option in options)
        {
            Assert.NotNull(option.CustomPickerValues);
            var distinctValues = option.CustomPickerValues.Distinct().ToList();
            Assert.Equal(option.CustomPickerValues.Count(), distinctValues.Count);
        }
    }

    [Fact]
    public void CachingType_CustomPickerValues_AreAllLowercase()
    {
        // Act
        var option = AspireOptions.CachingType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.All(option.CustomPickerValues, value =>
        {
            Assert.Equal(value.ToLowerInvariant(), value);
        });
    }

    [Fact]
    public void DatabaseType_CustomPickerValues_FollowExpectedNamingConvention()
    {
        // Act
        var option = AspireOptions.DatabaseType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.All(option.CustomPickerValues, value =>
        {
            Assert.EndsWith("-efcore", value);
        });
    }

    [Fact]
    public void StorageType_CustomPickerValues_ContainOnlyAzureStorageTypes()
    {
        // Arrange
        var expectedPrefixes = new[] { "azure-storage-", "azure-data-" };

        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.All(option.CustomPickerValues, value =>
        {
            Assert.True(
                expectedPrefixes.Any(prefix => value.StartsWith(prefix)),
                $"Value '{value}' does not start with any expected prefix");
        });
    }

    [Fact]
    public void AllCustomPickerValues_UseHyphenDelimiters()
    {
        // Arrange
        var options = new[]
        {
            AspireOptions.CachingType,
            AspireOptions.DatabaseType,
            AspireOptions.StorageType
        };

        // Act & Assert
        foreach (var option in options)
        {
            Assert.NotNull(option.CustomPickerValues);
            Assert.All(option.CustomPickerValues, value =>
            {
                Assert.DoesNotContain("_", value);
                Assert.DoesNotContain(" ", value);
            });
        }
    }

    [Theory]
    [InlineData("redis")]
    [InlineData("redis-with-output-caching")]
    public void CachingType_CustomPickerValues_ContainsExpectedValue(string expectedValue)
    {
        // Act
        var option = AspireOptions.CachingType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains(expectedValue, option.CustomPickerValues);
    }

    [Theory]
    [InlineData("npgsql-efcore")]
    [InlineData("sqlserver-efcore")]
    public void DatabaseType_CustomPickerValues_ContainsExpectedValue(string expectedValue)
    {
        // Act
        var option = AspireOptions.DatabaseType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains(expectedValue, option.CustomPickerValues);
    }

    [Theory]
    [InlineData("azure-storage-queues")]
    [InlineData("azure-storage-blobs")]
    [InlineData("azure-data-tables")]
    public void StorageType_CustomPickerValues_ContainsExpectedValue(string expectedValue)
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains(expectedValue, option.CustomPickerValues);
    }
}
