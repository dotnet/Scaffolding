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
///
/// <para>Service registration is performed exclusively by the shared
/// JSON/Roslyn code-modification pipeline (see
/// <c>syncfusionBlazorToolkitChanges.json</c> for net8–net11). This helper is
/// only used by the dedicated imports step and by the add-packages step that
/// keeps the package on the client up-to-date.</para>
/// </summary>
internal static class SyncfusionBlazorToolkitHelper
{
    internal const string UsingDirective = "@using Syncfusion.Blazor.Toolkit";

    /// <summary>
    /// Loads the Program.cs-only code-modification config. Imports are applied
    /// by a dedicated executable step (see
    /// <see cref="EnsureImportsUsing"/>) so the using is written whether
    /// <c>_Imports.razor</c> lives under <c>Components</c> or at the project
    /// root, and so that detection does not depend on the Roslyn workspace.
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
    /// Result of attempting to insert the
    /// <c>@using Syncfusion.Blazor.Toolkit</c> directive.
    /// </summary>
    public enum ImportsUpdateOutcome
    {
        /// <summary>
        /// The directive was already present; no write happened.
        /// </summary>
        AlreadyPresent = 0,

        /// <summary>
        /// The directive was appended to an existing file.
        /// </summary>
        Inserted = 1,

        /// <summary>
        /// A new file was created with the directive.
        /// </summary>
        Created = 2,

        /// <summary>
        /// No suitable _Imports.razor was found and none could be created.
        /// </summary>
        Skipped = 3,
    }

    /// <summary>
    /// Adds <c>@using Syncfusion.Blazor.Toolkit</c> to the discovered
    /// <c>_Imports.razor</c> on disk and reports the outcome.
    ///
    /// <para>Detection is reliable: we look for an exact, non-commented
    /// directive line, not for any substring. We preserve the file's
    /// existing line endings (CRLF or LF), trim a trailing newline
    /// before appending, and never duplicate the directive on rerun.</para>
    ///
    /// <para>The file is selected in priority order:</para>
    /// <list type="number">
    ///   <item><description><c>Components/_Imports.razor</c> — preferred for Blazor Web Apps.</description></item>
    ///   <item><description>Root-level <c>_Imports.razor</c> — classic standalone Blazor Server/WASM.</description></item>
    ///   <item><description>Any other <c>_Imports.razor</c> under the project (excluding bin / obj / node_modules / .git / sibling csproj folders).</description></item>
    /// </list>
    ///
    /// <para>If no candidate exists we try to create one: when a
    /// <c>Components</c> folder is already present we create
    /// <c>Components/_Imports.razor</c>; otherwise we create
    /// <c>_Imports.razor</c> at the project root.</para>
    /// </summary>
    public static (ImportsUpdateOutcome Outcome, string? ImportsFile) EnsureImportsUsing(string? projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return (ImportsUpdateOutcome.Skipped, null);
        }

        string? projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return (ImportsUpdateOutcome.Skipped, null);
        }

        string? importsPath = DiscoverImportsFile(projectDirectory);
        if (string.IsNullOrEmpty(importsPath))
        {
            // No _Imports.razor anywhere — create one in the
            // conventional location and treat as a fresh insert.
            string createdPath = TryCreateImportsFile(projectDirectory)
                ?? throw new InvalidOperationException(
                    "Syncfusion Blazor Toolkit could not find or create a suitable _Imports.razor file. " +
                    "Add one manually and re-run the scaffolder, or use a Blazor Web App / standalone " +
                    "Blazor WebAssembly template that includes Components/_Imports.razor.");

            WriteUsingDirective(createdPath, appendNewlineAtEnd: true);
            return (ImportsUpdateOutcome.Created, ToRelativePath(projectDirectory, createdPath));
        }

        string existing = File.ReadAllText(importsPath);
        if (DirectiveAlreadyPresent(existing))
        {
            return (ImportsUpdateOutcome.AlreadyPresent, ToRelativePath(projectDirectory, importsPath));
        }

        AppendUsingDirective(importsPath, existing);
        return (ImportsUpdateOutcome.Inserted, ToRelativePath(projectDirectory, importsPath));
    }

    /// <summary>
    /// True when the file already contains a real (non-commented)
    /// <c>@using Syncfusion.Blazor.Toolkit</c> directive. A substring
    /// match would mis-detect the directive appearing inside a Razor
    /// <c>@* … *@</c> comment block.
    /// </summary>
    private static bool DirectiveAlreadyPresent(string razorContents)
    {
        if (string.IsNullOrEmpty(razorContents))
        {
            return false;
        }

        foreach (string rawLine in razorContents.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            // Strip razor block comments (single-line) before matching.
            int blockCommentStart = line.IndexOf("@*", StringComparison.Ordinal);
            string searchable = blockCommentStart >= 0 ? line[..blockCommentStart] : line;

            // Strip end-of-line comments (@* … *@ on the same line).
            int blockCommentEnd = searchable.IndexOf("*@", StringComparison.Ordinal);
            if (blockCommentEnd >= 0)
            {
                searchable = searchable[..blockCommentEnd];
            }

            if (searchable.IndexOf(UsingDirective, StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Looks for an <c>_Imports.razor</c> file in the conventional Blazor
    /// locations. The first match wins. Returns the absolute path of the
    /// discovered file, or null when no candidate exists.
    /// </summary>
    public static string? DiscoverImportsFile(string projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        // Preferred order mirrors the conventional Blazor Web App layout,
        // then standalone Blazor WASM (no Components folder), then a
        // catch-all for any _Imports.razor under the project root.
        string[] orderedCandidates =
        [
            Path.Combine(projectDirectory, "Components", "_Imports.razor"),
            Path.Combine(projectDirectory, "_Imports.razor"),
        ];

        foreach (string candidate in orderedCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Catch-all: any _Imports.razor under the project. Enumerate
        // manually so we stay IFileSystem-agnostic and avoid recursing
        // into well-known excluded directories (bin, obj, node_modules).
        return FindFirstImportsRazor(projectDirectory, projectDirectory);
    }

    private static string? FindFirstImportsRazor(string projectDirectory, string currentDirectory)
    {
        if (!Directory.Exists(currentDirectory))
        {
            return null;
        }

        string directMatch = Path.Combine(currentDirectory, "_Imports.razor");
        if (File.Exists(directMatch))
        {
            return directMatch;
        }

        foreach (string subdirectory in Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(subdirectory);
            if (string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Don't recurse into nested project directories; their
            // _Imports.razor belongs to a different project.
            if (subdirectory != projectDirectory &&
                Directory.EnumerateFiles(subdirectory, "*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                continue;
            }

            string? nested = FindFirstImportsRazor(projectDirectory, subdirectory);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static string? TryCreateImportsFile(string projectDirectory)
    {
        // Prefer creating under an existing Components folder (Blazor Web App).
        string componentsPath = Path.Combine(projectDirectory, "Components");
        if (Directory.Exists(componentsPath))
        {
            string target = Path.Combine(componentsPath, "_Imports.razor");
            File.WriteAllText(target, string.Empty);
            return target;
        }

        // Otherwise create one at the project root.
        string rootTarget = Path.Combine(projectDirectory, "_Imports.razor");
        File.WriteAllText(rootTarget, string.Empty);
        return rootTarget;
    }

    private static void AppendUsingDirective(string importsPath, string existing)
    {
        string separator = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string text = existing;
        if (text.Length > 0 && !text.EndsWith('\n'))
        {
            text += separator;
        }

        File.WriteAllText(importsPath, text + UsingDirective + separator);
    }

    private static void WriteUsingDirective(string importsPath, bool appendNewlineAtEnd)
    {
        File.WriteAllText(importsPath, UsingDirective + (appendNewlineAtEnd ? Environment.NewLine : string.Empty));
    }

    private static string ToRelativePath(string projectDirectory, string absolutePath)
    {
        string relative = Path.GetRelativePath(projectDirectory, absolutePath);
        // Use OS-native separators so the relative path component can be
        // matched against MSBuildWorkspace's AdditionalDocument paths.
        return relative;
    }
}