// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

public class GeneratedProjectBaselineTests
{
    [Theory]
    [InlineData("changed", "Account/Login.razor:1")]
    [InlineData("missing", "Missing file: Account/Login.razor")]
    [InlineData("unexpected", "Unexpected file: Other.cs")]
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
            case "package": actual["App.csproj"] = expected["App.csproj"].Replace("Include=\"Identity\"", "Include=\"Other\""); break;
            case "metadata": actual["App.csproj"] = expected["App.csproj"].Replace("PrivateAssets=\"all\"", "PrivateAssets=\"none\""); break;
            case "project-version": actual["App.csproj"] = expected["App.csproj"].Replace("<Version>1.0.0</Version>", "<Version>1.0.1</Version>"); break;
        }

        var exception = Assert.Throws<TrueException>(() =>
            GeneratedProjectBaseline.AssertMatches(expected, actual));
        Assert.Contains(diagnostic, exception.Message);
    }

    [Theory]
    [InlineData("""<PackageReference Include="Identity" Version="10.0.12"><PrivateAssets>all</PrivateAssets></PackageReference>""")]
    [InlineData("""<PackageReference Include="Identity"><Version>10.0.12</Version><PrivateAssets>all</PrivateAssets></PackageReference>""")]
    public void AssertMatches_IgnoresPackageReferenceVersions(string reference)
    {
        Dictionary<string, string> expected = new()
        {
            ["App.csproj"] = $"<Project><ItemGroup>{reference}</ItemGroup></Project>"
        };
        Dictionary<string, string> actual = new(expected)
        {
            ["App.csproj"] = expected["App.csproj"].Replace("10.0.12", "10.0.13")
        };

        GeneratedProjectBaseline.AssertMatches(expected, actual);
    }

    [Fact]
    public void ReadFiles_IncludesAllFilesExceptBuildOutputs()
    {
        var directory = Path.Combine(Path.GetTempPath(), nameof(GeneratedProjectBaselineTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Home.razor"), "Home\r\nContent\r\n");
            File.WriteAllBytes(Path.Combine(directory, "asset.bin"), [0xff]);
            File.WriteAllBytes(Path.Combine(directory, "data.db"), [0xfe]);
            foreach (var name in new[] { "bin", Path.Combine("Client", "obj"), ".vs" })
            {
                var child = Path.Combine(directory, name);
                Directory.CreateDirectory(child);
                File.WriteAllText(Path.Combine(child, "settings.json"), "{}");
            }

            var files = GeneratedProjectBaseline.ReadFiles(directory);
            Assert.Equal(4, files.Count);
            Assert.Equal("Home\nContent\n", files["Home.razor"]);
            Assert.Equal(Convert.ToBase64String([0xff]), files["asset.bin"]);
            Assert.Equal(Convert.ToBase64String([0xfe]), files["data.db"]);
            Assert.Equal("{}", files[".vs/settings.json"]);

            var actual = new Dictionary<string, string>(files) { ["asset.bin"] = Convert.ToBase64String([0xfe]) };
            var exception = Assert.Throws<TrueException>(() => GeneratedProjectBaseline.AssertMatches(files, actual));
            Assert.Contains("asset.bin (byte mismatch)", exception.Message);

            var textPath = Path.Combine(directory, "Home.razor");
            File.WriteAllBytes(textPath, [0xff]);
            var encodingException = Assert.Throws<InvalidDataException>(() => GeneratedProjectBaseline.ReadFiles(directory));
            Assert.Contains(textPath, encodingException.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

}
