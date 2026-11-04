// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class IgniteUIBlazorHelperTests
{
    private static readonly string s_projectDir = Path.Combine("C:", "src", "MyApp");

    #region Theme normalization

    [Theory]
    [InlineData("bootstrap", "bootstrap")]
    [InlineData("Material", "material")]
    [InlineData("FLUENT", "fluent")]
    [InlineData(" indigo ", "indigo")]
    [InlineData(null, "bootstrap")]
    [InlineData("", "bootstrap")]
    public void TryNormalizeTheme_ReturnsSupportedThemeOrDefaultWhenOmitted(string? theme, string expected)
    {
        Assert.True(IgniteUIBlazorHelper.TryNormalizeTheme(theme, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("bootstrap-dark")]
    public void TryNormalizeTheme_RejectsUnsupportedTheme(string theme)
        => Assert.False(IgniteUIBlazorHelper.TryNormalizeTheme(theme, out _));

    [Theory]
    [InlineData("light", "light")]
    [InlineData("Dark", "dark")]
    [InlineData(null, "light")]
    [InlineData("", "light")]
    public void TryNormalizeThemeVariant_ReturnsSupportedVariantOrDefaultWhenOmitted(string? variant, string expected)
    {
        Assert.True(IgniteUIBlazorHelper.TryNormalizeThemeVariant(variant, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("night")]
    [InlineData("darker")]
    public void TryNormalizeThemeVariant_RejectsUnsupportedVariant(string variant)
        => Assert.False(IgniteUIBlazorHelper.TryNormalizeThemeVariant(variant, out _));

    [Fact]
    public void Themes_ContainAllShippedThemes()
    {
        Assert.Equal(new[] { "bootstrap", "material", "fluent", "indigo" }, IgniteUIBlazorHelper.Themes);
        Assert.Equal(new[] { "light", "dark" }, IgniteUIBlazorHelper.ThemeVariants);
    }

    #endregion

    #region Stylesheet path

    [Theory]
    [InlineData("bootstrap", "light", "_content/IgniteUI.Blazor/themes/light/bootstrap.css")]
    [InlineData("material", "dark", "_content/IgniteUI.Blazor/themes/dark/material.css")]
    public void GetThemeStylesheetPath_UsesIgniteUIBlazorTheme(string theme, string variant, string expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.GetThemeStylesheetPath(theme, variant));

    [Theory]
    [InlineData("_content/IgniteUI.Blazor/themes/light/bootstrap.css")]
    [InlineData("_content/IgniteUI.Blazor/themes/dark/indigo.css")]
    [InlineData("_content/IgniteUI.Blazor.GridLite/css/themes/light/fluent.css")]
    [InlineData("_CONTENT/IGNITEUI.BLAZOR/THEMES/DARK/MATERIAL.CSS")]
    public void ThemeStylesheetRegex_MatchesEveryGeneratedPath(string path)
        => Assert.Matches(IgniteUIBlazorHelper.ThemeStylesheetRegex, path);

    [Theory]
    [InlineData("<link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" />", true, "material", "dark")]
    [InlineData("<link href=\"_content/IgniteUI.Blazor.GridLite/css/themes/light/fluent.css\" rel=\"stylesheet\" />", true, "fluent", "light")]
    [InlineData("<link rel=\"stylesheet\" href=\"@Assets[\"_content/IgniteUI.Blazor/themes/dark/indigo.css\"]\" />", true, "indigo", "dark")]
    [InlineData("<link href=\"_CONTENT/IGNITEUI.BLAZOR/THEMES/DARK/FLUENT.CSS\" rel=\"stylesheet\" />", true, "fluent", "dark")]
    [InlineData("<link href=\"css/app.css\" rel=\"stylesheet\" />", false, "bootstrap", "light")]
    [InlineData(null, false, "bootstrap", "light")]
    public void TryGetLinkedTheme_ReadsTheLinkedThemeOrReturnsDefaults(string? content, bool expectedFound, string expectedTheme, string expectedVariant)
    {
        Assert.Equal(expectedFound, IgniteUIBlazorHelper.TryGetLinkedTheme(Path.Combine(s_projectDir, "Components", "App.razor"), content, out var theme, out var variant));
        Assert.Equal(expectedTheme, theme);
        Assert.Equal(expectedVariant, variant);
    }

    [Theory]
    [InlineData("App.razor", "<!-- <link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" /> -->")]
    [InlineData("App.razor", "@*\n<link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" />\n*@")]
    [InlineData("_Host.cshtml", "@* <link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" /> *@")]
    [InlineData("index.html", "<!--\n<link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" />\n-->")]
    public void TryGetLinkedTheme_IgnoresCommentedOutLinks(string fileName, string content)
    {
        Assert.False(IgniteUIBlazorHelper.TryGetLinkedTheme(Path.Combine(s_projectDir, fileName), content, out var theme, out var variant));
        Assert.Equal(IgniteUIBlazorHelper.DefaultTheme, theme);
        Assert.Equal(IgniteUIBlazorHelper.DefaultThemeVariant, variant);
    }

    [Fact]
    public void TryGetLinkedTheme_ReadsTheActiveLinkAfterACommentedOutOne()
    {
        const string content = "<!-- <link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" /> -->\n" +
                               "<link href=\"_content/IgniteUI.Blazor/themes/light/fluent.css\" rel=\"stylesheet\" />";

        Assert.True(IgniteUIBlazorHelper.TryGetLinkedTheme(Path.Combine(s_projectDir, "wwwroot", "index.html"), content, out var theme, out var variant));
        Assert.Equal("fluent", theme);
        Assert.Equal("light", variant);
    }

    [Fact]
    public void TryGetLinkedTheme_TreatsRazorCommentSyntaxAsTextInHtmlPages()
    {
        // '@*' is not a comment in index.html, so the link after it is active.
        const string content = "<script>/* @* */</script>\n<link href=\"_content/IgniteUI.Blazor/themes/dark/indigo.css\" rel=\"stylesheet\" />\n<!-- *@ -->";

        Assert.True(IgniteUIBlazorHelper.TryGetLinkedTheme(Path.Combine(s_projectDir, "wwwroot", "index.html"), content, out var theme, out _));
        Assert.Equal("indigo", theme);
    }

    [Fact]
    public void GetThemeManualInstructions_NamesTheLinkToReplace_ForASwap()
    {
        var hostPage = Path.Combine(s_projectDir, "Components", "App.razor");
        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, hostPage,
            "<head>\n    <link href=\"_content/IgniteUI.Blazor/themes/light/bootstrap.css\" rel=\"stylesheet\" />\n</head>\n", LiteMaterialDark);

        Assert.Equal(
            $"To finish the setup, replace the Ignite UI theme link <link href=\"_content/IgniteUI.Blazor/themes/light/bootstrap.css\" rel=\"stylesheet\" /> in '{hostPage}' with <link href=\"{LiteMaterialDark}\" rel=\"stylesheet\" />",
            IgniteUIBlazorHelper.GetThemeManualInstructions(options, properties, LiteMaterialDark, hostPage));
    }

    [Fact]
    public void GetThemeManualInstructions_NamesTheLinkToAdd_WhenNoThemeIsLinked()
    {
        var hostPage = Path.Combine(s_projectDir, "Components", "App.razor");
        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, hostPage, "<head>\n</head>\n", LiteMaterialDark);

        Assert.Equal(
            $"To finish the setup, add the following line to the <head> of the page that hosts your Blazor app: <link href=\"{LiteMaterialDark}\" rel=\"stylesheet\" />",
            IgniteUIBlazorHelper.GetThemeManualInstructions(options, properties, LiteMaterialDark, hostPage));
        Assert.StartsWith("To finish the setup, add the following line", IgniteUIBlazorHelper.GetThemeManualInstructions([], new Dictionary<string, string>(), LiteMaterialDark, hostPagePath: null));
    }

    private const string LiteMaterialDark = "_content/IgniteUI.Blazor/themes/dark/material.css";

    [Fact]
    public void GetThemeRecipeInputs_LinksBeforeHead_WithAssetsSyntaxAndIndentation()
    {
        var hostPage = Path.Combine(s_projectDir, "Components", "App.razor");
        const string content = "<head>\n    <link rel=\"stylesheet\" href=\"@Assets[\"app.css\"]\" />\n    <HeadOutlet />\n</head>\n";

        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, hostPage, content, LiteMaterialDark);

        Assert.Equal(["IgniteUIAppRazorHost", IgniteUIBlazorHelper.LinkThemeRecipeOption], options);
        Assert.Equal("</head>", properties["$(IgniteUIHeadClosingTag)"]);
        Assert.Equal($"    <link rel=\"stylesheet\" href=\"@Assets[\"{LiteMaterialDark}\"]\" />\n</head>", properties["$(IgniteUIThemeLinkBeforeHead)"]);
    }

    [Theory]
    [InlineData("wwwroot", "index.html", "<html>\r\n<HEAD>\r\n    <title>App</title>\r\n</HEAD>\r\n", "IgniteUIIndexHtmlHost", "</HEAD>", "    <link href=\"{0}\" rel=\"stylesheet\" />\r\n</HEAD>")]
    [InlineData("Pages", "_Host.cshtml", "<head>\n\t\t<base href=\"~/\" />\n\t</head>\n", "IgniteUIHostCshtmlHost", "</head>", "\t<link href=\"{0}\" rel=\"stylesheet\" />\n\t</head>")]
    public void GetThemeRecipeInputs_UsesTheTagCasingLineEndingAndIndentationOfThePage(
        string folder, string fileName, string content, string expectedHostOption, string expectedTag, string expectedBlockFormat)
    {
        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, Path.Combine(s_projectDir, folder, fileName), content, LiteMaterialDark);

        Assert.Equal([expectedHostOption, IgniteUIBlazorHelper.LinkThemeRecipeOption], options);
        Assert.Equal(expectedTag, properties["$(IgniteUIHeadClosingTag)"]);
        Assert.Equal(string.Format(expectedBlockFormat, LiteMaterialDark), properties["$(IgniteUIThemeLinkBeforeHead)"]);
    }

    [Fact]
    public void GetThemeRecipeInputs_SwapsAnExistingIgniteUITheme()
    {
        const string gridLiteTheme = "_content/IgniteUI.Blazor.GridLite/css/themes/light/bootstrap.css";
        var content = $"<head>\n    <link href=\"{gridLiteTheme}\" rel=\"stylesheet\" />\n</head>\n";

        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, Path.Combine(s_projectDir, "wwwroot", "index.html"), content, LiteMaterialDark);

        Assert.Equal(["IgniteUIIndexHtmlHost", IgniteUIBlazorHelper.SwapThemeRecipeOption], options);
        Assert.Equal($"<link href=\"{gridLiteTheme}\" rel=\"stylesheet\" />", properties["$(IgniteUIExistingThemeLink)"]);
        Assert.Equal($"<link href=\"{LiteMaterialDark}\" rel=\"stylesheet\" />", properties["$(IgniteUISwappedThemeLink)"]);
    }

    [Fact]
    public void GetThemeRecipeInputs_SwapsTheActiveLink_NotACommentedOutOne()
    {
        const string commented = "<!-- <link href=\"_content/IgniteUI.Blazor/themes/light/bootstrap.css\" rel=\"stylesheet\" /> -->";
        const string active = "<link rel=\"stylesheet\" href=\"@Assets[\"_content/IgniteUI.Blazor/themes/light/fluent.css\"]\" />";
        var content = $"<head>\n    {commented}\n    {active}\n</head>\n";

        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, Path.Combine(s_projectDir, "Components", "App.razor"), content, LiteMaterialDark);

        Assert.Equal(["IgniteUIAppRazorHost", IgniteUIBlazorHelper.SwapThemeRecipeOption], options);
        Assert.Equal(active, properties["$(IgniteUIExistingThemeLink)"]);
        Assert.Equal($"<link rel=\"stylesheet\" href=\"@Assets[\"{LiteMaterialDark}\"]\" />", properties["$(IgniteUISwappedThemeLink)"]);
    }

    [Fact]
    public void GetThemeRecipeInputs_WidensTheSwap_WhenTheRequestedLinkIsCommentedOut()
    {
        // The recipe skips a replacement that the page already contains, even inside a comment, so the swapped text
        // is widened by a line around the active link (here the line after it, which the link ends).
        var hostPage = Path.Combine(s_projectDir, "Components", "App.razor");
        const string commented = "<!-- <link rel=\"stylesheet\" href=\"@Assets[\"_content/IgniteUI.Blazor/themes/dark/material.css\"]\" /> -->";
        const string active = "<link rel=\"stylesheet\" href=\"@Assets[\"_content/IgniteUI.Blazor/themes/dark/fluent.css\"]\" />";
        const string swapped = "<link rel=\"stylesheet\" href=\"@Assets[\"_content/IgniteUI.Blazor/themes/dark/material.css\"]\" />";
        var content = $"<head>\r\n    <HeadOutlet />\r\n    {commented}\r\n    {active}\r\n</head>\r\n";

        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, hostPage, content, LiteMaterialDark);

        Assert.Equal(["IgniteUIAppRazorHost", IgniteUIBlazorHelper.SwapThemeRecipeOption], options);
        Assert.Equal($"    {active}\r\n</head>", properties["$(IgniteUIExistingThemeLink)"]);
        Assert.Equal($"    {swapped}\r\n</head>", properties["$(IgniteUISwappedThemeLink)"]);
        Assert.Equal(
            $"To finish the setup, replace the Ignite UI theme link {active} in '{hostPage}' with {swapped}",
            IgniteUIBlazorHelper.GetThemeManualInstructions(options, properties, LiteMaterialDark, hostPage));
    }

    [Fact]
    public void GetThemeRecipeInputs_LinksANewTheme_WhenTheOnlyThemeLinkIsCommentedOut()
    {
        // The '</head>' inside the comment is not the page's head either.
        var content = $"<head>\n    @* <link href=\"{LiteMaterialDark}\" rel=\"stylesheet\" /></head> *@\n    <base href=\"/\" />\n  </head>\n";

        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, Path.Combine(s_projectDir, "Pages", "_Host.cshtml"), content, LiteMaterialDark);

        Assert.Equal(["IgniteUIHostCshtmlHost", IgniteUIBlazorHelper.LinkThemeRecipeOption], options);
        Assert.Equal($"    <link href=\"{LiteMaterialDark}\" rel=\"stylesheet\" />\n  </head>", properties["$(IgniteUIThemeLinkBeforeHead)"]);
    }

    [Theory]
    [InlineData("_content/IgniteUI.Blazor/app.bundle.js")]
    [InlineData("css/app.css")]
    [InlineData("_content/IgniteUI.Blazor/themes/light/custom.css")]
    public void ThemeStylesheetRegex_DoesNotMatchOtherAssets(string path)
        => Assert.DoesNotMatch(IgniteUIBlazorHelper.ThemeStylesheetRegex, path);

    [Fact]
    public void BuildStylesheetLink_UsesPlainHref_ByDefault()
        => Assert.Equal("<link href=\"_content/IgniteUI.Blazor/themes/light/bootstrap.css\" rel=\"stylesheet\" />",
            IgniteUIBlazorHelper.BuildStylesheetLink("_content/IgniteUI.Blazor/themes/light/bootstrap.css", useAssetsCollection: false));

    [Fact]
    public void BuildStylesheetLink_UsesAssetsCollection_WhenRequested()
        => Assert.Equal("<link rel=\"stylesheet\" href=\"@Assets[\"_content/IgniteUI.Blazor/themes/light/bootstrap.css\"]\" />",
            IgniteUIBlazorHelper.BuildStylesheetLink("_content/IgniteUI.Blazor/themes/light/bootstrap.css", useAssetsCollection: true));

    #endregion

    #region Project inspection

    [Theory]
    [InlineData(true, false, true, "Interactive Server support was enabled", "'@rendermode InteractiveServer' to them")]
    [InlineData(true, false, false, null, "'@rendermode InteractiveServer' to them")]
    [InlineData(false, true, false, null, "'@rendermode InteractiveWebAssembly' to them (such pages belong in the client project)")]
    [InlineData(true, true, false, null, "'@rendermode InteractiveAuto'")]
    public void GetRenderModeGuidance_SuggestsTheConfiguredRenderModes(bool server, bool webAssembly, bool added, string? expectedAddedNote, string expectedRenderModes)
    {
        var guidance = IgniteUIBlazorHelper.GetRenderModeGuidance(server, webAssembly, added, hasGlobalRenderMode: false);

        Assert.NotNull(guidance);
        Assert.Contains("Pages that use Ignite UI components need an interactive render mode", guidance);
        Assert.Contains(expectedRenderModes, guidance);
        Assert.Equal(server && webAssembly, guidance.Contains("InteractiveAuto"));
        Assert.Equal(expectedAddedNote is not null, guidance.Contains("Interactive Server support was enabled"));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    public void GetRenderModeGuidance_ReturnsNull_WhenGloballyInteractiveOrNotInteractive(bool server, bool webAssembly, bool hasGlobalRenderMode)
        => Assert.Null(IgniteUIBlazorHelper.GetRenderModeGuidance(server, webAssembly, addedInteractiveServer: false, hasGlobalRenderMode));

    [Theory]
    [InlineData("<Routes @rendermode=\"InteractiveAuto\" />", true)]
    [InlineData("@page \"/counter\"\n@rendermode InteractiveServer", true)]
    [InlineData("<Routes />", false)]
    [InlineData(null, false)]
    public void DeclaresRenderMode_DetectsDirectiveAndAttribute(string? razor, bool expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.DeclaresRenderMode(razor));

    [Theory]
    [InlineData("<head></head>", true)]
    [InlineData("<HEAD></HEAD>", true)]
    [InlineData("<body></body>", false)]
    [InlineData("<!-- </head> --><body></body>", false)]
    [InlineData("@* </head> *@<body></body>", false)]
    [InlineData(null, false)]
    public void ContainsHeadClosingTag_IsCaseInsensitiveAndIgnoresComments(string? content, bool expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.ContainsHeadClosingTag(Path.Combine(s_projectDir, "Components", "App.razor"), content));

    [Fact]
    public void UsesAssetsCollection_TrueOnlyForRazorHostPagesUsingAssets()
    {
        Assert.True(IgniteUIBlazorHelper.UsesAssetsCollection(Path.Combine("Components", "App.razor"), "<link rel=\"stylesheet\" href=\"@Assets[\"app.css\"]\" />"));
        Assert.False(IgniteUIBlazorHelper.UsesAssetsCollection(Path.Combine("Components", "App.razor"), "<link rel=\"stylesheet\" href=\"app.css\" />"));
        Assert.False(IgniteUIBlazorHelper.UsesAssetsCollection(Path.Combine("wwwroot", "index.html"), "@Assets[\"app.css\"]"));
        Assert.False(IgniteUIBlazorHelper.UsesAssetsCollection(null, "@Assets[\"app.css\"]"));
    }

    #endregion

    #region Host page and _Imports.razor resolution

    [Fact]
    public void FindHostPage_PrefersComponentsAppRazor()
    {
        var appRazor = Path.Combine(s_projectDir, "Components", "App.razor");
        var indexHtml = Path.Combine(s_projectDir, "wwwroot", "index.html");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(appRazor)).Returns(true);
        fileSystem.Setup(f => f.ReadAllText(appRazor)).Returns("<html><head></head><body></body></html>");
        fileSystem.Setup(f => f.FileExists(indexHtml)).Returns(true);
        fileSystem.Setup(f => f.ReadAllText(indexHtml)).Returns("<html><head></head><body></body></html>");

        Assert.Equal(appRazor, IgniteUIBlazorHelper.FindHostPage(fileSystem.Object, s_projectDir));
    }

    [Fact]
    public void FindHostPage_FallsBackToIndexHtml_ForWebAssemblyProjects()
    {
        var indexHtml = Path.Combine(s_projectDir, "wwwroot", "index.html");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        fileSystem.Setup(f => f.FileExists(indexHtml)).Returns(true);
        fileSystem.Setup(f => f.ReadAllText(indexHtml)).Returns("<html><head></head><body></body></html>");

        Assert.Equal(indexHtml, IgniteUIBlazorHelper.FindHostPage(fileSystem.Object, s_projectDir));
    }

    [Fact]
    public void FindHostPage_SkipsCandidatesWithoutHeadElement()
    {
        var hostCshtml = Path.Combine(s_projectDir, "Pages", "_Host.cshtml");
        var layoutCshtml = Path.Combine(s_projectDir, "Pages", "_Layout.cshtml");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        fileSystem.Setup(f => f.FileExists(layoutCshtml)).Returns(true);
        fileSystem.Setup(f => f.ReadAllText(layoutCshtml)).Returns("@page \"/\"\n<component type=\"typeof(App)\" render-mode=\"ServerPrerendered\" />");
        fileSystem.Setup(f => f.FileExists(hostCshtml)).Returns(true);
        fileSystem.Setup(f => f.ReadAllText(hostCshtml)).Returns("<html><head></head><body></body></html>");

        Assert.Equal(hostCshtml, IgniteUIBlazorHelper.FindHostPage(fileSystem.Object, s_projectDir));
    }

    [Fact]
    public void FindHostPage_ReturnsNull_WhenNoHostPageExists()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

        Assert.Null(IgniteUIBlazorHelper.FindHostPage(fileSystem.Object, s_projectDir));
    }

    [Fact]
    public void GetImportsFilePath_PrefersComponentsImports()
    {
        var componentsImports = Path.Combine(s_projectDir, "Components", "_Imports.razor");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

        Assert.Equal(componentsImports, IgniteUIBlazorHelper.GetImportsFilePath(fileSystem.Object, s_projectDir));
    }

    [Fact]
    public void GetImportsFilePath_UsesRootImports_WhenComponentsImportsMissing()
    {
        var rootImports = Path.Combine(s_projectDir, "_Imports.razor");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        fileSystem.Setup(f => f.FileExists(rootImports)).Returns(true);

        Assert.Equal(rootImports, IgniteUIBlazorHelper.GetImportsFilePath(fileSystem.Object, s_projectDir));
    }

    [Fact]
    public void GetImportsFilePath_DefaultsToComponentsFolder_WhenItExists()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        fileSystem.Setup(f => f.DirectoryExists(Path.Combine(s_projectDir, "Components"))).Returns(true);

        Assert.Equal(Path.Combine(s_projectDir, "Components", "_Imports.razor"), IgniteUIBlazorHelper.GetImportsFilePath(fileSystem.Object, s_projectDir));
    }

    [Fact]
    public void GetImportsFilePath_DefaultsToProjectRoot_WhenNoComponentsFolder()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        fileSystem.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(false);

        Assert.Equal(Path.Combine(s_projectDir, "_Imports.razor"), IgniteUIBlazorHelper.GetImportsFilePath(fileSystem.Object, s_projectDir));
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void GetClientImportsFilePath_PrefersRootImports(bool rootExists, bool componentsExists, bool expectComponents)
    {
        var rootImports = Path.Combine(s_projectDir, "_Imports.razor");
        var componentsImports = Path.Combine(s_projectDir, "Components", "_Imports.razor");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(rootImports)).Returns(rootExists);
        fileSystem.Setup(f => f.FileExists(componentsImports)).Returns(componentsExists);

        Assert.Equal(expectComponents ? componentsImports : rootImports, IgniteUIBlazorHelper.GetClientImportsFilePath(fileSystem.Object, s_projectDir));
    }

    #endregion
}
