// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Internal.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods and constants for the Ignite UI for Blazor scaffolder
/// (IgniteUI.Blazor.Lite and IgniteUI.Blazor.GridLite).
/// </summary>
internal static class IgniteUIBlazorHelper
{
    /// <summary>Default theme.</summary>
    internal const string DefaultTheme = "bootstrap";
    /// <summary>All themes shipped by Ignite UI for Blazor.</summary>
    internal static readonly List<string> Themes = [DefaultTheme, "material", "fluent", "indigo"];

    /// <summary>Default theme variant.</summary>
    internal const string DefaultThemeVariant = "light";
    /// <summary>All theme variants shipped by Ignite UI for Blazor.</summary>
    internal static readonly List<string> ThemeVariants = [DefaultThemeVariant, "dark"];

    /// <summary>The namespace that must be imported in _Imports.razor (and Program.cs for service registration).</summary>
    internal const string ControlsNamespace = "IgniteUI.Blazor.Controls";
    /// <summary>Static web asset root of the IgniteUI.Blazor.Lite package.</summary>
    internal const string LiteStaticAssetsRoot = "_content/IgniteUI.Blazor";

    /// <summary>
    /// Matches any Ignite UI theme stylesheet path (Lite or GridLite, any theme, any variant) so an
    /// existing link can be detected and swapped instead of linking two themes at once. GridLite-only
    /// links are still matched so that re-running the scaffolder replaces them with the IgniteUI.Blazor
    /// theme, which also styles the grid.
    /// </summary>
    internal static readonly Regex ThemeStylesheetRegex = new(
        @"_content/IgniteUI\.Blazor(?:\.GridLite)?/(?:css/)?themes/(?<variant>light|dark)/(?<theme>bootstrap|material|fluent|indigo)\.css",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Reads the Ignite UI theme and variant that the host page already links (the first Ignite UI theme stylesheet,
    /// IgniteUI.Blazor or GridLite-only), so that a re-run keeps them when '--theme' / '--theme-variant' are omitted.
    /// </summary>
    /// <returns>True when the content links an Ignite UI theme stylesheet.</returns>
    internal static bool TryGetLinkedTheme(string? hostPageContent, out string theme, out string themeVariant)
    {
        var match = string.IsNullOrEmpty(hostPageContent) ? Match.Empty : ThemeStylesheetRegex.Match(hostPageContent);
        theme = match.Success ? match.Groups["theme"].Value.ToLowerInvariant() : DefaultTheme;
        themeVariant = match.Success ? match.Groups["variant"].Value.ToLowerInvariant() : DefaultThemeVariant;
        return match.Success;
    }

    /// <summary>
    /// Normalizes the '--theme' value: <see cref="DefaultTheme"/> when it is omitted, otherwise the matching entry of
    /// <see cref="Themes"/> (case-insensitive).
    /// </summary>
    /// <returns>False when a value is given that is not a supported theme.</returns>
    internal static bool TryNormalizeTheme(string? theme, out string normalizedTheme)
        => TryNormalize(theme, Themes, DefaultTheme, out normalizedTheme);

    /// <summary>
    /// Normalizes the '--theme-variant' value: <see cref="DefaultThemeVariant"/> when it is omitted, otherwise the
    /// matching entry of <see cref="ThemeVariants"/> (case-insensitive).
    /// </summary>
    /// <returns>False when a value is given that is not a supported theme variant.</returns>
    internal static bool TryNormalizeThemeVariant(string? variant, out string normalizedVariant)
        => TryNormalize(variant, ThemeVariants, DefaultThemeVariant, out normalizedVariant);

    /// <summary>
    /// Builds the project-relative path of the IgniteUI.Blazor theme stylesheet to link. Both packages are always
    /// installed, so the IgniteUI.Blazor.Lite theme is used: it styles the core components and the GridLite grid.
    /// </summary>
    /// <param name="theme">A normalized theme name (see <see cref="TryNormalizeTheme"/>).</param>
    /// <param name="variant">A normalized theme variant (see <see cref="TryNormalizeThemeVariant"/>).</param>
    internal static string GetThemeStylesheetPath(string theme, string variant)
        => $"{LiteStaticAssetsRoot}/themes/{variant}/{theme}.css";

    private static bool TryNormalize(string? value, List<string> supportedValues, string defaultValue, out string normalizedValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalizedValue = defaultValue;
            return true;
        }

        var match = supportedValues.FirstOrDefault(supported => supported.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        normalizedValue = match ?? defaultValue;
        return match is not null;
    }

    /// <summary>
    /// Host page candidates, in priority order, relative to the project directory.
    /// </summary>
    internal static readonly string[] HostPageCandidates =
    [
        Path.Combine("Components", "App.razor"),     // Blazor Web App (.NET 8+)
        Path.Combine("Pages", "_Layout.cshtml"),     // Blazor Server (.NET 6 layout page)
        Path.Combine("Pages", "_Host.cshtml"),       // Blazor Server (.NET 7 and earlier)
        Path.Combine("wwwroot", "index.html"),       // Blazor WebAssembly standalone / Blazor Hybrid
    ];

    /// <summary>
    /// Finds the page that hosts the document &lt;head&gt; for the given project directory.
    /// Returns null when no known host page containing a &lt;/head&gt; tag exists.
    /// </summary>
    internal static string? FindHostPage(IFileSystem fileSystem, string projectDirectory)
    {
        foreach (var candidate in HostPageCandidates)
        {
            var path = Path.Combine(projectDirectory, candidate);
            if (fileSystem.FileExists(path) && ContainsHeadClosingTag(fileSystem.ReadAllText(path)))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the _Imports.razor that should receive the Ignite UI using directive. Prefers
    /// 'Components/_Imports.razor' (Blazor Web App), then the project root '_Imports.razor'
    /// (Blazor WebAssembly / Blazor Server). When neither exists, returns the path where a new
    /// file should be created: under 'Components' if that folder exists, otherwise the project root.
    /// </summary>
    internal static string GetImportsFilePath(IFileSystem fileSystem, string projectDirectory)
    {
        var componentsImports = Path.Combine(projectDirectory, "Components", "_Imports.razor");
        if (fileSystem.FileExists(componentsImports))
        {
            return componentsImports;
        }

        var rootImports = Path.Combine(projectDirectory, "_Imports.razor");
        if (fileSystem.FileExists(rootImports))
        {
            return rootImports;
        }

        return fileSystem.DirectoryExists(Path.Combine(projectDirectory, "Components"))
            ? componentsImports
            : rootImports;
    }

    /// <summary>
    /// Finds the _Imports.razor of a Blazor WebAssembly client project: the root '_Imports.razor' by convention,
    /// or 'Components/_Imports.razor' when only that one exists.
    /// </summary>
    internal static string GetClientImportsFilePath(IFileSystem fileSystem, string clientProjectDirectory)
    {
        var rootImports = Path.Combine(clientProjectDirectory, "_Imports.razor");
        var componentsImports = Path.Combine(clientProjectDirectory, "Components", "_Imports.razor");
        return !fileSystem.FileExists(rootImports) && fileSystem.FileExists(componentsImports) ? componentsImports : rootImports;
    }

    /// <summary>
    /// Returns true when the given host page content contains a closing &lt;/head&gt; tag.
    /// </summary>
    internal static bool ContainsHeadClosingTag(string? content)
        => !string.IsNullOrEmpty(content) && content.Contains("</head>", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true when a .razor host page already uses the fingerprinted static asset collection
    /// (<c>@Assets["..."]</c>, .NET 9+), in which case the new link should use the same syntax.
    /// </summary>
    internal static bool UsesAssetsCollection(string? hostPagePath, string? content)
        => !string.IsNullOrEmpty(hostPagePath) &&
           hostPagePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) &&
           !string.IsNullOrEmpty(content) &&
           content.Contains("@Assets[", StringComparison.Ordinal);

    /// <summary>
    /// Builds the &lt;link&gt; element for the theme stylesheet.
    /// </summary>
    internal static string BuildStylesheetLink(string stylesheetPath, bool useAssetsCollection)
        => useAssetsCollection
            ? $"<link rel=\"stylesheet\" href=\"@Assets[\"{stylesheetPath}\"]\" />"
            : $"<link href=\"{stylesheetPath}\" rel=\"stylesheet\" />";

    /// <summary>
    /// Returns the completion guidance for a Blazor Web App: Ignite UI components need an interactive render mode, and
    /// the scaffolder does not change render modes on existing pages or on &lt;Routes&gt;. Only the render modes the app
    /// is configured for are suggested (InteractiveAuto only when both Server and WebAssembly are configured).
    /// Returns null when a global interactive render mode already makes every page interactive.
    /// </summary>
    /// <param name="usesInteractiveServer">Interactive Server is configured (including support added by the scaffolder).</param>
    /// <param name="usesInteractiveWebAssembly">Interactive WebAssembly is configured.</param>
    /// <param name="addedInteractiveServer">The scaffolder added Interactive Server support to Program.cs.</param>
    /// <param name="hasGlobalRenderMode">App.razor or Routes.razor declares a render mode, e.g. on &lt;Routes /&gt;.</param>
    internal static string? GetRenderModeGuidance(bool usesInteractiveServer, bool usesInteractiveWebAssembly, bool addedInteractiveServer, bool hasGlobalRenderMode)
    {
        if (hasGlobalRenderMode || (!usesInteractiveServer && !usesInteractiveWebAssembly))
        {
            return null;
        }

        var added = addedInteractiveServer
            ? "Interactive Server support was added to Program.cs ('AddInteractiveServerComponents()' and 'AddInteractiveServerRenderMode()'); existing pages keep their render modes. "
            : string.Empty;
        var renderModes = usesInteractiveServer && usesInteractiveWebAssembly
            ? "'@rendermode InteractiveServer', '@rendermode InteractiveWebAssembly' or '@rendermode InteractiveAuto' to them (pages that use WebAssembly or Auto belong in the client project)"
            : usesInteractiveServer
                ? "'@rendermode InteractiveServer' to them"
                : "'@rendermode InteractiveWebAssembly' to them (such pages belong in the client project)";
        return $"{added}Pages that use Ignite UI components need an interactive render mode: add {renderModes}.";
    }

    /// <summary>
    /// Returns true when the given razor content declares a render mode (<c>@rendermode</c> directive
    /// or a <c>@rendermode="..."</c> attribute, e.g. on &lt;Routes /&gt; in App.razor).
    /// </summary>
    internal static bool DeclaresRenderMode(string? razorContent)
        => !string.IsNullOrEmpty(razorContent) && razorContent.Contains("@rendermode", StringComparison.OrdinalIgnoreCase);

    /// <summary>Recipe option that links the theme stylesheet right before &lt;/head&gt;.</summary>
    internal const string LinkThemeRecipeOption = "IgniteUILinkTheme";
    /// <summary>Recipe option that swaps the path of an Ignite UI theme stylesheet the host page already links.</summary>
    internal const string SwapThemeRecipeOption = "IgniteUISwapTheme";

    /// <summary>
    /// The recipe option of each host page in igniteUIBlazorThemeChanges.json, keyed by <see cref="HostPageCandidates"/>.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> HostPageRecipeOptions = new Dictionary<string, string>
    {
        [Path.Combine("Components", "App.razor")] = "IgniteUIAppRazorHost",
        [Path.Combine("Pages", "_Layout.cshtml")] = "IgniteUILayoutCshtmlHost",
        [Path.Combine("Pages", "_Host.cshtml")] = "IgniteUIHostCshtmlHost",
        [Path.Combine("wwwroot", "index.html")] = "IgniteUIIndexHtmlHost",
    };

    /// <summary>
    /// Analyzes the host page and returns the options and properties for the shared theme recipe
    /// (igniteUIBlazorThemeChanges.json): swap the path of an Ignite UI theme the page already links, or insert a
    /// &lt;link&gt; right before &lt;/head&gt; with the page's indentation, line endings and (for a .razor page that
    /// already uses it) the fingerprinted <c>@Assets["..."]</c> syntax.
    /// </summary>
    /// <param name="projectDirectory">The project directory.</param>
    /// <param name="hostPagePath">The host page found by <see cref="FindHostPage"/>.</param>
    /// <param name="hostPageContent">The host page content.</param>
    /// <param name="stylesheetPath">The theme stylesheet path to link.</param>
    internal static (List<string> Options, Dictionary<string, string> Properties) GetThemeRecipeInputs(
        string projectDirectory, string hostPagePath, string hostPageContent, string stylesheetPath)
    {
        var hostPageOption = HostPageRecipeOptions[Path.GetRelativePath(projectDirectory, hostPagePath)];
        var properties = new Dictionary<string, string> { ["$(IgniteUIThemeStylesheetPath)"] = stylesheetPath };
        var linkedTheme = ThemeStylesheetRegex.Match(hostPageContent);
        if (linkedTheme.Success)
        {
            properties["$(IgniteUIExistingThemeStylesheetPath)"] = linkedTheme.Value;
            return ([hostPageOption, SwapThemeRecipeOption], properties);
        }

        // The recipe replaces '</head>' with the link followed by '</head>'. The tag is passed as written in the page
        // (the replacement is case-sensitive), the link gets the page's line ending, and when '</head>' starts its line
        // the link is indented one level deeper than it.
        var headIndex = hostPageContent.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        var headClosingTag = hostPageContent.Substring(headIndex, "</head>".Length);
        var lineStart = hostPageContent.LastIndexOf('\n', Math.Max(headIndex - 1, 0)) + 1;
        var headIndent = hostPageContent[lineStart..headIndex];
        var link = BuildStylesheetLink(stylesheetPath, UsesAssetsCollection(hostPagePath, hostPageContent));
        properties["$(IgniteUIHeadClosingTag)"] = headClosingTag;
        properties["$(IgniteUIThemeLinkBeforeHead)"] = string.IsNullOrWhiteSpace(headIndent)
            ? $"{(headIndent.Contains('\t') ? "\t" : "    ")}{link}{DetectLineEnding(hostPageContent)}{headIndent}{headClosingTag}"
            : $"{link}{headClosingTag}";
        return ([hostPageOption, LinkThemeRecipeOption], properties);
    }

    /// <summary>
    /// Detects the dominant line ending of the given text ("\r\n" or "\n").
    /// </summary>
    internal static string DetectLineEnding(string? content)
        => !string.IsNullOrEmpty(content) && content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
}
