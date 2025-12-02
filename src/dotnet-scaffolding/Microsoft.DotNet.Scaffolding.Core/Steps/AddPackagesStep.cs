// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Steps;

/// <summary>
/// A scaffold step that adds NuGet packages to a project.
/// </summary>
public class AddPackagesStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the list of package names to add.
    /// </summary>
    public required IReadOnlyList<Package> Packages { get; set; }

    /// <summary>
    /// Gets or sets the path to the project file.
    /// </summary>
    public required string ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include prerelease versions of packages.
    /// </summary>
    public bool Prerelease { get; set; } = false;

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddPackagesStep"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for output.</param>
    public AddPackagesStep(ILogger<AddPackagesStep> logger)
    {
        _logger = logger;
        ContinueOnError = true;
    }

    /// <summary>
    /// Executes the step to add the specified packages to the project.
    /// </summary>
    /// <param name="context">The scaffolder context for the current operation.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>True if the packages were added successfully; otherwise, false.</returns>
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        // Try to get the target framework from the contextcls
        string? targetFramework = context.GetSpecifiedTargetFramework();

        foreach (Package package in Packages)
        {
            // Resolve the package version based on the target framework if needed
            Package resolvedPackage = package;
            if (package.IsVersionRequired && !string.IsNullOrEmpty(targetFramework))
            {
                resolvedPackage = await package.WithResolvedVersionAsync(targetFramework);
            }

            // Add the package to the project
            DotnetCommands.AddPackage(
                packageName: resolvedPackage.Name,
                logger: _logger,
                projectFile: ProjectPath,
                packageVersion: Prerelease ? null : resolvedPackage.PackageVersion,
                includePrerelease: Prerelease);
        }

        return true;
    }
}
