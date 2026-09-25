// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class DetectBlazorWasmStepTests
{
    [Fact]
    public void GetReferenceFullPath_ResolvesRelativeReference_AgainstAbsoluteProjectDirectory()
    {
        var projectDir = Path.Combine(Path.GetTempPath(), "Solution", "App");
        var projectPath = Path.Combine(projectDir, "App.csproj");
        var reference = Path.Combine("..", "App.Client", "App.Client.csproj");

        var result = DetectBlazorWasmStep.GetReferenceFullPath(projectPath, reference);

        Assert.Equal(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Solution", "App.Client", "App.Client.csproj")), result);
    }

    [Fact]
    public void GetReferenceFullPath_FullyQualifiesRelativeProjectPath_BeforeResolving()
    {
        // '--project .\App\App.csproj' style input: Path.GetFullPath(reference, basePath) throws for a relative base path,
        // which is exactly the failure this helper guards against.
        var projectPath = Path.Combine("App", "App.csproj");
        var reference = Path.Combine("..", "SharedLib", "SharedLib.csproj");

        var result = DetectBlazorWasmStep.GetReferenceFullPath(projectPath, reference);

        Assert.NotNull(result);
        Assert.True(Path.IsPathFullyQualified(result));
        Assert.Equal(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "SharedLib", "SharedLib.csproj")), result);
    }

    [Fact]
    public void GetReferenceFullPath_ReturnsAbsoluteReference_Unchanged()
    {
        var projectPath = Path.Combine("App", "App.csproj");
        var reference = Path.Combine(Path.GetTempPath(), "Elsewhere", "Lib.csproj");

        var result = DetectBlazorWasmStep.GetReferenceFullPath(projectPath, reference);

        Assert.Equal(Path.GetFullPath(reference), result);
    }

    [Theory]
    [InlineData(null, "Lib.csproj")]
    [InlineData("", "Lib.csproj")]
    [InlineData("App.csproj", null)]
    [InlineData("App.csproj", "")]
    public void GetReferenceFullPath_ReturnsNull_ForMissingInputs(string? projectPath, string? reference)
        => Assert.Null(DetectBlazorWasmStep.GetReferenceFullPath(projectPath, reference));
}
