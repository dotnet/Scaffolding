// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Roslyn;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Analyzes the Blazor registrations in a project's Program.cs with the semantic model, so that only registrations
/// that bind to the ASP.NET Core APIs count.
/// </summary>
internal static class BlazorInteractivityAnalyzer
{
    /// <summary>
    /// The Blazor hosting configuration registered in Program.cs.
    /// </summary>
    /// <param name="UsesRazorComponents">'builder.Services.AddRazorComponents()' is registered (a Blazor Web App).</param>
    /// <param name="UsesInteractiveServer">'.AddInteractiveServerComponents()' is registered.</param>
    /// <param name="UsesInteractiveWebAssembly">'.AddInteractiveWebAssemblyComponents()' is registered.</param>
    /// <param name="MapsRazorComponents">'app.MapRazorComponents&lt;...&gt;()' is called.</param>
    internal sealed record BlazorInteractivity(
        bool UsesRazorComponents,
        bool UsesInteractiveServer,
        bool UsesInteractiveWebAssembly,
        bool MapsRazorComponents)
    {
        /// <summary>
        /// True when the app registers both Server and WebAssembly interactivity, which is what the Auto render mode needs.
        /// </summary>
        public bool UsesInteractiveAuto => UsesInteractiveServer && UsesInteractiveWebAssembly;
    }

    /// <summary>
    /// Analyzes <paramref name="programDocument"/>. Returns an actionable error instead of guessing when the Blazor APIs
    /// cannot be resolved: an unresolved registration is not an absent registration.
    /// </summary>
    /// <param name="programDocument">The Program.cs document from the project's Roslyn workspace.</param>
    /// <param name="programPath">The Program.cs path, for diagnostics.</param>
    /// <param name="projectPath">The project path, for diagnostics.</param>
    /// <param name="allowSyntaxFallback">
    /// When the Blazor APIs are not available to the semantic model at all (for example a project that has not been
    /// restored for a target framework whose reference pack is not installed), match the invoked Blazor method names in
    /// Program.cs instead of failing. The names are specific to the Blazor APIs, so a registration is never missed.
    /// </param>
    internal static async Task<(BlazorInteractivity? Interactivity, string? Error)> AnalyzeAsync(Document? programDocument, string programPath, string? projectPath, bool allowSyntaxFallback = false)
    {
        var semanticModel = programDocument is null ? null : await programDocument.GetSemanticModelAsync();
        var programRoot = programDocument is null ? null : await programDocument.GetSyntaxRootAsync();
        if (allowSyntaxFallback && programRoot is not null &&
            semanticModel?.Compilation.GetTypeByMetadataName(BlazorCrudHelper.IRazorComponentsBuilderType) is null)
        {
            return (new BlazorInteractivity(
                IsInvoked(programRoot, BlazorCrudHelper.AddRazorComponentsMethod),
                IsInvoked(programRoot, BlazorCrudHelper.AddInteractiveServerComponentsMethod),
                IsInvoked(programRoot, BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod),
                IsInvoked(programRoot, BlazorCrudHelper.MapRazorComponentsMethod)), null);
        }

        if (programDocument is null || semanticModel is null || programRoot is null ||
            semanticModel.Compilation.GetTypeByMetadataName(BlazorCrudHelper.IRazorComponentsBuilderType) is null)
        {
            return (null, $"Unable to analyze Blazor registrations in '{programPath}'. Ensure the project's SDK and references are available and 'dotnet restore' succeeds.");
        }

        // An unresolved registration is not an absent registration. Other errors (such as unavailable
        // generated Razor component types) need not prevent analysis of these service registrations.
        var unresolvedRegistration = programRoot.DescendantNodes().OfType<SimpleNameSyntax>()
            .FirstOrDefault(name =>
                name.Identifier.ValueText is BlazorCrudHelper.AddInteractiveServerComponentsMethod or BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod &&
                IsInvokedName(name) &&
                semanticModel.GetSymbolInfo(name).Symbol is null);
        if (unresolvedRegistration is not null)
        {
            return (null, $"Unable to resolve Blazor registration '{unresolvedRegistration}' in '{programPath}'. Check the registration's imports and package references, then run 'dotnet build \"{projectPath}\"' for diagnostics.");
        }

        var interactivity = new BlazorInteractivity(
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddRazorComponentsMethod, BlazorCrudHelper.IServiceCollectionType),
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddInteractiveServerComponentsMethod, BlazorCrudHelper.IRazorComponentsBuilderType),
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod, BlazorCrudHelper.IRazorComponentsBuilderType),
            // The mapped component type (App) is generated from Razor and may not resolve, so the call is matched by name.
            IsInvoked(programRoot, BlazorCrudHelper.MapRazorComponentsMethod));
        return (interactivity, null);
    }

    private static bool IsInvoked(SyntaxNode root, string methodName)
        => root.DescendantNodes().OfType<SimpleNameSyntax>().Any(name => name.Identifier.ValueText == methodName && IsInvokedName(name));

    private static bool IsInvokedName(SimpleNameSyntax name)
        => name.Parent is InvocationExpressionSyntax ||
           name.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } ||
           name.Parent is MemberBindingExpressionSyntax { Parent: InvocationExpressionSyntax };
}
