// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.Helpers;

/// <summary>
/// End-to-end tests against the real code-modification JSON configs that
/// ship with the dotnet-scaffold tool, one per TFM. These guard against
/// the regression where the resolved JSON failed to target the theme host
/// and the _Imports.razor file, causing the @using directive and theme
/// stylesheet to be silently skipped by the CodeModifier.
/// </summary>
public class SyncfusionBlazorToolkitTfmConfigTests
{
    private readonly ITestOutputHelper _output;

    public SyncfusionBlazorToolkitTfmConfigTests(ITestOutputHelper output)
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

    private static string OsRelative(params string[] segments)
    {
        return string.Join(Path.DirectorySeparatorChar, segments);
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void RealTfmConfig_ResolvesAllThreeFileEntries_WithOsNativePaths(string tfm)
    {
        var repoRoot = FindRepoRoot();
        var tfmConfigPath = Path.Combine(
            repoRoot,
            "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", tfm, "CodeModificationConfigs",
            "syncfusionBlazorToolkitChanges.json");

        Assert.True(File.Exists(tfmConfigPath),
            $"TFM config file must exist at {tfmConfigPath}");

        // Simulate what the Validate + Resolve steps would produce for
        // a standard Blazor Web App (Components/_Imports.razor +
        // Components/App.razor both present).
        string themeFile = OsRelative("Components", "App.razor");
        string importsFile = OsRelative("Components", "_Imports.razor");

        var resolved = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            tfmConfigPath, themeFile, importsFile);

        Assert.NotNull(resolved);
        _output.WriteLine($"[{tfm}] Resolved: {resolved}");

        using var doc = JsonDocument.Parse(resolved);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(3, files.GetArrayLength());

        // Entry 0: Program.cs with the AddSyncfusionBlazorToolkit insertion.
        Assert.Equal("Program.cs", files[0].GetProperty("FileName").GetString());

        // Entry 1: theme host (Components/App.razor) with the stylesheet link.
        string resolvedTheme = files[1].GetProperty("FileName").GetString()!;
        Assert.Equal(themeFile, resolvedTheme);

        // Entry 2: _Imports.razor with the @using directive block.
        string resolvedImports = files[2].GetProperty("FileName").GetString()!;
        Assert.Equal(importsFile, resolvedImports);

        // Sanity: theme entry must carry the stylesheet anchor and
        // imports entry must carry the @using block.
        string themeJson = files[1].GetRawText();
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", themeJson);
        string importsJson = files[2].GetRawText();
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", importsJson);
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void RealTfmConfig_EmittedFileNames_MatchOsNativeSeparatorConvention(string tfm)
    {
        // The CodeModifier's EndsWith lookup compares the JSON FileName
        // against MSBuildWorkspace's AdditionalDocument.FilePath, which
        // always uses Path.DirectorySeparatorChar. Forward-slash-only
        // FileName values silently fail on Windows and skip the
        // associated change set. This test pins the OS-native
        // convention so the bug cannot regress.
        var repoRoot = FindRepoRoot();
        var tfmConfigPath = Path.Combine(
            repoRoot,
            "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", tfm, "CodeModificationConfigs",
            "syncfusionBlazorToolkitChanges.json");

        var resolved = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            tfmConfigPath,
            themeFile: OsRelative("Components", "App.razor"),
            importsFile: OsRelative("Components", "_Imports.razor"));

        Assert.NotNull(resolved);
        using var doc = JsonDocument.Parse(resolved);
        var files = doc.RootElement.GetProperty("Files");

        for (int i = 1; i < files.GetArrayLength(); i++)
        {
            string emitted = files[i].GetProperty("FileName").GetString()!;
            if (Path.DirectorySeparatorChar == '\\')
            {
                Assert.DoesNotContain('/', emitted);
                Assert.Contains("\\", emitted);
            }
            else
            {
                Assert.DoesNotContain('\\', emitted);
                Assert.Contains("/", emitted);
            }
        }
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void RealTfmConfig_WithoutTheme_DoesNotEmitThemeEntry(string tfm)
    {
        // When no theme host is found, the scaffolder should not emit a
        // theme change entry at all. The downstream CodeModifier would
        // fail to find a non-existent file and would skip silently, so
        // the cleanest behavior is to omit the entry.
        var repoRoot = FindRepoRoot();
        var tfmConfigPath = Path.Combine(
            repoRoot,
            "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", tfm, "CodeModificationConfigs",
            "syncfusionBlazorToolkitChanges.json");

        var resolved = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            tfmConfigPath,
            themeFile: null,
            importsFile: OsRelative("Components", "_Imports.razor"));

        Assert.NotNull(resolved);
        using var doc = JsonDocument.Parse(resolved);
        var files = doc.RootElement.GetProperty("Files");

        bool hasFluentCss = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.GetRawText().Contains("fluent.min.css"))
            {
                hasFluentCss = true;
                break;
            }
        }
        Assert.False(hasFluentCss, "Theme stylesheet entry should not be emitted when themeFile is null");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void RealTfmConfig_WithoutImports_DoesNotEmitImportsEntry(string tfm)
    {
        var repoRoot = FindRepoRoot();
        var tfmConfigPath = Path.Combine(
            repoRoot,
            "src", "dotnet-scaffolding", "dotnet-scaffold",
            "AspNet", "Templates", tfm, "CodeModificationConfigs",
            "syncfusionBlazorToolkitChanges.json");

        var resolved = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            tfmConfigPath,
            themeFile: OsRelative("Components", "App.razor"),
            importsFile: null);

        Assert.NotNull(resolved);
        using var doc = JsonDocument.Parse(resolved);
        var files = doc.RootElement.GetProperty("Files");

        bool hasUsingDirective = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.GetRawText().Contains("@using Syncfusion.Blazor.Toolkit"))
            {
                hasUsingDirective = true;
                break;
            }
        }
        Assert.False(hasUsingDirective, "@using directive entry should not be emitted when importsFile is null");
    }
}
