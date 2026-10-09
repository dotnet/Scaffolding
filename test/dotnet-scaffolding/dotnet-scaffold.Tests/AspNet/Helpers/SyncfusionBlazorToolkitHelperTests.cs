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

        SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project, "_Imports.razor");
        SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project, "_Imports.razor");

        string text = File.ReadAllText(imports);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", text);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(text, "@using Syncfusion\\.Blazor\\.Toolkit"));
    }

    [Fact]
    public void EnsureServiceRegistration_InsertsAfterCreateBuilder_AndIsIdempotent()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program, "var builder = WebApplication.CreateBuilder(args);\nbuilder.Build().Run();\n");

        SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(project);
        SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(project);

        string text = File.ReadAllText(program);
        Assert.Contains("using Syncfusion.Blazor.Toolkit;", text);
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();", text);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(text, "AddSyncfusionBlazorToolkit"));
        int create = text.IndexOf("CreateBuilder", StringComparison.Ordinal);
        int register = text.IndexOf("AddSyncfusionBlazorToolkit", StringComparison.Ordinal);
        Assert.True(register > create);
    }

    [Fact]
    public void EnsureImportsUsing_WritesNestedComponentsImports()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var components = Path.Combine(_tempDir, "Components");
        Directory.CreateDirectory(components);
        File.WriteAllText(Path.Combine(components, "_Imports.razor"), "@using Microsoft.AspNetCore.Components.Web\r\n");

        SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project, Path.Combine("Components", "_Imports.razor"));

        Assert.Contains("@using Syncfusion.Blazor.Toolkit", File.ReadAllText(Path.Combine(components, "_Imports.razor")));
    }

    private string WriteConfig(string contents)
    {
        var path = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(path, contents);
        return path;
    }
}
