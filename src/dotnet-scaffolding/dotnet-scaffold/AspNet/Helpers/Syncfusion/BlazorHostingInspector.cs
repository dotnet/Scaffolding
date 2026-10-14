// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;

/// <summary>
/// What the application's entry point has actually registered for interactive
/// hosting. Application kind, hosting modes and the global page render mode are
/// three different facts and are reported separately: having no global render
/// mode does not mean the app has no interactivity (components can opt in per
/// page), and a Server+WebAssembly registration is what <c>InteractiveAuto</c>
/// needs - not the presence of a client project reference.
/// </summary>
internal sealed record BlazorHostingFacts(
    BlazorHostingModes Registered,
    BlazorHostingModes Mapped,
    string GlobalRenderMode)
{
    /// <summary>Modes whose component services AND endpoint render mode are both configured.</summary>
    public BlazorHostingModes Supported => Registered & Mapped;

    public static BlazorHostingFacts Unknown { get; } = new(BlazorHostingModes.None, BlazorHostingModes.None, "None");
}

internal static class BlazorHostingInspector
{
    private const string ServerServices = "AddInteractiveServerComponents";
    private const string WasmServices = "AddInteractiveWebAssemblyComponents";
    private const string ServerEndpoint = "AddInteractiveServerRenderMode";
    private const string WasmEndpoint = "AddInteractiveWebAssemblyRenderMode";

    /// <summary>
    /// Reads the registrations from the entry point's syntax. Matching is by
    /// invoked method name inside the whole file, which also covers fluent
    /// chains (<c>AddRazorComponents().AddInteractiveServerComponents()</c>)
    /// and registrations split across statements or helper methods.
    /// </summary>
    public static BlazorHostingFacts Inspect(string? entryPointText, string? appRazorText)
    {
        BlazorHostingModes registered = BlazorHostingModes.None;
        BlazorHostingModes mapped = BlazorHostingModes.None;

        if (!string.IsNullOrEmpty(entryPointText))
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(entryPointText).GetRoot();
            foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string? name = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    _ => null,
                };

                switch (name)
                {
                    case ServerServices: registered |= BlazorHostingModes.Server; break;
                    case WasmServices: registered |= BlazorHostingModes.WebAssembly; break;
                    case ServerEndpoint: mapped |= BlazorHostingModes.Server; break;
                    case WasmEndpoint: mapped |= BlazorHostingModes.WebAssembly; break;
                }
            }
        }

        return new BlazorHostingFacts(registered, mapped, ReadGlobalRenderMode(appRazorText));
    }

    /// <summary>
    /// The render mode applied to the whole app from <c>App.razor</c>
    /// (e.g. <c>&lt;Routes @rendermode="InteractiveServer" /&gt;</c>), or
    /// <c>"None"</c> when pages choose their own. <c>"None"</c> never means
    /// "not interactive".
    /// </summary>
    private static string ReadGlobalRenderMode(string? appRazorText)
    {
        if (string.IsNullOrEmpty(appRazorText))
        {
            return "None";
        }

        foreach (string mode in new[] { "InteractiveAuto", "InteractiveServer", "InteractiveWebAssembly" })
        {
            if (appRazorText.Contains($"@rendermode=\"{mode}\"", StringComparison.OrdinalIgnoreCase) ||
                appRazorText.Contains($"@rendermode=\"RenderMode.{mode}\"", StringComparison.OrdinalIgnoreCase) ||
                appRazorText.Contains($"@rendermode=\"@RenderMode.{mode}\"", StringComparison.OrdinalIgnoreCase) ||
                appRazorText.Contains($"@rendermode=\"@({mode})\"", StringComparison.OrdinalIgnoreCase))
            {
                return mode;
            }
        }

        return "None";
    }
}
