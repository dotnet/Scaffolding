// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helpers for the Syncfusion Blazor Toolkit setup scaffolder.
/// </summary>
internal static class SyncfusionBlazorToolkitHelper
{
    // The theme entry in syncfusionBlazorToolkitChanges.json uses a
    // $(ThemeFile) placeholder for the file name. We rewrite that entry
    // at runtime to point at the resolved host file
    // (Components/App.razor or wwwroot/index.html) or drop it entirely
    // when no host file was found.
    internal const string ThemeFilePlaceholder = "$(ThemeFile)";

    // FileName marker used to identify the file entry that needs to be
    // removed/rewritten at runtime to point at the discovered
    // _Imports.razor file.
    internal const string ImportsFileMarker = "Components\\_Imports.razor";

    // Theme block (link to fluent.min.css) used to inject the stylesheet
    // into the resolved host file when a host is available. THEMEFILE is
    // replaced with the actual file path at runtime.
    private const string ThemeBlockJson =
        "{" +
        "\"FileName\":\"THEMEFILE\"," +
        "\"Replacements\":[" +
            "{" +
                "\"ReplaceSnippet\":[\"</head>\"]," +
                "\"MultiLineBlock\":[" +
                    "\"    <link href=\\\"_content/Syncfusion.Blazor.Toolkit/styles/fluent.min.css\\\" rel=\\\"stylesheet\\\" />\"," +
                    "\"</head>\"" +
                "]," +
                "\"CheckBlock\":\"Syncfusion.Blazor.Toolkit/styles/fluent.min.css\"" +
            "}" +
        "]" +
        "}";

    // A robust, anchor-free snippet that adds @using Syncfusion.Blazor.Toolkit
    // to a discovered _Imports.razor file. The CodeModifier's
    // 'applicableCodeChanges' filter (Block text already present) makes this
    // idempotent and resilient to missing framework usings such as
    // @using Microsoft.AspNetCore.Components.Forms. The block is also
    // appended (not replaced) so the snippet is safe regardless of which
    // other usings the project already has.
    private const string ImportsBlockJson =
        "{" +
        "\"FileName\":\"IMPORTSFILE\"," +
        "\"Replacements\":[" +
            "{" +
                "\"Block\":\"@using Syncfusion.Blazor.Toolkit\"" +
            "}" +
        "]" +
        "}";

    /// <summary>
    /// Canonicalizes a project-relative file path so that all forward slashes
    /// are used regardless of host OS. Avoids path-separator drift between
    /// Windows and Linux/macOS environments.
    /// </summary>
    public static string CanonicalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Loads the syncfusionBlazorToolkitChanges.json code-modification config
    /// from disk and returns a JSON string with the file entries rewritten
    /// to match the files actually present in the target project.
    ///
    /// <para>The <c>$(ThemeFile)</c> placeholder entry is replaced with a
    /// concrete entry for <paramref name="themeFile"/> when supplied, or
    /// dropped when <paramref name="themeFile"/> is null/empty.</para>
    ///
    /// <para>The <c>Components\_Imports.razor</c> anchor entry is replaced
    /// with a concrete, anchor-free entry for <paramref name="importsFile"/>
    /// when supplied, or dropped when <paramref name="importsFile"/> is
    /// null/empty (so the scaffolder still succeeds in projects that don't
    /// host a _Imports.razor file under Components/ or anywhere else).</para>
    ///
    /// <para>Both file paths are normalized to forward-slash form before
    /// being emitted, ensuring identical configuration across Windows and
    /// Linux.</para>
    /// </summary>
    /// <param name="codeModificationFilePath">Absolute path of the source JSON file.</param>
    /// <param name="themeFile">Project-relative theme host file path
    /// (e.g. "Components/App.razor", "wwwroot/index.html") or null to drop
    /// the theme entry.</param>
    /// <param name="importsFile">Project-relative path of the discovered
    /// _Imports.razor file (e.g. "Components/_Imports.razor" or
    /// "_Imports.razor") or null to drop the imports entry.</param>
    /// <param name="logger">Optional logger for structured diagnostic
    /// output. When null, logging is skipped.</param>
    public static string? BuildResolvedCodeModifierConfigJson(
        string codeModificationFilePath,
        string? themeFile,
        string? importsFile,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(codeModificationFilePath) || !File.Exists(codeModificationFilePath))
        {
            logger?.LogWarning(
                "Syncfusion Blazor Toolkit code-modification config file '{ConfigPath}' was not found. " +
                "Skipping code-modification step.",
                codeModificationFilePath);
            return null;
        }

        string jsonText;
        try
        {
            jsonText = File.ReadAllText(codeModificationFilePath);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex,
                "Syncfusion Blazor Toolkit failed to read the code-modification config file '{ConfigPath}'. " +
                "Verify the file exists, is readable, and is not locked by another process.",
                codeModificationFilePath);
            return null;
        }

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            logger?.LogWarning(
                "Syncfusion Blazor Toolkit code-modification config file '{ConfigPath}' is empty. " +
                "Skipping code-modification step.",
                codeModificationFilePath);
            return null;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonText);
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex,
                "Syncfusion Blazor Toolkit code-modification config file '{ConfigPath}' is not valid JSON. " +
                "Repair the JSON or restore the file from source control, then re-run the scaffolder.",
                codeModificationFilePath);
            return null;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("Files", out JsonElement filesElement) ||
                filesElement.ValueKind != JsonValueKind.Array)
            {
                logger?.LogError(
                    "Syncfusion Blazor Toolkit code-modification config file '{ConfigPath}' is missing the required 'Files' array. " +
                    "Skipping code-modification step.",
                    codeModificationFilePath);
                return null;
            }

            // Build a new Files array. We carry over each entry except the
            // theme placeholder and the imports anchor. Those are
            // re-emitted (or dropped) below based on what was actually
            // discovered in the target project.
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Files");
                writer.WriteStartArray();
                foreach (JsonElement fileEntry in filesElement.EnumerateArray())
                {
                    string? fileName = null;
                    if (fileEntry.ValueKind == JsonValueKind.Object &&
                        fileEntry.TryGetProperty("FileName", out JsonElement fileNameElement) &&
                        fileNameElement.ValueKind == JsonValueKind.String)
                    {
                        fileName = fileNameElement.GetString();
                    }

                    if (string.Equals(fileName, ThemeFilePlaceholder, StringComparison.Ordinal))
                    {
                        // Drop the placeholder entry; we'll re-add it below
                        // if a theme file was discovered.
                        continue;
                    }

                    if (string.Equals(fileName, ImportsFileMarker, StringComparison.Ordinal))
                    {
                        // Drop the anchor-based placeholder; we'll re-add
                        // it below with an anchor-free Block-only snippet
                        // when an _Imports.razor file was discovered.
                        continue;
                    }

                    fileEntry.WriteTo(writer);
                }

                if (!string.IsNullOrEmpty(themeFile))
                {
                    string canonicalThemeFile = CanonicalizePath(themeFile);
                    string themeEntryJson = ThemeBlockJson.Replace("THEMEFILE", EscapeForJson(canonicalThemeFile));
                    using JsonDocument themeEntryDoc = JsonDocument.Parse(themeEntryJson);
                    themeEntryDoc.RootElement.WriteTo(writer);
                }

                if (!string.IsNullOrEmpty(importsFile))
                {
                    string canonicalImportsFile = CanonicalizePath(importsFile);
                    string importsEntryJson = ImportsBlockJson.Replace("IMPORTSFILE", EscapeForJson(canonicalImportsFile));
                    using JsonDocument importsEntryDoc = JsonDocument.Parse(importsEntryJson);
                    importsEntryDoc.RootElement.WriteTo(writer);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// JSON-escapes a string for inline insertion into a JSON literal.
    /// We build the theme block as a string template and substitute the
    /// file path, so the path itself must be escaped (backslashes and
    /// quotes).
    /// </summary>
    private static string EscapeForJson(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
