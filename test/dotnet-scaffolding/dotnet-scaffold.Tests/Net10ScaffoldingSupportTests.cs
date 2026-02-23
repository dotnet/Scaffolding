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
/// Comprehensive tests to ensure dotnet scaffold can run on .NET 10 projects.
/// These tests verify the complete scaffolding pipeline for net10.0 target framework.
/// </summary>
public class Net10ScaffoldingSupportTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _toolsDirectory;
    private readonly string _templatesDirectory;
    private readonly List<string> _createdProjects;

    public Net10ScaffoldingSupportTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "Net10ScaffoldingSupportTests", Guid.NewGuid().ToString());
        _toolsDirectory = Path.Combine(_testDirectory, "tools");
        _templatesDirectory = Path.Combine(_testDirectory, "AspNet", "Templates");
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

    #region Target Framework Detection for Net10

    [Fact]
    public void TargetFrameworkHelpers_Net10Project_DetectsNet10Correctly()
    {
        // Arrange
        string projectPath = CreateTestProject("Net10App.csproj", "net10.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net10WebProject_DetectsNet10Correctly()
    {
        // Arrange
        string projectPath = CreateWebProject("Net10WebApp.csproj", "net10.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net10ProjectWithWindowsTfm_ReturnsNull()
    {
        // Arrange - net10.0-windows uses OS-specific TFM which is not directly matched
        // This is expected behavior - the helper looks for exact framework matches
        string projectPath = CreateTestProject("Net10WindowsApp.csproj", "net10.0-windows");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert - OS-specific TFMs may not be supported, which returns null
        // This documents current behavior
        Assert.Null(result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net10MinimalistProject_ReturnsNet10()
    {
        // Arrange - A bare minimum project file
        string projectPath = Path.Combine(_testDirectory, "MinimalNet10.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    #endregion

    #region Template Folder Selection for Net10

    [Fact]
    public void TargetFrameworkHelpers_Net10Project_ReturnsNet10TemplateFolder()
    {
        // Arrange
        string projectPath = CreateTestProject("Net10TemplateFolder.csproj", "net10.0");

        // Act
        string result = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.Equal("net10.0", result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net10WebProject_ReturnsNet10TemplateFolder()
    {
        // Arrange
        string projectPath = CreateWebProject("Net10WebTemplateFolder.csproj", "net10.0");

        // Act
        string result = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.Equal("net10.0", result);
    }

    [Fact]
    public void TemplateFoldersUtilities_Net10Project_SelectsNet10Templates()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        string projectPath = CreateTestProject("Net10TemplateSelection.csproj", "net10.0");
        
        // Create net10.0 template structure
        string net10TemplatePath = Path.Combine(_templatesDirectory, "net10.0", "BlazorCrud");
        Directory.CreateDirectory(net10TemplatePath);
        File.WriteAllText(Path.Combine(net10TemplatePath, "Create.tt"), "net10 template content");
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllT4TemplatesForNet10Project(projectPath, baseFolders);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("net10.0", result.First());
        Assert.Contains("Create.tt", result.First());
    }

    [Fact]
    public void TemplateFoldersUtilities_Net10Project_DoesNotSelectNet9Templates()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        string projectPath = CreateTestProject("Net10NotNet9.csproj", "net10.0");
        
        // Create both net9.0 and net10.0 template structures
        string net9TemplatePath = Path.Combine(_templatesDirectory, "net9.0", "BlazorCrud");
        string net10TemplatePath = Path.Combine(_templatesDirectory, "net10.0", "BlazorCrud");
        Directory.CreateDirectory(net9TemplatePath);
        Directory.CreateDirectory(net10TemplatePath);
        File.WriteAllText(Path.Combine(net9TemplatePath, "Create.tt"), "net9 template");
        File.WriteAllText(Path.Combine(net10TemplatePath, "Create.tt"), "net10 template");
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllT4TemplatesForNet10Project(projectPath, baseFolders);

        // Assert
        Assert.Single(result);
        Assert.Contains("net10.0", result.First());
        Assert.DoesNotContain(result, f => f.Contains("net9.0"));
    }

    #endregion

    #region Multi-targeting with Net10

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetIncludingNet10_SelectsLowestSupported()
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
    public void TargetFrameworkHelpers_MultiTargetNet10Net11_SelectsNet10()
    {
        // Arrange - When multi-targeting net10.0;net11.0, should pick net10.0
        string projectPath = CreateMultiTargetProject("MultiNet10Net11.csproj", "net10.0;net11.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetNet8Net9Net10_SelectsNet8()
    {
        // Arrange
        string projectPath = CreateMultiTargetProject("MultiNet8Net9Net10.csproj", "net8.0;net9.0;net10.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetNet10Only_SelectsNet10()
    {
        // Arrange - Multi-target property with single value
        string projectPath = CreateMultiTargetProject("MultiNet10Only.csproj", "net10.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    #endregion

    #region Template Folder Retrieval for Net10

    [Fact]
    public void TemplateFoldersUtilities_Net10Templates_ReturnsCorrectTemplatesForBlazorCrud()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create complete net10.0 BlazorCrud template structure
        string[] templateNames = ["Create", "Delete", "Details", "Edit", "Index", "NotFound"];
        string net10TemplatePath = Path.Combine(_templatesDirectory, "net10.0", "BlazorCrud");
        Directory.CreateDirectory(net10TemplatePath);
        
        foreach (var templateName in templateNames)
        {
            File.WriteAllText(Path.Combine(net10TemplatePath, $"{templateName}.tt"), $"{templateName} template");
        }
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllFiles("net10.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(6, result.Count);
        foreach (var templateName in templateNames)
        {
            Assert.Contains(result, f => f.EndsWith($"{templateName}.tt"));
        }
    }

    [Fact]
    public void TemplateFoldersUtilities_Net10Templates_ReturnsCorrectTemplatesForMinimalApi()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create net10.0 MinimalApi template structure
        string net10TemplatePath = Path.Combine(_templatesDirectory, "net10.0", "MinimalApi");
        Directory.CreateDirectory(net10TemplatePath);
        File.WriteAllText(Path.Combine(net10TemplatePath, "Endpoints.tt"), "endpoints template");
        
        string[] baseFolders = ["MinimalApi"];

        // Act
        var result = utilities.GetAllFiles("net10.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains("Endpoints.tt", result.First());
    }

    [Fact]
    public void TemplateFoldersUtilities_Net10Templates_ReturnsCorrectTemplatesForRazorPages()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create net10.0 RazorPages template structure
        string[] templateNames = ["Create", "Delete", "Details", "Edit", "Index"];
        string net10TemplatePath = Path.Combine(_templatesDirectory, "net10.0", "RazorPages");
        Directory.CreateDirectory(net10TemplatePath);
        
        foreach (var templateName in templateNames)
        {
            File.WriteAllText(Path.Combine(net10TemplatePath, $"{templateName}.tt"), $"{templateName} template");
        }
        
        string[] baseFolders = ["RazorPages"];

        // Act
        var result = utilities.GetAllFiles("net10.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void TemplateFoldersUtilities_Net10Templates_SupportsNestedFolderStructure()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create nested structure like net10.0/Identity/Pages/Account/
        string identityPath = Path.Combine(_templatesDirectory, "net10.0", "Identity");
        string pagesPath = Path.Combine(identityPath, "Pages");
        string accountPath = Path.Combine(pagesPath, "Account");
        Directory.CreateDirectory(accountPath);
        
        File.WriteAllText(Path.Combine(identityPath, "_ViewStart.tt"), "viewstart");
        File.WriteAllText(Path.Combine(pagesPath, "Login.tt"), "login");
        File.WriteAllText(Path.Combine(accountPath, "Manage.tt"), "manage");
        
        string[] baseFolders = ["Identity"];

        // Act
        var result = utilities.GetAllFiles("net10.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.EndsWith("_ViewStart.tt"));
        Assert.Contains(result, f => f.EndsWith("Login.tt"));
        Assert.Contains(result, f => f.EndsWith("Manage.tt"));
    }

    #endregion

    #region Integration-style Tests for Net10 Support

    [Fact]
    public void Net10Project_FullScaffoldingPipeline_SelectsCorrectFramework()
    {
        // Arrange
        string projectPath = CreateTestProject("Net10FullPipeline.csproj", "net10.0");

        // Act - Simulate the scaffolding pipeline framework detection
        TargetFramework? detectedFramework = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);
        string templateFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.NotNull(detectedFramework);
        Assert.Equal(TargetFramework.Net10, detectedFramework);
        Assert.Equal("net10.0", templateFolder);
    }

    [Fact]
    public void Net10WebApiProject_FullScaffoldingPipeline_SelectsCorrectFramework()
    {
        // Arrange - Web API style project with additional properties
        string projectPath = Path.Combine(_testDirectory, "Net10WebApi.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.OpenApi"" Version=""10.0.0"" />
  </ItemGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? detectedFramework = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);
        string templateFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.NotNull(detectedFramework);
        Assert.Equal(TargetFramework.Net10, detectedFramework);
        Assert.Equal("net10.0", templateFolder);
    }

    [Fact]
    public void Net10BlazorProject_FullScaffoldingPipeline_SelectsCorrectFramework()
    {
        // Arrange - Blazor WebAssembly style project
        string projectPath = Path.Combine(_testDirectory, "Net10Blazor.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? detectedFramework = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);
        string templateFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.NotNull(detectedFramework);
        Assert.Equal(TargetFramework.Net10, detectedFramework);
        Assert.Equal("net10.0", templateFolder);
    }

    #endregion

    #region Edge Cases for Net10

    [Fact]
    public void Net10Project_WithExtraWhitespace_ReturnsNull()
    {
        // Arrange - TFM with extra whitespace is not trim-handled by the parser
        // This documents current behavior - whitespace in TFM not supported
        string projectPath = Path.Combine(_testDirectory, "Net10Whitespace.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>  net10.0  </TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert - Whitespace-padded TFM is not matched
        Assert.Null(result);
    }

    [Fact]
    public void Net10Project_WithUppercaseTfm_DetectsCorrectly()
    {
        // Arrange - Uppercase TFM (should still work)
        string projectPath = Path.Combine(_testDirectory, "Net10Uppercase.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>NET10.0</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    [Fact]
    public void Net10Project_WithMixedCase_DetectsCorrectly()
    {
        // Arrange
        string projectPath = Path.Combine(_testDirectory, "Net10MixedCase.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>Net10.0</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
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

        public IEnumerable<string> GetAllT4TemplatesForNet10Project(string projectPath, string[] baseFolders)
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
