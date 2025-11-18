// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Scaffolding.Core.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step for creating the folder layout for an ASP.NET Core Area, including Controllers, Models, Data, and Views folders.
/// </summary>
internal class AreaScaffolderStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the project file path.
    /// </summary>
    public string? Project { get; set; }
    /// <summary>
    /// Gets or sets the name of the area to scaffold.
    /// </summary>
    public string? Name { get; set; }
    private readonly IScaffolderLogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentService _environmentService;
    /// <summary>
    /// Initializes a new instance of the <see cref="AreaScaffolderStep"/> class.
    /// </summary>
    public AreaScaffolderStep(IFileSystem fileSystem, IScaffolderLogger logger, IEnvironmentService environmentService)
    {
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Validates the area command settings and returns the settings object if valid.
    /// </summary>
    private AreaStepSettings? ValidateAreaCommandSettings()
    {
        if (string.IsNullOrEmpty(Name))
        {
            _logger.LogError("Missing/Invalid --name option.");
            return null;
        }

        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError("Missing/Invalid --project option.");
            return null;
        }

        return new AreaStepSettings
        {
            Project = Project,
            Name = Name,
        };
    }

    /// <summary>
    /// Ensures the folder layout for the area exists, creating necessary directories.
    /// </summary>
    private void EnsureFolderLayout(AreaStepSettings stepSettings)
    {
        _logger.LogInformation($"Adding area '{stepSettings.Name}'...");
        var basePath = _environmentService.CurrentDirectory;
        var projectDirectoryPath = Path.GetDirectoryName(stepSettings.Project);
        if (!string.IsNullOrEmpty(projectDirectoryPath) && _fileSystem.DirectoryExists(projectDirectoryPath))
        {
            basePath = projectDirectoryPath;
        }

        var areaBasePath = Path.Combine(basePath, "Areas");
        _fileSystem.CreateDirectoryIfNotExists(areaBasePath);

        var areaPath = Path.Combine(areaBasePath, stepSettings.Name);
        _fileSystem.CreateDirectoryIfNotExists(areaPath);

        foreach (var areaFolder in AreaFolders)
        {
            var path = Path.Combine(areaPath, areaFolder);
            _fileSystem.CreateDirectoryIfNotExists(path);
        }

        _logger.LogInformation("Done");
    }

    /// <inheritdoc />
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var stepSettings = ValidateAreaCommandSettings();
        if (stepSettings is null)
        {
            return Task.FromResult(false);
        }

        _logger.LogInformation("Updating project...");
        EnsureFolderLayout(stepSettings);
        _logger.LogInformation("Finished");
        return Task.FromResult(true);
    }

    private static readonly string[] AreaFolders =
    [
        "Controllers",
        "Models",
        "Data",
        "Views"
    ];
}
