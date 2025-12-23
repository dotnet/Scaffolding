// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Common;

public class ProjectReferenceParserTests
{
    [Fact]
    public void ParseProjectReferences_WithValidOutput_ReturnsReferences()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- 
C:\Projects\MyProject\MyProject.csproj
C:\Projects\AnotherProject\AnotherProject.csproj";

        // Act
        List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("C:\\Projects\\MyProject\\MyProject.csproj", result);
        Assert.Contains("C:\\Projects\\AnotherProject\\AnotherProject.csproj", result);
    }

    [Fact]
    public void ParseProjectReferences_WithNoReferences_ReturnsEmptyList()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- ";

        // Act
        List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseProjectReferences_WithEmptyString_ReturnsEmptyList()
    {
        // Arrange
        string input = "";

        // Act
        List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseProjectReferences_WithoutBanner_ReturnsEmptyList()
    {
        // Arrange
        string input = @"C:\Projects\MyProject\MyProject.csproj
C:\Projects\AnotherProject\AnotherProject.csproj";

    // Act
    List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

    // Assert
    Assert.NotNull(result);
    Assert.Empty(result);
}

[Fact]
public void ParseProjectReferences_WithCaseInsensitiveBanner_ReturnsReferences()
    {
        // Arrange
        string input = @"project REFERENCE(s)
--------------------- 
C:\Projects\MyProject\MyProject.csproj";

    // Act
    List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

    // Assert
    Assert.NotNull(result);
    Assert.Single(result);
    Assert.Contains("C:\\Projects\\MyProject\\MyProject.csproj", result);
}

[Fact]
public void ParseProjectReferences_WithMultipleDashedLines_IgnoresThem()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- 
------------------
C:\Projects\MyProject\MyProject.csproj
-----
C:\Projects\AnotherProject\AnotherProject.csproj";

    // Act
    List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
}

[Fact]
public void ParseProjectReferences_WithWhitespace_TrimsReferences()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- 
  C:\Projects\MyProject\MyProject.csproj  
    C:\Projects\AnotherProject\AnotherProject.csproj   ";

        // Act
        List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("C:\\Projects\\MyProject\\MyProject.csproj", result);
        Assert.Contains("C:\\Projects\\AnotherProject\\AnotherProject.csproj", result);
    }

    [Fact]
    public void ParseProjectReferences_WithEmptyLines_IgnoresThem()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- 

C:\Projects\\MyProject\MyProject.csproj

C:\Projects\AnotherProject\AnotherProject.csproj


";

    // Act
    List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseProjectReferences_WithRelativePaths_ReturnsReferences()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- 
..\MyProject\MyProject.csproj
..\..\AnotherProject\AnotherProject.csproj";

        // Act
        List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("..\\MyProject\\MyProject.csproj", result);
        Assert.Contains("..\\..\\AnotherProject\\AnotherProject.csproj", result);
    }

    [Fact]
    public void ParseProjectReferences_WithUnixStylePaths_ReturnsReferences()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- 
/home/user/projects/MyProject/MyProject.csproj
/home/user/projects/AnotherProject/AnotherProject.csproj";

// Act
List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

// Assert
Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("/home/user/projects/MyProject/MyProject.csproj", result);
        Assert.Contains("/home/user/projects/AnotherProject/AnotherProject.csproj", result);
    }

    [Fact]
    public void ParseProjectReferences_WithAdditionalTextAfterBanner_IgnoresIt()
    {
        // Arrange
        string input = @"Some other output
Project reference(s)
--------------------- 
C:\Projects\MyProject\MyProject.csproj
Some footer text
C:\Projects\AnotherProject\AnotherProject.csproj";

// Act
List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

// Assert
Assert.NotNull(result);
Assert.Equal(3, result.Count);
        Assert.Contains("C:\\Projects\\MyProject\\MyProject.csproj", result);
        Assert.Contains("Some footer text", result);
        Assert.Contains("C:\\Projects\\AnotherProject\\AnotherProject.csproj", result);
    }

    [Fact]
    public void ParseProjectReferences_WithSingleReference_ReturnsSingleReference()
    {
        // Arrange
        string input = @"Project reference(s)
--------------------- 
C:\Projects\MyProject\MyProject.csproj";

        // Act
        List<string> result = ProjectReferenceParser.ParseProjectReferences(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("C:\\Projects\\MyProject\\MyProject.csproj", result[0]);
    }
}
