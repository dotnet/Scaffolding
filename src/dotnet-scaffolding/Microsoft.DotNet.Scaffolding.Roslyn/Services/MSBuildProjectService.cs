// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.DotNet.Scaffolding.Roslyn.Helpers;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;

/// <summary>
/// Provides project capabilities, target frameworks, evaluated project references, and properties.
/// </summary>
/// <remarks>
/// Call <see cref="MsBuildInitializer.Initialize"/> before using this service so MSBuild is registered
/// before Microsoft.Build assemblies are loaded.
/// </remarks>
public class MSBuildProjectService : IMSBuildProjectService
{
    private readonly string _projectPath;
    private Project? _project;
    private bool _initialized;
    private readonly object _initLock = new();
    public MSBuildProjectService(string projectPath)
    {
        _projectPath = projectPath;
    }

    public string? GetLowestTargetFramework(bool refresh = false)
    {
        EnsureInitialized(refresh);
        if (_project is not null)
        {
            return MSBuildProjectServiceHelper.GetLowestTargetFramework(_project);
        }

        return null;
    }

    public IEnumerable<string> GetProjectCapabilities(bool refresh = false)
    {
        EnsureInitialized();
        if (_project is not null)
        {
            return MSBuildProjectServiceHelper.GetProjectCapabilities(_project);
        }

        return [];
    }

    /// <summary>
    /// Gets the absolute paths of evaluated ProjectReference items.
    /// </summary>
    /// <remarks>
    /// Resolves imports, conditions, and property substitutions using a fresh project collection that is disposed after evaluation.
    /// Unlike the cached capabilities evaluation, missing required SDKs or imports cause evaluation to fail.
    /// Check the return value before treating an empty reference list as valid.
    /// </remarks>
    /// <param name="projectReferences">The evaluated reference paths, or an empty list on evaluation failure.</param>
    /// <param name="error">The project evaluation diagnostic on failure, or null on success.</param>
    /// <returns>True if evaluation succeeds, including when there are no references; otherwise, false.</returns>
    public bool TryGetProjectReferences(out IReadOnlyList<string> projectReferences, out string? error)
    {
        try
        {
            // Do not reuse the capabilities evaluation: it permits missing imports.
            using var projects = new ProjectCollection();
            var project = new Project(_projectPath, null, null, projects);
            projectReferences = project.GetItems("ProjectReference")
                .Select(reference => reference.GetMetadataValue("FullPath"))
                .ToList();
            error = null;
            return true;
        }
        catch (InvalidProjectFileException ex)
        {
            projectReferences = [];
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Gets the evaluated values of the requested MSBuild properties.
    /// </summary>
    /// <remarks>
    /// Resolves imports, conditions, and property substitutions using a fresh project collection that is disposed after evaluation.
    /// Missing required SDKs or imports cause evaluation to fail.
    /// </remarks>
    /// <param name="propertyNames">The names of the properties to evaluate.</param>
    /// <param name="propertyValues">
    /// A case-insensitive dictionary of requested properties, or an empty dictionary on evaluation failure.
    /// Properties that are not defined have empty string values.
    /// </param>
    /// <param name="error">The project evaluation diagnostic on failure, or null on success.</param>
    /// <returns>True if evaluation succeeds; otherwise, false.</returns>
    public bool TryGetEvaluatedProperties(
        IEnumerable<string> propertyNames,
        out IReadOnlyDictionary<string, string> propertyValues,
        out string? error)
    {
        try
        {
            using var projects = new ProjectCollection();
            var project = new Project(_projectPath, null, null, projects);
            propertyValues = propertyNames.ToDictionary(
                propertyName => propertyName,
                project.GetPropertyValue,
                StringComparer.OrdinalIgnoreCase);
            error = null;
            return true;
        }
        catch (InvalidProjectFileException ex)
        {
            propertyValues = new Dictionary<string, string>();
            error = ex.Message;
            return false;
        }
    }

    private void Initialize(bool refresh = false)
    {
        lock (_initLock)
        {
            if (_initialized)
            {
                return;
            }

            if (_project is not null && !refresh)
            {
                return;
            }

            try
            {
                //try loading MSBuild project the faster way.
                _project = new Project(_projectPath, null, null, new ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports);
            }
            catch (Exception) { }
        }
    }

    private void EnsureInitialized(bool refresh = false)
    {
        if (!_initialized)
        {
            Initialize(refresh);
        }

        _initialized = true;
    }
}
