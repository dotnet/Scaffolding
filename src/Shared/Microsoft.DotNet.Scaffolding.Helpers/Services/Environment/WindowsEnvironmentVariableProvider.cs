// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Runtime.InteropServices;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Extensions;

namespace Microsoft.DotNet.Scaffolding.Helpers.Environment;
public class WindowsEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    private readonly IAppSettings _settings;
    public WindowsEnvironmentVariableProvider(IAppSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<KeyValuePair<string, string>>?> GetEnvironmentVariablesAsync()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetWindowsEnvironmentVariablesAsync()
            : new ValueTask<IEnumerable<KeyValuePair<string, string>>?>((IEnumerable<KeyValuePair<string, string>>?)null);
    }

    private ValueTask<IEnumerable<KeyValuePair<string, string>>?> GetWindowsEnvironmentVariablesAsync()
    {
        var variables = new Dictionary<string, string>();
        var workspace = _settings.Workspace();
        var (visualStudioPath, msbuildPath, visualStudioVersion) = GetLatestVisualStudioPath();
        if (!string.IsNullOrEmpty(visualStudioPath))
        {
            workspace.VisualStudioPath ??= visualStudioPath.WithOsPathSeparators();
            workspace.MSBuildPath = string.IsNullOrEmpty(msbuildPath) ? Path.Combine(visualStudioPath, "MSBuild") : msbuildPath;

            //variables.Add(Constants.EnvironmentVariables.VSINSTALLDIR, workspace.VisualStudioPath);
            variables.Add(Constants.EnvironmentVariables.MSBuildExtensionsPath32, workspace.MSBuildPath);
            variables.Add(Constants.EnvironmentVariables.MSBuildExtensionsPath, workspace.MSBuildPath);
        }

        if (visualStudioVersion is int version)
        {
            workspace.VisualStudioVersion ??= $"{version}.0";

            //variables.Add("VisualStudioVersion", workspace.VisualStudioVersion);
        }

        variables.Add(Constants.EnvironmentVariables.USERPROFILE, System.Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.USERPROFILE) ?? string.Empty);

        return new ValueTask<IEnumerable<KeyValuePair<string, string>>?>(variables);
    }

    private (string? Path, string? MsBuildPath, int? Version) GetLatestVisualStudioPath()
    {
        var latest = GetLatestVisualStudio();
        if (latest.InstallPath is null)
        {
            return default;
        }

        return (latest.InstallPath, latest.MsBuildPath, latest.Version?.Major);
    }

    private (string? InstallPath, string? MsBuildPath, Version? Version) GetLatestVisualStudio()
    {
        //var vsInstances = MSBuildLocator.QueryVisualStudioInstances();
        string? latestPath = null;
        string? latestMsBuildPath = null;
        Version? latestVersion = null;
/*        foreach (var instance in vsInstances)
        {
            if (latestVersion is null || instance.Version > latestVersion)
            {
                latestPath = instance.VisualStudioRootPath;
                latestMsBuildPath = instance.MSBuildPath;
                latestVersion = instance.Version;
            }
        }*/

        return (latestPath, latestMsBuildPath, latestVersion);
    }
}
