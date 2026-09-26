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
    [InlineData("untouched", "input file must remain unchanged")]
    [InlineData("package", "App.csproj:1")]
    public void AssertMatches_ReportsOutputRegressions(string mutation, string diagnostic)
    {
        Dictionary<string, string> input = new() { ["Home.razor"] = "Home", ["App.csproj"] = "<Project />" };
        Dictionary<string, string> expected = new(input)
        {
            ["Account/Login.razor"] = "Login",
            ["App.csproj"] = """<PackageReference Include="Identity" Version="10.0.12" />"""
        };
        Dictionary<string, string> actual = new(expected);
        switch (mutation)
        {
            case "changed": actual["Account/Login.razor"] = "Wrong"; break;
            case "missing": actual.Remove("Account/Login.razor"); break;
            case "unexpected": actual["Other.cs"] = "Unexpected"; break;
            case "untouched": actual["Home.razor"] = "Changed"; break;
            case "package": actual["App.csproj"] = """<PackageReference Include="Identity" Version="10.0.13" />"""; break;
        }

        var exception = Assert.Throws<TrueException>(() =>
            GeneratedProjectBaseline.AssertMatches(input, expected, actual, ["Account/Login.razor", "App.csproj"]));
        Assert.Contains(diagnostic, exception.Message);
    }

    [Fact]
    public void AssertMatches_UsesCurrentInputForUntouchedTemplateFiles()
    {
        Dictionary<string, string> input = new() { ["Home.razor"] = "New template" };
        Dictionary<string, string> expected = new() { ["Home.razor"] = "Old template", ["Login.razor"] = "Login" };
        Dictionary<string, string> actual = new(input) { ["Login.razor"] = "Login" };

        GeneratedProjectBaseline.AssertMatches(input, expected, actual, ["Login.razor"]);
    }
}
