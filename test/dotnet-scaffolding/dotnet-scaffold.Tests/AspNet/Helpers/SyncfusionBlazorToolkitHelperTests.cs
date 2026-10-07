// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.Helpers;

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

    private const string SampleConfig = @"{
  ""Files"": [
    {
      ""FileName"": ""Program.cs"",
      ""Methods"": {
        ""Global"": {
          ""CodeChanges"": [
            {
              ""InsertAfter"": ""WebApplication.CreateBuilder"",
              ""CheckBlock"": ""AddSyncfusionBlazorToolkit"",
              ""Block"": ""builder.Services.AddSyncfusionBlazorToolkit();"",
              ""LeadingTrivia"": { ""Newline"": true }
            }
          ]
        }
      },
      ""Usings"": [ ""Syncfusion.Blazor.Toolkit"" ]
    },
    {
      ""FileName"": ""Components\\_Imports.razor"",
      ""Replacements"": [
        {
          ""ReplaceSnippet"": [ ""@using Microsoft.AspNetCore.Components.Forms"" ],
          ""MultiLineBlock"": [
            ""@using Microsoft.AspNetCore.Components.Forms"",
            ""@using Syncfusion.Blazor.Toolkit""
          ],
          ""CheckBlock"": ""Syncfusion.Blazor.Toolkit""
        }
      ]
    },
    {
      ""FileName"": ""$(ThemeFile)"",
      ""Replacements"": [
        {
          ""ReplaceSnippet"": [ ""</head>"" ],
          ""MultiLineBlock"": [
            ""    <link href=\""_content/Syncfusion.Blazor.Toolkit/styles/fluent.min.css\"" rel=\""stylesheet\"" />"",
            ""</head>""
          ],
          ""CheckBlock"": ""Syncfusion.Blazor.Toolkit/styles/fluent.min.css""
        }
      ]
    }
  ]
}
";

    [Fact]
    public void CanonicalizePath_NullOrEmpty_ReturnsInputUnchanged()
    {
        Assert.Null(SyncfusionBlazorToolkitHelper.CanonicalizePath(null));
        Assert.Equal(string.Empty, SyncfusionBlazorToolkitHelper.CanonicalizePath(string.Empty));
    }

    [Fact]
    public void CanonicalizePath_ReplacesBackslashesWithForwardSlashes()
    {
        Assert.Equal(
            "Components/App.razor",
            SyncfusionBlazorToolkitHelper.CanonicalizePath(@"Components\App.razor"));
        Assert.Equal(
            "wwwroot/index.html",
            SyncfusionBlazorToolkitHelper.CanonicalizePath(@"wwwroot\index.html"));
    }

    [Fact]
    public void CanonicalizePath_LeavesForwardSlashesAlone()
    {
        Assert.Equal(
            "Components/App.razor",
            SyncfusionBlazorToolkitHelper.CanonicalizePath("Components/App.razor"));
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_NoThemeNoImports_RemovesPlaceholders()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: null,
            importsFile: null);

        Assert.NotNull(json);
        Assert.DoesNotContain("$(ThemeFile)", json);
        Assert.DoesNotContain("Components\\\\_Imports.razor", json);
        // Program.cs entry is preserved.
        Assert.Contains("\"Program.cs\"", json);
        // The other block contents are gone (no anchor, no theme link, no
        // Forms anchor).
        Assert.DoesNotContain("fluent.min.css", json);
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Forms", json);
        using var _ = JsonDocument.Parse(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithAppRazor_ReplacesPlaceholderWithAppRazor()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("Components", "App.razor"),
            importsFile: null);

        Assert.NotNull(json);
        Assert.DoesNotContain("$(ThemeFile)", json);
        // Path is JSON-escaped: backslashes are escaped as \\ and the
        // helper canonicalizes to forward slashes.
        Assert.Contains("Components/App.razor", json);
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(2, files.GetArrayLength());
        // Second entry is the resolved theme file.
        Assert.Equal("Components/App.razor", files[1].GetProperty("FileName").GetString());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithCanonicalThemePath_UsesForwardSlashes()
    {
        var configPath = WriteConfig(SampleConfig);

        // Pass a Windows-style path; the helper should canonicalize to forward slashes
        // before writing the JSON, so Windows and Linux produce the same output.
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: "Components\\App.razor",
            importsFile: null);

        Assert.NotNull(json);
        Assert.DoesNotContain("Components\\\\App.razor", json);
        Assert.Contains("Components/App.razor", json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithIndexHtml_ReplacesPlaceholderWithIndexHtml()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("wwwroot", "index.html"),
            importsFile: null);

        Assert.NotNull(json);
        Assert.DoesNotContain("$(ThemeFile)", json);
        Assert.Contains("wwwroot/index.html", json);
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(2, files.GetArrayLength());
        Assert.Equal("wwwroot/index.html", files[1].GetProperty("FileName").GetString());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithImportsFile_AddsAnchorFreeBlock()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: null,
            importsFile: Path.Combine("Components", "_Imports.razor"));

        Assert.NotNull(json);
        Assert.DoesNotContain("Components\\\\_Imports.razor", json);
        // The new entry uses a Block-only snippet, not the fragile
        // Microsoft.AspNetCore.Components.Forms anchor.
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Forms", json);
        Assert.Contains("Components/_Imports.razor", json);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(2, files.GetArrayLength());
        var importsEntry = files[1];
        Assert.Equal("Components/_Imports.razor", importsEntry.GetProperty("FileName").GetString());
        // The Replacements array carries a single Block-only snippet.
        var replacements = importsEntry.GetProperty("Replacements");
        Assert.Equal(1, replacements.GetArrayLength());
        Assert.Equal("@using Syncfusion.Blazor.Toolkit", replacements[0].GetProperty("Block").GetString());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithBothThemeAndImports_EmitsBothEntries()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: "Components/App.razor",
            importsFile: "_Imports.razor");

        Assert.NotNull(json);
        Assert.DoesNotContain("$(ThemeFile)", json);
        Assert.DoesNotContain("Components\\\\_Imports.razor", json);
        Assert.Contains("Components/App.razor", json);
        Assert.Contains("\"_Imports.razor\"", json);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", json);
        Assert.Contains("fluent.min.css", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(3, files.GetArrayLength());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_MissingFile_ReturnsNull()
    {
        var missingPath = Path.Combine(_tempDir, "does-not-exist.json");
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            missingPath, themeFile: null, importsFile: null);
        Assert.Null(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_InvalidJson_ReturnsNull()
    {
        var configPath = WriteConfig("{ not valid json");
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath, themeFile: null, importsFile: null);
        Assert.Null(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_EmptyFile_ReturnsNull()
    {
        var configPath = WriteConfig(string.Empty);
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath, themeFile: null, importsFile: null);
        Assert.Null(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_MissingFilesArray_ReturnsNull()
    {
        var configPath = WriteConfig(@"{ ""Options"": [] }");
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath, themeFile: null, importsFile: null);
        Assert.Null(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_PlaceholderAbsent_AppendsResolvedEntries()
    {
        // When both placeholders are absent (e.g. someone hand-edits the JSON
        // to remove them), the helper should still append the resolved
        // entries so the scaffolder still works.
        const string withoutPlaceholders = @"{
  ""Files"": [
    {
      ""FileName"": ""Program.cs"",
      ""Methods"": { ""Global"": { ""CodeChanges"": [] } }
    }
  ]
}";
        var configPath = WriteConfig(withoutPlaceholders);
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("Components", "App.razor"),
            importsFile: Path.Combine("Components", "_Imports.razor"));

        Assert.NotNull(json);
        Assert.Contains("Components/App.razor", json);
        Assert.Contains("Components/_Imports.razor", json);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(3, doc.RootElement.GetProperty("Files").GetArrayLength());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_NullLogger_DoesNotThrow()
    {
        var configPath = WriteConfig("{ not valid json");
        // Should not throw NullReferenceException when no logger is provided.
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath, themeFile: null, importsFile: null, logger: null);
        Assert.Null(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithLogger_DoesNotThrowOnInvalidJson()
    {
        var configPath = WriteConfig("{ not valid json");
        var logger = NullLogger.Instance;
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath, themeFile: null, importsFile: null, logger);
        Assert.Null(json);
    }

    private string WriteConfig(string contents)
    {
        var path = Path.Combine(_tempDir, "syncfusionBlazorToolkitChanges.json");
        File.WriteAllText(path, contents);
        return path;
    }
}
