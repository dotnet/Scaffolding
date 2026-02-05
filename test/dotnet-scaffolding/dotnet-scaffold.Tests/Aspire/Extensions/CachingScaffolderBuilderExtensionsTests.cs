// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Xunit;
using System.IO;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Extensions;

/// <summary>
/// Unit tests for CachingScaffolderBuilderExtensions to validate that caching-related
/// scaffolding steps are correctly configured and package constants are properly defined.
/// </summary>
public class CachingScaffolderBuilderExtensionsTests
{
    [Fact]
    public void CachingPackages_AppHostRedisPackage_HasCorrectName()
    {
        // Act
        var package = PackageConstants.CachingPackages.AppHostRedisPackage;

        // Assert
        Assert.NotNull(package);
        Assert.Equal("Aspire.Hosting.Redis", package.Name);
    }

    [Fact]
    public void CachingPackages_WebAppRedisPackage_HasCorrectName()
    {
        // Act
        var package = PackageConstants.CachingPackages.WebAppRedisPackage;

        // Assert
        Assert.NotNull(package);
        Assert.Equal("Aspire.StackExchange.Redis", package.Name);
    }

    [Fact]
    public void CachingPackages_WebAppRedisOutputCachingPackage_HasCorrectName()
    {
        // Act
        var package = PackageConstants.CachingPackages.WebAppRedisOutputCachingPackage;

        // Assert
        Assert.NotNull(package);
        Assert.Equal("Aspire.StackExchange.Redis.OutputCaching", package.Name);
    }

    [Fact]
    public void CachingPackagesDict_ContainsBothRedisTypes()
    {
        // Act
        var dict = PackageConstants.CachingPackages.CachingPackagesDict;

        // Assert
        Assert.Equal(2, dict.Count);
        Assert.True(dict.ContainsKey("redis"));
        Assert.True(dict.ContainsKey("redis-with-output-caching"));
    }

    [Fact]
    public void CachingPackagesDict_Redis_ReturnsCorrectPackage()
    {
        // Act
        var dict = PackageConstants.CachingPackages.CachingPackagesDict;
        var success = dict.TryGetValue("redis", out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.Equal("Aspire.StackExchange.Redis", package.Name);
    }

    [Fact]
    public void CachingPackagesDict_RedisWithOutputCaching_ReturnsCorrectPackage()
    {
        // Act
        var dict = PackageConstants.CachingPackages.CachingPackagesDict;
        var success = dict.TryGetValue("redis-with-output-caching", out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.Equal("Aspire.StackExchange.Redis.OutputCaching", package.Name);
    }

    [Theory]
    [InlineData("redis", "Aspire.StackExchange.Redis")]
    [InlineData("redis-with-output-caching", "Aspire.StackExchange.Redis.OutputCaching")]
    public void CachingPackagesDict_AllTypes_HaveCorrectPackageNames(string cacheType, string expectedPackageName)
    {
        // Act
        var dict = PackageConstants.CachingPackages.CachingPackagesDict;
        var success = dict.TryGetValue(cacheType, out var package);

        // Assert
        Assert.True(success);
        Assert.NotNull(package);
        Assert.Equal(expectedPackageName, package.Name);
    }

    [Fact]
    public void CachingPackagesDict_InvalidType_ReturnsNull()
    {
        // Act
        var dict = PackageConstants.CachingPackages.CachingPackagesDict;
        var success = dict.TryGetValue("invalid-cache-type", out var package);

        // Assert
        Assert.False(success);
        Assert.Null(package);
    }

    [Fact]
    public void CachingPackages_AllPackageNames_StartWithAspire()
    {
        // Assert
        Assert.StartsWith("Aspire.", PackageConstants.CachingPackages.AppHostRedisPackage.Name);
        Assert.StartsWith("Aspire.", PackageConstants.CachingPackages.WebAppRedisPackage.Name);
        Assert.StartsWith("Aspire.", PackageConstants.CachingPackages.WebAppRedisOutputCachingPackage.Name);
    }

    [Fact]
    public void CachingPackages_AllPackageNames_ContainRedis()
    {
        // Assert
        Assert.Contains("Redis", PackageConstants.CachingPackages.AppHostRedisPackage.Name);
        Assert.Contains("Redis", PackageConstants.CachingPackages.WebAppRedisPackage.Name);
        Assert.Contains("Redis", PackageConstants.CachingPackages.WebAppRedisOutputCachingPackage.Name);
    }

    [Fact]
    public void CachingPackages_OutputCachingPackage_ContainsOutputCaching()
    {
        // Assert
        Assert.Contains("OutputCaching", PackageConstants.CachingPackages.WebAppRedisOutputCachingPackage.Name);
    }

    [Fact]
    public void CachingPackagesDict_Keys_AreLowerCase()
    {
        // Act
        var dict = PackageConstants.CachingPackages.CachingPackagesDict;

        // Assert
        foreach (var key in dict.Keys)
        {
            Assert.Equal(key, key.ToLowerInvariant());
        }
    }

    [Fact]
    public void CachingPackagesDict_AllValues_HaveNonEmptyNames()
    {
        // Act
        var dict = PackageConstants.CachingPackages.CachingPackagesDict;

        // Assert
        foreach (var package in dict.Values)
        {
            Assert.NotNull(package);
            Assert.NotNull(package.Name);
            Assert.NotEmpty(package.Name);
        }
    }

    [Fact]
    public void RedisAppHostConfig_ExistsInCodeModificationConfigs()
    {
        // Arrange
        var expectedPath = Path.Combine("CodeModificationConfigs", "Caching", "redis-apphost.json");
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        
        // Navigate up to find the dotnet-scaffold project
        var currentDir = assemblyDir;
        while (currentDir != null && !Directory.Exists(Path.Combine(currentDir, "Aspire", "CodeModificationConfigs", "Caching")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        if (currentDir != null)
        {
            var configPath = Path.Combine(currentDir, "Aspire", "CodeModificationConfigs", "Caching", "redis-apphost.json");
            
            // Assert
            Assert.True(File.Exists(configPath), $"Expected config file to exist at: {configPath}");
        }
    }

    [Fact]
    public void RedisWebAppConfig_ExistsInCodeModificationConfigs()
    {
        // Arrange
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        
        // Navigate up to find the dotnet-scaffold project
        var currentDir = assemblyDir;
        while (currentDir != null && !Directory.Exists(Path.Combine(currentDir, "Aspire", "CodeModificationConfigs", "Caching")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        if (currentDir != null)
        {
            var configPath = Path.Combine(currentDir, "Aspire", "CodeModificationConfigs", "Caching", "redis-webapp.json");
            
            // Assert
            Assert.True(File.Exists(configPath), $"Expected config file to exist at: {configPath}");
        }
    }

    [Fact]
    public void RedisWebAppOutputCachingConfig_ExistsInCodeModificationConfigs()
    {
        // Arrange
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        
        // Navigate up to find the dotnet-scaffold project
        var currentDir = assemblyDir;
        while (currentDir != null && !Directory.Exists(Path.Combine(currentDir, "Aspire", "CodeModificationConfigs", "Caching")))
        {
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        if (currentDir != null)
        {
            var configPath = Path.Combine(currentDir, "Aspire", "CodeModificationConfigs", "Caching", "redis-webapp-oc.json");
            
            // Assert
            Assert.True(File.Exists(configPath), $"Expected config file to exist at: {configPath}");
        }
    }
}
