// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Steps;

internal class AddPackageReferenceStep(string targetProject, string packageName, bool prerelease, ILogger logger, IFileSystem fileSystem) : ScaffoldStep
{
    private readonly string _targetProject = targetProject;
    private readonly string _packageName = packageName;
    private readonly bool _prerelease = prerelease;
    private readonly ILogger _logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;

    public override Task<bool> ExecuteAsync()
    {
        if (_fileSystem.FileExists(_targetProject))
        {
            DotnetCommands.AddPackage(
                packageName: _packageName,
                logger: _logger,
                projectFile: _targetProject,
                includePrerelease: _prerelease);

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
