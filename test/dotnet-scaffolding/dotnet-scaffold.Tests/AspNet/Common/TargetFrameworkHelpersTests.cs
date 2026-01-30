// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Core.Tests.Helpers;

public class TargetFrameworkHelpersTests : IDisposable
{
    private readonly string _testProjectsDirectory;
    private readonly List<string> _createdProjects;

    public TargetFrameworkHelpersTests()
    {
        _testProjectsDirectory = Path.Combine(Path.GetTempPath(), "TargetFrameworkHelpersTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testProjectsDirectory);
        _createdProjects = new List<string>();
    }

    public void Dispose()
    {
        // Cleanup test projects
        if (Directory.Exists(_testProjectsDirectory))
        {
            try
            {
                Directory.Delete(_testProjectsDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net8Project_ReturnsNet8()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet8.csproj", "net8.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net9Project_ReturnsNet9()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet9.csproj", "net9.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net10Project_ReturnsNet10()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet10.csproj", "net10.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_MultiTargetNet8AndNet9_ReturnsNet8()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiTarget.csproj", "net8.0;net9.0", isMultiTarget: true);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_MultiTargetNet9AndNet10_ReturnsNet9()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiTarget2.csproj", "net9.0;net10.0", isMultiTarget: true);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net9, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_MultiTargetNet8Net9Net10_ReturnsNet8()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiTarget3.csproj", "net8.0;net9.0;net10.0", isMultiTarget: true);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net8, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net11Project_ReturnsNet11()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet11.csproj", "net11.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net11, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_MultiTargetNet10AndNet11_ReturnsNet10()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiTarget4.csproj", "net10.0;net11.0", isMultiTarget: true);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TargetFramework.Net10, result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net7Project_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet7.csproj", "net7.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net6Project_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet6.csproj", "net6.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_NetStandard20Project_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNetStandard.csproj", "netstandard2.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_MonoAndroidProject_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMonoAndroid.csproj", "monoandroid13.0");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_MultiTargetWithIncompatible_ReturnsNull()
    {
        // Arrange - Mix of compatible (net8.0) and incompatible (net7.0) frameworks
        string projectPath = CreateTestProject("TestMixedTarget.csproj", "net7.0;net8.0", isMultiTarget: true);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result); // Should return null because net7.0 is incompatible
    }

    [Fact]
    public void GetTargetFrameworkForProject_MultiTargetWithMonoAndroid_ReturnsNull()
    {
        // Arrange - Mix of compatible (net9.0) and incompatible (monoandroid13.0) frameworks
        string projectPath = CreateTestProject("TestMixedTarget2.csproj", "net9.0;monoandroid13.0", isMultiTarget: true);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result); // Should return null because monoandroid13.0 is incompatible
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net8Android_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet8Android.csproj", "net8.0-android");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result); // net8.0-android doesn't have an enum mapping
    }

    [Fact]
    public void GetTargetFrameworkForProject_Net9iOS_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet9iOS.csproj", "net9.0-ios");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result); // net9.0-ios doesn't have an enum mapping
    }

    [Fact]
    public void GetTargetFrameworkForProject_MultiTargetNet8AndroidAndNet9_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiPlatform.csproj", "net8.0-android;net9.0", isMultiTarget: true);

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result); // net8.0-android doesn't have an enum mapping
    }

    [Fact]
    public void GetTargetFrameworkForProject_InvalidProjectPath_ReturnsNull()
    {
        // Arrange
        string projectPath = Path.Combine(_testProjectsDirectory, "NonExistent.csproj");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTargetFrameworkForProject_EmptyProject_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateEmptyProject("EmptyProject.csproj");

        // Act
        TargetFramework? result = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        // Assert
        Assert.Null(result);
    }

    private string CreateTestProject(string projectName, string targetFramework, bool isMultiTarget = false)
    {
        string projectPath = Path.Combine(_testProjectsDirectory, projectName);
        string frameworkProperty = isMultiTarget ? "TargetFrameworks" : "TargetFramework";
        
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <{frameworkProperty}>{targetFramework}</{frameworkProperty}>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>";

        File.WriteAllText(projectPath, projectContent);
        _createdProjects.Add(projectPath);
        return projectPath;
    }

    private string CreateEmptyProject(string projectName)
    {
        string projectPath = Path.Combine(_testProjectsDirectory, projectName);
        
        string projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
  </PropertyGroup>
</Project>";

        File.WriteAllText(projectPath, projectContent);
        _createdProjects.Add(projectPath);
        return projectPath;
    }
}
