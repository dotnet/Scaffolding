// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Command;

/// <summary>
/// Unit tests for the aspire storage command, validating options, types, and command structure
/// based on 'dotnet scaffold aspire storage --help' output.
/// </summary>
public class AspireStorageCommandTests
{
    [Fact]
    public void StorageType_IsRequired_ReturnsTrue()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.True(option.Required);
    }

    [Fact]
    public void StorageType_HasCorrectCliOption()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.Equal("--type", option.CliOption);
    }

    [Fact]
    public void StorageType_HasCorrectDisplayName()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.Equal(AspireCliStrings.StorageTypeOption, option.DisplayName);
    }

    [Fact]
    public void StorageType_HasCorrectDescription()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.Equal(AspireCliStrings.StorageTypeDescription, option.Description);
    }

    [Fact]
    public void StorageType_UsesCustomPicker()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.Equal(Microsoft.DotNet.Scaffolding.Core.ComponentModel.InteractivePickerType.CustomPicker, option.PickerType);
    }

    [Fact]
    public void AppHostProject_IsRequired_ReturnsTrue()
    {
        // Act
        var option = AspireOptions.AppHostProject;

        // Assert
        Assert.True(option.Required);
    }

    [Fact]
    public void AppHostProject_HasCorrectCliOption()
    {
        // Act
        var option = AspireOptions.AppHostProject;

        // Assert
        Assert.Equal("--apphost-project", option.CliOption);
    }

    [Fact]
    public void Project_IsRequired_ReturnsTrue()
    {
        // Act
        var option = AspireOptions.Project;

        // Assert
        Assert.True(option.Required);
    }

    [Fact]
    public void Project_HasCorrectCliOption()
    {
        // Act
        var option = AspireOptions.Project;

        // Assert
        Assert.Equal("--project", option.CliOption);
    }

    [Fact]
    public void Prerelease_IsNotRequired_ReturnsFalse()
    {
        // Act
        var option = AspireOptions.Prerelease;

        // Assert
        Assert.False(option.Required);
    }

    [Fact]
    public void Prerelease_HasCorrectCliOption()
    {
        // Act
        var option = AspireOptions.Prerelease;

        // Assert
        Assert.Equal("--prerelease", option.CliOption);
    }

    [Fact]
    public void StorageType_SupportsAzureStorageQueues()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("azure-storage-queues", option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_SupportsAzureStorageBlobs()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("azure-storage-blobs", option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_SupportsAzureDataTables()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains("azure-data-tables", option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_HasExactlyThreeValidTypes()
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Equal(3, option.CustomPickerValues.Count());
    }

    [Theory]
    [InlineData("azure-storage-queues")]
    [InlineData("azure-storage-blobs")]
    [InlineData("azure-data-tables")]
    public void StorageType_AllValidTypes_AreIncluded(string storageType)
    {
        // Act
        var option = AspireOptions.StorageType;

        // Assert
        Assert.NotNull(option.CustomPickerValues);
        Assert.Contains(storageType, option.CustomPickerValues);
    }

    [Fact]
    public void StorageType_ValidValues_MatchExpectedOrder()
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
    public void StorageCliStrings_HasCorrectTitle()
    {
        // Assert
        Assert.Equal("storage", AspireCliStrings.StorageTitle);
    }

    [Fact]
    public void StorageCliStrings_HasCorrectDescription()
    {
        // Assert
        Assert.Equal("Modify Aspire project to make it storage ready.", AspireCliStrings.StorageDescription);
    }

    [Fact]
    public void StorageCliStrings_Example1_ContainsAzureStorageBlobs()
    {
        // Assert
        Assert.Contains("--type azure-storage-blobs", AspireCliStrings.StorageExample1);
        Assert.Contains("--apphost-project", AspireCliStrings.StorageExample1);
        Assert.Contains("--project", AspireCliStrings.StorageExample1);
    }

    [Fact]
    public void StorageCliStrings_Example2_ContainsAzureStorageQueues()
    {
        // Assert
        Assert.Contains("--type azure-storage-queues", AspireCliStrings.StorageExample2);
        Assert.Contains("--apphost-project", AspireCliStrings.StorageExample2);
        Assert.Contains("--project", AspireCliStrings.StorageExample2);
    }

    [Fact]
    public void StorageCliStrings_TypeCliOption_IsCorrect()
    {
        // Assert
        Assert.Equal("--type", AspireCliStrings.TypeCliOption);
    }

    [Fact]
    public void StorageCliStrings_AppHostCliOption_IsCorrect()
    {
        // Assert
        Assert.Equal("--apphost-project", AspireCliStrings.AppHostCliOption);
    }

    [Fact]
    public void StorageCliStrings_WorkerProjectCliOption_IsCorrect()
    {
        // Assert
        Assert.Equal("--project", AspireCliStrings.WorkerProjectCliOption);
    }

    [Fact]
    public void StorageCliStrings_PrereleaseCliOption_IsCorrect()
    {
        // Assert
        Assert.Equal("--prerelease", AspireCliStrings.PrereleaseCliOption);
    }

    [Fact]
    public void StorageConstants_HasCorrectQueuesVariableName()
    {
        // Assert
        Assert.Equal("queues", Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.QueuesVariableName);
    }

    [Fact]
    public void StorageConstants_HasCorrectBlobsVariableName()
    {
        // Assert
        Assert.Equal("blobs", Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.BlobsVariableName);
    }

    [Fact]
    public void StorageConstants_HasCorrectTablesVariableName()
    {
        // Assert
        Assert.Equal("entries", Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.TablesVariableName);
    }

    [Fact]
    public void StorageConstants_PropertiesDict_ContainsAllStorageTypes()
    {
        // Act
        var dict = Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.StoragePropertiesDict;

        // Assert
        Assert.Equal(3, dict.Count);
        Assert.True(dict.ContainsKey("azure-storage-queues"));
        Assert.True(dict.ContainsKey("azure-storage-blobs"));
        Assert.True(dict.ContainsKey("azure-data-tables"));
    }

    [Fact]
    public void StorageConstants_QueuesProperties_HasCorrectMethodNames()
    {
        // Act
        var props = Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.QueueProperties;

        // Assert
        Assert.Equal("queues", props.VariableName);
        Assert.Equal("AddQueues", props.AddMethodName);
        Assert.Equal("AddAzureQueueServiceClient", props.AddClientMethodName);
    }

    [Fact]
    public void StorageConstants_BlobsProperties_HasCorrectMethodNames()
    {
        // Act
        var props = Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.BlobProperties;

        // Assert
        Assert.Equal("blobs", props.VariableName);
        Assert.Equal("AddBlobs", props.AddMethodName);
        Assert.Equal("AddAzureBlobServiceClient", props.AddClientMethodName);
    }

    [Fact]
    public void StorageConstants_TablesProperties_HasCorrectMethodNames()
    {
        // Act
        var props = Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.TableProperties;

        // Assert
        Assert.Equal("entries", props.VariableName);
        Assert.Equal("AddTables", props.AddMethodName);
        Assert.Equal("AddAzureTableServiceClient", props.AddClientMethodName);
    }

    [Fact]
    public void StoragePropertiesDict_CanRetrieveQueuesProperties()
    {
        // Act
        var dict = Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue("azure-storage-queues", out var props);

        // Assert
        Assert.True(success);
        Assert.NotNull(props);
        Assert.Equal("queues", props.VariableName);
    }

    [Fact]
    public void StoragePropertiesDict_CanRetrieveBlobsProperties()
    {
        // Act
        var dict = Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue("azure-storage-blobs", out var props);

        // Assert
        Assert.True(success);
        Assert.NotNull(props);
        Assert.Equal("blobs", props.VariableName);
    }

    [Fact]
    public void StoragePropertiesDict_CanRetrieveTablesProperties()
    {
        // Act
        var dict = Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers.StorageConstants.StoragePropertiesDict;
        var success = dict.TryGetValue("azure-data-tables", out var props);

        // Assert
        Assert.True(success);
        Assert.NotNull(props);
        Assert.Equal("entries", props.VariableName);
    }
}
