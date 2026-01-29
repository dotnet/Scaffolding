// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Helpers;

/// <summary>
/// Unit tests for storage-related package constants and configurations
/// based on aspire storage command types.
/// </summary>
public class StoragePackageConstantsTests
{
    [Fact]
    public void AppHostStoragePackage_HasCorrectName()
    {
        // Act
        var package = PackageConstants.StoragePackages.AppHostStoragePackage;

        // Assert
        Assert.Equal("Aspire.Hosting.Azure.Storage", package.Name);
    }

    [Fact]
    public void ApiServiceBlobsPackage_HasCorrectName()
    {
        // Act
        var package = PackageConstants.StoragePackages.ApiServiceBlobsPackage;

        // Assert
        Assert.Equal("Aspire.Azure.Storage.Blobs", package.Name);
    }

    [Fact]
    public void ApiServiceQueuesPackage_HasCorrectName()
    {
        // Act
        var package = PackageConstants.StoragePackages.ApiServiceQueuesPackage;

        // Assert
        Assert.Equal("Aspire.Azure.Storage.Queues", package.Name);
    }

    [Fact]
    public void ApiServiceTablesPackage_HasCorrectName()
    {
        // Act
        var package = PackageConstants.StoragePackages.ApiServiceTablesPackage;

        // Assert
        Assert.Equal("Aspire.Azure.Data.Tables", package.Name);
    }

    [Fact]
    public void StoragePackagesDict_ContainsAllThreeTypes()
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;

        // Assert
        Assert.Equal(3, dict.Count);
        Assert.True(dict.ContainsKey("azure-storage-queues"));
        Assert.True(dict.ContainsKey("azure-storage-blobs"));
        Assert.True(dict.ContainsKey("azure-data-tables"));
    }

    [Fact]
    public void StoragePackagesDict_AzureStorageQueues_MapsToQueuesPackage()
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;
        var success = dict.TryGetValue("azure-storage-queues", out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.Equal("Aspire.Azure.Storage.Queues", package.Name);
    }

    [Fact]
    public void StoragePackagesDict_AzureStorageBlobs_MapsToBlobsPackage()
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;
        var success = dict.TryGetValue("azure-storage-blobs", out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.Equal("Aspire.Azure.Storage.Blobs", package.Name);
    }

    [Fact]
    public void StoragePackagesDict_AzureDataTables_MapsToTablesPackage()
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;
        var success = dict.TryGetValue("azure-data-tables", out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.Equal("Aspire.Azure.Data.Tables", package.Name);
    }

    [Theory]
    [InlineData("azure-storage-queues", "Aspire.Azure.Storage.Queues")]
    [InlineData("azure-storage-blobs", "Aspire.Azure.Storage.Blobs")]
    [InlineData("azure-data-tables", "Aspire.Azure.Data.Tables")]
    public void StoragePackagesDict_ValidTypes_MapToCorrectPackages(string storageType, string expectedPackageName)
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;
        var success = dict.TryGetValue(storageType, out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.Equal(expectedPackageName, package.Name);
    }

    [Fact]
    public void StoragePackagesDict_InvalidType_ReturnsNull()
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;
        var success = dict.TryGetValue("invalid-storage-type", out var package);

        // Assert
        Assert.False(success);
        Assert.Null(package);
    }

    [Fact]
    public void StoragePackagesDict_Keys_MatchStorageTypeCustomValues()
    {
        // Arrange
        var expectedKeys = new List<string>
        {
            "azure-storage-queues",
            "azure-storage-blobs",
            "azure-data-tables"
        };

        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;
        var actualKeys = new List<string>(dict.Keys);

        // Assert
        Assert.Equal(expectedKeys.Count, actualKeys.Count);
        foreach (var key in expectedKeys)
        {
            Assert.Contains(key, actualKeys);
        }
    }

    [Fact]
    public void AllStoragePackages_AreNotNull()
    {
        // Assert
        Assert.NotNull(PackageConstants.StoragePackages.AppHostStoragePackage);
        Assert.NotNull(PackageConstants.StoragePackages.ApiServiceBlobsPackage);
        Assert.NotNull(PackageConstants.StoragePackages.ApiServiceQueuesPackage);
        Assert.NotNull(PackageConstants.StoragePackages.ApiServiceTablesPackage);
    }

    [Fact]
    public void AllStoragePackages_HaveNonEmptyNames()
    {
        // Assert
        Assert.NotEmpty(PackageConstants.StoragePackages.AppHostStoragePackage.Name);
        Assert.NotEmpty(PackageConstants.StoragePackages.ApiServiceBlobsPackage.Name);
        Assert.NotEmpty(PackageConstants.StoragePackages.ApiServiceQueuesPackage.Name);
        Assert.NotEmpty(PackageConstants.StoragePackages.ApiServiceTablesPackage.Name);
    }

    [Fact]
    public void StoragePackagesDict_AllValues_AreNotNull()
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;

        // Assert
        foreach (var kvp in dict)
        {
            Assert.NotNull(kvp.Value);
            Assert.NotEmpty(kvp.Value.Name);
        }
    }

    [Fact]
    public void StoragePackages_AllPackageNames_StartWithAspire()
    {
        // Assert
        Assert.StartsWith("Aspire.", PackageConstants.StoragePackages.AppHostStoragePackage.Name);
        Assert.StartsWith("Aspire.", PackageConstants.StoragePackages.ApiServiceBlobsPackage.Name);
        Assert.StartsWith("Aspire.", PackageConstants.StoragePackages.ApiServiceQueuesPackage.Name);
        Assert.StartsWith("Aspire.", PackageConstants.StoragePackages.ApiServiceTablesPackage.Name);
    }

    [Theory]
    [InlineData("azure-storage-queues")]
    [InlineData("azure-storage-blobs")]
    [InlineData("azure-data-tables")]
    public void StoragePackagesDict_AllKnownTypes_CanBeRetrieved(string storageType)
    {
        // Act
        var dict = PackageConstants.StoragePackages.StoragePackagesDict;
        var success = dict.TryGetValue(storageType, out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.StartsWith("Aspire.", package.Name);
    }
}
