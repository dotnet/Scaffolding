// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Scaffolding.Roslyn.Helpers;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;

public class MSBuildProjectService : IMSBuildProjectService
{
    private readonly string _projectPath;
    private Build.Evaluation.Project? _project;
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

            //try loading MSBuild project the faster way.
            _project = new Project(_projectPath, null, null, new Build.Evaluation.ProjectCollection(), ProjectLoadSettings.IgnoreMissingImports);
            //if fails, use the longer way using GLobalProjectCollection.
            if (_project is null)
            {
                _project = ProjectCollection.GlobalProjectCollection.LoadProject(_projectPath);
            }
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
