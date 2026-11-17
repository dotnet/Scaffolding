// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class ProjectModifierHelperTests
{
    [Theory]
    [InlineData("net10.0", "blazorEntraChanges.json")]
    [InlineData("net10.0", "blazorWasmEntraChanges.json")]
    [InlineData("net11.0", "blazorEntraChanges.json")]
    [InlineData("net11.0", "blazorWasmEntraChanges.json")]
    public async Task EntraRouterReplacements_MatchWhitespaceVariantsAndAreIdempotent(string framework, string configName)
    {
        var configPath = Path.Combine(ScaffoldCliHelper.GetRepoRoot(), "src", "dotnet-scaffolding",
            "dotnet-scaffold", "AspNet", "Templates", framework, "CodeModificationConfigs", configName);
        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var routesConfig = config.RootElement.GetProperty("Files").EnumerateArray()
            .Single(file => file.GetProperty("FileName").GetString()!.EndsWith("Routes.razor", StringComparison.Ordinal));
        var snippet = routesConfig.GetProperty("Replacements")[0].Deserialize<CodeSnippet>()!;
        const string prefix = "<Router>\r\n    <Found Context=\"routeData\">\n        ";
        const string suffix = "\r\n        <FocusOnNavigate RouteData=\"routeData\" Selector=\"h1\" />\n    </Found>\r\n</Router>";

        foreach (var whitespace in new[] { "\n            ", "\r\n            ", " ", "  ", "\t" })
        {
            var routeView = $"<RouteView{whitespace}RouteData=\"routeData\"{whitespace}DefaultLayout=\"typeof(Layout.MainLayout)\"{whitespace}/>";
            await AssertBothReplacementPathsAsync(prefix + routeView + suffix, snippet, prefix + snippet.Block + suffix);
        }
    }

    [Theory]
    [InlineData("\n", "\n")]
    [InlineData("\r\n", "\r\n")]
    [InlineData("\r\n", "\n")]
    [InlineData("\n", "\r\n")]
    public async Task Replacements_MatchNewlineVariantsAndPreserveUnrelatedText(string firstNewline, string secondNewline)
    {
        const string prefix = "unchanged\r\nprefix\n";
        const string suffix = "\r\nunchanged\nsuffix\r\n";
        const string replacement = "replacement $1 ${value} \\literal\n";
        var snippet = new CodeSnippet
        {
            ReplaceSnippet = ["first(.*)", "    [second]$", "{third}"],
            Block = replacement
        };
        var input = $"{prefix}first(.*){firstNewline}    [second]${secondNewline}{{third}}{suffix}";

        await AssertBothReplacementPathsAsync(input, snippet, prefix + replacement + suffix);
    }

    [Fact]
    public Task Replacements_PreserveSingleLineLiteralAndCaseSensitiveMatching() =>
        AssertBothReplacementPathsAsync(
            "OLD(.*)$\r\nold(.*)$\n",
            new CodeSnippet { ReplaceSnippet = ["old(.*)$"], Block = "new $1" },
            "OLD(.*)$\r\nnew $1\n");

    [Fact]
    public Task Replacements_WhitespaceVariationsPreserveLiteralTextAndCaseSensitivity() =>
        AssertBothReplacementPathsAsync(
            "FIRST(.*) [second]$\r\nfirst(.*)\t[second]$\n",
            new CodeSnippet
            {
                ReplaceSnippet = ["first(.*) [second]$"],
                Block = "new $1 ${value} \\literal",
                MatchWhitespaceVariations = true
            },
            "FIRST(.*) [second]$\r\nnew $1 ${value} \\literal\n");

    [Theory]
    [InlineData("<RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(L)\"/>")]
    [InlineData("<RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(L)\" />")]
    [InlineData("<RouteView RouteData=\"routeData\"\r\n    DefaultLayout=\"typeof(L)\"\r\n/>")]
    public Task Replacements_WhitespaceVariationsAllowOptionalGapBeforeSelfClosingTag(string input) =>
        AssertBothReplacementPathsAsync(
            input,
            new CodeSnippet
            {
                ReplaceSnippet = ["<RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(L)\" />"],
                Block = "replaced",
                MatchWhitespaceVariations = true
            },
            "replaced");

    [Fact]
    public Task Replacements_WhitespaceVariationsStillRequireAttributeSeparators() =>
        AssertBothReplacementPathsAsync(
            "<RouteView RouteData=\"routeData\"DefaultLayout=\"typeof(L)\"/>",
            new CodeSnippet
            {
                ReplaceSnippet = ["<RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(L)\" />"],
                Block = "replaced",
                MatchWhitespaceVariations = true
            },
            "<RouteView RouteData=\"routeData\"DefaultLayout=\"typeof(L)\"/>");

    [Fact]
    public Task Replacements_RespectExistingCheckBlock() =>
        AssertBothReplacementPathsAsync(
            "EXISTING GUARD\r\nfirst\nsecond",
            new CodeSnippet { ReplaceSnippet = ["first", "second"], Block = "replacement", CheckBlock = "existing guard" },
            "EXISTING GUARD\r\nfirst\nsecond");

    [Fact]
    public Task Replacements_DoNotIgnoreOtherWhitespace() =>
        AssertBothReplacementPathsAsync(
            "first\r\nsecond",
            new CodeSnippet { ReplaceSnippet = ["first", "    second"], Block = "replacement" },
            "first\r\nsecond");

    [Theory]
    [InlineData(null, false, "")]
    [InlineData(new string[0], true, "original")]
    [InlineData(new string[] { "" }, false, "original")]
    public Task Replacements_NullAndEmptyAnchorsInsert(string[]? anchor, bool prepend, string input) =>
        AssertBothReplacementPathsAsync(input,
            new CodeSnippet { ReplaceSnippet = anchor, Block = "inserted", Prepend = prepend },
            prepend ? "inserted" + input : input + "inserted");

    [Theory]
    [InlineData(" ", "a b", "areplacementb")]
    [InlineData("\n", "a\r\nb\nc", "areplacementbreplacementc")]
    public Task Replacements_WhitespaceAnchorsRemainLiteral(string anchor, string input, string expected) =>
        AssertBothReplacementPathsAsync(input,
            new CodeSnippet { ReplaceSnippet = [anchor], Block = "replacement" }, expected);

    [Fact]
    public async Task ApplyTextReplacements_NoOpPreservesTheCurrentDocument()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("Program.cs", SourceText.From("earlier edit"));
        var file = new CodeFile
        {
            FileName = "Program.cs",
            Replacements = [new CodeSnippet { ReplaceSnippet = ["missing"], Block = "replacement" }]
        };

        Assert.Same(document, await ProjectModifier.ApplyTextReplacements(file, document, []));
    }

    private static async Task AssertBothReplacementPathsAsync(string input, CodeSnippet snippet, string expected)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "Program.cs", SourceText.From(input));
        var updated = await ProjectModifierHelper.ModifyDocumentTextAsync(document, [snippet]) ?? document;
        Assert.Equal(expected, (await updated.GetTextAsync()).ToString());
        var rerun = await ProjectModifierHelper.ModifyDocumentTextAsync(updated, [snippet]);
        Assert.Null(rerun);

        var additionalDocument = document.Project.AddAdditionalDocument("Page.razor", SourceText.From(input));
        var updatedAdditional = await ProjectModifierHelper.ModifyDocumentTextAsync(additionalDocument, [snippet]) ?? additionalDocument;
        Assert.Equal(expected, (await updatedAdditional.GetTextAsync()).ToString());

        var path = Path.Combine(Path.GetTempPath(), $"{nameof(ProjectModifierHelperTests)}-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(path, input);
            ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(path, [snippet]);
            Assert.Equal(expected, File.ReadAllText(path));
            ProjectModifierHelper.ApplyReplacementsOnFileOnDisk(path, [snippet]);
            Assert.Equal(expected, File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
