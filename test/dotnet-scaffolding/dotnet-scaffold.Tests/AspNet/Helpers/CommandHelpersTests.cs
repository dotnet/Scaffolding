// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class CommandHelpersTests
{
    [Fact]
    public void GetNewFilePath_WithValidInputs_ReturnsExpectedPath()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string className = "MyClass";
        string expectedDirectory = Path.Combine("test", "project");

        // Act
        string result = CommandHelpers.GetNewFilePath(projectPath, className);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.StartsWith(expectedDirectory, result);
        Assert.EndsWith(".cs", result);
        Assert.Contains("MyClass", result);
    }

    [Fact]
    public void GetNewFilePath_WithClassNameWithoutExtension_AddsExtension()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string className = "MyClass";

        // Act
        string result = CommandHelpers.GetNewFilePath(projectPath, className);

        // Assert
        Assert.EndsWith(".cs", result);
    }

    [Fact]
    public void GetNewFilePath_WithClassNameWithExtension_DoesNotDuplicateExtension()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string className = "MyClass.cs";

        // Act
        string result = CommandHelpers.GetNewFilePath(projectPath, className);

        // Assert
        Assert.EndsWith(".cs", result);
        Assert.DoesNotContain(".cs.cs", result);
    }

    [Fact]
    public void GetNewFilePath_WithEmptyClassName_ReturnsPathWithExtension()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string className = string.Empty;

        // Act
        string result = CommandHelpers.GetNewFilePath(projectPath, className);

        // Assert
        Assert.NotNull(result);
        Assert.EndsWith(".cs", result);
    }

    [Fact]
    public void GetNewFilePath_WithInvalidProjectPath_ReturnsEmptyString()
    {
        // Arrange
        string projectPath = string.Empty;
        string className = "MyClass";

        // Act
        string result = CommandHelpers.GetNewFilePath(projectPath, className);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetNewFilePath_WithNestedDirectoryStructure_ReturnsPathInProjectDirectory()
    {
        // Arrange
        string projectPath = Path.Combine("src", "nested", "folders", "TestProject.csproj");
        string className = "NestedClass";
        string expectedDirectory = Path.Combine("src", "nested", "folders");

        // Act
        string result = CommandHelpers.GetNewFilePath(projectPath, className);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(expectedDirectory, result);
        Assert.Contains("NestedClass", result);
        Assert.EndsWith(".cs", result);
    }
}
