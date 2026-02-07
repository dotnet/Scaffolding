// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Internal;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Internal.Tests;

public class GlobalToolFileFinderTests : IDisposable
{
    private readonly string _testRootDirectory;
    private readonly List<string> _createdDirectories;
    private readonly List<string> _createdFiles;

    public GlobalToolFileFinderTests()
    {
        _testRootDirectory = Path.Combine(Path.GetTempPath(), "GlobalToolFileFinderTests", Guid.NewGuid().ToString());
        _createdDirectories = new List<string>();
        _createdFiles = new List<string>();
        Directory.CreateDirectory(_testRootDirectory);
    }

    public void Dispose()
    {
        // Cleanup test directories and files
        if (Directory.Exists(_testRootDirectory))
        {
            try
            {
                Directory.Delete(_testRootDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithValidFileInNet11Folder_ReturnsFilePath()
    {
        // Arrange
        var (assembly, configFile) = SetupTestEnvironment("net11.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testConfig.json", assembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithValidFileInNet9Folder_ReturnsFilePath()
    {
        // Arrange
        var (assembly, configFile) = SetupTestEnvironment("net9.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testConfig.json", assembly, "net9.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithValidFileInNet8Folder_ReturnsFilePath()
    {
        // Arrange
        var (assembly, configFile) = SetupTestEnvironment("net8.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testConfig.json", assembly, "net8.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithValidFileInNet10Folder_ReturnsFilePath()
    {
        // Arrange
        var (assembly, configFile) = SetupTestEnvironment("net10.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testConfig.json", assembly, "net10.0");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithFileInSubdirectory_ReturnsFilePath()
    {
        // Arrange
        var (assembly, _) = SetupTestEnvironment("net11.0", null);
        var subfolder = Path.Combine(_testRootDirectory, "Aspnet", "CodeModificationConfigs", "net11.0", "subfolder");
        Directory.CreateDirectory(subfolder);
        _createdDirectories.Add(subfolder);
        
        var configFile = Path.Combine(subfolder, "nested.json");
        File.WriteAllText(configFile, "{}");
        _createdFiles.Add(configFile);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("nested.json", assembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithNullFileName_ReturnsNull()
    {
        // Arrange
        var (assembly, _) = SetupTestEnvironment("net11.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile(null!, assembly);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithEmptyFileName_ReturnsNull()
    {
        // Arrange
        var (assembly, _) = SetupTestEnvironment("net11.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile(string.Empty, assembly);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var (assembly, _) = SetupTestEnvironment("net11.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("nonExistent.json", assembly);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithoutToolsFolder_ReturnsNull()
    {
        // Arrange
        var assemblyDir = Path.Combine(_testRootDirectory, "notools");
        Directory.CreateDirectory(assemblyDir);
        _createdDirectories.Add(assemblyDir);
        
        var assembly = CreateMockAssembly(assemblyDir);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testConfig.json", assembly);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithoutConfigFolder_ReturnsNull()
    {
        // Arrange
        var toolsDir = Path.Combine(_testRootDirectory, "tools");
        Directory.CreateDirectory(toolsDir);
        _createdDirectories.Add(toolsDir);
        
        var assemblyDir = Path.Combine(_testRootDirectory, "assembly");
        Directory.CreateDirectory(assemblyDir);
        _createdDirectories.Add(assemblyDir);
        
        var assembly = CreateMockAssembly(assemblyDir);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testConfig.json", assembly);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithNullTargetFrameworkFolder_DefaultsToNet11()
    {
        // Arrange
        var (assembly, configFile) = SetupTestEnvironment("net11.0", "testConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testConfig.json", assembly, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithCaseInsensitiveFileName_ReturnsFilePath()
    {
        // Arrange
        var (assembly, configFile) = SetupTestEnvironment("net11.0", "TestConfig.json");

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("testconfig.json", assembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithMultipleFilesInSubdirectories_ReturnsFirstMatch()
    {
        // Arrange
        var (assembly, _) = SetupTestEnvironment("net11.0", null);
        
        var subfolder1 = Path.Combine(_testRootDirectory, "Aspnet", "CodeModificationConfigs", "net11.0", "folder1");
        Directory.CreateDirectory(subfolder1);
        _createdDirectories.Add(subfolder1);
        
        var configFile1 = Path.Combine(subfolder1, "config.json");
        File.WriteAllText(configFile1, "{}");
        _createdFiles.Add(configFile1);
        
        var subfolder2 = Path.Combine(_testRootDirectory, "Aspnet", "CodeModificationConfigs", "net11.0", "folder2");
        Directory.CreateDirectory(subfolder2);
        _createdDirectories.Add(subfolder2);
        
        var configFile2 = Path.Combine(subfolder2, "config.json");
        File.WriteAllText(configFile2, "{}");
        _createdFiles.Add(configFile2);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("config.json", assembly);

        // Assert
        Assert.NotNull(result);
        Assert.True(result == configFile1 || result == configFile2);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithDirectFileAndSubdirectoryFile_PrefersDirectFile()
    {
        // Arrange
        var (assembly, directFile) = SetupTestEnvironment("net11.0", "config.json");
        
        var subfolder = Path.Combine(_testRootDirectory, "Aspnet", "CodeModificationConfigs", "net11.0", "subfolder");
        Directory.CreateDirectory(subfolder);
        _createdDirectories.Add(subfolder);
        
        var nestedFile = Path.Combine(subfolder, "config.json");
        File.WriteAllText(nestedFile, "{}");
        _createdFiles.Add(nestedFile);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("config.json", assembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(directFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithAspireFolder_ReturnsFilePath()
    {
        // Arrange
        var toolsDir = Path.Combine(_testRootDirectory, "tools");
        var aspireDir = Path.Combine(_testRootDirectory, "Aspire");
        var tfmDir = Path.Combine(aspireDir, "net11.0");
        var configDir = Path.Combine(tfmDir, "CodeModificationConfigs");
        var subfolderDir = Path.Combine(configDir, "Database");
        
        Directory.CreateDirectory(toolsDir);
        Directory.CreateDirectory(subfolderDir);
        _createdDirectories.Add(toolsDir);
        _createdDirectories.Add(aspireDir);
        _createdDirectories.Add(tfmDir);
        _createdDirectories.Add(configDir);
        _createdDirectories.Add(subfolderDir);

        var configFile = Path.Combine(subfolderDir, "db-apphost.json");
        File.WriteAllText(configFile, "{}");
        _createdFiles.Add(configFile);

        var binDir = Path.Combine(_testRootDirectory, "bin");
        var assemblyDir = Path.Combine(binDir, "net11.0");
        Directory.CreateDirectory(assemblyDir);
        _createdDirectories.Add(binDir);
        _createdDirectories.Add(assemblyDir);

        var assembly = CreateMockAssembly(assemblyDir);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile(Path.Combine("Database", "db-apphost.json"), assembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_WithAspnetPriority_ReturnsAspnetFile()
    {
        // Arrange
        var (assembly, _) = SetupTestEnvironment("net11.0", null);
        
        // Create Aspnet config file
        var aspnetConfigDir = Path.Combine(_testRootDirectory, "Aspnet", "CodeModificationConfigs", "net11.0");
        Directory.CreateDirectory(aspnetConfigDir);
        var aspnetFile = Path.Combine(aspnetConfigDir, "config.json");
        File.WriteAllText(aspnetFile, "{}");
        _createdFiles.Add(aspnetFile);

        // Create Aspire config file (should be checked second)
        var aspireDir = Path.Combine(_testRootDirectory, "Aspire");
        var aspireTfmDir = Path.Combine(aspireDir, "net11.0");
        var aspireConfigDir = Path.Combine(aspireTfmDir, "CodeModificationConfigs");
        Directory.CreateDirectory(aspireConfigDir);
        _createdDirectories.Add(aspireDir);
        _createdDirectories.Add(aspireTfmDir);
        _createdDirectories.Add(aspireConfigDir);

        var aspireFile = Path.Combine(aspireConfigDir, "config.json");
        File.WriteAllText(aspireFile, "{}");
        _createdFiles.Add(aspireFile);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("config.json", assembly);

        // Assert - Should return Aspnet file (checked first)
        Assert.NotNull(result);
        Assert.Equal(aspnetFile, result);
    }

    [Fact]
    public void FindCodeModificationConfigFile_BackwardCompatibility_WithTemplatesFolder_ReturnsFilePath()
    {
        // Arrange - Create old Templates structure
        var toolsDir = Path.Combine(_testRootDirectory, "tools");
        var templatesDir = Path.Combine(_testRootDirectory, "Templates");
        var tfmDir = Path.Combine(templatesDir, "net11.0");
        var configDir = Path.Combine(tfmDir, "CodeModificationConfigs");
        
        Directory.CreateDirectory(toolsDir);
        Directory.CreateDirectory(configDir);
        _createdDirectories.Add(toolsDir);
        _createdDirectories.Add(templatesDir);
        _createdDirectories.Add(tfmDir);
        _createdDirectories.Add(configDir);

        var configFile = Path.Combine(configDir, "oldConfig.json");
        File.WriteAllText(configFile, "{}");
        _createdFiles.Add(configFile);

        var binDir = Path.Combine(_testRootDirectory, "bin");
        var assemblyDir = Path.Combine(binDir, "net11.0");
        Directory.CreateDirectory(assemblyDir);
        _createdDirectories.Add(binDir);
        _createdDirectories.Add(assemblyDir);

        var assembly = CreateMockAssembly(assemblyDir);

        // Act
        string? result = GlobalToolFileFinder.FindCodeModificationConfigFile("oldConfig.json", assembly);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile, result);
    }

    private (Assembly assembly, string configFile) SetupTestEnvironment(string targetFramework, string? fileName)
    {
        // Create directory structure: {testRoot}/tools (marker) and {testRoot}/Aspnet/CodeModificationConfigs/{tfm}
        // and {testRoot}/bin/assembly for the mock assembly location
        var toolsDir = Path.Combine(_testRootDirectory, "tools");
        var aspnetDir = Path.Combine(_testRootDirectory, "Aspnet");
        var aspnetConfigDir = Path.Combine(aspnetDir, "CodeModificationConfigs");
        var configDir = Path.Combine(aspnetConfigDir, targetFramework);
        
        Directory.CreateDirectory(toolsDir);
        Directory.CreateDirectory(configDir);
        _createdDirectories.Add(toolsDir);
        _createdDirectories.Add(aspnetDir);
        _createdDirectories.Add(aspnetConfigDir);
        _createdDirectories.Add(configDir);

        // Create the config file if fileName is provided
        string configFile = string.Empty;
        if (!string.IsNullOrEmpty(fileName))
        {
            configFile = Path.Combine(configDir, fileName);
            File.WriteAllText(configFile, "{}");
            _createdFiles.Add(configFile);
        }

        // Create assembly directory - multiple levels deep to simulate real structure
        var binDir = Path.Combine(_testRootDirectory, "bin");
        var assemblyDir = Path.Combine(binDir, "net11.0");
        Directory.CreateDirectory(assemblyDir);
        _createdDirectories.Add(binDir);
        _createdDirectories.Add(assemblyDir);

        var assembly = CreateMockAssembly(assemblyDir);
        return (assembly, configFile);
    }

    private Assembly CreateMockAssembly(string assemblyDirectory)
    {
        // For testing, we'll use a simple wrapper that returns the desired location
        // The actual Assembly methods won't work, but Location will
        return new TestAssemblyWrapper(Path.Combine(assemblyDirectory, "TestAssembly.dll"));
    }

    // Minimal wrapper to provide Location property for testing
    private class TestAssemblyWrapper : Assembly
    {
        private readonly string _location;

        public TestAssemblyWrapper(string location)
        {
            _location = location;
        }

        public override string Location => _location;
    }
}
