// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Extensions;

/// <summary>
/// Unit tests for Storage constants and properties used by StorageScaffolderBuilderExtensions,
/// validating configurations for Azure Storage integration (queues, blobs, tables)
/// based on aspire storage command.
/// </summary>
public class StorageScaffolderBuilderExtensionsTests
{
    [Fact]
    public void StoragePropertiesDict_ContainsAllThreeTypes()
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;

        // Assert
        Assert.Equal(3, dict.Count);
        Assert.True(dict.ContainsKey("azure-storage-queues"));
        Assert.True(dict.ContainsKey("azure-storage-blobs"));
        Assert.True(dict.ContainsKey("azure-data-tables"));
    }

    [Fact]
    public void StoragePropertiesDict_AzureStorageQueues_HasCorrectProperties()
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue("azure-storage-queues", out var properties);

        // Assert
        Assert.True(success);
        Assert.NotNull(properties);
        Assert.Equal("queues", properties.VariableName);
        Assert.Equal("AddQueues", properties.AddMethodName);
        Assert.Equal("AddAzureQueueServiceClient", properties.AddClientMethodName);
    }

    [Fact]
    public void StoragePropertiesDict_AzureStorageBlobs_HasCorrectProperties()
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue("azure-storage-blobs", out var properties);

        // Assert
        Assert.True(success);
        Assert.NotNull(properties);
        Assert.Equal("blobs", properties.VariableName);
        Assert.Equal("AddBlobs", properties.AddMethodName);
        Assert.Equal("AddAzureBlobServiceClient", properties.AddClientMethodName);
    }

    [Fact]
    public void StoragePropertiesDict_AzureDataTables_HasCorrectProperties()
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue("azure-data-tables", out var properties);

        // Assert
        Assert.True(success);
        Assert.NotNull(properties);
        Assert.Equal("entries", properties.VariableName);
        Assert.Equal("AddTables", properties.AddMethodName);
        Assert.Equal("AddAzureTableServiceClient", properties.AddClientMethodName);
    }

    [Theory]
    [InlineData("azure-storage-queues", "queues", "AddQueues", "AddAzureQueueServiceClient")]
    [InlineData("azure-storage-blobs", "blobs", "AddBlobs", "AddAzureBlobServiceClient")]
    [InlineData("azure-data-tables", "entries", "AddTables", "AddAzureTableServiceClient")]
    public void StoragePropertiesDict_AllTypes_HaveCorrectMappings(
        string storageType,
        string expectedVariableName,
        string expectedAddMethod,
        string expectedAddClientMethod)
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue(storageType, out var properties);

        // Assert
        Assert.True(success);
        Assert.NotNull(properties);
        Assert.Equal(expectedVariableName, properties.VariableName);
        Assert.Equal(expectedAddMethod, properties.AddMethodName);
        Assert.Equal(expectedAddClientMethod, properties.AddClientMethodName);
    }

    [Fact]
    public void QueueProperties_HasCorrectValues()
    {
        // Act
        var props = StorageConstants.QueueProperties;

        // Assert
        Assert.Equal("queues", props.VariableName);
        Assert.Equal("AddQueues", props.AddMethodName);
        Assert.Equal("AddAzureQueueServiceClient", props.AddClientMethodName);
    }

    [Fact]
    public void BlobProperties_HasCorrectValues()
    {
        // Act
        var props = StorageConstants.BlobProperties;

        // Assert
        Assert.Equal("blobs", props.VariableName);
        Assert.Equal("AddBlobs", props.AddMethodName);
        Assert.Equal("AddAzureBlobServiceClient", props.AddClientMethodName);
    }

    [Fact]
    public void TableProperties_HasCorrectValues()
    {
        // Act
        var props = StorageConstants.TableProperties;

        // Assert
        Assert.Equal("entries", props.VariableName);
        Assert.Equal("AddTables", props.AddMethodName);
        Assert.Equal("AddAzureTableServiceClient", props.AddClientMethodName);
    }

    [Fact]
    public void StorageConstants_AllMethodNames_AreNotEmpty()
    {
        // Assert
        Assert.NotEmpty(StorageConstants.AddQueuesMethodName);
        Assert.NotEmpty(StorageConstants.AddBlobsMethodName);
        Assert.NotEmpty(StorageConstants.AddTablesMethodName);
        Assert.NotEmpty(StorageConstants.BlobsClientMethodName);
        Assert.NotEmpty(StorageConstants.QueuesClientMethodName);
        Assert.NotEmpty(StorageConstants.TablesClientMethodName);
    }

    [Fact]
    public void StorageConstants_AllVariableNames_AreNotEmpty()
    {
        // Assert
        Assert.NotEmpty(StorageConstants.QueuesVariableName);
        Assert.NotEmpty(StorageConstants.BlobsVariableName);
        Assert.NotEmpty(StorageConstants.TablesVariableName);
    }

    [Theory]
    [InlineData("azure-storage-queues")]
    [InlineData("azure-storage-blobs")]
    [InlineData("azure-data-tables")]
    public void StoragePropertiesDict_AllKnownTypes_HaveNonNullProperties(string storageType)
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue(storageType, out var properties);

        // Assert
        Assert.True(success);
        Assert.NotNull(properties);
        Assert.NotNull(properties.VariableName);
        Assert.NotNull(properties.AddMethodName);
        Assert.NotNull(properties.AddClientMethodName);
        Assert.NotEmpty(properties.VariableName);
        Assert.NotEmpty(properties.AddMethodName);
        Assert.NotEmpty(properties.AddClientMethodName);
    }

    [Fact]
    public void StoragePropertiesDict_InvalidType_ReturnsNull()
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue("invalid-storage-type", out var properties);

        // Assert
        Assert.False(success);
        Assert.Null(properties);
    }

    [Fact]
    public void StorageConstants_MethodNames_FollowNamingConvention()
    {
        // Assert - All method names should start with "Add"
        Assert.StartsWith("Add", StorageConstants.AddQueuesMethodName);
        Assert.StartsWith("Add", StorageConstants.AddBlobsMethodName);
        Assert.StartsWith("Add", StorageConstants.AddTablesMethodName);
        Assert.StartsWith("Add", StorageConstants.BlobsClientMethodName);
        Assert.StartsWith("Add", StorageConstants.QueuesClientMethodName);
        Assert.StartsWith("Add", StorageConstants.TablesClientMethodName);
    }

    [Fact]
    public void StorageConstants_ClientMethodNames_ContainServiceClient()
    {
        // Assert - All client method names should contain "ServiceClient"
        Assert.Contains("ServiceClient", StorageConstants.BlobsClientMethodName);
        Assert.Contains("ServiceClient", StorageConstants.QueuesClientMethodName);
        Assert.Contains("ServiceClient", StorageConstants.TablesClientMethodName);
    }

    [Theory]
    [InlineData("azure-storage-queues", "queues")]
    [InlineData("azure-storage-blobs", "blobs")]
    [InlineData("azure-data-tables", "entries")]
    public void StoragePropertiesDict_VariableNames_MatchConstantsForType(
        string storageType,
        string expectedVariableName)
    {
        // Act
        var dict = StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue(storageType, out var properties);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedVariableName, properties!.VariableName);
    }

    [Fact]
    public void StoragePropertiesDict_Keys_MatchStorageTypeCustomValues()
    {
        // Arrange
        var expectedKeys = new[] { "azure-storage-queues", "azure-storage-blobs", "azure-data-tables" };

        // Act
        var dict = StorageConstants.StoragePropertiesDict;

        // Assert
        Assert.Equal(expectedKeys.Length, dict.Keys.Count);
        foreach (var key in expectedKeys)
        {
            Assert.Contains(key, dict.Keys);
        }
    }
}

