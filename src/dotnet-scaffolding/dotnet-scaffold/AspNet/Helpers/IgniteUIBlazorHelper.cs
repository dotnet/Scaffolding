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
        @"_content/IgniteUI\.Blazor(?:\.GridLite)?/(?:css/)?themes/(?:light|dark)/(?:bootstrap|material|fluent|indigo)\.css",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Normalizes the theme name; falls back to <see cref="DefaultTheme"/> when null, empty or unknown.
    /// </summary>
    internal static string NormalizeTheme(string? theme)
        => Themes.FirstOrDefault(t => t.Equals(theme, StringComparison.OrdinalIgnoreCase)) ?? DefaultTheme;

    /// <summary>
    /// Normalizes the theme variant; falls back to <see cref="DefaultThemeVariant"/> when null, empty or unknown.
    /// </summary>
    internal static string NormalizeThemeVariant(string? variant)
        => ThemeVariants.FirstOrDefault(v => v.Equals(variant, StringComparison.OrdinalIgnoreCase)) ?? DefaultThemeVariant;

    /// <summary>
    /// Builds the project-relative path of the IgniteUI.Blazor theme stylesheet to link. Both packages are always
    /// installed, so the IgniteUI.Blazor.Lite theme is used: it styles the core components and the GridLite grid.
    /// </summary>
    /// <param name="theme">Theme name (see <see cref="Themes"/>).</param>
    /// <param name="variant">Theme variant (see <see cref="ThemeVariants"/>).</param>
    internal static string GetThemeStylesheetPath(string? theme, string? variant)
        => $"{LiteStaticAssetsRoot}/themes/{NormalizeThemeVariant(variant)}/{NormalizeTheme(theme)}.css";

    /// <summary>
    /// Detects whether the project is a standalone Blazor WebAssembly project, i.e. uses the
    /// 'Microsoft.NET.Sdk.BlazorWebAssembly' SDK or bootstraps with 'WebAssemblyHostBuilder'.
    /// </summary>
    /// <param name="projectFileContent">The .csproj content.</param>
    /// <param name="programFileContent">The Program.cs content, or null when the file does not exist.</param>
    internal static bool IsWebAssemblyProject(string? projectFileContent, string? programFileContent)
    {
        if (!string.IsNullOrEmpty(projectFileContent) &&
            Regex.IsMatch(projectFileContent,
                @"Sdk\s*=\s*""Microsoft\.NET\.Sdk\.BlazorWebAssembly""|<Sdk\s+Name\s*=\s*""Microsoft\.NET\.Sdk\.BlazorWebAssembly""",
                RegexOptions.IgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrEmpty(programFileContent) &&
               programFileContent.Contains("WebAssemblyHostBuilder", StringComparison.Ordinal);
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
    /// Returns true when the Program.cs of a Blazor Web App registers an interactive render mode
    /// (server, WebAssembly or both). Ignite UI components render nothing usable under static SSR.
    /// </summary>
    internal static bool HasInteractiveRenderModeServices(string? programFileContent)
        => !string.IsNullOrEmpty(programFileContent) &&
           (programFileContent.Contains("AddInteractiveServerComponents", StringComparison.Ordinal) ||
            programFileContent.Contains("AddInteractiveWebAssemblyComponents", StringComparison.Ordinal));

    /// <summary>
    /// Returns true when the given razor content declares a render mode (<c>@rendermode</c> directive
    /// or a <c>@rendermode="..."</c> attribute, e.g. on &lt;Routes /&gt; in App.razor).
    /// </summary>
    internal static bool DeclaresRenderMode(string? razorContent)
        => !string.IsNullOrEmpty(razorContent) && razorContent.Contains("@rendermode", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Detects the dominant line ending of the given text ("\r\n" or "\n").
    /// </summary>
    internal static string DetectLineEnding(string? content)
        => !string.IsNullOrEmpty(content) && content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
}
