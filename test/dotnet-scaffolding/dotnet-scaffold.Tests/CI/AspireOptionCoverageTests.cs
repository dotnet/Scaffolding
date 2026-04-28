// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CI;

/// <summary>
/// Tests that validate every Aspire CLI option has correct metadata:
/// CliOption flag, DisplayName, Description, Required semantics, and picker values.
/// </summary>
public class AspireOptionCoverageTests
{
    #region --type (CachingType)

    [Fact]
    public void CachingType_HasCorrectCliOption()
        => Assert.Equal("--type", AspireOptions.CachingType.CliOption);

    [Fact]
    public void CachingType_IsRequired()
        => Assert.True(AspireOptions.CachingType.Required);

    [Fact]
    public void CachingType_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.CachingType.Description));

    [Fact]
    public void CachingType_HasNonEmptyDisplayName()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.CachingType.DisplayName));

    [Fact]
    public void CachingType_HasCustomPickerValues()
        => Assert.NotEmpty(AspireOptions.CachingType.CustomPickerValues);

    [Fact]
    public void CachingType_ValuesContainRedis()
        => Assert.Contains("redis", AspireOptions.CachingType.CustomPickerValues);

    [Fact]
    public void CachingType_ValuesContainRedisWithOutputCaching()
        => Assert.Contains("redis-with-output-caching", AspireOptions.CachingType.CustomPickerValues);

    #endregion

    #region --type (DatabaseType)

    [Fact]
    public void DatabaseType_HasCorrectCliOption()
        => Assert.Equal("--type", AspireOptions.DatabaseType.CliOption);

    [Fact]
    public void DatabaseType_IsRequired()
        => Assert.True(AspireOptions.DatabaseType.Required);

    [Fact]
    public void DatabaseType_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.DatabaseType.Description));

    [Fact]
    public void DatabaseType_HasCustomPickerValues()
        => Assert.NotEmpty(AspireOptions.DatabaseType.CustomPickerValues);

    [Fact]
    public void DatabaseType_ValuesContainNpgsqlEfCore()
        => Assert.Contains("npgsql-efcore", AspireOptions.DatabaseType.CustomPickerValues);

    [Fact]
    public void DatabaseType_ValuesContainSqlServerEfCore()
        => Assert.Contains("sqlserver-efcore", AspireOptions.DatabaseType.CustomPickerValues);

    #endregion

    #region --type (StorageType)

    [Fact]
    public void StorageType_HasCorrectCliOption()
        => Assert.Equal("--type", AspireOptions.StorageType.CliOption);

    [Fact]
    public void StorageType_IsRequired()
        => Assert.True(AspireOptions.StorageType.Required);

    [Fact]
    public void StorageType_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.StorageType.Description));

    [Fact]
    public void StorageType_HasCustomPickerValues()
        => Assert.NotEmpty(AspireOptions.StorageType.CustomPickerValues);

    [Fact]
    public void StorageType_ValuesContainAzureStorageQueues()
        => Assert.Contains("azure-storage-queues", AspireOptions.StorageType.CustomPickerValues);

    [Fact]
    public void StorageType_ValuesContainAzureStorageBlobs()
        => Assert.Contains("azure-storage-blobs", AspireOptions.StorageType.CustomPickerValues);

    [Fact]
    public void StorageType_ValuesContainAzureDataTables()
        => Assert.Contains("azure-data-tables", AspireOptions.StorageType.CustomPickerValues);

    #endregion

    #region --apphost-project

    [Fact]
    public void AppHostProject_HasCorrectCliOption()
        => Assert.Equal("--apphost-project", AspireOptions.AppHostProject.CliOption);

    [Fact]
    public void AppHostProject_IsRequired()
        => Assert.True(AspireOptions.AppHostProject.Required);

    [Fact]
    public void AppHostProject_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.AppHostProject.Description));

    [Fact]
    public void AppHostProject_HasNonEmptyDisplayName()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.AppHostProject.DisplayName));

    #endregion

    #region --project (Aspire worker project)

    [Fact]
    public void Project_HasCorrectCliOption()
        => Assert.Equal("--project", AspireOptions.Project.CliOption);

    [Fact]
    public void Project_IsRequired()
        => Assert.True(AspireOptions.Project.Required);

    [Fact]
    public void Project_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.Project.Description));

    [Fact]
    public void Project_HasNonEmptyDisplayName()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.Project.DisplayName));

    #endregion

    #region --prerelease (Aspire)

    [Fact]
    public void Prerelease_HasCorrectCliOption()
        => Assert.Equal("--prerelease", AspireOptions.Prerelease.CliOption);

    [Fact]
    public void Prerelease_IsNotRequired()
        => Assert.False(AspireOptions.Prerelease.Required);

    [Fact]
    public void Prerelease_HasNonEmptyDescription()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.Prerelease.Description));

    [Fact]
    public void Prerelease_HasNonEmptyDisplayName()
        => Assert.False(string.IsNullOrWhiteSpace(AspireOptions.Prerelease.DisplayName));

    #endregion

    #region Cross-option consistency

    [Fact]
    public void AllTypeOptions_ShareSameCliFlag()
    {
        Assert.Equal(AspireOptions.CachingType.CliOption, AspireOptions.DatabaseType.CliOption);
        Assert.Equal(AspireOptions.CachingType.CliOption, AspireOptions.StorageType.CliOption);
    }

    [Fact]
    public void AllCustomPickerValues_AreDistinct()
    {
        var allOptions = new[]
        {
            AspireOptions.CachingType,
            AspireOptions.DatabaseType,
            AspireOptions.StorageType
        };

        foreach (var option in allOptions)
        {
            Assert.NotNull(option.CustomPickerValues);
            var distinct = option.CustomPickerValues.Distinct().ToList();
            Assert.Equal(option.CustomPickerValues.Count(), distinct.Count);
        }
    }

    #endregion
}
