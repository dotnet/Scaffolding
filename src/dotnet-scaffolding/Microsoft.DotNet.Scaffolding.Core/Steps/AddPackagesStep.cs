// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Steps;

public class AddPackagesStep : ScaffoldStep
{
    //key is the name of the packages, value is the version. version is not required.
    public required IDictionary<string, string?> Packages { get; set; }
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
        foreach (var package in Packages)
        {
            DotnetCommands.AddPackage(
                packageName: package.Key,
                logger: _logger,
                projectFile: ProjectPath,
                packageVersion: package.Value,
                includePrerelease: Prerelease);
        }

        return Task.FromResult(true);
    }
}
