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
/// Comprehensive tests to ensure dotnet scaffold can run on .NET 8 projects.
/// These tests verify the complete scaffolding pipeline for net8.0 target framework.
/// </summary>
public class Net8ScaffoldingSupportTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _toolsDirectory;
    private readonly string _templatesDirectory;
    private readonly List<string> _createdProjects;

    public Net8ScaffoldingSupportTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "Net8ScaffoldingSupportTests", Guid.NewGuid().ToString());
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

    #region Target Framework Detection for Net8

    [Fact]
    public void TargetFrameworkHelpers_Net8Project_DetectsNet8Correctly()
    {
        // Arrange
        string projectPath = CreateTestProject("Net8App.csproj", "net8.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8WebProject_DetectsNet8Correctly()
    {
        // Arrange
        string projectPath = CreateWebProject("Net8WebApp.csproj", "net8.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8MinimalistProject_ReturnsNet8()
    {
        // Arrange - A bare minimum project file
        string projectPath = Path.Combine(_testDirectory, "MinimalNet8.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8BlazorProject_DetectsNet8Correctly()
    {
        // Arrange - A Blazor WebAssembly project
        string projectPath = Path.Combine(_testDirectory, "Net8Blazor.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
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
        Assert.Equal(TargetFramework.Net8, result);
    }

    #endregion

    #region Template Folder Selection for Net8

    [Fact]
    public void TargetFrameworkHelpers_Net8Project_ReturnsNet8TemplateFolder()
    {
        // Arrange
        string projectPath = CreateTestProject("Net8TemplateFolder.csproj", "net8.0");

        // Act
        string result = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.Equal("net8.0", result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8WebProject_ReturnsNet8TemplateFolder()
    {
        // Arrange
        string projectPath = CreateWebProject("Net8WebTemplateFolder.csproj", "net8.0");

        // Act
        string result = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);

        // Assert
        Assert.Equal("net8.0", result);
    }

    [Fact]
    public void TemplateFoldersUtilities_Net8Project_SelectsNet8Templates()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        string projectPath = CreateTestProject("Net8TemplateSelection.csproj", "net8.0");
        
        // Create net8.0 template structure
        string net8TemplatePath = Path.Combine(_templatesDirectory, "net8.0", "BlazorCrud");
        Directory.CreateDirectory(net8TemplatePath);
        File.WriteAllText(Path.Combine(net8TemplatePath, "Create.tt"), "net8 template content");
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllT4TemplatesForNet8Project(projectPath, baseFolders);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("net8.0", result.First());
        Assert.Contains("Create.tt", result.First());
    }

    [Fact]
    public void TemplateFoldersUtilities_Net8Project_DoesNotSelectNet9Templates()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        string projectPath = CreateTestProject("Net8NotNet9.csproj", "net8.0");
        
        // Create both net8.0 and net9.0 template structures
        string net8TemplatePath = Path.Combine(_templatesDirectory, "net8.0", "BlazorCrud");
        string net9TemplatePath = Path.Combine(_templatesDirectory, "net9.0", "BlazorCrud");
        Directory.CreateDirectory(net8TemplatePath);
        Directory.CreateDirectory(net9TemplatePath);
        File.WriteAllText(Path.Combine(net8TemplatePath, "Create.tt"), "net8 template");
        File.WriteAllText(Path.Combine(net9TemplatePath, "Create.tt"), "net9 template");
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllT4TemplatesForNet8Project(projectPath, baseFolders);

        // Assert
        Assert.Single(result);
        Assert.Contains("net8.0", result.First());
        Assert.DoesNotContain(result, f => f.Contains("net9.0"));
    }

    #endregion

    #region Multi-targeting with Net8

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetIncludingNet8_SelectsNet8()
    {
        // Arrange - When multi-targeting net8.0;net9.0, should pick net8.0 (lowest)
        string projectPath = CreateMultiTargetProject("MultiNet8Net9.csproj", "net8.0;net9.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetNet8Net9Net10_SelectsNet8()
    {
        // Arrange - When multi-targeting net8.0;net9.0;net10.0, should pick net8.0
        string projectPath = CreateMultiTargetProject("MultiNet8Net9Net10.csproj", "net8.0;net9.0;net10.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_MultiTargetNet8Only_SelectsNet8()
    {
        // Arrange - Multi-target property with single value
        string projectPath = CreateMultiTargetProject("MultiNet8Only.csproj", "net8.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    #endregion

    #region Template Folder Retrieval for Net8

    [Fact]
    public void TemplateFoldersUtilities_Net8Templates_ReturnsCorrectTemplatesForBlazorCrud()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create complete net8.0 BlazorCrud template structure
        string[] templateNames = ["Create", "Delete", "Details", "Edit", "Index", "NotFound"];
        string net8TemplatePath = Path.Combine(_templatesDirectory, "net8.0", "BlazorCrud");
        Directory.CreateDirectory(net8TemplatePath);
        
        foreach (var templateName in templateNames)
        {
            File.WriteAllText(Path.Combine(net8TemplatePath, $"{templateName}.tt"), $"{templateName} template");
        }
        
        string[] baseFolders = ["BlazorCrud"];

        // Act
        var result = utilities.GetAllFiles("net8.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(6, result.Count);
        foreach (var templateName in templateNames)
        {
            Assert.Contains(result, f => f.EndsWith($"{templateName}.tt"));
        }
    }

    [Fact]
    public void TemplateFoldersUtilities_Net8Templates_ReturnsCorrectTemplatesForMinimalApi()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create net8.0 MinimalApi template structure
        string net8TemplatePath = Path.Combine(_templatesDirectory, "net8.0", "MinimalApi");
        Directory.CreateDirectory(net8TemplatePath);
        File.WriteAllText(Path.Combine(net8TemplatePath, "Endpoints.tt"), "endpoints template");
        
        string[] baseFolders = ["MinimalApi"];

        // Act
        var result = utilities.GetAllFiles("net8.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => f.EndsWith("Endpoints.tt"));
    }

    [Fact]
    public void TemplateFoldersUtilities_Net8Templates_ReturnsCorrectTemplatesForController()
    {
        // Arrange
        var utilities = new TestableTemplateFoldersUtilities(_testDirectory);
        
        // Create net8.0 Controller template structure
        string net8TemplatePath = Path.Combine(_templatesDirectory, "net8.0", "Controller");
        Directory.CreateDirectory(net8TemplatePath);
        string[] controllerTemplates = ["ControllerEmpty", "ControllerWithActions", "ApiControllerEmpty"];
        foreach (var template in controllerTemplates)
        {
            File.WriteAllText(Path.Combine(net8TemplatePath, $"{template}.tt"), $"{template} template");
        }
        
        string[] baseFolders = ["Controller"];

        // Act
        var result = utilities.GetAllFiles("net8.0", baseFolders, ".tt").ToList();

        // Assert
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region Cross-TFM Comparison Tests

    [Fact]
    public void TargetFrameworkHelpers_Net8VsNet9_DifferentResults()
    {
        // Arrange
        string net8Project = CreateTestProject("Net8Comparison.csproj", "net8.0");
        string net9Project = CreateTestProject("Net9Comparison.csproj", "net9.0");

        // Act
        TargetFramework? net8Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net8Project);
        TargetFramework? net9Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net9Project);

        // Assert
        Assert.NotEqual(net8Result, net9Result);
        Assert.Equal(TargetFramework.Net8, net8Result);
        Assert.Equal(TargetFramework.Net9, net9Result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8VsNet10_DifferentResults()
    {
        // Arrange
        string net8Project = CreateTestProject("Net8Vs10Comparison.csproj", "net8.0");
        string net10Project = CreateTestProject("Net10Vs8Comparison.csproj", "net10.0");

        // Act
        TargetFramework? net8Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net8Project);
        TargetFramework? net10Result = TargetFrameworkHelpers.GetTargetFrameworkForProject(net10Project);

        // Assert
        Assert.NotEqual(net8Result, net10Result);
        Assert.Equal(TargetFramework.Net8, net8Result);
        Assert.Equal(TargetFramework.Net10, net10Result);
    }

    [Fact]
    public void TargetFrameworkFolder_Net8VsNet9_DifferentFolders()
    {
        // Arrange
        string net8Project = CreateTestProject("Net8FolderCompare.csproj", "net8.0");
        string net9Project = CreateTestProject("Net9FolderCompare.csproj", "net9.0");

        // Act
        string net8Folder = TargetFrameworkHelpers.GetTargetFrameworkFolder(net8Project);
        string net9Folder = TargetFrameworkHelpers.GetTargetFrameworkFolder(net9Project);

        // Assert
        Assert.NotEqual(net8Folder, net9Folder);
        Assert.Equal("net8.0", net8Folder);
        Assert.Equal("net9.0", net9Folder);
    }

    #endregion

    #region Project File Variations for Net8

    [Fact]
    public void TargetFrameworkHelpers_Net8WithComments_DetectsCorrectly()
    {
        // Arrange - Project file with XML comments
        string projectPath = Path.Combine(_testDirectory, "Net8Comments.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <!-- Target framework configuration -->
    <TargetFramework>net8.0</TargetFramework>
    <!-- End of target framework configuration -->
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8RazorClassLibrary_DetectsCorrectly()
    {
        // Arrange - Razor Class Library project
        string projectPath = Path.Combine(_testDirectory, "Net8RazorLib.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.Razor"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
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
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8WorkerService_DetectsCorrectly()
    {
        // Arrange - Worker Service project
        string projectPath = Path.Combine(_testDirectory, "Net8Worker.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.Worker"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>dotnet-Net8Worker-12345</UserSecretsId>
  </PropertyGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void TargetFrameworkHelpers_Net8AspNetCore_DetectsCorrectly()
    {
        // Arrange - ASP.NET Core project
        string projectPath = Path.Combine(_testDirectory, "Net8AspNet.csproj");
        string content = @"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.OpenApi"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";
        File.WriteAllText(projectPath, content);
        _createdProjects.Add(projectPath);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
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

        public IEnumerable<string> GetAllT4TemplatesForNet8Project(string projectPath, string[] baseFolders)
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
