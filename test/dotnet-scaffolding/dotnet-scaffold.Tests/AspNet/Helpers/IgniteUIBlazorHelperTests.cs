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

    #region --package option

    [Theory]
    [InlineData("Lite")]
    [InlineData("GridLite")]
    [InlineData("All")]
    [InlineData("lite")]
    [InlineData("GRIDLITE")]
    [InlineData("all")]
    public void IsValidPackageOption_AcceptsKnownValuesCaseInsensitively(string package)
        => Assert.True(IgniteUIBlazorHelper.IsValidPackageOption(package));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Grid")]
    [InlineData("IgniteUI.Blazor.Lite")]
    public void IsValidPackageOption_RejectsUnknownValues(string? package)
        => Assert.False(IgniteUIBlazorHelper.IsValidPackageOption(package));

    [Theory]
    [InlineData("Lite", true, false)]
    [InlineData("GridLite", false, true)]
    [InlineData("All", true, true)]
    [InlineData("all", true, true)]
    [InlineData(null, false, false)]
    public void IncludesLite_And_IncludesGridLite_MatchSelection(string? package, bool expectLite, bool expectGridLite)
    {
        Assert.Equal(expectLite, IgniteUIBlazorHelper.IncludesLite(package));
        Assert.Equal(expectGridLite, IgniteUIBlazorHelper.IncludesGridLite(package));
    }

    #endregion

    #region Theme normalization

    [Theory]
    [InlineData("bootstrap", "bootstrap")]
    [InlineData("Material", "material")]
    [InlineData("FLUENT", "fluent")]
    [InlineData("indigo", "indigo")]
    [InlineData(null, "bootstrap")]
    [InlineData("", "bootstrap")]
    [InlineData("unknown", "bootstrap")]
    public void NormalizeTheme_ReturnsKnownThemeOrDefault(string? theme, string expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.NormalizeTheme(theme));

    [Theory]
    [InlineData("light", "light")]
    [InlineData("Dark", "dark")]
    [InlineData(null, "light")]
    [InlineData("", "light")]
    [InlineData("night", "light")]
    public void NormalizeThemeVariant_ReturnsKnownVariantOrDefault(string? variant, string expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.NormalizeThemeVariant(variant));

    [Fact]
    public void Themes_ContainAllShippedThemes()
    {
        Assert.Equal(new[] { "bootstrap", "material", "fluent", "indigo" }, IgniteUIBlazorHelper.Themes);
        Assert.Equal(new[] { "light", "dark" }, IgniteUIBlazorHelper.ThemeVariants);
        Assert.Equal(new[] { "Lite", "GridLite", "All" }, IgniteUIBlazorHelper.PackageOptions);
    }

    #endregion

    #region Stylesheet path

    [Fact]
    public void GetThemeStylesheetPath_UsesLiteRoot_WhenLiteStylesheetRequested()
        => Assert.Equal("_content/IgniteUI.Blazor/themes/light/bootstrap.css",
            IgniteUIBlazorHelper.GetThemeStylesheetPath(useLiteStylesheet: true, "bootstrap", "light"));

    [Fact]
    public void GetThemeStylesheetPath_UsesGridLiteRoot_WhenGridLiteOnly()
        => Assert.Equal("_content/IgniteUI.Blazor.GridLite/css/themes/dark/material.css",
            IgniteUIBlazorHelper.GetThemeStylesheetPath(useLiteStylesheet: false, "material", "dark"));

    [Fact]
    public void GetThemeStylesheetPath_NormalizesThemeAndVariant()
        => Assert.Equal("_content/IgniteUI.Blazor/themes/light/bootstrap.css",
            IgniteUIBlazorHelper.GetThemeStylesheetPath(useLiteStylesheet: true, "not-a-theme", null));

    [Theory]
    [InlineData("_content/IgniteUI.Blazor/themes/light/bootstrap.css")]
    [InlineData("_content/IgniteUI.Blazor/themes/dark/indigo.css")]
    [InlineData("_content/IgniteUI.Blazor.GridLite/css/themes/light/fluent.css")]
    [InlineData("_CONTENT/IGNITEUI.BLAZOR/THEMES/DARK/MATERIAL.CSS")]
    public void ThemeStylesheetRegex_MatchesEveryGeneratedPath(string path)
        => Assert.Matches(IgniteUIBlazorHelper.ThemeStylesheetRegex, path);

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
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk.Web\"><ItemGroup><PackageReference Include=\"IgniteUI.Blazor.Lite\" Version=\"0.1.1\" /></ItemGroup></Project>", true)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk.Web\"><ItemGroup><PackageReference Version=\"0.1.1\" Include=\"igniteui.blazor.lite\" /></ItemGroup></Project>", true)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk.Web\"><ItemGroup><PackageReference Include=\"IgniteUI.Blazor.GridLite\" Version=\"0.9.2\" /></ItemGroup></Project>", false)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk.Web\" />", false)]
    [InlineData(null, false)]
    public void ProjectReferencesLitePackage_DetectsPackageReference(string? csproj, bool expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.ProjectReferencesLitePackage(csproj));

    [Fact]
    public void IsWebAssemblyProject_TrueForBlazorWebAssemblySdkAttribute()
        => Assert.True(IgniteUIBlazorHelper.IsWebAssemblyProject("<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"></Project>", null));

    [Fact]
    public void IsWebAssemblyProject_TrueForBlazorWebAssemblySdkElement()
        => Assert.True(IgniteUIBlazorHelper.IsWebAssemblyProject("<Project>\n  <Sdk Name=\"Microsoft.NET.Sdk.BlazorWebAssembly\" />\n</Project>", null));

    [Fact]
    public void IsWebAssemblyProject_TrueWhenProgramUsesWebAssemblyHostBuilder()
        => Assert.True(IgniteUIBlazorHelper.IsWebAssemblyProject("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>",
            "var builder = WebAssemblyHostBuilder.CreateDefault(args);"));

    [Fact]
    public void IsWebAssemblyProject_FalseForWebSdkWithWebApplicationBuilder()
        => Assert.False(IgniteUIBlazorHelper.IsWebAssemblyProject("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>",
            "var builder = WebApplication.CreateBuilder(args);"));

    [Fact]
    public void IsWebAssemblyProject_FalseForNullInputs()
        => Assert.False(IgniteUIBlazorHelper.IsWebAssemblyProject(null, null));

    [Theory]
    [InlineData("builder.Services.AddRazorComponents().AddInteractiveServerComponents();", true)]
    [InlineData("builder.Services.AddRazorComponents()\n    .AddInteractiveWebAssemblyComponents();", true)]
    [InlineData("builder.Services.AddRazorComponents();", false)]
    [InlineData(null, false)]
    public void HasInteractiveRenderModeServices_DetectsInteractiveRegistrations(string? program, bool expected)
        => Assert.Equal(expected, IgniteUIBlazorHelper.HasInteractiveRenderModeServices(program));

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

    #endregion
}
