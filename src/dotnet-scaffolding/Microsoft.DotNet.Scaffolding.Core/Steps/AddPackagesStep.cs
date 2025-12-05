// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Steps;

public class AddPackagesStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the list of package names to add.
    /// </summary>
    public required IList<Package> Packages { get; set; }

    /// <summary>
    /// Gets or sets the path to the project file.
    /// </summary>
    public required string ProjectPath { get; set; }
    public bool Prerelease { get; set; } = false;
    private readonly ILogger _logger;

    public AddPackagesStep(ILogger<AddPackagesStep> logger)
    {
        _logger = logger;
        ContinueOnError = true;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        foreach (Package package in Packages)
        {
            // add package version here
            DotnetCommands.AddPackage(
                packageName: package.Name,
                logger: _logger,
                projectFile: ProjectPath,
                packageVersion: package.Version,
                includePrerelease: Prerelease);
        }

        return Task.FromResult(true);
    }
}
