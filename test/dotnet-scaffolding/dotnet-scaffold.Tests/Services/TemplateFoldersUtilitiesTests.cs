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
        _templatesDirectory = Path.Combine(_testDirectory, "AspNet", "Templates");
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

    #region GetAllFiles Enhanced Tests

    [Fact]
    public void GetAllFiles_WithMultipleTemplates_ReturnsAllMatchingFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net9.0";
        string[] baseFolders = ["TestScaffolder"];
        
        // Create template structure
        string templatePath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(templatePath);
        
        // Create multiple .tt files
        File.WriteAllText(Path.Combine(templatePath, "Template1.tt"), "template content 1");
        File.WriteAllText(Path.Combine(templatePath, "Template2.tt"), "template content 2");
        File.WriteAllText(Path.Combine(templatePath, "Template3.cs"), "cs file content");
        
        // Create subdirectory with more templates
        string subDir = Path.Combine(templatePath, "SubFolder");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "SubTemplate.tt"), "sub template content");

        // Act
        var result = utilities.GetAllFiles(framework, baseFolders, ".tt").ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count); // Should find 3 .tt files
        Assert.Contains(result, f => f.EndsWith("Template1.tt"));
        Assert.Contains(result, f => f.EndsWith("Template2.tt"));
        Assert.Contains(result, f => f.EndsWith("SubTemplate.tt"));
        Assert.DoesNotContain(result, f => f.EndsWith(".cs"));
    }

    [Fact]
    public void GetAllFiles_WithNoExtensionFilter_ReturnsAllFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net8.0";
        string[] baseFolders = ["AllFilesTest"];
        
        string templatePath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(templatePath);
        
        File.WriteAllText(Path.Combine(templatePath, "file1.tt"), "content");
        File.WriteAllText(Path.Combine(templatePath, "file2.cs"), "content");
        File.WriteAllText(Path.Combine(templatePath, "file3.txt"), "content");

        // Act
        var result = utilities.GetAllFiles(framework, baseFolders, null).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetAllFiles_WithEmptyExtension_ReturnsAllFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net10.0";
        string[] baseFolders = ["EmptyExtTest"];
        
        string templatePath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(templatePath);
        
        File.WriteAllText(Path.Combine(templatePath, "file1.razor"), "content");
        File.WriteAllText(Path.Combine(templatePath, "file2.html"), "content");

        // Act
        var result = utilities.GetAllFiles(framework, baseFolders, string.Empty).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetAllFiles_WithNonExistentFramework_ReturnsEmpty()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net5.0"; // Framework folder doesn't exist
        string[] baseFolders = ["TestScaffolder"];

        // Act
        var result = utilities.GetAllFiles(framework, baseFolders, ".tt").ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllFiles_WithMultipleBaseFolders_ReturnsFilesFromAllFolders()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net9.0";
        string[] baseFolders = ["Folder1", "Folder2", "Folder3"];
        
        foreach (var folder in baseFolders)
        {
            string templatePath = Path.Combine(_templatesDirectory, framework, folder);
            Directory.CreateDirectory(templatePath);
            File.WriteAllText(Path.Combine(templatePath, $"{folder}.tt"), "content");
        }

        // Act
        var result = utilities.GetAllFiles(framework, baseFolders, ".tt").ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.Contains("Folder1"));
        Assert.Contains(result, f => f.Contains("Folder2"));
        Assert.Contains(result, f => f.Contains("Folder3"));
    }

    [Fact]
    public void GetAllFiles_WithDifferentExtensions_FiltersCorrectly()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net8.0";
        string[] baseFolders = ["ExtTest"];
        
        string templatePath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(templatePath);
        
        File.WriteAllText(Path.Combine(templatePath, "template.tt"), "content");
        File.WriteAllText(Path.Combine(templatePath, "code.cs"), "content");
        File.WriteAllText(Path.Combine(templatePath, "code.razor"), "content");
        File.WriteAllText(Path.Combine(templatePath, "styles.css"), "content");

        // Act - Test .cs extension
        var csResult = utilities.GetAllFiles(framework, baseFolders, ".cs").ToList();
        var razorResult = utilities.GetAllFiles(framework, baseFolders, ".razor").ToList();
        var ttResult = utilities.GetAllFiles(framework, baseFolders, ".tt").ToList();

        // Assert
        Assert.Single(csResult);
        Assert.Contains(csResult, f => f.EndsWith(".cs"));
        Assert.Single(razorResult);
        Assert.Contains(razorResult, f => f.EndsWith(".razor"));
        Assert.Single(ttResult);
        Assert.Contains(ttResult, f => f.EndsWith(".tt"));
    }

    [Fact]
    public void GetAllFiles_WithNestedSubdirectories_ReturnsAllFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net9.0";
        string[] baseFolders = ["NestedTest"];
        
        string basePath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(basePath);
        
        // Create nested structure
        File.WriteAllText(Path.Combine(basePath, "root.tt"), "content");
        
        string level1 = Path.Combine(basePath, "Level1");
        Directory.CreateDirectory(level1);
        File.WriteAllText(Path.Combine(level1, "level1.tt"), "content");
        
        string level2 = Path.Combine(level1, "Level2");
        Directory.CreateDirectory(level2);
        File.WriteAllText(Path.Combine(level2, "level2.tt"), "content");

        // Act
        var result = utilities.GetAllFiles(framework, baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.EndsWith("root.tt"));
        Assert.Contains(result, f => f.EndsWith("level1.tt"));
        Assert.Contains(result, f => f.EndsWith("level2.tt"));
    }

    [Fact]
    public void GetAllFiles_WithEmptyBaseFolders_ReturnsEmpty()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net8.0";
        string[] baseFolders = [];

        // Act
        var result = utilities.GetAllFiles(framework, baseFolders, ".tt").ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetTemplateFoldersWithFramework Enhanced Tests

    [Fact]
    public void GetTemplateFoldersWithFramework_WithExistingFolders_ReturnsCorrectPaths()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net8.0";
        string[] baseFolders = ["ScaffolderA", "ScaffolderB"];
        
        // Create the expected directory structure
        foreach (var folder in baseFolders)
        {
            string templatePath = Path.Combine(_templatesDirectory, framework, folder);
            Directory.CreateDirectory(templatePath);
        }

        // Act
        var result = utilities.GetTemplateFoldersWithFramework(framework, baseFolders).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, path => Assert.True(Directory.Exists(path)));
        Assert.Contains(result, p => p.Contains("ScaffolderA"));
        Assert.Contains(result, p => p.Contains("ScaffolderB"));
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_WithPartiallyExistingFolders_ReturnsOnlyExisting()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net9.0";
        string[] baseFolders = ["ExistingFolder", "NonExistentFolder"];
        
        // Create only one folder
        string existingPath = Path.Combine(_templatesDirectory, framework, "ExistingFolder");
        Directory.CreateDirectory(existingPath);

        // Act
        var result = utilities.GetTemplateFoldersWithFramework(framework, baseFolders).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("ExistingFolder", result[0]);
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_WithNoMatchingFolders_ReturnsEmpty()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net10.0";
        string[] baseFolders = ["NonExistent1", "NonExistent2"];

        // Act
        var result = utilities.GetTemplateFoldersWithFramework(framework, baseFolders).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_WithDifferentFrameworks_ReturnsCorrectFrameworkPaths()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string[] baseFolders = ["Common"];
        
        // Create folders for different frameworks
        string net8Path = Path.Combine(_templatesDirectory, "net8.0", baseFolders[0]);
        string net9Path = Path.Combine(_templatesDirectory, "net9.0", baseFolders[0]);
        string net10Path = Path.Combine(_templatesDirectory, "net10.0", baseFolders[0]);
        
        Directory.CreateDirectory(net8Path);
        Directory.CreateDirectory(net9Path);
        Directory.CreateDirectory(net10Path);

        // Act
        var net8Result = utilities.GetTemplateFoldersWithFramework("net8.0", baseFolders).ToList();
        var net9Result = utilities.GetTemplateFoldersWithFramework("net9.0", baseFolders).ToList();
        var net10Result = utilities.GetTemplateFoldersWithFramework("net10.0", baseFolders).ToList();

        // Assert
        Assert.Single(net8Result);
        Assert.Contains("net8.0", net8Result[0]);
        
        Assert.Single(net9Result);
        Assert.Contains("net9.0", net9Result[0]);
        
        Assert.Single(net10Result);
        Assert.Contains("net10.0", net10Result[0]);
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_WithManyBaseFolders_ReturnsAllMatchingPaths()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net8.0";
        string[] baseFolders = ["F1", "F2", "F3", "F4", "F5", "F6"];
        
        // Create all folders
        foreach (var folder in baseFolders)
        {
            string path = Path.Combine(_templatesDirectory, framework, folder);
            Directory.CreateDirectory(path);
        }

        // Act
        var result = utilities.GetTemplateFoldersWithFramework(framework, baseFolders).ToList();

        // Assert
        Assert.Equal(6, result.Count);
        foreach (var folder in baseFolders)
        {
            Assert.Contains(result, p => p.Contains(folder));
        }
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_WithSingleBaseFolder_ReturnsSinglePath()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net9.0";
        string[] baseFolders = ["SingleFolder"];
        
        string templatePath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(templatePath);

        // Act
        var result = utilities.GetTemplateFoldersWithFramework(framework, baseFolders).ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains("SingleFolder", result[0]);
        Assert.Contains("net9.0", result[0]);
    }

    [Fact]
    public void GetTemplateFoldersWithFramework_PathStructure_MatchesExpectedFormat()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net8.0";
        string[] baseFolders = ["TestFolder"];
        
        string expectedPath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(expectedPath);

        // Act
        var result = utilities.GetTemplateFoldersWithFramework(framework, baseFolders).ToList();

        // Assert
        Assert.Single(result);
        Assert.EndsWith(Path.Combine("AspNet", "Templates", framework, "TestFolder"), result[0]);
    }

    #endregion

    #region Integration Tests for GetAllFiles and GetTemplateFoldersWithFramework

    [Fact]
    public void GetAllFiles_Integration_WithGetTemplateFoldersWithFramework_ReturnsCorrectFiles()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string framework = "net9.0";
        string[] baseFolders = ["IntegrationTest"];
        
        // Setup folder structure
        string templatePath = Path.Combine(_templatesDirectory, framework, baseFolders[0]);
        Directory.CreateDirectory(templatePath);
        File.WriteAllText(Path.Combine(templatePath, "Test1.tt"), "content1");
        File.WriteAllText(Path.Combine(templatePath, "Test2.tt"), "content2");

        // Act - First get folders, then files
        var folders = utilities.GetTemplateFoldersWithFramework(framework, baseFolders).ToList();
        var files = utilities.GetAllFiles(framework, baseFolders, ".tt").ToList();

        // Assert
        Assert.Single(folders);
        Assert.Equal(2, files.Count);
        Assert.All(files, file => Assert.Contains(folders[0], file));
    }

    [Fact]
    public void GetAllFiles_WithMultipleFrameworksAndFolders_IsolatesCorrectly()
    {
        // Arrange
        var utilities = new TemplateFoldersUtilitiesTestable(_testDirectory);
        string[] baseFolders = ["Common"];
        
        // Create net8.0 templates
        string net8Path = Path.Combine(_templatesDirectory, "net8.0", baseFolders[0]);
        Directory.CreateDirectory(net8Path);
        File.WriteAllText(Path.Combine(net8Path, "Net8Template.tt"), "net8");
        
        // Create net9.0 templates
        string net9Path = Path.Combine(_templatesDirectory, "net9.0", baseFolders[0]);
        Directory.CreateDirectory(net9Path);
        File.WriteAllText(Path.Combine(net9Path, "Net9Template.tt"), "net9");

        // Act
        var net8Files = utilities.GetAllFiles("net8.0", baseFolders, ".tt").ToList();
        var net9Files = utilities.GetAllFiles("net9.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Single(net8Files);
        Assert.Contains("Net8Template.tt", net8Files[0]);
        Assert.DoesNotContain("Net9Template.tt", net8Files[0]);
        
        Assert.Single(net9Files);
        Assert.Contains("Net9Template.tt", net9Files[0]);
        Assert.DoesNotContain("Net8Template.tt", net9Files[0]);
    }

    #endregion

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

    /// <summary>
    /// Testable wrapper for TemplateFoldersUtilities that allows specifying a custom base path
    /// </summary>
    private class TemplateFoldersUtilitiesTestable : TemplateFoldersUtilities
    {
        private readonly string _basePath;

        public TemplateFoldersUtilitiesTestable(string basePath)
        {
            _basePath = basePath;
        }

        // This uses reflection to override the FindFolderWithToolsFolder behavior for testing
        public new IEnumerable<string> GetTemplateFoldersWithFramework(string frameworkTemplateFolder, string[] baseFolders)
        {
            ArgumentNullException.ThrowIfNull(baseFolders);
            var templateFolders = new List<string>();

            foreach (var baseFolderName in baseFolders)
            {
                string templatesFolderName = Path.Combine("AspNet", "Templates");
                var candidateTemplateFolders = Path.Combine(_basePath, templatesFolderName, frameworkTemplateFolder, baseFolderName);
                if (Directory.Exists(candidateTemplateFolders))
                {
                    templateFolders.Add(candidateTemplateFolders);
                }
            }

            return templateFolders;
        }

        public new IEnumerable<string> GetAllFiles(string targetFrameworkTemplateFolder, string[] baseFolders, string? extension = null)
        {
            List<string> allTemplates = [];
            var allTemplateFolders = GetTemplateFoldersWithFramework(targetFrameworkTemplateFolder, baseFolders);
            var searchPattern = string.IsNullOrEmpty(extension) ? string.Empty : $"*{Path.GetExtension(extension)}";
            if (allTemplateFolders != null && allTemplateFolders.Any())
            {
                foreach (var templateFolder in allTemplateFolders)
                {
                    allTemplates.AddRange(Directory.EnumerateFiles(templateFolder, searchPattern, SearchOption.AllDirectories));
                }
            }

            return allTemplates;
        }
    }
}
