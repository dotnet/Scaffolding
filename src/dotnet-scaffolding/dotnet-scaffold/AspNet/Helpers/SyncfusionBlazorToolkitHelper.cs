// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

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
    private const string ThemeFilePlaceholder = "$(ThemeFile)";

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

    /// <summary>
    /// Loads the syncfusionBlazorToolkitChanges.json code-modification config
    /// from disk and returns a JSON string with the theme <c>FileName</c>
    /// entry resolved to <paramref name="themeFile"/>, or with the theme
    /// file entry removed entirely when <paramref name="themeFile"/> is
    /// null/empty.
    /// </summary>
    public static string? BuildResolvedCodeModifierConfigJson(string codeModificationFilePath, string? themeFile)
    {
        if (string.IsNullOrEmpty(codeModificationFilePath) || !File.Exists(codeModificationFilePath))
        {
            return null;
        }

        string jsonText = File.ReadAllText(codeModificationFilePath);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            return null;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonText);
        }
        catch
        {
            return null;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("Files", out JsonElement filesElement) ||
                filesElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            // Build a new Files array. We carry over each entry except the
            // theme placeholder. If a theme file is provided we append a
            // new entry for it at the end of the array.
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
                        // if a theme file is provided.
                        continue;
                    }

                    fileEntry.WriteTo(writer);
                }

                if (!string.IsNullOrEmpty(themeFile))
                {
                    string normalizedThemeFile = themeFile.Replace('\\', Path.DirectorySeparatorChar);
                    string themeEntryJson = ThemeBlockJson.Replace("THEMEFILE", EscapeForJson(normalizedThemeFile));
                    using JsonDocument themeEntryDoc = JsonDocument.Parse(themeEntryJson);
                    themeEntryDoc.RootElement.WriteTo(writer);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
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
        var sb = new System.Text.StringBuilder(input.Length);
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
