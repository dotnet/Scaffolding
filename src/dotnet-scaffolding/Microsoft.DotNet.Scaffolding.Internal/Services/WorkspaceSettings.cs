// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.DotNet.Scaffolding.Internal.Extensions;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;

/// <summary>
/// Workspace settings are initialized in following order:
///
/// - by default we get values from the environment variables
/// - then when command line arguments are passed these settings could be overridden
/// - then when we discover any environment variables to be set/modified during IStartup executions,
///   we could set those settings if they were not provided before by earlier steps.
///
/// This way we let shell environment variables override what we could potentially pick for msbuild or VS paths,
/// then user could explicitly specify those paths, and only then if nothing happened we would set them ourselves
/// to the value we think is the best on current machine.
/// </summary>
internal class WorkspaceSettings
{
    public WorkspaceSettings()
    {
        MSBuildPath = System.Environment.GetEnvironmentVariable("MSBuildExtensionsPath");
        VisualStudioPath = System.Environment.GetEnvironmentVariable("VSINSTALLDIR");
        VisualStudioVersion = System.Environment.GetEnvironmentVariable("VisualStudioVersion");
    }

    private string? _inputPath;
    public string? InputPath
    {
        get => _inputPath;
        set => _inputPath = value?.WithOsPathSeparators();
    }

    private string? _msbuildPath;
    public string? MSBuildPath
    {
        get => _msbuildPath;
        set => _msbuildPath = value?.WithOsPathSeparators();
    }

    private string? _visualStudioPath;
    public string? VisualStudioPath
    {
        get => _visualStudioPath;
        set => _visualStudioPath = value?.WithOsPathSeparators();
    }

    public string? VisualStudioVersion { get; set; }
}
