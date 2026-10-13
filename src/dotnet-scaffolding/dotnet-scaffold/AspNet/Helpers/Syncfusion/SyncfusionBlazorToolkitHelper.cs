// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
/// JSON/Roslyn code-modification pipeline. This helper is
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

        // Syntax-aware: comments and string literals are never invocation
        // nodes, so they cannot be mistaken for a real registration. A
        // fluent chain such as
        // `builder.Services.AddRazorComponents().AddSyncfusionBlazorToolkit()`
        // has an invocation as its receiver and is rejected.
        SyntaxNode root = CSharpSyntaxTree.ParseText(programText).GetRoot();
        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax call &&
                call.Name.Identifier.ValueText == ServiceRegistrationCall &&
                call.Expression is MemberAccessExpressionSyntax services &&
                services.Name.Identifier.ValueText == "Services" &&
                services.Expression is IdentifierNameSyntax)
            {
                return true;
            }
        }

        return false;
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
        /// No safe entry point was found (no <c>Program.cs</c>, or no
        /// recognisable host-builder declaration in it), so nothing was
        /// written. The caller is expected to log this so the user can
        /// add the registration to their existing entry point. A new
        /// <c>Program.cs</c> is deliberately never generated: it would
        /// compete with the real entry point (CS8802).
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
    /// requires an existing entry point (it never creates one). It also works when <c>Program.cs</c> lives
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
        if (string.IsNullOrEmpty(programPath))
        {
            return (ServiceRegistrationOutcome.Skipped, null);
        }

        string text = File.ReadAllText(programPath);
        string relativePath = ToRelativePath(projectDirectory, programPath);
        if (IsActiveServiceRegistrationPresent(text))
        {
            return (ServiceRegistrationOutcome.AlreadyPresent, relativePath);
        }

        string builderName = DetectBuilderVariableName(text, hostKind) ?? DefaultBuilderVariableName;
        if (!TryAppendServiceRegistration(programPath, text, builderName))
        {
            // No host-builder declaration to anchor on. Appending at the
            // end of the file would land after Build()/Run() and break DI.
            return (ServiceRegistrationOutcome.Skipped, relativePath);
        }

        return (ServiceRegistrationOutcome.Inserted, relativePath);
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

        string marker = hostKind == ProgramHostKind.StandaloneWasm
            ? WasmBuilderMarker
            : WebBuilderMarker;

        SyntaxNode root = CSharpSyntaxTree.ParseText(programText).GetRoot();
        return FindHostBuilderDeclaration(root, [marker])?.Declarator.Identifier.ValueText;
    }

    private const string WebBuilderMarker = "WebApplication.CreateBuilder";
    private const string WasmBuilderMarker = "WebAssemblyHostBuilder.CreateDefault";

    /// <summary>
    /// Finds the first local declaration whose initializer calls one of
    /// <paramref name="markers"/> (e.g. <c>WebApplication.CreateBuilder</c>).
    /// Working on the syntax tree means the declaration may span several
    /// lines, and comments / string literals are never matched.
    /// </summary>
    private static (LocalDeclarationStatementSyntax Statement, VariableDeclaratorSyntax Declarator)? FindHostBuilderDeclaration(
        SyntaxNode root,
        string[] markers)
    {
        foreach (LocalDeclarationStatementSyntax statement in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            foreach (VariableDeclaratorSyntax declarator in statement.Declaration.Variables)
            {
                ExpressionSyntax? initializer = declarator.Initializer?.Value;
                if (initializer is null)
                {
                    continue;
                }

                foreach (InvocationExpressionSyntax invocation in initializer.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                {
                    if (markers.Contains(invocation.Expression.ToString().Replace(" ", string.Empty), StringComparer.Ordinal))
                    {
                        return (statement, declarator);
                    }
                }
            }
        }

        return null;
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

    private static bool TryAppendServiceRegistration(string programPath, string existingText, string builderName)
    {
        string registrationLine = $"{builderName}.Services.{ServiceRegistrationCall}();";

        // Insert the call immediately after the host-builder statement so
        // the services are wired up before `Build()` consumes them.
        string? withRegistration = InsertAfterBuilderLine(existingText, registrationLine);
        if (withRegistration is null)
        {
            return false;
        }

        File.WriteAllText(programPath, EnsureUsingDirective(withRegistration, ToolKitUsingDirective));
        return true;
    }

    /// <summary>
    /// Inserts <paramref name="registrationLine"/> on the line after the
    /// complete host-builder declaration statement (the one calling
    /// <c>WebApplication.CreateBuilder</c> or
    /// <c>WebAssemblyHostBuilder.CreateDefault</c>), however many lines
    /// that statement spans, preserving the file's line ending.
    ///
    /// <para>Returns <c>null</c> when no such declaration exists; the
    /// caller must then leave the file untouched rather than guess a
    /// location (appending after <c>Build()</c> would register too late).</para>
    /// </summary>
    private static string? InsertAfterBuilderLine(string source, string registrationLine)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(registrationLine))
        {
            return null;
        }

        SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var found = FindHostBuilderDeclaration(root, [WebBuilderMarker, WasmBuilderMarker]);
        if (found is null)
        {
            return null;
        }

        string newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        // FullSpan.End includes trailing trivia (comment + end-of-line).
        int insertAt = found.Value.Statement.FullSpan.End;
        string before = source[..insertAt];
        string after = source[insertAt..];
        string prefix = before.EndsWith('\n') ? string.Empty : newline;
        return before + prefix + registrationLine + newline + after;
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

        // Use the same line ending as the rest of the file so we do
        // not introduce a mixed-ending section. CRLF on Windows, LF
        // on Linux/macOS, falling back to the platform newline when
        // the file uses neither.
        string newline = source.Contains("\r\n", StringComparison.Ordinal)
            ? "\r\n"
            : "\n";

        StringBuilder sb = new(source.Length + usingDirective.Length + 4);
        sb.Append(usingDirective);
        sb.Append(newline);
        // If the existing file already starts with a newline, do not
        // double it.
        if (!StartsWithNewline(source))
        {
            sb.Append(newline);
        }

        sb.Append(source);
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

}