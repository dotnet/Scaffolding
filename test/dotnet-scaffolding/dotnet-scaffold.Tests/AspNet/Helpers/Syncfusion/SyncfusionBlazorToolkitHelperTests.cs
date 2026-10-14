// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.Helpers.Syncfusion;

public class SyncfusionBlazorToolkitHelperTests : IDisposable
{
    private readonly string _tempDir;

    private const string WebProjectXml =
        "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";

    public SyncfusionBlazorToolkitHelperTests()
    {
        if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
        {
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
        }

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

    [Fact]
    public void EnsureImportsUsing_MultilineRazorComment_NotCountedAsPresent()
    {
        // The directive must NOT be detected when it appears only
        // inside a multi-line @* … *@ Razor comment block that spans
        // several lines. Comment-aware detection (single-line
        // stripping alone is not enough) is required.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var imports = Path.Combine(_tempDir, "_Imports.razor");
        File.WriteAllText(imports,
            "@using Microsoft.AspNetCore.Components\n" +
            "@*\n" +
            "  @using Syncfusion.Blazor.Toolkit\n" +
            "  This is a long comment that\n" +
            "  spans several lines\n" +
            "*@\n");

        // The helper's strict, comment-aware detector must report
        // the directive as NOT present (the only occurrence is inside
        // a multi-line comment) — so the helper must add one.
        Assert.False(SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(
            "@*\n  @using Syncfusion.Blazor.Toolkit\n  spans lines\n*@\n"));

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(imports);

        // The output now contains a real (un-commented) Toolkit
        // using directive. The line-by-line counter used here is
        // not enough to track multi-line comments, so we just check
        // the strict, helper-level detector.
        Assert.True(SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(text));

        // The appended line is at the END of the file (after the
        // closing *@), so it is on its own line, not inside the
        // comment, and the strict detector sees it.
        string[] lines = text.Split('\n');
        string lastNonEmpty = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Last();
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", lastNonEmpty);
    }

    [Fact]
    public void EnsureImportsUsing_SubNamespaceUsingOnly_StillInsertsExactToolkitUsing()
    {
        // `@using Syncfusion.Blazor.Toolkit.Buttons` must NOT count as
        // the Toolkit directive — the scaffolder must still add the
        // exact `@using Syncfusion.Blazor.Toolkit` line.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var imports = Path.Combine(_tempDir, "_Imports.razor");
        File.WriteAllText(imports, "@using Syncfusion.Blazor.Toolkit.Buttons\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(imports);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit\n", text);
        Assert.Contains("@using Syncfusion.Blazor.Toolkit.Buttons", text);
    }

    [Fact]
    public void DirectiveAlreadyPresent_ExactDirective_NotInsideLargerIdentifier()
    {
        // The exact directive must be detected. A line with
        // "XSyncfusion.Blazor.Toolkit" (a larger identifier that
        // happens to contain the namespace as a suffix) must NOT be
        // treated as the directive.
        Assert.True(SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(
            "@using Syncfusion.Blazor.Toolkit\n"));
        Assert.True(SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(
            "@using   Syncfusion.Blazor.Toolkit   \n"));
        Assert.False(SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(
            "@using Syncfusion.Blazor.Toolkit.Buttons\n"));
        Assert.False(SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(
            "XSyncfusion.Blazor.Toolkit\n"));
        Assert.False(SyncfusionBlazorToolkitHelper.DirectiveAlreadyPresent(
            "@*\n@using Syncfusion.Blazor.Toolkit\n*@\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_RecognisesStandaloneCall()
    {
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddSyncfusionBlazorToolkit();\nvar app = builder.Build();\n"));
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "var wasmBuilder = WebAssemblyHostBuilder.CreateDefault(args);\nwasmBuilder.Services.AddSyncfusionBlazorToolkit();\nawait wasmBuilder.Build().RunAsync();\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_RejectsChainedAndCommentedCalls()
    {
        const string head = "var builder = WebApplication.CreateBuilder(args);\n";
        const string tail = "var app = builder.Build();\n";
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            head + "builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddSyncfusionBlazorToolkit();\n" + tail));
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            head + "// builder.Services.AddSyncfusionBlazorToolkit();\n" + tail));
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            head + "/* builder.Services.AddSyncfusionBlazorToolkit(); */\n" + tail));
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(""));
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

    /// (no-op helper to keep the build clean)


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
    public void EnsureImportsUsing_PrefersConventionalScope_OverNarrowNestedFile()
    {
        // Only a narrow nested _Imports.razor (Pages/_Imports.razor) and
        // an excluded bin/_Imports.razor exist. The scaffolder must NOT
        // pick the narrow Pages file (it would not cover Layout/Shared
        // components) — it should create an app-wide _Imports.razor at
        // the project root instead, and bin/_Imports.razor must be
        // ignored.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var bin = Path.Combine(_tempDir, "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(bin, "_Imports.razor"), "@using BinScope\n");
        var pages = Path.Combine(_tempDir, "Pages");
        Directory.CreateDirectory(pages);
        File.WriteAllText(Path.Combine(pages, "_Imports.razor"), "@using PagesScope\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Created, result.Outcome);
        Assert.Equal("_Imports.razor", result.ImportsFile);
        Assert.True(File.Exists(Path.Combine(_tempDir, "_Imports.razor")));
        // bin/_Imports.razor must NOT be touched.
        string binText = File.ReadAllText(Path.Combine(bin, "_Imports.razor"));
        Assert.DoesNotContain("@using Syncfusion.Blazor.Toolkit", binText);
        // Pages/_Imports.razor must NOT be modified — the directive
        // is added at the conventional scope instead.
        string pagesText = File.ReadAllText(Path.Combine(pages, "_Imports.razor"));
        Assert.DoesNotContain("@using Syncfusion.Blazor.Toolkit", pagesText);
    }

    [Fact]
    public void EnsureImportsUsing_OnlyNested_StillCreatesConventionalScope_AndCoversApp()
    {
        // Project has only a Pages/_Imports.razor (which doesn't cover
        // Layout/Shared components) and no Components folder. The
        // scaffolder must create the project-root _Imports.razor and
        // add the directive there so every component sees it.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project />");
        var pages = Path.Combine(_tempDir, "Pages");
        Directory.CreateDirectory(pages);
        File.WriteAllText(Path.Combine(pages, "_Imports.razor"), "@using PagesScope\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(project);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Created, result.Outcome);
        string rootImports = File.ReadAllText(Path.Combine(_tempDir, "_Imports.razor"));
        Assert.Contains("@using Syncfusion.Blazor.Toolkit", rootImports);
        // Pages/_Imports.razor is left alone (the user can decide
        // whether to keep or remove it; updating it would be the old,
        // wrong behaviour).
        string pagesText = File.ReadAllText(Path.Combine(pages, "_Imports.razor"));
        Assert.DoesNotContain("@using Syncfusion.Blazor.Toolkit", pagesText);
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

    // ------------------------------------------------------------------
    // EnsureServiceRegistration tests
    // ------------------------------------------------------------------

    private const string WebAppProgramCs = """
        using Microsoft.AspNetCore.Components;

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.Run();
        """;

    private const string StandaloneWasmProgramCs = """
        using Microsoft.AspNetCore.Components;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        await builder.Build().RunAsync();
        """;

    [Fact]
    public void EnsureServiceRegistration_WebApp_AppendsCallAndUsing()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program, WebAppProgramCs);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        Assert.Equal("Program.cs", result.ProgramCsRelative);

        string text = File.ReadAllText(program);
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();", text);
        Assert.Contains("using Syncfusion.Blazor.Toolkit;", text);
    }

    [Fact]
    public void EnsureServiceRegistration_WebApp_InsertsAfterBuilderLine_NotAtEnd()
    {
        // The service registration call must land immediately after
        // the `var builder = WebApplication.CreateBuilder(...)`
        // declaration, not at the end of the file. A late insertion
        // happens *after* `builder.Build()` and therefore never
        // participates in the application's DI container.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program, WebAppProgramCs);

        SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        string text = File.ReadAllText(program);
        string[] lines = text.Split('\n');

        int builderLine = -1;
        int registrationLine = -1;
        int buildLine = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').Trim();
            if (trimmed.StartsWith("var builder = WebApplication.CreateBuilder", StringComparison.Ordinal))
            {
                builderLine = i;
            }
            else if (trimmed == "builder.Services.AddSyncfusionBlazorToolkit();")
            {
                registrationLine = i;
            }
            else if (trimmed == "var app = builder.Build();")
            {
                buildLine = i;
            }
        }

        Assert.True(builderLine >= 0, $"Program.cs must still contain the var builder = ... line. Lines: [{string.Join(" | ", lines.Select(l => l.TrimEnd('\r')))}]");
        Assert.True(registrationLine >= 0, $"Program.cs must contain the registration line. Lines: [{string.Join(" | ", lines.Select(l => l.TrimEnd('\r')))}]");
        Assert.True(buildLine >= 0, $"Program.cs must still contain the var app = builder.Build(); line. Lines: [{string.Join(" | ", lines.Select(l => l.TrimEnd('\r')))}]");
        // The registration must come AFTER the builder declaration
        // and BEFORE the build/run call. Anything else means the
        // services will not be wired into the container.
        Assert.True(registrationLine > builderLine,
            $"Registration line (index {registrationLine}) must come after the var builder line (index {builderLine}).");
        Assert.True(registrationLine < buildLine,
            $"Registration line (index {registrationLine}) must come before the var app = builder.Build(); line (index {buildLine}).");
    }

    [Fact]
    public void EnsureServiceRegistration_StandaloneWasm_InsertsAfterBuilderLine_NotAtEnd()
    {
        // Same as the Web App test, but for the standalone Blazor
        // WebAssembly host. The registration must land right after
        // `var builder = WebAssemblyHostBuilder.CreateDefault(args);`
        // so it precedes `await builder.Build().RunAsync();`.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program, StandaloneWasmProgramCs);

        SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.StandaloneWasm);

        string text = File.ReadAllText(program);
        string[] lines = text.Split('\n');

        int builderLine = -1;
        int registrationLine = -1;
        int runLine = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').Trim();
            if (trimmed.StartsWith("var builder = WebAssemblyHostBuilder.CreateDefault", StringComparison.Ordinal))
            {
                builderLine = i;
            }
            else if (trimmed == "builder.Services.AddSyncfusionBlazorToolkit();")
            {
                registrationLine = i;
            }
            else if (trimmed.StartsWith("await builder.Build().RunAsync", StringComparison.Ordinal))
            {
                runLine = i;
            }
        }

        Assert.True(builderLine >= 0, $"Program.cs must still contain the var builder = ... line. Lines: [{string.Join(" | ", lines.Select(l => l.TrimEnd('\r')))}]");
        Assert.True(registrationLine >= 0, $"Program.cs must contain the registration line. Lines: [{string.Join(" | ", lines.Select(l => l.TrimEnd('\r')))}]");
        Assert.True(runLine >= 0, $"Program.cs must still contain the await builder.Build().RunAsync(); line. Lines: [{string.Join(" | ", lines.Select(l => l.TrimEnd('\r')))}]");
        Assert.True(registrationLine > builderLine,
            $"Registration line (index {registrationLine}) must come after the var builder line (index {builderLine}).");
        Assert.True(registrationLine < runLine,
            $"Registration line (index {registrationLine}) must come before the await builder.Build().RunAsync(); line (index {runLine}).");
    }

    [Fact]
    public void EnsureServiceRegistration_BlankFile_SkipsAndLeavesFileUntouched()
    {
        // No host-builder declaration to anchor on: appending at the end
        // would register after Build()/Run(), so the file must be left alone.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program, "");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.Equal("", File.ReadAllText(program));
    }

    [Fact]
    public void EnsureServiceRegistration_MultilineBuilderDeclaration_InsertsAfterWholeStatement()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "var builder = WebApplication.CreateBuilder(new WebApplicationOptions\n" +
            "{\n" +
            "    Args = args,\n" +
            "});\n" +
            "var app = builder.Build();\n" +
            "app.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(program);
        Assert.Contains("});\nbuilder.Services.AddSyncfusionBlazorToolkit();\nvar app", text);
    }

    [Fact]
    public void EnsureServiceRegistration_StringLiteralMentioningCall_IsNotTreatedAsRegistered()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "var note = \"builder.Services.AddSyncfusionBlazorToolkit();\";\n" +
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "var app = builder.Build();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
    }

    [Theory]
    [InlineData("var note = \"builder.Services.AddSyncfusionBlazorToolkit();\";", false)]
    [InlineData("var note = @\"builder.Services.AddSyncfusionBlazorToolkit();\";", false)]
    [InlineData("// builder.Services.AddSyncfusionBlazorToolkit();", false)]
    [InlineData("builder.Services.AddSyncfusionBlazorToolkit();", true)]
    [InlineData("builder.Services\n    .AddSyncfusionBlazorToolkit();", true)]
    [InlineData("builder.Services.AddRazorComponents().AddSyncfusionBlazorToolkit();", false)]
    public void IsActiveServiceRegistrationPresent_IgnoresLiteralsAndComments(string code, bool expected)
    {
        Assert.Equal(expected, SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "var builder = WebApplication.CreateBuilder(args);\n" + code + "\nvar app = builder.Build();\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_RejectsCallOnWrongBuilder()
    {
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "var builder = WebApplication.CreateBuilder(args);\nother.Services.AddSyncfusionBlazorToolkit();\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_RejectsCallInsideUnexecutedLocalFunction()
    {
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "void Unused() { builder.Services.AddSyncfusionBlazorToolkit(); }\n" +
            "var app = builder.Build();\n"));
    }

    [Fact]
    public void EnsureServiceRegistration_AmbiguousHostBuilders_SkipsWithoutWriting()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        const string text =
            "var a = WebApplication.CreateBuilder(args);\n" +
            "var b = WebApplication.CreateBuilder(args);\n" +
            "var app = a.Build();\n";
        File.WriteAllText(program, text);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.Equal(text, File.ReadAllText(program));
    }

    [Fact]
    public void EnsureServiceRegistration_BuilderInsideLocalFunction_IsIgnored()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "void Helper() { var other = WebApplication.CreateBuilder(); }\n" +
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "var app = builder.Build();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(program);
        Assert.True(text.IndexOf("builder.Services.AddSyncfusionBlazorToolkit();", StringComparison.Ordinal)
            > text.IndexOf("var builder =", StringComparison.Ordinal));
        Assert.DoesNotContain("other.Services", text);
    }

    [Fact]
    public void EnsureServiceRegistration_PreservesCrlfLineEndings()
    {
        // The file uses CRLF line endings; the inserted registration
        // must keep using CRLF so we do not end up with a file that
        // mixes CRLF and LF.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program, WebAppProgramCs.Replace("\n", "\r\n"));

        SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        string text = File.ReadAllText(program);
        // The inserted registration must use CRLF, matching the rest
        // of the file.
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();\r\n", text);
        // The file must not contain an LF-only wrapped version of the
        // registration line.
        Assert.DoesNotContain("\nbuilder.Services.AddSyncfusionBlazorToolkit();\n", text);
    }

    [Fact]
    public void EnsureServiceRegistration_StandaloneWasm_AppendsCallAndUsing()
    {
        // Reproduces the user-reported failure: a Blazor WebAssembly
        // project whose Program.cs uses WebAssemblyHostBuilder.CreateDefault.
        // Before the fix, the shared JSON recipe wrote
        //   WebAssemblyHostBuilder.CreateDefault.Services.AddSyncfusionBlazorToolkit();
        // which is invalid C#. The dedicated step must write a valid
        // line that uses the discovered builder variable.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program, StandaloneWasmProgramCs);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.StandaloneWasm);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);

        string text = File.ReadAllText(program);
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();", text);
        // The pre-fix broken line must not appear anywhere in the file.
        Assert.DoesNotContain("WebAssemblyHostBuilder.CreateDefault.Services", text);
        Assert.Contains("using Syncfusion.Blazor.Toolkit;", text);
    }

    [Fact]
    public void EnsureServiceRegistration_HonoursCustomBuilderVariableName()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "var appBuilder = WebApplication.CreateBuilder(args);\n" +
            "var app = appBuilder.Build();\n" +
            "app.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);

        string text = File.ReadAllText(program);
        Assert.Contains("appBuilder.Services.AddSyncfusionBlazorToolkit();", text);
        Assert.DoesNotContain("builder.Services", text);
    }

    [Fact]
    public void EnsureServiceRegistration_AlreadyPresent_IsIdempotentNoOp()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "using Syncfusion.Blazor.Toolkit;\n" +
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "builder.Services.AddSyncfusionBlazorToolkit();\n" +
            "var app = builder.Build();\n" +
            "app.Run();\n");

        var first = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);
        var second = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.AlreadyPresent, first.Outcome);
        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.AlreadyPresent, second.Outcome);

        string text = File.ReadAllText(program);
        // Exactly one real registration line.
        int count = System.Text.RegularExpressions.Regex.Matches(
            text,
            @"\.Services\.AddSyncfusionBlazorToolkit\(\)").Count;
        Assert.Equal(1, count);
    }

    [Fact]
    public void EnsureServiceRegistration_CommentedOutCall_StillAppends()
    {
        // The pre-existing commented-out registration must NOT count
        // as a real registration; the step must append a real line.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "// builder.Services.AddSyncfusionBlazorToolkit();\n" +
            "var app = builder.Build();\n" +
            "app.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        // The regex matches both the original commented line and the
        // new active line; we strip comments first to count only the
        // active registrations.
        string text = File.ReadAllText(program);
        string codeOnly = SyncfusionBlazorToolkitHelper.StripCSharpCommentsForTest(text);
        int count = System.Text.RegularExpressions.Regex.Matches(
            codeOnly,
            @"\.Services\.AddSyncfusionBlazorToolkit\(\);").Count;
        Assert.Equal(1, count);
    }

    [Fact]
    public void EnsureServiceRegistration_ChainedCall_StillAppends()
    {
        // A fluent chain like
        //   builder.Services.AddRazorComponents().AddSyncfusionBlazorToolkit();
        // is treated as "not yet registered" by the strict detector
        // (because the `)` before the `.` marks it as a chain, not a
        // standalone statement) so the helper must still append a
        // standalone call.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "builder.Services.AddRazorComponents().AddSyncfusionBlazorToolkit();\n" +
            "var app = builder.Build();\n" +
            "app.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        // The strict detector reports the chained call as not-yet-present,
        // so the helper appends a standalone line.
        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(program);
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();", text);
    }

    [Fact]
    public void EnsureServiceRegistration_EntryPointInOtherFile_EditsItAndNeverCreatesProgramCs()
    {
        // The entry point lives in Hosting.cs. It is found from the compiled
        // sources and edited in place; a top-level Program.cs must never be
        // created next to it (that would trigger CS8802).
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var hosting = Path.Combine(_tempDir, "Hosting.cs");
        File.WriteAllText(hosting, WebAppProgramCs);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        Assert.Equal("Hosting.cs", result.ProgramCsRelative);
        Assert.False(File.Exists(Path.Combine(_tempDir, "Program.cs")));
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(File.ReadAllText(hosting)));
    }

    [Fact]
    public void EnsureServiceRegistration_NoCompiledSources_SkipsAndCreatesNothing()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.Empty(Directory.GetFiles(_tempDir, "*.cs"));
    }

    [Fact]
    public void EnsureServiceRegistration_ProgramCsInSubdirectory_StillFound()
    {
        // User requirement: "if the file present in different location
        // it should add correspondingly". The helper must find
        // Program.cs in a one-level subdirectory of the project.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var srcDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(srcDir);
        var program = Path.Combine(srcDir, "Program.cs");
        File.WriteAllText(program, WebAppProgramCs);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        Assert.Equal(Path.Combine("src", "Program.cs"), result.ProgramCsRelative);
        string text = File.ReadAllText(program);
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();", text);
    }

    [Fact]
    public void EnsureServiceRegistration_ExcludedBinAndObjDirectories_Ignored()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        // bin/ and obj/ are output folders — a Program.cs in them must
        // NOT be treated as the project's entry point.
        Directory.CreateDirectory(Path.Combine(_tempDir, "bin"));
        File.WriteAllText(Path.Combine(_tempDir, "bin", "Program.cs"),
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "var app = builder.Build();\n" +
            "app.Run();\n");
        Directory.CreateDirectory(Path.Combine(_tempDir, "obj"));
        File.WriteAllText(Path.Combine(_tempDir, "obj", "Program.cs"),
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "var app = builder.Build();\n" +
            "app.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        // Neither bin/Program.cs nor obj/Program.cs counts, so the
        // helper skips and must not create a Program.cs.
        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.False(File.Exists(Path.Combine(_tempDir, "Program.cs")));
    }

    [Fact]
    public void EnsureServiceRegistration_FileRemovedFromCompile_NotClaimed()
    {
        // Discovery follows the evaluated Compile items: a Program.cs the
        // project excludes (e.g. a sibling project's folder) is not its
        // entry point, whatever its file name.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project,
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            "<ItemGroup><Compile Remove=\"Sibling/**\" /></ItemGroup></Project>");
        var sibling = Path.Combine(_tempDir, "Sibling");
        Directory.CreateDirectory(sibling);
        File.WriteAllText(Path.Combine(sibling, "Program.cs"),
            "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.False(File.Exists(Path.Combine(_tempDir, "Program.cs")));
    }

    [Fact]
    public void EnsureServiceRegistration_EntryPointNamedHostingCs_IsFoundAndRegistered()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var hosting = Path.Combine(_tempDir, "Hosting.cs");
        File.WriteAllText(hosting, "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        Assert.Equal("Hosting.cs", result.ProgramCsRelative);
        Assert.Contains("builder.Services.AddSyncfusionBlazorToolkit();", File.ReadAllText(hosting));
        Assert.False(File.Exists(Path.Combine(_tempDir, "Program.cs")));
    }

    [Fact]
    public void DiscoverEntryPoint_SeveralCompiledHostFiles_IsAmbiguous()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), "var builder = WebApplication.CreateBuilder(args);\n");
        File.WriteAllText(Path.Combine(_tempDir, "Hosting.cs"), "var builder = WebApplication.CreateBuilder(args);\n");

        Assert.Null(SyncfusionBlazorToolkitHelper.DiscoverEntryPoint(project, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void TryIsPackageReferenced_UsesEvaluatedItems()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project,
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            "<!-- Syncfusion.Blazor.Toolkit -->" +
            "<ItemGroup><PackageReference Include=\"Syncfusion.Blazor.Toolkit.Extras\" Version=\"1.0.0\" /></ItemGroup></Project>");
        Assert.True(SyncfusionBlazorToolkitHelper.TryIsPackageReferenced(project, out bool referenced, out _));
        Assert.False(referenced);

        File.WriteAllText(Path.Combine(_tempDir, "Directory.Build.props"),
            "<Project><ItemGroup><PackageReference Include=\"Syncfusion.Blazor.Toolkit\" Version=\"2.0.0\" /></ItemGroup></Project>");
        Assert.True(SyncfusionBlazorToolkitHelper.TryIsPackageReferenced(project, out referenced, out _));
        Assert.True(referenced);
    }

    [Fact]
    public void TryIsPackageReferenced_EvaluationFailure_ReportsError()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, "<Project><Import Project=\"missing.props\" /></Project>");

        Assert.False(SyncfusionBlazorToolkitHelper.TryIsPackageReferenced(project, out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_RequiresEveryHostBuilder()
    {
        const string two =
            "var a = WebApplication.CreateBuilder(args);\nvar b = WebApplication.CreateBuilder(args);\n";
        const string builds = "a.Build();\nb.Build();\n";
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            two + "a.Services.AddSyncfusionBlazorToolkit();\n" + builds));
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            two + "a.Services.AddSyncfusionBlazorToolkit();\nb.Services.AddSyncfusionBlazorToolkit();\n" + builds));
    }

    [Theory]
    [InlineData("extern alias X;\nvar builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n", "extern alias X;", "using Syncfusion.Blazor.Toolkit;")]
    [InlineData("using System;\nvar builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n", "using System;", "using Syncfusion.Blazor.Toolkit;")]
    [InlineData("// header\nvar builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n", "// header", "using Syncfusion.Blazor.Toolkit;")]
    [InlineData("#nullable enable\nvar builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n", "#nullable enable", "using Syncfusion.Blazor.Toolkit;")]
    public void EnsureServiceRegistration_UsingIsPlacedAfterExternsUsingsAndHeader(string program, string mustComeFirst, string directive)
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var path = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(path, program);

        SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        string text = File.ReadAllText(path);
        Assert.True(text.IndexOf(mustComeFirst, StringComparison.Ordinal) < text.IndexOf(directive, StringComparison.Ordinal));
        Assert.True(text.IndexOf(directive, StringComparison.Ordinal) < text.IndexOf("var builder", StringComparison.Ordinal));
        Assert.Equal(1, text.Split(directive).Length - 1);
    }

    [Fact]
    public void EnsureServiceRegistration_EscapedBuilderIdentifier_KeepsTheEscape()
    {
        // `@class` is a valid identifier whose ValueText is `class`. The generated
        // statement must keep the source spelling, or it would not compile.
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            "var @class = WebApplication.CreateBuilder(args);\nvar app = @class.Build();\napp.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(program);
        Assert.Contains("@class.Services.AddSyncfusionBlazorToolkit();", text);
        Assert.DoesNotContain("\nclass.Services", text);
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(text));
    }

    [Fact]
    public void EnsureServiceRegistration_EmptyProjectPath_ReturnsSkipped()
    {
        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            null,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.Null(result.ProgramCsRelative);
    }

    [Fact]
    public void DetectBuilderVariableName_WebApp_ExtractsName()
    {
        Assert.Equal("builder",
            SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
                WebAppProgramCs,
                SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
        Assert.Equal("appBuilder",
            SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
                "var appBuilder = WebApplication.CreateBuilder(args);\n",
                SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
    }

    [Fact]
    public void DetectBuilderVariableName_StandaloneWasm_ExtractsName()
    {
        Assert.Equal("builder",
            SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
                StandaloneWasmProgramCs,
                SyncfusionBlazorToolkitHelper.ProgramHostKind.StandaloneWasm));
        Assert.Equal("wasmBuilder",
            SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
                "var wasmBuilder = WebAssemblyHostBuilder.CreateDefault(args);\n",
                SyncfusionBlazorToolkitHelper.ProgramHostKind.StandaloneWasm));
    }

    [Fact]
    public void DetectBuilderVariableName_NoBuilderMarker_ReturnsNull()
    {
        Assert.Null(SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
            "var unrelated = 1;\n",
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
        Assert.Null(SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
            string.Empty,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
    }

    // ------------------------------------------------------------------
    // Registration must be on the startup path, parameterless, before Build()
    // ------------------------------------------------------------------

    private const string BuilderDecl = "var builder = WebApplication.CreateBuilder(args);\n";

    [Theory]
    [InlineData("if (false)\n{\n    builder.Services.AddSyncfusionBlazorToolkit();\n}\nvar app = builder.Build();\napp.Run();\n")]
    [InlineData("try { builder.Services.AddSyncfusionBlazorToolkit(); } catch { }\nvar app = builder.Build();\n")]
    [InlineData("for (var i = 0; i < 1; i++) builder.Services.AddSyncfusionBlazorToolkit();\nvar app = builder.Build();\n")]
    [InlineData("var app = builder.Build();\nbuilder.Services.AddSyncfusionBlazorToolkit();\napp.Run();\n")]
    [InlineData("builder.Services.AddSyncfusionBlazorToolkit(true);\nvar app = builder.Build();\n")]
    [InlineData("var app = builder.Build();\napp.Run();\nstatic void Helper(WebApplicationBuilder b) { b.Services.AddSyncfusionBlazorToolkit(); }\n")]
    public void IsActiveServiceRegistrationPresent_RejectsRegistrationThatDoesNotConfigureTheHost(string body)
    {
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(BuilderDecl + body));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_RejectsOrdinaryHelperMethodThatIsNeverCalled()
    {
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            BuilderDecl +
            "var app = builder.Build();\n" +
            "class Setup { public void Configure(WebApplicationBuilder builder) { builder.Services.AddSyncfusionBlazorToolkit(); } }\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_AcceptsRegistrationInStaticMain()
    {
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "class P { static void Main(string[] a) { var builder = WebApplication.CreateBuilder(a);\n" +
            "builder.Services.AddSyncfusionBlazorToolkit();\nbuilder.Build(); } }"));
    }

    [Fact]
    public void EnsureServiceRegistration_RegistrationAfterBuild_IsNotTreatedAsPresent()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            BuilderDecl + "var app = builder.Build();\nbuilder.Services.AddSyncfusionBlazorToolkit();\napp.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(program);
        Assert.True(text.IndexOf("AddSyncfusionBlazorToolkit();", StringComparison.Ordinal)
            < text.IndexOf("builder.Build()", StringComparison.Ordinal));
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(text));
    }

    // ------------------------------------------------------------------
    // Aliased / qualified host builder types; fail closed when unresolved
    // ------------------------------------------------------------------

    private const string AliasHeader =
        "using WebApp = Microsoft.AspNetCore.Builder.WebApplication;\nusing Syncfusion.Blazor.Toolkit;\n";

    [Fact]
    public void IsActiveServiceRegistrationPresent_AliasedBuilder_RegistrationAfterBuild_IsRejected()
    {
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            AliasHeader +
            "var builder = WebApp.CreateBuilder(args);\n" +
            "var app = builder.Build();\n" +
            "builder.Services.AddSyncfusionBlazorToolkit();\n" +
            "app.Run();\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_AliasedBuilder_RegistrationBeforeBuild_IsAccepted()
    {
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            AliasHeader +
            "var builder = WebApp.CreateBuilder(args);\n" +
            "builder.Services.AddSyncfusionBlazorToolkit();\n" +
            "var app = builder.Build();\n"));
    }

    [Theory]
    [InlineData("global::Microsoft.AspNetCore.Builder.WebApplication")]
    [InlineData("Microsoft.AspNetCore.Builder.WebApplication")]
    [InlineData("WebApplication")]
    public void DetectBuilderVariableName_ResolvesFrameworkTypeSpellings(string type)
    {
        Assert.Equal("b", SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
            $"var b = {type}.CreateBuilder(args);\n",
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
    }

    [Theory]
    [InlineData("using WebApp = Contoso.Hosting.WebApplication;\nvar b = WebApp.CreateBuilder(args);\n")]
    [InlineData("var b = Contoso.WebApplication.CreateBuilder(args);\n")]
    [InlineData("class WebApplication { public static object CreateBuilder(string[] a) => null; }\nvar b = WebApplication.CreateBuilder(args);\n")]
    public void DetectBuilderVariableName_LookAlikeTypes_AreNotTheFrameworkBuilder(string code)
    {
        Assert.Null(SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
            code, SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_AliasedWasmBuilder_IsResolved()
    {
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "using Host = Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHostBuilder;\n" +
            "var b = Host.CreateDefault(args);\nb.Services.AddSyncfusionBlazorToolkit();\nawait b.Build().RunAsync();\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_UnresolvedBuilder_IsNotAcceptedJustBecauseACallExists()
    {
        // The host builder cannot be identified (custom factory): a matching
        // statement must not be treated as proof of registration.
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "var builder = MyHost.Create(args);\nvar app = builder.Build();\nbuilder.Services.AddSyncfusionBlazorToolkit();\n"));
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "builder.Services.AddSyncfusionBlazorToolkit();\n"));
    }

    [Fact]
    public void IsActiveServiceRegistrationPresent_NoBuildBoundary_IsNotConclusive()
    {
        Assert.False(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(
            "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddSyncfusionBlazorToolkit();\n"));
    }

    [Fact]
    public void EnsureServiceRegistration_UnresolvedBuilderWithLateCall_SkipsAndDoesNotModifyFile()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        const string text =
            "var builder = MyHost.Create(args);\nvar app = builder.Build();\nbuilder.Services.AddSyncfusionBlazorToolkit();\n";
        File.WriteAllText(program, text);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project, SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.Equal(text, File.ReadAllText(program));
    }

    [Fact]
    public void EnsureServiceRegistration_NoBuildBoundary_SkipsAndDoesNotModifyFile()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        const string text = "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddSyncfusionBlazorToolkit();\n";
        File.WriteAllText(program, text);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project, SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.Equal(text, File.ReadAllText(program));
    }

    [Fact]
    public void EnsureServiceRegistration_AliasedBuilderWithLateCall_InsertsBeforeBuildAndKeepsAliasUsing()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        File.WriteAllText(program,
            AliasHeader +
            "var builder = WebApp.CreateBuilder(args);\nvar app = builder.Build();\nbuilder.Services.AddSyncfusionBlazorToolkit();\napp.Run();\n");

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project, SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted, result.Outcome);
        string text = File.ReadAllText(program);
        Assert.True(SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(text));
        Assert.Contains("using WebApp = Microsoft.AspNetCore.Builder.WebApplication;", text);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(text, "using Syncfusion\\.Blazor\\.Toolkit;"));
    }

    // ------------------------------------------------------------------
    // Host builder must BE the initializer, matched structurally
    // ------------------------------------------------------------------

    [Fact]
    public void DetectBuilderVariableName_WrapperAroundCreateBuilder_IsNotTheHostBuilder()
    {
        Assert.Null(SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
            "var wrappedBuilder = Wrap(WebApplication.CreateBuilder(args));\n",
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
    }

    [Fact]
    public void EnsureServiceRegistration_WrappedBuilder_SkipsWithoutWriting()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        var program = Path.Combine(_tempDir, "Program.cs");
        const string text = "var wrappedBuilder = Wrap(WebApplication.CreateBuilder(args));\nvar app = wrappedBuilder.Build();\n";
        File.WriteAllText(program, text);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);

        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
        Assert.Equal(text, File.ReadAllText(program));
    }

    [Theory]
    [InlineData("var builder = WebApplication\n    .CreateBuilder(args);\n")]
    [InlineData("var builder = WebApplication\t.CreateBuilder(args);\n")]
    [InlineData("var builder = (WebApplication.CreateBuilder(args));\n")]
    [InlineData("var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);\n")]
    public void DetectBuilderVariableName_IsIndependentOfFormatting(string code)
    {
        Assert.Equal("builder", SyncfusionBlazorToolkitHelper.DetectBuilderVariableName(
            code, SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp));
    }

    // ------------------------------------------------------------------
    // Entry point must be on the startup path
    // ------------------------------------------------------------------

    [Fact]
    public void DiscoverEntryPoint_BuilderOnlyInUnusedHelperMethod_IsNotAnEntryPoint()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        File.WriteAllText(Path.Combine(_tempDir, "HostingFactory.cs"),
            "public static class HostingFactory\n{\n" +
            "    public static WebApplication CreateUnusedApp(string[] args)\n    {\n" +
            "        var builder = WebApplication.CreateBuilder(args);\n" +
            "        builder.Services.AddSyncfusionBlazorToolkit();\n" +
            "        return builder.Build();\n    }\n}\n");

        Assert.Null(SyncfusionBlazorToolkitHelper.DiscoverEntryPoint(project, out string? error));
        Assert.NotNull(error);

        var result = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
            project,
            SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp);
        Assert.Equal(SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped, result.Outcome);
    }

    [Fact]
    public void DiscoverEntryPoint_StartupFileWinsOverHelperBuilderInAnotherFile()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project, WebProjectXml);
        File.WriteAllText(Path.Combine(_tempDir, "Hosting.cs"), BuilderDecl + "var app = builder.Build();\napp.Run();\n");
        File.WriteAllText(Path.Combine(_tempDir, "Other.cs"),
            "public static class F { public static WebApplication M(string[] args) { var builder = WebApplication.CreateBuilder(args); return builder.Build(); } }\n");

        string? entry = SyncfusionBlazorToolkitHelper.DiscoverEntryPoint(project, out _);

        Assert.Equal(Path.Combine(_tempDir, "Hosting.cs"), entry);
    }

    [Fact]
    public void TryIsPackageReferenced_CommentAndSimilarIdAreNotTheToolkit()
    {
        var project = Path.Combine(_tempDir, "App.csproj");
        File.WriteAllText(project,
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            "<!-- Syncfusion.Blazor.Toolkit -->" +
            "<ItemGroup><PackageReference Include=\"Syncfusion.Blazor.Toolkit.Extras\" Version=\"1.0.0\" /></ItemGroup></Project>");

        Assert.True(SyncfusionBlazorToolkitHelper.TryIsPackageReferenced(project, out bool referenced, out _));
        Assert.False(referenced);
    }
}
