// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.Helpers.Syncfusion;

public class SyncfusionBlazorToolkitHelperTests : IDisposable
{
    private readonly string _tempDir;

    public SyncfusionBlazorToolkitHelperTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sfbt-helper-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private const string SampleConfig = """
        {
          "Files": [
            {
              "FileName": "Program.cs",
              "Usings": [ "Syncfusion.Blazor.Toolkit" ]
            }
          ]
        }
        """;

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_KeepsOnlyProgramCs()
    {
        var configPath = WriteConfig(SampleConfig);
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(configPath);
        Assert.NotNull(json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(1, files.GetArrayLength());
        Assert.Equal("Program.cs", files[0].GetProperty("FileName").GetString());
        Assert.DoesNotContain("fluent.min.css", json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_MissingFile_ReturnsNull()
    {
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            Path.Combine(_tempDir, "does-not-exist.json"));
        Assert.Null(json);
    }

    [Fact]
    public void EnsureImportsUsing_AppendsDirective_AndIsIdempotent()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var imports = Path.Combine(_tempDir, "_Imports.razor");
        File.WriteAllText(imports, "@using Microsoft.AspNetCore.Components\n");

        var first = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);
        var second = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Inserted, first.Outcome);
        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.AlreadyPresent, second.Outcome);
        Assert.Equal("_Imports.razor", first.ImportsFile);

        string text = File.ReadAllText(imports);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", text);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(text, "@using Syncfusion\\.Blazor\\.Toolkit"));
    }

    [Fact]
    public void EnsureImportsUsing_DetectsDirectiveInRazorComment_AndTreatsAsAbsent()
    {
        // The directive must NOT be detected when it appears only
        // inside an @* … *@ Razor comment block — otherwise the
        // scaffolder would treat it as already present and skip
        // writing it.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var imports = Path.Combine(_tempDir, "_Imports.razor");
        File.WriteAllText(imports,
            "@using Microsoft.AspNetCore.Components\n" +
            "@* @using Syncfusion.Blazor.Toolkit *@\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(imports);
        // Strip out the razor block comment before counting: the test
        // asserts that exactly one real (non-commented) directive is
        // present in the file. The original commented one is also
        // matched by the regex, so we count matches outside the
        // comment explicitly.
        int nonCommented = CountRealMatches(text);
        Assert.Equal(1, nonCommented);
    }

    private static int CountRealMatches(string text)
    {
        int real = 0;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            int start = line.IndexOf("@*", StringComparison.Ordinal);
            string searchable = start >= 0 ? line[..start] : line;
            int end = searchable.IndexOf("*@", StringComparison.Ordinal);
            if (end >= 0)
            {
                searchable = searchable[..end];
            }
            if (searchable.Contains("@using Syncfusion.Blazor.Toolkit", StringComparison.Ordinal))
            {
                real++;
            }
        }
        return real;
    }

    [Fact]
    public void EnsureImportsUsing_DetectsRealDirective_NotCommentSubstring()
    {
        // The directive is real and present — the helper must detect
        // it on the line that is not a comment.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var imports = Path.Combine(_tempDir, "_Imports.razor");
        File.WriteAllText(imports,
            "@* @using Syncfusion.Blazor.Toolkit *@\n" +
            "@using Syncfusion.Blazor.Toolkit\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.AlreadyPresent, result.Outcome);
    }

    [Fact]
    public void EnsureImportsUsing_WritesNestedComponentsImports()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var components = Path.Combine(_tempDir, "Components");
        Directory.CreateDirectory(components);
        File.WriteAllText(Path.Combine(components, "_Imports.razor"), "@using Microsoft.AspNetCore.Components.Web\r\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Contains("@using Syncfusion.Blazor.Toolkit", File.ReadAllText(Path.Combine(components, "_Imports.razor")));
        // Must be the OS-native relative path, e.g. "Components\_Imports.razor" on Windows.
        Assert.Equal(Path.Combine("Components", "_Imports.razor"), result.ImportsFile);
    }

    [Fact]
    public void EnsureImportsUsing_CreatesImportsFile_WhenMissing()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        // No _Imports.razor anywhere.

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Created, result.Outcome);
        // Created at the project root (no Components folder present).
        Assert.True(File.Exists(Path.Combine(_tempDir, "_Imports.razor")));
    }

    [Fact]
    public void EnsureImportsUsing_CreatesImportsFileUnderComponents_WhenComponentsExists()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        Directory.CreateDirectory(Path.Combine(_tempDir, "Components"));

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Created, result.Outcome);
        Assert.True(File.Exists(Path.Combine(_tempDir, "Components", "_Imports.razor")));
        Assert.False(File.Exists(Path.Combine(_tempDir, "_Imports.razor")));
    }

    [Fact]
    public void EnsureImportsUsing_PrefersNestedScope_OverUnrelatedFiles()
    {
        // bin/_Imports.razor must be ignored.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var bin = Path.Combine(_tempDir, "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(bin, "_Imports.razor"), "@using BinScope\n");
        var pages = Path.Combine(_tempDir, "Pages");
        Directory.CreateDirectory(pages);
        File.WriteAllText(Path.Combine(pages, "_Imports.razor"), "@using PagesScope\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(Path.Combine("Pages", "_Imports.razor"), result.ImportsFile);
        string text = File.ReadAllText(Path.Combine(pages, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", text);
    }

    [Fact]
    public void EnsureImportsUsing_PreservesLineEndings()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var imports = Path.Combine(_tempDir, "_Imports.razor");
        File.WriteAllText(imports, "@using Microsoft.AspNetCore.Components\r\n");

        SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        // Both the existing line and the new one must keep CRLF.
        string text = File.ReadAllText(imports);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit\r\n", text);
    }

    private string WriteConfig(string contents)
    {
        var path = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(path, contents);
        return path;
    }
}