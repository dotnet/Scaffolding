// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;

/// <summary>
/// What kind of application a project is, decided once from MSBuild-evaluated
/// state (never from raw .csproj text).
/// </summary>
internal enum BlazorAppKind
{
    /// <summary>Not a Blazor application the scaffolder supports (class library, console, Web API, ...).</summary>
    NotBlazor = 0,

    /// <summary>Blazor Web App / Blazor Server: a <c>Microsoft.NET.Sdk.Web</c> host that maps Razor components.</summary>
    WebApp = 1,

    /// <summary>Standalone Blazor WebAssembly application (<c>Microsoft.NET.Sdk.BlazorWebAssembly</c>).</summary>
    StandaloneWasm = 2,

    /// <summary>MAUI / Blazor Hybrid. Explicitly unsupported.</summary>
    Hybrid = 3,
}

/// <summary>
/// The interactive hosting support a Blazor Web App has actually registered.
/// This is independent of the page render modes used by individual components.
/// </summary>
[Flags]
internal enum BlazorHostingModes
{
    None = 0,

    /// <summary><c>AddInteractiveServerComponents()</c> and <c>AddInteractiveServerRenderMode()</c>.</summary>
    Server = 1,

    /// <summary><c>AddInteractiveWebAssemblyComponents()</c> and <c>AddInteractiveWebAssemblyRenderMode()</c>.</summary>
    WebAssembly = 2,
}

/// <summary>Result of <see cref="BlazorProjectClassifier.Classify"/>.</summary>
internal sealed record BlazorProjectInfo(BlazorAppKind Kind, string? Error);

/// <summary>
/// Classifies a project once, from evaluated MSBuild properties, so every
/// step of the scaffolder agrees about what it is operating on.
/// </summary>
internal static class BlazorProjectClassifier
{
    // Evaluated by the SDKs themselves, so imports, conditions and Directory.Build.props are honoured.
    private static readonly string[] Properties =
    [
        "UsingMicrosoftNETSdkWeb",
        "UsingMicrosoftNETSdkBlazorWebAssembly",
        "UsingMicrosoftNETSdkRazor",
        "UseMaui",
        "OutputType",
    ];

    /// <summary>
    /// Decides the application kind. Fails closed: when evaluation fails the
    /// kind is <see cref="BlazorAppKind.NotBlazor"/> with a diagnostic, so
    /// callers must stop instead of guessing.
    /// </summary>
    public static BlazorProjectInfo Classify(string? projectPath)
    {
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            return new BlazorProjectInfo(BlazorAppKind.NotBlazor, $"The project file '{projectPath}' was not found.");
        }

        var service = new MSBuildProjectService(projectPath);
        if (!service.TryGetEvaluatedProperties(Properties, out var values, out string? error))
        {
            return new BlazorProjectInfo(
                BlazorAppKind.NotBlazor,
                $"Unable to evaluate '{projectPath}' to determine whether it is a Blazor application. {error}");
        }

        // `UseMaui=false` is an explicit opt-out; only a true value means MAUI.
        if (IsTrue(values["UseMaui"]))
        {
            return new BlazorProjectInfo(BlazorAppKind.Hybrid, null);
        }

        if (IsTrue(values["UsingMicrosoftNETSdkBlazorWebAssembly"]))
        {
            return new BlazorProjectInfo(BlazorAppKind.StandaloneWasm, null);
        }

        // Microsoft.NET.Sdk.Web is also the SDK of Web API / minimal API apps, so the SDK alone proves nothing.
        // A Blazor Web App is an executable Web project that also uses the Razor SDK (UsingMicrosoftNETSdkRazor)
        // AND actually contains Razor components or maps them.
        if (IsTrue(values["UsingMicrosoftNETSdkWeb"]) &&
            IsTrue(values["UsingMicrosoftNETSdkRazor"]) &&
            string.Equals(values["OutputType"], "Exe", StringComparison.OrdinalIgnoreCase) &&
            (HasRazorComponents(service) || MapsRazorComponents(service)))
        {
            return new BlazorProjectInfo(BlazorAppKind.WebApp, null);
        }

        return new BlazorProjectInfo(BlazorAppKind.NotBlazor, null);
    }

    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    // The evaluated Content/RazorComponent items include every .razor file the SDK compiles, wherever it lives.
    private static bool HasRazorComponents(MSBuildProjectService service)
    {
        foreach (string itemType in new[] { "Content", "RazorComponent", "None" })
        {
            if (service.TryGetEvaluatedItems(itemType, out var items, out _) &&
                items.Any(item => item.Include.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    // A host can keep its components in a referenced Razor class library and only call
    // MapRazorComponents<Library.App>() from startup code, so it has no local .razor items.
    private static bool MapsRazorComponents(MSBuildProjectService service)
    {
        if (!service.TryGetEvaluatedItems("Compile", out var items, out _))
        {
            return false;
        }

        foreach (string path in items.Select(i => i.FullPath).Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Distinct())
        {
            string text;
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                text = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (!text.Contains("MapRazorComponents", StringComparison.Ordinal))
            {
                continue;
            }

            SyntaxNode root = CSharpSyntaxTree.ParseText(text).GetRoot();
            bool mapped = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                invocation.Expression switch
                {
                    MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText == "MapRazorComponents",
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "MapRazorComponents",
                    _ => false,
                });
            if (mapped)
            {
                return true;
            }
        }

        return false;
    }
}
