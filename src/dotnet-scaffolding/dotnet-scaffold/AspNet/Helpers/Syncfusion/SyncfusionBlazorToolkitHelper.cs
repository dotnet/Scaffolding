// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;

/// <summary>
/// Helpers for the Syncfusion Blazor Toolkit setup scaffolder.
///
/// <para>Setup is limited to the NuGet package, <c>AddSyncfusionBlazorToolkit()</c>
/// in the application entry point, and a single <c>@using Syncfusion.Blazor.Toolkit</c> in
/// the project's <c>_Imports.razor</c>. Syncfusion.Blazor.Toolkit 2.0.0+ ships
/// styles with the assembly, so no host stylesheet link is added.</para>
///
/// <para>The dedicated scaffold steps call into this class to read the
/// project's evaluated state (compiled sources, package references), to detect an
/// existing registration or import, and to write the registration and import.</para>
/// </summary>
internal static class SyncfusionBlazorToolkitHelper
{
    internal const string UsingDirective = "@using Syncfusion.Blazor.Toolkit";

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

        string existing = ReadTextPreservingEncoding(importsPath, out Encoding encoding);
        if (DirectiveAlreadyPresent(existing))
        {
            return (ImportsUpdateOutcome.AlreadyPresent, ToRelativePath(projectDirectory, importsPath));
        }

        AppendUsingDirective(importsPath, existing, encoding);
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

    /// <summary>
    /// Reads a text file and reports its encoding. <c>File.ReadAllText</c> silently drops a
    /// UTF-8 byte-order mark, so writing the text back with <c>File.WriteAllText</c> would strip a
    /// BOM the user's file had. Callers write back with the returned encoding instead.
    /// </summary>
    private static string ReadTextPreservingEncoding(string path, out Encoding encoding)
    {
        using var reader = new StreamReader(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true);
        string text = reader.ReadToEnd();
        // `CurrentEncoding` reflects a detected BOM only after the first read.
        encoding = reader.CurrentEncoding is UTF8Encoding
            ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: HasUtf8Bom(path))
            : reader.CurrentEncoding;
        return text;
    }

    private static bool HasUtf8Bom(string path)
    {
        Span<byte> head = stackalloc byte[3];
        using FileStream stream = File.OpenRead(path);
        return stream.Read(head) == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
    }

    private static void AppendUsingDirective(string importsPath, string existing, Encoding encoding)
    {
        string separator = existing.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string text = existing;
        if (text.Length > 0 && !text.EndsWith('\n'))
        {
            text += separator;
        }

        File.WriteAllText(importsPath, text + UsingDirective + separator, encoding);
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
    /// Outcome of inspecting an entry point for the Toolkit registration.
    /// </summary>
    private enum RegistrationState
    {
        /// <summary>Every host builder is registered before its <c>Build()</c>.</summary>
        Registered = 0,

        /// <summary>A host builder is positively identified and a confirmed
        /// <c>Build()</c> boundary exists, but the registration is missing,
        /// late, conditional or malformed.</summary>
        NotRegistered = 1,

        /// <summary>The framework host builder could not be identified, or
        /// its <c>Build()</c> boundary could not be established. Nothing can
        /// be concluded, so callers must neither report success nor
        /// modify the file.</summary>
        Unverifiable = 2,
    }

    /// <summary>
    /// True only when <paramref name="programText"/> conclusively registers
    /// the Toolkit on the application's startup path. A registration counts
    /// only when it is:
    /// <list type="bullet">
    ///   <item><description>a real, parameterless
    ///   <c>&lt;builder&gt;.Services.AddSyncfusionBlazorToolkit()</c>
    ///   statement (not a comment, string literal, fluent chain, or call
    ///   with arguments),</description></item>
    ///   <item><description>a direct statement of the startup scope -
    ///   top-level statements or the body of a static <c>Main</c> - so it
    ///   always executes,</description></item>
    ///   <item><description>made on a variable that is positively resolved
    ///   (through using aliases and qualified names) to the framework's
    ///   <c>Microsoft.AspNetCore.Builder.WebApplication</c> or
    ///   <c>WebAssemblyHostBuilder</c> creation call, after its
    ///   declaration, and</description></item>
    ///   <item><description>before a confirmed statement that calls
    ///   <c>Build()</c> on that variable.</description></item>
    /// </list>
    /// Fails closed: when no host builder can be identified, or no
    /// <c>Build()</c> boundary exists, the result is <c>false</c>.
    /// </summary>
    public static bool IsActiveServiceRegistrationPresent(string? programText) =>
        EvaluateRegistration(programText) == RegistrationState.Registered;

    private static RegistrationState EvaluateRegistration(string? programText)
    {
        if (string.IsNullOrEmpty(programText))
        {
            return RegistrationState.Unverifiable;
        }

        SyntaxNode root = CSharpSyntaxTree.ParseText(programText).GetRoot();
        List<HostBuilderDeclaration> builders = FindHostBuilderDeclarations(root, ProgramHostKind.WebApp, ProgramHostKind.StandaloneWasm);
        if (builders.Count == 0)
        {
            return RegistrationState.Unverifiable;
        }

        var states = builders.Select(EvaluateBuilder).ToList();
        if (states.Contains(RegistrationState.Unverifiable))
        {
            return RegistrationState.Unverifiable;
        }

        return states.All(state => state == RegistrationState.Registered)
            ? RegistrationState.Registered
            : RegistrationState.NotRegistered;
    }

    private static RegistrationState EvaluateBuilder(HostBuilderDeclaration builder)
    {
        int declarationIndex = builder.Scope.IndexOf(builder.Statement);
        int buildIndex = builder.Scope.FindIndex(declarationIndex + 1, statement => CallsBuildOn(statement, builder.Name));
        if (buildIndex < 0)
        {
            // No host-construction boundary: an incomplete or unsupported
            // bootstrap. Do not claim the registration is effective.
            return RegistrationState.Unverifiable;
        }

        for (int i = declarationIndex + 1; i < buildIndex; i++)
        {
            if (string.Equals(GetRegistrationReceiver(builder.Scope[i]), builder.Name, StringComparison.Ordinal))
            {
                return RegistrationState.Registered;
            }
        }

        return RegistrationState.NotRegistered;
    }

    private static bool CallsBuildOn(StatementSyntax statement, string builderName) =>
        statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax access &&
            access.Name.Identifier.ValueText == "Build" &&
            access.Expression is IdentifierNameSyntax receiver &&
            receiver.Identifier.ValueText == builderName);

    /// <summary>
    /// For a statement of the exact form
    /// <c>x.Services.AddSyncfusionBlazorToolkit();</c> (no arguments, not
    /// chained) returns the receiver <c>x</c>; otherwise <c>null</c>.
    /// </summary>
    private static string? GetRegistrationReceiver(StatementSyntax statement)
    {
        if (statement is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation } &&
            invocation.ArgumentList.Arguments.Count == 0 &&
            invocation.Expression is MemberAccessExpressionSyntax call &&
            call.Name is IdentifierNameSyntax { Identifier.ValueText: ServiceRegistrationCall } &&
            call.Expression is MemberAccessExpressionSyntax services &&
            services.Name.Identifier.ValueText == "Services" &&
            services.Expression is IdentifierNameSyntax receiver)
        {
            return receiver.Identifier.ValueText;
        }

        return null;
    }

    /// <summary>
    /// The statement lists that make up the application's startup path:
    /// the file's top-level statements and the body of every static
    /// <c>Main</c> method. Statements inside any other method, lambda or
    /// local function are deliberately not part of it.
    /// </summary>
    private static List<List<StatementSyntax>> GetStartupStatementLists(SyntaxNode root)
    {
        var lists = new List<List<StatementSyntax>>();

        if (root is CompilationUnitSyntax unit)
        {
            List<StatementSyntax> topLevel = unit.Members
                .OfType<GlobalStatementSyntax>()
                .Select(global => global.Statement)
                .ToList();
            if (topLevel.Count > 0)
            {
                lists.Add(topLevel);
            }
        }

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method.Identifier.ValueText == "Main" &&
                method.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                method.Body is not null)
            {
                lists.Add(method.Body.Statements.ToList());
            }
        }

        return lists;
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

    private const string ServiceRegistrationCall = "AddSyncfusionBlazorToolkit";
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

        string? programPath = DiscoverEntryPoint(projectPath, out _);
        if (string.IsNullOrEmpty(programPath))
        {
            return (ServiceRegistrationOutcome.Skipped, null);
        }

        string text = ReadTextPreservingEncoding(programPath, out Encoding programEncoding);
        string relativePath = ToRelativePath(projectDirectory, programPath);
        switch (EvaluateRegistration(text))
        {
            case RegistrationState.Registered:
                return (ServiceRegistrationOutcome.AlreadyPresent, relativePath);
            case RegistrationState.Unverifiable:
                // Host builder or Build() boundary could not be established:
                // leave the file untouched and let the caller report it.
                return (ServiceRegistrationOutcome.Skipped, relativePath);
        }

        if (!TryAppendServiceRegistration(programPath, text, hostKind, programEncoding))
        {
            // No single host-builder declaration on the startup path to
            // anchor on. Appending at the end of the file would land after
            // Build()/Run() and break DI, so leave the file untouched.
            return (ServiceRegistrationOutcome.Skipped, relativePath);
        }

        return (ServiceRegistrationOutcome.Inserted, relativePath);
    }

    /// <summary>
    /// Locates the application entry-point source file from the project's
    /// MSBuild-evaluated <c>Compile</c> items, i.e. from what is actually
    /// compiled (SDK globbing, <c>Remove</c>/<c>Update</c>, imported
    /// <c>.props</c>/<c>.targets</c> and <c>DefaultItemExcludes</c> all
    /// applied) rather than from file names on disk.
    ///
    /// <para>The entry point is the single compiled file that declares a
    /// host builder (<c>WebApplication.CreateBuilder</c> /
    /// <c>WebAssemblyHostBuilder.CreateDefault</c>) on the startup path
    /// (top-level statements or a static <c>Main</c>); this also finds
    /// <c>Hosting.cs</c>-style layouts. A builder created inside an
    /// unrelated helper method does not make a file the entry point.
    /// When no compiled file declares one,
    /// a single compiled <c>Program.cs</c> is returned so a custom
    /// bootstrap that is already registered can still be recognised.</para>
    ///
    /// <para>Fails closed: returns <c>null</c> with a diagnostic when
    /// evaluation fails, a source file cannot be read, or the answer is
    /// ambiguous (several compiled files declare a host builder).</para>
    /// </summary>
    public static string? DiscoverEntryPoint(string? projectPath, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            error = $"The project file '{projectPath}' was not found.";
            return null;
        }

        var projectService = new MSBuildProjectService(projectPath);
        if (!projectService.TryGetEvaluatedItems("Compile", out var items, out string? evaluationError))
        {
            error = $"Unable to evaluate the compiled sources of '{projectPath}'. {evaluationError}";
            return null;
        }

        List<string> sources = items
            .Select(item => item.FullPath)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hostFiles = new List<string>();
        foreach (string path in sources)
        {
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = $"Unable to read compiled source '{path}': {ex.Message}";
                return null;
            }

            if (!text.Contains("CreateBuilder", StringComparison.Ordinal) &&
                !text.Contains("CreateDefault", StringComparison.Ordinal))
            {
                continue;
            }

            SyntaxNode root = CSharpSyntaxTree.ParseText(text).GetRoot();
            if (FindHostBuilderDeclarations(root, ProgramHostKind.WebApp, ProgramHostKind.StandaloneWasm).Count > 0)
            {
                hostFiles.Add(path);
            }
        }

        if (hostFiles.Count == 1)
        {
            return hostFiles[0];
        }

        if (hostFiles.Count > 1)
        {
            error = $"Several compiled files declare a host builder, so the application entry point is ambiguous: {string.Join(", ", hostFiles)}.";
            return null;
        }

        List<string> conventional = sources
            .Where(path => string.Equals(Path.GetFileName(path), "Program.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (conventional.Count == 1)
        {
            return conventional[0];
        }

        error = $"No compiled source of '{projectPath}' declares a host builder " +
            "('WebApplication.CreateBuilder(...)' or 'WebAssemblyHostBuilder.CreateDefault(...)').";
        return null;
    }

    /// <summary>
    /// True when the project's MSBuild-evaluated <c>PackageReference</c>
    /// items include <c>Syncfusion.Blazor.Toolkit</c> (exact id). Reflects
    /// references that come from imported <c>.props</c>/<c>.targets</c> and
    /// is not fooled by comments or similarly named packages. Returns
    /// <c>false</c> with a diagnostic when evaluation fails.
    /// </summary>
    public static bool TryIsPackageReferenced(string? projectPath, out bool referenced, out string? error)
    {
        referenced = false;
        error = null;
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            error = $"The project file '{projectPath}' was not found.";
            return false;
        }

        var projectService = new MSBuildProjectService(projectPath);
        if (!projectService.TryGetEvaluatedItems("PackageReference", out var items, out string? evaluationError))
        {
            error = $"Unable to evaluate the package references of '{projectPath}'. {evaluationError}";
            return false;
        }

        referenced = items.Any(item => string.Equals(item.Include, "Syncfusion.Blazor.Toolkit", StringComparison.OrdinalIgnoreCase));
        return true;
    }

    /// <summary>
    /// The version of the evaluated <c>Syncfusion.Blazor.Toolkit</c> package
    /// reference: <c>null</c> when it is not referenced, an empty string when it is
    /// referenced without a version (central package management). Returns
    /// <c>false</c> with a diagnostic when evaluation fails.
    /// </summary>
    public static bool TryGetReferencedPackageVersion(string? projectPath, out string? version, out string? error)
    {
        version = null;
        error = null;
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            error = $"The project file '{projectPath}' was not found.";
            return false;
        }

        var projectService = new MSBuildProjectService(projectPath);
        if (!projectService.TryGetEvaluatedItems("PackageReference", "Version", out var items, out string? evaluationError))
        {
            error = $"Unable to evaluate the package references of '{projectPath}'. {evaluationError}";
            return false;
        }

        foreach (var item in items)
        {
            if (string.Equals(item.Include, "Syncfusion.Blazor.Toolkit", StringComparison.OrdinalIgnoreCase))
            {
                version = item.Metadata;
                return true;
            }
        }

        return true;
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

        SyntaxNode root = CSharpSyntaxTree.ParseText(programText).GetRoot();
        return FindHostBuilderDeclaration(root, hostKind)?.Name;
    }

    /// <summary>
    /// A host-builder variable declared as a direct statement of the
    /// startup scope, e.g. <c>var builder = WebApplication.CreateBuilder(args);</c>.
    /// </summary>
    private sealed record HostBuilderDeclaration(
        string Name,
        string SourceName,
        LocalDeclarationStatementSyntax Statement,
        List<StatementSyntax> Scope);

    /// <summary>
    /// The single host-builder declaration of the requested kind(s), or
    /// <c>null</c> when there is none or it is ambiguous (refuse rather
    /// than guess).
    /// </summary>
    private static HostBuilderDeclaration? FindHostBuilderDeclaration(SyntaxNode root, params ProgramHostKind[] kinds)
    {
        List<HostBuilderDeclaration> matches = FindHostBuilderDeclarations(root, kinds);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Host-builder declarations that are direct statements of the startup
    /// scope and whose initializer <em>is</em> the host-builder creation
    /// call (optionally parenthesised). Matching is structural, so line
    /// breaks inside the member access do not matter, and a creation call
    /// nested inside an unrelated wrapper such as
    /// <c>Wrap(WebApplication.CreateBuilder(args))</c> is not mistaken for
    /// the builder itself.
    /// </summary>
    private static List<HostBuilderDeclaration> FindHostBuilderDeclarations(SyntaxNode root, params ProgramHostKind[] kinds)
    {
        var matches = new List<HostBuilderDeclaration>();
        var types = new HostTypeResolver(root);
        foreach (List<StatementSyntax> scope in GetStartupStatementLists(root))
        {
            foreach (LocalDeclarationStatementSyntax statement in scope.OfType<LocalDeclarationStatementSyntax>())
            {
                foreach (VariableDeclaratorSyntax declarator in statement.Declaration.Variables)
                {
                    ProgramHostKind? kind = types.GetHostBuilderKind(declarator.Initializer?.Value);
                    if (kind is not null && kinds.Contains(kind.Value))
                    {
                        // `Name` is the unescaped identifier (used to compare), `SourceName` keeps
                        // the source spelling (`@class`) for any code that is generated.
                        matches.Add(new HostBuilderDeclaration(declarator.Identifier.ValueText, declarator.Identifier.Text, statement, scope));
                    }
                }
            }
        }

        return matches;
    }

    private const string WebApplicationType = "Microsoft.AspNetCore.Builder.WebApplication";
    private const string WasmHostBuilderType = "Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHostBuilder";

    /// <summary>
    /// Resolves the type named in <c>Type.CreateBuilder(...)</c> /
    /// <c>Type.CreateDefault(...)</c> to the framework's host-builder types
    /// without a compilation: using aliases (<c>using WebApp =
    /// Microsoft.AspNetCore.Builder.WebApplication;</c>, including
    /// <c>global using</c> and chains of aliases), <c>global::</c> and fully
    /// qualified names are followed, and the resulting full name must equal
    /// the framework type. A bare <c>WebApplication</c> /
    /// <c>WebAssemblyHostBuilder</c> (supplied by the SDK's implicit usings
    /// or an ordinary using) is accepted only when the file does not
    /// declare a type or alias of that name. Anything else - a different
    /// namespace, an alias to a non-framework type, a look-alike - is not a
    /// host builder.
    /// </summary>
    private sealed class HostTypeResolver
    {
        private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);
        private readonly HashSet<string> _declaredTypes = new(StringComparer.Ordinal);

        public HostTypeResolver(SyntaxNode root)
        {
            foreach (UsingDirectiveSyntax directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                if (directive.Alias is not null && directive.StaticKeyword.IsKind(SyntaxKind.None))
                {
                    _aliases[directive.Alias.Name.Identifier.ValueText] = Normalize(directive.NamespaceOrType.ToString());
                }
            }

            foreach (BaseTypeDeclarationSyntax type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                _declaredTypes.Add(type.Identifier.ValueText);
            }

            foreach (DelegateDeclarationSyntax type in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
            {
                _declaredTypes.Add(type.Identifier.ValueText);
            }
        }

        public ProgramHostKind? GetHostBuilderKind(ExpressionSyntax? initializer)
        {
            while (initializer is ParenthesizedExpressionSyntax parenthesized)
            {
                initializer = parenthesized.Expression;
            }

            if (initializer is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access })
            {
                return null;
            }

            string method = access.Name.Identifier.ValueText;
            string type = Resolve(Normalize(access.Expression.ToString()));

            return (type, method) switch
            {
                (WebApplicationType, "CreateBuilder") => ProgramHostKind.WebApp,
                (WasmHostBuilderType, "CreateDefault") => ProgramHostKind.StandaloneWasm,
                ("WebApplication", "CreateBuilder") when !_declaredTypes.Contains("WebApplication") => ProgramHostKind.WebApp,
                ("WebAssemblyHostBuilder", "CreateDefault") when !_declaredTypes.Contains("WebAssemblyHostBuilder") => ProgramHostKind.StandaloneWasm,
                _ => null,
            };
        }

        private string Resolve(string name)
        {
            // Follow alias chains (bounded, to survive cyclic aliases).
            for (int i = 0; i < 8; i++)
            {
                int dot = name.IndexOf('.');
                string head = dot < 0 ? name : name[..dot];
                if (!_aliases.TryGetValue(head, out string? target))
                {
                    break;
                }

                name = dot < 0 ? target : target + name[dot..];
            }

            return name;
        }

        private static string Normalize(string name)
        {
            string compact = string.Concat(name.Where(c => !char.IsWhiteSpace(c)));
            return compact.StartsWith("global::", StringComparison.Ordinal) ? compact["global::".Length..] : compact;
        }
    }

    private static bool TryAppendServiceRegistration(string programPath, string existingText, ProgramHostKind hostKind, Encoding encoding)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(existingText).GetRoot();
        HostBuilderDeclaration? builder = FindHostBuilderDeclaration(root, hostKind);
        if (builder is null)
        {
            return false;
        }

        // The variable name comes from the same declaration used as the
        // insertion anchor, so the generated call always targets it.
        string registrationLine = $"{builder.SourceName}.Services.{ServiceRegistrationCall}();";

        // Insert immediately after the (possibly multi-line) host-builder
        // statement so the services are wired up before `Build()`.
        string newline = existingText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        int insertAt = builder.Statement.FullSpan.End; // includes trailing trivia
        string before = existingText[..insertAt];
        string after = existingText[insertAt..];
        string prefix = before.EndsWith('\n') ? string.Empty : newline;
        string withRegistration = before + prefix + registrationLine + newline + after;

        File.WriteAllText(programPath, EnsureUsingDirective(withRegistration, ToolKitUsingDirective), encoding);
        return true;
    }

    /// <summary>
    /// Returns <paramref name="source"/> with
    /// <paramref name="usingDirective"/> added when it is not already
    /// present. Placement is syntax-aware: the directive goes after the
    /// last existing <c>extern alias</c>/<c>using</c> (so it never lands
    /// before an <c>extern alias</c>, which would be CS0439, and stays
    /// grouped with the other usings). Without any, it goes after the
    /// file's leading header comments and preprocessor lines, immediately
    /// before the first code token. An already-present (non-commented)
    /// directive leaves the source unchanged.
    /// </summary>
    private static string EnsureUsingDirective(string source, string usingDirective)
    {
        if (string.IsNullOrEmpty(source))
        {
            return usingDirective + Environment.NewLine;
        }

        if (string.IsNullOrEmpty(usingDirective))
        {
            return source;
        }

        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        string wanted = usingDirective.TrimEnd(';').Replace("using", string.Empty, StringComparison.Ordinal).Trim();
        foreach (UsingDirectiveSyntax existing in root.Usings)
        {
            if (existing.Alias is null &&
                existing.StaticKeyword.IsKind(SyntaxKind.None) &&
                existing.Name?.ToString().Replace(" ", string.Empty) == wanted)
            {
                return source;
            }
        }

        string newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

        int lastDirectiveEnd = -1;
        foreach (ExternAliasDirectiveSyntax extern_ in root.Externs)
        {
            lastDirectiveEnd = Math.Max(lastDirectiveEnd, extern_.FullSpan.End);
        }

        foreach (UsingDirectiveSyntax existing in root.Usings)
        {
            lastDirectiveEnd = Math.Max(lastDirectiveEnd, existing.FullSpan.End);
        }

        if (lastDirectiveEnd >= 0)
        {
            string before = source[..lastDirectiveEnd];
            string prefix = before.EndsWith('\n') ? string.Empty : newline;
            return before + prefix + usingDirective + newline + source[lastDirectiveEnd..];
        }

        // No usings: insert at the start of the first code token's line,
        // i.e. after any header comment / #nullable / #pragma lines.
        SyntaxToken first = root.GetFirstToken(includeZeroWidth: false);
        int insertAt = first.IsKind(SyntaxKind.None) ? source.Length : first.SpanStart;
        int lineStart = source.LastIndexOf('\n', Math.Max(0, insertAt - 1)) + 1;
        if (lineStart > insertAt)
        {
            lineStart = insertAt;
        }

        return source[..lineStart] + usingDirective + newline + newline + source[lineStart..];
    }
}
