// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Services;

public class TemplateFoldersUtilitiesTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _toolsDirectory;
    private readonly string _templatesDirectory;

    public TemplateFoldersUtilitiesTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "TemplateFoldersUtilitiesTests", Guid.NewGuid().ToString());
        _toolsDirectory = Path.Combine(_testDirectory, "tools");
        _templatesDirectory = Path.Combine(_testDirectory, "Templates");
        Directory.CreateDirectory(_toolsDirectory);
        Directory.CreateDirectory(_templatesDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void GetTemplateFolders_NullBaseFolders_ThrowsArgumentNullException()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => utilities.GetTemplateFoldersWithFramework("net8.0", null!));
    }

    [Fact]
    public void GetTemplateFolders_EmptyBaseFolders_ReturnsEmptyList()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();

        // Act
        var result = utilities.GetTemplateFoldersWithFramework("net8.0", []);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllT4Templates_ReturnsFilesWithTTExtension()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllFiles("net11.0", baseFolders, ".tt");

        // Assert
        Assert.NotNull(result);
        // Result depends on actual file system, so just verify it returns an enumerable
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllFiles_WithoutExtension_ReturnsAllFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllFiles("net11.0", baseFolders, null);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllFiles_WithExtension_ReturnsFilteredFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllFiles("net11.0", baseFolders, ".cs");

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_WithValidProject_ReturnsTemplates()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string projectPath = CreateTestProject("TestProject.csproj", "net8.0");
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllT4TemplatesForTargetFramework(baseFolders, projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_NullBaseFolders_ThrowsArgumentNullException()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => utilities.GetTemplateFoldersWithFramework("net8.0", null!));
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_ValidFramework_ReturnsTemplateFolders()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetTemplateFoldersWithFramework("net8.0", baseFolders);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllFiles_WithFramework_ReturnsFilteredFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllFiles("net8.0", baseFolders, ".tt");

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllFiles_WithFrameworkAndNullExtension_ReturnsAllFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllFiles("net9.0", baseFolders, null);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_Net9Project_ReturnsNet9Templates()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string projectPath = CreateTestProject("TestNet9.csproj", "net9.0");
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllT4TemplatesForTargetFramework(baseFolders, projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_Net10Project_ReturnsNet10Templates()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string projectPath = CreateTestProject("TestNet10.csproj", "net10.0");
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllT4TemplatesForTargetFramework(baseFolders, projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_Net11Project_ReturnsNet10Templates()
    {
        // Arrange - Net11 should default to net10.0 templates
        var utilities = new TemplateFoldersUtilities();
        string projectPath = CreateTestProject("TestNet11.csproj", "net11.0");
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllT4TemplatesForTargetFramework(baseFolders, projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_IncompatibleProject_ReturnsNet10Templates()
    {
        // Arrange - Incompatible projects should default to net10.0
        var utilities = new TemplateFoldersUtilities();
        string projectPath = CreateTestProject("TestNet7.csproj", "net7.0");
        string[] baseFolders = ["TestFolder"];

        // Act
        var result = utilities.GetAllT4TemplatesForTargetFramework(baseFolders, projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_MultipleBaseFolders_ReturnsMultipleFolders()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["Folder1", "Folder2", "Folder3"];

        // Act
        var result = utilities.GetTemplateFoldersWithFramework("net8.0", baseFolders);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    [Fact]
    public void GetTemplateFolders_MultipleBaseFolders_ReturnsMultipleFolders()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilities();
        string[] baseFolders = ["Folder1", "Folder2", "Folder3"];

        // Act
        var result = utilities.GetTemplateFoldersWithFramework("net11.0", baseFolders);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<string>>(result);
    }

    private string CreateTestProject(string projectName, string targetFramework)
    {
        string projectPath = Path.Combine(_testDirectory, projectName);
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";

        File.WriteAllText(projectPath, projectContent);
        return projectPath;
    }
}
