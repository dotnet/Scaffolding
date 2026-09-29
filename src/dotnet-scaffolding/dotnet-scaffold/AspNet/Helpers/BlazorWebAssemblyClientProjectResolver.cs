// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Finds referenced Blazor WebAssembly projects without deciding whether a client is required.
/// </summary>
internal static class BlazorWebAssemblyClientProjectResolver
{
    /// <summary>
    /// Resolves the single referenced WebAssembly client. Returns true with a null client when none is present;
    /// returns false with a diagnostic when project evaluation fails or multiple clients are found.
    /// The caller decides whether a missing client is an error.
    /// </summary>
    internal static bool TryGetClient(
        string? projectPath,
        IFileSystem fileSystem,
        out (string ProjectPath, string RootNamespace)? client,
        out string? error)
    {
        client = null;
        if (string.IsNullOrEmpty(projectPath))
        {
            error = "Unable to resolve the Blazor WebAssembly client project because the server project path is unavailable.";
            return false;
        }

        var projectService = new MSBuildProjectService(projectPath);
        if (!projectService.TryGetProjectReferences(out var references, out error))
        {
            error = $"Unable to evaluate project references for '{projectPath}'. {error} Ensure the project's SDK and imports are available.";
            return false;
        }

        var matches = new List<(string ProjectPath, string RootNamespace)>();
        foreach (var reference in references.Where(fileSystem.FileExists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var clientProjectService = new MSBuildProjectService(reference);
            if (!clientProjectService.TryGetEvaluatedProperties(
                ["UsingMicrosoftNETSdkBlazorWebAssembly", "RootNamespace"],
                out var properties,
                out error))
            {
                error = $"Unable to evaluate referenced project '{reference}' while resolving the Blazor WebAssembly client for '{projectPath}'. {error} Ensure the referenced project's SDK and imports are available.";
                return false;
            }

            if (!string.Equals(properties["UsingMicrosoftNETSdkBlazorWebAssembly"], "true", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rootNamespace = properties["RootNamespace"];
            if (string.IsNullOrEmpty(rootNamespace))
            {
                rootNamespace = Path.GetFileNameWithoutExtension(reference);
            }

            matches.Add((reference, rootNamespace));
        }

        if (matches.Count > 1)
        {
            error = $"Unable to resolve the Blazor WebAssembly client project for '{projectPath}'. Multiple referenced projects use the Microsoft.NET.Sdk.BlazorWebAssembly SDK: {string.Join(", ", matches.Select(match => match.ProjectPath))}. Ensure the server project has exactly one ProjectReference to its Blazor WebAssembly client.";
            return false;
        }

        client = matches.Count == 1 ? matches[0] : null;
        error = null;
        return true;
    }
}
