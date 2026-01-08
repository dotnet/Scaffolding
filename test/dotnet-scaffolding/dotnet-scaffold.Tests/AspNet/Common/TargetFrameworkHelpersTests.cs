// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Common;

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
    public void GetLowestCompatibleTargetFramework_Net8Project_ReturnsNet8()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet8.csproj", "net8.0");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net8.0", result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_Net9Project_ReturnsNet9()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet9.csproj", "net9.0");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net9.0", result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_MultiTargetNet8AndNet9_ReturnsNet8()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiTarget.csproj", "net8.0;net9.0", isMultiTarget: true);

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net8.0", result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_MultiTargetNet8Net9Net10_ReturnsNet8()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiTarget3.csproj", "net8.0;net9.0", isMultiTarget: true);

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net8.0", result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_Net7Project_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet7.csproj", "net7.0");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_Net6Project_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet6.csproj", "net6.0");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_NetStandard20Project_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNetStandard.csproj", "netstandard2.0");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_MonoAndroidProject_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMonoAndroid.csproj", "monoandroid13.0");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_MultiTargetWithIncompatible_ReturnsNull()
    {
        // Arrange - Mix of compatible (net8.0) and incompatible (net7.0) frameworks
        string projectPath = CreateTestProject("TestMixedTarget.csproj", "net7.0;net8.0", isMultiTarget: true);

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.Null(result); // Should return null because net7.0 is incompatible
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_MultiTargetWithMonoAndroid_ReturnsNull()
    {
        // Arrange - Mix of compatible (net9.0) and incompatible (monoandroid13.0) frameworks
        string projectPath = CreateTestProject("TestMixedTarget2.csproj", "net9.0;monoandroid13.0", isMultiTarget: true);

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.Null(result); // Should return null because monoandroid13.0 is incompatible
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_Net8Android_ReturnsNet8Android()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet8Android.csproj", "net8.0-android");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net8.0-android", result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_Net9iOS_ReturnsNet9iOS()
    {
        // Arrange
        string projectPath = CreateTestProject("TestNet9iOS.csproj", "net9.0-ios");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net9.0-ios", result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_MultiTargetNet8AndroidAndNet9_ReturnsNet8Android()
    {
        // Arrange
        string projectPath = CreateTestProject("TestMultiPlatform.csproj", "net8.0-android;net9.0", isMultiTarget: true);

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("net8.0-android", result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_InvalidProjectPath_ReturnsNull()
    {
        // Arrange
        string projectPath = Path.Combine(_testProjectsDirectory, "NonExistent.csproj");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetLowestCompatibleTargetFramework_EmptyProject_ReturnsNull()
    {
        // Arrange
        string projectPath = CreateEmptyProject("EmptyProject.csproj");

        // Act
        string? result = TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath);

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