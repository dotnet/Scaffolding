// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.Helpers;

public class SyncfusionBlazorToolkitTfmConfigTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void RealTfmConfig_ResolvesProgramCsOnly(string tfm)
    {
        var tfmConfigPath = Path.Combine(
            FindRepoRoot(),
            "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", tfm, "CodeModificationConfigs",
            "syncfusionBlazorToolkitChanges.json");

        Assert.True(File.Exists(tfmConfigPath), $"TFM config file must exist at {tfmConfigPath}");

        var resolved = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(tfmConfigPath);
        Assert.NotNull(resolved);
        using var doc = JsonDocument.Parse(resolved);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(1, files.GetArrayLength());
        Assert.Equal("Program.cs", files[0].GetProperty("FileName").GetString());
        Assert.Contains("AddSyncfusionBlazorToolkit", files[0].GetRawText());
        Assert.DoesNotContain("fluent.min.css", resolved);
    }
}
