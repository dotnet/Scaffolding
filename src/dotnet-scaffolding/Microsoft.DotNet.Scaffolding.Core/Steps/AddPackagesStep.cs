// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Steps;

public class AddPackagesStep : ScaffoldStep
{
    public required IList<string> PackageNames { get; set; }
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
        foreach (var packageName in PackageNames)
        {
            DotnetCommands.AddPackage(
                packageName: packageName,
                logger: _logger,
                projectFile: ProjectPath,
                includePrerelease: Prerelease);
        }

        return Task.FromResult(true);
    }
}
