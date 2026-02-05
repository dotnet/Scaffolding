// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Templates;

public class Net9TemplatesFolderTests
{
    private static readonly string[] ExpectedTemplateFolders =
    [
        "BlazorCrud",
        "BlazorIdentity",
        "EfController",
        "Files",
        "Identity",
        "MinimalApi",
        "RazorPages",
        "Views"
    ];

    [Fact]
    public void Net9TemplatesFolder_Exists()
    {
        // Arrange
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        Assert.NotNull(assemblyDirectory);

        // Navigate to the dotnet-scaffold assembly location
        // From test assembly: test/dotnet-scaffolding/dotnet-scaffold.Tests/bin/.../
        // To source assembly: artifacts/bin/dotnet-scaffold/.../AspNet/Templates/net9.0
        string? currentDir = assemblyDirectory;
        string? artifactsDir = null;
        
        // Find artifacts directory
        while (currentDir != null && !string.IsNullOrEmpty(currentDir))
        {
            if (Path.GetFileName(currentDir) == "artifacts")
            {
                artifactsDir = currentDir;
                break;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }

        Assert.NotNull(artifactsDir);
        
        // Look for dotnet-scaffold bin directory
        string binDir = Path.Combine(artifactsDir, "bin", "dotnet-scaffold");
        Assert.True(Directory.Exists(binDir), $"Expected dotnet-scaffold bin directory to exist at: {binDir}");

        // Find the actual build configuration directory (Debug or Release) and TFM
        string[] configDirs = Directory.GetDirectories(binDir);
        Assert.NotEmpty(configDirs);
        
        string? templatesDir = null;
        foreach (var configDir in configDirs)
        {
            // Check for TFM subdirectories (e.g., net11.0, net9.0)
            string[] tfmDirs = Directory.Exists(configDir) ? Directory.GetDirectories(configDir) : Array.Empty<string>();
            foreach (var tfmDir in tfmDirs)
            {
                var candidateTemplatesDir = Path.Combine(tfmDir, "AspNet", "Templates", "net9.0");
                if (Directory.Exists(candidateTemplatesDir))
                {
                    templatesDir = candidateTemplatesDir;
                    break;
                }
            }
            if (templatesDir != null) break;
        }

        // Assert
        Assert.NotNull(templatesDir);
        Assert.True(Directory.Exists(templatesDir), $"net9.0 templates directory should exist at: {templatesDir}");
    }

    [Theory]
    [InlineData("BlazorCrud")]
    [InlineData("BlazorIdentity")]
    [InlineData("EfController")]
    [InlineData("Files")]
    [InlineData("Identity")]
    [InlineData("MinimalApi")]
    [InlineData("RazorPages")]
    [InlineData("Views")]
    public void Net9TemplatesFolder_ContainsExpectedFolder(string folderName)
    {
        // Arrange
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        Assert.NotNull(assemblyDirectory);

        // Navigate to find artifacts directory
        string? currentDir = assemblyDirectory;
        string? artifactsDir = null;
        
        while (currentDir != null && !string.IsNullOrEmpty(currentDir))
        {
            if (Path.GetFileName(currentDir) == "artifacts")
            {
                artifactsDir = currentDir;
                break;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }

        Assert.NotNull(artifactsDir);
        
        // Find dotnet-scaffold bin directory and templates
        string binDir = Path.Combine(artifactsDir, "bin", "dotnet-scaffold");
        string[] configDirs = Directory.GetDirectories(binDir);
        
        string? templateFolderPath = null;
        foreach (var configDir in configDirs)
        {
            string[] tfmDirs = Directory.Exists(configDir) ? Directory.GetDirectories(configDir) : Array.Empty<string>();
            foreach (var tfmDir in tfmDirs)
            {
                var candidatePath = Path.Combine(tfmDir, "AspNet", "Templates", "net9.0", folderName);
                if (Directory.Exists(candidatePath))
                {
                    templateFolderPath = candidatePath;
                    break;
                }
            }
            if (templateFolderPath != null) break;
        }

        // Assert
        Assert.NotNull(templateFolderPath);
        Assert.True(Directory.Exists(templateFolderPath), 
            $"Template folder '{folderName}' should exist in net9.0 templates directory at: {templateFolderPath}");
    }

    [Fact]
    public void Net9TemplatesFolder_ContainsAllExpectedFolders()
    {
        // Arrange
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        Assert.NotNull(assemblyDirectory);

        // Navigate to find artifacts directory
        string? currentDir = assemblyDirectory;
        string? artifactsDir = null;
        
        while (currentDir != null && !string.IsNullOrEmpty(currentDir))
        {
            if (Path.GetFileName(currentDir) == "artifacts")
            {
                artifactsDir = currentDir;
                break;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }

        Assert.NotNull(artifactsDir);
        
        // Find dotnet-scaffold bin directory and templates
        string binDir = Path.Combine(artifactsDir, "bin", "dotnet-scaffold");
        string[] configDirs = Directory.GetDirectories(binDir);
        
        string? templatesDir = null;
        foreach (var configDir in configDirs)
        {
            string[] tfmDirs = Directory.Exists(configDir) ? Directory.GetDirectories(configDir) : Array.Empty<string>();
            foreach (var tfmDir in tfmDirs)
            {
                var candidateTemplatesDir = Path.Combine(tfmDir, "AspNet", "Templates", "net9.0");
                if (Directory.Exists(candidateTemplatesDir))
                {
                    templatesDir = candidateTemplatesDir;
                    break;
                }
            }
            if (templatesDir != null) break;
        }

        Assert.NotNull(templatesDir);

        // Act
        var actualFolders = Directory.GetDirectories(templatesDir)
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToArray();

        var expectedFolders = ExpectedTemplateFolders.OrderBy(f => f).ToArray();

        // Assert
        Assert.Equal(expectedFolders, actualFolders);
    }

    [Theory]
    [InlineData("BlazorCrud")]
    [InlineData("BlazorIdentity")]
    [InlineData("EfController")]
    [InlineData("Files")]
    [InlineData("Identity")]
    [InlineData("MinimalApi")]
    [InlineData("RazorPages")]
    [InlineData("Views")]
    public void Net9TemplatesFolder_ContainedFolderIsNotEmpty(string folderName)
    {
        // Arrange
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        string? assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        Assert.NotNull(assemblyDirectory);

        // Navigate to find artifacts directory
        string? currentDir = assemblyDirectory;
        string? artifactsDir = null;
        
        while (currentDir != null && !string.IsNullOrEmpty(currentDir))
        {
            if (Path.GetFileName(currentDir) == "artifacts")
            {
                artifactsDir = currentDir;
                break;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }

        Assert.NotNull(artifactsDir);
        
        // Find dotnet-scaffold bin directory and templates
        string binDir = Path.Combine(artifactsDir, "bin", "dotnet-scaffold");
        string[] configDirs = Directory.GetDirectories(binDir);
        
        string? templateFolderPath = null;
        foreach (var configDir in configDirs)
        {
            string[] tfmDirs = Directory.Exists(configDir) ? Directory.GetDirectories(configDir) : Array.Empty<string>();
            foreach (var tfmDir in tfmDirs)
            {
                var candidatePath = Path.Combine(tfmDir, "AspNet", "Templates", "net9.0", folderName);
                if (Directory.Exists(candidatePath))
                {
                    templateFolderPath = candidatePath;
                    break;
                }
            }
            if (templateFolderPath != null) break;
        }

        Assert.NotNull(templateFolderPath);

        // Act
        var files = Directory.GetFiles(templateFolderPath, "*.*", SearchOption.AllDirectories);

        // Assert
        Assert.NotEmpty(files);
    }
}
