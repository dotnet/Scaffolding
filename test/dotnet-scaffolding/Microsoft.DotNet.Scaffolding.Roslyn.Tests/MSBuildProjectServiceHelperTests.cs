// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.Build.Evaluation;
using System.IO;
using System.Xml;
using Microsoft.DotNet.Scaffolding.Roslyn.Helpers;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Tests;

public class MSBuildProjectServiceHelperTests
{
    [Theory]
    [InlineData("net5.0", "5.0")]
    [InlineData("net6.0", "6.0")]
    [InlineData("netstandard2.0", "2.0")]
    [InlineData("netstandard2.1", "2.1")]
    [InlineData("invalid", "0.0")]
    [InlineData("bleh", "0.0")]
    [InlineData("", "0.0")]
    public void ParseFrameworkVersionTests(string shortTfm, string expectedVersion)
    {
        // Act
        var result = MSBuildProjectServiceHelper.ParseFrameworkVersion(shortTfm);
        // Assert
        Assert.Equal(new Version(expectedVersion), result);
    }

    [Fact]
    public void GetProjectCapabilitiesTest()
    {
        // Arrange
        var csprojContent = @"
                <Project>
                    <PropertyGroup>
                        <TargetFramework>net9.0</TargetFramework>
                    </PropertyGroup>
                    <ItemGroup>
                        <ProjectCapability Include=""CapabilityA"" />
                        <ProjectCapability Include=""CapabilityB"" />
                    </ItemGroup>
                </Project>";

        using var reader = XmlReader.Create(new StringReader(csprojContent));
        var testProject = new Project(reader, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports);
        // Act
        var capabilities = MSBuildProjectServiceHelper.GetProjectCapabilities(testProject);

        // Assert
        Assert.Contains("CapabilityA", capabilities);
        Assert.Contains("CapabilityB", capabilities);
    }

    [Fact]
    public void GetLowestTargetFrameworkTest()
    {
        // Arrange
        var csprojContent = @"
                <Project>
                    <PropertyGroup>
                        <TargetFramework>net9.0</TargetFramework>
                    </PropertyGroup>
                </Project>";

        using var reader = XmlReader.Create(new StringReader(csprojContent));
        var testProject = new Project(reader, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports);
        // Act
        var lowestTfm = MSBuildProjectServiceHelper.GetLowestTargetFramework(testProject);
        //Assert
        Assert.Equal("net9.0", lowestTfm);
    }
}
