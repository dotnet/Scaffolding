// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps;

internal class AddPackagesStep : ScaffoldStep
{
    public required IList<string> PackageNames { get; init; }
    public required string ProjectPath { get; init; }
    public required ILogger Logger { get; init; }
    public bool Prerelease { get; set; } = false;

    public override Task<bool> ExecuteAsync()
    {
        foreach (var packageName in PackageNames)
        {
            if (!string.IsNullOrEmpty(packageName))
            {
                DotnetCommands.AddPackage(
                    packageName: packageName,
                    logger: Logger,
                    projectFile: ProjectPath,
                    includePrerelease: Prerelease);
            }
        }

        return Task.FromResult(true);
    }
}
