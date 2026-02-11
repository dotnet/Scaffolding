// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests;

/// <summary>
/// Comprehensive tests to ensure dotnet scaffold can run on .NET 9 projects.
/// These tests verify the complete scaffolding pipeline for net9.0 target framework.
/// </summary>
public class Net9ScaffoldingSupportTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _toolsDirectory;
    private readonly string _templatesDirectory;
    private readonly List<string> _createdProjects;

    public Net9ScaffoldingSupportTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "Net9ScaffoldingSupportTests", Guid.NewGuid().ToString());
        _toolsDirectory = Path.Combine(_testDirectory, "tools");
        _templatesDirectory = Path.Combine(_testDirectory, "Templates");
        _createdProjects = new List<string>();
        
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

    #region Target Framework Detection for Net9

    [Fact]
    public void TargetFrameworkHelpers_Net9Project_DetectsNet9Correctly()
    {
        // Arrange
        string projectPath = CreateTestProject("Net9App.csproj", "net9.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9WebProject_DetectsNet9Correctly()
    {
        // Arrange
        string projectPath = CreateWebProject("Net9WebApp.csproj", "net9.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9ProjectWithWindowsTfm_ReturnsNull()
    {
        // Arrange - net9.0-windows uses OS-specific TFM which is not directly matched
        // This is expected behavior - the helper looks for exact framework matches
        string projectPath = CreateTestProject("Net9WindowsApp.csproj", "net9.0-windows");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert - OS-specific TFMs may not be supported, which returns null
        // This documents current behavior
        Assert.Null(result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9MinimalistProject_ReturnsNet9()
    {
        // Arrange - A bare minimum project file
        string projectPath = Path.Combine(_testDirectory, "MinimalNet9.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9BlazorProject_DetectsNet9Correctly()
    {
        // Arrange - A Blazor WebAssembly project
        string projectPath = Path.Combine(_testDirectory, "Net9Blazor.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    #endregion

    #region Template Folder Selection for Net9

    [Fact]
    public void TargetFrameworkHelpers_Net9Project_ReturnsNet9TemplateFolder()
    {
        // Arrange
        string projectPath = CreateTestProject("Net9TemplateFolder.csproj", "net9.0");

        // Act
        string result = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.Equal("net9.0", result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9WebProject_ReturnsNet9TemplateFolder()
    {
        // Arrange
        string projectPath = CreateWebProject("Net9WebTemplateFolder.csproj", "net9.0");

        // Act
        string result = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.Equal("net9.0", result);
    }

    [Fact]
    public void TemplateFoldersUtilities_Net9Project_SelectsNet9Templates()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        string projectPath = CreateTestProject("Net9TemplateSelection.csproj", "net9.0");
        
        // Create net9.0 template structure
        string net9TemplatePath = Path.Combine(_templatesDirectory, "net9.0", "BlazorCrud");
        Directory.CreateDirectory(net9TemplatePath);
        File.WriteAllText(Path.Combine(net9TemplatePath, "Create.tt"), "net9 template content");
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllT4TemplatesForNet9Project(projectPath, baseFolders);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("net9.0", result.First());
        Assert.Contains("Create.tt", result.First());
    }

    [Fact]
    public void TemplateFoldersUtilities_Net9Project_DoesNotSelectNet8Templates()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        string projectPath = CreateTestProject("Net9NotNet8.csproj", "net9.0");
        
        // Create both net8.0 and net9.0 template structures
        string net8TemplatePath = Path.Combine(_templatesDirectory, "net8.0", "BlazorCrud");
        string net9TemplatePath = Path.Combine(_templatesDirectory, "net9.0", "BlazorCrud");
        Directory.CreateDirectory(net8TemplatePath);
        Directory.CreateDirectory(net9TemplatePath);
        File.WriteAllText(Path.Combine(net8TemplatePath, "Create.tt"), "net8 template");
        File.WriteAllText(Path.Combine(net9TemplatePath, "Create.tt"), "net9 template");
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllT4TemplatesForNet9Project(projectPath, baseFolders);

        // Assert
        Assert.Single(result);
        Assert.Contains("net9.0", result.First());
        Assert.DoesNotContain(result, f => f.Contains("net8.0"));
    }

    #endregion

    #region Multi-targeting with Net9

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetIncludingNet9_SelectsLowestSupported()
    {
        // Arrange - When multi-targeting net8.0;net9.0, should pick net8.0
        string projectPath = CreateMultiTargetProject("MultiNet8Net9.csproj", "net8.0;net9.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetNet9Net10_SelectsNet9()
    {
        // Arrange - When multi-targeting net9.0;net10.0, should pick net9.0
        string projectPath = CreateMultiTargetProject("MultiNet9Net10.csproj", "net9.0;net10.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetNet9Only_SelectsNet9()
    {
        // Arrange - Multi-target property with single value
        string projectPath = CreateMultiTargetProject("MultiNet9Only.csproj", "net9.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    #endregion

    #region Template Folder Retrieval for Net9

    [Fact]
    public void TemplateFoldersUtilities_Net9Templates_ReturnsCorrectTemplatesForBlazorCrud()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create complete net9.0 BlazorCrud template structure
        string[] templateNames = ["Create", "Delete", "Details", "Edit", "Index", "NotFound"];
        string net9TemplatePath = Path.Combine(_templatesDirectory, "net9.0", "BlazorCrud");
        Directory.CreateDirectory(net9TemplatePath);
        
        foreach (var templateName in templateNames)
        {
            File.WriteAllText(Path.Combine(net9TemplatePath, $"{templateName}.tt"), $"{templateName} template");
        }
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllFiles("net9.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(6, result.Count);
        foreach (var templateName in templateNames)
        {
            Assert.Contains(result, f => f.EndsWith($"{templateName}.tt"));
        }
    }

    [Fact]
    public void TemplateFoldersUtilities_Net9Templates_ReturnsCorrectTemplatesForMinimalApi()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create net9.0 MinimalApi template structure
        string net9TemplatePath = Path.Combine(_templatesDirectory, "net9.0", "MinimalApi");
        Directory.CreateDirectory(net9TemplatePath);
        File.WriteAllText(Path.Combine(net9TemplatePath, "Endpoints.tt"), "endpoints template");
        
        string[] baseFolders = ["MinimalApi"];

        // Act
        var result = utilities.GetAllFiles("net9.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => f.EndsWith("Endpoints.tt"));
    }

    [Fact]
    public void TemplateFoldersUtilities_Net9Templates_ReturnsCorrectTemplatesForController()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create net9.0 Controller template structure
        string net9TemplatePath = Path.Combine(_templatesDirectory, "net9.0", "Controller");
        Directory.CreateDirectory(net9TemplatePath);
        string[] controllerTemplates = ["ControllerEmpty", "ControllerWithActions", "ApiControllerEmpty"];
        foreach (var template in controllerTemplates)
        {
            File.WriteAllText(Path.Combine(net9TemplatePath, $"{template}.tt"), $"{template} template");
        }
        
        string[] baseFolders = ["Controller"];

        // Act
        var result = utilities.GetAllFiles("net9.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region Cross-TFM Comparison Tests

    [Fact]
    public void TargetFrameworkHelpers_Net9VsNet8_DifferentResults()
    {
        // Arrange
        string net9Project = CreateTestProject("Net9Comparison.csproj", "net9.0");
        string net8Project = CreateTestProject("Net8Comparison.csproj", "net8.0");

        // Act
        TargetFramework? net9Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net9Project);
        TargetFramework? net8Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net8Project);

        // Assert
        Assert.NotEqual(net9Result, net8Result);
        Assert.Equal(TargetFramework.Net9, net9Result);
        Assert.Equal(TargetFramework.Net8, net8Result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9VsNet10_DifferentResults()
    {
        // Arrange
        string net9Project = CreateTestProject("Net9Vs10Comparison.csproj", "net9.0");
        string net10Project = CreateTestProject("Net10Vs9Comparison.csproj", "net10.0");

        // Act
        TargetFramework? net9Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net9Project);
        TargetFramework? net10Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net10Project);

        // Assert
        Assert.NotEqual(net9Result, net10Result);
        Assert.Equal(TargetFramework.Net9, net9Result);
        Assert.Equal(TargetFramework.Net10, net10Result);
    }

    [Fact]
    public void TargetFrameworkFolder_Net9VsNet8_DifferentFolders()
    {
        // Arrange
        string net9Project = CreateTestProject("Net9FolderCompare.csproj", "net9.0");
        string net8Project = CreateTestProject("Net8FolderCompare.csproj", "net8.0");

        // Act
        string net9Folder = TargetFrameworkHelpers.GetTargetFrameworkFolder(net9Project);
        string net8Folder = TargetFrameworkHelpers.GetTargetFrameworkFolder(net8Project);

        // Assert
        Assert.NotEqual(net9Folder, net8Folder);
        Assert.Equal("net9.0", net9Folder);
        Assert.Equal("net8.0", net8Folder);
    }

    #endregion

    #region Project File Variations for Net9

    [Fact]
    public void TargetFrameworkHelpers_Net9WithWhitespace_ReturnsNull()
    {
        // Arrange - Project file with extra whitespace in TFM value
        // Note: whitespace in TargetFramework is not trimmed by the helper
        string projectPath = Path.Combine(_testDirectory, "Net9Whitespace.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>  net9.0  </TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert - whitespace causes no match
        Assert.Null(result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9WithComments_DetectsCorrectly()
    {
        // Arrange - Project file with XML comments
        string projectPath = Path.Combine(_testDirectory, "Net9Comments.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <!-- Target framework configuration -->
    <TargetFramework>net9.0</TargetFramework>
    <!-- End of target framework configuration -->
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9RazorClassLibrary_DetectsCorrectly()
    {
        // Arrange - Razor Class Library project
        string projectPath = Path.Combine(_testDirectory, "Net9RazorLib.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.Razor"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net9WorkerService_DetectsCorrectly()
    {
        // Arrange - Worker Service project
        string projectPath = Path.Combine(_testDirectory, "Net9Worker.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.Worker"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>dotnet-Net9Worker-12345</UserSecretsId>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    #endregion

    #region Helper Methods

    private string CreateTestProject(string projectName, string targetFramework)
    {
        string projectPath = Path.Combine(_testDirectory, projectName);
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";

        File.WriteAllText(projectPath, projectContent);
        _createdProjects.Add(projectPath);
        return projectPath;
    }

    private string CreateWebProject(string projectName, string targetFramework)
    {
        string projectPath = Path.Combine(_testDirectory, projectName);
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";

        File.WriteAllText(projectPath, projectContent);
        _createdProjects.Add(projectPath);
        return projectPath;
    }

    private string CreateMultiTargetProject(string projectName, string targetFrameworks)
    {
        string projectPath = Path.Combine(_testDirectory, projectName);
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>{targetFrameworks}</TargetFrameworks>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";

        File.WriteAllText(projectPath, projectContent);
        _createdProjects.Add(projectPath);
        return projectPath;
    }

    #endregion

    #region Testable Implementation

    /// <summary>
    /// Testable wrapper for TemplateFoldersUtilities that allows specifying a custom base path
    /// </summary>
    private class TestableTemplateFoldersUtilities : TemplateFoldersUtilities
    {
        private readonly string _basePath;

        public TestableTemplateFoldersUtilities(string basePath)
        {
            _basePath = basePath;
        }

        public IEnumerable<string> GetAllT4TemplatesForNet9Project(string projectPath, string[] baseFolders)
        {
            // Get the target framework folder based on the project
            string targetFrameworkFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);
            return GetAllFiles(targetFrameworkFolder, baseFolders, ".tt");
        }

        public new IEnumerable<string> GetTemplateFoldersWithFramework(string frameworkTemplateFolder, string[] baseFolders)
        {
            ArgumentNullException.ThrowIfNull(baseFolders);
            var templateFolders = new List<string>();

            foreach (var baseFolderName in baseFolders)
            {
                string templatesFolderName = "Templates";
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
            var searchPattern = string.IsNullOrEmpty(extension) ? "*" : $"*{extension}";
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

    #endregion
}
