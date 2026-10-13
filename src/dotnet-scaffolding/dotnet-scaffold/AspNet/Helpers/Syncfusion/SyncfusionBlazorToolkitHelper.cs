// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;

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
    /// <c>@using Syncfusion.Blazor.Toolkit</c> directive.
    /// <para>The check is exact: a sub-namespace using such as
    /// <c>@using Syncfusion.Blazor.Toolkit.Buttons</c> does <b>not</b>
    /// count, and content inside Razor <c>@* … *@</c> comment blocks
    /// (including multiline ones) is ignored. Whitespace and end-of-line
    /// comments are tolerated.</para>
    /// </summary>
    internal static bool DirectiveAlreadyPresent(string razorContents)
    {
        if (string.IsNullOrEmpty(razorContents))
        {
            return false;
        }

        // Strip every multiline Razor comment block first so a comment
        // spanning many lines cannot leak `@using Syncfusion.Blazor.Toolkit`
        // out of the directive scan below.
        string codeOnly = StripMultilineRazorComments(razorContents);

        foreach (string rawLine in codeOnly.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');

            // Drop any single-line @* ... *@ tail on the same line.
            int inlineStart = line.IndexOf("@*", StringComparison.Ordinal);
            if (inlineStart >= 0)
            {
                line = line[..inlineStart];
            }

            if (HasExactToolkitUsing(line))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes every <c>@* … *@</c> Razor block-comment region from
    /// the input. Handles both single-line and multiline forms and
    /// preserves line breaks so line numbers are not lost on the
    /// remaining content.
    /// </summary>
    private static string StripMultilineRazorComments(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        var builder = new StringBuilder(source.Length);
        int i = 0;
        while (i < source.Length)
        {
            if (i + 1 < source.Length && source[i] == '@' && source[i + 1] == '*')
            {
                // Skip to the matching *@. Replace each non-newline
                // character inside the comment with a space so column
                // counts in the remaining text are preserved.
                i += 2;
                while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '@'))
                {
                    if (source[i] != '\n')
                    {
                        builder.Append(' ');
                    }
                    else
                    {
                        builder.Append('\n');
                    }
                    i++;
                }

                if (i + 1 < source.Length)
                {
                    // Skip the closing *@.
                    i += 2;
                }
                else
                {
                    // Unterminated comment; consume the rest.
                    break;
                }
            }
            else
            {
                builder.Append(source[i]);
                i++;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// True when <paramref name="line"/> contains the exact directive
    /// <c>@using Syncfusion.Blazor.Toolkit</c>. The directive must be
    /// the whole using clause: any non-whitespace, non-comment
    /// character immediately after the namespace disqualifies the
    /// match (e.g. <c>@using Syncfusion.Blazor.Toolkit.Buttons</c>).
    /// </summary>
    private static bool HasExactToolkitUsing(string line)
    {
        const string Namespace = "Syncfusion.Blazor.Toolkit";
        int idx = line.IndexOf(Namespace, StringComparison.Ordinal);
        if (idx < 0)
        {
            return false;
        }

        // The character preceding the namespace must be '@' or whitespace
        // (i.e. we are inside a using directive, not the middle of an
        // identifier or a string literal).
        int atIdx = line.LastIndexOf('@', idx);
        if (atIdx < 0)
        {
            return false;
        }

        // Everything between '@' and the namespace must be "using"
        // surrounded by whitespace.
        string between = line.Substring(atIdx + 1, idx - atIdx - 1).Trim();
        if (!string.Equals(between, "using", StringComparison.Ordinal))
        {
            return false;
        }

        // Reject when the namespace is followed by another identifier
        // character — that means a sub-namespace using directive
        // (e.g. '.Buttons') or some chained token, not our exact
        // directive.
        int after = idx + Namespace.Length;
        if (after < line.Length)
        {
            char c = line[after];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Looks for an <c>_Imports.razor</c> file in the conventional Blazor
    /// locations. Conventional Blazor Web App / Blazor Server templates
    /// place the file under <c>Components/_Imports.razor</c>; standalone
    /// Blazor WebAssembly / Blazor Server templates place it at the
    /// project root. Those two are the only scopes that cover every
    /// component in the app — picking a narrower nested file
    /// (e.g. <c>Pages/_Imports.razor</c>) would leave components under
    /// <c>Layout</c>, <c>Shared</c>, or sibling folders without the
    /// added <c>@using</c> directive, so we deliberately do not fall
    /// back to arbitrary descendants here.
    /// </summary>
    /// <returns>
    /// The absolute path of the discovered file, or <c>null</c> when
    /// no conventional <c>_Imports.razor</c> exists. Callers can then
    /// create one at the conventional location.
    /// </returns>
    public static string? DiscoverImportsFile(string projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        // Preferred order mirrors the conventional Blazor Web App layout,
        // then standalone Blazor WASM (no Components folder).
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

        // Deliberately do NOT fall back to an arbitrary nested
        // _Imports.razor (e.g. Pages/_Imports.razor). Such a file
        // does not cover the whole app; the caller should create
        // an app-wide _Imports.razor at the conventional location
        // instead.
        return null;
    }

    /// <summary>
    /// Returns the conventional shared <c>_Imports.razor</c> path for
    /// a project. Used by callers that need the file path even when it
    /// does not exist yet, so the file can be created at the right
    /// place.
    /// </summary>
    public static string GetConventionalImportsFilePath(string projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return string.Empty;
        }

        string componentsPath = Path.Combine(projectDirectory, "Components", "_Imports.razor");
        if (Directory.Exists(Path.GetDirectoryName(componentsPath)))
        {
            return componentsPath;
        }

        return Path.Combine(projectDirectory, "_Imports.razor");
    }

    /// <summary>
    /// True when a discovered <c>_Imports.razor</c> path lives at the
    /// conventional shared scope (project root or <c>Components</c>).
    /// Callers use this to decide whether to log a warning when a
    /// narrower nested file was found.
    /// </summary>
    public static bool IsAtConventionalScope(string projectDirectory, string importsPath)
    {
        if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(importsPath))
        {
            return false;
        }

        string normalized = Path.GetFullPath(importsPath);
        string rootScope = Path.GetFullPath(Path.Combine(projectDirectory, "_Imports.razor"));
        string componentsScope = Path.GetFullPath(Path.Combine(projectDirectory, "Components", "_Imports.razor"));
        return string.Equals(normalized, rootScope, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, componentsScope, StringComparison.OrdinalIgnoreCase);
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

    /// <summary>
    /// True when <paramref name="programText"/> contains an active
    /// (non-commented) call to <c>AddSyncfusionBlazorToolkit()</c>
    /// on a <c>*.Services</c> receiver. The check rejects:
    /// <list type="bullet">
    ///   <item><description>substrings inside <c>//</c> or <c>/* */</c> comments,</description></item>
    ///   <item><description>chained calls (e.g. <c>AddRazorComponents().AddSyncfusionBlazorToolkit()</c>),</description></item>
    ///   <item><description>substring occurrences inside larger identifiers.</description></item>
    /// </list>
    /// </summary>
    public static bool IsActiveServiceRegistrationPresent(string? programText)
    {
        if (string.IsNullOrEmpty(programText))
        {
            return false;
        }

        string codeOnly = StripCSharpComments(programText);
        foreach (string rawLine in codeOnly.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
            {
                continue;
            }

            // The standalone registration we are looking for has the
            // shape "*.Services.AddSyncfusionBlazorToolkit();" or
            // "*.Services.AddSyncfusionBlazorToolkit()".
            // A chained registration has the form "...AddSyncfusionBlazorToolkit()"
            // preceded by a member access without a trailing semicolon on
            // its own line.
            if (IsStandaloneServiceRegistration(line))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStandaloneServiceRegistration(string trimmedLine)
    {
        if (string.IsNullOrEmpty(trimmedLine))
        {
            return false;
        }

        // The standalone registration we want has the shape
        //   <receiver>.AddSyncfusionBlazorToolkit();
        // where <receiver> is a simple dotted name (e.g.
        // `builder.Services` or `wasmBuilder.Services`). A chained
        // registration has the form
        //   <receiver>...methodCall(<args>).AddSyncfusionBlazorToolkit();
        // — distinguished by the `)` (or `(`) that immediately
        // precedes the final `.`.
        const string Call = "AddSyncfusionBlazorToolkit";
        int callIdx = trimmedLine.IndexOf(Call, StringComparison.Ordinal);
        if (callIdx < 0)
        {
            return false;
        }

        // Must end with a semicolon — most single-statement C#
        // calls do, and chains that terminate this call also do, so
        // this is necessary but not sufficient.
        if (!trimmedLine.EndsWith(';'))
        {
            return false;
        }

        // Must be preceded by a `.` member access and not be a
        // substring of a larger identifier.
        int dotIdx = callIdx - 1;
        if (dotIdx < 0 || trimmedLine[dotIdx] != '.')
        {
            return false;
        }

        if (dotIdx == 0)
        {
            return false;
        }

        // The character right before the `.` tells us whether the
        // call is standalone or the last link in a chain:
        //   standalone : ...Services.   → letter / digit
        //   chained    : ...Args().     → ')'
        // Reject any character that is itself the closing paren of
        // an enclosing call (or its argument list) — that is the
        // signature of a fluent chain.
        char prev = trimmedLine[dotIdx - 1];
        if (prev == ')' || prev == '(')
        {
            return false;
        }

        // Also reject whitespace — the receiver must be glued to
        // the `.` (i.e. a member access, not a separate statement).
        if (char.IsWhiteSpace(prev))
        {
            return false;
        }

        // The receiver before the first `.` must end with `Services`
        // (we accept any name, but reject bare `AddSyncfusionBlazorToolkit`).
        int firstDot = trimmedLine.IndexOf('.');
        if (firstDot <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes <c>// … \n</c> and <c>/* … */</c> C# comment regions
    /// from <paramref name="source"/>, preserving newlines so the
    /// remaining line structure is stable. Used by
    /// <see cref="IsActiveServiceRegistrationPresent"/> to make sure
    /// commented-out registrations are not detected.
    /// </summary>
    internal static string StripCSharpCommentsForTest(string source) => StripCSharpComments(source);

    private static string StripCSharpComments(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        var builder = new StringBuilder(source.Length);
        int i = 0;
        while (i < source.Length)
        {
            char c = source[i];
            char next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (c == '/' && next == '/')
            {
                // Line comment — copy the newline at the end, drop the
                // rest.
                i += 2;
                while (i < source.Length && source[i] != '\n')
                {
                    builder.Append(' ');
                    i++;
                }
            }
            else if (c == '/' && next == '*')
            {
                // Block comment — preserve newlines, drop everything
                // else.
                i += 2;
                while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/'))
                {
                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                if (i + 1 < source.Length)
                {
                    // Drop the closing */.
                    i += 2;
                }
                else
                {
                    break;
                }
            }
            else if (c == '"' || c == '\'')
            {
                // String/char literal — copy verbatim until the next
                // unescaped terminator.
                char quote = c;
                builder.Append(c);
                i++;
                while (i < source.Length)
                {
                    char sc = source[i];
                    builder.Append(sc);
                    if (sc == '\\' && i + 1 < source.Length)
                    {
                        builder.Append(source[i + 1]);
                        i += 2;
                        continue;
                    }

                    i++;
                    if (sc == quote)
                    {
                        break;
                    }
                }
            }
            else
            {
                builder.Append(c);
                i++;
            }
        }

        return builder.ToString();
    }

    // ---------------------------------------------------------------------
    // Program.cs service registration helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Outcome of <see cref="EnsureServiceRegistration"/>. The values
    /// mirror the categories reported in the scaffolder summary.
    /// </summary>
    public enum ServiceRegistrationOutcome
    {
        /// <summary>
        /// A real, uncommented <c>AddSyncfusionBlazorToolkit()</c> call was
        /// already present in the project's <c>Program.cs</c>. No write
        /// happened.
        /// </summary>
        AlreadyPresent = 0,

        /// <summary>
        /// The call was appended to an existing <c>Program.cs</c>.
        /// </summary>
        Inserted = 1,

        /// <summary>
        /// No <c>Program.cs</c> existed; a minimal one was created with
        /// the call in place.
        /// </summary>
        Created = 2,

        /// <summary>
        /// The step could not find or create a <c>Program.cs</c> and the
        /// service registration was skipped. The caller is expected to
        /// log this so the user can act on it.
        /// </summary>
        Skipped = 3,
    }

    /// <summary>
    /// Identifies the Blazor hosting model the <c>Program.cs</c> targets.
    /// Drives the choice of minimal scaffold used when a <c>Program.cs</c>
    /// has to be created from scratch and the builder variable name to
    /// register the service on.
    /// </summary>
    public enum ProgramHostKind
    {
        /// <summary>
        /// Blazor Web App / Server — uses <c>WebApplication.CreateBuilder</c>.
        /// </summary>
        WebApp = 0,

        /// <summary>
        /// Standalone Blazor WebAssembly — uses
        /// <c>WebAssemblyHostBuilder.CreateDefault</c>.
        /// </summary>
        StandaloneWasm = 1,
    }

    /// <summary>
    /// The conventional file name expected for the application entry point.
    /// </summary>
    public const string ProgramCsFileName = "Program.cs";

    /// <summary>
    /// The set of file names that are treated as the application entry
    /// point by <see cref="DiscoverProgramCs"/>. <c>Program.cs</c> is the
    /// .NET 6+ default; the remaining entries are accepted as alternates
    /// for older / non-standard layouts so the scaffolder still works
    /// when the file lives at a custom name.
    /// </summary>
    public static readonly string[] ProgramCsFileNameCandidates =
    [
        "Program.cs",
        "Program.vb",
        "Startup.cs",
    ];

    private const string ServiceRegistrationCall = "AddSyncfusionBlazorToolkit";
    private const string DefaultBuilderVariableName = "builder";
    private const string ToolKitUsingDirective = "using Syncfusion.Blazor.Toolkit;";

    /// <summary>
    /// Idempotently writes
    /// <c>builder.Services.AddSyncfusionBlazorToolkit();</c> (and the
    /// matching <c>using</c> directive) into the project's
    /// <c>Program.cs</c>. The function works against a Blazor Web App /
    /// Server host and against a standalone Blazor WebAssembly host, and
    /// works whether <c>Program.cs</c> already exists or has to be
    /// created from scratch. It also works when <c>Program.cs</c> lives
    /// in a subdirectory of the project or has a non-default name
    /// (<c>Startup.cs</c>, <c>Program.vb</c>).
    /// </summary>
    /// <param name="projectPath">
    /// Absolute path to the project's <c>.csproj</c> / <c>.vbproj</c>.
    /// Used to locate the project directory and to default the
    /// <c>Program.cs</c> location.
    /// </param>
    /// <param name="hostKind">
    /// Hosting model — controls the minimal <c>Program.cs</c> template
    /// when a file has to be created and the builder variable name to
    /// look for when one already exists.
    /// </param>
    /// <returns>
    /// A tuple of (outcome, project-relative path of the file the
    /// scaffolder targeted, or <c>null</c> when the file could not be
    /// found or created).
    /// </returns>
    public static (ServiceRegistrationOutcome Outcome, string? ProgramCsRelative) EnsureServiceRegistration(
        string? projectPath,
        ProgramHostKind hostKind)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return (ServiceRegistrationOutcome.Skipped, null);
        }

        string? projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return (ServiceRegistrationOutcome.Skipped, null);
        }

        string? programPath = DiscoverProgramCs(projectDirectory);
        bool created = false;
        if (string.IsNullOrEmpty(programPath))
        {
            programPath = TryCreateMinimalProgramCs(projectDirectory, hostKind);
            if (string.IsNullOrEmpty(programPath))
            {
                return (ServiceRegistrationOutcome.Skipped, null);
            }

            created = true;
        }

        string text = File.ReadAllText(programPath);
        if (IsActiveServiceRegistrationPresent(text))
        {
            if (created)
            {
                // The minimal template we just wrote already contains
                // a working AddSyncfusionBlazorToolkit() call. Reporting
                // "Created" instead of "AlreadyPresent" makes the
                // distinction between a fresh file and a re-run clearer
                // for the summary step.
                return (
                    ServiceRegistrationOutcome.Created,
                    ToRelativePath(projectDirectory, programPath));
            }

            return (
                ServiceRegistrationOutcome.AlreadyPresent,
                ToRelativePath(projectDirectory, programPath));
        }

        if (created)
        {
            // Defensive: the minimal template should already include
            // the call. If it does not, fall through to the regular
            // insertion path so we never leave the project without a
            // registration.
        }

        string builderName = DetectBuilderVariableName(text, hostKind) ?? DefaultBuilderVariableName;
        AppendServiceRegistration(programPath, text, builderName);
        return (
            created ? ServiceRegistrationOutcome.Created : ServiceRegistrationOutcome.Inserted,
            ToRelativePath(projectDirectory, programPath));
    }

    /// <summary>
    /// Locates the application entry-point file inside
    /// <paramref name="projectDirectory"/>. The root <c>Program.cs</c>
    /// is preferred; one-level subdirectories are searched as fallbacks.
    /// <c>bin</c> / <c>obj</c> / <c>node_modules</c> / <c>.git</c> and any
    /// other directory that hosts a <c>.csproj</c> are excluded so
    /// sibling projects never claim the entry point of the project the
    /// scaffolder was invoked against.
    /// </summary>
    public static string? DiscoverProgramCs(string projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        // 1. Root-level entry point — the conventional .NET 6+ layout.
        foreach (string candidate in ProgramCsFileNameCandidates)
        {
            string root = Path.Combine(projectDirectory, candidate);
            if (File.Exists(root))
            {
                return root;
            }
        }

        // 2. One-level subdirectory fallback. We walk the immediate
        //    children (excluding build/output/VCS folders and any
        //    directory that itself contains a .csproj — that would be
        //    a sibling project, not the current project's entry point).
        IEnumerable<string> subdirs;
        try
        {
            subdirs = Directory.EnumerateDirectories(projectDirectory);
        }
        catch
        {
            return null;
        }

        foreach (string subdir in subdirs)
        {
            string name = Path.GetFileName(subdir);
            if (IsExcludedDirectory(name))
            {
                continue;
            }

            if (DirectoryContainsProjectFile(subdir))
            {
                continue;
            }

            foreach (string candidate in ProgramCsFileNameCandidates)
            {
                string candidatePath = Path.Combine(subdir, candidate);
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a minimal <c>Program.cs</c> in
    /// <paramref name="projectDirectory"/> matching
    /// <paramref name="hostKind"/>, and returns its absolute path. The
    /// minimal template already contains a working
    /// <c>AddSyncfusionBlazorToolkit()</c> registration so callers do
    /// not need to perform a separate insertion pass.
    /// </summary>
    /// <returns>
    /// The path of the freshly-created <c>Program.cs</c>, or
    /// <c>null</c> if the file could not be created.
    /// </returns>
    public static string? TryCreateMinimalProgramCs(string projectDirectory, ProgramHostKind hostKind)
    {
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        string programPath = Path.Combine(projectDirectory, ProgramCsFileName);
        if (File.Exists(programPath))
        {
            // A file already exists but the discovery pass missed it
            // (for example, because it lives in a deeper subdirectory
            // than we searched). Return the existing file unchanged so
            // the caller can perform the regular insertion path.
            return programPath;
        }

        string builderName = hostKind == ProgramHostKind.StandaloneWasm
            ? "wasmBuilder"
            : DefaultBuilderVariableName;

        string template = hostKind == ProgramHostKind.StandaloneWasm
            ? BuildStandaloneWasmMinimalProgramCs(builderName)
            : BuildWebAppMinimalProgramCs(builderName);

        try
        {
            File.WriteAllText(programPath, template);
            return programPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Scans the contents of a <c>Program.cs</c> file for the variable
    /// name the host builder was assigned to. Supports both the
    /// Blazor Web App / Server pattern
    /// (<c>var builder = WebApplication.CreateBuilder(...)</c>) and the
    /// standalone Blazor WebAssembly pattern
    /// (<c>var builder = WebAssemblyHostBuilder.CreateDefault(...)</c>).
    /// Returns <c>null</c> when no host builder is detected, in which
    /// case callers should fall back to the conventional
    /// <c>builder</c> name.
    /// </summary>
    public static string? DetectBuilderVariableName(string? programText, ProgramHostKind hostKind)
    {
        if (string.IsNullOrEmpty(programText))
        {
            return null;
        }

        string codeOnly = StripCSharpComments(programText);
        string marker = hostKind == ProgramHostKind.StandaloneWasm
            ? "WebAssemblyHostBuilder.CreateDefault"
            : "WebApplication.CreateBuilder";

        foreach (string rawLine in codeOnly.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0)
            {
                continue;
            }

            // Recognise the assignment shape:
            //   var NAME = ...Marker...(...);
            //   WebApplicationBuilder NAME = ...Marker...(...);
            //   NAME NAME = ...Marker...(...);
            int markerIdx = line.IndexOf(marker, StringComparison.Ordinal);
            if (markerIdx < 0)
            {
                continue;
            }

            int equalsIdx = line.IndexOf('=');
            if (equalsIdx <= 0)
            {
                continue;
            }

            string leftHandSide = line[..equalsIdx].Trim();
            if (leftHandSide.Length == 0)
            {
                continue;
            }

            // Pull the last identifier on the LHS — this is the
            // declared variable name, regardless of whether the
            // declaration is `var x = ...` or
            // `WebApplicationBuilder x = ...`.
            int lastSpace = leftHandSide.LastIndexOf(' ');
            if (lastSpace < 0 || lastSpace == leftHandSide.Length - 1)
            {
                continue;
            }

            string candidate = leftHandSide[(lastSpace + 1)..];
            if (IsValidIdentifier(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (!char.IsLetter(value[0]) && value[0] != '_')
        {
            return false;
        }

        for (int i = 1; i < value.Length; i++)
        {
            char c = value[i];
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsExcludedDirectory(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }

        return name.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
            || name.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || name.Equals(".vs", StringComparison.OrdinalIgnoreCase)
            || name.Equals(".idea", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Properties", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DirectoryContainsProjectFile(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory, "*.csproj").Any()
                || Directory.EnumerateFiles(directory, "*.vbproj").Any()
                || Directory.EnumerateFiles(directory, "*.fsproj").Any();
        }
        catch
        {
            return false;
        }
    }

    private static string BuildWebAppMinimalProgramCs(string builderName)
    {
        return
            $"var {builderName} = WebApplication.CreateBuilder(args);\n" +
            $"{builderName}.Services.AddSyncfusionBlazorToolkit();\n" +
            $"var app = {builderName}.Build();\n" +
            $"app.Run();\n";
    }

    private static string BuildStandaloneWasmMinimalProgramCs(string builderName)
    {
        return
            $"var {builderName} = WebAssemblyHostBuilder.CreateDefault(args);\n" +
            $"{builderName}.Services.AddSyncfusionBlazorToolkit();\n" +
            $"await {builderName}.Build().RunAsync();\n";
    }

    private static void AppendServiceRegistration(string programPath, string existingText, string builderName)
    {
        string registrationLine = $"{builderName}.Services.{ServiceRegistrationCall}();";

        // Build the new file content: ensure the using directive is
        // present, preserve the existing body, and append the service
        // registration call at the bottom on its own line.
        string withUsing = EnsureUsingDirective(existingText, ToolKitUsingDirective);
        string withRegistration = AppendLine(withUsing, registrationLine);

        File.WriteAllText(programPath, withRegistration);
    }

    /// <summary>
    /// Returns <paramref name="source"/> with
    /// <paramref name="usingDirective"/> prepended as the first line of
    /// the file when it is not already present (and not commented out).
    /// When the directive is already present the original source is
    /// returned unchanged.
    /// </summary>
    private static string EnsureUsingDirective(string source, string usingDirective)
    {
        if (string.IsNullOrEmpty(source))
        {
            // Empty file — write the using directive on its own.
            return usingDirective + Environment.NewLine;
        }

        if (string.IsNullOrEmpty(usingDirective))
        {
            return source;
        }

        // Use the comment-stripped view so a commented-out using does
        // not count as already-present.
        string codeOnly = StripCSharpComments(source);
        foreach (string rawLine in codeOnly.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').Trim();
            if (string.Equals(line, usingDirective, StringComparison.Ordinal))
            {
                return source;
            }
        }

        StringBuilder sb = new(source.Length + usingDirective.Length + 4);
        sb.Append(usingDirective);
        sb.Append(Environment.NewLine);
        // If the existing file already starts with a newline, do not
        // double it.
        if (!StartsWithNewline(source))
        {
            sb.Append(Environment.NewLine);
        }

        sb.Append(source);
        return sb.ToString();
    }

    private static string AppendLine(string source, string line)
    {
        StringBuilder sb = new(source.Length + line.Length + 2);
        sb.Append(source);
        // Guarantee exactly one separator between the existing body
        // and the appended line.
        if (source.Length == 0)
        {
            sb.Append(line);
        }
        else
        {
            if (!EndsWithNewline(source))
            {
                sb.Append(Environment.NewLine);
            }

            sb.Append(line);
            sb.Append(Environment.NewLine);
        }

        return sb.ToString();
    }

    private static bool StartsWithNewline(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        return source[0] == '\n' || source[0] == '\r';
    }

    private static bool EndsWithNewline(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        char last = source[source.Length - 1];
        return last == '\n' || last == '\r';
    }
}