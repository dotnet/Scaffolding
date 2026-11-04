// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Inspects evaluated MSBuild projects for the Ignite UI for Blazor scaffolder: whether the target is a supported
/// Blazor app, and whether a project already references the commercial Ignite UI for Blazor package.
/// MSBuild evaluation resolves SDKs, imports (e.g. Directory.Build.props), conditions and globs, so the checks do not
/// depend on the default template layout and do not require the projects to have been restored.
/// </summary>
internal static class IgniteUIBlazorProjectInspector
{
    /// <summary>
    /// Commercial Ignite UI for Blazor packages. Each one already contains every IgniteUI.Blazor.Lite component in the
    /// same 'IgniteUI.Blazor.Controls' namespace, so it cannot be combined with IgniteUI.Blazor.Lite.
    /// </summary>
    internal static readonly string[] CommercialPackageIds = ["IgniteUI.Blazor", "IgniteUI.Blazor.Trial"];

    private const string UsingWebSdkProperty = "UsingMicrosoftNETSdkWeb";
    private const string UsingRazorSdkProperty = "UsingMicrosoftNETSdkRazor";
    private const string UsingBlazorWebAssemblySdkProperty = "UsingMicrosoftNETSdkBlazorWebAssembly";
    private const string UseMauiProperty = "UseMaui";
    private const string TargetFrameworkProperty = "TargetFramework";
    private const string TargetFrameworksProperty = "TargetFrameworks";

    /// <summary>
    /// The evaluated facts about a project that the Ignite UI checks need.
    /// </summary>
    /// <param name="ProjectPath">The project file path.</param>
    /// <param name="UsesWebSdk">True when the project uses Microsoft.NET.Sdk.Web (ASP.NET Core).</param>
    /// <param name="UsesRazorSdk">True when the project uses the Razor SDK (directly or through the Web/WebAssembly SDKs).</param>
    /// <param name="UsesBlazorWebAssemblySdk">True when the project uses Microsoft.NET.Sdk.BlazorWebAssembly.</param>
    /// <param name="UsesMaui">True when the project is a .NET MAUI app ('UseMaui' is true).</param>
    /// <param name="HasRazorComponents">
    /// True when the project includes at least one .razor file: as a Content item (Razor SDK default items) or as a
    /// RazorComponent item (the .NET MAUI SDK moves .razor files from Content to RazorComponent during evaluation).
    /// </param>
    /// <param name="PackageReferences">The evaluated PackageReference items.</param>
    internal sealed record ProjectEvaluation(
        string ProjectPath,
        bool UsesWebSdk,
        bool UsesRazorSdk,
        bool UsesBlazorWebAssemblySdk,
        bool UsesMaui,
        bool HasRazorComponents,
        IReadOnlyList<EvaluatedProjectItem> PackageReferences)
    {
        /// <summary>
        /// True for a .NET MAUI Blazor Hybrid app: a MAUI app that uses the Razor SDK and includes Razor components.
        /// </summary>
        public bool IsMauiBlazorHybrid => UsesMaui && UsesRazorSdk && HasRazorComponents;
    }

    /// <summary>
    /// Evaluates the project. MSBuild must already be registered (as done by ClassAnalyzers.GetProjectInfo).
    /// A multi-targeted project (e.g. a .NET MAUI app) gets its default items, such as .razor files, only in its
    /// per-framework inner builds, so items are collected from each target framework as well. This also finds
    /// PackageReference items that are conditioned on a target framework.
    /// </summary>
    /// <remarks>
    /// The per-framework evaluations run out of process with 'dotnet msbuild': platform target frameworks such as
    /// 'net10.0-android' evaluate SDK and workload property functions that need the SDK's own MSBuild assemblies.
    /// </remarks>
    /// <returns>True when evaluation succeeded; otherwise false with an actionable <paramref name="error"/>.</returns>
    internal static bool TryEvaluate(string projectPath, out ProjectEvaluation? evaluation, out string? error)
    {
        evaluation = null;
        string[] itemTypes = ["Content", "RazorComponent", "PackageReference"];
        var projectService = new MSBuildProjectService(projectPath);
        if (!projectService.TryGetEvaluatedProperties(
                [UsingWebSdkProperty, UsingRazorSdkProperty, UsingBlazorWebAssemblySdkProperty, UseMauiProperty, TargetFrameworkProperty, TargetFrameworksProperty],
                out var properties,
                out error) ||
            !projectService.TryGetEvaluatedItems(itemTypes, out var outerItems, out error))
        {
            error = $"Unable to evaluate '{projectPath}'. {error} Ensure the project's SDK and imports are available.";
            return false;
        }

        List<EvaluatedProjectItem> items = [.. outerItems];
        if (string.IsNullOrEmpty(properties[TargetFrameworkProperty]))
        {
            var targetFrameworks = properties[TargetFrameworksProperty].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var targetFramework in targetFrameworks)
            {
                if (!TryGetTargetFrameworkItems(projectPath, targetFramework, itemTypes, items, out error))
                {
                    error = $"Unable to evaluate '{projectPath}' for target framework '{targetFramework}'. {error} Ensure the project's SDK, workloads and imports are available.";
                    return false;
                }
            }
        }

        evaluation = new ProjectEvaluation(
            projectPath,
            IsTrue(properties[UsingWebSdkProperty]),
            IsTrue(properties[UsingRazorSdkProperty]),
            IsTrue(properties[UsingBlazorWebAssemblySdkProperty]),
            IsTrue(properties[UseMauiProperty]),
            items.Any(item => item.ItemType is "Content" or "RazorComponent" && item.EvaluatedInclude.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)),
            items.Where(item => item.ItemType == "PackageReference").Distinct().ToList());
        return true;
    }

    /// <summary>
    /// Returns an error when the project is not an app this scaffolder supports, or null when it is supported:
    /// <list type="bullet">
    /// <item>a standalone Blazor WebAssembly app (Microsoft.NET.Sdk.BlazorWebAssembly),</item>
    /// <item>a Blazor Web App or Blazor Server app: an ASP.NET Core project (Microsoft.NET.Sdk.Web) that includes Razor
    /// components or references a Blazor WebAssembly client project, or</item>
    /// <item>a .NET MAUI Blazor Hybrid app (see <see cref="ProjectEvaluation.IsMauiBlazorHybrid"/>).</item>
    /// </list>
    /// </summary>
    internal static string? GetUnsupportedProjectError(ProjectEvaluation evaluation, bool hasWebAssemblyClient)
    {
        if (evaluation.UsesBlazorWebAssemblySdk ||
            evaluation.IsMauiBlazorHybrid ||
            (evaluation.UsesWebSdk && (evaluation.HasRazorComponents || hasWebAssemblyClient)))
        {
            return null;
        }

        var reason = evaluation.UsesWebSdk
            ? "It is an ASP.NET Core project, but it includes no Razor components (.razor files) and references no Blazor WebAssembly client project."
            : evaluation.UsesMaui
                ? "It is a .NET MAUI app, but it does not use the Razor SDK with Razor components (.razor files), so it is not a Blazor Hybrid app."
                : evaluation.UsesRazorSdk
                    ? "It uses the Razor SDK without the ASP.NET Core Web SDK or .NET MAUI, like a Razor class library."
                    : "It uses neither the ASP.NET Core Web SDK, the Blazor WebAssembly SDK nor .NET MAUI.";
        return $"'{evaluation.ProjectPath}' is not a supported Blazor app. {reason} " +
            "Pass a Blazor Web App or Blazor Server project (Microsoft.NET.Sdk.Web with Razor components), a standalone Blazor WebAssembly project (Microsoft.NET.Sdk.BlazorWebAssembly) or a .NET MAUI Blazor Hybrid project to --project.";
    }

    /// <summary>
    /// Returns an error when the project references a commercial Ignite UI for Blazor package
    /// (see <see cref="CommercialPackageIds"/>), or null when it does not.
    /// </summary>
    internal static string? GetCommercialPackageConflictError(ProjectEvaluation evaluation)
    {
        var reference = evaluation.PackageReferences.FirstOrDefault(item =>
            CommercialPackageIds.Contains(item.EvaluatedInclude, StringComparer.OrdinalIgnoreCase));
        if (reference is null)
        {
            return null;
        }

        var definedIn = string.Equals(Path.GetFullPath(reference.DefiningProjectFullPath), Path.GetFullPath(evaluation.ProjectPath), StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" (declared in '{reference.DefiningProjectFullPath}')";
        return $"'{evaluation.ProjectPath}' references the commercial {reference.EvaluatedInclude} package{definedIn}. " +
            $"It already contains every IgniteUI.Blazor.Lite component, so adding IgniteUI.Blazor.Lite would introduce duplicate components in the {IgniteUIBlazorHelper.ControlsNamespace} namespace. " +
            $"To keep {reference.EvaluatedInclude}, set it up by following the Ignite UI for Blazor documentation instead of running this scaffolder. " +
            $"To switch to the MIT-licensed packages, remove the {reference.EvaluatedInclude} PackageReference and re-run the scaffolder.";
    }

    /// <summary>
    /// Adds the items of one inner build, evaluated with 'dotnet msbuild -p:TargetFramework=... -getItem:...'.
    /// </summary>
    private static bool TryGetTargetFrameworkItems(string projectPath, string targetFramework, string[] itemTypes, List<EvaluatedProjectItem> items, out string? error)
    {
        var runner = DotnetCliRunner.CreateDotNet("msbuild",
            [$"-p:{TargetFrameworkProperty}={targetFramework}", .. itemTypes.Select(itemType => $"-getItem:{itemType}"), projectPath]);
        var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var stdErr);
        if (exitCode != 0 || string.IsNullOrWhiteSpace(stdOut))
        {
            error = string.Join(" ", new[] { stdOut, stdErr }.Where(output => !string.IsNullOrWhiteSpace(output)).Select(output => output!.Trim()));
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(stdOut);
            if (document.RootElement.TryGetProperty("Items", out var itemsByType))
            {
                foreach (var itemType in itemTypes)
                {
                    if (!itemsByType.TryGetProperty(itemType, out var itemsOfType))
                    {
                        continue;
                    }

                    foreach (var item in itemsOfType.EnumerateArray())
                    {
                        items.Add(new EvaluatedProjectItem(
                            itemType,
                            item.GetProperty("Identity").GetString() ?? string.Empty,
                            item.TryGetProperty("DefiningProjectFullPath", out var definingProject) ? definingProject.GetString() ?? projectPath : projectPath));
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            error = $"Unexpected 'dotnet msbuild' output: {ex.Message}";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsTrue(string value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
