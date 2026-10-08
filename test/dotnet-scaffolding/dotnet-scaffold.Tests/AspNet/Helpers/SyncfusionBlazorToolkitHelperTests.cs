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

    // Sample JSON that includes the original placeholder-style entries
    // (Components\_Imports.razor and $(ThemeFile)) so the helper can
    // exercise the filter-and-rewrite path.
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
              ""Block"": ""builder.Services.AddSyncfusionBlazorToolkit()"",
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

    // Expected OS-native separator for the host platform.
    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();
    private static readonly string OsAppRazor = "Components" + Sep + "App.razor";
    private static readonly string OsImportsRazor = "Components" + Sep + "_Imports.razor";
    private static readonly string OsIndexHtml = "wwwroot" + Sep + "index.html";

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
    }

    [Fact]
    public void ToOsNativePath_NullOrEmpty_ReturnsInputUnchanged()
    {
        Assert.Null(SyncfusionBlazorToolkitHelper.ToOsNativePath(null));
        Assert.Equal(string.Empty, SyncfusionBlazorToolkitHelper.ToOsNativePath(string.Empty));
    }

    [Fact]
    public void ToOsNativePath_UsesOsNativeSeparator()
    {
        string? result = SyncfusionBlazorToolkitHelper.ToOsNativePath("Components/App.razor");
        Assert.Equal(OsAppRazor, result);
        // And the round-trip via backslashes.
        string? result2 = SyncfusionBlazorToolkitHelper.ToOsNativePath(@"Components\App.razor");
        Assert.Equal(OsAppRazor, result2);
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
        // Program.cs entry is preserved.
        Assert.Contains("\"Program.cs\"", json);
        // The other block contents are gone (no anchor, no theme link, no
        // Forms anchor) because the placeholders were filtered out and no
        // replacements were re-emitted.
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
        // Path uses OS-native separators (NOT canonical forward slashes)
        // so that MSBuildWorkspace's AdditionalDocument.FilePath lookup
        // succeeds on Windows.
        Assert.Contains(EscapeForJsonString(OsAppRazor), json);
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(2, files.GetArrayLength());
        // Second entry is the resolved theme file.
        Assert.Equal(OsAppRazor, files[1].GetProperty("FileName").GetString());
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
        Assert.Contains(EscapeForJsonString(OsIndexHtml), json);
        Assert.Contains("Syncfusion.Blazor.Toolkit/styles/fluent.min.css", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(2, files.GetArrayLength());
        Assert.Equal(OsIndexHtml, files[1].GetProperty("FileName").GetString());
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
        // The new entry uses a Block-only snippet, not the fragile
        // Microsoft.AspNetCore.Components.Forms anchor.
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Forms", json);
        Assert.Contains(EscapeForJsonString(OsImportsRazor), json);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(2, files.GetArrayLength());
        var importsEntry = files[1];
        Assert.Equal(OsImportsRazor, importsEntry.GetProperty("FileName").GetString());
        // The Replacements array carries a single Block-only snippet.
        var replacements = importsEntry.GetProperty("Replacements");
        Assert.Equal(1, replacements.GetArrayLength());
        // The Block carries a leading and trailing newline so the appended
        // snippet does not glue to the surrounding lines of the file.
        Assert.Equal("\n@using Syncfusion.Blazor.Toolkit\n", replacements[0].GetProperty("Block").GetString());
        // CheckBlock makes re-runs idempotent even with the extra newlines.
        Assert.Equal("Syncfusion.Blazor.Toolkit", replacements[0].GetProperty("CheckBlock").GetString());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_WithBothThemeAndImports_EmitsBothEntries()
    {
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("Components", "App.razor"),
            importsFile: Path.Combine("Components", "_Imports.razor"));

        Assert.NotNull(json);
        Assert.DoesNotContain("$(ThemeFile)", json);
        Assert.Contains(EscapeForJsonString(OsAppRazor), json);
        Assert.Contains(EscapeForJsonString(OsImportsRazor), json);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", json);
        Assert.Contains("fluent.min.css", json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(3, files.GetArrayLength());
    }

    [Fact]
    public void BuildResolvedCodeModifierConfigJson_EmittedFileNames_MatchOsNativePaths()
    {
        // The bug: the helper used to canonicalize the emitted FileName
        // to forward slashes, which silently broke MSBuildWorkspace's
        // EndsWith lookup on Windows and caused the CodeModifier to skip
        // every theme and using-directive change. Guard against
        // regression by asserting the emitted FileName uses the OS-native
        // separator that Path.GetRelativePath / AdditionalDocument.FilePath
        // would produce.
        var configPath = WriteConfig(SampleConfig);

        var json = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
            configPath,
            themeFile: Path.Combine("Components", "App.razor"),
            importsFile: Path.Combine("Components", "_Imports.razor"));

        Assert.NotNull(json);
        using var doc = JsonDocument.Parse(json);
        var files = doc.RootElement.GetProperty("Files");
        Assert.Equal(3, files.GetArrayLength());

        // The theme and imports entries must NOT contain forward slashes
        // when running on Windows (where backslashes are native). On
        // Linux they are allowed because that IS the OS-native form.
        for (int i = 1; i < files.GetArrayLength(); i++)
        {
            string emittedFileName = files[i].GetProperty("FileName").GetString()!;
            if (Path.DirectorySeparatorChar == '\\')
            {
                Assert.DoesNotContain('/', emittedFileName);
                Assert.Contains("\\", emittedFileName);
            }
            else
            {
                Assert.DoesNotContain('\\', emittedFileName);
                Assert.Contains("/", emittedFileName);
            }
        }
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

    /// <summary>
    /// JSON-escapes a path string for substring matching against a
    /// serialized JSON document. On Windows, backslashes are encoded
    /// as <c>\\</c> in the JSON, so the substring needs the same
    /// encoding. On Linux, no escaping is needed.
    /// </summary>
    private static string EscapeForJsonString(string s)
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            return s.Replace("\\", "\\\\");
        }
        return s;
    }
}
