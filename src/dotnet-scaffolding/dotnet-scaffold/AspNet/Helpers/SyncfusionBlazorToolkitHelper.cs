// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helpers for the Syncfusion Blazor Toolkit setup scaffolder.
///
/// <para>Setup is limited to the NuGet package, <c>AddSyncfusionBlazorToolkit()</c>
/// in <c>Program.cs</c>, and a single <c>@using Syncfusion.Blazor.Toolkit</c> in
/// the project's <c>_Imports.razor</c>. Syncfusion.Blazor.Toolkit 2.0.0+ ships
/// styles with the assembly, so no host stylesheet link is added.</para>
/// </summary>
internal static class SyncfusionBlazorToolkitHelper
{
    internal const string UsingDirective = "@using Syncfusion.Blazor.Toolkit";
    internal const string ServiceRegistration = "builder.Services.AddSyncfusionBlazorToolkit();";

    /// <summary>
    /// Loads the Program.cs-only code-modification config. Imports are applied
    /// separately on disk so the using is written whether <c>_Imports.razor</c>
    /// lives under <c>Components</c> or at the project root.
    /// </summary>
    public static string? BuildResolvedCodeModifierConfigJson(
        string codeModificationFilePath,
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
                "Syncfusion Blazor Toolkit failed to read the code-modification config file '{ConfigPath}'.",
                codeModificationFilePath);
            return null;
        }

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            logger?.LogWarning(
                "Syncfusion Blazor Toolkit code-modification config file '{ConfigPath}' is empty.",
                codeModificationFilePath);
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            if (!doc.RootElement.TryGetProperty("Files", out JsonElement filesElement) ||
                filesElement.ValueKind != JsonValueKind.Array)
            {
                logger?.LogError(
                    "Syncfusion Blazor Toolkit code-modification config file '{ConfigPath}' is missing the required 'Files' array.",
                    codeModificationFilePath);
                return null;
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Files");
                writer.WriteStartArray();
                foreach (JsonElement fileEntry in filesElement.EnumerateArray())
                {
                    if (fileEntry.ValueKind == JsonValueKind.Object &&
                        fileEntry.TryGetProperty("FileName", out JsonElement fileNameElement) &&
                        fileNameElement.ValueKind == JsonValueKind.String &&
                        string.Equals(fileNameElement.GetString(), "Program.cs", StringComparison.OrdinalIgnoreCase))
                    {
                        fileEntry.WriteTo(writer);
                    }
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException ex)
        {
            logger?.LogError(ex,
                "Syncfusion Blazor Toolkit code-modification config file '{ConfigPath}' is not valid JSON.",
                codeModificationFilePath);
            return null;
        }
    }

    /// <summary>
    /// Appends <c>@using Syncfusion.Blazor.Toolkit</c> to the discovered
    /// <c>_Imports.razor</c> on disk. Idempotent. Does nothing when no imports
    /// file was discovered. This stays in the Toolkit scaffolder so razor edits
    /// do not depend on shared workspace lookup.
    /// </summary>
    public static void EnsureImportsUsing(string? projectPath, string? importsRelativePath)
    {
        if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(importsRelativePath))
        {
            return;
        }

        string? projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return;
        }

        string importsPath = Path.Combine(projectDirectory, importsRelativePath);
        if (!File.Exists(importsPath))
        {
            return;
        }

        string text = File.ReadAllText(importsPath);
        if (text.Contains("Syncfusion.Blazor.Toolkit", StringComparison.Ordinal))
        {
            return;
        }

        string separator = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        if (text.Length > 0 && !text.EndsWith('\n'))
        {
            text += separator;
        }

        File.WriteAllText(importsPath, text + UsingDirective + separator);
    }

    /// <summary>
    /// Writes <c>using Syncfusion.Blazor.Toolkit;</c> and
    /// <c>builder.Services.AddSyncfusionBlazorToolkit();</c> into Program.cs.
    /// Idempotent. Done in the Toolkit scaffolder so registration is not
    /// dropped when the shared code-modification workspace does not load
    /// Program.cs.
    /// </summary>
    public static void EnsureServiceRegistration(string? projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return;
        }

        string? projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return;
        }

        string programPath = Path.Combine(projectDirectory, "Program.cs");
        if (!File.Exists(programPath))
        {
            return;
        }

        string text = File.ReadAllText(programPath);
        string separator = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        if (!text.Contains("using Syncfusion.Blazor.Toolkit", StringComparison.Ordinal))
        {
            text = "using Syncfusion.Blazor.Toolkit;" + separator + text;
        }

        if (text.Contains("AddSyncfusionBlazorToolkit", StringComparison.Ordinal))
        {
            File.WriteAllText(programPath, text);
            return;
        }

        string[] anchors =
        [
            "WebApplication.CreateBuilder",
            "WebAssemblyHostBuilder.CreateDefault",
        ];

        foreach (string anchor in anchors)
        {
            int anchorIndex = text.IndexOf(anchor, StringComparison.Ordinal);
            if (anchorIndex < 0)
            {
                continue;
            }

            int lineEnd = text.IndexOf('\n', anchorIndex);
            if (lineEnd < 0)
            {
                text += separator + ServiceRegistration + separator;
            }
            else
            {
                text = text.Insert(lineEnd + 1, ServiceRegistration + separator);
            }

            File.WriteAllText(programPath, text);
            return;
        }
    }
}
