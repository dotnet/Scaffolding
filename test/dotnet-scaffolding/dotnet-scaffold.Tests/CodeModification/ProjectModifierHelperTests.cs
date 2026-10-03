// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.CodeModification.CodeChange;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CodeModification;

public class ProjectModifierHelperTests
{
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
    [InlineData("\n", "\r\n")]
    [InlineData("\r\n", "\n")]
    public void HtmlReplacements_MatchMixedNewlinesAndPreserveCaseInsensitiveMatching(string firstNewline, string secondNewline)
    {
        const string replacement = "replacement $1 ${value} \\literal\n";
        var input = $"prefix\r\n<HEAD>{firstNewline}    [TITLE]${secondNewline}</HEAD>\nsuffix\r\n";
        var snippet = new CodeSnippet { ReplaceSnippet = ["<head>", "    [title]$", "</head>"], Block = replacement };
        var path = Path.Combine(Path.GetTempPath(), $"{nameof(ProjectModifierHelperTests)}-{Guid.NewGuid():N}.html");
        try
        {
            File.WriteAllText(path, input);
            Assert.True(ProjectModifierHelper.TryApplyReplacementsOnFileOnDisk(path, [snippet], out var error));
            Assert.Null(error);
            Assert.Equal($"prefix\r\n{replacement}\nsuffix\r\n", File.ReadAllText(path));
            Assert.True(ProjectModifierHelper.TryApplyReplacementsOnFileOnDisk(path, [snippet], out error));
            Assert.Null(error);
            Assert.Equal($"prefix\r\n{replacement}\nsuffix\r\n", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void HtmlReplacements_ReportMissingAnchorsWithoutWritingTheFile()
    {
        const string input = "first\r\nsecond";
        var snippet = new CodeSnippet { ReplaceSnippet = ["first", "    second"], Block = "replacement" };
        var path = Path.Combine(Path.GetTempPath(), $"{nameof(ProjectModifierHelperTests)}-{Guid.NewGuid():N}.html");
        try
        {
            File.WriteAllText(path, input);
            Assert.False(ProjectModifierHelper.TryApplyReplacementsOnFileOnDisk(path, [snippet], out var error));
            Assert.Contains("was not found", error);
            Assert.Equal(input, File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("K", "\u212A", false)]
    [InlineData("\u03C3", "\u03C2", true)]
    public void HtmlReplacements_PreserveOrdinalIgnoreCaseSemantics(string expectedLetter, string actualLetter, bool shouldMatch)
    {
        var input = $"{actualLetter}\nend";
        var snippet = new CodeSnippet { ReplaceSnippet = [expectedLetter, "END"], Block = "replacement" };
        var path = Path.Combine(Path.GetTempPath(), $"{nameof(ProjectModifierHelperTests)}-{Guid.NewGuid():N}.html");
        try
        {
            File.WriteAllText(path, input);
            Assert.Equal(shouldMatch, ProjectModifierHelper.TryApplyReplacementsOnFileOnDisk(path, [snippet], out var error));
            Assert.Equal(shouldMatch ? "replacement" : input, File.ReadAllText(path));
            if (shouldMatch)
            {
                Assert.Null(error);
            }
            else
            {
                Assert.Contains("was not found", error);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task AssertBothReplacementPathsAsync(string input, CodeSnippet snippet, string expected)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = workspace.AddDocument(project.Id, "Program.cs", SourceText.From(input));
        var updated = await ProjectModifierHelper.ModifyDocumentTextAsync(document, [snippet]);
        Assert.NotNull(updated);
        Assert.Equal(expected, (await updated.GetTextAsync()).ToString());

        var path = Path.Combine(Path.GetTempPath(), $"{nameof(ProjectModifierHelperTests)}-{Guid.NewGuid():N}.css");
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
