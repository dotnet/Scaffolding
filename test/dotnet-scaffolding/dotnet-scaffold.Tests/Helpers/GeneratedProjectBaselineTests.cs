// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

public class GeneratedProjectBaselineTests
{
    [Theory]
    [InlineData("changed", "Account/Login.razor:1")]
    [InlineData("missing", "Missing file: Account/Login.razor")]
    [InlineData("unexpected", "Unexpected file: Other.cs")]
    [InlineData("untouched", "Home.razor:1")]
    [InlineData("package", "App.csproj:1")]
    [InlineData("metadata", "App.csproj:1")]
    [InlineData("project-version", "App.csproj:1")]
    public void AssertMatches_ReportsOutputRegressions(string mutation, string diagnostic)
    {
        Dictionary<string, string> expected = new()
        {
            ["Home.razor"] = "Home",
            ["Account/Login.razor"] = "Login",
            ["App.csproj"] = """<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup><ItemGroup><PackageReference Include="Identity" Version="10.0.12" PrivateAssets="all" /></ItemGroup></Project>"""
        };
        Dictionary<string, string> actual = new(expected);
        switch (mutation)
        {
            case "changed": actual["Account/Login.razor"] = "Wrong"; break;
            case "missing": actual.Remove("Account/Login.razor"); break;
            case "unexpected": actual["Other.cs"] = "Unexpected"; break;
            case "untouched": actual["Home.razor"] = "Changed"; break;
            case "package": actual["App.csproj"] = expected["App.csproj"].Replace("Include=\"Identity\"", "Include=\"Other\""); break;
            case "metadata": actual["App.csproj"] = expected["App.csproj"].Replace("PrivateAssets=\"all\"", "PrivateAssets=\"none\""); break;
            case "project-version": actual["App.csproj"] = expected["App.csproj"].Replace("<Version>1.0.0</Version>", "<Version>1.0.1</Version>"); break;
        }

        var exception = Assert.Throws<TrueException>(() =>
            GeneratedProjectBaseline.AssertMatches(expected, actual));
        Assert.Contains(diagnostic, exception.Message);
    }

    [Fact]
    public void AssertMatches_IgnoresPackageReferenceVersions()
    {
        Dictionary<string, string> expected = new()
        {
            ["App.csproj"] = """
                <Project><ItemGroup><PackageReference Include="Identity" Version="10.0.12">
                  <PrivateAssets>all</PrivateAssets>
                </PackageReference></ItemGroup></Project>
                """
        };
        Dictionary<string, string> actual = new(expected)
        {
            ["App.csproj"] = expected["App.csproj"].Replace("10.0.12", "10.0.13")
        };

        GeneratedProjectBaseline.AssertMatches(expected, actual);
    }

    [Fact]
    public void AssertMatches_DetectsEditsToAnyExpectedFile()
    {
        Dictionary<string, string> expected = new() { ["Home.razor"] = "Edited expected template file" };
        Dictionary<string, string> actual = new() { ["Home.razor"] = "Original template file" };

        var exception = Assert.Throws<TrueException>(() =>
            GeneratedProjectBaseline.AssertMatches(expected, actual));
        Assert.Contains("Home.razor:1", exception.Message);
    }

}
