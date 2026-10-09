// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        Assert.Equal(expectedFound, IgniteUIBlazorHelper.TryGetLinkedTheme(content, out var theme, out var variant));
        Assert.Equal(expectedTheme, theme);
        Assert.Equal(expectedVariant, variant);
    }

    private const string LiteMaterialDark = "_content/IgniteUI.Blazor/themes/dark/material.css";

    [Fact]
    public void GetThemeRecipeInputs_LinksBeforeHead_WithAssetsSyntaxAndIndentation()
    {
        var hostPage = Path.Combine(s_projectDir, "Components", "App.razor");
        const string content = "<head>\n    <link rel=\"stylesheet\" href=\"@Assets[\"app.css\"]\" />\n    <HeadOutlet />\n</head>\n";

        var (options, properties) = IgniteUIBlazorHelper.GetThemeRecipeInputs(s_projectDir, hostPage, content, LiteMaterialDark);

        Assert.Equal(["IgniteUIAppRazorHost", IgniteUIBlazorHelper.LinkThemeRecipeOption], options);
        Assert.Equal(LiteMaterialDark, properties["$(IgniteUIThemeStylesheetPath)"]);
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
        Assert.Equal(gridLiteTheme, properties["$(IgniteUIExistingThemeStylesheetPath)"]);
        Assert.Equal(LiteMaterialDark, properties["$(IgniteUIThemeStylesheetPath)"]);
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
    [InlineData(true, false, true, "Interactive Server support was added", "'@rendermode InteractiveServer' to them")]
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
        Assert.Equal(expectedAddedNote is not null, guidance.Contains("Interactive Server support was added"));
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
    [InlineData("a\r\nb\r\n", "\r\n")]
    [InlineData("a\nb\n", "\n")]
    [InlineData("", "\n")]
    [InlineData(null, "\n")]
    public void DetectLineEnding_ReturnsDominantLineEnding(string? content, string expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.DetectLineEnding(content));

    [Theory]
    [InlineData("<head></head>", true)]
    [InlineData("<HEAD></HEAD>", true)]
    [InlineData("<body></body>", false)]
    [InlineData(null, false)]
    public void ContainsHeadClosingTag_IsCaseInsensitive(string? content, bool expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.ContainsHeadClosingTag(content));

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
