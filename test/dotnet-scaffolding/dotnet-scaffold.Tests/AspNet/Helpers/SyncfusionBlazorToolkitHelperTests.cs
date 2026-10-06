// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
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
    public void BuildResolvedCodeModifierConfigJson_NoThemeFile_RemovesPlaceholderEntry()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(configPath, themeFile: null);

        Assert.NotNull(json);
        // Placeholder entry is removed entirely.
        Assert.DoesNotContain("$(ThemeFile)", json);
        // The other two file entries are still present.
        Assert.Contains("\"Program.cs\"", json);
        Assert.Contains("Components\\\\_Imports.razor", json);
        // The theme link replacement is also gone (it lived in the removed
        // entry only).
        Assert.DoesNotContain("fluent.min.css", json);
        // Resulting JSON is valid.
        using var _ = JsonDocument.Parse(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithAppRazor_ReplacesPlaceholderWithAppRazor()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("Components", "App.razor"));

        Assert.NotNull(json);
        Assert.DoesNotContain("$(ThemeFile)", json);
        // Path is JSON-escaped: backslashes are escaped as \\.
        Assert.Contains("Components\\\\App.razor", json);
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", json);
        // Valid JSON.
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(3, files.GetArrayLength());
        // Third entry is the resolved theme file.
        Assert.Equal("Components\\App.razor", files[2].GetProperty("FileName").GetString());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithIndexHtml_ReplacesPlaceholderWithIndexHtml()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("wwwroot", "index.html"));

        Assert.NotNull(json);
        Assert.DoesNotContain("$(ThemeFile)", json);
        Assert.Contains("wwwroot\\\\index.html", json);
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(3, files.GetArrayLength());
        Assert.Equal("wwwroot\\index.html", files[2].GetProperty("FileName").GetString());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_MissingFile_ReturnsNull()
    {
        var missingPath = Path.Combine(_tempDir, "does-not-exist.json");
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(missingPath, themeFile: null);
        Assert.Null(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_InvalidJson_ReturnsNull()
    {
        var configPath = WriteConfig("{ not valid json");
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(configPath, themeFile: null);
        Assert.Null(json);
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_PlaceholderAbsent_AppendsResolvedEntry()
    {
        // When the placeholder is absent (e.g. someone hand-edits the JSON
        // to remove the theme entry), the helper should still append the
        // resolved theme entry so the scaffolder still works.
        const string withoutPlaceholder = @"{
  ""Files"": [
    {
      ""FileName"": ""Program.cs"",
      ""Methods"": { ""Global"": { ""CodeChanges"": [] } }
    }
  ]
}";
        var configPath = WriteConfig(withoutPlaceholder);
        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("Components", "App.razor"));

        Assert.NotNull(json);
        Assert.Contains("Components\\\\App.razor", json);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(2, doc.RootElement.GetProperty("Files").GetArrayLength());
    }

    private string WriteConfig(string contents)
    {
        var path = Path.Combine(_tempDir, "syncfusionBlazorToolkitChanges.json");
        File.WriteAllText(path, contents);
        return path;
    }
}
