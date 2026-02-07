// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire;

public class AspireCodeModificationConfigPackingTests
{
    private readonly string _buildOutputPath;
    private readonly string _aspireConfigsPath;

    public AspireCodeModificationConfigPackingTests()
    {
        // Get the path to the build output directory
        _buildOutputPath = AppContext.BaseDirectory;
        _aspireConfigsPath = Path.Combine(_buildOutputPath, "Aspire", "CodeModificationConfigs");
    }

    [Fact]
    public void BuildOutput_ContainsAspireCodeModificationConfigs()
    {
        // Assert
        Assert.True(Directory.Exists(_aspireConfigsPath), 
            $"Aspire CodeModificationConfigs directory should exist in build output at {_aspireConfigsPath}");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void BuildOutput_ContainsTfmDirectories(string tfm)
    {
        // Arrange
        var tfmPath = Path.Combine(_aspireConfigsPath, tfm);

        // Assert
        Assert.True(Directory.Exists(tfmPath), 
            $"TFM directory {tfm} should exist in build output at {tfmPath}");
    }

    [Theory]
    [InlineData("net8.0", "Caching")]
    [InlineData("net8.0", "Database")]
    [InlineData("net8.0", "Storage")]
    [InlineData("net9.0", "Caching")]
    [InlineData("net9.0", "Database")]
    [InlineData("net9.0", "Storage")]
    [InlineData("net10.0", "Caching")]
    [InlineData("net10.0", "Database")]
    [InlineData("net10.0", "Storage")]
    [InlineData("net11.0", "Caching")]
    [InlineData("net11.0", "Database")]
    [InlineData("net11.0", "Storage")]
    public void BuildOutput_ContainsCategoryDirectories(string tfm, string category)
    {
        // Arrange
        var categoryPath = Path.Combine(_aspireConfigsPath, tfm, category);

        // Assert
        Assert.True(Directory.Exists(categoryPath), 
            $"Category directory {category} should exist for {tfm} at {categoryPath}");
    }

    [Theory]
    [InlineData("net8.0", "Caching", "redis-apphost.json")]
    [InlineData("net8.0", "Caching", "redis-webapp.json")]
    [InlineData("net8.0", "Caching", "redis-webapp-oc.json")]
    [InlineData("net8.0", "Database", "db-apphost.json")]
    [InlineData("net8.0", "Database", "db-webapi.json")]
    [InlineData("net8.0", "Storage", "storage-apphost.json")]
    [InlineData("net8.0", "Storage", "storage-webapi.json")]
    [InlineData("net9.0", "Caching", "redis-apphost.json")]
    [InlineData("net9.0", "Database", "db-webapi.json")]
    [InlineData("net9.0", "Storage", "storage-apphost.json")]
    [InlineData("net10.0", "Caching", "redis-webapp-oc.json")]
    [InlineData("net10.0", "Database", "db-apphost.json")]
    [InlineData("net10.0", "Storage", "storage-webapi.json")]
    [InlineData("net11.0", "Caching", "redis-apphost.json")]
    [InlineData("net11.0", "Database", "db-webapi.json")]
    [InlineData("net11.0", "Storage", "storage-apphost.json")]
    public void BuildOutput_ContainsExpectedConfigFiles(string tfm, string category, string fileName)
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, tfm, category, fileName);

        // Assert
        Assert.True(File.Exists(filePath), 
            $"Config file {fileName} should exist in build output at {filePath}");
    }

    [Fact]
    public void BuildOutput_PreservesDirectoryStructure()
    {
        // Arrange
        var expectedStructure = new[]
        {
            Path.Combine("Aspire", "CodeModificationConfigs", "net8.0", "Caching"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net8.0", "Database"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net8.0", "Storage"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net9.0", "Caching"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net9.0", "Database"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net9.0", "Storage"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net10.0", "Caching"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net10.0", "Database"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net10.0", "Storage"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net11.0", "Caching"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net11.0", "Database"),
            Path.Combine("Aspire", "CodeModificationConfigs", "net11.0", "Storage")
        };

        // Act & Assert
        foreach (var expectedPath in expectedStructure)
        {
            var fullPath = Path.Combine(_buildOutputPath, expectedPath);
            Assert.True(Directory.Exists(fullPath), 
                $"Directory structure should be preserved: {expectedPath}");
        }
    }

    [Theory]
    [InlineData("net8.0", 7)]  // 3 caching + 2 database + 2 storage
    [InlineData("net9.0", 7)]
    [InlineData("net10.0", 7)]
    [InlineData("net11.0", 7)]
    public void BuildOutput_ContainsAllExpectedFiles(string tfm, int expectedFileCount)
    {
        // Arrange
        var tfmPath = Path.Combine(_aspireConfigsPath, tfm);
        if (!Directory.Exists(tfmPath))
        {
            Assert.Fail($"TFM directory {tfm} does not exist at {tfmPath}");
        }

        // Act
        var jsonFiles = Directory.GetFiles(tfmPath, "*.json", SearchOption.AllDirectories);

        // Assert
        Assert.Equal(expectedFileCount, jsonFiles.Length);
    }

    [Fact]
    public void BuildOutput_DoesNotContainUnexpectedFiles()
    {
        // Arrange
        if (!Directory.Exists(_aspireConfigsPath))
        {
            Assert.Fail($"Aspire CodeModificationConfigs directory does not exist at {_aspireConfigsPath}");
        }

        var allFiles = Directory.GetFiles(_aspireConfigsPath, "*.*", SearchOption.AllDirectories);
        var jsonFiles = allFiles.Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).ToList();
        var nonJsonFiles = allFiles.Except(jsonFiles).ToList();

        // Assert
        Assert.Empty(nonJsonFiles);
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void BuildOutput_FilesHaveCorrectPath(string tfm)
    {
        // Arrange
        var tfmPath = Path.Combine(_aspireConfigsPath, tfm);
        if (!Directory.Exists(tfmPath))
        {
            Assert.Fail($"TFM directory {tfm} does not exist");
        }

        var categories = new[] { "Caching", "Database", "Storage" };
        
        // Act & Assert
        foreach (var category in categories)
        {
            var categoryPath = Path.Combine(tfmPath, category);
            Assert.True(Directory.Exists(categoryPath), 
                $"Category {category} should exist for {tfm}");

            var files = Directory.GetFiles(categoryPath, "*.json");
            Assert.NotEmpty(files);

            foreach (var file in files)
            {
                // Verify the file path structure is correct
                var relativePath = Path.GetRelativePath(_aspireConfigsPath, file);
                var parts = relativePath.Split(Path.DirectorySeparatorChar);
                
                Assert.Equal(3, parts.Length); // tfm/category/file.json
                Assert.Equal(tfm, parts[0]);
                Assert.Equal(category, parts[1]);
                Assert.EndsWith(".json", parts[2]);
            }
        }
    }
}
