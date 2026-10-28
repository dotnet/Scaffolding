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

    /// <summary>Matches a &lt;link&gt; element.</summary>
    private static readonly Regex LinkElementRegex = new(@"<link\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Finds the first active (not commented-out) &lt;link&gt; element of the host page that references an Ignite UI theme
    /// stylesheet (IgniteUI.Blazor or GridLite-only).
    /// </summary>
    /// <param name="hostPagePath">The host page path, which determines whether Razor comments apply.</param>
    /// <param name="hostPageContent">The host page content.</param>
    /// <param name="linkElement">The match of the &lt;link&gt; element as written in the page (its index is into the page).</param>
    /// <param name="themeStylesheet">The match of <see cref="ThemeStylesheetRegex"/> within <paramref name="linkElement"/>.</param>
    /// <returns>True when the page links an Ignite UI theme stylesheet outside of comments.</returns>
    internal static bool TryFindThemeLink(string? hostPagePath, string? hostPageContent, out Match linkElement, out Match themeStylesheet)
    {
        // Comments are masked without changing indices or the active markup, so the match is the element as written.
        var activeContent = MarkupHelper.MaskComments(hostPagePath, hostPageContent);
        foreach (Match link in LinkElementRegex.Matches(activeContent))
        {
            var match = ThemeStylesheetRegex.Match(link.Value);
            if (match.Success)
            {
                linkElement = link;
                themeStylesheet = match;
                return true;
            }
        }

        linkElement = Match.Empty;
        themeStylesheet = Match.Empty;
        return false;
    }

    /// <summary>
    /// Reads the Ignite UI theme and variant that the host page already links (see <see cref="TryFindThemeLink"/>), so
    /// that a re-run keeps them when '--theme' / '--theme-variant' are omitted. Commented-out links are ignored.
    /// </summary>
    /// <returns>True when the page links an Ignite UI theme stylesheet.</returns>
    internal static bool TryGetLinkedTheme(string? hostPagePath, string? hostPageContent, out string theme, out string themeVariant)
    {
        var found = TryFindThemeLink(hostPagePath, hostPageContent, out _, out var match);
        theme = found ? match.Groups["theme"].Value.ToLowerInvariant() : DefaultTheme;
        themeVariant = found ? match.Groups["variant"].Value.ToLowerInvariant() : DefaultThemeVariant;
        return found;
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
            if (fileSystem.FileExists(path) && ContainsHeadClosingTag(path, fileSystem.ReadAllText(path)))
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
    /// Returns true when the given host page content contains a closing &lt;/head&gt; tag outside of comments.
    /// </summary>
    internal static bool ContainsHeadClosingTag(string? hostPagePath, string? content)
        => IndexOfHeadClosingTag(hostPagePath, content) >= 0;

    /// <summary>
    /// Returns the index of the first closing &lt;/head&gt; tag outside of comments, or -1 when there is none.
    /// </summary>
    private static int IndexOfHeadClosingTag(string? hostPagePath, string? content)
        => MarkupHelper.MaskComments(hostPagePath, content).IndexOf("</head>", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true when a .razor host page already uses the fingerprinted static asset collection
    /// (<c>@Assets["..."]</c>, .NET 9+), in which case the new link should use the same syntax.
    /// </summary>
    internal static bool UsesAssetsCollection(string? hostPagePath, string? content)
        => !string.IsNullOrEmpty(hostPagePath) &&
           hostPagePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) &&
           MarkupHelper.MaskComments(hostPagePath, content).Contains("@Assets[", StringComparison.Ordinal);

    /// <summary>
    /// Builds the &lt;link&gt; element for the theme stylesheet.
    /// </summary>
    internal static string BuildStylesheetLink(string stylesheetPath, bool useAssetsCollection)
        => useAssetsCollection
            ? $"<link rel=\"stylesheet\" href=\"@Assets[\"{stylesheetPath}\"]\" />"
            : $"<link href=\"{stylesheetPath}\" rel=\"stylesheet\" />";

    /// <summary>
    /// Returns how to link the theme stylesheet manually when the theme recipe could not: replace the Ignite UI theme
    /// &lt;link&gt; the host page already has (so that only one theme is linked), or add a &lt;link&gt; to its &lt;head&gt;.
    /// </summary>
    /// <param name="themeCodeChangeOptions">The theme recipe options from <see cref="GetThemeRecipeInputs"/>.</param>
    /// <param name="themeCodeModifierProperties">The theme recipe properties from <see cref="GetThemeRecipeInputs"/>.</param>
    /// <param name="stylesheetPath">The theme stylesheet path to link.</param>
    /// <param name="hostPagePath">The host page, or null when none was found.</param>
    internal static string GetThemeManualInstructions(
        IEnumerable<string> themeCodeChangeOptions, IReadOnlyDictionary<string, string> themeCodeModifierProperties, string stylesheetPath, string? hostPagePath)
    {
        if (themeCodeChangeOptions.Contains(SwapThemeRecipeOption) &&
            themeCodeModifierProperties.TryGetValue("$(IgniteUIExistingThemeLink)", out var existingLink) &&
            themeCodeModifierProperties.TryGetValue("$(IgniteUISwappedThemeLink)", out var swappedLink))
        {
            // The swapped text can span whole lines around the link (see GetThemeSwap); name just the link elements,
            // i.e. the <link> element in which the two texts first differ.
            var firstDifference = 0;
            while (firstDifference < Math.Min(existingLink.Length, swappedLink.Length) && existingLink[firstDifference] == swappedLink[firstDifference])
            {
                firstDifference++;
            }

            if (LinkElementRegex.Matches(existingLink).FirstOrDefault(link => link.Index <= firstDifference && firstDifference < link.Index + link.Length) is { } link)
            {
                swappedLink = swappedLink.Substring(link.Index, link.Length + swappedLink.Length - existingLink.Length);
                existingLink = link.Value;
            }

            return $"To finish the setup, replace the Ignite UI theme link {existingLink} in '{hostPagePath}' with {swappedLink}";
        }

        return $"To finish the setup, add the following line to the <head> of the page that hosts your Blazor app: {BuildStylesheetLink(stylesheetPath, useAssetsCollection: false)}";
    }

    /// <summary>
    /// Returns the completion guidance for a Blazor Web App: Ignite UI components need an interactive render mode, and
    /// the scaffolder does not change render modes on existing pages or on &lt;Routes&gt;. Only the render modes the app
    /// is configured for are suggested (InteractiveAuto only when both Server and WebAssembly are configured).
    /// Returns null when a global interactive render mode already makes every page interactive.
    /// </summary>
    /// <param name="usesInteractiveServer">Interactive Server is configured (including support added by the scaffolder).</param>
    /// <param name="usesInteractiveWebAssembly">Interactive WebAssembly is configured.</param>
    /// <param name="addedInteractiveServer">The scaffolder added or completed Interactive Server support in Program.cs.</param>
    /// <param name="hasGlobalRenderMode">App.razor or Routes.razor declares a render mode, e.g. on &lt;Routes /&gt;.</param>
    internal static string? GetRenderModeGuidance(bool usesInteractiveServer, bool usesInteractiveWebAssembly, bool addedInteractiveServer, bool hasGlobalRenderMode)
    {
        if (hasGlobalRenderMode || (!usesInteractiveServer && !usesInteractiveWebAssembly))
        {
            return null;
        }

        var added = addedInteractiveServer
            ? "Interactive Server support was enabled in Program.cs ('AddInteractiveServerComponents()' and 'AddInteractiveServerRenderMode()'); existing pages keep their render modes. "
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
    /// <summary>Recipe option that swaps the theme stylesheet path in the Ignite UI theme &lt;link&gt; the host page already has.</summary>
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
    /// (igniteUIBlazorThemeChanges.json): swap the stylesheet path in the Ignite UI theme &lt;link&gt; element the page
    /// already has, or insert a &lt;link&gt; right before &lt;/head&gt; with the page's indentation, line endings and (for
    /// a .razor page that already uses it) the fingerprinted <c>@Assets["..."]</c> syntax. Commented-out markup is
    /// ignored, so a commented-out theme link is neither swapped nor counted as linked.
    /// </summary>
    /// <param name="projectDirectory">The project directory.</param>
    /// <param name="hostPagePath">The host page found by <see cref="FindHostPage"/>.</param>
    /// <param name="hostPageContent">The host page content.</param>
    /// <param name="stylesheetPath">The theme stylesheet path to link.</param>
    internal static (List<string> Options, Dictionary<string, string> Properties) GetThemeRecipeInputs(
        string projectDirectory, string hostPagePath, string hostPageContent, string stylesheetPath)
    {
        var hostPageOption = HostPageRecipeOptions[Path.GetRelativePath(projectDirectory, hostPagePath)];
        var properties = new Dictionary<string, string>();
        if (TryFindThemeLink(hostPagePath, hostPageContent, out var linkElement, out var linkedTheme))
        {
            // Swap within the active element as written rather than the bare path, so that a commented-out link to a
            // different theme is neither changed instead of the active one nor taken as the swapped link.
            var (existing, swapped) = GetThemeSwap(hostPageContent, linkElement.Index, linkElement.Length, linkElement.Index + linkedTheme.Index, linkedTheme.Length, stylesheetPath);
            properties["$(IgniteUIExistingThemeLink)"] = existing;
            properties["$(IgniteUISwappedThemeLink)"] = swapped;
            return ([hostPageOption, SwapThemeRecipeOption], properties);
        }

        // The recipe replaces '</head>' with the link followed by '</head>'. The tag is passed as written in the page
        // (the replacement is case-sensitive), the link gets the page's line ending, and when '</head>' starts its line
        // the link is indented one level deeper than it.
        var headIndex = IndexOfHeadClosingTag(hostPagePath, hostPageContent);
        var headClosingTag = hostPageContent.Substring(headIndex, "</head>".Length);
        var lineStart = hostPageContent.LastIndexOf('\n', Math.Max(headIndex - 1, 0)) + 1;
        var headIndent = hostPageContent[lineStart..headIndex];
        var link = BuildStylesheetLink(stylesheetPath, UsesAssetsCollection(hostPagePath, hostPageContent));
        properties["$(IgniteUIHeadClosingTag)"] = headClosingTag;
        properties["$(IgniteUIThemeLinkBeforeHead)"] = string.IsNullOrWhiteSpace(headIndent)
            ? $"{(headIndent.Contains('\t') ? "\t" : "    ")}{link}{MarkupHelper.DetectLineEnding(hostPageContent)}{headIndent}{headClosingTag}"
            : $"{link}{headClosingTag}";
        return ([hostPageOption, LinkThemeRecipeOption], properties);
    }

    /// <summary>
    /// Returns the host page text that the swap recipe replaces and its replacement: the active theme &lt;link&gt; with
    /// the requested stylesheet path. The recipe skips a change whose replacement the page already contains (ignoring
    /// whitespace), even inside a comment, and replaces every occurrence of the replaced text. So when the page also has
    /// the requested link (e.g. commented out) or another copy of the active one, the text is widened by whole lines
    /// around the active link until the replacement is new to the page and the replaced text occurs only there.
    /// </summary>
    /// <param name="content">The host page content.</param>
    /// <param name="linkIndex">The index of the active theme &lt;link&gt; element in <paramref name="content"/>.</param>
    /// <param name="linkLength">The length of the &lt;link&gt; element.</param>
    /// <param name="stylesheetIndex">The index of the linked theme stylesheet path in <paramref name="content"/>.</param>
    /// <param name="stylesheetLength">The length of the linked theme stylesheet path.</param>
    /// <param name="stylesheetPath">The theme stylesheet path to link.</param>
    private static (string Existing, string Swapped) GetThemeSwap(
        string content, int linkIndex, int linkLength, int stylesheetIndex, int stylesheetLength, string stylesheetPath)
    {
        var trimmedContent = TrimRecipeWhitespace(content);
        var (start, end) = (linkIndex, linkIndex + linkLength);
        while (true)
        {
            var existing = content[start..end];
            var swapped = existing.Remove(stylesheetIndex - start, stylesheetLength).Insert(stylesheetIndex - start, stylesheetPath);
            if (swapped == existing ||
                (start == 0 && end == content.Length) ||
                (!trimmedContent.Contains(TrimRecipeWhitespace(swapped), StringComparison.Ordinal) &&
                 content.IndexOf(existing, StringComparison.Ordinal) == content.LastIndexOf(existing, StringComparison.Ordinal)))
            {
                return (existing, swapped);
            }

            // Widen by a line at each end: to the start of the previous line, or of the current one when the text starts
            // within it.
            if (start > 0)
            {
                var previous = content[start - 1] == '\n' ? start - 2 : start - 1;
                start = previous < 0 ? 0 : content.LastIndexOf('\n', previous) + 1;
            }

            // To the end of the next line, or of the current one when the text ends within it, without its line break.
            if (end < content.Length)
            {
                var next = end;
                if (content[next] == '\r')
                {
                    next++;
                }

                if (next < content.Length && content[next] == '\n')
                {
                    next++;
                }

                var lineBreak = content.IndexOf('\n', next);
                end = lineBreak < 0 ? content.Length : lineBreak;
                if (end > next && content[end - 1] == '\r')
                {
                    end--;
                }
            }
        }
    }

    /// <summary>
    /// Removes the characters that the code modifier ignores when it checks whether a change is already applied
    /// (see ProjectModifierHelper.TrimStatement in Microsoft.DotNet.Scaffolding.CodeModification).
    /// </summary>
    private static string TrimRecipeWhitespace(string text)
        => string.Concat(text.Where(c => c is not (' ' or '\r' or '\n' or ';')));
}
