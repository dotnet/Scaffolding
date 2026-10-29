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
    /// <param name="MapsInteractiveServerRenderMode">'.AddInteractiveServerRenderMode()' is chained on the mapped components.</param>
    /// <param name="MapsInteractiveWebAssemblyRenderMode">'.AddInteractiveWebAssemblyRenderMode()' is chained on the mapped components.</param>
    internal sealed record BlazorInteractivity(
        bool UsesRazorComponents,
        bool UsesInteractiveServer,
        bool UsesInteractiveWebAssembly,
        bool MapsRazorComponents,
        bool MapsInteractiveServerRenderMode,
        bool MapsInteractiveWebAssemblyRenderMode)
    {
        /// <summary>
        /// True when the app registers both Server and WebAssembly interactivity, which is what the Auto render mode needs.
        /// </summary>
        public bool UsesInteractiveAuto => UsesInteractiveServer && UsesInteractiveWebAssembly;

        /// <summary>
        /// True when Interactive Server can run: both the service registration and the endpoint render mode are configured.
        /// </summary>
        public bool HasCompleteInteractiveServer => UsesInteractiveServer && MapsInteractiveServerRenderMode;

        /// <summary>
        /// True when only one of the Interactive Server service registration and endpoint render mode is configured.
        /// </summary>
        public bool HasPartialInteractiveServer => UsesInteractiveServer != MapsInteractiveServerRenderMode;

        /// <summary>
        /// True when Interactive WebAssembly can run: both the service registration and the endpoint render mode are configured.
        /// </summary>
        public bool HasCompleteInteractiveWebAssembly => UsesInteractiveWebAssembly && MapsInteractiveWebAssemblyRenderMode;

        /// <summary>
        /// True when only one of the Interactive WebAssembly service registration and endpoint render mode is configured.
        /// </summary>
        public bool HasPartialInteractiveWebAssembly => UsesInteractiveWebAssembly != MapsInteractiveWebAssemblyRenderMode;
    }

    /// <summary>
    /// Analyzes <paramref name="programDocument"/>. Returns an actionable error instead of guessing when the Blazor APIs
    /// cannot be resolved: an unresolved registration is not an absent registration.
    /// </summary>
    /// <param name="programDocument">The Program.cs document from the project's Roslyn workspace.</param>
    /// <param name="programPath">The Program.cs path, for diagnostics.</param>
    /// <param name="projectPath">The project path, for diagnostics.</param>
    /// <param name="allowSyntaxFallback">
    /// When the semantic model cannot resolve the Blazor registrations, match the invoked Blazor method names in
    /// Program.cs instead of failing. This covers projects that have not been restored: the framework reference pack may
    /// be missing, and registrations from packages (such as 'AddInteractiveWebAssemblyComponents()' from
    /// Microsoft.AspNetCore.Components.WebAssembly.Server) do not resolve until the packages are restored. The names are
    /// specific to the Blazor APIs, so an unresolved registration still counts as present and is never duplicated.
    /// </param>
    internal static async Task<(BlazorInteractivity? Interactivity, string? Error)> AnalyzeAsync(Document? programDocument, string programPath, string? projectPath, bool allowSyntaxFallback = false)
    {
        var semanticModel = programDocument is null ? null : await programDocument.GetSemanticModelAsync();
        var programRoot = programDocument is null ? null : await programDocument.GetSyntaxRootAsync();
        var blazorApisAvailable = semanticModel?.Compilation.GetTypeByMetadataName(BlazorCrudHelper.IRazorComponentsBuilderType) is not null;

        // An unresolved registration is not an absent registration. Other errors (such as unavailable
        // generated Razor component types) need not prevent analysis of these registrations.
        var unresolvedRegistration = !blazorApisAvailable || programRoot is null ? null : programRoot.DescendantNodes().OfType<SimpleNameSyntax>()
            .FirstOrDefault(name =>
                name.Identifier.ValueText is BlazorCrudHelper.AddInteractiveServerComponentsMethod or BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod
                    or BlazorCrudHelper.AddInteractiveServerRenderModeMethod or BlazorCrudHelper.AddInteractiveWebAssemblyRenderModeMethod &&
                IsInvokedName(name) &&
                semanticModel!.GetSymbolInfo(name).Symbol is null);

        if (allowSyntaxFallback && programRoot is not null && (!blazorApisAvailable || unresolvedRegistration is not null))
        {
            return (new BlazorInteractivity(
                IsInvoked(programRoot, BlazorCrudHelper.AddRazorComponentsMethod),
                IsInvoked(programRoot, BlazorCrudHelper.AddInteractiveServerComponentsMethod),
                IsInvoked(programRoot, BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod),
                IsInvoked(programRoot, BlazorCrudHelper.MapRazorComponentsMethod),
                IsInvoked(programRoot, BlazorCrudHelper.AddInteractiveServerRenderModeMethod),
                IsInvoked(programRoot, BlazorCrudHelper.AddInteractiveWebAssemblyRenderModeMethod)), null);
        }

        if (programDocument is null || semanticModel is null || programRoot is null || !blazorApisAvailable)
        {
            return (null, $"Unable to analyze Blazor registrations in '{programPath}'. Ensure the project's SDK and references are available and 'dotnet restore' succeeds.");
        }

        if (unresolvedRegistration is not null)
        {
            return (null, $"Unable to resolve Blazor registration '{unresolvedRegistration}' in '{programPath}'. Check the registration's imports and package references, then run 'dotnet build \"{projectPath}\"' for diagnostics.");
        }

        var interactivity = new BlazorInteractivity(
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddRazorComponentsMethod, BlazorCrudHelper.IServiceCollectionType),
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddInteractiveServerComponentsMethod, BlazorCrudHelper.IRazorComponentsBuilderType),
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod, BlazorCrudHelper.IRazorComponentsBuilderType),
            // The mapped component type (App) is generated from Razor and may not resolve, so the call is matched by name.
            IsInvoked(programRoot, BlazorCrudHelper.MapRazorComponentsMethod),
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddInteractiveServerRenderModeMethod, BlazorCrudHelper.RazorComponentsEndpointsConventionBuilderType),
            await RoslynUtilities.CheckDocumentForMethodInvocationAsync(programDocument, BlazorCrudHelper.AddInteractiveWebAssemblyRenderModeMethod, BlazorCrudHelper.RazorComponentsEndpointsConventionBuilderType));
        return (interactivity, null);
    }

    /// <summary>
    /// Returns true when a method named <paramref name="methodName"/> is invoked in <paramref name="root"/>. Comments,
    /// string literals and disabled preprocessor blocks are not syntax nodes, so calls in them do not count.
    /// </summary>
    internal static bool IsInvoked(SyntaxNode root, string methodName)
        => root.DescendantNodes().OfType<SimpleNameSyntax>().Any(name => name.Identifier.ValueText == methodName && IsInvokedName(name));

    private static bool IsInvokedName(SimpleNameSyntax name)
        => name.Parent is InvocationExpressionSyntax ||
           name.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax } ||
           name.Parent is MemberBindingExpressionSyntax { Parent: InvocationExpressionSyntax };
}
