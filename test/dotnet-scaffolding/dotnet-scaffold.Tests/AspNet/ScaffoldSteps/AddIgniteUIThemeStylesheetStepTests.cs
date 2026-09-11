// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.AddIgniteUIThemeStylesheetStep;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class AddIgniteUIThemeStylesheetStepTests
{
    private const string LiteBootstrapLight = "_content/IgniteUI.Blazor/themes/light/bootstrap.css";
    private const string LiteMaterialDark = "_content/IgniteUI.Blazor/themes/dark/material.css";
    private const string GridLiteBootstrapLight = "_content/IgniteUI.Blazor.GridLite/css/themes/light/bootstrap.css";

    private static readonly string s_appRazorPath = Path.Combine("C:", "src", "MyApp", "Components", "App.razor");
    private static readonly string s_indexHtmlPath = Path.Combine("C:", "src", "MyApp", "wwwroot", "index.html");
    private static readonly string s_hostCshtmlPath = Path.Combine("C:", "src", "MyApp", "Pages", "_Host.cshtml");

    // Fixtures are normalized to '\n' so the assertions do not depend on the checkout's line endings.
    private static readonly string AppRazor = AppRazorRaw.ReplaceLineEndings("\n");
    private static readonly string IndexHtml = IndexHtmlRaw.ReplaceLineEndings("\n");

    private const string AppRazorRaw = """
        <!DOCTYPE html>
        <html lang="en">

        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <base href="/" />
            <link rel="stylesheet" href="@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]" />
            <link rel="stylesheet" href="@Assets["app.css"]" />
            <link rel="stylesheet" href="@Assets["MyApp.styles.css"]" />
            <ImportMap />
            <link rel="icon" type="image/png" href="favicon.png" />
            <HeadOutlet />
        </head>

        <body>
            <Routes />
            <script src="_framework/blazor.web.js"></script>
        </body>

        </html>
        """;

    private const string IndexHtmlRaw = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <title>MyApp</title>
            <base href="/" />
            <link rel="stylesheet" href="css/app.css" />
        </head>
        <body>
            <div id="app">Loading...</div>
            <script src="_framework/blazor.webassembly.js"></script>
        </body>
        </html>
        """;

    private readonly Mock<IFileSystem> _mockFileSystem = new();
    private readonly Mock<ITelemetryService> _mockTelemetryService = new();
    private readonly ScaffolderContext _context;

    public AddIgniteUIThemeStylesheetStepTests()
    {
        var mockScaffolder = new Mock<IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(mockScaffolder.Object);
    }

    private AddIgniteUIThemeStylesheetStep CreateStep(string hostPagePath, string stylesheetPath)
        => new(NullLogger<AddIgniteUIThemeStylesheetStep>.Instance, _mockFileSystem.Object, _mockTelemetryService.Object)
        {
            HostPagePath = hostPagePath,
            StylesheetPath = stylesheetPath
        };

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenHostPageMissing()
    {
        _mockFileSystem.Setup(f => f.FileExists(s_appRazorPath)).Returns(false);
        var step = CreateStep(s_appRazorPath, LiteBootstrapLight);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenPathsEmpty()
    {
        var step = CreateStep(string.Empty, string.Empty);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_WritesUpdatedHostPage_AndTracksTelemetry()
    {
        _mockFileSystem.Setup(f => f.FileExists(s_indexHtmlPath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(s_indexHtmlPath)).Returns(IndexHtml);
        string? written = null;
        _mockFileSystem.Setup(f => f.WriteAllText(s_indexHtmlPath, It.IsAny<string>())).Callback<string, string>((_, c) => written = c);
        var step = CreateStep(s_indexHtmlPath, GridLiteBootstrapLight);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(written);
        Assert.Contains($"<link href=\"{GridLiteBootstrapLight}\" rel=\"stylesheet\" />", written);
        _mockTelemetryService.Verify(t => t.TrackEvent(
            It.Is<string>(name => name.Contains(nameof(AddIgniteUIThemeStylesheetStep))),
            It.IsAny<IReadOnlyDictionary<string, string>>(),
            It.IsAny<IReadOnlyDictionary<string, double>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotWrite_WhenAlreadyLinked()
    {
        var content = IndexHtml.Replace("css/app.css", GridLiteBootstrapLight);
        _mockFileSystem.Setup(f => f.FileExists(s_indexHtmlPath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(s_indexHtmlPath)).Returns(content);
        var step = CreateStep(s_indexHtmlPath, GridLiteBootstrapLight);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenNoHeadElement()
    {
        _mockFileSystem.Setup(f => f.FileExists(s_appRazorPath)).Returns(true);
        _mockFileSystem.Setup(f => f.ReadAllText(s_appRazorPath)).Returns("<Routes />");
        var step = CreateStep(s_appRazorPath, LiteBootstrapLight);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ApplyStylesheetLink

    [Fact]
    public void ApplyStylesheetLink_InsertsAfterLastLink_UsingAssetsSyntax_ForRazorHostPage()
    {
        var updated = ApplyStylesheetLink(s_appRazorPath, AppRazor, LiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Inserted, outcome);
        var expectedLine = $"    <link rel=\"stylesheet\" href=\"@Assets[\"{LiteBootstrapLight}\"]\" />";
        Assert.Contains(expectedLine, updated);
        // inserted right after the last <link> (favicon) and before <HeadOutlet />
        var faviconIndex = updated.IndexOf("favicon.png", System.StringComparison.Ordinal);
        var insertedIndex = updated.IndexOf(expectedLine, System.StringComparison.Ordinal);
        var headOutletIndex = updated.IndexOf("<HeadOutlet />", System.StringComparison.Ordinal);
        Assert.True(faviconIndex < insertedIndex && insertedIndex < headOutletIndex);
        Assert.Single(Regex.Matches(updated, Regex.Escape(LiteBootstrapLight)));
    }

    [Fact]
    public void ApplyStylesheetLink_UsesPlainHref_ForHtmlHostPage()
    {
        var updated = ApplyStylesheetLink(s_indexHtmlPath, IndexHtml, GridLiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Inserted, outcome);
        Assert.Contains($"    <link rel=\"stylesheet\" href=\"css/app.css\" />\n    <link href=\"{GridLiteBootstrapLight}\" rel=\"stylesheet\" />\n</head>", updated);
        Assert.DoesNotContain("@Assets", updated);
    }

    [Fact]
    public void ApplyStylesheetLink_UsesPlainHref_ForRazorHostPageWithoutAssets()
    {
        var content = AppRazor.Replace("@Assets[\"lib/bootstrap/dist/css/bootstrap.min.css\"]", "lib/bootstrap/dist/css/bootstrap.min.css")
            .Replace("@Assets[\"app.css\"]", "app.css")
            .Replace("@Assets[\"MyApp.styles.css\"]", "MyApp.styles.css");

        var updated = ApplyStylesheetLink(s_appRazorPath, content, LiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Inserted, outcome);
        Assert.Contains($"    <link href=\"{LiteBootstrapLight}\" rel=\"stylesheet\" />", updated);
        Assert.DoesNotContain("@Assets", updated);
    }

    [Fact]
    public void ApplyStylesheetLink_ReturnsUnchanged_WhenAlreadyLinked()
    {
        var content = IndexHtml.Replace("css/app.css", LiteBootstrapLight);

        var updated = ApplyStylesheetLink(s_indexHtmlPath, content, LiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.AlreadyLinked, outcome);
        Assert.Equal(content, updated);
    }

    [Fact]
    public void ApplyStylesheetLink_SwapsExistingIgniteUITheme_InsteadOfAddingSecondLink()
    {
        var content = IndexHtml.Replace("css/app.css", LiteBootstrapLight);

        var updated = ApplyStylesheetLink(s_indexHtmlPath, content, LiteMaterialDark, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Replaced, outcome);
        Assert.DoesNotContain(LiteBootstrapLight, updated);
        Assert.Contains(LiteMaterialDark, updated);
        Assert.Single(Regex.Matches(updated, "_content/IgniteUI"));
    }

    [Fact]
    public void ApplyStylesheetLink_SwapsGridLiteTheme_ForLiteTheme()
    {
        var content = IndexHtml.Replace("css/app.css", GridLiteBootstrapLight);

        var updated = ApplyStylesheetLink(s_indexHtmlPath, content, LiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Replaced, outcome);
        Assert.DoesNotContain(GridLiteBootstrapLight, updated);
        Assert.Contains($"href=\"{LiteBootstrapLight}\"", updated);
    }

    [Fact]
    public void ApplyStylesheetLink_InsertsBeforeHead_WhenNoLinkExists()
    {
        var content = "<html>\n<head>\n    <title>Test</title>\n</head>\n<body></body>\n</html>\n";

        var updated = ApplyStylesheetLink(s_hostCshtmlPath, content, LiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Inserted, outcome);
        Assert.Equal($"<html>\n<head>\n    <title>Test</title>\n    <link href=\"{LiteBootstrapLight}\" rel=\"stylesheet\" />\n</head>\n<body></body>\n</html>\n", updated);
    }

    [Fact]
    public void ApplyStylesheetLink_BreaksLine_WhenHeadIsOnSingleLine()
    {
        var content = "<html>\n<head><title>Test</title><HeadOutlet /></head>\n<body><Routes /></body>\n</html>\n";

        var updated = ApplyStylesheetLink(s_appRazorPath, content, LiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Inserted, outcome);
        Assert.Equal($"<html>\n<head><title>Test</title><HeadOutlet />\n    <link href=\"{LiteBootstrapLight}\" rel=\"stylesheet\" />\n</head>\n<body><Routes /></body>\n</html>\n", updated);
    }

    [Fact]
    public void ApplyStylesheetLink_PreservesCrlfLineEndings()
    {
        var content = IndexHtml.Replace("\n", "\r\n");

        var updated = ApplyStylesheetLink(s_indexHtmlPath, content, GridLiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.Inserted, outcome);
        Assert.Contains($"    <link rel=\"stylesheet\" href=\"css/app.css\" />\r\n    <link href=\"{GridLiteBootstrapLight}\" rel=\"stylesheet\" />\r\n</head>", updated);
        // no lone '\n' was introduced
        Assert.DoesNotContain("\n", updated.Replace("\r\n", string.Empty));
    }

    [Fact]
    public void ApplyStylesheetLink_ReturnsNoHeadElement_WhenHeadMissing()
    {
        var content = "<Routes />";

        var updated = ApplyStylesheetLink(s_appRazorPath, content, LiteBootstrapLight, out var outcome);

        Assert.Equal(StylesheetLinkOutcome.NoHeadElement, outcome);
        Assert.Equal(content, updated);
    }

    #endregion
}
