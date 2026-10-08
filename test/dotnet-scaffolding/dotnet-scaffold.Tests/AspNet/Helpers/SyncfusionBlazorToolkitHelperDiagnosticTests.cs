// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.Helpers;

public class SyncfusionBlazorToolkitHelperDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public SyncfusionBlazorToolkitHelperDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test bin folder until we find a Directory.Build.props.
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
    public void Diagnostic_ShowResolvedJson_FromActualTfmConfig(string tfm)
    {
        var repoRoot = FindRepoRoot();
        var tfmConfigPath = Path.Combine(
            repoRoot,
            "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", tfm, "CodeModificationConfigs",
            "syncfusionBlazorToolkitChanges.json");

        _output.WriteLine($"TFM: {tfm}");
        _output.WriteLine($"Config path: {tfmConfigPath}");
        Assert.True(File.Exists(tfmConfigPath), $"Config file must exist at {tfmConfigPath}");

        var rawJson = File.ReadAllText(tfmConfigPath);
        _output.WriteLine($"--- Source JSON ({rawJson.Length} chars) ---");
        _output.WriteLine(rawJson);

        // Now resolve with both theme and imports discovered.
        var resolved = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            tfmConfigPath,
            themeFile: "Components/App.razor",
            importsFile: "Components/_Imports.razor");

        Assert.NotNull(resolved);

        _output.WriteLine($"--- Resolved JSON ({resolved.Length} chars) ---");
        _output.WriteLine(resolved);

        using var doc = JsonDocument.Parse(resolved);
        var files = doc.RootElement.GetProperty("Files");
        _output.WriteLine($"Files array length: {files.GetArrayLength()}");
        for (int i = 0; i < files.GetArrayLength(); i++)
        {
            var file = files[i];
            var fileName = file.GetProperty("FileName").GetString();
            _output.WriteLine($" [{i}] FileName = {fileName}");
        }

        // After the fix, we expect 3 file entries: Program.cs, theme, imports.
        Assert.Equal(3, files.GetArrayLength());
    }
}
